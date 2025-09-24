using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 업적 카드 컴포넌트
/// 개별 업적의 정보를 표시하고 보상 수령 기능을 제공합니다.
/// </summary>
public class AchievementCard : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    [SerializeField] private Image cardBackground;
    [SerializeField] private TextMeshProUGUI achievementTitle;
    [SerializeField] private TextMeshProUGUI achievementDescription;
    
    [Header("보상 UI")]
    [SerializeField] private Image rewardIcon;
    [SerializeField] private TextMeshProUGUI rewardAmount;
    [SerializeField] private Button rewardButton;
    [SerializeField] private TextMeshProUGUI rewardButtonText;
    
    [Header("색상 설정")]
    [SerializeField] private Color completedCardColor = Color.yellow;
    [SerializeField] private Color incompleteCardColor = Color.gray;
    [SerializeField] private Color lockedCardColor = Color.black;
    
    // 현재 표시 중인 업적 데이터
    public AchievementData Achievement { get; private set; }
    
    // 이벤트
    private Action<AchievementData> onRewardButtonClicked;
    
    private void Awake()
    {
        if (rewardButton != null)
        {
            rewardButton.onClick.AddListener(OnRewardButtonClicked);
        }
    }
    
    /// <summary>
    /// 업적 카드 초기화
    /// </summary>
    public void Initialize(AchievementData achievement, Action<AchievementData> onRewardButtonClicked)
    {
        Achievement = achievement;
        this.onRewardButtonClicked = onRewardButtonClicked;
        
        UpdateCardDisplay();
    }
    
    /// <summary>
    /// 카드 표시 업데이트
    /// </summary>
    private void UpdateCardDisplay()
    {
        if (Achievement == null) return;
        
        // 업적 정보 표시
        UpdateAchievementInfo();
        
        // 보상 정보 표시
        UpdateRewardInfo();
        
        // 카드 상태에 따른 시각적 업데이트
        UpdateVisualState();
    }
    
    /// <summary>
    /// 업적 정보 업데이트
    /// </summary>
    private void UpdateAchievementInfo()
    {
        // 제목 설정
        if (achievementTitle != null)
        {
            achievementTitle.text = Achievement.title;
        }
        
        // 설명 설정
        if (achievementDescription != null)
        {
            achievementDescription.text = Achievement.description;
        }
    }
    
    
    
    /// <summary>
    /// 보상 정보 업데이트
    /// </summary>
    private void UpdateRewardInfo()
    {
        if (Achievement.reward == null) return;
        
        // 보상 아이콘 설정
        if (rewardIcon != null)
        {
            // 보상 타입에 따른 아이콘 설정 (실제 구현 필요)
            rewardIcon.sprite = GetRewardIcon(Achievement.reward.rewardType);
        }
        
        // 보상 수량 표시
        if (rewardAmount != null)
        {
            rewardAmount.text = Achievement.reward.rewardValue.ToString();
        }
        
        // 보상 버튼 상태 업데이트
        UpdateRewardButton();
    }
    
    /// <summary>
    /// 보상 아이콘 가져오기
    /// </summary>
    private Sprite GetRewardIcon(RewardType rewardType)
    {
        // 실제 아이콘 리소스 로드 로직 (구현 필요)
        return null;
    }
    
    /// <summary>
    /// 보상 버튼 업데이트
    /// </summary>
    private void UpdateRewardButton()
    {
        if (rewardButton == null || rewardButtonText == null) return;
        
        bool canClaimReward = Achievement.isCompleted && !Achievement.isRewardClaimed;
        bool isRewardClaimed = Achievement.isRewardClaimed;
        
        // 버튼 활성화/비활성화
        rewardButton.interactable = canClaimReward;
        
        // 버튼 텍스트 설정
        if (isRewardClaimed)
        {
            rewardButtonText.text = "수령완료";
            rewardButton.interactable = false;
        }
        else if (canClaimReward)
        {
            rewardButtonText.text = "수령하기";
            rewardButton.interactable = true;
        }
        else
        {
            rewardButtonText.text = "미달성";
            rewardButton.interactable = false;
        }
    }
    
    /// <summary>
    /// 시각적 상태 업데이트
    /// </summary>
    private void UpdateVisualState()
    {
        // 카드 배경색 설정
        if (cardBackground != null)
        {
            if (!Achievement.isUnlocked)
            {
                cardBackground.color = lockedCardColor;
            }
            else if (Achievement.isCompleted)
            {
                cardBackground.color = completedCardColor;
            }
            else
            {
                cardBackground.color = incompleteCardColor;
            }
        }
        
        // 텍스트 알파값 조정 (잠긴 상태)
        float textAlpha = Achievement.isUnlocked ? 1.0f : 0.5f;
        
        if (achievementTitle != null)
        {
            var titleColor = achievementTitle.color;
            titleColor.a = textAlpha;
            achievementTitle.color = titleColor;
        }
        
        if (achievementDescription != null)
        {
            var descColor = achievementDescription.color;
            descColor.a = textAlpha;
            achievementDescription.color = descColor;
        }
    }
    
    /// <summary>
    /// 보상 버튼 클릭 이벤트
    /// </summary>
    private void OnRewardButtonClicked()
    {
        if (Achievement.isCompleted && !Achievement.isRewardClaimed)
        {
            onRewardButtonClicked?.Invoke(Achievement);
        }
    }
    
    /// <summary>
    /// 보상 상태 업데이트 (외부에서 호출)
    /// </summary>
    public void UpdateRewardStatus()
    {
        UpdateRewardButton();
    }
    
    /// <summary>
    /// 카드 전체 업데이트 (외부에서 호출)
    /// </summary>
    public void RefreshCard()
    {
        UpdateCardDisplay();
    }
}
