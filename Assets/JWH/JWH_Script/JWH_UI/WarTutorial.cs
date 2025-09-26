using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WarTutorial : MonoBehaviour
{
    public void OnCloseButtonClicked()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.HideWarTutorial();
        }
        else
        {
            Debug.LogWarning("GameManager 인스턴스를 찾을 수 없어");
            gameObject.SetActive(false);
        }
    }
}
