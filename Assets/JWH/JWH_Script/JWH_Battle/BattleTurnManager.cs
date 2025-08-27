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

    [Header("Character Initial Stats")]
    [SerializeField] int initialPlayerHP = 5;
    [SerializeField] int initialPlayerShield = 0;
    [SerializeField] int initialEnemyHP = 5;
    [SerializeField] int initialEnemyShield = 0;
    [SerializeField] int playerStartIndex = 0;
    public void OnClick_PlayerAttack() { if (!turnRunning && !IsBattleEnded) GoStartTurn(BattleAction.Attack); }
    public void OnClick_PlayerDefend() { if (!turnRunning && !IsBattleEnded) GoStartTurn(BattleAction.Defend); }

    public System.Action<string> OnBattleEnd; // ��� ����(���丮 ���Ϳ�)

    public void ResetForNewBattle(int newMaxTurns = 30)
    {
        maxTurns = newMaxTurns;
        currentTurn = 0;
        battleEnded = false;
        turnRunning = false;

        // �÷��̾� ���� �� ��ġ �ʱ�ȭ
        if (player != null)
        {
            player.ResetStatus(initialPlayerHP, initialPlayerShield);
            if (player.Ctrl != null)
            {
                player.Ctrl.ForcePlace(playerStartIndex);
            }
        }

        // �� ���� �� ��ġ �ʱ�ȭ
        if (enemy != null && ground != null)
        {
            enemy.ResetStatus(initialEnemyHP, initialEnemyShield);
            if (enemy.Ctrl != null)
            {
                int enemyStartIndex = ground.LaneLength > 0 ? ground.LaneLength - 1 : 13;
                enemy.Ctrl.ForcePlace(enemyStartIndex);
            }
        }

        Debug.Log("���� �� ĳ���� ���� �ʱ�ȭ �Ϸ�");
    }
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
        OnBattleEnd?.Invoke(resultLog);
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

      //  GameManager.instance.GoToBattleResultPanel();
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

    int IntendedBackIndex(BattleController ctrl, int meet) => meet - ctrl.Direction;
    void ApplyCollisionRules(BattleAction pAct, BattleAction eAct, int meet, int last)
    {
        int pBackIntended = IntendedBackIndex(player.Ctrl, meet);
        int eBackIntended = IntendedBackIndex(enemy.Ctrl, meet);

        switch ((pAct, eAct))
        {
            case (BattleAction.Attack, BattleAction.Attack):
                Debug.Log("���� vs ����  ���� ���� 1, ���� �ڷ� 1ĭ");
                player.TakeDamage(1);
                enemy.TakeDamage(1);
                SeparateBoth(meet, pBackIntended, eBackIntended, last);
                break;

            case (BattleAction.Attack, BattleAction.Defend):
                Debug.Log("���� vs ����  ���� ��ȿ, �÷��̾� �ڷ� 1ĭ");
                SeparatePlayerOnly(meet, pBackIntended, last);
                break;

            case (BattleAction.Defend, BattleAction.Attack):
                Debug.Log("���� vs ����  ���� ��ȿ, ���� �ڷ� 1ĭ (�÷��̾� ���� +1)");
                player.GainShield(1);
                SeparateEnemyOnly(meet, eBackIntended, last);
                break;

            case (BattleAction.Defend, BattleAction.Defend):
                Debug.Log("���� vs ����  ���� �ڷ� 1ĭ");
                SeparateBoth(meet, pBackIntended, eBackIntended, last);
                break;
        }
    }

    void ResolveBackFor(bool isPlayer, BattleController ctrl, int intended, int last)
    {
        if (intended < 0 || intended > last)
        {
            if (isPlayer) player.KillByRingOut();
            else enemy.KillByRingOut();
        }
        else
        {
            ctrl.CrushResult(intended);
        }
    }

    // �ڷ� 1ĭ(�����ڸ� ����)
    //int SafeBackIndex(BattleController ctrl, int meet, int last)
    //{
    //    int idx = meet - ctrl.Direction; // �ڴ� direction  1
    //    idx = Mathf.Clamp(idx, 0, last);
    //    return idx;
    //}

    // ���� �ڷ�: �׻� 1ĭ �̻� ���������� ����
    void SeparateBoth(int meet, int pBackIntended, int eBackIntended, int last)
    {
        // ��ġ�� �ʵ��� �߰� ���� ��, �켱 KO ���� �Ǵ�
        bool pOut = (pBackIntended < 0 || pBackIntended > last);
        bool eOut = (eBackIntended < 0 || eBackIntended > last);

        if (pOut) player.KillByRingOut();
        if (eOut) enemy.KillByRingOut();
        if (pOut || eOut) return;

        // �ǵ� ��ġ�� ���� ĭ�̸� ������ �߰� ����
        int pBack = pBackIntended;
        int eBack = eBackIntended;
        if (pBack == eBack)
        {
            if (meet == 0) eBack = Mathf.Min(meet + 1, last);
            else if (meet == last) pBack = Mathf.Max(meet - 1, 0);
            else eBack = Mathf.Min(meet + 1, last);
        }

        player.Ctrl.CrushResult(pBack);
        enemy.Ctrl.CrushResult(eBack);
    }

    void SeparatePlayerOnly(int meet, int pBackIntended, int last)
    {
        // ���� ���� ����(��ħ ���� ���� ����)
        if (pBackIntended == meet)
        {
            int eFwd = Mathf.Min(meet + 1, last);
            enemy.Ctrl.CrushResult(eFwd);
        }
        ResolveBackFor(true, player.Ctrl, pBackIntended, last);
    }

    void SeparateEnemyOnly(int meet, int eBackIntended, int last)
    {
        if (eBackIntended == meet)
        {
            int pFwd = Mathf.Max(meet - 1, 0);
            player.Ctrl.CrushResult(pFwd);
        }
        ResolveBackFor(false, enemy.Ctrl, eBackIntended, last);
    }

    //�ܺη� �� ���� �ѱ濹�� �Ƹ� �����ʿ���
    public int CurrentTurn => currentTurn;
    public int MaxTurns => maxTurns;
    public bool IsBattleEnded => battleEnded || currentTurn >= maxTurns;
}