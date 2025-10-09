using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SkillData : ScriptableObject
{
    [Header("공통 정보")]
    public string skillID; // 저장에 사용할 ID
    public string skillName;
    public int skillRank;
    public Sprite icon; // 특수 이벤트 스킬 획득/교체 UI 시각화 아이콘
    [TextArea(3, 5)]
    public string description;
    public int cooltime; // 스킬 쿨타임 턴 수
    [Header("전투 당 1회용 스킬 여부")]
    public bool isSingleUsePerCombat;

    public virtual bool CanUse(WarPlayer player, WarEnemy enemy)
    {
        return true;
    }

    public abstract void Activate(WarPlayer player, WarEnemy enemy, WarTurnManager turnManager);
}
