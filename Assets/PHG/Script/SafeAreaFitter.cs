using UnityEngine;

// RectTransform 컴포넌트가 반드시 필요함을 명시합니다.
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    [Header("Safe Area Settings")]
    [SerializeField] private bool useSafeArea = true;
    [SerializeField] private Vector4 padding = Vector4.zero; // left, bottom, right, top
    
    [Header("Padding Limits")]
    [SerializeField] private Vector2 minPadding = new Vector2(10, 10); // 최소 패딩
    [SerializeField] private Vector2 maxPadding = new Vector2(50, 100); // 최대 패딩
    
    [Header("Performance")]
    [SerializeField] private bool checkOnOrientationChange = true; // 화면 회전시에만 체크
    
    private RectTransform panelRectTransform;
    private Rect lastSafeArea = new Rect(0, 0, 0, 0);
    private ScreenOrientation lastOrientation;

    void Awake()
    {
        panelRectTransform = GetComponent<RectTransform>();
        lastOrientation = Screen.orientation;
        ApplySafeArea();
    }

    void Start()
    {
        // Start에서 한 번 더 적용 (Canvas가 완전히 초기화된 후)
        ApplySafeArea();
    }

    void Update()
    {
        // 화면 회전이나 Safe Area 변경시에만 체크 (성능 최적화)
        if (checkOnOrientationChange && Screen.orientation != lastOrientation)
        {
            lastOrientation = Screen.orientation;
            ApplySafeArea();
        }
        else if (Screen.safeArea != lastSafeArea)
        {
            ApplySafeArea();
        }
    }

    private void ApplySafeArea()
    {
        if (!useSafeArea)
        {
            // Safe Area 사용하지 않을 때는 전체 화면 사용
            panelRectTransform.anchorMin = Vector2.zero;
            panelRectTransform.anchorMax = Vector2.one;
            return;
        }

        Rect safeArea = Screen.safeArea;
        lastSafeArea = safeArea;

        // safe area 사각형 값을 픽셀 좌표에서 뷰포트 좌표로 변환합니다.
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        // 패딩 적용 (픽셀 단위)
        anchorMin.x += padding.x; // left padding
        anchorMin.y += padding.y; // bottom padding
        anchorMax.x -= padding.z; // right padding
        anchorMax.y -= padding.w; // top padding

        // 최소/최대 패딩 제한 적용
        float minPaddingX = minPadding.x;
        float minPaddingY = minPadding.y;
        float maxPaddingX = maxPadding.x;
        float maxPaddingY = maxPadding.y;

        // 화면 가장자리에서의 최소/최대 거리 보장
        anchorMin.x = Mathf.Max(anchorMin.x, minPaddingX);
        anchorMin.y = Mathf.Max(anchorMin.y, minPaddingY);
        anchorMax.x = Mathf.Min(anchorMax.x, Screen.width - maxPaddingX);
        anchorMax.y = Mathf.Min(anchorMax.y, Screen.height - maxPaddingY);

        // 뷰포트 좌표로 변환
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        // 뷰포트 좌표를 RectTransform의 앵커에 적용합니다.
        panelRectTransform.anchorMin = anchorMin;
        panelRectTransform.anchorMax = anchorMax;

        Debug.Log($"세이프 에어리어 적용됨: Min({anchorMin.x:F2}, {anchorMin.y:F2}) Max({anchorMax.x:F2}, {anchorMax.y:F2}) " +
                  $"패딩: L{padding.x:F0}, B{padding.y:F0}, R{padding.z:F0}, T{padding.w:F0}");
    }

    // 런타임에서 패딩 조정
    public void SetPadding(Vector4 newPadding)
    {
        padding = newPadding;
        ApplySafeArea();
    }

    // Safe Area 사용 여부 토글
    public void SetUseSafeArea(bool use)
    {
        useSafeArea = use;
        ApplySafeArea();
    }

    // 수동으로 Safe Area 재적용
    public void RefreshSafeArea()
    {
        ApplySafeArea();
    }
}