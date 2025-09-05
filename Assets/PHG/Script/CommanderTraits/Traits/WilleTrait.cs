using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Wille_PoliticalTies", menuName = "Aegis/Traits/Wille_PoliticalTies")]
public class WilleTrait : Trait
{
    [SerializeField] private int supplyBonus = 5;

    public override void ProcessEventOutcome(bool wasSuccess, List<ParameterChange> outcomeChanges)
    {
        // 1. 정치력 고정: outcomeChanges 리스트에서 '정치력' 관련 항목을 모두 제거합니다.
        outcomeChanges.RemoveAll(change => change.parameterType == ParameterType.정치력);

        // 2. 물자 보너스: 리스트를 순회하며 '물자'가 '증가'하는 항목을 찾습니다.
        for (int i = 0; i < outcomeChanges.Count; i++)
        {
            var change = outcomeChanges[i];
            if (change.parameterType == ParameterType.물자 && change.valueChange > 0)
            {
                Debug.Log($"<color=yellow>[특성 발동] 정계 연줄: 물자 획득량 +{supplyBonus} 보너스!</color>");
                // 클래스이므로 참조가 변경됩니다. valueChange 값을 직접 더해줍니다.
                change.valueChange += supplyBonus;
            }
        }
    }
}