using UnityEngine;
using System.Collections.Generic;
using System.Linq; // ✨ 정렬(OrderBy)을 위해 추가

public class CommanderCarouselController : MonoBehaviour
{
    [Header("UI Elements")]
    public List<Transform> commanderButtons;

    [Header("Rotation Settings")]
    [Tooltip("회전 반경 (클수록 넓게 퍼짐)")]
    public float radius = 500f;
    [Tooltip("드래그 감도")]
    public float dragSensitivity = 0.2f;
    [Tooltip("자동 정렬 속도")]
    public float snapSpeed = 10f;

    [Header("Scale Settings")]
    [Tooltip("중앙(앞)에 있을 때 크기")]
    public Vector3 centerScale = new Vector3(1.2f, 1.2f, 1f);
    [Tooltip("양 옆(뒤)에 있을 때 크기")]
    public Vector3 sideScale = new Vector3(0.8f, 0.8f, 1f);

    private float currentRotationAngle = 0f;
    private float targetRotationAngle = 0f;
    private float[] itemBaseAngles;

    private bool isDragging = false;
    private float startDragAngle;
    private Vector2 startDragPos;

    public int centerIndex { get; private set; }

    // ✨ 겹침 순서 정렬을 위한 임시 저장 클래스
    private class CommanderDepthInfo
    {
        public Transform transform;
        public float zPos;
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
        HandleDragInput();

        if (!isDragging)
        {
            currentRotationAngle = Mathf.LerpAngle(currentRotationAngle, targetRotationAngle, Time.deltaTime * snapSpeed);
        }

        UpdateCommanderPositions();
    }

    private void HandleDragInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            startDragPos = Input.mousePosition;
            startDragAngle = currentRotationAngle;
        }

        if (Input.GetMouseButton(0) && isDragging)
        {
            float dragDistance = (Input.mousePosition.x - startDragPos.x);
            currentRotationAngle = startDragAngle + (dragDistance * dragSensitivity);
        }

        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            isDragging = false;

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