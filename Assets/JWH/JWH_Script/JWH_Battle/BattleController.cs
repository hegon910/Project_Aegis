using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BattleAction { None, Attack, Defend }

public class BattleController : MonoBehaviour
{
    [SerializeField] BattleMover mover;

    const int ATTACK = 4;  // ������ 4ĭ
    const int DEFEND = 1;  // �ڷ� 1ĭ

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
                mover.TryMoveBy(ATTACK);
                break;

            case BattleAction.Defend:
                mover.TryMoveBy(-DEFEND);
                break;
        }
    }
}