using UnityEngine;
using System.Collections.Generic;

// [System.Serializable] 이나 ParameterChange, UIOutcome 같은 중복 정의를 모두 제거했습니다.
// 대신 EventData.cs 에 있는 원본 EventChoice, ChoiceOutcome 클래스를 직접 사용합니다.

[CreateAssetMenu(fileName = "New UI Event", menuName = "UI Event Data")]
public class UIEventData : ScriptableObject
{
    [Header("이벤트 내용")]
    public string characterName;
    public Sprite characterSprite; // 기존 EventData 와의 호환을 위해 유지
    [TextArea(3, 10)]
    public string dialogue;

    [Header("선택지 (카드 스와이프용)")]
    // EventData.cs 에 정의된 EventChoice 클래스를 재사용합니다.
    // 단, UI 연출을 위해 choiceText 필드는 스와이프 시 보일 텍스트로 사용합니다.
    public EventChoice leftChoice;
    public EventChoice rightChoice;
}