using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class DrivableVehicle : MonoBehaviour
{
    [Header("--- VEHICLE NAME & TYPE ---")]
    public string vehicleName = "Pickup Truck";

    [Header("--- SEAT & EXIT ANCHORS ---")]
    [Tooltip("Camera position & view when driving inside the cabin")]
    public Transform driverSeatPoint;

    [Tooltip("Spawn point outside the driver door when getting out")]
    public Transform exitPoint;

    [Header("--- INTERACTION RANGE ---")]
    [Tooltip("Maximum distance to enter the driver seat")]
    public float enterDistance = 3.0f;

    [Header("--- COMPONENTS ---")]
    public CarController carController;
    public VehicleTailgate rearTailgate;

    [Header("--- CURRENT STATE ---")]
    public bool isPlayerInside = false;
    public FPSPlayerController currentPlayer;

    private Rigidbody rb;
    private float enterTimestamp = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (carController == null) carController = GetComponent<CarController>();
        if (rearTailgate == null) rearTailgate = GetComponentInChildren<VehicleTailgate>();

        // Remove old asset pack script if still attached
        CarControl oldScript = GetComponent<CarControl>();
        if (oldScript != null) Destroy(oldScript);

        EnsureAnchors();
    }

    private void Start()
    {
        // Guarantee vehicle controls are OFF on game start
        isPlayerInside = false;
        if (carController != null)
        {
            carController.enabled = false;
        }
    }

    private void EnsureAnchors()
    {
        if (driverSeatPoint == null)
        {
            Transform existingSeat = transform.Find("DriverSeatPoint");
            if (existingSeat != null)
            {
                driverSeatPoint = existingSeat;
            }
            else
            {
                GameObject seatObj = new GameObject("DriverSeatPoint");
                seatObj.transform.SetParent(transform);
                seatObj.transform.localPosition = new Vector3(-0.45f, 1.35f, 0.15f);
                seatObj.transform.localRotation = Quaternion.identity;
                driverSeatPoint = seatObj.transform;
            }
        }

        if (exitPoint == null)
        {
            Transform existingExit = transform.Find("ExitPoint");
            if (existingExit != null)
            {
                exitPoint = existingExit;
            }
            else
            {
                GameObject exitObj = new GameObject("ExitPoint");
                exitObj.transform.SetParent(transform);
                exitObj.transform.localPosition = new Vector3(-1.8f, 0.2f, 0.2f);
                exitObj.transform.localRotation = Quaternion.identity;
                exitPoint = exitObj.transform;
            }
        }
    }

    public void EnterVehicle(FPSPlayerController player)
    {
        if (isPlayerInside || player == null) return;

        currentPlayer = player;
        isPlayerInside = true;
        enterTimestamp = Time.time;

        // 1. Disable player on-foot movement and CharacterController
        player.SetOnFootActive(false);

        // 2. Parent player object to driver seat so it travels with the car
        player.transform.SetParent(driverSeatPoint);
        player.transform.localPosition = Vector3.zero;
        player.transform.localRotation = Quaternion.identity;

        // 3. Attach player camera to driver seat
        player.AttachCameraToSeat(driverSeatPoint);

        // 4. Enable vehicle controls
        if (carController != null)
        {
            carController.enabled = true;
        }

        if (InteractionPromptHUD.Instance != null)
        {
            InteractionPromptHUD.Instance.HidePrompt();
        }

        Debug.Log($"[DrivableVehicle] Player entered '{vehicleName}'. Press [E] to exit.");
    }

    public void ExitVehicle()
    {
        if (!isPlayerInside || currentPlayer == null) return;

        // 1. Cut all engine torque, center steer and apply neutral coasting deceleration
        if (carController != null)
        {
            carController.ClearAllForces();
            carController.enabled = false;
        }

        // 2. Calculate exit position outside driver door
        Vector3 spawnPos = exitPoint != null ? exitPoint.position : transform.position - (transform.right * 2.0f);
        spawnPos.y += 0.1f;

        // 3. Unparent player from vehicle
        currentPlayer.transform.SetParent(null);
        currentPlayer.transform.position = spawnPos;
        currentPlayer.transform.rotation = Quaternion.LookRotation(transform.forward, Vector3.up);

        // 4. Detach camera and restore on-foot player
        currentPlayer.DetachCameraFromSeat();
        currentPlayer.SetOnFootActive(true);

        isPlayerInside = false;
        currentPlayer = null;

        Debug.Log($"[DrivableVehicle] Player exited '{vehicleName}'. On-foot controls restored.");
    }

    private void FixedUpdate()
    {
        // Araçta kimse yokken boşa çıkmış gibi yumuşakça yavaşlayarak park eder
        if (!isPlayerInside && rb != null)
        {
            if (rb.linearVelocity.magnitude > 0.05f)
            {
                rb.linearVelocity = Vector3.MoveTowards(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 2.5f);
                rb.angularVelocity = Vector3.MoveTowards(rb.angularVelocity, Vector3.zero, Time.fixedDeltaTime * 4.0f);
            }
        }
    }

    private void Update()
    {
        if (isPlayerInside)
        {
            // Ignore [E] keypress in the same frame as entering (0.3s cooldown)
            if (Time.time - enterTimestamp > 0.35f)
            {
                if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                {
                    ExitVehicle();
                }
            }
        }
    }
}
