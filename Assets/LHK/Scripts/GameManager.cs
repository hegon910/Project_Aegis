using GooglePlayGames;
using GooglePlayGames.BasicApi;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro; // TextMeshPro를 사용하기 위해 추가
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

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
    // 게임오버 중복 호출 방지 플래그
    private bool _isGameOverActive = false;
    public static GameManager instance { get; private set; }
    public GameState currentGameState { get; private set; }
    
    // 게임 상태 변경 이벤트
    public static System.Action<GameState> OnGameStateChanged;
    // 이어하기 직후 1회성 튜토리얼(스토리/파라미터) 표시 억제 플래그
    private bool suppressTutorialOnce = false;

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
    [SerializeField] private GameObject tutorialText;
    [SerializeField] private GameObject parameterTutorialPanel;
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject subEventSelectPanel;
    [SerializeField] private TMPro.TextMeshProUGUI subEventSelectedText;
    [SerializeField] private GameObject warTutorialPanel;
    [SerializeField] private GameObject achievementPanel;

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
    private bool hasShownWarTutorialThisPlaythrough = false;
    private bool isInitialized = false;
    
    [Header("Guest Login Popup System")]
    [SerializeField] private PopupController popupController;

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
        // 시작 시점에는 이어하기 버튼을 항상 숨김. 저장 데이터 확인 후에만 활성화
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(false);
        }
        subEventSelectedText.text = "선택된 서브 이벤트 팩이 없습니다.";
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        EnsureCheatManagerExists();
#endif

    }

    private void OnEnable() { }
    private void OnDisable() { }

    private async void Start()
    {
        await InitializeGameAndLoadData();
        if (popupController == null)
            popupController = FindObjectOfType<PopupController>();    
    }

    private async Task InitializeGameAndLoadData()
    {
        DataManager.Instance.LoadGame();

        await DataManager.Instance.InitializeDataAsync();

        await DataManager.Instance.IsReady;

        // EventManager.Instance가 준비될 때까지 기다림
        await WaitForEventManagerReady();
        
        if (EventManager.Instance != null)
        {
            EventManager.Instance.InitializeEventManager();
        }
        else
        {
            Debug.LogError("[GameManager] EventManager.Instance가 여전히 null입니다. EventManager가 씬에 존재하는지 확인하세요.");
        }
        
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }

        isInitialized = true;

        ChangeState(GameState.Title);
        if (Application.platform == RuntimePlatform.Android) PlayGamesPlatform.Instance.Authenticate(OnAuthenticated);
        else OnAuthenticated(SignInStatus.Success);
    }

    private async Task WaitForEventManagerReady()
    {
        int maxWaitFrames = 60; // 최대 1초 (60프레임) 기다림
        int waitFrames = 0;
        
        while (EventManager.Instance == null && waitFrames < maxWaitFrames)
        {
            await Task.Yield(); // 다음 프레임까지 대기
            waitFrames++;
        }
        
        if (EventManager.Instance == null)
        {
            Debug.LogWarning($"[GameManager] EventManager.Instance가 {maxWaitFrames}프레임 후에도 null입니다.");
        }
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
                GamePlayerStats.Instance.SetStat(ParameterType.전황, 50);
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
                // 엔딩 분기 규칙
                const int NORMAL_ENDING_ID = 1; // 일반 엔딩 ID
                const int TRUE_ENDING_ID_2ND = 2; // 2회차 진엔딩 ID
                const int TRUE_ENDING_ID_3RD = 3; // 3회차 진엔딩 ID
                const int HIDDEN_ENDING_ID_3RD = 4; // 3회차 히든엔딩 ID (임시, 확인 필요)

                var playerData = DataManager.Instance.PlayerData;
                int determinedEndingId = NORMAL_ENDING_ID; // 기본은 일반 엔딩

                if (playerData.playthroughCount == 2)
                {
                    // 2회차에서는 2회차 진엔딩 조건만 확인
                    if (playerData.realEnding2ChoiceCount > 0)
                    {
                        determinedEndingId = TRUE_ENDING_ID_2ND;
                    }
                }
                else if (playerData.playthroughCount >= 3) // 3회차 이상
                {
                    // 3회차 이상에서는 히든 엔딩 조건 먼저 확인 (우선순위 높음)
                    // TODO: 히든 엔딩 조건 추가 (예: playerData.hiddenEndingChoiceCount > 0)
                    // if (playerData.hiddenEndingChoiceCount > 0)
                    // {
                    //     determinedEndingId = HIDDEN_ENDING_ID_3RD;
                    // }
                    // else if (playerData.realEnding3ChoiceCount > 0) // 히든 엔딩 조건 미충족 시 3회차 진엔딩 조건 확인
                    // { 
                    //     determinedEndingId = TRUE_ENDING_ID_3RD;
                    // }
                    // For now, without hidden ending condition, just check 3rd playthrough true ending
                    if (playerData.realEnding3ChoiceCount > 0)
                    {
                        determinedEndingId = TRUE_ENDING_ID_3RD;
                    }
                }
                // else (playthroughCount == 1 or other cases not explicitly handled)
                // determinedEndingId remains NORMAL_ENDING_ID

                // 결정된 엔딩 ID 기록
                DataManager.Instance.RecordEnding(determinedEndingId);

                // 이제 기록된 lastEndingId를 기반으로 분기 로직 실행
                // 2회차에서 진엔딩을 못 봤다면 2회차를 다시 시작
                if (playerData.playthroughCount == 2 && playerData.lastEndingId != TRUE_ENDING_ID_2ND)
                {
                    Debug.Log($"[분기] 2회차, 진엔딩({TRUE_ENDING_ID_2ND})이 아니므로 2회차를 다시 시작합니다. lastEndingId: {playerData.lastEndingId}");
                    // 회차를 증가시키지 않고 현재 챕터만 1로 리셋
                    playerData.currentChapter = 1;
                }
                // 3회차 이상에서 히든엔딩을 봤다면 게임 완전 초기화
                else if (playerData.playthroughCount >= 3 && playerData.lastEndingId == HIDDEN_ENDING_ID_3RD)
                {
                    Debug.Log($"[분기] 3회차 이상, 히든엔딩({HIDDEN_ENDING_ID_3RD})을 봤으므로 게임을 초기화합니다.");
                    // 새 게임 데이터로 덮어쓰고, 지휘관 선택 화면으로 이동
                    DataManager.Instance.StartNewGame(); 
                    ResetAllGameData();
                    nextState = GameState.CommanderSelection;
                    break; // 아래의 공통 로직을 건너뛰고 바로 상태 변경
                }
                else
                {
                    // 일반적인 다음 회차 진행
                    playerData.playthroughCount++;
                    playerData.currentChapter = 1;
                }

                hasShownWarTutorialThisPlaythrough = false;
                EventManager.Instance.ResetEventManagerState();
                nextState = GameState.CommanderSelection; // Changed from MainMenu to CommanderSelection
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
            newState != GameState.CommanderSelection);

        commanderSelectionCanvas.SetActive(newState == GameState.CommanderSelection);
        storyPanel.SetActive(newState == GameState.InStory);
        InGameUIPanel.SetActive(newState == GameState.InEventCycle || newState == GameState.InStory);
        battlePanel.SetActive(newState == GameState.InBattle);
        battleResultPanel.SetActive(newState == GameState.InBattleResult);
        chapterResultPanel.SetActive(newState == GameState.InChapterResult);
        optionCanvas.SetActive(newState == GameState.GamePaused);
        gameOverPanel.SetActive(false);

        // 이어하기 복원 직후에는 어떤 튜토리얼도 표시하지 않음 (한 번만 동작)
        if (!suppressTutorialOnce)
        {
            if (newState == GameState.InStory && CurrentChapter == 1 && DataManager.Instance.PlayerData.playthroughCount == 1)
            {
                if (tutorialPanel != null) tutorialPanel.SetActive(true);
                if (tutorialText != null) tutorialText.SetActive(false);
            }

            // 메인 이벤트(스토리) 종료 후 파라미터 이벤트 사이클 진입 시 텍스트 다시 활성화
            if (newState == GameState.InEventCycle && CurrentChapter == 1 && DataManager.Instance.PlayerData.playthroughCount == 1)
            {
                if (parameterTutorialPanel!= null) parameterTutorialPanel.SetActive(true);
                if (tutorialText != null) tutorialText.SetActive(true);
            }
        }
        else
        {
            if (tutorialPanel != null) tutorialPanel.SetActive(false);
            if (tutorialText != null) tutorialText.SetActive(false);
            if (parameterTutorialPanel != null) parameterTutorialPanel.SetActive(false);
        }

        // 한 프레임 동안만 억제하도록 플래그 해제
        if (suppressTutorialOnce) suppressTutorialOnce = false;
    }
    // 팩 선택 온 클릭 이벤트 (다중 선택 지원)
    public void SelectStoryPack(int packNumber)
    {
        if (DataManager.Instance.PlayerSettings == null) return;

        subEventSelectedText.gameObject.SetActive(true);

        // 현재 선택된 ID 리스트를 가져옵니다.
        List<int> selectedIDs = DataManager.Instance.PlayerSettings.selectedSubEventPackIDs;

        // 이미 해당 팩이 선택되어 있었는지 확인
        bool isAlreadySelected = selectedIDs.Contains(packNumber);

        if (isAlreadySelected)
        {
            // 선택 취소: 리스트에서 해당 팩 ID를 제거합니다.
            selectedIDs.Remove(packNumber);
            Debug.Log($"[GameManager] 서브 스토리 팩 {packNumber}번 선택이 취소되었습니다.");
        }
        else
        {
            // 새로운 팩 선택: 리스트에 현재 팩 ID를 추가합니다.
            selectedIDs.Add(packNumber);
            Debug.Log($"[GameManager] 서브 스토리 팩 {packNumber}번이 선택되었습니다.");
        }

        // UI 텍스트 업데이트
        if (subEventSelectedText != null)
        {
            if (selectedIDs.Count > 0)
            {
                // 선택된 팩들의 번호를 쉼표로 구분하여 문자열로 만듭니다.
                string selectedPacksStr = string.Join(", ", selectedIDs.OrderBy(id => id));
                subEventSelectedText.text = $"활성화된 팩: {selectedPacksStr}";
            }
            else
            {
                subEventSelectedText.text = "선택된 서브 이벤트 팩이 없습니다.";
            }
        }

        // 변경된 리스트 정보로 설정을 저장합니다.
        DataManager.Instance.SaveSettings();
    }
    public void ChangeState(GameState newState)
    {
          if (newState == currentGameState) return;
        UnsubscribeFromCurrentStateEvent();

        currentGameState = newState;
        Debug.Log($"[게임 상태 변경] -> {newState}");
        
        // 상태 변경 이벤트 발생
        OnGameStateChanged?.Invoke(newState);

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
                if (DataManager.Instance?.PlayerData != null &&
                    DataManager.Instance.PlayerData.playthroughCount == 1 &&
                    CurrentChapter == 1 &&
                    !hasShownWarTutorialThisPlaythrough)
                {
                    if (warTutorialPanel != null) warTutorialPanel.SetActive(true);
                }
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
                var selectedEndingTemplate = MultiEndingSystem.Instance?.GetFinalEndingData();

                FullEndingData finalContentData = null;
                if (selectedEndingTemplate != null)
                {
                    finalContentData = MultiEndingSystem.Instance.GetFullEndingData(selectedEndingTemplate);
                }

                CutsceneData cutsceneToPlay = null;
                if (finalContentData != null)
                {
                    cutsceneToPlay = MultiEndingSystem.Instance.ConvertToCutsceneData(finalContentData);
                }

                if (cutsceneToPlay != null)
                {
                    Debug.Log($"[GameManager] 멀티 엔딩 재생: {selectedEndingTemplate.endingType} - {selectedEndingTemplate.route} - {selectedEndingTemplate.branch}");
                    CutsceneManager.OnCutsceneFinished += OnStateFinished;
                    cutsceneManager.StartCutscene(cutsceneToPlay);
                }
                else
                {
                    Debug.LogWarning("[GameManager] 엔딩 데이터 로드/변환 실패. 기본 엔딩 사용.");
                    CutsceneManager.OnCutsceneFinished += OnStateFinished;
                    cutsceneManager.StartCutscene(finalEndingCutscene);
                }

                //회상기록
                string finalOutcome = selectedEndingTemplate != null ?$"{selectedEndingTemplate.endingType} ({selectedEndingTemplate.route})" : "Final End";

                GameSessionManager sessionManager = GetComponent<GameSessionManager>();

                if (sessionManager != null)
                {
                    sessionManager.EndSession(finalOutcome);
                    Debug.Log("[GameManager] GameSessionManager를 통해 회차 기록을 완료했습니다.");
                }
                else
                {
                    Debug.LogError("[GameManager] GameSessionManager 컴포넌트 참조 오류! 회상 기록이 누락됩니다.");
                }


                // 엔딩 기록 저장
                if (DataManager.Instance?.PlayerData != null && selectedEndingTemplate != null)
                {
                    string endingKey = $"{selectedEndingTemplate.endingType}_{selectedEndingTemplate.route}_{selectedEndingTemplate.branch}";
                    if (!DataManager.Instance.PlayerData.completedEndings.Contains(endingKey))
                    {
                        DataManager.Instance.PlayerData.completedEndings.Add(endingKey);
                        DataManager.Instance.SaveLocal();
                    }
                }
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
            // 실제 플레이 가능한 상태에 진입했으므로 저장 억제를 해제합니다.
            DataManager.Instance.AllowSavesFromNow();
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
        // 이어하기로 복원하는 경우에도 게임오버 가드는 해제되어야 함
        _isGameOverActive = false;
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
                // 전투 시작 전 상태 초기화
                if (battleTurnManager != null)
                {
                    battleTurnManager.ResetForNewBattle();
                    Debug.Log("[GameManager] 전투 상태 초기화 완료");
                }
                break;
        }
    }
    public void HideTutorial() { if (tutorialPanel != null && tutorialPanel.activeSelf) { tutorialPanel.SetActive(false); parameterTutorialPanel.SetActive(false); } }
    public void HideWarTutorial()
    {
        hasShownWarTutorialThisPlaythrough = true;
        if (warTutorialPanel != null && warTutorialPanel.activeSelf)
        {
            warTutorialPanel.SetActive(false);
        }
    }

    public void OnClickNewGameFromScratch()
    {
        ShowConfirmation("모든 진행 상황과 설정이 삭제됩니다. 정말 새로 시작하시겠습니까?", () =>
        {
            // 진행도만 삭제하여 Settings(서브이벤트 팩 선택)는 보존
            DataManager.Instance.DeleteLocalSaveData();

            // 데이터를 모두 지운 후, 새 데이터 객체를 생성하고 게임을 시작합니다.
            DataManager.Instance.StartNewGame();
            DataManager.Instance.LoadSettings(); // 삭제 후 새로 로드
            ResetAllGameData();
            UnlockManager.ResetAllUnlocks();
            // [추가] 저장 데이터 초기화 직후 이어하기 버튼 즉시 숨김
            if (continueButton != null) continueButton.gameObject.SetActive(false);
            ChangeState(GameState.CommanderSelection);
            // UI 상태 강제 갱신 (지휘관 잠금 표시/파라미터 토글 초기화 등)
            StartCoroutine(RefreshUINextFrame());
        });
    }
    public void OnclickResetData()
    {
        ShowConfirmation("모든 진행 상황과 설정이 삭제됩니다. 정말 초기화하시겠습니까?", () =>
        {
            // 1. 모든 로컬 파일과 PlayerPrefs 기록 삭제
            // 진행도만 삭제하여 Settings 보존
            DataManager.Instance.DeleteLocalSaveData();

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
            // [추가] 데이터 리셋 직후 이어하기 버튼 즉시 숨김
            if (continueButton != null) continueButton.gameObject.SetActive(false);
            ChangeState(GameState.MainMenu);
            // UI 상태 강제 갱신 (지휘관 잠금 표시/파라미터 토글 초기화 등)
            StartCoroutine(RefreshUINextFrame());
        });
    }
    public void OnClickContinueGame()
    {
        // 저장 파일 존재 여부 확인. 없으면 버튼 숨기고 동작 중단
        if (DataManager.Instance == null || !DataManager.Instance.CheckIfSaveDataExists())
        {
            if (continueButton != null) continueButton.gameObject.SetActive(false);
            return;
        }

        // 메모리 데이터가 없으면 로드 시도
        if (DataManager.Instance.PlayerData == null)
        {
            DataManager.Instance.LoadGame();
        }
        if (DataManager.Instance.PlayerData == null)
        {
            if (continueButton != null) continueButton.gameObject.SetActive(false);
            return;
        }

        // 게임오버 후 메인복귀 상태라면, 이어하기 시 챕터 처음부터 재시작
        if (DataManager.Instance.PlayerData.pendingRestartFromGameOver)
        {
            ResetAllGameData();
            ResetParameterDataToDefaults();
            EventManager.Instance.ResetEventManagerState();
            int targetChapter = DataManager.Instance.PlayerData.pendingRestartChapter;
            DataManager.Instance.PlayerData.currentChapter = (targetChapter >= 1 && targetChapter <= 6) ? targetChapter : 1;
            DataManager.Instance.PlayerData.completedEventIds.Clear();
            DataManager.Instance.PlayerData.eventPlaylistIndex = 0;
            DataManager.Instance.PlayerData.currentPlaylist.Clear();
            // 저장 억제를 해제한 뒤 사이클을 구성하여 SaveLocal이 억제되지 않도록 순서 수정
            DataManager.Instance.AllowSavesFromNow();
            EventManager.Instance.StartNewCycle();
            DataManager.Instance.PlayerData.currentGameState = GameState.InEventCycle;
            DataManager.Instance.PlayerData.pendingRestartFromGameOver = false;
            DataManager.Instance.PlayerData.pendingRestartChapter = 0;
            DataManager.Instance.SaveLocal();

            // 튜토리얼 억제 및 즉시 숨김
            suppressTutorialOnce = true;
            if (tutorialPanel != null) tutorialPanel.SetActive(false);
            if (parameterTutorialPanel != null) parameterTutorialPanel.SetActive(false);
            if (uiFlowSimulator != null) uiFlowSimulator.MarkParameterTutorialShown();

            ChangeState(GameState.InEventCycle);
            return;
        }

        var stateToRestore = DataManager.Instance.PlayerData.currentGameState;
        // 비플레이 상태는 이어하기 대상에서 제외하고 버튼 숨김
        switch (stateToRestore)
        {
            case GameState.Title:
            case GameState.Login:
            case GameState.MainMenu:
            case GameState.CommanderSelection:
            case GameState.GamePaused:
                if (continueButton != null) continueButton.gameObject.SetActive(false);
                ChangeState(GameState.MainMenu);
                return;
        }

        DataManager.Instance.AllowSavesFromNow();
        // 이어하기 직후 튜토리얼(스토리/파라미터) 표시 억제
        suppressTutorialOnce = true;
        // 안전을 위해 즉시 패널을 내림
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        if (parameterTutorialPanel != null) parameterTutorialPanel.SetActive(false);
        if (uiFlowSimulator != null) uiFlowSimulator.MarkParameterTutorialShown();
        RestoreGameState(stateToRestore);
    }
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
    public void TutorialPanelTouched() { tutorialPanel.SetActive(true); tutorialText.SetActive(true); parameterTutorialPanel.SetActive(true); } 
    public void ParameterTutorialPanelTouched() { parameterTutorialPanel.SetActive(true); } 
    public void OnTitlePanelTouched() { continueButton.gameObject.SetActive(DataManager.Instance.CheckIfSaveDataExists()); ChangeState(GameState.MainMenu); }
    private void OnAuthenticated(SignInStatus status) { if (status == SignInStatus.Success) { Debug.Log("구글 플레이 게임 서비스 로그인 성공!"); } else { Debug.LogError("구글 플레이 게임 서비스 로그인 실패: " + status); } if (FirebaseManager.Instance != null) FirebaseManager.Instance.GPGSLogin(); }
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
        // 새 게임 시작 시점에 파라미터를 명확히 기본값으로 초기화
        ResetParameterDataToDefaults();

        GamePlayerStats.Instance.SetActiveCommander(selectedCommander);
        if (selectedCommander.initialStatAdjustments.Count > 0)
        {
            GamePlayerStats.Instance.ApplyChanges(selectedCommander.initialStatAdjustments);
        }
        // 즉시 저장하여 이후 초기화 루틴이 덮어쓰지 않도록 보존
        if (DataManager.Instance?.PlayerData != null)
        {
            DataManager.Instance.SaveLocal();
        }
        // 튜토리얼 플래그 및 UI 초기화 (데이터 리셋 후 오동작 방지)
        ResetTutorialFlagsAndUI();

        // [수정] 선택 저장을 StoryPackManager가 디스크에 먼저 저장하므로, 시작 직전에 설정을 다시 로드하여 동기화합니다.
        DataManager.Instance.LoadSettings();
        // StoryPackManager가 씬에 존재하면, 매니저를 통해 선택값을 우선 가져옵니다(동일 인스턴스 참조 강제).
        var spm = FindObjectOfType<StoryPackManager>();
        var selectedPacksForNewGame = spm != null ? 
            spm.GetSelectedPackIDs() : (DataManager.Instance.PlayerSettings != null ? 
            DataManager.Instance.PlayerSettings.selectedSubEventPackIDs : null);
        Debug.Log($"[GameManager] 새게임 직전 선택 팩: {(selectedPacksForNewGame != null ? string.Join(", ", selectedPacksForNewGame) : "null")}, 개수: {selectedPacksForNewGame?.Count ?? -1}");
        var sessionManager = FindObjectOfType<GameSessionManager>();
        if (sessionManager != null)
        {
            sessionManager.StartSession();
        }
        await EventManager.Instance.StartNewGame(selectedPacksForNewGame);
        OnStateFinished();
    }
    private void StartDetailedResultSequence() { int warSituation = GamePlayerStats.Instance.GetStat(ParameterType.전황); GameOutcome outcome = (warSituation <= 19) ? GameOutcome.Defeat : (warSituation >= 81) ? GameOutcome.Victory : GameOutcome.Draw; int chapterIndex = CurrentChapter - 1; if (chapterIndex < chapterEndDataList.Count && chapterEndDataList[chapterIndex] != null) { chapterEndController.StartChapterEndSequence(chapterEndDataList[chapterIndex], outcome); } else { OnStateFinished(); } }
    public void GameOver(string reason)
    {
        if (_isGameOverActive) return; // 재진입 방지
        _isGameOverActive = true;
        if (gameOverText != null)
        {
            gameOverText.text = reason;
        }
        // 진행 중인 UI 전환 코루틴이 다음 턴을 호출하지 못하도록 즉시 중단
        if (uiFlowSimulator != null)
        {
            uiFlowSimulator.AbortPendingTransitions();
        }
        // 게임오버 패널 즉시 노출 (원상복구).
        gameOverPanel.SetActive(true);
		// 게임오버 패널을 누르기 전에 종료되더라도 이어하기 시 챕터 처음부터 재시작되도록 즉시 플래그 저장
		if (DataManager.Instance?.PlayerData != null)
		{
			DataManager.Instance.PlayerData.pendingRestartFromGameOver = true;
			DataManager.Instance.PlayerData.pendingRestartChapter = CurrentChapter;
			DataManager.Instance.AllowSavesFromNow();
			DataManager.Instance.SaveLocal();
		}
    }
    // 지연 노출 코루틴 제거 (원상복구)
    public void OnGameOverPanelTouched()
    {
        // 게임 오버 후에는 자동으로 새 게임을 시작하거나 지휘관 선택으로 이동하지 않습니다.
        // 메인 메뉴로 돌아가 플레이어가 다음 행동(새 게임 시작, 구매 등)을 선택할 수 있게 합니다.
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        // 메인화면 복귀 시, 다음 이어하기에서 해당 챕터 처음부터 시작하도록 플래그 설정
        if (DataManager.Instance?.PlayerData != null)
        {
            DataManager.Instance.PlayerData.pendingRestartFromGameOver = true;
            DataManager.Instance.PlayerData.pendingRestartChapter = CurrentChapter;
            DataManager.Instance.SaveLocal();
        }
        ResetAllGameData();
        ChangeState(GameState.MainMenu);
        _isGameOverActive = false; // 게임오버 플로우 종료
    }
    
    // 게임 오버 화면에서 즉시 현재 챕터 진행을 재시작
    public void OnClickRestartAfterGameOver()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        // 같은 프레임에 패널 비활성화와 상태/플로우 전환을 동시에 하면
        // UI 차단/포커스 충돌로 첫 클릭 효과가 시각적으로 지연될 수 있어 한 프레임 뒤에 처리
        StartCoroutine(Co_RestartAfterGameOverNextFrame());
    }

    private System.Collections.IEnumerator Co_RestartAfterGameOverNextFrame()
    {
        // 입력 잔여 처리/레이캐스트 정리를 위해 이벤트 시스템 포커스 해제
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (es != null) es.SetSelectedGameObject(null);

        // 타임스케일이 0으로 잠겨 있었다면 정상 진행을 위해 복구
        if (Time.timeScale == 0f) Time.timeScale = 1f;

        // 한 프레임 대기 후 재시작 실행
        yield return null;
        RestartEventCycleSimple();
        _isGameOverActive = false; // 재시작으로 게임오버 상태 종료
    }

    // 재시작용 간소화 루틴: 이벤트/파라미터만 초기화하고 새 사이클부터 바로 시작
    private void RestartEventCycleSimple()
    {
        if (DataManager.Instance?.PlayerData == null)
        {
            ChangeState(GameState.MainMenu);
            return;
        }

        // 1) 진행 중 전환 중단(PlayNextTurn 잔여 호출 차단), UIPanel 토글 시퀀스는 유지됨
        if (uiFlowSimulator != null) uiFlowSimulator.AbortPendingTransitions();

        // 2) 파라미터/이벤트 관련만 초기화
        ResetParameterDataToDefaults();
        EventManager.Instance.ResetEventManagerState();
        var pd = DataManager.Instance.PlayerData;
        pd.currentChapter = Mathf.Clamp(pd.currentChapter, 1, 6);
        pd.completedEventIds.Clear();
        pd.eventPlaylistIndex = 0;
        pd.currentPlaylist.Clear();

        // 3) 저장 허용 후 새 사이클 구성 → 즉시 저장
        DataManager.Instance.AllowSavesFromNow();
        EventManager.Instance.StartNewCycle();
        pd.currentGameState = GameState.InEventCycle;
        pd.pendingRestartFromGameOver = false;
        pd.pendingRestartChapter = 0;
        DataManager.Instance.SaveLocal();

        // 4) 상태 전환 및 첫 턴 시작
        // 데이터가 모두 안전하게 초기화된 뒤에 가드를 해제
        _isGameOverActive = false;
        ChangeState(GameState.InEventCycle);
        if (uiFlowSimulator != null)
        {
            uiFlowSimulator.BeginFlow(true);
        }
    }
    public void CheckGameOverConditions()
    {
        if (GamePlayerStats.Instance.GetStat(ParameterType.정치력) <= 0)
        {
            GameOver("정치력이 0이 되어 통치 기반을 잃었습니다.");
        }
        else if (GamePlayerStats.Instance.GetStat(ParameterType.병력) <= 0)
        {
            GameOver("병력이 0이 되어 전선을 유지할 수 없습니다.");
        }
        else if (GamePlayerStats.Instance.GetStat(ParameterType.물자) <= 0)
        {
            GameOver("물자가 0이 되어 부대를 운용할 수 없습니다.");
        }
        else if (GamePlayerStats.Instance.GetStat(ParameterType.리더십) <= 0)
        {
            GameOver("리더십이 0이 되어 병사들이 따르지 않습니다.");
        }
    }
    public void ResetAllGameData() { EventManager.Instance.ResetEventManagerState(); battleTurnManager.ResetForNewBattle(); mainScenarioManager.ResetScenarioState();
        // 파라미터 UI 잔상(토글/하이라이트) 제거
        var paramUI = FindObjectOfType<ParameterUIController>();
        if (paramUI != null) { paramUI.ClearAllToggles(); }
    }
    private void ResetParameterDataToDefaults()
    {
        // DataManager의 GameData가 새로 생성되지 않은 경우에도 안전하게 기본값을 보장
        GamePlayerStats.Instance.SetStat(ParameterType.정치력, 50);
        GamePlayerStats.Instance.SetStat(ParameterType.병력, 50);
        GamePlayerStats.Instance.SetStat(ParameterType.물자, 50);
        GamePlayerStats.Instance.SetStat(ParameterType.리더십, 50);
        GamePlayerStats.Instance.SetStat(ParameterType.전황, 50);
        GamePlayerStats.Instance.SetStat(ParameterType.카르마, 50);

        // UI 즉시 반영
        var paramUI = FindObjectOfType<ParameterUIController>();
        if (paramUI != null) { paramUI.InitializeAndDisplayStats(); }
    }
    private void ResetTutorialFlagsAndUI()
    {
        // 내부 플래그 초기화
        hasShownWarTutorialThisPlaythrough = false;
        // UIFlowSimulator 튜토리얼 상태 초기화
        if (uiFlowSimulator != null)
        {
            uiFlowSimulator.ResetTutorialState();
        }
        // 패널 안전하게 닫기
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        if (tutorialText != null) tutorialText.SetActive(false);
        if (parameterTutorialPanel != null) parameterTutorialPanel.SetActive(false);
        if (warTutorialPanel != null) warTutorialPanel.SetActive(false);
    }
    private IEnumerator RefreshUINextFrame()
    {
        // Canvas 활성화 반영 이후에 실행되도록 한 프레임 대기
        yield return null;
        // 지휘관 잠금 오버레이, 시작 버튼 상태 등 갱신
        var carousel = FindObjectOfType<CommanderCarouselController>();
        if (carousel != null)
        {
            // 캐러셀의 설명/잠금 알림/버튼 상태까지 완전 초기화
            carousel.RefreshUIState();
        }
        // 파라미터 슬라이더/락 상태 초기화 및 현재 값 반영
        var paramUI = FindObjectOfType<ParameterUIController>();
        if (paramUI != null)
        {
            paramUI.InitializeAndDisplayStats();
        }
    }
    private void EnsureCheatManagerExists()
    {
        if (FindObjectOfType<CheatManager>() == null)
        {
            var cheatGo = new GameObject("CheatManager_AutoSpawn");
            cheatGo.AddComponent<CheatManager>();
            DontDestroyOnLoad(cheatGo);
            Debug.Log("[GameManager] CheatManager가 없어 자동 생성했습니다 (에디터/개발 빌드 전용).");
        }
    }
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

    #region 업적 시스템 관련 메서드들
    
    /// <summary>
    /// 업적 UI 열기
    /// </summary>
    public void OpenAchievementUI()
    {
        if (achievementPanel != null)
        {
            achievementPanel.SetActive(true);
            
            // AchievementUIManager 컴포넌트가 있다면 UI 새로고침
            var achievementUIManager = achievementPanel.GetComponent<AchievementUIManager>();
            if (achievementUIManager != null)
            {
                achievementUIManager.OpenAchievementUI();
            }
            
            Debug.Log("[GameManager] 업적 UI가 열렸습니다.");
        }
        else
        {
            Debug.LogWarning("[GameManager] achievementPanel이 설정되지 않았습니다.");
        }
    }
    
    /// <summary>
    /// 업적 UI 닫기
    /// </summary>
    public void CloseAchievementUI()
    {
        if (achievementPanel != null)
        {
            achievementPanel.SetActive(false);
            Debug.Log("[GameManager] 업적 UI가 닫혔습니다.");
        }
    }
    
    /// <summary>
    /// 특정 타입의 업적 UI 열기
    /// </summary>
    public void OpenAchievementUI(AchievementType type)
    {
        if (achievementPanel != null)
        {
            achievementPanel.SetActive(true);
            
            // AchievementUIManager 컴포넌트가 있다면 해당 타입으로 UI 열기
            var achievementUIManager = achievementPanel.GetComponent<AchievementUIManager>();
            if (achievementUIManager != null)
            {
                achievementUIManager.OpenAchievementUI(type);
            }
            
            Debug.Log($"[GameManager] {type} 타입의 업적 UI가 열렸습니다.");
        }
        else
        {
            Debug.LogWarning("[GameManager] achievementPanel이 설정되지 않았습니다.");
        }
    }
    
    /// <summary>
    /// 업적 UI 토글 (열림/닫힘 상태 전환)
    /// </summary>
    public void ToggleAchievementUI()
    {
        if (achievementPanel != null)
        {
            bool isActive = achievementPanel.activeSelf;
            
            if (isActive)
            {
                CloseAchievementUI();
            }
            else
            {
                OpenAchievementUI();
            }
        }
    }
    
    /// <summary>
    /// 업적 UI가 현재 열려있는지 확인
    /// </summary>
    public bool IsAchievementUIOpen()
    {
        return achievementPanel != null && achievementPanel.activeSelf;
    }
    
    #endregion
}