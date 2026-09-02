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
        if (!isOnFoot) return;

        HandleMouseLook();
        HandleMovement();
        HandleInteraction();
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
            if (InteractionPromptHUD.Instance != null)
            {
                InteractionPromptHUD.Instance.ShowPrompt("[E] or [LMB] Drop Cargo");
            }

            if (interactPressed || leftClickPressed)
            {
                grabber.ReleaseObject();
            }
            return;
        }

        if (playerCamera == null) return;

        // Raycast forward with fallback SphereCast for rock-solid interaction detection (zero flickering)
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        bool hasHit = Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactionLayers, QueryTriggerInteraction.Collide);
        if (!hasHit)
        {
            hasHit = Physics.SphereCast(ray, 0.20f, out hit, interactionDistance, interactionLayers, QueryTriggerInteraction.Collide);
        }

        if (hasHit)
        {
            // 1. Physical Cargo Package or Rigidbody Object
            PhysicalCargoPackage pkg = hit.collider.GetComponentInParent<PhysicalCargoPackage>();
            if (pkg == null) pkg = hit.collider.GetComponent<PhysicalCargoPackage>();
            if (pkg == null) pkg = hit.collider.GetComponentInChildren<PhysicalCargoPackage>();

            Rigidbody targetRb = hit.collider.attachedRigidbody;

            if (pkg != null || (targetRb != null && !targetRb.isKinematic && hit.collider.GetComponentInParent<DrivableVehicle>() == null))
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
                    }
                }
                return;
            }

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

            // 3. Hit Vehicle - Prompt Drive everywhere except rear tailgate area
            DrivableVehicle vehicle = hit.collider.GetComponentInParent<DrivableVehicle>();
            if (vehicle != null && !vehicle.isPlayerInside)
            {
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

        playerCamera.transform.SetParent(seatPoint);
        playerCamera.transform.localPosition = Vector3.zero;
        playerCamera.transform.localRotation = Quaternion.identity;
    }

    public void DetachCameraFromSeat()
    {
        if (playerCamera == null) return;

        playerCamera.transform.SetParent(cameraHolder != null ? cameraHolder : transform);
        playerCamera.transform.localPosition = originalCameraLocalPos;
        playerCamera.transform.localRotation = originalCameraLocalRot;
    }

    public static void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
