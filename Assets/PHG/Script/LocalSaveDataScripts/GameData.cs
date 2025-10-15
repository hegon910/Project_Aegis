using System;
using System.Collections.Generic;


[Serializable]
public class GameData
{
    // --- 게임 상태 ---
     public GameState currentGameState;
    public int playthroughCount;        // 현재 횣차 (PlayerStats)
    public int lastEndingId;            // 직전 회차에서 본 엔딩 ID
    public CommanderTrait activeTrait;
    public int currentChapter;          // 현재 챕터 (GameManager)
    public int eventPlaylistIndex;      // 현재 챕터의 이벤트 진행도 (EventManager)
    public List<int> currentPlaylist;   // 현재 챕터의 이벤트 목록 (EventManager)

    // --- 파라미터 ---
    public int politics;                // 정치력
    public int militaryPower;           // 병력
    public int supplies;                // 물자
    public int leadership;              // 리더십
    public int warSituation;            // 전황
    public int karma;                   // 카르마

    // --- 플레이 기록 & 업적 ---
    public List<int> completedEventIds;        // 완료한 이벤트 ID 목록 (PlayerStats)
    
    // [Deprecated] 업적 데이터는 이제 SettingsData로 이동되었습니다.
    // 호환성을 위해 필드는 남겨두지만, 새 게임 시작 시 초기화되지 않도록 
    // AchievementManager는 SettingsData를 사용합니다.
    public List<string> unlockedAchievements;  // 달성한 도전과제 ID 목록 (SettingsData로 이동됨)
    public List<string> claimedAchievementIds; // 수령 완료한 업적 ID 목록 (SettingsData로 이동됨)
    
    public List<string> completedEndings;      // 본 엔딩 ID 목록
    public List<int> playedSubEventGroups;     // 플레이한 서브 이벤트 그룹 (EventManager)
    public List<int> completedBattleResultIds; // 완료한 전투 결과 ID 목록
    public List<int> completedEndingIds;       // 완료한 엔딩 ID 목록

    // --- 기타 데이터 ---
    public bool isTutorialFinished;     // 튜토리얼 완료 여부
    public float totalPlayTime;         // 총 플레이 시간
    public GameSettings settings;       // 환경 설정

    // --- 멀티 엔딩 시스템 ---
    public List<ChapterBattleResult> chapterBattleResults; // 챕터별 전투 결과
    public bool hasHiddenEndingFlag; // 히든 엔딩 플래그
    public int hiddenEndingChoiceCount; // 히든 엔딩을 위한 선택 횟수
    
    // --- 진엔딩 카운팅 ---
    public int realEnding2ChoiceCount; // 2회차 진엔딩용 선택 카운트
    public int realEnding3ChoiceCount; // 3회차 진엔딩용 선택 카운트

    // --- 게임오버 후 메인복귀 시 이어하기에서 챕터 처음부터 재시작하기 위한 플래그 ---
    public bool pendingRestartFromGameOver; // true면 다음 이어하기 시 챕터 처음부터 재시작
    public int pendingRestartChapter;       // 재시작할 챕터(0이면 무시)

     // --- 동기화를 위한 타임스탬프 -- 9.9. 이학권 추가
    public long lastUpdated;
    // 서버 권위 타임스탬프(UTC ms). Firebase RTDB ServerValue.Timestamp로 채워짐
    public long lastUpdatedServer;
    
    // --- 게스트 계정 마이그레이션 정보 ---
    public bool isMigratedFromGuest;        // 게스트에서 연동된 계정인지 여부
    public long migrationTimestamp;         // 마이그레이션된 시점의 타임스탬프

    /// <summary>
    ///     기본 생성자
    /// </summary>
    public GameData()
    {
        // PlayerStats.InitializeStats()의 초기값과 동일하게 설정
        playthroughCount = 1;
        lastEndingId = 0;
        activeTrait = CommanderTrait.Devost;// [추가] 기본 지휘관 특성 초기화
        currentChapter = 1;
        eventPlaylistIndex = 0;
        currentPlaylist = new List<int>();

        politics = 50;
        militaryPower = 50;
        supplies = 50;
        leadership = 50;
        warSituation = 50;
        karma = 50;

        completedEventIds = new List<int>();
        unlockedAchievements = new List<string>();
        claimedAchievementIds = new List<string>();
        completedEndings = new List<string>();
        playedSubEventGroups = new List<int>();
        completedBattleResultIds = new List<int>();
        completedEndingIds = new List<int>();

        isTutorialFinished = false;
        totalPlayTime = 0f;
        settings = new GameSettings();

        pendingRestartFromGameOver = false;
        pendingRestartChapter = 0;

        // 멀티 엔딩 시스템 초기화
        chapterBattleResults = new List<ChapterBattleResult>();
        hasHiddenEndingFlag = false;
        hiddenEndingChoiceCount = 0;
        
        // 진엔딩 카운팅 초기화
        realEnding2ChoiceCount = 0;
        realEnding3ChoiceCount = 0;

        // 타임스탬프 9.9. 이학권 추가
        lastUpdated = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        lastUpdatedServer = 0;
        
        // 마이그레이션 정보 초기화
        isMigratedFromGuest = false;
        migrationTimestamp = 0;
    }
}