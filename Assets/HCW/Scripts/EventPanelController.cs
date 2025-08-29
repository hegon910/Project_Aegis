using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EventPanelController : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private Image eventImage;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI choiceText;

    private EventData currentEventData;

    // EventData를 받아와서 UI에 내용을 채우는 메서드
    public void DisplayEvent(EventData data)
    {
        currentEventData = data;

        if (currentEventData == null)
        {
            Debug.LogError("표시할 이벤트 데이터가 없습니다.");
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        // UI 요소에 데이터 할당
        eventImage.sprite = currentEventData.eventSprite;
        dialogueText.text = currentEventData.dialogue;

        // 드래그 시작 전에는 선택지 텍스트를 비워둡니다.
        if (choiceText != null)
        {
            choiceText.text = string.Empty;
        }
    }
}
