using UnityEngine;

public static class ProgressResetService
{
    public static void ResetProgressAndHistory(bool clearPlaythroughHistory = true, bool resetPlaythroughCount = true, bool resetStats = true, bool resetChapter = true)
    {
        if (clearPlaythroughHistory)
        {
            // HCW의 PlaythroughHistory에는 ClearHistory 메서드가 없으므로 직접 PlayerPrefs 삭제
            if (global::PlaythroughHistory.Instance != null)
            {
                PlayerPrefs.DeleteKey("PlaythroughHistory_Events");
                PlayerPrefs.DeleteKey("PlaythroughHistory_Endings");
                PlayerPrefs.DeleteKey("PlaythroughHistory_BattleResult");
                PlayerPrefs.Save();
            }
        }

        var dm = DataManager.Instance;
        if (dm == null)
        {
            Debug.LogError("[ProgressResetService] DataManager.Instance is null");
            return;
        }

        if (dm.PlayerData == null)
        {
            Debug.LogError("[ProgressResetService] DataManager.PlayerData is null");
            return;
        }

        var pd = dm.PlayerData;
        if (pd != null)
        {
            // 진행도/기록 리스트 초기화
            pd.completedEventIds?.Clear();
            pd.playedSubEventGroups?.Clear();
            pd.completedBattleResultIds?.Clear();
            pd.completedEndingIds?.Clear();

            if (resetChapter)
            {
                pd.currentChapter = 1;
                pd.currentPlaylist?.Clear();
                pd.eventPlaylistIndex = 0;
                pd.pendingRestartFromGameOver = false;
                pd.pendingRestartChapter = 0;
                pd.isTutorialFinished = false;
            }

            if (resetPlaythroughCount)
            {
                pd.playthroughCount = 1;
            }

            if (resetStats)
            {
                pd.activeTrait = CommanderTrait.Devost;
                pd.politics = 50;
                pd.militaryPower = 50;
                pd.supplies = 50;
                pd.leadership = 50;
                pd.warSituation = 50;
                pd.karma = 50;
            }
        }

        // 저장 억제가 켜져있을 수 있으므로 임시로 해제하고 저장 후 복구
        var dmType = typeof(DataManager);
        var suppressField = dmType.GetField("_suppressSavesUntilGameplay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        bool prevSuppress = false;
        if (suppressField != null)
        {
            prevSuppress = (bool)suppressField.GetValue(dm);
            suppressField.SetValue(dm, false);
        }
        dm.SaveData();
        if (suppressField != null)
        {
            suppressField.SetValue(dm, prevSuppress);
        }
    }
}


