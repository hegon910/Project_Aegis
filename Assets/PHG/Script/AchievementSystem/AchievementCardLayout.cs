using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 업적 카드의 레이아웃을 자동으로 설정하는 컴포넌트
/// ScrollRect에서 올바른 크기와 배치를 보장합니다.
/// </summary>
public class AchievementCardLayout : MonoBehaviour
{
    [Header("카드 크기 설정")]
    [SerializeField] private float cardHeight = 120f;
    [SerializeField] private float cardWidth = 400f;
    [SerializeField] private Vector2 cardSize = new Vector2(400f, 120f);
    
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
            SetupCardLayout();
        }
    }
    
    /// <summary>
    /// 카드 레이아웃 설정
    /// </summary>
    public void SetupCardLayout()
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
        rectTransform.sizeDelta = cardSize;
        
        // LayoutElement 설정
        layoutElement.preferredHeight = cardHeight;
        layoutElement.preferredWidth = cardWidth;
        layoutElement.minHeight = cardHeight;
        layoutElement.minWidth = cardWidth;
        layoutElement.flexibleHeight = 0;
        layoutElement.flexibleWidth = 1;
        
        Debug.Log($"[AchievementCardLayout] {gameObject.name}의 레이아웃이 설정되었습니다. 크기: {cardSize}");
    }
    
    /// <summary>
    /// 카드 크기 업데이트
    /// </summary>
    public void UpdateCardSize(Vector2 newSize)
    {
        cardSize = newSize;
        cardWidth = newSize.x;
        cardHeight = newSize.y;
        
        SetupCardLayout();
    }
}

