using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SkillData : ScriptableObject
{
    [Header("공통 정보")]
    public string skillID; // 저장에 사용할 ID
    public string skillName;
    [TextArea(3, 5)]
    public string description;
    public int cooldown; // 스킬 쿨타임 턴 수

    public virtual bool CanUse(WarPlayer player, WarEnemy enemy)
    {
        return true;
    }

    public abstract void Activate(WarPlayer player, WarEnemy enemy, WarTurnManager turnManager);
}
