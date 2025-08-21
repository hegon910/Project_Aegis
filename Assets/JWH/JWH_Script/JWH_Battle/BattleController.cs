using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BattleAction { None, Attack, Defend }

public class BattleController : MonoBehaviour
{
    [Header("전장 참조/인덱스")]
    [SerializeField] BattleGround ground;
    [SerializeField] int currentIndex = 0;

    [Header("이동 설정")]
    [SerializeField] float moveSpeed = 5f;

    [Header("행동 파라미터")]
    [SerializeField] int attackStep = 4;   // 전진 칸 수
    [SerializeField] int defendStep = 1;   // 후퇴 칸 수
    [SerializeField] int direction = 1;    // 플레이어:+1, 적:-1

    bool isMoving = false;
    Vector3 targetPos;

    public bool IsBusy => isMoving;

    void Start()
    {
        if (!ground) ground = FindObjectOfType<BattleGround>();
        if (ground) transform.position = ground.GetGroundPos(currentIndex);
    }

    public void DoAction(BattleAction action)
    {
        if (isMoving || !ground) return;

        switch (action)
        {
            case BattleAction.Attack:
                TryMoveBy(direction * attackStep);
                break;
            case BattleAction.Defend:
                TryMoveBy(-direction * defendStep);
                break;
        }
    }

    void TryMoveBy(int step)
    {
        if (isMoving) return;

        int nextIndex = Mathf.Clamp(currentIndex + step, 0, ground.LaneLength - 1);
        if (nextIndex == currentIndex) return;

        currentIndex = nextIndex;
        targetPos = ground.GetGroundPos(currentIndex);
        StartCoroutine(MoveToTarget());
    }

    IEnumerator MoveToTarget()
    {
        isMoving = true;
        while (Vector3.Distance(transform.position, targetPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPos;
        isMoving = false;
    }
}