using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "���ο� ���� ���� ��ų", menuName = "Skills/��������/���� ���")]
public class DirectDamageSkill : SkillData
{
    [Header("���� ���� ����")]
    public int damage; // ���ط�

    public override void Activate(WarPlayer player, WarEnemy enemy, WarTurnManager turnManager)
    {
        Debug.Log($"{skillName} ���! ������ ���� {damage}�� �ݴϴ�.");
        enemy.TakeDamage(damage);
    }
}
