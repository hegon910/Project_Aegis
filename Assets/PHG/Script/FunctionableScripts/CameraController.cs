using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    // �����̳ʰ� �۾��� ���� �ػ� (��: 1080x1920)
    [SerializeField] private Vector2 referenceResolution = new Vector2(1080, 1920);

    // ���� �ػ󵵿��� �ǵ��ߴ� ���� ������ ���� �ʺ�
    // ��: Orthographic Size�� 10�̰� ���� �ػ󵵰� 1080x1920 (9:16) �̶��,
    // ���� �ʺ� = (10 * 2) * (1080 / 1920) = 20 * 0.5625 = 11.25 �� �˴ϴ�.
    [SerializeField] private float targetWidth = 11.25f;

    private Camera mainCamera;

    void Awake()
    {
        mainCamera = GetComponent<Camera>();
        AdjustCameraSize();
    }

    /// <summary>
    /// ���� ȭ�� ������ ���� ī�޶��� Orthographic Size�� �����Ͽ�
    /// �׻� targetWidth ��ŭ�� ���� �ʺ� �����ϴ� �Լ�
    /// </summary>
    private void AdjustCameraSize()
    {
        if (mainCamera.orthographic == false)
        {
            Debug.LogWarning("ī�޶� Orthographic ��尡 �ƴմϴ�. �� ��ũ��Ʈ�� Orthographic ī�޶󿡼��� �����մϴ�.");
            return;
        }

        // ���� ȭ���� ����/���� ����
        float screenAspect = (float)Screen.width / (float)Screen.height;

        // ��ǥ ���� �ʺ� ���� ȭ�� ������ �����ϱ� ���� �ʿ��� Orthographic Size�� ���
        float newOrthographicSize = targetWidth / screenAspect / 2.0f;

        // ���� ������ ī�޶��� Orthographic Size�� ����
        mainCamera.orthographicSize = newOrthographicSize;
    }
}
