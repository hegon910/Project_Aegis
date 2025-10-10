using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class OpeningEventData 
{
    public int ID { get; set; }
    public int StoryNum { get; set; }
    public int BG_ID { get; set; }
    public int SFX_ID { get; set; }
    public int OpeningCutScene {  get; set; }
    public string Text_kr {  get; set; }
    public string Text_en { get; set; }
    public int Font_Direction { get; set; }
}
[System.Serializable]
public class OpeningCutSceneData 
{ 
    public int OpeningCutScene_ID { get; set; }
    public string IMGName { get; set; }
}

[System.Serializable]
public class FullOpeningEventData
{
    public int ID;
    public int StoryNum;
    public string Text_kr;

    public BGData bgData;
    public SFXData sfxData;
    public OpeningCutSceneData cutSceneData;
}
