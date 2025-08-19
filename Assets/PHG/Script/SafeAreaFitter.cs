using UnityEngine;

// RectTransform ������Ʈ�� �ݵ�� �ʿ����� ����մϴ�.
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
        // ȭ�� ���� ��ȯ ������ ��Ÿ�� �߿� Safe Area�� ����� �� �ֽ��ϴ�.
        if (Screen.safeArea != lastSafeArea)
        {
            ApplySafeArea();
        }
    }

    private void ApplySafeArea()
    {
        Rect safeArea = Screen.safeArea;
        lastSafeArea = safeArea;

        // safe area �簢�� ���� �ȼ� ��ǥ���� ����Ʈ ��ǥ�� ��ȯ�մϴ�.
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        // ����Ʈ ��ǥ�� RectTransform�� ��Ŀ�� �����մϴ�.
        panelRectTransform.anchorMin = anchorMin;
        panelRectTransform.anchorMax = anchorMax;

        Debug.Log($"������ ����� �����: Min({anchorMin.x:F2}, {anchorMin.y:F2}) Max({anchorMax.x:F2}, {anchorMax.y:F2})");
    }
}