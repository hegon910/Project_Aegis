using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[RequireComponent(typeof(RectTransform))]
public class WarGround : MonoBehaviour
{
    [Header("레인 설정")]
    [Tooltip("전투가 벌어질 전체 칸의 개수")]
    [SerializeField] private int laneLength = 16;

    [SerializeField] private float sideMargin = 0.5f;
    public float SideMargin => sideMargin;

    [Tooltip("각 칸의 너비 (자동 계산)")]
    [SerializeField] private float cellSize;

    private RectTransform rectTransform;

    public int LaneLength => laneLength;
    public float CellSize => cellSize;

    public event Action OnGridUpdated;

    public void InitializeGrid(int newLaneLength)
    {
        // 새로운 값으로 laneLength를 업데이트
        this.laneLength = newLaneLength;
        CalculateAndNotify();
    }

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void OnEnable()
    {
        CalculateAndNotify();
    }

    void OnRectTransformDimensionsChange()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }
        CalculateAndNotify();
    }

    public void CalculateAndNotify()
    {
        if (rectTransform != null && laneLength > 0)
        {
            float totalDivisions = laneLength + (sideMargin * 2);
            if (totalDivisions > 0)
            {
                cellSize = rectTransform.rect.width / totalDivisions;
            }
        }
        OnGridUpdated?.Invoke();
    }

    public Vector2 GetGroundPos(int laneIndex)
    {
        laneIndex = Mathf.Clamp(laneIndex, 0, laneLength - 1);
        float totalGridWidth = laneLength * cellSize;
        float remainingSpace = rectTransform.rect.width - totalGridWidth;
        float leftEdgeX = -rectTransform.rect.width * rectTransform.pivot.x;
        float centeredStartX = leftEdgeX + (remainingSpace / 2);
        float targetX = centeredStartX + (laneIndex * cellSize) + (cellSize / 2);
        float targetY = 0;
        return new Vector2(targetX, targetY);
    }
}
