using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// TODO: 이 Enum은 전투 시스템의 실제 결과값과 연동되어야 합니다.
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
    private HashSet<int> _completedEventIds = new HashSet<int>();
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

    public bool HasCompletedEvent(int eventId)
    {
        return _completedEventIds.Contains(eventId);
    }

    public bool HasCompletedEnding(int endingId)
    {
        return _completedEndingIds.Contains(endingId);
    }

    public BattleOutcome GetLastBattleResult()
    {
        return _lastBattleResult;
    }

    public void RecordEventCompletion(int eventId)
    {
        if (_completedEventIds.Add(eventId))
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


    // --- 데이터 저장/로드 (PlayerPrefs 기반의 임시 구현) ---
    // TODO: 실제 프로젝트에서는 파일 또는 다른 직렬화 방식으로 교체하는 것을 권장합니다.

    private const string EventHistoryKey = "PlaythroughHistory_Events";
    private const string EndingHistoryKey = "PlaythroughHistory_Endings";
    private const string BattleResultKey = "PlaythroughHistory_BattleResult";

    private void SaveHistory()
    {
        string eventString = string.Join(",", _completedEventIds);
        PlayerPrefs.SetString(EventHistoryKey, eventString);

        string endingString = string.Join(",", _completedEndingIds);
        PlayerPrefs.SetString(EndingHistoryKey, endingString);

        PlayerPrefs.SetInt(BattleResultKey, (int)_lastBattleResult);

        PlayerPrefs.Save();
        Debug.Log("[PlaythroughHistory] History saved.");
    }

    private void LoadHistory()
    {
        if (PlayerPrefs.HasKey(EventHistoryKey))
        {
            string eventString = PlayerPrefs.GetString(EventHistoryKey);
            if (!string.IsNullOrEmpty(eventString))
            {
                _completedEventIds = new HashSet<int>(eventString.Split(',').Select(int.Parse));
            }
        }

        if (PlayerPrefs.HasKey(EndingHistoryKey))
        {
            string endingString = PlayerPrefs.GetString(EndingHistoryKey);
            if (!string.IsNullOrEmpty(endingString))
            {
                _completedEndingIds = new HashSet<int>(endingString.Split(',').Select(int.Parse));
            }
        }

        if (PlayerPrefs.HasKey(BattleResultKey))
        {
            _lastBattleResult = (BattleOutcome)PlayerPrefs.GetInt(BattleResultKey);
        }
        Debug.Log("[PlaythroughHistory] History loaded.");
    }
}
