using UnityEngine;
using System.Collections;

public class CommanderController : MonoBehaviour
{
    [Header("참조")]
    public HiddenBattleManager battleManager;

    [Header("스탯")]
    public int maxHp = 10;
    public int currentHp;

    //[Header("위치 정보")]
    [Tooltip("현재 위치 (1~14칸)")]
    public int currentPosition { get; private set; }
    private const int START_POSITION = 6; // [cite: 2-1-1]

    public enum ActionType { NONE, SHOOT, EVADE, RESET_POSITION }
    public ActionType lastAction { get; private set; }
    private ActionType pendingAction;

    void Start()
    {
        currentHp = maxHp;
        currentPosition = START_POSITION;
        // TODO: 실제 캐릭터 위치를 currentPosition에 맞게 설정
    }

    public void OnSelectShoot() => SelectAction(ActionType.SHOOT);
    public void OnSelectEvade() => SelectAction(ActionType.EVADE);
    public void OnSelectResetPosition() => SelectAction(ActionType.RESET_POSITION);

    private void SelectAction(ActionType action)
    {
        // 플레이어 턴일 때만 행동 선택 가능
        if (battleManager.currentState == HiddenBattleManager.BattleState.PlayerTurn)
        {
            pendingAction = action;
            battleManager.PlayerActionSubmitted();
        }
    }

    public IEnumerator ExecuteCurrentAction()
    {
        lastAction = pendingAction;
        pendingAction = ActionType.NONE;

        switch (lastAction)
        {
            case ActionType.SHOOT:
                if (currentPosition == 8)
                {
                    Debug.Log("사격 실패: 중력장에 막혔습니다.");
                    // TODO: 특수 사운드, 캐릭터 떨림 연출
                }
                else
                {
                    currentPosition++;
                    Debug.Log($"사격! 전진하여 {currentPosition}번 칸으로 이동. 박사에게 1의 피해."); // [cite: 2-1-1]
                    //battleManager.doctorPhase1.TakeDamage(1);
                    // TODO: 사격 애니메이션 및 이펙트
                }
                break;

            case ActionType.EVADE:
                if (currentPosition == 1)
                {
                    Debug.Log("회피 실패: 필드 이탈!");
                    battleManager.LoseBattle("필드 이탈");
                    yield break; // 코루틴 즉시 종료
                }
                else
                {
                    currentPosition--;
                    Debug.Log($"회피! 후퇴하여 {currentPosition}번 칸으로 이동.");
                    // TODO: 회피 애니메이션
                }
                break;

            case ActionType.RESET_POSITION:
                int previousPosition = currentPosition;
                currentPosition = START_POSITION;
                Debug.Log($"위치 초기화! {START_POSITION}번 칸으로 순간이동."); 
                // TODO: 스킬 연출. 이전 위치와 같아도 연출은 재생
                break;
        }

        // TODO: 실제 캐릭터의 위치를 이동시키는 연출
        Debug.Log($"지휘관 최종 위치: {currentPosition}");
        yield return new WaitForSeconds(1.0f); 
    }

    public void TakeDamage(int damage)
    {
        currentHp -= damage;
        Debug.Log($"지휘관 {damage} 피해 받음. 현재 체력: {currentHp}/{maxHp}");
        if (currentHp <= 0)
        {
            battleManager.LoseBattle("지휘관 사망");
        }
        // TODO: 피격 연출, UI 업데이트
    }

    public void ForceSetPosition(int position)
    {
        currentPosition = position;
        Debug.Log($"외부 요인에 의해 위치 강제 이동: {currentPosition}");
        // TODO: 끌려가거나 밀려나는 연출
    }
}