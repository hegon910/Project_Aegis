using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIPanelController : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private Image characterImage;
    [SerializeField] private TextMeshProUGUI characterNameText;

    void Awake()
    {
        // 시작 시 비활성화
        gameObject.SetActive(false);
    }

    public void Show(Sprite sprite, string name)
    {
        gameObject.SetActive(true);
        if (characterImage != null)
        {
            characterImage.sprite = sprite;
            // 스프라이트가 없으면 이미지를 투명하게 처리
            characterImage.color = (sprite == null) ? Color.clear : Color.white;
        }
        if (characterNameText != null)
        {
            characterNameText.text = name;
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}