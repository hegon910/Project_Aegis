using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class WarEnemy : MonoBehaviour
{
    [SerializeField] protected WarController controller;

    [Header("AI & Info")]
    [SerializeField, Range(0f, 1f)] protected float attackChance = 0.5f;
    [SerializeField] private int rank = 1;

    [Header("Hint System")] 
    [Tooltip("적의 행동 힌트를 표시할 UI")]
    [SerializeField] private TMP_Text enemyInfoText;

    [Tooltip("공격 표시할 무작위 힌트 목록")]
    [SerializeField] private List<string> attackHints;

    [Tooltip("방어 표시할 무작위 힌트 목록")]
    [SerializeField] private List<string> defendHints;

    private WarAction nextAction; // 다음 행동 변수


    public int Rank => rank;
    public WarController Ctrl => controller;
    public bool IsDead => controller != null && controller.CurrentHP <= 0;
    public async UniTask ActAsync(WarAction action)
    {
        await controller.DoActionAsync(action);
    }

    protected virtual void Awake()
    {
        if (!controller) controller = GetComponent<WarController>();
    }

    public void TakeDamage(int amount)
    {
        if (controller) controller.TakeDamage(amount);
    }

    public virtual WarAction ChooseAction()
        => (Random.value < attackChance) ? WarAction.Attack : WarAction.Defend;

    public virtual void HandleCollision(WarPlayer player, WarAction playerAction, WarAction myAction)
    {
        Debug.LogWarning("기본 충돌 로직");
    }

    public void PrepareAndShowHint()
    {
        nextAction = ChooseAction(); // 다음 턴의 행동 결정
        if (enemyInfoText != null)
        {
            switch (nextAction)
            {
                case WarAction.Attack:
                    enemyInfoText.text = GetRandomHint(attackHints);
                    break;
                case WarAction.Defend:
                    enemyInfoText.text = GetRandomHint(defendHints);
                    break;
                default:
                    enemyInfoText.text = ""; // 그 외의 경우 텍스트 초기화
                    break;
            }
            enemyInfoText.gameObject.SetActive(true); // 힌트 보이기
        }
    }

    private string GetRandomHint(List<string> hintList)
    {
        // 리스트가 비어있으면 오류 방지를 위해 빈 문자열을 반환합니다.
        if (hintList == null || hintList.Count == 0)
        {
            return "힌트가 없습니다.";
        }

        // 0부터 리스트의 크기 -1 사이의 무작위 인덱스를 선택합니다.
        int randomIndex = Random.Range(0, hintList.Count);
        return hintList[randomIndex];
    }

    public WarAction GetPreparedAction()
    {
        return nextAction;
    }

    public void HideHint()
    {
        if (enemyInfoText != null)
        {
            enemyInfoText.gameObject.SetActive(false);
        }
    }

    public void KillByRingOut()
    {
        if (controller.CurrentHP <= 0) return;
        controller.CurrentHP = 0;
        Debug.Log("적군 링아웃");
    }


}