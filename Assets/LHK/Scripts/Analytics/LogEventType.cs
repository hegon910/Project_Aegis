using UnityEngine;

/// <summary>
/// Firebase Analytics 이벤트 타입 정의
/// </summary>
public enum LogEventType
{
    // 기본 이벤트
    DAU,                        // 일일 접속자 (Firebase 기본 제공)
    
    // 구매 이벤트
    PURCHASE,                   // 유료 아이템 구매
    PUCOUNT,                    // 구매 유저 발생 (최초 1회)
    
    // 회차 진입 이벤트
    ENTER_STORY_1ST,            // 1회차 진입
    ENTER_STORY_2ND,            // 2회차 진입
    ENTER_STORY_3RD,            // 3회차 진입
    ENTER_STORY_EXTRA,          // 다회차 진입
    
    // 회차 클리어 이벤트
    FINISH_STORY_1ST,           // 1회차 클리어
    FINISH_STORY_2ND,           // 2회차 클리어
    FINISH_STORY_3RD,           // 3회차 클리어
    FINISH_STORY_EXTRA,         // 다회차 클리어
    
    // 장 클리어 이벤트
    FINISH_CHAPTER_1ST,         // 1회차 장 클리어
    FINISH_CHAPTER_2ND,         // 2회차 장 클리어
    FINISH_CHAPTER_3RD,         // 3회차 장 클리어
    FINISH_CHAPTER_EXTRA,       // 다회차 장 클리어
    
    // 파라미터 이벤트 종료
    FINISH_PARAMETER_1ST,       // 1회차 파라미터 이벤트 종료
    FINISH_PARAMETER_2ND,       // 2회차 파라미터 이벤트 종료
    FINISH_PARAMETER_3RD,       // 3회차 파라미터 이벤트 종료
    FINISH_PARAMETER_EXTRA,     // 다회차 파라미터 이벤트 종료
    
    // 메인 스토리 이벤트 종료
    FINISH_MAINSTORY_1ST,       // 1회차 메인 이벤트 종료
    FINISH_MAINSTORY_2ND,       // 2회차 메인 이벤트 종료
    FINISH_MAINSTORY_3RD,       // 3회차 메인 이벤트 종료
    FINISH_MAINSTORY_EXTRA,     // 다회차 메인 이벤트 종료
    
    // 전투 이벤트 종료
    FINISH_BATTLE_1ST,          // 1회차 전투 이벤트 종료
    FINISH_BATTLE_2ND,          // 2회차 전투 이벤트 종료
    FINISH_BATTLE_3RD,          // 3회차 전투 이벤트 종료
    FINISH_BATTLE_EXTRA         // 다회차 전투 이벤트 종료
}

/// <summary>
/// 이벤트 타입 확장 메서드
/// </summary>
public static class LogEventTypeExtensions
{
    /// <summary>
    /// 이벤트 타입을 Firebase Analytics 이벤트 이름으로 변환
    /// </summary>
    public static string ToEventName(this LogEventType eventType)
    {
        return eventType.ToString().ToLower();
    }
    
    /// <summary>
    /// 이벤트가 1회만 실행되어야 하는지 확인
    /// </summary>
    public static bool IsOneTimeEvent(this LogEventType eventType)
    {
        switch (eventType)
        {
            case LogEventType.PUCOUNT:
            case LogEventType.ENTER_STORY_1ST:
            case LogEventType.FINISH_STORY_1ST:
            case LogEventType.ENTER_STORY_2ND:
            case LogEventType.FINISH_STORY_2ND:
            case LogEventType.ENTER_STORY_3RD:
            case LogEventType.FINISH_STORY_3RD:
                return true;
            default:
                return false;
        }
    }
}

