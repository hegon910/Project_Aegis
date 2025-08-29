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

    // [제거] PlayerStats의 이벤트 구독/해제 로직은 더 이상 필요 없으므로 OnEnable/OnDisable 제거
    // private void OnEnable() ...
    // private void OnDisable() ...

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
        PlayGamesPlatform.Instance.Authenticate(OnAuthenticated);
        menuPanel.SetActive(true);
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

    public void OnCommanderSelected(int commanderIndex)
    {
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
                battleResultText.text = "승리";
            }
            else if (battleResult.Contains("패배"))
            {
                battleResultText.text = "패배";
            }
            else
            {
                battleResultText.text = "무승부";
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
}