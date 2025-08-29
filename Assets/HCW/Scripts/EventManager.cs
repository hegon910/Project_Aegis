using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;

public enum EventManagerState
{
    Idle,
    InCycle,
    InSubEvent
}

public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }

    public static event Action<int> OnParameterEventReady;
    public static event Action<SubEventData> OnSubEventReady;

    [Header("설정")]
    [SerializeField] private int totalEventsPerCycle = 24;

    // 상태 변수
    private EventManagerState currentState = EventManagerState.Idle;
    private int currentChapter = 1; 
    private int selectedPackNumber;

    // 이벤트 풀 및 재생 목록
    private List<int> playedSubEventGroups = new List<int>();
    private List<int> currentCyclePlaylist = new List<int>();
    private int playlistIndex = 0;
    private int currentSubEventIndex;


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
        }
    }

    public async UniTask StartNewGame(int packNumber)
    {
        selectedPackNumber = packNumber;
        currentChapter = 1;
        playedSubEventGroups.Clear();

        // DataManager가 여기서 초기화된다고 가정
        await DataManager.Instance.InitializeDataAsync();
        await DataManager.Instance.SubIntializeDataAsync();
        
        StartNewCycle();
        currentState = EventManagerState.InCycle;
        Debug.Log($"새 게임 시작. 팩: {selectedPackNumber}, 챕터: {currentChapter}");
        
        // PlayNextTurn(); 
    }

    public void StartNewCycle()
    {
        currentCyclePlaylist.Clear();
        List<int> tutorialEvents = new List<int>();

        // 1회차일 경우 튜토리얼 이벤트를 모두 포함
        if (PlayerStats.Instance.playthroughCount == 1)
        {
            tutorialEvents = DataManager.Instance.StringDataList
                .Where(d => d.PageType == "Tutorial" && !PlayerStats.Instance.completedEventIds.Contains(d.ID))
                .Select(d => d.ID)
                .ToList();
            currentCyclePlaylist.AddRange(tutorialEvents);
        }

        // 남은 슬롯 계산
        int remainingSlots = totalEventsPerCycle - currentCyclePlaylist.Count;
        
        if (remainingSlots > 0)
        {
            // 서브 이벤트 추가 (1개 그룹)
            var availableGroups = GetSubEventGroupsForPack(selectedPackNumber).Except(playedSubEventGroups).ToList();
            if (availableGroups.Count > 0)
            {
                int selectedGroup = availableGroups[UnityEngine.Random.Range(0, availableGroups.Count)];
                var subEventChain = GetSubEventChain(selectedPackNumber, selectedGroup);
                
                // 서브 이벤트 체인 전체가 들어갈 자리가 있는지 확인
                if (subEventChain.Count <= remainingSlots)
                {
                    playedSubEventGroups.Add(selectedGroup); // 확정되면 추가
                    currentCyclePlaylist.Add(subEventChain.First().Index);
                    remainingSlots -= subEventChain.Count;
                }
            }

            // 나머지 슬롯을 공용 파라미터 이벤트로 채움
            if (remainingSlots > 0)
            {
                var commonParameterEvents = GetCommonParameterEvents();
                currentCyclePlaylist.AddRange(commonParameterEvents.OrderBy(x => Guid.NewGuid()).Take(remainingSlots));
            }
        }

        // 최종 재생 목록 셔플
        currentCyclePlaylist = currentCyclePlaylist.OrderBy(x => Guid.NewGuid()).ToList();
        playlistIndex = 0;
        
        Debug.Log($"사이클 시작 (회차: {PlayerStats.Instance.playthroughCount}). 총 이벤트: {currentCyclePlaylist.Count}개 (튜토리얼: {tutorialEvents.Count}개)");
    }

    public void PlayNextTurn()
    {
        if (currentState == EventManagerState.Idle) return;

        if (currentState == EventManagerState.InSubEvent)
        {
            Debug.Log("서브 이벤트 진행 중... 유저의 선택을 기다립니다.");
            return;
        }
        
        if (currentState == EventManagerState.InCycle)
        {
            if (playlistIndex >= currentCyclePlaylist.Count)
            {
                Debug.Log("현재 사이클의 모든 이벤트를 완료. 다음 사이클을 시작합니다.");
                currentChapter++; // 또는 다른 사이클 종료 로직
                StartNewCycle();
                if (currentCyclePlaylist.Count == 0)
                {
                    Debug.LogError("플레이할 이벤트가 없습니다.");
                    return;
                }
            }

            int eventId = currentCyclePlaylist[playlistIndex++];
            
            // ID가 10000 미만이면 서브 이벤트의 Index로 이상이면 파라미터 이벤트의 ID로 간주
            if (eventId < 10000) 
            {
                currentState = EventManagerState.InSubEvent;
                DisplaySubEvent(eventId);
            }
            else 
            {
                Debug.Log($"파라미터 이벤트(ID: {eventId}) 발생을 알립니다.");
                OnParameterEventReady?.Invoke(eventId);
                PlayerStats.Instance.AddCompletedEvent(eventId);
            }
        }
    }

    /// <summary>
    /// 서브 이벤트를 화면에 표시합니다.
    /// </summary>
    private void DisplaySubEvent(int index)
    {
        currentSubEventIndex = index;
        var data = DataManager.Instance.SubEvents.FirstOrDefault(e => e.Index == index);
        if (data != null)
        {
            OnSubEventReady?.Invoke(data);
            Debug.Log($"서브 이벤트 표시: (Index: {data.Index}) {data.QuestionString_kr}");

            if (data.IsFinish)
            {
                Debug.Log("서브 이벤트 체인 종료.");
                // 여기서 바로 다음 턴으로 넘어가지 않고 유저의 확인 후 다음 턴이 진행되도록 UI 플로우에 맡김
                currentState = EventManagerState.InCycle; 
            }
        }
    }
    
    /// <summary>
    /// 유저가 서브 이벤트 선택지를 골랐을 때 호출됩니다.
    /// </summary>
    public void OnSubEventChoiceSelected(bool isLeftChoice)
    {
        if (currentState != EventManagerState.InSubEvent) return;

        var currentData = DataManager.Instance.SubEvents.FirstOrDefault(e => e.Index == currentSubEventIndex);
        if (currentData == null) return;

        // 사용한 서브 이벤트는 완료 목록에 추가 (체인 시작점 기준)
        PlayerStats.Instance.AddCompletedEvent(currentSubEventIndex);

        int nextIndex = -1;
        string nextIndexStr = isLeftChoice ? currentData.NextLeftSelectString : currentData.NextRightSelectString;
        int.TryParse(nextIndexStr, out nextIndex);

        if(nextIndex > 0)
        {
            DisplaySubEvent(nextIndex);
        }
        else
        {
            Debug.Log("서브 이벤트의 마지막입니다.");
            currentState = EventManagerState.InCycle;
            // 여기서도 바로 다음 턴으로 넘어가지 않고 UI 플로우(결과 연출 등)가 끝난 후 PlayNextTurn이 호출되도록 기다림
        }
    }

    public void ResetEventManagerState()
    {
        currentState = EventManagerState.Idle;
        currentChapter = 1;
        selectedPackNumber = 0;
        playedSubEventGroups.Clear();
        currentCyclePlaylist.Clear();
        playlistIndex = 0;
        Debug.Log("EventManager 상태가 초기화되었습니다.");
    }

    private List<int> GetSubEventGroupsForPack(int packNumber)
    {
        if (DataManager.Instance?.SubEvents == null) return new List<int>();
        return DataManager.Instance.SubEvents
            .Where(e => e.PackNumber == packNumber)
            .Select(e => e.GroupNumber)
            .Distinct()
            .ToList();
    }

    private List<SubEventData> GetSubEventChain(int packNumber, int groupNumber)
    {
        if (DataManager.Instance?.SubEvents == null) return new List<SubEventData>();
        return DataManager.Instance.SubEvents
            .Where(e => e.PackNumber == packNumber && e.GroupNumber == groupNumber)
            .OrderBy(e => e.Index)
            .ToList();
    }

    private List<int> GetCommonParameterEvents()
    {
        if (DataManager.Instance?.StringDataList == null || PlayerStats.Instance == null) return new List<int>();
    
        var completedIds = new HashSet<int>(PlayerStats.Instance.completedEventIds);

        return DataManager.Instance.StringDataList
            .Where(d => d.PageType == "Common" && !completedIds.Contains(d.ID))
            .Select(d => d.ID)
            .ToList();
    }
}