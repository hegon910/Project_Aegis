// MainScenarioManager.cs

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

    void Start()
    {
        if (cardController != null) cardController.choiceHandler = this;
        if (dimmerPanel != null) dimmerPanel.color = Color.clear;

        if (mainStoryUI.choicePreviewText != null)
        {
            mainStoryUI.choicePreviewText.text = "";
            mainStoryUI.choicePreviewText.color = Color.clear;
        }

        if (startingNode != null)
        {
            StartScenario(startingNode);
        }
        else
        {
            Debug.LogError("시작 노드(startingNode)가 인스펙터에 할당되지 않았습니다!");
            gameObject.SetActive(false);
        }
    }

    public void UpdateChoicePreview(string text, Color color)
    {
        // [추가] 이미지의 색상을 업데이트합니다.
        if (mainStoryUI.choicePreviewImage != null)
        {
            // 기존 이미지의 RGB 색상은 유지한 채, 투명도(alpha) 값만 전달받은 color의 값으로 변경합니다.
            Color imageColor = mainStoryUI.choicePreviewImage.color;
            imageColor.a = color.a;
            mainStoryUI.choicePreviewImage.color = imageColor;
        }

        // 기존 텍스트 업데이트 로직은 그대로 유지합니다.
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

        if (currentNode.choices != null && currentNode.choices.Count >= 2)
        {
            cardController.gameObject.SetActive(true);
            cardController.SetChoiceTexts(currentNode.choices[0].choiceText, currentNode.choices[1].choiceText);
            cardController.ResetCardState();
        }
        else
        {
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

    private void EndScenario()
    {
        mainStoryUI.panelRoot.SetActive(false);
        GameManager.instance.GoToBattlePanel();
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