using System;
using System.Collections.Generic;

[Serializable]
public class GameSettings
{
    // 사운드, 언어 등 환경설정 값
    public float masterVolume = 1.0f;
    public float bgmVolume = 1.0f;
    public float sfxVolume = 1.0f;
}

[Serializable]
public class SettingsData
{
    public List<int> selectedSubEventPackIDs; // 여러 ID를 저장할 수 있도록 List<int>로 변경
    public GameSettings settings;

    /// <summary>
    /// 새 설정 파일 생성 시 기본값
    /// </summary>
    public SettingsData()
    {
        selectedSubEventPackIDs = new List<int>(); // 빈 리스트로 초기화
        settings = new GameSettings();
    }
}