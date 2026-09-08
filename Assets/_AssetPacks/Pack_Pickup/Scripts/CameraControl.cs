using UnityEngine;

// Obsolete pack script - Replaced by FPSPlayerController
public class CameraControl : MonoBehaviour
{
    private void Awake()
    {
        // Automatically self-disable so FPSPlayerController camera takes full control
        enabled = false;
    }
}
