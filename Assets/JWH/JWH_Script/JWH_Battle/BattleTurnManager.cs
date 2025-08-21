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

        // 1) �� �ൿ�� ���� ����
        var enemyAction = enemy.ChooseAction50();

        // 2) �� �� ���ÿ� �̵� ���� (�÷��̾� �Է� �״��)
        player.Act(playerAction);
        enemy.Act(enemyAction);

        // 3) ��� ������ ���� & ��� ó��
        int last = ground.LaneLength - 1;
        while (player.IsBusy || enemy.IsBusy)
        {
            int pIdx = ground.GetGroundIndex(player.Tf.position);
            int eIdx = ground.GetGroundIndex(enemy.Tf.position);

            if (pIdx >= eIdx) // ����/����
            {
                // �������� ����(���� ���� ���ؿ� ����)
                int meet = Mathf.Clamp(Mathf.RoundToInt((pIdx + eIdx) * 0.5f), 0, last);
                player.Ctrl.CrushResult(meet);
                enemy.Ctrl.CrushResult(meet);

                // === 4���� �浹 ��Ģ ===
                ApplyCollisionRules(playerAction, enemyAction, meet, last);

                // (�浹 ó�� ��) �� ����
                turnRunning = false;
                yield break;
            }
            yield return null;
        }

        // 4) �� ���� �� ������� �׳� ����
        turnRunning = false;
    }

    void ApplyCollisionRules(BattleAction pAct, BattleAction eAct, int meet, int last)
    {
        // �и� ��� ĭ ��� (�׻� �ּ� 1ĭ �̻� �������� ����)
        int pBack = SafeBackIndex(player.Ctrl, meet, last); // �÷��̾� �ڷ� 1
        int eBack = SafeBackIndex(enemy.Ctrl, meet, last); // �� �ڷ� 1

        switch ((pAct, eAct))
        {
            case (BattleAction.Attack, BattleAction.Attack):
                Debug.Log("���� vs ���� �� ���� ���� 1, ���� �ڷ� 1ĭ");
                player.TakeDamage(1);
                enemy.TakeDamage(1);
                SeparateBoth(meet, pBack, eBack, last);
                break;

            case (BattleAction.Attack, BattleAction.Defend):
                Debug.Log("���� vs ���� �� ���� ��ȿ, �÷��̾� �ڷ� 1ĭ");
                // ���� ��ȿ �� �÷��̾ �ڷ� 1
                SeparatePlayerOnly(meet, pBack, last);
                break;

            case (BattleAction.Defend, BattleAction.Attack):
                Debug.Log("���� vs ���� �� ���� ��ȿ, ���� �ڷ� 1ĭ");
                // ���� ��ȿ �� ���� �ڷ� 1
                SeparateEnemyOnly(meet, eBack, last);
                break;

            case (BattleAction.Defend, BattleAction.Defend):
                Debug.Log("���� vs ���� �� ���� �ڷ� 1ĭ");
                // ���� �ڷ� 1
                SeparateBoth(meet, pBack, eBack, last);
                break;
        }
    }

    // �ڷ� 1ĭ(�����ڸ� ����)
    int SafeBackIndex(BattleController ctrl, int meet, int last)
    {
        int idx = meet - ctrl.Direction; // �ڴ� -direction * 1
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
}