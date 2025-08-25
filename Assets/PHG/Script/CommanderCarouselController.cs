using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

public class CommanderCarouselController : MonoBehaviour
{
    [Header("UI Elements")]
    public List<Transform> commanderButtons;

    [Header("Rotation Settings")]
    public float radius = 500f;
    public float dragSensitivity = 0.2f; // 이제 InputManager의 drag 값을 조절하는 용도
    public float snapSpeed = 10f;

    [Header("Scale Settings")]
    public Vector3 centerScale = new Vector3(1.2f, 1.2f, 1f);
    public Vector3 sideScale = new Vector3(0.8f, 0.8f, 1f);

    private float currentRotationAngle = 0f;
    private float targetRotationAngle = 0f;
    private float[] itemBaseAngles;
    private bool isDragging = false; // 캐러셀이 현재 드래그에 반응 중인지 여부

    public int centerIndex { get; private set; }

    private class CommanderDepthInfo { public Transform transform; public float zPos; }

    // ✨ InputManager의 방송을 듣기 시작 (구독)
    void OnEnable()
    {
        InputManager.OnDragStart += HandleDragStart;
        InputManager.OnDrag += HandleDrag;
        InputManager.OnDragEnd += HandleDragEnd;
        InputManager.OnClick += HandleClick;
    }

    // ✨ InputManager의 방송을 더 이상 듣지 않음 (구독 취소)
    void OnDisable()
    {
        InputManager.OnDragStart -= HandleDragStart;
        InputManager.OnDrag -= HandleDrag;
        InputManager.OnDragEnd -= HandleDragEnd;
        InputManager.OnClick -= HandleClick;
    }
    void Start()
    {
        int count = commanderButtons.Count;
        if (count == 0) return;

        itemBaseAngles = new float[count];
        float angleStep = 360f / count;

        for (int i = 0; i < count; i++)
        {
            itemBaseAngles[i] = i * angleStep;
        }

        centerIndex = 0;
        targetRotationAngle = -itemBaseAngles[centerIndex];
        currentRotationAngle = targetRotationAngle;

        UpdateCommanderPositions();
    }

    void Update()
    {
        // 드래그 중이 아닐 때만 목표 각도로 부드럽게 회전 (Snap)
        if (!isDragging)
        {
            currentRotationAngle = Mathf.LerpAngle(currentRotationAngle, targetRotationAngle, Time.deltaTime * snapSpeed);
        }
        UpdateCommanderPositions();
    }

    // --- 아래는 InputManager의 방송을 받아서 처리하는 함수들 ---

    private void HandleDragStart(Vector2 dragStartPosition)
    {
        isDragging = true;
    }

    private void HandleDrag(Vector2 dragDelta)
    {
        // InputManager가 보내준 '움직인 거리'만큼만 회전
        currentRotationAngle += dragDelta.x * dragSensitivity;
    }

    private void HandleDragEnd()
    {
        isDragging = false;

        // 가장 가까운 아이템으로 정렬
        float minAngleDiff = float.MaxValue;
        int closestIndex = 0;
        for (int i = 0; i < commanderButtons.Count; i++)
        {
            float angleDifference = Mathf.Abs(Mathf.DeltaAngle(currentRotationAngle, -itemBaseAngles[i]));
            if (angleDifference < minAngleDiff)
            {
                minAngleDiff = angleDifference;
                closestIndex = i;
            }
        }
        centerIndex = closestIndex;
        targetRotationAngle = -itemBaseAngles[centerIndex];
    }


    private void HandleClick(Vector2 clickPosition)
    {
        // UI Raycast를 통해 정확히 어떤 버튼이 클릭되었는지 확인
        PointerEventData pointerData = new PointerEventData(EventSystem.current) { position = clickPosition };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        if (results.Count > 0)
        {
            foreach (var result in results)
            {
                Transform clickedObject = result.gameObject.transform;
                if (commanderButtons.Contains(clickedObject))
                {
                    OnCommanderClicked(clickedObject);
                    break;
                }
            }
        }
    }

    // ✨ 겹치는 순서(SiblingIndex) 로직을 수정한 함수
    void UpdateCommanderPositions()
    {
        List<CommanderDepthInfo> depthList = new List<CommanderDepthInfo>();

        for (int i = 0; i < commanderButtons.Count; i++)
        {
            float itemAngle = currentRotationAngle + itemBaseAngles[i];
            float angleInRad = itemAngle * Mathf.Deg2Rad;

            float xPos = Mathf.Sin(angleInRad) * radius;
            float zPos = Mathf.Cos(angleInRad); // -1 (뒤) ~ 1 (앞)

            // 위치와 크기는 즉시 적용
            commanderButtons[i].localPosition = new Vector3(xPos, 0, 0);
            Vector3 scale = Vector3.Lerp(sideScale, centerScale, (zPos + 1f) / 2f);
            commanderButtons[i].localScale = scale;

            // 순서 정렬을 위해 정보를 리스트에 저장
            depthList.Add(new CommanderDepthInfo { transform = commanderButtons[i], zPos = zPos });
        }

        // Z 깊이(zPos)를 기준으로 리스트를 오름차순 정렬 (뒤에 있는 것부터 순서대로)
        var sortedList = depthList.OrderBy(d => d.zPos).ToList();

        // 정렬된 순서대로 SiblingIndex를 0부터 부여
        for (int i = 0; i < sortedList.Count; i++)
        {
            sortedList[i].transform.SetSiblingIndex(i);
        }
    }

    public void ConfirmSelection()
    {
        Transform selectedCommander = commanderButtons[centerIndex];
        Debug.Log($"지휘관 선택 완료: {selectedCommander.name}");
    }

    public void OnCommanderClicked(Transform clickedButton)
    {
        if (commanderButtons[centerIndex] == clickedButton)
        {
            Debug.Log($"{clickedButton.name}의 능력치 또는 정보를 표시합니다.");
        }
    }
}