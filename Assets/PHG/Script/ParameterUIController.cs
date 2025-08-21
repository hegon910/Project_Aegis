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
    [Header("슬라이더 설정")]
    public List<ParameterSliderUI> parameterSliders;

    // ▼ 1번 요청: 원래대로 그라디언트 사용으로 복귀
    public Gradient sliderColorGradient;

    // ▼ 1번 요청: 파라미터 변화 중에 표시될 특정 색상
    [Tooltip("파라미터 수치가 변하는 동안 표시될 색상")]
    public Color parameterChangeColor = Color.yellow;


    [Header("전세 슬라이더")]
    public Slider warSlider;

    [Header("카르마 테두리 설정")]
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

        UpdateWarInstantly(PlayerStats.Instance.GetStat(ParameterType.전세));
        UpdateKarma(PlayerStats.Instance.GetStat(ParameterType.카르마));
    }

    // (UpdateAffectedToggles, ClearAllToggles 함수는 이전과 동일하여 생략)
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
        if (type == ParameterType.카르마) UpdateKarma(newValue);
        else if (type == ParameterType.전세)
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
                // ★ 1번 요청: 새로운 애니메이션 로직을 호출
                AnimateSliderUpdate(ui, newValue);
                ShowChangeEffect(ui.slider.transform, changeAmount);
            }
        }
    }

    // 애니메이션 없이 슬라이더 UI를 즉시 업데이트하는 함수
    private void UpdateSliderInstantly(ParameterSliderUI ui, int currentValue)
    {
        if (ui == null || ui.slider == null) return;

        float valueForSlider = Mathf.Ceil(currentValue / 25.0f);
        ui.slider.value = valueForSlider;

        if (ui.fillImage != null && sliderColorGradient != null)
        {
            // 원래 그라디언트 색상으로 설정
            ui.fillImage.color = sliderColorGradient.Evaluate(currentValue / 100f);
        }
    }

    /// <summary>
    /// ★ 1번 요청: 수정된 파라미터 변화 애니메이션
    /// </summary>
    private void AnimateSliderUpdate(ParameterSliderUI ui, int newTotalValue)
    {
        if (ui == null || ui.slider == null) return;

        // 1. 즉시 '변화 중 색상'으로 변경
        if (ui.fillImage != null)
        {
            ui.fillImage.color = parameterChangeColor;
        }

        float targetSliderValue = Mathf.Ceil(newTotalValue / 25.0f);

        // 2. 슬라이더 값만 애니메이션으로 변경
        ui.slider.DOValue(targetSliderValue, 2f)
            .SetEase(Ease.OutCubic)
            .OnComplete(() => {
                // 3. 애니메이션이 끝나면 원래의 그라디언트 '대기 색상'으로 복귀
                UpdateSliderInstantly(ui, newTotalValue);
            });
    }

    private void UpdateWarInstantly(int currentValue)
    {
        if (warSlider != null) warSlider.value = currentValue;
    }

    // ★★★ 수정: 기존 UpdateWar 함수를 애니메이션 기능으로 변경
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
        Debug.Log($"{parent.name} 위치에 {(changeAmount > 0 ? "증가" : "감소")} 이펙트 표시!");
    }
}