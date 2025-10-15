using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class WarTurnManager : MonoBehaviour
{
    [Header("Test Mode")]
    [SerializeField] private bool isTestMode = false;
    [Tooltip("테스트 모드에서 사용할 챕터 번호 (1~6)")]
    [SerializeField] private int testChapter = 1;
    public int CurrentChapter { get; private set; }

    [SerializeField] WarGround ground;
    [SerializeField] WarPlayer player;
    [Header("챕터별 적 설정")]
    [SerializeField] private List<ChapterEnemyPool> chapterEnemies;
    //[SerializeField] WarEnemy enemy;
    private WarEnemy enemy;
    public WarEnemy CurrentEnemy => enemy;


    [Header("UI References")]
    //[SerializeField] private Canvas mainCanvas;// UI 들어가는 캔버스
    public Camera particleCamera;
    [SerializeField] private Canvas fxCanvas;// 특수효과 캔버스
    [SerializeField] private ChoiceCardSwipe choiceCard;
    [SerializeField] private WarHUD warHUD;

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
    }

    public void SpawnVFXOnPlayer(GameObject vfxPrefab)
    {
        if (vfxPrefab == null || player == null || fxCanvas == null)
        {
            Debug.LogError("VFX 프리팹, 플레이어, 또는 FX Canvas가 설정되지 않았습니다!");
            return;
        }

        GameObject vfxInstance = Instantiate(vfxPrefab, fxCanvas.transform);
        vfxInstance.transform.position = player.transform.position;
    }


    public void ResetForNewBattle(int newMaxTurns = 30)
    {
        int chapterToLoad;
        bool isRealGameMode = !isTestMode && GameManager.instance != null;

        if (isTestMode)
        {
            chapterToLoad = testChapter;
        }
        else if (isRealGameMode)
        {
            chapterToLoad = GameManager.instance.CurrentChapter;
        }
        else
        {
            Debug.LogError("GameManager를 찾을 수 없습니다! 정상 모드로 실행할 수 없습니다. WarTurnManager에서 테스트 모드를 활성화하세요.");
            return;
        }

        CurrentChapter = chapterToLoad;
        if (ground != null)
        {
            ground.SetBackgroundForChapter(CurrentChapter);
        }

        if (GameManager.instance != null && GameManager.instance.CurrentChapter == 1)
        {
            WarHistory.ResetHistory();
        }

        if (enemy != null)
        {
            Destroy(enemy.gameObject);
        }

        int chapterIndex = GameManager.instance.CurrentChapter - 1;
        if (chapterIndex < 0 || chapterIndex >= chapterEnemies.Count || chapterEnemies[chapterIndex].enemyPrefabs.Count == 0)
        {
            Debug.LogError($"챕터 {chapterIndex + 1}에 설정된 적이 없습니다!");
            return;
        }
        List<CustomEnemy> enemyPool = chapterEnemies[chapterIndex].enemyPrefabs;
        CustomEnemy selectedEnemyPrefab = enemyPool[Random.Range(0, enemyPool.Count)];
        int desiredLaneLength = selectedEnemyPrefab.battleLaneLength;
        if (ground != null)
        {
            ground.InitializeGrid(desiredLaneLength);
        }
        enemy = Instantiate(selectedEnemyPrefab, ground.transform);
        this.maxTurns = selectedEnemyPrefab.maxTurns;
        Debug.Log($"챕터 {chapterIndex + 1} 전투 시작! 등장한 적: {enemy.name.Replace("(Clone)", "")}, 전장 크기: {desiredLaneLength}칸");

        if (warHUD != null)
        {
            enemy.enemyInfoText = warHUD.EnemyInfoTextField;
        }
        maxTurns = newMaxTurns;
        currentTurn = 0;
        battleEnded = false;
        turnRunning = false;
        usedSingleUseSkills.Clear();

        if (player != null)
        {
            player.ResetState(ground, playerStartIndex);
        }
        if (enemy != null && enemy.Ctrl != null)
        {
            enemy.Ctrl.ResetState(ground, enemyStartIndex);
        }

        if (player != null && player.currentSkill != null)
        {
            skillCooldownTimer = player.currentSkill.cooltime;
        }
        else
        {
            skillCooldownTimer = 0;
        }
        if (!battleEnded && enemy != null && enemy.Ctrl != null)
        {
            enemy.PrepareAndShowHint();
        }
        if (warHUD != null)
        {
            warHUD.UpdateAllUI();
        }
        Debug.Log("전투 및 캐릭터 상태 초기화 완료");
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
        TurnFlowAsync(playerAction).Forget();
        if (battleEnded) return;
    }

    void EndBattle(string resultLog)
    {
        if (battleEnded) return;
        battleEnded = true;

        PlayerPrefs.DeleteKey("HasSeenWarTutorial");
        PlayerPrefs.Save();

        bool isWin = resultLog.Contains("승리");
        WarHistory.RecordWarResult(isWin); 
        var changes = new List<ParameterChange>

        {
            new ParameterChange
            {
            parameterType = ParameterType.전황,
            valueChange = isWin ? +20 : -20
            }
        };
        GamePlayerStats.Instance.ApplyChanges(changes); // 전황 파라미터 변경 적용
        
        // 업적 체크 - AchievementIntegration 직접 호출
        var achievementIntegration = FindObjectOfType<AchievementIntegration>();
        if (achievementIntegration != null)
        {
            GameOutcome battleOutcome = isWin ? GameOutcome.Victory : (resultLog.Contains("무승부") ? GameOutcome.Draw : GameOutcome.Defeat);
            // 첫 전투 판별: 1회차 1챕터에서 첫 번째 전투인지 확인
            bool isFirstBattle = DataManager.Instance?.PlayerData?.playthroughCount == 1 && 
                                DataManager.Instance?.PlayerData?.currentChapter == 1 &&
                                (DataManager.Instance?.PlayerData?.completedBattleResultIds?.Count ?? 0) == 0;
            achievementIntegration.OnBattleResult(battleOutcome, isFirstBattle);
            Debug.Log($"[WarTurnManager] 전투 결과 업적 체크: {battleOutcome}, 첫 전투: {isFirstBattle} (회차: {DataManager.Instance?.PlayerData?.playthroughCount}, 챕터: {DataManager.Instance?.PlayerData?.currentChapter}, 완료된 전투 수: {DataManager.Instance?.PlayerData?.completedBattleResultIds?.Count ?? 0})");
        }
        
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

    async UniTask TurnFlowAsync(WarAction playerAction)
    {
        turnRunning = true;
        enemy?.HideHint();
        var enemyAction = enemy.GetPreparedAction();

        bool playerActsFirst;
        switch ((playerAction, enemyAction))
        {
            case (WarAction.Attack, WarAction.Defend): playerActsFirst = false; break;
            default: playerActsFirst = true; break;
        }

        if (playerActsFirst)
        {
            await ProcessMoveWithCollisionCheck(player.ActAsync(playerAction));
            if (player.Ctrl.CurrentIndex < enemy.Ctrl.CurrentIndex)
            {
                await ProcessMoveWithCollisionCheck(enemy.ActAsync(enemyAction));
            }
        }
        else
        {
            await ProcessMoveWithCollisionCheck(enemy.ActAsync(enemyAction));
            if (player.Ctrl.CurrentIndex < enemy.Ctrl.CurrentIndex)
            {
                await ProcessMoveWithCollisionCheck(player.ActAsync(playerAction));
            }
        }

        int pIdx = player.Ctrl.CurrentIndex;
        int eIdx = enemy.Ctrl.CurrentIndex;

        if (pIdx >= eIdx)
        {
            int meet = Mathf.Clamp(Mathf.RoundToInt((pIdx + eIdx) * 0.5f), 0, ground.LaneLength - 1);

            await UniTask.WhenAll(
                player.Ctrl.CrushResultAsync(meet),
                enemy.Ctrl.CrushResultAsync(meet)
            );
            enemy.HandleCollision(player, playerAction, enemyAction);
        }

        // 턴 종료
        Debug.Log($"Turn {currentTurn} End / Player Index: {player.Ctrl.CurrentIndex}, Enemy Index: {enemy.Ctrl.CurrentIndex}");
        CheckRingOutStatus();
        CheckWinLoseDrawAfterTurn();
        if (!battleEnded && currentTurn >= maxTurns) EndBattle("무승부 - 턴 제한 소진");

        if (!battleEnded && enemy != null && enemy.Ctrl != null)
        {
            enemy.PrepareAndShowHint();
        }
        if (warHUD != null)
        {
            warHUD.UpdateAllUI();
        }
        if (choiceCard != null)
        {
            choiceCard.SetInteractable(true);
        }
        turnRunning = false;
    }

    async UniTask ProcessMoveWithCollisionCheck(UniTask moveTask)
    {
        while (!moveTask.Status.IsCompleted())
        {
            if (player.Ctrl.CurrentIndex >= enemy.Ctrl.CurrentIndex)
            {
                player.Ctrl.StopMovement();
                enemy.Ctrl.StopMovement();
                Debug.Log("이동 중 충돌! 모든 이동을 중단합니다.");
                break;
            }
            await UniTask.Yield(); // 다음 프레임
        }
    }

    public void OnClick_PlayerSkill()
    {
        if (warHUD == null) return;

        if (turnRunning || IsBattleEnded || player.currentSkill == null || skillCooldownTimer > 0)
        {
            string reason = "��ų ��� �Ұ�";
            if (turnRunning) reason = "���� �� ���� ��";
            else if (player.currentSkill == null) reason = "������ ��ų ����";
            else if (skillCooldownTimer > 0) reason = $"��Ÿ�� {skillCooldownTimer}�� ����";
            warHUD.ShowActionFeedback(reason); 

            return;
        }

        SkillData usedSkill = player.currentSkill;
        if (usedSkill.isSingleUsePerCombat && usedSingleUseSkills.Contains(usedSkill))
        {
            warHUD.ShowActionFeedback("�̹� �������� �̹� ����� ��ų");
            return;
        }

        warHUD.ShowActionFeedback($"{usedSkill.skillName} ���!");


        player.UseSkill(enemy, this);
        if (usedSkill.isSingleUsePerCombat)
        {
            usedSingleUseSkills.Add(usedSkill);
        }
        skillCooldownTimer = usedSkill.cooltime;

    }

    public string GetSkillName()
    {
        if (player != null && player.currentSkill != null)
        {
            return player.currentSkill.skillName;
        }
        return null;
    }


    


[System.Serializable]
    public class ChapterEnemyPool
    {
        public string chapterName;
        public List<CustomEnemy> enemyPrefabs;
    }

    public int CurrentTurn => currentTurn;
    public int MaxTurns => maxTurns;
    public bool IsBattleEnded => battleEnded || currentTurn >= maxTurns;
}