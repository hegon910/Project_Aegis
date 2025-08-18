using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

[RequireComponent(typeof(CanvasGroup))]
public class SituationCardController : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    public TextMeshProUGUI situationText; // 인스펙터에서 텍스트 컴포넌트 연결

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void Show(string text)
    {
        situationText.text = text;
        canvasGroup.alpha = 0;
        // 0.3초 동안 부드럽게 나타나는 애니메이션
        canvasGroup.DOFade(1, 0.3f);
    }

    public void Hide()
    {
        // 0.3초 동안 부드럽게 사라지는 애니메이션
        canvasGroup.DOFade(0, 0.3f).OnComplete(() => {
            // 필요하다면 비활성화
            // gameObject.SetActive(false); 
        });
    }
}
