using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleCamera : MonoBehaviour
{
    public Camera particleCamera;

    void OnEnable()
    {
        if (particleCamera != null)
        {
            particleCamera.gameObject.SetActive(true);
        }
    }

    void OnDisable()
    {
        if (particleCamera != null)
        {
            particleCamera.gameObject.SetActive(false);
        }
    }
}
