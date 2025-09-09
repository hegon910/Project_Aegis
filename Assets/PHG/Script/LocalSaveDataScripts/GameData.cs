using System;
using System.Collections.Generic;

[Serializable]
public class GameSettings
{
    // 향후 추가될 사운드, 언어 등 환경설정 값을 여기에 추가합니다.
    public float masterVolume = 1.0f;
    public float bgmVolume = 1.0f;
    public float sfxVolume = 1.0f;
}

[Serializable]
public class GameData
{
    // --- 진행 상태 ---
     public GameState currentGameState;
    public int playthroughCount;        // 현재 회차 (PlayerStats)
    public CommanderTrait activeTrait;
    public int currentChapter;          // 현재 챕터 (GameManager)
    public int eventPlaylistIndex;      // 현재 챕터의 이벤트 진행도 (EventManager)
    public List<int> currentPlaylist;   // 현재 챕터의 이벤트 목록 (EventManager)

    // --- 파라미터 ---
    public int politics;                // 정치력
    public int militaryPower;           // 병력
    public int supplies;                // 물자
    public int leadership;              // 리더십
    public int warSituation;            // 전세
    public int karma;                   // 카르마

    // --- 플래그 & 기록 ---
    public List<int> completedEventIds;        // 완료한 이벤트 ID 목록 (PlayerStats)
    public List<string> unlockedAchievements;  // 달성한 업적 ID 목록
    public List<string> completedEndings;      // 본 엔딩 ID 목록
    public List<int> playedSubEventGroups;     // 플레이한 서브 이벤트 그룹 (EventManager)

    // --- 메타 데이터 ---
    public bool isTutorialFinished;     // 튜토리얼 완료 여부
    public float totalPlayTime;         // 총 플레이 시간
    public GameSettings settings;       // 환경 설정

    /// <summary>
    /// 새 게임 시작 시 기본값 설정
    /// </summary>
    public GameData()
    {
        // PlayerStats.InitializeStats() 내용을 기반으로 초기값 설정
        playthroughCount = 1;
        activeTrait = CommanderTrait.Devost;// [추가] 기본 지휘관 특성으로 초기화
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

        isTutorialFinished = false;
        totalPlayTime = 0f;
        settings = new GameSettings();
    }
}