using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ParameterEventData
{
    public int ID { get; set; }
    public int RoundType { get; set; }
    public int PageType { get; set; }
    public int ConditionType { get; set; }
    public int ChangeCondition { get; set; }
    public int IsConditionSuccess { get; set; }
    public int EventQuestion { get; set; }
    public int LeftString { get; set; }
    public int NeedType1 { get; set; }
    public int NeedValue1 { get; set; }
    public int AcceptReward1 { get; set; }
    public int DenyReward1 { get; set; }
    public int AcceptString1 { get; set; }
    public int DenyString1 { get; set; }
    public int RightString { get; set; }
    public int NeedType2 { get; set; }
    public int NeedValue2 { get; set; }
    public int AcceptReward2 { get; set; }
    public int DenyReward2 { get; set; }
    public int AcceptString2 { get; set; }
    public int DenyString2 { get; set; }
    public int AnotherEventQuestion { get; set; }
    public int AnotherLeftString { get; set; }
    public int AnotherNeedType1 { get; set; }
    public int AnotehrNeedValue1 { get; set; }
    public int AnotherAcceptReward1 { get; set; }
    public int AnotherDenyReward1 { get; set; }
    public int AnotherAcceptString1 { get; set; }
    public int AnotherDenyString1 { get; set; }
    public int AnotherRightString { get; set; }
    public int AnotherNeedType2 { get; set; }
    public int AnotherNeedValue2 { get; set; }
    public int AnotherAcceptReward2 { get; set; }
    public int AnotherDenyReward2 { get; set; }
    public int AnotherAcceptString2 { get; set; }
    public int AnotherDenyString2 { get; set; }
}
[System.Serializable]
public class ParameterRewardData
{
    public int ID { get; set; }
    public int RewardType1 { get; set; }
    public int RewardValue1 { get; set; }
    public int RewardType2 { get; set; }
    public int RewardValue2 { get; set; }
    public int RewardType3 { get; set; }
    public int RewardValue3 { get; set; }
    public int RewardType4 { get; set; }
    public int RewardValue4 { get; set; }
    public int RewardType5 { get; set; }
    public int RewardValue5 { get; set; }
}
[System.Serializable]
public class ParameterEventStringData
{
    public int ID { get; set; }
    public string BG { get; set; }
    public string SoundEffect { get; set; }
    public int CharacterName { get; set; }
    public string CharacterImage { get; set; }
    public int IsFinishString { get; set; }
    public string String_kr { get; set; }
}

