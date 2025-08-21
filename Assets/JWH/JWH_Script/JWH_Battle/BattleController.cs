using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BattleAction { None, Attack, Defend }

public class BattleController : MonoBehaviour
{
    [SerializeField] BattleMover mover;

    [Header("����/���� ����")]
    [SerializeField] int attackStep = 4;   // ����
    [SerializeField] int defendStep = 1;   // ����

    [Header("���� ����")]
    [SerializeField] int direction = 1;    // �÷��̾�: +1(������ ����), ��: -1(���� ����)

    void Awake()
    {
        if (!mover) mover = GetComponent<BattleMover>();
    }

    public void DoAction(BattleAction action)
    {
        if (mover == null || mover.IsMoving) return;

        switch (action)
        {
            case BattleAction.Attack:
                mover.TryMoveBy(direction * attackStep);
                break;
            case BattleAction.Defend:
                mover.TryMoveBy(-direction * defendStep);
                break;
        }
    }

    public bool IsBusy => mover != null && mover.IsMoving;//�ܺ�������Ƽ
}