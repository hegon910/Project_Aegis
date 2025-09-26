using System.Collections;
using System.Collections.Generic;
using UnityEngine;


    public static class WarHistory
    {
    public static List<bool> results = new List<bool>();
    private const int maxRecords = 6; // 한 회차당 최대 기록할 전투 수


    public static void RecordWarResult(bool didWin)
    {
        if (results.Count >= maxRecords)
        {
            Debug.LogWarning($"전투 기록이 최대 {maxRecords}개에 도달하여 더 이상 기록하지 않습니다");
            return;
        }

        results.Add(didWin);
        Debug.Log($"전투 결과 기록됨: {(didWin ? "승리" : "패배")}. 현재 기록 수: {results.Count} / {maxRecords}개");
    }

    public static void ResetHistory()
    {
        results.Clear();
        Debug.Log("전투 기록(WarHistory)이 초기화");
    }
}