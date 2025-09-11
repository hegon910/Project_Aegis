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
                Debug.LogError("?åå?ù¥?ñ¥Î≤†Ïù¥?ä§ ?Ñ§?†ï ?ã§?å® " + task.Exception);
                return;
            }

            Debug.Log("?åå?ù¥?ñ¥Î≤†Ïù¥?ä§ ?Ñ§?†ï ?Ñ±Í≥?");
            Auth = FirebaseAuth.DefaultInstance;
            //Database = FirebaseDatabase.DefaultInstance;

#if !UNITY_EDITOR
            // v2.1.0?óê?Ñú?äî ActivateÎß? ?ò∏Ï∂úÌïòÎ©? ?ê®
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
        Debug.Log("?ù¥Î©îÏùº Î°úÍ∑∏?ù∏ Î≤ÑÌäº ?Å¥Î¶??ê®");

        if (string.IsNullOrEmpty(idInput.text) || string.IsNullOrEmpty(passwordInput.text))
        {
            Debug.Log("?ïÑ?ù¥?îî ?òê?äî ÎπÑÎ??Î≤àÌò∏Í∞? ?ûÖ?†•?êòÏß? ?ïä?ùå");
            return;
        }

        Auth.SignInWithEmailAndPasswordAsync(idInput.text, passwordInput.text)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled)
                {
                    Debug.LogError("Î°úÍ∑∏?ù∏ Ï∑®ÏÜå?ê®");
                    return;
                }
                if (task.IsFaulted)
                {
                    Debug.LogError("Î°úÍ∑∏?ù∏ ?ã§?å®" + task.Exception);
                    return;
                }

                Debug.Log("Î°úÍ∑∏?ù∏ ?Ñ±Í≥?");
                User = task.Result.User;
                loginPanel.SetActive(false);
                GameManager.instance.StartNewGame();
            });
    }
#endif

    // ---------------------------
    // GPGS + Firebase Î°úÍ∑∏?ù∏ Î∂?Î∂?
    // ---------------------------

    public void GPGSLogin()
    {
#if UNITY_EDITOR
        Debug.LogWarning("?óê?îî?Ñ∞?óê?Ñú?äî GPGS Î°úÍ∑∏?ù∏ Î∂àÍ??");
#else
        PlayGamesPlatform.Instance.Authenticate(status =>
        {
            if (status == SignInStatus.Success)
            {
                Debug.Log("GPGS Î°úÍ∑∏?ù∏ ?Ñ±Í≥?");
                RequestAuthCodeAndSignInFirebase();
            }
            else
            {
                Debug.LogError("GPGS Î°úÍ∑∏?ù∏ ?ã§?å®: " + status);
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
                Debug.Log("?ÑúÎ≤? ?ù∏Ï¶? ÏΩîÎìú: " + authCode);

                var credential = PlayGamesAuthProvider.GetCredential(authCode);
                Auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(task =>
                {
                    if (task.IsCanceled)
                    {
                        Debug.LogError("Firebase Î°úÍ∑∏?ù∏ Ï∑®ÏÜå?ê®");
                        return;
                    }
                    if (task.IsFaulted)
                    {
                        Debug.LogError("Firebase Î°úÍ∑∏?ù∏ ?ã§?å®: " + task.Exception);
                        return;
                    }

                    User = task.Result;
                    Debug.Log($"Firebase Î°úÍ∑∏?ù∏ ?Ñ±Í≥?: {User.DisplayName} ({User.UserId})");

                    GameManager.instance.OnTitlePanelTouched();
                });
            }
            else
            {
                Debug.LogError("?ÑúÎ≤? ?ù∏Ï¶? ÏΩîÎìú Î∞úÍ∏â ?ã§?å®");
            }
        });
    }
}