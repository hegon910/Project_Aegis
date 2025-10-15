using UnityEngine;

public class EventAudioPlayer : MonoBehaviour
{
    public static EventAudioPlayer Instance { get; private set; }
    
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
    
    /// <summary>
    /// 이벤트 ID로부터 음향을 재생합니다.
    /// </summary>
    /// <param name="eventId">이벤트 ID</param>
    public void PlayAudioForEvent(int eventId)
    {
        // MainEventData.csv에서 해당 이벤트의 BG_ID와 SFX_ID를 찾아서 재생
        var eventData = GetEventData(eventId);
        if (eventData != null)
        {
            // 기존 EventData 클래스의 BG와 SE 필드를 사용
            int bgId = ParseAudioId(eventData.BG);
            int sfxId = ParseAudioId(eventData.SE);
            PlayAudioFromEventData(bgId, sfxId);
        }
    }
    
    /// <summary>
    /// BG_ID와 SFX_ID를 직접 지정해서 음향을 재생합니다.
    /// </summary>
    /// <param name="bgId">BGM ID</param>
    /// <param name="sfxId">SFX ID</param>
    public void PlayAudioFromEventData(int bgId, int sfxId)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayAudioFromEventData(bgId, sfxId);
        }
        else
        {
            Debug.LogError("AudioManager 인스턴스를 찾을 수 없습니다.");
        }
    }
    
    /// <summary>
    /// BGM만 재생합니다.
    /// </summary>
    /// <param name="bgId">BGM ID</param>
    public void PlayBGMOnly(int bgId)
    {
        if (AudioManager.Instance != null && bgId != 0)
        {
            AudioManager.Instance.PlayBGMByID(bgId);
        }
    }
    
    /// <summary>
    /// SFX만 재생합니다.
    /// </summary>
    /// <param name="sfxId">SFX ID</param>
    public void PlaySFXOnly(int sfxId)
    {
        if (AudioManager.Instance != null && sfxId != 0)
        {
            AudioManager.Instance.PlaySFXByID(sfxId);
        }
    }
    
    /// <summary>
    /// BGM 이름으로 재생합니다.
    /// </summary>
    /// <param name="bgName">BGM 이름</param>
    public void PlayBGMByName(string bgName)
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(bgName) && bgName != "None")
        {
            AudioManager.Instance.PlayBGMByName(bgName);
        }
    }
    
    /// <summary>
    /// SFX 이름으로 재생합니다.
    /// </summary>
    /// <param name="sfxName">SFX 이름</param>
    public void PlaySFXByName(string sfxName)
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(sfxName) && sfxName != "None")
        {
            AudioManager.Instance.PlaySFXByName(sfxName);
        }
    }
    
    /// <summary>
    /// SFX 이름으로 볼륨 조절과 함께 재생합니다.
    /// </summary>
    /// <param name="sfxName">SFX 이름</param>
    /// <param name="volumeScale">볼륨 스케일</param>
    public void PlaySFXByName(string sfxName, float volumeScale)
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(sfxName) && sfxName != "None")
        {
            AudioManager.Instance.PlaySFXByName(sfxName, volumeScale);
        }
    }
    
    /// <summary>
    /// BGM과 SFX 이름으로 재생합니다.
    /// </summary>
    /// <param name="bgName">BGM 이름</param>
    /// <param name="sfxName">SFX 이름</param>
    public void PlayAudioByName(string bgName, string sfxName)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayAudioFromEventDataByName(bgName, sfxName);
        }
        else
        {
            Debug.LogError("AudioManager 인스턴스를 찾을 수 없습니다.");
        }
    }
    
    /// <summary>
    /// 이벤트 데이터를 가져옵니다. (실제 구현에서는 DataManager나 다른 데이터 관리 시스템을 사용)
    /// </summary>
    /// <param name="eventId">이벤트 ID</param>
    /// <returns>이벤트 데이터</returns>
    private EventData GetEventData(int eventId)
    {
        // 실제 구현에서는 DataManager나 다른 데이터 관리 시스템을 사용해야 합니다.
        // 여기서는 예시로 null을 반환합니다.
        Debug.LogWarning($"이벤트 데이터를 가져오는 기능은 DataManager와 연동되어야 합니다. EventID: {eventId}");
        return null;
    }
    
    /// <summary>
    /// 문자열을 오디오 ID로 변환합니다.
    /// </summary>
    /// <param name="audioString">오디오 문자열 (예: "100000001" 또는 "Story1")</param>
    /// <returns>변환된 오디오 ID, 변환 실패 시 0</returns>
    private int ParseAudioId(string audioString)
    {
        if (string.IsNullOrEmpty(audioString) || audioString == "None")
        {
            return 0;
        }
        
        // 숫자로 시작하는 경우 직접 파싱
        if (int.TryParse(audioString, out int audioId))
        {
            return audioId;
        }
        
        // 문자열인 경우 AudioDataManager에서 이름으로 ID 찾기
        if (AudioDataManager.Instance != null)
        {
            // BGM 이름으로 검색
            foreach (var bgData in AudioDataManager.Instance.bgAudioList)
            {
                if (bgData.BGName == audioString)
                {
                    return bgData.BG_ID;
                }
            }
            
            // SFX 이름으로 검색
            foreach (var sfxData in AudioDataManager.Instance.sfxAudioList)
            {
                if (sfxData.SFXName == audioString)
                {
                    return sfxData.SFX_ID;
                }
            }
        }
        
        Debug.LogWarning($"오디오 ID를 찾을 수 없습니다: {audioString}");
        return 0;
    }
}
