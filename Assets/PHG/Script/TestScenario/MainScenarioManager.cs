// MainScenarioManager.cs

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;
// [����] ������ ���� �ڵ�� ���� �� ������ �����ϹǷ� �����մϴ�.
// using UnityEditor.Experimental.GraphView;

public class MainScenarioManager : MonoBehaviour, IChoiceHandler
{
    [Header("UI ��Ʈ�ѷ� ����")]
    [SerializeField] private UIPanelController uiPanelController;
    [SerializeField] private SituationCardController situationCardController;
    [SerializeField] private CardController cardController;
    [SerializeField] private ParameterUIController parameterUIController;
    [SerializeField] private GameObject multiChoicePanel;
    [SerializeField] private Button[] multiChoiceButtons;
    [SerializeField] private Image dimmerPanel;

    [Header("�ó����� ������")]
    [SerializeField] private StoryNode startingNode;

    private StoryNode currentNode;

    void Start()
    {
        if (cardController != null) cardController.choiceHandler = this;
        if (dimmerPanel != null) dimmerPanel.color = Color.clear;

        if (startingNode != null)
        {
            StartScenario(startingNode);
        }
        else
        {
            Debug.LogError("���� ���(Starting Node)�� �������� �ʾҽ��ϴ�!");
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

        uiPanelController.Show(currentNode.characterSprite, currentNode.characterName);
        situationCardController.Show(currentNode.storyText);

        if (currentNode.choices.Count == 2)
        {
            multiChoicePanel.SetActive(false);
            cardController.gameObject.SetActive(true);
            cardController.SetChoiceTexts(currentNode.choices[0].choiceText, currentNode.choices[1].choiceText);
            cardController.ResetCardState();
        }
        else
        {
            cardController.gameObject.SetActive(false);
            multiChoicePanel.SetActive(true);

            for (int i = 0; i < multiChoiceButtons.Length; i++)
            {
                if (i < currentNode.choices.Count)
                {
                    multiChoiceButtons[i].gameObject.SetActive(true);
                    multiChoiceButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = currentNode.choices[i].choiceText;

                    int choiceIndex = i;
                    multiChoiceButtons[i].onClick.RemoveAllListeners();
                    // [����] AddListener ȣ���� for�� ������ �̵����׽��ϴ�.
                    multiChoiceButtons[i].onClick.AddListener(() => OnMultiChoiceSelected(choiceIndex));
                }
                else
                {
                    multiChoiceButtons[i].gameObject.SetActive(false);
                }
            }
        }
    }

    private void OnMultiChoiceSelected(int choiceIndex)
    {
        Choice selectedChoice = currentNode.choices[choiceIndex];

        if (selectedChoice.parameterChanges != null && PlayerStats.Instance != null)
        {
            PlayerStats.Instance.ApplyChanges(selectedChoice.parameterChanges);
        }

        DisplayNode(selectedChoice.nextNode);
    }

    private void EndScenario()
    {
        situationCardController.Hide();
        uiPanelController.Hide();
        cardController.gameObject.SetActive(false);
        multiChoicePanel.SetActive(false);
        Debug.Log("���� �ó������� ����Ǿ����ϴ�.");
    }


    public void HandleChoice(bool isRightChoice)
    {
        Choice selectedChoice = currentNode.choices[isRightChoice ? 1 : 0];

        if (selectedChoice.parameterChanges != null && PlayerStats.Instance != null)
        {
            PlayerStats.Instance.ApplyChanges(selectedChoice.parameterChanges);
        }

        DisplayNode(selectedChoice.nextNode);
    }

    public void PreviewAffectedParameters(bool isRightChoice)
    {
        int choiceIndex = isRightChoice ? 1 : 0;
        if (choiceIndex < currentNode.choices.Count)
        {
            Choice selectedChoice = currentNode.choices[choiceIndex];
            if (parameterUIController != null && selectedChoice.parameterChanges != null)
            {
                var previewChanges = selectedChoice.parameterChanges
                    .Where(change => change.valueChange != 0)
                    .ToList();
                parameterUIController.UpdateAffectedToggles(previewChanges);
            }
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