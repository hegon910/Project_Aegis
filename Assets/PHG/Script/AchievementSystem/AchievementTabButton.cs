using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 업적 탭 버튼 컴포넌트
/// 카테고리별 업적을 선택하는 탭 버튼을 관리합니다.
/// </summary>
public class AchievementTabButton : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    [SerializeField] private Button tabButton;
    [SerializeField] private TextMeshProUGUI tabText;
    
    [Header("색상 설정")]
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private Color unselectedColor = Color.gray;
    [SerializeField] private Color selectedTextColor = Color.black;
    [SerializeField] private Color unselectedTextColor = Color.gray;
    
    // 현재 타입
    public AchievementType Type { get; private set; }
    
    // 선택 상태
    private bool isSelected = false;
    
    // 이벤트
    private Action<AchievementType> onTabClicked;
    
    private void Awake()
    {
        if (tabButton == null)
            tabButton = GetComponent<Button>();
        
        if (tabButton != null)
        {
            tabButton.onClick.AddListener(OnButtonClicked);
        }
    }
    
    /// <summary>
    /// 탭 버튼 초기화
    /// </summary>
    public void Initialize(AchievementType type, Action<AchievementType> onTabClicked)
    {
        Type = type;
        this.onTabClicked = onTabClicked;
        
        UpdateTabDisplay();
    }
    
    /// <summary>
    /// 탭 표시 업데이트
    /// </summary>
    private void UpdateTabDisplay()
    {
        // 탭 텍스트 설정
        if (tabText != null)
        {
            tabText.text = GetTypeDisplayName(Type);
        }
        
        // 초기 선택 상태 설정
        SetSelected(false);
    }
    
    /// <summary>
    /// 타입 표시 이름 가져오기
    /// </summary>
    private string GetTypeDisplayName(AchievementType type)
    {
        switch (type)
        {
            case AchievementType.Ending:
                return "엔딩";
            case AchievementType.Playthrough:
                return "회차";
            case AchievementType.Battle:
                return "전투";
            case AchievementType.MainStory:
                return "메인 스토리";
            case AchievementType.SubStory:
                return "서브 스토리";
            case AchievementType.Collection:
                return "수집";
            default:
                return type.ToString();
        }
    }
    
    /// <summary>
    /// 선택 상태 설정
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateVisualState();
    }
    
    /// <summary>
    /// 시각적 상태 업데이트
    /// </summary>
    private void UpdateVisualState()
    {
        // 텍스트 색상 설정
        if (tabText != null)
        {
            tabText.color = isSelected ? selectedTextColor : unselectedTextColor;
            tabText.fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
        }
        
        // 버튼 상호작용 설정
        if (tabButton != null)
        {
            tabButton.interactable = !isSelected; // 선택된 탭은 비활성화
        }
    }
    
    /// <summary>
    /// 버튼 클릭 이벤트
    /// </summary>
    private void OnButtonClicked()
    {
        onTabClicked?.Invoke(Type);
    }
    
    /// <summary>
    /// 탭 버튼 활성화/비활성화
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        if (tabButton != null)
        {
            tabButton.interactable = interactable && !isSelected;
        }
    }
    
    /// <summary>
    /// 완료된 업적 수 표시 (선택사항)
    /// </summary>
    public void UpdateCompletionCount(int completedCount, int totalCount)
    {
        if (tabText != null)
        {
            string baseText = GetTypeDisplayName(Type);
            tabText.text = $"{baseText} ({completedCount}/{totalCount})";
        }
    }
}
