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
        
        // === 개별 엔딩 달성 업적 ===
        achievements.AddRange(GetEndingAchievements());
        
        // === 각 회차 시작 업적 ===
        achievements.AddRange(GetPlaythroughAchievements());
        
        // === 전투 관련 업적 ===
        achievements.AddRange(GetBattleAchievements());
        
        // === 이벤트 관련 업적 ===
        achievements.AddRange(GetEventAchievements());
        
        return achievements;
    }
    
    /// <summary>
    /// 엔딩 달성 업적들 (기획서 기준: 일반 엔딩 9개, 진 엔딩 2개, 히든 엔딩 1개)
    /// </summary>
    private static List<AchievementData> GetEndingAchievements()
    {
        return new List<AchievementData>
        {
            // === 일반 엔딩 9개 ===
            // 승리 + 높은 카르마
            CreateAchievement(
                "ending_general_victory_high_karma",
                "명장",
                "누구보다 빛나는 승리를 거두었습니다.",
                AchievementType.Ending,
                AchievementCategory.General,
                new AchievementCondition
                {
                    conditionType = ConditionType.EndingCompleted,
                    requiredEndingType = EndingType.General,
                    requiredEndingRoute = EndingRoute.Victory,
                    requiredParameter = ParameterType.카르마,
                    requiredParameterValue = 80
                },
                new AchievementReward { rewardType = RewardType.Title, rewardDescription = "명장 칭호 획득" }
            ),
            
            // 무승부 + 보통 카르마
            CreateAchievement(
                "ending_general_draw_normal_karma",
                "수호자",
                "모두를 지켜내며 무승부에 도달했습니다.",
                AchievementType.Ending,
                AchievementCategory.General,
                new AchievementCondition
                {
                    conditionType = ConditionType.EndingCompleted,
                    requiredEndingType = EndingType.General,
                    requiredEndingRoute = EndingRoute.Truce,
                    requiredParameter = ParameterType.카르마,
                    requiredParameterValue = 50
                },
                new AchievementReward { rewardType = RewardType.Title, rewardDescription = "수호자 칭호 획득" }
            ),
            
            // 패배 + 낮은 카르마
            CreateAchievement(
                "ending_general_defeat_low_karma",
                "위선자",
                "스스로의 모순 끝에 패배를 맞이했습니다.",
                AchievementType.Ending,
                AchievementCategory.General,
                new AchievementCondition
                {
                    conditionType = ConditionType.EndingCompleted,
                    requiredEndingType = EndingType.General,
                    requiredEndingRoute = EndingRoute.Defeat,
                    requiredParameter = ParameterType.카르마,
                    requiredParameterValue = 20
                },
                new AchievementReward { rewardType = RewardType.Title, rewardDescription = "위선자 칭호 획득" }
            ),
            
            // 추가 일반 엔딩들 (승리 + 중간 카르마)
            CreateAchievement(
                "ending_general_victory_normal_karma",
                "전술가",
                "균형잡힌 전술로 승리를 거두었습니다.",
                AchievementType.Ending,
                AchievementCategory.General,
                new AchievementCondition
                {
                    conditionType = ConditionType.EndingCompleted,
                    requiredEndingType = EndingType.General,
                    requiredEndingRoute = EndingRoute.Victory,
                    requiredParameter = ParameterType.카르마,
                    requiredParameterValue = 60
                },
                new AchievementReward { rewardType = RewardType.Title, rewardDescription = "전술가 칭호 획득" }
            ),
            
            // 승리 + 낮은 카르마
            CreateAchievement(
                "ending_general_victory_low_karma",
                "냉혈한",
                "냉혹한 선택으로 승리를 거두었습니다.",
                AchievementType.Ending,
                AchievementCategory.General,
                new AchievementCondition
                {
                    conditionType = ConditionType.EndingCompleted,
                    requiredEndingType = EndingType.General,
                    requiredEndingRoute = EndingRoute.Victory,
                    requiredParameter = ParameterType.카르마,
                    requiredParameterValue = 30
                },
                new AchievementReward { rewardType = RewardType.Title, rewardDescription = "냉혈한 칭호 획득" }
            ),
            
            // 무승부 + 높은 카르마
            CreateAchievement(
                "ending_general_draw_high_karma",
                "평화주의자",
                "모든 이를 구하려는 마음으로 무승부에 도달했습니다.",
                AchievementType.Ending,
                AchievementCategory.General,
                new AchievementCondition
                {
                    conditionType = ConditionType.EndingCompleted,
                    requiredEndingType = EndingType.General,
                    requiredEndingRoute = EndingRoute.Truce,
                    requiredParameter = ParameterType.카르마,
                    requiredParameterValue = 80
                },
                new AchievementReward { rewardType = RewardType.Title, rewardDescription = "평화주의자 칭호 획득" }
            ),
            
            // 무승부 + 낮은 카르마
            CreateAchievement(
                "ending_general_draw_low_karma",
                "회피자",
                "결단을 피하며 무승부에 도달했습니다.",
                AchievementType.Ending,
                AchievementCategory.General,
                new AchievementCondition
                {
                    conditionType = ConditionType.EndingCompleted,
                    requiredEndingType = EndingType.General,
                    requiredEndingRoute = EndingRoute.Truce,
                    requiredParameter = ParameterType.카르마,
                    requiredParameterValue = 20
                },
                new AchievementReward { rewardType = RewardType.Title, rewardDescription = "회피자 칭호 획득" }
            ),
            
            // 패배 + 높은 카르마
            CreateAchievement(
                "ending_general_defeat_high_karma",
                "순교자",
                "도덕적 선택으로 인해 패배를 맞이했습니다.",
                AchievementType.Ending,
                AchievementCategory.General,
                new AchievementCondition
                {
                    conditionType = ConditionType.EndingCompleted,
                    requiredEndingType = EndingType.General,
                    requiredEndingRoute = EndingRoute.Defeat,
                    requiredParameter = ParameterType.카르마,
                    requiredParameterValue = 80
                },
                new AchievementReward { rewardType = RewardType.Title, rewardDescription = "순교자 칭호 획득" }
            ),
            
            // 패배 + 중간 카르마
            CreateAchievement(
                "ending_general_defeat_normal_karma",
                "실패자",
                "중도적 선택으로 인해 패배를 맞이했습니다.",
                AchievementType.Ending,
                AchievementCategory.General,
                new AchievementCondition
                {
                    conditionType = ConditionType.EndingCompleted,
                    requiredEndingType = EndingType.General,
                    requiredEndingRoute = EndingRoute.Defeat,
                    requiredParameter = ParameterType.카르마,
                    requiredParameterValue = 50
                },
                new AchievementReward { rewardType = RewardType.Title, rewardDescription = "실패자 칭호 획득" }
            ),
            
            // === 진 엔딩 2개 ===
            CreateAchievement(
                "ending_true_2nd_playthrough",
                "진실의 문",
                "2회차에서 진엔딩에 도달했습니다.",
                AchievementType.Ending,
                AchievementCategory.Special,
                new AchievementCondition
                {
                    conditionType = ConditionType.EndingCompleted,
                    requiredEndingType = EndingType.True,
                    requiredPlaythroughCount = 2
                },
                new AchievementReward { rewardType = RewardType.Title, rewardDescription = "진실의 문 칭호 획득" }
            ),
            
            CreateAchievement(
                "ending_true_3rd_playthrough",
                "완전한 진실",
                "3회차에서 진엔딩에 도달했습니다.",
                AchievementType.Ending,
                AchievementCategory.Special,
                new AchievementCondition
                {
                    conditionType = ConditionType.EndingCompleted,
                    requiredEndingType = EndingType.True,
                    requiredPlaythroughCount = 3
                },
                new AchievementReward { rewardType = RewardType.Title, rewardDescription = "완전한 진실 칭호 획득" }
            ),
            
            // === 히든 엔딩 1개 ===
            CreateAchievement(
                "ending_hidden_master",
                "마스터",
                "모든 조건을 충족하여 히든 엔딩에 도달했습니다.",
                AchievementType.Ending,
                AchievementCategory.Hidden,
                new AchievementCondition
                {
                    conditionType = ConditionType.EndingCompleted,
                    requiredEndingType = EndingType.Hidden
                },
                new AchievementReward { rewardType = RewardType.Title, rewardDescription = "마스터 칭호 획득" }
            )
        };
    }
    
    /// <summary>
    /// 회차 시작 업적들 (1회차, 2회차, 3회차 각각 승리/무승부/패배)
    /// </summary>
    private static List<AchievementData> GetPlaythroughAchievements()
    {
        return new List<AchievementData>
        {
            // === 1회차 업적들 ===
            CreateAchievement(
                "playthrough_1st_start",
                "첫 걸음",
                "1회차 게임을 시작했습니다.",
                AchievementType.Playthrough,
                AchievementCategory.General,
                new AchievementCondition
                {
                    conditionType = ConditionType.PlaythroughStarted,
                    requiredPlaythroughCount = 1
                },
                new AchievementReward { rewardType = RewardType.Experience, rewardValue = 100, rewardDescription = "경험치 100 획득" }
            ),
            
            CreateAchievement(
                "playthrough_1st_victory",
                "신예",
                "1회차에서 승리했습니다.",
                AchievementType.Playthrough,
                AchievementCategory.General,
                new AchievementCondition
                {
                    conditionType = ConditionType.EndingCompleted,
                    requiredPlaythroughCount = 1,
                    requiredEndingRoute = EndingRoute.Victory
                },
                new AchievementReward { rewardType = RewardType.Experience, rewardValue = 200, rewardDescription = "경험치 200 획득" }
            ),
            
            CreateAchievement(
                "playthrough_1st_draw",
                "균형감각",
                "1회차에서 무승부를 기록했습니다.",
                AchievementType.Playthrough,
                AchievementCategory.General,
                new AchievementCondition
                {
                    conditionType = ConditionType.EndingCompleted,
                    requiredPlaythroughCount = 1,
                    requiredEndingRoute = EndingRoute.Truce
                },
                new AchievementReward { rewardType = RewardType.Experience, rewardValue = 150, rewardDescription = "경험치 150 획득" }
            ),
            
            CreateAchievement(
                "playthrough_1st_defeat",
                "교훈",
                "1회차에서 패배했습니다.",
                AchievementType.Playthrough,
                AchievementCategory.General,
                new AchievementCondition
                {
                    conditionType = ConditionType.EndingCompleted,
                    requiredPlaythroughCount = 1,
                    requiredEndingRoute = EndingRoute.Defeat
                },
                new AchievementReward { rewardType = RewardType.Experience, rewardValue = 100, rewardDescription = "경험치 100 획득" }
            ),
            
            // === 2회차 업적들 ===
            CreateAchievement(
                "playthrough_2nd_start",
                "재도전",
                "2회차 게임을 시작했습니다.",
                AchievementType.Playthrough,
                AchievementCategory.General,
                new AchievementCondition
                {
                    conditionType = ConditionType.PlaythroughStarted,
                    requiredPlaythroughCount = 2
                },
                new AchievementReward { rewardType = RewardType.Experience, rewardValue = 200, rewardDescription = "경험치 200 획득" }
            ),
            
            CreateAchievement(
                "playthrough_2nd_victory",
                "성장",
                "2회차에서 승리했습니다.",
                AchievementType.Playthrough,
                AchievementCategory.General,
                new AchievementCondition
                {
                    conditionType = ConditionType.EndingCompleted,
                    requiredPlaythroughCount = 2,
                    requiredEndingRoute = EndingRoute.Victory
                },
                new AchievementReward { rewardType = RewardType.Experience, rewardValue = 300, rewardDescription = "경험치 300 획득" }
            ),
            
            CreateAchievement(
                "playthrough_2nd_draw",
                "조화",
                "2회차에서 무승부를 기록했습니다.",
                AchievementType.Playthrough,
                AchievementCategory.General,
                new AchievementCondition
                {
                    conditionType = ConditionType.EndingCompleted,
                    requiredPlaythroughCount = 2,
                    requiredEndingRoute = EndingRoute.Truce
                },
                new AchievementReward { rewardType = RewardType.Experience, rewardValue = 250, rewardDescription = "경험치 250 획득" }
            ),
            
            CreateAchievement(
                "playthrough_2nd_defeat",
                "시행착오",
                "2회차에서 패배했습니다.",
                AchievementType.Playthrough,
                AchievementCategory.General,
                new AchievementCondition
                {
                    conditionType = ConditionType.EndingCompleted,
                    requiredPlaythroughCount = 2,
                    requiredEndingRoute = EndingRoute.Defeat
                },
                new AchievementReward { rewardType = RewardType.Experience, rewardValue = 200, rewardDescription = "경험치 200 획득" }
            ),
            
            // === 3회차 업적들 ===
            CreateAchievement(
                "playthrough_3rd_start",
                "완벽주의자",
                "3회차 게임을 시작했습니다.",
                AchievementType.Playthrough,
                AchievementCategory.Special,
                new AchievementCondition
                {
                    conditionType = ConditionType.PlaythroughStarted,
                    requiredPlaythroughCount = 3
                },
                new AchievementReward { rewardType = RewardType.Experience, rewardValue = 500, rewardDescription = "경험치 500 획득" }
            ),
            
            CreateAchievement(
                "playthrough_3rd_victory",
                "완성",
                "3회차에서 승리했습니다.",
                AchievementType.Playthrough,
                AchievementCategory.Special,
                new AchievementCondition
                {
                    conditionType = ConditionType.EndingCompleted,
                    requiredPlaythroughCount = 3,
                    requiredEndingRoute = EndingRoute.Victory
                },
                new AchievementReward { rewardType = RewardType.Experience, rewardValue = 600, rewardDescription = "경험치 600 획득" }
            ),
            
            CreateAchievement(
                "playthrough_3rd_draw",
                "완벽한 균형",
                "3회차에서 무승부를 기록했습니다.",
                AchievementType.Playthrough,
                AchievementCategory.Special,
                new AchievementCondition
                {
                    conditionType = ConditionType.EndingCompleted,
                    requiredPlaythroughCount = 3,
                    requiredEndingRoute = EndingRoute.Truce
                },
                new AchievementReward { rewardType = RewardType.Experience, rewardValue = 550, rewardDescription = "경험치 550 획득" }
            ),
            
            CreateAchievement(
                "playthrough_3rd_defeat",
                "인내심",
                "3회차에서 패배했습니다.",
                AchievementType.Playthrough,
                AchievementCategory.Special,
                new AchievementCondition
                {
                    conditionType = ConditionType.EndingCompleted,
                    requiredPlaythroughCount = 3,
                    requiredEndingRoute = EndingRoute.Defeat
                },
                new AchievementReward { rewardType = RewardType.Experience, rewardValue = 500, rewardDescription = "경험치 500 획득" }
            )
        };
    }
    
    /// <summary>
    /// 전투 관련 업적들 (최초 전투 승리/무승부/패배)
    /// </summary>
    private static List<AchievementData> GetBattleAchievements()
    {
        return new List<AchievementData>
        {
            CreateAchievement(
                "battle_first_victory",
                "첫 승리",
                "첫 전투에서 승리했습니다.",
                AchievementType.Battle,
                AchievementCategory.General,
                new AchievementCondition
                {
                    conditionType = ConditionType.BattleResult,
                    requiredBattleOutcome = GameOutcome.Victory
                },
                new AchievementReward { rewardType = RewardType.Experience, rewardValue = 50, rewardDescription = "경험치 50 획득" }
            ),
            
            CreateAchievement(
                "battle_first_draw",
                "균형감각",
                "첫 전투에서 무승부를 기록했습니다.",
                AchievementType.Battle,
                AchievementCategory.General,
                new AchievementCondition
                {
                    conditionType = ConditionType.BattleResult,
                    requiredBattleOutcome = GameOutcome.Draw
                },
                new AchievementReward { rewardType = RewardType.Experience, rewardValue = 30, rewardDescription = "경험치 30 획득" }
            ),
            
            CreateAchievement(
                "battle_first_defeat",
                "교훈",
                "첫 전투에서 패배했습니다.",
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
    /// 이벤트 관련 업적들 (메인/서브 이벤트 스토리의 특정 플래그 달성)
    /// </summary>
    private static List<AchievementData> GetEventAchievements()
    {
        return new List<AchievementData>
        {
            // 크리스마스 휴전 이벤트 성공
            CreateAchievement(
                "event_christmas_truce_success",
                "평화의 사도",
                "크리스마스 휴전 이벤트를 성공적으로 완료했습니다.",
                AchievementType.Event,
                AchievementCategory.Special,
                new AchievementCondition
                {
                    conditionType = ConditionType.EventCompleted,
                    requiredEventId = 304, // 실제 이벤트 ID로 변경 필요
                    requireEventSuccess = true
                },
                new AchievementReward { rewardType = RewardType.Title, rewardDescription = "평화의 사도 칭호 획득" }
            ),
            
            // 추가 이벤트 업적들 (기획서에 따라 확장 가능)
            CreateAchievement(
                "event_diplomatic_success",
                "외교관",
                "외교 이벤트를 성공적으로 완료했습니다.",
                AchievementType.Event,
                AchievementCategory.General,
                new AchievementCondition
                {
                    conditionType = ConditionType.EventCompleted,
                    requiredEventId = 0, // 실제 이벤트 ID로 변경 필요
                    requireEventSuccess = true
                },
                new AchievementReward { rewardType = RewardType.Experience, rewardValue = 100, rewardDescription = "경험치 100 획득" }
            ),
            
            CreateAchievement(
                "event_military_success",
                "전략가",
                "군사 이벤트를 성공적으로 완료했습니다.",
                AchievementType.Event,
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
