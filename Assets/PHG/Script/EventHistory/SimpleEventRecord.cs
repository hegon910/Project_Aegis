using System;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// 메인이벤트 Ending_Memoriar 기준의 이벤트 기록 데이터
/// </summary>
[Serializable]
public class SimpleEventRecord
{
    public int eventId;                    // 이벤트 ID
    public string eventType;               // 이벤트 타입 ("Main", "Parameter", "Sub")
    public int chapter;                    // 챕터
    public string dialogue;                // 지문 (메인이벤트의 Text_kr)
    public string selectedChoice;          // 선택한 답변
    public bool isEndingMemoriar;          // Ending_Memoriar 여부
    public string timestamp;               // 발생 시간
    public bool isCompleted;               // 완료 여부

    public SimpleEventRecord(int id, int ch, string dialogue, string choice, bool isEndingMemoriar = false)
    {
        eventId = id;
        eventType = "Main"; // 메인이벤트 타입
        chapter = ch;
        this.dialogue = dialogue;
        selectedChoice = choice;
        this.isEndingMemoriar = isEndingMemoriar;
        timestamp = DateTime.Now.ToString("MM-dd HH:mm");
        isCompleted = true;
    }

    public SimpleEventRecord(int ch, string outcome)
    {
        eventId = -1; // 결과 기록임을 나타내는 ID (또는 0)
        eventType = "BattleResult"; // 전투 결과 타입
        chapter = ch;
        dialogue = "챕터 결산";
        selectedChoice = outcome; // outcome(승/무/패)을 selectedChoice에 저장
        this.isEndingMemoriar = false; // 챕터 결산은 Ending_Memoriar가 아님
        timestamp = DateTime.Now.ToString("MM-dd HH:mm");
        isCompleted = true;
    }
}


