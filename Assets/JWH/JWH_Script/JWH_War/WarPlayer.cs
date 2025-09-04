using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarPlayer : MonoBehaviour
{
    [SerializeField] private WarController controller;

    [Header("Shield System")]
    [SerializeField] private int maxShield = 3;           
    [SerializeField] private int currentShield = 0;       
    

    [Header("Skill & Buffs")]
    public string equippedSkillID;
    [System.NonSerialized] public SkillData currentSkill;
    public SkillDatabase skillDatabase;

    public int Shield => currentShield; 
    public WarController Ctrl => controller;
    public bool IsDead => controller != null && controller.CurrentHP <= 0;

    public int AttackPower => controller ? controller.AttackPower : 0;

    void Awake()
    {
        if (!controller) controller = GetComponent<WarController>();
        LoadSkillFromID();
    }

    public void Act(WarAction action) => controller.DoAction(action);
    public bool IsBusy => controller != null && controller.IsBusy;

    public void TakeDamage(int amount)
    {
        int fromShield = Mathf.Min(currentShield, amount);
        currentShield -= fromShield;
        int remain = amount - fromShield;

        if (remain > 0 && controller)
        {
            controller.TakeDamage(remain);
        }

        Debug.Log($"Player HP -> {controller.CurrentHP}, Shield -> {currentShield}");
    }

    public void GainShield(int amount)
    {
        currentShield = Mathf.Clamp(currentShield + amount, 0, maxShield);
        Debug.Log($"Player Shield +{amount} => {currentShield}");
    }
    

    // 현재 체력 변경해야 함
    //public void Heal(int amount)
    //{
    //    hp = Mathf.Clamp(hp + amount, 0, hp);
    //    Debug.Log($"Player HP +{amount} => {hp}");
    //}

    //public void KillByRingOut()
    //{
    //    if (hp <= 0) return;
    //    hp = 0;
    //    Debug.Log("플레이어 링아웃");
    //}
    //public void ResetStatus(int hpInit = 5, int shieldInit = 0)//포기화용
    //{
    //    hp = Mathf.Max(0, hpInit);
    //    shield = Mathf.Clamp(shieldInit, 0, 3);
    //}

    public void EquipSkill(SkillData newSkill)
    {
        if (newSkill == null)
        {
            equippedSkillID = null;
            currentSkill = null;
            Debug.Log("스킬 해제");
        }
        else
        {
            equippedSkillID = newSkill.skillID;
            currentSkill = newSkill;
            Debug.Log($"스킬 장착: {newSkill.skillName}");
        }
    }

    public void LoadSkillFromID()
    {
        if (!string.IsNullOrEmpty(equippedSkillID) && skillDatabase != null)
        {
            currentSkill = skillDatabase.GetSkillByID(equippedSkillID);
            if (currentSkill != null)
            {
                Debug.Log($"스킬 로드 성공: {currentSkill.skillName} (ID: {equippedSkillID})");
            }
            else
            {
                Debug.LogError($"스킬 로드 실패! SkillDatabase에 ID '{equippedSkillID}'가 없습니다.");
            }
        }
        else
        {
            currentSkill = null;
        }
    }

    public void UseSkill(WarEnemy enemy, WarTurnManager turnManager)
    {
        Debug.Log("UseSkill 함수 호출됨.");

        if (currentSkill == null)
        {
            Debug.LogWarning("currentSkill이 null이라 스킬을 사용할 수 없습니다.");
            return;
        }

        if (currentSkill.CanUse(this, enemy))
        {
            Debug.Log($" '{currentSkill.skillName}' 스킬 사용 조건 만족. Activate 호출.");
            currentSkill.Activate(this, enemy, turnManager);
        }
        else
        {
            Debug.LogWarning($"'{currentSkill.skillName}' 스킬 사용 조건 불만족.");
        }
    }
}