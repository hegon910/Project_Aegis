// ChoiceCardSwipe.cs

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

[System.Serializable]
public class SwipePreviewUI
{
    public Image previewImage;
    public TextMeshProUGUI previewText;

    public void SetAlpha(float alpha)
    {
        if (previewImage)
        {
            var color = previewImage.color;
            color.a = alpha;
            previewImage.color = color;
        }
        if (previewText)
        {
            var color = previewText.color;
            color.a = alpha;
            previewText.color = color;
        }
    }
}

public class ChoiceCardSwipe : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Refs")]
    [SerializeField] RectTransform card;
    [SerializeField] Canvas canvas;

    [Header("Thresholds")]
    [SerializeField] float minSwipeDistance = 140f;
    [SerializeField] float verticalBias = 1.2f;

    [Header("Preview UI")]
    [SerializeField] private SwipePreviewUI attackPreview;
    [SerializeField] private SwipePreviewUI defendPreview;
    [SerializeField] private SwipePreviewUI skillPreview;

    [Header("Events")]
    public UnityEvent onSwipeLeft;
    public UnityEvent onSwipeRight;
    public UnityEvent onSwipeUp;

    Vector2 initialPosition;
    Vector2 dragDelta;

    void Reset()
    {
        card = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    void Awake()
    {
        if (!card) card = GetComponent<RectTransform>();
        if (!canvas) canvas = GetComponentInParent<Canvas>();
        initialPosition = card.anchoredPosition;

        attackPreview?.SetAlpha(0);
        defendPreview?.SetAlpha(0);
        skillPreview?.SetAlpha(0);
    }

    public void OnBeginDrag(PointerEventData e)
    {
        dragDelta = Vector2.zero;
    }

    public void OnDrag(PointerEventData e)
    {
        // 실제 터치/마우스의 이동량은 계속 누적합니다.
        dragDelta += e.delta / canvas.scaleFactor;

        // [핵심 수정] CardController의 로직을 정확히 반영합니다.
        // 드래그 방향을 판단하여 움직이는 축을 X 또는 Y로 고정합니다.
        Vector2 targetPosition = initialPosition;
        float absX = Mathf.Abs(dragDelta.x);
        float absY = Mathf.Abs(dragDelta.y);

        // 수직 움직임이 우세할 경우 (위쪽으로만)
        if (absY * verticalBias > absX)
        {
            // Y축으로만 움직이되, 아래로는 움직이지 않도록 고정합니다.
            targetPosition.y = initialPosition.y + Mathf.Max(0, dragDelta.y);
        }
        // 수평 움직임이 우세할 경우
        else
        {
            // X축으로만 움직입니다.
            targetPosition.x = initialPosition.x + dragDelta.x;
        }

        // 계산된 고정된 위치로 카드를 이동시킵니다.
        card.anchoredPosition = targetPosition;

        // 카드의 기울임은 수평 움직임에만 반응하도록 유지합니다.
        card.rotation = Quaternion.Euler(0, 0, Mathf.Clamp(-dragDelta.x * 0.05f, -10f, 10f));

        // 미리보기 연출은 실제 드래그 값을 사용해야 자연스럽습니다.
        HandlePreviews();
    }

    public void OnEndDrag(PointerEventData e)
    {
        attackPreview?.SetAlpha(0);
        defendPreview?.SetAlpha(0);
        skillPreview?.SetAlpha(0);

        var absX = Mathf.Abs(dragDelta.x);
        var absY = Mathf.Abs(dragDelta.y);

        bool decided = false;
        Vector2 direction = Vector2.zero;

        if (absY * verticalBias > absX && dragDelta.y > minSwipeDistance)
        {
            onSwipeUp?.Invoke();
            decided = true;
            direction = Vector2.up;
        }
        else if (absX >= absY * verticalBias && dragDelta.x <= -minSwipeDistance)
        {
            onSwipeLeft?.Invoke();
            decided = true;
            direction = Vector2.left;
        }
        else if (absX >= absY * verticalBias && dragDelta.x >= minSwipeDistance)
        {
            onSwipeRight?.Invoke();
            decided = true;
            direction = Vector2.right;
        }

        if (decided)
        {
            AnimateCardOffscreen(direction);
        }
        else
        {
            ResetCard();
        }
    }

    private void HandlePreviews()
    {
        float absX = Mathf.Abs(dragDelta.x);
        float absY = Mathf.Abs(dragDelta.y);
        float previewThreshold = minSwipeDistance / 2f;

        if (absY * verticalBias > absX && dragDelta.y > previewThreshold)
        {
            float alpha = Mathf.InverseLerp(previewThreshold, minSwipeDistance, dragDelta.y);
            skillPreview?.SetAlpha(alpha);
            attackPreview?.SetAlpha(0);
            defendPreview?.SetAlpha(0);
        }
        else if (absX >= absY * verticalBias && dragDelta.x < -previewThreshold)
        {
            float alpha = Mathf.InverseLerp(previewThreshold, minSwipeDistance, absX);
            attackPreview?.SetAlpha(alpha);
            skillPreview?.SetAlpha(0);
            defendPreview?.SetAlpha(0);
        }
        else if (absX >= absY * verticalBias && dragDelta.x > previewThreshold)
        {
            float alpha = Mathf.InverseLerp(previewThreshold, minSwipeDistance, absX);
            defendPreview?.SetAlpha(alpha);
            attackPreview?.SetAlpha(0);
            skillPreview?.SetAlpha(0);
        }
        else
        {
            attackPreview?.SetAlpha(0);
            defendPreview?.SetAlpha(0);
            skillPreview?.SetAlpha(0);
        }
    }

    private void AnimateCardOffscreen(Vector2 direction)
    {
        card.DOAnchorPos(card.anchoredPosition + direction * 1200f, 0.5f).SetEase(Ease.InQuad);
        card.DORotate(new Vector3(0, 0, -direction.x * 45f), 0.5f).SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                card.anchoredPosition = initialPosition;
                card.rotation = Quaternion.identity;
                dragDelta = Vector2.zero;
            });
    }

    void ResetCard()
    {
        card.DOAnchorPos(initialPosition, 0.3f).SetEase(Ease.OutBack);
        card.DORotate(Vector3.zero, 0.3f).SetEase(Ease.OutBack);
        dragDelta = Vector2.zero;
    }
}