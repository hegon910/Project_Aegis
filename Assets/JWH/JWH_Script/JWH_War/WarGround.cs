using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarGround : MonoBehaviour
{
    [Header("기준 설정")]
    [SerializeField] RectTransform origin; 
    [SerializeField] int laneLength = 14;
    [SerializeField] float cellSize = 100f;

    public enum AnchorX { Left, Center, Right }
    [SerializeField] AnchorX anchor = AnchorX.Left;

    public int LaneLength => laneLength;
    public float CellSize => cellSize;

    
    public Vector2 OriginAnchoredPos => (origin ? origin.anchoredPosition : Vector2.zero) + GroundOffset();

    
    Vector2 GroundOffset()
    {
        float half = cellSize * 0.5f;
        switch (anchor)
        {
            case AnchorX.Left:
                return new Vector2(half, 0);
            case AnchorX.Center:
                return new Vector2(-(laneLength - 1) * 0.5f * cellSize, 0);
            case AnchorX.Right:
                return new Vector2(-(laneLength - 0.5f) * cellSize, 0);
        }
        return Vector2.zero;
    }

    
    public Vector2 GetGroundPos(int tileIndex)
    {
        tileIndex = Mathf.Clamp(tileIndex, 0, laneLength - 1);
        return OriginAnchoredPos + new Vector2(tileIndex * cellSize, 0);
    }

    
    public int GetGroundIndex(Vector2 uiPos)
    {
        float dist = uiPos.x - OriginAnchoredPos.x;
        int tile = Mathf.RoundToInt(dist / cellSize);
        return Mathf.Clamp(tile, 0, laneLength - 1);
    }

#if UNITY_EDITOR
    // 기즈모는 월드 좌표 기준이므로 UI에서는 정확하지 않을 수 있습니다.
    // 참고용으로만 사용하거나 RectTransform 기준으로 다시 그리는 로직이 필요합니다.
    void OnDrawGizmos()
    {
        if (!origin) return;

        Gizmos.color = Color.green;
        // OnDrawGizmos는 UI 좌표계가 아닌 월드 좌표계에서 그려지므로,
        // 실제 게임 화면과 다르게 보일 수 있다는 점을 참고하세요.
        for (int i = 0; i < laneLength; i++)
        {
            // Gizmo를 위해 월드 좌표로 변환
            Vector3 worldPos = transform.TransformPoint(GetGroundPos(i));
            Gizmos.DrawWireCube(worldPos, new Vector3(cellSize, cellSize, 0.1f));
        }
    }
#endif
}