using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }
    public static event Action<ParameterType, int, int> OnStatChanged;

    private Dictionary<ParameterType, int> stats = new Dictionary<ParameterType, int>();

    public CommanderTrait ActiveTrait { get; private set; } = CommanderTrait.None;
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


        //********************세라 선택 시 '잃어버린 내면의 마음' 패시브******************************
        if (ActiveTrait == CommanderTrait.Sera)
        {
            Debug.Log($"<color=yellow>[특성 발동] 잃어버린 내면의 거울 2회 한정: 주요 스탯 +10</color>");
            var keys = stats.Keys.ToList();
            foreach (var key in keys)
            {
                if(key != ParameterType.카르마 && key != ParameterType.전황)
                {
                    stats[key] = Mathf.Clamp(stats[key] + 10, 0, 100);
                }
            }
        }
        //****************************************************************************************
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
                //********************알렉스 선택 시 '전장의 패권자' 패시브******************************
                int finalChange = change.valueChange;
                if (ActiveTrait == CommanderTrait.Alex && change.parameterType == ParameterType.정치력 && finalChange < 0)
                {
                    Debug.Log($"<color=yellow>[특성 발동] 전장의 파견자: 정치력 감소({finalChange})를 무시합니다.</color>");
                    continue; // 이 스탯 변경 로직을 건너뛰고 다음 변경으로 넘어감
                }
                //****************************************************************************************
                // *************** 시온 선택시 '감시망 네트워크' 패시브 ******************
                if (ActiveTrait == CommanderTrait.Cion && finalChange > 0)
                {
                    int bonus = 2;
                    if (change.parameterType == ParameterType.카르마)
                    {
                        bonus += 6;
                    }
                    finalChange += bonus;
                    Debug.Log($"<color=yellow>[특성 발동] 감시망 네트워크: 보상이 {change.valueChange}에서 {finalChange}(으)로 증가.</color>");
                }
                // ******************************************************************
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
    None,
    Alex,
    Sera,
    Cion
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