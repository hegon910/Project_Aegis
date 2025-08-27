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
    // [추가] UIFlowSimulator를 직접 제어하기 위한 참조
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

    private bool hasSaveDate = false; // 저장된 데이터가 있는지 여부(임시)

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

    // [추가] 스탯 변경 이벤트를 구독/해제하여 게임오버를 자동으로 체크
    private void OnEnable()
    {
        PlayerStats.OnStatChanged += OnStatChanged_CheckGameOver;
    }

    private void OnDisable()
    {
        PlayerStats.OnStatChanged -= OnStatChanged_CheckGameOver;
    }

    private void Start()
    {
        Debug.Log("====게임 매니저가 초기화=====");
        InitializeGame();
    }

    private void InitializeGame()
    {
        //플레이어 파라미터 초기화
        PlayerStats.Instance.InitializeStats();

        //필요한 패널만 활성화
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
            // TODO : 저장데이터 삭제 
            StartNewGame();
        }
    }

    public void StartNewGame()
    {
        ResetAllGameData();
       // if (PlayerStats.Instance != null)
       // {
       //     PlayerStats.Instance.InitializeStats();
       // }
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
        onConfirmAction?.Invoke(); // 저장해둔 행동 실행
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);
        }
        onConfirmAction = null; // 실행 후 초기화
    }

    /// <summary>
    /// [신규] 확인 창의 '아니오' 버튼에 연결될 메서드입니다.
    /// </summary>
    public void OnCancel()
    {
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);
        }
        onConfirmAction = null; // 행동 취소 후 초기화
    }
    public void OnCommanderSelected(int commanderIndex)
    {
        commanderSelectionCanvas.SetActive(false);
        mainGameCanvas.SetActive(true);
        InGameUIPanel.SetActive(true);
        // [추가] 지휘관 선택 후 UIFlowSimulator에게 이벤트 흐름 시작을 명령
        if (uiFlowSimulator != null)
        {
            uiFlowSimulator.BeginFlow();
        }
        else
        {
            Debug.LogError("UIFlowSimulator가 GameManager에 할당되지 않았습니다!");
        }
    }

    // [수정] 아직 구현되지 않은 패널에 대한 Null 예외 처리 추가
    public void GoToStoryPanel()
    {
        // ▼ [핵심 수정] MainScenarioManager가 이미 실행 중인지 확인하는 보호 코드 추가
        if (mainScenarioManager != null && mainScenarioManager.IsScenarioRunning)
        {
            // 이미 시나리오가 진행 중이라면, 중복 실행을 막고 아무것도 하지 않습니다.
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
           if(battleTurnManager != null)
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
            GoToBattleResultPanel("전투 패널 없음"); // 전투가 없으면 바로 결과로
        }
    }
    private void HandleBattleEnd(string resultLog)
    {
        // 결과 패널로 이동하면서 전달받은 결과 로그를 넘겨줍니다.
        GoToBattleResultPanel(resultLog);

        // 이벤트 중복 호출을 막기 위해 등록을 해제합니다.
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

            // 전달받은 문자열에 특정 키워드가 포함되어 있는지 확인하여 결과를 표시합니다.
            if (battleResult.Contains("승리"))
            {
                battleResultText.text = "승리";
            }
            else if (battleResult.Contains("패배"))
            {
                battleResultText.text = "패배";
            }
            else // "무승부" 또는 그 외의 모든 경우
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
      //  PlayerStats.Instance.InitializeStats();

        if (battleResultPanel != null) battleResultPanel.SetActive(false);
        mainGameCanvas.SetActive(true);

        // [추가] 한 챕터가 끝나고 다시 이벤트 흐름 시작
        if (uiFlowSimulator != null)
        {
            uiFlowSimulator.BeginFlow();
        }
    }

    // [추가] 스탯이 변경될 때마다 자동으로 호출될 함수
    private void OnStatChanged_CheckGameOver(ParameterType type, int change, int currentValue)
    {
        Debug.Log("====게임 매니저가 초기화=====");
        InitializeGame();
        

#if UNITY_EDITOR
        if (forceGameOverButton != null)
        {
            forceGameOverButton.gameObject.SetActive(true);
            forceGameOverButton.onClick.AddListener(OnForceGameOverButtonClicked); //** 나중에 리무브리스너******
        }
#endif
    }

#if UNITY_EDITOR
    public void OnForceGameOverButtonClicked()
    {
        ForceGameOver();
    }

    public void ForceGameOver()
    {

        // GameManager에서 직접 파라미터 변화 리스트를 생성하여 PlayerStats에 전달
        List<ParameterChange> changes = new List<ParameterChange>
        {
            // 모든 파라미터에 충분히 큰 음수 값을 적용
            new ParameterChange { parameterType = ParameterType.정치력, valueChange = -100 },
            new ParameterChange { parameterType = ParameterType.병력, valueChange = -100 },
            new ParameterChange { parameterType = ParameterType.물자, valueChange = -100 },
            new ParameterChange { parameterType = ParameterType.리더십, valueChange = -100 }
        };

        Debug.Log("<color=red>디버그: GameManager에서 파라미터 변경을 직접 호출하여 게임오버 유발.</color>");

        // PlayerStats의 ApplyChanges 메소드를 호출하여 파라미터 변경
        PlayerStats.Instance.ApplyChanges(changes);
    }
#endif

    public void OnParameterChanged()
    {
        CheckGameOverConditions();
    }

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

        // 조작 막기
        

        // 게임 오버 사운드 재생
        // AudioManager.Instance.PlayGameOverSound();

        // 게임 오버 메시지 표시 (필요한 경우)
        // Debug.Log("당신은 게임오버 되었습니다.");
    }


    public void OnGameOverPanelTouched()
    {
        Debug.Log("게임을 재시작합니다.");

        // 현재 진행상황 초기화 및 장 처음부터 다시 시작


        gameOverPanel.SetActive(false);
        Debug.Log("게임오버 패널 비활성화");

        PlayerStats.Instance.InitializeStats();
        
        Debug.Log("플레이어 파라미터 초기화");


        Debug.Log(PlayerStats.Instance.GetStat(ParameterType.정치력));
        Debug.Log(PlayerStats.Instance.GetStat(ParameterType.병력));
        Debug.Log(PlayerStats.Instance.GetStat(ParameterType.물자));
        Debug.Log(PlayerStats.Instance.GetStat(ParameterType.리더십));
        
        //TODO 이벤트매니저에 다시 이벤트풀 초기화?
        //TODO 전투이벤트도 초기화 할게 있으면?
        mainGameCanvas.SetActive(true);
    }


    public void ShowOptionsPanel()
    {
        if (optionCanvas != null)
        {
            optionCanvas.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Option Canvas가 Inspector에 할당되지 않았습니다!");
        }
    }

    public void HideOptionsPanel()
    {
        if (optionCanvas != null)
        {
            optionCanvas.SetActive(false);
        }
    }


    private void TestGameOverConditions()
    {
        // 테스트용으로 게임 오버 조건을 강제로 발생시킵니다.
        PlayerStats.Instance.ApplyChanges(new List<ParameterChange>
        {
            new ParameterChange { parameterType = ParameterType.정치력, valueChange = -100 },
            new ParameterChange { parameterType = ParameterType.병력, valueChange = -100 },
            new ParameterChange { parameterType = ParameterType.물자, valueChange = -100 },
            new ParameterChange { parameterType = ParameterType.리더십, valueChange = -100 }
        });
    }

    /// 게임엔딩
    /// 엔딩조건을 확인하여 
    /// 관련 매니저에게 전달

    /// 게임 저장
    /// 기능구현

    /// 게임 불러오기
    /// 기능구현

    public void ReturnToTitle()
    {
        Debug.Log("QA 버튼: 타이틀 화면으로 돌아갑니다.");
        Time.timeScale = 1;

        if (mainGameCanvas != null) mainGameCanvas.SetActive(false);
        if (commanderSelectionCanvas != null) commanderSelectionCanvas.SetActive(false);
        if (storyPanel != null) storyPanel.SetActive(false);
        if (battlePanel != null) battlePanel.SetActive(false);
        if (battleResultPanel != null) battleResultPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (menuPanel != null) menuPanel.SetActive(false);
        if (InGameUIPanel != null) InGameUIPanel.SetActive(false);

        if (titleCanvas != null) titleCanvas.SetActive(true);
        if (titlePanel != null) titlePanel.SetActive(true);

        //  모든 데이터 초기화 호출 시점 변경 (기존 PlayerStats 초기화는 ResetAllGameData 내부로 이동)
        ResetAllGameData();
    }


    private void ResetAllGameData()
    {
        Debug.Log("==== 모든 게임 데이터 초기화를 시작합니다 ====");

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.InitializeStats();
        }
        else
        {
            Debug.LogWarning("PlayerStats 인스턴스를 찾을 수 없어 초기화에 실패했습니다.");
        }

        if (EventManager.Instance != null)
        {
            EventManager.Instance.ResetEventManagerState();
        }
        else
        {
            Debug.LogWarning("EventManager 인스턴스를 찾을 수 없어 초기화에 실패했습니다.");
        }

        // 전투 관리자 초기화
        if (battleTurnManager != null)
        {
            battleTurnManager.ResetForNewBattle();
        }
        else
        {
            Debug.LogWarning("BattleTurnManager 인스턴스를 찾을 수 없어 초기화에 실패했습니다.");
        }

        // 시나리오 관리자 초기화
        if (mainScenarioManager != null)
        {
            // ▼ [디버깅 로그] GameManager가 시나리오 리셋을 요청하는지 확인합니다.
            Debug.Log("[GM] Requesting MainScenarioManager to reset state...");
            mainScenarioManager.ResetScenarioState();
        }
        else
        {
            Debug.LogWarning("MainScenarioManager 인스턴스를 찾을 수 없어 초기화에 실패했습니다.");
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
