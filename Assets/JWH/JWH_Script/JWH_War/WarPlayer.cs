using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarPlayer : MonoBehaviour
{
    [SerializeField] WarController controller;
    [SerializeField] int hp = 5; // 임시값
    [SerializeField] int shield = 0; // 쉴드 추가

    [Header("스킬 & 버프")]
    public string equippedSkillID; //장착스킬
    [System.NonSerialized] public SkillData currentSkill; // 스킬 데이터 런타임
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

    public void Heal(int amount)
    {
        hp = Mathf.Clamp(hp + amount, 0, hp);
        Debug.Log($"Player HP +{amount} => {hp}");
    }

    public void KillByRingOut()
    {
        if (hp <= 0) return;
        hp = 0;
        Debug.Log("플레이어 링아웃");
    }
    public void ResetStatus(int hpInit = 5, int shieldInit = 0)//포기화용
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