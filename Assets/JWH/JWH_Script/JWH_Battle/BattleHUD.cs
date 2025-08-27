using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class BattleHUD : MonoBehaviour
{
    [SerializeField] BattleTurnManager turnMgr;
    [SerializeField] BattlePlayer player;
    [SerializeField] BattleEnemy enemy;

    [Header("Texts")]
    [SerializeField] TMP_Text turnTxt;      // ��: "1/30"
    [SerializeField] TMP_Text pHpTxt;       // ��: "HP 5"
    [SerializeField] TMP_Text pShieldTxt;   // ��: "SH 0"
    [SerializeField] TMP_Text eHpTxt;

    [Header("War Status Slider")]
    [SerializeField] Slider warSlider;
    [SerializeField] Image warSliderFill; // �����̴��� Fill Image
    [SerializeField] Gradient warSliderGradient; // �����̴� ���� ���� ���� ����

    void LateUpdate()
    {
        if (turnMgr)
        {
            // �ִ� ���� �ڸ����� ���� �ּ� 2�ڸ��� �е�
            int width = Mathf.Max(2, turnMgr.MaxTurns.ToString().Length);
            string cur = turnMgr.CurrentTurn.ToString($"D{width}");
            string max = turnMgr.MaxTurns.ToString($"D{width}");
            turnTxt.text = $"{cur}/{max}";   // ��: 01/30, 02/30 ...
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
    }
    private void UpdateWarSlider()
    {
        // PlayerStats �ν��Ͻ��� warSlider�� ��� �Ҵ�Ǿ��� ���� ����
        if (warSlider != null && PlayerStats.Instance != null)
        {
            // ��Ȳ �Ķ���� ���� ������ (0~100 ������ ����)
            int warValue = PlayerStats.Instance.GetStat(ParameterType.��Ȳ);

            // �����̴� �� ������Ʈ
            warSlider.value = warValue;

            // �����̴� ���� ������Ʈ (Fill�� Gradient�� ��� �Ҵ�� ���)
            if (warSliderFill != null && warSliderGradient != null)
            {
                // ���� 0.0 ~ 1.0 ������ ����ȭ�Ͽ� Gradient�� ���
                warSliderFill.color = warSliderGradient.Evaluate(warValue / 100f);
            }
        }
    }
}