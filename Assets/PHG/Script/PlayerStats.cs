using UnityEngine;
using System.Collections.Generic;
using System;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    // private Dictionary<ParameterType, int> stats = new Dictionary<ParameterType, int>();
    // public int playthroughCount { get; set; } = 1;
    // public List<int> completedEventIds { get; private set; } = new List<int>();


    public static event Action<ParameterType, int, int> OnStatChanged;
    public static event Action OnActiveCommanderChanged;
    public CommanderInfo ActiveCommander { get; private set; }
    public CommanderTrait ActiveTrait
    {
        get
        {
            if (DataManager.Instance?.PlayerData != null)
            {
                return DataManager.Instance.PlayerData.activeTrait;
            }
            return CommanderTrait.Devost; // 데이터가 없을 경우 기본값 반환
        }
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// DataManager로부터 데이터를 받아와 게임 상태를 설정합니다.
    /// </summary>
    public void ApplyLoadedData(GameData data)
    {
        // 데이터 적용 시 로그 추가
        Debug.Log($"[PlayerStats] 로드된 데이터 적용 시작. 회차: {data.playthroughCount}, 챕터: {data.currentChapter}");
    }

    /// <summary>
    /// 현재 활성화된 지휘관 정보를 설정합니다.
    /// </summary>
    public void SetActiveCommander(CommanderInfo commanderInfo)
    {
        ActiveCommander = commanderInfo;

        // [복구] 지휘관이 선택되면, DataManager에 해당 지휘관의 Trait을 기록합니다.
        if (DataManager.Instance?.PlayerData != null)
        {
            if (commanderInfo.traitLogic != null)
            {
                DataManager.Instance.PlayerData.activeTrait = commanderInfo.traitLogic.identifier;
            }
            else
            {
                DataManager.Instance.PlayerData.activeTrait = CommanderTrait.Devost;
            }
            Debug.Log($"<color=lime>지휘관 특성 활성화: {DataManager.Instance.PlayerData.activeTrait}</color>");
        }
        OnActiveCommanderChanged?.Invoke();
    }

    /// <summary>
    /// 특정 스탯의 현재 값을 가져옵니다. DataManager의 데이터를 직접 참조합니다.
    /// </summary>
    public int GetStat(ParameterType type)
    {
        if (DataManager.Instance?.PlayerData == null) return 0;

        switch (type)
        {
            case ParameterType.정치력: return DataManager.Instance.PlayerData.politics;
            case ParameterType.병력: return DataManager.Instance.PlayerData.militaryPower;
            case ParameterType.물자: return DataManager.Instance.PlayerData.supplies;
            case ParameterType.리더십: return DataManager.Instance.PlayerData.leadership;
            case ParameterType.전황: return DataManager.Instance.PlayerData.warSituation;
            case ParameterType.카르마: return DataManager.Instance.PlayerData.karma;
            default: return 0;
        }
    }

    /// <summary>
    /// 파라미터 변경 사항들을 적용합니다.
    /// </summary>
    public void ApplyChanges(List<ParameterChange> changes)
    {
        if (DataManager.Instance?.PlayerData == null) return;

        foreach (var change in changes)
        {
            int oldValue = GetStat(change.parameterType);
            int newValue = 0;

            switch (change.parameterType)
            {
                case ParameterType.정치력:
                    newValue = DataManager.Instance.PlayerData.politics = Mathf.Clamp(oldValue + change.valueChange, 0, 100);
                    break;
                case ParameterType.병력:
                    newValue = DataManager.Instance.PlayerData.militaryPower = Mathf.Clamp(oldValue + change.valueChange, 0, 100);
                    break;
                // ... (다른 파라미터들도 동일하게 추가)
                case ParameterType.물자:
                    newValue = DataManager.Instance.PlayerData.supplies = Mathf.Clamp(oldValue + change.valueChange, 0, 100);
                    break;
                case ParameterType.리더십:
                    newValue = DataManager.Instance.PlayerData.leadership = Mathf.Clamp(oldValue + change.valueChange, 0, 100);
                    break;
                case ParameterType.전황:
                    newValue = DataManager.Instance.PlayerData.warSituation = Mathf.Clamp(oldValue + change.valueChange, 0, 100);
                    break;
                case ParameterType.카르마:
                    newValue = DataManager.Instance.PlayerData.karma = Mathf.Clamp(oldValue + change.valueChange, 0, 100);
                    break;
            }
            Debug.Log($"<color=cyan>스탯 변경: {change.parameterType}이(가) {change.valueChange}만큼 변경되어 현재 {newValue}입니다.</color>");
            OnStatChanged?.Invoke(change.parameterType, change.valueChange, newValue);
        }
        // 모든 변경 적용 후 게임 오버 체크 및 저장
        GameManager.instance.CheckGameOverConditions();
    }
    /// <summary>
    /// 특정 파라미터의 값을 지정된 수치로 즉시 설정합니다.
    /// </summary>
    public void SetStat(ParameterType type, int value)
    {
        if (DataManager.Instance?.PlayerData == null) return;

        int oldValue = GetStat(type);
        int clampedValue = Mathf.Clamp(value, 0, 100);

        switch (type)
        {
            case ParameterType.정치력: DataManager.Instance.PlayerData.politics = clampedValue; break;
            case ParameterType.병력: DataManager.Instance.PlayerData.militaryPower = clampedValue; break;
            case ParameterType.물자: DataManager.Instance.PlayerData.supplies = clampedValue; break;
            case ParameterType.리더십: DataManager.Instance.PlayerData.leadership = clampedValue; break;
            case ParameterType.전황: DataManager.Instance.PlayerData.warSituation = clampedValue; break;
            case ParameterType.카르마: DataManager.Instance.PlayerData.karma = clampedValue; break;
        }

        OnStatChanged?.Invoke(type, clampedValue - oldValue, clampedValue);
        GameManager.instance.CheckGameOverConditions();
    }
}
public enum CommanderTrait
{
    Devost,
    Wille,
    Risard
}