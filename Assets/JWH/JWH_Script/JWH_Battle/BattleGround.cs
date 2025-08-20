using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleGround : MonoBehaviour
{
    [Header("기준 설정")]
    [SerializeField] Transform origin;     // 기준 오브젝트(보통 스프라이트의 Transform)
    [SerializeField] int laneLength = 14;  // 칸 개수
    [SerializeField] float cellSize = 1f;  // 칸 간격

    public enum AnchorX { Left, Center, Right }
    [SerializeField] AnchorX anchor = AnchorX.Left;  // 왼쪽 기준으로 설정

    public int LaneLength => laneLength;
    public float CellSize => cellSize;

    
    public Vector3 OriginWorld
        => (origin ? origin.position : Vector3.zero) + GroundOffset();

    Vector3 GroundOffset()
    {
        float half = cellSize * 0.5f;

        switch (anchor)
        {
            case AnchorX.Left:
                
                return new Vector3(half, 0, 0);

            case AnchorX.Center:
                
                return new Vector3(-(laneLength - 1) * 0.5f * cellSize, 0, 0);

            case AnchorX.Right:
                
                return new Vector3(-(laneLength - 0.5f) * cellSize, 0, 0);
        }
        return Vector3.zero;
    }

    public Vector3 GetGroundPos(int tileIndex)
    {
        tileIndex = Mathf.Clamp(tileIndex, 0, laneLength - 1);
        return OriginWorld + new Vector3(tileIndex * cellSize, 0, 0);
    }

    public int GetGroundIndex(Vector3 worldPos)
    {
        float dist = worldPos.x - OriginWorld.x;
        int tile = Mathf.RoundToInt(dist / cellSize);
        return Mathf.Clamp(tile, 0, laneLength - 1);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!origin) return;

        Gizmos.color = Color.green;
        for (int i = 0; i < laneLength; i++)
        {
            Vector3 pos = GetGroundPos(i);
            Gizmos.DrawWireCube(pos, new Vector3(cellSize, cellSize, 0.1f));
        }
    }
#endif
}