using UnityEngine;
using Cysharp.Threading.Tasks; // UniTask 사용을 위해 추가

public class TestData : MonoBehaviour
{
    private async UniTaskVoid Start()
    {
        if (DataManager.Instance == null)
        {
            Debug.LogError("DataManager 인스턴스가 씬에 없습니다!");
            return;
        }

        Debug.Log("데이터 로드 시작을 요청합니다.");

        try
        {
            await DataManager.Instance.InitializeDataAsync();

            Debug.Log("✅ DataManager 초기화 (데이터 로드) 완료!");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"데이터 초기화 중 오류 발생: {ex.Message}");
        }
    }
}