using UnityEngine;
using UnityEngine.UI;
using TMPro;

    public class EventPanelController : MonoBehaviour
    {
        [Header("UI 참조")]
        [SerializeField] private Image characterImage;
        [SerializeField] private TextMeshProUGUI characterNameText;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private TextMeshProUGUI leftChoiceText;
        [SerializeField] private TextMeshProUGUI rightChoiceText;

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
            characterImage.sprite = currentEventData.characterSprite;
            characterNameText.text = currentEventData.characterName;
            dialogueText.text = currentEventData.dialogue;
            leftChoiceText.text = currentEventData.leftChoice.choiceText;
            rightChoiceText.text = currentEventData.rightChoice.choiceText;
        }
    }

