using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleTurnManager : MonoBehaviour
{
    [SerializeField] BattleGround ground;
    [SerializeField] BattlePlayer player;
    [SerializeField] BattleEnemy enemy;

    [Header("Turn Settings")]
    [SerializeField] int maxTurns = 30;
    int currentTurn = 0;
    bool battleEnded = false;
    bool turnRunning;

    public void OnClick_PlayerAttack() { if (!turnRunning && !IsBattleEnded) GoStartTurn(BattleAction.Attack); }
    public void OnClick_PlayerDefend() { if (!turnRunning && !IsBattleEnded) GoStartTurn(BattleAction.Defend); }

    void GoStartTurn(BattleAction playerAction)
    {
        if (battleEnded) return;
        if (currentTurn >= maxTurns)
        {
            Debug.Log($"�� ����({maxTurns})�� ���� ������ ����");
            return;
        }

        currentTurn++;
        Debug.Log($"Turn {currentTurn}/{maxTurns} ���� - Player Action: {playerAction}");
        StartCoroutine(Co_Turn(playerAction));
    }

    void EndBattle(string resultLog)
    {
        if (battleEnded) return;
        battleEnded = true;
        Debug.Log(resultLog);
        Debug.Log("���� ����");
    }

    void CheckWinLoseDrawAfterTurn()
    {
        
        if (player.IsDead && enemy.IsDead)
        {
            EndBattle("���º� ���� ����");
            return;
        }
        if (enemy.IsDead)
        {
            EndBattle("�¸� ���� ü���� 0");
            return;
        }
        if (player.IsDead)
        {
            EndBattle("�й� �÷��̾��� ü���� 0");
            return;
        }
    }

        IEnumerator Co_Turn(BattleAction playerAction)
        {
        turnRunning = true;

        // �� �ൿ�� ���� ����
        var enemyAction = enemy.ChooseAction50();

        // �� �� ���ÿ� �̵� ���� (�÷��̾� �Է� �״��)
        player.Act(playerAction);
        enemy.Act(enemyAction);

        // ��� ������ ���� ��� ó��
        int last = ground.LaneLength - 1;
        while (player.IsBusy || enemy.IsBusy)
        {
            int pIdx = ground.GetGroundIndex(player.Tf.position);
            int eIdx = ground.GetGroundIndex(enemy.Tf.position);

            if (pIdx >= eIdx) // ����
            {
                // �������� ����(���� ���� ���ؿ� ����)
                int meet = Mathf.Clamp(Mathf.RoundToInt((pIdx + eIdx) * 0.5f), 0, last);
                player.Ctrl.CrushResult(meet);
                enemy.Ctrl.CrushResult(meet);

                // �浹��Ģ
                ApplyCollisionRules(playerAction, enemyAction, meet, last);

                // ���а���
                CheckWinLoseDrawAfterTurn();
                Debug.Log($"Turn {currentTurn}/{maxTurns} ����");
                turnRunning = false;
                if (!battleEnded && currentTurn >= maxTurns)//���º� 30��
                    EndBattle($"���º� �� ���� {maxTurns} ����");

                yield break;
            }
            yield return null;
        }

        // �׳� ����
        Debug.Log($"Turn {currentTurn}/{maxTurns} ����(���浹)");
        turnRunning = false;
        CheckWinLoseDrawAfterTurn();
        if (!battleEnded && currentTurn >= maxTurns)
            EndBattle($"���º�  �� ���� {maxTurns} ����");
        }

    void ApplyCollisionRules(BattleAction pAct, BattleAction eAct, int meet, int last)
    {
        //ĭ ��� (�׻� �ּ� 1ĭ �̻� �������� ����)
        int pBack = SafeBackIndex(player.Ctrl, meet, last); // �÷��̾� �ڷ� 1
        int eBack = SafeBackIndex(enemy.Ctrl, meet, last); // �� �ڷ� 1

        switch ((pAct, eAct))
        {
            case (BattleAction.Attack, BattleAction.Attack):
                Debug.Log("���� vs ����  ���� ���� 1, ���� �ڷ� 1ĭ");
                player.TakeDamage(1);
                enemy.TakeDamage(1);
                SeparateBoth(meet, pBack, eBack, last);
                break;

            case (BattleAction.Attack, BattleAction.Defend):
                Debug.Log("���� vs ����  ���� ��ȿ, �÷��̾� �ڷ� 1ĭ");
                // ���� ��ȿ  �÷��̾ �ڷ� 1
                SeparatePlayerOnly(meet, pBack, last);
                break;

            case (BattleAction.Defend, BattleAction.Attack):
                Debug.Log("���� vs ����  ���� ��ȿ, ���� �ڷ� 1ĭ");
                // ���� ��ȿ  ���� �ڷ� 1, �÷��̾� ���� 1ȹ��
                player.GainShield(1);
                SeparateEnemyOnly(meet, eBack, last);
                break;

            case (BattleAction.Defend, BattleAction.Defend):
                Debug.Log("���� vs ����  ���� �ڷ� 1ĭ");
                // ���� �ڷ� 1
                SeparateBoth(meet, pBack, eBack, last);
                break;
        }
    }

    // �ڷ� 1ĭ(�����ڸ� ����)
    int SafeBackIndex(BattleController ctrl, int meet, int last)
    {
        int idx = meet - ctrl.Direction; // �ڴ� direction  1
        idx = Mathf.Clamp(idx, 0, last);
        return idx;
    }

    // ���� �ڷ�: �׻� 1ĭ �̻� ���������� ����
    void SeparateBoth(int meet, int pBack, int eBack, int last)
    {
        // Ȥ�� ���� ĭ�� �Ǹ� ������ �߰� ����
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
        // �÷��̾ �ڷ�, ���� ���� ����
        // ��, ���� ĭ�̸� ���� �� ĭ �������� �����Ͽ� ��ħ ����
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

    //�ܺη� �� ���� �ѱ濹�� �Ƹ� �����ʿ���
    public int CurrentTurn => currentTurn;
    public int MaxTurns => maxTurns;
    public bool IsBattleEnded => battleEnded || currentTurn >= maxTurns;
}