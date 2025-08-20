using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestBattleUI : MonoBehaviour
{
    [SerializeField] BattleController controller;
    [SerializeField] Button attackButton;
    [SerializeField] Button defendButton;

    void Awake()
    {
        attackButton.onClick.AddListener(() => controller.DoAction(BattleAction.Attack));
        defendButton.onClick.AddListener(() => controller.DoAction(BattleAction.Defend));
    }
}
