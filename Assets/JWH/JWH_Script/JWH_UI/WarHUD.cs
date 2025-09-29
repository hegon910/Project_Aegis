using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Threading; 
using Cysharp.Threading.Tasks;

public class WarHUD : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] WarTurnManager warturnMgr;
    [SerializeField] WarPlayer warplayer;
    [SerializeField] WarEnemy warenemy;
    [SerializeField] private ChoiceCardSwipe choiceCard;

    [Header("Texts")]
    [SerializeField] TMP_Text turnText;
    [SerializeField] TMP_Text playerHpNum;
    [SerializeField] TMP_Text playerShieldNum;
    [SerializeField] TMP_Text enemyNameText;
    [SerializeField] TMP_Text enemyHpNum;
    [SerializeField] TMP_Text skillNameText;
    [SerializeField] TMP_Text skillCooldownText;

    [Header("War Status Slider")]
    [SerializeField] Slider warSlider;
    [SerializeField] Image warSliderFill;
    [SerializeField] Gradient warSliderGradient;

    [Header("Skill UI")]
    [SerializeField] private Button skillInfoButton;
    [SerializeField] private GameObject skillInfoPanel;
    [SerializeField] private TMP_Text skillInfoText;

    [Header("Feedback UI")]
    [SerializeField] private TMP_Text actionFeedbackText;

    private CancellationTokenSource feedbackCts;

    void Start()
    {
        if (skillInfoButton != null)
        {
            skillInfoButton.onClick.AddListener(ToggleSkillDescription);
        }
        if (skillInfoPanel != null)
        {
            skillInfoPanel.SetActive(false);
        }
        if (actionFeedbackText != null)
        {
            actionFeedbackText.gameObject.SetActive(false);
        }
    }

    public void UpdateAllUI()
    {
        UpdateTurnUI();
        UpdatePlayerUI();
        UpdateEnemyUI();
        UpdateWarSlider();
        UpdateSkillUI();
    }

    public void ShowActionFeedback(string message, float duration = 1.5f)
    {
        feedbackCts?.Cancel();
        feedbackCts = new CancellationTokenSource();
        ShowFeedbackAsync(message, duration, feedbackCts.Token).Forget();
    }

    private async UniTaskVoid ShowFeedbackAsync(string message, float duration, CancellationToken token)
    {
        if (actionFeedbackText == null) return;

        actionFeedbackText.text = message;
        actionFeedbackText.gameObject.SetActive(true);

        try
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(duration), cancellationToken: token);
            actionFeedbackText.gameObject.SetActive(false);
        }
        catch (System.OperationCanceledException)
        {
        }
    }

    private void UpdateTurnUI()
    {
        if (warturnMgr)
        {
            int width = Mathf.Max(2, warturnMgr.MaxTurns.ToString().Length);
            string cur = warturnMgr.CurrentTurn.ToString($"D{width}");
            string max = warturnMgr.MaxTurns.ToString($"D{width}");
            turnText.text = $"{cur}/{max}";
        }
    }

    private void UpdatePlayerUI()
    {
        if (warplayer)
        {
            playerHpNum.text = $"{warplayer.Ctrl.CurrentHP}";
            playerShieldNum.text = $"{warplayer.Shield}";
        }
    }

    private void UpdateEnemyUI()
    {
        if (warenemy)
        {
            if (enemyNameText != null)
            {
                enemyNameText.text = warenemy.name;
            }
            enemyHpNum.text = $"{warenemy.Ctrl.CurrentHP}";
        }
    }

    private void UpdateWarSlider()
    {
        if (warSlider != null && GamePlayerStats.Instance != null)
        {
            int warValue = GamePlayerStats.Instance.GetStat(ParameterType.전황);
            warSlider.value = warValue;
            if (warSliderFill != null && warSliderGradient != null)
            {
                warSliderFill.color = warSliderGradient.Evaluate(warValue / 100f);
            }
        }
    }

    private void UpdateSkillUI()
    {
        if (warturnMgr == null) return;
        string currentSkillName = warturnMgr.GetSkillName();
        int cooldown = warturnMgr.GetSkillCooldown();
        bool isSkillAvailable = !string.IsNullOrEmpty(currentSkillName) && cooldown <= 0;
        if (choiceCard != null)
        {
            choiceCard.SetGlow(isSkillAvailable);
        }
        if (!string.IsNullOrEmpty(currentSkillName))
        {
            skillNameText.text = currentSkillName;
            skillCooldownText.text = (cooldown > 0) ? cooldown.ToString() : "사용 가능";
        }
        else
        {
            skillNameText.text = "스킬 없음";
            skillCooldownText.text = "";
        }
    }

    public void ToggleSkillDescription()
    {
        if (warplayer == null || warplayer.currentSkill == null || skillInfoPanel == null) return;
        bool isActive = skillInfoPanel.activeSelf;
        skillInfoPanel.SetActive(!isActive);
        if (!isActive)
        {
            skillInfoText.text = warplayer.currentSkill.description;
        }
    }
}
