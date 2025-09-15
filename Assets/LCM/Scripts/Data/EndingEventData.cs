using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EndingEventData
{
    public long ID { get; set; }
    public string EndingString { get; set; }
    public int Karma_Rate { get; set; }
    public long EndingCutScene_ID { get; set; }
    public int BG_ID { get; set; }
    public int SFX_ID { get; set; }
    public string Text_Kr { get; set; }
    public string Text_En { get; set; }
    public string Direction { get; set; }
    public string Fade_Out_Color { get; set; }
}

[System.Serializable]
public class EndingCutScene
{
    public long EndingCutScene_ID { get; set; }
    public string IMGName { get; set; }
}

[System.Serializable]
public class FullEndingData
{
    public long ID { get; set; }
    public string EndingString { get; set; }
    public int Karma_Rate { get; set; }

    public EndingCutScene cutSceneData;
    public BGData bgData;
    public SFXData sfxData;

    public string Text_Kr { get; set; }
    public string Text_En { get; set; }
    public string Direction { get; set; }
    public string Fade_Out_Color { get; set; }
}