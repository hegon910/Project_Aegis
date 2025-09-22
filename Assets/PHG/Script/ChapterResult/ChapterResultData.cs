// ChapterEndData.cs (수정)
using UnityEngine;

public enum GameOutcome { Victory, Draw, Defeat }

[CreateAssetMenu(fileName = "ChapterEnd_ChapterX", menuName = "Game/Chapter End Data")]
public class ChapterResultData : ScriptableObject
{
    [Header("결과별 배경 이미지")]
    public Sprite victoryImage;
    public Sprite drawImage;
    public Sprite defeatImage;

    [Tooltip("시간이 흐르기 시작하는 날짜")]
    public int startYear;
    public int startMonth;

    [Tooltip("시간 흐름이 멈추는 최종 날짜")]
    public int endYear;
    public int endMonth;

    [Header("결과별 요약 텍스트")]
    [TextArea(3, 5)]
    public string chapterSummary;
    
    [TextArea(3, 5)]
    public string victorySummary;

    [TextArea(3, 5)]
    public string drawSummary;

    [TextArea(3, 5)]
    public string defeatSummary;
}