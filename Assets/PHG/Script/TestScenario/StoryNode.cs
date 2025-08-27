// StoryNode.cs (새 파일로 만들거나 MainScenarioManager에서 분리)
using UnityEngine;
using System.Collections.Generic;

// [System.Serializable]은 ScriptableObject와 함께 쓸 때 필요 없습니다.
// [CreateAssetMenu]를 사용하면 Unity 에디터에서 직접 이 데이터 에셋을 생성할 수 있습니다.
[CreateAssetMenu(fileName = "New StoryNode", menuName = "Scenario/Story Node")]
public class StoryNode : ScriptableObject
{
    [Header("노드 정보")]
    public int nodeId; // 에셋 파일 이름으로도 구분이 가능하지만, 명시적 ID가 편리할 수 있습니다.

    [Header("표시될 내용")]
    public string characterName;
    public Sprite characterSprite;
    [TextArea(3, 10)]
    public string storyText;

    [Header("선택지")]
    public List<Choice> choices;
}

// Choice 클래스는 ScriptableObject가 아니므로 [System.Serializable]을 붙여야
// Unity 인스펙터 창에 정상적으로 표시됩니다.
[System.Serializable]
public class Choice
{
    public string choiceText;
    // [수정] nextNodeId 대신, 다음 StoryNode 에셋을 직접 연결합니다.
    public StoryNode nextNode;
    public List<ParameterChange> parameterChanges = new List<ParameterChange>();
}