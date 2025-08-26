// MainScenarioManager.cs

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;

// [�߰�] MainScenarioManager�� ������ UI ��ҵ��� ��� �����ϴ� ���� Ŭ����
// �̷��� �ϸ� �ν����� â�� ��������� ������ ���������ϴ�.
[System.Serializable]
public class MainStoryUI
{
    [Header("���� �г� ������Ʈ")]
    public GameObject panelRoot; // storyPanel ��ü�� GameObject

    [Header("ĳ���� ����")]
    public Image characterImage;
    public TextMeshProUGUI characterNameText;

    [Header("���")]
    public TextMeshProUGUI dialogueText;
}

public class MainScenarioManager : MonoBehaviour, IChoiceHandler
{
    [Header("UI ��Ʈ�ѷ� ����")]
    // [����] SituationCardController ������ �����ϰ�, �ʿ��� UI�� ����ϴ�.
    [SerializeField] private UIPanelController uiPanelController;
    [SerializeField] private CardController cardController;
    [SerializeField] private ParameterUIController parameterUIController;
    [SerializeField] private Image dimmerPanel;

    [Header("���� ���丮 UI ���")]
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private float typingSpeed = 0.05f;

    [Header("�ó����� ������")]
    // [����] �ν����Ϳ��� ���� ��带 ���� �����ϴ� ����� ����մϴ�.
    [SerializeField] private StoryNode startingNode;

    private StoryNode currentNode;
    private Coroutine typingCoroutine;
    private bool IsTyping => typingCoroutine != null;

    // [����] GameManager�� ���� ����, Start()���� �ڵ����� �ó������� �����մϴ�.
    void Start()
    {
        if (cardController != null) cardController.choiceHandler = this;
        if (dimmerPanel != null) dimmerPanel.color = Color.clear;

        if (startingNode != null)
        {
            // StartScenario �Լ��� ���� ȣ���մϴ�.
            StartScenario(startingNode);
        }
        else
        {
            Debug.LogError("���� ���(startingNode)�� �ν����Ϳ� �Ҵ���� �ʾҽ��ϴ�!");
            // ������ ���丮�� ������ �ٷ� ��Ȱ��ȭ�ϰų� �ٸ� ó���� �� �� �ֽ��ϴ�.
            gameObject.SetActive(false);
        }
    }

    public void StartScenario(StoryNode startNode)
    {
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

        // 1. UIPanelController�� �ʻ�ȭ�� �̸� ǥ��
        uiPanelController.Show(currentNode.characterSprite, currentNode.characterName);

        // 2. �� ��ũ��Ʈ�� ���� Ÿ���� ȿ�� ����
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(currentNode.storyText));

        // 3. ������ ������ ���� ī�� ǥ��/����
        if (currentNode.choices != null && currentNode.choices.Count >= 2)
        {
            cardController.gameObject.SetActive(true);
            cardController.SetChoiceTexts(currentNode.choices[0].choiceText, currentNode.choices[1].choiceText);
            cardController.ResetCardState();
        }
        else
        {
            cardController.gameObject.SetActive(false);
            // �������� �ϳ��̰ų� ���� ��� �ڵ� ����
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
        dialogueText.text = "";
        foreach (char letter in text.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
        typingCoroutine = null;
    }

    private void EndScenario()
    {
        gameObject.SetActive(false);
        // �ó������� ������ GameManager�� ���� �ܰ� �Լ��� ȣ�� (�� �κ��� ����)
        GameManager.instance.GoToBattlePanel();
    }

    // --- ������ IChoiceHandler �������̽� ������ ---

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

    public void CycleChoice() { }

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