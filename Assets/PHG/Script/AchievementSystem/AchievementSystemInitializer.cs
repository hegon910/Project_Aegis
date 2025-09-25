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
        // 1) NotificationManager를 가장 먼저 생성하여 업적 이벤트 구독 누락 방지
        if (FindObjectOfType<AchievementNotificationManager>() == null)
        {
            var notificationObject = new GameObject("AchievementNotificationManager");
            notificationObject.AddComponent<AchievementNotificationManager>();
            DontDestroyOnLoad(notificationObject);

            if (showInitializationLog)
            {
                Debug.Log("[AchievementSystemInitializer] AchievementNotificationManager가 자동으로 생성되었습니다.");
            }
        }

        // 2) 그 다음 AchievementManager 생성
        if (AchievementManager.Instance == null)
        {
            var managerObject = new GameObject("AchievementManager");
            managerObject.AddComponent<AchievementManager>();

            if (showInitializationLog)
            {
                Debug.Log("[AchievementSystemInitializer] AchievementManager가 자동으로 생성되었습니다.");
            }
        }

        // 3) 마지막으로 Integration 생성
        if (FindObjectOfType<AchievementIntegration>() == null)
        {
            var integrationObject = new GameObject("AchievementIntegration");
            integrationObject.AddComponent<AchievementIntegration>();
            DontDestroyOnLoad(integrationObject);

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

    // 씬 로드 이후 한 번만 실행되어 업적 시스템을 안전하게 보장합니다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        // 이미 초기화자가 존재하면 중복 생성하지 않음
        if (GameObject.FindObjectOfType<AchievementSystemInitializer>() != null)
        {
            return;
        }

        var bootstrap = new GameObject("AchievementSystemBootstrap");
        var initializer = bootstrap.AddComponent<AchievementSystemInitializer>();
        GameObject.DontDestroyOnLoad(bootstrap);

        // 가벼운 초기화 호출 (동기, 무거운 로딩 작업 없음)
        initializer.InitializeAchievementSystem();
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