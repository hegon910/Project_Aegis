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
        DataManager.Instance.GetEndingData(80001);
        Debug.Log("--- 모든 이벤트 데이터 로드 완료 ---");
    }
}