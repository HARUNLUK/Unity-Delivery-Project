using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FPSPlayerController : MonoBehaviour
{
    public static FPSPlayerController Instance { get; private set; }
    [Header("--- MOVEMENT SETTINGS ---")]
    public float walkSpeed = 4.5f;
    public float sprintSpeed = 8.0f;
    public float jumpHeight = 1.2f;
    public float gravity = -20f;

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
    private Vector3 velocity;
    private float pitch = 0f;
    private bool isOnFoot = true;

    private Transform originalCameraParent;
    private Vector3 originalCameraLocalPos;
    private Quaternion originalCameraLocalRot;

    public enum VehicleCameraMode { FirstPerson, ThirdPerson }

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

    private float vehicleYaw = 0f;
    private float vehiclePitch = 0f;
    private float tpsYawOffset = 0f;
    private float tpsPitchOffset = 0f;
    public Transform currentSeatPoint;
    public DrivableVehicle currentVehicle;
    public Transform currentVehicleTransform;

    private float currentDropHoldTime = 0f;
    private float afterGrabSafetyTimer = 0f;

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
        // Deactivate any conflicting scene cameras so FPS Camera is 100% in control
        Camera[] allCameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var cam in allCameras)
        {
            if (cam != playerCamera && !cam.transform.IsChildOf(transform))
            {
                cam.gameObject.SetActive(false);
            }
        }

        SetOnFootActive(true);
        LockCursor(true);
    }

    public bool IsUIBlockingInput()
    {
        if (CargoTabletUI.Instance != null && CargoTabletUI.Instance.IsTabletOpen)
            return true;

        if (DaySummaryManager.Instance != null && DaySummaryManager.Instance.summaryPanelRoot != null && DaySummaryManager.Instance.summaryPanelRoot.activeSelf)
            return true;

        if (Cursor.lockState != CursorLockMode.Locked || Cursor.visible)
            return true;

        return false;
    }

    private void Update()
    {
        bool isUIOpen = IsUIBlockingInput();

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

        // F9 Dev Reset for Vehicles, Shops / Properties & Branch Progression
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
                InteractionPromptHUD.Instance.ShowPrompt("<color=#FF5555>[DEV RESET] TÜM ARAÇLAR, DÜKKANLAR VE ŞUBE SIFIRLANDI (F9)</color>", 3.5f);
            }
            if (CargoTabletUI.Instance != null && CargoTabletUI.Instance.IsTabletOpen)
            {
                CargoTabletUI.Instance.PopulateVehicleList();
            }
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
        try
        {
            if (Input.GetKeyDown(KeyCode.F9))
            {
                return true;
            }
        }
        catch { }

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
    }

    private void HandleInVehicleLook()
    {
        if (playerCamera == null) return;
        if (IsUIBlockingInput()) return;

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
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
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

        Vector3 move = (transform.right * moveX + transform.forward * moveZ).normalized;
        float speed = isSprinting ? sprintSpeed : walkSpeed;

        controller.Move(move * speed * Time.deltaTime);

        if (jumpPressed && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleInteraction()
    {
        bool interactPressed = (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame);
        bool leftClickPressed = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);

        // If holding an object
        if (grabber != null && grabber.IsHoldingObject)
        {
            if (afterGrabSafetyTimer > 0f)
            {
                afterGrabSafetyTimer -= Time.deltaTime;
                if (InteractionPromptHUD.Instance != null)
                {
                    InteractionPromptHUD.Instance.ShowPrompt("[E] or [LMB] Drop (Hold to Throw)");
                }
                return;
            }

            bool isHoldingDropKey = (Keyboard.current != null && Keyboard.current.eKey.isPressed) ||
                                    (Mouse.current != null && Mouse.current.leftButton.isPressed);

            bool dropKeyReleased = (Keyboard.current != null && Keyboard.current.eKey.wasReleasedThisFrame) ||
                                   (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame);

            if (isHoldingDropKey)
            {
                currentDropHoldTime += Time.deltaTime;
                float chargePercent = Mathf.Clamp01(currentDropHoldTime / throwChargeDuration);

                if (currentDropHoldTime > 0.18f)
                {
                    if (InteractionPromptHUD.Instance != null)
                    {
                        InteractionPromptHUD.Instance.ShowPrompt($"Throw Power: {(int)(chargePercent * 100)}% (Release to Throw)");
                    }
                }
                else
                {
                    if (InteractionPromptHUD.Instance != null)
                    {
                        InteractionPromptHUD.Instance.ShowPrompt("[E] / [LMB] Drop (Hold: Throw)");
                    }
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
                if (InteractionPromptHUD.Instance != null)
                {
                    InteractionPromptHUD.Instance.HidePrompt();
                }
            }

            return;
        }

        if (playerCamera == null) return;

        // Perform raycast / spherecast with RaycastAll to avoid static shop/building geometry blocking interactables
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, interactionDistance, interactionLayers, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
        {
            hits = Physics.SphereCastAll(ray, 0.25f, interactionDistance, interactionLayers, QueryTriggerInteraction.Ignore);
        }

        if (hits != null && hits.Length > 1)
        {
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        }

        // 0. Check Branch Upgrade Terminal (Direct Hit or Proximity)
        BranchUpgradeTerminal terminal = null;
        if (hits != null)
        {
            foreach (var h in hits)
            {
                terminal = h.collider.GetComponentInParent<BranchUpgradeTerminal>();
                if (terminal == null) terminal = h.collider.GetComponent<BranchUpgradeTerminal>();
                if (terminal != null) break;
            }
        }

        if (terminal == null)
        {
            Collider[] closeTerminals = Physics.OverlapSphere(playerCamera.transform.position + (playerCamera.transform.forward * 1.0f), 1.2f, interactionLayers, QueryTriggerInteraction.Collide);
            foreach (var ct in closeTerminals)
            {
                if (ct.transform.IsChildOf(transform)) continue;
                BranchUpgradeTerminal t = ct.GetComponentInParent<BranchUpgradeTerminal>();
                if (t == null) t = ct.GetComponent<BranchUpgradeTerminal>();
                if (t != null)
                {
                    terminal = t;
                    break;
                }
            }
        }

        if (terminal != null)
        {
            if (InteractionPromptHUD.Instance != null)
            {
                InteractionPromptHUD.Instance.ShowPrompt(terminal.GetPromptText());
            }

            if (interactPressed)
            {
                terminal.InteractTerminal();
            }
            return;
        }

        // 1. Check Locked Purchasable Commercial Property
        PurchasableProperty lockedProperty = null;
        if (hits != null)
        {
            foreach (var h in hits)
            {
                PurchasableProperty p = h.collider.GetComponentInParent<PurchasableProperty>();
                if (p == null) p = h.collider.GetComponent<PurchasableProperty>();
                if (p != null && !p.IsUnlocked)
                {
                    lockedProperty = p;
                    break;
                }
            }
        }

        if (lockedProperty == null)
        {
            Collider[] closeProps = Physics.OverlapSphere(transform.position, 5.5f, interactionLayers, QueryTriggerInteraction.Collide);
            foreach (var cp in closeProps)
            {
                if (cp.transform.IsChildOf(transform)) continue;
                PurchasableProperty p = cp.GetComponentInParent<PurchasableProperty>();
                if (p == null) p = cp.GetComponent<PurchasableProperty>();
                if (p != null && !p.IsUnlocked)
                {
                    Vector3 anchorPos = p.interactionAnchor != null ? p.interactionAnchor.position : p.transform.position;
                    float d = Vector3.Distance(transform.position, anchorPos);
                    if (d <= p.interactionDistance)
                    {
                        lockedProperty = p;
                        break;
                    }
                }
            }
        }

        if (lockedProperty != null && !lockedProperty.IsUnlocked)
        {
            if (InteractionPromptHUD.Instance != null)
            {
                InteractionPromptHUD.Instance.ShowPrompt(lockedProperty.GetPromptText());
            }

            if (interactPressed)
            {
                lockedProperty.TryPurchase();
            }
            return;
        }

        // 2. Physical Cargo Package or Rigidbody Object Detection (High Priority)
        PhysicalCargoPackage pkg = null;
        Rigidbody targetRb = null;

        if (hits != null)
        {
            foreach (var h in hits)
            {
                PhysicalCargoPackage p = h.collider.GetComponentInParent<PhysicalCargoPackage>();
                if (p == null) p = h.collider.GetComponent<PhysicalCargoPackage>();
                if (p != null)
                {
                    pkg = p;
                    targetRb = p.GetComponent<Rigidbody>();
                    break;
                }

                Rigidbody rb = h.collider.attachedRigidbody;
                if (rb != null && !rb.isKinematic && h.collider.GetComponentInParent<DrivableVehicle>() == null)
                {
                    targetRb = rb;
                    break;
                }
            }
        }

        // Proximity scan fallback for packages directly under or in front of the player
        if (pkg == null && targetRb == null)
        {
            Collider[] closeHits = Physics.OverlapSphere(playerCamera.transform.position + (playerCamera.transform.forward * 1.2f), 0.95f, interactionLayers, QueryTriggerInteraction.Ignore);
            float closestDist = float.MaxValue;

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
                        pkg = p;
                        targetRb = p.GetComponent<Rigidbody>();
                    }
                }
            }
        }

        if (pkg != null || (targetRb != null && !targetRb.isKinematic))
        {
            if (InteractionPromptHUD.Instance != null)
            {
                string targetName = (pkg != null) ? $"Cargo #{pkg.targetPointId} (${pkg.deliveryReward})" : "Object";
                InteractionPromptHUD.Instance.ShowPrompt($"[E] Pick up {targetName}");
            }

            if (interactPressed && grabber != null)
            {
                Rigidbody rbToGrab = pkg != null ? pkg.GetComponent<Rigidbody>() : targetRb;
                if (rbToGrab != null)
                {
                    grabber.GrabObject(rbToGrab);
                    afterGrabSafetyTimer = 0.22f;
                    currentDropHoldTime = 0f;
                }
            }
            return;
        }

        // 3. Vehicle Tailgate Interaction
        if (hits != null)
        {
            foreach (var h in hits)
            {
                VehicleTailgate directTailgate = h.collider.GetComponent<VehicleTailgate>();
                if (directTailgate == null) directTailgate = h.collider.GetComponentInParent<VehicleTailgate>();

                if (directTailgate != null)
                {
                    if (InteractionPromptHUD.Instance != null)
                    {
                        InteractionPromptHUD.Instance.ShowPrompt(directTailgate.GetPromptText());
                    }

                    if (interactPressed)
                    {
                        directTailgate.ToggleDoor();
                    }
                    return;
                }
            }
        }

        // 4. Vehicle Drive & Service Garage Interaction
        if (hits != null)
        {
            foreach (var h in hits)
            {
                DrivableVehicle vehicle = h.collider.GetComponentInParent<DrivableVehicle>();
                if (vehicle != null && !vehicle.isPlayerInside)
                {
                    if (!vehicle.IsUnlocked)
                    {
                        int playerLevel = PlayerProgressionManager.Instance != null ? PlayerProgressionManager.Instance.PlayerLevel : 1;
                        int currentBalance = PlayerEconomyManager.Instance != null ? PlayerEconomyManager.Instance.CurrentLiveBalance : 0;

                        if (playerLevel < vehicle.requiredPlayerLevel)
                        {
                            if (InteractionPromptHUD.Instance != null)
                            {
                                InteractionPromptHUD.Instance.ShowPrompt($"<color=#FF5555>[LOCKED] {vehicle.vehicleName}</color> (Requires Level {vehicle.requiredPlayerLevel} - ${vehicle.purchasePrice})");
                            }
                        }
                        else if (currentBalance < vehicle.purchasePrice)
                        {
                            if (InteractionPromptHUD.Instance != null)
                            {
                                InteractionPromptHUD.Instance.ShowPrompt($"<color=#FFAA33>[LOCKED] {vehicle.vehicleName}</color> (${vehicle.purchasePrice} - Balance: ${currentBalance})");
                            }
                        }
                        else
                        {
                            if (InteractionPromptHUD.Instance != null)
                            {
                                InteractionPromptHUD.Instance.ShowPrompt($"<color=#32FF64>[E] Purchase: {vehicle.vehicleName}</color> (${vehicle.purchasePrice})");
                            }

                            if (interactPressed)
                            {
                                bool bought = vehicle.TryPurchase();
                                if (bought && InteractionPromptHUD.Instance != null)
                                {
                                    InteractionPromptHUD.Instance.ShowPrompt($"<color=#32FFFF>[PURCHASED] {vehicle.vehicleName} Successfully Purchased!</color>");
                                }
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
                        if (InteractionPromptHUD.Instance != null)
                        {
                            InteractionPromptHUD.Instance.ShowPrompt(vehicle.rearTailgate.GetPromptText());
                        }

                        if (interactPressed)
                        {
                            vehicle.rearTailgate.ToggleDoor();
                        }
                        return;
                    }

                    // Check if vehicle is in the unlocked Auto Service Garage bay
                    bool inGarageBay = VehicleServiceGarage.Instance != null &&
                                       VehicleServiceGarage.Instance.IsGarageUnlocked() &&
                                       VehicleServiceGarage.Instance.IsVehicleInServiceBay(vehicle);

                    if (inGarageBay)
                    {
                        VehicleServiceGarage.Instance.CheckGarageShortcutInputs(vehicle);
                        string garageInfo = VehicleServiceGarage.Instance.GetGaragePromptForVehicle(vehicle);

                        if (InteractionPromptHUD.Instance != null)
                        {
                            InteractionPromptHUD.Instance.ShowPrompt($"[E] Drive {vehicle.vehicleName} | " + garageInfo);
                        }
                    }
                    else
                    {
                        if (InteractionPromptHUD.Instance != null)
                        {
                            InteractionPromptHUD.Instance.ShowPrompt($"[E] Drive {vehicle.vehicleName}");
                        }
                    }

                    if (interactPressed)
                    {
                        vehicle.EnterVehicle(this);
                    }
                    return;
                }
            }
        }

        // 5. Check if standing near a vehicle inside the service garage bay without aiming directly at it
        if (VehicleServiceGarage.Instance != null && VehicleServiceGarage.Instance.IsGarageUnlocked())
        {
            DrivableVehicle bayVehicle = VehicleServiceGarage.Instance.FindActiveVehicleInBay();
            if (bayVehicle != null && Vector3.Distance(transform.position, bayVehicle.transform.position) <= 5.0f)
            {
                VehicleServiceGarage.Instance.CheckGarageShortcutInputs(bayVehicle);
            }
        }

        // No interactive target hit
        if (InteractionPromptHUD.Instance != null)
        {
            InteractionPromptHUD.Instance.HidePrompt();
        }
    }

    public void SetOnFootActive(bool active)
    {
        isOnFoot = active;
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
        playerCamera.transform.localPosition = originalCameraLocalPos;
        playerCamera.transform.localRotation = originalCameraLocalRot;
        if (cameraHolder != null) cameraHolder.localRotation = Quaternion.identity;
    }

    public static void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
