using TMPro;
using Unity.VisualScripting;
using UnityEngine;

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
    }
}