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

    [Tooltip("적이 입는 데미지")]
    public int enemyDamage = 0;
    [Tooltip("적이 밀려나는 칸 수 (뒤로 밀림)")]
    public int enemyKnockback = 0;
}

public class ProtoEnemy : WarEnemy
{
    [Header("충돌 결과 설정 가능")]
    [Tooltip("플레이어: 공격 / 적: 공격")]
    [SerializeField] private CollisionOutcome attackVsAttack;

    [Tooltip("플레이어: 공격 / 적: 방어")]
    [SerializeField] private CollisionOutcome playerAttackVsEnemyDefend;

    [Tooltip("플레이어: 방어 / 적: 공격")]
    [SerializeField] private CollisionOutcome playerDefendVsEnemyAttack;

    [Tooltip("플레이어: 방어 / 적: 방어")]
    [SerializeField] private CollisionOutcome defendVsDefend;

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

            case (WarAction.Defend, WarAction.Defend):
                Debug.Log("충돌: 방어 vs 방어");
                outcome = defendVsDefend;
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
        // 데미지 적용
        if (outcome.playerDamage > 0)
        {
            player.TakeDamage(outcome.playerDamage);
        }
        if (outcome.enemyDamage > 0)
        {
            // 생각해보니 플레이어 한테 공격력 뺏어야 하는데 이게 있어도 되는건가?
            this.TakeDamage(outcome.enemyDamage);
        }

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