// CardController.cs

using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private Vector2 initialPosition;
    private float distanceMoved;

    [Header("����")]
    // [����] ���� SituationCardController�� ���� ������ �ǻ츳�ϴ�.
    public SituationCardController situationCardController;
    // [����] MainScenarioManager���� ������ ���� ������ �״�� �����մϴ�.
    public IChoiceHandler choiceHandler;

    private string leftChoiceTextString;
    private string rightChoiceTextString;

    private bool isPreviewing = false;
    private bool wasRightPreview = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        initialPosition = rectTransform.anchoredPosition;
    }

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

        // [�ٽ� ����] �� �� ��ο� �̸����� ������Ʈ�� �����ϴ�.
        situationCardController?.UpdateChoicePreview(textToShow, colorToShow); // ���� ����� ���� ȣ��
        choiceHandler?.UpdateChoicePreview(textToShow, colorToShow);           // MainScenarioManager�� ���� ȣ��

        float dimmerAlpha = Mathf.InverseLerp(threshold, maxSwipe, Mathf.Abs(distanceMoved)) * 0.7f;
        choiceHandler?.UpdateDimmer(dimmerAlpha);

        if (distanceMoved > threshold)
        {
            if (!isPreviewing || !wasRightPreview)
            {
                choiceHandler?.PreviewAffectedParameters(true);
                isPreviewing = true;
                wasRightPreview = true;
            }
        }
        else if (distanceMoved < -threshold)
        {
            if (!isPreviewing || wasRightPreview)
            {
                choiceHandler?.PreviewAffectedParameters(false);
                isPreviewing = true;
                wasRightPreview = false;
            }
        }
        else
        {
            if (isPreviewing)
            {
                choiceHandler?.ClearParameterPreview();
                isPreviewing = false;
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // [�ٽ� ����] �巡�װ� ������ �� �� ����� �̸����⸦ �ʱ�ȭ�մϴ�.
        situationCardController?.UpdateChoicePreview("", Color.clear); // ���� ����� ���� ȣ��
        choiceHandler?.UpdateChoicePreview("", Color.clear);           // MainScenarioManager�� ���� ȣ��

        choiceHandler?.UpdateDimmer(0f);

        if (Mathf.Abs(distanceMoved) > 250f)
        {
            choiceHandler?.HandleChoice(distanceMoved > 0);
            AnimateCardOffscreen();
        }
        else
        {
            if (isPreviewing)
            {
                choiceHandler?.ClearParameterPreview();
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