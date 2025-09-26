using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "새로운 공격 강화 스킬", menuName = "Skills/버프/공격 강화")]
public class AttackBuffSkill : SkillData
{
    [Header("버프 설정")]
    public int attackStacks = 2; // 턴수

    public override void Activate(WarPlayer player, WarEnemy enemy, WarTurnManager turnManager)
    {
        Debug.Log($"{skillName} 사용! 다음 {attackStacks}회 공격이 추가 피해를 줍니다");
        player.enhancedAttackStacks = attackStacks;
    }

    //public override bool CanUse(WarPlayer player, WarEnemy enemy)//버프 스택이 남아있는지 확인하는거 이거 필요한가?
    //{
    //    return player.enhancedAttackStacks <= 0;
    //}
}
