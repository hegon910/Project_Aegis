using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UIFlowSimulator : MonoBehaviour
{
    [Header("UI ��Ʈ�ѷ� ����")]
    [SerializeField] private UIPanelController uiPanelController;
    [SerializeField] private SituationCardController situationCardController;
    [SerializeField] private CardController cardController;

    [Header("����� ���")]
    [SerializeField] private bool forceDebugMode = false;
    [SerializeField] private List<UIEventData> debugEventSequence;

    private EventData currentLiveEventData;
    private UIEventData currentDebugEventData;
    private int eventId = 1;

    void Start()
    {
        if (uiPanelController != null) uiPanelController.gameObject.SetActive(false);
        if (situationCardController != null) situationCardController.gameObject.SetActive(false);
        if (cardController != null) cardController.gameObject.SetActive(false);

        LoadEvent(eventId);
    }

    private void LoadEvent(int id)
    {
        currentLiveEventData = null;
        currentDebugEventData = null;

        string dialogue = "", characterName = "";
        string leftChoiceText = "", rightChoiceText = "";
        Sprite characterSprite = null;

        if (!forceDebugMode)
        {
            currentLiveEventData = DataManager.Instance.GetEventDataById(id);
        }
        else
        {
            int debugIndex = id - 1;
            if (debugEventSequence != null && debugIndex >= 0 && debugIndex < debugEventSequence.Count)
            {
                currentDebugEventData = debugEventSequence[debugIndex];
            }
        }

        // ������ �ҽ��� ���� ���� �Ҵ�
        if (currentLiveEventData != null)
        {
            dialogue = currentLiveEventData.dialogue;
            characterName = currentLiveEventData.characterName;
            characterSprite = currentLiveEventData.characterSprite;
            leftChoiceText = currentLiveEventData.leftChoice.choiceText;
            rightChoiceText = currentLiveEventData.rightChoice.choiceText;
        }
        else if (currentDebugEventData != null)
        {
            dialogue = currentDebugEventData.dialogue;
            characterName = currentDebugEventData.characterName;
            characterSprite = currentDebugEventData.characterSprite;
            leftChoiceText = currentDebugEventData.leftChoice.choiceText;
            rightChoiceText = currentDebugEventData.rightChoice.choiceText;
        }
        else
        {
            Debug.LogWarning("��� �̺�Ʈ�� ����Ǿ����ϴ�.");
            if (cardController != null) cardController.gameObject.SetActive(false);
            return;
        }

        uiPanelController.Show(characterSprite, characterName);
        situationCardController.Show(dialogue);
        cardController.SetChoiceTexts(leftChoiceText, rightChoiceText);

        // ���� �̺�Ʈ�� ���۵Ǳ� ������ ī���� ����(��ġ, ȸ��)�� �ʱ�ȭ�մϴ�.
        cardController.ResetCardState();
        cardController.gameObject.SetActive(true);
    }

    public void HandleChoice(bool isRightChoice)
    {
        // �ڡڡ� ������ �κ� �ڡڡ�
        // ���⼭ ī�带 ��� ��Ȱ��ȭ�ϴ� ������ �ڵ带 �����߽��ϴ�.
        // ���� ī��� ������ ��� ������ �� �ֽ��ϴ�.

        string resultTextToShow = "";
        List<ParameterChange> changes = null;

        if (currentLiveEventData != null)
        {
            var choice = isRightChoice ? currentLiveEventData.rightChoice : currentLiveEventData.leftChoice;
            bool success = CheckCondition(choice.successCondition);
            var outcome = success ? choice.successOutcome : choice.failOutcome;
            resultTextToShow = outcome.outcomeText;
            changes = outcome.parameterChanges;
        }
        else if (currentDebugEventData != null)
        {
            var choice = isRightChoice ? currentDebugEventData.rightChoice : currentDebugEventData.leftChoice;
            bool success = CheckCondition(choice.successCondition);
            var outcome = success ? choice.successOutcome : choice.failOutcome;
            resultTextToShow = outcome.outcomeText;
            changes = outcome.parameterChanges;
        }

        if (changes != null && PlayerStats.Instance != null)
        {
            PlayerStats.Instance.ApplyChanges(changes);
        }

        StartCoroutine(TransitionToNextEvent(resultTextToShow));
    }

    private IEnumerator TransitionToNextEvent(string resultText)
    {
        situationCardController.UpdateText(resultText);

        // ��û�ϽŴ�� ��� Ȯ�� �ð��� 0.3�ʷ� �����մϴ�.
        yield return new WaitForSeconds(3f);

        uiPanelController.Hide();
        situationCardController.Hide();
        yield return new WaitUntil(() => !situationCardController.gameObject.activeInHierarchy);

        eventId++;
        LoadEvent(eventId);
    }

    private bool CheckCondition(string condition)
    {
        if (string.IsNullOrEmpty(condition)) return true;
        var parts = condition.Split(',');
        if (parts.Length != 2) return true;
        System.Enum.TryParse(parts[0], true, out ParameterType paramType);
        string opAndValue = parts[1];
        char op = opAndValue.Contains(">") ? '>' : '<';
        if (!int.TryParse(opAndValue.Substring(1), out int value)) return true;
        return op == '>' ? PlayerStats.Instance.GetStat(paramType) > value : PlayerStats.Instance.GetStat(paramType) < value;
    }
}