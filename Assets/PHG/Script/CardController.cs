using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

public class CardController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private Vector2 initialPosition;
    private float distanceMoved;

    [Header("����")]
    public SituationCardController situationCardController;
    public UIFlowSimulator flowSimulator;

    private string leftChoiceTextString;
    private string rightChoiceTextString;

    private bool isPreviewing = false;
    private bool wasRightPreview = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        initialPosition = rectTransform.anchoredPosition;
    }

    // �ڡڡ� ������ ���� ���� ����! ���� �Լ��� ���� �ڡڡ�
    public void SetChoiceTexts(string left, string right)
    {
        leftChoiceTextString = left;
        rightChoiceTextString = right;
    }

    public void OnBeginDrag(PointerEventData eventData) { }

    public void OnDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)rectTransform.parent,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint);

        distanceMoved = localPoint.x;
        rectTransform.anchoredPosition = new Vector2(localPoint.x, initialPosition.y);
        rectTransform.localEulerAngles = new Vector3(0, 0, -distanceMoved * 0.1f);

        float threshold = 50f;
        float maxSwipe = 300f;

        string textToShow = "";
        Color colorToShow = Color.clear;

        if (distanceMoved > threshold)
        {
            textToShow = rightChoiceTextString;
            float alpha = Mathf.InverseLerp(threshold, maxSwipe, distanceMoved);
            colorToShow = new Color(0.2f, 0.8f, 0.2f, alpha);
        }
        else if (distanceMoved < -threshold)
        {
            textToShow = leftChoiceTextString;
            float alpha = Mathf.InverseLerp(-threshold, -maxSwipe, distanceMoved);
            colorToShow = new Color(0.8f, 0.2f, 0.2f, alpha);
        }

        // �ڡڡ� ������ ���� ���� ����! �ؽ�Ʈ�� ���� ���� �ڡڡ�
        situationCardController.UpdateChoicePreview(textToShow, colorToShow);

        float dimmerAlpha = Mathf.InverseLerp(threshold, maxSwipe, Mathf.Abs(distanceMoved)) * 0.7f;
        flowSimulator.UpdateDimmer(dimmerAlpha);

        if (distanceMoved > threshold)
        {
            if (!isPreviewing || !wasRightPreview)
            {
                flowSimulator.PreviewAffectedParameters(true);
                isPreviewing = true;
                wasRightPreview = true;
            }
        }
        else if (distanceMoved < -threshold)
        {
            if (!isPreviewing || wasRightPreview)
            {
                flowSimulator.PreviewAffectedParameters(false);
                isPreviewing = true;
                wasRightPreview = false;
            }
        }
        else
        {
            if (isPreviewing)
            {
                flowSimulator.ClearParameterPreview();
                isPreviewing = false;
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        situationCardController.UpdateChoicePreview("", Color.clear);
        flowSimulator.UpdateDimmer(0f);

        if (Mathf.Abs(distanceMoved) > 250f)
        {
            flowSimulator.HandleChoice(distanceMoved > 0);
            AnimateCardOffscreen();
        }
        else
        {
            if (isPreviewing)
            {
                flowSimulator.ClearParameterPreview();
                isPreviewing = false;
            }
            rectTransform.DOAnchorPos(initialPosition, 0.3f).SetEase(Ease.OutBack);
            rectTransform.DORotate(Vector3.zero, 0.3f).SetEase(Ease.OutBack);
        }
    }

    private void AnimateCardOffscreen()
    {
        float targetY = -1500f;
        float targetRotation = distanceMoved > 0 ? 45f : -45f;

        rectTransform.DOAnchorPosY(targetY, 0.5f).SetEase(Ease.InQuad);
        rectTransform.DORotate(new Vector3(0, 0, targetRotation), 0.5f).SetEase(Ease.InQuad);
    }

    public void ResetCardState()
    {
        rectTransform.DOKill();
        rectTransform.anchoredPosition = initialPosition;
        rectTransform.localEulerAngles = Vector3.zero;
    }
}