using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Image 컴포넌트를 사용하기 위해 추가

public class WarHistoryUI : MonoBehaviour
{
    [Tooltip("전투 기록을 표시할 6개의 UI 이미지")]
    [SerializeField] private Image[] recordImages;

    [Tooltip("승리 색상")]
    [SerializeField] private Color winColor = Color.cyan;

    [Tooltip("패배 색상")]
    [SerializeField] private Color lossColor = Color.red;

    [Tooltip("기본 색상")]
    [SerializeField] private Color defaultColor = Color.gray;

    void OnEnable()
    {
        UpdateWarDisplay();
    }

    // 이미지 업데이트
    public void UpdateWarDisplay()
    {
        List<bool> results = WarHistory.results;

        for (int i = 0; i < recordImages.Length; i++)
        {
            if (recordImages[i] == null) continue;

            if (i < results.Count)
            {
                recordImages[i].color = results[i] ? winColor : lossColor;
            }
            else
            {
                recordImages[i].color = defaultColor;
            }
        }
    }
}