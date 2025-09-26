using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class MainStoryUI
{
    [Header("메인 패널 오브젝트")]
    public GameObject panelRoot;
    [Header("캐릭터 관련")]
    public Image characterImage;
    public TextMeshProUGUI characterNameText;
    [Header("대사")]
    public TextMeshProUGUI dialogueText;
    [Header("선택지 미리보기")]
    public Image choicePreviewImage;
    public TextMeshProUGUI choicePreviewText;
    [Header("메인 스토리 전용 Dimmer")]
    public Image mainStoryDimmerPanel;
}

public class MainScenarioManager : MonoBehaviour, IChoiceHandler
{
    public static event Action OnScenarioFinished;
    public bool IsScenarioRunning { get; private set; }
    public UIPanelAnimator uiAnimator;

    [Header("UI 컨트롤러 참조")]
    [SerializeField] private CardController cardController;
    [SerializeField] private ParameterUIController parameterUIController;
   // [SerializeField] private Image dimmerPanel;

    [Header("메인 스토리 전용 UI")]
    [SerializeField] private MainStoryUI mainStoryUI;
    [SerializeField] private float typingSpeed = 0.05f;



    // StoryNode 대신 DataManager에서 받아오는 NewMainEventData를 사용
    private NewMainEventData currentNode;
    private Coroutine typingCoroutine;
    private int currentStoryNum;

    private bool IsTyping => typingCoroutine != null;
    public bool CanMakeChoice => !IsTyping;

    void Start()
    {
       // if (cardController != null) cardController.choiceHandler = this;
       // if (dimmerPanel != null) dimmerPanel.color = Color.clear;
    }

    public void ResetScenarioState()
    {
        StopAllCoroutines();
        typingCoroutine = null;
        currentNode = null;
        IsScenarioRunning = false;
        if (mainStoryUI.panelRoot != null) mainStoryUI.panelRoot.SetActive(false);
        Debug.Log("MainScenarioManager 상태가 초기화되었습니다.");
    }

    public void BeginScenarioFromStart()
    {
        int currentChapter = GameManager.instance.CurrentChapter;
        int startStoryNum = GameManager.instance.GetCurrentChapterStartStoryNum();

        if (startStoryNum > 0)
        {
            // 1. StoryNum으로 첫 번째 데이터 노드를 찾아옵니다.
            NewMainEventData firstNode = DataManager.Instance.GetMainEventDataByStoryNum(startStoryNum);

            // 2. 찾아온 노드가 유효하고, 그 노드의 ID가 있다면
            if (firstNode != null && firstNode.id > 0)
            {
                Debug.Log($"[MainScenarioManager] {currentChapter}챕터 스토리를 시작합니다. (시작 StoryNum: {startStoryNum}, 시작 ID: {firstNode.id})");
                uiAnimator.ShowMainStoryView();
                IsScenarioRunning = true;
                mainStoryUI.panelRoot.SetActive(true);

                // 3. DisplayNode에는 StoryNum이 아닌, 실제 데이터의 ID를 전달합니다.
                DisplayNode(firstNode.id);
            }
            else
            {
                Debug.LogWarning($"[MainScenarioManager] StoryNum {startStoryNum}에 해당하는 데이터를 찾았으나, 유효한 ID가 없습니다. 스토리 단계를 건너뜁니다.");
                EndScenario();
            }
        }
        else
        {
            Debug.LogWarning($"[MainScenarioManager] {currentChapter}챕터에 해당하는 시작 StoryNum을 찾을 수 없습니다. 스토리 단계를 건너뜁니다.");
            EndScenario();
        }
    }

    private void EndScenario()
    {
        IsScenarioRunning = false;
        OnScenarioFinished?.Invoke();
    }

    void Update()
    {
        // 시나리오가 실행 중이 아닐 때는 아무것도 하지 않음
        if (!IsScenarioRunning || currentNode == null) return;

        // 마우스 클릭 감지
        if (Input.GetMouseButtonDown(0))
        {
            // 1. 만약 텍스트 타이핑 중이라면, 즉시 전체 텍스트를 보여주고 종료
            if (IsTyping)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
                mainStoryUI.dialogueText.text = currentNode.dialogue;
            }
            // 2. 타이핑이 끝났고, 다음 노드가 정해진 경우
            else
            {
                bool hasLeftChoice = currentNode.leftChoice != null && currentNode.leftChoice.nextEventID > 0;
                bool hasRightChoice = currentNode.rightChoice != null && currentNode.rightChoice.nextEventID > 0;

                // 선택지가 2개인 경우 CardController가 처리하므로 대기
                if (hasLeftChoice && hasRightChoice)
                {
                    // 아무것도 하지 않음
                }
                // 선택지가 1개인 선형 진행
                else if (hasLeftChoice)
                {
                    DisplayNode(currentNode.leftChoice.nextEventID);
                }
                else if (hasRightChoice)
                {
                    DisplayNode(currentNode.rightChoice.nextEventID);
                }
                // 다음 노드가 없으므로 시나리오 종료
               
            }
        }
    }
    private void DisplayNode(int nodeID)
    {
        if (nodeID <= 0)
        {
            EndScenario();
            return;
        }
        currentNode = DataManager.Instance.GetMainEventDataById(nodeID);

        if (currentNode == null)
        {
            Debug.LogError($"[MainScenarioManager] ID({nodeID})에 해당하는 이벤트 데이터를 가져오지 못했습니다. 시나리오를 종료합니다.");
            EndScenario();
            return;
        }

        mainStoryUI.characterNameText.text = currentNode.characterData?.Chr_Name ?? "";

        // [변경] 초상 스프라이트 결정 로직 통합
        var portrait = ResolvePortraitSprite(currentNode);
        if (portrait != null)
        {
            mainStoryUI.characterImage.sprite = portrait;
            mainStoryUI.characterImage.color = Color.white;
        }
        else
        {
            mainStoryUI.characterImage.sprite = null;
            mainStoryUI.characterImage.color = Color.clear;
        }

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(currentNode.dialogue));

        bool hasLeftChoice = currentNode.leftChoice != null && !string.IsNullOrEmpty(currentNode.leftChoice.choiceText);
        bool hasRightChoice = currentNode.rightChoice != null && !string.IsNullOrEmpty(currentNode.rightChoice.choiceText);

        if (hasLeftChoice && hasRightChoice)
        {
            cardController.gameObject.SetActive(true);
            cardController.SetChoiceTexts(currentNode.leftChoice.choiceText, currentNode.rightChoice.choiceText);
            cardController.ResetCardState();
        }
        else
        {
            cardController.gameObject.SetActive(false);
        }
    }

    // 초상 스프라이트 결정: CharacterImg_ID의 IMGName → Resources/Portraits/<IMGName> 우선
    private Sprite ResolvePortraitSprite(NewMainEventData node)
    {
        string imgName = node.characterImgData?.IMGName;
        if (!string.IsNullOrEmpty(imgName))
        {
            // 1) 명시 경로: Resources/Portraits/<IMGName>
            var s = Resources.Load<Sprite>($"Portraits/{imgName}");
            if (s != null) return s;

            // 2) 예전 규칙(단수 폴더) 호환
            s = Resources.Load<Sprite>($"Portrait/{imgName}");
            if (s != null) return s;

            // 3) CSV가 전체 경로를 담고 있는 경우를 대비하여 원문 시도
            s = Resources.Load<Sprite>(imgName);
            if (s != null) return s;
        }
        return null;
    }
    
    // 메인 시나리오 선택 처리
    public void HandleChoice(bool isRightChoice)
    {
        if (currentNode == null) return;
        if (currentNode.leftChoice == null && currentNode.rightChoice == null) return;

        MainEventChoice selectedChoice = isRightChoice ? currentNode.rightChoice : currentNode.leftChoice;
        if (selectedChoice == null) return;

        Debug.Log($"[MainScenarioManager] 선택지 처리 시작. 선택된 다음 노드 ID: {selectedChoice.nextEventID}");

        // 회상 텍스트 저장
        if (selectedChoice.isEndingMemoriar)
        {
            if (DataManager.Instance.endingMemoriarDataDict.TryGetValue(selectedChoice.ID, out var memoriarData))
            {
                DataManager.Instance.AddEndingMemoriar(memoriarData.Text_KR);
                Debug.Log($"[DataManager] 엔딩 기억 조각 획득: {memoriarData.Text_KR}");
            }
        }

        // 진엔딩 카운팅 로직 추가
        if (selectedChoice.isCountingforRealEnding2)
        {
            DataManager.Instance.PlayerData.realEnding2ChoiceCount++;
            Debug.Log($"[MainScenarioManager] 2회차 진엔딩 카운트 증가: {DataManager.Instance.PlayerData.realEnding2ChoiceCount}");
        }

        if (selectedChoice.isCountingforRealEnding3)
        {
            DataManager.Instance.PlayerData.realEnding3ChoiceCount++;
            Debug.Log($"[MainScenarioManager] 3회차 진엔딩 카운트 증가: {DataManager.Instance.PlayerData.realEnding3ChoiceCount}");
        }

        if (selectedChoice.outcome?.parameterChanges != null)
        {
            GamePlayerStats.Instance.ApplyChanges(selectedChoice.outcome.parameterChanges);
        }

        StartCoroutine(TransitionToNextNode(selectedChoice.nextEventID));
    }

    private IEnumerator TransitionToNextNode(int nextNodeID)
    {

       // cardController.gameObject.SetActive(false);
        yield return new WaitForSeconds(0.5f); // 전환 대기 시간
        DisplayNode(nextNodeID);
    }

    private IEnumerator TypeText(string text)
    {
        mainStoryUI.dialogueText.text = "";
        foreach (char letter in text.ToCharArray())
        {
            mainStoryUI.dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
        typingCoroutine = null;
    }

    // IChoiceHandler 인터페이스의 나머지 함수들
    public void PreviewAffectedParameters(bool isRightChoice)
    {
        if (currentNode == null) return;

        bool hasLeftChoice = currentNode.leftChoice != null;
        bool hasRightChoice = currentNode.rightChoice != null;
        if (!hasLeftChoice || !hasRightChoice) return;

        MainEventChoice choiceToPreview = isRightChoice ? currentNode.rightChoice : currentNode.leftChoice;
        if (parameterUIController != null && choiceToPreview.outcome?.parameterChanges != null)
        {
            var previewChanges = choiceToPreview.outcome.parameterChanges.Where(c => c != null && c.valueChange != 0).ToList();
            parameterUIController.UpdateAffectedToggles(previewChanges);
        }
    }

    public void ClearParameterPreview()
    {
        if (parameterUIController != null) parameterUIController.ClearAllToggles();
    }

    public void UpdateDimmer(float alpha)
    {
           if (mainStoryUI.mainStoryDimmerPanel != null)
        {
            mainStoryUI.mainStoryDimmerPanel.color = new Color(0, 0, 0, alpha);
        }
    }

    public void UpdateChoicePreview(string text, Color color)
    {
        if (mainStoryUI.choicePreviewImage != null)
        {
            Color imageColor = mainStoryUI.choicePreviewImage.color;
            imageColor.a = color.a;
            mainStoryUI.choicePreviewImage.color = imageColor;
        }
        if (mainStoryUI.choicePreviewText != null)
        {
            mainStoryUI.choicePreviewText.text = text;
            mainStoryUI.choicePreviewText.color = color;
        }
    }

    public void SkipToNextNode()
    {
        Debug.Log($"[치트] 현재 노드 ID '{currentNode?.id}'를 스킵하고 다음으로 진행합니다.");

        // 진행 중인 텍스트 타이핑이나 다른 코루틴을 중지
        if (IsTyping)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        StopAllCoroutines();

        if (currentNode == null)
        {
            EndScenario();
            return;
        }

        // 다음 노드를 결정 (첫 번째 유효한 선택지를 따름)
        int nextNodeID = 0;
        if (currentNode.leftChoice != null && currentNode.leftChoice.nextEventID > 0)
        {
            nextNodeID = currentNode.leftChoice.nextEventID;
        }
        else if (currentNode.rightChoice != null && currentNode.rightChoice.nextEventID > 0)
        {
            nextNodeID = currentNode.rightChoice.nextEventID;
        }

        // 다음 노드를 표시 (nextNodeID가 0이면 시나리오 종료)
        DisplayNode(nextNodeID);
    }
}