using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase;
using Firebase.Auth;
//using Firebase.Database;
using Firebase.Extensions;
using GooglePlayGames;
using GooglePlayGames.BasicApi;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }
    public static FirebaseAuth Auth { get; private set; }
    //public static FirebaseDatabase Database { get; private set; }
    public static FirebaseUser User { get; private set; }

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
        openloginPnanelButton.onClick.AddListener(OpenLoginPanel);
        emailLoginButton.onClick.AddListener(EmailLogin);
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
                Debug.LogError("파이어베이스 설정 실패 " + task.Exception);
                return;
            }

            Debug.Log("파이어베이스 설정 성공");
            Auth = FirebaseAuth.DefaultInstance;
            //Database = FirebaseDatabase.DefaultInstance;

#if !UNITY_EDITOR
            // v2.1.0에서는 Activate만 호출하면 됨
            PlayGamesPlatform.Activate();
#endif
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
        Debug.Log("이메일 로그인 버튼 클릭됨");

        if (string.IsNullOrEmpty(idInput.text) || string.IsNullOrEmpty(passwordInput.text))
        {
            Debug.Log("아이디 또는 비밀번호가 입력되지 않음");
            return;
        }

        Auth.SignInWithEmailAndPasswordAsync(idInput.text, passwordInput.text)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled)
                {
                    Debug.LogError("로그인 취소됨");
                    return;
                }
                if (task.IsFaulted)
                {
                    Debug.LogError("로그인 실패" + task.Exception);
                    return;
                }

                Debug.Log("로그인 성공");
                User = task.Result.User;
                loginPanel.SetActive(false);
                GameManager.instance.OnTitlePanelTouched();
            });
    }
#endif

    // ---------------------------
    // GPGS + Firebase 로그인 부분
    // ---------------------------

    public void GPGSLogin()
    {
#if UNITY_EDITOR
        Debug.LogWarning("에디터에서는 GPGS 로그인 불가");
#else
        PlayGamesPlatform.Instance.Authenticate(status =>
        {
            if (status == SignInStatus.Success)
            {
                Debug.Log("GPGS 로그인 성공");
                RequestAuthCodeAndSignInFirebase();
            }
            else
            {
                Debug.LogError("GPGS 로그인 실패: " + status);
            }
        });
#endif
    }

    private void RequestAuthCodeAndSignInFirebase()
    {
        PlayGamesPlatform.Instance.RequestServerSideAccess(true, authCode =>
        {
            if (!string.IsNullOrEmpty(authCode))
            {
                Debug.Log("서버 인증 코드: " + authCode);

                var credential = PlayGamesAuthProvider.GetCredential(authCode);
                Auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(task =>
                {
                    if (task.IsCanceled)
                    {
                        Debug.LogError("Firebase 로그인 취소됨");
                        return;
                    }
                    if (task.IsFaulted)
                    {
                        Debug.LogError("Firebase 로그인 실패: " + task.Exception);
                        return;
                    }

                    User = task.Result;
                    Debug.Log($"Firebase 로그인 성공: {User.DisplayName} ({User.UserId})");

                    GameManager.instance.OnTitlePanelTouched();
                });
            }
            else
            {
                Debug.LogError("서버 인증 코드 발급 실패");
            }
        });
    }
}