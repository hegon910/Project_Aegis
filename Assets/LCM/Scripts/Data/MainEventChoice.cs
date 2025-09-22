using System;

[System.Serializable]
public class MainEventChoice
{
    public string choiceText;
    public int nextEventID;
    public ChoiceOutcome outcome;
    public bool isEndingMemoriar;
}