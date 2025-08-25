using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }

    [Header("참조")]
    [SerializeField]
    private EventPanelController eventPanelController;

    [SerializeField]
    private List<int> allEventIds; // DataManager가 로드한 모든 이벤트 ID의 원본 리스트
    [SerializeField]
    private List<int> eventIdPool; // 현재 플레이 가능한 이벤트 ID만 담는 리스트

    public bool IsInitialized { get; private set; } = false;

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

    private async UniTaskVoid Start()
    {
        if (DataManager.Instance == null)
        {
            Debug.LogError("DataManager 인스턴스를 찾을 수 없습니다. 씬에 배치되었는지 확인하세요.");
            return;
        }

        Debug.Log("EventManager: DataManager 초기화 완료를 기다립니다.");

        try
        {
            await DataManager.Instance.InitializeDataAsync();

            allEventIds = DataManager.Instance.StringDataList.Select(data => data.ID).ToList();
            ResetEventPool();

            Debug.Log($"이벤트 매니저 초기화 완료. 총 {allEventIds.Count}개의 이벤트가 로드되었습니다.");
            IsInitialized = true;
        }
        catch (System.Exception ex)
        {
            // 로드 실패 시 오류 처리
            Debug.LogError($"이벤트 매니저 초기화 실패: 데이터 로드 중 오류 발생. {ex.Message}");
            allEventIds = new List<int>();
            eventIdPool = new List<int>();
        }
    }

    /// <summary>
    /// 현재 게임 상태(회차, 완료한 이벤트)에 맞는 이벤트만 필터링하여 이벤트 풀을 리셋합니다.
    /// </summary>
    private void ResetEventPool()
    {
        if (PlayerStats.Instance == null || DataManager.Instance == null || DataManager.Instance.StringDataList == null)
        {
            Debug.LogError("PlayerStats 또는 DataManager가 초기화되지 않았습니다.");
            eventIdPool = new List<int>();
            return;
        }

        // 회차에 따라 PageType 결정
        string currentPageType = (PlayerStats.Instance.playthroughCount == 1) ? "Tutorial" : "Common";

        // PageType에 맞는 모든 이벤트를 가져옴
        var eventsForPage = DataManager.Instance.StringDataList
            .Where(data => data.PageType == currentPageType);

        // 이미 완료한 이벤트 ID 목록을 가져옴
        var completedIds = new HashSet<int>(PlayerStats.Instance.completedEventIds);

        // 완료한 이벤트를 제외하고 최종 ID 목록을 생성
        List<int> filteredEventIds = eventsForPage
            .Where(data => !completedIds.Contains(data.ID))
            .Select(data => data.ID)
            .ToList();

        eventIdPool = new List<int>(filteredEventIds);

        Debug.Log($"이벤트 풀 리셋 완료. 회차: {PlayerStats.Instance.playthroughCount}, PageType: '{currentPageType}'. 사용 가능한 이벤트 {eventIdPool.Count}개.");
    }

    /// <summary>
    /// 랜덤 이벤트를 재생, 기획서의 순서도 흐름을 따라야함
    /// </summary>
    public void PlayRandomEvent()
    {
        // 이벤트 플레이 여부 체크 (플레이할 이벤트가 풀에 남았는지 확인)
        if (eventIdPool == null || eventIdPool.Count == 0)
        {
            Debug.Log("현재 조건에서 보여줄 수 있는 모든 이벤트를 다 봤습니다. 이벤트 풀을 초기화합니다.");
            ResetEventPool();
            
            if (eventIdPool.Count == 0)
            {
                Debug.LogError("플레이할 이벤트가 없습니다.");
                if(eventPanelController != null) eventPanelController.gameObject.SetActive(false);
                return;
            }
        }

        // 파라미터 이벤트 랜덤 선택
        int randomIndex = Random.Range(0, eventIdPool.Count);
        int randomId = eventIdPool[randomIndex];

        // 한번 사용한 이벤트는 풀에서 제거 (중복 방지)
        eventIdPool.RemoveAt(randomIndex);

        // 지난 회차 플레이 검색
        // TODO: DataManager가 지난 회차 플레이 기록을 저장하고 GetEventDataById 호출 시
        // ID에 해당하는 이벤트가 지난 회차에 플레이되었는지 확인하고,
        // 그에 맞는 분기 데이터를(AnotherEventQuestion 등) 조합하여 EventData 반환필요
        EventData eventData = DataManager.Instance.GetEventDataById(randomId);

        // 유저 선택 및 결과 처리 (UI 표시)
        if (eventData != null && eventPanelController != null)
        {
            eventPanelController.DisplayEvent(eventData);
        }
        else
        {
            Debug.LogError($"EventManager: EventData(ID: {randomId}) 또는 EventPanelController가 없습니다.");
        }
    }
}