using UnityEngine;
using System.Collections.Generic;

public static class UnlockManager
{
    private const string UnlockKeyPrefix = "Commander_Unlocked_"; // legacy PlayerPrefs prefix
    private static bool _migrationAttempted = false;

    private static SettingsData Settings => DataManager.Instance?.PlayerSettings;

    private static void EnsureSettingsReady()
    {
        if (DataManager.Instance == null) return;
        if (DataManager.Instance.PlayerSettings == null)
        {
            DataManager.Instance.LoadSettings();
        }
        // 1회 마이그레이션: 기존 PlayerPrefs 기반 해금을 Settings로 이전
        if (!_migrationAttempted)
        {
            _migrationAttempted = true;
            MigrateLegacyPlayerPrefsToSettings();
        }
        // Settings 필드가 없다면 안전 초기화 (Devost만 해금)
        if (Settings.unlockedCommanderTraitIds == null)
        {
            Settings.unlockedCommanderTraitIds = new List<int> { (int)CommanderTrait.Devost };
            DataManager.Instance.SaveSettings();
        }
    }

    private static void MigrateLegacyPlayerPrefsToSettings()
    {
        if (Settings == null) return;
        if (Settings.unlockedCommanderTraitIds == null)
        {
            Settings.unlockedCommanderTraitIds = new List<int>();
        }
        bool changed = false;
        foreach (CommanderTrait trait in System.Enum.GetValues(typeof(CommanderTrait)))
        {
            if (trait == CommanderTrait.Devost)
            {
                if (!Settings.unlockedCommanderTraitIds.Contains((int)CommanderTrait.Devost))
                {
                    Settings.unlockedCommanderTraitIds.Add((int)CommanderTrait.Devost);
                    changed = true;
                }
                continue;
            }
            int v = PlayerPrefs.GetInt(UnlockKeyPrefix + trait.ToString(), -1);
            if (v == 1)
            {
                if (!Settings.unlockedCommanderTraitIds.Contains((int)trait))
                {
                    Settings.unlockedCommanderTraitIds.Add((int)trait);
                    changed = true;
                }
                PlayerPrefs.DeleteKey(UnlockKeyPrefix + trait.ToString());
            }
            else if (v == 0)
            {
                PlayerPrefs.DeleteKey(UnlockKeyPrefix + trait.ToString());
            }
        }
        if (changed)
        {
            DataManager.Instance.SaveSettings();
        }
    }

    public static bool IsUnlocked(CommanderTrait trait)
    {
        EnsureSettingsReady();
        // '데보스트'는 기획서대로 기본 캐릭터이므로 항상 해금 상태입니다.
        if (trait == CommanderTrait.Devost)
        {
            return true;
        }
        if (Settings == null) return false;
        return Settings.unlockedCommanderTraitIds != null && Settings.unlockedCommanderTraitIds.Contains((int)trait);
    }
    public static void Lock(CommanderTrait trait)
    {
        // Devost는 잠글 수 없습니다.
        if (trait == CommanderTrait.Devost) return;
        EnsureSettingsReady();
        if (Settings == null) return;
        if (Settings.unlockedCommanderTraitIds == null)
        {
            Settings.unlockedCommanderTraitIds = new List<int> { (int)CommanderTrait.Devost };
        }
        Settings.unlockedCommanderTraitIds.Remove((int)trait);
        DataManager.Instance.SaveSettings();
        Debug.Log($"<color=orange>[시스템] {trait} 지휘관을 다시 잠갔습니다.</color>");
    }
    public static void Unlock(CommanderTrait trait)
    {
        EnsureSettingsReady();
        if (Settings == null) return;
        if (Settings.unlockedCommanderTraitIds == null)
        {
            Settings.unlockedCommanderTraitIds = new List<int> { (int)CommanderTrait.Devost };
        }
        if (!Settings.unlockedCommanderTraitIds.Contains((int)trait))
        {
            Settings.unlockedCommanderTraitIds.Add((int)trait);
            DataManager.Instance.SaveSettings();
        }
        Debug.Log($"<color=cyan>[시스템] {trait} 지휘관이 해금되었습니다!</color>");
    }

    public static void ResetAllUnlocks()
    {
        EnsureSettingsReady();
        if (Settings == null) return;
        Settings.unlockedCommanderTraitIds = new List<int> { (int)CommanderTrait.Devost };
        DataManager.Instance.SaveSettings();
        Debug.LogWarning("[시스템] 모든 지휘관 해금 정보가 초기화되었습니다.");
    }
}