using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Safe Area 문제를 고려한 최적화된 Canvas Scaler
/// 빈 공간을 최소화하면서도 다양한 화면 비율에 대응
/// </summary>
[RequireComponent(typeof(CanvasScaler))]
public class OptimizedCanvasScaler : MonoBehaviour
{
    [Header("해상도 설정")]
    [SerializeField] private Vector2 referenceResolution = new Vector2(1080, 1920);
    [SerializeField] private float minAspectRatio = 0.46f; // 9:19.5 비율
    [SerializeField] private float maxAspectRatio = 0.6f;  // 9:15 비율
    
    [Header("Safe Area 최적화")]
    [SerializeField] private bool optimizeForSafeArea = true;
    [SerializeField] private float safeAreaTolerance = 0.1f; // 10% 허용 오차
    
    [Header("성능 설정")]
    [SerializeField] private bool checkOnOrientationChange = true;
    [SerializeField] private float updateInterval = 0.5f; // 0.5초마다 체크
    
    private CanvasScaler canvasScaler;
    private float lastUpdateTime;
    private int lastScreenWidth;
    private int lastScreenHeight;
    private ScreenOrientation lastOrientation;
    
    void Awake()
    {
        canvasScaler = GetComponent<CanvasScaler>();
        canvasScaler.referenceResolution = referenceResolution;
        
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        lastOrientation = Screen.orientation;
        
        OptimizeCanvasSettings();
    }
    
    void Update()
    {
        // 설정된 간격마다 체크 (성능 최적화)
        if (Time.time - lastUpdateTime < updateInterval) return;
        
        bool needsUpdate = false;
        
        if (checkOnOrientationChange && Screen.orientation != lastOrientation)
        {
            lastOrientation = Screen.orientation;
            needsUpdate = true;
        }
        else if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            needsUpdate = true;
        }
        
        if (needsUpdate)
        {
            lastUpdateTime = Time.time;
            OptimizeCanvasSettings();
        }
    }
    
    private void OptimizeCanvasSettings()
    {
        float currentAspectRatio = (float)Screen.width / Screen.height;
        
        // Safe Area 정보 가져오기
        Rect safeArea = Screen.safeArea;
        float safeAreaRatio = (safeArea.width * safeArea.height) / (Screen.width * Screen.height);
        
        // 화면 비율에 따른 최적화
        if (currentAspectRatio < minAspectRatio)
        {
            // 매우 긴 화면 (iPhone X 시리즈 등)
            OptimizeForLongScreen(currentAspectRatio, safeAreaRatio);
        }
        else if (currentAspectRatio > maxAspectRatio)
        {
            // 넓은 화면 (태블릿 등)
            OptimizeForWideScreen(currentAspectRatio, safeAreaRatio);
        }
        else
        {
            // 일반적인 모바일 화면
            OptimizeForStandardScreen(currentAspectRatio, safeAreaRatio);
        }
        
        Debug.Log($"[OptimizedCanvasScaler] 화면 비율: {currentAspectRatio:F2}, " +
                  $"Safe Area 비율: {safeAreaRatio:F2}, " +
                  $"Match: {canvasScaler.matchWidthOrHeight:F2}");
    }
    
    private void OptimizeForLongScreen(float aspectRatio, float safeAreaRatio)
    {
        if (optimizeForSafeArea && safeAreaRatio < (1f - safeAreaTolerance))
        {
            // Safe Area가 많이 잘린 경우 - 높이 우선
            canvasScaler.matchWidthOrHeight = 0f; // 높이 기준
            Debug.Log("[OptimizedCanvasScaler] 긴 화면 + Safe Area 최적화: 높이 기준");
        }
        else
        {
            // 일반적인 긴 화면
            canvasScaler.matchWidthOrHeight = 0.3f; // 약간 높이 우선
            Debug.Log("[OptimizedCanvasScaler] 긴 화면: 약간 높이 우선");
        }
    }
    
    private void OptimizeForWideScreen(float aspectRatio, float safeAreaRatio)
    {
        // 넓은 화면은 너비 기준으로 최적화
        canvasScaler.matchWidthOrHeight = 1f; // 너비 기준
        Debug.Log("[OptimizedCanvasScaler] 넓은 화면: 너비 기준");
    }
    
    private void OptimizeForStandardScreen(float aspectRatio, float safeAreaRatio)
    {
        if (optimizeForSafeArea && safeAreaRatio < (1f - safeAreaTolerance))
        {
            // Safe Area 최적화가 필요한 경우
            float referenceAspect = referenceResolution.x / referenceResolution.y;
            
            if (aspectRatio > referenceAspect)
            {
                canvasScaler.matchWidthOrHeight = 0.7f; // 약간 높이 우선
            }
            else
            {
                canvasScaler.matchWidthOrHeight = 0.3f; // 약간 너비 우선
            }
            Debug.Log("[OptimizedCanvasScaler] 표준 화면 + Safe Area 최적화");
        }
        else
        {
            // 일반적인 표준 화면
            float referenceAspect = referenceResolution.x / referenceResolution.y;
            canvasScaler.matchWidthOrHeight = (aspectRatio > referenceAspect) ? 0.5f : 0.5f;
            Debug.Log("[OptimizedCanvasScaler] 표준 화면: 균형");
        }
    }
    
    // 런타임에서 설정 변경
    public void SetReferenceResolution(Vector2 newResolution)
    {
        referenceResolution = newResolution;
        canvasScaler.referenceResolution = newResolution;
        OptimizeCanvasSettings();
    }
    
    public void SetOptimizeForSafeArea(bool optimize)
    {
        optimizeForSafeArea = optimize;
        OptimizeCanvasSettings();
    }
    
    public void SetUpdateInterval(float interval)
    {
        updateInterval = Mathf.Max(0.1f, interval);
    }
    
    // 수동으로 캔버스 설정 재적용
    public void RefreshCanvasSettings()
    {
        OptimizeCanvasSettings();
    }
    
    // 현재 설정 정보 반환
    public CanvasInfo GetCanvasInfo()
    {
        return new CanvasInfo
        {
            screenSize = new Vector2(Screen.width, Screen.height),
            safeArea = Screen.safeArea,
            referenceResolution = referenceResolution,
            matchWidthOrHeight = canvasScaler.matchWidthOrHeight,
            aspectRatio = (float)Screen.width / Screen.height,
            safeAreaRatio = (Screen.safeArea.width * Screen.safeArea.height) / (Screen.width * Screen.height)
        };
    }
    
    [System.Serializable]
    public struct CanvasInfo
    {
        public Vector2 screenSize;
        public Rect safeArea;
        public Vector2 referenceResolution;
        public float matchWidthOrHeight;
        public float aspectRatio;
        public float safeAreaRatio;
        
        public override string ToString()
        {
            return $"화면: {screenSize}, Safe Area 비율: {safeAreaRatio:F2}, " +
                   $"화면 비율: {aspectRatio:F2}, Match: {matchWidthOrHeight:F2}";
        }
    }
}
