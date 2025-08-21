﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class UIFlowSimulator : MonoBehaviour
{
    [Header("UI 컨트롤러 참조")]
    [SerializeField] private UIPanelController uiPanelController;
    [SerializeField] private SituationCardController situationCardController;
    [SerializeField] private CardController cardController;
    [SerializeField] private ParameterUIController parameterUIController;

    [Header("디버깅 모드")]
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

        // 데이터 소스에 따라 변수 할당
        if (currentLiveEventData != null)
        {
            dialogue = currentLiveEventData.dialogue;
            //characterName = currentLiveEventData.characterName;
            //characterSprite = currentLiveEventData.characterSprite;
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
        cardController.SetChoiceTexts(leftChoiceText, rightChoiceText);

        // 다음 이벤트가 시작되기 직전에 카드의 상태(위치, 회전)를 초기화합니다.
        cardController.ResetCardState();
        cardController.gameObject.SetActive(true);
    }

    public void HandleChoice(bool isRightChoice)
    {
        // ★★★ 수정된 부분 ★★★
        // 여기서 카드를 즉시 비활성화하던 문제의 코드를 삭제했습니다.
        // 이제 카드는 연출을 모두 수행할 수 있습니다.

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

        // 요청하신대로 결과 확인 시간은 0.3초로 유지합니다.
        yield return new WaitForSeconds(3f);

        uiPanelController.Hide();
        situationCardController.Hide();
        yield return new WaitUntil(() => !situationCardController.gameObject.activeInHierarchy);

        eventId++;
        LoadEvent(eventId);
    }

    /// <summary>
    /// (핵심 기능) 카드 드래그 시, 변경될 파라미터 토글을 미리 보여줍니다.
    /// </summary>
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

        // 성공/실패 결과에 포함된 모든 파라미터를 중복 없이 합칩니다.
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

    /// <summary>
    /// (핵심 기능) 카드 드래그를 멈추면 미리보기 토글을 모두 끕니다.
    /// </summary>
    public void ClearParameterPreview()
    {
        if (parameterUIController != null)
        {
            parameterUIController.ClearAllToggles();
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