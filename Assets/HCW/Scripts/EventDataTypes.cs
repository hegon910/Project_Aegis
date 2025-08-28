using System.Collections.Generic;
using UnityEngine;

public enum ParameterType
{
    정치력,
    병력,
    물자,
    리더십,
    전황,
    카르마
}

[System.Serializable]
public class ParameterChange
{
    [Tooltip("변경할 파라미터 종류")]
    public ParameterType parameterType;
    [Tooltip("변경할 값")]
    public int valueChange;
}

[System.Serializable]
public class ChoiceOutcome
{
    [Tooltip("결과 텍스트")]
    public string outcomeText;
    [Tooltip("파라미터 변화 목록")]
    public List<ParameterChange> parameterChanges = new List<ParameterChange>();
}

[System.Serializable]
public class EventChoice
{
    [Tooltip("선택지 텍스트")]
    public string choiceText;
    [Tooltip("성공 조건")]
    public string successCondition;
    [Tooltip("실패 조건")]
    public string failCondition;

    [Tooltip("성공 시 결과")]
    public ChoiceOutcome successOutcome;
    [Tooltip("실패 시 결과")]
    public ChoiceOutcome failOutcome;
}

[System.Serializable]
public class EventData
{
    [Header("이벤트 기본 정보")]
    public string eventName;
    public Sprite eventSprite;

    [TextArea(3, 10)]
    [Tooltip("이벤트 상황에 표시될 메인 대화 내용")]
    public string dialogue;

    [Header("선택지")]
    public EventChoice leftChoice;
    public EventChoice rightChoice;
}