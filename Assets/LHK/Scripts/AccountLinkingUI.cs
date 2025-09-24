using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 계정 연동 UI 관리 클래스
/// </summary>
public class AccountLinkingUI : MonoBehaviour
{
    [Header("계정 연동 버튼")]
    [SerializeField] private Button accountLinkingButton;
    [SerializeField] private TMP_Text accountLinkingButtonText;
    [SerializeField] private TMP_Text accountStatusText;
    
    [Header("계정 상태 표시")]
    [SerializeField] private GameObject accountStatusPanel;
    [SerializeField] private TMP_Text accountTypeText;
    [SerializeField] private TMP_Text accountInfoText;

    private void Awake()
    {
        // FirebaseManager의 로그인 상태 변경 이벤트 구독
        FirebaseManager.OnLoginStateChanged += UpdateUI;
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        FirebaseManager.OnLoginStateChanged -= UpdateUI;
    }

    private void Start()
    {
        // 버튼 이벤트 연결
        if (accountLinkingButton != null)
        {
            accountLinkingButton.onClick.RemoveAllListeners();
            accountLinkingButton.onClick.AddListener(OnAccountLinkingButtonClicked);
        }

        // 초기 UI 상태 업데이트
        UpdateUI(FirebaseManager.CurrentLoginType);
    }

    /// <summary>
    /// 로그인 상태 변경 시 UI 업데이트
    /// </summary>
    private void UpdateUI(LoginType loginType)
    {
        switch (loginType)
        {
            case LoginType.None:
                SetAccountLinkingButtonActive(false);
                SetAccountStatusText("로그인되지 않음", Color.gray);
                break;

            case LoginType.GPGS:
                SetAccountLinkingButtonActive(false);
                SetAccountStatusText("GPGS 계정으로 로그인됨", Color.green);
                break;

            case LoginType.Guest:
                SetAccountLinkingButtonActive(true);
                SetAccountStatusText("게스트 계정으로 로그인됨", Color.yellow);
                break;

            case LoginType.Linked:
                SetAccountLinkingButtonActive(false);
                SetAccountStatusText("계정 연동 완료", Color.green);
                break;
        }

        // 계정 상태 패널 업데이트
        UpdateAccountStatusPanel(loginType);
    }

    /// <summary>
    /// 계정 연동 버튼 활성화/비활성화
    /// </summary>
    private void SetAccountLinkingButtonActive(bool active)
    {
        if (accountLinkingButton != null)
        {
            accountLinkingButton.gameObject.SetActive(active);
        }

        if (accountLinkingButtonText != null)
        {
            accountLinkingButtonText.text = active ? "계정 연동" : "계정 연동 불가";
        }
    }

    /// <summary>
    /// 계정 상태 텍스트 설정
    /// </summary>
    private void SetAccountStatusText(string text, Color color)
    {
        if (accountStatusText != null)
        {
            accountStatusText.text = text;
            accountStatusText.color = color;
        }
    }

    /// <summary>
    /// 계정 상태 패널 업데이트
    /// </summary>
    private void UpdateAccountStatusPanel(LoginType loginType)
    {
        if (accountStatusPanel == null) return;

        switch (loginType)
        {
            case LoginType.None:
                accountStatusPanel.SetActive(false);
                break;

            case LoginType.GPGS:
                accountStatusPanel.SetActive(true);
                if (accountTypeText != null) accountTypeText.text = "GPGS 계정";
                if (accountInfoText != null) accountInfoText.text = "정식 계정으로 로그인되어 있습니다.\n게임 데이터가 서버에 안전하게 저장됩니다.";
                break;

            case LoginType.Guest:
                accountStatusPanel.SetActive(true);
                if (accountTypeText != null) accountTypeText.text = "게스트 계정";
                if (accountInfoText != null) accountInfoText.text = "임시 계정으로 로그인되어 있습니다.\n계정 연동을 하지 않으면 데이터가 손실될 수 있습니다.";
                break;

            case LoginType.Linked:
                accountStatusPanel.SetActive(true);
                if (accountTypeText != null) accountTypeText.text = "연동된 계정";
                if (accountInfoText != null) accountInfoText.text = "게스트 계정이 GPGS 계정에 성공적으로 연동되었습니다.\n게임 데이터가 서버에 안전하게 저장됩니다.";
                break;
        }
    }

    /// <summary>
    /// 계정 연동 버튼 클릭 처리
    /// </summary>
    private void OnAccountLinkingButtonClicked()
    {
        if (FirebaseManager.CurrentLoginType != LoginType.Guest)
        {
            Debug.LogWarning("게스트 계정이 아닙니다. 계정 연동을 할 수 없습니다.");
            return;
        }

        // 계정 연동 확인 다이얼로그 표시
        string message = "게스트 계정을 GPGS 계정에 연동하시겠습니까?\n연동 후에는 게임 데이터가 서버에 저장됩니다.";
        
        if (PopupController.Instance != null)
        {
            PopupController.Instance.ShowConfirmDialog(
                message: message,
                confirmText: "연동하기",
                cancelText: "취소",
                onConfirm: () => {
                    // 계정 연동 시작
                    FirebaseManager.Instance.LinkGuestToGPGS();
                },
                onCancel: () => {
                    Debug.Log("계정 연동이 취소되었습니다.");
                }
            );
        }
        else
        {
            Debug.LogError("PopupController 인스턴스를 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// 외부에서 UI 강제 업데이트 (테스트용)
    /// </summary>
    public void ForceUpdateUI()
    {
        UpdateUI(FirebaseManager.CurrentLoginType);
    }
}
