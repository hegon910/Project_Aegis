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

    // 저장될 데이터
    private Dictionary<int, bool> _eventSuccessHistory = new Dictionary<int, bool>();
    private HashSet<int> _playedSubEventGroups = new HashSet<int>();
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

    public bool GetEventOutcome(int eventId, out bool success)
    {
        return _eventSuccessHistory.TryGetValue(eventId, out success);
    }

    public bool HasPlayedSubEventGroup(int groupNumber)
    {
        return _playedSubEventGroups.Contains(groupNumber);
    }

    public bool HasCompletedEnding(int endingId)
    {
        return _completedEndingIds.Contains(endingId);
    }

    public BattleOutcome GetLastBattleResult()
    {
        return _lastBattleResult;
    }

    public void RecordEventOutcome(int eventId, bool success)
    {
        if (!_eventSuccessHistory.ContainsKey(eventId) || _eventSuccessHistory[eventId] != success)
        {
            _eventSuccessHistory[eventId] = success;
            SaveHistory();
        }
    }

    public void RecordSubEventGroup(int groupNumber)
    {
        if (_playedSubEventGroups.Add(groupNumber))
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


    private const string EventHistoryKey = "PlaythroughHistory_Events";
    private const string SubEventGroupHistoryKey = "PlaythroughHistory_SubEventGroups";
    private const string EndingHistoryKey = "PlaythroughHistory_Endings";
    private const string BattleResultKey = "PlaythroughHistory_BattleResult";

    private void SaveHistory()
    {
        // 이벤트 성공 기록 저장 (ID:성공여부,ID:성공여부,...)
        string eventString = string.Join(",", _eventSuccessHistory.Select(kvp => $"{kvp.Key}:{kvp.Value}"));
        PlayerPrefs.SetString(EventHistoryKey, eventString);

        // 서브 이벤트 그룹 기록 저장
        string subEventGroupString = string.Join(",", _playedSubEventGroups);
        PlayerPrefs.SetString(SubEventGroupHistoryKey, subEventGroupString);

        // 엔딩 기록 저장
        string endingString = string.Join(",", _completedEndingIds);
        PlayerPrefs.SetString(EndingHistoryKey, endingString);

        // 전투 결과 저장
        PlayerPrefs.SetInt(BattleResultKey, (int)_lastBattleResult);

        PlayerPrefs.Save();
        Debug.Log("[PlaythroughHistory] History saved.");
    }

    private void LoadHistory()
    {
        // 이벤트 성공 기록 불러오기
        if (PlayerPrefs.HasKey(EventHistoryKey))
        {
            string eventString = PlayerPrefs.GetString(EventHistoryKey);
            if (!string.IsNullOrEmpty(eventString))
            {
                _eventSuccessHistory = eventString.Split(',')
                    .Select(s => s.Split(':'))
                    .Where(parts => parts.Length == 2 && int.TryParse(parts[0], out _) && bool.TryParse(parts[1], out _))
                    .ToDictionary(parts => int.Parse(parts[0]), parts => bool.Parse(parts[1]));
            }
        }

        // 서브 이벤트 그룹 기록 불러오기
        if (PlayerPrefs.HasKey(SubEventGroupHistoryKey))
        {
            string subEventGroupString = PlayerPrefs.GetString(SubEventGroupHistoryKey);
            if (!string.IsNullOrEmpty(subEventGroupString))
            {
                _playedSubEventGroups = new HashSet<int>(subEventGroupString.Split(',').Select(int.Parse));
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

    public void ClearHistory()
    {
        _eventSuccessHistory.Clear();
        _playedSubEventGroups.Clear();
        _completedEndingIds.Clear();

        PlayerPrefs.DeleteKey(EventHistoryKey);
        PlayerPrefs.DeleteKey(SubEventGroupHistoryKey);
        PlayerPrefs.DeleteKey(EndingHistoryKey);
        PlayerPrefs.DeleteKey(BattleResultKey);

        PlayerPrefs.Save();
        Debug.Log("[PlaythroughHistory] 모든 기록이 삭제되었습니다.");
    }
}
