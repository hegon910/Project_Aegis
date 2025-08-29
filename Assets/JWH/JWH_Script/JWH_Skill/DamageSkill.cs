using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "새로운 직접 피해 스킬", menuName = "Skills/직접피해/단일 대상")]
public class DirectDamageSkill : SkillData
{
    [Header("직접 피해 설정")]
    public int damage; // 피해량

    public override void Activate(WarPlayer player, WarEnemy enemy, WarTurnManager turnManager)
    {
        Debug.Log($"{skillName} 사용! 적에게 피해 {damage}를 줍니다.");
        enemy.TakeDamage(damage);
    }
}
