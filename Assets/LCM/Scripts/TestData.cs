using UnityEngine;
using Cysharp.Threading.Tasks; // UniTask 사용을 위해 추가

public class TestData : MonoBehaviour
{
    private async void Start()
    {
        await DataManager.Instance.InitializeDataAsync();
        //DataManager가 준비될 때까지 기다립니다.
        await DataManager.Instance.IsReady;

        Debug.Log("--- 모든 이벤트 데이터 로드 시작 ---");

        // eventDataDict의 모든 키(ID)를 순회합니다.
        //foreach (var eventID in DataManager.Instance.eventDataDict.Keys)
        //{
        //    // 각 이벤트 ID에 해당하는 데이터를 불러옵니다.
        //    // GetEventDataById 함수 내부에 이미 로그 출력 로직이 있다고 가정합니다.
        //    DataManager.Instance.GetEventDataById(eventID);
        //}

        DataManager.Instance.GetMainEventDataById(10001);
        Debug.Log("--- 모든 이벤트 데이터 로드 완료 ---");

    }
}