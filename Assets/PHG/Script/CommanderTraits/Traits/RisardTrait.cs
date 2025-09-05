using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Risard_Gambler", menuName = "Aegis/Traits/Risard_Gambler")]
public class RisardTrait : Trait
{
    [SerializeField] private int bonusValue = 5;

    public override void ProcessEventOutcome(bool wasSuccess, List<ParameterChange> outcomeChanges)
    {
        int finalBonus = wasSuccess ? bonusValue : -bonusValue;
        string effect = wasSuccess ? "성공" : "실패";

        Debug.Log($"<color=yellow>[특성 발동] 승부사: 이벤트 {effect}으로 모든 파라미터에 {finalBonus} 적용!</color>");

        // 모든 파라미터에 대한 추가 변경안을 생성하여 결과 리스트에 더합니다.
        outcomeChanges.Add(new ParameterChange { parameterType = ParameterType.정치력, valueChange = finalBonus });
        outcomeChanges.Add(new ParameterChange { parameterType = ParameterType.병력, valueChange = finalBonus });
        outcomeChanges.Add(new ParameterChange { parameterType = ParameterType.물자, valueChange = finalBonus });
        outcomeChanges.Add(new ParameterChange { parameterType = ParameterType.리더십, valueChange = finalBonus });
    }
}