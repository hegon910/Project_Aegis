// CheatManager.cs (새 스크립트)
using UnityEngine;
using System.Linq;

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
        if (GamePlayerStats.Instance == null)
        {
            return;
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

        // --- 엔딩 클리어 치트 (F12) ---
        if (Input.GetKeyDown(KeyCode.F12))
        {
            if (DataManager.Instance != null && DataManager.Instance.PlayerData != null && DataManager.Instance.IsDataReady)
            {
                int targetEndingTitleId = 3001; // 테스트할 엔딩 그룹 ID

                var endingsInGroup = DataManager.Instance.FullendingDataDict.Values
                    .Where(data => data.EndingTitle != null && data.EndingTitle.EndingName_ID == targetEndingTitleId)
                    .ToList();

                if (endingsInGroup.Count == 0)
                {
                    Debug.LogWarning($"[CHEAT] 엔딩 그룹 ID {targetEndingTitleId}에 해당하는 엔딩을 찾을 수 없습니다.");
                    return;
                }

                int addedCount = 0;
                foreach (var endingData in endingsInGroup)
                {
                    if (!DataManager.Instance.PlayerData.completedEndingIds.Contains(endingData.ID))
                    {
                        DataManager.Instance.PlayerData.completedEndingIds.Add(endingData.ID);
                        addedCount++;
                    }
                }

                Debug.Log($"[CHEAT] 엔딩 그룹 {targetEndingTitleId}에 속한 {addedCount}개의 새로운 엔딩을 완료 목록에 추가했습니다.");

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