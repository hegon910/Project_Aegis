using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BGAudioData
{
    public int BG_ID;
    public string BGName;
    public AudioClip audioClip;
}

[System.Serializable]
public class SFXAudioData
{
    public int SFX_ID;
    public string SFXName;
    public AudioClip audioClip;
}

public class AudioDataManager : MonoBehaviour
{
    public static AudioDataManager Instance { get; private set; }
    
    [Header("Audio Data")]
    [SerializeField] public List<BGAudioData> bgAudioList = new List<BGAudioData>();
    [SerializeField] public List<SFXAudioData> sfxAudioList = new List<SFXAudioData>();
    
    private Dictionary<int, BGAudioData> bgAudioDict = new Dictionary<int, BGAudioData>();
    private Dictionary<int, SFXAudioData> sfxAudioDict = new Dictionary<int, SFXAudioData>();
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAudioData();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void LoadAudioData()
    {
        LoadBGData();
        LoadSFXData();
        BuildDictionaries();
    }
    
    private void LoadBGData()
    {
        TextAsset csvFile = Resources.Load<TextAsset>("MainBGData");
        if (csvFile == null)
        {
            Debug.LogError("MainBGData.csv 파일을 찾을 수 없습니다.");
            return;
        }
        
        string[] lines = csvFile.text.Split('\n');
        
        for (int i = 1; i < lines.Length; i++) // 첫 번째 줄은 헤더이므로 건너뛰기
        {
            if (string.IsNullOrEmpty(lines[i].Trim())) continue;
            
            string[] values = ParseCSVLine(lines[i]);
            if (values.Length >= 2)
            {
                BGAudioData bgData = new BGAudioData();
                if (int.TryParse(values[0], out int bgId))
                {
                    bgData.BG_ID = bgId;
                    bgData.BGName = values[1];
                    
                    // Resources/Audio/BGM/ 폴더에서 오디오 클립 로드
                    string audioPath = $"Audio/BGM/{bgData.BGName}";
                    bgData.audioClip = Resources.Load<AudioClip>(audioPath);
                    
                    if (bgData.audioClip == null)
                    {
                        Debug.LogWarning($"BGM 오디오 클립을 찾을 수 없습니다: {audioPath}");
                    }
                    
                    bgAudioList.Add(bgData);
                }
            }
        }
        
        Debug.Log($"BGM 데이터 로드 완료: {bgAudioList.Count}개");
    }
    
    private void LoadSFXData()
    {
        TextAsset csvFile = Resources.Load<TextAsset>("MainSFXData");
        if (csvFile == null)
        {
            Debug.LogError("MainSFXData.csv 파일을 찾을 수 없습니다.");
            return;
        }
        
        string[] lines = csvFile.text.Split('\n');
        
        for (int i = 1; i < lines.Length; i++) // 첫 번째 줄은 헤더이므로 건너뛰기
        {
            if (string.IsNullOrEmpty(lines[i].Trim())) continue;
            
            string[] values = ParseCSVLine(lines[i]);
            if (values.Length >= 2)
            {
                SFXAudioData sfxData = new SFXAudioData();
                if (int.TryParse(values[0], out int sfxId))
                {
                    sfxData.SFX_ID = sfxId;
                    sfxData.SFXName = values[1];
                    
                    // Resources/Audio/SFX/ 폴더에서 오디오 클립 로드
                    string audioPath = $"Audio/SFX/{sfxData.SFXName}";
                    sfxData.audioClip = Resources.Load<AudioClip>(audioPath);
                    
                    if (sfxData.audioClip == null)
                    {
                        Debug.LogWarning($"SFX 오디오 클립을 찾을 수 없습니다: {audioPath}");
                    }
                    
                    sfxAudioList.Add(sfxData);
                }
            }
        }
        
        Debug.Log($"SFX 데이터 로드 완료: {sfxAudioList.Count}개");
    }
    
    private void BuildDictionaries()
    {
        bgAudioDict.Clear();
        sfxAudioDict.Clear();
        
        foreach (var bgData in bgAudioList)
        {
            bgAudioDict[bgData.BG_ID] = bgData;
        }
        
        foreach (var sfxData in sfxAudioList)
        {
            sfxAudioDict[sfxData.SFX_ID] = sfxData;
        }
    }
    
    private string[] ParseCSVLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        string currentField = "";
        
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(currentField.Trim());
                currentField = "";
            }
            else
            {
                currentField += c;
            }
        }
        
        result.Add(currentField.Trim());
        return result.ToArray();
    }
    
    public BGAudioData GetBGAudioData(int bgId)
    {
        if (bgAudioDict.TryGetValue(bgId, out BGAudioData data))
        {
            return data;
        }
        
        Debug.LogWarning($"BGM ID {bgId}에 해당하는 데이터를 찾을 수 없습니다.");
        return null;
    }
    
    public SFXAudioData GetSFXAudioData(int sfxId)
    {
        if (sfxAudioDict.TryGetValue(sfxId, out SFXAudioData data))
        {
            return data;
        }
        
        Debug.LogWarning($"SFX ID {sfxId}에 해당하는 데이터를 찾을 수 없습니다.");
        return null;
    }
    
    public AudioClip GetBGAudioClip(int bgId)
    {
        var data = GetBGAudioData(bgId);
        return data?.audioClip;
    }
    
    public AudioClip GetSFXAudioClip(int sfxId)
    {
        var data = GetSFXAudioData(sfxId);
        return data?.audioClip;
    }
    
    public string GetBGName(int bgId)
    {
        var data = GetBGAudioData(bgId);
        return data?.BGName ?? "Unknown";
    }
    
    public string GetSFXName(int sfxId)
    {
        var data = GetSFXAudioData(sfxId);
        return data?.SFXName ?? "Unknown";
    }
}
