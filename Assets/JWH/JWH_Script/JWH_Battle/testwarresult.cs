using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//void FinishBattle(bool isWin) ���� ������ �Ǵ� ��� ��ȣ��
//{
//    var changes = new List<ParameterChange>
//    {
//        new ParameterChange
//        {
//            parameterType = ParameterType.��Ȳ,
//            valueChange = isWin ? +10 : -10
//        }
//    };

//    PlayerStats.Instance.ApplyChanges(changes);   // ��� �ݿ��ϴ� �κ�
//    GameManager.instance.GoToBattleResultPanel(); // ��� �гη� �̵�
//}

//�ѤѤѤѤѤѤѤѤѤѤѤѤѤѤѤѤѤѤѤѤѤѤѤѤѤѤѤѤѤѤѤѤѤѤѤѤѤѤѤѤѤѤѤѤѤѤѤѤ�

//// GameManager.cs
//public void ApplyBattleResult(bool isWin) �̰ŵ� ����� �����Ѱ� �ƴұ�?
//{
//    var changes = new List<ParameterChange> {
//        new ParameterChange { parameterType = ParameterType.��Ȳ, valueChange = isWin ? +10 : -10 }
//    };
//    PlayerStats.Instance.ApplyChanges(changes);   
//    GoToBattleResultPanel();
//}

//// BattleManager.cs
//void FinishBattle(bool isWin)
//{
//    GameManager.instance.ApplyBattleResult(isWin); // ����� ����
//}