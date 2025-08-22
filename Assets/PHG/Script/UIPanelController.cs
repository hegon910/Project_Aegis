using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIPanelController : MonoBehaviour
{
    [Header("UI ����")]
    [SerializeField] private Image characterImage;
    [SerializeField] private TextMeshProUGUI characterNameText;

    void Awake()
    {
        // ���� �� ��Ȱ��ȭ
        gameObject.SetActive(false);
    }

    public void Show(Sprite sprite, string name)
    {
        gameObject.SetActive(true);
        if (characterImage != null)
        {
            characterImage.sprite = sprite;
            // ��������Ʈ�� ������ �̹����� �����ϰ� ó��
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