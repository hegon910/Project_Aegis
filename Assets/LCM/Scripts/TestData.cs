using UnityEngine;
using Cysharp.Threading.Tasks; // UniTask 사용을 위해 추가

public class TestData : MonoBehaviour
{
    private async void Start()
    {
        await DataManager.Instance.InitializeDataAsync();
        //DataManager가 준비될 때까지 기다립니다.
        await DataManager.Instance.IsReady;

        //이제 안전하게 데이터를 사용할 수 있습니다.
        DataManager.Instance.LogFullEventData(1000002);
    }
}