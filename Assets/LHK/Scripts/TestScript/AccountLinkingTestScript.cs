using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 안드로이드 빌드 환경에서 게스트 계정과 GPGS 연동 기능을 테스트하기 위한 스크립트
/// 에디터에서는 동작하지 않으며, 안드로이드 빌드에서만 사용
/// </summary>
public class AccountLinkingTestScript : MonoBehaviour
{
    [Header("Test UI Elements")]
    [SerializeField] private Button guestLoginButton;
    [SerializeField] private Button gpgsLoginButton;
    [SerializeField] private Button linkAccountButton;
    [SerializeField] private Button logoutButton;
    [SerializeField] private Button resetDataButton;
    [SerializeField] private Button showStatusButton;
    
    [Header("Status Display")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI accountInfoText;
    [SerializeField] private ScrollRect statusScrollRect;
    
    [Header("Test Panel")]
    [SerializeField] private GameObject testPanel;
    [SerializeField] private Button toggleTestPanelButton;
    
    private void Start()
    {
        // FirebaseManager 초기화 완료 대기
        FirebaseManager.OnFirebaseManagerInitialized += InitializeTestUI;
        
        // 로그인 상태 변경 이벤트 구독
        FirebaseManager.OnLoginStateChanged += OnLoginStateChanged;
        
        // 테스트 패널 초기에는 비활성화
        if (testPanel != null)
            testPanel.SetActive(false);
    }
    
    private void OnDestroy()
    {
        // 이벤트 구독 해제
        FirebaseManager.OnFirebaseManagerInitialized -= InitializeTestUI;
        FirebaseManager.OnLoginStateChanged -= OnLoginStateChanged;
    }
    
    /// <summary>
    /// FirebaseManager 초기화 완료 후 테스트 UI 초기화
    /// </summary>
    private void InitializeTestUI()
    {
        Debug.Log("[AccountLinkingTestScript] 테스트 UI 초기화 시작");
        
        // 버튼 이벤트 연결
        if (guestLoginButton != null)
            guestLoginButton.onClick.AddListener(TestGuestLogin);
            
        if (gpgsLoginButton != null)
            gpgsLoginButton.onClick.AddListener(TestGPGSLogin);
            
        if (linkAccountButton != null)
            linkAccountButton.onClick.AddListener(TestAccountLinking);
            
        if (logoutButton != null)
            logoutButton.onClick.AddListener(TestLogout);
            
        if (resetDataButton != null)
            resetDataButton.onClick.AddListener(ResetTestData);
            
        if (showStatusButton != null)
            showStatusButton.onClick.AddListener(ShowCurrentStatus);
            
        if (toggleTestPanelButton != null)
            toggleTestPanelButton.onClick.AddListener(ToggleTestPanel);
        
        // 초기 상태 업데이트
        UpdateUI();
        
        AddLog("테스트 스크립트 초기화 완료");
    }
    
    /// <summary>
    /// 로그인 상태 변경 시 UI 업데이트
    /// </summary>
    private void OnLoginStateChanged(LoginType loginType)
    {
        UpdateUI();
        AddLog($"로그인 상태 변경: {loginType}");
    }
    
    /// <summary>
    /// UI 상태 업데이트
    /// </summary>
    private void UpdateUI()
    {
        bool isLoggedIn = FirebaseManager.IsLoggedIn;
        bool isGuest = FirebaseManager.IsGuestAccount;
        bool isLinked = FirebaseManager.IsLinkedAccount;
        
        // 버튼 활성화/비활성화
        if (guestLoginButton != null)
            guestLoginButton.interactable = !isLoggedIn;
            
        if (gpgsLoginButton != null)
            gpgsLoginButton.interactable = !isLoggedIn;
            
        if (linkAccountButton != null)
            linkAccountButton.interactable = isGuest;
            
        if (logoutButton != null)
            logoutButton.interactable = isLoggedIn;
        
        // 계정 정보 표시
        UpdateAccountInfo();
    }
    
    /// <summary>
    /// 계정 정보 업데이트
    /// </summary>
    private void UpdateAccountInfo()
    {
        if (accountInfoText == null) return;
        
        if (FirebaseManager.User != null)
        {
            string info = $"UID: {FirebaseManager.User.UserId}\n";
            info += $"로그인 타입: {FirebaseManager.CurrentLoginType}\n";
            info += $"생성 시간: {FirebaseManager.User.Metadata.CreationTimestamp}\n";
            info += $"마지막 로그인: {FirebaseManager.User.Metadata.LastSignInTimestamp}\n";
            
            if (!string.IsNullOrEmpty(FirebaseManager.User.DisplayName))
                info += $"표시 이름: {FirebaseManager.User.DisplayName}\n";
                
            if (!string.IsNullOrEmpty(FirebaseManager.User.Email))
                info += $"이메일: {FirebaseManager.User.Email}\n";
            
            accountInfoText.text = info;
        }
        else
        {
            accountInfoText.text = "로그인되지 않음";
        }
    }
    
    #region Test Methods
    
    /// <summary>
    /// 게스트 로그인 테스트
    /// </summary>
    public void TestGuestLogin()
    {
        AddLog("게스트 로그인 테스트 시작");
        FirebaseManager.Instance.AnonymousLogin();
    }
    
    /// <summary>
    /// GPGS 로그인 테스트
    /// </summary>
    public void TestGPGSLogin()
    {
        AddLog("GPGS 로그인 테스트 시작");
        FirebaseManager.Instance.GPGSLogin();
    }
    
    /// <summary>
    /// 계정 연동 테스트
    /// </summary>
    public void TestAccountLinking()
    {
        if (!FirebaseManager.IsGuestAccount)
        {
            AddLog("에러: 게스트 계정이 아닙니다. 계정 연동을 할 수 없습니다.");
            return;
        }
        
        AddLog("계정 연동 테스트 시작");
        FirebaseManager.Instance.LinkGuestToGPGS();
    }
    
    /// <summary>
    /// 로그아웃 테스트
    /// </summary>
    public void TestLogout()
    {
        AddLog("로그아웃 테스트 시작");
        
        if (FirebaseManager.Auth != null)
        {
            FirebaseManager.Auth.SignOut();
            //FirebaseManager.CurrentLoginType = LoginType.None;
            //FirebaseManager.User = null;
            
            // PlayerPrefs에서 로그인 타입 제거
            PlayerPrefs.DeleteKey("LoginType");
            PlayerPrefs.Save();
            
            UpdateUI();
            AddLog("로그아웃 완료");
        }
    }
    
    /// <summary>
    /// 현재 상태 표시
    /// </summary>
    public void ShowCurrentStatus()
    {
        string status = $"=== 현재 상태 ===\n";
        status += $"로그인 상태: {FirebaseManager.CurrentLoginType}\n";
        status += $"로그인 여부: {FirebaseManager.IsLoggedIn}\n";
        status += $"게스트 계정: {FirebaseManager.IsGuestAccount}\n";
        status += $"연동 계정: {FirebaseManager.IsLinkedAccount}\n";
        
        if (FirebaseManager.User != null)
        {
            status += $"UID: {FirebaseManager.User.UserId}\n";
            status += $"생성 시간: {FirebaseManager.User.Metadata.CreationTimestamp}\n";
        }
        
        AddLog(status);
    }
    
    /// <summary>
    /// 테스트 데이터 리셋
    /// </summary>
    public void ResetTestData()
    {
        AddLog("테스트 데이터 리셋 시작");
        
        // 로그아웃
        if (FirebaseManager.Auth != null)
        {
            FirebaseManager.Auth.SignOut();
        }
        
        // 모든 PlayerPrefs 데이터 삭제
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        
        // 상태 초기화
        //FirebaseManager.CurrentLoginType = LoginType.None;
        //FirebaseManager.User = null;
        
        UpdateUI();
        AddLog("테스트 데이터 리셋 완료");
    }
    
    /// <summary>
    /// 테스트 패널 토글
    /// </summary>
    public void ToggleTestPanel()
    {
        if (testPanel != null)
        {
            testPanel.SetActive(!testPanel.activeSelf);
            AddLog($"테스트 패널 {(testPanel.activeSelf ? "열림" : "닫힘")}");
        }
    }
    
    #endregion
    
    #region Utility Methods
    
    /// <summary>
    /// 로그 메시지 추가
    /// </summary>
    private void AddLog(string message)
    {
        if (statusText == null) return;
        
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        string logMessage = $"[{timestamp}] {message}\n";
        
        statusText.text += logMessage;
        
        // 스크롤을 맨 아래로 이동
        if (statusScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            statusScrollRect.verticalNormalizedPosition = 0f;
        }
        
        Debug.Log($"[AccountLinkingTestScript] {message}");
    }
    
    /// <summary>
    /// 로그 초기화
    /// </summary>
    public void ClearLog()
    {
        if (statusText != null)
        {
            statusText.text = "";
        }
    }
    
    #endregion
    
    #region Test Scenarios
    
    /// <summary>
    /// 시나리오 1: 게스트 로그인 → 계정 연동 → 로그아웃 → GPGS 로그인
    /// </summary>
    public void TestScenario1()
    {
        AddLog("=== 시나리오 1 시작 ===");
        AddLog("1단계: 게스트 로그인");
        TestGuestLogin();
        
        // 실제로는 코루틴이나 이벤트를 통해 순차적으로 실행해야 함
        // 여기서는 예시로만 표시
    }
    
    /// <summary>
    /// 시나리오 2: GPGS 로그인 → 로그아웃 → 게스트 로그인
    /// </summary>
    public void TestScenario2()
    {
        AddLog("=== 시나리오 2 시작 ===");
        AddLog("1단계: GPGS 로그인");
        TestGPGSLogin();
    }
    
    /// <summary>
    /// 시나리오 3: 게스트 로그인 → 데이터 생성 → 계정 연동 → 데이터 확인
    /// </summary>
    public void TestScenario3()
    {
        AddLog("=== 시나리오 3 시작 ===");
        AddLog("1단계: 게스트 로그인");
        TestGuestLogin();
    }
    
    #endregion
    
    #region Advanced Test Features
    
    /// <summary>
    /// 자동 테스트 시퀀스 실행
    /// </summary>
    public void RunAutomaticTestSequence()
    {
        StartCoroutine(AutomaticTestSequence());
    }
    
    private System.Collections.IEnumerator AutomaticTestSequence()
    {
        AddLog("=== 자동 테스트 시퀀스 시작 ===");
        
        // 1. 게스트 로그인
        AddLog("1단계: 게스트 로그인");
        TestGuestLogin();
        yield return new WaitForSeconds(2f);
        
        // 2. 계정 연동
        if (FirebaseManager.IsGuestAccount)
        {
            AddLog("2단계: 계정 연동");
            TestAccountLinking();
            yield return new WaitForSeconds(3f);
        }
        
        // 3. 로그아웃
        AddLog("3단계: 로그아웃");
        TestLogout();
        yield return new WaitForSeconds(1f);
        
        // 4. GPGS 로그인
        AddLog("4단계: GPGS 로그인");
        TestGPGSLogin();
        yield return new WaitForSeconds(3f);
        
        AddLog("=== 자동 테스트 시퀀스 완료 ===");
    }
    
    /// <summary>
    /// 성능 테스트 - 반복 로그인/로그아웃
    /// </summary>
    public void RunPerformanceTest()
    {
        StartCoroutine(PerformanceTest());
    }
    
    private System.Collections.IEnumerator PerformanceTest()
    {
        AddLog("=== 성능 테스트 시작 ===");
        
        for (int i = 0; i < 5; i++)
        {
            AddLog($"테스트 {i + 1}/5");
            
            // 게스트 로그인
            TestGuestLogin();
            yield return new WaitForSeconds(1f);
            
            // 로그아웃
            TestLogout();
            yield return new WaitForSeconds(0.5f);
        }
        
        AddLog("=== 성능 테스트 완료 ===");
    }
    
    /// <summary>
    /// 에러 상황 테스트
    /// </summary>
    public void TestErrorScenarios()
    {
        AddLog("=== 에러 상황 테스트 시작 ===");
        
        // 1. 로그인되지 않은 상태에서 계정 연동 시도
        if (!FirebaseManager.IsLoggedIn)
        {
            AddLog("에러 테스트 1: 로그인되지 않은 상태에서 계정 연동 시도");
            TestAccountLinking();
        }
        
        // 2. GPGS 계정에서 계정 연동 시도
        if (FirebaseManager.CurrentLoginType == LoginType.GPGS)
        {
            AddLog("에러 테스트 2: GPGS 계정에서 계정 연동 시도");
            TestAccountLinking();
        }
        
        AddLog("=== 에러 상황 테스트 완료 ===");
    }
    
    #endregion
}
