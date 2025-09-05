using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "새로운 버프 스킬", menuName = "Skills/버프/쉴드")]
public class AttackShieldBuffSkill : SkillData
{
    [Header("버프 설정")]
    public int shieldToGain = 1;

    // 이 스킬은 사용 즉시 쉴드를 주는 게 아니라 버프를 걸어줍니다
    public override void Activate(WarPlayer player, WarEnemy enemy, WarTurnManager turnManager)
    {
        Debug.Log($"{skillName} 다음 공격 시 보호막 {shieldToGain}을 획득하는 버프");
        player.AttackShieldBuff = true;
    }

    public override bool CanUse(WarPlayer player, WarEnemy enemy)//쉴드 버프 스킬 중복 제한
    {
        return !player.AttackShieldBuff;
    }
}
