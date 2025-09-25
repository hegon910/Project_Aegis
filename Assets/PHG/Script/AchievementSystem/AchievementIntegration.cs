using UnityEngine;
using System.Collections;

/// <summary>
/// 기존 시스템과 업적 시스템을 연동하는 클래스
/// </summary>
public class AchievementIntegration : MonoBehaviour
{
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
        
        // 전투 결과 이벤트 구독 (WarTurnManager와 연동)
        // 실제 구현에서는 전투 시스템에서 이벤트를 발생시키도록 수정 필요
        
        // 이벤트 완료 이벤트 구독 (EventManager와 연동)
        // 실제 구현에서는 EventManager에서 이벤트 완료 시 호출
        
        // 회차 시작 이벤트 구독 (GameManager와 연동)
        // 실제 구현에서는 GameManager에서 새 게임 시작 시 호출
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
}

