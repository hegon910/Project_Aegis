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


    /// <summary>
    /// 게임오버 조건
    /// 1. ParameterType 정치력이 0 이하
    /// 2. ParameterType 병력이 0 이하
    /// 3. ParameterType 물자가 0 이하
    /// 4. ParameterType 리더십이 0 이하
    /// 5. 또는 특수한 조건시 게임오버
    /// 게임오버시 처리할 사항들
    /// 1. 게임오버 UI 표시
    /// 2. UI외 조작을 막음 (게임오버 상태로 구현?)
    /// 3. 게임오버 사운드 재생
    /// 4. 게임오버 UI에서
    /// - 당신은 게임오버 되었습니다. 라는 메시지 표시 -> 메시지 필요한가?
    /// - 게임 재시작 버튼과 메인 메뉴 버튼 표시 
    /// - 게임 재시작 버튼 클릭 시 현재 진행상황을 초기화 하고 진행중이던 장의 처음부터 다시시작 -> 클릭 필요한가? 
    /// - 타이틀 메뉴 버튼 클릭 시 타이틀 메뉴로 이동 -> 타이틀로 이동이 필요한가?
    

}
