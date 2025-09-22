using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BGData
{
    public int BG_ID { get; set; }
    public string BGName { get; set; }
}
[System.Serializable]
public class SFXData
{
    public int SFX_ID { get; set; }
    public string SFXName { get; set; }
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
    public int CharacterImg_ID { get; set; }
    public string IMGName { get; set; }
}
[System.Serializable]
public class BackData
{
    public int Back_ID { get; set; }
    public string IMGName { get; set; }
}