using UnityEngine;

/// <summary>
/// 구매 이벤트 로거
/// 유료 아이템 구매 및 최초 구매 이벤트 로깅을 담당합니다.
/// </summary>
public static class PurchaseEventLogger
{
    private const string PARAM_ITEM_NAME = "item_name";
    private const string PUCOUNT_FLAG_KEY = "Analytics_HasMadePurchase";
    
    /// <summary>
    /// 유료 아이템 구매 이벤트 로그
    /// </summary>
    /// <param name="itemName">구매한 아이템 이름</param>
    public static void LogPurchase(string itemName)
    {
        if (AnalyticsEventManager.Instance == null)
        {
            Debug.LogWarning("[PurchaseEventLogger] AnalyticsEventManager가 초기화되지 않았습니다.");
            return;
        }
        
        // PURCHASE 이벤트 전송
        AnalyticsEventManager.Instance.LogEvent(LogEventType.PURCHASE, PARAM_ITEM_NAME, itemName);
        
        // 최초 구매인지 확인하고 PUCOUNT 이벤트 전송
        if (!HasMadePurchaseBefore())
        {
            MarkFirstPurchase();
            AnalyticsEventManager.Instance.LogEvent(LogEventType.PUCOUNT);
            Debug.Log("[PurchaseEventLogger] 최초 구매 유저 - PUCOUNT 이벤트 전송");
        }
    }
    
    /// <summary>
    /// 이전에 구매 이력이 있는지 확인
    /// </summary>
    private static bool HasMadePurchaseBefore()
    {
        return PlayerPrefs.GetInt(PUCOUNT_FLAG_KEY, 0) == 1;
    }
    
    /// <summary>
    /// 최초 구매 플래그 설정
    /// </summary>
    private static void MarkFirstPurchase()
    {
        PlayerPrefs.SetInt(PUCOUNT_FLAG_KEY, 1);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// 최초 구매 플래그 초기화 (테스트용)
    /// </summary>
    public static void ResetPurchaseFlag()
    {
        PlayerPrefs.DeleteKey(PUCOUNT_FLAG_KEY);
        PlayerPrefs.Save();
        Debug.Log("[PurchaseEventLogger] 구매 플래그 초기화됨");
    }
}

