using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProtoEnemy : WarEnemy
{
    public override void HandleCollision(WarPlayer player, WarAction playerAction, WarAction myAction)
    {
        switch ((playerAction, myAction))
        {
            case (WarAction.Attack, WarAction.Attack): // 공격 vs 공격
                Debug.Log("실험체: 공격 vs 공격! 서로 1 데미지, 플레이어 2칸, 자신 3칸 밀림");
                player.TakeDamage(1);
                this.TakeDamage(player.AttackPower);

                player.Ctrl.CrushResult(player.Ctrl.CurrentIndex - 2);
                this.Ctrl.CrushResult(this.Ctrl.CurrentIndex + 3);
                break;

            case (WarAction.Attack, WarAction.Defend): // 공격 vs 수비
                Debug.Log("실험체: 플레이어 공격, 자신 수비! 플레이어만 4칸 밀려남");
                player.Ctrl.CrushResult(player.Ctrl.CurrentIndex - 4);
                break;

            case (WarAction.Defend, WarAction.Attack): // 수비 vs 공격
                Debug.Log("실험체: 플레이어 수비, 자신 공격! 자신이 2칸 밀려나고 플레이어 쉴드 +1");
                player.GainShield(1);
                this.Ctrl.CrushResult(this.Ctrl.CurrentIndex + 2);
                break;

            case (WarAction.Defend, WarAction.Defend): // 수비 vs 수비
                Debug.Log("실험체: 수비 vs 수비! 서로 1칸씩 밀려남");
                player.Ctrl.CrushResult(player.Ctrl.CurrentIndex - 1);
                this.Ctrl.CrushResult(this.Ctrl.CurrentIndex + 1);
                break;
        }
    }
}