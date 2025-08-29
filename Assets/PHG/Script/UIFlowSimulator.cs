using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

public class UIFlowSimulator : MonoBehaviour, IChoiceHandler
{
    [Header("UI 컨트롤러 참조")]
    [SerializeField] private UIPanelController uiPanelController;
    [SerializeField] private SituationCardController situationCardController;
    [SerializeField] private CardController cardController;
    [SerializeField] private ParameterUIController parameterUIController;

    [Header("연출 효과")]
    [SerializeField] private Image dimmerPanel;

    private EventData currentParameterEventData;
    private SubEventData currentSubEventData;

    public bool CanMakeChoice => true;

    private void OnEnable()
    {
        EventManager.OnParameterEventReady += HandleParameterEvent;
        EventManager.OnSubEventReady += HandleSubEvent;
    }

    private void OnDisable()
    {
        EventManager.OnParameterEventReady -= HandleParameterEvent;
        EventManager.OnSubEventReady -= HandleSubEvent;
    }

    public void BeginFlow()
    {
        if (parameterUIController != null)
        {
            parameterUIController.InitializeAndDisplayStats();
        }
        
        // UI 초기화
        if (uiPanelController != null) uiPanelController.gameObject.SetActive(false);
        if (situationCardController != null) situationCardController.gameObject.SetActive(false);
        if (cardController != null)
        {
            cardController.gameObject.SetActive(false);
            cardController.choiceHandler = this;
        }
        if (dimmerPanel != null) dimmerPanel.color = Color.clear;

        // EventManager에게 첫 턴 시작을 요청
        EventManager.Instance.PlayNextTurn();
    }

    private void HandleParameterEvent(int eventId)
    {
        currentSubEventData = null; // 다른 타입의 이벤트가 시작됐으므로 초기화
        currentParameterEventData = DataManager.Instance.GetEventDataById(eventId);

        if (currentParameterEventData == null)
        {
            Debug.LogError($"ID({eventId})에 해당하는 파라미터 이벤트 데이터를 찾을 수 없습니다.");
            return;
        }

        DisplayEventUI(
            characterSprite: currentParameterEventData.eventSprite,
            characterName: currentParameterEventData.eventName,
            dialogue: currentParameterEventData.dialogue,
            leftChoice: currentParameterEventData.leftChoice.choiceText,
            rightChoice: currentParameterEventData.rightChoice.choiceText
        );
    }

    private void HandleSubEvent(SubEventData data)
    {
        currentParameterEventData = null; // 다른 타입의 이벤트가 시작됐으므로 초기화
        currentSubEventData = data;

        DisplayEventUI(
            characterSprite: null,
            characterName: data.CharacterName,
            dialogue: data.QuestionString_kr,
            leftChoice: data.LeftSelectString,
            rightChoice: data.RightSelectString
        );
    }

    private void DisplayEventUI(Sprite characterSprite, string characterName, string dialogue, string leftChoice, string rightChoice)
    {
        if (parameterUIController != null)
        {
            parameterUIController.ClearAllToggles();
        }

        uiPanelController.Show(characterSprite, characterName);
        situationCardController.Show(dialogue);

        cardController.SetChoiceTexts(leftChoice, rightChoice);
        cardController.ResetCardState();
        cardController.gameObject.SetActive(true);
    }

    public void HandleChoice(bool isRightChoice)
    {
        if (currentParameterEventData != null)
        {
            // 파라미터 이벤트 결과 처리
            var choice = isRightChoice ? currentParameterEventData.rightChoice : currentParameterEventData.leftChoice;
            bool success = CheckCondition(choice.successCondition);
            var outcome = success ? choice.successOutcome : choice.failOutcome;
            
            if (outcome.parameterChanges != null && PlayerStats.Instance != null)
            {
                PlayerStats.Instance.ApplyChanges(outcome.parameterChanges);
            }
            StartCoroutine(TransitionToNextEvent(outcome.outcomeText));
        }
        else if (currentSubEventData != null)
        {
            // 서브 이벤트 선택 처리
            EventManager.Instance.OnSubEventChoiceSelected(isRightChoice);
        }
    }

    private IEnumerator TransitionToNextEvent(string resultText)
    {
        situationCardController.UpdateText(resultText);
        yield return new WaitForSeconds(3f);
        uiPanelController.Hide();
        situationCardController.Hide();
        yield return new WaitUntil(() => !situationCardController.gameObject.activeInHierarchy);

        // EventManager에게 다음 이벤트를 달라고 요청
        EventManager.Instance.PlayNextTurn();
    }

    public void PreviewAffectedParameters(bool isRightChoice)
    {
        if (currentParameterEventData == null) return;

        EventChoice choice = isRightChoice ? currentParameterEventData.rightChoice : currentParameterEventData.leftChoice;
        
        var affectedTypes = new HashSet<ParameterType>();
        foreach (var change in choice.successOutcome.parameterChanges)
        {
            if (change.valueChange != 0) affectedTypes.Add(change.parameterType);
        }
        foreach (var change in choice.failOutcome.parameterChanges)
        {
            if (change.valueChange != 0) affectedTypes.Add(change.parameterType);
        }

        var previewChanges = affectedTypes.Select(type => new ParameterChange { parameterType = type, valueChange = 1 }).ToList();

        if (parameterUIController != null)
        {
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

    public void UpdateChoicePreview(string text, Color color)
    {
       //
    }
}