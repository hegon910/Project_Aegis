using UnityEngine;

// ����Ƽ �����Ϳ��� �ǽð����� ��������� Ȯ���� �� �ְ� �մϴ�.
[ExecuteInEditMode]
public class ThreePanelLayoutManager : MonoBehaviour
{
    [Header("�г� ����")]
    public RectTransform topPanel;
    public RectTransform middlePanel;
    public RectTransform bottomPanel;

    [Header("���� ����")]
    [Range(0f, 1f)]
    public float topPanelHeightPercentage = 0.2f; // ��� �г��� ������ ȭ�� ���� ���� (20%)

    [Range(0f, 1f)]
    public float bottomPanelHeightPercentage = 0.15f; // �ϴ� �г��� ������ ȭ�� ���� ���� (15%)

    private RectTransform parentRectTransform;

    void Awake()
    {
        parentRectTransform = transform.parent.GetComponent<RectTransform>();
    }

    void Update()
    {
        // �����Ϳ��� ���� ����� ������ �Ǵ� ���� ���� �߿� ����˴ϴ�.
        ApplyLayout();
    }

    private void ApplyLayout()
    {
        if (topPanel == null || middlePanel == null || bottomPanel == null || parentRectTransform == null)
        {
            Debug.LogWarning("���̾ƿ��� ������ �г��� ��� �Ҵ���� �ʾҽ��ϴ�.");
            return;
        }

        float parentHeight = parentRectTransform.rect.height;

        // 1. ��� �г� ����
        // ��Ŀ�� ��ܿ� �����ϰ�, ���� ���� ������ ���� ����Ͽ� �����մϴ�.
        topPanel.anchorMin = new Vector2(0, 1);
        topPanel.anchorMax = new Vector2(1, 1);
        topPanel.pivot = new Vector2(0.5f, 1);
        topPanel.anchoredPosition = Vector2.zero;
        topPanel.sizeDelta = new Vector2(0, parentHeight * topPanelHeightPercentage);

        // 2. �ϴ� �г� ����
        // ��Ŀ�� �ϴܿ� �����ϰ�, ���� ���� ������ ���� ����Ͽ� �����մϴ�.
        bottomPanel.anchorMin = new Vector2(0, 0);
        bottomPanel.anchorMax = new Vector2(1, 0);
        bottomPanel.pivot = new Vector2(0.5f, 0);
        bottomPanel.anchoredPosition = Vector2.zero;
        bottomPanel.sizeDelta = new Vector2(0, parentHeight * bottomPanelHeightPercentage);

        // 3. �߰� �г� ����
        // ��Ŀ�� stretch-stretch�� �����Ͽ� ��ܰ� �ϴ� �г��� ������ ��� ������ ä��ϴ�.
        middlePanel.anchorMin = new Vector2(0, 0);
        middlePanel.anchorMax = new Vector2(1, 1);
        middlePanel.pivot = new Vector2(0.5f, 0.5f);
        // offsetMin/Max�� ����Ͽ� ��/�Ʒ� �г��� ���̸�ŭ �о���ϴ�.
        middlePanel.offsetMin = new Vector2(0, bottomPanel.sizeDelta.y); // Left, Bottom
        middlePanel.offsetMax = new Vector2(0, -topPanel.sizeDelta.y);  // -Right, -Top
    }
}