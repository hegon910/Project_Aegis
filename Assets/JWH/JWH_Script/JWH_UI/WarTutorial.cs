using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WarTutorial : MonoBehaviour
{
    [Tooltip("튜토리얼 패널 게임 오브젝트")]
    [SerializeField] private GameObject tutorialPanel;

    private const string TutorialSeenKey = "HasSeenWarTutorial";

    void Start()
    {
        // 기본값 0을 반환 (0: 본 적 없음, 1: 본 적 있음)
        int tutorialSeen = PlayerPrefs.GetInt(TutorialSeenKey, 0);

        // 튜토리얼을 본 적이 없다면
        if (tutorialSeen == 0)
        {
            tutorialPanel.SetActive(true);
        }
        else
        {
            // 튜토리얼을 이미 봤다
            tutorialPanel.SetActive(false);
        }
    }

   
    public void HideTutorial()
    {
        if (tutorialPanel.activeSelf)
        {
            tutorialPanel.SetActive(false);

            // 튜토리얼 기록합 (값 1)
            PlayerPrefs.SetInt(TutorialSeenKey, 1);

            PlayerPrefs.Save();
        }
    }
}
