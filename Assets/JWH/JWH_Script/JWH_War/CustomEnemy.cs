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
                if (player.ThornsBuff)
                {
                    Debug.Log("수비 반격 버프 효과 발동! 적에게 피해 1을 줍니다.");
                    this.TakeDamage(1); // 적에게 1의 피해를 줌
                    player.ThornsBuff = false; // 버프는 즉시 제거
                }
                break;

            
        }

        // 결정된 결과를 적용
        if (outcome != null)
        {
            ApplyOutcome(player, outcome, playerAction);
        }
    }

    // 결과를 실제로 적용하는 함수
    private void ApplyOutcome(WarPlayer player, CollisionOutcome outcome, WarAction playerAction)
    {
        // 데미지 적용 로직 (기존과 동일)
        int finalDamage = player.AttackPower + outcome.enemyBaseDamage;
        finalDamage = Mathf.Max(0, finalDamage);
        if (player.enhancedAttackStacks > 0)
        {
            finalDamage += 1;
            player.enhancedAttackStacks--;
        }
        if (finalDamage > 0)
        {
            this.TakeDamage(finalDamage);
        }
        if (outcome.playerDamage > 0)
        {
            player.TakeDamage(outcome.playerDamage);
        }

        // 밀림 적용
        if (outcome.playerKnockback > 0)
        {
            player.Ctrl.CrushResult(player.Ctrl.CurrentIndex - outcome.playerKnockback);
        }
        int finalEnemyKnockback = outcome.enemyKnockback;
        if (playerAction == WarAction.Attack && player.KnockbackBuff)
        {
            Debug.Log("밀치기 강화 버프 효과 발동! 적을 1칸 더 밀어낸다");
            finalEnemyKnockback += 1; // 최종 밀치기 값에 1을 더함
            player.KnockbackBuff = false; // 버프 사용 후 제거
        }

        if (finalEnemyKnockback > 0)
        {
            this.Ctrl.CrushResult(this.Ctrl.CurrentIndex + finalEnemyKnockback);
        }
    }
}