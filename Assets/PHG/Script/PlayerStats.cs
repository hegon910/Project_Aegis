using UnityEngine;
using System.Collections.Generic;
using System;
// namespace PHG 삭제
public class PlayerStats : MonoBehaviour
{

    public static PlayerStats Instance { get; private set; }

    // 능력치 변경 이벤트를 선언합니다.
    // <어떤 능력치가, 몇 만큼 변해서, 현재 몇이 되었는지>를 알려줍니다.
    public static event Action<ParameterType, int, int> OnStatChanged;

    private Dictionary<ParameterType, int> stats = new Dictionary<ParameterType, int>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeStats();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeStats()
    {
        stats[ParameterType.정치력] = 50;
        stats[ParameterType.병력] = 50;
        stats[ParameterType.물자] = 80;
        stats[ParameterType.리더십] = 50;
        stats[ParameterType.전황] = 50;
        stats[ParameterType.카르마] = 50;
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

                // 능력치 변경 이벤트를 방송합니다.
                OnStatChanged?.Invoke(change.parameterType, change.valueChange, stats[change.parameterType]);
            }
        }
    }
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