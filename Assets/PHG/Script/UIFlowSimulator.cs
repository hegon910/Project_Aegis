using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
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
    [SerializeField] private Image subEventDimmerPanel;

    private EventData currentParameterEventData;
    private FullSubEventData currentSubEventData;

    [Header("튜토리얼")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private GameObject parameterTutoText;
    [SerializeField] private GameObject swipeTutorialImage;

    public bool CanMakeChoice => true;

    // 게임오버 등으로 다음 턴 전환 코루틴을 중단하기 위한 플래그
    private bool cancelTransitions = false;

    private static bool hasShownChapter1ParameterTutorial = false;
    private bool subEventExitFaded = false;

    // <<<<<<< [핵심 복원 1] 원본과 같이 OnEnable/OnDisable을 사용한 이벤트 구독으로 되돌립니다.
    private void OnEnable()
    {
        EventManager.OnParameterEventReady += HandleParameterEvent;
        EventManager.OnSubEventReady += HandleSubEvent;
        EventManager.OnSubEventExitFadeRequested += OnSubEventExitFadeRequested;
    }

    private void OnDisable()
    {
        EventManager.OnParameterEventReady -= HandleParameterEvent;
        EventManager.OnSubEventReady -= HandleSubEvent;
        EventManager.OnSubEventExitFadeRequested -= OnSubEventExitFadeRequested;
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
        cancelTransitions = false; // 새 플로우 시작 시 코루틴 허용
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

    // 외부(예: GameManager.GameOver)에서 호출: 진행 중 전환을 모두 중지
    public void AbortPendingTransitions()
    {
        cancelTransitions = true;
        StopAllCoroutines();
        // UIPanelController의 꺼졌다 켜지는 시퀀스를 방해하지 않기 위해 UI 활성 상태는 변경하지 않습니다.
    }

    // 튜토리얼 표시 상태를 강제로 설정 (이어하기 시 튜토리얼 억제용)
    public void MarkParameterTutorialShown()
    {
        hasShownChapter1ParameterTutorial = true;
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

        string characterName = "";

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

        // 파라미터 이벤트 초상 스프라이트 해석: Chr_name(이름) 기준 우선, 실패 시 CharacterImage 폴백
        var parameterPortrait = ResolveParameterPortraitSprite(characterName);
        if (parameterPortrait == null)
        {
            parameterPortrait = ResolveParameterPortraitSprite(currentParameterEventData.CharacterImage);
        }

        // 서브이벤트 종료로 인해 화면이 검게 페이드된 상태라면
        if (subEventExitFaded && subEventDimmerPanel != null)
        {
            DisplayEventUI(
                characterSprite: parameterPortrait,
                characterName: characterName,
                dialogue: currentParameterEventData.dialogue,
                leftChoice: currentParameterEventData.leftChoice.choiceText,
                rightChoice: currentParameterEventData.rightChoice.choiceText
            );
            StartCoroutine(FadeInAfterSubEventExit());
            return;
        }

        DisplayEventUI(
            characterSprite: parameterPortrait,
            characterName: characterName,
            dialogue: currentParameterEventData.dialogue,
            leftChoice: currentParameterEventData.leftChoice.choiceText,
            rightChoice: currentParameterEventData.rightChoice.choiceText
        );
    }

    private void HandleSubEvent(FullSubEventData data)
    {
        if (data == null)
        {
            Debug.LogError("[UIFlowSimulator] HandleSubEvent: data가 null입니다.");
            return;
        }

        if (tutorialPanel != null && tutorialPanel.activeSelf)
        {
            tutorialPanel.SetActive(false);
        }
        // 파라미터 이벤트에서 넘어온 경우인지 먼저 판별
        bool cameFromParameterEvent = currentParameterEventData != null;

        // 파라미터 이벤트 쪽 전환 코루틴이 진행 중이면 중단하여 페이드 연출을 막지 않도록 정리
        StopAllCoroutines();

        currentParameterEventData = null;
        currentSubEventData = data;

        string characterName = "";
        if (data.characterData != null)
        {
            characterName = data.characterData.Chr_Name ?? "";
        }

        // null 체크 추가
        string dialogue = data.Text_kr ?? "대화 내용이 없습니다.";
        string leftChoiceText = data.leftChoice?.choiceText ?? "선택지 1";
        string rightChoiceText = data.rightChoice?.choiceText ?? "선택지 2";

        // 파라미터 이벤트 도중 서브이벤트로 진입할 때는 페이드 아웃/인 연출 적용
        if (cameFromParameterEvent && subEventDimmerPanel != null)
        {
            StartCoroutine(FadeToBlackThenDisplaySubEvent(characterName, dialogue, leftChoiceText, rightChoiceText));
            return;
        }

        DisplayEventUI(
            characterSprite: ResolvePortraitSprite(data),
            characterName: characterName,
            dialogue: dialogue,
            leftChoice: leftChoiceText,
            rightChoice: rightChoiceText
        );
    }

    private IEnumerator FadeToBlackThenDisplaySubEvent(string characterName, string dialogue, string leftChoiceText, string rightChoiceText)
    {
        // 시작 상태 초기화 및 입력 차단 (서브이벤트 전용 디머 사용)
        subEventDimmerPanel.gameObject.SetActive(true);
        subEventDimmerPanel.raycastTarget = true;
        subEventDimmerPanel.DOKill();
        subEventDimmerPanel.color = new Color(0f, 0f, 0f, 0f);

        // 페이드 아웃: 알파 1까지 2초
        yield return subEventDimmerPanel.DOFade(1f, 2f).SetUpdate(false).WaitForCompletion();
        subEventDimmerPanel.color = new Color(0f, 0f, 0f, 1f);

        // 서브이벤트 UI로 전환
        DisplayEventUI(
            characterSprite: ResolvePortraitSprite(currentSubEventData),
            characterName: characterName,
            dialogue: dialogue,
            leftChoice: leftChoiceText,
            rightChoice: rightChoiceText
        );

        // 페이드 인
        yield return subEventDimmerPanel.DOFade(0f, 0.5f).SetUpdate(false).WaitForCompletion();
        subEventDimmerPanel.color = new Color(0f, 0f, 0f, 0f);
        subEventDimmerPanel.raycastTarget = false;
    }

    private void OnSubEventExitFadeRequested(float duration)
    {
        StopAllCoroutines();
        StartCoroutine(FadeOutForSubEventExit(duration));
    }

    private IEnumerator FadeOutForSubEventExit(float duration)
    {
        if (subEventDimmerPanel == null)
        {
            yield break;
        }

        subEventDimmerPanel.gameObject.SetActive(true);
        subEventDimmerPanel.raycastTarget = true;
        subEventDimmerPanel.DOKill();
        subEventDimmerPanel.color = new Color(0f, 0f, 0f, 0f);
        if (duration > 0f)
        {
            yield return subEventDimmerPanel.DOFade(1f, duration).SetUpdate(false).WaitForCompletion();
        }
        else
        {
            subEventDimmerPanel.color = new Color(0f, 0f, 0f, 1f);
            yield return null;
        }
        subEventExitFaded = true;
    }

    private IEnumerator FadeInAfterSubEventExit()
    {
        if (subEventDimmerPanel == null)
        {
            subEventExitFaded = false;
            yield break;
        }
        // 페이드 인
        yield return subEventDimmerPanel.DOFade(0f, 0.5f).SetUpdate(false).WaitForCompletion();
        subEventDimmerPanel.color = new Color(0f, 0f, 0f, 0f);
        subEventDimmerPanel.raycastTarget = false;
        subEventExitFaded = false;
    }

    // 파라미터 이벤트 초상 스프라이트 결정: 기본은 Resources/Portraits/<IMGName> 우선, 호환 경로 및 정규화 처리
    private Sprite ResolveParameterPortraitSprite(string imgName)
    {
        if (string.IsNullOrWhiteSpace(imgName)) return null;

        // 정규화: 트림, 경로 분리, 확장자 제거
        string cleaned = imgName.Trim();
        string normalized = cleaned.Replace('\\', '/');
        int lastSlash = normalized.LastIndexOf('/');
        string lastSegment = lastSlash >= 0 ? normalized.Substring(lastSlash + 1) : normalized;
        string baseName = Path.GetFileNameWithoutExtension(lastSegment);

        // 1) 현재 배치 경로: Portraits/
        var s = Resources.Load<Sprite>("Portraits/" + baseName);
        if (s != null) return s;

        // 2) 구 규칙 호환: Portrait/
        s = Resources.Load<Sprite>("Portrait/" + baseName);
        if (s != null) return s;

        // 3) 원문 경로 무확장 시도
        string rawNoExt = normalized;
        int dot = rawNoExt.LastIndexOf('.');
        if (dot > 0) rawNoExt = rawNoExt.Substring(0, dot);
        s = Resources.Load<Sprite>(rawNoExt);
        if (s != null) return s;

        Debug.LogWarning($"[UIFlowSimulator] 파라미터 초상 로드 실패: '{imgName}' (시도: Portraits/{baseName}, Portrait/{baseName}, {rawNoExt})");
        return null;
    }

    // 서브이벤트 초상 스프라이트 결정: CharacterImg_ID의 IMGName → Resources/Portraits/<IMGName>
    private Sprite ResolvePortraitSprite(FullSubEventData data)
    {
        string imgName = data?.characterImgData?.IMGName;
        if (!string.IsNullOrEmpty(imgName))
        {
            var s = Resources.Load<Sprite>($"Portraits/{imgName}");
            if (s != null) return s;

            // 예전 규칙(단수 폴더) 호환
            s = Resources.Load<Sprite>($"Portrait/{imgName}");
            if (s != null) return s;

            // 원문 경로가 이미 포함된 경우 대비
            s = Resources.Load<Sprite>(imgName);
            if (s != null) return s;
        }
        return null;
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
            // 널 가드
            var choice = isRightChoice ? currentParameterEventData.rightChoice : currentParameterEventData.leftChoice;
            if (choice == null || choice.condition == null)
            {
                Debug.LogWarning("[UIFlowSimulator] HandleChoice: choice 또는 condition 이 null입니다.");
                return;
            }
            
            // [수정] 새로운 Evaluate 시그니처 호출
            bool success = choice.condition.Evaluate();
            var outcome = success ? choice.successOutcome : choice.failOutcome;

            // [추가] Wille 특성일 때는 정치력 변화가 UI 하이라이트에도 반영되지 않도록 차단
            List<ParameterChange> finalChanges = new List<ParameterChange>(outcome.parameterChanges);

            if (GamePlayerStats.Instance != null && GamePlayerStats.Instance.ActiveTrait == CommanderTrait.Wille)
            {
                finalChanges = finalChanges.Where(c => c.parameterType != ParameterType.정치력).ToList();
            }
            // 현재 활성화된 특성이 '리사드'이고, 선택지에 '확정 성공'이 아닌 판정 조건이 있었을 경우에만 특성 로직을 실행합니다.
            if (GamePlayerStats.Instance.ActiveTrait == CommanderTrait.Risard &&
                !(choice.condition is GuaranteedSuccessCondition))
            {
                // '리사드' 특성 로직을 호출하여 finalChanges 목록에 성공/실패에 따른 보정치를 추가합니다.
                GamePlayerStats.Instance.ActiveCommander?.traitLogic?.ProcessEventOutcome(success, finalChanges);
            }
            // '리사드'가 아니거나 '확정 성공' 이벤트인 경우, 위 if문을 건너뛰고 원래 결과만 사용하게 됩니다.

            GamePlayerStats.Instance.ApplyChanges(finalChanges);

            // [신규] 파라미터 이벤트 완료 기록 (HCW의 PlaythroughHistory는 성공/실패 구분 없음)
            if (PlaythroughHistory.Instance != null)
            {
                PlaythroughHistory.Instance.RecordEventCompletion(currentParameterEventData.id);
            }
            // 파라미터 이벤트는 Ending_Memoriar 같은 플래그가 없으므로 기록하지 않음

            // [신규] 파라미터 이벤트 완료 기록
            DataManager.Instance.PlayerData.completedEventIds.Add(currentParameterEventData.id);

            StartCoroutine(TransitionToNextEvent(outcome.outcomeText));
        }
        else if (currentSubEventData != null)
        {
            Debug.Log($"[UIFlowSimulator] 서브이벤트 선택 처리: isRightChoice={isRightChoice}");
            
            // 서브이벤트 선택지에서 결과 텍스트 가져오기
            var choice = isRightChoice ? currentSubEventData.rightChoice : currentSubEventData.leftChoice;
            string resultText = choice?.outcome?.outcomeText ?? "";
            
            // 파라미터 이벤트와 동일한 타이밍으로 결과 표시 후 다음 이벤트 진행
            StartCoroutine(TransitionToNextSubEvent(resultText, isRightChoice));
        }
        else
        {
            Debug.LogWarning("[UIFlowSimulator] HandleChoice: 현재 이벤트 데이터가 null입니다.");
        }
    }

    private IEnumerator TransitionToNextEvent(string resultText)
    {
        situationCardController.UpdateText(resultText);
        yield return new WaitForSeconds(1.0f);
        
        // 대기 중에도 취소 플래그 체크
        if (cancelTransitions)
        {
            Debug.Log("UIFlowSimulator: 전환이 취소되었습니다.");
            yield break;
        }
        
        // 서브이벤트 체인 진행 중인지 확인
        if (EventManager.Instance.currentState == EventManagerState.InSubEvent)
        {
            Debug.Log("UIFlowSimulator: 서브이벤트 체인이 진행 중이므로 대기합니다.");
            yield break;
        }
        
        uiPanelController.Hide();
        situationCardController.Hide();
        yield return new WaitUntil(() => !situationCardController.gameObject.activeInHierarchy);

        // 게임 오버 상태인지 확인합니다.
        if (IsGameOver())
        {
            Debug.Log("UIFlowSimulator: 게임 오버 상태이므로 다음 턴을 진행하지 않습니다.");
            yield break;
        }

        // 취소 플래그 재확인
        if (cancelTransitions)
        {
            Debug.Log("UIFlowSimulator: 전환이 취소되었습니다.");
            yield break;
        }

        Debug.Log("UIFlowSimulator: 다음 턴을 시작하도록 EventManager에 요청합니다.");
        EventManager.Instance.PlayNextTurn();
    }

    private IEnumerator TransitionToNextSubEvent(string resultText, bool isRightChoice)
    {
        // 서브이벤트는 결과 텍스트 표시 및 대기 없이 즉시 다음으로 진행
        if (cancelTransitions)
        {
            yield break;
        }

        if (IsGameOver())
        {
            yield break;
        }

        EventManager.Instance.OnSubEventChoiceSelected(!isRightChoice);
        yield break;
    }

    private bool IsGameOver()
    {
        if (GamePlayerStats.Instance == null) return false;
        
        // 파라미터 중 하나라도 0 이하이면 게임 오버 상태로 간주
        return GamePlayerStats.Instance.GetStat(ParameterType.정치력) <= 0 ||
               GamePlayerStats.Instance.GetStat(ParameterType.병력) <= 0 ||
               GamePlayerStats.Instance.GetStat(ParameterType.물자) <= 0 ||
               GamePlayerStats.Instance.GetStat(ParameterType.리더십) <= 0;
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

    /// <summary>
    /// [신규] 전투 종료를 시뮬레이션하고 결과를 기록합니다.
    /// </summary>
    /// <param name="battleResultId">기록할 전투 결과 ID</param>
    public void SimulateBattleEnd(int battleResultId)
    {
        var resultData = DataManager.Instance.GetBattleResultDataById(battleResultId);
        if (resultData != null)
        {
            Debug.Log($"전투 결과 시뮬레이션: {resultData.Text_Kr}");
            //DataManager.Instance.RecordBattleResult(battleResultId);
        }
        else
        {
            Debug.LogError($"[UIFlowSimulator] ID {battleResultId}에 해당하는 전투 결과 데이터를 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// [신규] 엔딩을 시뮬레이션하고 결과를 기록합니다.
    /// </summary>
    /// <param name="endingId">기록할 엔딩 ID</param>
    public void SimulateEnding(int endingId)
    {
        if (DataManager.Instance?.FullendingDataDict != null && 
            DataManager.Instance.FullendingDataDict.TryGetValue(endingId, out var endingData))
        {
            Debug.Log($"엔딩 시뮬레이션: {endingData.Text_Kr}");
            Debug.Log($"  - BG_ID: {endingData.bgData?.BG_ID ?? -1}");
            Debug.Log($"  - CutScene_ID: {endingData.cutSceneData?.EndingCutScene_ID ?? -1}");
            Debug.Log($"  - SFX_ID: {endingData.sfxData?.SFX_ID ?? -1}");
        }
        else
        {
            Debug.LogError($"[UIFlowSimulator] ID {endingId}에 해당하는 엔딩 데이터를 FullendingDataDict에서 찾을 수 없습니다.");
        }
    }
}