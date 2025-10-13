using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// 특수 이벤트 전용 매니저
/// - 기존 서브이벤트(EventManager) 로직은 건드리지 않고 별도 체인으로 동작
/// - 24개 파라미터 이벤트 플레이리스트와 무관하게 실시간 조건을 평가하여 호출
/// - 선택지 처리/연출은 UIFlowSimulator가 이미 구독하는 카드 스와이프 UI를 그대로 사용
/// </summary>
public class SpecialEventManager : MonoBehaviour
{
    public static SpecialEventManager Instance { get; private set; }

    // UIFlowSimulator가 구독할 이벤트 (서브이벤트와 동일한 시그니처를 사용)
    public static event Action<FullSubEventData> OnSpecialEventReady;
    public static event Action<float> OnSpecialEventExitFadeRequested;
    public static event Action OnSpecialEventChainEnded;

    [Header("트리거 설정 (인스펙터에서 정의)")]
    public List<SpecialTrigger> triggers = new List<SpecialTrigger>();

    // 체인 상태
    private bool inSpecialChain = false;
    private int currentEventId = 0;
    private Dictionary<int, FullSubEventData> idToEvent = new Dictionary<int, FullSubEventData>();
        // 현재 진행 중인 특수 체인을 유발한 트리거 (UI 표출을 위한 보상 스킬 참조용)
        private SpecialTrigger activeTrigger = null;

    // 스킬 보상 매핑 (AnswerID -> SkillID 문자열)
    private Dictionary<int, string> answerToSkillId = new Dictionary<int, string>();

    // 스킬 획득 프롬프트 상태
    private bool inSkillPrompt = false;
    private string pendingSkillId;
        private int pendingNextEventIdAfterSkillPrompt;
        // 오버레이 선택 결과에 따라 분기 제어
        private int pendingNextEventIdIfAccept;
        private int pendingNextEventIdIfReject;
        private List<ParameterChange> pendingAcceptParamChanges;
        private List<ParameterChange> pendingRejectParamChanges;
        private SkillData pendingSkillData; // UI에서 바로 표시할 스킬 에셋 (트리거에서 지정)
    // 파라미터 변화가 발생한 직후에만 특수 이벤트를 시도하기 위한 트리거 윈도우 플래그
    private bool triggerWindowOpen = false;
    // 같은 프레임/같은 변화에서 중복 트리거 방지
    private int triggerWindowOpenFrame = -1;
    // 회차(한 사이클) 동안 이미 본 특수이벤트 그룹(SubStoryPac+StoryNum) 기록
    private HashSet<(int pac, int num)> seenGroupsThisPlaythrough = new HashSet<(int pac, int num)>();

    public bool IsInSpecialChain => inSpecialChain;
    public string GetPendingSkillId() => pendingSkillId;
        public SkillData GetPendingSkillData() => pendingSkillData;
    public void OpenTriggerWindow()
    {
        triggerWindowOpen = true;
        triggerWindowOpenFrame = Time.frameCount;
    }
    public void CloseTriggerWindow()
    {
        triggerWindowOpen = false;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private async void Start()
    {
        // DataManager 준비 완료를 기다린 후 데이터 인덱싱
        if (DataManager.Instance != null)
        {
            await DataManager.Instance.IsReady;
            IndexSpecialEvents();
            await LoadAnswerSkillMapAsync();
            // 사이클 게이팅 초기화
            InitializeFlowGatingState();
            // 회차 시작 시 중복 방지 상태 초기화
            seenGroupsThisPlaythrough.Clear();
        }
    }

    /// <summary>
    /// [변경] 특수 이벤트는 사이클 종료 후(24개 소진) 등장. 즉시 체인을 시작할지를 평가합니다.
    /// </summary>
    public bool TryTriggerAfterCycle()
    {
        if (inSpecialChain || triggers == null || triggers.Count == 0) return false;
        if (DataManager.Instance == null || DataManager.Instance.PlayerData == null) return false;
        // 1장부터 허용
        if (!IsChapterAllowedForChapter1()) return false;

        // 등급 우선순위로 후보를 모으고, 최상위 등급끼리 랜덤 선택
        var satisfied = new List<SpecialTrigger>();
        foreach (var trig in triggers)
        {
            if (EvaluateTrigger(trig)) satisfied.Add(trig);
        }
        if (satisfied.Count == 0) return false;

        // 이번 회차에서 이미 본 그룹은 제외
        satisfied = satisfied
            .Where(t => !seenGroupsThisPlaythrough.Contains((t.storyPac, t.storyNum)))
            .ToList();
        if (satisfied.Count == 0) return false;

        int maxPriority = satisfied.Max(t => t.priority);
        var top = satisfied.Where(t => t.priority == maxPriority).ToList();
        var chosen = top.OrderBy(_ => System.Guid.NewGuid()).First();

        var firstEvent = FindFirstEventOfGroup(chosen.storyPac, chosen.storyNum);
        if (firstEvent == null) return false;

        // 트리거 고정 (보상 스킬 에셋 표출용)
        activeTrigger = chosen;
        // 선택된 그룹을 회차 기록에 추가
        seenGroupsThisPlaythrough.Add((chosen.storyPac, chosen.storyNum));
        StartChain(firstEvent.ID);
        return true;
    }

    public async void OnSubEventChoiceSelected(bool isLeftChoice)
    {
        if (!inSpecialChain) return;

        var current = Resolve(currentEventId);
        if (current == null)
        {
            EndChain();
            return;
        }
        var selected = isLeftChoice ? current.leftChoice : current.rightChoice;
        if (selected == null)
        {
            EndChain();
            return;
        }

        // 현재 이벤트가 스킬 수락/거부를 담은 이벤트인지 먼저 검사하여 프롬프트로 위임
        // 두 선택지 중 하나라도 스킬 보상 매핑이 있으면 프롬프트로 전환
        int leftAnswer = current.leftChoice != null ? current.leftChoice.answerID : 0;
        int rightAnswer = current.rightChoice != null ? current.rightChoice.answerID : 0;
        string skillIdFromLeft = (leftAnswer != 0 && answerToSkillId.TryGetValue(leftAnswer, out var s1) && !string.IsNullOrEmpty(s1) && !IsNullLiteral(s1)) ? s1 : null;
        string skillIdFromRight = (rightAnswer != 0 && answerToSkillId.TryGetValue(rightAnswer, out var s2) && !string.IsNullOrEmpty(s2) && !IsNullLiteral(s2)) ? s2 : null;
        bool isSkillDecisionEvent = !string.IsNullOrEmpty(skillIdFromLeft) || !string.IsNullOrEmpty(skillIdFromRight);

        if (isSkillDecisionEvent && !inSkillPrompt)
        {
            // 수락 선택지는 스킬이 매핑된 쪽, 거부는 반대쪽으로 간주
            bool leftIsAccept = !string.IsNullOrEmpty(skillIdFromLeft);
            var acceptChoice = leftIsAccept ? current.leftChoice : current.rightChoice;
            var rejectChoice = leftIsAccept ? current.rightChoice : current.leftChoice;

            pendingSkillId = leftIsAccept ? skillIdFromLeft : skillIdFromRight;
            pendingNextEventIdIfAccept = acceptChoice != null ? acceptChoice.nextEventID : 0;
            pendingNextEventIdIfReject = rejectChoice != null ? rejectChoice.nextEventID : 0;
            pendingAcceptParamChanges = acceptChoice != null && acceptChoice.outcome != null ? (acceptChoice.outcome.parameterChanges ?? new List<ParameterChange>()) : new List<ParameterChange>();
            pendingRejectParamChanges = rejectChoice != null && rejectChoice.outcome != null ? (rejectChoice.outcome.parameterChanges ?? new List<ParameterChange>()) : new List<ParameterChange>();

            // 카드 리셋 시간 대기 후 프롬프트 표시
            await UniTask.Delay(System.TimeSpan.FromSeconds(0.5));
            ShowSkillPrompt();
            return;
        }

        // 스킬 결정 이벤트가 아니면 기존 파라미터 변화 적용
        if (selected.outcome != null && selected.outcome.parameterChanges != null)
        {
            GamePlayerStats.Instance.ApplyChanges(selected.outcome.parameterChanges);
        }

        // 스킬 보상 확인
        if (!inSkillPrompt)
        {
            if (answerToSkillId.TryGetValue(selected.answerID, out var skillId) && !string.IsNullOrEmpty(skillId) && !IsNullLiteral(skillId))
            {
                // 스킬 프롬프트 표시 (선택으로 수락/거부)
                pendingSkillId = skillId;
                pendingNextEventIdAfterSkillPrompt = selected.nextEventID;
                // 카드 오프스크린/리셋 연출 시간과 동일하게 대기 후 프롬프트를 띄움 (0.5s)
                await UniTask.Delay(System.TimeSpan.FromSeconds(0.5));
                ShowSkillPrompt();
                return; // 프롬프트가 끝나면 다시 체인 진행
            }
        }
        else
        {
            // 스킬 프롬프트 처리
            if (isLeftChoice)
            {
                ApplySkill(pendingSkillId);
                // 수락 시 파라미터 변화 적용 및 해당 분기로 이동
                if (pendingAcceptParamChanges != null && pendingAcceptParamChanges.Count > 0)
                {
                    GamePlayerStats.Instance.ApplyChanges(pendingAcceptParamChanges);
                }
                inSkillPrompt = false;
                await GoNextAsync(pendingNextEventIdIfAccept != 0 ? pendingNextEventIdIfAccept : pendingNextEventIdAfterSkillPrompt);
            }
            else
            {
                // 거부 시 파라미터 변화 적용 및 해당 분기로 이동
                if (pendingRejectParamChanges != null && pendingRejectParamChanges.Count > 0)
                {
                    GamePlayerStats.Instance.ApplyChanges(pendingRejectParamChanges);
                }
                inSkillPrompt = false;
                await GoNextAsync(pendingNextEventIdIfReject != 0 ? pendingNextEventIdIfReject : pendingNextEventIdAfterSkillPrompt);
            }
            return;
        }

        // 일반 체인 진행 (대기 없이 카드 리셋과 동시에 다음 텍스트 표시)
        await GoNextAsync(selected.nextEventID);
    }

    private async UniTask GoNextAsync(int nextId)
    {
        if (nextId <= 0)
        {
            // 체인 종료 → 페이드 아웃 요청 후 끝
            RequestExitFade(0.5f);
            EndChain();
            return;
        }
        // 서브이벤트와 동일하게 카드 오프스크린/리셋 연출 시간을 고려해 대기 후 시작 (0.5s)
        await UniTask.Delay(System.TimeSpan.FromSeconds(0.5));
        StartChain(nextId);
    }

    private void StartChain(int firstEventId)
    {
        inSpecialChain = true;
        currentEventId = firstEventId;
        var data = Resolve(firstEventId);
        if (data == null)
        {
            EndChain();
            return;
        }
        // 사이클 카운트 증가
        specialChainsTriggeredThisCycle++;
        OnSpecialEventReady?.Invoke(data);
    }

    private void EndChain()
    {
        inSpecialChain = false;
        inSkillPrompt = false;
        pendingSkillId = null;
        pendingNextEventIdAfterSkillPrompt = 0;
        pendingSkillData = null;
        activeTrigger = null;
        OnSpecialEventChainEnded?.Invoke();
    }

    private void RequestExitFade(float duration)
    {
        OnSpecialEventExitFadeRequested?.Invoke(duration);
    }

    private FullSubEventData Resolve(int id)
    {
        if (idToEvent.TryGetValue(id, out var d)) return d;
        return null;
    }

    private void IndexSpecialEvents()
    {
        idToEvent.Clear();
        // 특수 이벤트는 DataManager의 전용 목록(FullSpecialEvents)에 보관됩니다. 서브이벤트와 절대 혼합 금지.
        var list = DataManager.Instance.FullSpecialEvents ?? new List<FullSubEventData>();
        foreach (var e in list.Where(e => e != null))
        {
            if (!idToEvent.ContainsKey(e.ID)) idToEvent.Add(e.ID, e);
        }
    }

    private async UniTask LoadAnswerSkillMapAsync()
    {
        try
        {
            var rows = await Csvparser.ParseAsync<SpecialAnswerSkillRow>("SpecialEventAnswerID");
            answerToSkillId = rows
                .Where(r => r != null)
                .GroupBy(r => r.AnswerID)
                .ToDictionary(g => g.Key, g => g.First().AnswerReward_Skill);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SpecialEventManager] 스킬 보상 맵 로드 실패: {ex.Message}");
        }
    }

    private bool EvaluateTrigger(SpecialTrigger trig)
    {
        if (trig == null || trig.rules == null || trig.rules.Count == 0) return false;

        foreach (var r in trig.rules)
        {
            int v = GamePlayerStats.Instance.GetStat(r.parameterType);
            bool ok = r.op == SpecialTrigger.CompareOp.GreaterOrEqual ? (v >= r.value) : (v <= r.value);
            if (!ok) return false;
        }
        return true;
    }

    private FullSubEventData FindFirstEventOfGroup(int storyPac, int storyNum)
    {
        if (DataManager.Instance == null || DataManager.Instance.FullSpecialEvents == null) return null;
        return DataManager.Instance.FullSpecialEvents
            .Where(e => e.SubStoryPac == storyPac && e.StoryNum == storyNum)
            .OrderBy(e => e.ID)
            .FirstOrDefault();
    }

    private void ShowSkillPrompt()
    {
        // 프롬프트용 가짜 이벤트 구성 (텍스트/선택지만 사용)
        inSkillPrompt = true;

        string newSkillName = pendingSkillId;
        var player = FindObjectOfType<WarPlayer>();
        bool hasCurrent = (player != null && player.currentSkill != null);

        // 트리거에서 직접 할당된 스킬 에셋을 UI에 표출하도록 대기 상태에 저장
        pendingSkillData = activeTrigger != null ? activeTrigger.rewardSkill : null;

        var prompt = new FullSubEventData
        {
            ID = -999999, // 임시 ID (데이터와 충돌 없음)
            SubStoryPac = 1000000,
            StoryNum = 0,
            Text_kr = hasCurrent ? "스킬을 교체한다." : "스킬을 획득한다.",
            leftChoice = new SubChoice
            {
                answerID = -1,
                choiceText = hasCurrent ? "교체한다" : "획득한다",
                nextEventID = -1,
                outcome = new ChoiceOutcome { parameterChanges = new List<ParameterChange>() }
            },
            rightChoice = new SubChoice
            {
                answerID = -2,
                choiceText = hasCurrent ? "교체하지 않는다" : "획득하지 않는다",
                nextEventID = -1,
                outcome = new ChoiceOutcome { parameterChanges = new List<ParameterChange>() }
            }
        };

        OnSpecialEventReady?.Invoke(prompt);
    }

    private void ApplySkill(string skillId)
    {
        if (string.IsNullOrEmpty(skillId)) return;
        var player = FindObjectOfType<WarPlayer>();
        if (player == null)
        {
            // 전투 장면이 아니라면 PlayerPrefs에 저장하여 다음 전투에서 로드
            PlayerPrefs.SetString("EquippedSkillID", skillId);
            PlayerPrefs.Save();
            return;
        }

        // 트리거에서 지정한 스킬 에셋이 있으면 직접 장착(아이콘/이름 즉시 반영)
        if (pendingSkillData != null)
        {
            player.EquipSkill(pendingSkillData);
            PlayerPrefs.SetString("EquippedSkillID", pendingSkillData.skillID);
            PlayerPrefs.Save();
            // 전투 HUD가 존재하면 즉시 UI 반영
            var hud = FindObjectOfType<WarHUD>();
            if (hud != null) hud.UpdateAllUI();
            return;
        }

        // 에셋이 없으면 ID 기준으로 데이터베이스에서 로드
        player.equippedSkillID = skillId;
        player.LoadSkillFromID();
        PlayerPrefs.SetString("EquippedSkillID", skillId);
        PlayerPrefs.Save();
        // 전투 HUD가 존재하면 즉시 UI 반영
        {
            var hud = FindObjectOfType<WarHUD>();
            if (hud != null) hud.UpdateAllUI();
        }
    }

    private bool IsNullLiteral(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return true;
        var t = s.Trim();
        return string.Equals(t, "Null", StringComparison.OrdinalIgnoreCase) || string.Equals(t, "NULL", StringComparison.OrdinalIgnoreCase);
    }

    [Serializable]
    public class SpecialTrigger
    {
        public string name;
        public int storyPac = 1000001; // 특수 팩 기본값
        public int storyNum = 10000001; // 체인 그룹 번호(시작)
        [Range(1, 10)] public int priority = 1; // 등급/우선순위 (높을수록 먼저)
        public List<ParamRule> rules = new List<ParamRule>();
        [Header("보상 스킬 (UI 표출용, 이벤트 플로우에는 영향 없음)")]
        public SkillData rewardSkill; // 각 트리거별로 에셋을 인스펙터에서 직접 할당

        [Serializable]
        public class ParamRule
        {
            public ParameterType parameterType;
            public CompareOp op = CompareOp.GreaterOrEqual;
            public int value;
        }

        public enum CompareOp { GreaterOrEqual, LessOrEqual }
    }

    // -------------------------- 흐름 게이팅: 서브이벤트와 유사한 규칙 --------------------------
    [Header("Flow Gating Settings (SubEvent-like, do NOT merge data)")]
    [Tooltip("특수 이벤트가 등장 가능한 최소 챕터 (포함)")]
    [SerializeField] private int minChapterForSpecial = 1; // 1챕터부터 등장
    [Tooltip("플레이리스트 내에서 특수 이벤트를 끼워넣을 수 있는 시작 인덱스 (0-based)")]
    [SerializeField] private int flowWindowStartIndex = 7; // 8번째부터 (서브와 동일)
    [Tooltip("플레이리스트 내에서 특수 이벤트를 끼워넣을 수 있는 종료 인덱스 (0-based)")]
    [SerializeField] private int flowWindowEndIndex = 13;  // 14번째까지 (서브와 동일)
    [Tooltip("한 사이클(챕터) 동안 허용되는 특수 이벤트 체인 최대 횟수")]
    [SerializeField] private int maxSpecialChainsPerCycle = 1; // 기본 1회

    private int cachedChapterForCycle = 0;
    private int cachedPlaylistStartIndex = -1;
    private int specialChainsTriggeredThisCycle = 0;

    private void InitializeFlowGatingState()
    {
        var pd = DataManager.Instance?.PlayerData;
        cachedChapterForCycle = pd != null ? pd.currentChapter : 0;
        cachedPlaylistStartIndex = pd != null ? pd.eventPlaylistIndex : -1;
        specialChainsTriggeredThisCycle = 0;
        // 챕터/사이클 시작 시 특수이벤트 중복 기록도 초기화
        seenGroupsThisPlaythrough.Clear();
    }

    private void ResetFlowGatingIfCycleChanged()
    {
        var pd = DataManager.Instance?.PlayerData;
        if (pd == null) return;
        // 챕터가 바뀌었거나, 플레이리스트가 새로 시작된 경우(인덱스가 0으로 리셋) 카운터 초기화
        bool chapterChanged = (pd.currentChapter != cachedChapterForCycle);
        bool playlistReset = (pd.eventPlaylistIndex == 0 && cachedPlaylistStartIndex > 0);
        if (chapterChanged || playlistReset)
        {
            cachedChapterForCycle = pd.currentChapter;
            cachedPlaylistStartIndex = pd.eventPlaylistIndex;
            specialChainsTriggeredThisCycle = 0;
            // 새 사이클 시작: 이번 회차 중복 기록 초기화
            seenGroupsThisPlaythrough.Clear();
        }
    }

    private bool IsChapterAllowed()
    {
        var pd = DataManager.Instance?.PlayerData;
        if (pd == null) return false;
        return pd.currentChapter >= minChapterForSpecial;
    }

    private bool IsChapterAllowedForChapter1()
    {
        var pd = DataManager.Instance?.PlayerData;
        if (pd == null) return false;
        return pd.currentChapter >= 1;
    }

    private bool IsFlowWindowOpenForCurrentTurn()
    {
        var pd = DataManager.Instance?.PlayerData;
        if (pd == null) return false;
        int idx = pd.eventPlaylistIndex; // 다음에 진행할 슬롯 위치 (TryTriggerIfReady는 진행 직전에 호출됨)
        return idx >= flowWindowStartIndex && idx <= flowWindowEndIndex;
    }

    private bool HasReachedMaxChainsThisCycle()
    {
        return specialChainsTriggeredThisCycle >= maxSpecialChainsPerCycle;
    }

    [Serializable]
    private class SpecialAnswerSkillRow
    {
        public int AnswerID { get; set; }
        public string Text_KR { get; set; }
        public string Text_EN { get; set; }
        public int NextTextID { get; set; }
        public string AnswerReward_Skill { get; set; }
        public int Font_Direction { get; set; }
    }
}


