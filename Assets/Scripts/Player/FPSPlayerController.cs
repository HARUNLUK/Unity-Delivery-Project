using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FPSPlayerController : MonoBehaviour
{
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
    [Tooltip("Maksimum fırlatma hızı")]
    public float maxThrowForce = 11.0f;
    [Tooltip("Tam güçte fırlatma için basılı tutma süresi (saniye)")]
    public float throwChargeDuration = 0.85f;

    private float vehicleYaw = 0f;
    private float vehiclePitch = 0f;
    private float tpsYawOffset = 0f;
    private float tpsPitchOffset = 0f;
    private Transform currentSeatPoint;
    private Transform currentVehicleTransform;

    private float currentDropHoldTime = 0f;
    private float afterGrabSafetyTimer = 0f;

    private void Awake()
    {
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

    private void Update()
    {
        if (!isOnFoot)
        {
            // V tuşu ile FPS (İç görünüm) ve TPS (Dış takip) arasında geçiş yap
            if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
            {
                ToggleVehicleCameraMode();
            }

            if (vehicleCameraMode == VehicleCameraMode.FirstPerson)
            {
                HandleInVehicleLook();
            }
            else
            {
                HandleTPSOrbitInput();
            }
            return;
        }

        // F9 Dev Reset for Vehicle Purchases
        if (CheckF9DevInput())
        {
            DrivableVehicle.ResetAllVehiclesInGame();
            if (InteractionPromptHUD.Instance != null)
            {
                InteractionPromptHUD.Instance.ShowPrompt("<color=#FF5555>★ TÜM ARAÇ SATIN ALIMLARI SIFIRLANDI (F9) ★</color>");
            }
            if (CargoTabletUI.Instance != null && CargoTabletUI.Instance.IsTabletOpen)
            {
                CargoTabletUI.Instance.PopulateVehicleList();
            }
        }

        HandleMouseLook();
        HandleMovement();
        HandleInteraction();
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
                playerCamera.transform.localPosition = Vector3.zero;
                playerCamera.transform.localRotation = Quaternion.identity;
                vehicleYaw = 0f;
                vehiclePitch = 0f;
            }
        }
    }

    private void HandleInVehicleLook()
    {
        if (playerCamera == null) return;

        float mouseX = 0f;
        float mouseY = 0f;

        if (Mouse.current != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue() * (inVehicleMouseSensitivity * 0.08f);
            mouseX = delta.x;
            mouseY = delta.y;
        }

        vehicleYaw += mouseX;
        vehicleYaw = Mathf.Clamp(vehicleYaw, -inVehicleMaxYaw, inVehicleMaxYaw);

        vehiclePitch -= mouseY;
        vehiclePitch = Mathf.Clamp(vehiclePitch, inVehicleMinPitch, inVehicleMaxPitch);

        playerCamera.transform.localRotation = Quaternion.Euler(vehiclePitch, vehicleYaw, 0f);
    }

    private void HandleTPSOrbitInput()
    {
        float mouseX = 0f;
        float mouseY = 0f;

        if (Mouse.current != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue() * (inVehicleMouseSensitivity * 0.08f);
            mouseX = delta.x;
            mouseY = delta.y;
        }

        tpsYawOffset += mouseX;
        tpsYawOffset = Mathf.Clamp(tpsYawOffset, -90f, 90f);

        tpsPitchOffset -= mouseY;
        tpsPitchOffset = Mathf.Clamp(tpsPitchOffset, -20f, 35f);

        // Fare bırakıldığında arkaya doğru yumuşak toparlanma
        if (Mathf.Abs(mouseX) < 0.01f)
        {
            tpsYawOffset = Mathf.MoveTowards(tpsYawOffset, 0f, Time.deltaTime * 35f);
        }
    }

    private void UpdateTPSCameraPosition()
    {
        if (playerCamera == null || currentVehicleTransform == null) return;

        if (playerCamera.transform.parent != null)
        {
            playerCamera.transform.SetParent(null);
        }

        float wantedRotationAngle = currentVehicleTransform.eulerAngles.y + tpsYawOffset;
        float wantedHeight = currentVehicleTransform.position.y + tpsHeight;

        float currentRotationAngle = playerCamera.transform.eulerAngles.y;
        float currentHeight = playerCamera.transform.position.y;

        currentRotationAngle = Mathf.LerpAngle(currentRotationAngle, wantedRotationAngle, tpsRotationDamping * Time.deltaTime);
        currentHeight = Mathf.Lerp(currentHeight, wantedHeight, tpsHeightDamping * Time.deltaTime);

        Quaternion currentRotation = Quaternion.Euler(tpsPitchOffset, currentRotationAngle, 0f);

        Vector3 targetPos = currentVehicleTransform.position - (currentRotation * Vector3.forward * tpsDistance);
        targetPos.y = currentHeight;

        playerCamera.transform.position = targetPos;
        playerCamera.transform.LookAt(currentVehicleTransform.position + Vector3.up * tpsLookAtHeight);
    }

    private void HandleMouseLook()
    {
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

        if (Keyboard.current != null)
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
                    // Şarjlı fırlatma
                    float charge = Mathf.Clamp01(currentDropHoldTime / throwChargeDuration);
                    float throwSpeed = Mathf.Lerp(4.5f, maxThrowForce, charge);
                    Vector3 throwVel = (playerCamera.transform.forward * throwSpeed) + (Vector3.up * 1.5f);
                    grabber.ReleaseObject(throwVel);
                }
                else
                {
                    // Nazikçe yere bırakma
                    grabber.ReleaseObject(Vector3.zero);
                }

                currentDropHoldTime = 0f;
            }

            return;
        }

        if (playerCamera == null) return;

        // Raycast forward with fallback SphereCast (ignoring invisible trigger zones)
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        bool hasHit = Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactionLayers, QueryTriggerInteraction.Ignore);
        if (!hasHit)
        {
            hasHit = Physics.SphereCast(ray, 0.25f, out hit, interactionDistance, interactionLayers, QueryTriggerInteraction.Ignore);
        }

        // 1. Physical Cargo Package or Rigidbody Object Detection
        PhysicalCargoPackage pkg = null;
        Rigidbody targetRb = null;

        if (hasHit)
        {
            pkg = hit.collider.GetComponentInParent<PhysicalCargoPackage>();
            if (pkg == null) pkg = hit.collider.GetComponent<PhysicalCargoPackage>();
            if (pkg == null) pkg = hit.collider.GetComponentInChildren<PhysicalCargoPackage>();

            targetRb = hit.collider.attachedRigidbody;
        }

        // Yakın mesafe taraması (Kutunun dibinde durup aşağı bakarken kesin algılama)
        if (pkg == null && targetRb == null)
        {
            Collider[] closeHits = Physics.OverlapSphere(playerCamera.transform.position + (playerCamera.transform.forward * 1.2f), 0.85f, interactionLayers, QueryTriggerInteraction.Ignore);
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

        if (pkg != null || (targetRb != null && !targetRb.isKinematic && (hasHit ? hit.collider.GetComponentInParent<DrivableVehicle>() == null : true)))
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

        if (hasHit)
        {
            // 2. Direct hit on VehicleTailgate collider
            VehicleTailgate directTailgate = hit.collider.GetComponent<VehicleTailgate>();
            if (directTailgate == null) directTailgate = hit.collider.GetComponentInParent<VehicleTailgate>();

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

            // 3. Hit Vehicle - Check lock & ownership or drive
            DrivableVehicle vehicle = hit.collider.GetComponentInParent<DrivableVehicle>();
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
                            InteractionPromptHUD.Instance.ShowPrompt($"<color=#FF5555>🔒 [KİLİTLİ] {vehicle.vehicleName}</color> (Seviye {vehicle.requiredPlayerLevel} Gerekli - ${vehicle.purchasePrice} TL)");
                        }
                    }
                    else if (currentBalance < vehicle.purchasePrice)
                    {
                        if (InteractionPromptHUD.Instance != null)
                        {
                            InteractionPromptHUD.Instance.ShowPrompt($"<color=#FFAA33>🔒 [KİLİTLİ] {vehicle.vehicleName}</color> (${vehicle.purchasePrice} TL - Bakiye: ${currentBalance} TL)");
                        }
                    }
                    else
                    {
                        if (InteractionPromptHUD.Instance != null)
                        {
                            InteractionPromptHUD.Instance.ShowPrompt($"<color=#32FF64>[E] Satın Al: {vehicle.vehicleName}</color> (${vehicle.purchasePrice} TL)");
                        }

                        if (interactPressed)
                        {
                            bool bought = vehicle.TryPurchase();
                            if (bought && InteractionPromptHUD.Instance != null)
                            {
                                InteractionPromptHUD.Instance.ShowPrompt($"<color=#32FFFF>★ {vehicle.vehicleName} Satın Alındı! ★</color>");
                            }
                        }
                    }
                    return;
                }

                float distToTailgate = float.MaxValue;
                if (vehicle.rearTailgate != null)
                {
                    distToTailgate = Vector3.Distance(hit.point, vehicle.rearTailgate.transform.position);
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

                // Looking at the car cabin/body prompts driving
                if (InteractionPromptHUD.Instance != null)
                {
                    InteractionPromptHUD.Instance.ShowPrompt($"[E] Drive {vehicle.vehicleName}");
                }

                if (interactPressed)
                {
                    vehicle.EnterVehicle(this);
                }
                return;
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
        DrivableVehicle vehicle = seatPoint.GetComponentInParent<DrivableVehicle>();
        currentVehicleTransform = vehicle != null ? vehicle.transform : seatPoint.root;

        vehicleCameraMode = VehicleCameraMode.FirstPerson;
        vehicleYaw = 0f;
        vehiclePitch = 0f;
        tpsYawOffset = 0f;
        tpsPitchOffset = 0f;

        playerCamera.transform.SetParent(seatPoint);
        playerCamera.transform.localPosition = Vector3.zero;
        playerCamera.transform.localRotation = Quaternion.identity;
    }

    public void DetachCameraFromSeat()
    {
        if (playerCamera == null) return;

        vehicleCameraMode = VehicleCameraMode.FirstPerson;
        currentSeatPoint = null;
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
