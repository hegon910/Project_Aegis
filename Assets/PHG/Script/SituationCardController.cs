using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class SituationCardController : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    public TextMeshProUGUI situationText;

    [Header("선택지 미리보기")]
    public Image choicePreviewImage;
    public TextMeshProUGUI confirmText;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (confirmText != null)
        {
            confirmText.color = Color.clear;
        }
        if (choicePreviewImage != null)
        {
            choicePreviewImage.color = Color.clear;
        }
        
        canvasGroup.alpha = 0;
        gameObject.SetActive(false);
    }

    public void Show(string text)
    {
        gameObject.SetActive(true);
        situationText.text = text;
        UpdateChoicePreview("", Color.clear);
        
        canvasGroup.alpha = 0;
        canvasGroup.DOFade(1, 0.3f);
    }

    public void Hide()
    {
        canvasGroup.DOFade(0, 0.3f).OnComplete(() => {
            gameObject.SetActive(false);
        });
    }

    public void UpdateText(string newText)
    {
        situationText.text = newText.Replace("\\n", "\n");
    }

    public void UpdateChoicePreview(string text, Color textColor)
    {
        if (confirmText != null)
        {
            confirmText.text = text;
            
            confirmText.color = textColor;
        }

        if (choicePreviewImage != null)
        {
            Color imageColor = choicePreviewImage.color;
            imageColor.a = textColor.a * 0.5f;
            choicePreviewImage.color = imageColor;
        }
    }
}