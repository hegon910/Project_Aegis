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
    public TextMeshProUGUI detailedMemoirText;

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

        string formattedDate = record.playDate;
        if (System.DateTime.TryParse(record.playDate, out System.DateTime dateValue))
        {
            // 날짜 부분만 포맷
            formattedDate = dateValue.ToString("yyyy-MM-dd");
        }
        else
        {
            // 파싱 실패 시, 문자열을 공백 기준으로 나누어 날짜만 사용 시도
            formattedDate = record.playDate.Split(' ')[0];
        }

        playDateText.text = $"플레이 날짜: {formattedDate}";
        playDurationText.text = $"플레이 시간: {record.playDuration}";
        playthroughInfoText.text = $"회차 정보: {record.playthroughCount}회차 {record.outcome}";

        // 결과와 이벤트 기록 섹션 업데이트
        UpdateChapterResults(record.eventHistory);

        BuildDetailedHistoryText(record.eventHistory);
    }


    //선택지 데이터 출력
    private void BuildDetailedHistoryText(List<SimpleEventRecord> events)
    {
        if (detailedMemoirText == null) return;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        bool foundMemoir = false;

        foreach (var record in events)
        {
            if (record.eventType == "BattleResult") continue;

            if (record.isCompleted)
            {
                foundMemoir = true;

                sb.AppendLine("------------------------------------");
                sb.AppendLine($"<size=110%> 챕터 {record.chapter} 중요 선택</size>");
                sb.AppendLine($"지문: {record.dialogue}");
                sb.AppendLine($"선택: <color=#FFFF00>{record.selectedChoice}</color>");
            }
        }

        if (!foundMemoir)
        {
            sb.AppendLine("해당 회차에서 기록된 중요 선택지(Memoriar)가 없습니다.");
        }

        detailedMemoirText.text = sb.ToString();
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