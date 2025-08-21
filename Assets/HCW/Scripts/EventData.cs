using UnityEngine;
using System.Collections.Generic;

// 파라미터 종류
public enum ParameterType
{
    정치력,
    병력,
    물자,
    리더십,
    전황,
    카르마
}

// 파라미터 변화량을 저장하는 클래스
[System.Serializable]
public class ParameterChange
{
    [Tooltip("변경할 파라미터 종류")]
    public ParameterType parameterType;
    [Tooltip("변경할 값")]
    public int valueChange;
}

// 선택지의 성공 또는 실패 결과를 저장하는 클래스
[System.Serializable]
public class ChoiceOutcome
{
    [Tooltip("결과 텍스트")]
    public string outcomeText;
    [Tooltip("파라미터 변화 목록")]
    public List<ParameterChange> parameterChanges = new List<ParameterChange>();
}

// 하나의 선택지 정보를 모두 담는 클래스
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


// 최종 이벤트 데이터를 담는 스크립터블 오브젝트
[CreateAssetMenu(fileName = "New Event", menuName = "Aegis/Event Data")]
public class EventData : ScriptableObject
{
    [Header("이벤트 기본 정보")]
    public Sprite eventSprite;

    [TextArea(3, 10)]
    [Tooltip("이벤트 상황에 표시될 메인 대화 내용")]
    public string dialogue;

    [Header("선택지")]
    public EventChoice leftChoice;
    public EventChoice rightChoice;
}