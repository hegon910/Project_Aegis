using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SubEventData
{
    public int ID { get; set; }
    public int SubStoryPac { get; set; }
    public int StoryNum { get; set; }
    public int AnswerLeftID { get; set; }
    public int AnswerRightID { get; set; }
    public int BG_ID { get; set; }
    public int SFX_ID { get; set; }
    public int CharacterName { get; set; } // Chr_ID에 대응
    public string Text_kr { get; set; }
    public string Text_en { get; set; }
    public int Back_ID { get; set; }
    public int CharacterImg_ID { get; set; }
    public int Font_Direction { get; set; }
}

[System.Serializable]
public class SubEventAnswerData
{
    public int AnswerID { get; set; }
    public string Text_KR { get; set; }
    public string Text_EN { get; set; }
    public int NextTextID { get; set; }
    public string AnswerReward { get; set; }
    public int Font_Direction { get; set; }
}

public class FullSubEventData
{
    public int ID { get; set; }
    public int SubStoryPac { get; set; }
    public int StoryNum { get; set; }
    public string Text_kr { get; set; }

    // 연결된 데이터 객체들
    public BGData bgData;
    public SFXData sfxData;
    public MainCharacterData characterData;
    public MainCharacterImgData characterImgData;
    public BackData backData;

    // 선택지 객체들
    public SubChoice leftChoice;
    public SubChoice rightChoice;
}

public class SubChoice
{
    public int answerID;
    public string choiceText;
    public int nextEventID;
    public ChoiceOutcome outcome;
}
