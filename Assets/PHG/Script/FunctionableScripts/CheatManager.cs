// CheatManager.cs (새 스크립트)
using UnityEngine;

public class CheatManager : MonoBehaviour
{
    // [Header("설정")]
    // [Tooltip("이 스크립트는 에디터와 개발 빌드에서만 동작합니다.")]
    // public bool enableCheats = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    void Update()
    {
        // PlayerStats 인스턴스가 없으면 아무것도 하지 않음
        if (PlayerStats.Instance == null)
        {
            return;
        }

        // --- 개별 파라미터 설정 (숫자 1~4) ---
        // Shift를 누르면 100, 안 누르면 0으로 설정
        int value = Input.GetKey(KeyCode.LeftShift) ? 100 : 5;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            PlayerStats.Instance.SetStat(ParameterType.정치력, value);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            PlayerStats.Instance.SetStat(ParameterType.병력, value);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            PlayerStats.Instance.SetStat(ParameterType.물자, value);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            PlayerStats.Instance.SetStat(ParameterType.리더십, value);
        }

        // --- 전체 파라미터 설정 (F1) ---
        // Shift를 누르면 100, 안 누르면 1로 설정
        if (Input.GetKeyDown(KeyCode.F1))
        {
            int allValue = Input.GetKey(KeyCode.LeftShift) ? 100 : 1;
            PlayerStats.Instance.SetStat(ParameterType.정치력, allValue);
            PlayerStats.Instance.SetStat(ParameterType.병력, allValue);
            PlayerStats.Instance.SetStat(ParameterType.물자, allValue);
            PlayerStats.Instance.SetStat(ParameterType.리더십, allValue);
        }

        // --- 전세(전황) 설정 (F5, F6, F7) ---
        if (Input.GetKeyDown(KeyCode.F5))
        {
            PlayerStats.Instance.SetStat(ParameterType.전황, 10);
        }
        if (Input.GetKeyDown(KeyCode.F6))
        {
            PlayerStats.Instance.SetStat(ParameterType.전황, 50);
        }
        if (Input.GetKeyDown(KeyCode.F7))
        {
            PlayerStats.Instance.SetStat(ParameterType.전황, 90);
        }
    }
#endif
}