using System;

[System.Serializable]
public class MainEventChoice
{
    public int ID;
    public string choiceText;
    public int nextEventID;
    public ChoiceOutcome outcome;
    public bool isEndingMemoriar;
    public bool isCountingforRealEnding2;
    public bool isCountingforRealEnding3;
}