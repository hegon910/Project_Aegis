using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// 상점을 관리하는 매니저 클래스
/// </summary>
public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }
    
    [Header("상점 아이템")]
    [SerializeField] private List<ShopItemData> shopItems = new List<ShopItemData>();
    
    [Header("상점 설정")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool autoSaveOnPurchase = true;
    
    // 상점 아이템 저장
    private Dictionary<string, ShopItemData> shopItemDict = new Dictionary<string, ShopItemData>();
    
    // 이벤트
    public static event Action<ShopItemData> OnItemPurchased;
    public static event Action<ShopItemData> OnItemUnlocked;
    public static event Action<ShopItemData> OnItemLocked;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private System.Collections.IEnumerator Start()
    {
        // DataManager와 Settings가 준비될 때까지 잠시 대기
        // (재실행 시 설정 복원이 먼저 이루어지도록 보장)
        for (int i = 0; i < 5; i++) // 최대 5프레임 대기
        {
            if (DataManager.Instance != null) break;
            yield return null;
        }
        if (DataManager.Instance != null && DataManager.Instance.PlayerSettings == null)
        {
            DataManager.Instance.LoadSettings();
        }
        // 한 프레임 더 대기 후 초기화 (UI/다른 매니저 초기화 순서 보정)
        yield return null;
        InitializeShop();
    }
    
    /// <summary>
    /// 상점 초기화
    /// </summary>
    private void InitializeShop()
    {
        // 상점 아이템 딕셔너리 초기화
        shopItemDict.Clear();
        
        Debug.Log($"[ShopManager] Inspector에서 설정된 아이템 수: {shopItems.Count}");
        
        if (shopItems.Count == 0)
        {
            Debug.LogWarning("[ShopManager] Inspector에 설정된 아이템이 없습니다! Inspector에서 shopItems 리스트에 아이템을 추가해주세요.");
        }
        
        foreach (var item in shopItems)
        {
            Debug.Log($"[ShopManager] 아이템 처리 중: ID='{item.itemId}', Name='{item.itemName}', Type='{item.itemType}', Price='{item.price}'");
            
            if (!string.IsNullOrEmpty(item.itemId))
            {
                // 런타임 상태 초기화 (설정 복원 전 기본값으로 리셋)
                item.isPurchased = false;
                item.isUnlocked = false;
                item.purchasedCount = 0;

                shopItemDict[item.itemId] = item;
                Debug.Log($"[ShopManager] 아이템 추가됨: {item.itemName} (ID: {item.itemId})");

                // 재시작 시 구매 상태/해금 상태 복원
                var settings = DataManager.Instance?.PlayerSettings;
                if (settings != null)
                {
                    // 일반 아이템 구매 복원
                    if (settings.purchasedShopItemIds != null && settings.purchasedShopItemIds.Contains(item.itemId))
                    {
                        item.isPurchased = true;
                    }
                    // 스토리팩 해금 복원
                    if (item.itemType == ShopItemType.StoryPack && int.TryParse(item.unlockTargetId, out var packId))
                    {
                        if (settings.unlockedStoryPackIds != null && settings.unlockedStoryPackIds.Contains(packId))
                        {
                            item.isUnlocked = true;
                        }
                    }
                    // 지휘관 해금 복원 (UnlockManager 상태 기준)
                    if (item.itemType == ShopItemType.Commander && System.Enum.TryParse<CommanderTrait>(item.unlockTargetId, out var trait))
                    {
                        if (UnlockManager.IsUnlocked(trait))
                        {
                            item.isUnlocked = true;
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[ShopManager] itemId가 비어있는 아이템 발견: {item.itemName}");
            }
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[ShopManager] 상점이 초기화되었습니다. 총 {shopItemDict.Count}개 아이템");
            
            // 딕셔너리에 저장된 모든 아이템 출력
            foreach (var kvp in shopItemDict)
            {
                Debug.Log($"[ShopManager] 저장된 아이템: {kvp.Key} -> {kvp.Value.itemName}");
            }
        }
    }

    /// <summary>
    /// 디버그/리셋 직후 즉시 상점 상태를 재초기화하기 위한 공개 메서드
    /// </summary>
    public void ReinitializeNow()
    {
        InitializeShop();
        if (enableDebugLogs)
        {
            Debug.Log("[ShopManager] ReinitializeNow 호출됨 - 상점 상태를 즉시 재초기화했습니다.");
        }
    }
    
    /// <summary>
    /// 아이템 구매
    /// </summary>
    public bool PurchaseItem(string itemId)
    {
        if (!shopItemDict.ContainsKey(itemId))
        {
            Debug.LogWarning($"[ShopManager] 존재하지 않는 아이템 ID: {itemId}");
            return false;
        }
        
        var item = shopItemDict[itemId];
        
        if (!item.CanPurchase())
        {
            Debug.LogWarning($"[ShopManager] 아이템 '{item.itemName}'을 구매할 수 없습니다.");
            return false;
        }
        
        if (item.Purchase())
        {
            // 구매 후 해금 처리
            ProcessItemUnlock(item);
            
            OnItemPurchased?.Invoke(item);
            
            if (enableDebugLogs)
            {
                Debug.Log($"[ShopManager] 아이템 '{item.itemName}' 구매 완료!");
            }
            
            // 구매 내역 저장 + 자동 저장
            var settings = DataManager.Instance?.PlayerSettings;
            if (settings != null)
            {
                if (settings.purchasedShopItemIds == null) settings.purchasedShopItemIds = new List<string>();
                if (!settings.purchasedShopItemIds.Contains(item.itemId))
                {
                    settings.purchasedShopItemIds.Add(item.itemId);
                }
                DataManager.Instance.SaveSettings();
            }
            if (autoSaveOnPurchase) { SaveShopData(); }
            
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 아이템 구매 후 해금 처리
    /// </summary>
    private void ProcessItemUnlock(ShopItemData item)
    {
        switch (item.itemType)
        {
            case ShopItemType.Commander:
                // 지휘관 해금 처리
                if (System.Enum.TryParse<CommanderTrait>(item.unlockTargetId, out CommanderTrait commanderTrait))
                {
                    UnlockManager.Unlock(commanderTrait);
                }
                break;
                
            case ShopItemType.StoryPack:
                // 스토리팩 해금 처리 (추후 구현)
                Debug.Log($"[ShopManager] 스토리팩 '{item.unlockTargetId}' 해금 처리");
                if (int.TryParse(item.unlockTargetId, out var packId))
                {
                    var settings = DataManager.Instance?.PlayerSettings;
                    if (settings != null)
                    {
                        if (settings.unlockedStoryPackIds == null) settings.unlockedStoryPackIds = new List<int>();
                        if (!settings.unlockedStoryPackIds.Contains(packId))
                        {
                            settings.unlockedStoryPackIds.Add(packId);
                            DataManager.Instance.SaveSettings();
                            Debug.Log($"[ShopManager] 스토리팩 {packId} 해금 저장 완료");
                        }
                    }
                }
                break;
                
            case ShopItemType.Unlock:
                // 기타 해금 처리 (추후 구현)
                Debug.Log($"[ShopManager] 기타 해금 '{item.unlockTargetId}' 처리");
                break;
                
            case ShopItemType.Item:
            case ShopItemType.Currency:
                // 일반 아이템이나 재화는 별도 처리 없음
                break;
        }
    }
    
    /// <summary>
    /// 아이템 해금
    /// </summary>
    public void UnlockItem(string itemId)
    {
        if (!shopItemDict.ContainsKey(itemId))
        {
            Debug.LogWarning($"[ShopManager] 존재하지 않는 아이템 ID: {itemId}");
            return;
        }
        
        var item = shopItemDict[itemId];
        item.Unlock();
        
        OnItemUnlocked?.Invoke(item);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[ShopManager] 아이템 '{item.itemName}' 해금!");
        }
    }
    
    /// <summary>
    /// 아이템 잠금
    /// </summary>
    public void LockItem(string itemId)
    {
        if (!shopItemDict.ContainsKey(itemId))
        {
            Debug.LogWarning($"[ShopManager] 존재하지 않는 아이템 ID: {itemId}");
            return;
        }
        
        var item = shopItemDict[itemId];
        item.Lock();
        
        OnItemLocked?.Invoke(item);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[ShopManager] 아이템 '{item.itemName}' 잠금!");
        }
    }
    
    /// <summary>
    /// 특정 아이템 정보 가져오기
    /// </summary>
    public ShopItemData GetItem(string itemId)
    {
        return shopItemDict.ContainsKey(itemId) ? shopItemDict[itemId] : null;
    }
    
    /// <summary>
    /// 모든 상점 아이템 목록 가져오기
    /// </summary>
    public List<ShopItemData> GetAllItems()
    {
        return shopItemDict.Values.ToList();
    }
    
    /// <summary>
    /// 타입별 아이템 목록 가져오기
    /// </summary>
    public List<ShopItemData> GetItemsByType(ShopItemType itemType)
    {
        return shopItemDict.Values.Where(item => item.itemType == itemType).ToList();
    }
    
    /// <summary>
    /// 구매 가능한 아이템 목록 가져오기
    /// </summary>
    public List<ShopItemData> GetPurchasableItems()
    {
        return shopItemDict.Values.Where(item => item.CanPurchase()).ToList();
    }
    
    /// <summary>
    /// 구매한 아이템 목록 가져오기
    /// </summary>
    public List<ShopItemData> GetPurchasedItems()
    {
        return shopItemDict.Values.Where(item => item.isPurchased).ToList();
    }
    
    /// <summary>
    /// 해금된 아이템 목록 가져오기
    /// </summary>
    public List<ShopItemData> GetUnlockedItems()
    {
        return shopItemDict.Values.Where(item => item.isUnlocked).ToList();
    }
    
    /// <summary>
    /// 상점 데이터 저장
    /// </summary>
    private void SaveShopData()
    {
        // 여기서는 간단히 처리, 실제로는 SettingsData에 저장하거나 별도 파일로 저장
        Debug.Log("[ShopManager] 상점 데이터 저장 완료");
    }
    
    /// <summary>
    /// 상점 데이터 로드
    /// </summary>
    private void LoadShopData()
    {
        // 여기서는 간단히 처리, 실제로는 SettingsData에서 로드하거나 별도 파일에서 로드
        Debug.Log("[ShopManager] 상점 데이터 로드 완료");
    }
    
    /// <summary>
    /// 테스트용 아이템 추가 (디버그용)
    /// </summary>
    [ContextMenu("Add Test Items")]
    public void AddTestItems()
    {
        if (shopItems.Count > 0)
        {
            Debug.Log("[ShopManager] 이미 아이템이 있습니다. 테스트 아이템을 추가하지 않습니다.");
            return;
        }
        
        // 테스트 아이템 1: 지휘관 해금
        var testItem1 = new ShopItemData
        {
            itemId = "test_commander_1",
            itemName = "테스트 지휘관",
            description = "테스트용 지휘관입니다.",
            itemType = ShopItemType.Commander,
            price = 100,
            unlockTargetId = "TestCommander",
            isUnlocked = false,
            isPurchased = false
        };
        
        // 테스트 아이템 2: 일반 아이템
        var testItem2 = new ShopItemData
        {
            itemId = "test_item_1",
            itemName = "테스트 아이템",
            description = "테스트용 일반 아이템입니다.",
            itemType = ShopItemType.Item,
            price = 50,
            isUnlocked = false,
            isPurchased = false
        };
        
        shopItems.Add(testItem1);
        shopItems.Add(testItem2);
        
        // 상점 재초기화
        InitializeShop();
        
        Debug.Log("[ShopManager] 테스트 아이템 2개가 추가되었습니다.");
    }
    
    /// <summary>
    /// 디버그 정보 출력
    /// </summary>
    [ContextMenu("Print Debug Info")]
    public void PrintDebugInfo()
    {
        Debug.Log("=== ShopManager 디버그 정보 ===");
        Debug.Log($"전체 아이템 수: {shopItemDict.Count}");
        Debug.Log($"구매 가능한 아이템 수: {GetPurchasableItems().Count}");
        Debug.Log($"구매한 아이템 수: {GetPurchasedItems().Count}");
        Debug.Log($"해금된 아이템 수: {GetUnlockedItems().Count}");
        
        Debug.Log("\n구매 가능한 아이템 목록:");
        foreach (var item in GetPurchasableItems())
        {
            Debug.Log($"- {item.itemName} ({item.itemId})");
        }
    }
}
