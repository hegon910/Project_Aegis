using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EventPanelController : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private Image characterImage;
    [SerializeField] private TextMeshProUGUI dialogueText;

    private void Start()
    {
        // 시작할 때 패널을 비활성화
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        // TODO: 이벤트 기반으로 변경 시 구독
        // EventManager.OnSubEventReady += DisplaySubEvent;
    }

    private void OnDisable()
    {
        // TODO: 이벤트 기반으로 변경 시 구독 취소
        // EventManager.OnSubEventReady -= DisplaySubEvent;
    }

    /// <summary>
    /// 서브 이벤트를 UI에 표시
    /// </summary>
    public void DisplaySubEvent(SubEventData data)
    {
        if (data == null) 
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        dialogueText.text = data.QuestionString_kr;
        characterImage.sprite = Resources.Load<Sprite>($"Images/{data.CharacterImg}");
    }

    public void DisplayParameterEvent(EventData data)
    {
        if (data == null) 
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        dialogueText.text = data.dialogue;
        characterImage.sprite = data.eventSprite;
    }
}
