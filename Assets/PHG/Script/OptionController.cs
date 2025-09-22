using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class OptionController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button resetDataButton;
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TextMeshProUGUI tipText;
    [SerializeField] private TextMeshProUGUI bgmVolumeText;
    [SerializeField] private TextMeshProUGUI sfxVolumeText;
    
    [Header("Tip Messages")]
    [SerializeField] private List<string> tipMessages = new List<string>
    {
        "지휘관의 특성은 전략에 큰 변화를 줍니다.",
        "파라미터는 매 선택마다 변동됩니다.",
        "업적을 달성하면 특별한 보상을 획득할 수 있습니다.",
        "엔딩은 당신의 선택에 따라 달라집니다.",
        "자원을 균형 있게 관리하는 것이 중요합니다.",
        "루프마다 새로운 전략을 시도해 보세요!",
        "실패는 성공의 어머니입니다.",
        "지휘관마다 시작 조건이 다릅니다.",
        "선택은 즉각적으로 결과를 만듭니다.",
        "카르마는 눈에 보이지 않지만 강력한 힘입니다.",
        "전세는 매 장마다 새롭게 시작됩니다.",
        "때로는 손해를 감수해야 승리할 수 있습니다.",
        "업적은 새로운 길을 여는 열쇠입니다.",
        "짧은 플레이도 의미 있는 발자취가 됩니다.",
        "당신의 선택이 역사를 창조합니다...",
        "어둠 속에서도 희망은 존재합니다.",
        "길을 잃는 것도 여행의 일부입니다.",
        "작은 결정이 큰 변화를 만듭니다.",
        "끝은 또 다른 시작일 뿐입니다.",
        "미래는 당신의 손에 달려 있습니다."
    };
    
    [Header("Tip Settings")]
    [SerializeField] private float tipDisplayInterval = 3f;
    [SerializeField] private float tipFadeInDuration = 0.5f;
    [SerializeField] private float tipFadeOutDuration = 0.5f;
    
    private Coroutine tipCoroutine;
    
    private void Awake()
    {
        // tipMessages가 null이면 초기화
        if (tipMessages == null)
        {
            tipMessages = new List<string>();
        }
        
        // 기본 팁 메시지가 없으면 추가
        if (tipMessages.Count == 0)
        {
            tipMessages.AddRange(new List<string>
            {
                "지휘관의 특성은 전략에 큰 변화를 줍니다.",
                "파라미터는 매 선택마다 변동됩니다.",
                "업적을 달성하면 특별한 보상을 획득할 수 있습니다.",
                "엔딩은 당신의 선택에 따라 달라집니다.",
                "자원을 균형 있게 관리하는 것이 중요합니다.",
                "루프마다 새로운 전략을 시도해 보세요!",
                "실패는 성공의 어머니입니다.",
                "지휘관마다 시작 조건이 다릅니다.",
                "선택은 즉각적으로 결과를 만듭니다.",
                "카르마는 눈에 보이지 않지만 강력한 힘입니다.",
                "전세는 매 장마다 새롭게 시작됩니다.",
                "때로는 손해를 감수해야 승리할 수 있습니다.",
                "업적은 새로운 길을 여는 열쇠입니다.",
                "짧은 플레이도 의미 있는 발자취가 됩니다.",
                "당신의 선택이 역사를 창조합니다...",
                "어둠 속에서도 희망은 존재합니다.",
                "길을 잃는 것도 여행의 일부입니다.",
                "작은 결정이 큰 변화를 만듭니다.",
                "끝은 또 다른 시작일 뿐입니다.",
                "미래는 당신의 손에 달려 있습니다."
            });
        }
    }
    
    private void Start()
    {
        InitializeUI();
        StartTipDisplay();
    }
    
    private void OnEnable()
    {
        // GameManager의 상태 변경 이벤트 구독
        GameManager.OnGameStateChanged += OnGameStateChanged;
        if (GameManager.instance != null)
        {
            UpdateButtonStates();
        }
        
        // 팁 표시 시작 (OnEnable에서도 시작하여 GameObject가 다시 활성화될 때도 작동)
        StartTipDisplay();
    }
    
    private void OnDisable()
    {
        // 이벤트 구독 해제
        GameManager.OnGameStateChanged -= OnGameStateChanged;
        StopTipDisplay();
    }
    
    private void OnGameStateChanged(GameState newState)
    {
        UpdateButtonStates();
    }
    
    private void InitializeUI()
    {
        // 슬라이더 초기값 설정
        if (DataManager.Instance?.PlayerData?.settings != null)
        {
            var settings = DataManager.Instance.PlayerData.settings;
            bgmVolumeSlider.value = settings.bgmVolume;
            sfxVolumeSlider.value = settings.sfxVolume;
        }
        else
        {
            bgmVolumeSlider.value = 1f;
            sfxVolumeSlider.value = 1f;
        }
        
        // 슬라이더 이벤트 연결
        bgmVolumeSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        
        // 초기 텍스트 업데이트
        UpdateVolumeTexts();
        UpdateButtonStates();
    }
    
    private void UpdateButtonStates()
    {
        if (GameManager.instance == null) return;
        
        GameState currentState = GameManager.instance.currentGameState;
        
        // MainMenu 상태일 때 MainMenu 버튼 비활성화
        if (mainMenuButton != null)
        {
            mainMenuButton.gameObject.SetActive(currentState != GameState.MainMenu);
        }
        
        // InEventCycle 상태일 때 ResetData 버튼 비활성화
        if (resetDataButton != null)
        {
            resetDataButton.gameObject.SetActive(currentState != GameState.InEventCycle);
        }
    }
    
    private void OnBGMVolumeChanged(float value)
    {
        if (DataManager.Instance?.PlayerData?.settings != null)
        {
            DataManager.Instance.PlayerData.settings.bgmVolume = value;
            DataManager.Instance.SaveLocal();
        }
        
        // 실제 오디오 볼륨 적용
        AudioManager.Instance?.SetBGMVolume(value);
        
        UpdateVolumeTexts();
    }
    
    private void OnSFXVolumeChanged(float value)
    {
        if (DataManager.Instance?.PlayerData?.settings != null)
        {
            DataManager.Instance.PlayerData.settings.sfxVolume = value;
            DataManager.Instance.SaveLocal();
        }
        
        // 실제 오디오 볼륨 적용
        AudioManager.Instance?.SetSFXVolume(value);
        
        UpdateVolumeTexts();
    }
    
    private void UpdateVolumeTexts()
    {
        if (bgmVolumeText != null)
        {
            bgmVolumeText.text = $"배경음: {(bgmVolumeSlider.value * 100):F0}%";
        }
        
        if (sfxVolumeText != null)
        {
            sfxVolumeText.text = $"효과음: {(sfxVolumeSlider.value * 100):F0}%";
        }
    }
    
    private void StartTipDisplay()
    {
        // 이미 실행 중이면 중복 실행 방지
        if (tipCoroutine != null)
        {
            return;
        }
        
        if (tipText == null)
        {
            return;
        }
        
        if (tipMessages == null || tipMessages.Count == 0)
        {
            return;
        }
        
        tipCoroutine = StartCoroutine(DisplayTipsCoroutine());
    }
    
    private void StopTipDisplay()
    {
        if (tipCoroutine != null)
        {
            StopCoroutine(tipCoroutine);
            tipCoroutine = null;
        }
    }
    
    private IEnumerator DisplayTipsCoroutine()
    {
        while (true)
        {
            if (tipMessages.Count > 0)
            {
                // 랜덤 팁 선택
                int randomIndex = Random.Range(0, tipMessages.Count);
                yield return StartCoroutine(ShowTip(tipMessages[randomIndex]));
            }
            else
            {
                yield return new WaitForSeconds(1f);
            }
        }
    }
    
    private IEnumerator ShowTip(string message)
    {
        if (tipText == null) yield break;
        
        tipText.text = message;
        
        // 페이드 인
        yield return StartCoroutine(FadeText(tipText, 0f, 1f, tipFadeInDuration));
        
        // 표시 시간 대기 (3초)
        yield return new WaitForSeconds(tipDisplayInterval);
        
        // 페이드 아웃
        yield return StartCoroutine(FadeText(tipText, 1f, 0f, tipFadeOutDuration));
    }
    
    private IEnumerator FadeText(TextMeshProUGUI text, float startAlpha, float endAlpha, float duration)
    {
        if (text == null) yield break;
        
        Color color = text.color;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            color.a = Mathf.Lerp(startAlpha, endAlpha, t);
            text.color = color;
            yield return null;
        }
        
        color.a = endAlpha;
        text.color = color;
    }
    
    // 외부에서 호출할 수 있는 메서드들
    public void RefreshButtonStates()
    {
        UpdateButtonStates();
    }
    
    public void SetTipDisplayEnabled(bool enabled)
    {
        if (enabled)
        {
            StartTipDisplay();
        }
        else
        {
            StopTipDisplay();
            if (tipText != null)
            {
                tipText.text = "";
            }
        }
    }
}
