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

    // 스킬 보상 매핑 (AnswerID -> SkillID 문자열)
    private Dictionary<int, string> answerToSkillId = new Dictionary<int, string>();

    // 스킬 획득 프롬프트 상태
    private bool inSkillPrompt = false;
    private string pendingSkillId;
    private int pendingNextEventIdAfterSkillPrompt;
    // 파라미터 변화가 발생한 직후에만 특수 이벤트를 시도하기 위한 트리거 윈도우 플래그
    private bool triggerWindowOpen = false;
    // 같은 프레임/같은 변화에서 중복 트리거 방지
    private int triggerWindowOpenFrame = -1;

    public bool IsInSpecialChain => inSpecialChain;
    public string GetPendingSkillId() => pendingSkillId;
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
        }
    }

    /// <summary>
    /// 현재 파라미터 이벤트가 화면에 표시되기 직전 호출해 조건을 평가하고,
    /// 트리거가 만족되면 특수 이벤트 체인을 시작합니다.
    /// </summary>
    /// <returns>특수 이벤트가 시작되었으면 true</returns>
    public bool TryTriggerIfReady()
    {
        if (inSpecialChain || triggers == null || triggers.Count == 0) return false;
        // 파라미터 변화 트리거 윈도우가 아닐 때는 시도하지 않음 (시작부터 강제 발생 방지)
        if (!triggerWindowOpen) return false;
        // 한 프레임에 여러 번 호출될 수 있으므로, 같은 프레임에서 한 번만 허용
        if (triggerWindowOpenFrame == Time.frameCount)
        {
            // 계속 진행 허용 (동일 프레임 1회)
        }
        if (DataManager.Instance == null || DataManager.Instance.PlayerData == null) return false;
        // --- 흐름 게이팅: 서브이벤트와 유사한 윈도우/사이클 제약, 챕터 제한 ---
        ResetFlowGatingIfCycleChanged();
        if (!IsFlowWindowOpenForCurrentTurn()) return false; // 위치 윈도우 제약
        if (!IsChapterAllowed()) return false;                // 2챕터부터 허용
        if (HasReachedMaxChainsThisCycle()) return false;     // 사이클 당 제한
        // 등급 우선순위로 후보를 모으고, 최상위 등급끼리 랜덤 선택
        var satisfied = new List<SpecialTrigger>();
        foreach (var trig in triggers)
        {
            if (EvaluateTrigger(trig)) satisfied.Add(trig);
        }
        if (satisfied.Count == 0) return false;

        int maxPriority = satisfied.Max(t => t.priority);
        var top = satisfied.Where(t => t.priority == maxPriority).ToList();
        var chosen = top.OrderBy(_ => System.Guid.NewGuid()).First();

        var firstEvent = FindFirstEventOfGroup(chosen.storyPac, chosen.storyNum);
        if (firstEvent == null) return false;

        // 윈도우는 1회 소진: 다음 파라미터 변화까지 대기
        triggerWindowOpen = false;
        StartChain(firstEvent.ID);
        return true;
    }

    public void OnSubEventChoiceSelected(bool isLeftChoice)
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

        // 파라미터 변화 적용
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
            }
            // 프롬프트 종료 후 원래 체인으로 복귀
            inSkillPrompt = false;
            GoNext(pendingNextEventIdAfterSkillPrompt);
            return;
        }

        // 일반 체인 진행
        GoNext(selected.nextEventID);
    }

    private void GoNext(int nextId)
    {
        if (nextId <= 0)
        {
            // 체인 종료 → 페이드 아웃 요청 후 끝
            RequestExitFade(0.5f);
            EndChain();
            return;
        }
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

        player.equippedSkillID = skillId;
        player.LoadSkillFromID();
        PlayerPrefs.SetString("EquippedSkillID", skillId);
        PlayerPrefs.Save();
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
    [SerializeField] private int minChapterForSpecial = 2; // 2챕터부터 등장
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
        }
    }

    private bool IsChapterAllowed()
    {
        var pd = DataManager.Instance?.PlayerData;
        if (pd == null) return false;
        return pd.currentChapter >= minChapterForSpecial;
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


