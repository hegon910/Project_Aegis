using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleEnemy : MonoBehaviour
{
    [SerializeField] BattleController controller;
    [SerializeField] int hp = 5; // 임시값
    [SerializeField] int shield = 0; // 쉴드 추가, 어차피 적은 획듯하는 경우 없음
    const int MaxShield = 3;
    public int HP => hp;
    public bool IsDead => hp <= 0;

    void Awake() 
    {
        if (!controller) controller = GetComponent<BattleController>();
    }

    public void Act(BattleAction action) => controller.DoAction(action);
    public bool IsBusy => controller != null && controller.IsBusy;

    public BattleAction ChooseAction50()
        => (Random.value < 0.5f) ? BattleAction.Attack : BattleAction.Defend;

    public void TakeDamage(int amount)//임시값
    {
        int fromShield = Mathf.Min(shield, amount);
        shield -= fromShield;
        int remain = amount - fromShield;
        if (remain > 0) hp = Mathf.Max(0, hp - remain);

        Debug.Log($"Enemy HP -> {hp}, Shield -> {shield}");
    }

    public void KillByRingOut()
    {
        if (hp <= 0) return;
        hp = 0;
        Debug.Log("적군 링아웃");
    }

    public BattleController Ctrl => controller;
    public Transform Tf => controller != null ? controller.transform : transform;
}
