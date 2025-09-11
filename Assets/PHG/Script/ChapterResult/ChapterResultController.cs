using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using DG.Tweening;

public class ChapterResultController : MonoBehaviour
{
    [Header("UI 요소 연결")]
    [SerializeField] private GameObject endPanel;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI dateText;
    [SerializeField] private TextMeshProUGUI titleText; // "승리", "패배" 등
    [SerializeField] private TextMeshProUGUI summaryText;
    [SerializeField] private Button continueButton;

    //연출이 끝났음을 EventManager에게 알리는 이벤트
    public static event Action OnSequenceComplete;

    private void Start()
    {
        continueButton.onClick.AddListener(OnContinueButtonClicked);
        endPanel.SetActive(false);
    }

    ///<summary>
    ///EventManager가 호출할 진입점 함수
    /// </summary>
    /// 

    public void StartChapterEndSequence(ChapterResultData data, GameOutcome outcome)
    {
        endPanel.SetActive(true);
        continueButton.gameObject.SetActive(false); // 처음에는 버튼 숨기기
        summaryText.text = "";

        //결과에 따라 제목과 요약 텍스트
        string title;
        string summary;
        Sprite resultBackgroundImage; // ▼▼▼ 이미지를 담을 임시 변수 선언 ▼▼▼

        switch (outcome)
        {
            case GameOutcome.Victory:
                title = "승리";
                summary = data.victorySummary;
                resultBackgroundImage = data.victoryImage; // ▼▼▼ 승리 이미지 선택 ▼▼▼
                break;
            case GameOutcome.Defeat:
                title = "패배";
                summary = data.defeatSummary;
                resultBackgroundImage = data.defeatImage; // ▼▼▼ 패배 이미지 선택 ▼▼▼
                break;
            default: // Draw
                title = "무승부";
                summary = data.drawSummary;
                resultBackgroundImage = data.drawImage;   // ▼▼▼ 무승부 이미지 선택 ▼▼▼
                break;
        }
        backgroundImage.sprite = resultBackgroundImage;
        titleText.text = title;

        StartCoroutine(PlaySequence(data, title, summary));

    }

    private IEnumerator PlaySequence(ChapterResultData data, string title, string summary)
    {


        // ▼▼▼ 연출 : 6개월간 시간이 흐르는 애니메이션 호출 ▼▼▼
        yield return StartCoroutine(AnimateDateFlow(data));

        yield return new WaitForSeconds(0.5f); // 날짜 애니메이션 후 잠시 대기

        // 연출 : 줄거리 요약 텍스트 타이핑 효과
        foreach (char c in summary)
        {
            summaryText.text += c;
            yield return new WaitForSeconds(0.05f);
        }

        yield return new WaitForSeconds(0.5f);

        // 연출 4: 다음 버튼 활성화
        continueButton.gameObject.SetActive(true);
        AnimateContinueButton();
    }
    private void AnimateContinueButton()
    {
        // 현재 스케일 값을 저장
        Vector3 originalScale = continueButton.transform.localScale;

        // 버튼의 Transform에 대한 모든 트윈을 먼저 정지시켜 중복 실행을 방지
        continueButton.transform.DOKill();

        continueButton.transform.DOScale(originalScale * 2f, 1f) // 1. 목표 크기: 원래 크기의 1.1배, 2. 시간: 1초
            .SetEase(Ease.InOutSine)      // 부드러운 움직임 효과 (시작과 끝이 느려짐)
            .SetLoops(-1, LoopType.Yoyo); // 루프 설정: -1(무한 반복), Yoyo(갔다가 돌아오는 방식)
    }

    private IEnumerator AnimateDateFlow(ChapterResultData data)
    {
        // System.DateTime을 사용하면 월/연도 계산이 매우 편리해집니다.
        DateTime currentDate = new DateTime(data.startYear, data.startMonth, data.startDay);

        // 6개월 동안 반복
        for (int i = 0; i < 6; i++)
        {
            // "yyyy년 MM월 dd일" 형식으로 텍스트 업데이트
            dateText.text = currentDate.ToString("yyyy년 M월 d일");

            // 1초 대기
            yield return new WaitForSeconds(0.5f);

            // 날짜를 1개월 뒤로 이동
            currentDate = currentDate.AddMonths(1);
        }

        // 애니메이션이 끝난 후, 데이터에 명시된 최종 날짜로 텍스트를 고정합니다.
        dateText.text = $"{data.endYear}년 {data.endMonth}월 {data.endDay}일";
    }

    private void OnContinueButtonClicked()
    {
        // ▼▼▼ 버튼 클릭 시 진행 중인 애니메이션을 확실히 제거 ▼▼▼
        continueButton.transform.DOKill();

        endPanel.SetActive(false);
        OnSequenceComplete?.Invoke();
    }
}
