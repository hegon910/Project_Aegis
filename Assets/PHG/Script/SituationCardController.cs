using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

[RequireComponent(typeof(CanvasGroup))]
public class SituationCardController : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    public TextMeshProUGUI situationText; // �ν����Ϳ��� �ؽ�Ʈ ������Ʈ ����

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void Show(string text)
    {
        situationText.text = text;
        canvasGroup.alpha = 0;
        // 0.3�� ���� �ε巴�� ��Ÿ���� �ִϸ��̼�
        canvasGroup.DOFade(1, 0.3f);
    }

    public void Hide()
    {
        // 0.3�� ���� �ε巴�� ������� �ִϸ��̼�
        canvasGroup.DOFade(0, 0.3f).OnComplete(() => {
            // �ʿ��ϴٸ� ��Ȱ��ȭ
            // gameObject.SetActive(false); 
        });
    }
}
