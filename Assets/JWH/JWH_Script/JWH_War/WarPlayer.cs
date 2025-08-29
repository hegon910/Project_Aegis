using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarPlayer : MonoBehaviour
{
    [SerializeField] WarController controller;
    [SerializeField] int hp = 5; // �ӽð�
    [SerializeField] int shield = 0; // ���� �߰�

    [Header("��ų & ����")]
    public string equippedSkillID; //������ų
    [System.NonSerialized] public SkillData currentSkill; // ��ų ������ ��Ÿ��
    public SkillDatabase skillDatabase;

    const int MaxShield = 3;
    public int HP => hp;
    public int Shield => shield;
    public bool IsDead => hp <= 0;
    public WarController Ctrl => controller;
    public Transform Tf => controller != null ? controller.transform : transform;
   

    void Awake() { if (!controller) controller = GetComponent<WarController>(); }

    public void Act(WarAction action) => controller.DoAction(action);
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

    public void Heal(int amount)
    {
        hp = Mathf.Clamp(hp + amount, 0, hp);
        Debug.Log($"Player HP +{amount} => {hp}");
    }

    public void KillByRingOut()
    {
        if (hp <= 0) return;
        hp = 0;
        Debug.Log("�÷��̾� ���ƿ�");
    }
    public void ResetStatus(int hpInit = 5, int shieldInit = 0)//����ȭ��
    {
        hp = Mathf.Max(0, hpInit);
        shield = Mathf.Clamp(shieldInit, 0, 3);
    }

    public void EquipSkill(SkillData newSkill)
    {
        if (newSkill == null)
        {
            equippedSkillID = null;
            currentSkill = null;
            Debug.Log("��ų ����");
        }
        else
        {
            equippedSkillID = newSkill.skillID;
            currentSkill = newSkill;
            Debug.Log($"��ų ����: {newSkill.skillName}");
        }
    }

    public void LoadSkillFromID()
    {
        if (!string.IsNullOrEmpty(equippedSkillID) && skillDatabase != null)
        {
            currentSkill = skillDatabase.GetSkillByID(equippedSkillID);
            if (currentSkill != null)
            {
                Debug.Log($"��ų �ε� ����: {currentSkill.skillName} (ID: {equippedSkillID})");
            }
            else
            {
                Debug.LogError($"��ų �ε� ����! SkillDatabase�� ID '{equippedSkillID}'�� �����ϴ�.");
            }
        }
        else
        {
            currentSkill = null;
        }
    }

    public void UseSkill(WarEnemy enemy, WarTurnManager turnManager)
    {
        Debug.Log("UseSkill �Լ� ȣ���.");

        if (currentSkill == null)
        {
            Debug.LogWarning("currentSkill�� null�̶� ��ų�� ����� �� �����ϴ�.");
            return;
        }

        if (currentSkill.CanUse(this, enemy))
        {
            Debug.Log($" '{currentSkill.skillName}' ��ų ��� ���� ����. Activate ȣ��.");
            currentSkill.Activate(this, enemy, turnManager);
        }
        else
        {
            Debug.LogWarning($"'{currentSkill.skillName}' ��ų ��� ���� �Ҹ���.");
        }
    }
}