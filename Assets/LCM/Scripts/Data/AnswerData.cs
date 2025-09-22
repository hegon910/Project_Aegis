using System;

[System.Serializable]
public class AnswerData
{
    public int AnswerID { get; set; }
    public string Text_KR { get; set; }
    public string Text_EN { get; set; }
    public int NextTextID { get; set; }
    public string AnswerReward { get; set; }
    public string Font_Direction { get; set; }
    public string Ending_Memoriar { get; set; }
}