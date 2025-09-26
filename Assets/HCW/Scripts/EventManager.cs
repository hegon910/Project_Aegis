using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;
using GooglePlayGames.BasicApi;

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
    public static event Action<FullSubEventData> OnSubEventReady;
    public static event Action OnEventCycleCompleted;

    [Header("설정")]
    [SerializeField] private int totalEventsPerCycle = 24;

    // 상태 변수들이 이제 DataManager의 PlayerData와 동기화됩니다.
    public EventManagerState currentState { get; private set; } = EventManagerState.Idle;
    private List<int> selectedPackNumbers = new List<int>();
    // [제거] 아래 변수들은 이제 DataManager.Instance.PlayerData에 저장되므로 제거합니다.
    // private int currentChapter = 1; 
    // private List<int> playedSubEventGroups = new List<int>();
    // private List<int> currentCyclePlaylist = new List<int>();
    // private int playlistIndex = 0;

    private int currentSubEventIndex;
    private int subEventChainLength = 0; // 서브이벤트 체인 길이 추적
    // 서브이벤트 체인 길이 제한 제거 - 기획에서 24개 안 넘도록 조절

    // 이벤트 매니저 싱글톤
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
        // 전달받은 팩 ID 리스트가 비었으면 Settings의 값을 폴백으로 사용합니다.
        var fallback = DataManager.Instance?.PlayerSettings?.selectedSubEventPackIDs ?? new List<int>();
        selectedPackNumbers = new List<int>((packNumbers != null && packNumbers.Count > 0) ? packNumbers : fallback);

        // 사용자 선택 ID(예: 1,2)와 데이터 PackNumber(예: 1001,1002)가 불일치할 수 있어 보정합니다.
        // 규칙: 1000 미만이면 +1000을 적용하여 데이터 도메인으로 매핑.
        var remapped = selectedPackNumbers.Select(id => id < 1000 ? id + 1000 : id).ToList();
        if (!selectedPackNumbers.SequenceEqual(remapped))
        {
            Debug.Log($"[EventManager] 서브 팩 ID 매핑: 선택 [{string.Join(", ", selectedPackNumbers)}] -> 데이터 [{string.Join(", ", remapped)}]");
            selectedPackNumbers = remapped;
        }

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

        Debug.Log($"[EventManager] 플레이리스트 초기화 완료: {DataManager.Instance.PlayerData.currentPlaylist.Count}개");

        // 먼저 파라미터 이벤트로 24개를 채웁니다
        Debug.Log($"[EventManager] 파라미터 이벤트 추가 시작 - 목표: {totalEventsPerCycle}개");

        while (DataManager.Instance.PlayerData.currentPlaylist.Count < totalEventsPerCycle)
        {
            var additionalEvents = GetCommonParameterEvents();
            Debug.Log($"[EventManager] 사용 가능한 파라미터 이벤트: {additionalEvents.Count}개");

            if (additionalEvents.Count == 0)
            {
                Debug.LogWarning($"[EventManager] 더 이상 추가할 이벤트가 없습니다. 현재 {DataManager.Instance.PlayerData.currentPlaylist.Count}개로 부족합니다.");
                break;
            }

            // 이미 플레이리스트에 있는 이벤트 제외
            var existingIds = new HashSet<int>(DataManager.Instance.PlayerData.currentPlaylist);
            var newEvents = additionalEvents.Where(id => !existingIds.Contains(id)).ToList();

            Debug.Log($"[EventManager] 중복 제외 후 새로운 이벤트: {newEvents.Count}개");

            if (newEvents.Count == 0)
            {
                Debug.LogWarning($"[EventManager] 모든 이벤트가 이미 플레이리스트에 포함되어 있습니다. 중복을 허용하여 추가합니다.");
                newEvents = additionalEvents.ToList();
            }

            int needed = totalEventsPerCycle - DataManager.Instance.PlayerData.currentPlaylist.Count;
            int toAdd = Mathf.Min(needed, newEvents.Count);

            DataManager.Instance.PlayerData.currentPlaylist.AddRange(newEvents.OrderBy(x => Guid.NewGuid()).Take(toAdd));
            Debug.Log($"[EventManager] 파라미터 이벤트 {toAdd}개 추가됨 (현재: {DataManager.Instance.PlayerData.currentPlaylist.Count}/{totalEventsPerCycle})");
        }

        Debug.Log($"[EventManager] 파라미터 이벤트 추가 완료: {DataManager.Instance.PlayerData.currentPlaylist.Count}개");

        // 서브이벤트 추가 (8번째~14번째 사이에 배치)
        if (selectedPackNumbers != null && selectedPackNumbers.Count > 0)
        {
            if (DataManager.Instance.FullSubEvents == null || DataManager.Instance.FullSubEvents.Count == 0)
            {
                Debug.LogWarning("[EventManager] SubEvents 데이터가 비어 있어 서브 이벤트를 편성할 수 없습니다.");
            }
            else
            {
                Debug.Log($"[EventManager] 서브 편성 시작: 선택 팩 {string.Join(", ", selectedPackNumbers)}");

                // 진단: 데이터에 존재하는 팩 넘버 요약
                var distinctPacksInData = (DataManager.Instance.FullSubEvents ?? new List<FullSubEventData>())
                    .Select(e => e.SubStoryPac)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();
                Debug.Log($"[EventManager] 서브 데이터 내 팩 목록: [{string.Join(", ", distinctPacksInData)}]");

                // 진단: 선택된 각 팩이 실제 데이터에 존재하는지 표시
                foreach (var sel in selectedPackNumbers)
                {
                    bool exists = distinctPacksInData.Contains(sel);
                    if (!exists)
                    {
                        Debug.LogWarning($"[EventManager] 선택 팩 {sel}은(SubEvents.PackNumber) 데이터에 존재하지 않습니다.");
                    }
                }

                // 모든 선택된 팩에서 플레이 가능한 그룹들을 (팩 ID, 그룹 ID) 쌍으로 수집합니다.
                var allAvailableGroups = new List<(int packId, int groupId)>();
                foreach (var packNumber in selectedPackNumbers)
                {
                    var groupsFromPack = GetSubEventGroupsForPack(packNumber)
                        .Select(group => (packId: packNumber, groupId: group));
                    allAvailableGroups.AddRange(groupsFromPack);
                }

                // 이미 플레이한 그룹은 제외하고, 리스트를 무작위로 섞습니다.
                var availableGroups = allAvailableGroups
                    .Where(pg => !DataManager.Instance.PlayerData.playedSubEventGroups.Contains(pg.groupId))
                    .OrderBy(x => Guid.NewGuid()) // 리스트 셔플
                    .ToList();

                Debug.Log($"[EventManager] 서브 그룹 후보: 전체 {allAvailableGroups.Count}개, 미플레이 {availableGroups.Count}개");

                if (availableGroups.Count > 0)
                {
                    // 8번째~14번째 사이에 서브이벤트를 배치
                    int subEventStartIndex = 7; // 8번째 (0-based index)
                    int subEventEndIndex = 13;  // 14번째 (0-based index)
                    
                    // 서브이벤트를 배치할 위치들을 결정 (8~14번째 중에서 랜덤하게 선택)
                    var subEventPositions = new List<int>();
                    for (int i = subEventStartIndex; i <= subEventEndIndex; i++)
                    {
                        if (i < DataManager.Instance.PlayerData.currentPlaylist.Count)
                        {
                            subEventPositions.Add(i);
                        }
                    }
                    
                    // 서브이벤트 위치를 랜덤하게 섞기
                    subEventPositions = subEventPositions.OrderBy(x => Guid.NewGuid()).ToList();
                    
                    Debug.Log($"[EventManager] 서브이벤트 배치 가능 위치: {string.Join(", ", subEventPositions)}");

                    // 셔플된 그룹 목록을 순회하며 서브이벤트 체인을 배치
                    int positionIndex = 0;
                    int totalSubEventTurns = 0; // 서브이벤트가 소모할 총 턴 수
                    
                    foreach (var selectedPackAndGroup in availableGroups)
                    {
                        if (positionIndex >= subEventPositions.Count)
                        {
                            Debug.Log($"[EventManager] 서브이벤트 배치 위치가 모두 사용됨. 남은 그룹은 건너뜀.");
                            break;
                        }

                        var subEventChain = GetSubEventChain(selectedPackAndGroup.packId, selectedPackAndGroup.groupId);
                        if (subEventChain.Count > 0)
                        {
                            int targetPosition = subEventPositions[positionIndex];
                            
                            // 서브이벤트 체인이 24개 제한을 초과하지 않는지 확인
                            // 서브이벤트 체인은 첫 번째 이벤트만 플레이리스트에 배치되고, 나머지는 체인으로 처리됨
                            // 따라서 체인 길이만큼 턴을 소모하지만 플레이리스트에는 1개만 추가됨
                            if (totalSubEventTurns + subEventChain.Count <= totalEventsPerCycle)
                            {
                                DataManager.Instance.PlayerData.playedSubEventGroups.Add(selectedPackAndGroup.groupId);
                                
                                // 해당 위치의 파라미터 이벤트를 서브이벤트로 교체
                                DataManager.Instance.PlayerData.currentPlaylist[targetPosition] = subEventChain.First().ID;
                                
                                totalSubEventTurns += subEventChain.Count;
                                
                                Debug.Log($"[EventManager] 서브 이벤트 체인 배치: 팩 {selectedPackAndGroup.packId}, 그룹 {selectedPackAndGroup.groupId} ({subEventChain.Count}턴 소모) - 위치: {targetPosition + 1}번째, 첫 이벤트 ID: {subEventChain.First().ID}, 누적 서브이벤트 턴: {totalSubEventTurns}");
                                
                                positionIndex++;
                            }
                            else
                            {
                                Debug.Log($"[EventManager] 서브 이벤트 체인 배치 건너뜀: 팩 {selectedPackAndGroup.packId}, 그룹 {selectedPackAndGroup.groupId} (추가 시 {totalSubEventTurns + subEventChain.Count}턴으로 {totalEventsPerCycle}턴 초과)");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"[EventManager] 그룹 {selectedPackAndGroup.groupId} 체인 데이터 없음");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"[EventManager] 선택된 팩 [{string.Join(", ", selectedPackNumbers)}]에 더 이상 진행할 수 있는 새 서브 이벤트 그룹이 없습니다.");
                }
            }
        }
        else
        {
            Debug.Log("[EventManager] 선택된 서브 팩이 없어 서브 이벤트 편성 생략");
        }

        Debug.Log($"[EventManager] 서브이벤트 배치 완료: {DataManager.Instance.PlayerData.currentPlaylist.Count}개");

        // 안전장치: totalEventsPerCycle(24개) 제한 적용 (초과 시에만)
        if (DataManager.Instance.PlayerData.currentPlaylist.Count > totalEventsPerCycle)
        {
            Debug.LogWarning($"[EventManager] 예상치 못한 초과 발생! 플레이리스트가 {DataManager.Instance.PlayerData.currentPlaylist.Count}개로 제한({totalEventsPerCycle})을 초과합니다. 앞쪽 {totalEventsPerCycle}개만 사용합니다.");
            DataManager.Instance.PlayerData.currentPlaylist = DataManager.Instance.PlayerData.currentPlaylist.Take(totalEventsPerCycle).ToList();
        }

        // 표준 흐름 유지: 인덱스는 0에서 시작하고, 첫 이벤트 처리 시 증가
        DataManager.Instance.PlayerData.eventPlaylistIndex = 0;
        subEventChainLength = 0; // 새 사이클 시작 시 체인 길이 리셋
        currentState = EventManagerState.InCycle;

        DataManager.Instance.SaveData();


        Debug.Log($"[EventManager] 사이클 시작 (회차: {DataManager.Instance.PlayerData.playthroughCount}). 최종 플레이리스트: {DataManager.Instance.PlayerData.currentPlaylist.Count}개 (목표: {totalEventsPerCycle}개)");

        // 디버그: 플레이리스트 구성 상세 정보
        Debug.Log($"[EventManager] 플레이리스트 구성 상세:");
       //ebug.Log($"  - 튜토리얼 이벤트: {tutorialEvents.Count}개");
        Debug.Log($"  - totalEventsPerCycle 설정값: {totalEventsPerCycle}");
        Debug.Log($"  - 실제 플레이리스트 크기: {DataManager.Instance.PlayerData.currentPlaylist.Count}개");
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
        Debug.Log($"[PlayNextTurn] 호출됨. 현재 상태: {currentState}, 진행도: {DataManager.Instance.PlayerData.eventPlaylistIndex}/{DataManager.Instance.PlayerData.currentPlaylist.Count} (totalEventsPerCycle: {totalEventsPerCycle})");
        if (currentState == EventManagerState.Idle) return;

        // InSubEvent 상태일 때는 서브이벤트 체인이 끝나기를 기다려야 함
        if (currentState == EventManagerState.InSubEvent)
        {
            Debug.Log("[EventManager] 현재 서브이벤트 체인 진행 중입니다. 체인 완료를 기다립니다.");
            return;
        }

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
            DataManager.Instance.SaveData();


            // 4. 준비된 이벤트를 발생시킵니다. (ID 임계치 대신 데이터 존재로 분류)
            bool isSubByData = DataManager.Instance.FullSubEvents != null && DataManager.Instance.FullSubEvents.Any(e => e.ID == eventId);

            if (isSubByData)
            {
                Debug.Log($"서브이벤트(ID: {eventId}) 발생.");
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

    // (표기용 보조 메서드 없음 - 원상복구)

    private void DisplaySubEvent(int index)
    {
        currentSubEventIndex = index;

        // 서브이벤트 체인이 시작될 때만 체인 길이 증가 (첫 번째 서브이벤트)
        if (currentState != EventManagerState.InSubEvent)
        {
            subEventChainLength = 1;
        }
        else
        {
            // 체인 내에서 다음 서브이벤트로 진행할 때마다 체인 길이와 플레이리스트 인덱스 증가
            subEventChainLength++;
            DataManager.Instance.PlayerData.eventPlaylistIndex++;
            DataManager.Instance.SaveData(); // 인덱스 변경사항 저장
        }

        Debug.Log($"[EventManager] 서브이벤트 체인 길이: {subEventChainLength} (전체 플레이리스트: {DataManager.Instance.PlayerData.eventPlaylistIndex}/{DataManager.Instance.PlayerData.currentPlaylist.Count})");

        var data = DataManager.Instance.FullSubEvents.FirstOrDefault(e => e.ID == index);
        if (data != null)
        {
            OnSubEventReady?.Invoke(data);
            Debug.Log($"서브 이벤트 표시: (Index: {data.ID})");
            currentState = EventManagerState.InSubEvent;
        }
    }

    public void OnSubEventChoiceSelected(bool isLeftChoice)
    {
        if (currentState != EventManagerState.InSubEvent)
        {
            Debug.LogWarning("[EventManager] OnSubEventChoiceSelected: 서브이벤트 상태가 아닙니다.");
            return;
        }

        var currentData = DataManager.Instance.FullSubEvents.FirstOrDefault(e => e.ID == currentSubEventIndex);
        if (currentData == null)
        {
            Debug.LogError($"[EventManager] OnSubEventChoiceSelected: ID {currentSubEventIndex}에 해당하는 서브이벤트 데이터를 찾을 수 없습니다.");
            currentState = EventManagerState.InCycle;
            PlayNextTurn();
            return;
        }

        SubChoice selectedChoice = isLeftChoice ? currentData.leftChoice : currentData.rightChoice;
        if (selectedChoice == null)
        {
            Debug.LogError($"[EventManager] OnSubEventChoiceSelected: 선택된 선택지가 null입니다. (isLeftChoice: {isLeftChoice})");
            currentState = EventManagerState.InCycle;
            PlayNextTurn();
            return;
        }

        if (selectedChoice.outcome != null)
        {
            GamePlayerStats.Instance.ApplyChanges(selectedChoice.outcome.parameterChanges);
        }

        DataManager.Instance.PlayerData.completedEventIds.Add(currentSubEventIndex);

        int nextEventID = selectedChoice.nextEventID;
        Debug.Log($"[EventManager] 서브이벤트 선택 완료. 다음 이벤트 ID: {nextEventID}");

        // 체인 길이 제한 제거 - 기획에서 24개 안 넘도록 조절

        // 플레이리스트 끝 도달 확인 (24개 이벤트 완료)
        if (DataManager.Instance.PlayerData.eventPlaylistIndex >= DataManager.Instance.PlayerData.currentPlaylist.Count)
        {
            Debug.Log("[EventManager] 플레이리스트 끝에 도달했습니다. 서브이벤트 체인을 종료합니다.");
            subEventChainLength = 0; // 체인 길이 리셋
            currentState = EventManagerState.InCycle;
            PlayNextTurn();
            return;
        }

        if (nextEventID > 0)
        {
            // 다음 이벤트가 존재하는지 확인
            var nextEventData = DataManager.Instance.FullSubEvents.FirstOrDefault(e => e.ID == nextEventID);
            Debug.Log($"[EventManager] 다음 이벤트 ID: {nextEventID}, 데이터 존재: {nextEventData != null}");

            if (nextEventData != null)
            {
                Debug.Log($"[EventManager] 다음 서브이벤트로 진행: ID {nextEventID}");
                // 다음 서브이벤트로 진행하기 전에 잠시 대기 (UI 전환 시간 확보)
                StartCoroutine(TransitionToNextSubEvent(nextEventID));
            }
            else
            {
                Debug.LogWarning($"[EventManager] 다음 이벤트 ID {nextEventID}에 해당하는 데이터를 찾을 수 없습니다. 체인을 종료합니다.");
                subEventChainLength = 0; // 체인 길이 리셋
                currentState = EventManagerState.InCycle;
                PlayNextTurn();
            }
        }
        else
        {
            Debug.Log("[EventManager] 서브 이벤트 체인의 마지막입니다. 일반 이벤트로 돌아갑니다.");
            subEventChainLength = 0; // 체인 길이 리셋
            currentState = EventManagerState.InCycle;
            // 서브이벤트 체인이 끝났으므로 다음 일반 이벤트로 진행
            PlayNextTurn();
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
        if (DataManager.Instance?.FullSubEvents == null) return new List<int>();
        return DataManager.Instance.FullSubEvents
            .Where(e => e.SubStoryPac == packNumber)
            .Select(e => e.StoryNum)
            .Distinct()
            .ToList();
    }

    private List<FullSubEventData> GetSubEventChain(int packNumber, int groupNumber)
    {
        if (DataManager.Instance?.FullSubEvents == null) return new List<FullSubEventData>();
        return DataManager.Instance.FullSubEvents
            .Where(e => e.SubStoryPac == packNumber && e.StoryNum == groupNumber)
            .OrderBy(e => e.ID)
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

        // 신규 이벤트가 있으면 우선 사용, 없으면 모든 이벤트 사용
        if (newEvents.Count > 0)
        {
            return newEvents;
        }
        else
        {
            Debug.LogWarning("[EventManager] 사용 가능한 신규 공용 이벤트가 없어서, 봤던 이벤트를 포함하여 다시 목록을 만듭니다.");
            return DataManager.Instance.eventDataDict.Values
                .Where(d => d.PageType == 0)
                .Select(d => d.ID)
                .ToList();
        }
    }

    private IEnumerator TransitionToNextSubEvent(int nextEventID)
    {
        // UI 전환을 위한 짧은 대기 시간
        yield return new WaitForSeconds(0.5f);

        // 다음 서브이벤트 표시
        DisplaySubEvent(nextEventID);
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

        // 저장 시점에 PlayNextTurn에서 인덱스를 이미 +1 증가시킨 뒤 저장되므로,
        // 복원 시에는 실제로 화면에 표시되던 이벤트를 복구하기 위해 -1 보정이 필요합니다.
        int savedIndex = DataManager.Instance.PlayerData.eventPlaylistIndex;
        int indexToRestore = Mathf.Max(0, savedIndex - 1);
        if (indexToRestore >= DataManager.Instance.PlayerData.currentPlaylist.Count)
        {
            Debug.LogWarning($"[EventManager] 복원 인덱스({indexToRestore})가 범위를 벗어났습니다. 마지막 이벤트로 조정합니다.");
            indexToRestore = DataManager.Instance.PlayerData.currentPlaylist.Count - 1;
        }

        int eventId = DataManager.Instance.PlayerData.currentPlaylist[indexToRestore];

        Debug.Log($"[EventManager] 저장된 이벤트(ID: {eventId})를 복원합니다. (savedIndex={savedIndex} -> restoreIndex={indexToRestore})");

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