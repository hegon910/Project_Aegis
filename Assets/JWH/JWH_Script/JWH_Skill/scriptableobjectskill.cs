

//[CreateAssetMenu(fileName = "NewSkill", menuName = "Battle/Skill")]
//public class SkillSO : ScriptableObject
//{
//    public string key;           
//    public string displayName;
//    public int maxCooldown = 2;

//    public enum EffectType { Heal, Shield, DamageEnemy }
//    public EffectType effect;
//    public int value;

//    public void Execute(BattleContext ctx)
//    {
//        switch (effect)
//        {
//            case EffectType.Heal: ctx.HealPlayer(value); break;
//            case EffectType.Shield: ctx.GainShieldPlayer(value); break;
//            case EffectType.DamageEnemy: ctx.DamageEnemy(value); break;
//        }
//        Debug.Log($"½ºÅ³ [{displayName}]");
//    }
//}