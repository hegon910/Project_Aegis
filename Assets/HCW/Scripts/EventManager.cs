using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEditor.PackageManager;

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

    // [수정] 상태 변수들이 이제 DataManager의 PlayerData와 동기화됩니다.
    private EventManagerState currentState = EventManagerState.Idle;
    private int selectedPackNumber;

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

    public async UniTask StartNewGame(int packNumber)
    {
        selectedPackNumber = packNumber;

        // [수정] DataManager의 새 게임 데이터에 기반하여 초기화합니다.
        DataManager.Instance.PlayerData.currentChapter = 1;
        DataManager.Instance.PlayerData.playedSubEventGroups.Clear();

        // DataManager의 CSV 데이터 초기화는 GameManager에서 이미 수행했으므로 여기서는 호출하지 않습니다.
        // await DataManager.Instance.InitializeDataAsync();
        // await DataManager.Instance.SubIntializeDataAsync();

        StartNewCycle();
        currentState = EventManagerState.InCycle;
        Debug.Log($"새 게임 시작. 팩: {selectedPackNumber}, 챕터: {DataManager.Instance.PlayerData.currentChapter}");
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

        if (remainingSlots > 0)
        {
            // [수정] DataManager의 playedSubEventGroups를 참조합니다.
            var availableGroups = GetSubEventGroupsForPack(selectedPackNumber)
                .Except(DataManager.Instance.PlayerData.playedSubEventGroups).ToList();

            if (availableGroups.Count > 0)
            {
                int selectedGroup = availableGroups[UnityEngine.Random.Range(0, availableGroups.Count)];
                var subEventChain = GetSubEventChain(selectedPackNumber, selectedGroup);

                if (subEventChain.Count > 0 && subEventChain.Count <= remainingSlots)
                {
                    DataManager.Instance.PlayerData.playedSubEventGroups.Add(selectedGroup); // 확정되면 추가
                    DataManager.Instance.PlayerData.currentPlaylist.Add(subEventChain.First().Index);
                    remainingSlots -= subEventChain.Count;
                }
            }

            if (remainingSlots > 0)
            {
                var commonParameterEvents = GetCommonParameterEvents();
                DataManager.Instance.PlayerData.currentPlaylist.AddRange(commonParameterEvents.OrderBy(x => Guid.NewGuid()).Take(remainingSlots));
            }
        }

        DataManager.Instance.PlayerData.currentPlaylist = DataManager.Instance.PlayerData.currentPlaylist.OrderBy(x => Guid.NewGuid()).ToList();
        DataManager.Instance.PlayerData.eventPlaylistIndex = 0;

        // [추가] 새 사이클(챕터)이 구성되었으므로 이 상태를 저장합니다.
        DataManager.Instance.SaveGame();

        Debug.Log($"사이클 시작 (회차: {DataManager.Instance.PlayerData.playthroughCount}). 총 이벤트: {DataManager.Instance.PlayerData.currentPlaylist.Count}개");
    }

    public void PlayNextTurn()
    {
        if (currentState == EventManagerState.Idle) return;

        // [추가] 다음 턴을 시작하기 전에(즉, 이전 이벤트 선택 직후) 게임을 저장합니다. (요구사항 1)
        Debug.Log("이전 이벤트 선택 완료. **이벤트 종료 시점 저장**");
        DataManager.Instance.SaveGame();

        if (currentState == EventManagerState.InSubEvent)
        {
            Debug.Log("서브 이벤트 진행 중... 유저의 선택을 기다립니다.");
            return;
        }

        if (currentState == EventManagerState.InCycle)
        {
            // [수정] DataManager의 플레이리스트 인덱스를 사용합니다.
            if (DataManager.Instance.PlayerData.eventPlaylistIndex >= DataManager.Instance.PlayerData.currentPlaylist.Count)
            {
                Debug.Log("현재 사이클(챕터)의 모든 이벤트를 완료했습니다.");
                currentState = EventManagerState.Idle;
                OnEventCycleCompleted?.Invoke();
                return;
            }

            // [수정] DataManager에서 이벤트 ID를 가져오고 인덱스를 증가시킵니다.
            int eventId = DataManager.Instance.PlayerData.currentPlaylist[DataManager.Instance.PlayerData.eventPlaylistIndex++];

            if (eventId < 10000)
            {
                currentState = EventManagerState.InSubEvent;
                DisplaySubEvent(eventId);
            }
            else
            {
                Debug.Log($"파라미터 이벤트(ID: {eventId}) 발생.");
                OnParameterEventReady?.Invoke(eventId);
                DataManager.Instance.PlayerData.completedEventIds.Add(eventId); // 완료 기록은 PlayerStats를 통해 DataManager에 추가
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
        }
    }

    public void OnSubEventChoiceSelected(bool isLeftChoice)
    {
        if (currentState != EventManagerState.InSubEvent) return;

        var currentData = DataManager.Instance.SubEvents.FirstOrDefault(e => e.Index == currentSubEventIndex);
        if (currentData == null) return;

        DataManager.Instance.PlayerData.completedEventIds.Add(currentSubEventIndex);

        // 현재 이벤트가 '종료' 이벤트인지 확인
        if (currentData.IsFinish)
        {
            Debug.Log($"서브 이벤트 체인의 마지막 카드(Index: {currentData.Index})를 선택했습니다. 다음 턴으로 넘어갑니다.");
            currentState = EventManagerState.InCycle;
            PlayNextTurn();
            return; 
        }

        int nextIndex = -1;
        string nextIndexStr = isLeftChoice ? currentData.NextLeftSelectString : currentData.NextRightSelectString;
        int.TryParse(nextIndexStr, out nextIndex);

        if (nextIndex > 0)
        {
            DisplaySubEvent(nextIndex);
        }
        else
        {
            Debug.Log($"다음 서브 이벤트가 없습니다(nextIndex: {nextIndex}) 서브 이벤트 체인을 종료하고 다음 턴으로 넘어갑니다.");
            currentState = EventManagerState.InCycle;
            PlayNextTurn();
        }
    }

    public void ResetEventManagerState()
    {
        currentState = EventManagerState.Idle;
        selectedPackNumber = 0;

        // [수정] DataManager의 관련 데이터만 초기화하면 됩니다.
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

        return DataManager.Instance.eventDataDict.Values
            .Where(d => d.PageType == 0 && !completedIds.Contains(d.ID))
            .Select(d => d.ID)
            .ToList();
    }
}