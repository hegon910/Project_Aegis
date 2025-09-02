// UIPanelAnimator.cs (수정본)

using UnityEngine;
using DG.Tweening;

public class UIPanelAnimator : MonoBehaviour
{
    [Header("레이아웃 매니저")]
    [Tooltip("레이아웃을 제어하는 ThreePanelLayoutManager")]
    [SerializeField] private ThreePanelLayoutManager layoutManager;

    [Header("애니메이션 대상 패널 (RectTransform)")]
    [SerializeField] private RectTransform upperPanel;
    [SerializeField] private RectTransform middlePanel;
    [SerializeField] private RectTransform lowerPanel;

    [Header("애니메이션 설정")]
    [SerializeField] private float animationDuration = 0.6f;

    private Tween currentAnimation;

    private RectTransform GetParentRect()
    {
        if (layoutManager != null && layoutManager.transform.parent != null)
        {
            return layoutManager.transform.parent.GetComponent<RectTransform>();
        }
        if (upperPanel != null)
        {
            return upperPanel.parent.GetComponent<RectTransform>();
        }
        return null;
    }

    /// <summary>
    /// 메인 스토리 연출을 시작합니다.
    /// </summary>
    public void ShowMainStoryView()
    {
        currentAnimation?.Kill();
        if (layoutManager != null) layoutManager.layoutUpdateEnabled = false;

        Sequence sequence = DOTween.Sequence();

        // [수정 1] 상단 패널을 '위로' 보내기 위해 목표 y값을 양수로 변경합니다.
        // (Anchor가 상단일 때 anchoredPosition.y가 양수여야 위로 이동)
        if (upperPanel != null)
        {
            sequence.Join(upperPanel.DOAnchorPosY(upperPanel.rect.height, animationDuration).SetEase(Ease.OutCubic));
        }

        if (lowerPanel != null)
        {
            sequence.Join(lowerPanel.DOAnchorPosY(-lowerPanel.rect.height, animationDuration).SetEase(Ease.OutCubic));
        }

        if (middlePanel != null)
        {
            sequence.Join(DOTween.To(() => middlePanel.offsetMin, v => middlePanel.offsetMin = v, Vector2.zero, animationDuration).SetEase(Ease.OutCubic));
            sequence.Join(DOTween.To(() => middlePanel.offsetMax, v => middlePanel.offsetMax = v, Vector2.zero, animationDuration).SetEase(Ease.OutCubic));
        }

        currentAnimation = sequence;
    }

    /// <summary>
    /// 원래의 UI 레이아웃으로 부드럽게 복구합니다.
    /// </summary>
    public void ShowDefaultView()
    {
        currentAnimation?.Kill();
        if (layoutManager != null) layoutManager.layoutUpdateEnabled = false;

        Sequence sequence = DOTween.Sequence();
        RectTransform parentRect = GetParentRect();
        if (parentRect == null) return;

        float parentHeight = parentRect.rect.height;
        float topPanelTargetHeight = parentHeight * layoutManager.topPanelHeightPercentage;
        float bottomPanelTargetHeight = parentHeight * layoutManager.bottomPanelHeightPercentage;

        if (upperPanel != null)
        {
            sequence.Join(upperPanel.DOAnchorPos(Vector2.zero, animationDuration).SetEase(Ease.OutCubic));
            sequence.Join(upperPanel.DOSizeDelta(new Vector2(0, topPanelTargetHeight), animationDuration).SetEase(Ease.OutCubic));
        }

        if (lowerPanel != null)
        {
            sequence.Join(lowerPanel.DOAnchorPos(Vector2.zero, animationDuration).SetEase(Ease.OutCubic));
            sequence.Join(lowerPanel.DOSizeDelta(new Vector2(0, bottomPanelTargetHeight), animationDuration).SetEase(Ease.OutCubic));
        }

        if (middlePanel != null)
        {
            Vector2 targetOffsetMin = new Vector2(0, bottomPanelTargetHeight);
            Vector2 targetOffsetMax = new Vector2(0, -topPanelTargetHeight);
            sequence.Join(DOTween.To(() => middlePanel.offsetMin, v => middlePanel.offsetMin = v, targetOffsetMin, animationDuration).SetEase(Ease.OutCubic));
            sequence.Join(DOTween.To(() => middlePanel.offsetMax, v => middlePanel.offsetMax = v, targetOffsetMax, animationDuration).SetEase(Ease.OutCubic));
        }

        sequence.OnComplete(() =>
        {
            if (layoutManager != null)
            {
                layoutManager.layoutUpdateEnabled = true;
            }
        });

        currentAnimation = sequence;
    }

    /// <summary>
    /// [추가된 메소드] 화면 크기 변경 시 호출됩니다.
    /// 진행 중인 애니메이션을 중단하고 레이아웃을 즉시 갱신합니다.
    /// </summary>
    public void HandleScreenResize()
    {
        // 진행 중인 애니메이션이 있다면 즉시 중단합니다.
        currentAnimation?.Kill();

        // Layout Manager를 다시 활성화하고 레이아웃을 즉시 적용하여
        // UI가 깨진 상태로 남지 않도록 합니다.
        if (layoutManager != null)
        {
            layoutManager.layoutUpdateEnabled = true;
            layoutManager.ApplyLayout();
        }
    }
}