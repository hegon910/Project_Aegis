using UnityEngine;
using System.Collections.Generic;

public enum ParameterType
{
    정치력,
    병력,
    물자,
    리더십,
    전세,
    카르마
}

// 파라미터 변화량을 조절하는 클래스
[System.Serializable]
public class ParameterChange
{
    [Tooltip("변경할 파라미터 종류")]
    public ParameterType parameterType;
    [Tooltip("변경할 값")]
    public int valueChange;
}

[CreateAssetMenu(fileName = "New Event", menuName = "Aegis/Event Data")]
public class EventData : ScriptableObject
{
    [Header("이벤트 기본 정보")]
    public string eventName;
    public string characterName;
    public Sprite characterSprite; // 캐릭터 스프라이트

    [TextArea(3, 10)]
    [Tooltip("이벤트 상황에 표시될 메인 대화 내용")]
    public string dialogue;

    [Header("선택지 텍스트")]
    [Tooltip("왼쪽을 선택했을 때 표시될 선택지")]
    public string leftChoiceText;
    [Tooltip("오른을 선택했을 때 표시될 선택지")]
    public string rightChoiceText;

    [Header("선택지별 효과")]
    [Tooltip("왼쪽 선택으로 변경될 파라미터")]
    public List<ParameterChange> leftChoiceEffects;
    [Tooltip("오른쪽 선택으로 변경될 파라미터")]
    public List<ParameterChange> rightChoiceEffects;
}