using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 업적 UI와 게임플로우를 연결하는 헬퍼 스크립트
/// 버튼 클릭 이벤트나 다른 시스템에서 쉽게 업적 UI를 열 수 있도록 도와줍니다.
/// </summary>
public class AchievementUIHelper : MonoBehaviour
{
    [Header("업적 UI 연결")]
    [SerializeField] private Button achievementButton;
    [SerializeField] private AchievementType defaultType = AchievementType.Ending;
    
    [Header("키보드 단축키 (선택사항)")]
    [SerializeField] private KeyCode toggleKey = KeyCode.A;
    [SerializeField] private bool enableKeyboardShortcut = true;
    
    private void Start()
    {
        // 업적 버튼 이벤트 연결
        if (achievementButton != null)
        {
            achievementButton.onClick.AddListener(OpenAchievementUI);
        }
    }
    
    private void Update()
    {
        // 키보드 단축키 처리
        if (enableKeyboardShortcut && Input.GetKeyDown(toggleKey))
        {
            ToggleAchievementUI();
        }
    }
    
    /// <summary>
    /// 기본 업적 UI 열기
    /// </summary>
    public void OpenAchievementUI()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.OpenAchievementUI();
        }
        else
        {
            Debug.LogWarning("[AchievementUIHelper] GameManager 인스턴스를 찾을 수 없습니다.");
        }
    }
    
    /// <summary>
    /// 특정 타입의 업적 UI 열기
    /// </summary>
    public void OpenAchievementUI(AchievementType type)
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.OpenAchievementUI(type);
        }
        else
        {
            Debug.LogWarning("[AchievementUIHelper] GameManager 인스턴스를 찾을 수 없습니다.");
        }
    }
    
    /// <summary>
    /// 업적 UI 토글
    /// </summary>
    public void ToggleAchievementUI()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.ToggleAchievementUI();
        }
        else
        {
            Debug.LogWarning("[AchievementUIHelper] GameManager 인스턴스를 찾을 수 없습니다.");
        }
    }
    
    /// <summary>
    /// 업적 UI 닫기
    /// </summary>
    public void CloseAchievementUI()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.CloseAchievementUI();
        }
    }
    
    /// <summary>
    /// 엔딩 업적 UI 열기
    /// </summary>
    public void OpenEndingAchievements()
    {
        OpenAchievementUI(AchievementType.Ending);
    }
    
    /// <summary>
    /// 회차 업적 UI 열기
    /// </summary>
    public void OpenPlaythroughAchievements()
    {
        OpenAchievementUI(AchievementType.Playthrough);
    }
    
    /// <summary>
    /// 전투 업적 UI 열기
    /// </summary>
    public void OpenBattleAchievements()
    {
        OpenAchievementUI(AchievementType.Battle);
    }
    
    /// <summary>
    /// 이벤트 업적 UI 열기
    /// </summary>
    public void OpenEventAchievements()
    {
        OpenAchievementUI(AchievementType.Event);
    }
    
    /// <summary>
    /// 스토리 업적 UI 열기
    /// </summary>
    public void OpenStoryAchievements()
    {
        OpenAchievementUI(AchievementType.Story);
    }
    
    /// <summary>
    /// 수집 업적 UI 열기
    /// </summary>
    public void OpenCollectionAchievements()
    {
        OpenAchievementUI(AchievementType.Collection);
    }
}

