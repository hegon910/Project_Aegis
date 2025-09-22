using UnityEngine;
using System.Collections;

public class HiddenBattleManager : MonoBehaviour
{
    [Header("핵심 참조")]
    public CommanderController commander;
    public DoctorAI_Phase1 doctorPhase1;
    // public DoctorAI_Phase2 doctorPhase2; // 2페이즈 스크립트
    // public HiddenBattleUI battleUI; // 히든 전투 전용 UI 관리자

    [Header("전투 설정")]
    [Tooltip("턴당 주어지는 제한 시간(초)")]
    public float maxTurnTime = 15f; //

    public enum BattleState { Intro, PlayerTurn, ActionSequence, PhaseTransition, BattleEnd }
    public BattleState currentState { get; private set; }

    private float turnTimer;

    void Start()
    {
        // 전투 시작 시퀀스 실행
        StartCoroutine(StartBattleSequence());
    }

    void Update()
    {
        // 플레이어 턴일 때 시간 제한 타이머 작동
        if (currentState == BattleState.PlayerTurn)
        {
            turnTimer -= Time.deltaTime;
            // battleUI.UpdateTime(turnTimer); // UI에 시간 표시 업데이트
            if (turnTimer <= 0)
            {
                LoseBattle("시간 초과");
            }
        }
    }

    private IEnumerator StartBattleSequence()
    {
        currentState = BattleState.Intro;
        Debug.Log("히든 전투 시퀀스 시작!");
        // TODO: 전투 시작 연출 (카메라 이동, UI 등장 등)

        yield return StartCoroutine(doctorPhase1.ExecuteShieldPattern());

        // 1페이즈 시작
        StartPhase(1);
    }

    public void StartPhase(int phase)
    {
        if (phase == 1)
        {
            Debug.Log("1페이즈 시작");
            // doctorPhase2.gameObject.SetActive(false);
            doctorPhase1.gameObject.SetActive(true);
            StartPlayerTurn();
        }
        // else if (phase == 2) { ... 2페이즈 로직 ... }
    }

    public void StartPlayerTurn()
    {
        Debug.Log("플레이어 턴 시작.");
        currentState = BattleState.PlayerTurn;
        turnTimer = maxTurnTime; // 제한 시간 초기화

        doctorPhase1.PlanNextAction(commander.currentPosition);

    }

    public void PlayerActionSubmitted()
    {
        if (currentState != BattleState.PlayerTurn) return;

        currentState = BattleState.ActionSequence;
        // battleUI.ShowPlayerActions(false); // 행동 선택 UI 비활성화
        Debug.Log("플레이어 행동 선택 완료. 액션 시퀀스 시작.");

        StartCoroutine(ActionSequenceCoroutine());
    }

    private IEnumerator ActionSequenceCoroutine()
    {
        // 지휘관 행동 실행 및 완료 대기
        yield return StartCoroutine(commander.ExecuteCurrentAction());

        // 지휘관 행동으로 전투가 끝났는지 확인
        if (currentState == BattleState.BattleEnd) yield break;

        // 박사의 계획된 행동 실행 및 완료 대기
        yield return StartCoroutine(doctorPhase1.ExecutePlannedAction(commander.lastAction, commander.currentPosition));

        // 박사 행동으로 전투가 끝났는지 확인
        if (currentState == BattleState.BattleEnd) yield break;

        // 다음 플레이어 턴 시작
        StartPlayerTurn();
    }

    public void LoseBattle(string reason)
    {
        if (currentState == BattleState.BattleEnd) return;
        currentState = BattleState.BattleEnd;
        Debug.Log($"<color=red>패배: {reason}</color>");
        // TODO: 패배 처리 (UI 표시, 재시도 옵션 등)
    }

    // TODO: WinBattle, TransitionToPhase2 구현
}