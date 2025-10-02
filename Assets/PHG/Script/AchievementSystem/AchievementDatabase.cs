using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 기획서에 따른 모든 업적 데이터를 정의하는 클래스
/// ScriptableObject 없이 코드에서 직접 정의하여 사용
/// </summary>
public static class AchievementDatabase
{
    /// <summary>
    /// 모든 업적 데이터를 반환합니다
    /// </summary>
    public static List<AchievementData> GetAllAchievements()
    {
        var achievements = new List<AchievementData>();
        
        // === 일반 엔딩 업적 (9개) ===
        achievements.AddRange(GetGeneralEndingAchievements());
        
        // === 진 엔딩 업적 (2개) ===
        achievements.AddRange(GetTrueEndingAchievements());
        
        // === 히든 엔딩 업적 (1개) ===
        achievements.AddRange(GetHiddenEndingAchievements());
        
        // === 회차 업적 (7개) ===
        achievements.AddRange(GetPlaythroughAchievements());
        
        // === 전투 업적 (3개) ===
        achievements.AddRange(GetBattleAchievements());
        
        // === 메인 스토리 업적 (4개) ===
        achievements.AddRange(GetMainStoryAchievements());
        
        // === 서브 스토리 업적 (4개) ===
        achievements.AddRange(GetSubStoryAchievements());
        
        return achievements;
    }
    
    /// <summary>
    /// 일반 엔딩 업적들 (9개) - 스프레드시트 기준
    /// </summary>
    private static List<AchievementData> GetGeneralEndingAchievements()
    {
        return new List<AchievementData>
        {
            // 1회차 승리 + 카르마 높음
            CreateAchievement(
                "ending_general_1st_victory_high_karma",
                "명장",
                "누구보다 빛나는 승리를 거두었습니다.",
                AchievementType.Ending,
                AchievementCategory.General,
                new AchievementCondition
                {
                    conditionType = ConditionType.EndingCompleted,
                    requiredEndingType = EndingType.General,
                    requiredEndingRoute = EndingRoute.Victory,
                    requiredPlaythroughCount = 1,
                    requiredParameter = ParameterType.카르마,
                    requiredParameterValue = 80
                },
                new AchievementReward { rewardType = RewardType.Title, rewardDescription = "명장 칭호 획득" }
            ),
            
            // 1회차 무승부 + 카르마 보통
            CreateAchievement(
                "ending_general_1st_draw_normal_karma",
                "수호자",
                "모두를 지켜내며 무승부에 도달했습니다.",
                AchievementType.Ending,
                AchievementCategory.General,
                new AchievementCondition
                {
                    conditionType = ConditionType.EndingCompleted,
                    requiredEndingType = EndingType.General,
                    requiredEndingRoute = EndingRoute.Truce,
                    requiredPlaythroughCount = 1,
                    requiredParameter = ParameterType.카르마,
                    requiredParameterValue = 50
                },
                new AchievementReward { rewardType = RewardType.Title, rewardDescription = "수호자 칭호 획득" }
            ),
            
            // 1회차 패배 + 카르마 낮음
            CreateAchievement(
                "ending_general_1st_defeat_low_karma",
                "위선자",
                "스스로의 모순 끝에 패배를 맞이했습니다.",
                AchievementType.Ending,
                AchievementCategory.General,
                new AchievementCondition
                {
                    conditionType = ConditionType.EndingCompleted,
                    requiredEndingType = EndingType.General,
                    requiredEndingRoute = EndingRoute.Defeat,
                    requiredPlaythroughCount = 1,
                    requiredParameter = ParameterType.카르마,
                    requiredParameterValue = 20
                },
                new AchievementReward { rewardType = RewardType.Title, rewardDescription = "위선자 칭호 획득" }
            ),
            
            // 2회차 승리 + 카르마 높음
            CreateAchievement(
                "ending_general_2nd_victory_high_karma",
                "지휘관",
                "평범하지만 값진 승리를 거두었습니다.",
                AchievementType.Ending,
                AchievementCategory.General,
                new AchievementCondition
                {
                    conditionType = ConditionType.EndingCompleted,
                    requiredEndingType = EndingType.General,
                    requiredEndingRoute = EndingRoute.Victory,
                    requiredPlaythroughCount = 2,
                    requiredParameter = ParameterType.카르마,
                    requiredParameterValue = 80
                },
                new AchievementReward { rewardType = RewardType.Title, rewardDescription = "지휘관 칭호 획득" }
            ),
            
            // 2회차 무승부 + 카르마 보통
            CreateAchievement(
                "ending_general_2nd_draw_normal_karma",
                "범인",
                "특별하지 않은 길, 무승부로 여정을 마쳤습니다.",
                AchievementType.Ending,
                AchievementCategory.General,
                new AchievementCondition
                {
                    conditionType = ConditionType.EndingCompleted,
                    requiredEndingType = EndingType.General,
                    requiredEndingRoute = EndingRoute.Truce,
                    requiredPlaythroughCount = 2,
                    requiredParameter = ParameterType.카르마,
                    requiredParameterValue = 50
                },
                new AchievementReward { rewardType = RewardType.Title, rewardDescription = "범인 칭호 획득" }
            ),
            
            // 2회차 패배 + 카르마 낮음
            CreateAchievement(
                "ending_general_2nd_defeat_low_karma",
                "무능한 지휘관",
                "이끌 힘을 잃고 패배했습니다.",
                AchievementType.Ending,
                AchievementCategory.General,
                new AchievementCondition
                {
                    conditionType = ConditionType.EndingCompleted,
                    requiredEndingType = EndingType.General,
                    requiredEndingRoute = EndingRoute.Defeat,
                    requiredPlaythroughCount = 2,
                    requiredParameter = ParameterType.카르마,
                    requiredParameterValue = 20
                },
                new AchievementReward { rewardType = RewardType.Title, rewardDescription = "무능한 지휘관 칭호 획득" }
            ),
            
            // 3회차 승리 + 카르마 높음
            CreateAchievement(
                "ending_general_3rd_victory_high_karma",
                "승부사",
                "모든 걸 걸어 승리를 거두었습니다.",
                AchievementType.Ending,
                AchievementCategory.General,
                new AchievementCondition
                {
                    conditionType = ConditionType.EndingCompleted,
                    requiredEndingType = EndingType.General,
                    requiredEndingRoute = EndingRoute.Victory,
                    requiredPlaythroughCount = 3,
                    requiredParameter = ParameterType.카르마,
                    requiredParameterValue = 80
                },
                new AchievementReward { rewardType = RewardType.Title, rewardDescription = "승부사 칭호 획득" }
            ),
            
            // 3회차 무승부 + 카르마 보통
            CreateAchievement(
                "ending_general_3rd_draw_normal_karma",
                "기회주의자",
                "상황에 기대어 무승부로 끝냈습니다.",
                AchievementType.Ending,
                AchievementCategory.General,
                new AchievementCondition
                {
                    conditionType = ConditionType.EndingCompleted,
                    requiredEndingType = EndingType.General,
                    requiredEndingRoute = EndingRoute.Truce,
                    requiredPlaythroughCount = 3,
                    requiredParameter = ParameterType.카르마,
                    requiredParameterValue = 50
                },
                new AchievementReward { rewardType = RewardType.Title, rewardDescription = "기회주의자 칭호 획득" }
            ),
            
            // 3회차 패배 + 카르마 낮음
            CreateAchievement(
                "ending_general_3rd_defeat_low_karma",
                "전범",
                "패배의 책임을 홀로 짊어졌습니다.",
                AchievementType.Ending,
                AchievementCategory.General,
                new AchievementCondition
                {
                    conditionType = ConditionType.EndingCompleted,
                    requiredEndingType = EndingType.General,
                    requiredEndingRoute = EndingRoute.Defeat,
                    requiredPlaythroughCount = 3,
                    requiredParameter = ParameterType.카르마,
                    requiredParameterValue = 20
                },
                new AchievementReward { rewardType = RewardType.Title, rewardDescription = "전범 칭호 획득" }
            )
        };
    }
    
    /// <summary>
    /// 진 엔딩 업적들 (2개) - 스프레드시트 기준
    /// </summary>
    private static List<AchievementData> GetTrueEndingAchievements()
    {
        return new List<AchievementData>
        {
            // 2회차 진엔딩 달성
            CreateAchievement(
                "ending_true_2nd_playthrough",
                "진실에 다가서다",
                "감춰진 진실에 한 걸음 다가섰습니다.",
                AchievementType.Ending,
                AchievementCategory.True,
                new AchievementCondition
                {
                    conditionType = ConditionType.EndingCompleted,
                    requiredEndingType = EndingType.True,
                    requiredPlaythroughCount = 2
                },
                new AchievementReward { rewardType = RewardType.Title, rewardDescription = "진실에 다가서다 칭호 획득" }
            ),
            
            // 3회차 진엔딩 달성
            CreateAchievement(
                "ending_true_3rd_playthrough",
                "거짓된 파편",
                "거짓과 진실이 교차하는 파편을 마주했습니다.",
                AchievementType.Ending,
                AchievementCategory.True,
                new AchievementCondition
                {
                    conditionType = ConditionType.EndingCompleted,
                    requiredEndingType = EndingType.True,
                    requiredPlaythroughCount = 3
                },
                new AchievementReward { rewardType = RewardType.Title, rewardDescription = "거짓된 파편 칭호 획득" }
            )
        };
    }
    
    /// <summary>
    /// 히든 엔딩 업적들 (1개) - 스프레드시트 기준
    /// </summary>
    private static List<AchievementData> GetHiddenEndingAchievements()
    {
        return new List<AchievementData>
        {
            // 3회차 히든엔딩 달성
            CreateAchievement(
                "ending_hidden_3rd_playthrough",
                "파편",
                "숨겨진 결말에 도달했습니다.",
                AchievementType.Ending,
                AchievementCategory.Hidden,
                new AchievementCondition
                {
                    conditionType = ConditionType.EndingCompleted,
                    requiredEndingType = EndingType.Hidden,
                    requiredPlaythroughCount = 3
                },
                new AchievementReward { rewardType = RewardType.Title, rewardDescription = "파편 칭호 획득" }
            )
        };
    }
    
    /// <summary>
    /// 회차 업적들 (7개) - 스프레드시트 기준
    /// </summary>
    private static List<AchievementData> GetPlaythroughAchievements()
    {
        return new List<AchievementData>
        {
            // 1회차 시작
            CreateAchievement(
                "playthrough_1st_start",
                "새로운 시작",
                "첫 여정을 시작했습니다.",
                AchievementType.Playthrough,
                AchievementCategory.General,
                new AchievementCondition
                {
                    conditionType = ConditionType.PlaythroughStarted,
                    requiredPlaythroughCount = 1
                },
                new AchievementReward { rewardType = RewardType.Experience, rewardValue = 100, rewardDescription = "경험치 100 획득" }
            ),
            
            // 2회차 승리 루트 진입
            CreateAchievement(
                "playthrough_2nd_victory_route",
                "돌파구",
                "두 번째 도전에서 길을 열었습니다.",
                AchievementType.Playthrough,
                AchievementCategory.General,
                new AchievementCondition
                {
                    conditionType = ConditionType.EndingCompleted,
                    requiredPlaythroughCount = 2,
                    requiredEndingRoute = EndingRoute.Victory
                },
                new AchievementReward { rewardType = RewardType.Experience, rewardValue = 200, rewardDescription = "경험치 200 획득" }
            ),
            
            // 2회차 무승부 루트 진입
            CreateAchievement(
                "playthrough_2nd_draw_route",
                "끝나지 않은 싸움",
                "결판을 내지 못한 채 이야기가 이어집니다.",
                AchievementType.Playthrough,
                AchievementCategory.General,
                new AchievementCondition
                {
                    conditionType = ConditionType.EndingCompleted,
                    requiredPlaythroughCount = 2,
                    requiredEndingRoute = EndingRoute.Truce
                },
                new AchievementReward { rewardType = RewardType.Experience, rewardValue = 150, rewardDescription = "경험치 150 획득" }
            ),
            
            // 2회차 패배 루트 진입
            CreateAchievement(
                "playthrough_2nd_defeat_route",
                "고난의 길",
                "시련의 끝에서 쓰라린 패배를 맞이했습니다.",
                AchievementType.Playthrough,
                AchievementCategory.General,
                new AchievementCondition
                {
                    conditionType = ConditionType.EndingCompleted,
                    requiredPlaythroughCount = 2,
                    requiredEndingRoute = EndingRoute.Defeat
                },
                new AchievementReward { rewardType = RewardType.Experience, rewardValue = 100, rewardDescription = "경험치 100 획득" }
            ),
            
            // 3회차 승리 루트 진입
            CreateAchievement(
                "playthrough_3rd_victory_route",
                "영광의 순간",
                "마지막 도전에서 승리를 거두었습니다.",
                AchievementType.Playthrough,
                AchievementCategory.Special,
                new AchievementCondition
                {
                    conditionType = ConditionType.EndingCompleted,
                    requiredPlaythroughCount = 3,
                    requiredEndingRoute = EndingRoute.Victory
                },
                new AchievementReward { rewardType = RewardType.Experience, rewardValue = 300, rewardDescription = "경험치 300 획득" }
            ),
            
            // 3회차 무승부 루트 진입
            CreateAchievement(
                "playthrough_3rd_draw_route",
                "미완의 결말",
                "끝내 완결되지 못한 결말에 도달했습니다.",
                AchievementType.Playthrough,
                AchievementCategory.Special,
                new AchievementCondition
                {
                    conditionType = ConditionType.EndingCompleted,
                    requiredPlaythroughCount = 3,
                    requiredEndingRoute = EndingRoute.Truce
                },
                new AchievementReward { rewardType = RewardType.Experience, rewardValue = 250, rewardDescription = "경험치 250 획득" }
            ),
            
            // 3회차 패배 루트 진입
            CreateAchievement(
                "playthrough_3rd_defeat_route",
                "몰락",
                "마지막 도전에서 몰락을 맞이했습니다.",
                AchievementType.Playthrough,
                AchievementCategory.Special,
                new AchievementCondition
                {
                    conditionType = ConditionType.EndingCompleted,
                    requiredPlaythroughCount = 3,
                    requiredEndingRoute = EndingRoute.Defeat
                },
                new AchievementReward { rewardType = RewardType.Experience, rewardValue = 200, rewardDescription = "경험치 200 획득" }
            )
        };
    }
    
    /// <summary>
    /// 전투 업적들 (3개) - 스프레드시트 기준
    /// </summary>
    private static List<AchievementData> GetBattleAchievements()
    {
        return new List<AchievementData>
        {
            // 최초 전투 승리
            CreateAchievement(
                "battle_first_victory",
                "첫 승전보",
                "첫 전투에서 승리를 거두었습니다.",
                AchievementType.Battle,
                AchievementCategory.General,
                new AchievementCondition
                {
                    conditionType = ConditionType.BattleResult,
                    requiredBattleOutcome = GameOutcome.Victory
                },
                new AchievementReward { rewardType = RewardType.Experience, rewardValue = 50, rewardDescription = "경험치 50 획득" }
            ),
            
            // 최초 전투 무승부
            CreateAchievement(
                "battle_first_draw",
                "팽팽한 균형",
                "첫 전투에서 팽팽한 균형을 이루었습니다.",
                AchievementType.Battle,
                AchievementCategory.General,
                new AchievementCondition
                {
                    conditionType = ConditionType.BattleResult,
                    requiredBattleOutcome = GameOutcome.Draw
                },
                new AchievementReward { rewardType = RewardType.Experience, rewardValue = 30, rewardDescription = "경험치 30 획득" }
            ),
            
            // 최초 전투 패배
            CreateAchievement(
                "battle_first_defeat",
                "첫 좌절",
                "첫 전투에서 쓰라린 패배를 겪었습니다.",
                AchievementType.Battle,
                AchievementCategory.General,
                new AchievementCondition
                {
                    conditionType = ConditionType.BattleResult,
                    requiredBattleOutcome = GameOutcome.Defeat
                },
                new AchievementReward { rewardType = RewardType.Experience, rewardValue = 20, rewardDescription = "경험치 20 획득" }
            )
        };
    }
    
    /// <summary>
    /// 메인 스토리 업적들 (4개) - 스프레드시트 기준
    /// </summary>
    private static List<AchievementData> GetMainStoryAchievements()
    {
        return new List<AchievementData>
        {
            // 크리스마스 휴전 이벤트 성공
            CreateAchievement(
                "mainstory_christmas_truce",
                "성탄의 기적",
                "크리스마스 휴전 스토리를 성공시켰습니다.",
                AchievementType.MainStory,
                AchievementCategory.Special,
                new AchievementCondition
                {
                    conditionType = ConditionType.EventCompleted,
                    requiredEventId = 304, // 3회차 4장의 크리스마스 휴전 이벤트
                    requireEventSuccess = true
                },
                new AchievementReward { rewardType = RewardType.Title, rewardDescription = "성탄의 기적 칭호 획득" }
            ),
            
            // 운명의 갈림길 이벤트
            CreateAchievement(
                "mainstory_fateful_crossroads",
                "운명의 갈림길",
                "새로운 선택의 갈림길에 도달했습니다.",
                AchievementType.MainStory,
                AchievementCategory.Special,
                new AchievementCondition
                {
                    conditionType = ConditionType.EventCompleted,
                    requiredEventId = 0, // 실제 이벤트 ID로 변경 필요
                    requireEventSuccess = true
                },
                new AchievementReward { rewardType = RewardType.Title, rewardDescription = "운명의 갈림길 칭호 획득" }
            ),
            
            // 희망의 빛 이벤트
            CreateAchievement(
                "mainstory_light_of_hope",
                "희망의 빛",
                "어둠 속에서 희망의 빛을 발견했습니다.",
                AchievementType.MainStory,
                AchievementCategory.Special,
                new AchievementCondition
                {
                    conditionType = ConditionType.EventCompleted,
                    requiredEventId = 0, // 실제 이벤트 ID로 변경 필요
                    requireEventSuccess = true
                },
                new AchievementReward { rewardType = RewardType.Title, rewardDescription = "희망의 빛 칭호 획득" }
            ),
            
            // 역사의 한 장면 이벤트
            CreateAchievement(
                "mainstory_historic_moment",
                "역사의 한 장면",
                "역사에 남을 순간에 도달했습니다.",
                AchievementType.MainStory,
                AchievementCategory.Special,
                new AchievementCondition
                {
                    conditionType = ConditionType.EventCompleted,
                    requiredEventId = 0, // 실제 이벤트 ID로 변경 필요
                    requireEventSuccess = true
                },
                new AchievementReward { rewardType = RewardType.Title, rewardDescription = "역사의 한 장면 칭호 획득" }
            )
        };
    }
    
    /// <summary>
    /// 서브 스토리 업적들 (4개) - 스프레드시트 기준
    /// </summary>
    private static List<AchievementData> GetSubStoryAchievements()
    {
        return new List<AchievementData>
        {
            // 숨겨진 이야기 이벤트
            CreateAchievement(
                "substory_hidden_tale",
                "숨겨진 이야기",
                "알려지지 않은 이야기를 발견했습니다.",
                AchievementType.SubStory,
                AchievementCategory.General,
                new AchievementCondition
                {
                    conditionType = ConditionType.EventCompleted,
                    requiredEventId = 0, // 실제 이벤트 ID로 변경 필요
                    requireEventSuccess = true
                },
                new AchievementReward { rewardType = RewardType.Experience, rewardValue = 100, rewardDescription = "경험치 100 획득" }
            ),
            
            // 스쳐간 인연 이벤트
            CreateAchievement(
                "substory_passing_connection",
                "스쳐간 인연",
                "스쳐가는 인연의 순간을 만났습니다.",
                AchievementType.SubStory,
                AchievementCategory.General,
                new AchievementCondition
                {
                    conditionType = ConditionType.EventCompleted,
                    requiredEventId = 0, // 실제 이벤트 ID로 변경 필요
                    requireEventSuccess = true
                },
                new AchievementReward { rewardType = RewardType.Experience, rewardValue = 100, rewardDescription = "경험치 100 획득" }
            ),
            
            // 작은 기적 이벤트
            CreateAchievement(
                "substory_small_miracle",
                "작은 기적",
                "작은 기적 같은 사건을 경험했습니다.",
                AchievementType.SubStory,
                AchievementCategory.General,
                new AchievementCondition
                {
                    conditionType = ConditionType.EventCompleted,
                    requiredEventId = 0, // 실제 이벤트 ID로 변경 필요
                    requireEventSuccess = true
                },
                new AchievementReward { rewardType = RewardType.Experience, rewardValue = 100, rewardDescription = "경험치 100 획득" }
            ),
            
            // 새로운 색채 이벤트
            CreateAchievement(
                "substory_new_color",
                "새로운 색채",
                "잊혀진 기억의 조각을 발견했습니다.",
                AchievementType.SubStory,
                AchievementCategory.General,
                new AchievementCondition
                {
                    conditionType = ConditionType.EventCompleted,
                    requiredEventId = 0, // 실제 이벤트 ID로 변경 필요
                    requireEventSuccess = true
                },
                new AchievementReward { rewardType = RewardType.Experience, rewardValue = 100, rewardDescription = "경험치 100 획득" }
            )
        };
    }
    
    /// <summary>
    /// 업적 생성 헬퍼 메서드
    /// </summary>
    private static AchievementData CreateAchievement(
        string id, string title, string description, 
        AchievementType type, AchievementCategory category,
        AchievementCondition condition, AchievementReward reward)
    {
        return new AchievementData
        {
            achievementId = id,
            title = title,
            description = description,
            type = type,
            category = category,
            condition = condition,
            reward = reward,
            isUnlocked = false,
            isCompleted = false,
            isRewardClaimed = false
        };
    }
}
