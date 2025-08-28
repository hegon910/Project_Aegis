using UnityEngine;
using Cysharp.Threading.Tasks; // UniTask 사용을 위해 추가

public class TestData : MonoBehaviour
{
    private void Start()
    {
        InitializeGameData();
    }

    private async UniTaskVoid InitializeGameData()
    {
        // DataManager의 싱글톤 인스턴스를 가져옵니다.
        if (DataManager.Instance == null)
        {
            Debug.LogError("DataManager 인스턴스를 찾을 수 없습니다. 씬에 DataManager 오브젝트가 있는지 확인해주세요.");
            return;
        }

        Debug.Log("서브 이벤트 데이터 로딩 시작...");

        try
        {
            // SubIntializeDataAsync()를 호출하여 데이터 로딩을 시작합니다.
            // await 키워드를 사용하여 데이터 로드가 완료될 때까지 기다립니다.
            await DataManager.Instance.SubIntializeDataAsync();

            Debug.Log("모든 서브 이벤트 데이터 로딩이 완료되었습니다!");
            // 데이터 로드 완료 후 다음 씬으로 이동하거나 UI를 활성화하는 등의 로직을 여기에 추가할 수 있습니다.
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"게임 데이터 초기화 중 오류 발생: {ex.Message}");
        }
    }
}