using UnityEngine;
using System;

/// <summary>
/// 업적 보상으로 획득한 재화를 관리하는 매니저 클래스
/// </summary>
public static class CurrencyManager
{
    private static SettingsData Settings => DataManager.Instance?.PlayerSettings;
    
    // 이벤트
    public static event Action<int, int> OnCurrencyChanged; // 변화량, 현재량
    public static event Action<int> OnCurrencyInsufficient; // 부족량
    
    /// <summary>
    /// 설정이 준비되었는지 확인하고 필요시 초기화
    /// </summary>
    private static void EnsureSettingsReady()
    {
        if (DataManager.Instance == null) return;
        if (DataManager.Instance.PlayerSettings == null)
        {
            DataManager.Instance.LoadSettings();
        }
    }
    
    /// <summary>
    /// 현재 재화 보유량 가져오기
    /// </summary>
    public static int GetCurrency()
    {
        EnsureSettingsReady();
        return Settings?.currencyAmount ?? 0;
    }
    
    /// <summary>
    /// 재화 추가
    /// </summary>
    public static bool AddCurrency(int amount)
    {
        if (amount <= 0) return false;
        
        EnsureSettingsReady();
        if (Settings == null) return false;
        
        int oldAmount = Settings.currencyAmount;
        Settings.currencyAmount += amount;
        
        DataManager.Instance.SaveSettings();
        OnCurrencyChanged?.Invoke(amount, Settings.currencyAmount);
        Debug.Log($"[CurrencyManager] 재화 {amount}개 획득! (총 {Settings.currencyAmount}개)");
        
        return true;
    }
    
    /// <summary>
    /// 재화 차감
    /// </summary>
    public static bool SubtractCurrency(int amount)
    {
        if (amount <= 0) return false;
        
        EnsureSettingsReady();
        if (Settings == null) return false;
        
        if (Settings.currencyAmount < amount)
        {
            OnCurrencyInsufficient?.Invoke(amount - Settings.currencyAmount);
            Debug.LogWarning($"[CurrencyManager] 재화 부족! 필요: {amount}, 보유: {Settings.currencyAmount}");
            return false;
        }
        
        int oldAmount = Settings.currencyAmount;
        Settings.currencyAmount -= amount;
        
        DataManager.Instance.SaveSettings();
        OnCurrencyChanged?.Invoke(-amount, Settings.currencyAmount);
        Debug.Log($"[CurrencyManager] 재화 {amount}개 소모! (남은 {Settings.currencyAmount}개)");
        
        return true;
    }
    
    /// <summary>
    /// 재화 설정
    /// </summary>
    public static bool SetCurrency(int amount)
    {
        if (amount < 0) return false;
        
        EnsureSettingsReady();
        if (Settings == null) return false;
        
        int oldAmount = Settings.currencyAmount;
        Settings.currencyAmount = amount;
        
        DataManager.Instance.SaveSettings();
        OnCurrencyChanged?.Invoke(amount - oldAmount, Settings.currencyAmount);
        Debug.Log($"[CurrencyManager] 재화 {amount}개로 설정! (변화량: {amount - oldAmount})");
        
        return true;
    }
    
    /// <summary>
    /// 재화가 충분한지 확인
    /// </summary>
    public static bool HasEnoughCurrency(int requiredAmount)
    {
        EnsureSettingsReady();
        return Settings?.currencyAmount >= requiredAmount;
    }
    
    /// <summary>
    /// 재화 정보 출력 (디버그용)
    /// </summary>
    [ContextMenu("Print Currency Info")]
    public static void PrintCurrencyInfo()
    {
        EnsureSettingsReady();
        if (Settings == null)
        {
            Debug.Log("[CurrencyManager] 설정 데이터가 없습니다.");
            return;
        }
        
        Debug.Log($"=== CurrencyManager 재화 정보 ===");
        Debug.Log($"현재 재화: {Settings.currencyAmount}개");
    }
}
