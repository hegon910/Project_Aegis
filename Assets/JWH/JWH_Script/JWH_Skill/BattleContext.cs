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

//    // 자주 쓰는 헬퍼
//    public void DamageEnemy(int amount) => Enemy.TakeDamage(amount);
//    public void DamagePlayer(int amount) => Player.TakeDamage(amount);
//    public void HealPlayer(int amount) => Player.TakeDamage(-amount); // 음수 데미지로 힐
//    public void GainShieldPlayer(int s) => Player.GainShield(s);
//}