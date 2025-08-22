using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestBattleUI : MonoBehaviour
{
    [SerializeField] BattleTurnManager turnMgr;
    [SerializeField] Button attackButton;
    [SerializeField] Button defendButton;

    void Awake()
    {
        attackButton.onClick.AddListener(() => turnMgr.OnClick_PlayerAttack());
        defendButton.onClick.AddListener(() => turnMgr.OnClick_PlayerDefend());
    }
}
