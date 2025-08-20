using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

public class CardController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private Vector2 initialPosition;
    private float distanceMoved;

    [Header("참조")]
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

        // ★★★ 수정된 부분: 두 기능을 모두 합쳤습니다. ★★★
        float threshold = 50f;

        // 1. (복구된 기능) 선택지 텍스트 표시
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

        // 2. (유지된 기능) 토글 미리보기
        if (distanceMoved > threshold) // 오른쪽으로 기울였을 때
        {
            if (!isPreviewing || !wasRightPreview)
            {
                flowSimulator.PreviewAffectedParameters(true);
                isPreviewing = true;
                wasRightPreview = true;
            }
        }
        else if (distanceMoved < -threshold) // 왼쪽으로 기울였을 때
        {
            if (!isPreviewing || wasRightPreview)
            {
                flowSimulator.PreviewAffectedParameters(false);
                isPreviewing = true;
                wasRightPreview = false;
            }
        }
        else // 중앙 근처로 돌아왔을 때
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
            // 이 부분이 이제 정상적으로 호출됩니다.
            flowSimulator.HandleChoice(distanceMoved > 0);
            AnimateCardOffscreen();
        }
        else
        {
            // 드래그가 충분하지 않을 때만 제자리로 돌아옵니다.
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