using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AppearCard : MonoBehaviour
{
    [Header("카드")]
    [SerializeField] private RectTransform cardRect;

    [Header("활성 이미지")]
    [Tooltip("왼쪽이미지")]
    [SerializeField] private Image leftcardImage;
    [Tooltip("오른쪽이미지")]
    [SerializeField] private Image rightcardImage;

    [Header("거리설정")]
    [Tooltip("이미지가 활성화 전 거리")]
    [SerializeField] private float activationDistance = 150f;

    private Vector2 initialCardPosition;

    void Start()
    {
        if (cardRect == null)
        {
            cardRect = GetComponent<RectTransform>();
        }

        // 카드의 초기 위치
        initialCardPosition = cardRect.anchoredPosition;

        // 투명
        if (leftcardImage != null) SetIndicatorAlpha(leftcardImage, 0f);
        if (rightcardImage != null) SetIndicatorAlpha(rightcardImage, 0f);
    }

    void Update()
    {
        float currentX = cardRect.anchoredPosition.x;
        float deltaX = currentX - initialCardPosition.x;
        float ratio = Mathf.Abs(deltaX) / activationDistance;

        // 왼쪽 드래그
        if (deltaX < 0)
        {
            if (leftcardImage != null) SetIndicatorAlpha(leftcardImage, ratio);
            if (rightcardImage != null) SetIndicatorAlpha(rightcardImage, 0f); // 반대쪽은 투명하게
        }
        // 오른쪽 드래그
        else if (deltaX > 0)
        {
            if (rightcardImage != null) SetIndicatorAlpha(rightcardImage, ratio);
            if (leftcardImage != null) SetIndicatorAlpha(leftcardImage, 0f); // 반대쪽은 투명하게
        }
        else // 중앙
        {
            if (leftcardImage != null) SetIndicatorAlpha(leftcardImage, 0f);
            if (rightcardImage != null) SetIndicatorAlpha(rightcardImage, 0f);
        }
    }

    // 이미지의 투명ㄷ도
    private void SetIndicatorAlpha(Image image, float alpha)
    {
        Color color = image.color;
        color.a = Mathf.Clamp01(alpha); // 0과 1 사이로 제한
        image.color = color;
    }
}
