using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum BattleOutcome
{
    Win,
    Draw,
    Lose
}

/// <summary>
/// 여러 회차에 걸친 플레이어의 중요 기록(완료한 이벤트, 엔딩 등)을 저장하고 관리합니다.
/// 이 클래스의 데이터는 게임을 종료해도 유지되어야 합니다.
/// </summary>
public class PlaythroughHistory : MonoBehaviour
{
    public static PlaythroughHistory Instance { get; private set; }

    // --- 저장될 데이터 ---
    // Key: EventID, Value: 성공 여부 (true: 성공, false: 실패)
    private Dictionary<int, bool> _completedEventStates = new Dictionary<int, bool>();
    private HashSet<int> _completedSubEventGroupIds = new HashSet<int>();
    private HashSet<int> _completedEndingIds = new HashSet<int>();
    private BattleOutcome _lastBattleResult;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadHistory(); // 게임 시작 시 기록 불러오기
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- Public API ---

    /// <summary>
    /// 특정 파라미터 이벤트의 완료 상태(성공/실패)를 가져옵니다.
    /// </summary>
    /// <param name="eventId">이벤트 ID</param>
    /// <param name="wasSuccess">성공 여부</param>
    /// <returns>해당 이벤트를 완료한 기록이 있으면 true, 없으면 false</returns>
    public bool GetEventCompletionState(int eventId, out bool wasSuccess)
    {
        return _completedEventStates.TryGetValue(eventId, out wasSuccess);
    }

    public bool HasCompletedSubEventGroup(int groupId)
    {
        return _completedSubEventGroupIds.Contains(groupId);
    }

    public bool HasCompletedEnding(int endingId)
    {
        return _completedEndingIds.Contains(endingId);
    }

    public BattleOutcome GetLastBattleResult()
    {
        return _lastBattleResult;
    }

    /// <summary>
    /// 파라미터 이벤트 완료 기록을 저장합니다. (성공/실패 여부 포함)
    /// </summary>
    /// <param name="eventId">이벤트 ID</param>
    /// <param name="wasSuccess">성공 여부</param>
    public void RecordEventCompletion(int eventId, bool wasSuccess)
    {
        // 이미 같은 결과로 기록되어 있으면 저장하지 않음
        if (_completedEventStates.TryGetValue(eventId, out bool existingState) && existingState == wasSuccess)
        {
            return;
        }
        _completedEventStates[eventId] = wasSuccess;
        SaveHistory();
    }

    public void RecordSubEventGroupCompletion(int groupId)
    {
        if (_completedSubEventGroupIds.Add(groupId))
        {
            SaveHistory();
        }
    }

    public void RecordEndingCompletion(int endingId)
    {
        if (_completedEndingIds.Add(endingId))
        {
            SaveHistory();
        }
    }

    public void RecordBattleResult(BattleOutcome result)
    {
        _lastBattleResult = result;
        SaveHistory();
    }

    // --- 데이터 직렬화/역직렬화 ---

    private const string EventHistoryKey = "PlaythroughHistory_EventStates"; // Key 이름 변경
    private const string SubEventGroupHistoryKey = "PlaythroughHistory_SubEventGroups"; // 새로 추가
    private const string EndingHistoryKey = "PlaythroughHistory_Endings";
    private const string BattleResultKey = "PlaythroughHistory_BattleResult";

    private void SaveHistory()
    {
        // Dictionary<int, bool> -> "id:true,id:false,..." 형태의 문자열로 직렬화
        string eventString = string.Join(",", _completedEventStates.Select(kvp => $"{kvp.Key}:{(kvp.Value ? "1" : "0")}"));
        PlayerPrefs.SetString(EventHistoryKey, eventString);

        string subEventGroupString = string.Join(",", _completedSubEventGroupIds);
        PlayerPrefs.SetString(SubEventGroupHistoryKey, subEventGroupString);

        string endingString = string.Join(",", _completedEndingIds);
        PlayerPrefs.SetString(EndingHistoryKey, endingString);

        PlayerPrefs.SetInt(BattleResultKey, (int)_lastBattleResult);

        PlayerPrefs.Save();
        Debug.Log("[PlaythroughHistory] History saved.");
    }

    private void LoadHistory()
    {
        // 이벤트 성공/실패 기록 불러오기
        if (PlayerPrefs.HasKey(EventHistoryKey))
        {
            string eventString = PlayerPrefs.GetString(EventHistoryKey);
            if (!string.IsNullOrEmpty(eventString))
            {
                _completedEventStates = eventString.Split(',')
                    .Select(s => s.Split(':'))
                    .Where(parts => parts.Length == 2)
                    .ToDictionary(parts => int.Parse(parts[0]), parts => parts[1] == "1");
            }
        }

        // 서브 이벤트 그룹 기록 불러오기
        if (PlayerPrefs.HasKey(SubEventGroupHistoryKey))
        {
            string subEventGroupString = PlayerPrefs.GetString(SubEventGroupHistoryKey);
            if (!string.IsNullOrEmpty(subEventGroupString))
            {
                _completedSubEventGroupIds = new HashSet<int>(subEventGroupString.Split(',').Select(int.Parse));
            }
        }

        // 엔딩 기록 불러오기
        if (PlayerPrefs.HasKey(EndingHistoryKey))
        {
            string endingString = PlayerPrefs.GetString(EndingHistoryKey);
            if (!string.IsNullOrEmpty(endingString))
            {
                _completedEndingIds = new HashSet<int>(endingString.Split(',').Select(int.Parse));
            }
        }

        // 전투 결과 불러오기
        if (PlayerPrefs.HasKey(BattleResultKey))
        {
            _lastBattleResult = (BattleOutcome)PlayerPrefs.GetInt(BattleResultKey);
        }
        Debug.Log("[PlaythroughHistory] History loaded.");
    }
}