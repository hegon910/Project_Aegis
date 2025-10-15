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
    //[SerializeField] WarEnemy warenemy;
    [SerializeField] private ChoiceCardSwipe choiceCard;

    [Header("Texts")]
    [SerializeField] TMP_Text turnText;
    [SerializeField] TMP_Text playerHpNum;
    [SerializeField] TMP_Text playerShieldNum;
    [SerializeField] TMP_Text enemyNameText;
    [SerializeField] TMP_Text enemyHpNum;
    [SerializeField] TMP_Text enemyInfoText;
    [SerializeField] TMP_Text skillNameText;
    [SerializeField] TMP_Text skillCooldownText;

    [Header("War Status Slider")]
    [SerializeField] Slider warSlider;
    [SerializeField] Image warSliderFill;
    [SerializeField] Gradient warSliderGradient;

    [Header("Skill UI")]
    [SerializeField] private Image skillButtonImage;
    [SerializeField] private Button skillInfoButton;
    [SerializeField] private GameObject skillInfoPanel;
    [SerializeField] private TMP_Text skillInfoText;

    [Header("Feedback UI")]
    [SerializeField] private TMP_Text actionFeedbackText;
    public TMP_Text EnemyInfoTextField => enemyInfoText;


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
            playerShieldNum.text = $"{warplayer.currentShield}";
        }
    }

    private void UpdateEnemyUI()
    {
        WarEnemy currentEnemy = warturnMgr.CurrentEnemy;

        if (currentEnemy != null && currentEnemy.Ctrl != null)
        {
            if (enemyNameText != null)
            {
                enemyNameText.text = currentEnemy.name.Replace("(Clone)", "");
            }
            if (enemyHpNum != null)
            {
                enemyHpNum.text = $"{currentEnemy.Ctrl.CurrentHP}";
            }
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
        if (warplayer == null || skillInfoButton == null) return;

        var currentSkill = warplayer.currentSkill;

        if (currentSkill != null)
        {
            skillInfoButton.gameObject.SetActive(true);

            if (skillButtonImage != null && currentSkill.skillIcon != null)
            {
                skillButtonImage.sprite = currentSkill.skillIcon;
            }

            skillNameText.text = currentSkill.skillName; // 직접 스킬 데이터에서 이름 가져오기
            int cooldown = warturnMgr.GetSkillCooldown(); // 쿨다운은 TurnManager에서 가져옴
            skillCooldownText.text = (cooldown > 0) ? $"스킬쿨 {cooldown} 턴" : "사용 가능";
            if (choiceCard != null)
            {
                choiceCard.SetGlow(cooldown <= 0);
            }
        }
        else
        {
            skillInfoButton.gameObject.SetActive(false);

            skillNameText.text = "스킬 없음";
            skillCooldownText.text = "";
            if (choiceCard != null)
            {
                choiceCard.SetGlow(false);
            }
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
