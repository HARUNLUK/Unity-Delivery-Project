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
    public VehicleCameraMode vehicleCameraMode = VehicleCameraMode.FirstPerson;
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
    public int playerCash = 500;

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

        // 7. Direct scene hierarchy fallback for DaySummaryPanel
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
                InteractionPromptHUD.Instance.ShowPrompt("<color=#FF5555>[DEV RESET] TÜM ARAÇLAR (YAKIT & KONDİSYON %100), DÜKKANLAR VE ŞUBE SIFIRLANDI (F9)</color>", 3.5f);
            }
            if (CargoTabletUI.Instance != null && CargoTabletUI.Instance.IsTabletOpen)
            {
                CargoTabletUI.Instance.PopulateVehicleList();
            }
        }

        if (!isOnFoot)
        {
            // V tuşu ile FPS (İç görünüm) ve TPS (Dış takip) arasında geçiş yap
            if (!isUIOpen && Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
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
    }

    public void ToggleVehicleCameraMode()
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
                playerCamera.transform.SetParent(currentSeatPoint);
                Vector3 offset = currentVehicle != null ? currentVehicle.fpsCameraOffset : Vector3.zero;
                playerCamera.transform.localPosition = offset;
                playerCamera.transform.localRotation = Quaternion.identity;
                vehicleYaw = 0f;
                vehiclePitch = 0f;
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

        playerCamera.transform.localRotation = Quaternion.Euler(vehiclePitch, vehicleYaw, 0f);
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

        if (!IsUIBlockingInput() && Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveZ += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveZ -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveX += 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveX -= 1f;

            isSprinting = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
            jumpPressed = Keyboard.current.spaceKey.wasPressedThisFrame;
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        try
        {
            if (!IsUIBlockingInput())
            {
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) moveZ = Mathf.Max(moveZ, 1f);
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) moveZ = Mathf.Min(moveZ, -1f);
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) moveX = Mathf.Max(moveX, 1f);
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) moveX = Mathf.Min(moveX, -1f);
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) isSprinting = true;
                if (Input.GetKeyDown(KeyCode.Space)) jumpPressed = true;
            }
        }
        catch { }
#endif

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

    private void HandleInteraction()
    {
        bool interactPressed = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) interactPressed = true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        try { if (Input.GetKeyDown(KeyCode.E)) interactPressed = true; } catch { }
#endif

        // If holding an object
        if (grabber != null && grabber.IsHoldingObject)
        {
            UpdateCargoFocus(null);

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

            bool isHoldingDropKey = false;
            bool dropKeyReleased = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.eKey.isPressed) isHoldingDropKey = true;
            if (Mouse.current != null && Mouse.current.leftButton.isPressed) isHoldingDropKey = true;
            if (Keyboard.current != null && Keyboard.current.eKey.wasReleasedThisFrame) dropKeyReleased = true;
            if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame) dropKeyReleased = true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (Input.GetKey(KeyCode.E) || Input.GetMouseButton(0)) isHoldingDropKey = true;
                if (Input.GetKeyUp(KeyCode.E) || Input.GetMouseButtonUp(0)) dropKeyReleased = true;
            }
            catch { }
#endif

            if (isHoldingDropKey)
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

            if (dropKeyReleased)
            {
                if (currentDropHoldTime >= 0.25f)
                {
                    // Charged throw
                    float charge = Mathf.Clamp01(currentDropHoldTime / throwChargeDuration);
                    float throwSpeed = Mathf.Lerp(4.5f, maxThrowForce, charge);
                    Vector3 throwVel = (playerCamera.transform.forward * throwSpeed) + (Vector3.up * 1.5f);
                    grabber.ReleaseObject(throwVel);
                }
                else
                {
                    // Gently drop
                    grabber.ReleaseObject(Vector3.zero);
                }

                currentDropHoldTime = 0f;
                afterGrabSafetyTimer = 0.35f;
                if (InteractionPromptHUD.Instance != null)
                {
                    InteractionPromptHUD.Instance.HideThrowCharge();
                    InteractionPromptHUD.Instance.SuppressPrompts(0.35f);
                }
            }

            return;
        }

        if (playerCamera == null) return;

        // Perform raycast / spherecast with RaycastAll to avoid static shop/building geometry blocking interactables
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, interactionDistance, interactionLayers, QueryTriggerInteraction.Collide);
        if (hits == null || hits.Length == 0)
        {
            hits = Physics.SphereCastAll(ray, 0.35f, interactionDistance, interactionLayers, QueryTriggerInteraction.Collide);
        }

        if (hits != null && hits.Length > 1)
        {
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        }

        // STEP 1: Process the closest direct interactable hit along the raycast.
        if (hits != null)
        {
            foreach (var h in hits)
            {
                if (h.collider == null || h.collider.transform.IsChildOf(transform)) continue;

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
                            InteractionPromptHUD.Instance.ShowPrompt($"<color=#32FF64>[E] Kargo Sigorta Acentesi Menüsünü Aç ({ins.GetTierName()})</color>");
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
                            InteractionPromptHUD.Instance.ShowPrompt($"<color=#32FF64>[E] Bölge Dağıtım Şubesi Menüsünü Aç (Seviye {hub.DispatchHubLevel} - {hub.GetCourierCount()} Kurye)</color>");
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

                // 5. Physical Cargo Package or Grabbable Dynamic Rigidbody
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
                    if (interactPressed && grabber != null)
                    {
                        UpdateCargoFocus(null);
                        Rigidbody rbToGrab = pkg != null ? pkg.GetComponent<Rigidbody>() : targetRb;
                        if (rbToGrab != null)
                        {
                            if (InteractionPromptHUD.Instance != null) InteractionPromptHUD.Instance.SuppressPrompts(0.35f);
                            grabber.GrabObject(rbToGrab);
                            afterGrabSafetyTimer = 0.25f;
                            currentDropHoldTime = 0f;
                        }
                        return;
                    }

                    if (InteractionPromptHUD.Instance != null)
                    {
                        string targetName = (pkg != null) ? "Cargo" : "Object";
                        InteractionPromptHUD.Instance.ShowPrompt($"[E] Pick up {targetName}");
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
                                InteractionPromptHUD.Instance.ShowPrompt($"<color=#FF5555>[LOCKED] {vehicle.vehicleName}</color> (Requires Branch Level {vehicle.requiredPlayerLevel} - ${vehicle.purchasePrice})");
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
                                InteractionPromptHUD.Instance.ShowPrompt($"<color=#FFAA33>[LOCKED] {vehicle.vehicleName}</color> (${vehicle.purchasePrice} - Balance: ${currentBalance})");
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
                                    InteractionPromptHUD.Instance.ShowPrompt($"<color=#32FFFF>[PURCHASED] {vehicle.vehicleName} Successfully Purchased!</color>", 2.5f);
                                }
                                return;
                            }

                            if (InteractionPromptHUD.Instance != null)
                            {
                                InteractionPromptHUD.Instance.ShowPrompt($"<color=#32FF64>[E] Purchase: {vehicle.vehicleName}</color> (${vehicle.purchasePrice})");
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

                        bool fPressed = false;
#if ENABLE_INPUT_SYSTEM
                        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame) fPressed = true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                        try { if (Input.GetKeyDown(KeyCode.F)) fPressed = true; } catch { }
#endif

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
                            InteractionPromptHUD.Instance.ShowPrompt("<color=#FFD232>[F] Menüyü Aç</color>");
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
                            InteractionPromptHUD.Instance.ShowPrompt($"[E] Drive {vehicle.vehicleName}");
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

            if (interactPressed && grabber != null)
            {
                UpdateCargoFocus(null);
                Rigidbody rbToGrab = proxPkg != null ? proxPkg.GetComponent<Rigidbody>() : proxRb;
                if (rbToGrab != null)
                {
                    if (InteractionPromptHUD.Instance != null) InteractionPromptHUD.Instance.SuppressPrompts(0.35f);
                    grabber.GrabObject(rbToGrab);
                    afterGrabSafetyTimer = 0.25f;
                    currentDropHoldTime = 0f;
                }
                return;
            }

            if (InteractionPromptHUD.Instance != null)
            {
                string targetName = (proxPkg != null) ? "Cargo" : "Object";
                InteractionPromptHUD.Instance.ShowPrompt($"[E] Pick up {targetName}");
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
                        InteractionPromptHUD.Instance.ShowPrompt($"[E] Drive {v.vehicleName}");
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

        vehicleCameraMode = VehicleCameraMode.FirstPerson;
        vehicleYaw = 0f;
        vehiclePitch = 0f;
        tpsYawOffset = 0f;
        tpsPitchOffset = 0f;

        playerCamera.transform.SetParent(seatPoint);
        Vector3 offset = currentVehicle != null ? currentVehicle.fpsCameraOffset : Vector3.zero;
        playerCamera.transform.localPosition = offset;
        playerCamera.transform.localRotation = Quaternion.identity;

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
            InteractionPromptHUD.Instance.ShowPrompt("<color=#FF2222>💥 PATLAMA! Ağır yaralandınız ve acilen hastaneye kaldırıldınız...</color>", 6.0f);
        }

        if (DaySummaryManager.Instance != null)
        {
            DaySummaryManager.Instance.emergencyHospitalReason = "Kargo patlaması nedeniyle ağır yaralanma (Hastaneye Kaldırıldı)";
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
