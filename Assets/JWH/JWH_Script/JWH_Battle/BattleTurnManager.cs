using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleTurnManager : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] BattleController playerCtrl;
    [SerializeField] BattleEnemy enemyAI;

    [Header("선택 후 적 행동 지연(연출용)")]
    [SerializeField] float enemyDelay = 0.2f;

    public void OnClick_PlayerAttack()
    {
        StartCoroutine(HandleTurn(BattleAction.Attack));
    }

    public void OnClick_PlayerDefend()
    {
        StartCoroutine(HandleTurn(BattleAction.Defend));
    }

    private IEnumerator HandleTurn(BattleAction playerAction)
    {
        // 플레이어 행동
        playerCtrl.DoAction(playerAction);
        yield return new WaitUntil(() => !playerCtrl.IsBusy);

        // 적 행동 선택 약간의 연출 지연
        yield return new WaitForSeconds(enemyDelay);
        BattleAction enemyAction = enemyAI.ChooseAction50();
        enemyAI.Act(enemyAction);
        yield return new WaitUntil(() => !enemyAI.IsBusy);

        // 라운드 결과 처리
        // TODO: Resolve(playerAction, enemyAction);
    }
}
