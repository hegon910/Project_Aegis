using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;

/// <summary>
/// 업적 해금 시 나타나는 슬라이드 알림 패널
/// </summary>
public class AchievementNotificationPanel : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private Image achievementIcon;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI achievementNameText;
    
    [Header("애니메이션 설정")]
    [SerializeField] private float slideInDuration = 0.5f;
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private float slideOutDuration = 0.3f;
    [SerializeField] private Ease slideEase = Ease.OutBack;
    
    [Header("패널 설정")]
    [SerializeField] private float panelWidth = 400f;
    [SerializeField] private float panelHeight = 120f;
    [SerializeField] private float slideInDistance = 450f; // 화면 밖에서 들어올 거리
    
    private Vector2 hiddenPosition;
    private Vector2 visiblePosition;
    private bool isAnimating = false;
    
    private void Awake()
    {
        InitializePanel();
        SetupPositions();
        HidePanelImmediately();
        
        // 업적 알림 패널을 영구적으로 유지 (씬 전환 시에도 비활성화되지 않음)
        DontDestroyOnLoad(gameObject);
        
        // 패널이 비활성화되지 않도록 보호
        StartCoroutine(KeepPanelActive());
    }
    
    // AchievementNotificationManager에서 이벤트를 관리하므로 여기서는 구독하지 않음
    
    /// <summary>
    /// 패널 초기화
    /// </summary>
    private void InitializePanel()
    {
        if (panelRect == null)
        {
            panelRect = GetComponent<RectTransform>();
        }
        
        // 패널 크기 설정
        panelRect.sizeDelta = new Vector2(panelWidth, panelHeight);
        
        // 패널 앵커 설정 (왼쪽 상단)
        panelRect.anchorMin = new Vector2(0, 1);
        panelRect.anchorMax = new Vector2(0, 1);
        panelRect.pivot = new Vector2(0, 1);
    }
    
    /// <summary>
    /// 위치 설정
    /// </summary>
    private void SetupPositions()
    {
        // 화면 밖 위치 (왼쪽으로 숨겨진 위치)
        hiddenPosition = new Vector2(-slideInDistance, -50);
        
        // 화면 안 보이는 위치 (왼쪽 끝)
        visiblePosition = new Vector2(20, -50);
    }
    
    /// <summary>
    /// 업적 해금 알림 표시 (외부에서 호출)
    /// </summary>
    public void ShowAchievementUnlocked(AchievementData achievementData)
    {
        if (isAnimating) return;
        
        // 패널이 비활성화되어 있으면 활성화
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
        }
        
        // 업적 정보 설정
        SetAchievementInfo(achievementData);
        
        // 애니메이션 시작
        StartCoroutine(PlayNotificationAnimation());
    }
    
    /// <summary>
    /// 업적 정보 설정
    /// </summary>
    private void SetAchievementInfo(AchievementData achievementData)
    {
        if (achievementData == null) return;
        
        // 제목 텍스트 설정
        if (titleText != null)
        {
            titleText.text = "업적 해금!";
        }
        
        // 업적 이름 설정
        if (achievementNameText != null)
        {
            achievementNameText.text = achievementData.title;
        }
        
        // 업적 아이콘 설정 (있는 경우)
        if (achievementIcon != null && achievementData.icon != null)
        {
            achievementIcon.sprite = achievementData.icon;
            achievementIcon.gameObject.SetActive(true);
        }
        else if (achievementIcon != null)
        {
            // 기본 아이콘이나 투명도 설정
            achievementIcon.color = new Color(1f, 1f, 1f, 0.5f);
        }
    }
    
    /// <summary>
    /// 알림 애니메이션 재생
    /// </summary>
    private IEnumerator PlayNotificationAnimation()
    {
        isAnimating = true;
        
        // 패널 활성화 보장
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
        }
        
        // 1. 슬라이드 인 애니메이션
        panelRect.anchoredPosition = hiddenPosition;
        yield return panelRect.DOAnchorPos(visiblePosition, slideInDuration)
            .SetEase(slideEase)
            .WaitForCompletion();
        
        // 2. 잠시 대기
        yield return new WaitForSeconds(displayDuration);
        
        // 3. 슬라이드 아웃 애니메이션
        yield return panelRect.DOAnchorPos(hiddenPosition, slideOutDuration)
            .SetEase(Ease.InBack)
            .WaitForCompletion();
        
        // 패널 위치만 숨기기 (GameObject는 활성화 상태 유지)
        HidePanelImmediately();
        
        isAnimating = false;
    }
    
    /// <summary>
    /// 패널 즉시 숨기기 (위치는 숨기지만 GameObject는 활성화 상태 유지)
    /// </summary>
    private void HidePanelImmediately()
    {
        if (panelRect != null)
        {
            panelRect.anchoredPosition = hiddenPosition;
        }
        // gameObject.SetActive(false); // GameObject는 항상 활성화 상태 유지
        isAnimating = false;
    }
    
    /// <summary>
    /// 패널이 비활성화되지 않도록 지속적으로 보호
    /// </summary>
    private IEnumerator KeepPanelActive()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f); // 0.5초마다 체크
            
            if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(true);
                Debug.Log("[AchievementNotificationPanel] 패널이 비활성화되어 다시 활성화했습니다.");
            }
        }
    }
    
    /// <summary>
    /// 수동으로 알림 테스트 (디버그용)
    /// </summary>
    [ContextMenu("Test Achievement Notification")]
    public void TestNotification()
    {
        // 테스트용 업적 데이터 생성
        var testAchievement = new AchievementData
        {
            achievementId = "test_achievement",
            title = "테스트 업적",
            description = "이것은 테스트 업적입니다.",
            icon = null
        };
        
        ShowAchievementUnlocked(testAchievement);
    }

	public bool IsAnimating => isAnimating;

	public float GetTotalDuration()
	{
		return slideInDuration + displayDuration + slideOutDuration;
	}

	/// <summary>
	/// 동적으로 생성된 UI 컴포넌트를 바인딩
	/// </summary>
	public void BindUI(TextMeshProUGUI title, TextMeshProUGUI name, Image icon = null)
	{
		titleText = title;
		achievementNameText = name;
		achievementIcon = icon;
	}
    
    /// <summary>
    /// 애니메이션 강제 중단
    /// </summary>
    public void StopAnimation()
    {
        if (panelRect != null)
        {
            panelRect.DOKill();
        }
        StopAllCoroutines();
        HidePanelImmediately();
    }
}
