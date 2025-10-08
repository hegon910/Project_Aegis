using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SkillData : ScriptableObject
{
    [Header("공통 정보")]
    public string skillID; // 저장에 사용할 ID
    public string skillName;
    public int skillRank;
    [TextArea(3, 5)]
    public string description;
    public int cooltime; // 스킬 쿨타임 턴 수
    [Header("전투 당 1회용 스킬 여부")]
    public bool isSingleUsePerCombat;

    [Header("Visuals & Audio")]
    public GameObject skillEffectPrefab;
    public AudioClip skillSound;

    public virtual bool CanUse(WarPlayer player, WarEnemy enemy)
    {
        return true;
    }

    public abstract void Activate(WarPlayer player, WarEnemy enemy, WarTurnManager turnManager);
}
