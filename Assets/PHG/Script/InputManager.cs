using UnityEngine;
using System; // Action 이벤트를 위해 필요

public class InputManager : MonoBehaviour
{
    // 싱글톤 인스턴스: 어디서든 InputManager.Instance로 접근 가능
    public static InputManager Instance { get; private set; }

    // 방송할 이벤트 목록
    public static event Action<Vector2> OnClick;       // 클릭했을 때 (클릭 위치)
    public static event Action<Vector2> OnDragStart;   // 드래그 시작했을 때 (시작 위치)
    public static event Action<Vector2> OnDrag;        // 드래그 중일 때 (움직인 거리)
    public static event Action OnDragEnd;       // 드래그 끝났을 때

    [Tooltip("이 거리(픽셀) 이상 움직이면 드래그로, 미만이면 클릭으로 인식합니다.")]
    public float clickDragThreshold = 10f;

    private bool isPointerDown = false;
    private bool isDragging = false;
    private Vector2 startPos;
    private Vector2 lastPos;

    void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 유지
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // 마우스 입력 처리
        if (Input.GetMouseButtonDown(0))
        {
            isPointerDown = true;
            isDragging = false;
            startPos = Input.mousePosition;
            lastPos = startPos;

            // 드래그 시작 이벤트 방송
            OnDragStart?.Invoke(startPos);
        }

        if (Input.GetMouseButton(0) && isPointerDown)
        {
            if (!isDragging)
            {
                if (Vector2.Distance(startPos, Input.mousePosition) > clickDragThreshold)
                {
                    isDragging = true;
                }
            }

            if (isDragging)
            {
                Vector2 delta = (Vector2)Input.mousePosition - lastPos;
                // 드래그 이벤트 방송 (움직인 거리 전달)
                OnDrag?.Invoke(delta);
            }
            lastPos = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0) && isPointerDown)
        {
            if (isDragging)
            {
                // 드래그 종료 이벤트 방송
                OnDragEnd?.Invoke();
            }
            else
            {
                // 클릭 이벤트 방송 (클릭 위치 전달)
                OnClick?.Invoke(Input.mousePosition);
            }
            isPointerDown = false;
            isDragging = false;
        }
    }
}