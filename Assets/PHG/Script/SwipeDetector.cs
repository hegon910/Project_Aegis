using UnityEngine;

public class SwipeDetector : MonoBehaviour
{
    private Vector2 startTouchPosition;
    private Vector2 endTouchPosition;

    // �ּ� �������� �Ÿ� (�ʹ� ª�� �������� �����ϱ� ����)
    public float minSwipeDistance = 50f;

    void Update()
    {
        // ��ġ �Է��� �ִ��� Ȯ��
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            // ��ġ�� ���۵Ǵ� ����, ���� ��ġ�� ���
            if (touch.phase == TouchPhase.Began)
            {
                startTouchPosition = touch.position;
            }
            // ��ġ�� ������ ����, �� ��ġ�� ����ϰ� �������� ������ ���
            else if (touch.phase == TouchPhase.Ended)
            {
                endTouchPosition = touch.position;

                // ��ġ �������� ������ �Ÿ��� ���
                float swipeDistance = Vector2.Distance(startTouchPosition, endTouchPosition);

                // �Ÿ��� �ּ� �������� �Ÿ����� ��� ���������� ����
                if (swipeDistance > minSwipeDistance)
                {
                    DetectSwipeDirection();
                }
            }
        }
    }

    void DetectSwipeDirection()
    {
        // x�� �̵����� y�� �̵������� ũ�� �¿� ��������
        if (Mathf.Abs(endTouchPosition.x - startTouchPosition.x) > Mathf.Abs(endTouchPosition.y - startTouchPosition.y))
        {
            if (endTouchPosition.x > startTouchPosition.x)
            {
                Debug.Log("���������� ��������!");
                // ���⿡ '������ ����'�� ���� ������ ��������.
            }
            else
            {
                Debug.Log("�������� ��������!");
                // ���⿡ '���� ����'�� ���� ������ ��������.
            }
        }
        // y�� �̵����� x�� �̵������� ũ�� ���� �������� (Reigns������ �ַ� ��� ����)
        else
        {
            if (endTouchPosition.y > startTouchPosition.y)
            {
                Debug.Log("���� ��������!");
            }
            else
            {
                Debug.Log("�Ʒ��� ��������!");
            }
        }
    }
}