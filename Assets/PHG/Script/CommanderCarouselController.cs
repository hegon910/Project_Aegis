using UnityEngine;
using UnityEngine.EventSystems; // Raycast를 위해 다시 추가
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class CommanderCarouselController : MonoBehaviour
{
    [Header("UI Elements")]
    public List<CommanderInfo> commanderInfos;

    [Header("Rotation Settings")]
    public float radius = 500f;
    public float dragSensitivity = 0.2f;
    public float snapSpeed = 10f;
    [Tooltip("클릭 시 스와이프 애니메이션 지속 시간")]
    public float swipeAnimationDuration = 0.4f;

    [Header("Scale Settings")]
    public Vector3 centerScale = new Vector3(1.2f, 1.2f, 1f);
    public Vector3 sideScale = new Vector3(0.8f, 0.8f, 1f);

    [Header("Popup UI")]
    [Tooltip("선택 불가 팝업 패널 (CanvasGroup 컴포넌트 필요)")]
    public CanvasGroup cantSelectPopup;
    [Tooltip("팝업이 표시될 시간(초)")]
    public float popupDuration = 0.5f;

    [Header("Parameter Preview UI")]
    [Tooltip("각 파라미터의 예상 수치를 표시할 UI 슬라이더 또는 텍스트")]
    public Slider politicsPreviewText;
    public Slider troopsPreviewText;
    public Slider suppliesPreviewText;
    public Slider leadershipPreviewText;
    [Tooltip("잠금 상태에 따라 활성화/비활성화될 시작 버튼")]
    public Button startButton;
    [Tooltip("모든 카드들이 공유하는 스토리 버튼")]
    public Button storyButton;

    private float currentRotationAngle = 0f;
    private float targetRotationAngle = 0f;
    private float[] itemBaseAngles;
    private bool isDragging = false;
    private bool isTweening = false;
    private int clickCount = 0;
    private float lastClickTime = 0f;
    private const float tripleClickThreshold = 0.4f;
    public int centerIndex { get; private set; }
    private Sequence popupSequence;

    private class CommanderDepthInfo { public Transform transform; public float zPos; }

    // [핵심] InputManager로 '클릭'과 '드래그' 이벤트를 모두 다시 받습니다.
    void OnEnable()
    {
        InputManager.OnDragStart += HandleDragStart;
        InputManager.OnDrag += HandleDrag;
        InputManager.OnDragEnd += HandleDragEnd;
        InputManager.OnClick += HandleClick; // 짧은 클릭 감지를 위해 복원

        // 패널이 다시 활성화될 때, 해금 상태/버튼 상태를 즉시 반영
        if (commanderInfos != null && commanderInfos.Count > 0)
        {
            try
            {
                UpdateAllLockOverlays();
                UpdateCenterCardState();
            }
            catch { }
        }
    }

    void OnDisable()
    {
        InputManager.OnDragStart -= HandleDragStart;
        InputManager.OnDrag -= HandleDrag;
        InputManager.OnDragEnd -= HandleDragEnd;
        InputManager.OnClick -= HandleClick;
    }

    void Start()
    {
        int count = commanderInfos.Count;
        if (count == 0) return;

        // 1. 코어 데이터 설정 (가장 먼저 실행)
        itemBaseAngles = new float[count];
        float angleStep = 360f / count;
        for (int i = 0; i < count; i++)
        {
            commanderInfos[i].Setup(this);
            itemBaseAngles[i] = i * angleStep;
        }

        // 2. Invoke를 사용하여 0.1초 뒤에 InitializeUI 함수를 '단 한번' 실행하도록 예약합니다.
        Invoke("InitializeUI", 0.1f);
    }

    /// <summary>
    /// Invoke로 호출될 UI 초기화 전용 함수
    /// </summary>
    void InitializeUI()
    {
        // 1. 캐러셀의 '초기 상태'를 정의합니다.
        centerIndex = 0;
        targetRotationAngle = -itemBaseAngles[centerIndex];
        currentRotationAngle = targetRotationAngle;

        // 2. 정의된 '초기 상태'를 바탕으로 모든 UI를 업데이트합니다.
        UpdateCommanderPositions();
        UpdateAllLockOverlays();
        UpdateParameterPreview(commanderInfos[centerIndex]);
        UpdateInfoVisibility(centerIndex);
        UpdateCenterCardState();
    }

    public void UpdateLockStatus()
    {
        foreach (var info in commanderInfos)
        {
            // UnlockManager에게 해당 지휘관의 enum 값으로 잠금 해제 여부를 물어봅니다.
            bool isUnlocked = UnlockManager.IsUnlocked(info.traitEnum);

            // 잠금 오버레이 UI를 켜거나 끕니다.
            if (info.lockOverlay != null)
            {
                info.lockOverlay.SetActive(!isUnlocked);
            }

            // 버튼 자체의 상호작용도 제어할 수 있습니다.
            // info.GetComponent<Button>().interactable = isUnlocked;
        }
    }

    void Update()
    {
        UpdateCommanderPositions();

        if (!isDragging && !isTweening)
        {
            currentRotationAngle = Mathf.LerpAngle(currentRotationAngle, targetRotationAngle, Time.deltaTime * snapSpeed);
        }

        int currentClosest = FindClosestCommanderToCenter();
        if (centerIndex != currentClosest)
        {
            centerIndex = currentClosest;
            UpdateInfoVisibility(centerIndex);
            UpdateParameterPreview(commanderInfos[centerIndex]);
            UpdateCenterCardState(); // 버튼 상태 업데이트
        }

        if (Input.GetKeyDown(KeyCode.U))
        {
            UnlockCenterCommander();
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            LockCenterCommander();
        }
    

    }
    public void UpdateAllLockOverlays()
    {
        foreach (var info in commanderInfos)
        {
            bool isUnlocked = UnlockManager.IsUnlocked(info.traitEnum);
            if (info.lockOverlay != null)
            {
                info.lockOverlay.SetActive(!isUnlocked);
            }
        }
    }
    private void UnlockCenterCommander()
    {
        CommanderInfo centerCommander = commanderInfos[centerIndex];
        if (!UnlockManager.IsUnlocked(centerCommander.traitEnum))
        {
            UnlockManager.Unlock(centerCommander.traitEnum);
            RefreshUIState(); // UI 즉시 새로고침
        }
    }
    private void LockCenterCommander()
    {
        CommanderInfo centerCommander = commanderInfos[centerIndex];
        if (UnlockManager.IsUnlocked(centerCommander.traitEnum))
        {
            UnlockManager.Lock(centerCommander.traitEnum);
            RefreshUIState(); // UI 즉시 새로고침
        }

    }
    private void ToggleCenterCommanderLockState()
    {
        // 1. 현재 중앙에 있는 사령관 정보를 가져옵니다.
        CommanderInfo centerCommander = commanderInfos[centerIndex];

        // 2. 해당 사령관이 현재 잠금 해제되어 있는지 확인합니다.
        bool isCurrentlyUnlocked = UnlockManager.IsUnlocked(centerCommander.traitEnum);

        // 3. 현재 상태에 따라 적절한 함수를 호출합니다.
        if (isCurrentlyUnlocked)
        {
            // 이미 잠금 해제 상태라면 -> 잠급니다.
            LockCenterCommander();
            Debug.Log($"'{centerCommander.name}' Commander Locked by Toggle.");
        }
        else
        {
            // 잠겨있는 상태라면 -> 잠금 해제합니다.
            UnlockCenterCommander();
            Debug.Log($"'{centerCommander.name}' Commander Unlocked by Toggle.");
        }
    }
    public void RefreshUIState()
    {
        UpdateAllLockOverlays(); // 모든 카드의 자물쇠 아이콘 업데이트
        UpdateCenterCardState(); // 시작 버튼 상태 업데이트
        if (commanderInfos.Count > centerIndex)
        {
            commanderInfos[centerIndex].ShowInfo();
        }
    }

    void UpdateCenterCardState()
    {
        if (startButton == null || storyButton == null) return;



        // 중앙 카드의 잠금 해제 여부를 확인합니다.
        bool isCenterUnlocked = UnlockManager.IsUnlocked(commanderInfos[centerIndex].traitEnum);
        startButton.interactable = isCenterUnlocked;
        storyButton.gameObject.SetActive(isCenterUnlocked);


        // 버튼의 상호작용 가능 여부를 설정합니다.
        startButton.interactable = isCenterUnlocked;
        //만약 잠겨있다면, 팝업을 자동으로 표시합니다.
        if (isCenterUnlocked)
        {
            // [해금 상태]라면, 팝업을 강제로 즉시 숨깁니다.
            HideCantSelectPopup();
        }
        else
        {
            // [잠금 상태]라면, 팝업을 표시합니다.
            ShowCantSelectPopup();
        }
    }

    void UpdateParameterPreview(CommanderInfo commander)
    {
        // 1. 계산을 위한 임시 Dictionary를 만듭니다.
        Dictionary<ParameterType, int> previewStats = new Dictionary<ParameterType, int>
        {
            { ParameterType.정치력, 50 },
            { ParameterType.병력, 50 },
            { ParameterType.물자, 50 },
            { ParameterType.리더십, 50 },
            { ParameterType.전황, 50 },
            { ParameterType.카르마, 50 }
        };

        // 2. 해당 지휘관의 초기 보정값을 임시 Dictionary에 적용합니다.
        foreach (var change in commander.initialStatAdjustments)
        {
            if (previewStats.ContainsKey(change.parameterType))
            {
                previewStats[change.parameterType] += change.valueChange;
            }
        }

        // 3. 계산이 끝난 값으로 실제 UI 텍스트를 업데이트합니다.
        if (politicsPreviewText != null) politicsPreviewText.value = previewStats[ParameterType.정치력];
        if (troopsPreviewText != null) troopsPreviewText.value = previewStats[ParameterType.병력];
        if (suppliesPreviewText != null) suppliesPreviewText.value = previewStats[ParameterType.물자];
        if (leadershipPreviewText != null) leadershipPreviewText.value = previewStats[ParameterType.리더십];
        // ... (모든 파라미터 UI 업데이트) ...
    }
    // --- 입력 처리 함수들 ---


    private void HandleClick(Vector2 clickPosition)
    {
        if (isTweening || isDragging) return;

        PointerEventData pointerData = new PointerEventData(EventSystem.current) { position = clickPosition };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        if (results.Count > 0)
        {
            var info = commanderInfos.FirstOrDefault(c => c.transform == results[0].gameObject.transform);
            if (info != null)
            {
                // 클릭된 카드가 무엇이든 OnCommanderClicked에게 넘겨서 처리하도록 합니다.
                OnCommanderClicked(info.transform);

                // 중앙이 아닌 카드를 클릭하면 clickCount를 초기화해야 합니다.
                // OnCommanderClicked 내부에서 이미 centerIndex가 아니면 clickCount를 초기화하지 않으므로, 여기서 처리해줍니다.
                if (commanderInfos.IndexOf(info) != centerIndex)
                {
                    clickCount = 0;
                }
            }
        }
    }

    public void HandleDragStart(Vector2 dragStartPosition)
    {
        DOTween.Kill(this);
        isTweening = false;
        isDragging = true;
    }

    public void HandleDrag(Vector2 dragDelta)
    {
        currentRotationAngle += dragDelta.x * dragSensitivity;
    }

    public void HandleDragEnd()
    {
        isDragging = false;
        centerIndex = FindClosestCommanderToCenter();
        targetRotationAngle = -itemBaseAngles[centerIndex];
    }

    public void OnCommanderClicked(Transform clickedButton)
    {
        // 애니메이션이 진행 중일 때는 다른 클릭을 무시합니다.
        if (isTweening) return;

        // 1. 클릭된 버튼의 인덱스를 찾습니다.
        int clickedIndex = commanderInfos.FindIndex(info => info.transform == clickedButton);

        // 예외 처리
        if (clickedIndex == -1) return;

        // 2. 만약 클릭된 카드가 중앙이 아니라면
        if (clickedIndex != centerIndex)
        {
            // 3. DOTween을 사용하여 클릭된 카드를 중앙으로 이동시키는 애니메이션을 실행합니다.
            isTweening = true; // 애니메이션 시작 플래그
            DOTween.Kill(this); // 기존에 실행 중이던 Tween이 있다면 중지

            float newTargetAngle = -itemBaseAngles[clickedIndex];

            // 현재 각도에서 목표 각도까지 가장 짧은 경로의 최종 각도를 계산합니다.
            // 예를 들어 350도에서 10도로 갈 때, -340도가 아닌 +20도만 움직이게 합니다.
            float finalAngle = currentRotationAngle + Mathf.DeltaAngle(currentRotationAngle, newTargetAngle);

            DOTween.To(() => currentRotationAngle,      // 애니메이션 대상 값 (현재 회전 각도)
                       x => currentRotationAngle = x,   // 값을 변경할 액션
                       finalAngle,                      // 최종 목표 각도
                       swipeAnimationDuration)          // 애니메이션 지속 시간
                   .SetEase(Ease.OutQuad)               // 부드러운 감속 효과 (원하는 Ease로 변경 가능)
                   .SetTarget(this)                     // Tween의 주체를 현재 객체로 설정하여 관리 용이
                   .OnComplete(() =>                   // 애니메이션 완료 후 실행할 작업
                   {
                       isTweening = false; // 애니메이션 종료 플래그
                       centerIndex = clickedIndex; // 중앙 인덱스를 최종적으로 업데이트
                       targetRotationAngle = newTargetAngle; // 목표 각도 업데이트

                       // 장시간 사용 시 각도 값이 무한정 커지거나 작아지는 것을 방지
                       currentRotationAngle = Mathf.Repeat(targetRotationAngle, 360f);
                   });
        }
        else
        {
            if (Time.time - lastClickTime > tripleClickThreshold)
            {
                clickCount = 1;
            }
            else
            {
                clickCount++;
            }
            lastClickTime = Time.time;

            if (clickCount >= 3)
            {
                ToggleCenterCommanderLockState();
                clickCount = 0; // 카운트 초기화
            }
        }
    }

    // --- 이하 유틸리티 함수들 (변경 없음) ---
    private int FindClosestCommanderToCenter()
    {
        float minAngleDiff = float.MaxValue;
        int closestIndex = 0;
        for (int i = 0; i < commanderInfos.Count; i++)
        {
            float angleDifference = Mathf.Abs(Mathf.DeltaAngle(currentRotationAngle, -itemBaseAngles[i]));
            if (angleDifference < minAngleDiff)
            {
                minAngleDiff = angleDifference;
                closestIndex = i;
            }
        }
        return closestIndex;
    }

    private void UpdateInfoVisibility(int visibleIndex)
    {
        for (int i = 0; i < commanderInfos.Count; i++)
        {
            if (i == visibleIndex)
            {
                commanderInfos[i].ShowInfo();
            }
            else
            {
                commanderInfos[i].HideInfo();
            }
        }
    }

    void UpdateCommanderPositions()
    {
        List<CommanderDepthInfo> depthList = new List<CommanderDepthInfo>();
        for (int i = 0; i < commanderInfos.Count; i++)
        {
            float itemAngle = currentRotationAngle + itemBaseAngles[i];
            float angleInRad = itemAngle * Mathf.Deg2Rad;
            float xPos = Mathf.Sin(angleInRad) * radius;
            float zPos = Mathf.Cos(angleInRad);
            var buttonTransform = commanderInfos[i].transform;
            buttonTransform.localPosition = new Vector3(xPos, 0, 0);
            Vector3 scale = Vector3.Lerp(sideScale, centerScale, (zPos + 1f) / 2f);
            buttonTransform.localScale = scale;
            depthList.Add(new CommanderDepthInfo { transform = buttonTransform, zPos = zPos });
        }
        var sortedList = depthList.OrderBy(d => d.zPos).ToList();
        for (int i = 0; i < sortedList.Count; i++)
        {
            sortedList[i].transform.SetSiblingIndex(i);
        }
    }

     public void OnGameStartButtonClicked()
    {
        // 1. 현재 중앙에 있는 지휘관의 CommanderInfo 컴포넌트를 가져옵니다.
        CommanderInfo selectedCommander = commanderInfos[centerIndex];

        // 2. PlayerStats가 존재하는지 확인합니다.
        if (GamePlayerStats.Instance == null)
        {
            Debug.LogError("GamePlayerStats 인스턴스를 찾을 수 없습니다! 게임 시작이 불가능합니다.");
            return;
        }

        // 3. GameManager에 선택된 지휘관 정보를 설정해달라고 요청합니다.
        //    이제 이 안에서 "지휘관 활성화" 로그가 떠야 합니다.
        GameManager.instance.OnCommanderSelected(centerIndex);

        // 4. GameManager에게 실제 게임 시작을 지시합니다. (이 부분은 프로젝트의 GameManager 이름에 맞게 수정)
        //    이후 GameManager가 PlayerStats.InitializeStats()를 호출해야 합니다.
        // GameManager.Instance.StartNewGame(); 
        Debug.Log($"<color=green>{selectedCommander.gameObject.name} 지휘관으로 게임 시작 절차를 진행합니다.</color>");
    }
    private void ShowCantSelectPopup()
    {
        if (cantSelectPopup == null) return;

        // 1. 만약 이전에 실행되던 팝업 시퀀스가 있다면 즉시 중지하고 파괴합니다.
        if (popupSequence != null && popupSequence.IsActive())
        {
            popupSequence.Kill();
        }

        // 2. 팝업 오브젝트를 활성화하고, 새로운 시퀀스를 생성하여 변수에 저장합니다.
        cantSelectPopup.gameObject.SetActive(true);
        popupSequence = DOTween.Sequence();

        // 3. 페이드 인 -> 대기 -> 페이드 아웃 순서로 애니메이션을 구성합니다.
        popupSequence.Append(cantSelectPopup.DOFade(1f, 0.25f))      // 0.25초 동안 페이드 인
                     .AppendInterval(popupDuration)                  // 설정된 시간(0.5초)만큼 대기
                     .Append(cantSelectPopup.DOFade(0f, 0.25f))      // 0.25초 동안 페이드 아웃
                     .OnComplete(() => {
                         // 모든 애니메이션이 끝나면 팝업을 비활성화합니다.
                         cantSelectPopup.gameObject.SetActive(false);
                     });
    }

    private void HideCantSelectPopup()
    {
        if (cantSelectPopup == null) return;

        // 1. 실행 중인 팝업 시퀀스가 있다면 즉시 중지하고 파괴합니다.
        if (popupSequence != null && popupSequence.IsActive())
        {
            popupSequence.Kill();
        }

        // 2. 팝업을 즉시 투명하게 만들고 비활성화합니다.
        cantSelectPopup.alpha = 0f;
        cantSelectPopup.gameObject.SetActive(false);
    }


}