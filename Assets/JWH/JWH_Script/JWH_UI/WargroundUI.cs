using UnityEngine;
using TMPro;

[RequireComponent(typeof(WarGround))]
public class WarGroundUI : MonoBehaviour
{
    [Header("참조 및 프리팹")]
    [SerializeField] private GameObject dividerPrefab;
    [SerializeField] private GameObject numberPrefab;

    [Header("컨테이너 (UI가 들어갈 곳)")]
    [SerializeField] private Transform dividersContainer;
    [SerializeField] private Transform numbersContainer;

    [Header("마커 오프셋")]
    [SerializeField] private float numberYOffset = 30f;

    private WarGround warGround;

    void Awake()
    {
        warGround = GetComponent<WarGround>();
    }

    void OnEnable()
    {
        warGround.OnGridUpdated += GenerateMarkers;
    }

    void OnDisable()
    {
        warGround.OnGridUpdated -= GenerateMarkers;
    }

    void ClearMarkers()
    {
        foreach (Transform child in dividersContainer)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in numbersContainer)
        {
            Destroy(child.gameObject);
        }
    }

    public void GenerateMarkers()
    {
        if (dividerPrefab == null || numberPrefab == null) return;
        if (warGround.CellSize <= 0) return; // 셀 크기가 아직 계산되지 않았다면 실행하지 않음

        ClearMarkers();

        RectTransform groundRect = warGround.GetComponent<RectTransform>();
        float totalGridWidth = warGround.LaneLength * warGround.CellSize;
        float remainingSpace = groundRect.rect.width - totalGridWidth;
        float leftEdgeX = -groundRect.rect.width * groundRect.pivot.x;
        float startOffsetX = leftEdgeX + (remainingSpace / 2);

        for (int i = 1; i < warGround.LaneLength; i++)
        {
            GameObject dividerObj = Instantiate(dividerPrefab, dividersContainer);
            RectTransform dividerRect = dividerObj.GetComponent<RectTransform>();
            float lineX = startOffsetX + (i * warGround.CellSize);
            dividerRect.anchoredPosition = new Vector2(lineX, 0);
        }

        for (int i = 0; i < warGround.LaneLength - 2; i++)
        {
            int numberValue = i + 1;
            int laneIndex = i + 1;

            GameObject numberObj = Instantiate(numberPrefab, numbersContainer);
            RectTransform numberRect = numberObj.GetComponent<RectTransform>();

            Vector2 numberPos = warGround.GetGroundPos(laneIndex);
            numberPos.y += numberYOffset;

            numberRect.anchoredPosition = numberPos;

            TextMeshProUGUI numberText = numberObj.GetComponent<TextMeshProUGUI>();
            if (numberText != null)
            {
                numberText.text = numberValue.ToString();
            }
        }
    }
}

