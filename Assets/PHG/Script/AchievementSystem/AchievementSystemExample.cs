using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// 업적 시스템 사용 예시
/// 이 클래스는 실제 구현 시 기존 시스템에 통합되어야 합니다.
/// </summary>
public class AchievementSystemExample : MonoBehaviour
{
    [Header("업적 시스템 테스트")]
    [SerializeField] private bool enableTestMode = false;
    
    private void Start()
    {
        if (enableTestMode)
        {
            // 테스트 모드에서 업적 시스템 초기화 확인
            if (AchievementManager.Instance == null)
            {
                Debug.LogWarning("[AchievementSystemExample] AchievementManager가 초기화되지 않았습니다.");
            }
            else
            {
                Debug.Log("[AchievementSystemExample] 업적 시스템이 정상적으로 초기화되었습니다.");
            }
        }
    }
    
    /// <summary>
    /// 테스트용 업적 완료 메서드
    /// </summary>
    [ContextMenu("Test Complete First Victory")]
    public void TestCompleteFirstVictory()
    {
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.CheckBattleAchievements(GameOutcome.Victory, true);
        }
    }
    
    /// <summary>
    /// 테스트용 엔딩 완료 메서드
    /// </summary>
    [ContextMenu("Test Complete General Ending")]
    public void TestCompleteGeneralEnding()
    {
        if (AchievementManager.Instance != null && MultiEndingSystem.Instance != null)
        {
            var endingData = MultiEndingSystem.Instance.GetFinalEndingData();
            AchievementManager.Instance.CheckEndingAchievements(endingData);
        }
    }
    
    /// <summary>
    /// 테스트용 회차 시작 메서드
    /// </summary>
    [ContextMenu("Test Start Playthrough")]
    public void TestStartPlaythrough()
    {
        if (AchievementManager.Instance != null)
        {
            int playthroughCount = DataManager.Instance?.PlayerData?.playthroughCount ?? 1;
            AchievementManager.Instance.CheckPlaythroughAchievements(playthroughCount);
        }
    }
}

