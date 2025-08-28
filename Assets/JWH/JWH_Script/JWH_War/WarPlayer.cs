using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarPlayer : MonoBehaviour
{
    [SerializeField] WarController controller;
    [SerializeField] int hp = 5; // 임시값
    [SerializeField] int shield = 0; // 쉴드 추가
    const int MaxShield = 3;
    public int HP => hp;
    public int Shield => shield;
    public bool IsDead => hp <= 0;
    public void ResetStatus(int hpInit = 5, int shieldInit = 0)//포기화용
    {
        hp = Mathf.Max(0, hpInit);
        shield = Mathf.Clamp(shieldInit, 0, 3);
    }

    void Awake() { if (!controller) controller = GetComponent<WarController>(); }

    public void Act(WarAction action) => controller.DoAction(action);
    public bool IsBusy => controller != null && controller.IsBusy;

    public void TakeDamage(int amount)//임시값, 쉴드 → 체력 순으로
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

    public void KillByRingOut()
    {
        if (hp <= 0) return;
        hp = 0;
        Debug.Log("플레이어 링아웃");
    }

    public WarController Ctrl => controller;
    public Transform Tf => controller != null ? controller.transform : transform;
}