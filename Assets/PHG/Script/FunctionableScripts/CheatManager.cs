// CheatManager.cs (새 스크립트)
using UnityEngine;

public class CheatManager : MonoBehaviour
{
    public static CheatManager Instance { get; private set; }
    [Header("치트 활성화")]
    [SerializeField] private bool enableCheats = true;
    
    [Header("Multi Ending System 테스터")]
    [SerializeField] private Canvas multiEndingTestCanvas;
    [SerializeField] private MultiEndingSystemTester multiEndingTester;
    private bool isMultiEndingTestActive = false;

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
    }

    private void Start()
    {
        // Multi Ending System 테스터 초기화
        InitializeMultiEndingTester();
    }

    private void InitializeMultiEndingTester()
    {
        // MultiEndingSystemTester가 없으면 찾아서 할당
        if (multiEndingTester == null)
        {
            multiEndingTester = FindObjectOfType<MultiEndingSystemTester>();
        }
        
        // 캔버스가 없으면 MultiEndingSystemTester의 부모 캔버스를 찾아서 할당
        if (multiEndingTestCanvas == null && multiEndingTester != null)
        {
            multiEndingTestCanvas = multiEndingTester.GetComponentInParent<Canvas>();
        }
        
        // 초기에는 비활성화
        if (multiEndingTestCanvas != null)
        {
            multiEndingTestCanvas.gameObject.SetActive(false);
            isMultiEndingTestActive = false;
        }
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    void Update()
    {
        // PlayerStats 인스턴스가 없으면 아무것도 하지 않음
        if (GamePlayerStats.Instance == null)
        {
            return;
        }

        // --- Multi Ending System 테스터 토글 (0번 키) ---
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            ToggleMultiEndingTester();
        }

        // --- 개별 파라미터 설정 (숫자 1~4) ---
        // Shift를 누르면 100, 안 누르면 0으로 설정
        int value = Input.GetKey(KeyCode.LeftShift) ? 100 : 5;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            GamePlayerStats.Instance.SetStat(ParameterType.정치력, value);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            GamePlayerStats.Instance.SetStat(ParameterType.병력, value);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            GamePlayerStats.Instance.SetStat(ParameterType.물자, value);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            GamePlayerStats.Instance.SetStat(ParameterType.리더십, value);
        }

        // --- 전체 파라미터 설정 (F1) ---
        // Shift를 누르면 100, 안 누르면 1로 설정
        if (Input.GetKeyDown(KeyCode.F1))
        {
            int allValue = Input.GetKey(KeyCode.LeftShift) ? 100 : 1;
            GamePlayerStats.Instance.SetStat(ParameterType.정치력, allValue);
            GamePlayerStats.Instance.SetStat(ParameterType.병력, allValue);
            GamePlayerStats.Instance.SetStat(ParameterType.물자, allValue);
            GamePlayerStats.Instance.SetStat(ParameterType.리더십, allValue);
        }

        // --- 카르마 설정 (5, 6번 키) ---
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            int currentKarma = GamePlayerStats.Instance.GetStat(ParameterType.카르마);
            int newKarma = Mathf.Max(0, currentKarma - 10);
            GamePlayerStats.Instance.SetStat(ParameterType.카르마, newKarma);
            Debug.Log($"[CHEAT] 카르마 10 감소: {currentKarma} → {newKarma}");
        }
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            int currentKarma = GamePlayerStats.Instance.GetStat(ParameterType.카르마);
            int newKarma = currentKarma + 10;
            GamePlayerStats.Instance.SetStat(ParameterType.카르마, newKarma);
            Debug.Log($"[CHEAT] 카르마 10 증가: {currentKarma} → {newKarma}");
        }

        // --- 전세(전황) 설정 (F5, F6, F7) ---
        if (Input.GetKeyDown(KeyCode.F5))
        {
            GamePlayerStats.Instance.SetStat(ParameterType.전황, 10);
        }
        if (Input.GetKeyDown(KeyCode.F6))
        {
            GamePlayerStats.Instance.SetStat(ParameterType.전황, 50);
        }
        if (Input.GetKeyDown(KeyCode.F7))
        {
            GamePlayerStats.Instance.SetStat(ParameterType.전황, 90);
        }

        // '+' 키를 누르면 재화 2000개 추가
        if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            if (CurrencyManager.AddCurrency(2000))
            {
                Debug.Log("[CHEAT] 재화 2000개 추가 완료!");
            }
            else
            {
                Debug.LogError("[CHEAT] 재화 추가 실패!");
            }
        }
        // --- 엔딩 클리어 치트 (F12) ---
        if (Input.GetKeyDown(KeyCode.F12))
        {
            if (DataManager.Instance != null && DataManager.Instance.PlayerData != null)
            {
                int testEndingId = 50001; // 실제 존재하는 테스트용 엔딩 ID
                if (!DataManager.Instance.PlayerData.completedEndingIds.Contains(testEndingId))
                {
                    DataManager.Instance.PlayerData.completedEndingIds.Add(testEndingId);
                    Debug.Log($"[CHEAT] 테스트용 엔딩 ID {testEndingId}를 완료 목록에 추가했습니다.");
                }
                else
                {
                    Debug.Log($"[CHEAT] 엔딩 ID {testEndingId}는 이미 완료 목록에 있습니다.");
                }
                // 변경사항을 즉시 저장
                DataManager.Instance.SaveData();
            }
        }

        // ']' 키를 누르면 다음 이벤트로 넘어갑니다.
        if (Input.GetKeyDown(KeyCode.RightBracket))
        {
            Debug.Log("치트 키: 다음 이벤트로 스킵합니다.");

            // 메인 시나리오가 실행 중인지 먼저 확인
            var mainScenarioManager = FindObjectOfType<MainScenarioManager>();
            if (mainScenarioManager != null && mainScenarioManager.IsScenarioRunning)
            {
                mainScenarioManager.SkipToNextNode();
            }
            // 서브이벤트가 진행 중인지 확인
            else if (EventManager.Instance != null && EventManager.Instance.currentState == EventManagerState.InSubEvent)
            {
                Debug.Log("치트 키: 서브이벤트를 스킵합니다.");
                SkipSubEvent();
            }
            // 그렇지 않으면 일반 이벤트(파라미터) 스킵 시도
            else if (EventManager.Instance != null)
            {
                // 현재 UI 전환 효과 등을 무시하고 즉시 다음 턴을 호출합니다.
                EventManager.Instance.PlayNextTurn();
            }
        }
        if (Input.GetKeyDown(KeyCode.N))
        {
            SkipCurrentState();
        }
    }

    /// <summary>
    /// Multi Ending System 테스터를 토글합니다.
    /// </summary>
    private void ToggleMultiEndingTester()
    {
        // MultiEndingSystemTester가 없으면 다시 찾아보기
        if (multiEndingTester == null)
        {
            multiEndingTester = FindObjectOfType<MultiEndingSystemTester>();
        }
        
        // 캔버스가 없으면 MultiEndingSystemTester의 부모 캔버스를 찾아서 할당
        if (multiEndingTestCanvas == null && multiEndingTester != null)
        {
            multiEndingTestCanvas = multiEndingTester.GetComponentInParent<Canvas>();
        }
        
        if (multiEndingTestCanvas == null)
        {
            Debug.LogWarning("[CHEAT] Multi Ending System 테스터 캔버스를 찾을 수 없습니다.");
            return;
        }
        
        // 토글
        isMultiEndingTestActive = !isMultiEndingTestActive;
        multiEndingTestCanvas.gameObject.SetActive(isMultiEndingTestActive);
        
        Debug.Log($"[CHEAT] Multi Ending System 테스터 {(isMultiEndingTestActive ? "활성화" : "비활성화")}");
    }

    public void SkipCurrentState()
    {
        if (GameManager.instance != null)
        {
            Debug.LogWarning("[CHEAT] 현재 상태를 스킵하고 다음으로 진행합니다.");
            GameManager.instance.ForceResetTransitionFlag();
            GameManager.instance.OnStateFinished();
        }
        else
        {
            Debug.LogError("[CHEAT] GameManager 인스턴스를 찾을 수 없어 스킵할 수 없습니다.");
        }
    }

    private void SkipSubEvent()
    {
        // UIFlowSimulator에서 현재 진행 중인 서브이벤트를 스킵
        var uiFlowSimulator = FindObjectOfType<UIFlowSimulator>();
        if (uiFlowSimulator != null)
        {
            // 서브이벤트의 첫 번째 선택지(왼쪽)를 자동으로 선택하여 스킵
            uiFlowSimulator.HandleChoice(false);
            Debug.Log("[CHEAT] 서브이벤트를 첫 번째 선택지로 스킵했습니다.");
        }
        else
        {
            Debug.LogError("[CHEAT] UIFlowSimulator를 찾을 수 없어 서브이벤트를 스킵할 수 없습니다.");
        }
    }
#endif
}