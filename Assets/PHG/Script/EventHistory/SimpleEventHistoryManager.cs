using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 간단한 이벤트 기록 매니저
/// </summary>
public class SimpleEventHistoryManager : MonoBehaviour
{
    public static SimpleEventHistoryManager Instance { get; private set; }

    [Header("설정")]
    [SerializeField] private bool enableRecording = true;

    private List<SimpleEventRecord> eventHistory = new List<SimpleEventRecord>();

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
        // EventManager 이벤트 구독
        if (EventManager.Instance != null)
        {
            EventManager.OnParameterEventReady += OnParameterEvent;
            EventManager.OnSubEventReady += OnSubEvent;
        }
    }

    /// <summary>
    /// 파라미터 이벤트 기록
    /// </summary>
    private void OnParameterEvent(int eventId)
    {
        if (!enableRecording) return;

        // DataManager에서 이벤트 정보 가져오기
        var eventData = DataManager.Instance?.eventDataDict?.GetValueOrDefault(eventId);
        if (eventData == null) return;

        // 이벤트 제목과 설명 가져오기 (실제 구현에서는 CSV에서 가져와야 함)
        string title = $"파라미터 이벤트 {eventId}";
        string description = "파라미터 이벤트가 발생했습니다.";
        
        // 선택한 답변 (실제로는 사용자가 선택한 답변을 저장해야 함)
        string choice = "선택한 답변을 여기에 저장";

        var record = new SimpleEventRecord(eventId, "Parameter", 
            DataManager.Instance.PlayerData.currentChapter, title, description, choice);
        
        eventHistory.Add(record);
        Debug.Log($"[SimpleEventHistoryManager] 파라미터 이벤트 기록: {title}");
    }

    /// <summary>
    /// 서브 이벤트 기록
    /// </summary>
    private void OnSubEvent(DataManager.SubEventData subEventData)
    {
        if (!enableRecording) return;

        string title = subEventData.QuestionString_kr ?? $"서브 이벤트 {subEventData.Index}";
        string description = "서브 이벤트가 발생했습니다.";
        string choice = "선택한 답변을 여기에 저장";

        var record = new SimpleEventRecord(subEventData.Index, "Sub", 
            DataManager.Instance.PlayerData.currentChapter, title, description, choice);
        
        eventHistory.Add(record);
        Debug.Log($"[SimpleEventHistoryManager] 서브 이벤트 기록: {title}");
    }

    /// <summary>
    /// 이벤트 기록 가져오기
    /// </summary>
    public List<SimpleEventRecord> GetEventHistory()
    {
        return new List<SimpleEventRecord>(eventHistory);
    }

    /// <summary>
    /// 특정 챕터의 이벤트 기록 가져오기
    /// </summary>
    public List<SimpleEventRecord> GetEventsByChapter(int chapter)
    {
        return eventHistory.FindAll(e => e.chapter == chapter);
    }

    /// <summary>
    /// 이벤트 기록 초기화
    /// </summary>
    public void ClearHistory()
    {
        eventHistory.Clear();
        Debug.Log("[SimpleEventHistoryManager] 이벤트 기록이 초기화되었습니다.");
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (EventManager.Instance != null)
        {
            EventManager.OnParameterEventReady -= OnParameterEvent;
            EventManager.OnSubEventReady -= OnSubEvent;
        }
    }
}


