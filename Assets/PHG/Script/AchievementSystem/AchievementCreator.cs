using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 기획서에 따른 업적 데이터를 생성하는 클래스
/// </summary>
[CreateAssetMenu(fileName = "AchievementDatabase", menuName = "Game/Achievement Database")]
public class AchievementCreator : ScriptableObject
{
    [Header("업적 데이터베이스")]
    [SerializeField] private List<AchievementData> achievementDatabase = new List<AchievementData>();
    
    /// <summary>
    /// 기획서에 따른 기본 업적들을 생성합니다
    /// </summary>
    [ContextMenu("Create Default Achievements")]
    public void CreateDefaultAchievements()
    {
        achievementDatabase.Clear();
        
        // === 개별 엔딩 달성 업적 ===
        CreateEndingAchievements();
        
        // === 각 회차 시작 업적 ===
        CreatePlaythroughAchievements();
        
        // === 전투 관련 업적 ===
        CreateBattleAchievements();
        
        // === 이벤트 관련 업적 ===
        CreateEventAchievements();
        
        Debug.Log($"[AchievementCreator] {achievementDatabase.Count}개의 기본 업적이 생성되었습니다.");
    }
    
    /// <summary>
    /// 엔딩 달성 업적 생성
    /// </summary>
    private void CreateEndingAchievements()
    {
        // 일반 엔딩 업적들
        AddAchievement(new AchievementData
        {
            achievementId = "ending_general_victory_high_karma",
            title = "명장",
            description = "누구보다 빛나는 승리를 거두었습니다.",
            type = AchievementType.Ending,
            category = AchievementCategory.General,
            condition = new AchievementCondition
            {
                conditionType = ConditionType.EndingCompleted,
                requiredEndingType = EndingType.General,
                requiredEndingRoute = EndingRoute.Victory,
                requiredParameter = ParameterType.카르마,
                requiredParameterValue = 80
            },
            reward = new AchievementReward
            {
                rewardType = RewardType.Title,
                rewardDescription = "명장 칭호 획득"
            }
        });
        
        AddAchievement(new AchievementData
        {
            achievementId = "ending_general_draw_normal_karma",
            title = "수호자",
            description = "모두를 지켜내며 무승부에 도달했습니다.",
            type = AchievementType.Ending,
            category = AchievementCategory.General,
            condition = new AchievementCondition
            {
                conditionType = ConditionType.EndingCompleted,
                requiredEndingType = EndingType.General,
                requiredEndingRoute = EndingRoute.Truce,
                requiredParameter = ParameterType.카르마,
                requiredParameterValue = 50
            },
            reward = new AchievementReward
            {
                rewardType = RewardType.Title,
                rewardDescription = "수호자 칭호 획득"
            }
        });
        
        AddAchievement(new AchievementData
        {
            achievementId = "ending_general_defeat_low_karma",
            title = "위선자",
            description = "스스로의 모순 끝에 패배를 맞이했습니다.",
            type = AchievementType.Ending,
            category = AchievementCategory.General,
            condition = new AchievementCondition
            {
                conditionType = ConditionType.EndingCompleted,
                requiredEndingType = EndingType.General,
                requiredEndingRoute = EndingRoute.Defeat,
                requiredParameter = ParameterType.카르마,
                requiredParameterValue = 20
            },
            reward = new AchievementReward
            {
                rewardType = RewardType.Title,
                rewardDescription = "위선자 칭호 획득"
            }
        });
        
        // 진엔딩 업적들
        AddAchievement(new AchievementData
        {
            achievementId = "ending_true_2nd_playthrough",
            title = "진실의 문",
            description = "2회차에서 진엔딩에 도달했습니다.",
            type = AchievementType.Ending,
            category = AchievementCategory.Special,
            condition = new AchievementCondition
            {
                conditionType = ConditionType.EndingCompleted,
                requiredEndingType = EndingType.True,
                requiredPlaythroughCount = 2
            },
            reward = new AchievementReward
            {
                rewardType = RewardType.Title,
                rewardDescription = "진실의 문 칭호 획득"
            }
        });
        
        AddAchievement(new AchievementData
        {
            achievementId = "ending_true_3rd_playthrough",
            title = "완전한 진실",
            description = "3회차에서 진엔딩에 도달했습니다.",
            type = AchievementType.Ending,
            category = AchievementCategory.Special,
            condition = new AchievementCondition
            {
                conditionType = ConditionType.EndingCompleted,
                requiredEndingType = EndingType.True,
                requiredPlaythroughCount = 3
            },
            reward = new AchievementReward
            {
                rewardType = RewardType.Title,
                rewardDescription = "완전한 진실 칭호 획득"
            }
        });
        
        // 히든 엔딩 업적
        AddAchievement(new AchievementData
        {
            achievementId = "ending_hidden_master",
            title = "마스터",
            description = "모든 조건을 충족하여 히든 엔딩에 도달했습니다.",
            type = AchievementType.Ending,
            category = AchievementCategory.Hidden,
            condition = new AchievementCondition
            {
                conditionType = ConditionType.EndingCompleted,
                requiredEndingType = EndingType.Hidden
            },
            reward = new AchievementReward
            {
                rewardType = RewardType.Title,
                rewardDescription = "마스터 칭호 획득"
            }
        });
    }
    
    /// <summary>
    /// 회차 시작 업적 생성
    /// </summary>
    private void CreatePlaythroughAchievements()
    {
        // 1회차 시작
        AddAchievement(new AchievementData
        {
            achievementId = "playthrough_1st_start",
            title = "첫 걸음",
            description = "1회차 게임을 시작했습니다.",
            type = AchievementType.Playthrough,
            category = AchievementCategory.General,
            condition = new AchievementCondition
            {
                conditionType = ConditionType.PlaythroughStarted,
                requiredPlaythroughCount = 1
            },
            reward = new AchievementReward
            {
                rewardType = RewardType.Experience,
                rewardValue = 100,
                rewardDescription = "경험치 100 획득"
            }
        });
        
        // 2회차 시작
        AddAchievement(new AchievementData
        {
            achievementId = "playthrough_2nd_start",
            title = "재도전",
            description = "2회차 게임을 시작했습니다.",
            type = AchievementType.Playthrough,
            category = AchievementCategory.General,
            condition = new AchievementCondition
            {
                conditionType = ConditionType.PlaythroughStarted,
                requiredPlaythroughCount = 2
            },
            reward = new AchievementReward
            {
                rewardType = RewardType.Experience,
                rewardValue = 200,
                rewardDescription = "경험치 200 획득"
            }
        });
        
        // 3회차 시작
        AddAchievement(new AchievementData
        {
            achievementId = "playthrough_3rd_start",
            title = "완벽주의자",
            description = "3회차 게임을 시작했습니다.",
            type = AchievementType.Playthrough,
            category = AchievementCategory.Special,
            condition = new AchievementCondition
            {
                conditionType = ConditionType.PlaythroughStarted,
                requiredPlaythroughCount = 3
            },
            reward = new AchievementReward
            {
                rewardType = RewardType.Experience,
                rewardValue = 500,
                rewardDescription = "경험치 500 획득"
            }
        });
    }
    
    /// <summary>
    /// 전투 관련 업적 생성
    /// </summary>
    private void CreateBattleAchievements()
    {
        // 첫 전투 승리
        AddAchievement(new AchievementData
        {
            achievementId = "battle_first_victory",
            title = "첫 승리",
            description = "첫 전투에서 승리했습니다.",
            type = AchievementType.Battle,
            category = AchievementCategory.General,
            condition = new AchievementCondition
            {
                conditionType = ConditionType.BattleResult,
                requiredBattleOutcome = GameOutcome.Victory
            },
            reward = new AchievementReward
            {
                rewardType = RewardType.Experience,
                rewardValue = 50,
                rewardDescription = "경험치 50 획득"
            }
        });
        
        // 첫 전투 무승부
        AddAchievement(new AchievementData
        {
            achievementId = "battle_first_draw",
            title = "균형감각",
            description = "첫 전투에서 무승부를 기록했습니다.",
            type = AchievementType.Battle,
            category = AchievementCategory.General,
            condition = new AchievementCondition
            {
                conditionType = ConditionType.BattleResult,
                requiredBattleOutcome = GameOutcome.Draw
            },
            reward = new AchievementReward
            {
                rewardType = RewardType.Experience,
                rewardValue = 30,
                rewardDescription = "경험치 30 획득"
            }
        });
        
        // 첫 전투 패배
        AddAchievement(new AchievementData
        {
            achievementId = "battle_first_defeat",
            title = "교훈",
            description = "첫 전투에서 패배했습니다.",
            type = AchievementType.Battle,
            category = AchievementCategory.General,
            condition = new AchievementCondition
            {
                conditionType = ConditionType.BattleResult,
                requiredBattleOutcome = GameOutcome.Defeat
            },
            reward = new AchievementReward
            {
                rewardType = RewardType.Experience,
                rewardValue = 20,
                rewardDescription = "경험치 20 획득"
            }
        });
    }
    
    /// <summary>
    /// 이벤트 관련 업적 생성
    /// </summary>
    private void CreateEventAchievements()
    {
        // 크리스마스 휴전 이벤트 성공
        AddAchievement(new AchievementData
        {
            achievementId = "event_christmas_truce_success",
            title = "평화의 사도",
            description = "크리스마스 휴전 이벤트를 성공적으로 완료했습니다.",
            type = AchievementType.Event,
            category = AchievementCategory.Special,
            condition = new AchievementCondition
            {
                conditionType = ConditionType.EventCompleted,
                requiredEventId = 304, // 실제 이벤트 ID로 변경 필요
                requireEventSuccess = true
            },
            reward = new AchievementReward
            {
                rewardType = RewardType.Title,
                rewardDescription = "평화의 사도 칭호 획득"
            }
        });
    }
    
    /// <summary>
    /// 업적 추가
    /// </summary>
    private void AddAchievement(AchievementData achievement)
    {
        achievement.isUnlocked = false;
        achievement.isCompleted = false;
        achievement.isRewardClaimed = false;
        
        achievementDatabase.Add(achievement);
    }
    
    /// <summary>
    /// 업적 데이터베이스 반환
    /// </summary>
    public List<AchievementData> GetAchievementDatabase()
    {
        return achievementDatabase;
    }
}



