//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public sealed class BattleContext
//{
//    public BattlePlayer Player { get; }
//    public BattleEnemy Enemy { get; }
//    public BattleGround Ground { get; }

//    public BattleContext(BattlePlayer p, BattleEnemy e, BattleGround g)
//    {
//        Player = p; Enemy = e; Ground = g;
//    }

//    // ���� ���� ����
//    public void DamageEnemy(int amount) => Enemy.TakeDamage(amount);
//    public void DamagePlayer(int amount) => Player.TakeDamage(amount);
//    public void HealPlayer(int amount) => Player.TakeDamage(-amount); // ���� �������� ��
//    public void GainShieldPlayer(int s) => Player.GainShield(s);
//}