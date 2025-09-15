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

    [Header("메인 스토리 정보")]
    [Tooltip("모든 메인 스토리 CSV 파일에 사용되는 MainStoryPac ID")]
    [SerializeField] private int mainStoryPacID = 1000001;
    [Tooltip("게임 챕터 순서대로, 각 챕터가 시작하는 StoryNum을 입력")]
    [SerializeField] private List<int> chapterStartStoryNums;

    [Header("UI 참조")]
    [SerializeField] private GameObject loadingPanel;
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
    [SerializeField] private GameObject subEventSelectPanel;
    [SerializeField] private TMPro.TextMeshProUGUI subEventSelectedText;

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
    [SerializeField] private WarTurnManager battleTurnManager;
    [SerializeField] private MainScenarioManager mainScenarioManager;
    [SerializeField] private UIPanelAnimator uiPanelAnimator;
    [SerializeField] private CardController cardController;


    [Header("지휘관 선택")]
    [SerializeField] private Button Commander1Button;
    [SerializeField] private Button Commander2Button;
    [SerializeField] private Button Commander3Button;

    private UnityAction onConfirmAction;
    private bool isReturningToTitle = false;
    private int selectedPackNumber = -1;
    private bool isTransitioningState = false;
    private bool subEventsEnabled = false;

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
        DataManager.Instance.LoadGame();
        await DataManager.Instance.InitializeDataAsync();
        await DataManager.Instance.SubIntializeDataAsync();
        

        await DataManager.Instance.IsReady;
        // 모든 데이터 로딩이 완료되었습니다.
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false); // 로딩 패널을 숨깁니다.
        }


        ChangeState(GameState.Title);
    }
    public int GetCurrentChapterPacID()
    {
        return mainStoryPacID;
    }

    public int GetCurrentChapterStartStoryNum()
    {
        if (DataManager.Instance?.PlayerData == null) return 0;

        int chapterIndex = DataManager.Instance.PlayerData.currentChapter - 1;
        if (chapterIndex >= 0 && chapterIndex < chapterStartStoryNums.Count)
        {
            return chapterStartStoryNums[chapterIndex];
        }
        return 0; // 해당 챕터의 시작 StoryNum이 없음
    }
    public void OnStateFinished()
    {
        StartCoroutine(AdvanceGameStateCoroutine());
    }
    public void ForceResetTransitionFlag()
    {
        isTransitioningState = false;
    }
    //서브이벤트 패널을 열고 닫을 함수
    public void ToggleSubEventPanel(bool isOpen)
    {
        

        if (subEventSelectPanel != null)
        {
            subEventSelectPanel.SetActive(isOpen);
            //서브 이벤트 팩이 활성화 되있는 상태라면 텍스트 표시
            if (isOpen && EventManager.Instance != null)
            {
                int selectedPack = EventManager.Instance.GetCurrentSubEventPackID();
                if (selectedPack != -1)
                {
                    subEventSelectedText.gameObject.SetActive(true);
                    subEventSelectedText.text = $"선택된 서브 이벤트 팩: {selectedPack}";
                }
                else
                {

                    subEventSelectedText.text = "선택된 서브 이벤트 팩이 없습니다.";
                }
                if (subEventsEnabled)
                {
                    Debug.Log("서브이벤트 모드가 활성화되었습니다.");
                    // 예: UI 텍스트 변경 -> subEventModeText.text = "ON";
                }
                else
                {
                //    EventManager.Instance.ResetEventManagerState();
                    Debug.Log("서브이벤트 모드가 비활성화되었습니다.");
                    // 예: UI 텍스트 변경 -> subEventModeText.text = "OFF";
                }
            }
        }
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
                // 1장과 6장은 스토리가 먼저이므로, 다음은 이벤트 사이클입니다.
                if (CurrentChapter == 1 || CurrentChapter == 6)
                {
                    nextState = GameState.InEventCycle;
                }
                // 2-5장은 이벤트 사이클 다음이 스토리이므로, 스토리 다음은 컷신입니다.
                else
                {
                    nextState = GameState.PlayingChapterEndCutscene;
                }
                break;

            case GameState.InEventCycle:
                // 2-5장은 이벤트 사이클이 먼저이므로, 다음은 스토리입니다.
                if (CurrentChapter >= 2 && CurrentChapter <= 5)
                {
                    nextState = GameState.InStory;
                }
                // 1장과 6장은 스토리 다음이 이벤트 사이클이므로, 이벤트 사이클 다음은 컷신입니다.
                else
                {
                    nextState = GameState.PlayingChapterEndCutscene;
                }
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
                // 플레이 기록에 엔딩 완료 정보 저장
                if (finalEndingCutscene != null)
                {
                    // TODO: CutsceneData에 명확한 ID가 없다면, 이름 해시코드를 임시 ID로 사용합니다.
                    // 추후 CutsceneData에 엔딩 ID 필드를 추가하고, 그 값을 사용하도록 수정해야 합니다.
                    int endingId = finalEndingCutscene.name.GetHashCode();
                    PlaythroughHistory.Instance.RecordEndingCompletion(endingId);
                    Debug.Log($"[GameManager] 엔딩 완료 기록: {finalEndingCutscene.name} (ID: {endingId})");
                }

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
    // 팩 선택 온 클릭 이벤트
    public void SelectStoryPack(int packNumber)
    {
        // 1. 만약 지금 클릭한 팩이 이미 선택되어 있는 팩이라면
        if (selectedPackNumber == packNumber)
        {
            // 2. 선택을 취소합니다 (기본값인 -1로 되돌립니다).
            selectedPackNumber = -1;
            Debug.Log($"[GameManager] 서브 스토리 팩 {packNumber}번 선택이 취소되었습니다.");

            // 3. UI 텍스트도 초기 상태로 변경합니다.
            if (subEventSelectedText != null)
            {
                subEventSelectedText.text = "선택된 서브 이벤트 팩이 없습니다.";
            }
        }
        // 4. 그렇지 않다면 (다른 팩을 선택했거나 아무것도 선택되지 않은 상태라면)
        else
        {
            // 5. 새로 클릭한 팩을 선택합니다.
            selectedPackNumber = packNumber;
            Debug.Log($"[GameManager] 서브 스토리 팩 {packNumber}번이 선택되었습니다.");

            // 6. UI 텍스트에 선택된 팩 번호를 표시합니다.
            if (subEventSelectedText != null)
            {
                subEventSelectedText.text = $"선택된 서브 이벤트 팩: {packNumber}";
            }
        }
    }
    private void ChangeState(GameState newState)
    {
        UnsubscribeFromCurrentStateEvent();

        currentGameState = newState;
        Debug.Log($"[게임 상태 변경] -> {newState}");

        SetUIForState(newState);

        if (newState == GameState.InBattle)
        {
            Debug.Log(battleTurnManager == null ? "오류: battleTurnManager 참조가 없습니다!" : "1단계 OK: battleTurnManager 참조 정상");
        }
        Debug.Log($"[GameManager] 가 바라보는 WarTurnManager ID: {battleTurnManager.GetInstanceID()}");
        switch (newState)
        {
            case GameState.Login:
                if (Application.platform == RuntimePlatform.Android) PlayGamesPlatform.Instance.Authenticate(OnAuthenticated);
                else OnAuthenticated(SignInStatus.Success);

            Debug.LogError("구글 플레이 게임 서비스 로그인 실패: ");
            FirebaseManager.Instance.GPGSLogin();
                break;
            case GameState.PlayingOpeningCutscene:
                CutsceneManager.OnCutsceneFinished += OnStateFinished;
                cutsceneManager.StartCutscene(chapter1OpeningCutscene);
                break;
            case GameState.InEventCycle:
                if (cardController != null) cardController.choiceHandler = uiFlowSimulator;
                EventManager.OnEventCycleCompleted += OnStateFinished;

                // [핵심 수정] 새로운 이벤트 사이클이므로, 첫 턴을 바로 시작하라고(true) 지시합니다.
                // 중복 호출되던 부분도 하나로 정리했습니다.
                if (uiFlowSimulator != null)
                {
                    uiFlowSimulator.BeginFlow(true);
                }
                break;
            case GameState.InStory:
                if (cardController != null) cardController.choiceHandler = mainScenarioManager;
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
                battleTurnManager.ResetForNewBattle();
                battleTurnManager.OnBattleEnd += HandleBattleEnd;
                Debug.Log("2단계 OK: GameManager가 OnBattleEnd 신호를 구독했습니다.");
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

        if (DataManager.Instance.PlayerData != null && !isReturningToTitle)
        {
            DataManager.Instance.PlayerData.currentGameState = currentGameState;
            DataManager.Instance.SaveLocal();
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

        // 스탯 설정 및 플레이 기록 저장
        BattleOutcome outcome;
        if (resultLog.Contains("승리"))
        {
            PlayerStats.Instance.SetStat(ParameterType.전황, 90);
            outcome = BattleOutcome.Win;
        }
        else if (resultLog.Contains("패배"))
        {
            PlayerStats.Instance.SetStat(ParameterType.전황, 10);
            outcome = BattleOutcome.Lose;
        }
        else
        {
            PlayerStats.Instance.SetStat(ParameterType.전황, 50);
            outcome = BattleOutcome.Draw;
        }

        PlaythroughHistory.Instance.RecordBattleResult(outcome);
        Debug.Log($"[GameManager] 전투 결과 기록: {outcome}");

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
    private void RestoreGameState(GameState stateToRestore)
    {
        Debug.Log($"[게임 상태 복원] -> {stateToRestore}");
        currentGameState = stateToRestore;
        SetUIForState(stateToRestore);

        switch (stateToRestore)
        {
            case GameState.InEventCycle:
                //  "이벤트 사이클 완료" 신호를 GameManager가 받을 수 있도록 여기서도 연결해줍니다.
                EventManager.OnEventCycleCompleted += OnStateFinished;

                // 아래는 기존에 작업했던 올바른 코드입니다.
                EventManager.Instance.InitializeEventManager();
                uiFlowSimulator.BeginFlow(false);
                break;

            case GameState.InStory:
                // [추가] 스토리 이어하기 시에도 완료 신호를 연결해줍니다.
                MainScenarioManager.OnScenarioFinished += OnStateFinished;
                mainScenarioManager.BeginScenarioFromStart();
                break;

            case GameState.InBattle:
                battleTurnManager.OnBattleEnd += HandleBattleEnd;
                break;
        }
    }
    public void HideTutorial() { if (tutorialPanel != null && tutorialPanel.activeSelf) { tutorialPanel.SetActive(false); } }

    public void OnClickNewGameFromScratch() { ShowConfirmation("모든 진행 상황이 삭제됩니다. 정말 새로 시작하시겠습니까?", () => { DataManager.Instance.DeleteLocalSaveData(); DataManager.Instance.StartNewGame(); ResetAllGameData(); UnlockManager.ResetAllUnlocks(); ChangeState(GameState.CommanderSelection); }); }
    public void OnClickContinueGame() { if (DataManager.Instance.PlayerData != null) { RestoreGameState(DataManager.Instance.PlayerData.currentGameState); } }
    public void SaveAndReturnToTitle() { ShowConfirmation("진행 상황을 저장하고 타이틀로 돌아가시겠습니까?", () => { isReturningToTitle = true; if (DataManager.Instance.PlayerData != null) { DataManager.Instance.PlayerData.currentGameState = this.currentGameState; DataManager.Instance.SaveLocal(); } ChangeState(GameState.MainMenu); isReturningToTitle = false; }); }
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
    public void ClosePanel(GameObject panelToClose)
    {
        if (panelToClose != null)
        {
            panelToClose.SetActive(false);
        }
    }
    public void ExitGame()
    {
        ShowConfirmation("게임을 종료하시겠습니까?", () =>
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
        });
    }
}