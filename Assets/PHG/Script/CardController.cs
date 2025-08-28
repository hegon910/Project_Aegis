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

    [Header("�������� ����")]
    [SerializeField] private float maxSwipeDistance = 200f; // �ִ� �������� �Ÿ�
    [SerializeField] private float maxRotationAngle = 20f;  // �ִ� ȸ�� ����

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

        float clampedX = Mathf.Clamp(localPoint.x, -maxSwipeDistance, maxSwipeDistance);
        rectTransform.anchoredPosition = new Vector2(clampedX, initialPosition.y);


        distanceMoved = localPoint.x;

        float rotationMultiplier = 0.1f;
        float targetRotation = -distanceMoved * rotationMultiplier;
        float clampedRotation = Mathf.Clamp(targetRotation, -maxRotationAngle, maxRotationAngle);
        rectTransform.localEulerAngles = new Vector3(0, 0, clampedRotation);

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

        situationCardController?.UpdateChoicePreview("", Color.clear); // ���� ����� ���� ȣ��
        choiceHandler?.UpdateChoicePreview("", Color.clear);           // MainScenarioManager�� ���� ȣ��

        choiceHandler?.UpdateDimmer(0f);

        bool canChoose = true;
        if (choiceHandler is MainScenarioManager mainScenarioManager)
        {
            // 3. �´ٸ�, MainScenarioManager�� CanMakeChoice ������Ƽ�� ����
            //    Ÿ���� ������ Ȯ���Ͽ� ���� ���� ���θ� �����մϴ�.
            canChoose = mainScenarioManager.CanMakeChoice;
        }

        if (Mathf.Abs(distanceMoved) > 200f && canChoose)
        {
            choiceHandler.HandleChoice(distanceMoved > 0);
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
        isPreviewing = false;
        wasRightPreview = false;
    }
}