using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BattleAction { None, Attack, Defend }

public class BattleController : MonoBehaviour
{
    [SerializeField] BattleGround ground;
    [SerializeField] int currentIndex;
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] int attackStep = 4;
    [SerializeField] int defendStep = 1;
    [SerializeField] int direction = +1; // Player=1, Enemy=-1
    public int Direction => direction;     // Player=1, Enemy=-1
    public int AttackStep => attackStep;   // 4
    public int DefendStep => defendStep;   // 1
    public int CurrentIndex => currentIndex;

    bool isMoving;
    Coroutine moveRoutine;

    void Start()
    {
        if (!ground) ground = FindObjectOfType<BattleGround>();
        transform.position = ground.GetGroundPos(currentIndex);
    }

    public void DoAction(BattleAction action)
    {
        if (isMoving) return;
        int step = (action == BattleAction.Attack) ? direction * attackStep : -direction * defendStep;
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
        transform.position = ground.GetGroundPos(currentIndex);
        isMoving = false;
        moveRoutine = null;
    }

    IEnumerator MoveTo(Vector3 target)
    {
        isMoving = true;
        while (Vector3.Distance(transform.position, target) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = target;
        isMoving = false;
    }

    public bool RingOut(BattleAction action)
    {
        int step = (action == BattleAction.Attack) ? direction * attackStep : -direction * defendStep;
        int desired = currentIndex + step;
        int last = ground.LaneLength - 1;
        return desired < 0 || desired > last;   // 밖으로 나가면 링아웃
    }

    public bool IsBusy => isMoving;
}