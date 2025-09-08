using System;
using UnityEngine;

[System.Serializable]
public class MainEventData
{
    public int ID { get; set; }
    public int MainStoryPac { get; set; }
    public int LoopNum { get; set; }
    public int StoryNum { get; set; }
    public string EndingString { get; set; }
    public int AnswerLeftID { get; set; }
    public int AnswerRightID { get; set; }
    public long BG_ID { get; set; }
    public long SFX_ID { get; set; }
    public int CharacterName { get; set; }
    public string Text_kr { get; set; }
    public string Text_en { get; set; }
    public long CharacterImg_ID { get; set; }
    public long Font_Direction { get; set; }
}

[System.Serializable]
public class NewMainEventData
{
    // CSV의 ID와 매핑되는 필드
    public int id;
    public string dialogue;
    public string CharacterName;

    // 이제 string 대신 실제 데이터 객체를 받습니다.
    public BGData bgData;
    public SFXData sfxData;
    public MainCharacterData characterData;
    public MainCharacterImgData characterImgData;

    // 선택지 데이터
    public NewEventChoice leftChoice;
    public NewEventChoice rightChoice;
}