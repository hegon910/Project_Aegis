using GooglePlayGames;
using GooglePlayGames.BasicApi;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager instance { get; private set; }

    [Header("UI 참조")]
    [SerializeField] private GameObject titlePanel;
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject overwriteWarningPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject mainGameCanvas;
    [SerializeField] private GameObject commanderSelectionCanvas;
    [SerializeField] private GameObject storyPanel;
    [SerializeField] private GameObject battlePanel;
    [SerializeField] private GameObject battleResultPanel;
    [SerializeField] private Button testPlayerButton;

    // [추가] UIFlowSimulator를 직접 제어하기 위한 참조
    [Header("코어 시스템 참조")]
    [SerializeField] private UIFlowSimulator uiFlowSimulator;

    [Header("지휘관 선택")]
    [SerializeField] private Button Commander1Button;
    [SerializeField] private Button Commander2Button;
    [SerializeField] private Button Commander3Button;

    private bool hasSaveDate = false;

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
        titlePanel.SetActive(true);
        loginPanel.SetActive(false);
        menuPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        mainGameCanvas.SetActive(false);
        commanderSelectionCanvas.SetActive(false);
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
        menuPanel.SetActive(false);
        commanderSelectionCanvas.SetActive(true);
    }

    public void OnCommanderSelected(int commanderIndex)
    {
        commanderSelectionCanvas.SetActive(false);
        mainGameCanvas.SetActive(true);

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
       // mainGameCanvas.SetActive(false);
        if (storyPanel != null)
        {
            storyPanel.SetActive(true);
            // TODO: MainScenarioManager 시작 로직 호출
            // MainScenarioManager.Instance.StartScenario(...);
        }
        else
        {
            Debug.LogWarning("Story Panel이 할당되지 않아 전투 페이즈로 바로 넘어갑니다.");
            GoToBattlePanel(); // 스토리가 없으면 바로 전투로
        }
    }

    public void GoToBattlePanel()
    {
        if (storyPanel != null) storyPanel.SetActive(false);
        if (battlePanel != null)
        {
            battlePanel.SetActive(true);
            // TODO: BattleManager 시작 로직 호출
        }
        else
        {
            Debug.LogWarning("Battle Panel이 할당되지 않아 결과 페이즈로 바로 넘어갑니다.");
            GoToBattleResultPanel(); // 전투가 없으면 바로 결과로
        }
    }

    public void GoToBattleResultPanel()
    {
        if (battlePanel != null) battlePanel.SetActive(false);
        if (battleResultPanel != null)
        {
            battleResultPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Battle Result Panel이 할당되지 않아 메인 캔버스로 바로 돌아갑니다.");
            ReturnToMainGameCanvas();
        }
    }

    public void ReturnToMainGameCanvas()
    {
        PlayerStats.Instance.InitializeStats();

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
        CheckGameOverConditions();
    }

    public void CheckGameOverConditions()
    {
        if (PlayerStats.Instance.GetStat(ParameterType.정치력) <= 0 ||
            PlayerStats.Instance.GetStat(ParameterType.병력) <= 0 ||
            PlayerStats.Instance.GetStat(ParameterType.물자) <= 0 ||
            PlayerStats.Instance.GetStat(ParameterType.리더십) <= 0)
        {
            HandleGameOver();
        }
    }

    // [수정] GameOver() 함수를 HandleGameOver()로 통합하여 중복 호출 방지
    private void HandleGameOver()
    {
        // 이미 게임오버 상태이면 중복 실행 방지
        if (gameOverPanel.activeSelf) return;

        Debug.Log("게임 오버!");
        gameOverPanel.SetActive(true);
        Time.timeScale = 0; // 게임 일시 정지
    }

    // ... 나머지 함수는 기존과 동일 ...

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


    public void ExitGame()
    {
        Application.Quit();
    }



}
