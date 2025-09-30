using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class WarHistoryUI : MonoBehaviour
{
    [Tooltip("전투 기록을 표시할 6개의 UI 이미지들")]
    [SerializeField] private Image[] recordImages;

    [Tooltip("승리 색상")]
    [SerializeField] private Color winColor = Color.cyan;

    [Tooltip("패배 색상")]
    [SerializeField] private Color lossColor = Color.red;

    [Tooltip("무승부 색상")]
    [SerializeField] private Color drawColor = Color.white;

    [Tooltip("기록이 없을 때의 기본 색상")]
    [SerializeField] private Color defaultColor = Color.gray;

    void OnEnable()
    {
        UpdateWarDisplay();
    }

    public void UpdateWarDisplay()
    {
        List<SimpleEventRecord> battleRecords = new List<SimpleEventRecord>();
        if (SimpleEventHistoryManager.Instance != null)
        {
            battleRecords = SimpleEventHistoryManager.Instance.GetEventHistory()
                .Where(record => record.eventType == "BattleResult")
                .ToList();
        }

        for (int i = 0; i < recordImages.Length; i++)
        {
            if (recordImages[i] == null) continue;

            if (i < battleRecords.Count)
            {
                string outcome = battleRecords[i].selectedChoice;
                switch (outcome)
                {
                    case "승리":
                        recordImages[i].color = winColor;
                        break;
                    case "패배":
                        recordImages[i].color = lossColor;
                        break;
                    case "무승부":
                        recordImages[i].color = drawColor;
                        break;
                    default:
                        recordImages[i].color = defaultColor;
                        break;
                }
            }
            else
            {
                recordImages[i].color = defaultColor;
            }
        }
    }
}