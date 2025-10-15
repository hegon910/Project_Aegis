using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class WarHistoryUI : MonoBehaviour
{
    [Tooltip("전투 기록을 표시할 6개의 UI 이미지들")]
    [SerializeField] private Image[] recordImages;

    [Tooltip("승리 색상")]
    [SerializeField] private Sprite winSprite;

    [Tooltip("패배 색상")]
    [SerializeField] private Sprite lossSprite;

    [Tooltip("무승부 색상")]
    [SerializeField] private Sprite drawSprite;

    [Tooltip("기록이 없을 때의 기본 색상")]
    [SerializeField] private Sprite defaultSprite;

    [Header("현재 챕터 표시")]
    [Tooltip("현재 진행 중인 챕터 이미지")]
    [SerializeField] private Sprite currentChapterSprite;

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

        int battlesFought = battleRecords.Count;

        for (int i = 0; i < recordImages.Length; i++)
        {
            if (recordImages[i] == null) continue;

            if (i < battleRecords.Count)
            {
                string outcome = battleRecords[i].selectedChoice;
                switch (outcome)
                {
                    case "승리":
                        recordImages[i].sprite = winSprite;
                        break;
                    case "패배":
                        recordImages[i].sprite = lossSprite;
                        break;
                    case "무승부":
                        recordImages[i].sprite = drawSprite;
                        break;
                    default:
                        recordImages[i].sprite = defaultSprite;
                        break;
                }
                recordImages[i].color = Color.white;
            }
            else if (i == battlesFought) // 현재 진행 중인 전투
            {
                recordImages[i].sprite = currentChapterSprite; // 현재 챕터 이미지
                recordImages[i].color = Color.white;
            }
            else
            {
                recordImages[i].sprite = defaultSprite;

                if (defaultSprite == null)
                {
                    recordImages[i].color = Color.clear;
                }
                else
                {
                    recordImages[i].color = Color.white;
                }
            }
        }
    }
}