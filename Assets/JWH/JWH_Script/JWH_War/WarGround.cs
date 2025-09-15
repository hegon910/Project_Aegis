using System.Collections;
using UnityEngine;
[RequireComponent(typeof(RectTransform))]
public class WarGround : MonoBehaviour
{
    [Header("레인 설정")]
    [Tooltip("전투가 벌어질 전체 칸의 개수")]
    [SerializeField] private int laneLength = 16;

    [Tooltip("각 칸의 너비")]
    [SerializeField] private float cellSize;  //화면 크기에 맞춰 자동으로 계산

    private RectTransform rectTransform;

    public int LaneLength => laneLength;
    public float CellSize => cellSize;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        CalculateCellSize();
    }

    public void CalculateCellSize()    // 화면 너비를 기반으로 각 셀의 크기를 계산하는 함수

    {
        if (rectTransform != null && laneLength > 0)
        {
            cellSize = rectTransform.rect.width / laneLength;
        }
    }

    public Vector2 GetGroundPos(int laneIndex)
    {
        laneIndex = Mathf.Clamp(laneIndex, 0, laneLength - 1);
        float leftEdgeX = -rectTransform.rect.width * rectTransform.pivot.x;
        float targetX = leftEdgeX + (laneIndex * cellSize) + (cellSize / 2);
        float targetY = 0;
        return new Vector2(targetX, targetY);
    }

#if UNITY_EDITOR
    
    [SerializeField, HideInInspector]
    private int previousLaneLength;

    private void OnValidate()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        if (laneLength != previousLaneLength)
        {
            CalculateCellSize();
            previousLaneLength = laneLength;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        CalculateCellSize();

        Gizmos.color = new Color(0, 1, 0, 0.5f);
        Vector3 cellSizeVec = new Vector3(cellSize, rectTransform.rect.height, 0.1f);

        for (int i = 0; i < laneLength; i++)
        {
            Vector3 cellCenterWorldPos = transform.TransformPoint(GetGroundPos(i));
            Gizmos.DrawWireCube(cellCenterWorldPos, cellSizeVec);
        }
    }

  //  public class ReadOnlyAttribute : PropertyAttribute { }
#endif
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
