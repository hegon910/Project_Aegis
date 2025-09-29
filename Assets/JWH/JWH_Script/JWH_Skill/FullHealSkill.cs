using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "새로운 회복 스킬", menuName = "Skills/회복/풀회복")]
public class FullHealSkill : SkillData
{

    [Tooltip("획득할 보호막의 양")]
    public int shieldToGain = 3;
    public override void Activate(WarPlayer player, WarEnemy enemy, WarTurnManager turnManager)
    {
        Debug.Log($"{skillName} 사용! 플레이어의 체력을 모두 회복합니다.");
        player.Ctrl.CurrentHP = player.Ctrl.MaxHP;
        player.GainShield(shieldToGain);
    }
}
