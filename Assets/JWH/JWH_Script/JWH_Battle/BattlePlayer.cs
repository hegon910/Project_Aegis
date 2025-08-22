using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattlePlayer : MonoBehaviour
{
    [SerializeField] BattleController controller;
    [SerializeField] int hp = 5; // 임시값

    void Awake() { if (!controller) controller = GetComponent<BattleController>(); }

    public void Act(BattleAction action) => controller.DoAction(action);
    public bool IsBusy => controller != null && controller.IsBusy;

    public void TakeDamage(int amount)//임시값
    {
        hp = Mathf.Max(0, hp - amount);
        Debug.Log($"Player HP -> {hp}");
    }
    public BattleController Ctrl => controller;
    public Transform Tf => controller != null ? controller.transform : transform;
}