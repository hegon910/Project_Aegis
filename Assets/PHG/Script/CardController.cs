using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

namespace PHG
{
    public class CardController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private RectTransform rectTransform; // �ڽ��� RectTransform
        private Vector2 initialPosition;     // UI ��ǥ�踦 ����ϱ� ���� Vector2�� ����
        private float distanceMoved;

        public TMP_Text choiceText;
        public UIFlowSimulator flowSimulator;

        void Awake()
        {
            // ������ �� �ڽ��� RectTransform ������Ʈ�� �̸� ã�ƵӴϴ�.
            rectTransform = GetComponent<RectTransform>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // anchoredPosition�� UI ��ǥ�迡���� ��ġ�Դϴ�.
            initialPosition = rectTransform.anchoredPosition;
        }

        public void OnDrag(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)rectTransform.parent,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint);

            // X�� �̵� �Ÿ� ���
            distanceMoved = localPoint.x - initialPosition.x;

            // 1. �Ÿ��� ����� ī�� ȸ����Ű��
            float rotationAngle = -distanceMoved * 0.1f; // 0.1f ���� ������ ȸ�� ������ �����ϼ���.
            rectTransform.localEulerAngles = new Vector3(0, 0, rotationAngle);

            // 2. �Ÿ��� ����� ī�� ��¦ ����߸��� (��ũ ȿ��)
            float dropAmount = Mathf.Abs(distanceMoved) * 0.2f; // 0.2f ���� ������ �������� ���̸� �����ϼ���.
            rectTransform.anchoredPosition = new Vector2(localPoint.x, initialPosition.y - dropAmount);

            // �ؽ�Ʈ ���� ���� (������ ����)
            if (distanceMoved > 200) { choiceText.text = "�����Ѵ�."; }
            else if (distanceMoved < -200) { choiceText.text = "�����Ѵ�."; }
            else { choiceText.text = ""; }
        }


        public void OnEndDrag(PointerEventData eventData)
        {
            if (Mathf.Abs(distanceMoved) > 200)
            {
                flowSimulator.ProceedToNext();

                // �θ� Canvas�� ���̿� �ʺ� �����ɴϴ�.
                RectTransform parentRect = (RectTransform)rectTransform.parent;
                float screenWidth = parentRect.rect.width;
                float screenHeight = parentRect.rect.height;

                // ��ǥ ��ġ ����: ���� X ��ġ���� ȭ�� �Ʒ��� ����������
                float targetY = -screenHeight * 0.7f; // ȭ�� ������ ���ݺ��� ���� �� �Ʒ�
                float targetRotation = -45f;

                if (distanceMoved > 0) // ������ ����
                {
                    Debug.Log("������ ���� Confirm");
                    // 1. ��ġ �ִϸ��̼�: ���� X ��ġ�� �����ϸ� �Ʒ��� ������
                    rectTransform.DOAnchorPosY(targetY, 0.5f).SetEase(Ease.InQuad);

                    // 2. ȸ�� �ִϸ��̼�: �������鼭 ȸ��
                    rectTransform.DORotate(new Vector3(0, 0, targetRotation), 0.5f).SetEase(Ease.InQuad)
                        .OnComplete(() => {
                            // �ִϸ��̼� �� ��ġ�� ȸ�� �ʱ�ȭ
                            rectTransform.anchoredPosition = initialPosition;
                            rectTransform.localEulerAngles = Vector3.zero;
                            choiceText.text = "";
                        });
                }
                else // ���� ����
                {
                    Debug.Log("���� ���� Reject");
                    // 1. ��ġ �ִϸ��̼�
                    rectTransform.DOAnchorPosY(targetY, 0.5f).SetEase(Ease.InQuad);

                    // 2. ȸ�� �ִϸ��̼�
                    rectTransform.DORotate(new Vector3(0, 0, -targetRotation), 0.5f).SetEase(Ease.InQuad)
                        .OnComplete(() => {
                            // �ִϸ��̼� �� ��ġ�� ȸ�� �ʱ�ȭ
                            rectTransform.anchoredPosition = initialPosition;
                            rectTransform.localEulerAngles = Vector3.zero;
                            choiceText.text = "";
                        });
                }
            }
            else
            {
                // ���������� ������� ������ ���� ��ġ�� ȸ������ �ǵ����ϴ�.
                rectTransform.DOAnchorPos(initialPosition, 0.3f).SetEase(Ease.OutBack);
                rectTransform.DORotate(Vector3.zero, 0.3f).SetEase(Ease.OutBack);
            }
        }
    }
}