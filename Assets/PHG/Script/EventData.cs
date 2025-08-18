using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PHG
{
    /// <summary>
    /// �� ��ũ��Ʈ�� ��� �����ͷμ�, �׽�Ʈ�� �̺�Ʈ �����͸� �����մϴ�.
    /// 
    /// </summary>
    [CreateAssetMenu(fileName = "New Event", menuName = "Game Event")]
    public class EventData : ScriptableObject
    {
        [TextArea(3, 10)]
        public string eventText; // ī�忡 ǥ�õ� ���� �ؽ�Ʈ

        public string leftChoiceText; // ���� ������ �ؽ�Ʈ
        public string rightChoiceText; // ������ ������ �ؽ�Ʈ

        // ���⿡ ���߿� �Ķ���� ��ȭ�� ���� �������� �߰��� �� �ֽ��ϴ�.
    }
}
