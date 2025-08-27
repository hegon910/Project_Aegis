using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleGround : MonoBehaviour
{
    [Header("���� ����")]
    [SerializeField] Transform origin;     // ���� ������Ʈ(���� ��������Ʈ�� Transform)
    [SerializeField] int laneLength = 14;  // ĭ ����
    [SerializeField] float cellSize = 1f;  // ĭ ����

    public enum AnchorX { Left, Center, Right }
    [SerializeField] AnchorX anchor = AnchorX.Left;  // ���� �������� ����

    private Canvas rootCanvas;
    public int LaneLength => laneLength;
    public float CellSize => cellSize;


    private float AdjustedCellSize
    {
        get
        {
            if (rootCanvas == null)
            {
                // ���� ���� ������ Canvas�� ã�� ���ߴٸ� �⺻�� ���
                return cellSize;
            }
            // Canvas�� ������ ���ͷ� �����־� ũ�⸦ �����մϴ�.
            // scaleFactor�� 0.5���, cellSize�� 2�谡 �Ǿ� ���� ũ�Ⱑ 1�� �������ϴ�.
            return cellSize / rootCanvas.scaleFactor;
        }
    }

    void Awake()
    {
        // === �߰��� �κ� START ===
        // ��ũ��Ʈ�� ���۵� ��, �θ� �������� Canvas�� ã�� �����մϴ�.
        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null)
        {
            Debug.LogWarning("BattleGround: �θ𿡰Լ� Canvas�� ã�� �� �����ϴ�. UI �����ϸ��� ������� ���� �� �ֽ��ϴ�.");
        }
        // === �߰��� �κ� END ===
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