using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PHG
{
    /// <summary>
    /// 본 스크립트는 목업 데이터로서, 테스트용 이벤트 데이터를 포함합니다.
    /// 
    /// </summary>
    [CreateAssetMenu(fileName = "New Event", menuName = "Game Event")]
    public class EventData : ScriptableObject
    {
        [TextArea(3, 10)]
        public string eventText; // 카드에 표시될 메인 텍스트

        public string leftChoiceText; // 왼쪽 선택지 텍스트
        public string rightChoiceText; // 오른쪽 선택지 텍스트

        // 여기에 나중에 파라미터 변화량 같은 변수들을 추가할 수 있습니다.
    }
}
