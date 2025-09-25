using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WarEnemy : MonoBehaviour
{
    [SerializeField] protected WarController controller;

    [Header("AI & Info")]
    [SerializeField, Range(0f, 1f)] protected float attackChance = 0.5f;
    [SerializeField] private int rank = 1;

    [Header("Hint System")] 
    [Tooltip("적의 행동 힌트를 표시할 UI")]
    [SerializeField] private TMP_Text actionHintText;

    private WarAction nextAction; // 다음 행동 변수


    public int Rank => rank;
    public WarController Ctrl => controller;
    public bool IsDead => controller != null && controller.CurrentHP <= 0;
    public bool IsBusy => controller != null && controller.IsBusy;
    public void Act(WarAction action) => controller.DoAction(action);

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
        if (actionHintText != null)
        {
            switch (nextAction)
            {
                case WarAction.Attack:
                    actionHintText.text = "적들이 분주하다";
                    break;
                case WarAction.Defend:
                    actionHintText.text = "적들이 잠잠하다";
                    break;
                default:
                    actionHintText.text = ""; // 그 외의 경우 텍스트 초기화
                    break;
            }
            actionHintText.gameObject.SetActive(true); // 힌트 보이기
        }
    }

    public WarAction GetPreparedAction()
    {
        return nextAction;
    }

    public void HideHint()
    {
        if (actionHintText != null)
        {
            actionHintText.gameObject.SetActive(false);
        }
    }

    public void KillByRingOut()
    {
        if (controller.CurrentHP <= 0) return;
        controller.CurrentHP = 0;
        Debug.Log("적군 링아웃");
    }


}