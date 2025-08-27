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
    }

    private void OnDisable()
    {
        // EventManager의 이벤트 구독 취소
        EventManager.OnParameterEventReady -= HandleParameterEvent;
    }

    public void BeginFlow()
    {
        // 시작 시 UI 초기화
        if (uiPanelController != null) uiPanelController.gameObject.SetActive(false);
        if (situationCardController != null) situationCardController.gameObject.SetActive(false);
        if (cardController != null)
        {
            cardController.gameObject.SetActive(false);
            cardController.choiceHandler = this;
        }
        if (dimmerPanel != null) dimmerPanel.color = Color.clear;

        // 디버그 모드일 경우, 첫 이벤트를 강제로 시작
        if (forceDebugMode && debugEventSequence.Count > 0)
        {
            HandleParameterEvent(1); // 디버그 시퀀스는 ID를 1부터 시작한다고 가정
        }
    }

    private void HandleParameterEvent(int eventId)
    {
        if (forceDebugMode)
        {
            // 디버그 모드에서는 eventId를 인덱스로 변환하여 사용
            int debugIndex = eventId - 1;
            if (debugEventSequence != null && debugIndex >= 0 && debugIndex < debugEventSequence.Count)
            {
                currentDebugEventData = debugEventSequence[debugIndex];
                LoadEvent(currentDebugEventData);
            }
            else
            {
                Debug.LogWarning("디버그 이벤트 시퀀스가 종료되었습니다.");
            }
        }
        else
        {
            // 일반 모드
            currentEventId = eventId;
            var eventData = DataManager.Instance.GetEventDataById(eventId);
            if (eventData != null)
            { 
                currentLiveEventData = eventData;
                LoadEvent(currentLiveEventData);
            }
            else
            {
                Debug.LogError($"ID({eventId})에 해당하는 이벤트 데이터를 찾을 수 없습니다.");
            }
        }
    }

    private void LoadEvent(object eventDataObject)
    {
        if (parameterUIController != null)
        {
            parameterUIController.ClearAllToggles();
        }

        string dialogue = "", characterName = "";
        string leftChoiceText = "", rightChoiceText = "";
        Sprite characterSprite = null;

        if (eventDataObject is EventData liveData)
        {
            dialogue = liveData.dialogue;
            characterSprite = liveData.eventSprite;
            leftChoiceText = liveData.leftChoice.choiceText;
            rightChoiceText = liveData.rightChoice.choiceText;
        }
        else if (eventDataObject is UIEventData debugData)
        {
            dialogue = debugData.dialogue;
            characterName = debugData.characterName;
            characterSprite = debugData.characterSprite;
            leftChoiceText = debugData.leftChoice.choiceText;
            rightChoiceText = debugData.rightChoice.choiceText;
        }
        else
        {
            Debug.LogError("알 수 없는 이벤트 데이터 타입입니다.");
            return;
        }

        uiPanelController.Show(characterSprite, characterName);
        situationCardController.Show(dialogue);

        cardController.SetChoiceTexts(leftChoiceText, rightChoiceText);
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
            choice = isRightChoice ? currentDebugEventData.rightChoice : currentDebugEventData.leftChoice;
        }

        if (choice == null) return;

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
        uiPanelController.Hide();
        situationCardController.Hide();
        yield return new WaitUntil(() => !situationCardController.gameObject.activeInHierarchy);

        // 다음 턴 진행 준비가 완료되었음을 알림 (필요 시)
        // GameManager.Instance.OnEventFinished(); 
        Debug.Log("이벤트 종료. 다음 턴을 기다립니다.");
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
}