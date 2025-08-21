using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleTurnManager : MonoBehaviour
{
    [SerializeField] BattleGround ground;
    [SerializeField] BattlePlayer player;
    [SerializeField] BattleEnemy enemy;

    bool turnRunning;

    public void OnClick_PlayerAttack() { if (!turnRunning) StartCoroutine(Co_Turn(BattleAction.Attack)); }
    public void OnClick_PlayerDefend() { if (!turnRunning) StartCoroutine(Co_Turn(BattleAction.Defend)); }

    IEnumerator Co_Turn(BattleAction playerAction)
    {
        turnRunning = true;

        // 1) 적 행동만 랜덤 결정
        var enemyAction = enemy.ChooseAction50();

        // 2) 둘 다 동시에 이동 시작 (플레이어 입력 그대로)
        player.Act(playerAction);
        enemy.Act(enemyAction);

        // 3) 닿는 프레임 감지 & 즉시 처리
        int last = ground.LaneLength - 1;
        while (player.IsBusy || enemy.IsBusy)
        {
            int pIdx = ground.GetGroundIndex(player.Tf.position);
            int eIdx = ground.GetGroundIndex(enemy.Tf.position);

            if (pIdx >= eIdx) // 접촉/교차
            {
                // 접점으로 스냅(둘을 같은 기준에 정렬)
                int meet = Mathf.Clamp(Mathf.RoundToInt((pIdx + eIdx) * 0.5f), 0, last);
                player.Ctrl.CrushResult(meet);
                enemy.Ctrl.CrushResult(meet);

                // === 4가지 충돌 규칙 ===
                ApplyCollisionRules(playerAction, enemyAction, meet, last);

                // (충돌 처리 끝) 턴 종료
                turnRunning = false;
                yield break;
            }
            yield return null;
        }

        // 4) 한 번도 안 닿았으면 그냥 종료
        turnRunning = false;
    }

    void ApplyCollisionRules(BattleAction pAct, BattleAction eAct, int meet, int last)
    {
        // 분리 대상 칸 계산 (항상 최소 1칸 이상 떨어지게 보정)
        int pBack = SafeBackIndex(player.Ctrl, meet, last); // 플레이어 뒤로 1
        int eBack = SafeBackIndex(enemy.Ctrl, meet, last); // 적 뒤로 1

        switch ((pAct, eAct))
        {
            case (BattleAction.Attack, BattleAction.Attack):
                Debug.Log("공격 vs 공격 → 서로 피해 1, 각자 뒤로 1칸");
                player.TakeDamage(1);
                enemy.TakeDamage(1);
                SeparateBoth(meet, pBack, eBack, last);
                break;

            case (BattleAction.Attack, BattleAction.Defend):
                Debug.Log("공격 vs 수비 → 공격 무효, 플레이어 뒤로 1칸");
                // 공격 무효 → 플레이어만 뒤로 1
                SeparatePlayerOnly(meet, pBack, last);
                break;

            case (BattleAction.Defend, BattleAction.Attack):
                Debug.Log("수비 vs 공격 → 공격 무효, 적군 뒤로 1칸");
                // 공격 무효 → 적만 뒤로 1
                SeparateEnemyOnly(meet, eBack, last);
                break;

            case (BattleAction.Defend, BattleAction.Defend):
                Debug.Log("수비 vs 수비 → 서로 뒤로 1칸");
                // 서로 뒤로 1
                SeparateBoth(meet, pBack, eBack, last);
                break;
        }
    }

    // 뒤로 1칸(가장자리 보정)
    int SafeBackIndex(BattleController ctrl, int meet, int last)
    {
        int idx = meet - ctrl.Direction; // 뒤는 -direction * 1
        idx = Mathf.Clamp(idx, 0, last);
        return idx;
    }

    // 서로 뒤로: 항상 1칸 이상 떨어지도록 보정
    void SeparateBoth(int meet, int pBack, int eBack, int last)
    {
        // 혹시 같은 칸이 되면 한쪽을 추가 보정
        if (pBack == eBack)
        {
            if (meet == 0) eBack = Mathf.Min(meet + 1, last);
            else if (meet == last) pBack = Mathf.Max(meet - 1, 0);
            else eBack = Mathf.Min(meet + 1, last);
        }
        player.Ctrl.CrushResult(pBack);
        enemy.Ctrl.CrushResult(eBack);
    }

    void SeparatePlayerOnly(int meet, int pBack, int last)
    {
        // 플레이어만 뒤로, 적은 접점 유지
        // 단, 같은 칸이면 적을 한 칸 전방으로 보정하여 겹침 방지
        if (pBack == meet)
        {
            int eFwd = Mathf.Min(meet + 1, last);
            enemy.Ctrl.CrushResult(eFwd);
        }
        player.Ctrl.CrushResult(pBack);
    }

    void SeparateEnemyOnly(int meet, int eBack, int last)
    {
        if (eBack == meet)
        {
            int pFwd = Mathf.Max(meet - 1, 0);
            player.Ctrl.CrushResult(pFwd);
        }
        enemy.Ctrl.CrushResult(eBack);
    }
}