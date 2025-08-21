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
    [Header("슬라이더 설정")]
    public List<ParameterSliderUI> parameterSliders;
    public Gradient sliderColorGradient;

    [Header("전세 슬라이더")] 
    public Slider warSlider;
    public Image warFillImage;

    [Header("카르마 테두리 설정")]
    public Image karmaBorderImage;
    public Gradient karmaColorGradient;

    // 이 스크립트가 활성화될 때 이벤트 리스너를 등록합니다.
    void OnEnable()
    {
        PlayerStats.OnStatChanged += OnStatChanged;
    }

    // 비활성화될 때 리스너를 해제합니다.
    void OnDisable()
    {
        PlayerStats.OnStatChanged -= OnStatChanged;
    }

    // ★★★ 추가된 부분 ★★★
    // 게임 시작 시, 모든 UI의 초기 상태를 설정합니다.
    void Start()
    {
        // PlayerStats가 초기화될 시간을 벌기 위해 한 프레임 대기합니다.
        StartCoroutine(InitializeUI());
    }

    private IEnumerator<WaitForEndOfFrame> InitializeUI()
    {
        yield return new WaitForEndOfFrame();

        if (PlayerStats.Instance == null) yield break;
        ClearAllToggles();

        // 모든 슬라이더의 초기값을 설정합니다.
        foreach (var ui in parameterSliders)
        {
            int initialValue = PlayerStats.Instance.GetStat(ui.type);
            UpdateSlider(ui, initialValue); // 이펙트 없이 UI만 업데이트
        }
        int initialWar = PlayerStats.Instance.GetStat(ParameterType.전황);
        UpdateWar(initialWar);
        // 카르마의 초기값을 설정합니다.
        int initialKarma = PlayerStats.Instance.GetStat(ParameterType.카르마);
        UpdateKarma(initialKarma); // 이펙트 없이 UI만 업데이트
    }

    /// <summary>
    /// 변경될 파라미터 목록을 받아와 해당하는 토글을 켭니다.
    /// </summary>
    public void UpdateAffectedToggles(List<ParameterChange> changes)
    {
        // 1. 우선 모든 토글을 끕니다.
        ClearAllToggles();

        // 2. 변경량이 0이 아닌 파라미터에 해당하는 토글만 찾아서 켭니다.
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


    // PlayerStats에서 변경 알림이 오면 이 함수가 호출됩니다.

    private void OnStatChanged(ParameterType type, int changeAmount, int newValue)
    {
        // ★★★ 수정된 부분 ★★★
        // if-else if 구조로 각 파라미터를 명확하게 분리합니다.
        if (type == ParameterType.카르마)
        {
            UpdateKarma(newValue);
        }
        else if (type == ParameterType.전황)
        {
            UpdateWar(newValue);
        }
        else // 나머지 4개 파라미터
        {
            ParameterSliderUI ui = parameterSliders.FirstOrDefault(s => s.type == type);
            if (ui != null)
            {
                UpdateSlider(ui, newValue);
                ShowChangeEffect(ui.slider.transform, changeAmount);
            }
        }
    }

    // 슬라이더 UI를 업데이트하는 로직 (분리됨)
    private void UpdateSlider(ParameterSliderUI ui, int currentValue)
    {
        if (ui == null || ui.slider == null) return;

        // ★★★ 수정된 핵심 로lic ★★★
        // 0~100 사이의 값을 0~4 사이의 값으로 변환합니다.
        // 1~25 -> 1, 26~50 -> 2, 51~75 -> 3, 76~100 -> 4
        float valueForSlider = Mathf.Ceil(currentValue / 25.0f);
        ui.slider.value = valueForSlider;

        // 슬라이더 색상은 부드러운 변화를 위해 원래 값을 사용합니다.
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
            // 1. 전세 값을 슬라이더에 그대로 반영합니다.
            warSlider.value = currentValue;

            // 2. 슬라이더 색상을 그라디언트에 맞춰 변경합니다.
            if (warFillImage != null && sliderColorGradient != null)
            {
                float normalizedValue = currentValue / 100f;
                warFillImage.color = sliderColorGradient.Evaluate(normalizedValue);
            }
        }
    }
    // 카르마 UI를 업데이트하는 로직 (분리됨)
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
        // 이펙트 연출 로직 (변경 없음)
        Debug.Log($"{parent.name} 위치에 {(changeAmount > 0 ? "증가" : "감소")} 이펙트 표시!");
    }
}