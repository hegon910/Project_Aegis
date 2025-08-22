using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattlePlayer : MonoBehaviour
{
    [SerializeField] BattleController controller;
    [SerializeField] int hp = 5; // �ӽð�
    [SerializeField] int shield = 0; // ���� �߰�
    const int MaxShield = 3;

    void Awake() { if (!controller) controller = GetComponent<BattleController>(); }

    public void Act(BattleAction action) => controller.DoAction(action);
    public bool IsBusy => controller != null && controller.IsBusy;

    public void TakeDamage(int amount)//�ӽð�, ���� �� ü�� ������
    {
        int fromShield = Mathf.Min(shield, amount);
        shield -= fromShield;
        int remain = amount - fromShield;
        if (remain > 0) hp = Mathf.Max(0, hp - remain);

        Debug.Log($"Player HP -> {hp}, Shield -> {shield}");
    }

    public void GainShield(int amount)
    {
        shield = Mathf.Clamp(shield + amount, 0, MaxShield);
        Debug.Log($"Player Shield +{amount} => {shield}");
    }
    public BattleController Ctrl => controller;
    public Transform Tf => controller != null ? controller.transform : transform;
}