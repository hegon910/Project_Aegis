using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CollisionOutcome
{
    [Tooltip("플레이어가 입는 데미지")]
    public int playerDamage = 0;
    [Tooltip("플레이어가 밀려나는 칸 수 (뒤로 밀림)")]
    public int playerKnockback = 0;

    [Tooltip("적이 입는 기본 피해량 (플레이어 공격력에 더해짐)")]
    public int enemyBaseDamage = 0;
    [Tooltip("적이 밀려나는 칸 수 (뒤로 밀림)")]
    public int enemyKnockback = 0;
}

public class CustomEnemy : WarEnemy
{
    [Header("충돌 결과 설정 가능")]
    [Tooltip("플레이어: 공격 / 적: 공격")]
    [SerializeField] private CollisionOutcome attackVsAttack;

    [Tooltip("플레이어: 공격 / 적: 방어")]
    [SerializeField] private CollisionOutcome playerAttackVsEnemyDefend;

    [Tooltip("플레이어: 방어 / 적: 공격")]
    [SerializeField] private CollisionOutcome playerDefendVsEnemyAttack;

    

    // 기존 로직을 데이터 기반으로 변경 나는 천재야
    public override void HandleCollision(WarPlayer player, WarAction playerAction, WarAction myAction)
    {
        CollisionOutcome outcome = null;

        switch ((playerAction, myAction))
        {
            case (WarAction.Attack, WarAction.Attack):
                Debug.Log("충돌: 공격 vs 공격");
                outcome = attackVsAttack;
                break;

            case (WarAction.Attack, WarAction.Defend):
                Debug.Log("충돌: 플레이어 공격 vs 자신 방어");
                outcome = playerAttackVsEnemyDefend;
                break;

            case (WarAction.Defend, WarAction.Attack):
                Debug.Log("충돌: 플레이어 방어 vs 자신 공격");
                outcome = playerDefendVsEnemyAttack;

                player.GainShield(1);// 내가 플레이어한테 쉴드 얻는걸 줬던거 같은데 맞나?
                break;

            
        }

        // 결정된 결과를 적용
        if (outcome != null)
        {
            ApplyOutcome(player, outcome);
        }
    }

    // 결과를 실제로 적용하는 함수
    private void ApplyOutcome(WarPlayer player, CollisionOutcome outcome)
    {
        int finalDamage = player.AttackPower + outcome.enemyBaseDamage;
        // 데미지 적용
        if (outcome.playerDamage > 0)
        {
            player.TakeDamage(outcome.playerDamage);
        }
        if (outcome.enemyBaseDamage > 0 || player.AttackPower > 0)
        {
            // 최종 데미지 = 플레이어의 현재 공격력 + 설정된 기본 피해량
            this.TakeDamage(finalDamage);
        }
        if (player.enhancedAttackStacks > 0)
        {
            Debug.Log($"강화 공격 효과 발동! 추가 피해 1을 줍니다. (남은 횟수: {player.enhancedAttackStacks - 1})");
            finalDamage += 1; // 추가 피해 1 적용
            player.enhancedAttackStacks--; // 스택 1 감소
        }
        //플레이어의 공격력만큼만 피해: 0
        //플레이어 공격력 +1의 피해: 1
        //플레이어 공격력보다 1 낮은 피해: -1

        // 밀림적용
        // 일단은 만들었는데 이게 필요한가? 플레이어는 얼만큼 밀리는지 기억이 안난다
        if (outcome.playerKnockback > 0)
        {
            player.Ctrl.CrushResult(player.Ctrl.CurrentIndex - outcome.playerKnockback);
        }
        if (outcome.enemyKnockback > 0)
        {
            this.Ctrl.CrushResult(this.Ctrl.CurrentIndex + outcome.enemyKnockback);
        }
    }
}