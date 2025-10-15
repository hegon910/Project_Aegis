using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// MultiEndingSystem 테스트를 위한 UI 컨트롤러
/// </summary>
public class MultiEndingSystemTester : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private Button testChapter1Button;
    [SerializeField] private Button testChapter2Button;
    [SerializeField] private Button testChapter3Button;
    [SerializeField] private Button testChapter4Button;
    [SerializeField] private Button testChapter5Button;
    [SerializeField] private Button testChapter6Button;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button debugInfoButton;
    [SerializeField] private Button testEndingButton;
    
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI karmaText;
    [SerializeField] private TextMeshProUGUI endingText;

    private void Start()
    {
        // 버튼 이벤트 연결
        if (testChapter1Button) testChapter1Button.onClick.AddListener(() => TestChapter(1, GameOutcome.Victory, 85));
        if (testChapter2Button) testChapter2Button.onClick.AddListener(() => TestChapter(2, GameOutcome.Victory, 90));
        if (testChapter3Button) testChapter3Button.onClick.AddListener(() => TestChapter(3, GameOutcome.Draw, 50));
        if (testChapter4Button) testChapter4Button.onClick.AddListener(() => TestChapter(4, GameOutcome.Victory, 75));
        if (testChapter5Button) testChapter5Button.onClick.AddListener(() => TestChapter(5, GameOutcome.Victory, 80));
        if (testChapter6Button) testChapter6Button.onClick.AddListener(() => TestChapter(6, GameOutcome.Victory, 95));
        if (resetButton) resetButton.onClick.AddListener(ResetSystem);
        if (debugInfoButton) debugInfoButton.onClick.AddListener(ShowDebugInfo);
        if (testEndingButton) testEndingButton.onClick.AddListener(TestEnding);

        // MultiEndingSystem 이벤트 구독
        MultiEndingSystem.OnLoopCompleted += OnLoopCompleted;
        MultiEndingSystem.OnNewLoopStarted += OnNewLoopStarted;

        UpdateUI();
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        MultiEndingSystem.OnLoopCompleted -= OnLoopCompleted;
        MultiEndingSystem.OnNewLoopStarted -= OnNewLoopStarted;
    }

private void TestChapter(int chapter, GameOutcome outcome, int warSituation)
{
    if (MultiEndingSystem.Instance == null)
    {
        Debug.LogError("MultiEndingSystem.Instance가 null입니다!");
        return;
    }

    // 챕터 강제 점프
    MultiEndingSystem.Instance.SetCurrentChapter(chapter);

    // 챕터 결과 기록
    MultiEndingSystem.Instance.RecordChapterResult(chapter, outcome, warSituation);
    
    Debug.Log($"[테스트] 챕터 {chapter}로 점프하고 결과 기록: {outcome} (전세: {warSituation})");
    UpdateUI();
}

    private void ResetSystem()
    {
        if (MultiEndingSystem.Instance == null)
        {
            Debug.LogError("MultiEndingSystem.Instance가 null입니다!");
            return;
        }

        MultiEndingSystem.Instance.ResetLoopSystem();
        Debug.Log("[테스트] 루프 시스템 리셋 완료");
        UpdateUI();
    }

    private void ShowDebugInfo()
    {
        if (MultiEndingSystem.Instance == null)
        {
            Debug.LogError("MultiEndingSystem.Instance가 null입니다!");
            return;
        }

        MultiEndingSystem.Instance.PrintDebugInfo();
    }

    private void TestEnding()
    {
        if (MultiEndingSystem.Instance == null)
        {
            Debug.LogError("MultiEndingSystem.Instance가 null입니다!");
            return;
        }

        Debug.Log("[테스트] 엔딩 테스트 시작");
        
        // 6챕터 완료 시뮬레이션
        for (int i = 1; i <= 6; i++)
        {
            MultiEndingSystem.Instance.RecordChapterResult(i, GameOutcome.Victory, 80 + i);
        }
        
        // 엔딩 데이터 확인
        var endingData = MultiEndingSystem.Instance.GetFinalEndingData();
        if (endingData != null)
        {
            Debug.Log($"[테스트] 엔딩 데이터 확인:");
            Debug.Log($"  - 타입: {endingData.endingType}");
            Debug.Log($"  - 루트: {endingData.route}");
            Debug.Log($"  - 분기: {endingData.branch}");
            Debug.Log($"  - 제목: {endingData.title}");
            Debug.Log($"  - 설명: {endingData.description}");
            
            // FullEndingData 확인
            if (endingData.fullEndingData != null)
            {
                Debug.Log($"  - 텍스트: {endingData.fullEndingData.Text_Kr}");
                Debug.Log($"  - BG_ID: {endingData.fullEndingData.bgData?.BG_ID ?? -1}");
                Debug.Log($"  - CutScene_ID: {endingData.fullEndingData.cutSceneData?.EndingCutScene_ID ?? -1}");
            }
        }
        
        // CutsceneData 생성 테스트
        var cutsceneData = MultiEndingSystem.Instance.CreateCutsceneDataFromTable();
        if (cutsceneData != null)
        {
            Debug.Log($"[테스트] CutsceneData 생성 성공! 스텝 수: {cutsceneData.steps.Count}");
        }
        else
        {
            Debug.LogError("[테스트] CutsceneData 생성 실패!");
        }
        
        UpdateUI();
    }

 private void UpdateUI()
{
    if (MultiEndingSystem.Instance == null) return;

    var status = MultiEndingSystem.Instance.GetCurrentLoopStatus();
    
    if (statusText)
    {
        // currentChapter를 사용하여 현재 진행 상황 표시
        statusText.text = $"회차: {status.currentPlaythrough}\n" +
                        $"챕터: {status.currentChapter}/6\n" +
                        $"완료된 챕터: {status.currentChapter}/6\n" +
                        $"루프 완료: {(status.isLoopCompleted ? "예" : "아니오")}";
    }

    if (karmaText)
    {
        karmaText.text = $"현재 회차 카르마: {status.totalKarma}\n" +
                       $"현재 카르마: {status.currentKarma}";
    }

    if (endingText)
    {
        var endingType = MultiEndingSystem.Instance.DetermineEndingType();
        var endingRoute = MultiEndingSystem.Instance.DetermineEndingRoute();
        var endingBranch = MultiEndingSystem.Instance.DetermineEndingBranch(endingRoute);
        
        endingText.text = $"엔딩 타입: {endingType}\n" +
                        $"엔딩 루트: {endingRoute}\n" +
                        $"엔딩 분기: {endingBranch}";
    }
}

    private void OnLoopCompleted(int playthrough)
    {
        Debug.Log($"[이벤트] 루프 완료! 회차 {playthrough} 완료");
        UpdateUI();
    }

    private void OnNewLoopStarted()
    {
        Debug.Log("[이벤트] 새 루프 시작!");
        UpdateUI();
    }
}
