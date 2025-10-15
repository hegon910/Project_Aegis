using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [Header("VFX 대상 설정")]
    public WarPlayer player; // 상태를 감시할 플레이어

    [Header("핵심 컴포넌트")]
    public Canvas vfxCanvas; // 모든 UI VFX가 그려질 캔버스 (FVX Cavas)
    public Camera vfxCamera; // UI 캔버스를 렌더링하는 카메라 (FVX Camera)

    [Header("VFX 프리팹")]
    public GameObject shieldOnPrefab;
    public GameObject shieldOffPrefab;

    private GameObject currentShieldVFX; // 현재 활성화된 쉴드 이펙트
    private bool wasShieldActive = false; // 이전 프레임의 쉴드 상태를 저장

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        if (player == null)
        {
            Debug.LogError("VFXManager에 Player가 할당되지 않았습니다!");
            return; 
        }

        wasShieldActive = player.currentShield > 0;
        UpdateShieldVFX(true);
    }

    void LateUpdate()
    {
        if (player == null) return; // 플레이어가 없으면 실행 중지

        bool isShieldActive = player.currentShield > 0;

        if (isShieldActive != wasShieldActive)
        {
            UpdateShieldVFX(false); // 상태가 변경되었으므로 VFX 업데이트
        }

        if (currentShieldVFX != null)
        {
            currentShieldVFX.transform.position = vfxCamera.WorldToScreenPoint(player.transform.position);
        }

        wasShieldActive = isShieldActive;
    }

    
    private void UpdateShieldVFX(bool isInitial)
    {
        if (currentShieldVFX != null)
        {
            Destroy(currentShieldVFX);
        }

        bool hasShield = player.currentShield > 0;

        if (hasShield) // 쉴드가 켜졌을 때
        {
            if (shieldOnPrefab != null && vfxCanvas != null)
            {
                currentShieldVFX = Instantiate(shieldOnPrefab, vfxCanvas.transform);
            }
        }
        else // 쉴드가 꺼졌을 때
        {
            if (!isInitial && shieldOffPrefab != null && vfxCanvas != null)
            {
                GameObject offVFX = Instantiate(shieldOffPrefab, vfxCanvas.transform);
                offVFX.transform.position = vfxCamera.WorldToScreenPoint(player.transform.position);
            }
        }
    }
}