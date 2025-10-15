using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SyncCanvasPosition : MonoBehaviour
{
    // 기준이 될 UI 요소의 RectTransform (Canvas)
    public RectTransform targetUI;

    // 위치를 동기화할 VFX RectTransform (VfxCanvas)
    public RectTransform vfxRectTransform;

    // 기본캔버스
    public Canvas mainUICanvas;
   
    //파티클카메라
    public Camera vfxCamera;

    void Update()
    {
        if (targetUI == null || vfxRectTransform == null || mainUICanvas == null)
        {
            return;
        }

        SyncPosition();
    }

    void SyncPosition()
    {
        
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(mainUICanvas.worldCamera, targetUI.position);

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            vfxRectTransform.parent as RectTransform, // RectTransform (VfxCanvas)
            screenPoint,
            vfxCamera, // VfxCanvas의 렌더 카메라
            out localPoint
        );

        vfxRectTransform.anchoredPosition = localPoint;
    }
}
