using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class BattleHUD : MonoBehaviour
{
    [SerializeField] BattleTurnManager turnMgr;
    [SerializeField] BattlePlayer player;
    [SerializeField] BattleEnemy enemy;

    [Header("Texts")]
    [SerializeField] TMP_Text turnTxt;      // 예: "1/30"
    [SerializeField] TMP_Text pHpTxt;       // 예: "HP 5"
    [SerializeField] TMP_Text pShieldTxt;   // 예: "SH 0"
    [SerializeField] TMP_Text eHpTxt;
    

    void LateUpdate()
    {
        if (turnMgr)
        {
            // 최대 턴의 자릿수에 맞춰 최소 2자리로 패딩
            int width = Mathf.Max(2, turnMgr.MaxTurns.ToString().Length);
            string cur = turnMgr.CurrentTurn.ToString($"D{width}");
            string max = turnMgr.MaxTurns.ToString($"D{width}");
            turnTxt.text = $"{cur}/{max}";   // 예: 01/30, 02/30 ...
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