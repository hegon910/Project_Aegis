using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class WarGround : MonoBehaviour
{
    [Header("레인 설정")]
    [Tooltip("전투가 벌어질 전체 칸의 개수")]
    [SerializeField] private int laneLength = 16;

    [SerializeField] private float sideMargin = 0.5f;
    public float SideMargin => sideMargin;

    [Tooltip("각 칸의 너비")]
    [SerializeField] private float cellSize;

    private RectTransform rectTransform;

    public int LaneLength => laneLength;
    public float CellSize => cellSize;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Start()
    {
        CalculateCellSize();
    }

    public void CalculateCellSize()
    {
        if (rectTransform != null && laneLength > 0)
        {
            float totalDivisions = laneLength + (sideMargin * 2);
            if (totalDivisions > 0)
            {
                cellSize = rectTransform.rect.width / totalDivisions;
            }
        }
    }

    public Vector2 GetGroundPos(int laneIndex)
    {
        laneIndex = Mathf.Clamp(laneIndex, 0, laneLength - 1);
        float leftEdgeX = -rectTransform.rect.width * rectTransform.pivot.x;

        float startOffsetX = leftEdgeX + (sideMargin * cellSize);
        float targetX = startOffsetX + (laneIndex * cellSize) + (cellSize / 2);

        float targetY = 0;
        return new Vector2(targetX, targetY);
    }


    //  public class ReadOnlyAttribute : PropertyAttribute { }

}

#if UNITY_EDITOR
//[UnityEditor.CustomPropertyDrawer(typeof(WarGround.ReadOnlyAttribute))]
//public class ReadOnlyDrawer : UnityEditor.PropertyDrawer
//{
//    public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
//    {
//        GUI.enabled = false;
//        UnityEditor.EditorGUI.PropertyField(position, property, label, true);
//        GUI.enabled = true;
//    }
//}
#endif
