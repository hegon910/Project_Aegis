using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class ParameterSliderUI
{
    public ParameterType type;
    public Slider slider;
    public Image fillImage;
    public Toggle affectedToggle;
}

public class ParameterUIController : MonoBehaviour
{
    [Header("�����̴� ����")]
    public List<ParameterSliderUI> parameterSliders;
    public Gradient sliderColorGradient;

    [Header("���� �����̴�")] 
    public Slider warSlider;
    public Image warFillImage;

    [Header("ī���� �׵θ� ����")]
    public Image karmaBorderImage;
    public Gradient karmaColorGradient;

    // �� ��ũ��Ʈ�� Ȱ��ȭ�� �� �̺�Ʈ �����ʸ� ����մϴ�.
    void OnEnable()
    {
        PlayerStats.OnStatChanged += OnStatChanged;
    }

    // ��Ȱ��ȭ�� �� �����ʸ� �����մϴ�.
    void OnDisable()
    {
        PlayerStats.OnStatChanged -= OnStatChanged;
    }

    // �ڡڡ� �߰��� �κ� �ڡڡ�
    // ���� ���� ��, ��� UI�� �ʱ� ���¸� �����մϴ�.
    void Start()
    {
        // PlayerStats�� �ʱ�ȭ�� �ð��� ���� ���� �� ������ ����մϴ�.
        StartCoroutine(InitializeUI());
    }

    private IEnumerator<WaitForEndOfFrame> InitializeUI()
    {
        yield return new WaitForEndOfFrame();

        if (PlayerStats.Instance == null) yield break;
        ClearAllToggles();

        // ��� �����̴��� �ʱⰪ�� �����մϴ�.
        foreach (var ui in parameterSliders)
        {
            int initialValue = PlayerStats.Instance.GetStat(ui.type);
            UpdateSlider(ui, initialValue); // ����Ʈ ���� UI�� ������Ʈ
        }
        int initialWar = PlayerStats.Instance.GetStat(ParameterType.����);
        UpdateWar(initialWar);
        // ī������ �ʱⰪ�� �����մϴ�.
        int initialKarma = PlayerStats.Instance.GetStat(ParameterType.ī����);
        UpdateKarma(initialKarma); // ����Ʈ ���� UI�� ������Ʈ
    }

    /// <summary>
    /// ����� �Ķ���� ����� �޾ƿ� �ش��ϴ� ����� �մϴ�.
    /// </summary>
    public void UpdateAffectedToggles(List<ParameterChange> changes)
    {
        // 1. �켱 ��� ����� ���ϴ�.
        ClearAllToggles();

        // 2. ���淮�� 0�� �ƴ� �Ķ���Ϳ� �ش��ϴ� ��۸� ã�Ƽ� �մϴ�.
        foreach (var change in changes)
        {
            if (change.valueChange != 0)
            {
                ParameterSliderUI ui = parameterSliders.FirstOrDefault(s => s.type == change.parameterType);
                if (ui != null && ui.affectedToggle != null)
                {
                    ui.affectedToggle.isOn = true;
                }
            }
        }
    }

    public void ClearAllToggles()
    {
        foreach (var ui in parameterSliders)
        {
            if (ui.affectedToggle != null)
            {
                ui.affectedToggle.isOn = false;
            }
        }
    }


    // PlayerStats���� ���� �˸��� ���� �� �Լ��� ȣ��˴ϴ�.

    private void OnStatChanged(ParameterType type, int changeAmount, int newValue)
    {
        // �ڡڡ� ������ �κ� �ڡڡ�
        // if-else if ������ �� �Ķ���͸� ��Ȯ�ϰ� �и��մϴ�.
        if (type == ParameterType.ī����)
        {
            UpdateKarma(newValue);
        }
        else if (type == ParameterType.����)
        {
            UpdateWar(newValue);
        }
        else // ������ 4�� �Ķ����
        {
            ParameterSliderUI ui = parameterSliders.FirstOrDefault(s => s.type == type);
            if (ui != null)
            {
                UpdateSlider(ui, newValue);
                ShowChangeEffect(ui.slider.transform, changeAmount);
            }
        }
    }

    // �����̴� UI�� ������Ʈ�ϴ� ���� (�и���)
    private void UpdateSlider(ParameterSliderUI ui, int currentValue)
    {
        if (ui == null || ui.slider == null) return;

        // �ڡڡ� ������ �ٽ� ��lic �ڡڡ�
        // 0~100 ������ ���� 0~4 ������ ������ ��ȯ�մϴ�.
        // 1~25 -> 1, 26~50 -> 2, 51~75 -> 3, 76~100 -> 4
        float valueForSlider = Mathf.Ceil(currentValue / 25.0f);
        ui.slider.value = valueForSlider;

        // �����̴� ������ �ε巯�� ��ȭ�� ���� ���� ���� ����մϴ�.
        if (ui.fillImage != null && sliderColorGradient != null)
        {
            float normalizedOriginalValue = currentValue / 100f;
            ui.fillImage.color = sliderColorGradient.Evaluate(normalizedOriginalValue);
        }
    }
    private void UpdateWar(int currentValue)
    {
        if (warSlider != null)
        {
            // 1. ���� ���� �����̴��� �״�� �ݿ��մϴ�.
            warSlider.value = currentValue;

            // 2. �����̴� ������ �׶���Ʈ�� ���� �����մϴ�.
            if (warFillImage != null && sliderColorGradient != null)
            {
                float normalizedValue = currentValue / 100f;
                warFillImage.color = sliderColorGradient.Evaluate(normalizedValue);
            }
        }
    }
    // ī���� UI�� ������Ʈ�ϴ� ���� (�и���)
    private void UpdateKarma(int currentValue)
    {
        if (karmaBorderImage != null)
        {
            float normalizedValue = currentValue / 100f;
            karmaBorderImage.color = karmaColorGradient.Evaluate(normalizedValue);
        }
    }

    private void ShowChangeEffect(Transform parent, int changeAmount)
    {
        // ����Ʈ ���� ���� (���� ����)
        Debug.Log($"{parent.name} ��ġ�� {(changeAmount > 0 ? "����" : "����")} ����Ʈ ǥ��!");
    }
}