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

    public SimpleEventRecord(int id, int ch, string dialogue, string choice,bool isEndingMemoriar = false)
    {
        eventId = id;
        eventType = "Main"; // 메인이벤트만 기록
        chapter = ch;
        this.dialogue = dialogue;
        selectedChoice = choice;
        this.isEndingMemoriar = isEndingMemoriar;
        timestamp = DateTime.Now.ToString("MM-dd HH:mm");
        isCompleted = true;
    }
}


