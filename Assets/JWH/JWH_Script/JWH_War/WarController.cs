using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WarAction { None, Attack, Defend }

public class WarController : MonoBehaviour
{
    [SerializeField] WarGround ground;
    [SerializeField] int currentIndex;
    [SerializeField] float moveSpeed = 500f;
    [SerializeField] int attackStep = 4;
    [SerializeField] int defendStep = 1;
    [SerializeField] int direction = +1;

    public int Direction => direction;
    public int AttackStep => attackStep;
    public int DefendStep => defendStep;
    public int CurrentIndex => currentIndex;

    RectTransform rectTransform;
    Coroutine moveRoutine;
    bool isMoving;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void ForcePlace(int tileIndex) => CrushResult(tileIndex);
    public void SetDirection(int dir) { direction = Mathf.Sign(dir) >= 0 ? +1 : -1; }

    void Start()
    {
        if (!ground) ground = FindObjectOfType<WarGround>();
        rectTransform.anchoredPosition = ground.GetGroundPos(currentIndex);
    }

    public void DoAction(WarAction action)
    {
        if (isMoving) return;
        int step = (action == WarAction.Attack) ? direction * attackStep : -direction * defendStep;
        int next = Mathf.Clamp(currentIndex + step, 0, ground.LaneLength - 1);
        if (next == currentIndex) return;

        currentIndex = next;
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(MoveTo(ground.GetGroundPos(currentIndex)));
    }

    public void CrushResult(int index)
    {
        index = Mathf.Clamp(index, 0, ground.LaneLength - 1);
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        currentIndex = index;
        rectTransform.anchoredPosition = ground.GetGroundPos(index);
        isMoving = false;
        moveRoutine = null;
    }

    IEnumerator MoveTo(Vector3 target)
    {
        isMoving = true;
        while (Vector3.Distance(rectTransform.anchoredPosition, target) > 0.01f)
        {
            rectTransform.anchoredPosition = Vector3.MoveTowards(rectTransform.anchoredPosition, target, moveSpeed * Time.deltaTime);
            yield return null;
        }
        rectTransform.anchoredPosition = target;
        isMoving = false;
    }

    public bool RingOut(WarAction action)
    {
        int step = (action == WarAction.Attack) ? direction * attackStep : -direction * defendStep;
        int desired = currentIndex + step;
        int last = ground.LaneLength - 1;
        return desired < 0 || desired > last;
    }

    public bool IsBusy => isMoving;
}