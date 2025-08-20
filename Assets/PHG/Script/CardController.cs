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
    public TMP_Text choiceText;
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

        // �ڡڡ� ������ �κ�: �� ����� ��� ���ƽ��ϴ�. �ڡڡ�
        float threshold = 50f;

        // 1. (������ ���) ������ �ؽ�Ʈ ǥ��
        float maxSwipe = 300f;
        if (distanceMoved > threshold)
        {
            choiceText.text = rightChoiceTextString;
            choiceText.color = new Color(0.2f, 0.8f, 0.2f, Mathf.InverseLerp(threshold, maxSwipe, distanceMoved));
        }
        else if (distanceMoved < -threshold)
        {
            choiceText.text = leftChoiceTextString;
            choiceText.color = new Color(0.8f, 0.2f, 0.2f, Mathf.InverseLerp(-threshold, -maxSwipe, distanceMoved));
        }
        else
        {
            choiceText.text = "";
        }

        // 2. (������ ���) ��� �̸�����
        if (distanceMoved > threshold) // ���������� ��￴�� ��
        {
            if (!isPreviewing || !wasRightPreview)
            {
                flowSimulator.PreviewAffectedParameters(true);
                isPreviewing = true;
                wasRightPreview = true;
            }
        }
        else if (distanceMoved < -threshold) // �������� ��￴�� ��
        {
            if (!isPreviewing || wasRightPreview)
            {
                flowSimulator.PreviewAffectedParameters(false);
                isPreviewing = true;
                wasRightPreview = false;
            }
        }
        else // �߾� ��ó�� ���ƿ��� ��
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
        choiceText.color = new Color(choiceText.color.r, choiceText.color.g, choiceText.color.b, 0);

        if (Mathf.Abs(distanceMoved) > 250f)
        {
            // �� �κ��� ���� ���������� ȣ��˴ϴ�.
            flowSimulator.HandleChoice(distanceMoved > 0);
            AnimateCardOffscreen();
        }
        else
        {
            // �巡�װ� ������� ���� ���� ���ڸ��� ���ƿɴϴ�.
            if (isPreviewing)
            {
                flowSimulator.ClearParameterPreview();
                isPreviewing = false;
            }
            rectTransform.DOAnchorPos(initialPosition, 0.3f).SetEase(Ease.OutBack);
            rectTransform.DORotate(Vector3.zero, 0.3f).SetEase(Ease.OutBack);
            choiceText.text = "";
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
        choiceText.text = "";
    }
}