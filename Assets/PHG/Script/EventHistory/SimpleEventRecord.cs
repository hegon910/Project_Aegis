using System;
using UnityEngine;

/// <summary>
/// 간단한 이벤트 기록 데이터
/// </summary>
[Serializable]
public class SimpleEventRecord
{
    public int eventId;                    // 이벤트 ID
    public string eventType;               // 이벤트 타입 ("Parameter" 또는 "Sub")
    public int chapter;                    // 챕터
    public string eventTitle;              // 이벤트 제목
    public string eventDescription;        // 이벤트 설명
    public string selectedChoice;          // 선택한 답변
    public string timestamp;               // 발생 시간
    public bool isCompleted;               // 완료 여부

    public SimpleEventRecord(int id, string type, int ch, string title, string desc, string choice)
    {
        eventId = id;
        eventType = type;
        chapter = ch;
        eventTitle = title;
        eventDescription = desc;
        selectedChoice = choice;
        timestamp = DateTime.Now.ToString("MM-dd HH:mm");
        isCompleted = true;
    }
}

