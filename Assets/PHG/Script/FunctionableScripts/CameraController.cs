using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    // 디자이너가 작업한 기준 해상도 (예: 1080x1920)
    [SerializeField] private Vector2 referenceResolution = new Vector2(1080, 1920);

    // 기준 해상도에서 의도했던 월드 유닛의 가로 너비
    // 예: Orthographic Size가 10이고 기준 해상도가 1080x1920 (9:16) 이라면,
    // 가로 너비 = (10 * 2) * (1080 / 1920) = 20 * 0.5625 = 11.25 가 됩니다.
    [SerializeField] private float targetWidth = 11.25f;

    private Camera mainCamera;

    void Awake()
    {
        mainCamera = GetComponent<Camera>();
        AdjustCameraSize();
    }

    /// <summary>
    /// 현재 화면 비율에 맞춰 카메라의 Orthographic Size를 조절하여
    /// 항상 targetWidth 만큼의 가로 너비를 보장하는 함수
    /// </summary>
    private void AdjustCameraSize()
    {
        if (mainCamera.orthographic == false)
        {
            Debug.LogWarning("카메라가 Orthographic 모드가 아닙니다. 이 스크립트는 Orthographic 카메라에서만 동작합니다.");
            return;
        }

        // 현재 화면의 가로/세로 비율
        float screenAspect = (float)Screen.width / (float)Screen.height;

        // 목표 가로 너비를 현재 화면 비율로 유지하기 위해 필요한 Orthographic Size를 계산
        float newOrthographicSize = targetWidth / screenAspect / 2.0f;

        // 계산된 값으로 카메라의 Orthographic Size를 설정
        mainCamera.orthographicSize = newOrthographicSize;
    }
}
