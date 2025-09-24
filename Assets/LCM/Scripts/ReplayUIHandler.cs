using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ReplayUIHandler : MonoBehaviour
{
    public Transform recordContent;
    public GameObject recordButtonPrefab;

    private void OnEnable()
    {
        RefreshRecordList();
    }

    public void RefreshRecordList()
    {
        if (SimpleEventHistoryManager.Instance == null)
        {
            Debug.LogWarning("[ReplayUIHandler] SimpleEventHistoryManager 인스턴스를 찾을 수 없습니다.");
            return;
        }
        foreach (Transform child in recordContent)
        {
            Destroy(child.gameObject);
        }

        List<GamePlaythroughRecord> records = SimpleEventHistoryManager.Instance.GetPlaythroughHistory();

        // 최신 기록이 위로 오도록 역순으로 순회
        for (int i = records.Count - 1; i >= 0; i--)
        {
            GamePlaythroughRecord record = records[i];

            GameObject newButton = Instantiate(recordButtonPrefab, recordContent);

            string buttonText = $"{record.playDate} | {record.playDuration} | {record.outcome}";
            newButton.GetComponentInChildren<Text>().text = buttonText;
        }
    }
}