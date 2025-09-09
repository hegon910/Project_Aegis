using GooglePlayGames;
using GooglePlayGames.BasicApi;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro; // TextMeshPro를 사용하기 위해 추가
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum GameState
{
    Title,
    Login,
    MainMenu,
    CommanderSelection,
    PlayingOpeningCutscene,
    InEventCycle,
    InStory,
    PlayingChapterEndCutscene,
    InBattle,
    InBattleResult,
    InChapterResult,
    PlayingEndingCutscene,
    GamePaused
}
public class GameManager : MonoBehaviour
{
    public static GameManager instance { get; private set; }
    public GameState currentGameState { get; private set; }

    [Header("UI 참조")]
    [SerializeField] private Button continueButton;
    [SerializeField] private GameObject titlePanel;
    [SerializeField] private GameObject titleCanvas;
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject mainGameCanvas;
    [SerializeField] private GameObject commanderSelectionCanvas;
    [SerializeField] private GameObject storyPanel;
    [SerializeField] private GameObject battlePanel;
    // <<<<<<< [추가 2] BattleResultPanel 참조 추가
    [SerializeField] private GameObject battleResultPanel;
    [SerializeField] private TextMeshProUGUI battleResultText; // 승리/패배 텍스트 표시용
    [SerializeField] private GameObject InGameUIPanel;
    [SerializeField] private GameObject optionCanvas;
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private GameObject loginPanel;

    [Header("컷신 시스템")]
    [SerializeField] private CutsceneManager cutsceneManager;
    [SerializeField] private CutsceneData chapter1OpeningCutscene;
    [SerializeField] private List<CutsceneData> chapterMidCutscenes;
    [SerializeField] private List<CutsceneData> chapterEndingCutscenes;
    [SerializeField] private CutsceneData finalEndingCutscene;

    [Header("챕터 결산 연출")]
    [SerializeField] private ChapterResultController chapterEndController;
    [SerializeField] private List<ChapterResultData> chapterEndDataList;
    [SerializeField] private GameObject chapterResultPanel; // ChapterResultCanvas의 패널
    public int CurrentChapter => DataManager.Instance?.PlayerData != null ? DataManager.Instance.PlayerData.currentChapter : 1;

    [Header("범용 확인 창")]
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private TMPro.TextMeshProUGUI confirmationText;

    [Header("코어 시스템 참조")]
    [SerializeField] private UIFlowSimulator uiFlowSimulator;
    [SerializeField] private BattleTurnManager battleTurnManager;
    [SerializeField] private MainScenarioManager mainScenarioManager;
    [SerializeField] private UIPanelAnimator uiPanelAnimator;


    [Header("지휘관 선택")]
    [SerializeField] private Button Commander1Button;
    [SerializeField] private Button Commander2Button;
    [SerializeField] private Button Commander3Button;

    private UnityAction onConfirmAction;
    private bool isReturningToTitle = false;
    private int selectedPackNumber = 1001;
    private bool isTransitioningState = false;

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

    private void OnEnable() { }
    private void OnDisable() { }

    private async void Start()
    {
        await InitializeGameAndLoadData();
    }

    private async Task InitializeGameAndLoadData()
    {
        await DataManager.Instance.InitializeDataAsync();
        await DataManager.Instance.SubIntializeDataAsync();
        DataManager.Instance.LoadGame();

        ChangeState(GameState.Title);
    }

    public void OnStateFinished()
    {
        StartCoroutine(AdvanceGameStateCoroutine());
    }

    private IEnumerator AdvanceGameStateCoroutine()
    {
        if (isTransitioningState) yield break;
        isTransitioningState = true;

        yield return new WaitForEndOfFrame();

        GameState nextState = currentGameState;
        switch (currentGameState)
        {
            case GameState.CommanderSelection: nextState = GameState.PlayingOpeningCutscene; break;
            case GameState.PlayingOpeningCutscene: nextState = GameState.InStory; break;
            case GameState.InStory:
                nextState = (CurrentChapter == 1) ? GameState.InEventCycle : GameState.PlayingChapterEndCutscene;
                break;
            case GameState.InEventCycle:
                nextState = (CurrentChapter == 1 || CurrentChapter == 6) ? GameState.PlayingChapterEndCutscene : GameState.InStory;
                break;
            case GameState.PlayingChapterEndCutscene: nextState = GameState.InBattle; break;
            case GameState.InBattle: nextState = GameState.InBattleResult; break;
            case GameState.InBattleResult: nextState = GameState.InChapterResult; break;
            case GameState.InChapterResult:
                DataManager.Instance.PlayerData.currentChapter++;

                // 6챕터(5회차 완료 후)가 되면 엔딩으로, 그 전까지는 이벤트 사이클로 돌아가 반복
                if (DataManager.Instance.PlayerData.currentChapter > 6)
                {
                    nextState = GameState.PlayingEndingCutscene;
                }
                else
                {
                    EventManager.Instance.StartNewCycle();
                    // 6챕터 시작 시에는 스토리 먼저, 그 외에는 이벤트 사이클 먼저
                    nextState = (DataManager.Instance.PlayerData.currentChapter == 6) ? GameState.InStory : GameState.InEventCycle;
                }
                break;
            case GameState.PlayingEndingCutscene:
                DataManager.Instance.PlayerData.playthroughCount++;
                DataManager.Instance.PlayerData.currentChapter = 1;
                EventManager.Instance.ResetEventManagerState();
                nextState = GameState.MainMenu;
                break;
        }

        if (nextState != currentGameState)
        {
            ChangeState(nextState);
        }

        isTransitioningState = false;
    }

    private void SetUIForState(GameState newState)
    {
        titleCanvas.SetActive(newState == GameState.Title || newState == GameState.Login || newState == GameState.MainMenu);
        titlePanel.SetActive(newState == GameState.Title);
        loginPanel.SetActive(newState == GameState.Login);
        menuPanel.SetActive(newState == GameState.MainMenu);

        mainGameCanvas.SetActive(
            newState != GameState.Title &&
            newState != GameState.Login &&
            newState != GameState.MainMenu &&
            newState != GameState.CommanderSelection);

        commanderSelectionCanvas.SetActive(newState == GameState.CommanderSelection);
        storyPanel.SetActive(newState == GameState.InStory);
        InGameUIPanel.SetActive(newState == GameState.InEventCycle || newState == GameState.InStory);
        battlePanel.SetActive(newState == GameState.InBattle);
        battleResultPanel.SetActive(newState == GameState.InBattleResult);
        chapterResultPanel.SetActive(newState == GameState.InChapterResult);
        optionCanvas.SetActive(newState == GameState.GamePaused);
        gameOverPanel.SetActive(false);

        if (newState == GameState.InStory && CurrentChapter == 1 && DataManager.Instance.PlayerData.playthroughCount == 1)
        {
            if (tutorialPanel != null) tutorialPanel.SetActive(true);
        }
    }

    private void ChangeState(GameState newState)
    {
        UnsubscribeFromCurrentStateEvent();

        currentGameState = newState;
        Debug.Log($"[게임 상태 변경] -> {newState}");

        SetUIForState(newState);

        switch (newState)
        {
            case GameState.Login:
                if (Application.platform == RuntimePlatform.Android) PlayGamesPlatform.Instance.Authenticate(OnAuthenticated);
                else OnAuthenticated(SignInStatus.Success);
                break;
            case GameState.PlayingOpeningCutscene:
                CutsceneManager.OnCutsceneFinished += OnStateFinished;
                cutsceneManager.StartCutscene(chapter1OpeningCutscene);
                break;
            case GameState.InEventCycle:
                EventManager.OnEventCycleCompleted += OnStateFinished;
                if (uiFlowSimulator != null)
                {
                    uiFlowSimulator.BeginFlow();
                }
                uiFlowSimulator.BeginFlow();
                break;
            case GameState.InStory:
                if (uiPanelAnimator != null) uiPanelAnimator.ShowMainStoryView();
                MainScenarioManager.OnScenarioFinished += OnStateFinished;
                mainScenarioManager.BeginScenarioFromStart();
                break;
            case GameState.PlayingChapterEndCutscene:
                CutsceneManager.OnCutsceneFinished += OnStateFinished;
                int chapterIndex = CurrentChapter - 1;
                if (chapterMidCutscenes != null && chapterIndex < chapterMidCutscenes.Count && chapterMidCutscenes[chapterIndex] != null)
                    cutsceneManager.StartCutscene(chapterMidCutscenes[chapterIndex]);
                else
                    OnStateFinished();
                break;
            case GameState.InBattle:
                battleTurnManager.OnBattleEnd += HandleBattleEnd;
                break;
            // <<<<<<< [추가 5] InBattleResult 상태에 대한 로직 추가 (현재는 UI 표시 외에 특별한 동작 없음)
            case GameState.InBattleResult:
                // 이 상태는 UI 버튼 클릭을 통해 다음 상태로 진행되므로, 여기서는 대기합니다.
                break;
            case GameState.InChapterResult:
             //   ChapterResultController.OnSequenceComplete += OnStateFinished;
                StartDetailedResultSequence();
                break;
            case GameState.PlayingEndingCutscene:
                CutsceneManager.OnCutsceneFinished += OnStateFinished;
                cutsceneManager.StartCutscene(finalEndingCutscene);
                break;
        }

        if (DataManager.Instance.PlayerData != null)
        {
            DataManager.Instance.PlayerData.currentGameState = currentGameState;
            DataManager.Instance.SaveGame();
        }
    }

    private void UnsubscribeFromCurrentStateEvent()
    {
        switch (currentGameState)
        {
            case GameState.PlayingOpeningCutscene:
            case GameState.PlayingChapterEndCutscene:
            case GameState.PlayingEndingCutscene:
                CutsceneManager.OnCutsceneFinished -= OnStateFinished;
                break;
            case GameState.InEventCycle:
                EventManager.OnEventCycleCompleted -= OnStateFinished;
                break;
            case GameState.InStory:
                if (uiPanelAnimator != null) uiPanelAnimator.ShowDefaultView();
                MainScenarioManager.OnScenarioFinished -= OnStateFinished;
                break;
            case GameState.InBattle:
                battleTurnManager.OnBattleEnd -= HandleBattleEnd;
                break;
            case GameState.InChapterResult:
              //  ChapterResultController.OnSequenceComplete -= OnStateFinished;
                break;
        }
    }

    // <<<<<<< [수정 6] 전투 종료 시 호출되는 함수 변경
    private void HandleBattleEnd(string resultLog)
    {
        // 결과 텍스트 설정
        if (battleResultText != null)
        {
            battleResultText.text = resultLog; // BattleTurnManager에서 "승리" 또는 "패배" 텍스트를 넘겨주는 것을 가정
        }

        // 스탯 설정
        if (resultLog.Contains("승리")) PlayerStats.Instance.SetStat(ParameterType.전황, 90);
        else if (resultLog.Contains("패배")) PlayerStats.Instance.SetStat(ParameterType.전황, 10);
        else PlayerStats.Instance.SetStat(ParameterType.전황, 50);

        // OnStateFinished()를 바로 호출하는 대신, InBattleResult 상태로 직접 변경
        ChangeState(GameState.InBattleResult);
    }

    // <<<<<<< [추가 7] BattleResultPanel의 버튼이 호출할 공개 함수
    public void OnBattleResultConfirmed()
    {
        // OnClick 이벤트가 발생하면 다음 상태(InChapterResult)로 진행
        OnStateFinished();
    }

    public void OnChapterResultConfirmed()
    {
        // OnClick 이벤트가 발생하면 다음 상태(다음 챕터)로 진행시킵니다.
        OnStateFinished();
    }

    public void HideTutorial() { if (tutorialPanel != null && tutorialPanel.activeSelf) { tutorialPanel.SetActive(false); } }
    private void RestoreGameState(GameState stateToRestore) { Debug.Log($"[게임 상태 복원] -> {stateToRestore}"); currentGameState = stateToRestore; SetUIForState(stateToRestore); switch (stateToRestore) { case GameState.InEventCycle: uiFlowSimulator.BeginFlow(); break; case GameState.InStory: mainScenarioManager.BeginScenarioFromStart(); break; case GameState.InBattle: battleTurnManager.OnBattleEnd += HandleBattleEnd; break; } }
    public void OnClickNewGameFromScratch() { ShowConfirmation("모든 진행 상황이 삭제됩니다. 정말 새로 시작하시겠습니까?", () => { DataManager.Instance.DeleteLocalSaveData(); DataManager.Instance.StartNewGame(); ResetAllGameData(); UnlockManager.ResetAllUnlocks(); ChangeState(GameState.CommanderSelection); }); }
    public void OnClickContinueGame() { if (DataManager.Instance.PlayerData != null) { RestoreGameState(DataManager.Instance.PlayerData.currentGameState); } }
    public void SaveAndReturnToTitle() { ShowConfirmation("진행 상황을 저장하고 타이틀로 돌아가시겠습니까?", () => { isReturningToTitle = true; if (DataManager.Instance.PlayerData != null) { DataManager.Instance.PlayerData.currentGameState = this.currentGameState; DataManager.Instance.SaveGame(); } ChangeState(GameState.MainMenu); isReturningToTitle = false; }); }
    public void OnTitlePanelTouched() { ChangeState(GameState.Login); titlePanel.SetActive(false); menuPanel.SetActive(true); if (Application.platform == RuntimePlatform.Android) { PlayGamesPlatform.Instance.Authenticate(OnAuthenticated); } else { OnAuthenticated(SignInStatus.Success); } }
    private void OnAuthenticated(SignInStatus status) { ChangeState(GameState.MainMenu); continueButton.interactable = DataManager.Instance.CheckIfSaveDataExists(); if (status == SignInStatus.Success) { Debug.Log("구글 플레이 게임 서비스 로그인 성공!"); } else { Debug.LogError("구글 플레이 게임 서비스 로그인 실패: " + status); if (FirebaseManager.Instance != null) FirebaseManager.Instance.GPGSLogin(); } }
    public async void OnCommanderSelected(int commanderIndex) { CommanderInfo selectedCommander = null; switch (commanderIndex) { case 0: selectedCommander = Commander1Button.GetComponent<CommanderInfo>(); break; case 1: selectedCommander = Commander2Button.GetComponent<CommanderInfo>(); break; case 2: selectedCommander = Commander3Button.GetComponent<CommanderInfo>(); break; default: Debug.LogError($"잘못된 지휘관 인덱스입니다: {commanderIndex}"); return; } if (!UnlockManager.IsUnlocked(selectedCommander.traitEnum)) { Debug.LogWarning($"[시스템] 잠겨있는 지휘관({selectedCommander.name})은 선택할 수 없습니다."); return; } PlayerStats.Instance.SetActiveCommander(selectedCommander); if (selectedCommander.initialStatAdjustments.Count > 0) { PlayerStats.Instance.ApplyChanges(selectedCommander.initialStatAdjustments); } await EventManager.Instance.StartNewGame(selectedPackNumber); OnStateFinished(); }
    private void StartDetailedResultSequence() { int warSituation = PlayerStats.Instance.GetStat(ParameterType.전황); GameOutcome outcome = (warSituation <= 19) ? GameOutcome.Defeat : (warSituation >= 81) ? GameOutcome.Victory : GameOutcome.Draw; int chapterIndex = CurrentChapter - 1; if (chapterIndex < chapterEndDataList.Count && chapterEndDataList[chapterIndex] != null) { chapterEndController.StartChapterEndSequence(chapterEndDataList[chapterIndex], outcome); } else { OnStateFinished(); } }
    public void GameOver() { gameOverPanel.SetActive(true); }
    public void OnGameOverPanelTouched() { DataManager.Instance.StartNewGame(); ResetAllGameData(); ChangeState(GameState.CommanderSelection); }
    public void CheckGameOverConditions() { if (PlayerStats.Instance.GetStat(ParameterType.정치력) <= 0 || PlayerStats.Instance.GetStat(ParameterType.병력) <= 0 || PlayerStats.Instance.GetStat(ParameterType.물자) <= 0 || PlayerStats.Instance.GetStat(ParameterType.리더십) <= 0) { GameOver(); } }
    public void ResetAllGameData() { EventManager.Instance.ResetEventManagerState(); battleTurnManager.ResetForNewBattle(); mainScenarioManager.ResetScenarioState(); }
    public void ShowConfirmation(string message, UnityAction confirmAction) { confirmationText.text = message; onConfirmAction = confirmAction; confirmationPanel.SetActive(true); }
    public void OnConfirm() { onConfirmAction?.Invoke(); confirmationPanel.SetActive(false); onConfirmAction = null; }
    public void OnCancel() { confirmationPanel.SetActive(false); onConfirmAction = null; }
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
}