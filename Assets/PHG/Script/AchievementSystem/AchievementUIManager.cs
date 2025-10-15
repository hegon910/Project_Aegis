using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

/// <summary>
/// 업적 UI를 관리하는 매니저 클래스
/// 기획서에 따른 UI 구조를 구현합니다.
/// </summary>
public class AchievementUIManager : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button closeButton;
    
    [Header("탭 영역")]
    [SerializeField] private Transform tabButtonParent;
    [SerializeField] private ScrollRect tabScrollRect;
    [SerializeField] private GameObject tabButtonPrefab;
    
    [Header("카드 영역")]
    [SerializeField] private Transform achievementCardParent;
    [SerializeField] private ScrollRect cardScrollRect;
    [SerializeField] private GameObject achievementCardPrefab;
    
    [Header("스크롤바")]
    [SerializeField] private Scrollbar tabScrollbar;
    [SerializeField] private Scrollbar cardScrollbar;
    
    [Header("설정")]
    [SerializeField] private AchievementType defaultType = AchievementType.Ending;
    [SerializeField] private Color completedCardColor = new Color(1f, 0.8f, 0f, 1f); // 노란색
    [SerializeField] private Color incompleteCardColor = new Color(0.5f, 0.5f, 0.5f, 1f); // 회색
    [SerializeField] private Color lockedCardColor = new Color(0.2f, 0.2f, 0.2f, 1f); // 어두운 회색
    
    // 현재 선택된 타입
    private AchievementType currentType = AchievementType.Ending;
    
    // 탭 버튼들
    private List<AchievementTabButton> tabButtons = new List<AchievementTabButton>();
    
    // 업적 카드들
    private List<AchievementCard> achievementCards = new List<AchievementCard>();
    
    private void Awake()
    {
        InitializeUI();
    }
    
    private void OnEnable()
    {
        // 이벤트 구독
        AchievementManager.OnAchievementUnlocked += OnAchievementUnlocked;
        AchievementManager.OnAchievementCompleted += OnAchievementCompleted;
        AchievementManager.OnRewardClaimed += OnRewardClaimed;
    }
    
    private void OnDisable()
    {
        // 이벤트 구독 해제
        AchievementManager.OnAchievementUnlocked -= OnAchievementUnlocked;
        AchievementManager.OnAchievementCompleted -= OnAchievementCompleted;
        AchievementManager.OnRewardClaimed -= OnRewardClaimed;
    }
    
    /// <summary>
    /// UI 초기화
    /// </summary>
    private void InitializeUI()
    {
        // 타이틀 텍스트 설정
        if (titleText != null)
        {
            titleText.text = "업적";
        }
        
        // 닫기 버튼 이벤트 설정
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseAchievementUI);
        }
        
        // 탭 버튼들 생성
        CreateTabButtons();
        
        // Layout Group 설정
        SetupLayoutGroup(tabButtonParent);
        SetupLayoutGroup(achievementCardParent);
        
        // 기본 타입 선택
        SelectType(defaultType);
    }
    
    /// <summary>
    /// 탭 버튼들 생성
    /// </summary>
    private void CreateTabButtons()
    {
        if (tabButtonPrefab == null || tabButtonParent == null) return;
        
        // 기존 탭 버튼들 제거
        foreach (var button in tabButtons)
        {
            if (button != null && button.gameObject != null)
            {
                DestroyImmediate(button.gameObject);
            }
        }
        tabButtons.Clear();
        
        // 타입별 탭 버튼 생성
        var types = System.Enum.GetValues(typeof(AchievementType)).Cast<AchievementType>().ToList();
        
        foreach (var type in types)
        {
            var tabButtonObj = Instantiate(tabButtonPrefab, tabButtonParent);
            var tabButton = tabButtonObj.GetComponent<AchievementTabButton>();
            
            if (tabButton != null)
            {
                tabButton.Initialize(type, OnTabButtonClicked);
                tabButtons.Add(tabButton);
            }
            
            // 탭 레이아웃 컴포넌트 추가
            var tabLayout = tabButtonObj.GetComponent<AchievementTabLayout>();
            if (tabLayout == null)
            {
                tabLayout = tabButtonObj.AddComponent<AchievementTabLayout>();
            }
            tabLayout.SetupTabLayout();
        }
    }
    
    /// <summary>
    /// 탭 버튼 클릭 이벤트
    /// </summary>
    private void OnTabButtonClicked(AchievementType type)
    {
        SelectType(type);
    }
    
    /// <summary>
    /// 타입 선택
    /// </summary>
    private void SelectType(AchievementType type)
    {
        currentType = type;
        
        // 탭 버튼 상태 업데이트
        foreach (var tabButton in tabButtons)
        {
            if (tabButton != null)
            {
                tabButton.SetSelected(tabButton.Type == type);
            }
        }
        
        // 업적 카드들 업데이트
        UpdateAchievementCards();
    }
    
    /// <summary>
    /// 업적 카드들 업데이트
    /// </summary>
    private void UpdateAchievementCards()
    {
        if (achievementCardPrefab == null || achievementCardParent == null) return;
        
        // 기존 카드들 제거
        foreach (var card in achievementCards)
        {
            if (card != null && card.gameObject != null)
            {
                DestroyImmediate(card.gameObject);
            }
        }
        achievementCards.Clear();
        
        // Layout Group 설정 (자동으로 추가)
        SetupLayoutGroup(achievementCardParent);
        
        // 업적 매니저에서 해당 타입 업적들 가져오기
        List<AchievementData> achievements = new List<AchievementData>();
        
        if (AchievementManager.Instance != null)
        {
            achievements = AchievementManager.Instance.GetAllAchievements();
            
            // 선택된 타입에 해당하는 업적만 필터링
            achievements = achievements.Where(a => a.type == currentType).ToList();
            
            // 히든 업적은 달성된 경우만 표시
            achievements = achievements.Where(a => a.category != AchievementCategory.Hidden || a.isCompleted).ToList();
        }
        
        // 카드들 생성
        foreach (var achievement in achievements)
        {
            var cardObj = Instantiate(achievementCardPrefab, achievementCardParent);
            var card = cardObj.GetComponent<AchievementCard>();
            
            if (card != null)
            {
                card.Initialize(achievement, OnRewardButtonClicked);
                achievementCards.Add(card);
            }
            
            // 카드 레이아웃 컴포넌트 추가
            var cardLayout = cardObj.GetComponent<AchievementCardLayout>();
            if (cardLayout == null)
            {
                cardLayout = cardObj.AddComponent<AchievementCardLayout>();
            }
            cardLayout.SetupCardLayout();
        }

        // 레이아웃 강제 갱신 및 스크롤 위치 초기화
        Canvas.ForceUpdateCanvases();
        var parentRect = achievementCardParent as RectTransform;
        if (parentRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
        }
        if (cardScrollRect != null)
        {
            cardScrollRect.verticalNormalizedPosition = 1f; // 맨 위
        }
    }
    
    /// <summary>
    /// 보상 버튼 클릭 이벤트
    /// </summary>
    private void OnRewardButtonClicked(AchievementData achievement)
    {
        if (AchievementManager.Instance != null)
        {
            bool success = AchievementManager.Instance.ClaimReward(achievement.achievementId);
            
            if (success)
            {
                // 해당 카드 업데이트
                var card = achievementCards.FirstOrDefault(c => c.Achievement.achievementId == achievement.achievementId);
                if (card != null)
                {
                    card.UpdateRewardStatus();
                }
            }
        }
    }
    
    /// <summary>
    /// 업적 해금 이벤트
    /// </summary>
    private void OnAchievementUnlocked(AchievementData achievement)
    {
        // UI 새로고침
        UpdateAchievementCards();
    }
    
    /// <summary>
    /// 업적 완료 이벤트
    /// </summary>
    private void OnAchievementCompleted(AchievementData achievement)
    {
        // UI 새로고침
        UpdateAchievementCards();
    }
    
    /// <summary>
    /// 보상 수령 이벤트
    /// </summary>
    private void OnRewardClaimed(AchievementData achievement)
    {
        // 해당 카드만 업데이트
        var card = achievementCards.FirstOrDefault(c => c.Achievement.achievementId == achievement.achievementId);
        if (card != null)
        {
            card.UpdateRewardStatus();
        }
    }
    
    /// <summary>
    /// 업적 UI 닫기
    /// </summary>
    private void CloseAchievementUI()
    {
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 업적 UI 열기
    /// </summary>
    public void OpenAchievementUI()
    {
        gameObject.SetActive(true);
        
        // UI 새로고침
        UpdateAchievementCards();
    }
    
    /// <summary>
    /// 특정 타입으로 업적 UI 열기
    /// </summary>
    public void OpenAchievementUI(AchievementType type)
    {
        OpenAchievementUI();
        SelectType(type);
    }
    
    /// <summary>
    /// Layout Group 설정 (Vertical Layout Group과 Content Size Fitter 자동 추가)
    /// </summary>
    private void SetupLayoutGroup(Transform parent)
    {
        if (parent == null) return;
        
        // Vertical Layout Group 추가/설정
        var layoutGroup = parent.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = parent.gameObject.AddComponent<VerticalLayoutGroup>();
        }
        
        // Vertical Layout Group 설정
        layoutGroup.spacing = 10f; // 요소 간 간격
        layoutGroup.padding = new RectOffset(10, 10, 10, 10); // 상하좌우 여백
        layoutGroup.childAlignment = TextAnchor.UpperLeft; // 자식 요소 정렬
        layoutGroup.childControlHeight = false; // 자식 높이 자동 조절 안함
        layoutGroup.childControlWidth = true; // 자식 너비 자동 조절
        layoutGroup.childForceExpandHeight = false; // 높이 강제 확장 안함
        layoutGroup.childForceExpandWidth = true; // 너비 강제 확장
        
        // Content Size Fitter 추가/설정
        var contentSizeFitter = parent.GetComponent<ContentSizeFitter>();
        if (contentSizeFitter == null)
        {
            contentSizeFitter = parent.gameObject.AddComponent<ContentSizeFitter>();
        }
        
        // Content Size Fitter 설정
        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        Debug.Log($"[AchievementUIManager] {parent.name}에 Layout Group이 설정되었습니다.");
    }
}
