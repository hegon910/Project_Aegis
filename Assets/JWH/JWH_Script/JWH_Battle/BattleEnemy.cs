using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleEnemy : MonoBehaviour
{
    [SerializeField] BattleController controller;
    [SerializeField] int hp = 5; // �ӽð�

    void Awake() 
    {
        if (!controller) controller = GetComponent<BattleController>();
    }

    public void Act(BattleAction action) => controller.DoAction(action);
    public bool IsBusy => controller != null && controller.IsBusy;

    public BattleAction ChooseAction50()
        => (Random.value < 0.5f) ? BattleAction.Attack : BattleAction.Defend;

    public void TakeDamage(int amount)//�ӽð�
    {
        hp = Mathf.Max(0, hp - amount);
        Debug.Log($"Enemy HP -> {hp}");
    }

    public BattleController Ctrl => controller;
    public Transform Tf => controller != null ? controller.transform : transform;
}
