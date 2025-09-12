using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BGData
{
    public long BG_ID { get; set; }
    public string BG_path { get; set; }
}

[System.Serializable]
public class SFXData
{
    public long SFX_ID { get; set; }
    public string SFX_path { get; set; }
}


[System.Serializable]
public class MainCharacterData
{
    public int Chr_ID { get; set; }
    public string Chr_Name { get; set; }
}



[System.Serializable]
public class MainCharacterImgData
{
    public long CharacterImg_ID { get; set; }
    public string CharacterImg_path { get; set; }
}