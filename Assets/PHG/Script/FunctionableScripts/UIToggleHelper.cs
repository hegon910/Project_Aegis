using UnityEngine;

public class UIToggleHelper : MonoBehaviour
{
    // 이 함수를 버튼의 OnClick 이벤트에 연결하여 사용합니다.
    public void ToggleActive(GameObject targetObject)
    {
        if (targetObject != null)
        {
            // 현재 활성화 상태의 반대 값을 넣어줍니다.
            // targetObject.activeSelf가 true이면 false를, false이면 true를 대입합니다.
            targetObject.SetActive(!targetObject.activeSelf);
        }
    }
}