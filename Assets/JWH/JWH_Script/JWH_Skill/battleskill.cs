//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class BattleSkillSystem : MonoBehaviour
//{
//    [SerializeField] BattlePlayer player;
//    [SerializeField] BattleEnemy enemy;
//    [SerializeField] BattleGround ground;
//    [SerializeField] string equippedKey = "heal_small"; // 현재 장착 스킬 key

//    Dictionary<string, int> cooldown = new();
//    BattleContext ctx;

//    void Awake() { ctx = new BattleContext(player, enemy, ground); }

//    public bool TryUseEquippedSkill()
//    {
        
//    }

//    public void Cooldowns()
//    {
//        var keys = new List<string>(cooldown.Keys);
//        foreach (var k in keys) cooldown[k] = Mathf.Max(0, cooldown[k] - 1);
//    }
//}