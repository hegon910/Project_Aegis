using UnityEngine;
using System.Collections.Generic;
using System;
// namespace PHG ����
public class PlayerStats : MonoBehaviour
{

    public static PlayerStats Instance { get; private set; }

    // �ɷ�ġ ���� �̺�Ʈ�� �����մϴ�.
    // <� �ɷ�ġ��, �� ��ŭ ���ؼ�, ���� ���� �Ǿ�����>�� �˷��ݴϴ�.
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
        stats[ParameterType.��ġ��] = 50;
        stats[ParameterType.����] = 50;
        stats[ParameterType.����] = 80;
        stats[ParameterType.������] = 50;
        stats[ParameterType.����] = 50;
        stats[ParameterType.ī����] = 50;
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
                Debug.Log($"<color=cyan>���� ����: {change.parameterType}��(��) {change.valueChange}��ŭ ����Ǿ� ���� {stats[change.parameterType]}�Դϴ�.</color>");

                // �ɷ�ġ ���� �̺�Ʈ�� ����մϴ�.
                OnStatChanged?.Invoke(change.parameterType, change.valueChange, stats[change.parameterType]);
            }
        }
    }
}

// ParameterType enum�� EventData.cs�� ���ǵǾ� ���� ������ ����˴ϴ�.
// ���� ���ٸ� �Ʒ� �ڵ带 PlayerStats.cs ���� �ϴܿ� �߰����ּ���.
/*
public enum ParameterType
{
    ��ġ��,
    ����,
    ����,
    ������,
    ����,
    ī����
}
*/