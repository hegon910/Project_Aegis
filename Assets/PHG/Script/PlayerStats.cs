using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }
    public static event Action<ParameterType, int, int> OnStatChanged;

    private Dictionary<ParameterType, int> stats = new Dictionary<ParameterType, int>();

    public CommanderInfo ActiveCommander { get; private set; }
    public CommanderTrait ActiveTrait { get; private set; } = CommanderTrait.Devost;
    public int playthroughCount { get; set; } = 1;
    public List<int> completedEventIds { get; private set; } = new List<int>();

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void EditorReset()
    {
        Instance = null;
    }
#endif

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
#if UNITY_EDITOR
            // 에디터에서 테스트할 때만 자동으로 초기화합니다.
            // 실제 빌드에서는 메뉴에서 '새 게임'을 눌렀을 때 초기화해야 합니다.
          //  InitializeStats();
#endif
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void InitializeStats()
    {
        stats.Clear();
        stats[ParameterType.정치력] = 50;
        stats[ParameterType.병력] = 50;
        stats[ParameterType.물자] = 50;
        stats[ParameterType.리더십] = 50;
        stats[ParameterType.전황] = 50;
        stats[ParameterType.카르마] = 50;

        playthroughCount = 1;
        completedEventIds.Clear();

        if (ActiveCommander == null)
        {
            Debug.LogError("InitializeStats 호출 시점에 ActiveCommander가 null입니다!");
        }
        else
        {
            Debug.Log($"ActiveCommander는 {ActiveCommander.gameObject.name}입니다. initialStatAdjustments 리스트의 항목 개수는 {ActiveCommander.initialStatAdjustments.Count}개 입니다.");
        }
        //--------------------------

        if (ActiveCommander != null && ActiveCommander.initialStatAdjustments.Count > 0)
        {
            Debug.Log($"<color=yellow>[{ActiveCommander.gameObject.name}] 지휘관의 초기 스탯 보너스를 적용합니다.</color>");
            ApplyChanges(ActiveCommander.initialStatAdjustments);
        }
    }

    public void SetActiveCommander(CommanderInfo commanderInfo)
    {
        ActiveCommander = commanderInfo;
        // 개선안을 적용했다면 Trait Logic에서 enum을 가져옵니다.
        if (commanderInfo.traitLogic != null)
        {
            ActiveTrait = commanderInfo.traitLogic.identifier;
        }
        else // Trait Logic이 없는 기본 지휘관(데보스트)의 경우
        {
            ActiveTrait = CommanderTrait.Devost;
        }
        Debug.Log($"<color=lime>지휘관 활성화: {ActiveCommander.gameObject.name}</color>");
    }
    public void ApplyImmediateBonus(ParameterType type, int amount)
    {
        if (stats.ContainsKey(type))
        {
            Debug.Log($"<color=cyan>[특성 보너스] {type}에 {amount}만큼 즉시 적용!</color>");
            int oldValue = stats[type];
            stats[type] = Mathf.Clamp(oldValue + amount, 0, 100);
            OnStatChanged?.Invoke(type, stats[type] - oldValue, stats[type]);
            GameManager.instance.CheckGameOverConditions();
        }
    }
    public int GetStat(ParameterType type)
    {
        return stats.ContainsKey(type) ? stats[type] : 0;
    }

    public void ApplyChanges(List<ParameterChange> changes)
    {


        foreach (var change in changes)
        {
            if (stats.ContainsKey(change.parameterType))
            {
                stats[change.parameterType] += change.valueChange;
                Debug.Log($"<color=cyan>스탯 변경: {change.parameterType}이(가) {change.valueChange}만큼 변경되어 현재 {stats[change.parameterType]}입니다.</color>");
                OnStatChanged?.Invoke(change.parameterType, change.valueChange, stats[change.parameterType]);

                // [수정] 불필요한 중간 메서드 대신 게임오버 조건을 직접 확인하도록 변경
                // GameManager.instance.OnParameterChanged(); (기존 코드)
                GameManager.instance.CheckGameOverConditions(); // (수정된 코드)
            }
        }
    }

    public void StartNewPlaythrough()
    {
        playthroughCount++;

        stats.Clear();
        stats[ParameterType.정치력] = 50;
        stats[ParameterType.병력] = 50;
        stats[ParameterType.물자] = 50;
        stats[ParameterType.리더십] = 50;
        stats[ParameterType.전황] = 50;
        stats[ParameterType.카르마] = 50;
        
        Debug.Log($"({playthroughCount})회차를 시작합니다. 스탯이 초기화되었습니다.");
       
    }

    public void AddCompletedEvent(int eventId)
    {
        if (!completedEventIds.Contains(eventId))
        {
            completedEventIds.Add(eventId);
        }
    }

    /// <summary>
    /// 특정 파라미터의 값을 지정된 수치로 즉시 설정합니다. (치트용)
    /// </summary>
    public void SetStat(ParameterType type, int value)
    {
        if (stats.ContainsKey(type))
        {
            int oldValue = stats[type];
            // 값의 범위를 0~100 사이로 제한
            stats[type] = Mathf.Clamp(value, 0, 100);

            int changeAmount = stats[type] - oldValue;

            Debug.Log($"<color=orange>[치트] 스탯 설정: {type}을(를) {stats[type]}(으)로 설정.</color>");

            // UI가 변경사항을 인지하도록 OnStatChanged 이벤트를 호출합니다.
            // changeAmount가 0이어도 UI 즉시 업데이트를 위해 이벤트를 호출하도록 합니다.
            OnStatChanged?.Invoke(type, changeAmount, stats[type]);

            // 게임 오버 조건도 확인합니다.
            GameManager.instance.CheckGameOverConditions();
        }
    }
    public void SetCommanderTrait(CommanderTrait trait)
    {
        ActiveTrait = trait;
        Debug.Log($"커맨더 특성 설정: {trait}");
        Debug.Log($"<color=lime>지휘관 특성 활성화 완료: {ActiveTrait}</color>");
    }
}

public enum CommanderTrait
{
    Devost,
    Wille,
    Risard
}


// ParameterType enum은 EventData.cs에 정의되어 있을 것으로 예상됩니다.
// 만약 없다면 아래 코드를 PlayerStats.cs 파일 하단에 추가해주세요.
/*
public enum ParameterType
{
    정치력,
    병력,
    물자,
    리더십,
    전세,
    카르마
}
*/