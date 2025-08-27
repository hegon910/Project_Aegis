using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EventPanelController : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private Image characterImage;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI leftChoiceText;
    [SerializeField] private TextMeshProUGUI rightChoiceText;
    [SerializeField] private Button leftChoiceButton;
    [SerializeField] private Button rightChoiceButton;

    private void Start()
    {
        // 버튼 클릭 리스너 설정
        leftChoiceButton.onClick.AddListener(OnLeftChoice);
        rightChoiceButton.onClick.AddListener(OnRightChoice);

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

        // TODO: 이벤트 테이블 완성되면 다시보기
        leftChoiceText.text = "좌측 선택"; 
        rightChoiceText.text = "우측 선택";

        leftChoiceButton.gameObject.SetActive(data.NextLeftSelectString > 0);
        rightChoiceButton.gameObject.SetActive(data.NextRightSelectString > 0);
    }

    private void OnLeftChoice() // 서브 이벤트만 처리해서 타입 체크가 필요 없습니다
    {
        EventManager.Instance.OnSubEventChoiceSelected(true);
    }

    private void OnRightChoice()
    {
        
        EventManager.Instance.OnSubEventChoiceSelected(false);
    }
}