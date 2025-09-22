using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class WarHUD : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] WarTurnManager turnMgr;
    [SerializeField] WarPlayer player;
    [SerializeField] WarEnemy enemy;
    [SerializeField] private ChoiceCardSwipe choiceCard;

    [Header("Texts")]
    [SerializeField] TMP_Text turnTxt;     
    
    [SerializeField] TMP_Text pHpTxt;      
    [SerializeField] TMP_Text pShieldTxt;

    [SerializeField] TMP_Text eNameTxt;
    [SerializeField] TMP_Text eHpTxt;

    [SerializeField] TMP_Text skillNameText;
    [SerializeField] TMP_Text skillCooldownText;

    [Header("War Status Slider")]
    [SerializeField] Slider warSlider;
    [SerializeField] Image warSliderFill; // 슬라이더의 Fill Image
    [SerializeField] Gradient warSliderGradient; // 슬라이더 값에 따라 변할 색상

    [Header("Skill Description UI")]
    [Tooltip("��ų ��ġ ��ư")]
    [SerializeField] private Button skillInfoButton;
    [Tooltip("��ų ���� �г�")]
    [SerializeField] private GameObject skillDescriptionPanel;
    [Tooltip("��ų ���� �ؽ�Ʈ")]
    [SerializeField] private TMP_Text skillDescriptionText;

    [Header("Feedback UI")]
    [Tooltip("�ൿ ��� �ǵ�� �ؽ�Ʈ (��: ��ų ���!)")]
    [SerializeField] private TMP_Text actionFeedbackText;

    private Coroutine feedbackCoroutine;


    void Start()
    {
        if (skillInfoButton != null)
        {
            skillInfoButton.onClick.AddListener(ToggleSkillDescription);
        }
        if (skillDescriptionPanel != null)
        {
            skillDescriptionPanel.SetActive(false);
        }
        if (actionFeedbackText != null)
        {
            actionFeedbackText.gameObject.SetActive(false);
        }
    }
    void Update()
    {
        if (turnMgr)
        {
            // 최대 턴의 자릿수에 맞춰 최소 2자리로 패딩
            int width = Mathf.Max(2, turnMgr.MaxTurns.ToString().Length);
            string cur = turnMgr.CurrentTurn.ToString($"D{width}");
            string max = turnMgr.MaxTurns.ToString($"D{width}");
            turnTxt.text = $"{cur}/{max}"; 
        }
        if (player)
        {
            pHpTxt.text = $"{player.Ctrl.CurrentHP}";
            pShieldTxt.text = $"{player.Shield}";
        }
        if (enemy)
        {
            if (eNameTxt != null)
            {
                eNameTxt.text = enemy.name;
            }
            eHpTxt.text = $"{enemy.Ctrl.CurrentHP}";
        }
        UpdateWarSlider();
        UpdateSkillUI();
    }

    public void ShowActionFeedback(string message, float duration = 1.5f)
    {
        // �̹� ���� ���� �ǵ�� �ڷ�ƾ�� �ִٸ� ������ŵ�ϴ�.
        if (feedbackCoroutine != null)
        {
            StopCoroutine(feedbackCoroutine);
        }
        feedbackCoroutine = StartCoroutine(Co_ShowFeedbackText(message, duration));
    }
    private IEnumerator Co_ShowFeedbackText(string message, float duration)
    {
        if (actionFeedbackText != null)
        {
            actionFeedbackText.text = message;
            actionFeedbackText.gameObject.SetActive(true);

            yield return new WaitForSeconds(duration);

            actionFeedbackText.gameObject.SetActive(false);
            feedbackCoroutine = null;
        }
    }

    private void UpdateWarSlider()
    {
        // PlayerStats 인스턴스와 warSlider가 모두 할당되었을 때만 실행
        if (warSlider != null && GamePlayerStats.Instance != null)
        {
            // 전황 파라미터 값을 가져옴 (0~100 범위로 가정)
            int warValue = GamePlayerStats.Instance.GetStat(ParameterType.전황);

            // 슬라이더 값 업데이트
            warSlider.value = warValue;

            // 슬라이더 색상 업데이트 (Fill과 Gradient가 모두 할당된 경우)
            if (warSliderFill != null && warSliderGradient != null)
            {
                // 값을 0.0 ~ 1.0 범위로 정규화하여 Gradient에 사용
                warSliderFill.color = warSliderGradient.Evaluate(warValue / 100f);
            }
        }
    }

    void UpdateSkillUI()
    {
        if (turnMgr == null) return;

        string currentSkillName = turnMgr.GetSkillName();
        int cooldown = turnMgr.GetSkillCooldown();
        bool isSkillAvailable = !string.IsNullOrEmpty(currentSkillName) && cooldown <= 0;

        if (choiceCard != null)
        {
            choiceCard.SetGlow(isSkillAvailable);
        }

        if (!string.IsNullOrEmpty(currentSkillName))
        {
            skillNameText.text = currentSkillName;
            if (cooldown > 0)
            {
                skillCooldownText.text = cooldown.ToString();
            }
            else
            {
                skillCooldownText.text = "사용 가능";
            }
        }
        else
        {
            skillNameText.text = "스킬 없음";
            skillCooldownText.text = "";
        }
    }

    public void ToggleSkillDescription()
    {        
        if (player == null || player.currentSkill == null || skillDescriptionPanel == null)
        {
            return;
        }
        bool isActive = skillDescriptionPanel.activeSelf;
        skillDescriptionPanel.SetActive(!isActive);
        if (!isActive)
        {
            skillDescriptionText.text = player.currentSkill.description;
        }
    }
    
}
