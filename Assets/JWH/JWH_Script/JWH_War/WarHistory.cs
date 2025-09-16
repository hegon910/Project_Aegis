using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarHistory
{
    public static List<bool> results = new List<bool>();
    private const int maxRecords = 6; // 기록칸수
    public static void RecordWarResult(bool didWin)
    {
        // 리스트에 새로운 결과를 추가
        results.Add(didWin);

        // 가장 오래된 기록을 삭제인데 초기화 하지 않을까?
        //if (results.Count > maxRecords)
        //{
        //    results.RemoveAt(0);
        //}
        //Debug.Log($"전투 결과 기록됨: {(didWin ? "승리" : "패배")}. 현재 기록 수: {results.Count}개");
    }
}
