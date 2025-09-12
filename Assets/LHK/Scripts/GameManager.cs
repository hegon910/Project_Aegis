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
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private GameObject mainGameCanvas;
    [SerializeField] private GameObject commanderSelectionCanvas;
    [SerializeField] private GameObject storyPanel;
    [SerializeField] private GameObject battlePanel;
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
   // private int selectedPackNumber = -1;
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
        subEventSelectedText.text = "선택된 서브 이벤트 팩이 없습니다.";
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

        EventManager.Instance.InitializeEventManager();
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
        // [수정] 이 함수는 이제 단순히 패널을 켜고 끄는 역할만 합니다.
        if (subEventSelectPanel != null)
        {
            subEventSelectPanel.SetActive(isOpen);
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
                // 1장은 스토리가 먼저이므로, 다음은 이벤트 사이클입니다.
                if (CurrentChapter == 1)
                {
                    nextState = GameState.InEventCycle;
                }
                // 2-6장은 이벤트 사이클 다음이 스토리이므로, 스토리 다음은 컷신입니다.
                else
                {
                    nextState = GameState.PlayingChapterEndCutscene;
                }
                break;

            case GameState.InEventCycle:
                // 2-6장은 이벤트 사이클이 먼저이므로, 다음은 스토리입니다.
                if (CurrentChapter >= 2 && CurrentChapter <= 6)
                {
                    nextState = GameState.InStory;
                }
                // 1장은 스토리 다음이 이벤트 사이클이므로, 이벤트 사이클 다음은 컷신입니다.
                else
                {
                    nextState = GameState.PlayingChapterEndCutscene;
                }
                break;
            case GameState.PlayingChapterEndCutscene: nextState = GameState.InBattle; break;
            case GameState.InBattle: nextState = GameState.InBattleResult; break;
            case GameState.InBattleResult: nextState = GameState.InChapterResult; break;
            case GameState.InChapterResult:
                PlayerStats.Instance.SetStat(ParameterType.전황, 50);
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
                    nextState = GameState.InEventCycle;
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
    // 팩 선택 온 클릭 이벤트
    public void SelectStoryPack(int packNumber)
    {
        if (DataManager.Instance.PlayerSettings == null) return;

        subEventSelectedText.gameObject.SetActive(true);

        // 현재 선택된 ID 리스트를 가져옵니다.
        List<int> selectedIDs = DataManager.Instance.PlayerSettings.selectedSubEventPackIDs;

        // 이미 해당 팩이 유일하게 선택되어 있었는지 확인
        bool wasSelected = selectedIDs.Count == 1 && selectedIDs.Contains(packNumber);

        if (wasSelected)
        {
            // 선택 취소: 리스트를 완전히 비웁니다.
            selectedIDs.Clear();
            Debug.Log($"[GameManager] 서브 스토리 팩 {packNumber}번 선택이 취소되었습니다.");
            if (subEventSelectedText != null)
            {
                subEventSelectedText.text = "선택된 서브 이벤트 팩이 없습니다.";
            }
        }
        else
        {
            // 새로운 팩 선택: 리스트를 비우고 현재 팩만 추가합니다. (단일 선택)
            selectedIDs.Clear();
            selectedIDs.Add(packNumber);
            Debug.Log($"[GameManager] 서브 스토리 팩 {packNumber}번이 선택되었습니다.");
            if (subEventSelectedText != null)
            {
                subEventSelectedText.text = $"서브 이벤트 팩: {packNumber}이 활성화 되었습니다.";
            }
        }

        // 변경된 리스트 정보로 설정을 저장합니다.
        DataManager.Instance.SaveSettings();
    }
    private void ChangeState(GameState newState)
    {
        UnsubscribeFromCurrentStateEvent();

        currentGameState = newState;
        Debug.Log($"[게임 상태 변경] -> {newState}");

        SetUIForState(newState);
        if (newState == GameState.MainMenu)
        {
            continueButton.gameObject.SetActive(DataManager.Instance.CheckIfSaveDataExists());
        }
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
            // InBattleResult 상태에 대한 로직 추가 (현재는 UI 표시 외에 특별한 동작 없음)
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

        bool shouldSaveState = true;
        switch (newState)
        {
            case GameState.Title:
            case GameState.Login:
            case GameState.MainMenu:
            case GameState.CommanderSelection:
            case GameState.GamePaused:
                shouldSaveState = false;
                break;
        }

        if (shouldSaveState && DataManager.Instance.PlayerData != null && !isReturningToTitle)
        {
            DataManager.Instance.PlayerData.currentGameState = currentGameState;
            DataManager.Instance.SaveLocal();
        }
    }
    public void OnclickSkip()
    {


        // 메인 시나리오가 실행 중인지 먼저 확인
        var mainScenarioManager = FindObjectOfType<MainScenarioManager>();
        if (mainScenarioManager != null && mainScenarioManager.IsScenarioRunning)
        {
            mainScenarioManager.SkipToNextNode();
        }
        // 그렇지 않으면 일반 이벤트(파라미터/서브) 스킵 시도
        else if (EventManager.Instance != null)
        {
            // 현재 UI 전환 효과 등을 무시하고 즉시 다음 턴을 호출합니다.
            EventManager.Instance.PlayNextTurn();
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

    //  전투 종료 시 호출되는 함수 변경
    private void HandleBattleEnd(string resultLog)
    {
        // 결과 텍스트 설정
        if (battleResultText != null)
        {
            battleResultText.text = resultLog; // BattleTurnManager에서 "승리" 또는 "패배" 텍스트를 넘겨주는 것을 가정
        }

        // 스탯을 추가 혹은 감소 조정하는 것으로 변경
        // if (resultLog.Contains("승리")) PlayerStats.Instance.ApplyChanges(new List<ParameterChange> { new ParameterChange { parameterType = ParameterType.전황, valueChange = +20 } });
        // else if (resultLog.Contains("패배")) PlayerStats.Instance.ApplyChanges(new List<ParameterChange> { new ParameterChange { parameterType = ParameterType.전황, valueChange = -20 } });
        // else PlayerStats.Instance.ApplyChanges(new List<ParameterChange> { new ParameterChange { parameterType = ParameterType.전황, valueChange = 0 } });

        // OnStateFinished()를 바로 호출하는 대신, InBattleResult 상태로 직접 변경
        ChangeState(GameState.InBattleResult);
    }

    // BattleResultPanel의 버튼이 호출할 공개 함수
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
                EventManager.OnEventCycleCompleted += OnStateFinished;

                //  카드 스와이프 로직이 동작하도록 choiceHandler를 연결합니다.
                if (cardController != null) cardController.choiceHandler = uiFlowSimulator;

                EventManager.Instance.InitializeEventManager();
                uiFlowSimulator.BeginFlow(false);

                // [PlayNextTurn() 대신 새로 추가한 RestoreCurrentEvent()를 호출합니다.
                // 이렇게 해야 이벤트를 건너뛰지 않고 정확한 시점의 이벤트를 불러올 수 있습니다.
                EventManager.Instance.RestoreCurrentEvent();
                break;

            case GameState.InStory:
                // 스토리 이어하기 시에도 완료 신호를 연결해줍니다.
                MainScenarioManager.OnScenarioFinished += OnStateFinished;
                mainScenarioManager.BeginScenarioFromStart();
                break;

            case GameState.InBattle:
                battleTurnManager.OnBattleEnd += HandleBattleEnd;
                break;
        }
    }
    public void HideTutorial() { if (tutorialPanel != null && tutorialPanel.activeSelf) { tutorialPanel.SetActive(false); } }

    public void OnClickNewGameFromScratch()
    {
        ShowConfirmation("모든 진행 상황과 설정이 삭제됩니다. 정말 새로 시작하시겠습니까?", () =>
        {
            // [수정] DeleteLocalSaveData() 대신 DeleteAllLocalData()를 호출합니다.
            DataManager.Instance.DeleteAllLocalData();

            // 데이터를 모두 지운 후, 새 데이터 객체를 생성하고 게임을 시작합니다.
            DataManager.Instance.StartNewGame();
            DataManager.Instance.LoadSettings(); // 삭제 후 새로 로드
            ResetAllGameData();
            UnlockManager.ResetAllUnlocks();
            ChangeState(GameState.CommanderSelection);
        });
    }
    public void OnclickResetData()
    {
        ShowConfirmation("모든 진행 상황과 설정이 삭제됩니다. 정말 초기화하시겠습니까?", () =>
        {
            // 1. 모든 로컬 파일과 PlayerPrefs 기록 삭제
            DataManager.Instance.DeleteAllLocalData();

            // 2. 메모리에 새로운 기본 데이터 객체를 즉시 생성하고 로드
            // StartNewGame은 새 GameData를 만들고 기본 파일까지 생성해줍니다.
            DataManager.Instance.StartNewGame();
            // LoadSettings는 파일이 없으면 새 SettingsData를 만들어줍니다.
            DataManager.Instance.LoadSettings();

            // 3. EventManager 등 다른 게임 시스템들의 상태 초기화
            ResetAllGameData();
            UnlockManager.ResetAllUnlocks();

            // 4. 메인 메뉴로 돌아가 UI를 갱신합니다.
            // (예: '이어하기' 버튼이 사라지는 등 초기화된 상태를 시각적으로 보여줌)
            ChangeState(GameState.MainMenu);
        });
    }
    public void OnClickContinueGame() { if (DataManager.Instance.PlayerData != null) { RestoreGameState(DataManager.Instance.PlayerData.currentGameState); } }
    public void SaveAndReturnToTitle()
    {
        ShowConfirmation("진행 상황을 저장하고 타이틀로 돌아가시겠습니까?", () =>
        {
            isReturningToTitle = true;
            if (DataManager.Instance.PlayerData != null)
            {
                // [추가] 현재 상태가 저장 가능한 상태인지 확인하는 로직
                bool shouldSaveState = true;
                switch (this.currentGameState)
                {
                    case GameState.Title:
                    case GameState.Login:
                    case GameState.MainMenu:
                    case GameState.CommanderSelection:
                    case GameState.GamePaused:
                        shouldSaveState = false;
                        break;
                }

                // [수정] 저장 가능한 상태일 때만 저장하도록 if문 추가
                if (shouldSaveState)
                {
                    //  PlayerStats가 DataManager를 직접 수정하므로 별도의 동기화가 필요 없습니다.
                    var dataToSave = DataManager.Instance.PlayerData;
                    dataToSave.currentGameState = this.currentGameState;

                    DataManager.Instance.SaveLocal();
                }
            }
            ChangeState(GameState.MainMenu);
            isReturningToTitle = false;
        });
    }
    public void OnTitlePanelTouched() { ChangeState(GameState.Login); titlePanel.SetActive(false); menuPanel.SetActive(true); if (Application.platform == RuntimePlatform.Android) { PlayGamesPlatform.Instance.Authenticate(OnAuthenticated); } else { OnAuthenticated(SignInStatus.Success); } }
    private void OnAuthenticated(SignInStatus status) { ChangeState(GameState.MainMenu); continueButton.gameObject.SetActive(DataManager.Instance.CheckIfSaveDataExists()); if (status == SignInStatus.Success) { Debug.Log("구글 플레이 게임 서비스 로그인 성공!"); } else { Debug.LogError("구글 플레이 게임 서비스 로그인 실패: " + status); if (FirebaseManager.Instance != null) FirebaseManager.Instance.GPGSLogin(); } }
    public async void OnCommanderSelected(int commanderIndex)
    {
        CommanderInfo selectedCommander = null;
        switch (commanderIndex)
        {
            case 0: selectedCommander = Commander1Button.GetComponent<CommanderInfo>(); break;
            case 1: selectedCommander = Commander2Button.GetComponent<CommanderInfo>(); break;
            case 2: selectedCommander = Commander3Button.GetComponent<CommanderInfo>(); break;
            default: Debug.LogError($"잘못된 지휘관 인덱스입니다: {commanderIndex}"); return;
        }
        if (!UnlockManager.IsUnlocked(selectedCommander.traitEnum))
        {
            Debug.LogWarning($"[시스템] 잠겨있는 지휘관({selectedCommander.name})은 선택할 수 없습니다."); return;
        }
        PlayerStats.Instance.SetActiveCommander(selectedCommander);
        if (selectedCommander.initialStatAdjustments.Count > 0)
        {
            PlayerStats.Instance.ApplyChanges(selectedCommander.initialStatAdjustments);
        }
        // [수정] DataManager에 저장된 설정값을 사용합니다.
        await EventManager.Instance.StartNewGame(DataManager.Instance.PlayerSettings.selectedSubEventPackIDs);
        OnStateFinished();
    }
    private void StartDetailedResultSequence() { int warSituation = PlayerStats.Instance.GetStat(ParameterType.전황); GameOutcome outcome = (warSituation <= 19) ? GameOutcome.Defeat : (warSituation >= 81) ? GameOutcome.Victory : GameOutcome.Draw; int chapterIndex = CurrentChapter - 1; if (chapterIndex < chapterEndDataList.Count && chapterEndDataList[chapterIndex] != null) { chapterEndController.StartChapterEndSequence(chapterEndDataList[chapterIndex], outcome); } else { OnStateFinished(); } }
    public void GameOver(string reason)
    {
        if (gameOverText != null)
        {
            gameOverText.text = reason;
        }
        gameOverPanel.SetActive(true);
    }
    public void OnGameOverPanelTouched() { DataManager.Instance.StartNewGame(); ResetAllGameData(); ChangeState(GameState.CommanderSelection); }
    public void CheckGameOverConditions()
    {
        if (PlayerStats.Instance.GetStat(ParameterType.정치력) <= 0)
        {
            GameOver("정치력이 0이 되어 통치 기반을 잃었습니다.");
        }
        else if (PlayerStats.Instance.GetStat(ParameterType.병력) <= 0)
        {
            GameOver("병력이 0이 되어 전선을 유지할 수 없습니다.");
        }
        else if (PlayerStats.Instance.GetStat(ParameterType.물자) <= 0)
        {
            GameOver("물자가 0이 되어 부대를 운용할 수 없습니다.");
        }
        else if (PlayerStats.Instance.GetStat(ParameterType.리더십) <= 0)
        {
            GameOver("리더십이 0이 되어 병사들이 따르지 않습니다.");
        }
    }
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
            if (DataManager.Instance != null)
            {
                // [추가] 여기에도 동일한 상태 저장 방지 로직을 적용합니다.
                bool shouldSaveState = true;
                switch (this.currentGameState)
                {
                    case GameState.Title:
                    case GameState.Login:
                    case GameState.MainMenu:
                    case GameState.CommanderSelection:
                    case GameState.GamePaused:
                        shouldSaveState = false;
                        break;
                }

                // [수정] 저장 가능한 상태일 때만 진행도를 저장합니다.
                if (shouldSaveState)
                {
                    DataManager.Instance.PlayerData.currentGameState = this.currentGameState;
                    DataManager.Instance.SaveLocal();
                }

                // 설정은 언제나 저장합니다.
                DataManager.Instance.SaveSettings();
            }
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
        });
    }
}