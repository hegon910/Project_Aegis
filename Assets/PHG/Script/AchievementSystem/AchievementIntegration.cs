using UnityEngine;
using System.Collections;
using Cysharp.Threading.Tasks;

/// <summary>
/// 기존 시스템과 업적 시스템을 연동하는 클래스
/// </summary>
public class AchievementIntegration : MonoBehaviour
{
    private static bool _bootPlaythroughChecked;
    private async void Start()
    {
        // 부팅 직후 업적 체크는 제거 - 새게임 시작 시에만 체크하도록 변경
        await UniTask.Yield();
        if (_bootPlaythroughChecked) return;
        _bootPlaythroughChecked = true;

        // 게임 부팅 시에는 회차 시작 업적을 체크하지 않음
        // 새게임 시작 시에만 체크하도록 GameManager에서 처리
        Debug.Log("[AchievementIntegration] 부팅 완료 - 회차 시작 업적은 새게임 시작 시에만 체크됩니다.");
    }

    private void Awake()
    {
        // 이벤트 구독
        SubscribeToGameEvents();
    }
    
    private void OnDestroy()
    {
        // 이벤트 구독 해제
        UnsubscribeFromGameEvents();
    }
    
    /// <summary>
    /// 게임 이벤트 구독
    /// </summary>
    private void SubscribeToGameEvents()
    {
        // MultiEndingSystem 이벤트 구독
        MultiEndingSystem.OnLoopCompleted += OnLoopCompleted;
        MultiEndingSystem.OnNewLoopStarted += OnNewLoopStarted;
        
        Debug.Log("[AchievementIntegration] 게임 이벤트 구독이 완료되었습니다.");
    }
    
    /// <summary>
    /// 게임 이벤트 구독 해제
    /// </summary>
    private void UnsubscribeFromGameEvents()
    {
        // MultiEndingSystem 이벤트 구독 해제
        MultiEndingSystem.OnLoopCompleted -= OnLoopCompleted;
        MultiEndingSystem.OnNewLoopStarted -= OnNewLoopStarted;
    }
    
    /// <summary>
    /// 루프 완료 시 호출되는 메서드 (엔딩 완료)
    /// </summary>
    public void OnLoopCompleted(int playthrough)
    {
        if (AchievementManager.Instance != null && MultiEndingSystem.Instance != null)
        {
            // MultiEndingSystem에서 엔딩 정보를 가져와서 업적 체크
            var endingType = MultiEndingSystem.Instance.DetermineEndingType();
            var endingRoute = MultiEndingSystem.Instance.DetermineEndingRoute();
            var endingBranch = MultiEndingSystem.Instance.DetermineEndingBranch(endingRoute);
            
            AchievementManager.Instance.CheckEndingAchievements(endingType, endingRoute, endingBranch, playthrough);
        }
    }
    
    /// <summary>
    /// 새 루프 시작 시 호출되는 메서드
    /// </summary>
    public void OnNewLoopStarted()
    {
        if (AchievementManager.Instance != null && MultiEndingSystem.Instance != null)
        {
            var status = MultiEndingSystem.Instance.GetCurrentLoopStatus();
            AchievementManager.Instance.CheckPlaythroughAchievements(status.currentPlaythrough);
        }
    }

    /// <summary>
    /// 부팅 직후 상태가 실제로 회차 시작 상태(챕터 1, 플레이리스트 시작)라면 회차 시작 업적을 체크합니다.
    /// 재개(이어하기) 상황에서의 오검 출력을 방지하기 위해 초깃값 조건을 함께 확인합니다.
    /// </summary>
    // 부팅 시 임시 체크 로직 제거(원래 위치로 복귀)
    
    /// <summary>
    /// 전투 결과 시 호출되는 메서드
    /// </summary>
    public void OnBattleResult(GameOutcome battleOutcome, bool isFirstBattle = false)
    {
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.CheckBattleAchievements(battleOutcome, isFirstBattle);
        }
    }
    
    /// <summary>
    /// 이벤트 완료 시 호출되는 메서드
    /// </summary>
    public void OnEventCompleted(int eventId, bool wasSuccess)
    {
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.CheckEventAchievements(eventId, wasSuccess);
        }
    }
    
    /// <summary>
    /// 파라미터 변경 시 호출되는 메서드
    /// </summary>
    public void OnParameterChanged(ParameterType parameterType, int newValue)
    {
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.CheckParameterAchievements(parameterType, newValue);
        }
    }
    
    /// <summary>
    /// 테스트용 업적 완료 (디버그용)
    /// </summary>
    [ContextMenu("Test Achievement Completion")]
    public void TestAchievementCompletion()
    {
        if (AchievementManager.Instance != null)
        {
            // 첫 번째 업적을 테스트 완료
            var achievements = AchievementManager.Instance.GetAllAchievements();
            if (achievements.Count > 0)
            {
                AchievementManager.Instance.CompleteAchievement(achievements[0].achievementId);
                Debug.Log($"[AchievementIntegration] 테스트 업적 완료: {achievements[0].title}");
            }
        }
    }
}

