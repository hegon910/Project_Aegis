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
    private int currentSubEventIndex;

    // 이벤트 풀 및 재생 목록
    private List<int> playedSubEventGroups = new List<int>();
    private List<int> currentCyclePlaylist = new List<int>();
    private int playlistIndex = 0;
    private List<int> commonEventPool = new List<int>();

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

    // --- 내부 이벤트 필터링 메서드 ---

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

    private List<int> GetParameterEventsForChapter(int chapter)
    {
        if (DataManager.Instance?.StringDataList == null) return new List<int>();
        string pageType = (chapter == 1) ? "Tutorial" : "Common";
        return DataManager.Instance.StringDataList
            .Where(d => d.PageType == pageType)
            .Select(d => d.ID)
            .ToList();
    }

    private void ResetCommonEventPool()
    {
        if (DataManager.Instance?.StringDataList == null)
        {
            commonEventPool = new List<int>();
            return;
        }
        commonEventPool = DataManager.Instance.StringDataList
            .Where(d => d.PageType == "Common")
            .Select(d => d.ID)
            .ToList();
    }


    public async UniTask StartNewGame(int packNumber)
    {
        selectedPackNumber = packNumber;
        currentChapter = 1;
        playedSubEventGroups.Clear();

        await DataManager.Instance.InitializeDataAsync();
        await DataManager.Instance.SubIntializeDataAsync();
        
        ResetCommonEventPool();
        StartNewCycle();
        currentState = EventManagerState.InCycle;
        Debug.Log($"새 게임 시작. 팩: {selectedPackNumber}, 챕터: {currentChapter}");
        PlayNextTurn(); // Start the first event
    }

    private void StartNewCycle()
    {
        var availableGroups = GetSubEventGroupsForPack(selectedPackNumber)
            .Except(playedSubEventGroups).ToList();

        if (availableGroups.Count == 0)
        {
            Debug.LogWarning("플레이할 수 있는 서브 이벤트 그룹이 더 이상 없습니다. 공용 이벤트를 재생합니다.");
            playlistIndex = 0;
            currentCyclePlaylist = commonEventPool.OrderBy(x => Guid.NewGuid()).ToList();
            return;
        }

        int selectedGroup = availableGroups[UnityEngine.Random.Range(0, availableGroups.Count)];
        playedSubEventGroups.Add(selectedGroup);
        var subEventChain = GetSubEventChain(selectedPackNumber, selectedGroup);
        int subEventLength = subEventChain.Count;

        int numParameterEvents = totalEventsPerCycle - subEventLength;
        var parameterEventIds = GetParameterEventsForChapter(currentChapter);
        var selectedParameterEvents = parameterEventIds.OrderBy(x => Guid.NewGuid()).Take(numParameterEvents).ToList();

        currentCyclePlaylist.Clear();
        currentCyclePlaylist.Add(subEventChain.First().Index);
        currentCyclePlaylist.AddRange(selectedParameterEvents);
        
        currentCyclePlaylist = currentCyclePlaylist.OrderBy(x => Guid.NewGuid()).ToList();

        playlistIndex = 0;
        Debug.Log($"새로운 사이클 시작. 서브 이벤트 그룹: {selectedGroup} ({subEventLength}턴), 파라미터 이벤트: {numParameterEvents}턴");
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
                Debug.Log("현재 사이클의 모든 이벤트를 완료했습니다. 다음 사이클을 시작합니다.");
                currentChapter++;
                StartNewCycle();
                if (currentCyclePlaylist.Count == 0)
                {
                    Debug.LogError("플레이할 이벤트가 없습니다.");
                    return;
                }
            }

            int eventId = currentCyclePlaylist[playlistIndex++];
            
            if (eventId < 10000)
            {
                currentState = EventManagerState.InSubEvent;
                DisplaySubEvent(eventId);
            }
            else
            {
                Debug.Log($"파라미터 이벤트(ID: {eventId}) 발생을 알립니다.");
                OnParameterEventReady?.Invoke(eventId);
            }
        }
    }

    private void DisplaySubEvent(int index)
    {
        currentSubEventIndex = index;
        var data = DataManager.Instance.SubEvents.FirstOrDefault(e => e.Index == index);
        if (data != null)
        {
            OnSubEventReady?.Invoke(data);
            Debug.Log($"서브 이벤트 (Index: {data.Index}) 발생을 알립니다.");

            if (data.IsFinish)
            {
                Debug.Log("서브 이벤트 체인 종료.");
                currentState = EventManagerState.InCycle;
            }
        }
    }
    
    public void OnSubEventChoiceSelected(bool isLeftChoice)
    {
        if (currentState != EventManagerState.InSubEvent) return;

        var currentData = DataManager.Instance.SubEvents.FirstOrDefault(e => e.Index == currentSubEventIndex);
        if (currentData == null) return;

        int nextIndex = -1;
        string nextIndexStr = isLeftChoice ? currentData.NextLeftSelectString : currentData.NextRightSelectString;
        int.TryParse(nextIndexStr, out nextIndex);

        if(nextIndex > 0)
        {
            DisplaySubEvent(nextIndex);
        }
        else
        {
            Debug.Log("서브 이벤트 체인 종료. 공용 이벤트를 재생합니다.");
            currentState = EventManagerState.InCycle;

            if (commonEventPool.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, commonEventPool.Count);
                int randomId = commonEventPool[randomIndex];
                commonEventPool.RemoveAt(randomIndex);
                
                OnParameterEventReady?.Invoke(randomId);
            }
            else
            {
                Debug.LogWarning("모든 공용 이벤트를 완료했습니다. 다음 턴을 진행합니다.");
                PlayNextTurn();
            }
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
        ResetCommonEventPool();
        Debug.Log("EventManager 상태가 초기화되었습니다.");
    }

    public void DisplayEventById(int id)
    {
        if (id < 10000)
        {
            currentState = EventManagerState.InSubEvent;
            DisplaySubEvent(id);
        }
        else
        {
            currentState = EventManagerState.InCycle;
            OnParameterEventReady?.Invoke(id);
        }
    }
}