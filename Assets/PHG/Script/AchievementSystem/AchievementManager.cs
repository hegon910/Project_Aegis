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

	// 알림 누락 방지를 위한 해금 대기열 (구독자 준비 전 해금된 업적 보관)
	private readonly Queue<AchievementData> pendingUnlockedQueue = new Queue<AchievementData>();
    
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

			// 업적 알림 매니저가 씬에 없다면 자동 생성하여 구독/표시 누락을 방지
			EnsureNotificationManagerExists();
        }
        else
        {
            Destroy(gameObject);
        }
    }

	/// <summary>
	/// 업적 알림 매니저 자동 보장 (씬에 없으면 생성)
	/// </summary>
	private void EnsureNotificationManagerExists()
	{
		if (AchievementNotificationManager.Instance == null)
		{
			var go = new GameObject("AchievementNotificationManager_AutoSpawn");
			go.AddComponent<AchievementNotificationManager>();
			DontDestroyOnLoad(go);
			Debug.Log("[AchievementManager] AchievementNotificationManager가 없어 자동 생성했습니다.");
		}
	}
    
    private void Start()
    {
        LoadAchievementProgress();
    }

	/// <summary>
	/// 구독자 등록 시점 이전에 해금된 업적 알림을 회수하여 전달하기 위한 API
	/// </summary>
	public List<AchievementData> DequeueAllPendingUnlocked()
	{
        var list = new List<AchievementData>(pendingUnlockedQueue);
		pendingUnlockedQueue.Clear();
        if (enableDebugLogs)
        {
            Debug.Log($"[AchievementManager] 대기열 비우기: {list.Count}개 전달");
        }
		return list;
	}

    /// <summary>
    /// 모든 업적 상태를 초기 상태로 리셋하고 저장합니다.
    /// 디버깅용 OnclickResetData에서만 호출되어야 합니다.
    /// </summary>
    public void ResetAllAchievements()
    {
        // 내부 상태 초기화
        foreach (var kvp in achievementDict)
        {
            var ach = kvp.Value;
            ach.isUnlocked = false;
            ach.isCompleted = false;
            ach.isRewardClaimed = false;
            ach.unlockedDate = default;
            ach.completedDate = default;
        }

        completedAchievementIds.Clear();
        claimedRewardIds.Clear();

        // 플레이어 설정 데이터도 초기화 (SettingsData 사용)
        if (DataManager.Instance?.PlayerSettings != null)
        {
            DataManager.Instance.PlayerSettings.unlockedAchievements = new List<string>();
            DataManager.Instance.PlayerSettings.claimedAchievementIds = new List<string>();
        }

        if (enableDebugLogs)
        {
            Debug.Log("[AchievementManager] 모든 업적이 초기화되었습니다. (디버깅용 전체 리셋)");
        }

        // 저장
        SaveAchievementProgress();
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
    /// 업적 진행도 로드 (SettingsData에서 로드하도록 변경)
    /// </summary>
    private void LoadAchievementProgress()
    {
        if (DataManager.Instance?.PlayerSettings == null) return;
        
        var playerSettings = DataManager.Instance.PlayerSettings;
        
        // [마이그레이션] 기존 GameData에 있던 업적 데이터를 SettingsData로 이동
        MigrateAchievementsFromGameData();
        
        // 완료된 업적 로드 (SettingsData에서)
        completedAchievementIds.Clear();
        if (playerSettings.unlockedAchievements != null)
        {
            completedAchievementIds.AddRange(playerSettings.unlockedAchievements);
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

        // [보강] 수령 상태 로드: SettingsData.claimedAchievementIds를 기준으로 반영
        if (playerSettings.claimedAchievementIds == null)
        {
            playerSettings.claimedAchievementIds = new List<string>();
        }
        foreach (var kvp in achievementDict)
        {
            var ach = kvp.Value;
            ach.isRewardClaimed = playerSettings.claimedAchievementIds.Contains(ach.achievementId);
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[AchievementManager] {completedAchievementIds.Count}개의 완료된 업적을 로드했습니다.");
        }
    }
    
    /// <summary>
    /// 기존 GameData에 있던 업적 데이터를 SettingsData로 마이그레이션 (호환성 유지)
    /// </summary>
    private void MigrateAchievementsFromGameData()
    {
        if (DataManager.Instance?.PlayerData == null || DataManager.Instance?.PlayerSettings == null)
            return;
        
        var playerData = DataManager.Instance.PlayerData;
        var playerSettings = DataManager.Instance.PlayerSettings;
        
        // GameData에 업적 데이터가 있고, SettingsData에는 없는 경우 마이그레이션
        bool needsMigration = false;
        
        if (playerData.unlockedAchievements != null && playerData.unlockedAchievements.Count > 0)
        {
            if (playerSettings.unlockedAchievements == null || playerSettings.unlockedAchievements.Count == 0)
            {
                playerSettings.unlockedAchievements = new List<string>(playerData.unlockedAchievements);
                needsMigration = true;
                if (enableDebugLogs)
                {
                    Debug.Log($"[AchievementManager] GameData에서 {playerData.unlockedAchievements.Count}개의 업적을 SettingsData로 마이그레이션했습니다.");
                }
            }
        }
        
        if (playerData.claimedAchievementIds != null && playerData.claimedAchievementIds.Count > 0)
        {
            if (playerSettings.claimedAchievementIds == null || playerSettings.claimedAchievementIds.Count == 0)
            {
                playerSettings.claimedAchievementIds = new List<string>(playerData.claimedAchievementIds);
                needsMigration = true;
                if (enableDebugLogs)
                {
                    Debug.Log($"[AchievementManager] GameData에서 {playerData.claimedAchievementIds.Count}개의 수령 정보를 SettingsData로 마이그레이션했습니다.");
                }
            }
        }
        
        // 마이그레이션이 일어났다면 SettingsData 저장
        if (needsMigration)
        {
            DataManager.Instance.SaveSettings();
        }
    }
    
    /// <summary>
    /// 업적 진행도 저장 (SettingsData에 저장하도록 변경)
    /// </summary>
    private void SaveAchievementProgress()
    {
        if (DataManager.Instance?.PlayerSettings == null) return;
        
        var playerSettings = DataManager.Instance.PlayerSettings;
        playerSettings.unlockedAchievements = new List<string>(completedAchievementIds);

        // [보강] 수령 상태 저장: SettingsData.claimedAchievementIds에 동기화
        playerSettings.claimedAchievementIds = achievementDict.Values
            .Where(a => a.isRewardClaimed)
            .Select(a => a.achievementId)
            .ToList();
        
        if (autoSaveOnUpdate)
        {
            // 업적은 SettingsData에 저장되므로 SaveSettings 호출
            if (DataManager.Instance != null)
            {
                DataManager.Instance.SaveSettings();
            }
        }
    }
    
    /// <summary>
    /// 엔딩 완료 시 업적 체크 (수정된 메서드)
    /// </summary>
    public void CheckEndingAchievements(EndingType endingType, EndingRoute endingRoute, int endingBranch, int playthrough)
    {
        foreach (var achievement in achievementDict.Values)
        {
            if (achievement.isCompleted) continue;
            
            if (achievement.condition.conditionType == ConditionType.EndingCompleted)
            {
                bool isMatch = true;
                
                // 엔딩 타입 체크
                if (achievement.condition.requiredEndingType != EndingType.General)
                {
                    isMatch &= endingType == achievement.condition.requiredEndingType;
                }
                
                // 엔딩 루트 체크
                if (achievement.condition.requiredEndingRoute != EndingRoute.Victory)
                {
                    isMatch &= endingRoute == achievement.condition.requiredEndingRoute;
                }
                
                // 엔딩 분기 체크
                if (achievement.condition.requiredEndingBranch > 0)
                {
                    isMatch &= endingBranch == achievement.condition.requiredEndingBranch;
                }
                
                // 회차 체크 (필요한 경우)
                if (achievement.condition.requiredPlaythroughCount > 0)
                {
                    isMatch &= playthrough == achievement.condition.requiredPlaythroughCount;
                }
                
                // 파라미터 조건 체크 (카르마 등)
                if (achievement.condition.requiredParameter != ParameterType.None && 
                    achievement.condition.requiredParameterValue > 0)
                {
                    if (DataManager.Instance?.PlayerData != null)
                    {
                        int currentValue = GetParameterValue(achievement.condition.requiredParameter);
                        isMatch &= currentValue >= achievement.condition.requiredParameterValue;
                    }
                }
                
                if (isMatch)
                {
                    CompleteAchievement(achievement.achievementId);
                }
            }
        }
    }
    
    /// <summary>
    /// 파라미터 값 가져오기
    /// </summary>
    private int GetParameterValue(ParameterType parameterType)
    {
        if (DataManager.Instance?.PlayerData == null) return 0;
        
        switch (parameterType)
        {
            case ParameterType.카르마:
                return DataManager.Instance.PlayerData.karma;
            case ParameterType.정치력:
                return DataManager.Instance.PlayerData.politics;
            case ParameterType.병력:
                return DataManager.Instance.PlayerData.militaryPower;
            case ParameterType.물자:
                return DataManager.Instance.PlayerData.supplies;
            case ParameterType.리더십:
                return DataManager.Instance.PlayerData.leadership;
            case ParameterType.전황:
                return DataManager.Instance.PlayerData.warSituation;
            default:
                return 0;
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

				// 문자열 기반의 'FirstBattle' 이름 의존을 제거하고 결과 일치만으로 해금
				if (isMatch)
				{
					CompleteAchievement(achievement.achievementId);
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
        
        // 이벤트 구독자 준비 전 누락 방지를 위해 대기열에 적재
        pendingUnlockedQueue.Enqueue(achievement);
        if (enableDebugLogs)
        {
            Debug.Log($"[AchievementManager] 해금 대기열 적재: {achievement.achievementId} (대기열={pendingUnlockedQueue.Count})");
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
        
        // 저장: 재시작 후 재수령 방지를 위해 즉시 저장
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
                // 재화 보상 적용
                CurrencyManager.AddCurrency(reward.rewardValue);
                Debug.Log($"[AchievementManager] 재화 보상: {reward.rewardValue}개 획득!");
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
                // 경험치 보상 적용 (재화로 처리)
                CurrencyManager.AddCurrency(reward.rewardValue);
                Debug.Log($"[AchievementManager] 경험치 보상: {reward.rewardValue}개 획득!");
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
