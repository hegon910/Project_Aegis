using UnityEngine;

/// <summary>
/// 업적 시스템을 자동으로 초기화하는 클래스
/// 씬에 이 컴포넌트만 추가하면 모든 업적이 자동으로 로드됩니다
/// </summary>
public class AchievementSystemInitializer : MonoBehaviour
{
    [Header("자동 초기화 설정")]
    [SerializeField] private bool initializeOnAwake = true;
    [SerializeField] private bool showInitializationLog = true;

    private void Awake()
    {
        if (initializeOnAwake)
        {
            InitializeAchievementSystem();
        }
    }

    /// <summary>
    /// 업적 시스템 초기화
    /// </summary>
    public void InitializeAchievementSystem()
    {
        // AchievementManager가 없으면 생성
        if (AchievementManager.Instance == null)
        {
            var managerObject = new GameObject("AchievementManager");
            managerObject.AddComponent<AchievementManager>();

            if (showInitializationLog)
            {
                Debug.Log("[AchievementSystemInitializer] AchievementManager가 자동으로 생성되었습니다.");
            }
        }

        // AchievementIntegration이 없으면 생성
        if (FindObjectOfType<AchievementIntegration>() == null)
        {
            var integrationObject = new GameObject("AchievementIntegration");
            integrationObject.AddComponent<AchievementIntegration>();

            if (showInitializationLog)
            {
                Debug.Log("[AchievementSystemInitializer] AchievementIntegration이 자동으로 생성되었습니다.");
            }
        }

        if (showInitializationLog)
        {
            Debug.Log("[AchievementSystemInitializer] 업적 시스템 초기화가 완료되었습니다.");
            Debug.Log($"[AchievementSystemInitializer] 총 {AchievementDatabase.GetAllAchievements().Count}개의 업적이 로드되었습니다.");
        }
    }

    /// <summary>
    /// 테스트용 업적 완료
    /// </summary>
    [ContextMenu("Test Complete All Achievements")]
    public void TestCompleteAllAchievements()
    {
        if (AchievementManager.Instance != null)
        {
            var achievements = AchievementManager.Instance.GetAllAchievements();
            foreach (var achievement in achievements)
            {
                AchievementManager.Instance.CompleteAchievement(achievement.achievementId);
            }
            Debug.Log($"[AchievementSystemInitializer] {achievements.Count}개의 업적을 테스트 완료 처리했습니다.");
        }
    }
}