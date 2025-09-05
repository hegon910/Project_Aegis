using UnityEngine;

public static class UnlockManager
{
    private const string UnlockKeyPrefix = "Commander_Unlocked_";

    public static bool IsUnlocked(CommanderTrait trait)
    {
        // '데보스트'는 기획서대로 기본 캐릭터이므로 항상 해금 상태입니다.
        if (trait == CommanderTrait.Devost)
        {
            return true;
        }

        // PlayerPrefs에 저장된 값이 1이면 해금, 아니면 잠금 상태입니다.
        return PlayerPrefs.GetInt(UnlockKeyPrefix + trait.ToString(), 0) == 1;
    }
    public static void Lock(CommanderTrait trait)
    {
        // Devost는 잠글 수 없습니다.
        if (trait == CommanderTrait.Devost) return;

        PlayerPrefs.SetInt(UnlockKeyPrefix + trait.ToString(), 0);
        PlayerPrefs.Save();
        Debug.Log($"<color=orange>[시스템] {trait} 지휘관을 다시 잠갔습니다.</color>");
    }
    public static void Unlock(CommanderTrait trait)
    {
        PlayerPrefs.SetInt(UnlockKeyPrefix + trait.ToString(), 1);
        PlayerPrefs.Save(); // 변경사항을 디스크에 즉시 저장
        Debug.Log($"<color=cyan>[시스템] {trait} 지휘관이 해금되었습니다!</color>");
    }

    public static void ResetAllUnlocks()
    {
        // CommanderTrait Enum의 모든 값을 순회합니다.
        foreach (CommanderTrait trait in System.Enum.GetValues(typeof(CommanderTrait)))
        {
            // Devost는 건너뜁니다.
            if (trait == CommanderTrait.Devost) continue;

            PlayerPrefs.DeleteKey(UnlockKeyPrefix + trait.ToString());
        }
        Debug.LogWarning("[시스템] 모든 지휘관 해금 정보가 초기화되었습니다.");
    }
}