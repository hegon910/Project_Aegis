using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 자원, 능력치 등 게임의 주요 파라미터 종류
/// </summary>
public enum ParameterType
{
    None, // 조건 없음
    정치력,
    병력,
    물자,
    리더십,
    전황,
    카르마
}

/// <summary>
/// 성공 조건의 종류
/// </summary>
public enum SuccessConditionType
{
    Guaranteed,     // 무조건 성공
    Parameter,      // 파라미터 수치 비교
    History,        // 이전 플레이(이벤트) 기록
    BattleResult,   // 전투 결과
    Ending          // 특정 엔딩 완료 여부
}

/// <summary>
/// 파라미터 값의 변화를 정의
/// </summary>
[System.Serializable]
public class ParameterChange
{
    [Tooltip("변경할 파라미터 종류")]
    public ParameterType parameterType;
    [Tooltip("변경할 값")]
    public int valueChange;
}

/// <summary>
/// 선택지의 성공 또는 실패 시의 결과를 정의
/// </summary>
[System.Serializable]
public class ChoiceOutcome
{
    [Tooltip("결과 텍스트")]
    public string outcomeText;
    [Tooltip("파라미터 변화 목록")]
    public List<ParameterChange> parameterChanges = new List<ParameterChange>();
}

/// <summary>
/// 성공 조건의 기본 클래스 (추상)
/// </summary>
[System.Serializable]
public abstract class SuccessCondition
{
    public abstract SuccessConditionType Type { get; }

    /// <summary>
    /// 이 조건의 성공 여부를 평가합니다.
    /// </summary>
    /// <param name="playerStats">현재 플레이어 스탯</param>
    /// <param name="playthroughHistory">과거 플레이 기록</param>
    /// <returns>성공 여부</returns>
    public abstract bool Evaluate(PlayerStats playerStats, PlaythroughHistory playthroughHistory);
}

/// <summary>
/// 무조건 성공 조건
/// </summary>
[System.Serializable]
public class GuaranteedSuccessCondition : SuccessCondition
{
    public override SuccessConditionType Type => SuccessConditionType.Guaranteed;

    public override bool Evaluate(PlayerStats playerStats, PlaythroughHistory playthroughHistory)
    {
        return true;
    }
}

/// <summary>
/// 파라미터 수치에 따라 확률적으로 성공하는 조건
/// </summary>
[System.Serializable]
public class ParameterSuccessCondition : SuccessCondition
{
    public override SuccessConditionType Type => SuccessConditionType.Parameter;

    [Tooltip("비교할 파라미터 타입")]
    public ParameterType targetParameter;
    [Tooltip("성공 확률을 계산하기 위한 요구 파라미터 값")]
    public int requiredValue;

    public override bool Evaluate(PlayerStats playerStats, PlaythroughHistory playthroughHistory)
    {
        if (playerStats == null)
        {
            Debug.LogError("[ParameterSuccessCondition] PlayerStats가 null입니다.");
            return false;
        }

        // 요구치가 0 이하면 항상 성공
        if (requiredValue <= 0)
        {
            return true;
        }

        float playerValue = playerStats.GetStat(targetParameter);

        // 플레이어의 스탯이 요구치보다 높거나 같으면 무조건 성공
        if (playerValue >= requiredValue)
        { 
            return true;
        }

        // 플레이어의 스탯이 요구치보다 낮으면 확률 계산
        float successRate = playerValue / requiredValue;

        return Random.value < successRate;
    }
}

/// <summary>
/// 이전 이벤트 완료 여부 조건
/// </summary>
[System.Serializable]
public class HistorySuccessCondition : SuccessCondition
{
    public override SuccessConditionType Type => SuccessConditionType.History;

    [Tooltip("완료 여부를 체크할 이벤트의 ID")]
    public int requiredEventID;

    public override bool Evaluate(PlayerStats playerStats, PlaythroughHistory playthroughHistory)
    {
        if (playthroughHistory == null) return false;
        return playthroughHistory.GetEventOutcome(requiredEventID, out _);
    }
}

/// <summary>
/// 특정 전투 결과 여부 조건
/// </summary>
[System.Serializable]
public class BattleResultSuccessCondition : SuccessCondition
{
    public override SuccessConditionType Type => SuccessConditionType.BattleResult;

    // TODO: 전투 결과 Enum 정의 필요
    [Tooltip("요구되는 전투 결과")]
    public int requiredBattleResult; // 예: 0=승리, 1=무승부, 2=패배

    public override bool Evaluate(PlayerStats playerStats, PlaythroughHistory playthroughHistory)
    {
        if (playthroughHistory == null) return false;
        // TODO: playthroughHistory에서 특정 전투 결과를 가져오는 로직 구현 필요
        // return playthroughHistory.GetLastBattleResult() == requiredBattleResult;
        return false; // 임시
    }
}

/// <summary>
/// 특정 엔딩 완료 여부 조건
/// </summary>
[System.Serializable]
public class EndingSuccessCondition : SuccessCondition
{
    public override SuccessConditionType Type => SuccessConditionType.Ending;

    [Tooltip("완료 여부를 체크할 엔딩의 ID")]
    public int requiredEndingID;

    public override bool Evaluate(PlayerStats playerStats, PlaythroughHistory playthroughHistory)
    {
        if (playthroughHistory == null) return false;
        return playthroughHistory.HasCompletedEnding(requiredEndingID);
    }
}

/// <summary>
/// 이벤트의 각 선택지를 정의
/// </summary>
[System.Serializable]
public class EventChoice
{
    [Tooltip("선택지 텍스트")]
    public string choiceText;

    [Tooltip("선택지의 성공 조건")]
    [SerializeReference] // 다형성 직렬화
    public SuccessCondition condition = new GuaranteedSuccessCondition();

    [Tooltip("성공 시 결과")]
    public ChoiceOutcome successOutcome;
    [Tooltip("실패 시 결과")]
    public ChoiceOutcome failOutcome;
}

/// <summary>
/// 하나의 파라미터 이벤트를 구성하는 모든 데이터
/// </summary>
[System.Serializable]
public class EventData
{
    [Header("이벤트 기본 정보")]
    public int id;
    public int eventName;
    public Sprite eventSprite;
    public string RoundType;
    public string ConditionType;
    public string CharacterName;
    public string CharacterImage;
    public bool IsConditionSuccess;
    public string BG;
    public string SE;

    [TextArea(3, 10)]
    [Tooltip("이벤트 상황에 표시될 메인 대화 내용")]
    public string dialogue;

    [Header("선택지")]
    public EventChoice leftChoice;
    public EventChoice rightChoice;
}