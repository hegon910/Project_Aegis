using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
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


    

    ///
    /// </summary>
    /// 타이틀화면에서 아무데나 터치하면 시작 -> 바로 메뉴화면으로
    /// 로그인 및 회원가입 화면  -> 회원가입창 필요 (데모에선 생략)
    /// 메뉴화면에서 테스트플레이어 클릭 시 메뉴화면(가칭) '새 게임', '이어하기' '엔딩기록' '업적' '옵션' '종료' 버튼  
    /// 새 게임 시 지휘관 선택 창
    /// 지휘관 선택 창에서 지휘관 선택 후 '시작' 버튼 클릭 시 게임 시작

    private void Start()
    {
        // 게임 시작 시 필요한 초기화 작업을 여기에 추가할 수 있습니다.
        Debug.Log("게임 매니저가 초기화되었습니다.");
    }

    private void Update()
    {
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
        // 게임 오버 UI 표시
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

   
    

}
