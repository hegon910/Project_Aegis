using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "새로운 비상 스킬", menuName = "Skills/회복/긴급 방어")]
public class EmergencyShieldSkill : SkillData
{
    [Header("스킬 설정")]
    [Tooltip("스킬 발동이 가능한 체력")]
    public int activationHpThreshold = 2;

    [Tooltip("획득할 보호막의 양")]
    public int shieldToGain = 2;

    public override void Activate(WarPlayer player, WarEnemy enemy, WarTurnManager turnManager)
    {
        Debug.Log($"{skillName} 사용! 보호막 {shieldToGain}을 획득합니다.");
        player.GainShield(shieldToGain);
    }

    public override bool CanUse(WarPlayer player, WarEnemy enemy)
    {
        // 플레이어의 현재 체력이 설정된 값(activationHpThreshold) 이하일 때만 true를 반환
        bool canUse = player.Ctrl.CurrentHP <= activationHpThreshold;

        if (!canUse)
        {
            Debug.LogWarning($"스킬 사용 실패: 현재 체력({player.Ctrl.CurrentHP})이 {activationHpThreshold}보다 많음");
        }

        return canUse;
    }
}