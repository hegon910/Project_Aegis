using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DoctorAI_Phase1 : MonoBehaviour
{
    [Header("참조")]
    public HiddenBattleManager battleManager;

    [Header("스탯")]
    public int maxHp = 50;
    public int currentHp;
    public int shield { get; private set; }

    [Header("패턴 데미지 설정")]
    [Tooltip("영역 파괴 스킬의 기본 피해량")]
    public int areaDestructionDamage = 4; //

    private enum Skill { NONE, AREA_DESTRUCTION_SHOOT, AREA_DESTRUCTION_EVADE, AREA_DESTRUCTION_RESET, RAILGUN, GRAVITY_FIELD }
    private Skill plannedSkill;

    void Start()
    {
        currentHp = maxHp;
    }

    // 전투 시작 시 보호막 생성
    public IEnumerator ExecuteShieldPattern()
    {
        Debug.Log("박사: 보호막을 전개합니다.");
        shield = 5;
        // TODO: 보호막 UI/이펙트 표시
        yield return new WaitForSeconds(1.5f); // 연출 시간
    }

    // 플레이어 턴 시작 시 다음 행동 계획
    public void PlanNextAction(int playerPosition)
    {
        if (playerPosition >= 1 && playerPosition <= 4)
        {
            plannedSkill = Skill.AREA_DESTRUCTION_RESET; // 위치 초기화 유도
        }
        else if (playerPosition > 1 && Random.value < 0.5f) // 50% 확률
        {
            plannedSkill = Skill.AREA_DESTRUCTION_EVADE; // 회피 유도
        }
        else
        {
            plannedSkill = Skill.AREA_DESTRUCTION_SHOOT; // 사격 유도
        }

        // 특정 조건 하에 레일건을 사용하도록 확률 추가
        if (Random.value < 0.3f) // 30% 확률로 레일건 계획
        {
            plannedSkill = Skill.RAILGUN;
        }

        Debug.Log($"박사가 다음 행동 계획: <color=yellow>{plannedSkill}</color>");
        // TODO: 공격 예고 UI 표시 (영역 파괴의 경우)
    }

    // 지휘관 행동이 끝난 후 계획된 행동 실행
    public IEnumerator ExecutePlannedAction(CommanderController.ActionType playerLastAction, int finalPlayerPosition)
    {
        // 중력장 전개는 다른 스킬과 별개로, 지휘관이 8번 칸에 도달하면 즉시 발동
        if (finalPlayerPosition == 8)
        {
            yield return StartCoroutine(ExecuteGravityField());
        }

        switch (plannedSkill)
        {
            case Skill.AREA_DESTRUCTION_SHOOT:
            case Skill.AREA_DESTRUCTION_EVADE:
            case Skill.AREA_DESTRUCTION_RESET:
                yield return StartCoroutine(ExecuteAreaDestruction(finalPlayerPosition));
                break;
            case Skill.RAILGUN:
                yield return StartCoroutine(ExecuteRailgun(playerLastAction));
                break;
        }
        plannedSkill = Skill.NONE; // 행동 계획 초기화
    }

    private IEnumerator ExecuteAreaDestruction(int finalPlayerPosition)
    {
        Debug.Log($"박사: 영역 파괴({plannedSkill}) 공격 실행!");
        var attackTiles = new HashSet<int>();

        // 계획된 패턴에 따라 공격할 타일 결정
        //int playerInitialPosition = 0; 

        attackTiles.Add(finalPlayerPosition);
        if (finalPlayerPosition > 1) attackTiles.Add(finalPlayerPosition - 1);

        Debug.Log("공격 발생 위치: " + string.Join(", ", attackTiles));
        // TODO: 공격 이펙트 연출

        if (attackTiles.Contains(finalPlayerPosition))
        {
            Debug.Log($"지휘관 피격! 피해량 {areaDestructionDamage}");
            battleManager.commander.TakeDamage(areaDestructionDamage);
        }
        else
        {
            Debug.Log("지휘관이 공격을 피했습니다!");
        }
        yield return new WaitForSeconds(1.0f);
    }

    private IEnumerator ExecuteRailgun(CommanderController.ActionType playerLastAction)
    {
        Debug.Log("박사: 레일건 충전 완료.");

        if (playerLastAction != CommanderController.ActionType.SHOOT)
        {
            Debug.Log("레일건 발사! 즉사합니다.");
            // TODO: 레일건 발사 이펙트
            battleManager.LoseBattle("레일건 피격");
        }
        else
        {
            Debug.Log("지휘관의 사격으로 레일건이 취소되었습니다.");
            // TODO: 충전 취소 이펙트
        }
        yield return new WaitForSeconds(1.0f);
    }

    private IEnumerator ExecuteGravityField()
    {
        Debug.Log("박사: 중력장 전개!");
        // TODO: 중력장 이펙트
        battleManager.commander.ForceSetPosition(4); // 지휘관을 4번 칸으로 밀어냄
        yield return new WaitForSeconds(1.0f); // 밀려나는 연출 시간
    }

    public void TakeDamage(int damage)
    {
        int damageToShield = Mathf.Min(shield, damage);
        shield -= damageToShield;
        int remainingDamage = damage - damageToShield;

        if (remainingDamage > 0)
        {
            currentHp -= remainingDamage;
        }

        Debug.Log($"박사 피격! {damage} 피해. 보호막: {shield}, 체력: {currentHp}/{maxHp}");

        if (currentHp <= 0)
        {
            Debug.Log("박사 1페이즈 처치!");
            // battleManager.TransitionToPhase2(); // 2페이즈 전환
        }
    }
}