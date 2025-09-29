using UnityEngine;

/// <summary>
/// CSV 기반 음향 재생 시스템 사용 예시
/// 이 스크립트는 테스트 목적으로만 사용하세요.
/// </summary>
public class AudioTestExample : MonoBehaviour
{
    [Header("테스트용 설정")]
    [SerializeField] private int testBGId = 100000001; // Story1
    [SerializeField] private int testSFXId = 1000000001; // Door
    [SerializeField] private int testEventId = 10001; // 첫 번째 이벤트
    
    [Header("이름으로 테스트")]
    [SerializeField] private string testBGName = "Story1";
    [SerializeField] private string testSFXName = "Door";
    
    private void Start()
    {
        // AudioDataManager와 EventAudioPlayer가 초기화될 때까지 잠시 대기
        Invoke(nameof(TestAudioSystem), 1f);
    }
    
    private void TestAudioSystem()
    {
        Debug.Log("=== CSV 기반 음향 재생 시스템 테스트 시작 ===");
        
        // 1. BGM 재생 테스트
        TestBGMPlayback();
        
        // 2. SFX 재생 테스트 (3초 후)
        Invoke(nameof(TestSFXPlayback), 3f);
        
        // 3. 이벤트 데이터 기반 재생 테스트 (6초 후)
        Invoke(nameof(TestEventAudioPlayback), 6f);
        
        // 4. 이름으로 재생 테스트 (9초 후)
        Invoke(nameof(TestNameBasedPlayback), 9f);
    }
    
    private void TestBGMPlayback()
    {
        Debug.Log($"BGM 재생 테스트: ID {testBGId}");
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGMByID(testBGId);
        }
        else
        {
            Debug.LogError("AudioManager를 찾을 수 없습니다.");
        }
    }
    
    private void TestSFXPlayback()
    {
        Debug.Log($"SFX 재생 테스트: ID {testSFXId}");
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFXByID(testSFXId);
        }
        else
        {
            Debug.LogError("AudioManager를 찾을 수 없습니다.");
        }
    }
    
    private void TestEventAudioPlayback()
    {
        Debug.Log($"이벤트 음향 재생 테스트: Event ID {testEventId}");
        if (EventAudioPlayer.Instance != null)
        {
            // 실제 이벤트 데이터가 있다면 이렇게 사용
            // EventAudioPlayer.Instance.PlayAudioForEvent(testEventId);
            
            // 또는 직접 BG_ID와 SFX_ID를 지정
            EventAudioPlayer.Instance.PlayAudioFromEventData(testBGId, testSFXId);
        }
        else
        {
            Debug.LogError("EventAudioPlayer를 찾을 수 없습니다.");
        }
    }
    
    private void TestNameBasedPlayback()
    {
        Debug.Log($"이름 기반 음향 재생 테스트: BGM '{testBGName}', SFX '{testSFXName}'");
        if (EventAudioPlayer.Instance != null)
        {
            EventAudioPlayer.Instance.PlayAudioByName(testBGName, testSFXName);
        }
        else
        {
            Debug.LogError("EventAudioPlayer를 찾을 수 없습니다.");
        }
    }
    
    // 키보드 입력으로 테스트할 수 있는 메서드들
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            TestBGMPlayback();
        }
        
        if (Input.GetKeyDown(KeyCode.S))
        {
            TestSFXPlayback();
        }
        
        if (Input.GetKeyDown(KeyCode.E))
        {
            TestEventAudioPlayback();
        }
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopBGM();
                Debug.Log("BGM 정지");
            }
        }
        
        if (Input.GetKeyDown(KeyCode.N))
        {
            TestNameBasedPlayback();
        }
    }
    
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("=== CSV 기반 음향 재생 테스트 ===");
        GUILayout.Label("B키: BGM 재생 테스트");
        GUILayout.Label("S키: SFX 재생 테스트");
        GUILayout.Label("E키: 이벤트 음향 재생 테스트");
        GUILayout.Label("N키: 이름으로 음향 재생 테스트");
        GUILayout.Label("스페이스바: BGM 정지");
        GUILayout.EndArea();
    }
}
