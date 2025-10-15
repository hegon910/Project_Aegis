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

    [Header("배경")]
    [SerializeField] private Image backgroundImage; // BG_ID/Back_ID 기반 배경 스프라이트 적용 대상
    [SerializeField] private SkillPromptOverlay skillPromptOverlay; // 특수 이벤트 스킬 교체 시각화

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

	// 서브/특수 이벤트 전환 중 페이드가 진행 중인지 추적하여 메인 딤머 업데이트를 가드
	private bool isSubEventFading = false;

	// 외부(예: CheatManager)에서 전환 중 여부를 확인하기 위한 읽기 전용 플래그
	public bool IsDuringTransition => isSubEventFading || subEventExitFaded;

	// 특수 이벤트 BGM 상태 추적
	private bool isSpecialEventBGMPlaying = false;
	private int lastSpecialEventBgmId = -1;

    // <<<<<<< [핵심 복원 1] 원본과 같이 OnEnable/OnDisable을 사용한 이벤트 구독으로 되돌립니다.
    private void OnEnable()
    {
        EventManager.OnParameterEventReady += HandleParameterEvent;
        EventManager.OnSubEventReady += HandleSubEvent;
        EventManager.OnSubEventExitFadeRequested += OnSubEventExitFadeRequested;
        SpecialEventManager.OnSpecialEventReady += HandleSpecialEvent;
        SpecialEventManager.OnSpecialEventExitFadeRequested += OnSubEventExitFadeRequested;
        SpecialEventManager.OnSpecialEventChainEnded += OnSpecialEventChainEnded;
        EventManager.OnEventCycleCompleted += OnEventCycleCompleted;
    }

    private void OnDisable()
    {
        EventManager.OnParameterEventReady -= HandleParameterEvent;
        EventManager.OnSubEventReady -= HandleSubEvent;
        EventManager.OnSubEventExitFadeRequested -= OnSubEventExitFadeRequested;
        SpecialEventManager.OnSpecialEventReady -= HandleSpecialEvent;
        SpecialEventManager.OnSpecialEventExitFadeRequested -= OnSubEventExitFadeRequested;
        SpecialEventManager.OnSpecialEventChainEnded -= OnSpecialEventChainEnded;
        EventManager.OnEventCycleCompleted -= OnEventCycleCompleted;
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
        
        // BGM 상태 리셋
        isParameterEventBGMPlaying = false;
        
        if (parameterUIController != null)
        {
            parameterUIController.InitializeAndDisplayStats();
        }

        // UI 초기화
        if (uiPanelController != null) uiPanelController.gameObject.SetActive(false);
        if (situationCardController != null) situationCardController.gameObject.SetActive(false);
        if (dimmerPanel != null)
        {
            dimmerPanel.color = Color.clear;
            // 메인 딤머는 입력 차단 목적이 아니므로 항상 레이캐스트 비활성
            dimmerPanel.raycastTarget = false;
        }
        // 서브 디머는 기본적으로 비활성화하고 알파/레이캐스트 리셋
        if (subEventDimmerPanel != null)
        {
            subEventDimmerPanel.DOKill();
            subEventDimmerPanel.color = new Color(0f, 0f, 0f, 0f);
            subEventDimmerPanel.raycastTarget = false;
            subEventDimmerPanel.gameObject.SetActive(false);
        }

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

        // 파라미터 이벤트 BGM 재생: 서브이벤트에서 막 돌아왔거나 아직 미재생일 때만 재생
        bool cameFromSubEvent = subEventExitFaded;
        if (cameFromSubEvent || !isParameterEventBGMPlaying)
        {
            PlayParameterEventBGM();
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

                // 파라미터 이벤트 배경 적용 (stringData.BG 사용)
                TryApplyBackgroundFromString(stringData.BG);
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
        // 서브이벤트 배경 적용 (BackData 우선, 없으면 BGName 폴백)
        TryApplyBackgroundFromSubEvent(data);

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

    // 특수 이벤트 표시 (서브이벤트와 동일 흐름, 배경/사운드/입력 차단 타이밍 완전 동일화)
    private void HandleSpecialEvent(FullSubEventData data)
    {
        if (data == null)
        {
            Debug.LogError("[UIFlowSimulator] HandleSpecialEvent: data가 null입니다.");
            return;
        }

        if (tutorialPanel != null && tutorialPanel.activeSelf)
        {
            tutorialPanel.SetActive(false);
        }

        // 파라미터 이벤트에서 특수 이벤트로 전환되는지 판별 (서브이벤트와 동일)
        bool cameFromParameterEvent = currentParameterEventData != null;

        // 진행 중 전환 코루틴 정지 및 상태 세팅
        StopAllCoroutines();
        currentParameterEventData = null;
        currentSubEventData = data;

        // 특수 이벤트의 경우 MainCharacter 대신 SpecialEventCharacterData의 설명(이름) 사용을 우선 시도
        string characterName = "";
        var specialName = TryResolveSpecialEventName(data);
        if (!string.IsNullOrEmpty(specialName))
        {
            characterName = specialName;
        }
        else
        {
            characterName = data.characterData != null ? (data.characterData.Chr_Name ?? "") : "";
        }
        string dialogue = data.Text_kr ?? "대화 내용이 없습니다.";
        string leftChoiceText = data.leftChoice?.choiceText ?? "선택지 1";
        string rightChoiceText = data.rightChoice?.choiceText ?? "선택지 2";

        // 배경 적용 (서브이벤트와 동일 규칙)
        TryApplyBackgroundFromSubEvent(data);

		// 특수 이벤트 체인 시작 시 BGM 추적 상태 초기화
		if (cameFromParameterEvent)
		{
			isSpecialEventBGMPlaying = false;
			lastSpecialEventBgmId = -1;
		}

        // 사운드 처리: 파라미터 → 특수 전환 시 기존 BGM 즉시 정지 후, 특수 이벤트 BGM/SFX 재생
        if (cameFromParameterEvent && AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBGM();
        }
        PlaySpecialEventAudio(data);

        if (cameFromParameterEvent && subEventDimmerPanel != null)
        {
            // 서브이벤트와 동일한 페이드-투-블랙 후 표시, 이 동안 입력 차단
            StartCoroutine(FadeToBlackThenDisplaySubEvent(characterName, dialogue, leftChoiceText, rightChoiceText));
            return;
        }

        // 바로 표시 (서브에서 특수로 이어질 경우 등)
        DisplayEventUI(
            characterSprite: ResolvePortraitSprite(data),
            characterName: characterName,
            dialogue: dialogue,
            leftChoice: leftChoiceText,
            rightChoice: rightChoiceText
        );

        // 특수 이벤트가 스킬 교체 프롬프트일 경우, 기존/신규 스킬 아이콘을 시각화
        if (SpecialEventManager.Instance != null && data.ID == -999999 && skillPromptOverlay != null)
        {
			var player = GameManager.instance != null ? FindObjectOfType<WarPlayer>() : null;
			// 오버레이 진입 직전에 항상 최신 PlayerPrefs 값을 반영하여 현재 스킬 동기화
			if (player != null)
			{
				player.LoadSkillFromID();
			}
			Sprite currentIcon = player?.currentSkill?.icon;
			string currentName = player?.currentSkill?.skillName ?? "";
			// [보강] 전투 씬이 아니거나 player.currentSkill이 비어 있을 때 PlayerPrefs 기반으로 현재 스킬 표시
			if ((player == null || player.currentSkill == null))
			{
				string prefId = PlayerPrefs.HasKey("EquippedSkillID") ? PlayerPrefs.GetString("EquippedSkillID") : null;
				if (!string.IsNullOrEmpty(prefId))
				{
					SkillDatabase db = player != null ? player.skillDatabase : null;
					if (db == null)
					{
						var allDbs = Resources.FindObjectsOfTypeAll<SkillDatabase>();
						if (allDbs != null && allDbs.Length > 0) db = allDbs[0];
					}
					if (db != null)
					{
						var curSkill = db.GetSkillByID(prefId);
						if (curSkill != null)
						{
							currentIcon = curSkill.icon;
							currentName = !string.IsNullOrEmpty(curSkill.skillName) ? curSkill.skillName : prefId;
						}
						else
						{
							currentName = prefId;
						}
					}
					else
					{
						currentName = prefId;
					}
				}
			}

            string newSkillId = SpecialEventManager.Instance.GetPendingSkillId();
            Sprite newIcon = null; string newName = "";
            // 매니저에 대기 중 에셋이 있으면 우선 사용
            var pendingAsset = SpecialEventManager.Instance.GetPendingSkillData();
            if (pendingAsset != null)
            {
                newIcon = pendingAsset.icon;
                newName = !string.IsNullOrEmpty(pendingAsset.skillName) ? pendingAsset.skillName : newSkillId;
            }
            else if (player != null && player.skillDatabase != null && !string.IsNullOrEmpty(newSkillId))
            {
                var newSkill = player.skillDatabase.GetSkillByID(newSkillId);
                newIcon = newSkill?.icon;
                newName = newSkill?.skillName ?? newSkillId;
            }

            bool hasCurrent = player != null && player.currentSkill != null;
            string title = hasCurrent ? "스킬을 교체한다." : "스킬을 획득한다.";
            skillPromptOverlay.Show(currentIcon, currentName, newIcon, newName, title);
			// 특수 이벤트 스킬 패널이 떴을 때 상황 텍스트는 공란 처리하여 중복 노출 방지
			if (situationCardController != null)
			{
				situationCardController.UpdateText("");
			}
        }
    }

	// 특수 이벤트용 BGM/SFX 재생 (EventManager의 서브이벤트 로직과 동등한 규칙 적용)
    private void PlaySpecialEventAudio(FullSubEventData eventData)
    {
        if (eventData == null || AudioManager.Instance == null) return;

		// BGM: 체인 동안 동일한 트랙이면 재시작하지 않음
		int bgId = (eventData.bgData != null) ? eventData.bgData.BG_ID : 0;
		if (bgId != 0)
		{
			if (!isSpecialEventBGMPlaying || lastSpecialEventBgmId != bgId)
			{
				AudioManager.Instance.PlayBGMByID(bgId);
				isSpecialEventBGMPlaying = true;
				lastSpecialEventBgmId = bgId;
			}
		}

        // SFX
        if (eventData.sfxData != null && eventData.sfxData.SFX_ID != 0)
        {
            AudioManager.Instance.PlaySFXByID(eventData.sfxData.SFX_ID);
        }
    }

    private IEnumerator FadeToBlackThenDisplaySubEvent(string characterName, string dialogue, string leftChoiceText, string rightChoiceText)
    {
        // 시작 상태 초기화 및 입력 차단 (서브이벤트 전용 디머 사용)
        isSubEventFading = true;
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
        subEventDimmerPanel.gameObject.SetActive(false);
        isSubEventFading = false;
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

        isSubEventFading = true;
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
            isSubEventFading = false;
            yield break;
        }
        // 페이드 인
        yield return subEventDimmerPanel.DOFade(0f, 0.5f).SetUpdate(false).WaitForCompletion();
        subEventDimmerPanel.color = new Color(0f, 0f, 0f, 0f);
        subEventDimmerPanel.raycastTarget = false;
        subEventDimmerPanel.gameObject.SetActive(false);
        subEventExitFaded = false;
        isSubEventFading = false;
    }

	private void OnSpecialEventChainEnded()
	{
		// 특수 이벤트 종료 시 BGM 정지 및 상태 리셋
		if (AudioManager.Instance != null)
		{
			AudioManager.Instance.StopBGM();
		}
		isSpecialEventBGMPlaying = false;
		lastSpecialEventBgmId = -1;

		// 특수 이벤트 종료 후 다음 일반 이벤트로 복귀
		StartCoroutine(Co_ResumeAfterSpecialEvent());
	}

    private void OnEventCycleCompleted()
    {
        // 특수 이벤트 우선 처리 로직은 GameManager에서 수행. 여기서는 개입하지 않음.
    }

    private IEnumerator Co_ResumeAfterSpecialEvent()
    {
        if (skillPromptOverlay != null) skillPromptOverlay.Hide();
        // 안전: 특수 이벤트 체인 종료 시 서브 디머가 남아 있으면 즉시 해제
        if (subEventDimmerPanel != null)
        {
            subEventDimmerPanel.DOKill();
            subEventDimmerPanel.color = new Color(0f, 0f, 0f, 0f);
            subEventDimmerPanel.raycastTarget = false;
            subEventDimmerPanel.gameObject.SetActive(false);
        }
        subEventExitFaded = false;
        // 페이드 인 완료 대기 (SubEventExit 페이드가 0.5s로 설계되어 있음)
        yield return new WaitForSeconds(0.1f);
        if (EventManager.Instance != null)
        {
            EventManager.Instance.PlayNextTurn();
        }
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
        // 특수 이벤트 캐릭터 IMGName가 있으면 우선 사용
        string imgName = null;
        if (data != null && DataManager.Instance != null)
        {
            if (TryGetSpecialImgName(data, out var specialImg))
            {
                imgName = specialImg;
            }
            else
            {
                imgName = data.characterImgData?.IMGName;
            }
        }
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

    private bool TryGetSpecialImgName(FullSubEventData data, out string imgName)
    {
        imgName = null;
        if (DataManager.Instance == null || data == null) return false;
        // CharacterImg_ID를 특수 캐릭터 사전에서 조회
        var dictField = typeof(DataManager).GetField("specialEventCharacterDict", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (dictField == null) return false;
        var dict = dictField.GetValue(DataManager.Instance) as System.Collections.Generic.Dictionary<int, SpecialEventCharacterData>;
        if (dict == null) return false;
        if (dict.TryGetValue(data?.characterImgData?.CharacterImg_ID ?? 0, out var entry) && entry != null)
        {
            imgName = entry.IMGName;
            return !string.IsNullOrEmpty(imgName);
        }
        return false;
    }

    private string TryResolveSpecialEventName(FullSubEventData data)
    {
        if (DataManager.Instance == null || data == null) return null;
        var dictField = typeof(DataManager).GetField("specialEventCharacterDict", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (dictField == null) return null;
        var dict = dictField.GetValue(DataManager.Instance) as System.Collections.Generic.Dictionary<int, SpecialEventCharacterData>;
        if (dict == null) return null;
        if (dict.TryGetValue(data?.characterImgData?.CharacterImg_ID ?? 0, out var entry) && entry != null)
        {
            // 설명 칼럼은 "크리스티안 노멀" 처럼 감정이 붙을 수 있으므로 이름만 추출
            var exp = entry.Explanation ?? string.Empty;
            var nameOnly = exp.Replace(" 노멀", string.Empty)
                              .Replace(" 놀람", string.Empty)
                              .Replace(" 분노", string.Empty)
                              .Replace(" 고민", string.Empty)
                              .Replace(" 웃음", string.Empty)
                              .Replace(" 울상", string.Empty)
                              .Replace(" 비열", string.Empty);
            nameOnly = nameOnly.Trim();
            return string.IsNullOrEmpty(nameOnly) ? null : nameOnly;
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

    // 배경 스프라이트 적용 유틸리티들
    private void TryApplyBackgroundFromString(string bgString)
    {
        if (backgroundImage == null) return;
        if (string.IsNullOrWhiteSpace(bgString)) return;

        var sprite = ResolveBackgroundSpriteByName(bgString);
        if (sprite != null)
        {
            SetBackgroundSprite(sprite);
        }
        else
        {
            // 파라미터 이벤트 전용 폴백: Base
            var baseSprite = ResolveBackgroundSpriteByName("Base");
            if (baseSprite != null)
            {
                SetBackgroundSprite(baseSprite);
            }
            else
            {
                Debug.LogWarning($"[UIFlowSimulator] 배경 스프라이트 로드 실패: '{bgString}', 폴백(Base)도 없음");
            }
        }
    }

    private void TryApplyBackgroundFromSubEvent(FullSubEventData data)
    {
        if (backgroundImage == null || data == null) return;

        // BackData 우선
        string backName = data.backData?.IMGName;
        if (!string.IsNullOrEmpty(backName))
        {
            var s = ResolveBackgroundSpriteByName(backName);
            if (s != null)
            {
                SetBackgroundSprite(s);
                return;
            }
        }

        // 폴백: BGName 사용
        string bgName = data.bgData?.BGName;
        if (!string.IsNullOrEmpty(bgName))
        {
            var s = ResolveBackgroundSpriteByName(bgName);
            if (s != null)
            {
                SetBackgroundSprite(s);
                return;
            }
        }

        // 서브이벤트 전용 최종 폴백: Base
        var baseSprite = ResolveBackgroundSpriteByName("Base");
        if (baseSprite != null)
        {
            SetBackgroundSprite(baseSprite);
        }
    }

    private void SetBackgroundSprite(Sprite sprite)
    {
        backgroundImage.sprite = sprite;
    }

    private Sprite ResolveBackgroundSpriteByName(string nameOrPath)
    {
        if (string.IsNullOrWhiteSpace(nameOrPath)) return null;

        string cleaned = nameOrPath.Trim();
        string normalized = cleaned.Replace('\\', '/');
        int lastSlash = normalized.LastIndexOf('/');
        string lastSegment = lastSlash >= 0 ? normalized.Substring(lastSlash + 1) : normalized;
        string baseName = Path.GetFileNameWithoutExtension(lastSegment);

        // 시도 1) Backgrounds/
        var s = Resources.Load<Sprite>("Backgrounds/" + baseName);
        if (s != null) return s;

        // 시도 2) Back/
        s = Resources.Load<Sprite>("Back/" + baseName);
        if (s != null) return s;

        // 시도 3) 원문 경로
        string rawNoExt = normalized;
        int dot = rawNoExt.LastIndexOf('.');
        if (dot > 0) rawNoExt = rawNoExt.Substring(0, dot);
        s = Resources.Load<Sprite>(rawNoExt);
        if (s != null) return s;

        return null;
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
            // 파라미터 변화 발생: 특수 이벤트 트리거 윈도우를 엽니다.
            if (SpecialEventManager.Instance != null)
            {
                SpecialEventManager.Instance.OpenTriggerWindow();
            }

            // [신규] 파라미터 이벤트 완료 기록 (HCW의 PlaythroughHistory는 성공/실패 구분 없음)
            if (PlaythroughHistory.Instance != null)
            {
                PlaythroughHistory.Instance.RecordEventCompletion(currentParameterEventData.id);
            }
            // 파라미터 이벤트는 Ending_Memoriar 같은 플래그가 없으므로 기록하지 않음

            // [신규] 파라미터 이벤트 완료 기록
            DataManager.Instance.PlayerData.completedEventIds.Add(currentParameterEventData.id);

            // 다음 이벤트가 파라미터 이벤트가 아닐 때만 BGM 페이드 아웃
            if (IsNextEventParameterEvent())
            {
                // 다음 이벤트도 파라미터 이벤트이므로 BGM 페이드 아웃 없이 전환
                StartCoroutine(TransitionToNextEvent(outcome.outcomeText));
            }
            else
            {
            // 다음 이벤트가 서브이벤트이므로 BGM 페이드 아웃 후 전환 (시각적 페이드 유지)
            StartCoroutine(FadeOutBGMAndTransition(outcome.outcomeText));
            }
        }
        else if (currentSubEventData != null)
        {
            Debug.Log($"[UIFlowSimulator] 서브이벤트 선택 처리: isRightChoice={isRightChoice}");
            
            // 서브이벤트 선택지에서 결과 텍스트 가져오기
            var choice = isRightChoice ? currentSubEventData.rightChoice : currentSubEventData.leftChoice;
            string resultText = choice?.outcome?.outcomeText ?? "";
            
            // 서브/특수 이벤트 모두 동일한 즉시 전환 코루틴을 사용
            StartCoroutine(TransitionToNextSubEvent(resultText, isRightChoice));
        }
        else
        {
            Debug.LogWarning("[UIFlowSimulator] HandleChoice: 현재 이벤트 데이터가 null입니다.");
        }
    }

    // 파라미터 이벤트 BGM 재생 상태 추적
    private bool isParameterEventBGMPlaying = false;
    
    /// <summary>
    /// 파라미터 이벤트용 BGM을 재생합니다 (CommandCenter).
    /// </summary>
    private void PlayParameterEventBGM()
    {
        if (AudioManager.Instance != null)
        {
            // 항상 기존 BGM을 즉시 중지하고 CommandCenter로 전환
            AudioManager.Instance.StopBGM();
            
            AudioClip commandCenterClip = Resources.Load<AudioClip>("Audio/BGM/CommandCenter");
            if (commandCenterClip != null)
            {
                AudioManager.Instance.PlayBGM(commandCenterClip);
                isParameterEventBGMPlaying = true;
                Debug.Log("[UIFlowSimulator] 파라미터 이벤트 BGM 재생: CommandCenter (강제 전환)");
            }
            else
            {
                Debug.LogError("[UIFlowSimulator] CommandCenter BGM 파일을 찾을 수 없습니다.");
            }
        }
    }

    /// <summary>
    /// 다음 이벤트가 파라미터 이벤트인지 확인합니다.
    /// </summary>
    /// <returns>다음 이벤트가 파라미터 이벤트이면 true</returns>
    private bool IsNextEventParameterEvent()
    {
        if (EventManager.Instance == null || DataManager.Instance == null) return false;
        
        // 현재 플레이리스트 인덱스가 범위를 벗어나면 false
        if (DataManager.Instance.PlayerData.eventPlaylistIndex >= DataManager.Instance.PlayerData.currentPlaylist.Count)
            return false;
        
        int nextEventId = DataManager.Instance.PlayerData.currentPlaylist[DataManager.Instance.PlayerData.eventPlaylistIndex];
        
        // 서브이벤트인지 확인 (서브이벤트가 아니면 파라미터 이벤트)
        bool isSubEvent = DataManager.Instance.FullSubEvents != null && 
                         DataManager.Instance.FullSubEvents.Any(e => e.ID == nextEventId);
        
        return !isSubEvent; // 서브이벤트가 아니면 파라미터 이벤트
    }

    /// <summary>
    /// BGM을 페이드 아웃시키고 다음 이벤트로 전환합니다.
    /// </summary>
    /// <param name="outcomeText">결과 텍스트</param>
    private IEnumerator FadeOutBGMAndTransition(string outcomeText)
    {
        // BGM 페이드 아웃 시작
        if (AudioManager.Instance != null)
        {
            StartCoroutine(AudioManager.Instance.FadeOutBGM(0.5f)); // 0.5초 동안 페이드 아웃
        }

        // 페이드 아웃과 동시에 결과 텍스트 표시
        yield return StartCoroutine(TransitionToNextEvent(outcomeText));
    }

    /// <summary>
    /// BGM을 페이드 아웃시키고 서브이벤트로 전환합니다.
    /// </summary>
    /// <param name="resultText">결과 텍스트</param>
    /// <param name="isRightChoice">오른쪽 선택지 여부</param>
    private IEnumerator FadeOutBGMAndTransitionToSubEvent(string resultText, bool isRightChoice)
    {
        // BGM 페이드 아웃 시작
        if (AudioManager.Instance != null)
        {
            StartCoroutine(AudioManager.Instance.FadeOutBGM(0.5f)); // 0.5초 동안 페이드 아웃
        }

        // 페이드 아웃과 동시에 결과 텍스트 표시
        yield return StartCoroutine(TransitionToNextSubEvent(resultText, isRightChoice));
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

        // 특수 이벤트 체인 진행 중이면 카드가 사라지는 연출을 기다리지 않고 바로 다음 이벤트로 라우팅
        if (SpecialEventManager.Instance != null && SpecialEventManager.Instance.IsInSpecialChain)
        {
            if (skillPromptOverlay != null) skillPromptOverlay.Hide();
            // SpecialEventManager는 isLeftChoice 시그니처를 사용하므로 반전 전달
            SpecialEventManager.Instance.OnSubEventChoiceSelected(!isRightChoice);
        }
        else
        {
            // EventManager는 isLeftChoice 시그니처를 사용하므로 반전 전달
            EventManager.Instance.OnSubEventChoiceSelected(!isRightChoice);
        }
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
    public void UpdateDimmer(float alpha)
    {
        // 서브/특수 이벤트 전환 페이드 중에는 메인 딤머를 업데이트하지 않음
        if (isSubEventFading) return;
        if (dimmerPanel != null)
        {
            dimmerPanel.DOKill();
            dimmerPanel.color = new Color(0, 0, 0, alpha);
            // 메인 딤머는 항상 레이캐스트 비활성로 유지하여 입력 차단을 유발하지 않도록 함
            if (dimmerPanel.raycastTarget)
            {
                dimmerPanel.raycastTarget = false;
            }
        }
    }

    // 어디서든 즉시 딤머 상태를 정상화하기 위한 강제 초기화 API (치트/강제 스킵 대비)
    public void ForceClearAllDimmers()
    {
        // 메인 딤머 초기화
        if (dimmerPanel != null)
        {
            dimmerPanel.DOKill();
            dimmerPanel.color = Color.clear;
            dimmerPanel.raycastTarget = false;
            if (!dimmerPanel.gameObject.activeSelf) dimmerPanel.gameObject.SetActive(true);
        }

        // 서브/특수 전환용 딤머 초기화
        if (subEventDimmerPanel != null)
        {
            subEventDimmerPanel.DOKill();
            subEventDimmerPanel.color = new Color(0f, 0f, 0f, 0f);
            subEventDimmerPanel.raycastTarget = false;
            subEventDimmerPanel.gameObject.SetActive(false);
        }
        subEventExitFaded = false;
        isSubEventFading = false;
    }
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