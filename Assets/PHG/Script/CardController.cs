// CardController.cs

using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private Vector2 initialPosition;
    private float distanceMoved;

    [Header("참조")]
    // [복원] 기존 SituationCardController에 대한 참조를 되살립니다.
    public SituationCardController situationCardController;
    // [유지] MainScenarioManager와의 연결을 위한 참조는 그대로 유지합니다.
    public IChoiceHandler choiceHandler;

    [Header("스와이프 제한")]
    [SerializeField] private float maxSwipeDistance = 200f; // 최대 스와이프 거리
    [SerializeField] private float maxRotationAngle = 20f;  // 최대 회전 각도

    [Header("미리보기 UI 직접 참조")]
    // MainScenarioManager에 있던 미리보기 텍스트 UI를 CardController가 직접 제어합니다.
    public TextMeshProUGUI choicePreviewText;
    public Image choicePreviewImage;



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
            colorToShow = new Color(0.8f, 0.2f, 0.2f, alpha);
        }
        else if (distanceMoved < -threshold)
        {
            textToShow = leftChoiceTextString;
            float alpha = Mathf.InverseLerp(-threshold, -maxSwipe, distanceMoved);
            colorToShow = new Color(0.2f, 0.8f, 0.2f, alpha);
        }

        situationCardController?.UpdateChoicePreview(textToShow, colorToShow); // 기존 기능을 위한 호출
        if (choicePreviewText != null)
        {
            choicePreviewText.text = textToShow;
            choicePreviewText.color = colorToShow;
        }
        if (choicePreviewImage != null)
        {
            Color imageColor = choicePreviewImage.color;
            imageColor.a = colorToShow.a; // 계산된 alpha값을 이미지에 적용
            choicePreviewImage.color = imageColor;
        }
        choiceHandler?.UpdateChoicePreview(textToShow, colorToShow);
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
        if (choicePreviewText != null)
        {
            choicePreviewText.text = "";
        }
        if (choicePreviewImage != null)
        {
            Color imageColor = choicePreviewImage.color;
            imageColor.a = 0f;
            choicePreviewImage.color = imageColor;
        }
        situationCardController?.UpdateChoicePreview("", Color.clear); // 기존 기능을 위한 호출
        choiceHandler?.UpdateChoicePreview("", Color.clear);

        choiceHandler?.UpdateDimmer(0f);

        bool canChoose = true;
        if (choiceHandler is MainScenarioManager mainScenarioManager)
        {
            // 3. 맞다면, MainScenarioManager의 CanMakeChoice 프로퍼티를 통해
            //    타이핑 중인지 확인하여 선택 가능 여부를 결정합니다.
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