using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "새로운 밀치기 스킬", menuName = "Skills/버프/밀치기 강화")]
public class KnockbackAttackSkill : SkillData
{
    [Header("버프 설정")]
    public int extraKnockback = 1; // 추가로 밀어낼 칸 수

    public override void Activate(WarPlayer player, WarEnemy enemy, WarTurnManager turnManager)
    {
        Debug.Log($"{skillName} 사용! 다음 공격 시 적을 {extraKnockback}칸 추가로 밀어냅니다");
        player.KnockbackBuff = true;
    }

    public override bool CanUse(WarPlayer player, WarEnemy enemy)
    {
        return !player.KnockbackBuff;
    }
}