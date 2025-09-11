// CheatManager.cs (새 스크립트)
using UnityEngine;

public class CheatManager : MonoBehaviour
{
    public static CheatManager Instance { get; private set; }
    [Header("치트 활성화")]
    [SerializeField] private bool enableCheats = true;
    // [Header("설정")]
    // [Tooltip("이 스크립트는 에디터와 개발 빌드에서만 동작합니다.")]
    // public bool enableCheats = true;

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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    void Update()
    {
        // PlayerStats 인스턴스가 없으면 아무것도 하지 않음
        if (PlayerStats.Instance == null)
        {
            return;
        }

        // --- 개별 파라미터 설정 (숫자 1~4) ---
        // Shift를 누르면 100, 안 누르면 0으로 설정
        int value = Input.GetKey(KeyCode.LeftShift) ? 100 : 5;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            PlayerStats.Instance.SetStat(ParameterType.정치력, value);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            PlayerStats.Instance.SetStat(ParameterType.병력, value);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            PlayerStats.Instance.SetStat(ParameterType.물자, value);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            PlayerStats.Instance.SetStat(ParameterType.리더십, value);
        }

        // --- 전체 파라미터 설정 (F1) ---
        // Shift를 누르면 100, 안 누르면 1로 설정
        if (Input.GetKeyDown(KeyCode.F1))
        {
            int allValue = Input.GetKey(KeyCode.LeftShift) ? 100 : 1;
            PlayerStats.Instance.SetStat(ParameterType.정치력, allValue);
            PlayerStats.Instance.SetStat(ParameterType.병력, allValue);
            PlayerStats.Instance.SetStat(ParameterType.물자, allValue);
            PlayerStats.Instance.SetStat(ParameterType.리더십, allValue);
        }

        // --- 전세(전황) 설정 (F5, F6, F7) ---
        if (Input.GetKeyDown(KeyCode.F5))
        {
            PlayerStats.Instance.SetStat(ParameterType.전황, 10);
        }
        if (Input.GetKeyDown(KeyCode.F6))
        {
            PlayerStats.Instance.SetStat(ParameterType.전황, 50);
        }
        if (Input.GetKeyDown(KeyCode.F7))
        {
            PlayerStats.Instance.SetStat(ParameterType.전황, 90);
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
            // 그렇지 않으면 일반 이벤트(파라미터/서브) 스킵 시도
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
#endif
}