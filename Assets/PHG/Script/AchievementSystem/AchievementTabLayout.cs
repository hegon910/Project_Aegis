using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 업적 탭 버튼의 레이아웃을 자동으로 설정하는 컴포넌트
/// ScrollRect에서 올바른 크기와 배치를 보장합니다.
/// </summary>
public class AchievementTabLayout : MonoBehaviour
{
    [Header("탭 크기 설정")]
    [SerializeField] private float tabHeight = 60f;
    [SerializeField] private float tabWidth = 150f;
    [SerializeField] private Vector2 tabSize = new Vector2(150f, 60f);
    
    [Header("자동 설정")]
    [SerializeField] private bool autoSetupOnStart = true;
    
    private RectTransform rectTransform;
    private LayoutElement layoutElement;
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        // LayoutElement 컴포넌트 추가/설정
        layoutElement = GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = gameObject.AddComponent<LayoutElement>();
        }
    }
    
    private void Start()
    {
        if (autoSetupOnStart)
        {
            SetupTabLayout();
        }
    }
    
    /// <summary>
    /// 탭 레이아웃 설정
    /// </summary>
    public void SetupTabLayout()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }
        
        if (layoutElement == null)
        {
            layoutElement = GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = gameObject.AddComponent<LayoutElement>();
            }
        }
        
        // RectTransform 설정
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.pivot = new Vector2(0.5f, 1);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = tabSize;
        
        // LayoutElement 설정
        layoutElement.preferredHeight = tabHeight;
        layoutElement.preferredWidth = tabWidth;
        layoutElement.minHeight = tabHeight;
        layoutElement.minWidth = tabWidth;
        layoutElement.flexibleHeight = 0;
        layoutElement.flexibleWidth = 1;
        
        Debug.Log($"[AchievementTabLayout] {gameObject.name}의 레이아웃이 설정되었습니다. 크기: {tabSize}");
    }
    
    /// <summary>
    /// 탭 크기 업데이트
    /// </summary>
    public void UpdateTabSize(Vector2 newSize)
    {
        tabSize = newSize;
        tabWidth = newSize.x;
        tabHeight = newSize.y;
        
        SetupTabLayout();
    }
}
