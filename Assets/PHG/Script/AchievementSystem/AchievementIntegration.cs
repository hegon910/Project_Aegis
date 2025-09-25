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
        // 엔딩 완료 이벤트 구독 (MultiEndingSystem과 연동)
        if (MultiEndingSystem.Instance != null)
        {
            // MultiEndingSystem에서 엔딩 완료 시 호출될 메서드 연결
            // 실제 구현에서는 MultiEndingSystem에 이벤트를 추가하거나 직접 호출
        }
        
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
        // 이벤트 구독 해제 로직
    }
    
    /// <summary>
    /// 엔딩 완료 시 호출되는 메서드
    /// </summary>
    public void OnEndingCompleted(EndingData endingData)
    {
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.CheckEndingAchievements(endingData);
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
    /// 회차 시작 시 호출되는 메서드
    /// </summary>
    public void OnPlaythroughStarted(int playthroughCount)
    {
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.CheckPlaythroughAchievements(playthroughCount);
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

