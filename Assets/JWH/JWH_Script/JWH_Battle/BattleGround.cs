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

    private Canvas rootCanvas;
    public int LaneLength => laneLength;
    public float CellSize => cellSize;


    private float AdjustedCellSize
    {
        get
        {
            if (rootCanvas == null)
            {
                // 게임 시작 시점에 Canvas를 찾지 못했다면 기본값 사용
                return cellSize;
            }
            // Canvas의 스케일 팩터로 나눠주어 크기를 보정합니다.
            // scaleFactor가 0.5라면, cellSize는 2배가 되어 최종 크기가 1로 맞춰집니다.
            return cellSize / rootCanvas.scaleFactor;
        }
    }

    void Awake()
    {
        // === 추가된 부분 START ===
        // 스크립트가 시작될 때, 부모 계층에서 Canvas를 찾아 저장합니다.
        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null)
        {
            Debug.LogWarning("BattleGround: 부모에게서 Canvas를 찾을 수 없습니다. UI 스케일링이 적용되지 않을 수 있습니다.");
        }
        // === 추가된 부분 END ===
    }

    public Vector3 OriginWorld
        => (origin ? origin.position : Vector3.zero) + GroundOffset();


 
    Vector3 GroundOffset()
    {
        float half = AdjustedCellSize * 0.5f;

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
        return OriginWorld + new Vector3(tileIndex * AdjustedCellSize, 0, 0);
    }

    public int GetGroundIndex(Vector3 worldPos)
    {
        float dist = worldPos.x - OriginWorld.x;
        int tile = Mathf.RoundToInt(dist / AdjustedCellSize);
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
            Gizmos.DrawWireCube(pos, new Vector3(AdjustedCellSize, AdjustedCellSize, 0.1f));
        }
    }
#endif
}