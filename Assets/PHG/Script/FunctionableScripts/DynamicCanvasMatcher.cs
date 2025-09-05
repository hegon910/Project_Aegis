using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasScaler))]
public class DynamicCanvasMatcher : MonoBehaviour
{
    [SerializeField] private Vector2 referenceResolution = new Vector2(1080, 1920);

    // UIPanelAnimator는 선택적으로 할당될 수 있습니다.
    [SerializeField] private UIPanelAnimator uiPanelAnimator;

    private CanvasScaler canvasScaler;
    private int lastScreenWidth = 0;
    private int lastScreenHeight = 0;

    void Awake()
    {
        canvasScaler = GetComponent<CanvasScaler>();
        AdjustMatchValue();
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
    }

    void Update()
    {
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;

            AdjustMatchValue();

            // [수정] uiPanelAnimator가 할당되어 있을 경우에만 함수를 호출하도록 변경
            if (uiPanelAnimator != null)
            {
                uiPanelAnimator.HandleScreenResize();
            }
        }
    }

    private void AdjustMatchValue()
    {
        float referenceAspect = referenceResolution.x / referenceResolution.y;
        float currentAspect = (float)Screen.width / (float)Screen.height;

        if (currentAspect > referenceAspect)
        {
            canvasScaler.matchWidthOrHeight = 1f;
        }
        else
        {
            canvasScaler.matchWidthOrHeight = 0f;
        }
    }
}