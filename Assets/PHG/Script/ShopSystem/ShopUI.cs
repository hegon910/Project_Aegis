using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class ShopUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject shopPanel;           // 상점 패널
    [SerializeField] private Transform shopItemParent;       // 아이템들이 들어갈 부모
    [SerializeField] private GameObject shopItemPrefab;      // 아이템 UI 프리팹
    [SerializeField] private TMP_Text currencyText;              // 재화 표시 텍스트
    [SerializeField] private Button closeButton;             // 닫기 버튼
    
    [Header("Shop Item UI Components")]
    // 이 컴포넌트들은 동적으로 찾아서 사용하므로 SerializeField 제거
    
    [Header("Layout Settings")]
    [SerializeField] private float itemHeight = 300f; // 프리팹 높이
    [SerializeField] private float itemSpacing = 20f; // 아이템 간 간격
    [SerializeField] private float paddingTop = 40f;  // 헤더와의 간격
    [SerializeField] private float paddingBottom = 20f; // 리스트 하단 여백
    
    private List<GameObject> shopItemUIs = new List<GameObject>();
    
    void Start()
    {
        // 이벤트 구독
        ShopManager.OnItemPurchased += OnItemPurchased;
        CurrencyManager.OnCurrencyChanged += OnCurrencyChanged;
        
        // 버튼 이벤트 연결
        closeButton.onClick.AddListener(CloseShop);
        
        // 상점 패널은 처음에 비활성화
        if (shopPanel != null)
            shopPanel.SetActive(false);
    }
    
    void OnDestroy()
    {
        // 이벤트 구독 해제
        ShopManager.OnItemPurchased -= OnItemPurchased;
        CurrencyManager.OnCurrencyChanged -= OnCurrencyChanged;
    }
    
    /// <summary>
    /// 상점 열기
    /// </summary>
    public void OpenShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
            RefreshShopUI();
        }
    }
    
    /// <summary>
    /// 상점 닫기
    /// </summary>
    public void CloseShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 상점 UI 새로고침
    /// </summary>
    private void RefreshShopUI()
    {
        // 기존 아이템 UI들 제거
        foreach (var itemUI in shopItemUIs)
        {
            if (itemUI != null)
                Destroy(itemUI);
        }
        shopItemUIs.Clear();
        
        // 재화 표시 업데이트
        UpdateCurrencyDisplay();
        
        // 상점 아이템들 UI에 표시
        DisplayShopItems();
        
        // Content 크기 업데이트
        UpdateContentSize();

        // 첫 항목이 헤더에 가리지 않도록 Content 위치 초기화
        var contentRect = shopItemParent as RectTransform;
        if (contentRect != null)
        {
            contentRect.anchoredPosition = Vector2.zero;
        }
    }
    
    /// <summary>
    /// 상점 아이템들 표시
    /// </summary>
    private void DisplayShopItems()
    {
        if (ShopManager.Instance == null) 
        {
            Debug.LogWarning("[ShopUI] ShopManager.Instance가 null입니다!");
            return;
        }
        
        var allItems = ShopManager.Instance.GetAllItems();
        Debug.Log($"[ShopUI] 표시할 아이템 수: {allItems.Count}");
        
        foreach (var item in allItems)
        {
            Debug.Log($"[ShopUI] 아이템 처리 중: {item.itemName} (ID: {item.itemId})");
            CreateShopItemUI(item);
        }
    }
    
    /// <summary>
    /// 개별 상점 아이템 UI 생성
    /// </summary>
    private void CreateShopItemUI(ShopItemData item)
    {
        if (shopItemPrefab == null)
        {
            Debug.LogError("[ShopUI] shopItemPrefab이 null입니다! Inspector에서 프리팹을 할당해주세요.");
            return;
        }
        
        if (shopItemParent == null)
        {
            Debug.LogError("[ShopUI] shopItemParent가 null입니다! Inspector에서 부모 Transform을 할당해주세요.");
            return;
        }
        
        Debug.Log($"[ShopUI] 아이템 UI 생성 중: {item.itemName}");
        GameObject itemUI = Instantiate(shopItemPrefab, shopItemParent);
        itemUI.SetActive(true); // 프리팹이 비활성화 상태이므로 활성화
        
        // 아이템 위치 조정 (세로로 정렬)
        RectTransform itemRect = itemUI.GetComponent<RectTransform>();
        if (itemRect != null)
        {
            // 각 아이템을 세로로 배치 (아이템 높이만큼 간격을 두고)
            int index = shopItemUIs.Count;
            float yPosition = -(paddingTop + index * (itemHeight + itemSpacing));
            
            itemRect.anchoredPosition = new Vector2(0, yPosition);
            itemRect.anchorMin = new Vector2(0, 1); // 상단 앵커
            itemRect.anchorMax = new Vector2(1, 1); // 상단 앵커
            itemRect.sizeDelta = new Vector2(0, itemHeight); // 부모 너비에 맞춤
        }
        
        shopItemUIs.Add(itemUI);
        
        Debug.Log($"[ShopUI] 아이템 UI 생성 완료: {itemUI.name}");
        
        // 아이템 정보 설정
        SetupShopItemUI(itemUI, item);
        
        // Content 크기 조정
        UpdateContentSize();
    }
    
    /// <summary>
    /// 상점 아이템 UI 설정
    /// </summary>
    private void SetupShopItemUI(GameObject itemUI, ShopItemData item)
    {
        Debug.Log($"[ShopUI] Setting up UI for item: {item.itemName}");
        Debug.Log($"[ShopUI] ItemUI GameObject name: {itemUI.name}");
        Debug.Log($"[ShopUI] ItemUI children count: {itemUI.transform.childCount}");
        
        // 모든 자식 오브젝트 이름 출력
        for (int i = 0; i < itemUI.transform.childCount; i++)
        {
            Transform child = itemUI.transform.GetChild(i);
            Debug.Log($"[ShopUI] Child {i}: {child.name}");
        }
        
        // 아이템 이름
        Transform nameTransform = itemUI.transform.Find("ItemName");
        if (nameTransform != null)
        {
            TMP_Text nameText = nameTransform.GetComponent<TMP_Text>();
            if (nameText != null)
            {
                nameText.text = item.itemName;
                Debug.Log($"[ShopUI] Set item name: '{item.itemName}' -> '{nameText.text}'");
            }
            else
            {
                Debug.LogWarning("[ShopUI] ItemName TMP_Text component not found!");
            }
        }
        else
        {
            Debug.LogWarning("[ShopUI] ItemName Transform not found!");
        }
        
        // 아이템 설명
        Transform descTransform = itemUI.transform.Find("ItemDescription");
        if (descTransform != null)
        {
            TMP_Text descText = descTransform.GetComponent<TMP_Text>();
            if (descText != null)
                descText.text = item.description;
        }
        
        // 아이템 가격
        Transform priceTransform = itemUI.transform.Find("ItemPrice");
        if (priceTransform != null)
        {
            TMP_Text priceText = priceTransform.GetComponent<TMP_Text>();
            if (priceText != null)
                priceText.text = $"{item.price}개";
        }
        
        // 아이템 아이콘
        Transform iconTransform = itemUI.transform.Find("ItemIcon");
        if (iconTransform != null)
        {
            Image iconImage = iconTransform.GetComponent<Image>();
            if (iconImage != null && item.icon != null)
                iconImage.sprite = item.icon;
        }
        
        // 구매 버튼
        Transform buttonTransform = itemUI.transform.Find("BuyButton");
        if (buttonTransform != null)
        {
            Button buyButton = buttonTransform.GetComponent<Button>();
            if (buyButton != null)
            {
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(() => TryPurchaseItem(item.itemId));
                
                // 구매 가능 여부에 따라 버튼 활성화/비활성화
                buyButton.interactable = item.CanPurchase();
            }
        }
        
        // 구매 상태 표시
        UpdateItemUIState(itemUI, item);
    }
    
    /// <summary>
    /// 아이템 UI 상태 업데이트
    /// </summary>
    private void UpdateItemUIState(GameObject itemUI, ShopItemData item)
    {
        Transform buttonTransform = itemUI.transform.Find("BuyButton");
        if (buttonTransform == null) return;
        
        Button buyButton = buttonTransform.GetComponent<Button>();
        if (buyButton == null) return;
        
        // 버튼 텍스트 찾기
        Transform textTransform = buttonTransform.Find("Text (TMP)");
        TMP_Text buttonText = null;
        if (textTransform != null)
        {
            buttonText = textTransform.GetComponent<TMP_Text>();
        }
        
        bool treatAsPurchased = item.isPurchased || item.isUnlocked;
        // 구매 제한 상품의 경우, 한도 도달 시 구매완료로 간주
        if (!treatAsPurchased && item.isLimited && item.purchaseLimit > 0 && item.purchasedCount >= item.purchaseLimit)
        {
            treatAsPurchased = true;
        }

        if (treatAsPurchased)
        {
            buyButton.interactable = false;
            if (buttonText != null)
                buttonText.text = "구매완료";
        }
        else if (item.CanPurchase())
        {
            buyButton.interactable = true;
            if (buttonText != null)
                buttonText.text = "구매";
        }
        else
        {
            buyButton.interactable = false;
            if (buttonText != null)
                buttonText.text = "구매불가";
        }
    }
    
    /// <summary>
    /// 아이템 구매 시도
    /// </summary>
    private void TryPurchaseItem(string itemId)
    {
        bool success = ShopManager.Instance.PurchaseItem(itemId);
        if (success)
        {
            Debug.Log("구매 성공!");
            // UI 새로고침
            RefreshShopUI();
        }
        else
        {
            Debug.Log("구매 실패 - 재화 부족 또는 조건 미달");
            // 실패 메시지 표시 등
        }
    }
    
    /// <summary>
    /// 재화 표시 업데이트
    /// </summary>
    private void UpdateCurrencyDisplay()
    {
        if (currencyText != null)
        {
            int currentCurrency = CurrencyManager.GetCurrency();
            currencyText.text = $"재화: {currentCurrency}개";
        }
    }
    
    /// <summary>
    /// 아이템 구매 이벤트 처리
    /// </summary>
    private void OnItemPurchased(ShopItemData item)
    {
        Debug.Log($"아이템 구매됨: {item.itemName}");
        RefreshShopUI();

        // 지휘관 해금이 있었다면 캐러셀 UI를 즉시 갱신
        var carousels = GameObject.FindObjectsOfType<CommanderCarouselController>();
        foreach (var carousel in carousels)
        {
            try
            {
                carousel.UpdateAllLockOverlays();
                carousel.RefreshUIState();
            }
            catch { }
        }
    }
    
    /// <summary>
    /// 재화 변화 이벤트 처리
    /// </summary>
    private void OnCurrencyChanged(int changeAmount, int currentAmount)
    {
        UpdateCurrencyDisplay();
    }
    
    /// <summary>
    /// Content 크기 업데이트 (스크롤 가능하도록)
    /// </summary>
    private void UpdateContentSize()
    {
        if (shopItemParent == null) return;
        
        RectTransform contentRect = shopItemParent.GetComponent<RectTransform>();
        if (contentRect == null) return;
        
        // 아이템 개수에 따라 Content 높이 계산
        int count = shopItemUIs.Count;
        if (count <= 0)
        {
            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, paddingTop + paddingBottom);
            return;
        }
        float totalHeight = paddingTop + paddingBottom + (count * itemHeight) + ((count - 1) * itemSpacing);
        
        // Content 크기 설정
        contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, totalHeight);
        
        Debug.Log($"[ShopUI] Content 크기 업데이트: {totalHeight} (아이템 {shopItemUIs.Count}개)");
    }
}