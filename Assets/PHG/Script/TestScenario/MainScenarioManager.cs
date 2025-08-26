// MainScenarioManager.cs

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;
// [삭제] 에디터 전용 코드는 빌드 시 오류를 유발하므로 삭제합니다.
// using UnityEditor.Experimental.GraphView;

public class MainScenarioManager : MonoBehaviour, IChoiceHandler
{
    [Header("UI 컨트롤러 참조")]
    [SerializeField] private UIPanelController uiPanelController;
    [SerializeField] private SituationCardController situationCardController;
    [SerializeField] private CardController cardController;
    [SerializeField] private ParameterUIController parameterUIController;
    [SerializeField] private GameObject multiChoicePanel;
    [SerializeField] private Button[] multiChoiceButtons;
    [SerializeField] private Image dimmerPanel;

    [Header("시나리오 시작점")]
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
            Debug.LogError("시작 노드(Starting Node)가 지정되지 않았습니다!");
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
                    // [수정] AddListener 호출을 for문 안으로 이동시켰습니다.
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
        Debug.Log("메인 시나리오가 종료되었습니다.");
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