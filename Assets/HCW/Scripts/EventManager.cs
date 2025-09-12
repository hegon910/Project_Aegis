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
    public static event Action<DataManager.SubEventData> OnSubEventReady;
    public static event Action OnEventCycleCompleted;

    [Header("설정")]
    [SerializeField] private int totalEventsPerCycle = 24;

    // 상태 변수들이 이제 DataManager의 PlayerData와 동기화됩니다.
    private EventManagerState currentState = EventManagerState.Idle;
    private List<int> selectedPackNumbers = new List<int>();
    // [제거] 아래 변수들은 이제 DataManager.Instance.PlayerData에 저장되므로 제거합니다.
    // private int currentChapter = 1; 
    // private List<int> playedSubEventGroups = new List<int>();
    // private List<int> currentCyclePlaylist = new List<int>();
    // private int playlistIndex = 0;

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

    public int GetCurrentSubEventPackID()
    {
        if (selectedPackNumbers != null && selectedPackNumbers.Count > 0)
        {
            return selectedPackNumbers[0];
        }
        return -1;
    }

    public async UniTask StartNewGame(List<int> packNumbers)
    {
        // 전달받은 팩 ID 리스트를 저장합니다.
        selectedPackNumbers = new List<int>(packNumbers ?? new List<int>());

        DataManager.Instance.PlayerData.currentChapter = 1;
        DataManager.Instance.PlayerData.playedSubEventGroups.Clear();
        DataManager.Instance.PlayerData.currentPlaylist.Clear();
        DataManager.Instance.PlayerData.eventPlaylistIndex = 0;

        InitializeEventManager();

        // 로그 메시지를 수정하여 여러 팩이 선택되었음을 표시합니다.
        string packsString = selectedPackNumbers.Count > 0 ? string.Join(", ", selectedPackNumbers) : "없음";
        Debug.Log($"새 게임 시작. 팩: [{packsString}], 챕터: {DataManager.Instance.PlayerData.currentChapter}");
    }

    public void StartNewCycle()
    {
        // [수정] DataManager의 플레이리스트를 비웁니다.
        DataManager.Instance.PlayerData.currentPlaylist.Clear();
        List<int> tutorialEvents = new List<int>();

        // [수정] PlayerStats 대신 DataManager에서 회차 정보와 완료 이벤트 목록을 가져옵니다.
        if (DataManager.Instance.PlayerData.playthroughCount == 1)
        {
            var completedIds = new HashSet<int>(DataManager.Instance.PlayerData.completedEventIds);
            tutorialEvents = DataManager.Instance.eventDataDict.Values
                .Where(d => d.PageType == 1 && !completedIds.Contains(d.ID))
                .Select(d => d.ID)
                .ToList();
            DataManager.Instance.PlayerData.currentPlaylist.AddRange(tutorialEvents);
        }

        Debug.Log($"[EventManager] 튜토리얼 이벤트 추가 후: {DataManager.Instance.PlayerData.currentPlaylist.Count}개");


        int remainingSlots = totalEventsPerCycle - DataManager.Instance.PlayerData.currentPlaylist.Count;

        // 아래의 if문으로 서브이벤트 추가 로직 전체를 감싸줍니다.
        // 선택된 팩이 있을 경우에만 (-1이 아닐 경우) 서브이벤트를 추가하도록 변경합니다.
        if (remainingSlots > 0 && selectedPackNumbers != null && selectedPackNumbers.Count > 0)
        {
            // 모든 선택된 팩에서 플레이 가능한 그룹들을 (팩 ID, 그룹 ID) 쌍으로 수집합니다.
            var allAvailableGroups = new List<(int packId, int groupId)>();
            foreach (var packNumber in selectedPackNumbers)
            {
                var groupsFromPack = GetSubEventGroupsForPack(packNumber)
                    .Select(group => (packId: packNumber, groupId: group));
                allAvailableGroups.AddRange(groupsFromPack);
            }

            // 이미 플레이한 그룹은 제외합니다.
            var availableGroups = allAvailableGroups
                .Where(pg => !DataManager.Instance.PlayerData.playedSubEventGroups.Contains(pg.groupId))
                .ToList();

            if (availableGroups.Count > 0)
            {
                // 플레이 가능한 모든 그룹 중에서 무작위로 하나를 선택합니다.
                var selectedPackAndGroup = availableGroups[UnityEngine.Random.Range(0, availableGroups.Count)];
                var subEventChain = GetSubEventChain(selectedPackAndGroup.packId, selectedPackAndGroup.groupId);

                if (subEventChain.Count > 0 && subEventChain.Count <= remainingSlots)
                {
                    DataManager.Instance.PlayerData.playedSubEventGroups.Add(selectedPackAndGroup.groupId);
                    DataManager.Instance.PlayerData.currentPlaylist.Add(subEventChain.First().Index);
                    remainingSlots -= subEventChain.Count;
                    Debug.Log($"[EventManager] 서브 이벤트 체인 추가: 팩 {selectedPackAndGroup.packId}, 그룹 {selectedPackAndGroup.groupId}");
                }
            }
            else
            {
                Debug.LogWarning($"[EventManager] 선택된 팩 [{string.Join(", ", selectedPackNumbers)}]에 더 이상 진행할 수 있는 새 서브 이벤트 그룹이 없습니다.");
            }
        }
        if (remainingSlots > 0)
            {
                var commonParameterEvents = GetCommonParameterEvents();
                DataManager.Instance.PlayerData.currentPlaylist.AddRange(commonParameterEvents.OrderBy(x => Guid.NewGuid()).Take(remainingSlots));
            }
        

        DataManager.Instance.PlayerData.currentPlaylist = DataManager.Instance.PlayerData.currentPlaylist.OrderBy(x => Guid.NewGuid()).ToList();
        DataManager.Instance.PlayerData.eventPlaylistIndex = 0;
        currentState = EventManagerState.InCycle;

        DataManager.Instance.SaveLocal();


        Debug.Log($"사이클 시작 (회차: {DataManager.Instance.PlayerData.playthroughCount}). 총 이벤트: {DataManager.Instance.PlayerData.currentPlaylist.Count}개");
    }
    /// <summary>
    /// EventManager를 초기화합니다.
    /// 저장된 플레이리스트가 있으면 불러오고, 없으면 새 사이클을 시작합니다.
    /// </summary>
    public void InitializeEventManager()
    {
        var playerData = DataManager.Instance.PlayerData;

        // 저장된 플레이리스트가 있는지 확인합니다.
        if (playerData != null && playerData.currentPlaylist != null && playerData.currentPlaylist.Count > 0)
        {
            // 저장된 목록이 있다면, 상태만 'InCycle'로 설정하고 아무것도 하지 않습니다.
            // 이미 필요한 데이터(플레이리스트, 인덱스)는 DataManager에 로드되어 있습니다.
            currentState = EventManagerState.InCycle;
            Debug.Log($"[EventManager] 저장된 이벤트 사이클을 이어합니다. 현재 진행도: {playerData.eventPlaylistIndex} / {playerData.currentPlaylist.Count}");
        }
        else
        {
            // 저장된 목록이 없다면 (새 게임 또는 새 챕터), 새로운 사이클을 시작합니다.
            Debug.Log("[EventManager] 새로운 이벤트 사이클을 시작합니다.");
            StartNewCycle();
        }
    }
   public void PlayNextTurn()
{
        Debug.Log($"[PlayNextTurn] 호출됨. 현재 상태: {currentState}, 진행도: {DataManager.Instance.PlayerData.eventPlaylistIndex}/{DataManager.Instance.PlayerData.currentPlaylist.Count}");
        if (currentState == EventManagerState.Idle) return;

    if (currentState == EventManagerState.InCycle)
    {
        if (DataManager.Instance.PlayerData.eventPlaylistIndex >= DataManager.Instance.PlayerData.currentPlaylist.Count)
        {
            Debug.Log("현재 사이클(챕터)의 모든 이벤트를 완료했습니다.");
            currentState = EventManagerState.Idle;
            OnEventCycleCompleted?.Invoke();
            return;
        }

        // 1. 현재 인덱스에서 이벤트 ID를 먼저 가져옵니다.
        int eventId = DataManager.Instance.PlayerData.currentPlaylist[DataManager.Instance.PlayerData.eventPlaylistIndex];

        // 2. 인덱스를 증가시킵니다. (다음 상태를 준비)
        DataManager.Instance.PlayerData.eventPlaylistIndex++;

        // 3. 변경된 인덱스를 포함하여 즉시 저장합니다.
        Debug.Log($"다음 이벤트 진행. 인덱스 {DataManager.Instance.PlayerData.eventPlaylistIndex}로 변경 후 저장.");
        DataManager.Instance.SaveLocal();

        // 4. 준비된 이벤트를 발생시킵니다.
        if (eventId < 10000)
        {
            DisplaySubEvent(eventId);
        }
        else
        {
            Debug.Log($"파라미터 이벤트(ID: {eventId}) 발생.");
            OnParameterEventReady?.Invoke(eventId);
            DataManager.Instance.PlayerData.completedEventIds.Add(eventId);
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
            Debug.Log($"서브 이벤트 표시: (Index: {data.Index})");

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

        DataManager.Instance.PlayerData.completedEventIds.Add(currentSubEventIndex);

        int nextIndex = -1;
        string nextIndexStr = isLeftChoice ? currentData.NextLeftSelectString : currentData.NextRightSelectString;
        int.TryParse(nextIndexStr, out nextIndex);

        if (nextIndex > 0)
        {
            DisplaySubEvent(nextIndex);
        }
        else
        {
            Debug.Log("서브 이벤트의 마지막입니다.");
            currentState = EventManagerState.InCycle;
        }
    }

    public void ResetEventManagerState()
    {
        currentState = EventManagerState.Idle;
        // [변경] int 변수 대신 리스트를 초기화합니다.
        selectedPackNumbers.Clear();

        if (DataManager.Instance?.PlayerData != null)
        {
            DataManager.Instance.PlayerData.playedSubEventGroups.Clear();
            DataManager.Instance.PlayerData.currentPlaylist.Clear();
            DataManager.Instance.PlayerData.eventPlaylistIndex = 0;
        }

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

    private List<DataManager.SubEventData> GetSubEventChain(int packNumber, int groupNumber)
    {
        if (DataManager.Instance?.SubEvents == null) return new List<DataManager.SubEventData>();
        return DataManager.Instance.SubEvents
            .Where(e => e.PackNumber == packNumber && e.GroupNumber == groupNumber)
            .OrderBy(e => e.Index)
            .ToList();
    }

    private List<int> GetCommonParameterEvents()
    {
        if (DataManager.Instance?.eventDataDict.Values == null || DataManager.Instance.PlayerData == null) return new List<int>();

        var completedIds = new HashSet<int>(DataManager.Instance.PlayerData.completedEventIds);
        var newEvents = DataManager.Instance.eventDataDict.Values
       .Where(d => d.PageType == 0 && !completedIds.Contains(d.ID))
       .Select(d => d.ID)
       .ToList();
        if (newEvents.Count < totalEventsPerCycle)
        {
            Debug.LogWarning("[EventManager] 사용 가능한 신규 공용 이벤트가 부족하여, 봤던 이벤트를 포함하여 다시 목록을 만듭니다.");
            return DataManager.Instance.eventDataDict.Values
                .Where(d => d.PageType == 0) // <-- 'completedIds' 필터링을 제거한 것이 핵심!
                .Select(d => d.ID)
                .ToList();
        }

        return newEvents;
    }

    /// <summary>
    /// [신규] 저장된 상태를 이어할 때, 현재 인덱스의 이벤트를 상태 변경 없이 그대로 다시 보여줍니다.
    /// </summary>
    public void RestoreCurrentEvent()
    {
        if (currentState != EventManagerState.InCycle)
        {
            Debug.LogWarning("[EventManager] 사이클 진행 중이 아닐 때 RestoreCurrentEvent가 호출되었습니다.");
            return;
        }

        if (DataManager.Instance.PlayerData.eventPlaylistIndex >= DataManager.Instance.PlayerData.currentPlaylist.Count)
        {
            Debug.LogWarning("[EventManager] 저장된 이벤트 인덱스가 플레이리스트 범위를 벗어났습니다. 사이클을 종료합니다.");
            currentState = EventManagerState.Idle;
            OnEventCycleCompleted?.Invoke();
            return;
        }

        // 현재 인덱스의 이벤트 ID를 가져와서, 인덱스 변경이나 저장 없이 즉시 이벤트를 발생시킵니다.
        int eventId = DataManager.Instance.PlayerData.currentPlaylist[DataManager.Instance.PlayerData.eventPlaylistIndex];

        Debug.Log($"[EventManager] 저장된 이벤트(ID: {eventId})를 복원합니다.");

        if (eventId < 10000)
        {
            DisplaySubEvent(eventId);
        }
        else
        {
            OnParameterEventReady?.Invoke(eventId);
        }
    }
}