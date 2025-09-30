using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

/// <summary>
/// 메인이벤트 Ending_Memoriar 기준의 이벤트 기록 매니저
/// </summary>
public class SimpleEventHistoryManager : MonoBehaviour
{
    public static SimpleEventHistoryManager Instance { get; private set; }

    [Header("설정")]
    [SerializeField] private bool enableRecording = true;
    [SerializeField] private bool enableLocalSave = true;


    //챕터 이벤트 기록
    public List<SimpleEventRecord> eventHistory = new List<SimpleEventRecord>();
    // 전체 플레이 기록
    private List<GamePlaythroughRecord> playthroughHistory = new List<GamePlaythroughRecord>();
    private string savePath;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 저장 경로 설정
            savePath = Path.Combine(Application.persistentDataPath, "ending_memoriar_history.json");
            
            // 기존 데이터 로드
            LoadHistory();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 이벤트 구독은 더 이상 필요하지 않음
        // 선택지 선택 시점에서 직접 RecordMainEvent, RecordParameterEvent, RecordSubEvent를 호출
        Debug.Log("[SimpleEventHistoryManager] Ending_Memoriar 기준 이벤트 기록 시스템이 활성화되었습니다.");
    }

    // 이벤트 발생 시점에서는 기록하지 않고, 선택지 선택 시점에서만 기록
    // (OnParameterEvent와 OnSubEvent 메서드는 제거됨)
    // 이벤트 기록
    public void RecordMainEvent(int eventId, string dialogue, string selectedChoice, bool isEndingMemoriar)
    {
        if (!enableRecording) return;

        var record = new SimpleEventRecord(
            eventId,
            DataManager.Instance.PlayerData.currentChapter,
            dialogue,
            selectedChoice,
            isEndingMemoriar);

        eventHistory.Add(record);
        Debug.Log($"[SimpleEventHistoryManager] 메인이벤트 기록: {dialogue} (Ending_Memoriar: {isEndingMemoriar})");

        if (enableLocalSave)
        {
            SaveHistory();
        }
    }

    //게임 전체 흐름 저장
    public void RecordPlaythrough(string date, string duration, int chapter, string outcome, List<SimpleEventRecord> events)
    {
        if (!enableRecording) return;

        var record = new GamePlaythroughRecord(date, duration, chapter, outcome, events);
        playthroughHistory.Add(record);

        Debug.Log($"[SimpleEventHistoryManager] 플레이 기록 저장: {date}, {duration}, 결과: {outcome}");

        if (enableLocalSave)
        {
            SaveHistory();
        }
    }

    //챕터별 승무패 기록
    public void RecordChapterOutcome(int chapter, string outcome)
    {
        if (!enableRecording) return;
        var record = new SimpleEventRecord(chapter, outcome);

        eventHistory.Add(record);
        Debug.Log($"[SimpleEventHistoryManager] 챕터 기록: 챕터 {chapter} - {outcome}");

        if (enableLocalSave)
        {
            SaveHistory();
        }
    }

    public List<GamePlaythroughRecord> GetPlaythroughHistory()
    {
        return new List<GamePlaythroughRecord>(playthroughHistory);
    }

    /// 이벤트 기록 가져오기
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
    /// Ending_Memoriar 이벤트만 가져오기
    /// </summary>
    public List<SimpleEventRecord> GetEndingMemoriarEvents()
    {
        return eventHistory.FindAll(e => e.isEndingMemoriar);
    }

    /// <summary>
    /// 특정 챕터의 Ending_Memoriar 이벤트만 가져오기
    /// </summary>
    public List<SimpleEventRecord> GetEndingMemoriarEventsByChapter(int chapter)
    {
        return eventHistory.FindAll(e => e.chapter == chapter && e.isEndingMemoriar);
    }

    /// <summary>
    /// 이벤트 기록 초기화
    /// </summary>
    public void ClearHistory()
    {
        eventHistory.Clear();
        playthroughHistory.Clear(); // 전체 플레이 기록도 함께 초기화

        if (enableLocalSave && File.Exists(savePath))
        {
            File.Delete(savePath);
        }

        Debug.Log("[SimpleEventHistoryManager] 모든 기록이 초기화되었습니다.");
    }

    /// 이벤트 기록을 로컬 파일에 저장
    private void SaveHistory()
    {
        if (!enableLocalSave || string.IsNullOrEmpty(savePath)) return;

        try
        {
            // 모든 기록을 담는 래퍼 클래스 생성
            var historyData = new SerializableHistory(eventHistory, playthroughHistory);
            var jsonData = JsonUtility.ToJson(historyData, true);
            File.WriteAllText(savePath, jsonData);
            Debug.Log($"[SimpleEventHistoryManager] 모든 기록이 저장되었습니다. (이벤트: {eventHistory.Count}개, 플레이: {playthroughHistory.Count}개)");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SimpleEventHistoryManager] 저장 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// 로컬 파일에서 이벤트 기록 로드
    /// </summary>
    private void LoadHistory()
    {
        if (!enableLocalSave || string.IsNullOrEmpty(savePath) || !File.Exists(savePath)) return;

        try
        {
            var jsonData = File.ReadAllText(savePath);
            var historyData = JsonUtility.FromJson<SerializableHistory>(jsonData);

            if (historyData != null)
            {
                // 로드된 데이터로 리스트 업데이트
                eventHistory = historyData.eventRecords?.ToList() ?? new List<SimpleEventRecord>();
                playthroughHistory = historyData.playthroughRecords?.ToList() ?? new List<GamePlaythroughRecord>();

                Debug.Log($"[SimpleEventHistoryManager] 모든 기록이 로드되었습니다. (이벤트: {eventHistory.Count}개, 플레이: {playthroughHistory.Count}개)");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SimpleEventHistoryManager] 로드 실패: {ex.Message}");
            eventHistory = new List<SimpleEventRecord>();
            playthroughHistory = new List<GamePlaythroughRecord>();
        }
    }

    private void OnDestroy()
    {
        if (enableLocalSave)
        {
            SaveHistory();
        }
        Debug.Log("[SimpleEventHistoryManager] 이벤트 기록 시스템이 종료되었습니다.");
    }
}

/// <summary>
/// JsonUtility 직렬화를 위한 래퍼 클래스
/// </summary>
[System.Serializable]
public class SerializableEventHistory
{
    public SimpleEventRecord[] records;

    public SerializableEventHistory(List<SimpleEventRecord> eventHistory)
    {
        records = eventHistory.ToArray();
    }
}


