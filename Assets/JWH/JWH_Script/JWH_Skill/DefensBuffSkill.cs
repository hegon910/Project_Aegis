using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "새로운 반격 스킬", menuName = "Skills/버프/수비 반격")]
public class DefenseBuffSkill : SkillData
{
    [Header("버프 설정")]
    public int damageOnSuccess = 1; // 수비 성공 시 줄 피해량

    public override void Activate(WarPlayer player, WarEnemy enemy, WarTurnManager turnManager)
    {
        Debug.Log($"{skillName} 사용! 다음 수비 성공 시 적에게 {damageOnSuccess}의 피해");
        player.ThornsBuff = true;
    }

    // 버프가 이미 활성화되어 있으면 스킬을 중복해서 사용할 수 없도록 제한
    public override bool CanUse(WarPlayer player, WarEnemy enemy)
    {
        return !player.ThornsBuff;
    }
}
