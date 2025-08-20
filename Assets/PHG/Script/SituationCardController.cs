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
        // ���� �ÿ��� ������ �ʵ��� Ȯ���ϰ� ó��
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
    /// (�߰��� �κ�) ���� ī���� �ؽ�Ʈ�� ���ΰ�ħ�մϴ�.
    /// </summary>
    public void UpdateText(string newText)
    {
        situationText.text = newText.Replace("\\n", "\n");
    }
}