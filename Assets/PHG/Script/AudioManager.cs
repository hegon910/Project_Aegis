using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    
    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmAudioSource;
    [SerializeField] private AudioSource sfxAudioSource;
    
    [Header("Volume Settings")]
    [SerializeField] private float masterVolume = 1f;
    [SerializeField] private float bgmVolume = 1f;
    [SerializeField] private float sfxVolume = 1f;
    
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
        // DataManager에서 설정 불러오기 (설정이 있는 경우에만)
        if (DataManager.Instance?.PlayerData?.settings != null)
        {
            var settings = DataManager.Instance.PlayerData.settings;
            // 설정값이 유효한 경우에만 적용 (0보다 큰 값)
            if (settings.bgmVolume > 0) bgmVolume = settings.bgmVolume;
            if (settings.sfxVolume > 0) sfxVolume = settings.sfxVolume;
            if (settings.masterVolume > 0) masterVolume = settings.masterVolume;
        }
        
        UpdateVolumes();
    }
    
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
    }
    
    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
    }
    
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
    }
    
    private void UpdateVolumes()
    {
        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = masterVolume * bgmVolume;
        }
        
        if (sfxAudioSource != null)
        {
            sfxAudioSource.volume = masterVolume * sfxVolume;
        }
    }
    
    public void PlayBGM(AudioClip clip, bool loop = true)
    {
        if (bgmAudioSource != null && clip != null)
        {
            bgmAudioSource.clip = clip;
            bgmAudioSource.loop = loop;
            bgmAudioSource.Play();
        }
    }
    
    public void StopBGM()
    {
        if (bgmAudioSource != null)
        {
            bgmAudioSource.Stop();
        }
    }
    
    /// <summary>
    /// BGM을 페이드 아웃시킵니다.
    /// </summary>
    /// <param name="fadeTime">페이드 아웃 시간 (초)</param>
    public IEnumerator FadeOutBGM(float fadeTime)
    {
        if (bgmAudioSource != null && bgmAudioSource.isPlaying)
        {
            float startVolume = bgmAudioSource.volume;
            float elapsedTime = 0f;
            
            while (elapsedTime < fadeTime)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / fadeTime;
                bgmAudioSource.volume = Mathf.Lerp(startVolume, 0f, progress);
                yield return null;
            }
            
            bgmAudioSource.Stop();
            bgmAudioSource.volume = startVolume; // 볼륨 복원
            Debug.Log("[AudioManager] BGM 페이드 아웃 완료");
        }
    }
    
    /// <summary>
    /// BGM을 페이드 인시킵니다.
    /// </summary>
    /// <param name="fadeTime">페이드 인 시간 (초)</param>
    public IEnumerator FadeInBGM(float fadeTime)
    {
        if (bgmAudioSource != null)
        {
            float targetVolume = masterVolume * bgmVolume;
            float startVolume = 0f;
            bgmAudioSource.volume = startVolume;
            
            // BGM이 재생 중이 아니면 재생 시작
            if (!bgmAudioSource.isPlaying)
            {
                bgmAudioSource.Play();
            }
            
            float elapsedTime = 0f;
            
            while (elapsedTime < fadeTime)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / fadeTime;
                bgmAudioSource.volume = Mathf.Lerp(startVolume, targetVolume, progress);
                yield return null;
            }
            
            bgmAudioSource.volume = targetVolume;
            Debug.Log("[AudioManager] BGM 페이드 인 완료");
        }
    }
    
    public void PlaySFX(AudioClip clip)
    {
        if (sfxAudioSource != null && clip != null)
        {
            sfxAudioSource.PlayOneShot(clip);
        }
    }
    
    public void PlaySFX(AudioClip clip, float volumeScale)
    {
        if (sfxAudioSource != null && clip != null)
        {
            sfxAudioSource.PlayOneShot(clip, volumeScale);
        }
    }
    
    // CSV 기반 음향 재생 메서드들
    public void PlayBGMByID(int bgId, bool loop = true)
    {
        if (AudioDataManager.Instance != null)
        {
            AudioClip clip = AudioDataManager.Instance.GetBGAudioClip(bgId);
            if (clip != null)
            {
                PlayBGM(clip, loop);
                Debug.Log($"BGM 재생: ID {bgId} - {AudioDataManager.Instance.GetBGName(bgId)}");
            }
            else
            {
                Debug.LogWarning($"BGM ID {bgId}에 해당하는 오디오 클립을 찾을 수 없습니다.");
            }
        }
        else
        {
            Debug.LogError("AudioDataManager 인스턴스를 찾을 수 없습니다.");
        }
    }
    
    public void PlaySFXByID(int sfxId)
    {
        if (AudioDataManager.Instance != null)
        {
            AudioClip clip = AudioDataManager.Instance.GetSFXAudioClip(sfxId);
            if (clip != null)
            {
                PlaySFX(clip);
                Debug.Log($"SFX 재생: ID {sfxId} - {AudioDataManager.Instance.GetSFXName(sfxId)}");
            }
            else
            {
                Debug.LogWarning($"SFX ID {sfxId}에 해당하는 오디오 클립을 찾을 수 없습니다.");
            }
        }
        else
        {
            Debug.LogError("AudioDataManager 인스턴스를 찾을 수 없습니다.");
        }
    }
    
    public void PlaySFXByID(int sfxId, float volumeScale)
    {
        if (AudioDataManager.Instance != null)
        {
            AudioClip clip = AudioDataManager.Instance.GetSFXAudioClip(sfxId);
            if (clip != null)
            {
                PlaySFX(clip, volumeScale);
                Debug.Log($"SFX 재생: ID {sfxId} - {AudioDataManager.Instance.GetSFXName(sfxId)} (볼륨: {volumeScale})");
            }
            else
            {
                Debug.LogWarning($"SFX ID {sfxId}에 해당하는 오디오 클립을 찾을 수 없습니다.");
            }
        }
        else
        {
            Debug.LogError("AudioDataManager 인스턴스를 찾을 수 없습니다.");
        }
    }
    
    // 이벤트 데이터에서 음향 재생하는 메서드
    public void PlayAudioFromEventData(int bgId, int sfxId)
    {
        // BGM 재생 (0이 아닌 경우에만)
        if (bgId != 0)
        {
            PlayBGMByID(bgId);
        }
        
        // SFX 재생 (0이 아닌 경우에만)
        if (sfxId != 0)
        {
            PlaySFXByID(sfxId);
        }
    }
    
    // 이름으로 음향 재생하는 메서드들
    public void PlayBGMByName(string bgName, bool loop = true)
    {
        if (AudioDataManager.Instance != null)
        {
            var bgData = AudioDataManager.Instance.bgAudioList.Find(data => data.BGName == bgName);
            if (bgData != null && bgData.audioClip != null)
            {
                PlayBGM(bgData.audioClip, loop);
                Debug.Log($"BGM 재생: {bgName}");
            }
            else
            {
                Debug.LogWarning($"BGM '{bgName}'에 해당하는 오디오 클립을 찾을 수 없습니다.");
            }
        }
        else
        {
            Debug.LogError("AudioDataManager 인스턴스를 찾을 수 없습니다.");
        }
    }
    
    public void PlaySFXByName(string sfxName)
    {
        if (AudioDataManager.Instance != null)
        {
            var sfxData = AudioDataManager.Instance.sfxAudioList.Find(data => data.SFXName == sfxName);
            if (sfxData != null && sfxData.audioClip != null)
            {
                PlaySFX(sfxData.audioClip);
                Debug.Log($"SFX 재생: {sfxName}");
            }
            else
            {
                Debug.LogWarning($"SFX '{sfxName}'에 해당하는 오디오 클립을 찾을 수 없습니다.");
            }
        }
        else
        {
            Debug.LogError("AudioDataManager 인스턴스를 찾을 수 없습니다.");
        }
    }
    
    public void PlaySFXByName(string sfxName, float volumeScale)
    {
        if (AudioDataManager.Instance != null)
        {
            var sfxData = AudioDataManager.Instance.sfxAudioList.Find(data => data.SFXName == sfxName);
            if (sfxData != null && sfxData.audioClip != null)
            {
                PlaySFX(sfxData.audioClip, volumeScale);
                Debug.Log($"SFX 재생: {sfxName} (볼륨: {volumeScale})");
            }
            else
            {
                Debug.LogWarning($"SFX '{sfxName}'에 해당하는 오디오 클립을 찾을 수 없습니다.");
            }
        }
        else
        {
            Debug.LogError("AudioDataManager 인스턴스를 찾을 수 없습니다.");
        }
    }
    
    // 이름으로 이벤트 데이터에서 음향 재생하는 메서드
    public void PlayAudioFromEventDataByName(string bgName, string sfxName)
    {
        // BGM 재생 (빈 문자열이 아닌 경우에만)
        if (!string.IsNullOrEmpty(bgName) && bgName != "None")
        {
            PlayBGMByName(bgName);
        }
        
        // SFX 재생 (빈 문자열이 아닌 경우에만)
        if (!string.IsNullOrEmpty(sfxName) && sfxName != "None")
        {
            PlaySFXByName(sfxName);
        }
    }
    
    public float GetMasterVolume()
    {
        return masterVolume;
    }
    
    public float GetBGMVolume()
    {
        return bgmVolume;
    }
    
    public float GetSFXVolume()
    {
        return sfxVolume;
    }
}
