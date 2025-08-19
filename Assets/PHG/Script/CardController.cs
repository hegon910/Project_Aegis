using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

namespace PHG
{
    public class CardController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private RectTransform rectTransform; // 자신의 RectTransform
        private Vector2 initialPosition;     // UI 좌표계를 사용하기 위해 Vector2로 변경
        private float distanceMoved;

        public TMP_Text choiceText;
        public UIFlowSimulator flowSimulator;

        void Awake()
        {
            // 시작할 때 자신의 RectTransform 컴포넌트를 미리 찾아둡니다.
            rectTransform = GetComponent<RectTransform>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // anchoredPosition은 UI 좌표계에서의 위치입니다.
            initialPosition = rectTransform.anchoredPosition;
        }

        public void OnDrag(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)rectTransform.parent,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint);

            // X축 이동 거리 계산
            distanceMoved = localPoint.x - initialPosition.x;

            // 1. 거리에 비례해 카드 회전시키기
            float rotationAngle = -distanceMoved * 0.1f; // 0.1f 값을 조절해 회전 감도를 조절하세요.
            rectTransform.localEulerAngles = new Vector3(0, 0, rotationAngle);

            // 2. 거리에 비례해 카드 살짝 떨어뜨리기 (아크 효과)
            float dropAmount = Mathf.Abs(distanceMoved) * 0.2f; // 0.2f 값을 조절해 떨어지는 깊이를 조절하세요.
            rectTransform.anchoredPosition = new Vector2(localPoint.x, initialPosition.y - dropAmount);

            // 텍스트 변경 로직 (이전과 동일)
            if (distanceMoved > 200) { choiceText.text = "수락한다."; }
            else if (distanceMoved < -200) { choiceText.text = "거절한다."; }
            else { choiceText.text = ""; }
        }


        public void OnEndDrag(PointerEventData eventData)
        {
            if (Mathf.Abs(distanceMoved) > 200)
            {
                flowSimulator.ProceedToNext();

                // 부모 Canvas의 높이와 너비를 가져옵니다.
                RectTransform parentRect = (RectTransform)rectTransform.parent;
                float screenWidth = parentRect.rect.width;
                float screenHeight = parentRect.rect.height;

                // 목표 위치 설정: 현재 X 위치에서 화면 아래로 떨어지도록
                float targetY = -screenHeight * 0.7f; // 화면 높이의 절반보다 조금 더 아래
                float targetRotation = -45f;

                if (distanceMoved > 0) // 오른쪽 선택
                {
                    Debug.Log("오른쪽 선택 Confirm");
                    // 1. 위치 애니메이션: 현재 X 위치를 유지하며 아래로 떨어짐
                    rectTransform.DOAnchorPosY(targetY, 0.5f).SetEase(Ease.InQuad);

                    // 2. 회전 애니메이션: 떨어지면서 회전
                    rectTransform.DORotate(new Vector3(0, 0, targetRotation), 0.5f).SetEase(Ease.InQuad)
                        .OnComplete(() => {
                            // 애니메이션 후 위치와 회전 초기화
                            rectTransform.anchoredPosition = initialPosition;
                            rectTransform.localEulerAngles = Vector3.zero;
                            choiceText.text = "";
                        });
                }
                else // 왼쪽 선택
                {
                    Debug.Log("왼쪽 선택 Reject");
                    // 1. 위치 애니메이션
                    rectTransform.DOAnchorPosY(targetY, 0.5f).SetEase(Ease.InQuad);

                    // 2. 회전 애니메이션
                    rectTransform.DORotate(new Vector3(0, 0, -targetRotation), 0.5f).SetEase(Ease.InQuad)
                        .OnComplete(() => {
                            // 애니메이션 후 위치와 회전 초기화
                            rectTransform.anchoredPosition = initialPosition;
                            rectTransform.localEulerAngles = Vector3.zero;
                            choiceText.text = "";
                        });
                }
            }
            else
            {
                // 스와이프가 충분하지 않으면 원래 위치와 회전으로 되돌립니다.
                rectTransform.DOAnchorPos(initialPosition, 0.3f).SetEase(Ease.OutBack);
                rectTransform.DORotate(Vector3.zero, 0.3f).SetEase(Ease.OutBack);
            }
        }
    }
}