using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ReplayUIHandler : MonoBehaviour
{
    // 스크롤뷰의 Content 트랜스폼
    public Transform recordContent;

    // 동적으로 생성할 버튼 프리팹
    public GameObject recordButtonPrefab;

    // 상세 기록 패널을 관리하는 스크립트
    public ReplayDetailHandler detailHandler;

    private void OnEnable()
    {
        RefreshRecordList();
    }

    public void RefreshRecordList()
    {
        if (SimpleEventHistoryManager.Instance == null)
        {
            Debug.LogWarning("[ReplayUIHandler] SimpleEventHistoryManager 인스턴스를 찾을 수 없습니다.");
            return;
        }

        // 기존에 생성된 버튼이 있다면 모두 삭제
        foreach (Transform child in recordContent)
        {
            Destroy(child.gameObject);
        }

        // SimpleEventHistoryManager에서 기록 데이터 가져오기
        List<GamePlaythroughRecord> records = SimpleEventHistoryManager.Instance.GetPlaythroughHistory();

    }
}