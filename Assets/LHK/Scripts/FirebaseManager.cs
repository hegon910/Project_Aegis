using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using GooglePlayGames;
using GooglePlayGames.BasicApi;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }
    public static FirebaseAuth Auth { get; private set; }
    public static FirebaseDatabase Database { get; private set; }
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
                    Debug.Log($"Firebase 인증완료: {User.DisplayName} ({User.UserId})");

                    // DataManager에 로그인 완료 알림
                    if (DataManager.Instance != null)
                    {
                        Debug.Log("[FirebaseManager] DataManager에 로그인 완료 알림 전송");
                       // DataManager.Instance.OnFirebaseLoginCompleted();
                    }

                    GameManager.instance.OnTitlePanelTouched();
                });
            }
            else
            {
                Debug.LogError("인증코드 발급실패");
            }
        });
    }
}