using UnityEngine;

/// <summary>
/// 게임 이벤트 로거
/// 회차 진입/클리어, 장 클리어, 파라미터/메인스토리/전투 이벤트 로깅을 담당합니다.
/// </summary>
public static class GameEventLogger
{
    private const string PARAM_CHAPTER_NUM = "chapter_num";
    
    #region 회차 진입 이벤트
    
    /// <summary>
    /// 1회차 진입 이벤트 (최초 게임 시작)
    /// </summary>
    public static void LogEnterStory1st()
    {
        if (AnalyticsEventManager.Instance != null)
        {
            AnalyticsEventManager.Instance.LogEvent(LogEventType.ENTER_STORY_1ST);
        }
    }
    
    /// <summary>
    /// 2회차 진입 이벤트
    /// </summary>
    public static void LogEnterStory2nd()
    {
        if (AnalyticsEventManager.Instance != null)
        {
            AnalyticsEventManager.Instance.LogEvent(LogEventType.ENTER_STORY_2ND);
        }
    }
    
    /// <summary>
    /// 3회차 진입 이벤트
    /// </summary>
    public static void LogEnterStory3rd()
    {
        if (AnalyticsEventManager.Instance != null)
        {
            AnalyticsEventManager.Instance.LogEvent(LogEventType.ENTER_STORY_3RD);
        }
    }
    
    /// <summary>
    /// 다회차 진입 이벤트 (3회차 이후)
    /// </summary>
    public static void LogEnterStoryExtra()
    {
        if (AnalyticsEventManager.Instance != null)
        {
            AnalyticsEventManager.Instance.LogEvent(LogEventType.ENTER_STORY_EXTRA);
        }
    }
    
    #endregion
    
    #region 회차 클리어 이벤트
    
    /// <summary>
    /// 1회차 클리어 이벤트
    /// </summary>
    public static void LogFinishStory1st()
    {
        if (AnalyticsEventManager.Instance != null)
        {
            AnalyticsEventManager.Instance.LogEvent(LogEventType.FINISH_STORY_1ST);
        }
    }
    
    /// <summary>
    /// 2회차 클리어 이벤트
    /// </summary>
    public static void LogFinishStory2nd()
    {
        if (AnalyticsEventManager.Instance != null)
        {
            AnalyticsEventManager.Instance.LogEvent(LogEventType.FINISH_STORY_2ND);
        }
    }
    
    /// <summary>
    /// 3회차 클리어 이벤트
    /// </summary>
    public static void LogFinishStory3rd()
    {
        if (AnalyticsEventManager.Instance != null)
        {
            AnalyticsEventManager.Instance.LogEvent(LogEventType.FINISH_STORY_3RD);
        }
    }
    
    /// <summary>
    /// 다회차 클리어 이벤트
    /// </summary>
    public static void LogFinishStoryExtra()
    {
        if (AnalyticsEventManager.Instance != null)
        {
            AnalyticsEventManager.Instance.LogEvent(LogEventType.FINISH_STORY_EXTRA);
        }
    }
    
    #endregion
    
    #region 장 클리어 이벤트
    
    /// <summary>
    /// 1회차 장 클리어 이벤트
    /// </summary>
    /// <param name="chapterNum">클리어한 장 번호</param>
    public static void LogFinishChapter1st(int chapterNum)
    {
        if (AnalyticsEventManager.Instance != null)
        {
            AnalyticsEventManager.Instance.LogEvent(LogEventType.FINISH_CHAPTER_1ST, PARAM_CHAPTER_NUM, chapterNum);
        }
    }
    
    /// <summary>
    /// 2회차 장 클리어 이벤트
    /// </summary>
    /// <param name="chapterNum">클리어한 장 번호</param>
    public static void LogFinishChapter2nd(int chapterNum)
    {
        if (AnalyticsEventManager.Instance != null)
        {
            AnalyticsEventManager.Instance.LogEvent(LogEventType.FINISH_CHAPTER_2ND, PARAM_CHAPTER_NUM, chapterNum);
        }
    }
    
    /// <summary>
    /// 3회차 장 클리어 이벤트
    /// </summary>
    /// <param name="chapterNum">클리어한 장 번호</param>
    public static void LogFinishChapter3rd(int chapterNum)
    {
        if (AnalyticsEventManager.Instance != null)
        {
            AnalyticsEventManager.Instance.LogEvent(LogEventType.FINISH_CHAPTER_3RD, PARAM_CHAPTER_NUM, chapterNum);
        }
    }
    
    /// <summary>
    /// 다회차 장 클리어 이벤트
    /// </summary>
    /// <param name="chapterNum">클리어한 장 번호</param>
    public static void LogFinishChapterExtra(int chapterNum)
    {
        if (AnalyticsEventManager.Instance != null)
        {
            AnalyticsEventManager.Instance.LogEvent(LogEventType.FINISH_CHAPTER_EXTRA, PARAM_CHAPTER_NUM, chapterNum);
        }
    }
    
    #endregion
    
    #region 파라미터 이벤트 종료
    
    /// <summary>
    /// 1회차 파라미터 이벤트 종료
    /// </summary>
    /// <param name="chapterNum">파라미터 이벤트를 끝낸 장 번호</param>
    public static void LogFinishParameter1st(int chapterNum)
    {
        if (AnalyticsEventManager.Instance != null)
        {
            AnalyticsEventManager.Instance.LogEvent(LogEventType.FINISH_PARAMETER_1ST, PARAM_CHAPTER_NUM, chapterNum);
        }
    }
    
    /// <summary>
    /// 2회차 파라미터 이벤트 종료
    /// </summary>
    /// <param name="chapterNum">파라미터 이벤트를 끝낸 장 번호</param>
    public static void LogFinishParameter2nd(int chapterNum)
    {
        if (AnalyticsEventManager.Instance != null)
        {
            AnalyticsEventManager.Instance.LogEvent(LogEventType.FINISH_PARAMETER_2ND, PARAM_CHAPTER_NUM, chapterNum);
        }
    }
    
    /// <summary>
    /// 3회차 파라미터 이벤트 종료
    /// </summary>
    /// <param name="chapterNum">파라미터 이벤트를 끝낸 장 번호</param>
    public static void LogFinishParameter3rd(int chapterNum)
    {
        if (AnalyticsEventManager.Instance != null)
        {
            AnalyticsEventManager.Instance.LogEvent(LogEventType.FINISH_PARAMETER_3RD, PARAM_CHAPTER_NUM, chapterNum);
        }
    }
    
    /// <summary>
    /// 다회차 파라미터 이벤트 종료
    /// </summary>
    /// <param name="chapterNum">파라미터 이벤트를 끝낸 장 번호</param>
    public static void LogFinishParameterExtra(int chapterNum)
    {
        if (AnalyticsEventManager.Instance != null)
        {
            AnalyticsEventManager.Instance.LogEvent(LogEventType.FINISH_PARAMETER_EXTRA, PARAM_CHAPTER_NUM, chapterNum);
        }
    }
    
    #endregion
    
    #region 메인 스토리 이벤트 종료
    
    /// <summary>
    /// 1회차 메인 스토리 이벤트 종료
    /// </summary>
    /// <param name="chapterNum">메인 스토리 이벤트를 끝낸 장 번호</param>
    public static void LogFinishMainStory1st(int chapterNum)
    {
        if (AnalyticsEventManager.Instance != null)
        {
            AnalyticsEventManager.Instance.LogEvent(LogEventType.FINISH_MAINSTORY_1ST, PARAM_CHAPTER_NUM, chapterNum);
        }
    }
    
    /// <summary>
    /// 2회차 메인 스토리 이벤트 종료
    /// </summary>
    /// <param name="chapterNum">메인 스토리 이벤트를 끝낸 장 번호</param>
    public static void LogFinishMainStory2nd(int chapterNum)
    {
        if (AnalyticsEventManager.Instance != null)
        {
            AnalyticsEventManager.Instance.LogEvent(LogEventType.FINISH_MAINSTORY_2ND, PARAM_CHAPTER_NUM, chapterNum);
        }
    }
    
    /// <summary>
    /// 3회차 메인 스토리 이벤트 종료
    /// </summary>
    /// <param name="chapterNum">메인 스토리 이벤트를 끝낸 장 번호</param>
    public static void LogFinishMainStory3rd(int chapterNum)
    {
        if (AnalyticsEventManager.Instance != null)
        {
            AnalyticsEventManager.Instance.LogEvent(LogEventType.FINISH_MAINSTORY_3RD, PARAM_CHAPTER_NUM, chapterNum);
        }
    }
    
    /// <summary>
    /// 다회차 메인 스토리 이벤트 종료
    /// </summary>
    /// <param name="chapterNum">메인 스토리 이벤트를 끝낸 장 번호</param>
    public static void LogFinishMainStoryExtra(int chapterNum)
    {
        if (AnalyticsEventManager.Instance != null)
        {
            AnalyticsEventManager.Instance.LogEvent(LogEventType.FINISH_MAINSTORY_EXTRA, PARAM_CHAPTER_NUM, chapterNum);
        }
    }
    
    #endregion
    
    #region 전투 이벤트 종료
    
    /// <summary>
    /// 1회차 전투 이벤트 종료
    /// </summary>
    /// <param name="chapterNum">전투 이벤트를 끝낸 장 번호</param>
    public static void LogFinishBattle1st(int chapterNum)
    {
        if (AnalyticsEventManager.Instance != null)
        {
            AnalyticsEventManager.Instance.LogEvent(LogEventType.FINISH_BATTLE_1ST, PARAM_CHAPTER_NUM, chapterNum);
        }
    }
    
    /// <summary>
    /// 2회차 전투 이벤트 종료
    /// </summary>
    /// <param name="chapterNum">전투 이벤트를 끝낸 장 번호</param>
    public static void LogFinishBattle2nd(int chapterNum)
    {
        if (AnalyticsEventManager.Instance != null)
        {
            AnalyticsEventManager.Instance.LogEvent(LogEventType.FINISH_BATTLE_2ND, PARAM_CHAPTER_NUM, chapterNum);
        }
    }
    
    /// <summary>
    /// 3회차 전투 이벤트 종료
    /// </summary>
    /// <param name="chapterNum">전투 이벤트를 끝낸 장 번호</param>
    public static void LogFinishBattle3rd(int chapterNum)
    {
        if (AnalyticsEventManager.Instance != null)
        {
            AnalyticsEventManager.Instance.LogEvent(LogEventType.FINISH_BATTLE_3RD, PARAM_CHAPTER_NUM, chapterNum);
        }
    }
    
    /// <summary>
    /// 다회차 전투 이벤트 종료
    /// </summary>
    /// <param name="chapterNum">전투 이벤트를 끝낸 장 번호</param>
    public static void LogFinishBattleExtra(int chapterNum)
    {
        if (AnalyticsEventManager.Instance != null)
        {
            AnalyticsEventManager.Instance.LogEvent(LogEventType.FINISH_BATTLE_EXTRA, PARAM_CHAPTER_NUM, chapterNum);
        }
    }
    
    #endregion
    
    #region 통합 헬퍼 메서드
    
    /// <summary>
    /// 회차에 따라 자동으로 적절한 진입 이벤트 로그
    /// </summary>
    /// <param name="playthrough">회차 번호 (1, 2, 3, 4+)</param>
    public static void LogEnterStory(int playthrough)
    {
        switch (playthrough)
        {
            case 1:
                LogEnterStory1st();
                break;
            case 2:
                LogEnterStory2nd();
                break;
            case 3:
                LogEnterStory3rd();
                break;
            default:
                if (playthrough > 3)
                    LogEnterStoryExtra();
                break;
        }
    }
    
    /// <summary>
    /// 회차에 따라 자동으로 적절한 클리어 이벤트 로그
    /// </summary>
    /// <param name="playthrough">회차 번호 (1, 2, 3, 4+)</param>
    public static void LogFinishStory(int playthrough)
    {
        switch (playthrough)
        {
            case 1:
                LogFinishStory1st();
                break;
            case 2:
                LogFinishStory2nd();
                break;
            case 3:
                LogFinishStory3rd();
                break;
            default:
                if (playthrough > 3)
                    LogFinishStoryExtra();
                break;
        }
    }
    
    /// <summary>
    /// 회차에 따라 자동으로 적절한 장 클리어 이벤트 로그
    /// </summary>
    /// <param name="playthrough">회차 번호 (1, 2, 3, 4+)</param>
    /// <param name="chapterNum">클리어한 장 번호</param>
    public static void LogFinishChapter(int playthrough, int chapterNum)
    {
        switch (playthrough)
        {
            case 1:
                LogFinishChapter1st(chapterNum);
                break;
            case 2:
                LogFinishChapter2nd(chapterNum);
                break;
            case 3:
                LogFinishChapter3rd(chapterNum);
                break;
            default:
                if (playthrough > 3)
                    LogFinishChapterExtra(chapterNum);
                break;
        }
    }
    
    /// <summary>
    /// 회차에 따라 자동으로 적절한 파라미터 이벤트 종료 로그
    /// </summary>
    /// <param name="playthrough">회차 번호 (1, 2, 3, 4+)</param>
    /// <param name="chapterNum">파라미터 이벤트를 끝낸 장 번호</param>
    public static void LogFinishParameter(int playthrough, int chapterNum)
    {
        switch (playthrough)
        {
            case 1:
                LogFinishParameter1st(chapterNum);
                break;
            case 2:
                LogFinishParameter2nd(chapterNum);
                break;
            case 3:
                LogFinishParameter3rd(chapterNum);
                break;
            default:
                if (playthrough > 3)
                    LogFinishParameterExtra(chapterNum);
                break;
        }
    }
    
    /// <summary>
    /// 회차에 따라 자동으로 적절한 메인 스토리 이벤트 종료 로그
    /// </summary>
    /// <param name="playthrough">회차 번호 (1, 2, 3, 4+)</param>
    /// <param name="chapterNum">메인 스토리 이벤트를 끝낸 장 번호</param>
    public static void LogFinishMainStory(int playthrough, int chapterNum)
    {
        switch (playthrough)
        {
            case 1:
                LogFinishMainStory1st(chapterNum);
                break;
            case 2:
                LogFinishMainStory2nd(chapterNum);
                break;
            case 3:
                LogFinishMainStory3rd(chapterNum);
                break;
            default:
                if (playthrough > 3)
                    LogFinishMainStoryExtra(chapterNum);
                break;
        }
    }
    
    /// <summary>
    /// 회차에 따라 자동으로 적절한 전투 이벤트 종료 로그
    /// </summary>
    /// <param name="playthrough">회차 번호 (1, 2, 3, 4+)</param>
    /// <param name="chapterNum">전투 이벤트를 끝낸 장 번호</param>
    public static void LogFinishBattle(int playthrough, int chapterNum)
    {
        switch (playthrough)
        {
            case 1:
                LogFinishBattle1st(chapterNum);
                break;
            case 2:
                LogFinishBattle2nd(chapterNum);
                break;
            case 3:
                LogFinishBattle3rd(chapterNum);
                break;
            default:
                if (playthrough > 3)
                    LogFinishBattleExtra(chapterNum);
                break;
        }
    }
    
    #endregion
}

