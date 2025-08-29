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
}

public class MainScenarioManager : MonoBehaviour, IChoiceHandler
{
    public event Action OnScenarioEnded;
    public bool IsScenarioRunning { get; private set; }

    [Header("UI 컨트롤러 참조")]
    [SerializeField] private CardController cardController;
    [SerializeField] private ParameterUIController parameterUIController;
    [SerializeField] private Image dimmerPanel;

    [Header("메인 스토리 전용 UI")]
    [SerializeField] private MainStoryUI mainStoryUI;
    [SerializeField] private float typingSpeed = 0.05f;

    [Header("시나리오 시작점")]
    [SerializeField] private StoryNode startingNode;

    private StoryNode currentNode;
    private Coroutine typingCoroutine;
    private bool IsTyping => typingCoroutine != null;
    public bool CanMakeChoice => !IsTyping;
    private float lastClickTime = 0f;
    private const float DOUBLE_CLICK_TIME = 0.3f;

    void Start()
    {
       // if (cardController != null) cardController.choiceHandler = this;
        if (dimmerPanel != null) dimmerPanel.color = Color.clear;
        if (mainStoryUI.choicePreviewText != null)
        {
            mainStoryUI.choicePreviewText.text = "";
            mainStoryUI.choicePreviewText.color = Color.clear;
        }

        // ▼ [수정 1] GameManager와의 충돌을 막기 위해, 여기서 시나리오를 자동으로 시작하는 부분을 비활성화합니다.
        /*
        if (startingNode != null)
        {
            StartScenario(startingNode);
        }
        else
        {
            Debug.LogError("시작 노드(startingNode)가 인스펙터에 할당되지 않았습니다!");
            gameObject.SetActive(false);
        }
        */
    }

    public void ResetScenarioState()
    {
        StopAllCoroutines();
        typingCoroutine = null;
        currentNode = startingNode;

        IsScenarioRunning = false;

        if (mainStoryUI.dialogueText != null) mainStoryUI.dialogueText.text = "";
        if (mainStoryUI.characterNameText != null) mainStoryUI.characterNameText.text = "";
        if (mainStoryUI.characterImage != null) mainStoryUI.characterImage.color = Color.clear;
        if (cardController != null) cardController.gameObject.SetActive(false);
        Debug.Log("MainScenarioManager 상태가 초기화되었습니다.");
    }

    public void BeginScenarioFromStart()
    {
        IsScenarioRunning = true; // 상태 플래그 설정
        currentNode = startingNode;
        if (currentNode != null)
        {
            mainStoryUI.panelRoot.SetActive(true);
            DisplayNode(currentNode);
        }
        else
        {
            Debug.LogError("시작 노드가 없습니다!");
        }
    }

    private void EndScenario()
    {
        IsScenarioRunning = false;
        mainStoryUI.panelRoot.SetActive(false);
        if (PlayerStats.Instance.playthroughCount == 1)
        {
            GameManager.instance.StartEventFlow();
        }
        else
        {
            GameManager.instance.GoToBattlePanel();
        }
            
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (Time.time - lastClickTime < DOUBLE_CLICK_TIME)
            {
                if (IsTyping)
                {
                    StopCoroutine(typingCoroutine);
                    mainStoryUI.dialogueText.text = currentNode.storyText;
                    typingCoroutine = null;
                }
            }
            lastClickTime = Time.time;
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

    public void StartScenario(StoryNode startNode)
    {
        mainStoryUI.panelRoot.SetActive(true);
        DisplayNode(startNode);
    }

    private void DisplayNode(StoryNode node)
    {
        if (node == null)
        {
            EndScenario();
            return;
        }
        currentNode = node;
        mainStoryUI.characterNameText.text = currentNode.characterName;
        if (currentNode.characterSprite != null)
        {
            mainStoryUI.characterImage.sprite = currentNode.characterSprite;
            mainStoryUI.characterImage.color = Color.white;
        }
        else
        {
            mainStoryUI.characterImage.color = Color.clear;
        }
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(currentNode.storyText));
        // 선택지 표시 로직이 왜 실행되지 않는지 확인하기 위해 상세 로그를 추가합니다.
        Debug.Log($"--- [진단] DisplayNode 정보: 현재 노드 '{currentNode.name}' ---");
        if (currentNode.choices == null)
        {
            Debug.LogError("[진단] currentNode.choices 리스트가 NULL입니다!");
        }
        else
        {
            Debug.Log($"[진단] currentNode.choices 리스트에 포함된 선택지 개수: {currentNode.choices.Count}");
        }
        if (currentNode.choices != null && currentNode.choices.Count >= 2)
        {
            if (cardController != null) cardController.choiceHandler = this;
            Debug.Log("[진단] 선택지 표시 조건 충족. 카드 UI를 활성화합니다.");
            cardController.gameObject.SetActive(true);
            cardController.SetChoiceTexts(currentNode.choices[0].choiceText, currentNode.choices[1].choiceText);
            cardController.ResetCardState();
        }
        else
        {
            Debug.Log("[진단] 선택지가 없거나 부족하여 자동 진행 로직으로 넘어갑니다.");
            cardController.gameObject.SetActive(false);
            StartCoroutine(AutoTransitionToNext(
                (currentNode.choices != null && currentNode.choices.Count == 1) ? currentNode.choices[0].nextNode : null
            ));
        }
    }

    public void HandleChoice(bool isRightChoice)
    {
        if (IsTyping) return;
        int choiceIndex = isRightChoice ? 1 : 0;
        Choice selectedChoice = currentNode.choices[choiceIndex];
        if (selectedChoice.parameterChanges != null && PlayerStats.Instance != null)
        {
            PlayerStats.Instance.ApplyChanges(selectedChoice.parameterChanges);
        }
        StartCoroutine(TransitionToNextNode(selectedChoice.nextNode));
    }

    private IEnumerator TransitionToNextNode(StoryNode nextNode)
    {
        cardController.gameObject.SetActive(false);
        yield return new WaitForSeconds(1.0f);
        DisplayNode(nextNode);
    }

    private IEnumerator AutoTransitionToNext(StoryNode nextNode)
    {
        yield return new WaitUntil(() => !IsTyping);
        yield return new WaitForSeconds(1.5f);
        DisplayNode(nextNode);
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

    public void PreviewAffectedParameters(bool isRightChoice)
    {
        if (currentNode.choices.Count < 2) return;
        Choice choiceToPreview = currentNode.choices[isRightChoice ? 1 : 0];
        if (parameterUIController != null && choiceToPreview.parameterChanges != null)
        {
            var previewChanges = choiceToPreview.parameterChanges.Where(c => c.valueChange != 0).ToList();
            parameterUIController.UpdateAffectedToggles(previewChanges);
        }
    }

    public void ClearParameterPreview()
    {
        if (parameterUIController != null)
        {
            parameterUIController.ClearAllToggles();
        }
    }

    public void UpdateDimmer(float alpha)
    {
        if (dimmerPanel != null)
        {
            dimmerPanel.color = new Color(0, 0, 0, alpha);
        }
    }
}