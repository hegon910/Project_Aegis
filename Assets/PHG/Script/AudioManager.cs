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
        // DataManager에서 설정 불러오기
        if (DataManager.Instance?.PlayerData?.settings != null)
        {
            var settings = DataManager.Instance.PlayerData.settings;
            bgmVolume = settings.bgmVolume;
            sfxVolume = settings.sfxVolume;
            masterVolume = settings.masterVolume;
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
