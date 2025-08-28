using UnityEngine;
using UnityEngine.EventSystems; // Raycast를 위해 다시 추가
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

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

    private float currentRotationAngle = 0f;
    private float targetRotationAngle = 0f;
    private float[] itemBaseAngles;
    private bool isDragging = false;
    private bool isTweening = false;
    public int centerIndex { get; private set; }

    private class CommanderDepthInfo { public Transform transform; public float zPos; }

    // [핵심] InputManager로 '클릭'과 '드래그' 이벤트를 모두 다시 받습니다.
    void OnEnable()
    {
        InputManager.OnDragStart += HandleDragStart;
        InputManager.OnDrag += HandleDrag;
        InputManager.OnDragEnd += HandleDragEnd;
        InputManager.OnClick += HandleClick; // 짧은 클릭 감지를 위해 복원
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

        itemBaseAngles = new float[count];
        float angleStep = 360f / count;

        for (int i = 0; i < count; i++)
        {
            itemBaseAngles[i] = i * angleStep;
            // [수정] CommanderInfo에 Setup을 호출하는 부분은 이제 필요 없습니다.
            // commanderInfos[i].Setup(this);
        }

        centerIndex = 0;
        targetRotationAngle = -itemBaseAngles[centerIndex];
        currentRotationAngle = targetRotationAngle;

        UpdateCommanderPositions();
        UpdateInfoVisibility(centerIndex);
    }

    void Update()
    {
        UpdateCommanderPositions();

        if (!isDragging && !isTweening)
        {
            currentRotationAngle = Mathf.LerpAngle(currentRotationAngle, targetRotationAngle, Time.deltaTime * snapSpeed);
        }

        int currentClosest = FindClosestCommanderToCenter();
        UpdateInfoVisibility(currentClosest);
    }

    // --- 입력 처리 함수들 ---

    // [복원 및 수정] 짧은 클릭을 처리하는 함수
    private void HandleClick(Vector2 clickPosition)
    {
        Debug.Log("--- HandleClick Start ---");

        // --- 추가된 안전장치 ---
        // 클릭 이벤트가 수신되면, 이전에 끝내지 못한 드래그 상태를 강제로 종료시킵니다.
        // 이는 InputManager가 micro-drag 후 OnDragEnd를 호출하지 않는 문제에 대한 방어 코드입니다.
        if (isDragging)
        {
            Debug.LogWarning("isDragging was 'true' at the start of a click. Forcing it to 'false'.");
            isDragging = false;
        }
        // --- 안전장치 끝 ---

        if (isTweening)
        {
            Debug.Log("Action skipped: Tweening is in progress.");
            return;
        }

        PointerEventData pointerData = new PointerEventData(EventSystem.current) { position = clickPosition };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        if (results.Count == 0)
        {
            Debug.LogWarning("Raycast did not hit ANY UI elements. Check 'Raycast Target' on your images.");
        }
        else
        {
            Debug.Log($"Raycast hit {results.Count} object(s). Checking for a match...");
            bool foundMatch = false;
            foreach (var result in results)
            {
                Debug.Log("-> Hit object: " + result.gameObject.name);

                var info = commanderInfos.FirstOrDefault(c => c.transform == result.gameObject.transform);
                if (info != null)
                {
                    Debug.Log($"SUCCESS! Found matching commander '{info.gameObject.name}'. Calling OnCommanderClicked.");
                    OnCommanderClicked(info.transform);
                    foundMatch = true;
                    break;
                }
            }

            if (!foundMatch)
            {
                Debug.LogWarning("Raycast hit UI, but none of the hit objects matched the commander list.");
            }
        }
        Debug.Log("--- HandleClick End ---");
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
            // 4. 원래 있던 기능: 중앙 카드를 클릭했다면 로그를 표시합니다.
            Debug.Log($"{clickedButton.name}의 능력치 또는 정보를 표시합니다.");
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
}