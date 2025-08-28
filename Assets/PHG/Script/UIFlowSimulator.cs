using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

public class UIFlowSimulator : MonoBehaviour, IChoiceHandler
{
    [Header("UI 컨트롤러 참조")]
    [SerializeField] private EventPanelController eventPanelController;
    [SerializeField] private UIPanelController uiPanelController;
    [SerializeField] private SituationCardController situationCardController;
    [SerializeField] private CardController cardController;
    [SerializeField] private ParameterUIController parameterUIController;

    [Header("연출 효과")]
    [SerializeField] private Image dimmerPanel;

    [Header("디버깅 모드")]
    [SerializeField] private bool forceDebugMode = false;
    [SerializeField] private List<UIEventData> debugEventSequence;

    private EventData currentLiveEventData;
    private UIEventData currentDebugEventData;
    private int currentEventId;

    private void OnEnable()
    {
        // EventManager의 이벤트 구독
        EventManager.OnParameterEventReady += HandleParameterEvent;
        EventManager.OnSubEventReady += HandleSubEvent;
    }

    private void OnDisable()
    {
        // EventManager의 이벤트 구독 취소
        EventManager.OnParameterEventReady -= HandleParameterEvent;
        EventManager.OnSubEventReady -= HandleSubEvent;
    }

    public void BeginFlow()
    {
        if (parameterUIController != null)
        {
            parameterUIController.InitializeAndDisplayStats();
        }
        // 시작 시 UI 초기화
        if (eventPanelController != null) eventPanelController.gameObject.SetActive(false);
        if (uiPanelController != null) uiPanelController.gameObject.SetActive(false);
        if (situationCardController != null) situationCardController.gameObject.SetActive(false);
        if (cardController != null)
        {
            cardController.gameObject.SetActive(false);
            cardController.choiceHandler = this;
        }
        if (dimmerPanel != null) dimmerPanel.color = Color.clear;

        // 디버그 모드일 경우 첫 이벤트 시작
        if (forceDebugMode && debugEventSequence.Count > 0)
        {

        }
    }

    private void HandleParameterEvent(int eventId)
    {
        if (forceDebugMode)
        {

        }
        else
        {
            currentEventId = eventId;
            var eventData = DataManager.Instance.GetEventDataById(eventId);
            if (eventData != null)
            { 
                currentLiveEventData = eventData;
                eventPanelController.DisplayParameterEvent(currentLiveEventData);
                
                situationCardController.Show(eventData.dialogue);
                cardController.SetChoiceTexts(eventData.leftChoice.choiceText, eventData.rightChoice.choiceText);
                cardController.ResetCardState();
                cardController.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogError($"ID({eventId})에 해당하는 이벤트 데이터를 찾을 수 없습니다.");
            }
        }
    }

    private void HandleSubEvent(SubEventData data)
    {
        eventPanelController.DisplaySubEvent(data);
        situationCardController.Show(data.QuestionString_kr);
        cardController.SetChoiceTexts(data.LeftSelectString, data.RightSelectString);
        cardController.ResetCardState();
        cardController.gameObject.SetActive(true);
    }


    public void HandleChoice(bool isRightChoice)
    {
        string resultTextToShow = "";
        List<ParameterChange> changes = null;

        EventChoice choice = null;
        if (currentLiveEventData != null)
        {
            choice = isRightChoice ? currentLiveEventData.rightChoice : currentLiveEventData.leftChoice;
        }
        else if (currentDebugEventData != null)
        {
            
        }

        if (choice == null)
        {
            EventManager.Instance.OnSubEventChoiceSelected(isRightChoice);
            return;
        }

        bool success = CheckCondition(choice.successCondition);
        var outcome = success ? choice.successOutcome : choice.failOutcome;
        resultTextToShow = outcome.outcomeText;
        changes = outcome.parameterChanges;

        if (changes != null && PlayerStats.Instance != null)
        {
            PlayerStats.Instance.ApplyChanges(changes);
        }
        StartCoroutine(TransitionToNext(resultTextToShow));
    }

    private IEnumerator TransitionToNext(string resultText)
    {
        if (PlayerStats.Instance != null && !forceDebugMode)
        {
            PlayerStats.Instance.completedEventIds.Add(currentEventId);
        }

        situationCardController.UpdateText(resultText);
        yield return new WaitForSeconds(3f);
        
        // 모든 UI 숨기기
        if (eventPanelController != null) eventPanelController.gameObject.SetActive(false);
        if (uiPanelController != null) uiPanelController.Hide();
        if (situationCardController != null) situationCardController.Hide();

        yield return new WaitUntil(() => (situationCardController == null || !situationCardController.gameObject.activeInHierarchy));

        Debug.Log("이벤트 종료. 다음 턴을 기다립니다.");
        
        EventManager.Instance.PlayNextTurn();
    }

    public void PreviewAffectedParameters(bool isRightChoice)
    {
        EventChoice choice = null;
        if (currentLiveEventData != null)
        {
            choice = isRightChoice ? currentLiveEventData.rightChoice : currentLiveEventData.leftChoice;
        }
        else if (currentDebugEventData != null)
        {
            choice = isRightChoice ? currentDebugEventData.rightChoice : currentDebugEventData.leftChoice;
        }

        if (choice == null) return;

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