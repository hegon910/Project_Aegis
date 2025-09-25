using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 업적 알림 패널을 영구적으로 관리하는 매니저
/// 게임 중 캔버스가 비활성화되어도 업적 알림은 계속 작동하도록 보장
/// </summary>
public class AchievementNotificationManager : MonoBehaviour
{
    public static AchievementNotificationManager Instance { get; private set; }
    
    [Header("업적 알림 UI")]
    [SerializeField] private Canvas achievementNotificationCanvas;
    [SerializeField] private AchievementNotificationPanel notificationPanel;
    
    [Header("캔버스 설정")]
    [SerializeField] private int canvasSortOrder = 100; // 다른 UI보다 높은 우선순위
    [SerializeField] private RenderMode renderMode = RenderMode.ScreenSpaceOverlay;
    
    private void Awake()
    {
        // 싱글톤 패턴
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
			InitializeNotificationSystem();

			// AchievementManager의 업적 해금 이벤트를 초기 시점부터 구독하여 누락 방지
			AchievementManager.OnAchievementUnlocked += OnAchievementUnlocked;
			// 구독 직후, 이미 해금되어 대기열에 쌓인 항목을 즉시 표시
			FlushPendingUnlocked();

			// 주기적으로 캔버스 상태 확인 및 복구
			InvokeRepeating(nameof(EnsureCanvasActive), 1f, 1f);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void OnDestroy()
    {
        // 이벤트 구독 해제
        AchievementManager.OnAchievementUnlocked -= OnAchievementUnlocked;
    }
    
    /// <summary>
    /// 알림 시스템 초기화
    /// </summary>
    private void InitializeNotificationSystem()
    {
		// 우선 기존 씬 내 UI 탐색 및 재사용 시도
		TryFindExistingUI();
		
		// 캔버스가 없으면 자동 생성
		if (achievementNotificationCanvas == null)
		{
			CreateNotificationCanvas();
		}
		
		// 알림 패널이 없으면 자동 생성
		if (notificationPanel == null)
		{
			CreateNotificationPanel();
		}
        
        // 캔버스 설정
        SetupCanvas();
        
        Debug.Log("[AchievementNotificationManager] 업적 알림 시스템이 초기화되었습니다.");
    }

	/// <summary>
	/// 기존 씬에 배치된 캔버스/패널을 자동 탐색하여 재사용
	/// </summary>
	private void TryFindExistingUI()
	{
		// 패널 우선 탐색
		if (notificationPanel == null)
		{
			notificationPanel = FindObjectOfType<AchievementNotificationPanel>(true);
			if (notificationPanel != null)
			{
				var parentCanvas = notificationPanel.GetComponentInParent<Canvas>();
				if (achievementNotificationCanvas == null && parentCanvas != null)
				{
					achievementNotificationCanvas = parentCanvas;
				}
				SetupCanvas();
				Debug.Log("[AchievementNotificationManager] 기존 업적 알림 패널을 재사용합니다.");
			}
		}

		// 이름으로 캔버스 탐색
		if (achievementNotificationCanvas == null)
		{
			var found = GameObject.Find("AchievementNotificationCanvas");
			if (found != null)
			{
				achievementNotificationCanvas = found.GetComponent<Canvas>();
				SetupCanvas();
				Debug.Log("[AchievementNotificationManager] 기존 캔버스를 재사용합니다.");
			}
		}
	}

	/// <summary>
	/// 매니저 구독 이전에 해금되어 대기열에 누적된 업적 알림을 즉시 표출
	/// </summary>
	private void FlushPendingUnlocked()
	{
		if (AchievementManager.Instance == null) return;
		var pendings = AchievementManager.Instance.DequeueAllPendingUnlocked();
		Debug.Log($"[AchievementNotificationManager] Flush 대기 알림 수: {pendings.Count}");
		foreach (var ach in pendings)
		{
			OnAchievementUnlocked(ach);
		}
	}
    
    /// <summary>
    /// 알림 전용 캔버스 생성
    /// </summary>
    private void CreateNotificationCanvas()
    {
        GameObject canvasObject = new GameObject("AchievementNotificationCanvas");
        canvasObject.transform.SetParent(transform);
        
        achievementNotificationCanvas = canvasObject.AddComponent<Canvas>();
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();
        
        SetupCanvas();
    }
    
    /// <summary>
    /// 캔버스 설정
    /// </summary>
    private void SetupCanvas()
    {
        if (achievementNotificationCanvas != null)
        {
            achievementNotificationCanvas.renderMode = renderMode;
            achievementNotificationCanvas.sortingOrder = canvasSortOrder;
            achievementNotificationCanvas.overrideSorting = true; // 다른 캔버스의 영향을 받지 않음
        }
    }
    
    /// <summary>
    /// 알림 패널 생성
    /// </summary>
    private void CreateNotificationPanel()
    {
        if (achievementNotificationCanvas == null) return;
        
        GameObject panelObject = new GameObject("AchievementNotificationPanel");
        panelObject.transform.SetParent(achievementNotificationCanvas.transform);
        
        // RectTransform 설정
        RectTransform rectTransform = panelObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.pivot = new Vector2(0, 1);
        rectTransform.sizeDelta = new Vector2(400, 120);
        
        // 배경 이미지 추가
        Image background = panelObject.AddComponent<Image>();
        background.color = new Color(0, 0, 0, 0.8f);

		// 아이콘 이미지 생성 (좌측)
		GameObject iconObject = new GameObject("Icon");
		iconObject.transform.SetParent(panelObject.transform);
		RectTransform iconRect = iconObject.AddComponent<RectTransform>();
		iconRect.anchorMin = new Vector2(0, 0);
		iconRect.anchorMax = new Vector2(0, 1);
		iconRect.pivot = new Vector2(0, 0.5f);
		iconRect.sizeDelta = new Vector2(96, 96);
		iconRect.anchoredPosition = new Vector2(12, -12);
		Image iconImage = iconObject.AddComponent<Image>();
        
        // 제목 텍스트 생성
        GameObject titleObject = new GameObject("TitleText");
        titleObject.transform.SetParent(panelObject.transform);
        RectTransform titleRect = titleObject.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.5f);
        titleRect.anchorMax = new Vector2(1, 1);
		titleRect.offsetMin = new Vector2(120, 10); // 아이콘 공간 확보
        titleRect.offsetMax = new Vector2(-10, -10);
        
        TextMeshProUGUI titleText = titleObject.AddComponent<TextMeshProUGUI>();
        titleText.text = "업적 해금!";
        titleText.fontSize = 24;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontStyle = FontStyles.Bold;
        
        // 업적 이름 텍스트 생성
        GameObject nameObject = new GameObject("AchievementNameText");
        nameObject.transform.SetParent(panelObject.transform);
        RectTransform nameRect = nameObject.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0);
        nameRect.anchorMax = new Vector2(1, 0.5f);
		nameRect.offsetMin = new Vector2(120, 10); // 아이콘 공간 확보
        nameRect.offsetMax = new Vector2(-10, -10);
        
        TextMeshProUGUI nameText = nameObject.AddComponent<TextMeshProUGUI>();
        nameText.text = "업적 이름";
        nameText.fontSize = 18;
        nameText.color = Color.yellow;
        nameText.alignment = TextAlignmentOptions.Center;
        
        // 알림 패널 스크립트 추가
        notificationPanel = panelObject.AddComponent<AchievementNotificationPanel>();
		// 동적 생성한 UI 컴포넌트 레퍼런스를 패널에 바인딩
		notificationPanel.BindUI(titleText, nameText, iconImage);
        
        Debug.Log("[AchievementNotificationManager] 업적 알림 패널이 자동으로 생성되었습니다.");
    }
    
    /// <summary>
    /// 업적 해금 시 호출
    /// </summary>
	private void OnAchievementUnlocked(AchievementData achievementData)
    {
        Debug.Log($"[AchievementNotificationManager] OnAchievementUnlocked 수신: {achievementData?.achievementId}");
        if (notificationPanel != null)
        {
            // 패널이 비활성화되어 있으면 활성화
            if (!notificationPanel.gameObject.activeInHierarchy)
            {
                notificationPanel.gameObject.SetActive(true);
                Debug.Log("[AchievementNotificationManager] 패널이 비활성화되어 다시 활성화했습니다.");
            }
			
			// 연출 큐: 현재 연출 중이면 이어서 재생되도록 대기
			StartCoroutine(ShowWithQueue(achievementData));
        }
        else
        {
            Debug.LogWarning("[AchievementNotificationManager] notificationPanel이 null입니다!");
        }
    }

	private readonly Queue<AchievementData> _queue = new Queue<AchievementData>();
	private bool _playing;
	private IEnumerator ShowWithQueue(AchievementData data)
	{
		Debug.Log($"[AchievementNotificationManager] 큐 적재: {data?.achievementId}");
		_queue.Enqueue(data);
		if (_playing) yield break;
		_playing = true;
		while (_queue.Count > 0)
		{
			var next = _queue.Dequeue();
			Debug.Log($"[AchievementNotificationManager] 알림 표시: {next?.achievementId}");
			notificationPanel.ShowAchievementUnlocked(next);
			// 패널이 총 연출 시간 동안 진행되므로 그만큼 대기
			float wait = notificationPanel != null ? notificationPanel.GetTotalDuration() : 4f;
			yield return new WaitForSeconds(wait + 0.05f);
		}
		_playing = false;
		Debug.Log("[AchievementNotificationManager] 큐 재생 완료");
	}
    
    /// <summary>
    /// 캔버스가 비활성화되었는지 확인하고 복구
    /// </summary>
    private void EnsureCanvasActive()
    {
        if (achievementNotificationCanvas != null)
        {
            if (!achievementNotificationCanvas.gameObject.activeInHierarchy)
            {
                achievementNotificationCanvas.gameObject.SetActive(true);
                Debug.Log("[AchievementNotificationManager] 캔버스가 비활성화되어 다시 활성화했습니다.");
            }
            
            if (!achievementNotificationCanvas.enabled)
            {
                achievementNotificationCanvas.enabled = true;
                Debug.Log("[AchievementNotificationManager] 캔버스 컴포넌트가 비활성화되어 다시 활성화했습니다.");
            }
        }
    }
    
    /// <summary>
    /// 알림 시스템 강제 활성화 (다른 캔버스가 비활성화되어도 작동)
    /// </summary>
    public void EnsureNotificationSystemActive()
    {
        EnsureCanvasActive();
    }
    
    /// <summary>
    /// 알림 테스트
    /// </summary>
    [ContextMenu("Test Achievement Notification")]
    public void TestNotification()
    {
        var testAchievement = new AchievementData
        {
            achievementId = "test_achievement",
            title = "테스트 업적",
            description = "이것은 테스트 업적입니다.",
            icon = null
        };
        
        OnAchievementUnlocked(testAchievement);
    }
    
    /// <summary>
    /// 알림 시스템 상태 확인
    /// </summary>
    [ContextMenu("Check Notification System Status")]
    public void CheckSystemStatus()
    {
        Debug.Log("=== AchievementNotificationManager 상태 ===");
        Debug.Log($"Instance: {(Instance != null ? "활성" : "비활성")}");
        Debug.Log($"Canvas: {(achievementNotificationCanvas != null ? "존재" : "없음")}");
        Debug.Log($"Canvas Active: {(achievementNotificationCanvas != null ? achievementNotificationCanvas.gameObject.activeInHierarchy.ToString() : "N/A")}");
        Debug.Log($"Panel: {(notificationPanel != null ? "존재" : "없음")}");
        Debug.Log($"Panel Active: {(notificationPanel != null ? notificationPanel.gameObject.activeInHierarchy.ToString() : "N/A")}");
    }
}
