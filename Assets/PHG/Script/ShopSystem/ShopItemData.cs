using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상점 아이템 타입 열거형
/// </summary>
public enum ShopItemType
{
    Commander,      // 지휘관 해금
    StoryPack,      // 스토리팩 해금
    Item,           // 일반 아이템
    Currency,       // 재화 교환
    Unlock          // 기타 해금
}

/// <summary>
/// 상점 아이템 데이터 클래스
/// </summary>
[Serializable]
public class ShopItemData
{
    [Header("기본 정보")]
    public string itemId;              // 아이템 고유 ID
    public string itemName;            // 아이템 이름
    [TextArea(2, 4)]
    public string description;         // 아이템 설명
    public ShopItemType itemType;      // 아이템 타입
    
    [Header("가격 정보")]
    public int price; // 가격 (재화 개수)
    
    [Header("해금 정보")]
    public string unlockTargetId;      // 해금할 대상 ID (지휘관, 스토리팩 등)
    public bool isUnlocked;            // 이미 해금되었는지
    public bool isPurchased;           // 구매했는지
    
    [Header("UI 표시")]
    public Sprite icon;                // 아이템 아이콘
    public Sprite lockedIcon;          // 잠긴 상태 아이콘
    public Color nameColor = Color.white;
    public Color descriptionColor = Color.gray;
    
    [Header("제한 사항")]
    public bool isLimited;             // 제한 상품인지
    public int purchaseLimit;          // 구매 제한 횟수 (0이면 무제한)
    public int purchasedCount;         // 구매한 횟수
    
    [Header("판매 조건")]
    public List<string> requiredAchievements; // 필요한 업적 ID 목록
    public List<string> requiredUnlocks;      // 필요한 해금 ID 목록
    
    public ShopItemData()
    {
        itemId = "";
        itemName = "Unknown Item";
        description = "";
        itemType = ShopItemType.Item;
        price = 0;
        unlockTargetId = "";
        isUnlocked = false;
        isPurchased = false;
        isLimited = false;
        purchaseLimit = 0;
        purchasedCount = 0;
        requiredAchievements = new List<string>();
        requiredUnlocks = new List<string>();
    }
    
    /// <summary>
    /// 아이템을 구매할 수 있는지 확인
    /// </summary>
    public bool CanPurchase()
    {
        // 이미 구매했거나 해금된 경우
        if (isPurchased || isUnlocked) return false;
        
        // 제한 상품이고 구매 제한에 도달한 경우: 더 이상 구매 불가
        if (isLimited && purchaseLimit > 0 && purchasedCount >= purchaseLimit)
        {
            return false;
        }
        
        // 필요한 업적이 모두 달성되었는지 확인
        if (requiredAchievements.Count > 0)
        {
            foreach (string achievementId in requiredAchievements)
            {
                var achievement = AchievementManager.Instance?.GetAchievement(achievementId);
                if (achievement == null || !achievement.isCompleted)
                {
                    return false;
                }
            }
        }
        
        // 필요한 해금이 모두 완료되었는지 확인
        if (requiredUnlocks.Count > 0)
        {
            foreach (string unlockId in requiredUnlocks)
            {
                // 여기서는 간단히 처리, 실제로는 UnlockManager와 연동
                // if (!UnlockManager.IsUnlocked(unlockId)) return false;
            }
        }
        
        // 가격이 설정되어 있는지 확인
        if (price <= 0) return false;
        
        // 재화가 충분한지 확인
        return CurrencyManager.HasEnoughCurrency(price);
    }
    
    /// <summary>
    /// 아이템 구매 처리
    /// </summary>
    public bool Purchase()
    {
        if (!CanPurchase()) return false;
        
        // 재화 차감
        if (!CurrencyManager.SubtractCurrency(price))
        {
            return false;
        }
        
        // 구매 처리
        isPurchased = true;
        purchasedCount++;
        
        return true;
    }
    
    /// <summary>
    /// 아이템 해금 처리
    /// </summary>
    public void Unlock()
    {
        isUnlocked = true;
    }
    
    /// <summary>
    /// 아이템 잠금 처리
    /// </summary>
    public void Lock()
    {
        isUnlocked = false;
        isPurchased = false;
    }
}
