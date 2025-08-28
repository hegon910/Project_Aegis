using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarEnemy : MonoBehaviour
{
    [SerializeField] WarController controller;
    [SerializeField] int hp = 5; // �ӽð�
    [SerializeField] int shield = 0; // ���� �߰�, ������ ���� ȹ���ϴ� ��� ����
    const int MaxShield = 3;
    public int HP => hp;
    public bool IsDead => hp <= 0;
    public int Shield => shield; //
    public void ResetStatus(int hpInit = 5, int shieldInit = 0)
    {
        hp = Mathf.Max(0, hpInit);
        shield = Mathf.Clamp(shieldInit, 0, 3);
    }

    void Awake() 
    {
        if (!controller) controller = GetComponent<WarController>();
    }

    public void Act(WarAction action) => controller.DoAction(action);
    public bool IsBusy => controller != null && controller.IsBusy;

    public WarAction ChooseAction50()
        => (Random.value < 0.5f) ? WarAction.Attack : WarAction.Defend;

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

    public WarController Ctrl => controller;
    public Transform Tf => controller != null ? controller.transform : transform;
}
