using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CharacterData
{
    public string Chr_name { get; set; }
    public int Chr_index { get; set; }
}

[System.Serializable]
public class RewardTypeData
{
    public string RewardType { get; set; }
    public int RewardType_index { get; set; }
}

[System.Serializable]
public class EventDataList
{
    public int Appearance_Num { get; set; }
    public string Appearance_Type { get; set; }
    public int PageType_Num { get; set; }
    public string PageType { get; set; }
    public int Parameter_Num { get; set; }
    public string Parameter_type { get; set; }
    public int Event_Num { get; set; }
    public string Event_Type { get; set; }
    public int Cho_Num { get; set; }
    public string Cho_txt { get; set; }
}
[System.Serializable]
public class RewardInfo
{
    public string RewardType { get; set; }
    public int RewardValue { get; set; }
}
