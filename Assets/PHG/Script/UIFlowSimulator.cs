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
    public void ResetTutorialState()
    {
        hasShownChapter1ParameterTutorial = false;
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
        if (PlayerStats.Instance.playthroughCount == 1 &&
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
            // 다른 이벤트에서는 스와이프 튜토리얼 등도 꺼준다.
            tutorialPanel.SetActive(false);
        }
        currentSubEventData = null;

        // 이 함수는 dialogue나 choiceText 등을 가져오기 위해 그대로 호출합니다.
        currentParameterEventData = DataManager.Instance.GetEventDataById(eventId);

        if (currentParameterEventData == null)
        {
            Debug.LogError($"ID({eventId})에 해당하는 파라미터 이벤트 데이터를 찾을 수 없습니다.");
            return;
        }

        // --- 캐릭터 이름을 직접 찾는 로직 ---
        string characterName = "이름 없음"; // 기본값

        // 1. eventId로 원본 데이터(ParameterEventData)
        if (DataManager.Instance.eventDataDict.TryGetValue(eventId, out var rawEventData))
        {
            // 2. EventQuestion ID로 문자열 데이터(ParameterEventStringData)
            if (DataManager.Instance.eventStringDataDict.TryGetValue(rawEventData.EventQuestion, out var stringData))
            {
                // 3. 문자열 데이터 안의 CharacterName ID로 characterNameDict에서 실제 이름(string)
                if (DataManager.Instance.characterNameDict.TryGetValue(stringData.CharacterName, out var name))
                {
                    characterName = name;
                }
            }
        }

        // --- 이제 직접 찾은 characterName 변수를 사용 ---
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
        if (tutorialPanel != null && tutorialPanel.activeSelf)
        {
            tutorialPanel.SetActive(false);
        }

        if (currentParameterEventData != null)
        {
            // 파라미터 이벤트 결과 처리
            var choice = isRightChoice ? currentParameterEventData.rightChoice : currentParameterEventData.leftChoice;
            bool success = CheckCondition(choice.successCondition);
            var outcome = success ? choice.successOutcome : choice.failOutcome;

            currentParameterEventData.IsConditionSuccess = success;
            DataManager.Instance.eventDataDict[currentParameterEventData.eventName].IsConditionSuccess = success;

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