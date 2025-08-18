using UnityEngine;

// RectTransform 컴포넌트가 반드시 필요함을 명시합니다.
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform panelRectTransform;
    private Rect lastSafeArea = new Rect(0, 0, 0, 0);

    void Awake()
    {
        panelRectTransform = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    void Update()
    {
        // 화면 방향 전환 등으로 런타임 중에 Safe Area가 변경될 수 있습니다.
        if (Screen.safeArea != lastSafeArea)
        {
            ApplySafeArea();
        }
    }

    private void ApplySafeArea()
    {
        Rect safeArea = Screen.safeArea;
        lastSafeArea = safeArea;

        // safe area 사각형 값을 픽셀 좌표에서 뷰포트 좌표로 변환합니다.
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        // 뷰포트 좌표를 RectTransform의 앵커에 적용합니다.
        panelRectTransform.anchorMin = anchorMin;
        panelRectTransform.anchorMax = anchorMax;

        Debug.Log($"세이프 에어리어 적용됨: Min({anchorMin.x:F2}, {anchorMin.y:F2}) Max({anchorMax.x:F2}, {anchorMax.y:F2})");
    }
}