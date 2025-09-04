// StoryPopupController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StoryPopupController : MonoBehaviour
{
    // 어디서든 쉽게 접근할 수 있도록 싱글톤 인스턴스 설정
    public static StoryPopupController Instance { get; private set; }

    [SerializeField] private GameObject popupPanel;
    [SerializeField] private TMP_Text storyText;
    [SerializeField] private Button closeButton;

    [Header("Carousel Reference")]
    [SerializeField] private CommanderCarouselController carouselController;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // 닫기 버튼에 HidePopup 함수를 미리 연결해둡니다.
            closeButton.onClick.AddListener(HidePopup);
            // 팝업은 시작할 때 꺼져있도록 확실히 합니다.
            popupPanel.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
        popupPanel.SetActive(false);
    }

    /// <summary>
    /// 외부(CommanderInfo)에서 이 함수를 호출하여 팝업을 띄웁니다.
    /// </summary>
    /// <param name="sprite">표시할 캐릭터 이미지</param>
    /// <param name="story">표시할 스토리 텍스트</param>
    public void ShowPopup(string story)
    {
        if (storyText != null) storyText.text = story;
        popupPanel.SetActive(true);

    }

    /// <summary>
    /// 팝업을 닫습니다. (닫기 버튼에 의해 호출됨)
    /// </summary>
    public void HidePopup()
    {
        popupPanel.SetActive(false);
        if (carouselController != null)
        {
            carouselController.enabled = true;
        }
    }

    public void ShowCenterCommanderStory()
    {
        if (carouselController == null)
        {
            Debug.LogError("Carousel Controller가 연결되지 않았습니다!");
            return;
        }
        carouselController.enabled = false;
        // 1. 캐러셀 컨트롤러로부터 현재 중앙 인덱스를 가져옵니다.
        int centerIndex = carouselController.centerIndex;

        // 2. 해당 인덱스를 사용하여 중앙에 있는 CommanderInfo를 찾습니다.
        CommanderInfo centerCommander = carouselController.commanderInfos[centerIndex];

        // 3. 찾은 지휘관의 정보(스토리 이미지, 텍스트)를 사용하여 팝업을 띄웁니다.
        ShowPopup(centerCommander.characterStory);
    }
}
