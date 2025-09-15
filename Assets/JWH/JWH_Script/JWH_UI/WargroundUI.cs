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
        float leftEdgeX = -groundRect.rect.width * groundRect.pivot.x;
        float startOffsetX = leftEdgeX + (warGround.SideMargin * warGround.CellSize);

        // 눈금선 생성
        for (int i = 0; i <= warGround.LaneLength; i++)
        {
            GameObject dividerObj = Instantiate(dividerPrefab, dividersContainer);
            RectTransform dividerRect = dividerObj.GetComponent<RectTransform>();
            float lineX = startOffsetX + (i * warGround.CellSize);
            dividerRect.anchoredPosition = new Vector2(lineX, 0);
        }

        for (int i = 1; i <= warGround.LaneLength; i++) // i를 1부터 16까지 반복
        {
            GameObject numberObj = Instantiate(numberPrefab, numbersContainer);
            RectTransform numberRect = numberObj.GetComponent<RectTransform>();
            Vector2 numberPos;

            // 숫자 '1'의 위치 (첫 번째 경계선)
            if (i == 1)
            {
                float lineX = startOffsetX + ((i - 1) * warGround.CellSize);
                numberPos = new Vector2(lineX, numberYOffset);
            }
            // 숫자 '16'의 위치 (마지막 경계선)
            else if (i == warGround.LaneLength)
            {
                float lineX = startOffsetX + (i * warGround.CellSize);
                numberPos = new Vector2(lineX, numberYOffset);
            }
            // 나머지 숫자 '2' ~ '15'의 위치 (각 칸의 중앙)
            else
            {
                numberPos = warGround.GetGroundPos(i - 1); // GetGroundPos는 0부터 시작하므로 i-1
                numberPos.y += numberYOffset;
            }

            numberRect.anchoredPosition = numberPos;

            TextMeshProUGUI numberText = numberObj.GetComponent<TextMeshProUGUI>();
            if (numberText != null)
            {
                numberText.text = i.ToString();
            }
        }
    }
}
