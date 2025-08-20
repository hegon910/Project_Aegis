using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BattleAction { None, Attack, Defend }

public class BattleController : MonoBehaviour
{
    [SerializeField] BattleMover mover;

    const int ATTACK = 4;  // ¾ÕÀ¸·Î 4Ä­
    const int DEFEND = 1;  // µÚ·Î 1Ä­

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