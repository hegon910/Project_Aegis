using System;
using System.Collections;
using System.Collections.Generic;
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

[System.Serializable]
public class ChapterStory
{
    public int chapterNumber;
    public StoryNode startingNode;
}


public class MainScenarioManager : MonoBehaviour, IChoiceHandler
{
    public static event Action OnScenarioFinished;
    public bool IsScenarioRunning { get; private set; }
    public UIPanelAnimator uiAnimator;

    [Header("UI 컨트롤러 참조")]
    [SerializeField] private CardController cardController;
    [SerializeField] private ParameterUIController parameterUIController;
    [SerializeField] private Image dimmerPanel;

    [Header("메인 스토리 전용 UI")]
    [SerializeField] private MainStoryUI mainStoryUI;
    [SerializeField] private float typingSpeed = 0.05f;

    [Header("챕터별 시나리오 시작점")]
    [SerializeField] private List<ChapterStory> chapterStories;

    private StoryNode currentNode;
    private Coroutine typingCoroutine;
    private bool IsTyping => typingCoroutine != null;
    public bool CanMakeChoice => !IsTyping;

    void Start()
    {
        if (cardController != null) cardController.choiceHandler = this;
        if (dimmerPanel != null) dimmerPanel.color = Color.clear;
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
        ChapterStory storyToPlay = chapterStories.FirstOrDefault(story => story.chapterNumber == currentChapter);

        if (storyToPlay != null && storyToPlay.startingNode != null)
        {
            Debug.Log($"[MainScenarioManager] {currentChapter}챕터 스토리를 시작합니다.");
            uiAnimator.ShowMainStoryView();
            IsScenarioRunning = true;
            mainStoryUI.panelRoot.SetActive(true);
            DisplayNode(storyToPlay.startingNode);
        }
        else
        {
            Debug.LogWarning($"[MainScenarioManager] {currentChapter}챕터에 해당하는 스토리가 없습니다. 스토리 단계를 건너뜁니다.");
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
                mainStoryUI.dialogueText.text = currentNode.storyText;
            }
            // 2. 타이핑이 끝났고, 선택지가 없는 노드라면 다음으로 자동 진행
            else if (currentNode.choices == null || currentNode.choices.Count == 0)
            {
                // 다음 노드가 없으므로 시나리오 종료
                DisplayNode(null);
            }
            else if (currentNode.choices.Count == 1)
            {
                // 다음 노드가 하나뿐인 선형 진행
                DisplayNode(currentNode.choices[0].nextNode);
            }
            // (선택지가 2개 이상인 경우는 CardController가 처리하므로 여기서는 반응하지 않음)
        }
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

        // 선택지 UI 처리
        if (currentNode.choices != null && currentNode.choices.Count >= 2)
        {
            cardController.gameObject.SetActive(true);
            cardController.SetChoiceTexts(currentNode.choices[0].choiceText, currentNode.choices[1].choiceText);
            cardController.ResetCardState();
        }
        else
        {
            cardController.gameObject.SetActive(false);
        }
    }

    public void HandleChoice(bool isRightChoice)
    {
        if (IsTyping || currentNode.choices.Count < 2) return;

        int choiceIndex = isRightChoice ? 1 : 0;
        Choice selectedChoice = currentNode.choices[choiceIndex];

        if (selectedChoice.parameterChanges != null)
        {
            PlayerStats.Instance.ApplyChanges(selectedChoice.parameterChanges);
        }

        StartCoroutine(TransitionToNextNode(selectedChoice.nextNode));
    }

    private IEnumerator TransitionToNextNode(StoryNode nextNode)
    {
        cardController.gameObject.SetActive(false);
        yield return new WaitForSeconds(0.5f); // 전환 대기 시간
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

    // IChoiceHandler 인터페이스의 나머지 함수들
    public void PreviewAffectedParameters(bool isRightChoice)
    {
        if (currentNode == null || currentNode.choices == null || currentNode.choices.Count < 2) return;

        Choice choiceToPreview = currentNode.choices[isRightChoice ? 1 : 0];
        if (parameterUIController != null && choiceToPreview.parameterChanges != null)
        {
            // <<<<<<< [수정] 리스트 내 null 항목이 있어도 오류가 나지 않도록 c != null 조건을 추가합니다.
            var previewChanges = choiceToPreview.parameterChanges.Where(c => c != null && c.valueChange != 0).ToList();
            parameterUIController.UpdateAffectedToggles(previewChanges);
        }
    }

    public void ClearParameterPreview()
    {
        if (parameterUIController != null) parameterUIController.ClearAllToggles();
    }

    public void UpdateDimmer(float alpha)
    {
        if (dimmerPanel != null) dimmerPanel.color = new Color(0, 0, 0, alpha);
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
        Debug.Log($"[치트] 현재 노드 '{currentNode.name}'를 스킵하고 다음으로 진행합니다.");

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

        // 다음 노드를 결정 (선택지가 있으면 첫 번째, 없으면 null)
        StoryNode nextNode = null;
        if (currentNode.choices != null && currentNode.choices.Count > 0)
        {
            nextNode = currentNode.choices[0].nextNode;
        }

        // 다음 노드를 표시 (nextNode가 null이면 시나리오 종료)
        DisplayNode(nextNode);
    }
}