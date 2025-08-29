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

    // 이벤트 데이터 관련
    private List<int> tutorialEventIds;
    private List<int> commonEventPool;
    private int currentTutorialIndex = 0;

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
            Debug.LogError("DataManager 인스턴스를 찾을 수 없습니다.");
            return;
        }

        try
        {
            await DataManager.Instance.InitializeDataAsync();
            
            // 튜토리얼 ID 목록을 미리 만들어 정렬해둡니다.
            tutorialEventIds = DataManager.Instance.StringDataList
                .Where(data => data.PageType == "Tutorial")
                .Select(data => data.ID)
                .OrderBy(id => id)
                .ToList();

            // 공용 이벤트 풀을 준비합니다.
            ResetCommonEventPool();

            Debug.Log("이벤트 매니저 초기화 완료.");
            IsInitialized = true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"이벤트 매니저 초기화 실패: {ex.Message}");
        }
    }

    // 공용 이벤트 풀을 리셋하는 함수
    private void ResetCommonEventPool()
    {
        if (PlayerStats.Instance == null || DataManager.Instance == null || DataManager.Instance.StringDataList == null)
        {
            Debug.LogError("PlayerStats 또는 DataManager가 초기화되지 않았습니다.");
            commonEventPool = new List<int>();
            return;
        }

        var commonEvents = DataManager.Instance.StringDataList
            .Where(data => data.PageType == "Common");

        var completedIds = new HashSet<int>(PlayerStats.Instance.completedEventIds);

        List<int> filteredEventIds = commonEvents
            .Where(data => !completedIds.Contains(data.ID))
            .Select(data => data.ID)
            .ToList();

        commonEventPool = new List<int>(filteredEventIds);
        Debug.Log($"공용 이벤트 풀 리셋 완료. 사용 가능한 이벤트 {commonEventPool.Count}개.");
    }

    // 다음 이벤트를 가져오는 메인 함수
    public int GetNextEventId()
    {
        if (PlayerStats.Instance.playthroughCount == 1)
        {
            // 1회차: 튜토리얼 순차 진행
            if (currentTutorialIndex < tutorialEventIds.Count)
            {
                int nextTutorialId = tutorialEventIds[currentTutorialIndex];
                
                // 이미 완료한 튜토리얼 이벤트는 건너뜀 (세이브/로드 시 필요)
                if (PlayerStats.Instance.completedEventIds.Contains(nextTutorialId))
                {
                    currentTutorialIndex++;
                    return GetNextEventId(); // 재귀 호출로 다음 이벤트 찾기
                }
                
                currentTutorialIndex++;
                return nextTutorialId;
            }
            else
            {
                Debug.Log("튜토리얼 완료.");
                return -1; // 튜토리얼 종료
            }
        }
        else
        {
            // 2회차 이상: 공용 이벤트 랜덤 진행
            if (commonEventPool == null || commonEventPool.Count == 0)
            {
                ResetCommonEventPool();
                if (commonEventPool.Count == 0)
                {
                    Debug.LogWarning("모든 공용 이벤트를 완료했습니다.");
                    return -1; // 모든 이벤트 완료
                }
            }

            int randomIndex = Random.Range(0, commonEventPool.Count);
            int randomId = commonEventPool[randomIndex];
            commonEventPool.RemoveAt(randomIndex);
            return randomId;
        }
    }

    public void ResetEventManagerState()
    {
        currentTutorialIndex = 0; // 튜토리얼 진행도 초기화
        ResetCommonEventPool();   // 공용 이벤트 풀을 다시 채움
        Debug.Log("EventManager 상태가 초기화되었습니다.");
    }
    // 이벤트를 화면에 표시하는 역할
    public void DisplayEventById(int id)
    {
        if (id == -1)
        {
            if(eventPanelController != null) eventPanelController.gameObject.SetActive(false);
            Debug.Log("표시할 이벤트가 없습니다.");
            return;
        }

        EventData eventData = DataManager.Instance.GetEventDataById(id);

        if (eventData != null && eventPanelController != null)
        {
            eventPanelController.DisplayEvent(eventData);
        }
        else
        {
            Debug.LogError($"EventManager: EventData(ID: {id}) 또는 EventPanelController가 없습니다.");
        }
    }
}