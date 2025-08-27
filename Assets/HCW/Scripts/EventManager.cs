
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;

public enum EventManagerState
{
    Idle,           
    InCycle,        // 일반 사이클 진행 중
    InSubEvent      // 서브 이벤트 체인 진행 중
}

public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }

    // --- Public Events ---
    public static event Action<int> OnParameterEventReady;
    public static event Action<SubEventData> OnSubEventReady;

    [Header("설정")]
    [SerializeField] private int totalEventsPerCycle = 24; // 사이클 당 총 이벤트 수

    [Header("참조")]
    [SerializeField] private EventPanelController eventPanelController;

    // --- 상태 변수 ---
    private EventManagerState currentState = EventManagerState.Idle;
    private int currentChapter = 1;
    private int selectedPackNumber;
    private int currentSubEventIndex; // 현재 진행중인 서브 이벤트의 Index

    // --- 이벤트 풀 및 재생 목록 ---
    private List<int> playedSubEventGroups = new List<int>(); // 현재 회차에서 이미 플레이한 서브 이벤트 그룹
    private List<int> currentCyclePlaylist = new List<int>(); // 이번 사이클에서 플레이할 이벤트 ID 목록 (서브이벤트 시작점 + 파라미터 이벤트)
    private int playlistIndex = 0; // 현재 재생 목록의 인덱스

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

    /// <summary>
    /// 새로운 게임을 시작할 때 호출됩니다.
    /// </summary>
    public async UniTask StartNewGame(int packNumber)
    {
        selectedPackNumber = packNumber;
        currentChapter = 1;
        playedSubEventGroups.Clear();

        await DataManager.Instance.InitializeDataAsync();
        
        StartNewCycle();
        currentState = EventManagerState.InCycle;
        Debug.Log($"새 게임 시작. 팩: {selectedPackNumber}, 챕터: {currentChapter}");
    }

    /// <summary>
    /// 새로운 사이클을 시작합니다. (재생 목록 생성)
    /// </summary>
    private void StartNewCycle()
    {
        // 플레이 가능한 서브 이벤트 그룹 목록 가져오기
        var availableGroups = DataManager.Instance.GetSubEventGroupsForPack(selectedPackNumber)
            .Except(playedSubEventGroups).ToList();

        if (availableGroups.Count == 0)
        {
            Debug.LogWarning("플레이할 수 있는 서브 이벤트 그룹이 더 이상 없습니다.");
            // TODO: 모든 서브 이벤트를 다 봤을 때의 처리 필요
            return;
        }

        // 이번 사이클에 플레이할 서브 이벤트 그룹 하나를 무작위로 선택
        int selectedGroup = availableGroups[Random.Range(0, availableGroups.Count)];
        playedSubEventGroups.Add(selectedGroup);
        var subEventChain = DataManager.Instance.GetSubEventChain(selectedPackNumber, selectedGroup);
        int subEventLength = subEventChain.Count;

        // 파라미터 이벤트 수 계산 및 무작위 선택
        int numParameterEvents = totalEventsPerCycle - subEventLength;
        var parameterEventIds = DataManager.Instance.GetParameterEventsForChapter(currentChapter);
        var selectedParameterEvents = parameterEventIds.OrderBy(x => Random.value).Take(numParameterEvents).ToList();

        // 재생 목록 생성 및 셔플
        currentCyclePlaylist.Clear();
        currentCyclePlaylist.Add(subEventChain.First().Index); // 서브 이벤트는 시작 Index만 추가
        currentCyclePlaylist.AddRange(selectedParameterEvents); // 파라미터 이벤트 ID 추가
        
        // 리스트 셔플 (피셔-예이츠 알고리즘)
        for (int i = 0; i < currentCyclePlaylist.Count - 1; i++)
        {
            int j = Random.Range(i, currentCyclePlaylist.Count);
            var temp = currentCyclePlaylist[i];
            currentCyclePlaylist[i] = currentCyclePlaylist[j];
            currentCyclePlaylist[j] = temp;
        }

        playlistIndex = 0;
        Debug.Log($"새로운 사이클 시작. 서브 이벤트 그룹: {selectedGroup} ({subEventLength}턴), 파라미터 이벤트: {numParameterEvents}턴");
    }

    /// <summary>
    /// 매 턴 호출되는 메인 로직
    /// </summary>
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
                StartNewCycle();
                if (currentCyclePlaylist.Count == 0) return;
            }

            int eventId = currentCyclePlaylist[playlistIndex++];
            
            // ID의 크기로 서브/파라미터 이벤트를 구분합니다.
            if (eventId < 10000) // 서브 이벤트 시작점인 경우
            {
                currentState = EventManagerState.InSubEvent;
                DisplaySubEvent(eventId);
            }
            else // 파라미터 이벤트인 경우
            {
                Debug.Log($"파라미터 이벤트(ID: {eventId}) 발생을 알립니다.");
                OnParameterEventReady?.Invoke(eventId);
            }
        }
    }

    /// <summary>
    /// 서브 이벤트를 화면에 표시합니다.
    /// </summary>
    private void DisplaySubEvent(int index)
    {
        currentSubEventIndex = index;
        var data = DataManager.Instance.GetSubEventByIndex(index);
        if (data != null)
        {
            // OnSubEventReady?.Invoke(data);
            eventPanelController.DisplaySubEvent(data);
            Debug.Log($"서브 이벤트 표시: (Index: {data.Index}) {data.QuestionString_kr}");

            if (data.IsFinishString)
            {
                Debug.Log("서브 이벤트 체인 종료.");
                currentState = EventManagerState.InCycle; // 다시 사이클 상태로 복귀
            }
        }
    }
    
    // 유저가 선택지를 골랐을 때 호출될 함수 (EventPanelController에서 호출)
    public void OnSubEventChoiceSelected(bool isLeftChoice)
    {
        if (currentState != EventManagerState.InSubEvent) return;

        var currentData = DataManager.Instance.GetSubEventByIndex(currentSubEventIndex);
        if (currentData == null) return;

        int nextIndex = isLeftChoice ? currentData.NextLeftSelectString : currentData.NextRightSelectString;
        
        if(nextIndex > 0)
        {
            DisplaySubEvent(nextIndex);
        }
        else
        {
            Debug.LogError("다음 서브 이벤트 Index가 유효하지 않습니다.");
            currentState = EventManagerState.InCycle; // 오류 발생 시 사이클로 복귀
        }
    }
}