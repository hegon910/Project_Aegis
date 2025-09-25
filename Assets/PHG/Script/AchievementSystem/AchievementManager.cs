using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// 업적 시스템을 관리하는 매니저 클래스
/// </summary>
public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }
    
    [Header("업적 데이터")]
    [SerializeField] private List<AchievementData> allAchievements = new List<AchievementData>();
    
    [Header("업적 설정")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool autoSaveOnUpdate = true;
    
    // 업적 상태 저장
    private Dictionary<string, AchievementData> achievementDict = new Dictionary<string, AchievementData>();
    private List<string> completedAchievementIds = new List<string>();
    private List<string> claimedRewardIds = new List<string>();
    
    // 이벤트
    public static event Action<AchievementData> OnAchievementUnlocked;
    public static event Action<AchievementData> OnAchievementCompleted;
    public static event Action<AchievementData> OnRewardClaimed;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAchievements();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        LoadAchievementProgress();
    }
    
    /// <summary>
    /// 업적 데이터 초기화
    /// </summary>
    private void InitializeAchievements()
    {
        achievementDict.Clear();
        allAchievements.Clear();
        
        // AchievementDatabase에서 모든 업적 로드
        allAchievements.AddRange(AchievementDatabase.GetAllAchievements());
        
        foreach (var achievement in allAchievements)
        {
            if (!string.IsNullOrEmpty(achievement.achievementId))
            {
                achievementDict[achievement.achievementId] = achievement;
            }
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[AchievementManager] {achievementDict.Count}개의 업적이 로드되었습니다.");
            LogAchievementSummary();
        }
    }
    
    /// <summary>
    /// 업적 요약 정보 출력
    /// </summary>
    private void LogAchievementSummary()
    {
        var endingCount = allAchievements.Count(a => a.type == AchievementType.Ending);
        var playthroughCount = allAchievements.Count(a => a.type == AchievementType.Playthrough);
        var battleCount = allAchievements.Count(a => a.type == AchievementType.Battle);
        var eventCount = allAchievements.Count(a => a.type == AchievementType.Event);
        
        Debug.Log($"[AchievementManager] 업적 요약:");
        Debug.Log($"  - 엔딩 업적: {endingCount}개 (일반 9개, 진 2개, 히든 1개)");
        Debug.Log($"  - 회차 업적: {playthroughCount}개 (1-3회차 각각 4개씩)");
        Debug.Log($"  - 전투 업적: {battleCount}개 (첫 전투 3가지 결과)");
        Debug.Log($"  - 이벤트 업적: {eventCount}개 (특정 이벤트 완료)");
    }
    
    /// <summary>
    /// 업적 진행도 로드
    /// </summary>
    private void LoadAchievementProgress()
    {
        if (DataManager.Instance?.PlayerData == null) return;
        
        var playerData = DataManager.Instance.PlayerData;
        
        // 완료된 업적 로드
        completedAchievementIds.Clear();
        if (playerData.unlockedAchievements != null)
        {
            completedAchievementIds.AddRange(playerData.unlockedAchievements);
        }
        
        // 업적 상태 업데이트
        foreach (var achievementId in completedAchievementIds)
        {
            if (achievementDict.ContainsKey(achievementId))
            {
                var achievement = achievementDict[achievementId];
                achievement.isUnlocked = true;
                achievement.isCompleted = true;
                achievement.unlockedDate = DateTime.Now; // 실제로는 저장된 날짜를 로드해야 함
                achievement.completedDate = DateTime.Now;
            }
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[AchievementManager] {completedAchievementIds.Count}개의 완료된 업적을 로드했습니다.");
        }
    }
    
    /// <summary>
    /// 업적 진행도 저장
    /// </summary>
    private void SaveAchievementProgress()
    {
        if (DataManager.Instance?.PlayerData == null) return;
        
        var playerData = DataManager.Instance.PlayerData;
        playerData.unlockedAchievements = new List<string>(completedAchievementIds);
        
        if (autoSaveOnUpdate)
        {
            DataManager.Instance.SaveLocal();
        }
    }
    
    /// <summary>
    /// 엔딩 완료 시 업적 체크
    /// </summary>
    public void CheckEndingAchievements(EndingData endingData)
    {
        if (endingData == null) return;
        
        foreach (var achievement in achievementDict.Values)
        {
            if (achievement.isCompleted) continue;
            
            if (achievement.condition.conditionType == ConditionType.EndingCompleted)
            {
                bool isMatch = true;
                
                // 엔딩 타입 체크
                if (achievement.condition.requiredEndingType != EndingType.General)
                {
                    isMatch &= endingData.endingType == achievement.condition.requiredEndingType;
                }
                
                // 엔딩 루트 체크
                if (achievement.condition.requiredEndingRoute != EndingRoute.Victory)
                {
                    isMatch &= endingData.route == achievement.condition.requiredEndingRoute;
                }
                
                // 엔딩 분기 체크
                if (achievement.condition.requiredEndingBranch > 0)
                {
                    isMatch &= endingData.branch == achievement.condition.requiredEndingBranch;
                }
                
                if (isMatch)
                {
                    CompleteAchievement(achievement.achievementId);
                }
            }
        }
    }
    
    /// <summary>
    /// 회차 시작 시 업적 체크
    /// </summary>
    public void CheckPlaythroughAchievements(int playthroughCount)
    {
        foreach (var achievement in achievementDict.Values)
        {
            if (achievement.isCompleted) continue;
            
            if (achievement.condition.conditionType == ConditionType.PlaythroughStarted)
            {
                if (achievement.condition.requiredPlaythroughCount == playthroughCount)
                {
                    CompleteAchievement(achievement.achievementId);
                }
            }
        }
    }
    
    /// <summary>
    /// 전투 결과 업적 체크
    /// </summary>
    public void CheckBattleAchievements(GameOutcome battleOutcome, bool isFirstBattle = false)
    {
        foreach (var achievement in achievementDict.Values)
        {
            if (achievement.isCompleted) continue;
            
            if (achievement.condition.conditionType == ConditionType.BattleResult)
            {
                bool isMatch = achievement.condition.requiredBattleOutcome == battleOutcome;
                
                // 첫 전투 조건 체크
                if (isFirstBattle && achievement.achievementId.Contains("FirstBattle"))
                {
                    if (isMatch)
                    {
                        CompleteAchievement(achievement.achievementId);
                    }
                }
                else if (!isFirstBattle && !achievement.achievementId.Contains("FirstBattle"))
                {
                    if (isMatch)
                    {
                        CompleteAchievement(achievement.achievementId);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 이벤트 완료 업적 체크
    /// </summary>
    public void CheckEventAchievements(int eventId, bool wasSuccess)
    {
        foreach (var achievement in achievementDict.Values)
        {
            if (achievement.isCompleted) continue;
            
            if (achievement.condition.conditionType == ConditionType.EventCompleted)
            {
                bool isMatch = true;
                
                // 특정 이벤트 ID 체크
                if (achievement.condition.requiredEventId > 0)
                {
                    isMatch &= eventId == achievement.condition.requiredEventId;
                }
                
                // 성공/실패 조건 체크
                if (achievement.condition.requireEventSuccess)
                {
                    isMatch &= wasSuccess;
                }
                
                if (isMatch)
                {
                    CompleteAchievement(achievement.achievementId);
                }
            }
        }
    }
    
    /// <summary>
    /// 파라미터 도달 업적 체크
    /// </summary>
    public void CheckParameterAchievements(ParameterType parameterType, int currentValue)
    {
        foreach (var achievement in achievementDict.Values)
        {
            if (achievement.isCompleted) continue;
            
            if (achievement.condition.conditionType == ConditionType.ParameterReached)
            {
                if (achievement.condition.requiredParameter == parameterType &&
                    currentValue >= achievement.condition.requiredParameterValue)
                {
                    CompleteAchievement(achievement.achievementId);
                }
            }
        }
    }
    
    /// <summary>
    /// 업적 완료 처리
    /// </summary>
    public void CompleteAchievement(string achievementId)
    {
        if (!achievementDict.ContainsKey(achievementId))
        {
            Debug.LogWarning($"[AchievementManager] 존재하지 않는 업적 ID: {achievementId}");
            return;
        }
        
        var achievement = achievementDict[achievementId];
        
        if (achievement.isCompleted)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[AchievementManager] 업적 '{achievement.title}'은 이미 완료되었습니다.");
            }
            return;
        }
        
        // 업적 해금 및 완료 처리
        achievement.isUnlocked = true;
        achievement.isCompleted = true;
        achievement.unlockedDate = DateTime.Now;
        achievement.completedDate = DateTime.Now;
        
        // 완료 목록에 추가
        if (!completedAchievementIds.Contains(achievementId))
        {
            completedAchievementIds.Add(achievementId);
        }
        
        // 이벤트 발생
        OnAchievementUnlocked?.Invoke(achievement);
        OnAchievementCompleted?.Invoke(achievement);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[AchievementManager] 업적 완료: '{achievement.title}' - {achievement.description}");
        }
        
        // 저장
        SaveAchievementProgress();
    }
    
    /// <summary>
    /// 보상 수령 처리
    /// </summary>
    public bool ClaimReward(string achievementId)
    {
        if (!achievementDict.ContainsKey(achievementId))
        {
            Debug.LogWarning($"[AchievementManager] 존재하지 않는 업적 ID: {achievementId}");
            return false;
        }
        
        var achievement = achievementDict[achievementId];
        
        if (!achievement.isCompleted)
        {
            Debug.LogWarning($"[AchievementManager] 완료되지 않은 업적의 보상을 수령할 수 없습니다: {achievement.title}");
            return false;
        }
        
        if (achievement.isRewardClaimed)
        {
            Debug.LogWarning($"[AchievementManager] 이미 보상을 수령한 업적입니다: {achievement.title}");
            return false;
        }
        
        // 보상 수령 처리
        achievement.isRewardClaimed = true;
        
        // 보상 적용 (실제 구현 필요)
        ApplyReward(achievement.reward);
        
        // 이벤트 발생
        OnRewardClaimed?.Invoke(achievement);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[AchievementManager] 보상 수령: '{achievement.title}' - {achievement.reward.rewardDescription}");
        }
        
        // 저장
        SaveAchievementProgress();
        
        return true;
    }
    
    /// <summary>
    /// 보상 적용
    /// </summary>
    private void ApplyReward(AchievementReward reward)
    {
        switch (reward.rewardType)
        {
            case RewardType.Currency:
                // 화폐 보상 적용
                Debug.Log($"[AchievementManager] 화폐 보상: {reward.rewardValue}");
                break;
            case RewardType.Item:
                // 아이템 보상 적용
                Debug.Log($"[AchievementManager] 아이템 보상: {reward.rewardDescription}");
                break;
            case RewardType.Title:
                // 칭호 보상 적용
                Debug.Log($"[AchievementManager] 칭호 보상: {reward.rewardDescription}");
                break;
            case RewardType.Unlock:
                // 해금 보상 적용
                Debug.Log($"[AchievementManager] 해금 보상: {reward.rewardDescription}");
                break;
            case RewardType.Experience:
                // 경험치 보상 적용
                Debug.Log($"[AchievementManager] 경험치 보상: {reward.rewardValue}");
                break;
        }
    }
    
    /// <summary>
    /// 특정 업적 정보 가져오기
    /// </summary>
    public AchievementData GetAchievement(string achievementId)
    {
        return achievementDict.ContainsKey(achievementId) ? achievementDict[achievementId] : null;
    }
    
    /// <summary>
    /// 모든 업적 목록 가져오기
    /// </summary>
    public List<AchievementData> GetAllAchievements()
    {
        return achievementDict.Values.ToList();
    }
    
    /// <summary>
    /// 카테고리별 업적 목록 가져오기
    /// </summary>
    public List<AchievementData> GetAchievementsByCategory(AchievementCategory category)
    {
        return achievementDict.Values.Where(a => a.category == category).ToList();
    }
    
    /// <summary>
    /// 완료된 업적 목록 가져오기
    /// </summary>
    public List<AchievementData> GetCompletedAchievements()
    {
        return achievementDict.Values.Where(a => a.isCompleted).ToList();
    }
    
    /// <summary>
    /// 완료율 계산
    /// </summary>
    public float GetCompletionRate()
    {
        if (achievementDict.Count == 0) return 0f;
        return (float)completedAchievementIds.Count / achievementDict.Count * 100f;
    }
    
    /// <summary>
    /// 디버그 정보 출력
    /// </summary>
    [ContextMenu("Print Debug Info")]
    public void PrintDebugInfo()
    {
        Debug.Log("=== AchievementManager 디버그 정보 ===");
        Debug.Log($"전체 업적 수: {achievementDict.Count}");
        Debug.Log($"완료된 업적 수: {completedAchievementIds.Count}");
        Debug.Log($"완료율: {GetCompletionRate():F1}%");
        
        Debug.Log("\n완료된 업적 목록:");
        foreach (var achievementId in completedAchievementIds)
        {
            var achievement = achievementDict[achievementId];
            Debug.Log($"- {achievement.title} ({achievement.achievementId})");
        }
    }
}
