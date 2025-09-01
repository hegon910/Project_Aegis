using UnityEngine;
using Cysharp.Threading.Tasks; // UniTask 사용을 위해 추가

public class TestData : MonoBehaviour
{
    private async void Start()
    {
        await DataManager.Instance.InitializeDataAsync();
        //DataManager가 준비될 때까지 기다립니다.
        await DataManager.Instance.IsReady;

    }
}