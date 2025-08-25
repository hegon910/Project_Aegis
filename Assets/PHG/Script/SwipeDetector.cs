using UnityEngine;

public class SwipeDetector : MonoBehaviour
{
    private Vector2 startTouchPosition;
    private Vector2 endTouchPosition;

    // 최소 스와이프 거리 (너무 짧은 움직임을 무시하기 위함)
    public float minSwipeDistance = 50f;

    void Update()
    {
        // 터치 입력이 있는지 확인
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            // 터치가 시작되는 순간, 시작 위치를 기록
            if (touch.phase == TouchPhase.Began)
            {
                startTouchPosition = touch.position;
            }
            // 터치가 끝나는 순간, 끝 위치를 기록하고 스와이프 방향을 계산
            else if (touch.phase == TouchPhase.Ended)
            {
                endTouchPosition = touch.position;

                // 터치 시작점과 끝점의 거리를 계산
                float swipeDistance = Vector2.Distance(startTouchPosition, endTouchPosition);

                // 거리가 최소 스와이프 거리보다 길면 스와이프로 판정
                if (swipeDistance > minSwipeDistance)
                {
                    DetectSwipeDirection();
                }
            }
        }
    }

    void DetectSwipeDirection()
    {
        // x축 이동량이 y축 이동량보다 크면 좌우 스와이프
        if (Mathf.Abs(endTouchPosition.x - startTouchPosition.x) > Mathf.Abs(endTouchPosition.y - startTouchPosition.y))
        {
            if (endTouchPosition.x > startTouchPosition.x)
            {
                Debug.Log("오른쪽으로 스와이프!");
                // 여기에 '오른쪽 선택'에 대한 로직을 넣으세요.
            }
            else
            {
                Debug.Log("왼쪽으로 스와이프!");
                // 여기에 '왼쪽 선택'에 대한 로직을 넣으세요.
            }
        }
        // y축 이동량이 x축 이동량보다 크면 상하 스와이프 (Reigns에서는 주로 사용 안함)
        else
        {
            if (endTouchPosition.y > startTouchPosition.y)
            {
                Debug.Log("위로 스와이프!");
            }
            else
            {
                Debug.Log("아래로 스와이프!");
            }
        }
    }
}