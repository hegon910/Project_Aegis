// StoryNode.cs (�� ���Ϸ� ����ų� MainScenarioManager���� �и�)
using UnityEngine;
using System.Collections.Generic;

// [System.Serializable]�� ScriptableObject�� �Բ� �� �� �ʿ� �����ϴ�.
// [CreateAssetMenu]�� ����ϸ� Unity �����Ϳ��� ���� �� ������ ������ ������ �� �ֽ��ϴ�.
[CreateAssetMenu(fileName = "New StoryNode", menuName = "Scenario/Story Node")]
public class StoryNode : ScriptableObject
{
    [Header("��� ����")]
    public int nodeId; // ���� ���� �̸����ε� ������ ����������, ����� ID�� ���� �� �ֽ��ϴ�.

    [Header("ǥ�õ� ����")]
    public string characterName;
    public Sprite characterSprite;
    [TextArea(3, 10)]
    public string storyText;

    [Header("������")]
    public List<Choice> choices;
}

// Choice Ŭ������ ScriptableObject�� �ƴϹǷ� [System.Serializable]�� �ٿ���
// Unity �ν����� â�� ���������� ǥ�õ˴ϴ�.
[System.Serializable]
public class Choice
{
    public string choiceText;
    // [����] nextNodeId ���, ���� StoryNode ������ ���� �����մϴ�.
    public StoryNode nextNode;
    public List<ParameterChange> parameterChanges = new List<ParameterChange>();
}