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
    // 로그인 완료 후에만 타이틀 터치로 메인 메뉴로 진입하도록 제어
    private bool hasCompletedLogin = false;
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
        [SerializeField] private ShopUI shopUI;

    [Header("컷신 시스템")]
    [SerializeField] private CutsceneManager cutsceneManager;
    [SerializeField] private CutsceneData chapter1OpeningCutscene;
    // 최소 변경: 지휘관별 오프닝 컷씬(SO) 선택 지원. 미지정 시 chapter1OpeningCutscene로 폴백
    [SerializeField] private CutsceneData openingCutscene_Devost;
    [SerializeField] private CutsceneData openingCutscene_Wille;
    [SerializeField] private CutsceneData openingCutscene_Risard;
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

        // [변경] 특수 이벤트 매니저는 씬/프리팹에 미리 배치하여 인스펙터에서 트리거를 설정합니다.
        // (자동 생성 로직 제거)
        
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }

        isInitialized = true;

        ChangeState(GameState.Title);
        
        // 로딩 완료 후 로그인 처리 시작
        if (FirebaseManager.Instance != null)
        {
            // FirebaseManager가 이미 초기화되어 있으면 바로 로그인 처리
            HandleLoginFlow();
        }
        else
        {
            // FirebaseManager 초기화를 기다림
            FirebaseManager.OnFirebaseManagerInitialized += HandleLoginFlow;
        }
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
        if (isOpen)
        {
            // 패널을 열 때는 검증 없이 바로 열기
            if (subEventSelectPanel != null)
            {
                subEventSelectPanel.SetActive(true);
            }
        }
        else
        {
            // 패널을 닫을 때는 서브이벤트팩 선택 검증
            var storyPackManager = FindObjectOfType<StoryPackManager>();
            if (storyPackManager != null && !storyPackManager.CanClosePanel())
            {
                // 검증 실패 시 패널을 닫지 않음
                return;
            }
            
            if (subEventSelectPanel != null)
            {
                subEventSelectPanel.SetActive(false);
            }
        }
    }

    // 서브이벤트 패널을 검증 없이 직접 닫는 함수 (StoryPackManager에서 사용)
    public void CloseSubEventPanelDirectly()
    {
        if (subEventSelectPanel != null)
        {
            subEventSelectPanel.SetActive(false);
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
                // 엔딩 분기 규칙 - 기획서 기준 일치화
                const int NORMAL_ENDING_ID = 1001; // 1회차 일반 엔딩 (EndingString 1001)
                const int TRUE_ENDING_ID_2ND = 1002; // 2회차 진엔딩 (EndingString 1002)
                const int TRUE_ENDING_ID_3RD = 1003; // 3회차 진엔딩 (EndingString 1003)

                var playerData = DataManager.Instance.PlayerData;

                // MultiEndingSystem을 통해 최종 엔딩 데이터를 가져옵니다.
                EndingData determinedEnding = MultiEndingSystem.Instance.GetFinalEndingData();
                FullEndingData determinedFullData = MultiEndingSystem.Instance.GetFullEndingData(determinedEnding);

                int determinedEndingId = -1;
                if (determinedFullData != null)
                {
                    // 해당 엔딩의 '고유 ID'를 가져옵니다.
                    determinedEndingId = (int)determinedFullData.ID;
                }
                else
                {
                    // MultiEndingSystem이 엔딩을 결정하지 못했을 경우의 안전장치
                    // 기존 상수 기반 ID 사용으로 폴백
                    Debug.LogWarning("[GameManager] MultiEndingSystem에서 엔딩 데이터를 가져오지 못했습니다. 기존 상수 기반 시스템으로 폴백합니다.");
                    var endingType = MultiEndingSystem.Instance?.DetermineEndingType() ?? EndingType.General;
                    var endingRoute = MultiEndingSystem.Instance?.DetermineEndingRoute() ?? EndingRoute.Truce;

                    if (playerData.playthroughCount == 1)
                    {
                        determinedEndingId = NORMAL_ENDING_ID;
                    }
                    else if (playerData.playthroughCount == 2)
                    {
                        // 기획: 2회차 +4점 이상 AND 승리 루트일 때 진엔딩
                        if (endingType == EndingType.True && endingRoute == EndingRoute.Victory)
                        {
                            determinedEndingId = TRUE_ENDING_ID_2ND;
                        }
                        else
                        {
                            determinedEndingId = NORMAL_ENDING_ID;
                        }
                    }
                    else // 3회차 이상: MultiEndingSystem의 True/Hidden 기준 사용
                    {
                        if (endingType == EndingType.True || endingType == EndingType.Hidden)
                        {
                            determinedEndingId = TRUE_ENDING_ID_3RD;
                        }
                        else
                        {
                            determinedEndingId = NORMAL_ENDING_ID;
                        }
                    }
                }

                // 결정된 '고유 ID'를 기록합니다.
                DataManager.Instance.RecordEnding(determinedEndingId);
                PlaythroughHistory.Instance.RecordEndingCompletion(determinedEndingId);

                // 이제 기록된 lastEndingId와 엔딩의 '타입'을 기반으로 분기 로직을 실행합니다.
                EndingType finalEndingType = determinedEnding?.endingType ?? EndingType.General;

                // 회차 진행/리셋 처리 로직 - 두 시스템의 장점을 결합
                if (playerData.playthroughCount == 2 && finalEndingType != EndingType.True)
                {
                    Debug.Log($"[분기] 2회차, 진엔딩이 아니므로 2회차를 다시 시작합니다. 달성한 엔딩: {finalEndingType}");
                    // 회차를 증가시키지 않고 현재 챕터만 1로 리셋
                    playerData.currentChapter = 1;
                    playerData.currentGameState = GameState.MainMenu;
                    playerData.completedEventIds.Clear();
                    playerData.eventPlaylistIndex = 0;
                    playerData.currentPlaylist.Clear();
                    // 선택 카운트 리셋은 유지
                    playerData.realEnding2ChoiceCount = 0;
                    Debug.Log("[분기] 2회차 재시작을 위한 상태 초기화 완료");
                }
                // 3회차 이상에서 히든엔딩을 봤다면 게임 완전 초기화
                else if (playerData.playthroughCount >= 3 && finalEndingType == EndingType.Hidden)
                {
                    Debug.Log($"[분기] 3회차 이상, 히든엔딩을 봤으므로 게임을 초기화합니다.");
                    // 새 게임 데이터로 덮어쓰고, 지휘관 선택 화면으로 이동
                    DataManager.Instance.StartNewGame();
                    ResetAllGameData();
                    nextState = GameState.CommanderSelection;
                    break;
                }
                else
                {
                    // 일반적인 다음 회차 진행
                    playerData.playthroughCount++;
                    playerData.currentChapter = 1;
                    playerData.currentGameState = GameState.MainMenu;
                }

                hasShownWarTutorialThisPlaythrough = false;
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
            newState != GameState.CommanderSelection);

        commanderSelectionCanvas.SetActive(newState == GameState.CommanderSelection);
        storyPanel.SetActive(newState == GameState.InStory);
        InGameUIPanel.SetActive(newState == GameState.InEventCycle || newState == GameState.InStory);
        battlePanel.SetActive(newState == GameState.InBattle);
        battleResultPanel.SetActive(newState == GameState.InBattleResult);
        chapterResultPanel.SetActive(newState == GameState.InChapterResult);
        optionCanvas.SetActive(newState == GameState.GamePaused);
        gameOverPanel.SetActive(false);

        // 메인화면으로 이동할 때 BGM 처리
        if (newState == GameState.MainMenu || newState == GameState.Title || newState == GameState.Login)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopBGM();
                Debug.Log("[GameManager] 메인화면으로 이동하여 BGM을 중단했습니다.");
                
                // 메인메뉴 상태일 때는 BGM을 재생하지 않음 (메인 스토리에서 재생됨)
            }
        }

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
            // 파라미터 이벤트는 함부로 비활성화하지 않도록 보호
            // if (parameterTutorialPanel != null) parameterTutorialPanel.SetActive(false);
        }

        // 한 프레임 동안만 억제하도록 플래그 해제
        if (suppressTutorialOnce) suppressTutorialOnce = false;
        
        // MainMenu 상태일 때 이어하기 버튼 활성화
        if (newState == GameState.MainMenu && continueButton != null)
        {
            bool hasSaveData = DataManager.Instance != null && DataManager.Instance.CheckIfSaveDataExists();
            continueButton.gameObject.SetActive(hasSaveData);
            Debug.Log($"[GameManager] MainMenu 상태에서 이어하기 버튼 설정: {hasSaveData}");
        }
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
        // 파라미터 이벤트 사이클에서 벗어날 때(전투/컷신/결과 등) BGM 즉시 정지
        if (currentGameState == GameState.InEventCycle && newState != GameState.InEventCycle)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopBGM();
            }
        }
        UnsubscribeFromCurrentStateEvent();

        currentGameState = newState;
        Debug.Log($"[게임 상태 변경] -> {newState}");
        
        // 상태 변경 이벤트 발생
        OnGameStateChanged?.Invoke(newState);

        SetUIForState(newState);
        if (newState == GameState.InBattle)
        {
            Debug.Log(battleTurnManager == null ? "오류: battleTurnManager 참조가 없습니다!" : "1단계 OK: battleTurnManager 참조 정상");
        }

        Debug.Log($"[GameManager] 가 바라보는 WarTurnManager ID: {battleTurnManager.GetInstanceID()}");
        
        // 안전 가드: battleTurnManager가 null일 수 있으므로 NRE 방지
        if (battleTurnManager != null)
        {
            Debug.Log($"[GameManager] 가 바라보는 WarTurnManager ID: {battleTurnManager.GetInstanceID()}");
        }
        switch (newState)
        {
            case GameState.Login:
                // FirebaseManager를 통해 로그인 처리 (중복 방지)
                Debug.Log("[GameManager] Login 상태 - FirebaseManager를 통해 로그인 처리");
                if (FirebaseManager.Instance != null)
                {
                    // FirebaseManager가 개발 모드에서 로그인 선택 UI를 표시하도록 함
                    // (FirebaseManager에서 자동으로 처리됨)
                }
                else
                {
                    Debug.LogError("[GameManager] FirebaseManager 인스턴스가 없습니다!");
                }
                break;
            case GameState.PlayingOpeningCutscene:
                CutsceneManager.OnCutsceneFinished += OnStateFinished;
                {
                    var opening = GetOpeningCutsceneForActiveCommander();
                    cutsceneManager.StartCutscene(opening);
                }
                break;
            case GameState.InEventCycle:
                if (cardController != null) cardController.choiceHandler = uiFlowSimulator;
                // 사이클 완료 시 특수 이벤트를 우선 실행한 뒤 컷신으로 진행
                EventManager.OnEventCycleCompleted += OnEventCycleCompleted_Handle;

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
                // 배틀 컷신 시작 전 메인 스토리 BGM 정지
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.StopBGM();
                    Debug.Log("[GameManager] 배틀 컷신 시작: 메인 스토리 BGM 정지");
                }
                CutsceneManager.OnCutsceneFinished += OnStateFinished;
                int chapterIndex = CurrentChapter - 1;
                if (chapterMidCutscenes != null && chapterIndex < chapterMidCutscenes.Count && chapterMidCutscenes[chapterIndex] != null)
                    cutsceneManager.StartCutscene(chapterMidCutscenes[chapterIndex]);
                else
                    OnStateFinished();
                break;
            case GameState.InBattle:
                // 배틀 BGM 재생 (배틀 전용 음악 재생)
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.StopBGM();
                    AudioManager.Instance.PlayBGMByName("BattleMusic");
                    Debug.Log("[GameManager] 배틀 시작: 배틀 음악 재생");
                }
                if (DataManager.Instance?.PlayerData != null &&
                    DataManager.Instance.PlayerData.playthroughCount == 1 &&
                    CurrentChapter == 1 &&
                    !hasShownWarTutorialThisPlaythrough)
                {
                    if (warTutorialPanel != null) warTutorialPanel.SetActive(true);
                }
                battleTurnManager.ResetForNewBattle();
                // 전투 종료 시 결과 화면으로 진입하도록 이벤트 구독 복구
                if (battleTurnManager != null)
                {
                    battleTurnManager.OnBattleEnd += HandleBattleEnd;
                }
                Debug.Log("2단계 OK: GameManager가 OnBattleEnd 신호를 구독했습니다.");
                break;
            // InBattleResult 상태에 대한 로직 추가 (현재는 UI 표시 외에 특별한 동작 없음)
            case GameState.InBattleResult:
                // 배틀 종료 후 배틀 음악 정지
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.StopBGM();
                    Debug.Log("[GameManager] 배틀 결과 화면: 배틀 음악 정지");
                }
                // 이 상태는 UI 버튼 클릭을 통해 다음 상태로 진행되므로, 여기서는 대기합니다.
                break;
            case GameState.InChapterResult:
                //   ChapterResultController.OnSequenceComplete += OnStateFinished;
                StartDetailedResultSequence();
                break;
            case GameState.PlayingEndingCutscene:
                // 멀티 엔딩 시스템을 사용하여 적절한 엔딩 결정
                var endingData = MultiEndingSystem.Instance?.GetFinalEndingData();
                
                // 기존 finalEndingCutscene 대신 동적으로 결정된 엔딩 사용
                if (endingData?.fullEndingData != null)
                {
                    // FullEndingData를 CutsceneData로 변환
                    var cutsceneData = MultiEndingSystem.Instance.ConvertToCutsceneData(endingData.fullEndingData);
                    
                    if (cutsceneData != null)
                    {
                        Debug.Log($"[GameManager] 멀티 엔딩 재생: {endingData.endingType} - {endingData.route} - {endingData.branch}");
                        CutsceneManager.OnCutsceneFinished += OnStateFinished;
                        cutsceneManager.StartCutscene(cutsceneData);
                    }
                    else
                    {
                        // 변환 실패 시 폴백
                        Debug.LogWarning("[GameManager] 엔딩 데이터 변환 실패, 기본 엔딩 사용");
                        CutsceneManager.OnCutsceneFinished += OnStateFinished;
                        cutsceneManager.StartCutscene(finalEndingCutscene);
                    }
                }
                else
                {
                    // 폴백: 기존 엔딩 사용
                    Debug.Log("[GameManager] 엔딩 데이터 없음, 기본 엔딩 사용");
                    CutsceneManager.OnCutsceneFinished += OnStateFinished;
                    cutsceneManager.StartCutscene(finalEndingCutscene);
                }
                
                // 엔딩 기록 저장
                if (DataManager.Instance?.PlayerData != null && endingData != null)
                {
                    string endingKey = $"{endingData.endingType}_{endingData.route}_{endingData.branch}";
                    if (!DataManager.Instance.PlayerData.completedEndings.Contains(endingKey))
                    {
                        DataManager.Instance.PlayerData.completedEndings.Add(endingKey);
                        DataManager.Instance.SaveData();
                    }
                    
                    // 업적 체크 - AchievementIntegration 직접 호출
                    var achievementIntegration = FindObjectOfType<AchievementIntegration>();
                    if (achievementIntegration != null)
                    {
                        achievementIntegration.OnLoopCompleted(DataManager.Instance.PlayerData.playthroughCount);
                        Debug.Log($"[GameManager] 엔딩 완료 업적 체크: {endingData.endingType} - {endingData.route} - {endingData.branch}");
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
            DataManager.Instance.SaveData();
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
                EventManager.OnEventCycleCompleted -= OnEventCycleCompleted_Handle;
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

    // 사이클 완료 시 특수 이벤트(있으면) 우선 실행, 종료 후 컷신으로 진행
    private void OnEventCycleCompleted_Handle()
    {
        if (SpecialEventManager.Instance != null)
        {
            bool started = SpecialEventManager.Instance.TryTriggerAfterCycle();
            if (started)
            {
                SpecialEventManager.OnSpecialEventChainEnded += ProceedToChapterCutscene;
                return;
            }
        }
        ProceedToChapterCutscene();
    }

    private void ProceedToChapterCutscene()
    {
        SpecialEventManager.OnSpecialEventChainEnded -= ProceedToChapterCutscene;
        OnStateFinished();
    }

    //  전투 종료 시 호출되는 함수 변경
    private void HandleBattleEnd(string resultLog)
    {
        if (battleResultText != null)
        {
            battleResultText.text = resultLog; 
        }

        string outcome = ParseOutcome(resultLog);
        int currentChapter = DataManager.Instance.PlayerData.currentChapter;

        GameOutcome battleOutcome = ConvertOutcomeToEnum(outcome);

        if (MultiEndingSystem.Instance != null)
        {
            // 현재 전세 수치 가져오기
            int warSituation = 50; // 기본값
            if (GamePlayerStats.Instance != null)
            {
                warSituation = GamePlayerStats.Instance.GetStat(ParameterType.전황);
            }
            
            MultiEndingSystem.Instance.RecordChapterResult(
                currentChapter,
                battleOutcome,
                warSituation
            );
            Debug.Log($"[GameManager] 챕터 {currentChapter} 결과와 점수를 MultiEndingSystem에 기록했습니다. (전세: {warSituation})");
        }


        if (SimpleEventHistoryManager.Instance != null)
        {
            SimpleEventHistoryManager.Instance.RecordChapterOutcome(currentChapter, outcome);
            Debug.Log($"[GameManager] 챕터 {currentChapter}의 전투 결과({outcome})를 기록했습니다.");
        }
        ChangeState(GameState.InBattleResult);
    }
    private string ParseOutcome(string resultLog)
    {
        if (resultLog.Contains("승리"))
        {
            return "승리";
        }
        if (resultLog.Contains("패배"))
        {
            return "패배";
        }
        if (resultLog.Contains("무승부"))
        {
            return "무승부";
        }
        return "알 수 없음"; // 예외 처리
    }
    private GameOutcome ConvertOutcomeToEnum(string outcome)
    {
        return outcome switch
        {
            "승리" => GameOutcome.Victory,
            "패배" => GameOutcome.Defeat,
            "무승부" => GameOutcome.Draw,
            _ => GameOutcome.Draw // 안전장치
        };
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
                // 전투 시작 전 상태 초기화
                if (battleTurnManager != null)
                {
                    battleTurnManager.ResetForNewBattle();
                    Debug.Log("[GameManager] 전투 상태 초기화 완료");
                }
                // 복원 시에도 전투 종료 이벤트 구독 보장
                if (battleTurnManager != null)
                {
                    battleTurnManager.OnBattleEnd += HandleBattleEnd;
                }
                break;
        }
    }
    public void HideTutorial() { 
        if (tutorialPanel != null && tutorialPanel.activeSelf) { 
            tutorialPanel.SetActive(false); 
            // 파라미터 이벤트는 함부로 비활성화하지 않도록 보호
            // parameterTutorialPanel.SetActive(false); 
        } 
    }
    
    /// <summary>
    /// 파라미터 이벤트 패널을 안전하게 비활성화하는 메서드
    /// 특별한 상황에서만 호출되어야 함
    /// </summary>
    public void SafeHideParameterEventPanel()
    {
        if (parameterTutorialPanel != null && parameterTutorialPanel.activeSelf)
        {
            Debug.Log("[GameManager] 파라미터 이벤트 패널을 안전하게 비활성화합니다.");
            parameterTutorialPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 파라미터 이벤트 패널을 활성화하는 메서드
    /// 파라미터 이벤트가 필요할 때 사용
    /// </summary>
    public void ShowParameterEventPanel()
    {
        if (parameterTutorialPanel != null && !parameterTutorialPanel.activeSelf)
        {
            Debug.Log("[GameManager] 파라미터 이벤트 패널을 활성화합니다.");
            parameterTutorialPanel.SetActive(true);
        }
    }
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
        // 서브이벤트팩 선택 검증
        DataManager.Instance.LoadSettings();
        var selectedPacks = DataManager.Instance.PlayerSettings?.selectedSubEventPackIDs;
        
        Debug.Log($"[GameManager] 새 게임 시작 전 서브이벤트팩 검증 - 선택된 팩: {(selectedPacks != null ? string.Join(", ", selectedPacks) : "null")}, 개수: {selectedPacks?.Count ?? 0}");
        
        if (selectedPacks == null || selectedPacks.Count == 0)
        {
            Debug.LogError("[GameManager] 서브이벤트팩이 선택되지 않았습니다. 새 게임을 시작할 수 없습니다.");
            ShowConfirmation("최소 1개의 서브이벤트팩을 선택해야 새 게임을 시작할 수 있습니다.", () => { });
            return;
        }

        ShowConfirmation("모든 진행 상황과 설정이 삭제됩니다. 정말 새로 시작하시겠습니까?", () =>
        {
			// 새 게임 전 현재 해금 상태를 보존
			var preservedUnlockedTraits = new List<CommanderTrait>();
			foreach (CommanderTrait trait in System.Enum.GetValues(typeof(CommanderTrait)))
			{
				if (UnlockManager.IsUnlocked(trait))
				{
					preservedUnlockedTraits.Add(trait);
				}
			}
            // 진행도만 삭제하여 Settings(서브이벤트 팩 선택, 업적 데이터 등)는 보존
            DataManager.Instance.DeleteLocalSaveData();

            // 데이터를 모두 지운 후, 새 데이터 객체를 생성하고 게임을 시작합니다.
            // 주의: 업적 데이터는 SettingsData에 저장되므로 StartNewGame()으로 초기화되지 않습니다.
            DataManager.Instance.StartNewGame();
            DataManager.Instance.LoadSettings(); // 삭제 후 새로 로드 (업적 포함)
            ResetAllGameData();
			// 보존된 해금 상태 재적용 (혹시 초기화된 경우 대비)
			foreach (var trait in preservedUnlockedTraits)
			{
				UnlockManager.Unlock(trait);
			}
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
            // 진행도와 설정 모두 삭제
            DataManager.Instance.DeleteLocalSaveData();

            // 2. 메모리에 새로운 기본 데이터 객체를 즉시 생성하고 로드
            // StartNewGame은 새 GameData를 만들고 기본 파일까지 생성해줍니다.
            DataManager.Instance.StartNewGame();
            // LoadSettings는 파일이 없으면 새 SettingsData를 만들어주지만,
            // 디버그 전체 초기화에서는 '설정'도 완전 초기화가 필요하므로 강제로 새로 생성/저장합니다.
            DataManager.Instance.LoadSettings();
            DataManager.Instance.PlayerSettings = new SettingsData();
            // 서브이벤트팩은 기본값(1번 팩)으로 유지
            DataManager.Instance.PlayerSettings.selectedSubEventPackIDs = new List<int> { 1 };
            // 여기서 저장: 구매 이력(purchasedShopItemIds), 스토리팩 해금(unlockedStoryPackIds), 지휘관 해금 목록 등 초기화값이 파일에 반영됨
            DataManager.Instance.SaveSettings();

			// 3. EventManager 등 다른 게임 시스템들의 상태 초기화
            ResetAllGameData();
			// [디버깅 전용] 전체 초기화에서는 업적/해금 모두 초기화
			// 주의: 일반 "새 게임"에서는 업적이 보존됩니다!
			if (AchievementManager.Instance != null)
			{
				AchievementManager.Instance.ResetAllAchievements();
			}
			UnlockManager.ResetAllUnlocks();

            // 4. 메인 메뉴로 돌아가 UI를 갱신합니다.
            // (예: '이어하기' 버튼이 사라지는 등 초기화된 상태를 시각적으로 보여줌)
            // [추가] 데이터 리셋 직후 이어하기 버튼 즉시 숨김
            if (continueButton != null) continueButton.gameObject.SetActive(false);
            ChangeState(GameState.MainMenu);
            // UI 상태 강제 갱신 (지휘관 잠금 표시/파라미터 토글 초기화 등)
            StartCoroutine(RefreshUINextFrame());

            // [추가] 상점 즉시 재초기화하여 구매 상태가 즉시 반영되도록 함
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.ReinitializeNow();
            }
        });
    }
    public void OnClickContinueGame()
    {
        Debug.Log("[GameManager] OnClickContinueGame 호출됨");
        
        // 저장 파일 존재 여부 확인. 없으면 버튼 숨기고 동작 중단
        if (DataManager.Instance == null || !DataManager.Instance.CheckIfSaveDataExists())
        {
            Debug.Log("[GameManager] 저장 파일이 없어서 이어하기 버튼을 숨깁니다.");
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
            DataManager.Instance.SaveData();

            // 튜토리얼 억제 및 즉시 숨김
            suppressTutorialOnce = true;
            if (tutorialPanel != null) tutorialPanel.SetActive(false);
            if (parameterTutorialPanel != null) parameterTutorialPanel.SetActive(false);
            if (uiFlowSimulator != null) uiFlowSimulator.MarkParameterTutorialShown();

            ChangeState(GameState.InEventCycle);
            return;
        }

        var stateToRestore = DataManager.Instance.PlayerData.currentGameState;
        Debug.Log($"[GameManager] 복원할 게임 상태: {stateToRestore}, 현재 회차: {DataManager.Instance.PlayerData.playthroughCount}");
        
        // [수정] 2회차 이상에서 CommanderSelection 상태인 경우 자동으로 지휘관 선택
        if (stateToRestore == GameState.CommanderSelection && 
            DataManager.Instance.PlayerData.playthroughCount > 1)
        {
            Debug.Log("[GameManager] CommanderSelection 상태에서 다음 회차를 시작합니다.");
            StartNextPlaythroughWithSameCommander();
            return;
        }

        // 비플레이 상태는 이어하기 대상에서 제외하고 버튼 숨김
        switch (stateToRestore)
        {
            case GameState.Title:
            case GameState.Login:
            case GameState.CommanderSelection:
            case GameState.GamePaused:
                Debug.Log($"[GameManager] 비플레이 상태({stateToRestore})로 인해 이어하기를 건너뜁니다.");
                if (continueButton != null) continueButton.gameObject.SetActive(false);
                ChangeState(GameState.MainMenu);
                return;
            case GameState.MainMenu:
                // MainMenu 상태일 때는 다음 회차를 시작
                Debug.Log($"[GameManager] MainMenu 상태에서 다음 회차를 시작합니다. 현재 회차: {DataManager.Instance.PlayerData.playthroughCount}");
                StartNextPlaythroughWithSameCommander();
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

                    DataManager.Instance.SaveData();
                }
            }
            ChangeState(GameState.MainMenu);
            isReturningToTitle = false;
        });
    }
    public void TutorialPanelTouched() { tutorialPanel.SetActive(true); tutorialText.SetActive(true); parameterTutorialPanel.SetActive(true); } 
    public void ParameterTutorialPanelTouched() { parameterTutorialPanel.SetActive(true); } 
    public void OnTitlePanelTouched()
    {
        // 로그인 완료 전에는 메인 메뉴로 넘어가지 않음
        if (!hasCompletedLogin)
        {
            Debug.Log("[GameManager] 로그인이 완료되지 않았습니다. 먼저 로그인을 완료하세요.");
            return;
        }
        
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(DataManager.Instance.CheckIfSaveDataExists());
        }
        ChangeState(GameState.MainMenu);
    }
    /// <summary>
    /// FirebaseManager 초기화 완료 후 로그인 플로우 처리
    /// </summary>
    private void HandleLoginFlow()
    {
        Debug.Log("[GameManager] 로그인 플로우 시작 (로딩 완료 후)");
        
        // FirebaseManager의 로그인 상태 변경 이벤트 구독
        FirebaseManager.OnLoginStateChanged += OnLoginStateChanged;
        
        // 이미 로그인되어 있으면 타이틀 대기 상태로 유지
        if (FirebaseManager.IsLoggedIn)
        {
            Debug.Log("[GameManager] 이미 로그인되어 있음. 타이틀에서 대기");
            hasCompletedLogin = true;
            ChangeState(GameState.Title);
        }
        else
        {
#if UNITY_EDITOR
            // 에디터 환경에서는 로딩 완료 후 GPGS 로그인 시도 (팝업 표시)
            Debug.Log("[GameManager] 에디터 환경: GPGS 로그인 시도 -> 실패 -> 팝업 표시");
            if (FirebaseManager.Instance != null)
            {
                FirebaseManager.Instance.GPGSLogin();
            }
#endif
        }
    }
    
    /// <summary>
    /// 로그인 상태 변경 시 호출
    /// </summary>
    private void OnLoginStateChanged(LoginType loginType)
    {
        Debug.Log($"[GameManager] 로그인 상태 변경: {loginType}");
        
        if (loginType != LoginType.None)
        {
            // 로그인 완료: 타이틀 화면에서 대기, 터치 시 메인메뉴로 이동
            hasCompletedLogin = true;
            ChangeState(GameState.Title);
        }
    }
    
    
    /// <summary>
    /// 기존 OnAuthenticated 메서드 (호환성을 위해 유지하되 사용하지 않음)
    /// </summary>
    private void OnAuthenticated(SignInStatus status) 
    { 
        Debug.LogWarning("[GameManager] OnAuthenticated 호출됨 - 이 메서드는 더 이상 사용되지 않습니다. FirebaseManager를 통해 로그인하세요.");
    }
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
            DataManager.Instance.SaveData();
        }
        // 튜토리얼 플래그 및 UI 초기화 (데이터 리셋 후 오동작 방지)
        ResetTutorialFlagsAndUI();

        // [수정] 선택 저장을 StoryPackManager가 디스크에 먼저 저장하므로, 시작 직전에 설정을 다시 로드하여 동기화합니다.
        DataManager.Instance.LoadSettings();
        // StoryPackManager가 씬에 존재하면, 매니저를 통해 선택값을 우선 가져옵니다(동일 인스턴스 참조 강제).
        var spm = FindObjectOfType<StoryPackManager>();
        var selectedPacksForNewGame = spm != null ? spm.GetSelectedPackIDs() : (DataManager.Instance.PlayerSettings != null ? DataManager.Instance.PlayerSettings.selectedSubEventPackIDs : null);
        Debug.Log($"[GameManager] 새게임 직전 선택 팩: {(selectedPacksForNewGame != null ? string.Join(", ", selectedPacksForNewGame) : "null")}, 개수: {selectedPacksForNewGame?.Count ?? -1}");
        await EventManager.Instance.StartNewGame(selectedPacksForNewGame);
		
		// 업적: 새게임 시작 직후 회차 업적 체크
		// 새게임 시작 시 playthroughCount는 1로 설정되므로 1회차 시작 업적이 해금됨
		if (AchievementManager.Instance != null && DataManager.Instance?.PlayerData != null)
		{
			Debug.Log($"[GameManager] 새게임 시작 - 회차 업적 체크: {DataManager.Instance.PlayerData.playthroughCount}회차");
			AchievementManager.Instance.CheckPlaythroughAchievements(DataManager.Instance.PlayerData.playthroughCount);
		}
        OnStateFinished();
    }
    private void StartDetailedResultSequence() 
    { 
        int warSituation = GamePlayerStats.Instance.GetStat(ParameterType.전황); 
        GameOutcome outcome = (warSituation <= 19) ? GameOutcome.Defeat : (warSituation >= 81) ?  GameOutcome.Victory : GameOutcome.Draw; 
        int chapterIndex = CurrentChapter - 1; 
        if (chapterIndex < chapterEndDataList.Count && chapterEndDataList[chapterIndex] != null) 
        { 
            chapterEndController.StartChapterEndSequence(chapterEndDataList[chapterIndex], outcome); 
        } 
        else 
        { 
            OnStateFinished(); 
        } 
    }
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
			DataManager.Instance.SaveData();
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
            DataManager.Instance.SaveData();
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
        DataManager.Instance.SaveData();

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
    public void ResetAllGameData() { EventManager.Instance.ResetEventManagerState(); 
        if (battleTurnManager != null && battleTurnManager.gameObject.activeInHierarchy) 
        { 
            battleTurnManager.ResetForNewBattle(); 
        }
        mainScenarioManager.ResetScenarioState();
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
        // 파라미터 이벤트는 함부로 비활성화하지 않도록 보호
        // if (parameterTutorialPanel != null) parameterTutorialPanel.SetActive(false);
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
                    DataManager.Instance.SaveData();
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

    private void StartNextPlaythroughWithSameCommander()
    {
        Debug.Log("[GameManager] StartNextPlaythroughWithSameCommander 호출됨");
        var playerData = DataManager.Instance.PlayerData;
        
        // 기존 지휘관 정보 복원
        if (GamePlayerStats.Instance != null)
        {
            // 저장된 지휘관 특성으로 CommanderInfo 찾기
            var selectedCommander = GetCommanderByTrait(playerData.activeTrait);
            if (selectedCommander != null)
            {
                // 기존 SetActiveCommander 함수 사용
                GamePlayerStats.Instance.SetActiveCommander(selectedCommander);
                
                // 지휘관별 초기 스탯 조정 적용
                if (selectedCommander.initialStatAdjustments.Count > 0)
                {
                    GamePlayerStats.Instance.ApplyChanges(selectedCommander.initialStatAdjustments);
                }
            }
        }
        
        // 2회차 이상을 위한 게임 상태 초기화
        ResetAllGameData();
        ResetParameterDataToDefaults();
        EventManager.Instance.ResetEventManagerState();
        
        // 챕터와 이벤트 상태 초기화
        playerData.currentChapter = 1;
        playerData.completedEventIds.Clear();
        playerData.eventPlaylistIndex = 0;
        playerData.currentPlaylist.Clear();
        
        // 저장 억제를 해제한 뒤 사이클을 구성하여 SaveLocal이 억제되지 않도록 순서 수정
        DataManager.Instance.AllowSavesFromNow();
        EventManager.Instance.StartNewCycle();
        playerData.currentGameState = GameState.InEventCycle;
        
        // 튜토리얼 억제 (2회차 이상이므로)
        suppressTutorialOnce = true;
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        if (parameterTutorialPanel != null) parameterTutorialPanel.SetActive(false);
        if (uiFlowSimulator != null) uiFlowSimulator.MarkParameterTutorialShown();
        
        // 업적 체크 (회차 업적)
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.CheckPlaythroughAchievements(playerData.playthroughCount);
        }
        
        // 2회차 이상 이어하기의 경우 오프닝 컷씬을 건너뛰고 바로 메인 스토리로 진입
        if (playerData.playthroughCount >= 2)
        {
            // 메인 스토리부터 시작
            ChangeState(GameState.InStory);
        }
        else
        {
            // 1회차는 기존대로 오프닝 컷씬을 재생
            ChangeState(GameState.PlayingOpeningCutscene);
        }
    }


    
    public void OpenShop()
    {
        shopUI.OpenShop();
    }

    private CommanderInfo GetCommanderByTrait(CommanderTrait trait)
    {
        switch (trait)
        {
            case CommanderTrait.Devost:
                return Commander1Button?.GetComponent<CommanderInfo>();
            case CommanderTrait.Wille:
                return Commander2Button?.GetComponent<CommanderInfo>();
            case CommanderTrait.Risard:
                return Commander3Button?.GetComponent<CommanderInfo>();
            default:
                return Commander1Button?.GetComponent<CommanderInfo>(); // 기본값
        }
    }
    private CutsceneData GetOpeningCutsceneForActiveCommander()
    {
        // 활성 지휘관 특성에 따라 오프닝 컷씬 선택. 비어있으면 공통 SO로 폴백
        var trait = DataManager.Instance?.PlayerData != null ? DataManager.Instance.PlayerData.activeTrait : CommanderTrait.Devost;
        switch (trait)
        {
            case CommanderTrait.Devost:
                return openingCutscene_Devost != null ? openingCutscene_Devost : chapter1OpeningCutscene;
            case CommanderTrait.Wille:
                return openingCutscene_Wille != null ? openingCutscene_Wille : chapter1OpeningCutscene;
            case CommanderTrait.Risard:
                return openingCutscene_Risard != null ? openingCutscene_Risard : chapter1OpeningCutscene;
            default:
                return chapter1OpeningCutscene;
        }
    }
}