using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "새로운 돌진 스킬", menuName = "Skills/버프/돌진 강화")]
public class GoGoSkill : SkillData
{
    [Header("버프 설정")]
    public int extraDistance = 4;

    public override void Activate(WarPlayer player, WarEnemy enemy, WarTurnManager turnManager)
    {
        Debug.Log($"{skillName} 사용! 다음 공격 시 {extraDistance}칸 더 전진합니다.");
        player.GoGoBuff = true;
    }

    public override bool CanUse(WarPlayer player, WarEnemy enemy)
    {
        return !player.GoGoBuff;
    }
}
