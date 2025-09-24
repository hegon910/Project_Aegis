using UnityEngine;
using System;

/// <summary>
/// 업적 데이터 구조체
/// </summary>
[System.Serializable]
public class AchievementData
{
    [Header("업적 기본 정보")]
    public string achievementId;           // 업적 고유 ID
    public string title;                  // 업적 제목
    [TextArea(3, 5)]
    public string description;            // 업적 설명
    public AchievementType type;          // 업적 타입
    public AchievementCategory category;  // 업적 카테고리
    
    [Header("업적 조건")]
    public AchievementCondition condition; // 업적 달성 조건
    
    [Header("업적 보상")]
    public AchievementReward reward;      // 업적 보상
    
    [Header("업적 상태")]
    public bool isUnlocked;              // 업적 해금 여부
    public bool isCompleted;             // 업적 완료 여부
    public bool isRewardClaimed;         // 보상 수령 여부
    public DateTime unlockedDate;        // 해금 날짜
    public DateTime completedDate;       // 완료 날짜
    
    [Header("UI 표시")]
    public Sprite icon;                  // 업적 아이콘
    public Sprite lockedIcon;            // 잠긴 상태 아이콘
    public Color titleColor;             // 제목 색상
    public Color descriptionColor;       // 설명 색상
}

/// <summary>
/// 업적 타입 열거형
/// </summary>
public enum AchievementType
{
    Ending,         // 엔딩 달성
    Playthrough,    // 회차 시작
    Battle,         // 전투 관련
    Event,          // 이벤트 관련
    Story,          // 스토리 진행
    Collection      // 수집 관련
}

/// <summary>
/// 업적 카테고리 열거형
/// </summary>
public enum AchievementCategory
{
    All,
    General,        // 일반 업적
    Hidden,         // 히든 업적
    Special         // 특별 업적
}

/// <summary>
/// 업적 조건 클래스
/// </summary>
[System.Serializable]
public class AchievementCondition
{
    public ConditionType conditionType;
    public int requiredValue;           // 필요 값
    public string requiredString;       // 필요 문자열 (이벤트 ID 등)
    public bool requireExactMatch;      // 정확한 매치 필요 여부
    
    // 엔딩 관련 조건
    public EndingType requiredEndingType;
    public EndingRoute requiredEndingRoute;
    public int requiredEndingBranch;
    
    // 전투 관련 조건
    public GameOutcome requiredBattleOutcome;
    
    // 회차 관련 조건
    public int requiredPlaythroughCount;
    
    // 파라미터 관련 조건
    public ParameterType requiredParameter;
    public int requiredParameterValue;
    
    // 이벤트 관련 조건
    public int requiredEventId;
    public bool requireEventSuccess;
}

/// <summary>
/// 조건 타입 열거형
/// </summary>
public enum ConditionType
{
    None,                    // 조건 없음
    EndingCompleted,         // 특정 엔딩 완료
    PlaythroughStarted,      // 특정 회차 시작
    BattleResult,            // 전투 결과
    EventCompleted,          // 이벤트 완료
    ParameterReached,        // 파라미터 도달
    StoryFlagSet,            // 스토리 플래그 설정
    CollectionCount,         // 수집 개수
    PlayTimeReached,         // 플레이 시간 도달
    CustomCondition          // 커스텀 조건
}

/// <summary>
/// 업적 보상 클래스
/// </summary>
[System.Serializable]
public class AchievementReward
{
    public RewardType rewardType;
    public int rewardValue;             // 보상 값
    public string rewardDescription;    // 보상 설명
    public Sprite rewardIcon;           // 보상 아이콘
}

/// <summary>
/// 보상 타입 열거형
/// </summary>
public enum RewardType
{
    None,           // 보상 없음
    Currency,       // 화폐
    Item,           // 아이템
    Title,          // 칭호
    Unlock,         // 해금
    Experience      // 경험치
}
