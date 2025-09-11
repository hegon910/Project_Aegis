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
                Debug.LogError("?��?��?��베이?�� ?��?�� ?��?�� " + task.Exception);
                return;
            }

            Debug.Log("?��?��?��베이?�� ?��?�� ?���?");
            Auth = FirebaseAuth.DefaultInstance;
            Database = FirebaseDatabase.DefaultInstance;

#if !UNITY_EDITOR
            // v2.1.0?��?��?�� Activate�? ?��출하�? ?��
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
        Debug.Log("?��메일 로그?�� 버튼 ?���??��");

        if (string.IsNullOrEmpty(idInput.text) || string.IsNullOrEmpty(passwordInput.text))
        {
            Debug.Log("?��?��?�� ?��?�� 비�??번호�? ?��?��?���? ?��?��");
            return;
        }

        Auth.SignInWithEmailAndPasswordAsync(idInput.text, passwordInput.text)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled)
                {
                    Debug.LogError("로그?�� 취소?��");
                    return;
                }
                if (task.IsFaulted)
                {
                    Debug.LogError("로그?�� ?��?��" + task.Exception);
                    return;
                }

                Debug.Log("로그?�� ?���?");
                User = task.Result.User;
                loginPanel.SetActive(false);
                GameManager.instance.StartNewGame();
            });
    }
#endif

    // ---------------------------
    // GPGS + Firebase 로그?�� �?�?
    // ---------------------------

    public void GPGSLogin()
    {
#if UNITY_EDITOR
        Debug.LogWarning("?��?��?��?��?��?�� GPGS 로그?�� 불�??");
#else
        PlayGamesPlatform.Instance.Authenticate(status =>
        {
            if (status == SignInStatus.Success)
            {
                Debug.Log("GPGS 로그?�� ?���?");
                RequestAuthCodeAndSignInFirebase();
            }
            else
            {
                Debug.LogError("GPGS 로그?�� ?��?��: " + status);
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
                Debug.Log("?���? ?���? 코드: " + authCode);

                var credential = PlayGamesAuthProvider.GetCredential(authCode);
                Auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(task =>
                {
                    if (task.IsCanceled)
                    {
                        Debug.LogError("Firebase 로그?�� 취소?��");
                        return;
                    }
                    if (task.IsFaulted)
                    {
                        Debug.LogError("Firebase 로그?�� ?��?��: " + task.Exception);
                        return;
                    }

                    User = task.Result;
                    Debug.Log($"Firebase 로그?�� ?���?: {User.DisplayName} ({User.UserId})");

                    GameManager.instance.OnTitlePanelTouched();
                });
            }
            else
            {
                Debug.LogError("?���? ?���? 코드 발급 ?��?��");
            }
        });
    }
}