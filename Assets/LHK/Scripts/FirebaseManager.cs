using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using System;

public enum LoginType
{
    None,           // 로그인 안됨
    GPGS,           // GPGS 계정 로그인
    Guest,          // 게스트 계정 로그인
    Linked          // 게스트에서 GPGS로 연동된 계정
}

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }
    public static FirebaseAuth Auth { get; private set; }
    public static FirebaseDatabase Database { get; private set; }
    public static FirebaseUser User { get; private set; }
    
    // 로그인 상태 관리
    public static LoginType CurrentLoginType { get; private set; } = LoginType.None;
    public static bool IsGuestAccount => CurrentLoginType == LoginType.Guest;
    public static bool IsLinkedAccount => CurrentLoginType == LoginType.Linked;
    public static bool IsLoggedIn => CurrentLoginType != LoginType.None;
    
    // 이벤트
    public static event Action<LoginType> OnLoginStateChanged;
    public static event Action OnGuestWarningShown;

#if UNITY_EDITOR
    [Header("Login Input Field")]
    [SerializeField] TMP_InputField idInput;
    [SerializeField] TMP_InputField passwordInput;
    [SerializeField] Button openloginPnanelButton;
    [SerializeField] GameObject loginPanel;
    [SerializeField] GameObject funtionPanel;
    [SerializeField] Button emailLoginButton;
#endif

    private void Awake()
    {
#if UNITY_EDITOR
        //openloginPnanelButton.onClick.AddListener(OpenLoginPanel);
        //emailLoginButton.onClick.AddListener(EmailLogin);
#endif

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                return;
            }

            Auth = FirebaseAuth.DefaultInstance;
            Database = FirebaseDatabase.DefaultInstance;

#if !UNITY_EDITOR
            PlayGamesPlatform.Activate();
#endif
            
            // 저장된 로그인 타입 복원
            RestoreLoginType();
        });
    }

#if UNITY_EDITOR
    public void OpenLoginPanel()
    {
        funtionPanel.SetActive(false);
        loginPanel.SetActive(true);
    }

    public void EmailLogin()
    {

        if (string.IsNullOrEmpty(idInput.text) || string.IsNullOrEmpty(passwordInput.text))
        {
            return;
        }

        Auth.SignInWithEmailAndPasswordAsync(idInput.text, passwordInput.text)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled)
                {
                    return;
                }
                if (task.IsFaulted)
                {
                    return;
                }

                User = task.Result.User;
                loginPanel.SetActive(false);
                DataManager.Instance.StartNewGame();
            });
    }
#endif



    public void GPGSLogin()
    {
#if UNITY_EDITOR
        Debug.LogWarning("에디터에서는 gpgs 로그인 불가능");
        // 에디터에서는 바로 익명 로그인으로 진행
        ShowGPGSFailedPopup();
#else
        PlayGamesPlatform.Instance.Authenticate(status =>
        {
            if (status == SignInStatus.Success)
            {
                RequestAuthCodeAndSignInFirebase();
            }
            else
            {
                Debug.LogError("GPGS 로그인 실패 원인:" + status);
                ShowGPGSFailedPopup();
            }
        });
#endif
    }
    
    /// <summary>
    /// GPGS 로그인 실패 시 표시되는 팝업
    /// </summary>
    private void ShowGPGSFailedPopup()
    {
        if (PopupController.Instance != null)
        {
            PopupController.Instance.ShowGPGSFailedDialog(
                onGuestLogin: () => AnonymousLogin(),
                onRetry: () => GPGSLogin()
            );
        }
        else
        {
            Debug.LogError("PopupController 인스턴스를 찾을 수 없습니다.");
            // 폴백: 바로 게스트 로그인 진행
            AnonymousLogin();
        }
    }
    
    /// <summary>
    /// Firebase 익명 로그인
    /// </summary>
    public void AnonymousLogin()
    {
        Debug.Log("[FirebaseManager] 익명 로그인 시작");
        
        Auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("익명 로그인 취소됨");
                return;
            }
            
            if (task.IsFaulted)
            {
                Debug.LogError("익명 로그인 실패: " + task.Exception);
                return;
            }

            Firebase.Auth.AuthResult result = task.Result;
            User = result.User;
            SetLoginType(LoginType.Guest);
            
            Debug.Log($"익명 로그인 완료: {User.UserId}");
            Debug.Log($"=== 현재 에디터 익명 계정 정보 ===");
            Debug.Log($"UID: {User.UserId}");
            Debug.Log($"생성 시간: {User.Metadata.CreationTimestamp}");
            Debug.Log($"마지막 로그인: {User.Metadata.LastSignInTimestamp}");
            Debug.Log($"=== Firebase Console에서 이 UID를 검색하세요 ===");
            
            // DataManager에 로그인 완료 알림
            if (DataManager.Instance != null)
            {
                Debug.Log("[FirebaseManager] DataManager에 게스트 로그인 완료 알림 전송");
                // 게스트 계정이므로 서버 동기화는 하지 않음
            }
        });
    }

    private void RequestAuthCodeAndSignInFirebase()
    {
        PlayGamesPlatform.Instance.RequestServerSideAccess(true, authCode =>
        {
            if (!string.IsNullOrEmpty(authCode))
            {
                Debug.Log("인증코드 발급:" + authCode);

                var credential = PlayGamesAuthProvider.GetCredential(authCode);
                Auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(task =>
                {
                    if (task.IsCanceled)
                    {
                        Debug.LogError("Firebase 인증 취소됨");
                        return;
                    }
                    if (task.IsFaulted)
                    {
                        Debug.LogError("Firebase 인증 실패: " + task.Exception);
                        return;
                    }

                    User = task.Result;
                    SetLoginType(LoginType.GPGS);
                    
                    Debug.Log($"Firebase 인증완료: {User.DisplayName} ({User.UserId})");

                    // DataManager에 로그인 완료 알림 및 서버 동기화 시작
                    if (DataManager.Instance != null)
                    {
                        Debug.Log("[FirebaseManager] DataManager에 GPGS 로그인 완료 알림 전송");
                        // GPGS 로그인 완료 후 서버 동기화 시작
                        DataManager.Instance.OnFirebaseLoginCompleted();
                    }
                });
            }
            else
            {
                Debug.LogError("인증코드 발급실패");
                ShowGPGSFailedPopup();
            }
        });
    }
    
    /// <summary>
    /// 계정 연동 기능 - 게스트 계정을 GPGS 계정에 연결
    /// </summary>
    public void LinkGuestToGPGS()
    {
        if (CurrentLoginType != LoginType.Guest)
        {
            Debug.LogWarning("게스트 계정이 아닙니다. 계정 연동을 할 수 없습니다.");
            return;
        }
        
#if UNITY_EDITOR
        Debug.LogWarning("에디터에서는 GPGS 계정 연동 불가능");
        ShowAccountLinkingResult(false, "에디터에서는 계정 연동을 할 수 없습니다.");
#else
        PlayGamesPlatform.Instance.Authenticate(status =>
        {
            if (status == SignInStatus.Success)
            {
                PlayGamesPlatform.Instance.RequestServerSideAccess(true, authCode =>
                {
                    if (!string.IsNullOrEmpty(authCode))
                    {
                        LinkWithGPGS(authCode);
                    }
                    else
                    {
                        ShowAccountLinkingResult(false, "GPGS 인증코드 발급에 실패했습니다.");
                    }
                });
            }
            else
            {
                ShowAccountLinkingResult(false, "GPGS 로그인에 실패했습니다.");
            }
        });
#endif
    }
    
    /// <summary>
    /// 게스트 계정을 GPGS 계정에 연결하는 실제 로직
    /// </summary>
    private void LinkWithGPGS(string authCode)
    {
        var credential = PlayGamesAuthProvider.GetCredential(authCode);
        
        // 현재 게스트 계정에 GPGS 계정 연결
        User.LinkWithCredentialAsync(credential).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                ShowAccountLinkingResult(false, "계정 연동이 취소되었습니다.");
                return;
            }
            
            if (task.IsFaulted)
            {
                Debug.LogError("계정 연동 실패: " + task.Exception);
                ShowAccountLinkingResult(false, "계정 연동에 실패했습니다.");
                return;
            }
            
            User = task.Result.User;
            SetLoginType(LoginType.Linked);
            
            Debug.Log($"계정 연동 완료: {User.DisplayName} ({User.UserId})");
            
            // 연동 완료 후 서버에 데이터 업로드
            UploadGuestDataToServer();
            
            ShowAccountLinkingResult(true, "계정 연동이 완료되었습니다!");
        });
    }
    
    /// <summary>
    /// 계정 연동 결과 팝업 표시
    /// </summary>
    private void ShowAccountLinkingResult(bool success, string message)
    {
        if (PopupController.Instance != null)
        {
            PopupController.Instance.ShowAccountLinkingResultDialog(success, message);
        }
        else
        {
            Debug.LogError("PopupController 인스턴스를 찾을 수 없습니다.");
        }
    }
    
    /// <summary>
    /// 게스트 데이터를 서버에 업로드
    /// </summary>
    private void UploadGuestDataToServer()
    {
        if (DataManager.Instance != null)
        {
            // DataManager의 서버 업로드 메서드 호출
            Debug.Log("[FirebaseManager] 게스트 데이터를 서버에 업로드합니다.");
            DataManager.Instance.UploadGuestDataToServer();
        }
    }
    
    /// <summary>
    /// 로그인 타입 설정 및 이벤트 발생
    /// </summary>
    private void SetLoginType(LoginType loginType)
    {
        CurrentLoginType = loginType;
        OnLoginStateChanged?.Invoke(loginType);
        
        // 로그인 타입을 PlayerPrefs에 저장
        PlayerPrefs.SetInt("LoginType", (int)loginType);
        PlayerPrefs.Save();
        
        Debug.Log($"[FirebaseManager] 로그인 타입 변경: {loginType}");
    }
    
    /// <summary>
    /// 앱 시작 시 저장된 로그인 타입 복원
    /// </summary>
    private void RestoreLoginType()
    {
        if (PlayerPrefs.HasKey("LoginType"))
        {
            CurrentLoginType = (LoginType)PlayerPrefs.GetInt("LoginType");
            OnLoginStateChanged?.Invoke(CurrentLoginType);
            Debug.Log($"[FirebaseManager] 로그인 타입 복원: {CurrentLoginType}");
        }
    }
    
    
    /// <summary>
    /// 게스트 경고 팝업 표시 (메인 패널 진입 시 1회만)
    /// </summary>
    public void ShowGuestWarningPopup()
    {
        if (IsGuestAccount && !HasShownGuestWarning())
        {
            if (PopupController.Instance != null)
            {
                PopupController.Instance.ShowGuestWarningDialog();
            }
            else
            {
                Debug.LogError("PopupController 인스턴스를 찾을 수 없습니다.");
            }
            
            // 경고 팝업 표시 완료 플래그 설정
            PlayerPrefs.SetInt("HasShownGuestWarning", 1);
            PlayerPrefs.Save();
            
            OnGuestWarningShown?.Invoke();
        }
    }
    
    /// <summary>
    /// 게스트 경고 팝업을 이미 표시했는지 확인
    /// </summary>
    private bool HasShownGuestWarning()
    {
        return PlayerPrefs.GetInt("HasShownGuestWarning", 0) == 1;
    }
    
    /// <summary>
    /// 게스트 경고 팝업 표시 상태 초기화 (테스트용)
    /// </summary>
    public void ResetGuestWarningFlag()
    {
        PlayerPrefs.DeleteKey("HasShownGuestWarning");
        PlayerPrefs.Save();
    }
}