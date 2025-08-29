using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarGround : MonoBehaviour
{
    [Header("���� ����")]
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
    // ������ ���� ��ǥ �����̹Ƿ� UI������ ��Ȯ���� ���� �� �ֽ��ϴ�.
    // ��������θ� ����ϰų� RectTransform �������� �ٽ� �׸��� ������ �ʿ��մϴ�.
    void OnDrawGizmos()
    {
        if (!origin) return;

        Gizmos.color = Color.green;
        // OnDrawGizmos�� UI ��ǥ�谡 �ƴ� ���� ��ǥ�迡�� �׷����Ƿ�,
        // ���� ���� ȭ��� �ٸ��� ���� �� �ִٴ� ���� �����ϼ���.
        for (int i = 0; i < laneLength; i++)
        {
            // Gizmo�� ���� ���� ��ǥ�� ��ȯ
            Vector3 worldPos = transform.TransformPoint(GetGroundPos(i));
            Gizmos.DrawWireCube(worldPos, new Vector3(cellSize, cellSize, 0.1f));
        }
    }
#endif
}