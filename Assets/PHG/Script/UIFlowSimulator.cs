using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

public class UIFlowSimulator : MonoBehaviour
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
    private int eventId = 10001;

    async UniTaskVoid Start()
    {
        if (uiPanelController != null) uiPanelController.gameObject.SetActive(false);
        if (situationCardController != null) situationCardController.gameObject.SetActive(false);
        if (cardController != null) cardController.gameObject.SetActive(false);
        if (dimmerPanel != null) dimmerPanel.color = Color.clear;

        // EventManager가 준비될 때까지 기다림
        await UniTask.WaitUntil(() => EventManager.Instance != null && EventManager.Instance.IsInitialized);

        // EventManager에게 다음 이벤트를 달라고 요청
        RequestNextEvent();
    }

    private void RequestNextEvent()
    {
        if (forceDebugMode)
        {
            // 디버그 모드: 순차 진행 (이 부분은 디버그 시퀀스에 따라 다르게 동작할 수 있음)
            eventId++;
            LoadEvent(eventId);
        }
        else
        {
            // 일반 모드: EventManager에서 다음 이벤트 ID를 받아옴 (순차 또는 랜덤)
            int nextEventId = EventManager.Instance.GetNextEventId();
            if (nextEventId != -1)
            {
                eventId = nextEventId;
                LoadEvent(eventId);
            }
            else
            {
                Debug.Log("진행할 이벤트가 없습니다.");
                // TODO: 튜토리얼이 끝났거나 한 회차가 끝났을 때
                // PlayerStats.Instance.playthroughCount++; 를 추가해서 회차를 + 해야함

                if (cardController != null) cardController.gameObject.SetActive(false);
            }
        }
    }

    private void LoadEvent(int id)
    {
        if (parameterUIController != null)
        {
            parameterUIController.ClearAllToggles();
        }

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

        if (currentLiveEventData != null)
        {
            dialogue = currentLiveEventData.dialogue;
            characterSprite = currentLiveEventData.eventSprite;
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
            Debug.LogWarning("모든 이벤트가 종료되었습니다.");
            if (cardController != null) cardController.gameObject.SetActive(false);
            return;
        }

        uiPanelController.Show(characterSprite, characterName);
        situationCardController.Show(dialogue);

        // 선택지 텍스트 설정
        cardController.SetChoiceTexts(leftChoiceText, rightChoiceText);

        cardController.ResetCardState();
        cardController.gameObject.SetActive(true);
    }


    public void HandleChoice(bool isRightChoice)
    {
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
        // 현재 이벤트를 완료 목록에 추가 (디버그 모드가 아닐 때만)
        if (PlayerStats.Instance != null && !forceDebugMode)
        {
            PlayerStats.Instance.completedEventIds.Add(eventId);
        }

        situationCardController.UpdateText(resultText);
        yield return new WaitForSeconds(3f);
        uiPanelController.Hide();
        situationCardController.Hide();
        yield return new WaitUntil(() => !situationCardController.gameObject.activeInHierarchy);

        // EventManager에게 다음 이벤트를 달라고 요청
        RequestNextEvent();
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