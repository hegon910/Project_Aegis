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
    public List<string> unlockedAchievements;  // 달성한 도전과제 ID 목록
    public List<string> completedEndings;      // 본 엔딩 ID 목록
    public List<int> playedSubEventGroups;     // 플레이한 서브 이벤트 그룹 (EventManager)
    public List<int> completedBattleResultIds; // 완료한 전투 결과 ID 목록
    public List<int> completedEndingIds;       // 완료한 엔딩 ID 목록

    // --- 기타 데이터 ---
    public bool isTutorialFinished;     // 튜토리얼 완료 여부
    public float totalPlayTime;         // 총 플레이 시간
    public GameSettings settings;       // 환경 설정


    // --- 게임오버 후 메인복귀 시 이어하기에서 챕터 처음부터 재시작하기 위한 플래그 ---
    public bool pendingRestartFromGameOver; // true면 다음 이어하기 시 챕터 처음부터 재시작
    public int pendingRestartChapter;       // 재시작할 챕터(0이면 무시)

     // --- 동기화를 위한 타임스탬프 -- 9.9. 이학권 추가
    public long lastUpdated;
    // 서버 권위 타임스탬프(UTC ms). Firebase RTDB ServerValue.Timestamp로 채워짐
    public long lastUpdatedServer;

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
        completedEndings = new List<string>();
        playedSubEventGroups = new List<int>();
        completedBattleResultIds = new List<int>();
        completedEndingIds = new List<int>();

        isTutorialFinished = false;
        totalPlayTime = 0f;
        settings = new GameSettings();

        pendingRestartFromGameOver = false;
        pendingRestartChapter = 0;

        // 타임스탬프 9.9. 이학권 추가
        lastUpdated = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        lastUpdatedServer = 0;
    }
}