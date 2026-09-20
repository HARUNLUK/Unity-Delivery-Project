using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Controls the cinematic overview camera overlooking the player's shop/branch during Main Menu.
/// Directly operates on the active gameplay camera (Camera.main / playerCamera) so that 100% of the
/// game's lighting, shadows, URP post-processing, shaders, and visual fidelity are perfectly preserved.
/// </summary>
public class MainMenuCameraController : MonoBehaviour
{
    private static MainMenuCameraController _instance;
    public static MainMenuCameraController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = UnityEngine.Object.FindAnyObjectByType<MainMenuCameraController>(FindObjectsInactive.Include);
                if (_instance == null)
                {
                    GameObject camHolder = new GameObject("MainMenu_Camera_Controller");
                    _instance = camHolder.AddComponent<MainMenuCameraController>();
                }
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("--- TARGET & ANCHORS ---")]
    [Tooltip("Target shop/branch transform to focus on (Auto-found via BranchManager if empty)")]
    public Transform shopTarget;

    [Tooltip("Camera offset from shop target (if procedural viewpoint not used)")]
    public Vector3 cameraOffset = new Vector3(0f, 2.2f, -10f);

    [Tooltip("Look-at target height offset")]
    public float lookAtHeight = 2.0f;

    [Header("--- CINEMATIC AMBIENT MOTION ---")]
    [Tooltip("Enable subtle orbital/swaying camera movement")]
    public bool enableCinematicMotion = true;

    [Tooltip("Horizontal sway amplitude")]
    public float swayAmplitudeX = 0.8f;

    [Tooltip("Vertical breath amplitude")]
    public float swayAmplitudeY = 0.25f;

    [Tooltip("Sway speed multiplier")]
    public float swaySpeed = 0.35f;

    private Camera targetCamera;
    private Transform originalParent;
    private Vector3 originalLocalPos = Vector3.zero;
    private Quaternion originalLocalRot = Quaternion.identity;
    private bool isMenuMode = false;

    public bool IsMenuMode => isMenuMode;
    public Camera ActiveCamera => targetCamera;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    private void Start()
    {
        EnsureShopTarget();
        EnsureTargetCamera();
    }

    public void EnsureTargetCamera()
    {
        if (targetCamera != null) return;

        if (FPSPlayerController.Instance != null && FPSPlayerController.Instance.playerCamera != null)
        {
            targetCamera = FPSPlayerController.Instance.playerCamera;
        }
        else
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                Camera[] cams = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                if (cams != null && cams.Length > 0) targetCamera = cams[0];
            }
        }

        if (targetCamera != null)
        {
            // Ensure URP post processing and shadow fidelity
            var camData = targetCamera.GetComponent<UniversalAdditionalCameraData>();
            if (camData == null)
            {
                camData = targetCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }
            if (camData != null)
            {
                camData.renderShadows = true;
                camData.renderPostProcessing = true;
                camData.requiresDepthTexture = true;
                camData.requiresColorTexture = true;
                camData.volumeLayerMask = ~0;
                camData.volumeTrigger = targetCamera.transform;
            }

            if (originalParent == null && targetCamera.transform.parent != null)
            {
                originalParent = targetCamera.transform.parent;
                originalLocalPos = targetCamera.transform.localPosition;
                originalLocalRot = targetCamera.transform.localRotation;
            }
        }
    }

    public void EnsureShopTarget()
    {
        if (shopTarget != null) return;

        if (BranchManager.Instance != null)
        {
            if (BranchManager.Instance.playerExteriorSpawnPoint != null)
            {
                shopTarget = BranchManager.Instance.playerExteriorSpawnPoint;
                return;
            }
            if (BranchManager.Instance.buildingContainer != null)
            {
                shopTarget = BranchManager.Instance.buildingContainer;
                return;
            }
            shopTarget = BranchManager.Instance.transform;
            return;
        }

        GameObject branchObj = GameObject.Find("Branch_Container") ?? GameObject.Find("BranchManager") ?? GameObject.Find("PostOffice");
        if (branchObj != null)
        {
            shopTarget = branchObj.transform;
        }
    }

    public void SetActive(bool active) => SetMenuMode(active);

    public void SetMenuMode(bool menuActive)
    {
        isMenuMode = menuActive;
        EnsureTargetCamera();
        EnsureShopTarget();

        if (targetCamera == null) return;

        targetCamera.enabled = true;
        targetCamera.gameObject.SetActive(true);

        if (isMenuMode)
        {
            // Detach camera to freely position at the shop overview location
            if (targetCamera.transform.parent != null)
            {
                originalParent = targetCamera.transform.parent;
                originalLocalPos = targetCamera.transform.localPosition;
                originalLocalRot = targetCamera.transform.localRotation;
                targetCamera.transform.SetParent(null);
            }

            UpdateCameraPose(0f);
        }
        else
        {
            // Restore camera directly back to player camera holder
            Transform restoreParent = originalParent;
            if (restoreParent == null && FPSPlayerController.Instance != null)
            {
                restoreParent = FPSPlayerController.Instance.cameraHolder != null ?
                    FPSPlayerController.Instance.cameraHolder : FPSPlayerController.Instance.transform;
            }

            if (restoreParent != null)
            {
                targetCamera.transform.SetParent(restoreParent);
                targetCamera.transform.localPosition = originalLocalPos;
                targetCamera.transform.localRotation = originalLocalRot;
            }
        }
    }

    private void LateUpdate()
    {
        if (!isMenuMode || targetCamera == null) return;

        EnsureShopTarget();
        float time = Time.unscaledTime * swaySpeed;
        UpdateCameraPose(time);
    }

    private void UpdateCameraPose(float time)
    {
        if (targetCamera == null) EnsureTargetCamera();
        if (targetCamera == null) return;

        Vector3 targetBasePos = Vector3.zero;
        Vector3 targetLookPos = Vector3.zero;

        if (BranchManager.Instance != null)
        {
            // Use BranchManager's exterior viewpoint system for 100% lighting & angle match
            var (spawnPos, lookAtPos) = BranchManager.Instance.GetExteriorViewpoint(BranchManager.Instance.CurrentTier);
            targetBasePos = spawnPos + Vector3.up * 1.8f;
            targetLookPos = lookAtPos;
        }
        else if (shopTarget != null)
        {
            targetBasePos = shopTarget.position + shopTarget.TransformDirection(cameraOffset);
            targetLookPos = shopTarget.position + (Vector3.up * lookAtHeight);
        }
        else
        {
            targetBasePos = transform.position;
            targetLookPos = transform.position + transform.forward * 10f;
        }

        if (enableCinematicMotion)
        {
            Vector3 forwardDir = (targetLookPos - targetBasePos).normalized;
            if (forwardDir == Vector3.zero) forwardDir = Vector3.forward;
            Vector3 rightDir = Vector3.Cross(Vector3.up, forwardDir).normalized;

            float offsetX = Mathf.Sin(time) * swayAmplitudeX;
            float offsetY = Mathf.Cos(time * 0.7f) * swayAmplitudeY;

            targetBasePos += (rightDir * offsetX) + (Vector3.up * offsetY);
        }

        targetCamera.transform.position = targetBasePos;
        targetCamera.transform.LookAt(targetLookPos);
    }
}
