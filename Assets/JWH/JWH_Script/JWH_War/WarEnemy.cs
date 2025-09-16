using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarEnemy : MonoBehaviour
{
    [SerializeField] protected WarController controller;

    [Header("AI & Info")]
    [SerializeField, Range(0f, 1f)] protected float attackChance = 0.5f;
    [SerializeField] private int rank = 1;

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



    public void KillByRingOut()
    {
        if (controller.CurrentHP <= 0) return;
        controller.CurrentHP = 0;
        Debug.Log("적군 링아웃");
    }


}