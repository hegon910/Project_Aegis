using System;
using System.Collections;
using System.Collections.Generic;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
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
                GameManager.instance.OnNewGameButtonClicked();
            }

        });
    }
#endif

    /// <summary>
    /// GPGS로그인  
    /// </summary>

    /// <summary>
    /// 익명 로그인
    /// </summary>

    /// <summary>
    /// 익명 -> GPGS 계정 연결
    /// </summary>
}

