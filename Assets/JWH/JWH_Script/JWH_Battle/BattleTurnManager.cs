using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleTurnManager : MonoBehaviour
{
    [Header("����")]
    [SerializeField] BattleController playerCtrl;
    [SerializeField] BattleEnemy enemyAI;

    [Header("���� �� �� �ൿ ����(�����)")]
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
        // �÷��̾� �ൿ
        playerCtrl.DoAction(playerAction);
        yield return new WaitUntil(() => !playerCtrl.IsBusy);

        // �� �ൿ ���� �ణ�� ���� ����
        yield return new WaitForSeconds(enemyDelay);
        BattleAction enemyAction = enemyAI.ChooseAction50();
        enemyAI.Act(enemyAction);
        yield return new WaitUntil(() => !enemyAI.IsBusy);

        // ���� ��� ó��
        // TODO: Resolve(playerAction, enemyAction);
    }
}
