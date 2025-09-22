using UnityEngine;
using TMPro;

[RequireComponent(typeof(WarGround))]
public class WarGroundUI : MonoBehaviour
{
    [Header("참조 및 프리팹")]
    [Tooltip("눈금선 프리팹")]
    [SerializeField] private GameObject dividerPrefab;

    [Tooltip("숫자 프리팹")]
    [SerializeField] private GameObject numberPrefab;

    [Header("컨테이너 (UI가 들어갈 곳)")]
    [Tooltip("생성된 눈금선들을 담을 부모 오브젝트")]
    [SerializeField] private Transform dividersContainer;

    [Tooltip("생성된 숫자들을 담을 부모 오브젝트")]
    [SerializeField] private Transform numbersContainer;

    [Header("마커 오프셋")]
    [Tooltip("숫자가 눈금선 기준으로 표시될 Y축 높이")]
    [SerializeField] private float numberYOffset = 30f;

    private WarGround warGround;

    void Start()
    {
        warGround = GetComponent<WarGround>();
        GenerateMarkers();
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
        if (warGround == null) warGround = GetComponent<WarGround>();
        if (dividerPrefab == null || numberPrefab == null) return;

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
            numberPos.y += numberYOffset; // Y축 오프셋 적용

            numberRect.anchoredPosition = numberPos;

            TextMeshProUGUI numberText = numberObj.GetComponent<TextMeshProUGUI>();
            if (numberText != null)
            {
                numberText.text = numberValue.ToString();
            }
        }
    }
}
