using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ChoiceCardSwipe : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Refs")]
    [SerializeField] RectTransform card;          
    [SerializeField] Canvas canvas;               
    [SerializeField] float followLerp = 20f;      // �巡�� �� ������� �ӵ�

    [Header("Thresholds")]
    [SerializeField] float minSwipeDistance = 140f;   // �������� �Ÿ�
    [SerializeField] float verticalBias = 1.2f;       // ��/�¿� ���� ����

    [Header("Events")]
    public UnityEvent onSwipeLeft;   // ����
    public UnityEvent onSwipeRight;  // ���
    public UnityEvent onSwipeUp;     // ��ų

    Vector2 startPos;
    Vector2 dragDelta;
    bool dragging;

    void Reset()
    {
        card = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    void Awake()
    {
        if (!card) card = GetComponent<RectTransform>();
        if (!canvas) canvas = GetComponentInParent<Canvas>();
        startPos = card.anchoredPosition;
    }

    public void OnBeginDrag(PointerEventData e)
    {
        dragging = true;
        dragDelta = Vector2.zero;
    }

    public void OnDrag(PointerEventData e)
    {
        // ��ũ�� �̵�
        Vector2 delta;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            card.parent as RectTransform, e.position, e.pressEventCamera, out delta);

        dragDelta += e.delta / canvas.scaleFactor;
        var target = startPos + dragDelta;
        card.anchoredPosition = Vector2.Lerp(card.anchoredPosition, target, Time.deltaTime * followLerp);
        card.rotation = Quaternion.Euler(0, 0, Mathf.Clamp(-dragDelta.x * 0.05f, -10f, 10f)); // ��¦ ����̱�
    }

    public void OnEndDrag(PointerEventData e)
    {
        dragging = false;
        Vector2 v = dragDelta;
        var absX = Mathf.Abs(v.x);
        var absY = Mathf.Abs(v.y) * verticalBias;

        bool decided = false;
        if (absY > absX && v.y > minSwipeDistance)         // ��: ��ų
        {
            onSwipeUp?.Invoke();
            decided = true;
        }
        else if (absX >= absY && v.x <= -minSwipeDistance)  // ��: ����
        {
            onSwipeLeft?.Invoke();
            decided = true;
        }
        else if (absX >= absY && v.x >= minSwipeDistance)  // ��: ���
        {
            onSwipeRight?.Invoke();
            decided = true;
        }

        
        if (decided) StartCoroutine(Co_FlyAndReset(v.normalized));
        else ResetCard();
    }

    System.Collections.IEnumerator Co_FlyAndReset(Vector2 dir)
    {
        float t = 0f;
        Vector2 from = card.anchoredPosition;
        Vector2 to = from + dir * 400f;
        Quaternion rotFrom = card.rotation;
        Quaternion rotTo = Quaternion.Euler(0, 0, dir.x < 0 ? -20f : (dir.x > 0 ? 20f : 0f));

        while (t < 1f)
        {
            t += Time.deltaTime * 3f;
            card.anchoredPosition = Vector2.Lerp(from, to, t);
            card.rotation = Quaternion.Slerp(rotFrom, rotTo, t);
            yield return null;
        }
        ResetCard();
    }

    void ResetCard()
    {
        card.anchoredPosition = startPos;
        card.rotation = Quaternion.identity;
        dragDelta = Vector2.zero;
    }
}
