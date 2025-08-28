using System;
using System.Collections;
using System.Collections.Generic;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;

public class BackendManager : MonoBehaviour
{
    public static BackendManager Instance { get; private set; }
    public static FirebaseAuth Auth { get; private set; }
    public static FirebaseDatabase Database { get; private set; }

    private UserData data = new UserData();

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

    private void SaveAll() //유저 데이터 저장, 데이터 변동이 발생하는 곳마다 호출..누락하지 않고 어떻게 할 수 있을까?
    {
        //1. 게임종료 시 
        //1. 수치 변동시 ex)파라미터 이벤트, 전투 승패, 전세, 카르마 
        //2. 특정 조건 관련 플래그 획득 시 ex)특수이벤트조건, 2회차조건, 진엔딩조건, 히든엔딩조건 
        //3. 회차 정보 변동시
        //4. 보유 스킬 목록 변동시 
        //5. 서브이벤트팩 보유? 이건 유저데이터가 맞나?
        //6. 업적관련 정보

        FirebaseUser user = Auth.CurrentUser;
        DatabaseReference root = Database.RootReference;
        DatabaseReference userInfo = root.Child("UserData").Child(user.UserId);

        string json = JsonUtility.ToJson(data);
        userInfo.SetRawJsonValueAsync(json);
    }
    
    

}

[Serializable]
public class UserData
{
    //저장되어야할 사항 구체화 필요
    public string userId;
    public string email;
    public int level;
    public int exp;
    public int gold;
    public int gem;
    public List<string> skill;


    public UserData() { }

    public UserData(string userId, string email)
    {
        this.userId = userId;
        this.email = email;
        level = 1;
        exp = 0;
        gold = 1000;
        gem = 100;

    }
}
