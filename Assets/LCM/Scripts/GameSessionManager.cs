using System;
using UnityEngine;
using System.Collections.Generic;

public class GameSessionManager : MonoBehaviour
{
    // 게임 시작 날짜/시간
    private DateTime sessionStartTime;
    // 게임 시작 시점의 실제 경과 시간
    private float realTimeAtStart;

    public void StartSession()
    {
        // 현재 날짜 및 시간 기록
        sessionStartTime = DateTime.Now;
        // 게임 시작 시점의 유니티 내 경과 시간 기록
        realTimeAtStart = Time.realtimeSinceStartup;

        Debug.Log($"[GameSessionManager] 세션 시작. 날짜: {sessionStartTime}, 시작 시간: {realTimeAtStart}초");
    }
    public void EndSession(string outcome)
    {
        // 총 플레이 시간 계산
        float elapsedTime = Time.realtimeSinceStartup - realTimeAtStart;

        // 경과 시간을 시/분/초 형식으로 변환
        TimeSpan playDuration = TimeSpan.FromSeconds(elapsedTime);

        string playDate = sessionStartTime.ToString("yyyy-MM-dd HH:mm");
        string playDurationString = string.Format("{0:D2}시간 {1:D2}분 {2:D2}초", playDuration.Hours, playDuration.Minutes, playDuration.Seconds);

        Debug.Log($"[GameSessionManager] 세션 종료. 총 플레이 시간: {playDurationString}");

        // SimpleEventHistoryManager에 플레이 기록을 전달하여 저장
        if (SimpleEventHistoryManager.Instance != null)
        {
            // 현재 챕터와 이벤트 기록을 가져와 함께 저장
            int currentChapter = DataManager.Instance.PlayerData.currentChapter;
            List<SimpleEventRecord> eventList = SimpleEventHistoryManager.Instance.GetEventHistory();

            SimpleEventHistoryManager.Instance.RecordPlaythrough(playDate, playDurationString, currentChapter, outcome, eventList);
        }

    }
}