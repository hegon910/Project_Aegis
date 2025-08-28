using UnityEngine;
using UnityEngine.UI;

// 이 스크립트는 Canvas Scaler가 있는 GameObject에 붙어야 하므로,
// 자동으로 해당 컴포넌트가 있는지 확인하고 없으면 추가해줍니다.
[RequireComponent(typeof(CanvasScaler))]
public class DynamicCanvasMatcher : MonoBehaviour
{
    // 디자이너가 작업한 기준 해상도
    [SerializeField] private Vector2 referenceResolution = new Vector2(1080, 1920);

    private CanvasScaler canvasScaler;

    // 화면 크기 변경을 감지하기 위한 변수
    private int lastScreenWidth = 0;
    private int lastScreenHeight = 0;

    void Awake()
    {
        // CanvasScaler 컴포넌트를 가져옴
        canvasScaler = GetComponent<CanvasScaler>();
        // 시작할 때 한 번 실행
        AdjustMatchValue();
    }

    void Update()
    {
        // 에디터 등에서 실시간으로 해상도가 변경될 때를 대비
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            AdjustMatchValue();
        }
    }

    private void AdjustMatchValue()
    {
        // 현재 화면 크기 저장
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        // 기준 화면 비율 계산
        float referenceAspect = referenceResolution.x / referenceResolution.y;
        // 현재 화면 비율 계산
        float currentAspect = (float)Screen.width / (float)Screen.height;

        // 현재 화면이 기준보다 가로로 넓다면 (예: 폴드3, 태블릿 가로 모드)
        if (currentAspect > referenceAspect)
        {
            // Match 값을 1 (Height 기준)로 설정
            canvasScaler.matchWidthOrHeight = 1f;
        }
        else // 기준과 같거나 세로로 길다면 (예: 일반 스마트폰)
        {
            // Match 값을 0.5 (중간)로 설정
            canvasScaler.matchWidthOrHeight = 0.5f;
        }
    }
}