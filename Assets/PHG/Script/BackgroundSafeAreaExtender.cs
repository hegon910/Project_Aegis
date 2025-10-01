using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Safe Area 문제로 인한 빈 공간을 해결하기 위한 배경 확장 컴포넌트
/// 배경은 전체 화면을 사용하고, UI만 Safe Area를 따르도록 구성
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class BackgroundSafeAreaExtender : MonoBehaviour
{
    [Header("배경 확장 설정")]
    [SerializeField] private bool extendBackground = true;
    [SerializeField] private float backgroundExtendFactor = 1.1f; // 10% 확장
    
    [Header("장식 레이어 설정")]
    [SerializeField] private bool useDecorativeLayer = true;
    [SerializeField] private float decorativeMargin = 0.05f; // 5% 마진
    
    [Header("UI 레이어 설정")]
    [SerializeField] private bool useUISafeArea = true;
    [SerializeField] private Vector4 uiPadding = new Vector4(0, 0, 0, 0);
    
    [Header("자동 설정")]
    [SerializeField] private bool autoDetectLayers = true;
    
    private RectTransform rectTransform;
    private Rect lastSafeArea = new Rect(0, 0, 0, 0);
    private ScreenOrientation lastOrientation;
    
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        lastOrientation = Screen.orientation;
    }
    
    void Start()
    {
        if (autoDetectLayers)
        {
            AutoDetectAndSetupLayers();
        }
        
        ApplySafeAreaExtension();
    }
    
    void Update()
    {
        // 화면 회전이나 Safe Area 변경시 재적용
        if (Screen.orientation != lastOrientation || Screen.safeArea != lastSafeArea)
        {
            lastOrientation = Screen.orientation;
            lastSafeArea = Screen.safeArea;
            ApplySafeAreaExtension();
        }
    }
    
    private void AutoDetectAndSetupLayers()
    {
        // 자동으로 배경과 UI 레이어를 감지하고 설정
        Transform backgroundLayer = FindLayerByName("Background");
        Transform uiLayer = FindLayerByName("UI");
        Transform decorativeLayer = FindLayerByName("Decorative");
        
        if (backgroundLayer != null)
        {
            SetupBackgroundLayer(backgroundLayer.GetComponent<RectTransform>());
        }
        
        if (uiLayer != null)
        {
            SetupUILayer(uiLayer.GetComponent<RectTransform>());
        }
        
        if (decorativeLayer != null)
        {
            SetupDecorativeLayer(decorativeLayer.GetComponent<RectTransform>());
        }
    }
    
    private Transform FindLayerByName(string layerName)
    {
        // 현재 오브젝트의 자식에서 레이어 찾기
        Transform found = transform.Find(layerName);
        if (found != null) return found;
        
        // 형제 오브젝트에서 찾기
        if (transform.parent != null)
        {
            found = transform.parent.Find(layerName);
            if (found != null) return found;
        }
        
        // 태그로 찾기
        GameObject tagged = GameObject.FindGameObjectWithTag(layerName);
        return tagged?.transform;
    }
    
    private void SetupBackgroundLayer(RectTransform bgRect)
    {
        if (!extendBackground) return;
        
        // 배경은 전체 화면 사용 (Safe Area 무시)
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        
        // 확장 팩터 적용
        if (backgroundExtendFactor > 1f)
        {
            Vector2 currentSize = bgRect.sizeDelta;
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            Vector2 extendedSize = screenSize * backgroundExtendFactor;
            
            bgRect.sizeDelta = extendedSize;
        }
        
        Debug.Log($"[BackgroundSafeAreaExtender] 배경 레이어 확장됨: {bgRect.sizeDelta}");
    }
    
    private void SetupUILayer(RectTransform uiRect)
    {
        if (!useUISafeArea) return;
        
        // UI는 Safe Area 사용
        Rect safeArea = Screen.safeArea;
        
        Vector2 anchorMin = new Vector2(
            safeArea.x / Screen.width,
            safeArea.y / Screen.height
        );
        Vector2 anchorMax = new Vector2(
            (safeArea.x + safeArea.width) / Screen.width,
            (safeArea.y + safeArea.height) / Screen.height
        );
        
        // 패딩 적용
        anchorMin.x += uiPadding.x / Screen.width;
        anchorMin.y += uiPadding.y / Screen.height;
        anchorMax.x -= uiPadding.z / Screen.width;
        anchorMax.y -= uiPadding.w / Screen.height;
        
        uiRect.anchorMin = anchorMin;
        uiRect.anchorMax = anchorMax;
        
        Debug.Log($"[BackgroundSafeAreaExtender] UI 레이어 Safe Area 적용됨");
    }
    
    private void SetupDecorativeLayer(RectTransform decRect)
    {
        if (!useDecorativeLayer) return;
        
        // 장식 레이어는 약간 확장된 Safe Area 사용
        Rect safeArea = Screen.safeArea;
        
        Vector2 anchorMin = new Vector2(
            Mathf.Max(0, safeArea.x / Screen.width - decorativeMargin),
            Mathf.Max(0, safeArea.y / Screen.height - decorativeMargin)
        );
        Vector2 anchorMax = new Vector2(
            Mathf.Min(1, (safeArea.x + safeArea.width) / Screen.width + decorativeMargin),
            Mathf.Min(1, (safeArea.y + safeArea.height) / Screen.height + decorativeMargin)
        );
        
        decRect.anchorMin = anchorMin;
        decRect.anchorMax = anchorMax;
        
        Debug.Log($"[BackgroundSafeAreaExtender] 장식 레이어 설정됨");
    }
    
    private void ApplySafeAreaExtension()
    {
        // 현재 오브젝트에 Safe Area 확장 적용
        if (extendBackground)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
    
    // 런타임에서 설정 변경
    public void SetBackgroundExtendFactor(float factor)
    {
        backgroundExtendFactor = factor;
        ApplySafeAreaExtension();
    }
    
    public void SetUIPadding(Vector4 padding)
    {
        uiPadding = padding;
        if (autoDetectLayers)
        {
            AutoDetectAndSetupLayers();
        }
    }
    
    public void SetDecorativeMargin(float margin)
    {
        decorativeMargin = margin;
        if (autoDetectLayers)
        {
            AutoDetectAndSetupLayers();
        }
    }
    
    // 수동으로 레이어 설정
    public void SetupLayers(Transform backgroundLayer, Transform uiLayer, Transform decorativeLayer = null)
    {
        if (backgroundLayer != null)
            SetupBackgroundLayer(backgroundLayer.GetComponent<RectTransform>());
            
        if (uiLayer != null)
            SetupUILayer(uiLayer.GetComponent<RectTransform>());
            
        if (decorativeLayer != null)
            SetupDecorativeLayer(decorativeLayer.GetComponent<RectTransform>());
    }
}
