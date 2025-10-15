using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;


[RequireComponent(typeof(AudioSource))]
public class WarPlayer : MonoBehaviour
{
    [SerializeField] private WarController controller;
    private WarTurnManager turnManager;

    [Header("Shield System")]
    [SerializeField] public int maxShield = 3;
    [SerializeField] public int currentShield = 0;

    [Header("Skill & Buffs")]
    public string equippedSkillID;//스킬변경 건드리는 부분
    [System.NonSerialized] public SkillData currentSkill;
    public SkillDatabase skillDatabase;
    [System.NonSerialized] public bool AttackShieldBuff = false;
    [System.NonSerialized] public int enhancedAttackStacks = 0;
    [System.NonSerialized] public bool ThornsBuff = false;
    [System.NonSerialized] public bool KnockbackBuff = false;
    [System.NonSerialized] public bool GoGoBuff = false;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip shieldGainSound; 
    private AudioSource audioSource; 

    public WarController Ctrl => controller;
    public bool IsDead => controller != null && controller.CurrentHP <= 0;

    public int AttackPower => controller ? controller.AttackPower : 0;

    
    void Awake()
    {
        if (!controller) controller = GetComponent<WarController>();
        audioSource = GetComponent<AudioSource>();
		// PlayerPrefs에 저장된 스킬이 없으면 초기 기본(인스펙터) 스킬을 무시하여 무스킬 상태로 시작
		if (!PlayerPrefs.HasKey("EquippedSkillID"))
		{
			equippedSkillID = null;
			currentSkill = null;
		}
        LoadSkillFromID();
    }

    void OnEnable()
    {
        // 활성화 시마다 PlayerPrefs 기반으로 최신 스킬을 재적용
        LoadSkillFromID();
        turnManager = FindObjectOfType<WarTurnManager>();
    }


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
        
    }

    public void GainShield(int amount)
    {
        int previousShield = currentShield;
        currentShield = Mathf.Clamp(currentShield + amount, 0, maxShield);
        Debug.Log($"Player Shield +{amount} => {currentShield}");
        if (amount > 0 && shieldGainSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(shieldGainSound);
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
		// PlayerPrefs에 값이 있으면 항상 최우선으로 사용하여 씬 직렬화값을 덮어씁니다.
		if (PlayerPrefs.HasKey("EquippedSkillID"))
		{
			string fromPrefs = PlayerPrefs.GetString("EquippedSkillID");
			Debug.Log($"[스킬 디버그] LoadSkillFromID: PlayerPrefs EquippedSkillID='{fromPrefs}' (serialized='{equippedSkillID ?? "<null>"}')");
			equippedSkillID = fromPrefs;
		}

		// 숫자형 스킬 코드가 저장되어 있을 수 있으므로 에셋 ID로 정규화
		equippedSkillID = NormalizeSkillId(equippedSkillID);

        // skillDatabase가 비어 있으면 동적으로 탐색하여 자동 할당 (씬/리소스 어디든)
        if (skillDatabase == null)
        {
            var allDbs = Resources.FindObjectsOfTypeAll<SkillDatabase>();
            if (allDbs != null && allDbs.Length > 0)
            {
                skillDatabase = allDbs[0];
                Debug.Log("[스킬 디버그] LoadSkillFromID: skillDatabase 자동 할당 성공");
            }
        }

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
			if (skillDatabase == null)
			{
				Debug.LogWarning("[스킬 디버그] LoadSkillFromID: skillDatabase가 비어 있습니다.");
			}
			currentSkill = null;
        }
    }

	private static readonly System.Collections.Generic.Dictionary<string, string> SkillCodeMap = new System.Collections.Generic.Dictionary<string, string>
	{
		{"2000001", "FullCondition"},
		{"2000002", "ForwardStrike"},
		{"2000003", "Stimpack"},
		{"2000004", "AmmoReinforce"},
		{"2000005", "CounterAttack"},
		{"2000006", "SupFormation"},
		{"2000007", "Overdrive"},
		{"2000008", "NightAttack"},
		{"2000009", "Sacrifice"},
		{"2000010", "MoraleBoost"},
	};

	private string NormalizeSkillId(string id)
	{
		if (string.IsNullOrEmpty(id)) return id;
		if (SkillCodeMap.TryGetValue(id, out var mapped)) return mapped;
		return id;
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