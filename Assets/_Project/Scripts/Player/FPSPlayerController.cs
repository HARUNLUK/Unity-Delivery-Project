using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public enum VehicleCameraMode { FirstPerson, ThirdPerson }

[RequireComponent(typeof(CharacterController))]
public class FPSPlayerController : MonoBehaviour
{
    public static FPSPlayerController Instance { get; private set; }
    [Header("--- MOVEMENT SETTINGS ---")]
    public float walkSpeed = 4.5f;
    public float sprintSpeed = 8.0f;
    public float jumpHeight = 1.2f;
    public float gravity = -20f;

    [Header("--- FOOTSTEP TIMING SETTINGS ---")]
    [Tooltip("Yürüme adımları arasındaki süre (saniye - varsayılan 0.50)")]
    public float walkStepInterval = 0.50f;
    [Tooltip("Koşma adımları arasındaki süre (saniye - varsayılan 0.32)")]
    public float sprintStepInterval = 0.32f;

    [Header("--- MOUSE LOOK SETTINGS ---")]
    public float mouseSensitivity = 2.0f;
    public float minPitch = -80f;
    public float maxPitch = 80f;

    [Header("--- INTERACTION SETTINGS ---")]
    public float interactionDistance = 3.5f;
    public LayerMask interactionLayers = ~0;

    [Header("--- REFERENCES ---")]
    public Camera playerCamera;
    public Transform cameraHolder;
    public PhysicsGrabber grabber;

    private CharacterController controller;
    public CharacterController Controller => controller;
    public bool IsOnFoot => isOnFoot;
    public VehicleCameraMode CurrentVehicleCameraMode => vehicleCameraMode;
    private Vector3 velocity;
    private float pitch = 0f;
    private bool isOnFoot = true;

    private Transform originalCameraParent;
    private Vector3 originalCameraLocalPos;
    private Quaternion originalCameraLocalRot;

    [Header("--- IN-VEHICLE LOOK SETTINGS ---")]
    public VehicleCameraMode vehicleCameraMode = VehicleCameraMode.ThirdPerson;
    public float inVehicleMouseSensitivity = 2.0f;
    public float inVehicleMaxYaw = 110f;    // Sağa ve sola bakış limiti (aynalar/camlar)
    public float inVehicleMinPitch = -50f;  // Yukarı bakış limiti (dikiz aynası/tavan)
    public float inVehicleMaxPitch = 55f;   // Aşağı bakış limiti (direksiyon/göstergeler)

    [Header("--- TPS VEHICLE CHASE CAMERA SETTINGS ---")]
    public float tpsDistance = 6.0f;
    public float tpsHeight = 2.2f;
    public float tpsLookAtHeight = 1.1f;
    public float tpsRotationDamping = 6.0f;
    public float tpsHeightDamping = 5.0f;

    [Header("--- CARGO THROW SETTINGS ---")]
    [Tooltip("Maximum throw velocity")]
    public float maxThrowForce = 11.0f;
    [Tooltip("Hold duration in seconds for maximum throw power")]
    public float throwChargeDuration = 0.85f;

    [Header("--- ECONOMY / CASH OVERRIDE (OYUNCU PARASI) ---")]
    [Tooltip("Directly view or set the player's cash balance from Inspector in Editor/Runtime")]
    public int playerCash = 0;

    [Tooltip("If true, the player's saved cash in PlayerPrefs will be overwritten with playerCash on Start")]
    public bool overrideStartingCash = false;

    private int lastTrackedCash = -1;

    private float vehicleYaw = 0f;
    private float vehiclePitch = 0f;
    private float tpsYawOffset = 0f;
    private float tpsPitchOffset = 0f;
    public Transform currentSeatPoint;
    public DrivableVehicle currentVehicle;
    public Transform currentVehicleTransform;

    private float currentDropHoldTime = 0f;
    private float afterGrabSafetyTimer = 0f;
    public float exitVehicleSafetyTimer = 0f;
    private float footstepTimer = 0f;
    private bool wasGroundedLastFrame = true;
    private float previousAirborneVelocityY = 0f;
    private PhysicalCargoPackage currentlyFocusedPackage = null;
    private static readonly RaycastHit[] interactionHitsBuffer = new RaycastHit[32];
    private static readonly List<RaycastHit> validHitsCache = new List<RaycastHit>(32);
    private static readonly System.Comparison<RaycastHit> hitDistanceComparison = (a, b) => a.distance.CompareTo(b.distance);

    private void UpdateCargoFocus(PhysicalCargoPackage newTarget)
    {
        if (currentlyFocusedPackage != newTarget)
        {
            if (currentlyFocusedPackage != null)
            {
                currentlyFocusedPackage.SetFocused(false);
            }
            currentlyFocusedPackage = newTarget;
            if (currentlyFocusedPackage != null)
            {
                currentlyFocusedPackage.SetFocused(true);
            }
        }
    }

    private void Awake()
    {
        Instance = this;
        controller = GetComponent<CharacterController>();
        EnsureComponents();
    }

    private void EnsureComponents()
    {
        if (cameraHolder == null)
        {
            Transform existingHolder = transform.Find("CameraHolder");
            if (existingHolder != null)
            {
                cameraHolder = existingHolder;
            }
            else
            {
                GameObject holderObj = new GameObject("CameraHolder");
                holderObj.transform.SetParent(transform);
                holderObj.transform.localPosition = new Vector3(0f, 1.7f, 0f);
                cameraHolder = holderObj.transform;
            }
        }

        if (playerCamera == null)
        {
            playerCamera = cameraHolder.GetComponentInChildren<Camera>();
            if (playerCamera == null)
            {
                // Take over Main Camera or create dedicated FPS camera
                Camera mainCam = Camera.main;
                if (mainCam != null && !mainCam.transform.IsChildOf(transform))
                {
                    playerCamera = mainCam;
                    playerCamera.transform.SetParent(cameraHolder);
                    playerCamera.transform.localPosition = Vector3.zero;
                    playerCamera.transform.localRotation = Quaternion.identity;
                }
                else
                {
                    GameObject camObj = new GameObject("FPSCamera");
                    camObj.transform.SetParent(cameraHolder);
                    camObj.transform.localPosition = Vector3.zero;
                    camObj.transform.localRotation = Quaternion.identity;
                    playerCamera = camObj.AddComponent<Camera>();
                    camObj.AddComponent<AudioListener>();
                }
            }
        }

        if (playerCamera != null)
        {
            SmoothFollowCamera sfc = playerCamera.GetComponent<SmoothFollowCamera>();
            if (sfc != null) Destroy(sfc);

            playerCamera.tag = "MainCamera";
            playerCamera.enabled = true;
            if (playerCamera.GetComponent<AudioListener>() == null)
            {
                playerCamera.gameObject.AddComponent<AudioListener>();
            }
        }

        if (grabber == null && playerCamera != null)
        {
            grabber = playerCamera.GetComponent<PhysicsGrabber>();
            if (grabber == null) grabber = playerCamera.gameObject.AddComponent<PhysicsGrabber>();
        }

        if (playerCamera != null)
        {
            originalCameraParent = playerCamera.transform.parent;
            originalCameraLocalPos = playerCamera.transform.localPosition;
            originalCameraLocalRot = playerCamera.transform.localRotation;
        }
    }

    private void Start()
    {
        bool inMainMenu = GameMenuManager.Instance != null && GameMenuManager.Instance.startInMainMenu && GameMenuManager.Instance.CurrentState == GameFlowState.MainMenu;

        // Deactivate any duplicate non-player cameras
        Camera[] allCameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var cam in allCameras)
        {
            if (cam != playerCamera && !cam.transform.IsChildOf(transform))
            {
                cam.gameObject.SetActive(false);
            }
        }

        if (inMainMenu)
        {
            SetOnFootActive(false);
            if (playerCamera != null) playerCamera.enabled = true;
            if (MainMenuCameraController.Instance != null)
            {
                MainMenuCameraController.Instance.SetMenuMode(true);
            }
            LockCursor(false);
        }
        else
        {
            SetOnFootActive(true);
            if (playerCamera != null) playerCamera.enabled = true;
            LockCursor(true);
        }

        if (SettingsManager.Instance != null)
        {
            mouseSensitivity = SettingsManager.Instance.mouseSensitivity;
            inVehicleMouseSensitivity = SettingsManager.Instance.mouseSensitivity;
        }

        if (overrideStartingCash && PlayerEconomyManager.Instance != null)
        {
            PlayerEconomyManager.Instance.SetBalance(playerCash);
            lastTrackedCash = playerCash;
        }
        else if (PlayerEconomyManager.Instance != null)
        {
            playerCash = PlayerEconomyManager.Instance.CurrentLiveBalance;
            lastTrackedCash = playerCash;
        }
    }

    [ContextMenu("Apply Inspector Cash To Player Economy")]
    public void ApplyInspectorCash()
    {
        if (PlayerEconomyManager.Instance != null)
        {
            PlayerEconomyManager.Instance.SetBalance(playerCash);
            lastTrackedCash = playerCash;
            Debug.Log($"<color=#32FF64>[FPSPlayerController] Player cash updated to ${playerCash}.</color>");
        }
    }

    public static bool IsAnyUIOpen()
    {
        // 0. Main Menu, Pause Menu, Settings, or Transition
        if (GameMenuManager.Instance != null && GameMenuManager.Instance.IsMenuOrPauseOpen)
            return true;

        // 1. Tablet UI
        if (CargoTabletUI.Instance != null && CargoTabletUI.Instance.IsTabletOpen)
            return true;

        // 2. Day Summary Screen
        if (DaySummaryManager.Instance != null && DaySummaryManager.Instance.IsSummaryOpen)
            return true;

        // 3. Shift Ended (Time 18:00 or Hospital Detonation)
        if (DayTimeManager.Instance != null && DayTimeManager.Instance.IsShiftEnded)
            return true;

        // 4. Commercial Hub (Garage, Insurance, Dispatch, Property modal)
        if (CommercialHubUIManager.Instance != null && CommercialHubUIManager.Instance.IsAnyPanelOpen)
            return true;

        // 5. Delivery Selection Zone Dropdown
        if (DeliverySelectionUI.Instance != null && DeliverySelectionUI.Instance.IsOpen)
            return true;

        // 6. Branch Upgrade Cinematic Transition
        if (BranchUpgradeTransitionUI.IsTransitioning)
            return true;

        // 7. Delivery Tutorial Guide (old full-screen guide, kept for compatibility)
        if (DeliveryTutorialUI.Instance != null && DeliveryTutorialUI.Instance.IsOpen)
            return true;

        // 7b. Tutorial system: F1 guide list or a replayed tip window
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsBlockingOpen)
            return true;

        // 8. Game Over Screen
        if (GameOverManager.Instance != null && GameOverManager.Instance.IsGameOver)
            return true;

        // 9. Direct scene hierarchy fallback for DaySummaryPanel
        GameObject dsp = GameObject.Find("DaySummaryPanel");
        if (dsp != null && dsp.activeInHierarchy)
            return true;

        return false;
    }

    public bool IsUIBlockingInput()
    {
        return IsAnyUIOpen();
    }

    private void Update()
    {
        // Live sync Inspector playerCash <-> PlayerEconomyManager
        if (PlayerEconomyManager.Instance != null)
        {
            if (playerCash != lastTrackedCash)
            {
                PlayerEconomyManager.Instance.SetBalance(playerCash);
                lastTrackedCash = playerCash;
            }
            else
            {
                playerCash = PlayerEconomyManager.Instance.CurrentLiveBalance;
                lastTrackedCash = playerCash;
            }
        }

        if (exitVehicleSafetyTimer > 0f)
        {
            exitVehicleSafetyTimer -= Time.deltaTime;
        }

        bool isUIOpen = IsUIBlockingInput();

        if (isUIOpen && InteractionPromptHUD.Instance != null)
        {
            InteractionPromptHUD.Instance.HidePrompt();
        }

        // Enforce unlocked cursor if any UI is open
        if (isUIOpen)
        {
            if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
        else
        {
            // Auto re-lock cursor if clicking in game world without open UI and pointer not over UI
            if (Cursor.lockState != CursorLockMode.Locked || Cursor.visible)
            {
                bool isPointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                if (!isPointerOverUI)
                {
                    if ((Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
                        (Keyboard.current != null && (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)))
                    {
                        LockCursor(true);
                    }
                }
            }
        }

        // F9 Dev Reset for Vehicles, Shops / Properties & Branch Progression (Works on foot AND inside vehicles)
        if (CheckF9DevInput())
        {
            DrivableVehicle.ResetAllVehiclesInGame();
            PurchasableProperty.ResetAllPropertiesInGame();
            if (BranchManager.Instance != null)
            {
                BranchManager.Instance.ResetBranchProgression();
            }
            if (InteractionPromptHUD.Instance != null)
            {
                InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.Get("prompt_dev_reset"), 3.5f);
            }
            if (CargoTabletUI.Instance != null && CargoTabletUI.Instance.IsTabletOpen)
            {
                CargoTabletUI.Instance.PopulateVehicleList();
            }
        }

        if (!isOnFoot)
        {
            if (!isUIOpen && KeyBindingManager.WasPressedThisFrame(GameAction.Camera))
            {
                ToggleVehicleCameraMode();
            }

            if (!isUIOpen)
            {
                if (vehicleCameraMode == VehicleCameraMode.FirstPerson)
                {
                    HandleInVehicleLook();
                }
                else
                {
                    HandleTPSOrbitInput();
                }
            }
            return;
        }

        if (!isUIOpen)
        {
            HandleMouseLook();
            HandleInteraction();
        }

        HandleMovement();
    }

    private bool CheckF9DevInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame)
        {
            return true;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        try
        {
            if (Input.GetKeyDown(KeyCode.F9))
            {
                return true;
            }
        }
        catch { }
#endif

        return false;
    }

    private void LateUpdate()
    {
        if (!isOnFoot && vehicleCameraMode == VehicleCameraMode.ThirdPerson && currentVehicleTransform != null)
        {
            UpdateTPSCameraPosition();
        }
        else if (!isOnFoot && vehicleCameraMode == VehicleCameraMode.FirstPerson && currentSeatPoint != null)
        {
            UpdateFPSCamera(false);
        }
    }

    private Quaternion fpsSmoothedBodyRot = Quaternion.identity;
    [Tooltip("In-vehicle FPS camera: how fast the camera follows body pitch/roll (higher = stiffer, lower = smoother)")]
    public float fpsBodyFollowSpeed = 14f;

    private void UpdateFPSCamera(bool snap)
    {
        if (playerCamera == null || currentSeatPoint == null) return;

        Vector3 offset = currentVehicle != null ? currentVehicle.fpsCameraOffset : Vector3.zero;
        Quaternion bodyRot = currentSeatPoint.rotation;

        if (snap) fpsSmoothedBodyRot = bodyRot;
        else fpsSmoothedBodyRot = Quaternion.Slerp(fpsSmoothedBodyRot, bodyRot, 1f - Mathf.Exp(-fpsBodyFollowSpeed * Time.deltaTime));

        playerCamera.transform.SetPositionAndRotation(
            currentSeatPoint.TransformPoint(offset),
            fpsSmoothedBodyRot * Quaternion.Euler(vehiclePitch, vehicleYaw, 0f));
    }

    private const string VehicleCameraModePrefKey = "Vehicle_LastCameraMode";

    public void ToggleVehicleCameraMode()
    {
        ToggleVehicleCameraModeInternal();
        PlayerPrefs.SetInt(VehicleCameraModePrefKey, (int)vehicleCameraMode);
    }

    private void ToggleVehicleCameraModeInternal()
    {
        if (vehicleCameraMode == VehicleCameraMode.FirstPerson)
        {
            vehicleCameraMode = VehicleCameraMode.ThirdPerson;
            if (playerCamera != null) playerCamera.transform.SetParent(null);
            tpsYawOffset = 0f;
            tpsPitchOffset = 0f;
        }
        else
        {
            vehicleCameraMode = VehicleCameraMode.FirstPerson;
            if (currentSeatPoint != null && playerCamera != null)
            {
                // Camera is NOT parented: LateUpdate follows the seat with a smoothed rotation (see UpdateFPSCamera)
                playerCamera.transform.SetParent(null);
                fpsSmoothedBodyRot = currentSeatPoint.rotation;
                vehicleYaw = 0f;
                vehiclePitch = 0f;
                UpdateFPSCamera(true);
            }
        }

        if (currentVehicle != null)
        {
            currentVehicle.UpdateDriverVisibility(vehicleCameraMode);
        }
    }

    private void HandleInVehicleLook()
    {
        if (playerCamera == null) return;
        if (IsUIBlockingInput()) return;
        if (Cursor.lockState != CursorLockMode.Locked || Cursor.visible) return;

        float mouseX = 0f;
        float mouseY = 0f;

        if (Mouse.current != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue() * (inVehicleMouseSensitivity * 0.08f);
            mouseX = delta.x;
            mouseY = delta.y;
        }

        // 360 Derece Serbest Yatay Bakış
        vehicleYaw += mouseX;
        if (vehicleYaw > 360f || vehicleYaw < -360f)
        {
            vehicleYaw %= 360f;
        }

        // Sadece Düşey (Yukarı / Aşağı) Bakışta Açı Sınırı
        vehiclePitch -= mouseY;
        vehiclePitch = Mathf.Clamp(vehiclePitch, inVehicleMinPitch, inVehicleMaxPitch);

        // Rotation is applied in LateUpdate (UpdateFPSCamera)
    }

    private void HandleTPSOrbitInput()
    {
        if (IsUIBlockingInput()) return;
        if (Cursor.lockState != CursorLockMode.Locked || Cursor.visible) return;

        float mouseX = 0f;
        float mouseY = 0f;

        if (Mouse.current != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue() * (inVehicleMouseSensitivity * 0.08f);
            mouseX = delta.x;
            mouseY = delta.y;
        }

        // 360 Derece Serbest Yatay Bakış (TPS Orbit Yaw)
        tpsYawOffset += mouseX;
        if (tpsYawOffset > 360f || tpsYawOffset < -360f)
        {
            tpsYawOffset %= 360f;
        }

        // Düşey Bakış Açı Değişimi (TPS Orbit Pitch - Küresel Pivot)
        tpsPitchOffset -= mouseY;
        tpsPitchOffset = Mathf.Clamp(tpsPitchOffset, -40f, 60f);
    }

    private void UpdateTPSCameraPosition()
    {
        if (playerCamera == null || currentVehicleTransform == null) return;

        if (playerCamera.transform.parent != null)
        {
            playerCamera.transform.SetParent(null);
        }

        Transform pivotOrigin = (currentVehicle != null && currentVehicle.tpsCameraPoint != null)
            ? currentVehicle.tpsCameraPoint
            : currentVehicleTransform;

        float activeDistance = currentVehicle != null ? currentVehicle.tpsDistance : tpsDistance;
        float activeHeight = currentVehicle != null ? currentVehicle.tpsHeight : tpsHeight;
        float activeLookAtHeight = currentVehicle != null ? currentVehicle.tpsLookAtHeight : tpsLookAtHeight;

        Vector3 pivotPoint = pivotOrigin.position + Vector3.up * activeLookAtHeight;

        // Spherical Pivot Calculation:
        // Calculate base pitch angle and radius from default distance and height
        float baseRadius = Mathf.Sqrt((activeDistance * activeDistance) + (activeHeight * activeHeight));
        float basePitchAngle = Mathf.Atan2(activeHeight, Mathf.Max(0.1f, activeDistance)) * Mathf.Rad2Deg;

        // Mouse vertical movement orbits around pitch axis
        float totalPitch = Mathf.Clamp(basePitchAngle + tpsPitchOffset, 2f, 78f);

        // Mouse horizontal movement adds to vehicle yaw
        float targetYaw = currentVehicleTransform.eulerAngles.y + tpsYawOffset;

        float currentYaw = playerCamera.transform.eulerAngles.y;
        float smoothedYaw = Mathf.LerpAngle(currentYaw, targetYaw, tpsRotationDamping * Time.deltaTime);

        Quaternion orbitRotation = Quaternion.Euler(totalPitch, smoothedYaw, 0f);

        Vector3 targetPos = pivotPoint - (orbitRotation * Vector3.forward * baseRadius);

        // Prevent clipping below ground
        float minAllowedHeight = currentVehicleTransform.position.y + 0.35f;
        if (targetPos.y < minAllowedHeight)
        {
            targetPos.y = minAllowedHeight;
        }

        playerCamera.transform.position = targetPos;
        playerCamera.transform.LookAt(pivotPoint);
    }

    private void HandleMouseLook()
    {
        if (IsUIBlockingInput()) return;
        if (Cursor.lockState != CursorLockMode.Locked || Cursor.visible) return;

        float mouseX = 0f;
        float mouseY = 0f;

        if (Mouse.current != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue() * (mouseSensitivity * 0.08f);
            mouseX = delta.x;
            mouseY = delta.y;
        }

        transform.Rotate(Vector3.up * mouseX);

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        if (cameraHolder != null)
        {
            cameraHolder.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }

    private void HandleMovement()
    {
        if (controller == null || !controller.enabled) return;

        bool isGrounded = controller.isGrounded;

        // Reliable Land Audio Detection before velocity is clamped
        if (!wasGroundedLastFrame && isGrounded && previousAirborneVelocityY < -2.2f)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayLand();
            }
        }

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        if (!isGrounded)
        {
            previousAirborneVelocityY = velocity.y;
        }
        else
        {
            previousAirborneVelocityY = 0f;
        }

        float moveX = 0f;
        float moveZ = 0f;
        bool isSprinting = false;
        bool jumpPressed = false;

        if (!IsUIBlockingInput())
        {
            if (KeyBindingManager.IsPressed(GameAction.MoveForward)) moveZ += 1f;
            if (KeyBindingManager.IsPressed(GameAction.MoveBackward)) moveZ -= 1f;
            if (KeyBindingManager.IsPressed(GameAction.MoveRight)) moveX += 1f;
            if (KeyBindingManager.IsPressed(GameAction.MoveLeft)) moveX -= 1f;

            isSprinting = KeyBindingManager.IsPressed(GameAction.Sprint);
            jumpPressed = KeyBindingManager.WasPressedThisFrame(GameAction.Jump);
        }

        Vector3 move = (transform.right * moveX + transform.forward * moveZ).normalized;
        float speed = isSprinting ? sprintSpeed : walkSpeed;

        controller.Move(move * speed * Time.deltaTime);

        // Footsteps audio modulation
        if (isGrounded && move.sqrMagnitude > 0.01f)
        {
            float stepInterval = isSprinting ? sprintStepInterval : walkStepInterval;
            footstepTimer += Time.deltaTime;
            if (footstepTimer >= stepInterval)
            {
                footstepTimer = 0f;
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayFootstep(isSprinting);
                }
            }
        }
        else
        {
            // Reset so the next step triggers right after beginning movement
            footstepTimer = Mathf.Max(0f, walkStepInterval * 0.75f);
        }

        // Jump audio
        if (jumpPressed && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            previousAirborneVelocityY = velocity.y;
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayJump();
            }
        }

        wasGroundedLastFrame = isGrounded;

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    // Parcels are picked up with the primary action (left click by default); Interact (E) is for vehicles, shops, terminals.
    // After a pickup the button may still be down: wait for its release so the click does not charge a throw.
    private bool waitForPickupButtonRelease = false;

    /// <summary>Key name of the pickup action for prompts, e.g. "Sol Tık".</summary>
    private static string PickupKeyName()
    {
        InputBindingData b = KeyBindingManager.GetBinding(GameAction.ThrowCargo);
        string name = b.IsAssigned ? b.GetDisplayName() : "?";
        int paren = name.IndexOf('(');
        return paren > 0 ? name.Substring(0, paren).Trim() : name;
    }

    private void HandleInteraction()
    {
        bool interactPressed = KeyBindingManager.WasPressedThisFrame(GameAction.Interact);
        bool pickupPressed = KeyBindingManager.WasPressedThisFrame(GameAction.ThrowCargo);

        // If holding an object
        if (grabber != null && grabber.IsHoldingObject)
        {
            UpdateCargoFocus(null);

            // Picked up with the same button that throws: ignore it until it has been released once.
            if (waitForPickupButtonRelease)
            {
                if (!KeyBindingManager.IsPressed(GameAction.ThrowCargo)) waitForPickupButtonRelease = false;
                currentDropHoldTime = 0f;
                if (InteractionPromptHUD.Instance != null)
                {
                    InteractionPromptHUD.Instance.HideThrowCharge();
                    InteractionPromptHUD.Instance.HidePrompt();
                }
                return;
            }

            if (afterGrabSafetyTimer > 0f)
            {
                afterGrabSafetyTimer -= Time.deltaTime;
                if (InteractionPromptHUD.Instance != null)
                {
                    InteractionPromptHUD.Instance.HideThrowCharge();
                    InteractionPromptHUD.Instance.HidePrompt();
                }
                return;
            }

            // 1. Throw Cargo (Charged)
            bool isChargingThrow = KeyBindingManager.IsPressed(GameAction.ThrowCargo);
            bool throwReleased = KeyBindingManager.WasReleasedThisFrame(GameAction.ThrowCargo);

            // 2. Drop Cargo (Gently)
            bool dropPressed = KeyBindingManager.WasPressedThisFrame(GameAction.DropCargo);

            if (isChargingThrow)
            {
                currentDropHoldTime += Time.deltaTime;
                float chargePercent = Mathf.Clamp01(currentDropHoldTime / throwChargeDuration);

                if (InteractionPromptHUD.Instance != null)
                {
                    InteractionPromptHUD.Instance.SetThrowCharge(chargePercent);
                }
            }
            else
            {
                if (InteractionPromptHUD.Instance != null)
                {
                    InteractionPromptHUD.Instance.HideThrowCharge();
                    InteractionPromptHUD.Instance.HidePrompt();
                }
            }

            if (throwReleased)
            {
                if (currentDropHoldTime >= 0.15f)
                {
                    // Charged throw
                    float charge = Mathf.Clamp01(currentDropHoldTime / throwChargeDuration);
                    float throwSpeed = Mathf.Lerp(4.5f, maxThrowForce, charge);
                    Vector3 throwVel = (playerCamera.transform.forward * throwSpeed) + (Vector3.up * 1.5f);
                    grabber.ReleaseObject(throwVel);
                }
                else
                {
                    // Light toss
                    Vector3 tossVel = (playerCamera.transform.forward * 4.0f) + (Vector3.up * 0.8f);
                    grabber.ReleaseObject(tossVel);
                }

                currentDropHoldTime = 0f;
                afterGrabSafetyTimer = 0.35f;
                if (InteractionPromptHUD.Instance != null)
                {
                    InteractionPromptHUD.Instance.HideThrowCharge();
                    InteractionPromptHUD.Instance.SuppressPrompts(0.35f);
                }
                return;
            }

            if (dropPressed)
            {
                // Gently drop straight down
                grabber.ReleaseObject(Vector3.zero);
                currentDropHoldTime = 0f;
                afterGrabSafetyTimer = 0.35f;
                if (InteractionPromptHUD.Instance != null)
                {
                    InteractionPromptHUD.Instance.HideThrowCharge();
                    InteractionPromptHUD.Instance.SuppressPrompts(0.35f);
                }
                return;
            }

            return;
        }

        if (playerCamera == null) return;

        // Perform NonAlloc raycast / spherecast to avoid GC heap churn
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        int hitCount = Physics.RaycastNonAlloc(ray, interactionHitsBuffer, interactionDistance, interactionLayers, QueryTriggerInteraction.Collide);
        if (hitCount == 0)
        {
            hitCount = Physics.SphereCastNonAlloc(ray, 0.22f, interactionHitsBuffer, interactionDistance, interactionLayers, QueryTriggerInteraction.Collide);
        }

        validHitsCache.Clear();
        for (int i = 0; i < hitCount; i++)
        {
            var h = interactionHitsBuffer[i];
            if (h.collider == null || h.collider.transform.IsChildOf(transform)) continue;

            // If collider is a trigger, only keep if it has an interactive terminal, property, or cargo component
            if (h.collider.isTrigger)
            {
                bool isInteractiveTrigger =
                    h.collider.GetComponentInParent<BranchUpgradeTerminal>() != null ||
                    h.collider.GetComponent<BranchUpgradeTerminal>() != null ||
                    h.collider.GetComponentInParent<PurchasableProperty>() != null ||
                    h.collider.GetComponent<PurchasableProperty>() != null ||
                    h.collider.GetComponentInParent<InsuranceAgencyManager>() != null ||
                    h.collider.GetComponent<InsuranceAgencyManager>() != null ||
                    h.collider.GetComponentInParent<PassiveDispatchManager>() != null ||
                    h.collider.GetComponent<PassiveDispatchManager>() != null ||
                    h.collider.GetComponentInParent<PhysicalCargoPackage>() != null ||
                    h.collider.GetComponent<PhysicalCargoPackage>() != null;

                if (!isInteractiveTrigger) continue;
            }

            validHitsCache.Add(h);
        }

        if (validHitsCache.Count > 1)
        {
            validHitsCache.Sort(hitDistanceComparison);
        }

        List<RaycastHit> validHits = validHitsCache;

        // STEP 1: Check if there is a directly targeted cargo package along the ray
        PhysicalCargoPackage targetedPackage = null;
        Rigidbody targetedRb = null;
        RaycastHit packageHit = default;
        bool hasPackageHit = false;

        foreach (var h in validHits)
        {
            PhysicalCargoPackage pkg = h.collider.GetComponentInParent<PhysicalCargoPackage>();
            if (pkg == null) pkg = h.collider.GetComponent<PhysicalCargoPackage>();
            Rigidbody rb = null;

            if (pkg != null)
            {
                rb = pkg.GetComponent<Rigidbody>();
            }
            else
            {
                Rigidbody attached = h.collider.attachedRigidbody;
                if (attached != null && !attached.isKinematic && h.collider.GetComponentInParent<DrivableVehicle>() == null)
                {
                    rb = attached;
                }
            }

            if (pkg != null || (rb != null && !rb.isKinematic))
            {
                targetedPackage = pkg;
                targetedRb = rb;
                packageHit = h;
                hasPackageHit = true;
                break;
            }
        }

        // Check if there is a solid terminal or closed tailgate physically blocking access to the package
        bool isTerminalInFront = false;
        VehicleTailgate closedTailgateInFront = null;

        if (hasPackageHit)
        {
            foreach (var h in validHits)
            {
                if (h.distance >= packageHit.distance) break;

                // Terminal or Property in front?
                if (h.collider.GetComponentInParent<BranchUpgradeTerminal>() != null ||
                    h.collider.GetComponentInParent<PurchasableProperty>() != null ||
                    h.collider.GetComponentInParent<InsuranceAgencyManager>() != null ||
                    h.collider.GetComponentInParent<PassiveDispatchManager>() != null)
                {
                    isTerminalInFront = true;
                    break;
                }

                // Closed Tailgate in front?
                VehicleTailgate tg = h.collider.GetComponent<VehicleTailgate>();
                if (tg == null) tg = h.collider.GetComponentInParent<VehicleTailgate>();
                if (tg == null)
                {
                    DrivableVehicle v = h.collider.GetComponentInParent<DrivableVehicle>();
                    if (v != null && v.rearTailgate != null)
                    {
                        float distToTg = Vector3.Distance(h.point, v.rearTailgate.transform.position);
                        if (distToTg < 1.8f) tg = v.rearTailgate;
                    }
                }

                if (tg != null && !tg.isOpen)
                {
                    closedTailgateInFront = tg;
                    break;
                }
            }
        }

        // Direct cargo package priority (inside open trunk, on ground, on tables, in warehouse)
        if (hasPackageHit && !isTerminalInFront && closedTailgateInFront == null)
        {
            UpdateCargoFocus(targetedPackage);
            if (pickupPressed && grabber != null)
            {
                UpdateCargoFocus(null);
                Rigidbody rbToGrab = targetedPackage != null ? targetedPackage.GetComponent<Rigidbody>() : targetedRb;
                if (rbToGrab != null)
                {
                    if (InteractionPromptHUD.Instance != null) InteractionPromptHUD.Instance.SuppressPrompts(0.35f);
                    grabber.GrabObject(rbToGrab);
                    afterGrabSafetyTimer = 0.25f;
                    currentDropHoldTime = 0f;
                    waitForPickupButtonRelease = true;
                }
                return;
            }

            if (InteractionPromptHUD.Instance != null)
            {
                string targetName = (targetedPackage != null) ? LocalizationManager.Get("prompt_target_cargo") : LocalizationManager.Get("prompt_target_object");
                InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.GetFormat("prompt_pickup_cargo", targetName, PickupKeyName()));
            }
            return;
        }

        // STEP 1.2: Process other interactable targets in distance order
        if (validHits.Count > 0)
        {
            foreach (var h in validHits)
            {
                // 1. Branch Upgrade Terminal
                BranchUpgradeTerminal terminal = h.collider.GetComponentInParent<BranchUpgradeTerminal>();
                if (terminal == null) terminal = h.collider.GetComponent<BranchUpgradeTerminal>();
                if (terminal != null)
                {
                    UpdateCargoFocus(null);
                    if (interactPressed)
                    {
                        if (InteractionPromptHUD.Instance != null) InteractionPromptHUD.Instance.SuppressPrompts(0.35f);
                        terminal.InteractTerminal();
                        return;
                    }

                    if (InteractionPromptHUD.Instance != null)
                    {
                        InteractionPromptHUD.Instance.ShowPrompt(terminal.GetPromptText());
                    }
                    return;
                }

                // 2. Locked Purchasable Commercial Property
                PurchasableProperty lockedProperty = h.collider.GetComponentInParent<PurchasableProperty>();
                if (lockedProperty == null) lockedProperty = h.collider.GetComponent<PurchasableProperty>();
                if (lockedProperty != null && !lockedProperty.IsUnlocked && !lockedProperty.disablePurchase)
                {
                    Vector3 anchorPos = lockedProperty.interactionAnchor != null ? lockedProperty.interactionAnchor.position : lockedProperty.transform.position;
                    float d = Vector3.Distance(transform.position, anchorPos);
                    Vector3 dirToAnchor = (anchorPos - playerCamera.transform.position).normalized;
                    bool isFacingShop = lockedProperty.interactionAnchor == null || Vector3.Dot(playerCamera.transform.forward, dirToAnchor) > 0.20f;

                    if ((d <= lockedProperty.interactionDistance || h.distance <= lockedProperty.interactionDistance) && isFacingShop)
                    {
                        UpdateCargoFocus(null);
                        if (interactPressed)
                        {
                            if (InteractionPromptHUD.Instance != null) InteractionPromptHUD.Instance.SuppressPrompts(0.35f);
                            if (CommercialHubUIManager.Instance != null)
                                CommercialHubUIManager.Instance.OpenPropertyPurchaseModal(lockedProperty);
                            else
                                lockedProperty.TryPurchase();
                            return;
                        }

                        if (InteractionPromptHUD.Instance != null)
                        {
                            InteractionPromptHUD.Instance.ShowPrompt(lockedProperty.GetPromptText());
                        }
                        return;
                    }
                }

                // 3. Unlocked Insurance Agency & Passive Dispatch Hub Terminals
                if (h.distance <= interactionDistance + 0.5f)
                {
                    InsuranceAgencyManager ins = h.collider.GetComponentInParent<InsuranceAgencyManager>();
                    if (ins == null) ins = h.collider.GetComponent<InsuranceAgencyManager>();
                    if (ins != null && ins.IsAgencyUnlocked())
                    {
                        UpdateCargoFocus(null);
                        if (interactPressed)
                        {
                            if (InteractionPromptHUD.Instance != null) InteractionPromptHUD.Instance.SuppressPrompts(0.35f);
                            if (CommercialHubUIManager.Instance != null)
                                CommercialHubUIManager.Instance.OpenInsurancePanel();
                            else
                                ins.TryUpgradeTier();
                            return;
                        }

                        if (InteractionPromptHUD.Instance != null)
                        {
                            InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.GetFormat("prompt_open_insurance_menu", ins.GetTierName()));
                        }
                        return;
                    }

                    PassiveDispatchManager hub = h.collider.GetComponentInParent<PassiveDispatchManager>();
                    if (hub == null) hub = h.collider.GetComponent<PassiveDispatchManager>();
                    if (hub != null && hub.IsHubUnlocked())
                    {
                        UpdateCargoFocus(null);
                        if (interactPressed)
                        {
                            if (InteractionPromptHUD.Instance != null) InteractionPromptHUD.Instance.SuppressPrompts(0.35f);
                            if (CommercialHubUIManager.Instance != null)
                                CommercialHubUIManager.Instance.OpenDispatchHubPanel();
                            else
                                hub.TryUpgradeHub();
                            return;
                        }

                        if (InteractionPromptHUD.Instance != null)
                        {
                            InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.GetFormat("prompt_open_dispatch_menu", hub.DispatchHubLevel, hub.GetCourierCount()));
                        }
                        return;
                    }
                }

                // 4. Vehicle Tailgate (Direct Collider Hit)
                VehicleTailgate directTailgate = h.collider.GetComponent<VehicleTailgate>();
                if (directTailgate == null) directTailgate = h.collider.GetComponentInParent<VehicleTailgate>();
                if (directTailgate != null)
                {
                    UpdateCargoFocus(null);
                    if (interactPressed)
                    {
                        if (InteractionPromptHUD.Instance != null) InteractionPromptHUD.Instance.SuppressPrompts(0.35f);
                        directTailgate.ToggleDoor();
                        return;
                    }

                    if (InteractionPromptHUD.Instance != null)
                    {
                        InteractionPromptHUD.Instance.ShowPrompt(directTailgate.GetPromptText());
                    }
                    return;
                }

                // 5. Physical Cargo Package or Grabbable Dynamic Rigidbody (Fallback if reached)
                PhysicalCargoPackage pkg = h.collider.GetComponentInParent<PhysicalCargoPackage>();
                if (pkg == null) pkg = h.collider.GetComponent<PhysicalCargoPackage>();
                Rigidbody targetRb = null;

                if (pkg != null)
                {
                    targetRb = pkg.GetComponent<Rigidbody>();
                }
                else
                {
                    Rigidbody rb = h.collider.attachedRigidbody;
                    if (rb != null && !rb.isKinematic && h.collider.GetComponentInParent<DrivableVehicle>() == null)
                    {
                        targetRb = rb;
                    }
                }

                if (pkg != null || (targetRb != null && !targetRb.isKinematic))
                {
                    UpdateCargoFocus(pkg);
                    if (pickupPressed && grabber != null)
                    {
                        UpdateCargoFocus(null);
                        Rigidbody rbToGrab = pkg != null ? pkg.GetComponent<Rigidbody>() : targetRb;
                        if (rbToGrab != null)
                        {
                            if (InteractionPromptHUD.Instance != null) InteractionPromptHUD.Instance.SuppressPrompts(0.35f);
                            grabber.GrabObject(rbToGrab);
                            afterGrabSafetyTimer = 0.25f;
                            currentDropHoldTime = 0f;
                            waitForPickupButtonRelease = true;
                        }
                        return;
                    }

                    if (InteractionPromptHUD.Instance != null)
                    {
                        string targetName = (pkg != null) ? LocalizationManager.Get("prompt_target_cargo") : LocalizationManager.Get("prompt_target_object");
                        InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.GetFormat("prompt_pickup_cargo", targetName, PickupKeyName()));
                    }
                    return;
                }

                // 6. Drivable Vehicle (Drive, Tailgate Area, Purchase, or Service Garage)
                DrivableVehicle vehicle = h.collider.GetComponentInParent<DrivableVehicle>();
                if (vehicle != null && !vehicle.isPlayerInside)
                {
                    UpdateCargoFocus(null);

                    if (!vehicle.IsUnlocked)
                    {
                        int branchLevel = BranchManager.Instance != null ? BranchManager.Instance.CurrentBranchLevel : (PlayerProgressionManager.Instance != null ? PlayerProgressionManager.Instance.WarehouseLevel : 1);
                        int currentBalance = PlayerEconomyManager.Instance != null ? PlayerEconomyManager.Instance.CurrentLiveBalance : 0;

                        if (branchLevel < vehicle.requiredPlayerLevel)
                        {
                            if (interactPressed)
                            {
                                if (InteractionPromptHUD.Instance != null) InteractionPromptHUD.Instance.SuppressPrompts(0.35f);
                                if (AudioManager.Instance != null) AudioManager.Instance.PlayError();
                                return;
                            }

                            if (InteractionPromptHUD.Instance != null)
                            {
                                InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.GetFormat("prompt_vehicle_locked_level", vehicle.vehicleName, vehicle.requiredPlayerLevel, vehicle.purchasePrice));
                            }
                        }
                        else if (currentBalance < vehicle.purchasePrice)
                        {
                            if (interactPressed)
                            {
                                if (InteractionPromptHUD.Instance != null) InteractionPromptHUD.Instance.SuppressPrompts(0.35f);
                                if (AudioManager.Instance != null) AudioManager.Instance.PlayError();
                                return;
                            }

                            if (InteractionPromptHUD.Instance != null)
                            {
                                InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.GetFormat("prompt_vehicle_locked_cash", vehicle.vehicleName, vehicle.purchasePrice, currentBalance));
                            }
                        }
                        else
                        {
                            if (interactPressed)
                            {
                                if (InteractionPromptHUD.Instance != null) InteractionPromptHUD.Instance.SuppressPrompts(0.35f);
                                bool bought = vehicle.TryPurchase();
                                if (bought && InteractionPromptHUD.Instance != null)
                                {
                                    InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.GetFormat("prompt_vehicle_purchased_success", vehicle.vehicleName), 2.5f);
                                }
                                return;
                            }

                            if (InteractionPromptHUD.Instance != null)
                            {
                                InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.GetFormat("prompt_purchase_vehicle", vehicle.vehicleName, vehicle.purchasePrice));
                            }
                        }
                        return;
                    }

                    float distToTailgate = float.MaxValue;
                    if (vehicle.rearTailgate != null)
                    {
                        distToTailgate = Vector3.Distance(h.point, vehicle.rearTailgate.transform.position);
                    }

                    // If aiming specifically at the rear tailgate area (< 1.8m from tailgate)
                    if (vehicle.rearTailgate != null && distToTailgate < 1.8f)
                    {
                        if (interactPressed)
                        {
                            if (InteractionPromptHUD.Instance != null) InteractionPromptHUD.Instance.SuppressPrompts(0.35f);
                            vehicle.rearTailgate.ToggleDoor();
                            return;
                        }

                        if (InteractionPromptHUD.Instance != null)
                        {
                            InteractionPromptHUD.Instance.ShowPrompt(vehicle.rearTailgate.GetPromptText());
                        }
                        return;
                    }

                    // Check if vehicle is in the Auto Service Garage bay
                    bool inGarageBay = VehicleServiceGarage.Instance != null &&
                                       VehicleServiceGarage.Instance.IsGarageUnlocked() &&
                                       VehicleServiceGarage.Instance.IsVehicleInServiceBay(vehicle);

                    if (inGarageBay)
                    {
                        VehicleServiceGarage.Instance.CheckGarageShortcutInputs(vehicle);

                        bool fPressed = KeyBindingManager.WasPressedThisFrame(GameAction.Vehicle);

                        if (fPressed && CommercialHubUIManager.Instance != null)
                        {
                            if (InteractionPromptHUD.Instance != null) InteractionPromptHUD.Instance.SuppressPrompts(0.35f);
                            CommercialHubUIManager.Instance.OpenGarageWorkshopPanel(vehicle);
                            return;
                        }

                        if (interactPressed && exitVehicleSafetyTimer <= 0f)
                        {
                            if (InteractionPromptHUD.Instance != null) InteractionPromptHUD.Instance.SuppressPrompts(0.35f);
                            vehicle.EnterVehicle(this);
                            return;
                        }

                        if (InteractionPromptHUD.Instance != null)
                        {
                            InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.Get("prompt_open_menu"));
                        }
                    }
                    else
                    {
                        if (interactPressed && exitVehicleSafetyTimer <= 0f)
                        {
                            if (InteractionPromptHUD.Instance != null) InteractionPromptHUD.Instance.SuppressPrompts(0.35f);
                            vehicle.EnterVehicle(this);
                            return;
                        }

                        if (InteractionPromptHUD.Instance != null)
                        {
                            InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.GetFormat("prompt_drive_vehicle", vehicle.vehicleName));
                        }
                    }
                    return;
                }
            }
        }

        // STEP 2: Proximity scans (Fallback ONLY if direct raycast hits did not find any interactable object)

        // 2.1 Branch Upgrade Terminal Proximity Scan
        Collider[] closeTerminals = Physics.OverlapSphere(playerCamera.transform.position + (playerCamera.transform.forward * 1.0f), 1.2f, interactionLayers, QueryTriggerInteraction.Collide);
        foreach (var ct in closeTerminals)
        {
            if (ct.transform.IsChildOf(transform)) continue;
            BranchUpgradeTerminal t = ct.GetComponentInParent<BranchUpgradeTerminal>();
            if (t == null) t = ct.GetComponent<BranchUpgradeTerminal>();
            if (t != null)
            {
                UpdateCargoFocus(null);
                if (interactPressed)
                {
                    if (InteractionPromptHUD.Instance != null) InteractionPromptHUD.Instance.SuppressPrompts(0.35f);
                    t.InteractTerminal();
                    return;
                }

                if (InteractionPromptHUD.Instance != null)
                {
                    InteractionPromptHUD.Instance.ShowPrompt(t.GetPromptText());
                }
                return;
            }
        }

        // 2.2 Locked Purchasable Property Proximity Scan
        Collider[] closeProps = Physics.OverlapSphere(transform.position, 5.5f, interactionLayers, QueryTriggerInteraction.Collide);
        foreach (var cp in closeProps)
        {
            if (cp.transform.IsChildOf(transform)) continue;
            PurchasableProperty p = cp.GetComponentInParent<PurchasableProperty>();
            if (p == null) p = cp.GetComponent<PurchasableProperty>();
            if (p != null && !p.IsUnlocked && !p.disablePurchase)
            {
                Vector3 anchorPos = p.interactionAnchor != null ? p.interactionAnchor.position : p.transform.position;
                float d = Vector3.Distance(transform.position, anchorPos);
                Vector3 dirToAnchor = (anchorPos - playerCamera.transform.position).normalized;
                bool isFacingShop = Vector3.Dot(playerCamera.transform.forward, dirToAnchor) > 0.30f;

                if (d <= p.interactionDistance && isFacingShop)
                {
                    UpdateCargoFocus(null);
                    if (interactPressed)
                    {
                        if (InteractionPromptHUD.Instance != null) InteractionPromptHUD.Instance.SuppressPrompts(0.35f);
                        if (CommercialHubUIManager.Instance != null)
                            CommercialHubUIManager.Instance.OpenPropertyPurchaseModal(p);
                        else
                            p.TryPurchase();
                        return;
                    }

                    if (InteractionPromptHUD.Instance != null)
                    {
                        InteractionPromptHUD.Instance.ShowPrompt(p.GetPromptText());
                    }
                    return;
                }
            }
        }

        // 2.3 Proximity scan fallback for cargo packages directly under or near player's feet
        Collider[] closeHits = Physics.OverlapSphere(playerCamera.transform.position + (playerCamera.transform.forward * 1.0f), 0.85f, interactionLayers, QueryTriggerInteraction.Ignore);
        float closestDist = float.MaxValue;
        PhysicalCargoPackage proxPkg = null;
        Rigidbody proxRb = null;

        foreach (var ch in closeHits)
        {
            if (ch.transform.IsChildOf(transform)) continue;

            PhysicalCargoPackage p = ch.GetComponentInParent<PhysicalCargoPackage>();
            if (p != null)
            {
                float d = Vector3.Distance(playerCamera.transform.position, p.transform.position);
                if (d < closestDist)
                {
                    closestDist = d;
                    proxPkg = p;
                    proxRb = p.GetComponent<Rigidbody>();
                }
            }
        }

        if (proxPkg != null || (proxRb != null && !proxRb.isKinematic))
        {
            UpdateCargoFocus(proxPkg);

            if (pickupPressed && grabber != null)
            {
                UpdateCargoFocus(null);
                Rigidbody rbToGrab = proxPkg != null ? proxPkg.GetComponent<Rigidbody>() : proxRb;
                if (rbToGrab != null)
                {
                    if (InteractionPromptHUD.Instance != null) InteractionPromptHUD.Instance.SuppressPrompts(0.35f);
                    grabber.GrabObject(rbToGrab);
                    afterGrabSafetyTimer = 0.25f;
                    currentDropHoldTime = 0f;
                    waitForPickupButtonRelease = true;
                }
                return;
            }

            if (InteractionPromptHUD.Instance != null)
            {
                string targetName = (proxPkg != null) ? LocalizationManager.Get("prompt_target_cargo") : LocalizationManager.Get("prompt_target_object");
                InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.GetFormat("prompt_pickup_cargo", targetName, PickupKeyName()));
            }
            return;
        }

        // 2.4 Proximity scan fallback for Vehicles (Standing right beside driver door / front / rear)
        Collider[] closeVehs = Physics.OverlapSphere(transform.position, 3.2f, interactionLayers, QueryTriggerInteraction.Collide);
        foreach (var cv in closeVehs)
        {
            if (cv.transform.IsChildOf(transform)) continue;
            DrivableVehicle v = cv.GetComponentInParent<DrivableVehicle>();
            if (v != null && !v.isPlayerInside)
            {
                Vector3 dirToVeh = (v.transform.position - playerCamera.transform.position).normalized;
                bool isFacingVeh = Vector3.Dot(playerCamera.transform.forward, dirToVeh) > 0.15f;
                if (isFacingVeh)
                {
                    UpdateCargoFocus(null);
                    if (interactPressed && exitVehicleSafetyTimer <= 0f)
                    {
                        if (InteractionPromptHUD.Instance != null) InteractionPromptHUD.Instance.SuppressPrompts(0.35f);
                        v.EnterVehicle(this);
                        return;
                    }

                    if (InteractionPromptHUD.Instance != null)
                    {
                        InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.GetFormat("prompt_drive_vehicle", v.vehicleName));
                    }
                    return;
                }
            }
        }

        // No interactive target hit
        UpdateCargoFocus(null);

        if (InteractionPromptHUD.Instance != null)
        {
            InteractionPromptHUD.Instance.HidePrompt();
        }
    }

    public void SetOnFootActive(bool active)
    {
        isOnFoot = active;
        velocity = Vector3.zero;
        if (!active) UpdateCargoFocus(null);
        if (controller != null) controller.enabled = active;
    }

    public void AttachCameraToSeat(Transform seatPoint)
    {
        if (playerCamera == null || seatPoint == null) return;

        currentSeatPoint = seatPoint;
        currentVehicle = seatPoint.GetComponentInParent<DrivableVehicle>();
        currentVehicleTransform = currentVehicle != null ? currentVehicle.transform : seatPoint.root;

        // Restore the last used driving camera (default: first person)
        vehicleCameraMode = (VehicleCameraMode)PlayerPrefs.GetInt(VehicleCameraModePrefKey, (int)VehicleCameraMode.FirstPerson);
        vehicleYaw = 0f;
        vehiclePitch = 0f;
        tpsYawOffset = 0f;
        tpsPitchOffset = 0f;

        // Both driving cameras use an unparented camera (TPS orbits freely, FPS follows the seat in LateUpdate)
        playerCamera.transform.SetParent(null);
        if (vehicleCameraMode == VehicleCameraMode.FirstPerson) UpdateFPSCamera(true);

        if (currentVehicle != null)
        {
            currentVehicle.UpdateDriverVisibility(vehicleCameraMode);
        }
    }

    public void DetachCameraFromSeat()
    {
        if (playerCamera == null) return;

        vehicleCameraMode = VehicleCameraMode.FirstPerson;
        currentSeatPoint = null;
        currentVehicle = null;
        currentVehicleTransform = null;
        vehicleYaw = 0f;
        vehiclePitch = 0f;
        pitch = 0f;

        playerCamera.transform.SetParent(cameraHolder != null ? cameraHolder : transform);
        playerCamera.transform.localPosition = Vector3.zero;
        playerCamera.transform.localRotation = Quaternion.identity;
        if (cameraHolder != null) cameraHolder.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// Teleports the player character to a world position, rotates the body (yaw), and pitches the camera towards a target point.
    /// Safely handles CharacterController enable/disable and Physics transform syncing.
    /// </summary>
    public void TeleportAndLookAt(Vector3 targetPosition, Vector3 lookTargetPoint)
    {
        // 1. If currently inside vehicle or holding something, detach cleanly
        if (!isOnFoot && currentVehicle != null)
        {
            currentVehicle.ExitVehicle();
        }

        if (grabber != null && grabber.IsHoldingObject)
        {
            grabber.ReleaseObject(Vector3.zero);
        }

        if (controller != null) controller.enabled = false;

        transform.position = targetPosition;

        Vector3 dir = lookTargetPoint - targetPosition;
        Vector3 flatDir = new Vector3(dir.x, 0f, dir.z);

        if (flatDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(flatDir);
            transform.rotation = targetRot;
        }

        // Calculate vertical pitch angle towards look target
        float horizontalDist = flatDir.magnitude;
        float eyeHeight = cameraHolder != null ? cameraHolder.localPosition.y : 1.7f;
        float verticalDist = dir.y - eyeHeight;
        float targetPitch = -Mathf.Atan2(verticalDist, Mathf.Max(0.1f, horizontalDist)) * Mathf.Rad2Deg;
        targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);

        pitch = targetPitch;
        if (cameraHolder != null)
        {
            cameraHolder.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        Physics.SyncTransforms();

        if (controller != null && isOnFoot) controller.enabled = true;
    }

    /// <summary>
    /// Called when an explosive cargo detonates near the player. Drops held items, shows emergency notification and triggers hospital day end.
    /// </summary>
    public void TriggerExplosionCasualty(Vector3 explosionOrigin)
    {
        if (grabber != null && grabber.IsHoldingObject)
        {
            grabber.ReleaseObject(Vector3.up * 3f + (transform.position - explosionOrigin).normalized * 5f);
        }

        if (InteractionPromptHUD.Instance != null)
        {
            InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.Get("prompt_explosion_emergency"), 6.0f);
        }

        if (DaySummaryManager.Instance != null)
        {
            DaySummaryManager.Instance.emergencyHospitalReason = LocalizationManager.Get("prompt_hospital_reason_explosion");
        }

        LockCursor(false);

        if (DayTimeManager.Instance != null)
        {
            DayTimeManager.Instance.EndShift();
        }
        else if (DaySummaryManager.Instance != null)
        {
            DaySummaryManager.Instance.ShowDaySummary();
        }
    }

    public static void LockCursor(bool locked)
    {
        if (locked && IsAnyUIOpen())
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
