using UnityEngine;
using JUTPS.CameraSystems;

public class CameraManager : MonoBehaviour
{
    public static TPSCameraController MainCam;

    void Awake()
    {
        MainCam = GetComponent<TPSCameraController>();
    }
}