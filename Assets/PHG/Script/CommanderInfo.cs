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

    [Header("Fade Settings")]
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float fadeOutDuration = 0.1f;

    private CommanderCarouselController carouselController;
    private CanvasGroup nameCanvasGroup;
    private CanvasGroup descriptionCanvasGroup;

    private void Awake()
    {
        // �� �ؽ�Ʈ ������Ʈ�� CanvasGroup ������Ʈ�� ã�Ƽ� �Ҵ� (������ �ڵ� �߰�)
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

        // �ʱ� ����: �����ϰ� ��Ȱ��ȭ�� ����
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
        // �̸� �ؽ�Ʈ ���̵���
        if (commanderNameObject != null && nameCanvasGroup != null)
        {
            // [�ٽ� ����] DOFade�� �����ϱ� ���� �ݵ�� ���� ������Ʈ�� Ȱ��ȭ�մϴ�.
            commanderNameObject.SetActive(true);
            nameCanvasGroup.DOFade(1f, fadeInDuration);
        }
        // ���� �ؽ�Ʈ ���̵���
        if (commanderDescriptionObject != null && descriptionCanvasGroup != null)
        {
            // [�ٽ� ����] ���� ������Ʈ�� �����ϰ� ó���մϴ�.
            commanderDescriptionObject.SetActive(true);
            descriptionCanvasGroup.DOFade(1f, fadeInDuration);
        }
    }

    public void HideInfo()
    {
        // �̸� �ؽ�Ʈ ���̵�ƿ�
        if (commanderNameObject != null && nameCanvasGroup != null && commanderNameObject.activeSelf)
        {
            // [�ٽ� ����] ���̵�ƿ��� ���� �Ŀ� ���� ������Ʈ�� ��Ȱ��ȭ�մϴ�.
            nameCanvasGroup.DOFade(0f, fadeOutDuration)
                           .OnComplete(() => commanderNameObject.SetActive(false));
        }
        // ���� �ؽ�Ʈ ���̵�ƿ�
        if (commanderDescriptionObject != null && descriptionCanvasGroup != null && commanderDescriptionObject.activeSelf)
        {
            // [�ٽ� ����] ���� ������Ʈ�� �����ϰ� ó���մϴ�.
            descriptionCanvasGroup.DOFade(0f, fadeOutDuration)
                                  .OnComplete(() => commanderDescriptionObject.SetActive(false));
        }
    }
}