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

        // 1. Disable player on-foot movement
        player.SetOnFootActive(false);

        // 2. Attach player camera to driver seat
        player.AttachCameraToSeat(driverSeatPoint);

        // 3. Enable vehicle controls
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

        // 1. Disable vehicle controls & clear forces
        if (carController != null)
        {
            carController.enabled = false;
        }

        // 2. Position player at exit point
        Vector3 spawnPos = exitPoint != null ? exitPoint.position : transform.position - transform.right * 2.0f;
        spawnPos.y += 0.2f;

        currentPlayer.transform.position = spawnPos;
        currentPlayer.transform.rotation = Quaternion.LookRotation(transform.forward, Vector3.up);

        // 3. Detach camera and restore on-foot player
        currentPlayer.DetachCameraFromSeat();
        currentPlayer.SetOnFootActive(true);

        isPlayerInside = false;
        currentPlayer = null;

        Debug.Log($"[DrivableVehicle] Player exited '{vehicleName}'. On-foot controls restored.");
    }

    private void Update()
    {
        if (isPlayerInside)
        {
            // Press E to exit vehicle
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                ExitVehicle();
            }
        }
    }
}
