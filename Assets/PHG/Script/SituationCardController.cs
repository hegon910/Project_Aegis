using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class SituationCardController : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    public TextMeshProUGUI situationText;

    [Header("������ �̸�����")]
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
        if (situationText == null)
        {
            Debug.LogError("'situationText' 참조가 Inspector에 할당되지 않았습니다!", this.gameObject);
            return;
        }

        // newText가 null일 경우를 대비하여 안전하게 처리합니다.
        string displayText = newText ?? string.Empty;
        situationText.text = displayText.Replace("\n", "\n");
    }

    public void UpdateChoicePreview(string text, Color textColor)
    {
        if (confirmText != null)
        {
            confirmText.text = text;
            
            // �ڡڡ� �ѹ�� �κ� �ڡڡ�
            // ���޹��� textColor(�������� RGB ���� ��� ����)�� �״�� �����մϴ�.
            confirmText.color = textColor;
        }

        if (choicePreviewImage != null)
        {
            // ��� �̹����� ������ ������� ������ ä �������� ���󰡵��� �����մϴ�.
            Color imageColor = choicePreviewImage.color;
            imageColor.a = textColor.a * 0.5f;
            choicePreviewImage.color = imageColor;
        }
    }
}