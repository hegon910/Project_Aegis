using GooglePlayGames;
using GooglePlayGames.BasicApi;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public static GameManager instance { get; private set; }

    [Header("UI 참조")]
    [SerializeField] private GameObject titlePanel;
    [SerializeField] private GameObject titleCanvas;
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject overwriteWarningPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject mainGameCanvas;
    [SerializeField] private GameObject commanderSelectionCanvas;
    [SerializeField] private GameObject storyPanel;
    [SerializeField] private GameObject battlePanel;
    [SerializeField] private GameObject battleResultPanel;
    [SerializeField] private TMPro.TextMeshProUGUI battleResultText;
    [SerializeField] private GameObject InGameUIPanel;
    [SerializeField] private Button testPlayerButton;
    [SerializeField] private GameObject optionCanvas;
    [SerializeField] private Button exitButton;

    [Header("튜토리얼")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private GameObject swipeTutorialImage;
    [SerializeField] private GameObject parameterTutoText;

    [Header("챕터 결산 연출")]
    [SerializeField] private ChapterResultController chapterEndController; // 새로 만든 컨트롤러 연결
    [SerializeField] private List<ChapterResultData> chapterEndDataList;   // 챕터별 데이터 에셋 목록 연결
    private int currentChapter = 1; // 현재 챕터를 추적하기 위한 변수
    [SerializeField] private GameObject chapterResultPanel;
    public int CurrentChapter => currentChapter;

    [Header("범용 확인 창")]
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private TMPro.TextMeshProUGUI confirmationText;

    [Header("코어 시스템 참조")]
    [SerializeField] private UIFlowSimulator uiFlowSimulator;
    [SerializeField] private BattleTurnManager battleTurnManager;
    [SerializeField] private MainScenarioManager mainScenarioManager;

    [Header("지휘관 선택")]
    [SerializeField] private Button Commander1Button;
    [SerializeField] private Button Commander2Button;
    [SerializeField] private Button Commander3Button;

    [Header("디버그")]
    [SerializeField] private Button forceGameOverButton;

    private bool hasSaveDate = false;
    private UnityAction onConfirmAction;
    private bool isReturningToTitle = false;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void OnEnable()
    {
        ChapterResultController.OnSequenceComplete += OnChapterEndSequenceComplete;
        EventManager.OnEventCycleCompleted += HandleEventCycleCompleted;
    }

    private void OnDisable()
    {
        ChapterResultController.OnSequenceComplete -= OnChapterEndSequenceComplete;
        EventManager.OnEventCycleCompleted -= HandleEventCycleCompleted;
    }

    private void Start()
    {
        Debug.Log("====게임 매니저가 초기화=====");
        InitializeGame();
    }

    private void InitializeGame()
    {
        PlayerStats.Instance.InitializeStats();
        titlePanel.SetActive(true);
        loginPanel.SetActive(false);
        menuPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        mainGameCanvas.SetActive(false);
        commanderSelectionCanvas.SetActive(false);
        optionCanvas.SetActive(false);
        confirmationPanel.SetActive(false);
    }

    public void OnTitlePanelTouched()
    {
        titlePanel.SetActive(false);
        menuPanel.SetActive(true);
        if (Application.platform == RuntimePlatform.Android)
        {
            PlayGamesPlatform.Instance.Authenticate(OnAuthenticated);
        }
    }

    private void OnAuthenticated(SignInStatus status)
    {
        if (status == SignInStatus.Success)
        {
            Debug.Log("구글 플레이 게임 서비스 로그인 성공");
        }
        else
        {
            Debug.LogError("구글 플레이 게임 서비스 로그인 실패: " + status);
        }
    }

    public void OnNewGameButtonClicked()
    {
        if (hasSaveDate)
        {
            overwriteWarningPanel.SetActive(true);
        }
        else
        {
            StartNewGame();
        }
    }

    public void StartNewGame()
    {
        isReturningToTitle = false;
        ResetAllGameData();
        menuPanel.SetActive(false);
        titleCanvas.SetActive(false);
        commanderSelectionCanvas.SetActive(true);
    }

    public void ShowConfirmation(string message, UnityAction confirmAction)
    {
        if (confirmationPanel != null)
        {
            confirmationText.text = message;
            onConfirmAction = confirmAction;
            confirmationPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Confirmation Panel이 Inspector에 할당되지 않았습니다!");
        }
    }

    public void OnConfirm()
    {
        onConfirmAction?.Invoke();
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);
        }
        onConfirmAction = null;
    }

    public void OnCancel()
    {
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);
        }
        onConfirmAction = null;
    }

    public async void OnCommanderSelected(int commanderIndex)
    {
        // 지휘관 선택 시 이벤트 매니저를 초기화하고 새 게임 시작
        if (EventManager.Instance != null)
        {
            await EventManager.Instance.StartNewGame(commanderIndex);
        }
        else
        {
            Debug.LogError("EventManager 인스턴스를 찾을 수 없습니다!");
            return;
        }

        commanderSelectionCanvas.SetActive(false);
        mainGameCanvas.SetActive(true);
        InGameUIPanel.SetActive(true);

        if (PlayerStats.Instance.playthroughCount == 1)
        {
            GoToStoryPanel();
        }
        else
        {
            if (uiFlowSimulator != null)
            {
                uiFlowSimulator.BeginFlow();
            }
            else
            {
                Debug.LogError("UIFlowSimulator가 GameManager에 할당되지 않았습니다!");
            }
        }
    }

    public void StartEventFlow()
    {
        if (uiFlowSimulator != null)
        {
            uiFlowSimulator.BeginFlow();
        }
        else
        {
            Debug.LogError("UIFlowSimulator가 GameManager에 할당되지 않았습니다!");
        }
    }

    public void GoToStoryPanel()
    {
        if (mainScenarioManager != null && mainScenarioManager.IsScenarioRunning)
        {
            Debug.Log("[GM] 시나리오가 이미 진행 중이므로 새로운 시작 요청을 무시합니다.");
            return;
        }
        if (PlayerStats.Instance.playthroughCount == 1 && tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
            if (swipeTutorialImage != null) swipeTutorialImage.SetActive(true);
            if (parameterTutoText != null) parameterTutoText.SetActive(false);
        }
        if (storyPanel != null)
        {
            storyPanel.SetActive(true);
            if (mainScenarioManager != null)
            {
                mainScenarioManager.BeginScenarioFromStart();
            }
            else
            {
                Debug.LogError("MainScenarioManager가 GameManager에 할당되지 않았습니다!");
            }
        }
        else
        {
            Debug.LogWarning("Story Panel이 할당되지 않아 전투 페이즈로 바로 넘어갑니다.");
            GoToBattlePanel();
        }
    }

    public void HideTutorial()
    {
        tutorialPanel.SetActive(false);
    }

    public void GoToBattlePanel()
    {
        if (storyPanel != null) storyPanel.SetActive(false);
        if (InGameUIPanel != null) InGameUIPanel.SetActive(false);
        if (battlePanel != null)
        {
            battlePanel.SetActive(true);
            if (battleTurnManager != null)
            {
                battleTurnManager.OnBattleEnd += HandleBattleEnd;
            }
            else
            {
                Debug.LogError("BattleTurnManager가 GameManager에 할당되지 않았습니다!");
            }
        }
        else
        {
            Debug.LogWarning("Battle Panel이 할당되지 않아 결과 페이즈로 바로 넘어갑니다.");
            GoToBattleResultPanel("전투 패널 없음");
        }
    }

    private void HandleBattleEnd(string resultLog)
    {
        GoToBattleResultPanel(resultLog);
        if (battleTurnManager != null)
        {
            battleTurnManager.OnBattleEnd -= HandleBattleEnd;
        }
    }

    public void GoToBattleResultPanel(string battleResult)
    {
        if (battleResultPanel != null)
        {
            battleResultPanel.SetActive(true);
            if (battleResult.Contains("승리"))
            {
                //PlayerStat에 전황을 90으로 바꾸기
                PlayerStats.Instance.SetStat(ParameterType.전황, 90);
                battleResultText.text = "전투에서 승리하였다. 전쟁에서의 승리도 가까워졌기를.";
            }
            else if (battleResult.Contains("패배"))
            {
                //PlayerStat에 전황을 10으로 바꾸기
                PlayerStats.Instance.SetStat(ParameterType.전황, 10);
                battleResultText.text = "어쩔 수 없군 이번 전투에서는 패배를 받아드리지... 후퇴하라.";
            }
            else
            {
                //PlayerStat에 전황을 50으로 바꾸기
                PlayerStats.Instance.SetStat(ParameterType.전황, 50);
                battleResultText.text = "무승부라고? 결판을 짓지 못하다니...";
            }
        }
        else
        {
            Debug.LogWarning("Battle Result Panel이 할당되지 않아 메인 캔버스로 바로 돌아갑니다.");
            ReturnToMainGameCanvas();
        }
    }

    public void ReturnToMainGameCanvas()
    {
        if (battleResultPanel != null) battleResultPanel.SetActive(false);
        mainGameCanvas.SetActive(false);
        //  바로 다음 이벤트로 가는 대신, '상세 결과 연출'을 시작합니다.
        StartDetailedResultSequence();
        //if (battleResultPanel != null) battleResultPanel.SetActive(false);
        //mainGameCanvas.SetActive(true);
        //if (uiFlowSimulator != null)
        //{
        //    uiFlowSimulator.BeginFlow();
        //}
    }
    private void StartDetailedResultSequence()
    {
        if (chapterEndController != null)
        {
            chapterEndController.gameObject.SetActive(true);
            chapterResultPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("ChapterEndController가 할당되지 않았습니다! 연출 시퀀스를 건너뜁니다.");
            OnChapterEndSequenceComplete(); // 컨트롤러가 없으면 바로 다음 단계로 넘어갑니다.
            return;
        }
        int warSituation = PlayerStats.Instance.GetStat(ParameterType.전황);
        GameOutcome outcome;

        if (warSituation <= 19) outcome = GameOutcome.Defeat;
        else if (warSituation >= 81) outcome = GameOutcome.Victory;
        else outcome = GameOutcome.Draw;

        int chapterIndex = currentChapter - 1;
        if (chapterIndex < chapterEndDataList.Count && chapterEndDataList[chapterIndex] != null)
        {
            chapterEndController.StartChapterEndSequence(chapterEndDataList[chapterIndex], outcome);
        }
        else
        {
            Debug.LogError($"챕터 {currentChapter}에 대한 ChapterEndData가 없습니다! 연출을 건너뜁니다.");
            OnChapterEndSequenceComplete();
        }
    }
    private void OnChapterEndSequenceComplete()
    {
        if (isReturningToTitle)
        {
            return;
        }
        Debug.Log("모든 결과 연출 종료. 다음 이벤트 사이클을 시작합니다.");

        currentChapter++;

        //  여기서 메인 캔버스를 켜고 다음 이벤트 흐름을 시작
        mainGameCanvas.SetActive(true);
        if (uiFlowSimulator != null)
        {
            uiFlowSimulator.BeginFlow();
        }
    }
    // [제거] 이벤트 핸들러는 더 이상 사용하지 않음
    // private void OnStatChanged_CheckGameOver(...) ...

#if UNITY_EDITOR
    public void OnForceGameOverButtonClicked()
    {
        ForceGameOver();
    }

    public void ForceGameOver()
    {
        List<ParameterChange> changes = new List<ParameterChange>
        {
            new ParameterChange { parameterType = ParameterType.정치력, valueChange = -100 },
            new ParameterChange { parameterType = ParameterType.병력, valueChange = -100 },
            new ParameterChange { parameterType = ParameterType.물자, valueChange = -100 },
            new ParameterChange { parameterType = ParameterType.리더십, valueChange = -100 }
        };
        Debug.Log("<color=red>디버그: GameManager에서 파라미터 변경을 직접 호출하여 게임오버 유발.</color>");
        PlayerStats.Instance.ApplyChanges(changes);
    }
#endif

    // [제거] CheckGameOverConditions()로 역할이 통합되었으므로 불필요
    // public void OnParameterChanged() ...

    public void CheckGameOverConditions()
    {
        if (PlayerStats.Instance.GetStat(ParameterType.정치력) <= 0 ||
            PlayerStats.Instance.GetStat(ParameterType.병력) <= 0 ||
            PlayerStats.Instance.GetStat(ParameterType.물자) <= 0 ||
            PlayerStats.Instance.GetStat(ParameterType.리더십) <= 0)
        {
            GameOver();
            Debug.Log("게임 오버 조건 충족");
        }
    }

    public void GameOver()
    {
        gameOverPanel.SetActive(true);
        Debug.Log("게임 오버");
        // 여기에 게임 조작을 막는 로직 추가 가능
    }

    public void OnGameOverPanelTouched()
    {
        Debug.Log("게임을 재시작합니다.");
        gameOverPanel.SetActive(false);
        Debug.Log("게임오버 패널 비활성화");

        ResetAllGameData();
        Debug.Log("초기화");

        mainGameCanvas.SetActive(true);

        // [수정] 리셋 후 UI Flow를 다시 시작하도록 명령
        if (uiFlowSimulator != null)
        {
            uiFlowSimulator.BeginFlow();
        }
        else
        {
            Debug.LogError("UIFlowSimulator 참조가 없어 게임 흐름을 다시 시작할 수 없습니다!");
        }
    }

    public void ShowOptionsPanel()
    {
        if (optionCanvas != null)
        {
            optionCanvas.SetActive(true);
        }
    }

    public void HideOptionsPanel()
    {
        if (optionCanvas != null)
        {
            optionCanvas.SetActive(false);
        }
    }

    public void StartNextPlaythrough()
    {
        Debug.Log("다음 회차를 시작합니다.");

        // 회차 정보 갱신
        PlayerStats.Instance.StartNewPlaythrough();

        // 다음 사이클을 위해 이벤트 매니저 리셋
        EventManager.Instance.ResetEventManagerState();

        // UI 패널 리셋
        if (battleResultPanel != null) battleResultPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        mainGameCanvas.SetActive(true);
        InGameUIPanel.SetActive(true);

        // 새로운 이벤트 흐름 시작
        StartEventFlow();
    }

    public void ResetAllGameData()
    {
        Debug.Log("==== 모든 게임 데이터 초기화를 시작합니다 ====");
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.InitializeStats();
            Debug.Log("PlayerStats가 초기화되었습니다.");
        }
        if (EventManager.Instance != null)
        {
            EventManager.Instance.ResetEventManagerState();
            Debug.Log("EventManager가 초기화되었습니다.");
        }
        if (battleTurnManager != null)
        {
            battleTurnManager.ResetForNewBattle();
            Debug.Log("BattleTurnManager가 초기화되었습니다.");
        }
        if (mainScenarioManager != null)
        {
            Debug.Log("[GM] Requesting MainScenarioManager to reset state...");
            mainScenarioManager.ResetScenarioState();
        }
    }

    //임시 타이틀 화면 복귀용
    public void GoToTitleScreen()
    {
        isReturningToTitle = true;
        StopAllCoroutines();
        Debug.Log("==== 타이틀 화면으로 돌아갑니다. 모든 데이터를 초기화합니다. ====");

        // 1. 모든 게임 관련 데이터(스탯, 이벤트 매니저 등)를 리셋합니다.
        ResetAllGameData();

        // 2. 모든 UI 패널을 끈 후, 타이틀 화면 관련 UI만 다시 켭니다.
        // (게임 플레이 중에 활성화될 수 있는 모든 패널을 비활성화)
        mainGameCanvas.SetActive(false);
        commanderSelectionCanvas.SetActive(false);
        gameOverPanel.SetActive(false);
        optionCanvas.SetActive(false);
        confirmationPanel.SetActive(false);

        // 타이틀 화면 활성화
        titleCanvas.SetActive(true);
        titlePanel.SetActive(true);
        menuPanel.SetActive(false); // 메뉴 패널은 타이틀 터치 후에 나오도록 비활성화
        mainGameCanvas.SetActive(false);
    }
    public void ExitGame()
    {
        ShowConfirmation("게임을 종료하시겠습니까?", () =>
        {
            Debug.Log("게임 종료 확인됨");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        });
    }

    private void HandleEventCycleCompleted()
    {
        Debug.Log("GameManager: 이벤트 사이클 완료");
        if (PlayerStats.Instance == null)
        {
            Debug.LogError("GameManager: 이벤트 사이클이 완료되었지만 PlayerStats.Instance가 비었습니다.");
            return;
        }
        Debug.Log($"GameManager: 현재 회차 = {PlayerStats.Instance.playthroughCount}");

        if (PlayerStats.Instance.playthroughCount == 1)
        {
            // 1회차에는 이벤트 사이클 후 전투
            Debug.Log("GameManager: 1회차. 전투 패널로 이동합니다.");
            GoToBattlePanel();
        }
        else
        {
            // 2회차부터는 이벤트 사이클 후 메인 스토리
            Debug.Log("GameManager: 2회차 이상. 스토리 패널로 이동합니다.");
            GoToStoryPanel();
        }
    }
}