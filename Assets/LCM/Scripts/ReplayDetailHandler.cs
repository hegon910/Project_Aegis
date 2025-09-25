using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections.Generic;

public class ReplayDetailHandler : MonoBehaviour
{
    public GameObject detailPanel;
    public TextMeshProUGUI playDateText;
    public TextMeshProUGUI playDurationText;
    public TextMeshProUGUI playthroughInfoText;

    // 챕터별 결과를 표시할 6개의 Text 컴포넌트 배열
    public TextMeshProUGUI[] chapterResultTexts;

    private void Awake()
    {
        if (detailPanel != null)
        {
            detailPanel.SetActive(false);
        }
    }

    public void ShowDetails(GamePlaythroughRecord record)
    {
        detailPanel.SetActive(true);

        playDateText.text = $"플레이 날짜: {record.playDate}";
        playDurationText.text = $"플레이 시간: {record.playDuration}";
        playthroughInfoText.text = $"회차 정보: {record.outcome}";

        // 결과와 이벤트 기록 섹션 업데이트
        UpdateChapterResults(record.eventHistory);
    }

    private void UpdateChapterResults(List<SimpleEventRecord> events)
    {
        // 챕터 번호와 결과 텍스트를 매칭하여 표시합니다.
        for (int i = 0; i < chapterResultTexts.Length; i++)
        {
            int chapterNum = i + 1;
            string outcome = "기록 없음";

            // 해당 챕터의 전투 결과를 찾습니다.
            SimpleEventRecord chapterRecord = events.FirstOrDefault(e => e.chapter == chapterNum && e.eventType == "BattleResult");
            if (chapterRecord != null)
            {
                outcome = chapterRecord.selectedChoice; // '승리'/'패배'/'무승부'
            }

            // Text 컴포넌트의 텍스트를 업데이트합니다.
            if (chapterResultTexts[i] != null)
            {
                chapterResultTexts[i].text = $"{chapterNum}장: {outcome}";
            }
        }
    }

    public void CloseDetailPanel()
    {
        detailPanel.SetActive(false);
    }
}