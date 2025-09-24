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

    private List<SimpleEventRecord> eventHistory = new List<SimpleEventRecord>();
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

    /// <summary>
    /// 메인이벤트 기록 (Ending_Memoriar 기준)
    /// </summary>
    public void RecordMainEvent(int eventId, string dialogue, string selectedChoice, bool isEndingMemoriar, string playDate, string playDuration)
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
        
        // 로컬 저장
        if (enableLocalSave)
        {
            SaveHistory();
        }
    }

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

    public void RecordChapterOutcome(int chapter, string outcome)
    {
        if (!enableRecording) return;

        var record = new SimpleEventRecord(-1, chapter, "챕터 결산", outcome, false);

        eventHistory.Add(record);

        Debug.Log($"[SimpleEventHistoryManager] 챕터 기록: 챕터 {chapter} - {outcome}");

        // 로컬 저장
        if (enableLocalSave)
        {
            SaveHistory();
        }
    }

    public List<GamePlaythroughRecord> GetPlaythroughHistory()
    {
        return new List<GamePlaythroughRecord>(playthroughHistory);
    }

    // 파라미터 이벤트와 서브 이벤트는 Ending_Memoriar 같은 플래그가 없으므로 별도 기록하지 않음

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
        
        // 로컬 파일도 삭제
        if (enableLocalSave && File.Exists(savePath))
        {
            File.Delete(savePath);
        }
        
        Debug.Log("[SimpleEventHistoryManager] 이벤트 기록이 초기화되었습니다.");
    }

    /// <summary>
    /// 이벤트 기록을 로컬 파일에 저장
    /// </summary>
    private void SaveHistory()
    {
        if (!enableLocalSave || string.IsNullOrEmpty(savePath)) return;

        try
        {
            var jsonData = JsonUtility.ToJson(new SerializableEventHistory(eventHistory), true);
            File.WriteAllText(savePath, jsonData);
            Debug.Log($"[SimpleEventHistoryManager] 이벤트 기록이 저장되었습니다. ({eventHistory.Count}개)");
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
            var serializableHistory = JsonUtility.FromJson<SerializableEventHistory>(jsonData);
            
            if (serializableHistory != null && serializableHistory.records != null)
            {
                eventHistory = serializableHistory.records.ToList();
                Debug.Log($"[SimpleEventHistoryManager] 이벤트 기록이 로드되었습니다. ({eventHistory.Count}개)");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SimpleEventHistoryManager] 로드 실패: {ex.Message}");
            eventHistory = new List<SimpleEventRecord>();
        }
    }

    private void OnDestroy()
    {
        // 마지막에 한 번 더 저장
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


