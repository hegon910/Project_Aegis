using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEngine.SocialPlatforms;
using System.Threading.Tasks;

public class GameManager : MonoBehaviour
{
    public static GameManager instance {get; private set;}

    [Header("UI 참조")]
    [SerializeField] private GameObject titlePanel;
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject overwriteWarningPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject mainGameCanvas;
    [SerializeField] private GameObject commanderSelectionCanvas;
    [SerializeField] private Button testPlayerButton;

    [Header("지휘관 선택")]
    [SerializeField] private Button Commander1Button;
    [SerializeField] private Button Commander2Button;
    [SerializeField] private Button Commander3Button;


    private bool hasSaveDate = false; // 저장된 데이터가 있는지 여부(임시)

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

    private void InitializeGame()
    {
        //플레이어 파라미터 초기화


        //필요한 패널만 활성화
        titlePanel.SetActive(true);
        loginPanel.SetActive(false);
        menuPanel.SetActive(false);
//        overwriteWarningPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        mainGameCanvas.SetActive(false);
        commanderSelectionCanvas.SetActive(false);

        // 저장데이터 유무를 확인 저장관련 매니저에서 확인
        // hasSaveDate = TODO
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
        // 선택된 지휘관에 따라 게임 시작
        commanderSelectionCanvas.SetActive(false);
        mainGameCanvas.SetActive(true);
    }


    private void Start()
    {
        // 게임 시작 시 필요한 초기화 작업을 여기에 추가할 수 있습니다.
        Debug.Log("====게임 매니저가 초기화=====");
        InitializeGame();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            TestGameOverConditions();
            Debug.Log($"{PlayerStats.Instance.GetStat(ParameterType.정치력)}");
            Debug.Log($"{PlayerStats.Instance.GetStat(ParameterType.병력)}");
            Debug.Log($"{PlayerStats.Instance.GetStat(ParameterType.물자)}");
            Debug.Log($"{PlayerStats.Instance.GetStat(ParameterType.리더십)}");
        }

        //CheckGameOverConditions();

        // 게임 상태 업데이트 로직을 여기에 추가할 수 있습니다.
        // 예: 게임 오버 조건 체크, UI 업데이트 등
    }

    /// <summary>
    /// 게임오버 조건
    /// 1. ParameterType 정치력이 0 이하
    /// 2. ParameterType 병력이 0 이하
    /// 3. ParameterType 물자가 0 이하
    /// 4. ParameterType 리더십이 0 이하 
    /// 5. 또는 특수한 조건시 게임오버 (전투 패배시 죽는건지 확인 필요?)
    /// 게임오버시 처리할 사항들
    /// 1. 게임오버 UI 표시
    /// 2. UI외 조작을 막음 (게임오버 상태로 구현?)
    /// 3. 게임오버 사운드 재생
    /// 4. 게임오버 UI에서 -> UI가 필요한가? 오버 시 자동으로 재시작?
    /// - 당신은 게임오버 되었습니다. 라는 메시지 표시 -> 메시지 필요한가?
    /// - 게임 재시작 버튼과 메인 메뉴 버튼 표시 -> 버튼이 필요한가?
    /// - 게임 재시작 버튼 클릭 시 현재 진행상황을 초기화 하고 진행중이던 회차의 장 처음부터 다시시작 -> 클릭 필요한가? 
    /// - 타이틀 메뉴 버튼 클릭 시 타이틀 메뉴로 이동 -> 타이틀로 이동이 필요한가?
    /// 
    public void CheckGameOverConditions()
    {
        // 게임 오버 조건을 확인하고, 조건이 충족되면 게임 오버 처리
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
        if (PlayerStats.Instance.GetStat(ParameterType.정치력) <= 0 ||
            PlayerStats.Instance.GetStat(ParameterType.병력) <= 0 ||
            PlayerStats.Instance.GetStat(ParameterType.물자) <= 0 ||
            PlayerStats.Instance.GetStat(ParameterType.리더십) <= 0)
        {
            HandleGameOver();
        }
        // 게임 오버 처리 로직을 여기에 추가합니다.
        Debug.Log("게임 오버! 모든 능력치가 0 이하입니다.");
        // 게임 오버 UI 표시, 조작 막기, 사운드 재생 등을 구현할 수 있습니다.
    }
    

    private void HandleGameOver()
    {
        gameOverPanel.SetActive(true);
        // UI 매니저를 통해 게임 오버 화면을 표시하는 로직을 추가할 수 있습니다.
        Debug.Log("게임 오버 UI를 표시합니다.");

        // 예시: UIManager.Instance.ShowGameOverUI();

        // 조작 막기
        Time.timeScale = 0; // 게임 일시 정지

        // 게임 오버 사운드 재생
        // AudioManager.Instance.PlayGameOverSound();

        // 게임 오버 메시지 표시 (필요한 경우)
        // Debug.Log("당신은 게임오버 되었습니다.");

        // 
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

   
    public void ExitGame()
    {
        Application.Quit();
    }
}
