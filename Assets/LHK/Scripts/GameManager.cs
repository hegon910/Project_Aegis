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
        PlayerStats.Instance.InitializeStats();

        //필요한 패널만 활성화
        titlePanel.SetActive(true);
        loginPanel.SetActive(false);
        menuPanel.SetActive(false);
        //overwriteWarningPanel.SetActive(false);
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
        commanderSelectionCanvas.SetActive(false);
        mainGameCanvas.SetActive(true);
    }

    public void GoToStoryPanel() //uiflow에서 호출필요
    {
        mainGameCanvas.SetActive(false);
        storyPanel.SetActive(true);
    }

    public void GoToBattlePanel() //스토리 끝나면 호출
    {
        storyPanel.SetActive(false);
        battlePanel.SetActive(true);
    }

    public void GoToBattleResultPanel() //전투끝나면 호출
    {
        battlePanel.SetActive(false);
        battleResultPanel.SetActive(true);

        //1~5장일 경우

        //6장일 경우 엔딩
        //TODO 엔딩이벤트패널, 엔딩시스템...없음
    }

    public void ReturnToMainGameCanvas() //전투 결과 끝나면
    {
        //플레이어 파라미터 초기화
        PlayerStats.Instance.InitializeStats();
        //TODO 이벤트매니저에 다시 이벤트풀 초기화?
        //TODO 전투이벤트도 초기화 할게 있으면?

        battleResultPanel.SetActive(false);
        mainGameCanvas.SetActive(true);
    }


    private void Start()
    {
        Debug.Log("====게임 매니저가 초기화=====");
        InitializeGame();
    }

    public void OnParameterChanged()
    {
        CheckGameOverConditions();
    }

    public void CheckGameOverConditions()
    {
        // 게임 오버 조건을 확인하고, 조건이 충족되면 게임 오버 처리
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
        Time.timeScale = 0; // 게임 일시 정지

        // 게임 오버 사운드 재생
        // AudioManager.Instance.PlayGameOverSound();

        // 게임 오버 메시지 표시 (필요한 경우)
        // Debug.Log("당신은 게임오버 되었습니다.");
    }

    public void OnGameOverPanelTouched()
    {
        Debug.Log("게임을 재시작합니다.");

        // 현재 진행상황 초기화 및 장 처음부터 다시 시작
        Time.timeScale = 1; // 게임 재개
        gameOverPanel.SetActive(false);
        PlayerStats.Instance.InitializeStats();
        //TODO 이벤트매니저에 다시 이벤트풀 초기화?
        //TODO 전투이벤트도 초기화 할게 있으면?
        mainGameCanvas.SetActive(true);
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
