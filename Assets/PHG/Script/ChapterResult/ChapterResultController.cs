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
    [SerializeField] private TextMeshProUGUI chapterSummaryText; // 챕터 요약 텍스트
    [SerializeField] private TextMeshProUGUI summaryText;
    [SerializeField] private Button continueButton;
    private Vector3 originalButtonScale;

    [Header("챕터 데이터")]
    [SerializeField] private int currentChapterNumber = 1; // 현재 챕터 번호
    [SerializeField] private GameOutcome currentGameOutcome = GameOutcome.Victory; // 현재 게임 결과


    //연출이 끝났음을 EventManager에게 알리는 이벤트
    public static event Action OnSequenceComplete;
    void Awake()
    {
        // continueButton이 할당되었는지 확인하고, 최초 스케일 값을 저장합니다.
        if (continueButton != null)
        {
            originalButtonScale = continueButton.transform.localScale;
        }
    }
    private void Start()
    {
        continueButton.onClick.AddListener(OnContinueButtonClicked);
        // 버튼 라벨을 요청에 맞게 변경
        if (continueButton != null)
        {
            var label = continueButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = "What's next?";
            }
        }
        endPanel.SetActive(false);
        
        // 현재 챕터 번호를 DataManager나 다른 매니저에서 가져오기
        LoadCurrentChapterData();
    }
      void OnEnable()
    {
        AnimateContinueButton();
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
        chapterSummaryText.text = "";

        // 현재 챕터와 결과 저장
        currentGameOutcome = outcome;
        if (GameManager.instance != null)
        {
            currentChapterNumber = GameManager.instance.CurrentChapter;
        }

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
        chapterSummaryText.text = data.chapterSummary;
        
        // 디버깅용 로그
        Debug.Log($"=== ChapterResult 디버깅 ===");
        Debug.Log($"Data가 null인가?: {data == null}");
        if (data != null)
        {
            Debug.Log($"Data.chapterSummary: '{data.chapterSummary}'");
            Debug.Log($"Data.victorySummary: '{data.victorySummary}'");
            Debug.Log($"Data.defeatSummary: '{data.defeatSummary}'");
            Debug.Log($"Data.drawSummary: '{data.drawSummary}'");
        }
        Debug.Log($"ChapterSummaryText UI 할당됨: {chapterSummaryText != null}");
        Debug.Log($"SummaryText UI 할당됨: {summaryText != null}");
        Debug.Log($"Game Outcome: {outcome}");
        Debug.Log($"Current Chapter: {currentChapterNumber}");
        Debug.Log($"Title: {title}");
        Debug.Log($"Summary: {summary}");
        Debug.Log($"===============================");

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
        continueButton.transform.DOKill();
        // 현재 스케일 값을 저장
        continueButton.transform.localScale = originalButtonScale;


        // 버튼의 Transform에 대한 모든 트윈을 먼저 정지시켜 중복 실행을 방지
        continueButton.transform.DOKill();

        continueButton.transform.DOScale(originalButtonScale * 2f, 1f) // 목표 크기: 원래 크기의 2배, 시간: 1초
            .SetEase(Ease.InOutSine)      // 부드러운 움직임 효과
            .SetLoops(-1, LoopType.Yoyo); // 무한 반복 (Yoyo 방식)
    }

    private IEnumerator AnimateDateFlow(ChapterResultData data)
    {
        // System.DateTime을 사용하면 월/연도 계산이 매우 편리해집니다.
        DateTime currentDate = new DateTime(data.startYear, data.startMonth, 1);

        // 6개월 동안 반복
        for (int i = 0; i < 6; i++)
        {
            // "yyyy년 MM월 dd일" 형식으로 텍스트 업데이트
            dateText.text = currentDate.ToString("yyyy년 M월");

            // 1초 대기
            yield return new WaitForSeconds(0.5f);

            // 날짜를 1개월 뒤로 이동
            currentDate = currentDate.AddMonths(1);
        }

        // 애니메이션이 끝난 후, 데이터에 명시된 최종 날짜로 텍스트를 고정합니다.
        dateText.text = $"{data.endYear}년 {data.endMonth}월";
    }

    private void OnContinueButtonClicked()
    {
        // ▼▼▼ 버튼 클릭 시 진행 중인 애니메이션을 확실히 제거 ▼▼▼
        continueButton.transform.DOKill();

        endPanel.SetActive(false);
        OnSequenceComplete?.Invoke();
    }
    void OnDisable()
    {
        if (continueButton != null)
        {
            continueButton.transform.DOKill();
        }
    }

    /// <summary>
    /// 현재 챕터 데이터를 로드합니다.
    /// </summary>
    private void LoadCurrentChapterData()
    {
        // GameManager에서 실제 챕터 번호 가져오기
        if (GameManager.instance != null)
        {
            currentChapterNumber = GameManager.instance.CurrentChapter;
            Debug.Log($"현재 챕터: {currentChapterNumber}");
        }
    }



}
