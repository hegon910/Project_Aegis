using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 특수 이벤트의 스킬 획득/교체 프롬프트에서 기존/신규 스킬 아이콘과 이름을 표시하는 오버레이
/// 씬에 없으면 아무 동작도 하지 않으며, 존재하면 UIFlowSimulator가 자동으로 제어합니다.
/// </summary>
public class SkillPromptOverlay : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Image currentSkillImage;
    [SerializeField] private TextMeshProUGUI currentSkillNameText;
    [SerializeField] private Image newSkillImage;
    [SerializeField] private TextMeshProUGUI newSkillNameText;
    [SerializeField] private TextMeshProUGUI titleText;

    public void Show(Sprite currentIcon, string currentName, Sprite newIcon, string newName, string title)
    {
        if (panel != null) panel.SetActive(true);
        if (titleText != null) titleText.text = title ?? string.Empty;

        if (currentSkillImage != null) currentSkillImage.sprite = currentIcon;
        if (currentSkillNameText != null) currentSkillNameText.text = currentName ?? string.Empty;

        if (newSkillImage != null) newSkillImage.sprite = newIcon;
        if (newSkillNameText != null) newSkillNameText.text = newName ?? string.Empty;
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }
}


