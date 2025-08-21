using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using DG.Tweening;

[System.Serializable]
public class ParameterSliderUI
{
    public ParameterType type;
    public Slider slider;
    public Image fillImage;
    public Toggle affectedToggle;
    public Image subEventFillImage;
}

public class ParameterUIController : MonoBehaviour
{
    [Header("�����̴� ����")]
    public List<ParameterSliderUI> parameterSliders;

    // �� 1�� ��û: ������� �׶���Ʈ ������� ����
    public Gradient sliderColorGradient;

    // �� 1�� ��û: �Ķ���� ��ȭ �߿� ǥ�õ� Ư�� ����
    [Tooltip("�Ķ���� ��ġ�� ���ϴ� ���� ǥ�õ� ����")]
    public Color parameterChangeColor = Color.yellow;


    [Header("���� �����̴�")]
    public Slider warSlider;

    [Header("ī���� �׵θ� ����")]
    public Image karmaBorderImage;
    public Gradient karmaColorGradient;

    void OnEnable()
    {
        PlayerStats.OnStatChanged += OnStatChanged;
    }

    void OnDisable()
    {
        PlayerStats.OnStatChanged -= OnStatChanged;
    }

    void Start()
    {
        StartCoroutine(InitializeUI());
    }

    private IEnumerator InitializeUI()
    {
        yield return new WaitForEndOfFrame();
        if (PlayerStats.Instance == null) yield break;
        ClearAllToggles();

        foreach (var ui in parameterSliders)
        {
            int initialValue = PlayerStats.Instance.GetStat(ui.type);
            UpdateSliderInstantly(ui, initialValue);
        }

        UpdateWarInstantly(PlayerStats.Instance.GetStat(ParameterType.����));
        UpdateKarma(PlayerStats.Instance.GetStat(ParameterType.ī����));
    }

    // (UpdateAffectedToggles, ClearAllToggles �Լ��� ������ �����Ͽ� ����)
    public void UpdateAffectedToggles(List<ParameterChange> changes, bool isSubEvent = false)
    {
        ClearAllToggles();
        foreach (var change in changes)
        {
            if (change.valueChange == 0) continue;

            ParameterSliderUI ui = parameterSliders.FirstOrDefault(s => s.type == change.parameterType);
            if (ui == null) continue;

            if (isSubEvent && ui.subEventFillImage != null)
            {
                ui.subEventFillImage.gameObject.SetActive(true);
                if (ui.affectedToggle != null) ui.affectedToggle.gameObject.SetActive(false);
            }
            else if (!isSubEvent && ui.affectedToggle != null)
            {
                ui.affectedToggle.isOn = true;
                if (ui.subEventFillImage != null) ui.subEventFillImage.gameObject.SetActive(false);
            }
        }
    }

    public void ClearAllToggles()
    {
        foreach (var ui in parameterSliders)
        {
            if (ui.affectedToggle != null) ui.affectedToggle.isOn = false;
            if (ui.subEventFillImage != null) ui.subEventFillImage.gameObject.SetActive(false);
        }
    }

    private void OnStatChanged(ParameterType type, int changeAmount, int newValue)
    {
        if (type == ParameterType.ī����) UpdateKarma(newValue);
        else if (type == ParameterType.����)
        {
            if (changeAmount == 0) return;
            AnimateWarUpdate(newValue);
        }
        else
        {
            if (changeAmount == 0) return;
            ParameterSliderUI ui = parameterSliders.FirstOrDefault(s => s.type == type);
            if (ui != null)
            {
                // �� 1�� ��û: ���ο� �ִϸ��̼� ������ ȣ��
                AnimateSliderUpdate(ui, newValue);
                ShowChangeEffect(ui.slider.transform, changeAmount);
            }
        }
    }

    // �ִϸ��̼� ���� �����̴� UI�� ��� ������Ʈ�ϴ� �Լ�
    private void UpdateSliderInstantly(ParameterSliderUI ui, int currentValue)
    {
        if (ui == null || ui.slider == null) return;

        float valueForSlider = Mathf.Ceil(currentValue / 25.0f);
        ui.slider.value = valueForSlider;

        if (ui.fillImage != null && sliderColorGradient != null)
        {
            // ���� �׶���Ʈ �������� ����
            ui.fillImage.color = sliderColorGradient.Evaluate(currentValue / 100f);
        }
    }

    /// <summary>
    /// �� 1�� ��û: ������ �Ķ���� ��ȭ �ִϸ��̼�
    /// </summary>
    private void AnimateSliderUpdate(ParameterSliderUI ui, int newTotalValue)
    {
        if (ui == null || ui.slider == null) return;

        // 1. ��� '��ȭ �� ����'���� ����
        if (ui.fillImage != null)
        {
            ui.fillImage.color = parameterChangeColor;
        }

        float targetSliderValue = Mathf.Ceil(newTotalValue / 25.0f);

        // 2. �����̴� ���� �ִϸ��̼����� ����
        ui.slider.DOValue(targetSliderValue, 2f)
            .SetEase(Ease.OutCubic)
            .OnComplete(() => {
                // 3. �ִϸ��̼��� ������ ������ �׶���Ʈ '��� ����'���� ����
                UpdateSliderInstantly(ui, newTotalValue);
            });
    }

    private void UpdateWarInstantly(int currentValue)
    {
        if (warSlider != null) warSlider.value = currentValue;
    }

    // �ڡڡ� ����: ���� UpdateWar �Լ��� �ִϸ��̼� ������� ����
    private void AnimateWarUpdate(int currentValue)
    {
        if (warSlider != null)
        {
            warSlider.DOValue(currentValue, 2f).SetEase(Ease.OutCubic);
        }
    }
    private void UpdateKarma(int currentValue)
    {
        if (karmaBorderImage == null) return;
        float normalizedValue = currentValue / 100f;
        karmaBorderImage.color = karmaColorGradient.Evaluate(normalizedValue);
    }

    private void ShowChangeEffect(Transform parent, int changeAmount)
    {
        Debug.Log($"{parent.name} ��ġ�� {(changeAmount > 0 ? "����" : "����")} ����Ʈ ǥ��!");
    }
}