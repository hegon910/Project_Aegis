using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//void FinishBattle(bool isWin) 내가 보내면 되는 경우 신호를
//{
//    var changes = new List<ParameterChange>
//    {
//        new ParameterChange
//        {
//            parameterType = ParameterType.전황,
//            valueChange = isWin ? +10 : -10
//        }
//    };

//    PlayerStats.Instance.ApplyChanges(changes);   // 결과 반영하는 부분
//    GameManager.instance.GoToBattleResultPanel(); // 결과 패널로 이동
//}

//ㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡ

//// GameManager.cs
//public void ApplyBattleResult(bool isWin) 이거도 사실은 가능한거 아닐까?
//{
//    var changes = new List<ParameterChange> {
//        new ParameterChange { parameterType = ParameterType.전황, valueChange = isWin ? +10 : -10 }
//    };
//    PlayerStats.Instance.ApplyChanges(changes);   
//    GoToBattleResultPanel();
//}

//// BattleManager.cs
//void FinishBattle(bool isWin)
//{
//    GameManager.instance.ApplyBattleResult(isWin); // 결과만 전달
//}