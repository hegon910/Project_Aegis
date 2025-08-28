using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Vector2 referenceResolution = new Vector2(1080, 1920);
    [SerializeField] private float targetWidth = 11.25f;

    private Camera mainCamera;

    // ���� �������� ȭ�� ũ�⸦ ������ ����
    private int lastScreenWidth = 0;
    private int lastScreenHeight = 0;

    void Awake()
    {
        mainCamera = GetComponent<Camera>();
        // ó�� ������ �� �� �� ����
        AdjustCameraSize();
    }

    void Update()
    {
        // ���� ȭ�� ũ�Ⱑ ���� �����Ӱ� �ٸ� ��쿡�� �Լ��� ȣ���Ͽ� ���ʿ��� ������ ����
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            AdjustCameraSize();
        }
    }

    private void AdjustCameraSize()
    {
        if (mainCamera.orthographic == false)
        {
            // �� ���� �� ���� ǥ�õǵ��� Awake�� �Űܵ� �����ϴ�.
            Debug.LogWarning("ī�޶� Orthographic ��尡 �ƴմϴ�.");
            return;
        }

        // ���� ȭ�� ũ�⸦ last ������ ����
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