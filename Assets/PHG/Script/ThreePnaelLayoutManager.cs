using UnityEngine;

// 유니티 에디터에서 실시간으로 변경사항을 확인할 수 있게 합니다.
[ExecuteInEditMode]
public class ThreePanelLayoutManager : MonoBehaviour
{
    [Header("패널 참조")]
    public RectTransform topPanel;
    public RectTransform middlePanel;
    public RectTransform bottomPanel;

    [Header("비율 설정")]
    [Range(0f, 1f)]
    public float topPanelHeightPercentage = 0.2f; // 상단 패널이 차지할 화면 세로 비율 (20%)

    [Range(0f, 1f)]
    public float bottomPanelHeightPercentage = 0.15f; // 하단 패널이 차지할 화면 세로 비율 (15%)

    private RectTransform parentRectTransform;

    void Awake()
    {
        parentRectTransform = transform.parent.GetComponent<RectTransform>();
    }

    void Update()
    {
        // 에디터에서 값이 변경될 때마다 또는 게임 실행 중에 적용됩니다.
        ApplyLayout();
    }

    private void ApplyLayout()
    {
        if (topPanel == null || middlePanel == null || bottomPanel == null || parentRectTransform == null)
        {
            Debug.LogWarning("레이아웃을 적용할 패널이 모두 할당되지 않았습니다.");
            return;
        }

        float parentHeight = parentRectTransform.rect.height;

        // 1. 상단 패널 설정
        // 앵커를 상단에 고정하고, 높이 값을 비율에 따라 계산하여 설정합니다.
        topPanel.anchorMin = new Vector2(0, 1);
        topPanel.anchorMax = new Vector2(1, 1);
        topPanel.pivot = new Vector2(0.5f, 1);
        topPanel.anchoredPosition = Vector2.zero;
        topPanel.sizeDelta = new Vector2(0, parentHeight * topPanelHeightPercentage);

        // 2. 하단 패널 설정
        // 앵커를 하단에 고정하고, 높이 값을 비율에 따라 계산하여 설정합니다.
        bottomPanel.anchorMin = new Vector2(0, 0);
        bottomPanel.anchorMax = new Vector2(1, 0);
        bottomPanel.pivot = new Vector2(0.5f, 0);
        bottomPanel.anchoredPosition = Vector2.zero;
        bottomPanel.sizeDelta = new Vector2(0, parentHeight * bottomPanelHeightPercentage);

        // 3. 중간 패널 설정
        // 앵커를 stretch-stretch로 설정하여 상단과 하단 패널을 제외한 모든 공간을 채웁니다.
        middlePanel.anchorMin = new Vector2(0, 0);
        middlePanel.anchorMax = new Vector2(1, 1);
        middlePanel.pivot = new Vector2(0.5f, 0.5f);
        // offsetMin/Max를 사용하여 위/아래 패널의 높이만큼 밀어냅니다.
        middlePanel.offsetMin = new Vector2(0, bottomPanel.sizeDelta.y); // Left, Bottom
        middlePanel.offsetMax = new Vector2(0, -topPanel.sizeDelta.y);  // -Right, -Top
    }
}