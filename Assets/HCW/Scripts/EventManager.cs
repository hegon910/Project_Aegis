using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }

    [Header("참조")]
    [SerializeField]
    private EventPanelController eventPanelController;

    private List<int> allEventIds; // DataManager가 로드한 모든 이벤트 ID의 원본 리스트
    private List<int> eventIdPool; // 현재 플레이 가능한 이벤트 ID만 담는 리스트

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

    private void Start()
    {
        if (DataManager.Instance != null && DataManager.Instance.StringDataList != null)
        {
            allEventIds = DataManager.Instance.StringDataList.Select(data => data.ID).ToList();
            ResetEventPool(); // 이벤트 풀 초기화
            Debug.Log($"이벤트 매니저 초기화 완료. 총 {allEventIds.Count}개의 이벤트가 로드되었습니다.");
        }
        else
        {
            Debug.LogError("DataManager 또는 StringDataList가 초기화되지 않았습니다. Scene에 DataManager가 있는지, 실행 순서가 맞는지 확인해주세요.");
            allEventIds = new List<int>();
            eventIdPool = new List<int>();
        }
    }

    /// <summary>
    /// 현재는 모든 이벤트를 가져오지만 추후 필터링 로직을 추가필요
    /// </summary>
    private void ResetEventPool()
    {
        // TODO: 기획서 2번 항목.
        // 현재는 모든 이벤트를 풀에 추가합니다.
        // 나중에는 현재 게임의 회차(RoundType), 장(PageType)에 맞는 이벤트만
        // allEventIds 리스트에서 필터링해서 eventIdPool에 추가하는 로직이 필요
        eventIdPool = new List<int>(allEventIds);
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
