using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarTurnManager : MonoBehaviour
{
    [SerializeField] WarGround ground;
    [SerializeField] WarPlayer player;
    [SerializeField] WarEnemy enemy;

    [Header("Turn Settings")]
    [SerializeField] int maxTurns = 30;
    //[SerializeField] int pushback = 1;// 밀려나는 칸 수
    int currentTurn = 0;
    bool battleEnded = false;
    bool turnRunning;

    [Header("Character Initial Stats")]
    [SerializeField] int initialPlayerHP = 5;
    [SerializeField] int initialPlayerShield = 0;
    [SerializeField] int initialEnemyHP = 5;
    [SerializeField] int initialEnemyShield = 0;
    [SerializeField] int playerStartIndex = 0;
    public void OnClick_PlayerAttack() { if (!turnRunning && !IsBattleEnded) GoStartTurn(WarAction.Attack); }
    public void OnClick_PlayerDefend() { if (!turnRunning && !IsBattleEnded) GoStartTurn(WarAction.Defend); }

    public System.Action<string> OnBattleEnd;

    private int skillCooldownTimer = 0;
    public int GetSkillCooldown() => skillCooldownTimer;

    public void ResetForNewBattle(int newMaxTurns = 30)
    {
        maxTurns = newMaxTurns;
        currentTurn = 0;
        battleEnded = false;
        turnRunning = false;

        //// 플레이어 상태 및 위치 초기화
        //if (player != null)
        //{
        //    player.ResetStatus(initialPlayerHP, initialPlayerShield);
        //    if (player.Ctrl != null)
        //    {
        //        player.Ctrl.ForcePlace(playerStartIndex);
        //    }
        //}

        //// 적 상태 및 위치 초기화
        //if (enemy != null && ground != null)
        //{
        //    enemy.ResetStatus(initialEnemyHP, initialEnemyShield);
        //    if (enemy.Ctrl != null)
        //    {
        //        int enemyStartIndex = ground.LaneLength > 0 ? ground.LaneLength - 1 : 13;
        //        enemy.Ctrl.ForcePlace(enemyStartIndex);
        //    }
        //}

        Debug.Log("전투 및 캐릭터 상태 초기화 완료");
    }
    void GoStartTurn(WarAction playerAction)
    {
        if (battleEnded) return;
        if (currentTurn >= maxTurns)
        {
            Debug.Log($"턴 제한({maxTurns})에 도달 전투를 종료");
            return;
        }
        if (skillCooldownTimer > 0)
        {
            skillCooldownTimer--;
            Debug.Log($"스킬 쿨타임 감소. 남은 턴: {skillCooldownTimer}");
        }

        currentTurn++;
        Debug.Log($"Turn {currentTurn}/{maxTurns} 시작 - Player Action: {playerAction}");
        StartCoroutine(Co_Turn(playerAction));
    }

    void EndBattle(string resultLog)
    {
        if (battleEnded) return;
        battleEnded = true;
        Debug.Log(resultLog);
        Debug.Log("전투 종료");
        OnBattleEnd?.Invoke(resultLog);
    }

    void CheckWinLoseDrawAfterTurn()
    {
        
        if (player.IsDead && enemy.IsDead)
        {
            EndBattle("무승부 동시 전멸");
            return;
        }
        if (enemy.IsDead)
        {
            EndBattle("승리 적의 체력이 0");
            return;
        }
        if (player.IsDead)
        {
            EndBattle("패배 플레이어의 체력이 0");
            return;
        }

      //  GameManager.instance.GoToBattleResultPanel();
    }

    void CheckRingOutStatus()
    {
        if (player == null || enemy == null) return;

        int lastIndex = ground.LaneLength - 1; // 15

        // 플레이어 위치 확인
        if (player.Ctrl.CurrentIndex == 0 || player.Ctrl.CurrentIndex == lastIndex)
        {
            player.KillByRingOut();
            Debug.Log("플레이어 장외!");
        }

        // 적 위치 확인
        if (enemy.Ctrl.CurrentIndex == 0 || enemy.Ctrl.CurrentIndex == lastIndex)
        {
            enemy.KillByRingOut();
            Debug.Log("적 장외!");
        }
    }

    IEnumerator Co_Turn(WarAction playerAction)
    {
        turnRunning = true;

        var enemyAction = enemy.ChooseAction();

        player.Act(playerAction);
        enemy.Act(enemyAction);

        int last = ground.LaneLength - 1;

        while (player.IsBusy || enemy.IsBusy)
        {
            // 캐릭터의 RectTransform 컴포넌트에서 anchoredPosition을 가져와서 위치를 판단합니다.
            Vector2 pPos = player.GetComponent<RectTransform>().anchoredPosition;
            Vector2 ePos = enemy.GetComponent<RectTransform>().anchoredPosition;

            int pIdx = ground.GetGroundIndex(pPos);
            int eIdx = ground.GetGroundIndex(ePos);

            if (pIdx >= eIdx)
            {
                int meet = Mathf.Clamp(Mathf.RoundToInt((pIdx + eIdx) * 0.5f), 0, last);
                player.Ctrl.CrushResult(meet);
                enemy.Ctrl.CrushResult(meet);
                Debug.Log($"Turn {currentTurn}/{maxTurns} 종료");
                turnRunning = false;
                ApplyCollisionRules(playerAction, enemyAction, meet, last);
                CheckRingOutStatus(); // 장외 확인
                CheckWinLoseDrawAfterTurn();
                Debug.Log($"[위치] Player: {player.Ctrl.CurrentIndex} / Enemy: {enemy.Ctrl.CurrentIndex}");
                if (!battleEnded && currentTurn >= maxTurns)
                    EndBattle($"무승부 턴 제한 {maxTurns} 소진");
                yield break;
            }
            yield return null;

        }

        Debug.Log($"Turn {currentTurn}/{maxTurns} 종료(비충돌)");
        turnRunning = false;
        CheckRingOutStatus(); // 장외 확인
        CheckWinLoseDrawAfterTurn(); // 승패 확인
        if (!battleEnded && currentTurn >= maxTurns)
            EndBattle($"무승부  턴 제한 {maxTurns} 소진");
    }

    int IntendedBackIndex(WarController pushedCtrl, WarController pusherCtrl, int meet)
    {
        int totalPush = pusherCtrl.PushPower * pushedCtrl.PushResistance;
        return meet - (pushedCtrl.Direction * totalPush);
    }
    void ApplyCollisionRules(WarAction pAct, WarAction eAct, int meet, int last)
    {
        // 플레이어는 적에 의해, 적은 플레이어에 의해 밀려날 때의 예상 위치를 계산합니다.
        int pBackIntended = IntendedBackIndex(player.Ctrl, enemy.Ctrl, meet);
        int eBackIntended = IntendedBackIndex(enemy.Ctrl, player.Ctrl, meet);

        switch ((pAct, eAct))
        {
            case (WarAction.Attack, WarAction.Attack):
                Debug.Log("공격 vs 공격  서로 피해 1, 각자 뒤로 밀려남");
                player.TakeDamage(enemy.AttackPower);
                enemy.TakeDamage(player.AttackPower);
                SeparateBoth(meet, pBackIntended, eBackIntended, last);
                break;

            case (WarAction.Attack, WarAction.Defend):
                Debug.Log("공격 vs 수비  공격 무효, 플레이어만 뒤로 밀려남");
                // 플레이어만 적에 의해 밀려납니다.
                pBackIntended = IntendedBackIndex(player.Ctrl, enemy.Ctrl, meet);
                SeparatePlayerOnly(meet, pBackIntended, last);
                break;

            case (WarAction.Defend, WarAction.Attack):
                Debug.Log("수비 vs 공격  공격 무효, 적군만 뒤로 밀려남 (플레이어 쉴드 +1)");
                player.GainShield(1);
                // 적군만 플레이어에 의해 밀려납니다.
                eBackIntended = IntendedBackIndex(enemy.Ctrl, player.Ctrl, meet);
                SeparateEnemyOnly(meet, eBackIntended, last);
                break;

            case (WarAction.Defend, WarAction.Defend):
                Debug.Log("수비 vs 수비  서로 뒤로 밀려남");
                SeparateBoth(meet, pBackIntended, eBackIntended, last);
                break;
        }
    }

    void ResolveBackFor(bool isPlayer, WarController ctrl, int intended, int last)
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


    void SeparateBoth(int meet, int pBackIntended, int eBackIntended, int last)
    {
        // 겹치지 않도록 추가 보정 전, 우선 KO 여부 판단
        bool pOut = (pBackIntended < 0 || pBackIntended > last);
        bool eOut = (eBackIntended < 0 || eBackIntended > last);

        if (pOut) player.KillByRingOut();
        if (eOut) enemy.KillByRingOut();
        if (pOut || eOut) return;

        // 의도 위치가 같은 칸이면 한쪽을 추가 보정
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
        // 적은 접점 유지(겹침 방지 보정 유지)
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

    public void OnClick_PlayerSkill()
    {
        Debug.Log("[WarTurnManager] OnClick_PlayerSkill() 호출됨 (스와이프 UP)");

        if (turnRunning || IsBattleEnded || player.currentSkill == null || skillCooldownTimer > 0)
        {
            Debug.Log("[WarTurnManager] 스킬 사용 조건 불충족.");
            return;
        }
        SkillData usedSkill = player.currentSkill;
        Debug.Log($"[WarTurnManager] 모든 조건 통과. '{usedSkill.skillName}' 스킬 사용 시도.");
        player.UseSkill(enemy, this);
        skillCooldownTimer = usedSkill.cooldown;
        Debug.Log($"[WarTurnManager] 스킬 쿨타임 {skillCooldownTimer}턴으로 설정.");
        player.EquipSkill(null);
    }

    //외부로 턴 정보 넘길예정 아마 승패쪽에서
    public int CurrentTurn => currentTurn;
    public int MaxTurns => maxTurns;
    public bool IsBattleEnded => battleEnded || currentTurn >= maxTurns;
}