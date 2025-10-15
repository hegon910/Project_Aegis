using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 통합 팝업 컨트롤러 - 게스트 로그인 관련 팝업들을 관리
/// </summary>
public class PopupController : MonoBehaviour
{
    public static PopupController Instance { get; private set; }

    [Header("팝업 패널들")]
    [SerializeField] private GameObject alertDialogPanel;
    [SerializeField] private GameObject confirmDialogPanel;
    
    [Header("Alert Dialog UI")]
    [SerializeField] private TMP_Text alertTitleText;
    [SerializeField] private TMP_Text alertMessageText;
    [SerializeField] private Button alertConfirmButton;
    
    [Header("Confirm Dialog UI")]
    [SerializeField] private TMP_Text confirmTitleText;
    [SerializeField] private TMP_Text confirmMessageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TMP_Text confirmButtonText;
    [SerializeField] private TMP_Text cancelButtonText;
    
    [Header("배경")]
    [SerializeField] private GameObject backgroundPanel;

    private Action onConfirmAction;
    private Action onCancelAction;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePopup();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializePopup()
    {
        // 모든 팝업 패널 비활성화
        if (alertDialogPanel != null) alertDialogPanel.SetActive(false);
        if (confirmDialogPanel != null) confirmDialogPanel.SetActive(false);
        if (backgroundPanel != null) backgroundPanel.SetActive(false);

        // 버튼 이벤트 연결
        if (alertConfirmButton != null)
        {
            alertConfirmButton.onClick.RemoveAllListeners();
            alertConfirmButton.onClick.AddListener(OnAlertConfirm);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirm);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(OnCancel);
        }
    }

    /// <summary>
    /// 알림 다이얼로그 표시 (확인 버튼만)
    /// </summary>
    public void ShowAlertDialog(string title, string message, Action onConfirm = null)
    {
        if (alertDialogPanel == null) return;

        alertTitleText.text = title;
        alertMessageText.text = message;
        onConfirmAction = onConfirm;

        backgroundPanel.SetActive(true);
        alertDialogPanel.SetActive(true);
    }

    /// <summary>
    /// 확인/취소 다이얼로그 표시
    /// </summary>
    public void ShowConfirmDialog(string message, string confirmText = "확인", string cancelText = "취소", 
        Action onConfirm = null, Action onCancel = null)
    {
        Debug.Log($"[PopupController] ShowConfirmDialog 호출됨 - message: {message}");
        
        if (confirmDialogPanel == null)
        {
            Debug.LogError("[PopupController] confirmDialogPanel이 null입니다! 인스펙터에서 할당하세요.");
            return;
        }

        if (confirmTitleText == null) Debug.LogError("[PopupController] confirmTitleText가 null입니다!");
        if (confirmMessageText == null) Debug.LogError("[PopupController] confirmMessageText가 null입니다!");
        if (confirmButtonText == null) Debug.LogError("[PopupController] confirmButtonText가 null입니다!");
        if (cancelButtonText == null) Debug.LogError("[PopupController] cancelButtonText가 null입니다!");
        if (backgroundPanel == null) Debug.LogError("[PopupController] backgroundPanel이 null입니다!");

        confirmTitleText.text = "";
        confirmMessageText.text = message;
        confirmButtonText.text = confirmText;
        cancelButtonText.text = cancelText;

        onConfirmAction = onConfirm;
        onCancelAction = onCancel;

        backgroundPanel.SetActive(true);
        confirmDialogPanel.SetActive(true);
        
        Debug.Log("[PopupController] 팝업 패널 활성화 완료");
    }

    /// <summary>
    /// GPGS 로그인 실패 팝업
    /// </summary>
    public void ShowGPGSFailedDialog(Action onGuestLogin, Action onRetry)
    {
        Debug.Log("[PopupController] ShowGPGSFailedDialog 호출됨");
        string message = "GPGS 로그인이 실패 또는 불가하여 게스트 로그인을 시도합니다. 진행하시겠습니까?";
        ShowConfirmDialog(message, "게스트 로그인", "GPGS 재시도", onGuestLogin, onRetry);
    }

    /// <summary>
    /// 게스트 계정 안내 팝업
    /// </summary>
    public void ShowGuestWarningDialog()
    {
        string message = "게스트 계정으로 사용 중 입니다, 계정연동을 하지 않으면 결제 및 게임 데이터가 손실 될 수 있습니다";
        ShowAlertDialog("게스트 계정 안내", message);
    }

    /// <summary>
    /// 계정 연동 결과 팝업
    /// </summary>
    public void ShowAccountLinkingResultDialog(bool success, string message)
    {
        string title = success ? "계정 연동 성공" : "계정 연동 실패";
        ShowAlertDialog(title, message);
    }

    private void OnAlertConfirm()
    {
        HideAllPopups();
        onConfirmAction?.Invoke();
        onConfirmAction = null;
    }

    private void OnConfirm()
    {
        HideAllPopups();
        onConfirmAction?.Invoke();
        onConfirmAction = null;
        onCancelAction = null;
    }

    private void OnCancel()
    {
        HideAllPopups();
        onCancelAction?.Invoke();
        onConfirmAction = null;
        onCancelAction = null;
    }

    /// <summary>
    /// 모든 팝업 숨기기
    /// </summary>
    public void HideAllPopups()
    {
        if (alertDialogPanel != null) alertDialogPanel.SetActive(false);
        if (confirmDialogPanel != null) confirmDialogPanel.SetActive(false);
        if (backgroundPanel != null) backgroundPanel.SetActive(false);
    }

    /// <summary>
    /// 백그라운드 클릭으로 팝업 닫기
    /// </summary>
    public void OnBackgroundClicked()
    {
        // 확인/취소 다이얼로그는 백그라운드 클릭으로 닫지 않음
        if (confirmDialogPanel != null && confirmDialogPanel.activeInHierarchy)
            return;

        HideAllPopups();
    }
}
