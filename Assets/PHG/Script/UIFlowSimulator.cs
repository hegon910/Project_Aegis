using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static DataManager;

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

    [Header("튜토리얼")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private GameObject parameterTutoText;
    [SerializeField] private GameObject swipeTutorialImage;

    public bool CanMakeChoice => true;

    private static bool hasShownChapter1ParameterTutorial = false;

    // <<<<<<< [핵심 복원 1] 원본과 같이 OnEnable/OnDisable을 사용한 이벤트 구독으로 되돌립니다.
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

    // <<<<<<< [삭제] 불필요해진 함수들을 삭제합니다.
    // public void SubscribeToEvents() { ... }
    // public void UnsubscribeFromEvents() { ... }
    // public void ResetFlow() { ... }

    public void ResetTutorialState()
    {
        hasShownChapter1ParameterTutorial = false;
    }

    // <<<<<<< [핵심 복원 2] 원본의 BeginFlow 함수 로직을 그대로 사용합니다.
    public void BeginFlow(bool startFirstTurn = true)
    {
        if (parameterUIController != null)
        {
            parameterUIController.InitializeAndDisplayStats();
        }

        // UI 초기화
        if (uiPanelController != null) uiPanelController.gameObject.SetActive(false);
        if (situationCardController != null) situationCardController.gameObject.SetActive(false);
        if (dimmerPanel != null) dimmerPanel.color = Color.clear;

        // startFirstTurn이 true일 때만 다음 턴을 시작하도록 수정
        if (startFirstTurn)
        {
            Debug.Log("[UIFlowSimulator] 새로운 사이클 시작. 첫 턴을 진행합니다.");
            EventManager.Instance.PlayNextTurn();
        }
        else
        {
            Debug.Log("[UIFlowSimulator] UI 준비 완료. 플레이어 입력을 기다립니다.");
        }
    }

    private void HandleParameterEvent(int eventId)
    {
        Debug.Log($"[UIFlowSimulator] EventManager로부터 ID: {eventId} 이벤트 신호를 성공적으로 받았습니다.");
        if (DataManager.Instance.PlayerData.playthroughCount == 1 &&
         GameManager.instance.CurrentChapter == 1 &&
         !hasShownChapter1ParameterTutorial)
        {
            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(true);
                if (parameterTutoText != null) parameterTutoText.SetActive(true);
                if (swipeTutorialImage != null) swipeTutorialImage.SetActive(false);
            }
            hasShownChapter1ParameterTutorial = true;
        }
        else if (tutorialPanel != null && tutorialPanel.activeSelf)
        {
            tutorialPanel.SetActive(false);
        }
        currentSubEventData = null;

        currentParameterEventData = DataManager.Instance.GetEventDataById(eventId);

        if (currentParameterEventData == null)
        {
            Debug.LogError($"ID({eventId})에 해당하는 파라미터 이벤트 데이터를 찾을 수 없습니다.");
            return;
        }

        string characterName = "이름 없음";

        if (DataManager.Instance.eventDataDict.TryGetValue(eventId, out var rawEventData))
        {
            if (DataManager.Instance.eventStringDataDict.TryGetValue(rawEventData.EventQuestion, out var stringData))
            {
                if (DataManager.Instance.characterNameDict.TryGetValue(stringData.CharacterName, out var name))
                {
                    characterName = name;
                }
            }
        }

        DisplayEventUI(
            characterSprite: currentParameterEventData.eventSprite,
            characterName: characterName,
            dialogue: currentParameterEventData.dialogue,
            leftChoice: currentParameterEventData.leftChoice.choiceText,
            rightChoice: currentParameterEventData.rightChoice.choiceText
        );
    }

    private void HandleSubEvent(SubEventData data)
    {
        if (tutorialPanel != null && tutorialPanel.activeSelf)
        {
            tutorialPanel.SetActive(false);
        }
        currentParameterEventData = null;
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
        if (tutorialPanel != null && tutorialPanel.activeSelf)
        {
            tutorialPanel.SetActive(false);
        }

        if (currentParameterEventData != null)
        {
            var choice = isRightChoice ? currentParameterEventData.rightChoice : currentParameterEventData.leftChoice;
            bool success = choice.condition.Evaluate(PlayerStats.Instance, PlaythroughHistory.Instance);
            var outcome = success ? choice.successOutcome : choice.failOutcome;

            List<ParameterChange> finalChanges = new List<ParameterChange>(outcome.parameterChanges);
            // 현재 활성화된 특성이 '리사드'이고, 선택지에 '확정 성공'이 아닌 판정 조건이 있었을 경우에만 특성 로직을 실행합니다.
            if (PlayerStats.Instance.ActiveTrait == CommanderTrait.Risard &&
                !(choice.condition is GuaranteedSuccessCondition))
            {
                // '리사드' 특성 로직을 호출하여 finalChanges 목록에 성공/실패에 따른 보정치를 추가합니다.
                PlayerStats.Instance.ActiveCommander?.traitLogic?.ProcessEventOutcome(success, finalChanges);
            }
            // '리사드'가 아니거나 '확정 성공' 이벤트인 경우, 위 if문을 건너뛰고 원래 결과만 사용하게 됩니다.

            PlayerStats.Instance.ApplyChanges(finalChanges);

            StartCoroutine(TransitionToNextEvent(outcome.outcomeText));
        }
        else if (currentSubEventData != null)
        {
            EventManager.Instance.OnSubEventChoiceSelected(isRightChoice);
            StartCoroutine(TransitionToNextEvent("선택지가 처리되었습니다."));
        }
    }

    private IEnumerator TransitionToNextEvent(string resultText)
    {
        situationCardController.UpdateText(resultText);
        yield return new WaitForSeconds(1.5f);
        uiPanelController.Hide();
        situationCardController.Hide();
        yield return new WaitUntil(() => !situationCardController.gameObject.activeInHierarchy);

        Debug.Log("UIFlowSimulator: 다음 턴을 시작하도록 EventManager에 요청합니다.");
        EventManager.Instance.PlayNextTurn();
    }

    public void PreviewAffectedParameters(bool isRightChoice)
    {
        if (currentParameterEventData == null) return;
        EventChoice choice = isRightChoice ? currentParameterEventData.rightChoice : currentParameterEventData.leftChoice;
        var affectedTypes = new HashSet<ParameterType>();
        foreach (var change in choice.successOutcome.parameterChanges) { if (change.valueChange != 0) affectedTypes.Add(change.parameterType); }
        foreach (var change in choice.failOutcome.parameterChanges) { if (change.valueChange != 0) affectedTypes.Add(change.parameterType); }
        var previewChanges = affectedTypes.Select(type => new ParameterChange { parameterType = type, valueChange = 1 }).ToList();
        if (parameterUIController != null) { parameterUIController.UpdateAffectedToggles(previewChanges); }
    }

    public void ClearParameterPreview() { if (parameterUIController != null) { parameterUIController.ClearAllToggles(); } }
    public void UpdateDimmer(float alpha) { if (dimmerPanel != null) { dimmerPanel.color = new Color(0, 0, 0, alpha); } }
    public void UpdateChoicePreview(string text, Color color) { }
}