using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BattleAction { None, Attack, Defend }

public class BattleController : MonoBehaviour
{
    [SerializeField] BattleMover mover;

    [Header("전진/후퇴 스텝")]
    [SerializeField] int attackStep = 4;   // 전진
    [SerializeField] int defendStep = 1;   // 후퇴

    [Header("방향 설정")]
    [SerializeField] int direction = 1;    // 플레이어: +1(오른쪽 전진), 적: -1(왼쪽 전진)

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

    public bool IsBusy => mover != null && mover.IsMoving;//외부프러퍼티
}