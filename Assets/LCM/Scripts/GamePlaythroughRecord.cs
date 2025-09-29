using System;
using System.Collections.Generic;

/// <summary>
/// 게임 한 판 전체의 종합적인 기록
/// </summary>
[Serializable]
public class GamePlaythroughRecord
{
    public string playDate;
    public string playDuration;
    public int playthroughCount;
    public string outcome; // 게임 승무패
    public List<SimpleEventRecord> eventHistory; // 해당 회차의 모든 이벤트 기록

    public GamePlaythroughRecord(string date, string duration, int playCount, string gameOutcome, List<SimpleEventRecord> events)
    {
        playDate = date;
        playDuration = duration;
        playthroughCount = playCount;
        outcome = gameOutcome;
        eventHistory = events;
    }
}