using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class WarHUD : MonoBehaviour
{
    [SerializeField] WarTurnManager turnMgr;
    [SerializeField] WarPlayer player;
    [SerializeField] WarEnemy enemy;
    

    [Header("Texts")]
    [SerializeField] TMP_Text turnTxt;      // 예: "1/30"
    [SerializeField] TMP_Text pHpTxt;       // 예: "HP 5"
    [SerializeField] TMP_Text pShieldTxt;   // 예: "SH 0"
    [SerializeField] TMP_Text eHpTxt;
    [SerializeField] TMP_Text skillCooldownText; // 쿨타임 표시 텍스트

    [Header("War Status Slider")]
    [SerializeField] Slider warSlider;
    [SerializeField] Image warSliderFill; // 슬라이더의 Fill Image
    [SerializeField] Gradient warSliderGradient; // 슬라이더 값에 따라 변할 색상

    void LateUpdate()
    {
        if (turnMgr)
        {
            // 최대 턴의 자릿수에 맞춰 최소 2자리로 패딩
            int width = Mathf.Max(2, turnMgr.MaxTurns.ToString().Length);
            string cur = turnMgr.CurrentTurn.ToString($"D{width}");
            string max = turnMgr.MaxTurns.ToString($"D{width}");
            turnTxt.text = $"{cur}/{max}"; 
        }
        if (player)
        {
            pHpTxt.text = $"HP {player.HP}";
            pShieldTxt.text = $"SH {player.Shield}";
        }
        if (enemy)
        {
            eHpTxt.text = $"HP {enemy.HP}";
            
        }
        UpdateWarSlider();
        UpdateSkillUI();
    }
    private void UpdateWarSlider()
    {
        // PlayerStats 인스턴스와 warSlider가 모두 할당되었을 때만 실행
        if (warSlider != null && PlayerStats.Instance != null)
        {
            // 전황 파라미터 값을 가져옴 (0~100 범위로 가정)
            int warValue = PlayerStats.Instance.GetStat(ParameterType.전황);

            // 슬라이더 값 업데이트
            warSlider.value = warValue;

            // 슬라이더 색상 업데이트 (Fill과 Gradient가 모두 할당된 경우)
            if (warSliderFill != null && warSliderGradient != null)
            {
                // 값을 0.0 ~ 1.0 범위로 정규화하여 Gradient에 사용
                warSliderFill.color = warSliderGradient.Evaluate(warValue / 100f);
            }
        }
    }

    void UpdateSkillUI()
    {
        int cooldown = turnMgr.GetSkillCooldown();
        if (cooldown > 0)
        {
            skillCooldownText.text = cooldown.ToString();
        }
        else
        {
            skillCooldownText.text = "";
        }
    }
}