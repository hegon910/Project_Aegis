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
        // SpecialEventManager에서 대기 중인 스킬 에셋이 있으면 우선 사용하여 UI에 즉시 반영
        var pendingSkill = SpecialEventManager.Instance != null ? SpecialEventManager.Instance.GetPendingSkillData() : null;
        if (pendingSkill != null)
        {
            if (pendingSkill.icon != null) newIcon = pendingSkill.icon;
            if (!string.IsNullOrEmpty(pendingSkill.skillName)) newName = pendingSkill.skillName;
        }

        if (panel != null) panel.SetActive(true);
        if (titleText != null) titleText.text = title ?? string.Empty;

        if (currentSkillImage != null)
        {
            if (currentIcon != null)
            {
                currentSkillImage.sprite = currentIcon;
                var c = currentSkillImage.color; c.a = 1f; currentSkillImage.color = c;
            }
            else
            {
                // 현재 스킬이 없을 때는 프리팹/씬에 기본 할당된 스프라이트를 유지하고 보이도록 처리
                var c = currentSkillImage.color; c.a = 1f; currentSkillImage.color = c;
            }
        }
        if (currentSkillNameText != null)
        {
			// 현재 스킬이 없으면 텍스트를 "스킬 없음"으로 표기
			string displayName = string.IsNullOrEmpty(currentName) ? "스킬 없음" : currentName;
			currentSkillNameText.text = displayName;
            // 잘못된 바인딩(예: SituationText)에 대한 가드 경고
            if (currentSkillNameText.name == "SituationText")
            {
                Debug.LogWarning("[SkillPromptOverlay] currentSkillNameText가 SituationText에 바인딩되어 있습니다. 프리팹/씬에서 올바른 텍스트로 재바인딩하세요.");
            }
        }

        if (newSkillImage != null)
        {
            newSkillImage.sprite = newIcon;
            var c2 = newSkillImage.color; c2.a = newIcon != null ? 1f : 0f; newSkillImage.color = c2;
        }
        if (newSkillNameText != null)
        {
            newSkillNameText.text = newName ?? string.Empty;
            if (newSkillNameText.name == "SituationText")
            {
                Debug.LogWarning("[SkillPromptOverlay] newSkillNameText가 SituationText에 바인딩되어 있습니다. 프리팹/씬에서 올바른 텍스트로 재바인딩하세요.");
            }
        }
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }
}


