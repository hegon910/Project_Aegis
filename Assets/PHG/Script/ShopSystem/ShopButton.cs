using UnityEngine;
using UnityEngine.UI;
public class ShopButton : MonoBehaviour
{
    [SerializeField] private ShopUI shopUI;
    
    void Start()
    {
        // 버튼 클릭 이벤트 연결
        GetComponent<Button>().onClick.AddListener(OpenShop);
    }
    
    public void OpenShop()
    {
        if (shopUI != null)
        {
            shopUI.OpenShop();
        }
    }
}