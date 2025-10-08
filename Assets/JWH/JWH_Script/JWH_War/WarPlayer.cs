using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public class WarPlayer : MonoBehaviour
{
    [SerializeField] private WarController controller;

    [Header("Shield System")]
    [SerializeField] private int maxShield = 3;           
    [SerializeField] private int currentShield = 0;       
    

    [Header("Skill & Buffs")]
    public string equippedSkillID;//스킬변경 건드리는 부분
    [System.NonSerialized] public SkillData currentSkill;
    public SkillDatabase skillDatabase;
    [System.NonSerialized] public bool AttackShieldBuff = false;
    [System.NonSerialized] public int enhancedAttackStacks = 0;
    [System.NonSerialized] public bool ThornsBuff = false;
    [System.NonSerialized] public bool KnockbackBuff = false;
    [System.NonSerialized] public bool GoGoBuff = false;

    [Header("VFX")]
    public GameObject shieldOnEffectPrefab;
    private GameObject activeShieldEffect;
    public GameObject shieldOffEffectPrefab;
    //public AudioClip shieldOn;
    //public AudioClip shieldOff;


    public int Shield => currentShield; 
    public WarController Ctrl => controller;
    public bool IsDead => controller != null && controller.CurrentHP <= 0;

    public int AttackPower => controller ? controller.AttackPower : 0;

    void Awake()
    {
        if (!controller) controller = GetComponent<WarController>();
        LoadSkillFromID();
    }

    // 스킬사용 vfx
    //void Update()// 유니태스크로 처리 가능하지 않을까?
    //{

    //    if (currentShield > 0 && activeShieldEffect == null)
    //    {
    //        activeShieldEffect = Instantiate(shieldOnEffectPrefab, transform.position, Quaternion.identity);
    //        activeShieldEffect.transform.SetParent(this.transform);
    //    }

    //    else if (currentShield <= 0 && activeShieldEffect != null)
    //    {
    //        Destroy(activeShieldEffect);
    //        activeShieldEffect = null;
    //    }
    //}

    public void ResetState(WarGround ground, int startIndex)
    {
        currentShield = 0;
        AttackShieldBuff = false;
        enhancedAttackStacks = 0;
        ThornsBuff = false;
        KnockbackBuff = false;
        GoGoBuff = false;

        if (controller != null)
        {
            controller.ResetState(ground, startIndex);
        }
        Debug.Log("플레이어의 모든 버프와 실드가 초기화되었습니다.");
    }
    public async UniTask ActAsync(WarAction action)
    {
        int extraForward = 0;
        if (action == WarAction.Attack && GoGoBuff)
        {
            Debug.Log("돌진 버프 효과 발동! 4칸 더 전진합니다.");
            extraForward = 4;
            GoGoBuff = false;
        }
        await controller.DoActionAsync(action, extraForward);
    }

    public void TakeDamage(int amount)
    {
        //int shieldBeforeDamage = this.currentShield; // 피해 전 쉴드량 기억
        int fromShield = Mathf.Min(currentShield, amount);
        currentShield -= fromShield;
        int remain = amount - fromShield;

        if (remain > 0 && controller)
        {
            controller.TakeDamage(remain);
        }

        Debug.Log($"Player HP -> {controller.CurrentHP}, Shield -> {currentShield}");
        //this.currentShield -= damage; //뭘로 바꾸지


        //if (shieldBeforeDamage > 0 && this.currentShield <= 0)
        //{
        //    this.currentShield = 0; // 쉴드가 마이너스가 되지 않도록 보정
        //    if (shieldOffEffectPrefab != null)
        //    {
        //        Instantiate(shieldOffEffectPrefab, transform.position, Quaternion.identity);
        //    }
        //}
    }

    public void GainShield(int amount)
    {
        currentShield = Mathf.Clamp(currentShield + amount, 0, maxShield);
        Debug.Log($"Player Shield +{amount} => {currentShield}");
        if (shieldOnEffectPrefab != null)
        {
            // 플레이어의 위치에 프리팹 생성
            Instantiate(shieldOnEffectPrefab, transform.position, Quaternion.identity);
        }
    }

    public void KillByRingOut()
    {
        if (Ctrl.CurrentHP <= 0) return;
        Ctrl.CurrentHP = 0;
        Debug.Log("플레이어 링아웃");
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

    public virtual void UseSkill(WarEnemy enemy, WarTurnManager turnManager)// 턴매니저랑 뭔가 겹치는데 모르겠네
    {
        Debug.Log("UseSkill 함수 호출됨.");

        if (currentSkill == null)
        {
            Debug.LogWarning("currentSkill이 null이라 스킬을 사용할 수 없습니다.");
            return;
        }

        if (currentSkill.CanUse(this, enemy))
        {
            Debug.Log($" '{currentSkill.skillName}' 스킬 사용 조건 만족-useskill");
            currentSkill.Activate(this, enemy, turnManager);
        }
        else
        {
            Debug.LogWarning($"'{currentSkill.skillName}' 스킬 사용 조건 불만족");
        }
    }
}