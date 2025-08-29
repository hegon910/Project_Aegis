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

    [Header("�������� ����")]
    [SerializeField] private float maxHorizontalSwipe = 350f;
    [SerializeField] private float maxVerticalSwipe = 200f;
    [SerializeField] private float maxRotationAngle = 20f;

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
        dragDelta += e.delta / canvas.scaleFactor;

        Vector2 targetPosition = initialPosition;
        float absX = Mathf.Abs(dragDelta.x);
        float absY = Mathf.Abs(dragDelta.y);

        if (absY * verticalBias > absX)
        {
            float deltaY = Mathf.Max(0, dragDelta.y);
            // [����] ���� �̵� �Ÿ� ����
            targetPosition.y = initialPosition.y + Mathf.Clamp(deltaY, 0, maxVerticalSwipe);
        }
        else
        {
            // [����] ���� �̵� �Ÿ� ����
            targetPosition.x = initialPosition.x + Mathf.Clamp(dragDelta.x, -maxHorizontalSwipe, maxHorizontalSwipe);
        }

        card.anchoredPosition = targetPosition;

        // [����] ȸ�� ������ �ν����Ϳ��� ���� �����ϵ��� ����
        card.rotation = Quaternion.Euler(0, 0, Mathf.Clamp(-dragDelta.x * 0.05f, -maxRotationAngle, maxRotationAngle));

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
        // [����] ���� ȿ���� ���� �ִϸ��̼��� Sequence�� ���� ���������� �����մϴ�.
        float shakeDuration = 0.4f;
        float shakeStrength = 7f;
        int vibrato = 25;

        Sequence sequence = DOTween.Sequence();

        // 1. ���� ī�带 ���� �ִϸ��̼��� �߰��մϴ�.
        sequence.Append(card.DOShakeRotation(shakeDuration, new Vector3(0, 0, shakeStrength), vibrato, 90, false, ShakeRandomnessMode.Harmonic));

        // 2. �̾ ī�尡 ȭ�� ������ ���ư��� �ִϸ��̼��� �߰��մϴ�.
      //  sequence.Append(card.DOAnchorPos(card.anchoredPosition + direction * 1200f, 0.5f).SetEase(Ease.InQuad));
       // sequence.Join(card.DORotate(new Vector3(0, 0, -direction.x * 45f), 0.5f).SetEase(Ease.InQuad));

        // 3. ��� �ִϸ��̼��� ������ ī���� ���¸� �ʱ�ȭ�մϴ�.
        sequence.OnComplete(() =>
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