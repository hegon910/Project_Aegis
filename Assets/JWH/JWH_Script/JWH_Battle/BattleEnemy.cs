using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleEnemy : MonoBehaviour
{
    [SerializeField] BattleController enemyController;

    void Awake()
    {
        if (!enemyController) enemyController = GetComponent<BattleController>();
    }

    public BattleAction ChooseAction50()
    {
        return (Random.value < 0.5f) ? BattleAction.Attack : BattleAction.Defend;
    }

    public void Act(BattleAction action)
    {
        if (enemyController != null)
            enemyController.DoAction(action);
    }

    public bool IsBusy => enemyController != null && enemyController.IsBusy;
}
