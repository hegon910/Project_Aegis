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
    int currentTurn = 0;
    bool battleEnded = false;
    bool turnRunning;

    [Header("Character Start Positions")]
    [SerializeField] int playerStartIndex = 6;
    [SerializeField] int enemyStartIndex = 9;

    public void OnClick_PlayerAttack() { if (!turnRunning && !IsBattleEnded) GoStartTurn(WarAction.Attack); }
    public void OnClick_PlayerDefend() { if (!turnRunning && !IsBattleEnded) GoStartTurn(WarAction.Defend); }

    public System.Action<string> OnBattleEnd;

    private int skillCooldownTimer = 0;
    public int GetSkillCooldown() => skillCooldownTimer;

    void Start()
    {
        // 컨트롤러 초기화
        if (ground != null && player != null && enemy != null)
        {
            player.Ctrl.Init(ground, playerStartIndex);
            enemy.Ctrl.Init(ground, enemyStartIndex);
        }
        else
        {
            Debug.LogError("WarTurnManager에 Ground, Player, 또는 Enemy가 할당되지 않아 초기화할 수 없습니다!");
        }
    }


    public void ResetForNewBattle(int newMaxTurns = 30)
    {
        maxTurns = newMaxTurns;
        currentTurn = 0;
        battleEnded = false;
        turnRunning = false;
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
        //if (player == null || enemy == null) return;

        //int lastIndex = ground.LaneLength - 1; // 15

        //// 플레이어 위치 확인
        //if (player.Ctrl.CurrentIndex == 0 || player.Ctrl.CurrentIndex == lastIndex)
        //{
        //    player.KillByRingOut();
        //    Debug.Log("플레이어 장외!");
        //}

        //// 적 위치 확인
        //if (enemy.Ctrl.CurrentIndex == 0 || enemy.Ctrl.CurrentIndex == lastIndex)
        //{
        //    enemy.KillByRingOut();
        //    Debug.Log("적 장외!");
        //}
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
            int pIdx = player.Ctrl.CurrentIndex;
            int eIdx = enemy.Ctrl.CurrentIndex;

            if (pIdx >= eIdx)
            {
                int meet = Mathf.Clamp(Mathf.RoundToInt((pIdx + eIdx) * 0.5f), 0, last);
                player.Ctrl.CrushResult(meet);
                enemy.Ctrl.CrushResult(meet);

                enemy.HandleCollision(player, playerAction, enemyAction);

                Debug.Log($"--- Turn {currentTurn} Collision --- Player Index: {player.Ctrl.CurrentIndex}, Enemy Index: {enemy.Ctrl.CurrentIndex}");

                CheckWinLoseDrawAfterTurn();
                if (!battleEnded && currentTurn >= maxTurns) EndBattle("무승부 - 턴 제한 소진");

                turnRunning = false;
                yield break;
            }
            yield return null;
        }

        Debug.Log($"Turn {currentTurn} End / Player Index: {player.Ctrl.CurrentIndex}, Enemy Index: {enemy.Ctrl.CurrentIndex}");

        CheckWinLoseDrawAfterTurn();
        if (!battleEnded && currentTurn >= maxTurns) EndBattle("무승부 - 턴 제한 소진");

        turnRunning = false;
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