using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ReplayUIHandler : MonoBehaviour
{
    // 스크롤뷰의 Content 트랜스폼
    public Transform recordContent;

    // 동적으로 생성할 버튼 프리팹
    public GameObject recordButtonPrefab;

    private void OnEnable()
    {
        // 기록 패널이 활성화될 때마다 기록 목록을 새로고침
        RefreshRecordList();
    }

    public void RefreshRecordList()
    {
        // 기존에 생성된 버튼이 있다면 모두 삭제
        foreach (Transform child in recordContent)
        {
            Destroy(child.gameObject);
        }

        // SimpleEventHistoryManager에서 기록 데이터 가져오기
        List<SimpleEventRecord> records = SimpleEventHistoryManager.Instance.GetEventHistory();

        // 최신 기록이 위로 오도록 역순으로 순회
        for (int i = records.Count - 1; i >= 0; i--)
        {
            SimpleEventRecord record = records[i];

            // 프리팹을 Content의 자식으로 생성
            GameObject newButton = Instantiate(recordButtonPrefab, recordContent);

            // 버튼의 텍스트 설정 (예: 날짜와 시간)
            string buttonText = $"{record.playDate} / {record.playDuration}";
            newButton.GetComponentInChildren<Text>().text = buttonText;

            // 버튼 클릭 시 상세 기록을 보여주는 로직을 연결할 수 있습니다.
        }
    }
}