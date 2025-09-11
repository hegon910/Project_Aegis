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

    [Header("컷신 시스템")]
    [SerializeField] private CutsceneManager cutsceneManager;
    [SerializeField] private CutsceneData chapter1OpeningCutscene;
    // 필요한 만큼 엔딩 컷신 데이터 추가
    [SerializeField] private List<CutsceneData> chapterEndingCutscenes;

    [Header("튜토리얼")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private GameObject swipeTutorialImage;
    [SerializeField] private GameObject parameterTutoText;

    [Header("챕터 결산 연출")]
    [SerializeField] private ChapterResultController chapterEndController;
    [SerializeField] private List<ChapterResultData> chapterEndDataList;
    [SerializeField] private GameObject chapterResultPanel;
    public int CurrentChapter => DataManager.Instance?.PlayerData != null ? DataManager.Instance.PlayerData.currentChapter : 1;

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

    private bool hasSaveData = false;
    private UnityAction onConfirmAction;
    private bool isReturningToTitle = false;
    private int selectedPackNumber = 1001;

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
        ChapterResultController.OnSequenceComplete += OnChapterEndSequenceComplete;
        EventManager.OnEventCycleCompleted += HandleEventCycleCompleted;
    }
    private void OnEnable()
    {
       // ChapterResultController.OnSequenceComplete += OnChapterEndSequenceComplete;
       // EventManager.OnEventCycleCompleted += HandleEventCycleCompleted;
    }

    private void OnDisable()
    {
        if (instance == this)
        {
            ChapterResultController.OnSequenceComplete -= OnChapterEndSequenceComplete;
            EventManager.OnEventCycleCompleted -= HandleEventCycleCompleted;
        }
    }

    private void Start()
    {
        InitializeGameAndLoadData();
    }

    private async void InitializeGameAndLoadData()
    {
        await DataManager.Instance.InitializeDataAsync();
        await DataManager.Instance.SubIntializeDataAsync();
        DataManager.Instance.LoadGame();

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
        if (status != SignInStatus.Success)
        {
            Debug.LogError("구글 플레이 게임 서비스 로그인 실패: " + status);
            FirebaseManager.Instance.GPGSLogin();
        }
    }

    public void OnNewGameButtonClicked()
    {
        if (hasSaveData)
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
        DataManager.Instance.StartNewGame();
        ResetAllGameData();

        menuPanel.SetActive(false);
        titleCanvas.SetActive(false);
        commanderSelectionCanvas.SetActive(true);
    }

    public void ShowConfirmation(string message, UnityAction confirmAction)
    {
        confirmationText.text = message;
        onConfirmAction = confirmAction;
        confirmationPanel.SetActive(true);
    }

    public void OnConfirm()
    {
        onConfirmAction?.Invoke();
        confirmationPanel.SetActive(false);
        onConfirmAction = null;
    }

    public void OnCancel()
    {
        confirmationPanel.SetActive(false);
        onConfirmAction = null;
    }
    // 팩 선택 온 클릭 이벤트
    public void SelectStoryPack(int packNumber)
    {
        selectedPackNumber = packNumber;
        Debug.Log($"[GameManager] 서브 스토리 팩 {packNumber}번이 선택되었습니다.");
    }

    // [수정] 기존의 (int commanderIndex) 방식을 그대로 유지합니다.
    public async void OnCommanderSelected(int commanderIndex)
    {
        CommanderInfo selectedCommander = null;

        // [복구] 보내주신 기존 플로우대로, int 값에 따라 버튼 변수에서 CommanderInfo를 가져옵니다.
        //        (새로운 리스트가 필요 없습니다.)
        switch (commanderIndex)
        {
            case 0:
                selectedCommander = Commander1Button.GetComponent<CommanderInfo>();
                break;
            case 1:
                selectedCommander = Commander2Button.GetComponent<CommanderInfo>();
                break;
            case 2:
                selectedCommander = Commander3Button.GetComponent<CommanderInfo>();
                break;
            default:
                Debug.LogError($"잘못된 지휘관 인덱스입니다: {commanderIndex}");
                return;
        }

        if (selectedCommander == null)
        {
            Debug.LogError($"지휘관 버튼에 CommanderInfo 컴포넌트가 없습니다! Index: {commanderIndex}");
            return;
        }

        // [복구] 유실되었던 기능 (해금 확인, 스탯 적용, 저장)을 이 함수 안에서 처리합니다.
        if (!UnlockManager.IsUnlocked(selectedCommander.traitEnum))
        {
            Debug.LogWarning($"[시스템] 잠겨있는 지휘관({selectedCommander.name})은 선택할 수 없습니다.");
            return;
        }

        PlayerStats.Instance.SetActiveCommander(selectedCommander);

        if (selectedCommander.initialStatAdjustments.Count > 0)
        {
            PlayerStats.Instance.ApplyChanges(selectedCommander.initialStatAdjustments);
        }

        DataManager.Instance.SaveGame();


        if (DataManager.Instance.PlayerData.playthroughCount == 1 && chapter1OpeningCutscene != null)
        {
            // 컷신이 끝나면 게임을 시작하도록 이벤트 구독
            CutsceneManager.OnCutsceneFinished += StartGameAfterOpening;
            cutsceneManager.StartCutscene(chapter1OpeningCutscene);
        }
        else
        {
            // 컷신이 없으면 바로 게임 시작
            await StartGameFlow();
        }
    }

    private async void StartGameAfterOpening()
    {
        // 이벤트 구독 해제 (중요!)
        CutsceneManager.OnCutsceneFinished -= StartGameAfterOpening;
        await StartGameFlow();
    }

    private async System.Threading.Tasks.Task StartGameFlow()
    {
        if (EventManager.Instance != null)
        {
            await EventManager.Instance.StartNewGame(selectedPackNumber);
        }
        else
        {
            Debug.LogError("EventManager 인스턴스를 찾을 수 없습니다!");
            return;
        }

        commanderSelectionCanvas.SetActive(false);
        mainGameCanvas.SetActive(true);
        InGameUIPanel.SetActive(true);

        if (DataManager.Instance.PlayerData.playthroughCount == 1)
        {
            GoToStoryPanel();
        }
        else
        {
            if (uiFlowSimulator != null)
            {
                uiFlowSimulator.BeginFlow();
            }
        }
    }

    public void StartEventFlow()
    {
        if (uiFlowSimulator != null)
        {
            uiFlowSimulator.BeginFlow();
        }
    }

    public void GoToStoryPanel()
    {
        if (mainScenarioManager != null && mainScenarioManager.IsScenarioRunning) return;

        if (DataManager.Instance.PlayerData.playthroughCount == 1 && tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
            if (swipeTutorialImage != null) swipeTutorialImage.SetActive(true);
            if (parameterTutoText != null) parameterTutoText.SetActive(false);
        }
        if (storyPanel != null)
        {
            storyPanel.SetActive(true);
            if (mainScenarioManager != null) mainScenarioManager.BeginScenarioFromStart();
        }
        else
        {
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
        }
        else
        {
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
            if (battleResult.Contains("승리")) PlayerStats.Instance.SetStat(ParameterType.전황, 90);
            else if (battleResult.Contains("패배")) PlayerStats.Instance.SetStat(ParameterType.전황, 10);
            else PlayerStats.Instance.SetStat(ParameterType.전황, 50);
        }
        else
        {
            ShowResultPanel();
        }
    }

    public void ShowResultPanel()
    {
        if (battleResultPanel != null) battleResultPanel.SetActive(false);
        mainGameCanvas.SetActive(false);
        StartDetailedResultSequence();
    }

    private void StartDetailedResultSequence()
    {
        if (chapterEndController == null)
        {
            OnChapterEndSequenceComplete();
            return;
        }

        chapterEndController.gameObject.SetActive(true);
        chapterResultPanel.SetActive(true);

        int warSituation = PlayerStats.Instance.GetStat(ParameterType.전황);
        GameOutcome outcome;

        if (warSituation <= 19) outcome = GameOutcome.Defeat;
        else if (warSituation >= 81) outcome = GameOutcome.Victory;
        else outcome = GameOutcome.Draw;

        int chapterIndex = CurrentChapter - 1;
        if (chapterIndex < chapterEndDataList.Count && chapterEndDataList[chapterIndex] != null)
        {
            chapterEndController.StartChapterEndSequence(chapterEndDataList[chapterIndex], outcome);
        }
        else
        {
            OnChapterEndSequenceComplete();
        }
    }

    private void OnChapterEndSequenceComplete()
    {
        if (isReturningToTitle) return;

        DataManager.Instance.PlayerData.currentChapter++;
        DataManager.Instance.SaveGame();

        mainGameCanvas.SetActive(true);
        if (uiFlowSimulator != null) uiFlowSimulator.BeginFlow();
    }

    public void CheckGameOverConditions()
    {
        if (PlayerStats.Instance.GetStat(ParameterType.정치력) <= 0 ||
            PlayerStats.Instance.GetStat(ParameterType.병력) <= 0 ||
            PlayerStats.Instance.GetStat(ParameterType.물자) <= 0 ||
            PlayerStats.Instance.GetStat(ParameterType.리더십) <= 0)
        {
            GameOver();
        }
    }

    public void GameOver()
    {
        gameOverPanel.SetActive(true);
    }

    public void OnGameOverPanelTouched()
    {
        gameOverPanel.SetActive(false);
        DataManager.Instance.StartNewGame();
        ResetAllGameData();
        mainGameCanvas.SetActive(true);
        if (uiFlowSimulator != null) uiFlowSimulator.BeginFlow();
    }

    public void ShowOptionsPanel()
    {
        if (optionCanvas != null) optionCanvas.SetActive(true);
    }

    public void HideOptionsPanel()
    {
        if (optionCanvas != null) optionCanvas.SetActive(false);
    }

    public void StartNextPlaythrough()
    {
        DataManager.Instance.PlayerData.playthroughCount++;
        EventManager.Instance.ResetEventManagerState();
        DataManager.Instance.SaveGame();

        if (battleResultPanel != null) battleResultPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        mainGameCanvas.SetActive(true);
        InGameUIPanel.SetActive(true);

        StartEventFlow();
    }

    public void ResetAllGameData()
    {
        if (EventManager.Instance != null) EventManager.Instance.ResetEventManagerState();
        if (battleTurnManager != null) battleTurnManager.ResetForNewBattle();
        if (mainScenarioManager != null) mainScenarioManager.ResetScenarioState();
        if (PlaythroughHistory.Instance != null) PlaythroughHistory.Instance.ClearHistory();
    }

    public void GoToTitleScreen()
    {
        isReturningToTitle = true;
        StopAllCoroutines();
        ResetAllGameData();

        mainGameCanvas.SetActive(false);
        commanderSelectionCanvas.SetActive(false);
        gameOverPanel.SetActive(false);
        optionCanvas.SetActive(false);
        confirmationPanel.SetActive(false);

        titleCanvas.SetActive(true);
        titlePanel.SetActive(true);
        menuPanel.SetActive(false);
    }

    public void ExitGame()
    {
        ShowConfirmation("게임을 종료하시겠습니까?", () =>
        {
            DataManager.Instance.SaveGame();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        });
    }



    private void HandleEventCycleCompleted()
    {
        if (DataManager.Instance?.PlayerData == null) return;

        int currentPlaythrough = DataManager.Instance.PlayerData.playthroughCount;

        int chapterIndex = CurrentChapter - 1;
        // 현재 챕터에 해당하는 엔딩 컷신이 있는지 확인
        if (chapterEndingCutscenes != null && chapterIndex < chapterEndingCutscenes.Count && chapterEndingCutscenes[chapterIndex] != null)
        {
            // 컷신이 끝나면 원래 로직을 실행하도록 이벤트 구독
            CutsceneManager.OnCutsceneFinished += ProceedAfterChapterEndCutscene;
            cutsceneManager.StartCutscene(chapterEndingCutscenes[chapterIndex]);
        }
        else
        {
            // 컷신이 없으면 바로 다음 진행
            ProceedAfterChapterEndCutscene();
        }
    }
    private void ProceedAfterChapterEndCutscene()
    {
        // 이벤트 구독 해제!
        CutsceneManager.OnCutsceneFinished -= ProceedAfterChapterEndCutscene;

        int currentPlaythrough = DataManager.Instance.PlayerData.playthroughCount;

        if (currentPlaythrough == 1) GoToBattlePanel();
        else GoToStoryPanel();
    }

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
        };
        PlayerStats.Instance.ApplyChanges(changes);
    }
#endif
}