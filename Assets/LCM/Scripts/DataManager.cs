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
    public SettingsData PlayerSettings { get; set; }
    private string _playerDataSavePath;
    private string _settingsSavePath;
    private bool _hasSyncedWithServer; // 9.9. 이학권 추가

    private UniTaskCompletionSource<bool> _isReady = new UniTaskCompletionSource<bool>();
    public UniTask IsReady => _isReady.Task;
    public bool IsDataReady => _isReady.Task.Status == UniTaskStatus.Succeeded;

    //서브이벤트 전체 목록
    public List<FullSubEventData> FullSubEvents { get; private set; } = new List<FullSubEventData>();
    //특수 이벤트 전체 목록 (서브와 분리)
    public List<FullSubEventData> FullSpecialEvents { get; private set; } = new List<FullSubEventData>();
    //엔딩 텍스트 누적 리스트
    private List<string> acquiredEndingMemoriars = new List<string>();

    //메인 이벤트 
    public Dictionary<int, MainEventData> mainEventDataDict;
    public Dictionary<int, AnswerData> answerDataDict;
    //서브 이벤트
    public Dictionary<int, SubEventData> subEventDataDict;
    public Dictionary<int, SubEventAnswerData> subEventAnswerDataDict;
    //특수 이벤트 답변 데이터
    public Dictionary<int, SubEventAnswerData> specialEventAnswerDataDict;
    //메인 룩업 테이블
    private Dictionary<int, MainCharacterData> CharacterDataDict;
    private Dictionary<int, BGData> bgDataDict;
    private Dictionary<int, SFXData> sfxDataDict;
    private Dictionary<int, MainCharacterImgData> characterImgDataDict;
    private Dictionary<int, SpecialEventCharacterData> specialEventCharacterDict;
    private Dictionary<int, BackData> backDataDict;
    //전투 결과 이벤트
    public Dictionary<int, BattleResultData> battleResultDataDict;
    //엔딩 이벤트
    public Dictionary<int, EndingEventData> endingEventDataDict;
    public Dictionary<int, EndingEventData> TrueEndingeventDataDict;
    public Dictionary<int, EndingCutScene> endingCutSceneDict;
    public Dictionary<int, EndingNameList> endingNameDict;
    public Dictionary<int, FullEndingData> FullendingDataDict;
    public Dictionary<int, FullEndingData> TrueEndingDataDict;
    //회상 이벤트
    public Dictionary<int, EndingMemoriarData> endingMemoriarDataDict;
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
        
        // FirebaseManager의 로그인 타입 복원을 기다린 후 동기화 시도
        StartCoroutine(WaitForFirebaseManagerAndTrySync());
    }
    
    private System.Collections.IEnumerator WaitForFirebaseManagerAndTrySync()
    {
        // FirebaseManager가 초기화될 때까지 대기
        yield return new WaitUntil(() => FirebaseManager.Instance != null);
        
        // FirebaseManager 초기화 완료 이벤트 구독
        bool initialized = false;
        System.Action onInitialized = () => initialized = true;
        FirebaseManager.OnFirebaseManagerInitialized += onInitialized;
        
        // 초기화 완료까지 대기
        yield return new WaitUntil(() => initialized);
        
        // 이벤트 구독 해제
        FirebaseManager.OnFirebaseManagerInitialized -= onInitialized;
        
        Debug.Log("[DataManager] FirebaseManager 초기화 완료, 동기화 시도");
        TrySyncIfLoggedIn();
    }

    private void OnAuthStateChanged(object sender, System.EventArgs e)
    {
        TrySyncIfLoggedIn();
    }

    private void TrySyncIfLoggedIn() /// 9.9. 이학권 추가
    {
        var user = FirebaseAuth.DefaultInstance.CurrentUser;
        Debug.Log($"[DataManager] TrySyncIfLoggedIn 호출 - User: {user?.UserId}, HasSynced: {_hasSyncedWithServer}, IsGuest: {FirebaseManager.IsGuestAccount}, LoginType: {FirebaseManager.CurrentLoginType}");
        
        if (user != null && !_hasSyncedWithServer && !FirebaseManager.IsGuestAccount)
        {
            Debug.Log("[DataManager] 서버 동기화 시작");
            _hasSyncedWithServer = true;
            StartCoroutine(SyncWithServer(user.UserId));
        }
        else
        {
            if (user == null) Debug.Log("[DataManager] Firebase User가 null입니다");
            if (_hasSyncedWithServer) Debug.Log("[DataManager] 이미 서버 동기화를 완료했습니다");
            if (FirebaseManager.IsGuestAccount) Debug.Log("[DataManager] 게스트 계정이므로 서버 동기화를 건너뜁니다");
        }
    }

    /// <summary>
    /// Firebase 로그인 완료 시 호출되는 메서드
    /// </summary>
    public void OnFirebaseLoginCompleted()
    {
        Debug.Log("[DataManager] Firebase 로그인 완료 알림 수신");
        TrySyncIfLoggedIn();
    }

    /// <summary>
    /// 새 게임을 시작할 때 호출됩니다.
    /// </summary>
    public void StartNewGame()
    {
        PlayerData = new GameData();
        // 새 게임에서는 장착 스킬을 초기화합니다.
        if (PlayerPrefs.HasKey("EquippedSkillID"))
        {
            PlayerPrefs.DeleteKey("EquippedSkillID");
            PlayerPrefs.Save();
            Debug.Log("[스킬 디버그] 새 게임 - PlayerPrefs 'EquippedSkillID' 초기화 완료");
        }
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
                    Debug.Log($"[DataManager] 기본 설정 생성 후 서브이벤트팩: {string.Join(", ", PlayerSettings.selectedSubEventPackIDs)}");
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
                    Debug.Log($"[DataManager] 손상된 파일로 인한 기본 설정 생성 후 서브이벤트팩: {string.Join(", ", PlayerSettings.selectedSubEventPackIDs)}");
                }
                
                // 서브이벤트팩이 선택되지 않았으면 기본 팩(1000001) 자동 선택
                if (PlayerSettings.selectedSubEventPackIDs == null || PlayerSettings.selectedSubEventPackIDs.Count == 0)
                {
                    const int defaultPackId = 1000001;
                    Debug.Log($"[DataManager] 서브이벤트팩이 선택되지 않아 기본 팩 {defaultPackId}을(를) 자동 선택합니다.");
                    PlayerSettings.selectedSubEventPackIDs = new List<int> { defaultPackId };
                    SaveSettings();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"설정 로드 실패: {ex.Message}. 기본 설정으로 재설정합니다.");
                PlayerSettings = new SettingsData();
                Debug.Log($"[DataManager] 예외로 인한 기본 설정 생성 후 서브이벤트팩: {string.Join(", ", PlayerSettings.selectedSubEventPackIDs)}");
                
                // 서브이벤트팩이 선택되지 않았으면 기본 팩(1000001) 자동 선택
                if (PlayerSettings.selectedSubEventPackIDs == null || PlayerSettings.selectedSubEventPackIDs.Count == 0)
                {
                    const int defaultPackId = 1000001;
                    Debug.Log($"[DataManager] 서브이벤트팩이 선택되지 않아 기본 팩 {defaultPackId}을(를) 자동 선택합니다.");
                    PlayerSettings.selectedSubEventPackIDs = new List<int> { defaultPackId };
                    SaveSettings();
                }
            }
        }
        else
        {
            Debug.Log("설정 파일 없음, 기본 설정 생성.");
            PlayerSettings = new SettingsData();
            Debug.Log($"[DataManager] 파일 없음으로 인한 기본 설정 생성 후 서브이벤트팩: {string.Join(", ", PlayerSettings.selectedSubEventPackIDs)}");
            
            // 서브이벤트팩이 선택되지 않았으면 기본 팩(1000001) 자동 선택
            if (PlayerSettings.selectedSubEventPackIDs == null || PlayerSettings.selectedSubEventPackIDs.Count == 0)
            {
                const int defaultPackId = 1000001;
                Debug.Log($"[DataManager] 서브이벤트팩이 선택되지 않아 기본 팩 {defaultPackId}을(를) 자동 선택합니다.");
                PlayerSettings.selectedSubEventPackIDs = new List<int> { defaultPackId };
                SaveSettings();
            }
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
    /// 9.9. 이학권 변경 - 암호화 기능 추가
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
        if (global::PlaythroughHistory.Instance != null)
        {
            // HCW의 PlaythroughHistory에는 ClearHistory 메서드가 없으므로 직접 PlayerPrefs 삭제
            PlayerPrefs.DeleteKey("PlaythroughHistory_Events");
            PlayerPrefs.DeleteKey("PlaythroughHistory_Endings");
            PlayerPrefs.DeleteKey("PlaythroughHistory_BattleResult");
            PlayerPrefs.Save();
        }

        // 초기화 직후에는 저장 생성/덮어쓰기 방지
        _suppressSavesUntilGameplay = true;

        Debug.LogWarning("[DataManager] 모든 로컬 데이터가 초기화되었습니다.");
    }

    /// <summary>
    /// [신규] 진행도와 회차 기록을 초기화합니다.
    /// - clearPlaythroughHistory: PlaythroughHistory PlayerPrefs 및 메모리 초기화
    /// - resetPlaythroughCount: 회차(playthroughCount)를 1로 재설정
    /// - resetStats: 파라미터 수치 및 특성 초기화
    /// - resetChapter: 챕터 및 현재 플레이리스트/인덱스 초기화
    /// </summary>
    public void ResetProgressAndHistory(bool clearPlaythroughHistory = true, bool resetPlaythroughCount = true, bool resetStats = true, bool resetChapter = true)
    {
        ProgressResetService.ResetProgressAndHistory(clearPlaythroughHistory, resetPlaythroughCount, resetStats, resetChapter);
    }

    /// <summary>
    /// [신규] 특정 엔딩을 플레이어 데이터에 기록합니다.
    /// </summary>
    public void RecordEnding(int endingId)
    {
        if (PlayerData == null) return;

        // 직전 엔딩 기록
        PlayerData.lastEndingId = endingId;

        // 이미 본 엔딩 목록에 없으면 추가
        if (!PlayerData.completedEndingIds.Contains(endingId))
        {
            PlayerData.completedEndingIds.Add(endingId);
        }

        Debug.Log($"[DataManager] 엔딩 기록됨: ID {endingId}, 직전 엔딩 ID: {PlayerData.lastEndingId}");
        SaveData();
    }

    /// <summary>
    /// 서버와 로컬데이터 동기화
    /// 9.9. 이학권 추가
    /// </summary>
    private IEnumerator SyncWithServer(string uid)
    {
        // 1) 서버에서 데이터 읽기
        var dbRef = FirebaseDatabase.DefaultInstance
            .GetReference($"users/{uid}/playerData");

        var fetchTask = dbRef.GetValueAsync();
        yield return new WaitUntil(() => fetchTask.IsCompleted);

        if (fetchTask.IsFaulted)
        {
            Debug.LogError("서버 데이터 가져오기 실패: " + fetchTask.Exception);
            yield break;
        }

        DataSnapshot snapshot = fetchTask.Result;
        if (snapshot.Exists)
        {
            // 2) 서버 JSON → GameData
            string serverJson = snapshot.GetRawJsonValue();
            var serverData = JsonUtility.FromJson<GameData>(serverJson);

            // 3) 타임스탬프 비교
            // 서버 권위 타임스탬프 우선 비교
            long serverSV = (serverData != null) ? serverData.lastUpdatedServer : 0;
            long localSV = (PlayerData != null) ? PlayerData.lastUpdatedServer : 0;
            bool serverHasSV = serverSV > 0;
            bool localHasSV = localSV > 0;

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
                Debug.Log("서버 데이터가 최신, 로컬 업데이트 완료.");
            }
            else
            {
                yield return UploadToServer(dbRef);
                Debug.Log("로컬 데이터가 최신 또는 동일, 서버 업데이트 완료.");
            }
        }
        else
        {
            // 서버에 데이터 없음 → 로컬 업로드
            yield return UploadToServer(dbRef);
            Debug.Log("서버 데이터 없음, 로컬 업로드 완료.");
        }
    }

    /// <summary>
    /// 서버에 로컬 세이브파일을 업로드 
    /// 9.9. 이학권 추가
    /// </summary>
    private IEnumerator UploadToServer(DatabaseReference dbRef)
    {
        // 1) 로컬 타임스탬프 갱신 및 전체 JSON 업로드
        SaveLocal();
        string json = JsonUtility.ToJson(PlayerData, true);
        var uploadTask = dbRef.SetRawJsonValueAsync(json);
        yield return new WaitUntil(() => uploadTask.IsCompleted);
        if (uploadTask.IsFaulted)
        {
            Debug.LogError("서버 업로드 실패" + uploadTask.Exception);
            yield break;
        }

        // 2) 서버 권위 타임스탬프 설정
        var updates = new Dictionary<string, object>
        {
            { "lastUpdatedServer", ServerValue.Timestamp }
        };
        var tsTask = dbRef.UpdateChildrenAsync(updates);
        yield return new WaitUntil(() => tsTask.IsCompleted);
        if (tsTask.IsFaulted)
        {
            Debug.LogError("서버 타임스탬프 설정 실패" + tsTask.Exception);
            yield break;
        }

        // 3) 서버가 기록한 값을 읽어와 로컬 반영
        var readTask = dbRef.Child("lastUpdatedServer").GetValueAsync();
        yield return new WaitUntil(() => readTask.IsCompleted);
        if (!readTask.IsFaulted && readTask.Result != null && long.TryParse(readTask.Result.Value?.ToString(), out var serverMillis))
        {
            PlayerData.lastUpdatedServer = serverMillis;
            SaveLocal();
        }
    }

    /// <summary>
    /// 게스트 계정의 로컬 데이터를 서버로 마이그레이션
    /// 계정 연동 시 호출됨
    /// </summary>
    public void UploadGuestDataToServer()
    {
        if (FirebaseManager.User == null)
        {
            Debug.LogError("[DataManager] Firebase 사용자가 없습니다. 업로드를 중단합니다.");
            return;
        }

        if (PlayerData == null)
        {
            Debug.LogWarning("[DataManager] 업로드할 게임 데이터가 없습니다.");
            return;
        }

        Debug.Log("[DataManager] 게스트 데이터를 서버로 마이그레이션 시작");
        StartCoroutine(UploadGuestDataCoroutine());
    }

    /// <summary>
    /// 게스트 데이터 업로드 코루틴
    /// </summary>
    private IEnumerator UploadGuestDataCoroutine()
    {
        string uid = FirebaseManager.User.UserId;
        var dbRef = FirebaseDatabase.DefaultInstance.GetReference($"users/{uid}/playerData");

        // 게스트 데이터에 마이그레이션 정보 추가
        PlayerData.lastUpdated = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        PlayerData.isMigratedFromGuest = true;
        PlayerData.migrationTimestamp = PlayerData.lastUpdated;

        string json = JsonUtility.ToJson(PlayerData, true);
        
        Debug.Log($"[DataManager] 게스트 데이터 업로드 중... (UID: {uid})");
        
        var uploadTask = dbRef.SetRawJsonValueAsync(json);
        yield return new WaitUntil(() => uploadTask.IsCompleted);

        if (uploadTask.IsFaulted)
        {
            Debug.LogError($"[DataManager] 게스트 데이터 업로드 실패: {uploadTask.Exception}");
            yield break;
        }

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

        // 서버 타임스탬프를 로컬에 반영
        var readTask = dbRef.Child("lastUpdatedServer").GetValueAsync();
        yield return new WaitUntil(() => readTask.IsCompleted);

        if (!readTask.IsFaulted && readTask.Result != null && 
            long.TryParse(readTask.Result.Value?.ToString(), out var serverMillis))
        {
            PlayerData.lastUpdatedServer = serverMillis;
            SaveLocal();
        }

        Debug.Log("[DataManager] 게스트 데이터 마이그레이션 완료");
        
        // 마이그레이션 완료 후 저장 방식 변경 (로컬 + 서버)
        _hasSyncedWithServer = true;
    }

    /// <summary>
    /// 게스트 계정용 로컬 전용 저장 (서버 동기화 없음)
    /// </summary>
    public void SaveLocalOnly()
    {
        if (PlayerData == null) return;
        if (_suppressSavesUntilGameplay) return;
        
        PlayerData.lastUpdated = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string json = JsonUtility.ToJson(PlayerData, true);

        bool saveSuccess = EncryptionUtility.SaveEncryptedFile(_playerDataSavePath, json);
        if (saveSuccess)
        {
            Debug.Log($"[DataManager] 게스트 전용 로컬 저장 완료: {_playerDataSavePath}");
        }
        else
        {
            Debug.LogError($"[DataManager] 게스트 데이터 저장 실패: {_playerDataSavePath}");
        }
    }

    /// <summary>
    /// 현재 저장 방식이 게스트 모드인지 확인
    /// </summary>
    public bool IsGuestMode()
    {
        return FirebaseManager.IsGuestAccount;
    }

    /// <summary>
    /// 저장 방식에 따른 적절한 저장 메서드 호출
    /// </summary>
    public void SaveData()
    {
        if (IsGuestMode())
        {
            Debug.Log("[DataManager] 게스트 모드: 로컬 전용 저장");
            SaveLocalOnly();
        }
        else
        {
            Debug.Log("[DataManager] 정식 계정: 로컬 저장 (서버 동기화는 별도 처리)");
            SaveLocal();
        }
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
            SaveData();
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
        try
        {
            await AllEventInitializeDataAsync();
            await ParameterEventInitializeDataAsync();
            
            // 모든 초기화가 완료된 후에만 _isReady를 설정
            _isReady.TrySetResult(true);
            Debug.Log("[DataManager] 모든 데이터 초기화가 완료되었습니다.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[DataManager] 데이터 초기화 실패: {ex.Message}");
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
            var subEventTask = Csvparser.ParseAsync<SubEventData>("SubEventData1");
            var subAnswerTask = Csvparser.ParseAsync<SubEventAnswerData>("SubEventAnswerID");
            // [추가] 특수 이벤트 데이터 로딩 (스키마 차이를 내부 변환으로 해소)
            var specialSubEventTask = Csvparser.ParseAsync<SpecialSubEventRow>("SpecialEventData");
            var specialSubAnswerTask = Csvparser.ParseAsync<SubEventAnswerData>("SpecialEventAnswerID");
            //룩업 테이블 로딩
            var bgDataTask = Csvparser.ParseAsync<BGData>("MainBGData");
            var sfxDataTask = Csvparser.ParseAsync<SFXData>("MainSFXData");
            var characterDataTask = Csvparser.ParseAsync<MainCharacterData>("MainCharacterData");
            var characterImgDataTask = Csvparser.ParseAsync<MainCharacterImgData>("MainCharacterImgData");
            // 특수 이벤트 캐릭터 표시명/이미지(감정명 포함)를 위한 CSV
            var specialEventCharacterTask = Csvparser.ParseAsync<SpecialEventCharacterData>("SpecialEventCharacterData");
            var battleResultTask = Csvparser.ParseAsync<BattleResultData>("BattleResultTextData");
            var backDataTask = Csvparser.ParseAsync<BackData>("BackData");
            //엔딩 데이터 
            var endingEventDataTask = Csvparser.ParseAsync<EndingEventData>("EndingEventData");
            var endingCutSceneTask = Csvparser.ParseAsync<EndingCutScene>("EndingEventCutScene");
            var endingNameDataTask = Csvparser.ParseAsync<EndingNameList>("EndingDataNameList");
            var trueEndingDataTask = Csvparser.ParseAsync<EndingEventData>("TrueEndingData");
            //회상 이벤트 데이터
            var endingMemoriarTask = Csvparser.ParseAsync<EndingMemoriarData>("EndingMemoriar");

            var mainEventList = await mainEventTask;
            var answerList = await answerTask;
            var characterList = await characterDataTask;
            var bgList = await bgDataTask;
            var sfxList = await sfxDataTask;
            var characterImgList = await characterImgDataTask;
            var endingEventList = await endingEventDataTask;
            var endingCutSceneList = await endingCutSceneTask;
            var endingNameList = await endingNameDataTask;
            var trueEndingList = await trueEndingDataTask;
            var battleResultList = await battleResultTask;
            var backDataList = await backDataTask;
            var subEventList = await subEventTask;
            var subAnswerList = await subAnswerTask;
            var specialSubEventRows = await specialSubEventTask;
            var specialSubAnswerList = await specialSubAnswerTask;
            var endingMemoriarList = await endingMemoriarTask;
            var specialEventCharacterList = await specialEventCharacterTask;

            Debug.Log("모든 파일 로딩 완료");

            // [추가] 특수 이벤트를 SubEvent 스키마로 변환 후 병합합니다.
            var combinedSubEventList = new List<SubEventData>(subEventList ?? new List<SubEventData>());
            var combinedSubAnswerList = new List<SubEventAnswerData>(subAnswerList ?? new List<SubEventAnswerData>());

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
            specialEventCharacterDict = specialEventCharacterList?.ToDictionary(c => c.CharacterImg_ID, c => c) 
                                        ?? new Dictionary<int, SpecialEventCharacterData>();
            endingEventDataDict = endingEventList.ToDictionary(e => e.ID, e => e);
            TrueEndingeventDataDict = trueEndingList.ToDictionary(e => e.ID, e => e);
            endingCutSceneDict = endingCutSceneList.ToDictionary(c => c.EndingCutScene_ID, c => c);
            endingNameDict = endingNameList.ToDictionary(c => c.EndingName_ID, c => c);
            backDataDict = backDataList.ToDictionary(d => d.Back_ID, d => d);
            // ID/AnswerID가 0이거나 중복인 항목을 제거하고 딕셔너리 구성 (CSV 구분줄 등 방지)
            var filteredSubEvents = combinedSubEventList
                .Where(e => e != null && e.ID > 0)
                .GroupBy(e => e.ID)
                .Select(g => g.First())
                .ToList();

            var filteredSubAnswers = combinedSubAnswerList
                .Where(a => a != null && a.AnswerID > 0)
                .GroupBy(a => a.AnswerID)
                .Select(g => g.First())
                .ToList();

            subEventDataDict = filteredSubEvents.ToDictionary(e => e.ID, e => e);
            subEventAnswerDataDict = filteredSubAnswers.ToDictionary(a => a.AnswerID, a => a);
            //회상 이벤트 데이터
            endingMemoriarDataDict = endingMemoriarList.ToDictionary(m => m.AnswerID, m => m);

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
                    }
                }
                else
                {
                    // 어떤 데이터가 가공에 실패했는지 로그로 확인
                    Debug.LogError($"[실패] ID: {rawData.ID}, StoryNum: {rawData.StoryNum} -> 가공 중 Null 반환됨. 이 데이터나 관련 데이터(BG, 캐릭터 등)에 문제가 있을 수 있습니다.");
                }
            }

            //-------------------------------서브 이벤트 데이터 가공단 ---------------------------------------------------------//
            FullSubEvents.Clear();
            if (combinedSubEventList != null && combinedSubAnswerList != null)
            {
                foreach (var rawData in combinedSubEventList)
                {
                    SubChoice leftChoice = null;
                    if (subEventAnswerDataDict.TryGetValue(rawData.AnswerLeftID, out var leftAnswerData))
                    {
                        leftChoice = new SubChoice
                        {
                            answerID = leftAnswerData.AnswerID,
                            choiceText = leftAnswerData.Text_KR,
                            nextEventID = leftAnswerData.NextTextID,
                            outcome = new ChoiceOutcome
                            {
                                parameterChanges = ParseRewardString(leftAnswerData.AnswerReward)
                            }
                        };
                    }

                    SubChoice rightChoice = null;
                    if (subEventAnswerDataDict.TryGetValue(rawData.AnswerRightID, out var rightAnswerData))
                    {
                        rightChoice = new SubChoice
                        {
                            answerID = rightAnswerData.AnswerID,
                            choiceText = rightAnswerData.Text_KR,
                            nextEventID = rightAnswerData.NextTextID,
                            outcome = new ChoiceOutcome
                            {
                                parameterChanges = ParseRewardString(rightAnswerData.AnswerReward)
                            }
                        };
                    }

                    BGData bgData = bgDataDict.TryGetValue(rawData.BG_ID, out var bg) ? bg : null;
                    SFXData sfxData = sfxDataDict.TryGetValue(rawData.SFX_ID, out var sfx) ? sfx : null;
                    MainCharacterData characterData = CharacterDataDict.TryGetValue(rawData.CharacterName, out var character) ? character : null;
                    MainCharacterImgData characterImgData = characterImgDataDict.TryGetValue(rawData.CharacterImg_ID, out var img) ? img : null;
                    if (characterImgData == null && specialEventCharacterDict != null && specialEventCharacterDict.TryGetValue(rawData.CharacterImg_ID, out var specialEntry) && specialEntry != null)
                    {
                        characterImgData = new MainCharacterImgData
                        {
                            CharacterImg_ID = specialEntry.CharacterImg_ID,
                            IMGName = specialEntry.IMGName
                        };
                    }
                    BackData backData = backDataDict.TryGetValue(rawData.Back_ID, out var back) ? back : null;


                    var fullEventData = new FullSubEventData
                    {
                        ID = rawData.ID,
                        SubStoryPac = rawData.SubStoryPac,
                        StoryNum = rawData.StoryNum,
                        Text_kr = rawData.Text_kr, // 텍스트 필드 추가

                        bgData = bgData,
                        sfxData = sfxData,
                        characterData = characterData,
                        characterImgData = characterImgData,
                        backData = backData,

                        leftChoice = leftChoice,
                        rightChoice = rightChoice
                    };

                    FullSubEvents.Add(fullEventData);
                }
                Debug.Log($"[DataManager] 서브 이벤트 데이터 통합 완료! 총 {FullSubEvents.Count}개의 이벤트가 준비되었습니다.");
            }
            else
            {
                Debug.LogError("[DataManager] 서브 이벤트 데이터 로딩 실패!");
            }
            // 특수 이벤트는 별도 목록으로 구성 (서브와 분리)
            FullSpecialEvents.Clear();
            if (specialSubEventRows != null && specialSubAnswerList != null)
            {
                var specialEvents = new List<SubEventData>();
                foreach (var row in specialSubEventRows)
                {
                    var conv = ConvertSpecialRowToSubEvent(row);
                    if (conv != null && conv.ID > 0) specialEvents.Add(conv);
                }
                specialEventAnswerDataDict = specialSubAnswerList
                    .Where(a => a != null && a.AnswerID > 0)
                    .GroupBy(a => a.AnswerID)
                    .Select(g => g.First())
                    .ToDictionary(a => a.AnswerID, a => a);

                foreach (var rawData in specialEvents)
                {
                    SubChoice leftChoice = null;
                    if (specialEventAnswerDataDict.TryGetValue(rawData.AnswerLeftID, out var leftAnswerData))
                    {
                        leftChoice = new SubChoice
                        {
                            answerID = leftAnswerData.AnswerID,
                            choiceText = leftAnswerData.Text_KR,
                            nextEventID = leftAnswerData.NextTextID,
                            outcome = new ChoiceOutcome
                            {
                                parameterChanges = ParseRewardString(leftAnswerData.AnswerReward)
                            }
                        };
                    }

                    SubChoice rightChoice = null;
                    if (specialEventAnswerDataDict.TryGetValue(rawData.AnswerRightID, out var rightAnswerData))
                    {
                        rightChoice = new SubChoice
                        {
                            answerID = rightAnswerData.AnswerID,
                            choiceText = rightAnswerData.Text_KR,
                            nextEventID = rightAnswerData.NextTextID,
                            outcome = new ChoiceOutcome
                            {
                                parameterChanges = ParseRewardString(rightAnswerData.AnswerReward)
                            }
                        };
                    }

                    BGData bgData = bgDataDict.TryGetValue(rawData.BG_ID, out var bg) ? bg : null;
                    SFXData sfxData = sfxDataDict.TryGetValue(rawData.SFX_ID, out var sfx) ? sfx : null;
                    MainCharacterData characterData = CharacterDataDict.TryGetValue(rawData.CharacterName, out var character) ? character : null;
                    MainCharacterImgData characterImgData = characterImgDataDict.TryGetValue(rawData.CharacterImg_ID, out var img) ? img : null;
                    if (characterImgData == null && specialEventCharacterDict != null && specialEventCharacterDict.TryGetValue(rawData.CharacterImg_ID, out var specialEntry2) && specialEntry2 != null)
                    {
                        characterImgData = new MainCharacterImgData
                        {
                            CharacterImg_ID = specialEntry2.CharacterImg_ID,
                            IMGName = specialEntry2.IMGName
                        };
                    }
                    BackData backData = backDataDict.TryGetValue(rawData.Back_ID, out var back) ? back : null;

                    var fullEventData = new FullSubEventData
                    {
                        ID = rawData.ID,
                        SubStoryPac = rawData.SubStoryPac,
                        StoryNum = rawData.StoryNum,
                        Text_kr = rawData.Text_kr,
                        bgData = bgData,
                        sfxData = sfxData,
                        characterData = characterData,
                        characterImgData = characterImgData,
                        backData = backData,
                        leftChoice = leftChoice,
                        rightChoice = rightChoice
                    };
                    FullSpecialEvents.Add(fullEventData);
                }
                Debug.Log($"[DataManager] 특수 이벤트 데이터 분리 완료! 총 {FullSpecialEvents.Count}개");
            }
            Debug.Log("---------- [DataManager] 엔딩 데이터 가공 시작 ----------");
            FullendingDataDict = new Dictionary<int, FullEndingData>();

            foreach (var rawData in endingEventList) // endingEventList는 로드된 EndingEventData 목록
            {
                var fullEndingData = GetEndingData(rawData.ID);
                if (fullEndingData != null)
                {
                    FullendingDataDict.Add(fullEndingData.ID, fullEndingData);

                }
            }

            TrueEndingDataDict = new Dictionary<int, FullEndingData>();

            foreach (var rawData in trueEndingList)
            {
                var trueEndingData = GetEndingData(rawData.ID);
                if (trueEndingData != null)
                {
                    TrueEndingDataDict.Add(trueEndingData.ID, trueEndingData);

                    if (!FullendingDataDict.ContainsKey(trueEndingData.ID))
                    {
                        FullendingDataDict.Add(trueEndingData.ID, trueEndingData);
                    }
                }
            }

            if (TrueEndingDataDict != null && TrueEndingeventDataDict != null)
            {
                foreach (var pair in TrueEndingDataDict.OrderBy(e => e.Key)) // ID 순서대로 정렬하여 확인
                {
                    int endingID = pair.Key;
                    FullEndingData fullData = pair.Value;
                }
                Debug.Log($"[DataManager] 총 {TrueEndingDataDict.Count}개의 진 엔딩 데이터 로딩 완료.");
            }
            else
            {
                Debug.LogWarning("[DataManager] 진 엔딩 딕셔너리가 초기화되지 않았습니다. 로딩 로직을 확인하세요.");
            }

            if (MultiEndingSystem.Instance != null)
            {
                MultiEndingSystem.Instance.InitializeEndings(FullendingDataDict);
            }
            else
            {
                // 만약 MultiEndingSystem이 꼭 필요하다면 여기서 에러를 띄워야 합니다.
                Debug.LogError("[DataManager] MultiEndingSystem 인스턴스를 찾을 수 없습니다. 인스턴스가 생성되는 시점을 확인하세요.");
            }

            Debug.Log("모든 이벤트 데이터가 성공적으로 로드되었습니다.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"데이터 로드 실패: {ex.Message}");
            throw; // 상위 메서드에서 예외 처리하도록 전파
        }
    }

    public async UniTask ParameterEventInitializeDataAsync()
    {
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
            Debug.Log("모든 파라미터 데이터가 성공적으로 로드되었습니다.");

        }
        catch (System.Exception ex)
        {
            Debug.LogError($"데이터 로드 실패: {ex.Message}");
            throw; // 상위 메서드에서 예외 처리하도록 전파
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
        if (backDataDict.TryGetValue(rawData.Back_ID, out var backData))
        {
            fullEventData.backData = backData;
        }

        // 왼쪽 및 오른쪽 선택지 구성
        fullEventData.leftChoice = CreateMainChoice(rawData.AnswerLeftID);
        fullEventData.rightChoice = CreateMainChoice(rawData.AnswerRightID);

        //데이터 확인용 로그
        Debug.Log($"<color=cyan>[DataManager] 이벤트 ID {eventID} 로드 성공!</color>");
        Debug.Log($"<b>대화 내용:</b> \"{fullEventData.dialogue}\"");
        Debug.Log($"<b>왼쪽 선택지:</b> '{fullEventData.leftChoice.choiceText}'");
        if (fullEventData.leftChoice.outcome.parameterChanges.Count > 0)
        {
            var changes = string.Join(", ", fullEventData.leftChoice.outcome.parameterChanges.Select(p => $"{p.parameterType} {p.valueChange}"));
            Debug.Log($"  <b>ㄴ 증감치:</b> {changes}");
        }
        Debug.Log($"<b>오른쪽 선택지:</b> '{fullEventData.rightChoice.choiceText}'");
        if (fullEventData.rightChoice.outcome.parameterChanges.Count > 0)
        {
            var changes = string.Join(", ", fullEventData.rightChoice.outcome.parameterChanges.Select(p => $"{p.parameterType} {p.valueChange}"));
            Debug.Log($"  <b>ㄴ 증감치:</b> {changes}");
        }

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

    /// <summary>
    /// 회차와 스토리 번호를 모두 고려하여 메인 이벤트 데이터를 찾습니다.
    /// </summary>
    public NewMainEventData GetMainEventDataByStoryNumAndLoop(int storyNum, int loopNum)
    {
        // 먼저 LoopNum과 StoryNum이 모두 일치하는 데이터를 찾습니다.
        var foundData = mainEventData.Values
                                     .Where(data => data.StoryNum == storyNum && data.LoopNum == loopNum)
                                     .OrderBy(data => data.id)
                                     .FirstOrDefault();

        if (foundData != null)
        {
            Debug.Log($"[DataManager] LoopNum {loopNum}, StoryNum {storyNum}에 해당하는 이벤트 데이터를 찾았습니다. ID: {foundData.id}");
            return foundData;
        }

        // LoopNum이 일치하는 데이터가 없으면 StoryNum만으로 찾습니다.
        foundData = mainEventData.Values
                                 .Where(data => data.StoryNum == storyNum)
                                 .OrderBy(data => data.id)
                                 .FirstOrDefault();

        if (foundData != null)
        {
            Debug.Log($"[DataManager] StoryNum {storyNum}에 해당하는 이벤트 데이터를 찾았습니다 (LoopNum 무시). ID: {foundData.id}, LoopNum: {foundData.LoopNum}");
            return foundData;
        }

        Debug.LogError($"[DataManager] LoopNum {loopNum}, StoryNum {storyNum}에 해당하는 이벤트 데이터를 찾을 수 없습니다.");
        return null;
    }

    private List<ParameterChange> ParseRewardString(string rewardString)
    {
        var changes = new List<ParameterChange>();

        if (string.IsNullOrEmpty(rewardString))
        {
            return changes;
        }

        var rewardItems = rewardString.Split(',');

        foreach (var item in rewardItems)
        {
            var cleanItem = item.Trim();
            if (string.IsNullOrEmpty(cleanItem)) continue;

            var parts = cleanItem.Split(' ');
            if (parts.Length < 3) continue;

            // 문자열을 ParameterType Enum으로 변환합니다.
            if (!Enum.TryParse(parts[0], out ParameterType parameterType))
            {
                Debug.LogError($"[DataManager] 파라미터 이름 '{parts[0]}'을 유효한 ParameterType으로 변환할 수 없습니다.");
                continue;
            }

            if (!int.TryParse(parts[1], out int value))
            {
                Debug.LogError($"[DataManager] 보상 값 '{parts[1]}'을 숫자로 변환할 수 없습니다.");
                continue;
            }

            var direction = parts[2];
            var finalValue = (direction == "하락") ? -value : value;

            // 수정된 ParameterChange 클래스에 맞게 값 할당
            changes.Add(new ParameterChange
            {
                parameterType = parameterType,
                valueChange = finalValue
            });
        }

        return changes;
    }

    private MainEventChoice CreateMainChoice(int answerID)
    {
        var choice = new MainEventChoice();

        choice.outcome = new ChoiceOutcome
        {
            parameterChanges = new List<ParameterChange>()
        };

        if (answerDataDict.TryGetValue(answerID, out var answerData))
        {
            //Debug.Log($"[DataManager] -> <color=green>성공!</color> AnswerID: {answerID}를 찾았습니다. 텍스트: '{answerData.Text_KR}', 다음 이벤트 ID: {answerData.NextTextID}");
            choice.ID = answerData.AnswerID;
            choice.choiceText = answerData.Text_KR;
            choice.nextEventID = answerData.NextTextID;

            //엔딩에 영향을 미치는지 플래그값 추가
            bool.TryParse(answerData.Ending_Memoriar, out choice.isEndingMemoriar);
            bool.TryParse(answerData.Counting_for_RealEnding2, out choice.isCountingforRealEnding2);
            bool.TryParse(answerData.Counting_for_RealEnding3, out choice.isCountingforRealEnding3);

            //선택지 보상치 적용 단 
            if (!string.IsNullOrEmpty(answerData.AnswerReward))
            {
                // ParseRewardString 함수를 호출하여 보상치 목록을 가져옵니다.
                choice.outcome.parameterChanges = ParseRewardString(answerData.AnswerReward);
            }
        }
        else
        {
            Debug.LogError($"[DataManager] -> <color=red>실패!</color> AnswerID: {answerID}에 해당하는 데이터를 'answerDataDict'에서 찾을 수 없습니다. MainAnswerID.csv 파일을 확인해주세요.");

            choice.choiceText = "선택지 데이터를 찾을 수 없습니다.";
        }

        return choice;
    }

    public void AddEndingMemoriar(string text)
    {
        // 중복을 방지하거나 필요한 로직을 추가할 수 있습니다.
        if (!string.IsNullOrEmpty(text))
        {
            acquiredEndingMemoriars.Add(text);
        }
    }

    public string GetFinalEndingText()
    {
        // 리스트의 모든 텍스트를 줄 바꿈으로 연결하여 반환
        return string.Join("\n", acquiredEndingMemoriars);
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

    public FullEndingData GetEndingData(int endingID)
    {
        EndingEventData rawData = null;

        if (endingEventDataDict != null && endingEventDataDict.TryGetValue(endingID, out rawData))
        {

        }

        else if (TrueEndingeventDataDict != null && TrueEndingeventDataDict.TryGetValue(endingID, out rawData))
        {
            
        }

        else
        {
            Debug.LogError($"[DataManager] ID {endingID}에 해당하는 엔딩 데이터를 찾을 수 없습니다.");
            return null;
        }


        var fullEndingData = new FullEndingData
        {
            ID = rawData.ID,
            PlayThrough = rawData.PlayThrough,
            EndingString = rawData.EndingString,
            Karma_Rate = rawData.Karma_Rate,
            Text_Kr = rawData.Text_Kr,
            Text_En = rawData.Text_En,
            Direction = rawData.Direction,
            Fade_Out_Color = rawData.Fade_Out_Color
        };
        if(endingNameDict.TryGetValue(rawData.EndingTitle, out var endingTitle))
        {
            fullEndingData.EndingTitle = endingTitle;
        }
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
        bool isBranchTriggered = false;
        // PlaythroughHistory가 초기화되었는지 확인
        if (global::PlaythroughHistory.Instance != null)
        {
            switch (rawData.ConditionType)
            {
                case 0: // 0: 조건 없음
                    isBranchTriggered = false;
                    break;

                case 1: // 1: 특정 파라미터 이벤트 경험
                    if (global::PlaythroughHistory.Instance != null)
                    {
                        // HCW의 PlaythroughHistory는 성공/실패 구분이 없으므로 단순히 완료 여부만 확인
                        isBranchTriggered = global::PlaythroughHistory.Instance.HasCompletedEvent(rawData.ChangeCondition);
                    }
                    break;

                case 2: // 2: 특정 서브 이벤트 그룹 경험
                    // HCW의 PlaythroughHistory에는 서브 이벤트 그룹 기능이 없으므로 false
                    isBranchTriggered = false;
                    break;

                case 3: // 3: 특정 전투 결과 경험
                    // 이 데이터는 GameData에 저장되므로 PlayerData에서 직접 확인
                    if (PlayerData != null)
                    {
                        isBranchTriggered = PlayerData.completedBattleResultIds.Contains(rawData.ChangeCondition);
                    }
                    break;

                case 4: // 4: 특정 엔딩 경험

                    if (PlayerData != null)
                    {
                        isBranchTriggered = PlayerData.completedEndingIds.Contains(rawData.ChangeCondition);
                    }
                    break;

                default:
                    isBranchTriggered = false;
                    break;
            }
        }

        var fullEventData = new EventData
        {
            id = eventID,
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

    //서브 이벤트의 ID값을 이용하여 데이터를 가져오는 메서드
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

    //GetSubEventDataById 선택지 데이터를 만드는 메서드
    private SubChoice CreateSubChoice(int answerID)
    {
        if (subEventAnswerDataDict.TryGetValue(answerID, out var rawData))
        {
            var choice = new SubChoice
            {
                answerID = rawData.AnswerID,
                choiceText = rawData.Text_KR,
                nextEventID = rawData.NextTextID,
                outcome = new ChoiceOutcome
                {
                    parameterChanges = new List<ParameterChange>()
                }
            };

            if (!string.IsNullOrEmpty(rawData.AnswerReward))
            {
                choice.outcome.parameterChanges = ParseRewardString(rawData.AnswerReward);
            }

            return choice;
        }

        Debug.LogError($"[DataManager] 서브 이벤트 AnswerID: {answerID}에 해당하는 데이터를 찾을 수 없습니다.");
        return null;
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
        int choiceTextId      = isLeft ? (isBranch ? rawData.AnotherLeftString : rawData.LeftString)
                                       : (isBranch ? rawData.AnotherRightString : rawData.RightString);
        int successStringId   = isLeft ? (isBranch ? rawData.AnotherAcceptString1 : rawData.AcceptString1)
                                       : (isBranch ? rawData.AnotherAcceptString2 : rawData.AcceptString2);
        int failStringId      = isLeft ? (isBranch ? rawData.AnotherDenyString1 : rawData.DenyString1)
                                       : (isBranch ? rawData.AnotherDenyString2 : rawData.DenyString2);
        int successRewardId   = isLeft ? (isBranch ? rawData.AnotherAcceptReward1 : rawData.AcceptReward1)
                                       : (isBranch ? rawData.AnotherAcceptReward2 : rawData.AcceptReward2);
        int failRewardId      = isLeft ? (isBranch ? rawData.AnotherDenyReward1 : rawData.DenyReward1)
                                       : (isBranch ? rawData.AnotherDenyReward2 : rawData.DenyReward2);
        int needType          = isLeft ? (isBranch ? rawData.AnotherNeedType1 : rawData.NeedType1)
                                       : (isBranch ? rawData.AnotherNeedType2 : rawData.NeedType2);
        int needValue         = isLeft ? (isBranch ? rawData.AnotehrNeedValue1 : rawData.NeedValue1) // 'Anotehr' 오타 대응
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

    //데이터 테이블
    [System.Serializable]
    public class ParameterEventData
    {
        public int ID { get; set; }
        public int RoundType { get; set; }
        public int PageType { get; set; }
        public int ConditionType { get; set; }
        public int ChangeCondition { get; set; }
        public int EventQuestion { get; set; }
        public int LeftString { get; set; }
        public int NeedType1 { get; set; }
        public int NeedValue1 { get; set; }
        public int AcceptReward1 { get; set; }
        public int DenyReward1 { get; set; }
        public int AcceptString1 { get; set; }
        public int DenyString1 { get; set; }
        public int RightString { get; set; }
        public int NeedType2 { get; set; }
        public int NeedValue2 { get; set; }
        public int AcceptReward2 { get; set; }
        public int DenyReward2 { get; set; }
        public int AcceptString2 { get; set; }
        public int DenyString2 { get; set; }
        public int AnotherEventQuestion { get; set; }
        public int AnotherLeftString { get; set; }
        public int AnotherNeedType1 { get; set; }
        public int AnotehrNeedValue1 { get; set; }
        public int AnotherAcceptReward1 { get; set; }
        public int AnotherDenyReward1 { get; set; }
        public int AnotherAcceptString1 { get; set; }
        public int AnotherDenyString1 { get; set; }
        public int AnotherRightString { get; set; }
        public int AnotherNeedType2 { get; set; }
        public int AnotherNeedValue2 { get; set; }
        public int AnotherAcceptReward2 { get; set; }
        public int AnotherDenyReward2 { get; set; }  // 이 줄이 올바른 이름입니다
        public int AnotherAcceptString2 { get; set; }
        public int AnotherDenyString2 { get; set; }

    }
    public AnswerData GetAnswerData(int answerId)
    {
        if (answerDataDict.TryGetValue(answerId, out var data))
        {
            return data;
        }
        Debug.LogWarning($"[DataManager] AnswerID {answerId}에 해당하는 AnswerData를 찾을 수 없습니다.");
        return null;
    }

    // PlayerPrefs를 사용하여 회차 기록을 관리하는 클래스
    public class PlaythroughHistory
    {
        public int ID { get; set; }
        public int RoundType { get; set; }
        public int PageType { get; set; }
        public int ConditionType { get; set; }
        public int ChangeCondition { get; set; }
        public int IsConditionSuccess { get; set; }
        public int EventQuestion { get; set; }
        public int LeftString { get; set; }
        public int NeedType1 { get; set; }
        public int NeedValue1 { get; set; }
        public int AcceptReward1 { get; set; }
        public int DenyReward1 { get; set; }
        public int AcceptString1 { get; set; }
        public int DenyString1 { get; set; }
        public int RightString { get; set; }
        public int NeedType2 { get; set; }
        public int NeedValue2 { get; set; }
        public int AcceptReward2 { get; set; }
        public int DenyReward2 { get; set; }
        public int AcceptString2 { get; set; }
        public int DenyString2 { get; set; }
        public int AnotherEventQuestion { get; set; }
        public int AnotherLeftString { get; set; }
        public int AnotherNeedType1 { get; set; }
        public int AnotehrNeedValue1 { get; set; }
        public int AnotherAcceptReward1 { get; set; }
        public int AnotherDenyReward1 { get; set; }
        public int AnotherAcceptString1 { get; set; }
        public int AnotherDenyString1 { get; set; }
        public int AnotherRightString { get; set; }
        public int AnotherNeedType2 { get; set; }
        public int AnotherNeedValue2 { get; set; }
        public int AnotherAcceptReward2 { get; set; }
        public int AnotherDenyReward2 { get; set; }
        public int AnotherAcceptString2 { get; set; }
        public int AnotherDenyString2 { get; set; }  // 이 줄이 올바른 이름입니다
    }
    [System.Serializable]
    public class ParameterRewardData
    {
        public int ID { get; set; }
        public int RewardType1 { get; set; }
        public int RewardValue1 { get; set; }
        public int RewardType2 { get; set; }
        public int RewardValue2 { get; set; }
        public int RewardType3 { get; set; }
        public int RewardValue3 { get; set; }
        public int RewardType4 { get; set; }
        public int RewardValue4 { get; set; }
        public int RewardType5 { get; set; }
        public int RewardValue5 { get; set; }
    }
    [System.Serializable]
    public class ParameterEventStringData
    {
        public int ID { get; set; }
        public string BG { get; set; }
        public string SoundEffect { get; set; }
        public int CharacterName { get; set; }
        public string CharacterImage { get; set; }
        public int IsFinishString { get; set; }
        public string String_kr { get; set; }
    }
    //룩업 테이블용 클래스 
    [System.Serializable]
    public class CharacterData
    {
        public string Chr_name { get; set; }
        public int Chr_index { get; set; }
    }

    // [추가] 특수 이벤트 CSV 행 정의 (모든 컬럼 문자열로 받아서 안전 파싱)
    [System.Serializable]
    public class SpecialSubEventRow
    {
        public string ID { get; set; }
        public string StoryPac { get; set; }
        public string StoryNum { get; set; }
        public string AnswerLeftID { get; set; }
        public string AnswerRightID { get; set; }
        public string BG_ID { get; set; }
        public string SFX_ID { get; set; }
        public string CharacterName { get; set; }
        public string Text_kr { get; set; }
        public string Text_en { get; set; }
        public string Back_ID { get; set; }
        public string CharacterImg_ID { get; set; }
        public string Font_Direction { get; set; }
    }

    // [추가] 특수 이벤트 스키마 → 기존 SubEventData 스키마로 변환
    private SubEventData ConvertSpecialRowToSubEvent(SpecialSubEventRow row)
    {
        if (row == null) return null;
        int ParseIntSafe(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            s = s.Trim();
            if (s.Equals("Null", System.StringComparison.OrdinalIgnoreCase) || s.Equals("NULL")) return 0;
            return int.TryParse(s, out var v) ? v : 0;
        }

        return new SubEventData
        {
            ID = ParseIntSafe(row.ID),
            SubStoryPac = ParseIntSafe(row.StoryPac),
            StoryNum = ParseIntSafe(row.StoryNum),
            AnswerLeftID = ParseIntSafe(row.AnswerLeftID),
            AnswerRightID = ParseIntSafe(row.AnswerRightID),
            BG_ID = ParseIntSafe(row.BG_ID),
            SFX_ID = ParseIntSafe(row.SFX_ID),
            CharacterName = ParseIntSafe(row.CharacterName),
            Text_kr = row.Text_kr ?? string.Empty,
            Text_en = row.Text_en ?? string.Empty,
            Back_ID = ParseIntSafe(row.Back_ID),
            CharacterImg_ID = ParseIntSafe(row.CharacterImg_ID),
            Font_Direction = ParseIntSafe(row.Font_Direction)
        };
    }
}