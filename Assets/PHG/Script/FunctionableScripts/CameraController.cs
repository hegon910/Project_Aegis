using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Vector2 referenceResolution = new Vector2(1080, 1920);
    [SerializeField] private float targetWidth = 11.25f;

    private Camera mainCamera;

    // 이전 프레임의 화면 크기를 저장할 변수
    private int lastScreenWidth = 0;
    private int lastScreenHeight = 0;

    void Awake()
    {
        mainCamera = GetComponent<Camera>();
        // 처음 시작할 때 한 번 조절
        AdjustCameraSize();
    }

    void Update()
    {
        // 현재 화면 크기가 이전 프레임과 다를 경우에만 함수를 호출하여 불필요한 연산을 줄임
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            AdjustCameraSize();
        }
    }

    private void AdjustCameraSize()
    {
        if (mainCamera.orthographic == false)
        {
            // 이 경고는 한 번만 표시되도록 Awake로 옮겨도 좋습니다.
            Debug.LogWarning("카메라가 Orthographic 모드가 아닙니다.");
            return;
        }

        // 현재 화면 크기를 last 변수에 저장
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        float referenceAspect = referenceResolution.x / referenceResolution.y;
        float screenAspect = (float)Screen.width / (float)Screen.height;
        float referenceOrthographicSize = targetWidth / referenceAspect / 2.0f;
        float newOrthographicSize;

        if (screenAspect >= referenceAspect)
        {
            newOrthographicSize = referenceOrthographicSize;
        }
        else
        {
            newOrthographicSize = targetWidth / screenAspect / 2.0f;
        }

        mainCamera.orthographicSize = newOrthographicSize;
    }
}