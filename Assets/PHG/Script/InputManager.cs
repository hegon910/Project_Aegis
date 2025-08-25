using UnityEngine;
using System; // Action �̺�Ʈ�� ���� �ʿ�

public class InputManager : MonoBehaviour
{
    // �̱��� �ν��Ͻ�: ��𼭵� InputManager.Instance�� ���� ����
    public static InputManager Instance { get; private set; }

    // ����� �̺�Ʈ ���
    public static event Action<Vector2> OnClick;       // Ŭ������ �� (Ŭ�� ��ġ)
    public static event Action<Vector2> OnDragStart;   // �巡�� �������� �� (���� ��ġ)
    public static event Action<Vector2> OnDrag;        // �巡�� ���� �� (������ �Ÿ�)
    public static event Action OnDragEnd;       // �巡�� ������ ��

    [Tooltip("�� �Ÿ�(�ȼ�) �̻� �����̸� �巡�׷�, �̸��̸� Ŭ������ �ν��մϴ�.")]
    public float clickDragThreshold = 10f;

    private bool isPointerDown = false;
    private bool isDragging = false;
    private Vector2 startPos;
    private Vector2 lastPos;

    void Awake()
    {
        // �̱��� ����
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // ���� �ٲ� ����
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // ���콺 �Է� ó��
        if (Input.GetMouseButtonDown(0))
        {
            isPointerDown = true;
            isDragging = false;
            startPos = Input.mousePosition;
            lastPos = startPos;

            // �巡�� ���� �̺�Ʈ ���
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
                // �巡�� �̺�Ʈ ��� (������ �Ÿ� ����)
                OnDrag?.Invoke(delta);
            }
            lastPos = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0) && isPointerDown)
        {
            if (isDragging)
            {
                // �巡�� ���� �̺�Ʈ ���
                OnDragEnd?.Invoke();
            }
            else
            {
                // Ŭ�� �̺�Ʈ ��� (Ŭ�� ��ġ ����)
                OnClick?.Invoke(Input.mousePosition);
            }
            isPointerDown = false;
            isDragging = false;
        }
    }
}