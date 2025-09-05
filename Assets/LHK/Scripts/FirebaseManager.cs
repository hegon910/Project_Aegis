using System;
using System.Collections;
using System.Collections.Generic;

using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEngine.SocialPlatforms;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;


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
               Debug.LogError("파이어베이스 설정 실패 " + task.Exception);
               return;
           }
           else
           {
               Debug.Log("파이어베이스 설정 성공");
               Auth = FirebaseAuth.DefaultInstance;
               Database = FirebaseDatabase.DefaultInstance;
           }
       });
    }


    /// <summary>
    /// 이메일 로그인
    /// 에디터에서만 개발용으로 사용
    /// </summary>
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

        Auth.SignInWithEmailAndPasswordAsync(idInput.text, passwordInput.text).ContinueWithOnMainThread(task =>
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
            else
            {
                Debug.Log($"로그인 성공 ");
                User = task.Result.User;

                loginPanel.SetActive(false);
                GameManager.instance.OnTitlePanelTouched();
            }

        });
    }
#endif

    public void GPGSLogin()
    {
        PlayGamesPlatform.Instance.Authenticate(ProcessAuthentication);
        // if (!Social.localUser.authenticated)
        // {
        // }
        // else
        // {
        //     Debug.Log("이미 로그인 되어있음");
        //     GetServerAuthCodeAndSignInFirebase();
        // }
    }

    private void ProcessAuthentication(SignInStatus status)
    {
        if (status == SignInStatus.Success)
        {
            Debug.Log("GPGS 로그인 성공, 서버인증코드 요청");
            //GetServerAuthCodeAndSignInFirebase();
        }
        else
        {
            Debug.LogError("GPGS 로그인 실패: " + status);
            //TODO로그인 실패 UI
        }
    }

    private void GetServerAuthCodeAndSignInFirebase()
    {
        //서버 인증 코드 요청
        PlayGamesPlatform.Instance.RequestServerSideAccess(false, authCode =>
        {
            if (!string.IsNullOrEmpty(authCode))
            {
                Debug.Log($"서버 인증 코드 받음 {authCode}, Firebase 로그인 시도");

                //서버 인증 코드로 파이어베이스 Credential 생성
                Credential credential = PlayGamesAuthProvider.GetCredential(authCode);

                //파이어베이스에 Credential로 로그인
                Auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(task =>
                {
                    if (task.IsCanceled)
                    {
                        Debug.LogError("Firebase 로그인 취소됨");
                        return;
                    }
                    if (task.IsFaulted)
                    {
                        Debug.LogError("Firebase 로그인 실패" + task.Exception);
                        return;
                    }
                    else
                    {
                        Debug.Log($"Firebase 로그인 성공 ");
                        User = task.Result;

                        GameManager.instance.OnTitlePanelTouched();
                    }

                });
            }
            else
            {
                Debug.LogError("서버 인증 코드 받기 실패");
                //TODO 서버 인증 코드 받기 실패 UI
            }
        });
    }

    public void anonymousLogin()
    {
        Auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("익명 로그인 취소됨");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("익명 로그인 실패" + task.Exception);
                return;
            }
            else
            {
                Debug.Log($"익명 로그인 성공 ");
                User = task.Result.User;

                GameManager.instance.OnTitlePanelTouched();
            }

        });
    }


    /// <summary>
    /// 익명 로그인
    /// </summary>

    /// <summary>
    /// 익명 -> GPGS 계정 연결
    /// </summary>
}

