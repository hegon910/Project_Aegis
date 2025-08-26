using System.Collections;
using System.Collections.Generic;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;

public class BackendManager : MonoBehaviour
{
    public static BackendManager Instance { get; private set; }
    public static FirebaseAuth Auth { get; private set; }

    private void Awake()
    {
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

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("파이어베이스 설정 실패 " + task.Exception);
                return;
            }

            Debug.Log("파이어베이스 설정 성공");
            Auth = FirebaseAuth.DefaultInstance;
        });
    }

    //익명로그인
    //이메일로그인
    
    
}
