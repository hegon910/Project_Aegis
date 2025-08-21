using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BattleAction { None, Attack, Defend }

public class BattleController : MonoBehaviour
{
    [Header("���� ����/�ε���")]
    [SerializeField] BattleGround ground;
    [SerializeField] int currentIndex = 0;

    [Header("�̵� ����")]
    [SerializeField] float moveSpeed = 5f;

    [Header("�ൿ �Ķ����")]
    [SerializeField] int attackStep = 4;   // ���� ĭ ��
    [SerializeField] int defendStep = 1;   // ���� ĭ ��
    [SerializeField] int direction = 1;    // �÷��̾�:+1, ��:-1

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