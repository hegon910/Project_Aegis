using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarTurnManager : MonoBehaviour
{
    [SerializeField] WarGround ground;
    [SerializeField] WarPlayer player;
    [SerializeField] WarEnemy enemy;

    [Header("UI References")]
    [SerializeField] private ChoiceCardSwipe choiceCard;

    [Header("Turn Settings")]
    [SerializeField] int maxTurns = 30;
    int currentTurn = 0;
    bool battleEnded = false;
    bool turnRunning;

    private HashSet<SkillData> usedSingleUseSkills = new HashSet<SkillData>();

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
        if (choiceCard != null)
        {
            choiceCard.SetInteractable(true);
        }
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
        if (player != null && player.currentSkill != null)
        {
            skillCooldownTimer = player.currentSkill.cooltime;
            Debug.Log($"전투 시작! '{player.currentSkill.skillName}' 스킬의 초기 쿨타임({skillCooldownTimer}턴)이 적용");
        }
    }


    public void ResetForNewBattle(int newMaxTurns = 30)
    {
        if (player != null)
        {
            player.ResetState(ground, playerStartIndex);
        }
        if (enemy != null)
        {
            enemy.Ctrl.ResetState(ground, enemyStartIndex); // WarEnemy는 별도 버프가 없으므로 컨트롤러만 초기화
        }

        if (player != null && player.currentSkill != null)
        {
            skillCooldownTimer = player.currentSkill.cooltime;
            Debug.Log($"전투 리셋! '{player.currentSkill.skillName}' 스킬의 초기 쿨타임({skillCooldownTimer}턴)이 적용됩니다.");
        }
        else
        {
            skillCooldownTimer = 0; // 스킬이 없는 경우 0으로 초기화
        }
    }
    void GoStartTurn(WarAction playerAction)
    {
        if (choiceCard != null)
        {
            choiceCard.SetInteractable(false);
        }
        
        if (currentTurn >= maxTurns)
        {
            Debug.Log($"턴 제한({maxTurns})에 도달 전투를 종료");
            return;
        }
        if (player.AttackShieldBuff)
        {
            if (playerAction == WarAction.Attack)
            {
                player.GainShield(1);
                Debug.Log("공격 강화 버프 효과 발동! 쉴드를 1 획득");
            }
            else
            {
                Debug.Log("공격을 선택하지 않아 버프가 소멸");
            }
            player.AttackShieldBuff = false;//버프 1턴 사용후 제거
        }
        if (skillCooldownTimer > 0)
        {
            skillCooldownTimer--;
            Debug.Log($"스킬 쿨타임 감소. 남은 턴: {skillCooldownTimer}");
        }

        currentTurn++;
        Debug.Log($"Turn {currentTurn}/{maxTurns} 시작 - Player Action: {playerAction}");
        StartCoroutine(Co_Turn(playerAction));
        if (battleEnded) return;
    }

    void EndBattle(string resultLog)
    {
        if (battleEnded) return;
        battleEnded = true;
        bool isWin = resultLog.Contains("승리");
        WarHistory.RecordWarResult(isWin); // 전투 결과를 기록 시스템에 저장
        var changes = new List<ParameterChange>//파라미터 관련 추가부분
        {
            new ParameterChange
            {
            parameterType = ParameterType.전황,
            valueChange = isWin ? +20 : -20
            }
        };
        PlayerStats.Instance.ApplyChanges(changes); // 전황 파라미터 변경 적용
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

        bool playerActsFirst;
        switch ((playerAction, enemyAction))
        {
            case (WarAction.Attack, WarAction.Defend):
                playerActsFirst = false;
                Debug.Log("행동 순서: 적 선공 (방어)");
                break;

            default:
                playerActsFirst = true;
                Debug.Log("행동 순서: 플레이어 선공");
                break;
        }

        if (playerActsFirst)
        {
            // 플레이어 이동 시작
            player.Act(playerAction);
            while (player.IsBusy)
            {
                if (player.Ctrl.CurrentIndex >= enemy.Ctrl.CurrentIndex)
                {
                    player.Ctrl.StopMovement(); // 충돌 시 즉시 멈춤
                    enemy.Ctrl.StopMovement();  // 상대도 멈춤
                    Debug.Log("이동 중 충돌! 플레이어 이동을 중단합니다.");
                    break;
                }
                yield return null; 
            }

            if (player.Ctrl.CurrentIndex < enemy.Ctrl.CurrentIndex)
            {
                enemy.Act(enemyAction);
                while (enemy.IsBusy)
                {
                    if (player.Ctrl.CurrentIndex >= enemy.Ctrl.CurrentIndex)
                    {
                        player.Ctrl.StopMovement();
                        enemy.Ctrl.StopMovement();
                        Debug.Log("이동 중 충돌! 적 이동을 중단합니다.");
                        break;
                    }
                    yield return null;
                }
            }
        }
        else // 적이 먼저 행동하는 경우
        {
            enemy.Act(enemyAction);
            while (enemy.IsBusy)
            {
                if (player.Ctrl.CurrentIndex >= enemy.Ctrl.CurrentIndex)
                {
                    player.Ctrl.StopMovement();
                    enemy.Ctrl.StopMovement();
                    Debug.Log("이동 중 충돌! 적 이동을 중단합니다.");
                    break;
                }
                yield return null;
            }

            if (player.Ctrl.CurrentIndex < enemy.Ctrl.CurrentIndex)
            {
                player.Act(playerAction);
                while (player.IsBusy)
                {
                    if (player.Ctrl.CurrentIndex >= enemy.Ctrl.CurrentIndex)
                    {
                        player.Ctrl.StopMovement();
                        enemy.Ctrl.StopMovement();
                        Debug.Log("이동 중 충돌! 플레이어 이동을 중단합니다.");
                        break;
                    }
                    yield return null;
                }
            }
        }
        int pIdx = player.Ctrl.CurrentIndex;
        int eIdx = enemy.Ctrl.CurrentIndex;

        if (pIdx >= eIdx)
        {
            int meet = Mathf.Clamp(Mathf.RoundToInt((pIdx + eIdx) * 0.5f), 0, ground.LaneLength - 1);
            player.Ctrl.CrushResult(meet);
            enemy.Ctrl.CrushResult(meet);

            yield return new WaitWhile(() => player.IsBusy || enemy.IsBusy);

            enemy.HandleCollision(player, playerAction, enemyAction);
        }

        Debug.Log($"Turn {currentTurn} End / Player Index: {player.Ctrl.CurrentIndex}, Enemy Index: {enemy.Ctrl.CurrentIndex}");
        CheckRingOutStatus();
        CheckWinLoseDrawAfterTurn();
        if (!battleEnded && currentTurn >= maxTurns) EndBattle("무승부 - 턴 제한 소진");
        if (choiceCard != null)
        {
            choiceCard.SetInteractable(true);
        }

        turnRunning = false;
    }

    public void OnClick_PlayerSkill()
    {
        Debug.Log("WarTurnManager OnClick_PlayerSkill() 호출됨 (스와이프 UP)");

        if (turnRunning || IsBattleEnded || player.currentSkill == null || skillCooldownTimer > 0)
        {
            if (turnRunning) Debug.LogWarning("WarTurnManager 턴이 진행 중이라 스킬을 사용할 수 없습니다");
            if (IsBattleEnded) Debug.LogWarning("WarTurnManager 전투가 종료되어 스킬을 사용할 수 없습니다");
            if (player.currentSkill == null) Debug.LogWarning("WarTurnManager 장착된 스킬이 없습니다");
            if (skillCooldownTimer > 0) Debug.LogWarning($"WarTurnManager 스킬 쿨타임이 {skillCooldownTimer}턴 남았습니다");
            return;
        }

        SkillData usedSkill = player.currentSkill;
        if (usedSkill.isSingleUsePerCombat && usedSingleUseSkills.Contains(usedSkill))
        {
            Debug.LogWarning($"'{usedSkill.skillName}' 스킬은 이번 전투에서 이미 사용했습니다");
            return;
        }
        Debug.Log($"WarTurnManager 모든 조건 통과. '{usedSkill.skillName}' 스킬 사용 시도");

        player.UseSkill(enemy, this);
        if (usedSkill.isSingleUsePerCombat)
        {
            usedSingleUseSkills.Add(usedSkill);
        }
        skillCooldownTimer = usedSkill.cooltime;
        Debug.Log($"WarTurnManager 스킬 쿨타임 {skillCooldownTimer}턴으로 설정");
    }

    public string GetSkillName()
    {
        if (player != null && player.currentSkill != null)
        {
            return player.currentSkill.skillName;
        }
        return null;
    }

    //외부로 턴 정보 넘길예정 아마 승패쪽에서
    public int CurrentTurn => currentTurn;
    public int MaxTurns => maxTurns;
    public bool IsBattleEnded => battleEnded || currentTurn >= maxTurns;
}