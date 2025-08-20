using UnityEngine;
using TMPro;
using DG.Tweening;

[RequireComponent(typeof(CanvasGroup))]
public class SituationCardController : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    public TextMeshProUGUI situationText;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        // 시작 시에는 보이지 않도록 확실하게 처리
        canvasGroup.alpha = 0;
        gameObject.SetActive(false);
    }

    public void Show(string text)
    {
        gameObject.SetActive(true);
        situationText.text = text;
        canvasGroup.alpha = 0;
        canvasGroup.DOFade(1, 0.3f);
    }

    public void Hide()
    {
        canvasGroup.DOFade(0, 0.3f).OnComplete(() => {
            gameObject.SetActive(false);
        });
    }

    /// <summary>
    /// (추가된 부분) 현재 카드의 텍스트만 새로고침합니다.
    /// </summary>
    public void UpdateText(string newText)
    {
        situationText.text = newText.Replace("\\n", "\n");
    }
}