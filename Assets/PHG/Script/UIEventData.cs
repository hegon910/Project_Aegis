using UnityEngine;
using System.Collections.Generic;

// [System.Serializable] �̳� ParameterChange, UIOutcome ���� �ߺ� ���Ǹ� ��� �����߽��ϴ�.
// ��� EventData.cs �� �ִ� ���� EventChoice, ChoiceOutcome Ŭ������ ���� ����մϴ�.

[CreateAssetMenu(fileName = "New UI Event", menuName = "UI Event Data")]
public class UIEventData : ScriptableObject
{
    [Header("�̺�Ʈ ����")]
    public string characterName;
    public Sprite characterSprite; // ���� EventData ���� ȣȯ�� ���� ����
    [TextArea(3, 10)]
    public string dialogue;

    [Header("������ (ī�� ����������)")]
    // EventData.cs �� ���ǵ� EventChoice Ŭ������ �����մϴ�.
    // ��, UI ������ ���� choiceText �ʵ�� �������� �� ���� �ؽ�Ʈ�� ����մϴ�.
    public EventChoice leftChoice;
    public EventChoice rightChoice;
}