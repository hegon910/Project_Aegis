using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

[RequireComponent(typeof(Button))]
public class CommanderInfo : MonoBehaviour
{
    [Header("UI References")]
    public GameObject commanderNameObject;
    public GameObject commanderDescriptionObject;

    [Header("Commander Data for Popup")]
    [TextArea(5, 10)] // Inspector에서 여러 줄 텍스트를 편하게 입력하도록 도와줍니다.
    public string characterStory; // 팝업에 띄울 심도있는 스토리 텍스트

    [Header("Commander Trait for Gameplay")]
//    public CommanderTrait trait;

    [Header("Fade Settings")]
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float fadeOutDuration = 0.1f;

    private CommanderCarouselController carouselController;
    private CanvasGroup nameCanvasGroup;
    private CanvasGroup descriptionCanvasGroup;

    private void Awake()
    {
        // 각 텍스트 오브젝트의 CanvasGroup 컴포넌트를 찾아서 할당 (없으면 자동 추가)
        if (commanderNameObject != null)
        {
            nameCanvasGroup = commanderNameObject.GetComponent<CanvasGroup>();
            if (nameCanvasGroup == null) nameCanvasGroup = commanderNameObject.AddComponent<CanvasGroup>();
        }
        if (commanderDescriptionObject != null)
        {
            descriptionCanvasGroup = commanderDescriptionObject.GetComponent<CanvasGroup>();
            if (descriptionCanvasGroup == null) descriptionCanvasGroup = commanderDescriptionObject.AddComponent<CanvasGroup>();
        }
    }

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnButtonClicked);

        // 초기 상태: 투명하고 비활성화된 상태
        if (nameCanvasGroup != null) nameCanvasGroup.alpha = 0f;
        if (descriptionCanvasGroup != null) descriptionCanvasGroup.alpha = 0f;

        if (commanderNameObject != null) commanderNameObject.SetActive(false);
        if (commanderDescriptionObject != null) commanderDescriptionObject.SetActive(false);
        
    }

    public void Setup(CommanderCarouselController controller)
    {
        carouselController = controller;
    }

    public void OnButtonClicked()
    {
        if (carouselController != null)
        {
            carouselController.OnCommanderClicked(this.transform);
        }
    }

    public void ShowInfo()
    {
        // 이름 텍스트 페이드인
        if (commanderNameObject != null && nameCanvasGroup != null)
        {
            // [핵심 수정] DOFade를 실행하기 전에 반드시 게임 오브젝트를 활성화합니다.
            commanderNameObject.SetActive(true);
            nameCanvasGroup.DOFade(1f, fadeInDuration);
        }
        // 설명 텍스트 페이드인
        if (commanderDescriptionObject != null && descriptionCanvasGroup != null)
        {
            // [핵심 수정] 설명 오브젝트도 동일하게 처리합니다.
            commanderDescriptionObject.SetActive(true);
            descriptionCanvasGroup.DOFade(1f, fadeInDuration);
        }
    }

    public void HideInfo()
    {
        // 이름 텍스트 페이드아웃
        if (commanderNameObject != null && nameCanvasGroup != null && commanderNameObject.activeSelf)
        {
            // [핵심 수정] 페이드아웃이 끝난 후에 게임 오브젝트를 비활성화합니다.
            nameCanvasGroup.DOFade(0f, fadeOutDuration)
                           .OnComplete(() => commanderNameObject.SetActive(false));
        }
        // 설명 텍스트 페이드아웃
        if (commanderDescriptionObject != null && descriptionCanvasGroup != null && commanderDescriptionObject.activeSelf)
        {
            // [핵심 수정] 설명 오브젝트도 동일하게 처리합니다.
            descriptionCanvasGroup.DOFade(0f, fadeOutDuration)
                                  .OnComplete(() => commanderDescriptionObject.SetActive(false));
        }
    }

    public void OnCardClicked()
    {
        if (carouselController != null)
        {
            carouselController.OnCommanderClicked(this.transform);
        }
    }

    // '스토리 버튼'이 클릭되었을 때 (팝업 호출)
}