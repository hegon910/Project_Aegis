using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SkillData : ScriptableObject
{
    [Header("���� ����")]
    public string skillID; // ���忡 ����� ID
    public string skillName;
    [TextArea(3, 5)]
    public string description;
    public int cooldown; // ��ų ��Ÿ�� �� ��

    public virtual bool CanUse(WarPlayer player, WarEnemy enemy)
    {
        return true;
    }

    public abstract void Activate(WarPlayer player, WarEnemy enemy, WarTurnManager turnManager);
}
