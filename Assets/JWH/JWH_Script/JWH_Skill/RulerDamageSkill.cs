using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "새로운 근접 스킬", menuName = "Skills/직접피해/거리데미지")]
public class RulerDamageSkill : SkillData
{
    [Header("스킬 설정")]
    [Tooltip("스킬 발동이 가능한 최대 거리 (칸 수)")]
    public int activationDistance = 3; 

    [Tooltip("적에게 입힐 피해량")]
    public int damage = 3; 

    [Tooltip("자신이 입을 피해량")]
    public int selfDamage = 1; 

    public override void Activate(WarPlayer player, WarEnemy enemy, WarTurnManager turnManager)
    {
        Debug.Log($"{skillName} 사용! 자신의 체력을 {selfDamage} 소모 적에게 {damage}의 피해");

        player.TakeDamage(selfDamage); // 플레이어 스스로 피해를 입음
        enemy.TakeDamage(damage);      // 적에게 피해를 줌
    }

    public override bool CanUse(WarPlayer player, WarEnemy enemy)
    {
        int playerIndex = player.Ctrl.CurrentIndex;//플레이어와 적의 현재 위치 인덱스
        int enemyIndex = enemy.Ctrl.CurrentIndex;
        int distance = Mathf.Abs(playerIndex - enemyIndex);//두 인덱스 간의 거리를 계산
        bool canUse = distance <= activationDistance;//계산된 거리가 발동 가능한 최대 거리 이하인지 확인

        if (!canUse)
        {
            Debug.LogWarning($"스킬 사용 실패: 적과의 거리가({distance}) {activationDistance}보다 멀리 있습니다");
        }

        return canUse;
    }
}