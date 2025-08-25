using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleEnemy : MonoBehaviour
{
    [SerializeField] BattleController controller;
    [SerializeField] int hp = 5; // �ӽð�
    [SerializeField] int shield = 0; // ���� �߰�, ������ ���� ȹ���ϴ� ��� ����
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

    public void TakeDamage(int amount)//�ӽð�
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
        Debug.Log("���� ���ƿ�");
    }

    public BattleController Ctrl => controller;
    public Transform Tf => controller != null ? controller.transform : transform;
}
