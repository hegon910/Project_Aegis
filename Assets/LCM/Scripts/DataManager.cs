using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Firebase.Database;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEditor;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    public GameData PlayerData { get; private set; }
    public SettingsData PlayerSettings { get; private set; }
    private string _playerDataSavePath;
    private string _settingsSavePath;
    private bool _hasSyncedWithServer; // 9.9. 이학권 추가

    private UniTaskCompletionSource<bool> _isReady = new UniTaskCompletionSource<bool>();
    public UniTask IsReady => _isReady.Task;

    public List<SubEventData> SubEvents { get; private set; }

    //메인 이벤트 
    public Dictionary<int, MainEventData> mainEventDataDict;
    public Dictionary<int, AnswerData> answerDataDict;
    //서브 이벤트
    public Dictionary<int, AllSubEventData> subEventDataDict;
    public Dictionary<int, SubEventAnswerData> subEventAnswerDataDict;
    //메인 룩업 테이블
    private Dictionary<int, MainCharacterData> CharacterDataDict;
    private Dictionary<int, BGData> bgDataDict;
    private Dictionary<int, SFXData> sfxDataDict;
    private Dictionary<int, MainCharacterImgData> characterImgDataDict;
    private Dictionary<int, BackData> backDataDict;
    //전투 결과 이벤트
    public Dictionary<int, BattleResultData> battleResultDataDict;
    //엔딩 이벤트
    private Dictionary<long, EndingEventData> endingEventDataDict;
    private Dictionary<long, EndingCutScene> endingCutSceneDict;
    //파라미터 이벤트 
    public Dictionary<int, ParameterEventData> eventDataDict;
    public Dictionary<int, ParameterEventStringData> eventStringDataDict;
    public Dictionary<int, ParameterRewardData> rewardDataDict;
    public Dictionary<int, string> characterNameDict; // 캐릭터 이름 룩업
    public Dictionary<int, string> rewardTypeDict; //보상 타입 룩업
    public Dictionary<int, string> choiceTextDict; //선택지 텍스트 룩업
    public Dictionary<int, string> eventDict; //이벤트타입 룩업
    public Dictionary<int, string> roundTypeDict; // 라운드타입 룩업
    public Dictionary<int, string> pageTypeDict; //페이지타입 룩업

    [Header("메인 스토리 데이터")]
    public List<MainEventData> MainStoryEvents;
    public Dictionary<int, string> MainStoryAnswers;
    public Dictionary<int, string> MainStoryCharacters;
    public Dictionary<int, string> MainStoryDialogues; // Dialogue CSV를 위한 Dictionary
    public Dictionary<int, Sprite> MainStoryCharacterImages; // 이미지 리소스를 위한 Dictionary
    public Dictionary<int, Sprite> MainStoryBGs; // 배경 리소스를 위한 Dictionary
    private Dictionary<int, NewMainEventData> mainEventData = new Dictionary<int, NewMainEventData>();
    // Reset 이후 의도치 않은 저장을 방지하기 위한 플래그
    private bool _suppressSavesUntilGameplay;

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
        }

        // 플레이어 데이터 저장 경로 설정
        _playerDataSavePath = Path.Combine(Application.persistentDataPath, "playerdata.json");
        _settingsSavePath = Path.Combine(Application.persistentDataPath, "settings.json");
        LoadSettings();
        LoadGame();
        if (PlayerData == null)
        {
            Debug.Log("저장된 데이터를 찾을 수 없어 새 게임 데이터를 생성합니다.");
            StartNewGame(); // 혹은 PlayerData = new GameData(); 로 직접 생성
        }
    }

    private void Start() /// 9.9. 이학권 추가
    {
        FirebaseAuth.DefaultInstance.StateChanged += OnAuthStateChanged;
        TrySyncIfLoggedIn();
    }

    private void OnAuthStateChanged(object sender, System.EventArgs e)
    {
       TrySyncIfLoggedIn();
    }

    /// <summary>
    /// Firebase 로그인 완료 시 호출되는 메서드
    /// </summary>
    public void OnFirebaseLoginCompleted()
    {
        Debug.Log("[DataManager] Firebase 로그인 완료 콜백 호출");
        _hasSyncedWithServer = false; // 동기화 플래그 리셋
        TrySyncIfLoggedIn();
    }

    private void TrySyncIfLoggedIn() /// 9.9. 이학권 추가
    {
        var user = FirebaseAuth.DefaultInstance.CurrentUser;
        Debug.Log($"[DataManager] TrySyncIfLoggedIn 호출 - User: {(user != null ? user.UserId : "null")}, HasSynced: {_hasSyncedWithServer}");
        
        if (user != null && !_hasSyncedWithServer)
        {
            // Firebase Database가 초기화되었는지 확인
            if (FirebaseDatabase.DefaultInstance == null)
            {
                Debug.LogWarning("[DataManager] Firebase Database가 아직 초기화되지 않음. 1초 후 재시도...");
                Invoke(nameof(TrySyncIfLoggedIn), 1f);
                return;
            }
            
            _hasSyncedWithServer = true;
            Debug.Log($"[DataManager] 서버 동기화 시작 - UserId: {user.UserId}");
            StartCoroutine(SyncWithServer(user.UserId));
        }
        else if (user == null)
        {
            Debug.Log("[DataManager] Firebase 사용자가 로그인되지 않음");
        }
        else
        {
            Debug.Log("[DataManager] 이미 서버 동기화 완료됨");
        }
    }

    /// <summary>
    /// 새 게임을 시작할 때 호출됩니다.
    /// </summary>
    public void StartNewGame()
    {
        PlayerData = new GameData();
        // 메뉴 단계에서 자동 저장이 일어나지 않도록 저장 억제
        _suppressSavesUntilGameplay = true;
     //   SaveLocal();
        Debug.Log("새로운 게임 데이터 생성 및 저장 완료.");
    }

    /// <summary>
    /// 파일에서 플레이어 데이터를 불러옵니다. 파일이 없으면 새 게임 데이터가 생성됩니다.
    /// 9.9. 이학권 변경 - 암호화 기능 추가 (기존 평문 파일 호환성 포함)
    /// </summary>
    public bool LoadGame() // void -> bool로 변경
    {
        if (File.Exists(_playerDataSavePath))
        {
            try
            {
                string json = null;
                
                // 1. 먼저 암호화된 파일로 시도
                try
                {
                    json = EncryptionUtility.LoadEncryptedFile(_playerDataSavePath);
                    if (!string.IsNullOrEmpty(json))
                    {
                        Debug.Log("암호화된 세이브 파일을 성공적으로 로드했습니다.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"암호화된 파일 로드 실패: {ex.Message}");
                }
                
                // 2. 암호화된 파일 로드 실패 시 평문 파일로 시도 (기존 호환성)
                if (string.IsNullOrEmpty(json))
                {
                    try
                    {
                        json = File.ReadAllText(_playerDataSavePath, Encoding.UTF8);
                        Debug.Log("기존 평문 세이브 파일을 로드했습니다. 다음 저장 시 암호화됩니다.");
                        
                        // 평문 파일을 암호화하여 다시 저장 (마이그레이션)
                        PlayerData = JsonUtility.FromJson<GameData>(json);
                        if (PlayerData != null)
                        {
                            SaveLocal(); // 암호화된 형태로 다시 저장
                            Debug.Log("기존 평문 파일을 암호화된 형태로 마이그레이션 완료.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"평문 파일 로드도 실패: {ex.Message}");
                        json = null;
                    }
                }
                
                // 3. 모든 로드 시도 실패
                if (string.IsNullOrEmpty(json))
                {
                    Debug.LogWarning("모든 세이브 파일 로드 시도 실패, 새 게임을 시작합니다.");
                    StartNewGame();
                    return false;
                }

                // 4. JSON 파싱 (암호화된 파일의 경우)
                if (PlayerData == null)
                {
                    PlayerData = JsonUtility.FromJson<GameData>(json);
                }

                if (PlayerData == null)
                {
                    Debug.LogWarning("세이브 파일이 손상되어 새 게임을 시작합니다.");
                    StartNewGame();
                    return false; // 로드 실패
                }
                else
                {
                    Debug.Log($"게임 데이터 로드 완료. (회차: {PlayerData.playthroughCount}, 챕터: {PlayerData.currentChapter})");
                    return true; // 로드 성공
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"로컬 로드 실패 ({ex.Message}), 새 게임 시작");
                StartNewGame();
                return false; // 로드 실패
            }
        }
        else
        {
            Debug.Log("로컬 데이터 없음, 새 게임 시작");
            StartNewGame();
            return false; // 파일 없음
        }
    }
    /// <summary>
    /// [신규] 설정 데이터를 로컬 파일에서 불러옵니다.
    /// 암호화 기능 추가 (기존 평문 파일 호환성 포함)
    /// </summary>
    public void LoadSettings()
    {
        if (File.Exists(_settingsSavePath))
        {
            try
            {
                string json = null;
                
                // 1. 먼저 암호화된 설정 파일로 시도
                try
                {
                    json = EncryptionUtility.LoadEncryptedFile(_settingsSavePath);
                    if (!string.IsNullOrEmpty(json))
                    {
                        Debug.Log("암호화된 설정 파일을 성공적으로 로드했습니다.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"암호화된 설정 파일 로드 실패: {ex.Message}");
                }
                
                // 2. 암호화된 파일 로드 실패 시 평문 파일로 시도 (기존 호환성)
                if (string.IsNullOrEmpty(json))
                {
                    try
                    {
                        json = File.ReadAllText(_settingsSavePath, Encoding.UTF8);
                        Debug.Log("기존 평문 설정 파일을 로드했습니다. 다음 저장 시 암호화됩니다.");
                        
                        // 평문 파일을 암호화하여 다시 저장 (마이그레이션)
                        PlayerSettings = JsonUtility.FromJson<SettingsData>(json);
                        if (PlayerSettings != null)
                        {
                            SaveSettings(); // 암호화된 형태로 다시 저장
                            Debug.Log("기존 평문 설정 파일을 암호화된 형태로 마이그레이션 완료.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"평문 설정 파일 로드도 실패: {ex.Message}");
                        json = null;
                    }
                }
                
                // 3. 모든 로드 시도 실패
                if (string.IsNullOrEmpty(json))
                {
                    Debug.LogWarning("모든 설정 파일 로드 시도 실패, 기본 설정을 생성합니다.");
                    PlayerSettings = new SettingsData();
                    return;
                }

                // 4. JSON 파싱 (암호화된 파일의 경우)
                if (PlayerSettings == null)
                {
                    PlayerSettings = JsonUtility.FromJson<SettingsData>(json);
                }
                
                if (PlayerSettings == null)
                {
                    Debug.LogWarning("설정 파일이 손상되어 기본 설정을 생성합니다.");
                    PlayerSettings = new SettingsData();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"설정 로드 실패: {ex.Message}. 기본 설정으로 재설정합니다.");
                PlayerSettings = new SettingsData();
            }
        }
        else
        {
            Debug.Log("설정 파일 없음, 기본 설정 생성.");
            PlayerSettings = new SettingsData();
        }
    }
    /// <summary>
    /// [신규] 현재 '설정'을 로컬파일에 저장합니다.
    /// 암호화 기능 추가
    /// </summary>
    public void SaveSettings()
    {
        if (PlayerSettings == null) return;
        string json = JsonUtility.ToJson(PlayerSettings, true);
        
        // 암호화하여 저장
        bool saveSuccess = EncryptionUtility.SaveEncryptedFile(_settingsSavePath, json);
        if (saveSuccess)
        {
            Debug.Log($"암호화된 설정 저장 완료: {_settingsSavePath}");
        }
        else
        {
            Debug.LogError($"설정 저장 실패: {_settingsSavePath}");
        }
    }

    /// <summary>
    /// 세이브 파일이 존재하는지 확인합니다.
    /// </summary>
    public bool CheckIfSaveDataExists()
    {
        return File.Exists(_playerDataSavePath);
    }

    /// <summary>
    /// 로컬 세이브 파일을 삭제합니다.
    /// </summary>
    public void DeleteLocalSaveData()
    {
        if (File.Exists(_playerDataSavePath))
        {
            File.Delete(_playerDataSavePath);
            Debug.Log($"세이브 파일 삭제 완료: {_playerDataSavePath}");
        }
    }

    /// <summary>
    /// 현재 플레이어 데이터를 로컬파일에 저장합니다.
    /// 9.9. 이학권 변경 - 암호화 기능 추가 + 서버 동기화
    /// </summary>
    public void SaveLocal()
    {
        if (PlayerData == null) return;
        if (_suppressSavesUntilGameplay) { Debug.Log("[DataManager] 저장 억제 중(게임 플레이 전). SaveLocal() 건너뜀"); return; }
        PlayerData.lastUpdated = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string json = JsonUtility.ToJson(PlayerData, true);
        
        // 암호화하여 저장
        bool saveSuccess = EncryptionUtility.SaveEncryptedFile(_playerDataSavePath, json);
        if (saveSuccess)
        {
            Debug.Log($"암호화된 로컬 저장 완료: {_playerDataSavePath}");
            
            // Firebase 사용자가 로그인되어 있으면 서버에도 업로드
            var user = FirebaseAuth.DefaultInstance.CurrentUser;
            if (user != null)
            {
                Debug.Log("[DataManager] Firebase 사용자 로그인됨, 서버 동기화 시작");
                StartCoroutine(UploadToServerAsync(user.UserId));
            }
            else
            {
                Debug.Log("[DataManager] Firebase 사용자 로그인되지 않음, 로컬 저장만 완료");
            }
        }
        else
        {
            Debug.LogError($"게임 데이터 저장 실패: {_playerDataSavePath}");
        }
    }
    /// <summary>
    /// [신규] 모든 로컬 데이터(진행도, 설정, PlayerPrefs)를 삭제합니다.
    /// </summary>
    public void DeleteAllLocalData()
    {
        // 1. 게임 진행도 파일 삭제
        if (File.Exists(_playerDataSavePath))
        {
            File.Delete(_playerDataSavePath);
            Debug.Log($"게임 진행도 파일 삭제 완료: {_playerDataSavePath}");
        }

        // 2. 설정 파일 삭제
        if (File.Exists(_settingsSavePath))
        {
            File.Delete(_settingsSavePath);
            Debug.Log($"설정 파일 삭제 완료: {_settingsSavePath}");
        }

        // 3. PlaythroughHistory가 사용하는 PlayerPrefs 기록 삭제
        PlaythroughHistory.Instance.ClearHistory();

        // 초기화 직후에는 저장 생성/덮어쓰기 방지
        _suppressSavesUntilGameplay = true;

        Debug.LogWarning("[DataManager] 모든 로컬 데이터가 초기화되었습니다.");
    }



    /// <summary>
    /// 서버와 로컬데이터 동기화
    /// 9.9. 이학권 추가
    /// </summary>
    private IEnumerator SyncWithServer(string uid)
    {
        Debug.Log($"[DataManager] SyncWithServer 시작 - UID: {uid}");
        
        // 1) 서버에서 데이터 읽기
        var dbRef = FirebaseDatabase.DefaultInstance
            .GetReference($"users/{uid}/playerData");

        Debug.Log($"[DataManager] Firebase Database 참조 생성: users/{uid}/playerData");

        var fetchTask = dbRef.GetValueAsync();
        yield return new WaitUntil(() => fetchTask.IsCompleted);

        if (fetchTask.IsFaulted)
        {
            Debug.LogError($"[DataManager] 서버 데이터 가져오기 실패: {fetchTask.Exception}");
            if (fetchTask.Exception != null)
            {
                Debug.LogError($"[DataManager] 예외 상세: {fetchTask.Exception.InnerException?.Message}");
            }
            yield break;
        }

        DataSnapshot snapshot = fetchTask.Result;
        Debug.Log($"[DataManager] 서버 스냅샷 존재 여부: {snapshot.Exists}");
        
        if (snapshot.Exists)
        {
            // 2) 서버 JSON → GameData
            string serverJson = snapshot.GetRawJsonValue();
            Debug.Log($"[DataManager] 서버에서 받은 JSON 길이: {serverJson?.Length ?? 0}");
            
            var serverData = JsonUtility.FromJson<GameData>(serverJson);

            // 3) 타임스탬프 비교
			// 서버 권위 타임스탬프 우선 비교
			long serverSV = (serverData != null) ? serverData.lastUpdatedServer : 0;
			long localSV = (PlayerData != null) ? PlayerData.lastUpdatedServer : 0;
			bool serverHasSV = serverSV > 0;
			bool localHasSV = localSV > 0;

			Debug.Log($"[DataManager] 타임스탬프 비교 - 서버: {serverSV}, 로컬: {localSV}");

			bool serverIsNewer;
			if (serverHasSV || localHasSV)
			{
				long a = serverHasSV ? serverSV : long.MinValue;
				long b = localHasSV ? localSV : long.MinValue;
				serverIsNewer = a > b;
			}
			else
			{
				long serverLocal = (serverData != null) ? serverData.lastUpdated : 0;
				long localLocal = (PlayerData != null) ? PlayerData.lastUpdated : 0;
				serverIsNewer = serverLocal > localLocal;
			}

			if (serverIsNewer)
			{
				PlayerData = serverData;
				SaveLocal();
				Debug.Log("[DataManager] 서버 데이터가 최신, 로컬 업데이트 완료.");
			}
			else
			{
				yield return UploadToServer(dbRef);
				Debug.Log("[DataManager] 로컬 데이터가 최신 또는 동일, 서버 업데이트 완료.");
			}
        }
        else
        {
            // 서버에 데이터 없음 → 로컬 업로드
            Debug.Log("[DataManager] 서버에 데이터 없음, 로컬 데이터 업로드 시작");
            yield return UploadToServer(dbRef);
            Debug.Log("[DataManager] 서버 데이터 없음, 로컬 업로드 완료.");
        }
    }

    /// <summary>
    /// 서버에 로컬 세이브파일을 업로드 
    /// 9.9. 이학권 추가
    /// </summary>
    private IEnumerator UploadToServer(DatabaseReference dbRef)
    {
        Debug.Log("[DataManager] UploadToServer 시작");
        
        // 1) 로컬 타임스탬프 갱신 및 전체 JSON 업로드
        SaveLocal();
        string json = JsonUtility.ToJson(PlayerData, true);
        Debug.Log($"[DataManager] 업로드할 JSON 길이: {json?.Length ?? 0}");
        
        var uploadTask = dbRef.SetRawJsonValueAsync(json);
        yield return new WaitUntil(() => uploadTask.IsCompleted);
        
        if (uploadTask.IsFaulted)
        {
            Debug.LogError($"[DataManager] 서버 업로드 실패: {uploadTask.Exception}");
            if (uploadTask.Exception != null)
            {
                Debug.LogError($"[DataManager] 업로드 예외 상세: {uploadTask.Exception.InnerException?.Message}");
            }
            yield break;
        }
        
        Debug.Log("[DataManager] 서버 업로드 성공");

        // 2) 서버 권위 타임스탬프 설정
        var updates = new Dictionary<string, object>
        {
            { "lastUpdatedServer", ServerValue.Timestamp }
        };
        var tsTask = dbRef.UpdateChildrenAsync(updates);
        yield return new WaitUntil(() => tsTask.IsCompleted);
        
        if (tsTask.IsFaulted)
        {
            Debug.LogError($"[DataManager] 서버 타임스탬프 설정 실패: {tsTask.Exception}");
            if (tsTask.Exception != null)
            {
                Debug.LogError($"[DataManager] 타임스탬프 예외 상세: {tsTask.Exception.InnerException?.Message}");
            }
            yield break;
        }
        
        Debug.Log("[DataManager] 서버 타임스탬프 설정 성공");

        // 3) 서버가 기록한 값을 읽어와 로컬 반영
        var readTask = dbRef.Child("lastUpdatedServer").GetValueAsync();
        yield return new WaitUntil(() => readTask.IsCompleted);
        
        if (!readTask.IsFaulted && readTask.Result != null && long.TryParse(readTask.Result.Value?.ToString(), out var serverMillis))
        {
            PlayerData.lastUpdatedServer = serverMillis;
            SaveLocal();
            Debug.Log($"[DataManager] 서버 타임스탬프 로컬 반영 완료: {serverMillis}");
        }
        else
        {
            Debug.LogWarning("[DataManager] 서버 타임스탬프 읽기 실패");
        }
        
        Debug.Log("[DataManager] UploadToServer 완료");
    }

    /// <summary>
    /// 서버에 로컬 데이터를 업로드 (SaveLocal에서 호출)
    /// </summary>
    private IEnumerator UploadToServerAsync(string uid)
    {
        Debug.Log("[DataManager] UploadToServerAsync 시작");
        
        var dbRef = FirebaseDatabase.DefaultInstance
            .GetReference($"users/{uid}/playerData");

        // 로컬 데이터를 JSON으로 변환
        string json = JsonUtility.ToJson(PlayerData, true);
        Debug.Log($"[DataManager] 서버 업로드할 JSON 길이: {json?.Length ?? 0}");
        
        // 서버에 업로드
        var uploadTask = dbRef.SetRawJsonValueAsync(json);
        yield return new WaitUntil(() => uploadTask.IsCompleted);
        
        if (uploadTask.IsFaulted)
        {
            Debug.LogError($"[DataManager] 서버 업로드 실패: {uploadTask.Exception}");
            if (uploadTask.Exception != null)
            {
                Debug.LogError($"[DataManager] 업로드 예외 상세: {uploadTask.Exception.InnerException?.Message}");
            }
            yield break;
        }
        
        Debug.Log("[DataManager] 서버 업로드 성공");

        // 서버 타임스탬프 설정
        var updates = new Dictionary<string, object>
        {
            { "lastUpdatedServer", ServerValue.Timestamp }
        };
        var tsTask = dbRef.UpdateChildrenAsync(updates);
        yield return new WaitUntil(() => tsTask.IsCompleted);
        
        if (tsTask.IsFaulted)
        {
            Debug.LogError($"[DataManager] 서버 타임스탬프 설정 실패: {tsTask.Exception}");
            yield break;
        }
        
        Debug.Log("[DataManager] 서버 타임스탬프 설정 성공");

        // 서버 타임스탬프를 로컬에 반영
        var readTask = dbRef.Child("lastUpdatedServer").GetValueAsync();
        yield return new WaitUntil(() => readTask.IsCompleted);
        
        if (!readTask.IsFaulted && readTask.Result != null && long.TryParse(readTask.Result.Value?.ToString(), out var serverMillis))
        {
            PlayerData.lastUpdatedServer = serverMillis;
            // 로컬 파일에 타임스탬프 반영 (암호화 없이 직접 저장)
            string updatedJson = JsonUtility.ToJson(PlayerData, true);
            EncryptionUtility.SaveEncryptedFile(_playerDataSavePath, updatedJson);
            Debug.Log($"[DataManager] 서버 타임스탬프 로컬 반영 완료: {serverMillis}");
        }
        
        Debug.Log("[DataManager] UploadToServerAsync 완료");
    }

    /// <summary>
    /// 게임이 종료될 때 자동으로 데이터를 저장합니다.
    /// </summary>
    private void OnApplicationQuit()
    {
        // [수정] 게임 종료 시 현재 진행 상황을 저장하도록 OnApplicationQuit 로직을 활성화합니다.
        // PlayerData가 null이 아닐 때만 저장 로직을 실행하여 예외를 방지합니다.
        if (PlayerData != null && !_suppressSavesUntilGameplay)
        {
            SaveLocal();
        }
    }

    /// <summary>
    /// 게임 플레이가 실제로 시작되었음을 알리고 저장 억제를 해제합니다.
    /// 예: 이벤트 사이클 시작 시 호출.
    /// </summary>
    public void AllowSavesFromNow()
    {
        _suppressSavesUntilGameplay = false;
    }


    public async UniTask InitializeDataAsync()
    {
        await AllEventInitializeDataAsync();
        try
        {
            Debug.Log("이벤트 데이터 로딩 시작");
            var eventDataTask = Csvparser.ParseAsync<ParameterEventData>("ParameterEventData");
            Debug.Log("이벤트 스트링 데이터 로딩 시작");
            var eventStringDataTask = Csvparser.ParseAsync<ParameterEventStringData>("ParameterEventStringData");
            Debug.Log("보상 데이터 로딩 시작");
            var rewardDataTask = Csvparser.ParseAsync<ParameterRewardData>("ParameterRewardData");
            Debug.Log("이벤트 스트링 데이터 리스트 데이터 로딩 시작");
            var characterTask = Csvparser.ParseAsync<CharacterData>("ParameterEventStringDataList");
            Debug.Log("보상 데이터 리스트 로딩 시작");
            var rewardTypeTask = Csvparser.ParseAsync<RewardTypeData>("ParameterRewardDataList");
            Debug.Log("이벤트 데이터 리스트 로딩 시작");
            var choTextTask = Csvparser.ParseAsync<EventDataList>("ParameterEventDataList");

            var (eventList, eventStringList, rewardList, characterList, rewardTypeList, choTextList) =
            await UniTask.WhenAll(eventDataTask, eventStringDataTask, rewardDataTask, characterTask, rewardTypeTask, choTextTask);

            Debug.Log("모든 파일 로딩 완료");

            eventDataDict = eventList.ToDictionary(e => e.ID, e => e);
            eventStringDataDict = eventStringList.ToDictionary(e => e.ID, e => e);
            rewardDataDict = rewardList.ToDictionary(r => r.ID, r => r);
            characterNameDict = characterList.ToDictionary(c => c.Chr_index, c => c.Chr_name);
            rewardTypeDict = rewardTypeList.ToDictionary(t => t.RewardType_index, t => t.RewardType);
            choiceTextDict = choTextList.ToDictionary(c => c.Cho_Num, c => c.Cho_txt);
            roundTypeDict = choTextList.GroupBy(a => a.Appearance_Num)
                                   .ToDictionary(g => g.Key, g => g.First().Appearance_Type);

            pageTypeDict = choTextList.GroupBy(p => p.PageType_Num)
                                      .ToDictionary(g => g.Key, g => g.First().PageType);

            eventDict = choTextList.GroupBy(c => c.Parameter_Num)
                                       .ToDictionary(g => g.Key, g => g.First().Parameter_type);

            _isReady.TrySetResult(true);
            Debug.Log("모든 이벤트 데이터가 성공적으로 로드되었습니다.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"데이터 로드 실패: {ex.Message}");
            _isReady.TrySetException(ex);
        }
    }

    public async UniTask AllEventInitializeDataAsync()
    {
        try
        {
            Debug.Log("이벤트 데이터 로딩 시작");

            // MainEventData11.csv와 AnswerID.csv를 비동기로 로드합니다.
            var mainEventTask = Csvparser.ParseAsync<MainEventData>("MainEventData");
            var answerTask = Csvparser.ParseAsync<AnswerData>("MainAnswerID");
            //서브 이벤트 데이터 로딩
            var subEventTask = Csvparser.ParseAsync<AllSubEventData>("SubEventData1");
            var subAnswerTask = Csvparser.ParseAsync<SubEventAnswerData>("SubEventAnswerID");
            //룩업 테이블 로딩
            var bgDataTask = Csvparser.ParseAsync<BGData>("MainBGData");
            var sfxDataTask = Csvparser.ParseAsync<SFXData>("MainSFXData");
            var characterDataTask = Csvparser.ParseAsync<MainCharacterData>("MainCharacterData");
            var characterImgDataTask = Csvparser.ParseAsync<MainCharacterImgData>("MainCharacterImgData");
            var endingEventDataTask = Csvparser.ParseAsync<EndingEventData>("EndingEventData");
            var endingCutSceneTask = Csvparser.ParseAsync<EndingCutScene>("EndingEventCutScene");
            var battleResultTask = Csvparser.ParseAsync<BattleResultData>("BattleResultTextData");
            var backDataTask = Csvparser.ParseAsync<BackData>("BackData");

            var (mainEventList, answerList, characterList, bgList, sfxList, characterImgList, endingEventList, endingCutSceneList, battleResultList, backDataList, subEventList, subAnswerList) =
                await UniTask.WhenAll(mainEventTask, answerTask, characterDataTask, bgDataTask, sfxDataTask, characterImgDataTask, endingEventDataTask, endingCutSceneTask, battleResultTask, backDataTask, subEventTask, subAnswerTask);

            Debug.Log("모든 파일 로딩 완료");

            // 새로운 딕셔너리로 데이터를 구성합니다.
            mainEventDataDict = mainEventList.ToDictionary(e => e.ID, e => e);
            answerDataDict = answerList.ToDictionary(a => a.AnswerID, a => a);
            //배틀 이벤트 데이터
            battleResultDataDict = battleResultList.ToDictionary(r => r.ResultID, r => r);
            //룩업 데이터 구성
            CharacterDataDict = characterList.ToDictionary(e => e.Chr_ID, e => e);
            bgDataDict = bgList.ToDictionary(bg => bg.BG_ID, bg => bg);
            sfxDataDict = sfxList.ToDictionary(sfx => sfx.SFX_ID, sfx => sfx);
            characterImgDataDict = characterImgList.ToDictionary(c => c.CharacterImg_ID, c => c);
            endingEventDataDict = endingEventList.ToDictionary(e => e.ID, e => e);
            endingCutSceneDict = endingCutSceneList.ToDictionary(c => c.EndingCutScene_ID, c => c);
            backDataDict = backDataList.ToDictionary(d => d.Back_ID, d => d);
            subEventDataDict = subEventList.ToDictionary(e => e.ID, e => e);
            subEventAnswerDataDict = subAnswerList.ToDictionary(a => a.AnswerID, a => a);

            // 모든 원본 데이터 로딩이 끝난 후, NewMainEventData 딕셔너리를 채웁니다.
            mainEventData.Clear(); // 혹시 모를 이전 데이터 삭제

            Debug.Log("---------- [DataManager] 데이터 가공 시작 ----------");
            // mainEventDataDict에 로드된 모든 원본 데이터를 순회합니다.
            foreach (var rawData in mainEventDataDict.Values.OrderBy(d => d.ID)) // ID 순서대로 확인
            {
                NewMainEventData processedData = GetMainEventDataById(rawData.ID);

                // rawData.ID가 10140 근처일 때 특히 주의 깊게 보세요.
                if (processedData != null)
                {
                    if (!mainEventData.ContainsKey(processedData.id))
                    {
                        mainEventData.Add(processedData.id, processedData);
                        // 어떤 데이터가 성공적으로 추가되었는지 로그로 확인
                        if (rawData.ID > 10010 && rawData.ID < 10020) // Chapter 2 시작 부근 로그 확인
                        {
                            Debug.Log($"[성공] ID: {rawData.ID}, StoryNum: {rawData.StoryNum} -> 가공 완료 및 추가 성공.");
                        }
                    }
                }
                else
                {
                    // 어떤 데이터가 가공에 실패했는지 로그로 확인
                    Debug.LogError($"[실패] ID: {rawData.ID}, StoryNum: {rawData.StoryNum} -> 가공 중 Null 반환됨. 이 데이터나 관련 데이터(BG, 캐릭터 등)에 문제가 있을 수 있습니다.");
                }
            }
                Debug.Log($"[DataManager] {mainEventData.Count}개의 메인 스토리 데이터를 가공하여 최종 준비했습니다.");


            _isReady.TrySetResult(true);
            Debug.Log("모든 이벤트 데이터가 성공적으로 로드되었습니다.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"데이터 로드 실패: {ex.Message}");
            _isReady.TrySetException(ex);
        }
    }

    public async UniTask SubIntializeDataAsync()
    {
        await LoadSubAllDataAsync();
    }

    private async UniTask LoadSubAllDataAsync()
    {
        var ct = this.GetCancellationTokenOnDestroy();

        try
        {
            // 두 개의 원본 CSV 파일을 비동기로 로드
            var subEventDataListTask = Csvparser.ParseAsync<SubEventDataList>("SubEventDataList", ct);
            var subEventActionDataTask = Csvparser.ParseAsync<SubEventActionData>("SubEventData", ct);

            var (loadedSubEventList, loadedSubEventActionList) = await UniTask.WhenAll(subEventDataListTask, subEventActionDataTask);

            // StringNum을 키로 하는 이벤트 정보 룩업 테이블 생성
            var subEventListLookup = loadedSubEventList
                .Where(s => s.StringNum != 0)
                .ToDictionary(s => s.StringNum, s => s);
            // Cho_Num을 키로 하는 선택지 텍스트 룩업 테이블 생성
            var choiceTextLookup = loadedSubEventList
                .Where(s => s.Cho_Num != 0 && !string.IsNullOrEmpty(s.Cho_Text))
                .ToDictionary(s => s.Cho_Num, s => s.Cho_Text);
            // End_Num을 키로 하는 엔딩 텍스트 룩업 테이블 추가
            var endTextLookup = loadedSubEventList
                .Where(s => s.End_Num != 0 && !string.IsNullOrEmpty(s.End_Text))
                .ToDictionary(s => s.End_Num, s => s.End_Text);
            //Chr_index를 키로 하는 캐릭터 이름 룩업 테이블 추가
            var characterNameLookup = loadedSubEventList
                .Where(s => s.Chr_index != 0 && !string.IsNullOrEmpty(s.Chr_Name))
                .ToDictionary(s => s.Chr_index, s => s.Chr_Name);

            // SubEventActionData를 순회하며 데이터를 통합합니다.
            SubEvents = loadedSubEventActionList.Select(actionData =>
            {

                // EndingString으로 SubEventDataList 정보 조회
                if (!subEventListLookup.TryGetValue(actionData.EndingString, out var listData))
                {
                    return null; // 매칭되는 데이터가 없으면 통합하지 않음
                }

                // LeftSelectString과 RightSelectString으로 선택지 텍스트 조회
                string leftText = string.Empty;
                choiceTextLookup.TryGetValue(actionData.LeftSelectString, out leftText);

                string rightText = string.Empty;
                choiceTextLookup.TryGetValue(actionData.RightSelectString, out rightText);

                // SubEventActionData의 CharacterName(정수)을 사용해 캐릭터 이름 조회
                string characterName = string.Empty;
                characterNameLookup.TryGetValue(actionData.CharacterName, out characterName);

                string endText = string.Empty;
                endTextLookup.TryGetValue(listData.End_Num, out endText);

                // 새로운 SubEventData 객체 생성 및 값 채우기
                return new SubEventData
                {
                    // SubEventActionData에서 가져올 필드
                    Index = actionData.Index,
                    PackNumber = actionData.PackNumber,
                    GroupNumber = actionData.GroupNumber,
                    NextLeftSelectString = actionData.NextLeftSelectString.ToString(),
                    NextRightSelectString = actionData.NextRightSelectString.ToString(),
                    IsFinish = actionData.IsFinishString == 1,
                    CharacterImg = actionData.CharacterImg,
                    BG = actionData.BG,
                    SE = actionData.SE,
                    QuestionString_kr = actionData.QuestionString_kr,

                    // SubEventDataList에서 가져올 필드
                    LeftSelectString = leftText,
                    RightSelectString = rightText,
                    CharacterName = characterName,
                    End_Text = endText
                };
            }).Where(e => e != null).ToList();

            Debug.Log($"데이터 로드 및 통합 완료! 총 {SubEvents.Count}개의 이벤트가 준비되었습니다.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"데이터 초기화 실패: {ex.Message}");
            SubEvents = new List<SubEventData>();
        }
    }

    // ID를 통해 메인 이벤트 데이터 구성 및 반환
    public NewMainEventData GetMainEventDataById(int eventID)
    {
        if (!mainEventDataDict.TryGetValue(eventID, out var rawData))
        {
            Debug.LogError($"[DataManager] ID {eventID}에 해당하는 이벤트 데이터를 찾을 수 없습니다.");
            return null;
        }

        var fullEventData = new NewMainEventData
        {
            id = rawData.ID,
            MainStoryPac = rawData.MainStoryPac,
            StoryNum = rawData.StoryNum,
            LoopNum = rawData.LoopNum,
            dialogue = rawData.Text_kr, 

        };

        //Chr_ID를 사용하여 캐릭터 이름 할당
        if (CharacterDataDict.TryGetValue(rawData.CharacterName, out var characterData))
        {
            fullEventData.characterData = characterData;
        }

        // BG_ID를 사용하여 BG 데이터를 직접 찾아서 할당합니다.
        if (bgDataDict.TryGetValue(rawData.BG_ID, out var bgData))
        {
            fullEventData.bgData = bgData;
        }

        // SFX_ID를 사용하여 SFX 데이터를 직접 찾아서 할당합니다.
        if (sfxDataDict.TryGetValue(rawData.SFX_ID, out var sfxData))
        {
            fullEventData.sfxData = sfxData;
        }

        // CharacterImg_ID를 사용하여 캐릭터 이미지 데이터를 직접 찾아서 할당합니다.
        if (characterImgDataDict.TryGetValue(rawData.CharacterImg_ID, out var characterImgData))
        {
            fullEventData.characterImgData = characterImgData;
        }
        if(backDataDict.TryGetValue(rawData.Back_ID, out var backData))
        {
            fullEventData.backData = backData;
        } 

        // 왼쪽 및 오른쪽 선택지 구성
        fullEventData.leftChoice = CreateMainChoice(rawData.AnswerLeftID);
        fullEventData.rightChoice = CreateMainChoice(rawData.AnswerRightID);

        return fullEventData;
    }
    public NewMainEventData GetMainEventDataByStoryNum(int storyNum)
    {
        // mainEventData 딕셔너리의 모든 값들 중에서
        // StoryNum이 일치하는 데이터를 찾되, ID가 가장 낮은(가장 먼저 나오는) 데이터를 반환합니다.
        var foundData = mainEventData.Values
                                     .Where(data => data.StoryNum == storyNum)
                                     .OrderBy(data => data.id) // ID 순으로 정렬
                                     .FirstOrDefault(); // 그 중 첫 번째 것을 선택

        if (foundData == null)
        {
            Debug.LogError($"[DataManager] StoryNum {storyNum}에 해당하는 이벤트 데이터를 찾을 수 없습니다.");
        }

        return foundData;
    }
    private MainEventChoice CreateMainChoice(int answerID)
    {
        var choice = new MainEventChoice();

        choice.outcome = new ChoiceOutcome
        {
            parameterChanges = new List<ParameterChange>()
        };
        Debug.Log($"[DataManager] 선택지를 생성합니다. AnswerID: {answerID}를 찾습니다...");

        if (answerDataDict.TryGetValue(answerID, out var answerData))
        {
            Debug.Log($"[DataManager] -> <color=green>성공!</color> AnswerID: {answerID}를 찾았습니다. 텍스트: '{answerData.Text_KR}', 다음 이벤트 ID: {answerData.NextTextID}");
            choice.choiceText = answerData.Text_KR;
            choice.nextEventID = answerData.NextTextID;
            //선택지 보상치 적용 단
        }
        else
        {
            Debug.LogError($"[DataManager] -> <color=red>실패!</color> AnswerID: {answerID}에 해당하는 데이터를 'answerDataDict'에서 찾을 수 없습니다. MainAnswerID.csv 파일을 확인해주세요.");

            choice.choiceText = "선택지 데이터를 찾을 수 없습니다.";
        }

        return choice;
    }

    public BattleResultData GetBattleResultDataById(int resultID)
    {
        if (battleResultDataDict.TryGetValue(resultID, out var data))
        {
            return data;
        }

        Debug.LogError($"[DataManager] ID {resultID}에 해당하는 전투 결과 데이터를 찾을 수 없습니다.");
        return null;
    }

    public FullEndingData GetEndingData(long endingID)
    {
        if (!endingEventDataDict.TryGetValue(endingID, out var rawData))
        {
            Debug.LogError($"[DataManager] ID {endingID}에 해당하는 엔딩 데이터를 찾을 수 없습니다.");
            return null;
        }

        var fullEndingData = new FullEndingData
        {
            ID = rawData.ID,
            EndingString = rawData.EndingString,
            Karma_Rate = rawData.Karma_Rate,
            Text_Kr = rawData.Text_Kr,
            Text_En = rawData.Text_En,
            Direction = rawData.Direction,
            Fade_Out_Color = rawData.Fade_Out_Color
        };
        if (bgDataDict.TryGetValue(rawData.BG_ID, out var bgData))
        {
            fullEndingData.bgData = bgData;
        }

        if (sfxDataDict.TryGetValue(rawData.SFX_ID, out var sfxData))
        {
            fullEndingData.sfxData = sfxData;
        }

        if (endingCutSceneDict.TryGetValue(rawData.EndingCutScene_ID, out var cutSceneData))
        {
            fullEndingData.cutSceneData = cutSceneData;
        }

        return fullEndingData;
    }



    // ID를 통해 파라미터 이벤트 데이터를 구성하고 반환하는 함수
    public EventData GetEventDataById(int eventID)
    {
        if (!eventDataDict.TryGetValue(eventID, out var rawData))
        {
            Debug.LogError($"[DataManager] ID {eventID}에 해당하는 이벤트 데이터를 찾을 수 없습니다.");
            return null;
        }


        // 분기 조건 확인: ChangeCondition 이벤트가 과거에 성공적으로 완료되었는지 여부
        bool isBranchTriggered = rawData.ConditionType == 1 && PlaythroughHistory.Instance.HasCompletedEvent(rawData.ChangeCondition);

        var fullEventData = new EventData
        {
            id = eventID,
            //eventName = $"Event_{eventID}" // 임시 이름
        };


        // 분기 여부에 따라 적절한 질문 ID 선택
        int questionId = isBranchTriggered ? rawData.AnotherEventQuestion : rawData.EventQuestion;
        if (eventStringDataDict.TryGetValue(questionId, out var questionString))
        {
            fullEventData.dialogue = questionString.String_kr;
            fullEventData.BG = questionString.BG;
            fullEventData.SE = questionString.SoundEffect;
            fullEventData.CharacterImage = questionString.CharacterImage;
            if (characterNameDict.TryGetValue(questionString.CharacterName, out var characterName))
            {
                fullEventData.CharacterName = characterName;
            }
        }
        else
        {
            Debug.LogError($"[DataManager] 질문 ID {questionId}에 해당하는 스트링 데이터를 찾을 수 없습니다.");
        }

        // 왼쪽 및 오른쪽 선택지 구성
        fullEventData.leftChoice = CreateChoice(rawData, isBranchTriggered, true);
        fullEventData.rightChoice = CreateChoice(rawData, isBranchTriggered, false);

        // 기타 데이터 설정
        if (roundTypeDict.TryGetValue(rawData.RoundType, out var roundTypeStr))
        {
            fullEventData.RoundType = roundTypeStr;
        }
        if (eventDict.TryGetValue(rawData.ConditionType, out var conditionTypeStr))
        {
            fullEventData.ConditionType = conditionTypeStr;
        }

        return fullEventData;
    }

    private SubChoice CreateSubChoice(int answerID)
    {
        if (subEventAnswerDataDict.TryGetValue(answerID, out var rawData))
        {
            return new SubChoice
            {
                answerID = rawData.AnswerID,
                choiceText = rawData.Text_KR,
                nextEventID = rawData.NextTextID,
                outcomeReward = rawData.AnswerReward
            };
        }
        return null;
    }

    public FullSubEventData GetSubEventDataById(int eventID)
    {
        if (!subEventDataDict.TryGetValue(eventID, out var rawData))
        {
            Debug.LogError($"[DataManager] ID {eventID}에 해당하는 서브 이벤트 데이터를 찾을 수 없습니다.");
            return null;
        }

        var fullEventData = new FullSubEventData
        {
            ID = rawData.ID,
            SubStoryPac = rawData.SubStoryPac,
            StoryNum = rawData.StoryNum,
            Text_kr = rawData.Text_kr,

            bgData = bgDataDict.TryGetValue(rawData.BG_ID, out var bg) ? bg : null,
            sfxData = sfxDataDict.TryGetValue(rawData.SFX_ID, out var sfx) ? sfx : null,
            backData = backDataDict.TryGetValue(rawData.Back_ID, out var back) ? back : null,
            characterData = CharacterDataDict.TryGetValue(rawData.CharacterName, out var character) ? character : null,
            characterImgData = characterImgDataDict.TryGetValue(rawData.CharacterImg_ID, out var img) ? img : null,

            leftChoice = CreateSubChoice(rawData.AnswerLeftID),
            rightChoice = CreateSubChoice(rawData.AnswerRightID)
        };

        return fullEventData;
    }

    /// <summary>
    /// 단일 선택지(EventChoice) 객체를 생성합니다.
    /// </summary>
    /// <param name="rawData">CSV에서 읽어온 원본 이벤트 데이터</param>
    /// <param name="isBranch">분기된 경로의 선택지를 생성할지 여부</param>
    /// <param name="isLeft">왼쪽 선택지인지 여부</param>
    /// <returns>생성된 EventChoice 객체</returns>
    private EventChoice CreateChoice(ParameterEventData rawData, bool isBranch, bool isLeft)
    {
        var choice = new EventChoice();

        // 선택지 텍스트, 성공/실패 결과 ID, 보상 ID 등을 분기 및 좌/우에 따라 결정
        int choiceTextId = isLeft ? (isBranch ? rawData.AnotherLeftString : rawData.LeftString)
                                       : (isBranch ? rawData.AnotherRightString : rawData.RightString);
        int successStringId = isLeft ? (isBranch ? rawData.AnotherAcceptString1 : rawData.AcceptString1)
                                       : (isBranch ? rawData.AnotherAcceptString2 : rawData.AcceptString2);
        int failStringId = isLeft ? (isBranch ? rawData.AnotherDenyString1 : rawData.DenyString1)
                                       : (isBranch ? rawData.AnotherDenyString2 : rawData.DenyString2);
        int successRewardId = isLeft ? (isBranch ? rawData.AnotherAcceptReward1 : rawData.AcceptReward1)
                                       : (isBranch ? rawData.AnotherAcceptReward2 : rawData.AcceptReward2);
        int failRewardId = isLeft ? (isBranch ? rawData.AnotherDenyReward1 : rawData.DenyReward1)
                                       : (isBranch ? rawData.AnotherDenyReward2 : rawData.DenyReward2);
        int needType = isLeft ? (isBranch ? rawData.AnotherNeedType1 : rawData.NeedType1)
                                       : (isBranch ? rawData.AnotherNeedType2 : rawData.NeedType2);
        int needValue = isLeft ? (isBranch ? rawData.AnotehrNeedValue1 : rawData.NeedValue1) // 'Anotehr' 오타 대응
                                       : (isBranch ? rawData.AnotherNeedValue2 : rawData.NeedValue2);

        // 선택지 텍스트 설정
        if (choiceTextDict.TryGetValue(choiceTextId, out var text))
        {
            choice.choiceText = text;
        }

        // 성공/실패 결과 Outcome 객체 생성
        choice.successOutcome = CreateOutcome(successStringId, successRewardId);
        choice.failOutcome = CreateOutcome(failStringId, failRewardId);

        // 성공 조건 객체 생성
        choice.condition = CreateSuccessCondition(needType, needValue);

        return choice;
    }

    /// <summary>
    /// 결과(Outcome) 객체를 생성합니다.
    /// </summary>
    private ChoiceOutcome CreateOutcome(int stringId, int rewardId)
    {
        var outcome = new ChoiceOutcome();
        if (eventStringDataDict.TryGetValue(stringId, out var outcomeString))
        {
            outcome.outcomeText = outcomeString.String_kr;
        }

        outcome.parameterChanges.AddRange(ConvertRewardsToParameterChanges(GetRewards(rewardId)));
        return outcome;

    }

    /// <summary>
    /// CSV 데이터 기반으로 다양한 성공 조건 객체를 생성합니다.
    /// </summary>
    private SuccessCondition CreateSuccessCondition(int needType, int needValue)
    {
        switch (needType)
        {
            case 0: // 없음
                return new GuaranteedSuccessCondition();
            case 1: // 정치력
                return new ParameterSuccessCondition { targetParameter = ParameterType.정치력, requiredValue = needValue };
            case 2: // 병력
                return new ParameterSuccessCondition { targetParameter = ParameterType.병력, requiredValue = needValue };
            case 3: // 물자
                return new ParameterSuccessCondition { targetParameter = ParameterType.물자, requiredValue = needValue };
            case 4: // 리더십
                return new ParameterSuccessCondition { targetParameter = ParameterType.리더십, requiredValue = needValue };
            default:
                Debug.LogWarning($"[DataManager] 알 수 없는 NeedType: {needType}. GuaranteedSuccessCondition으로 처리합니다.");
                return new GuaranteedSuccessCondition();
        }
    }
    private List<RewardInfo> GetRewards(int rewardID)
    {
        var rewards = new List<RewardInfo>();
        if (rewardID == 0) return rewards; // 보상 ID가 0이면 빈 리스트 반환

        if (rewardDataDict.TryGetValue(rewardID, out var rewardData))
        {
            // 여러 개의 보상을 리스트에 추가
            if (rewardData.RewardType1 != 0 && rewardTypeDict.TryGetValue(rewardData.RewardType1, out var type1))
                rewards.Add(new RewardInfo { RewardType = type1, RewardValue = rewardData.RewardValue1 });
            if (rewardData.RewardType2 != 0 && rewardTypeDict.TryGetValue(rewardData.RewardType2, out var type2))
                rewards.Add(new RewardInfo { RewardType = type2, RewardValue = rewardData.RewardValue2 });
            if (rewardData.RewardType3 != 0 && rewardTypeDict.TryGetValue(rewardData.RewardType3, out var type3))
                rewards.Add(new RewardInfo { RewardType = type3, RewardValue = rewardData.RewardValue3 });
            if (rewardData.RewardType4 != 0 && rewardTypeDict.TryGetValue(rewardData.RewardType4, out var type4))
                rewards.Add(new RewardInfo { RewardType = type4, RewardValue = rewardData.RewardValue4 });
            if (rewardData.RewardType5 != 0 && rewardTypeDict.TryGetValue(rewardData.RewardType5, out var type5))
                rewards.Add(new RewardInfo { RewardType = type5, RewardValue = rewardData.RewardValue5 });
        }
        return rewards;
    }

    // RewardInfo 리스트를 ParameterChange 리스트로 변환하는 헬퍼 함수
    private List<ParameterChange> ConvertRewardsToParameterChanges(List<RewardInfo> rewards)
    {
        List<ParameterChange> changes = new List<ParameterChange>();
        if (rewards == null)
        {
            return changes;
        }

        foreach (var reward in rewards)
        {
            ParameterType type = GetParameterType(reward.RewardType);

            changes.Add(new ParameterChange
            {
                parameterType = type,
                valueChange = reward.RewardValue
            });
        }
        return changes;
    }

    private ParameterType GetParameterType(string rewardType)
    {
        return rewardType switch
        {
            "정치력" => ParameterType.정치력,
            "병력" => ParameterType.병력,
            "물자" => ParameterType.물자,
            "리더십" => ParameterType.리더십,
            "전세" => ParameterType.전황,
            "카르마" => ParameterType.카르마,
        };
    }

    [System.Serializable]
    public class SubEventDataList
    {
        public int StringNum { get; set; }
        public int PackNum { get; set; }
        public string PackList { get; set; }
        public int IsFinish { get; set; }
        public bool SubStory_End { get; set; }
        public int Chr_index { get; set; }
        public string Chr_Name { get; set; }
        public int End_Num { get; set; }
        public string End_Text { get; set; }
        public int Cho_Num { get; set; }
        public string Cho_Text { get; set; }
    }

    [System.Serializable]
    public class SubEventActionData
    {
        public int Index { get; set; }
        public int PackNumber { get; set; }
        public int GroupNumber { get; set; }
        public int EndingString { get; set; }
        public int LeftSelectString { get; set; }
        public int NextLeftSelectString { get; set; }
        public int LeftSelectReward { get; set; }
        public int RightSelectString { get; set; }
        public int NextRightSelectString { get; set; }
        public int RightSelectReward { get; set; }
        public int IsFinishString { get; set; }
        public int CharacterName { get; set; }
        public string CharacterImg { get; set; }
        public string BG { get; set; }
        public string SE { get; set; }
        public string QuestionString_kr { get; set; }
    }

    [System.Serializable]
    public class SubEventData
    {
        // SubEventActionData에서 가져올 필드
        public int Index { get; set; }
        public int PackNumber { get; set; }
        public int GroupNumber { get; set; }
        public string NextLeftSelectString { get; set; } //SubEventData 에서 인덱스값에 맞춤
        public string NextRightSelectString { get; set; } //SubEventData 에서 인덱스값에 맞춤
        public bool IsFinish { get; set; } //IsFinishString (int)를 bool값으로 변환
        public string CharacterImg { get; set; }
        public string BG { get; set; }
        public string SE { get; set; }
        public string QuestionString_kr { get; set; }

        //public string LeftSelectReward { get; set; } 미정
        //public string RightSelectReward { get; set; } 미정

        //SubEventDataList에서 가져올 필드
        public string LeftSelectString { get; set; }
        public string RightSelectString { get; set; }
        public string CharacterName { get; set; }
        public string End_Text { get; set; }
    }

    // PlayerPrefs를 사용하여 회차 기록을 관리하는 클래스
    public class PlaythroughHistory
    {
        public static PlaythroughHistory Instance { get; private set; } = new PlaythroughHistory();

        private const string CompletedEventsKey = "CompletedEvents";
        private HashSet<int> completedEvents;

        // 생성자에서 데이터 로드
        private PlaythroughHistory()
        {
            Load();
        }

        private void Load()
        {
            completedEvents = new HashSet<int>();
            string savedEvents = PlayerPrefs.GetString(CompletedEventsKey, "");
            if (!string.IsNullOrEmpty(savedEvents))
            {
                foreach (var idStr in savedEvents.Split(','))
                {
                    if (int.TryParse(idStr, out int id))
                    {
                        completedEvents.Add(id);
                    }
                }
            }
            Debug.Log($"[PlaythroughHistory] 로드 완료. 완료된 이벤트 {completedEvents.Count}개");
        }

        private void Save()
        {
            string eventIds = string.Join(",", completedEvents);
            PlayerPrefs.SetString(CompletedEventsKey, eventIds);
            PlayerPrefs.Save(); // 확실한 저장을 위해 호출
            Debug.Log($"[PlaythroughHistory] 저장 완료. 현재 완료된 이벤트: {eventIds}");
        }

        public bool HasCompletedEvent(int eventId) => completedEvents.Contains(eventId);

        public void AddCompletedEvent(int eventId)
        {
            if (completedEvents.Add(eventId)) // 새로운 이벤트일 경우에만 저장
            {
                Save();
            }
        }

        public void ClearHistory()
        {
            completedEvents.Clear();
            PlayerPrefs.DeleteKey(CompletedEventsKey);
            PlayerPrefs.Save();
            Debug.Log("[PlaythroughHistory] 모든 기록이 삭제되었습니다.");
        }
    }
}