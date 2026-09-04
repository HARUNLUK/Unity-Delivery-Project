using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class DrivableVehicle : MonoBehaviour
{
    [Header("--- IDENTITY & CATALOG ---")]
    [Tooltip("Unique ID for saving purchase state (e.g. pickup_truck, cargo_van)")]
    public string vehicleId = "pickup_truck";

    [Tooltip("Display name shown in UI, Tablet and HUD")]
    public string vehicleName = "Pickup Truck";

    [TextArea(2, 4)]
    [Tooltip("Short description of vehicle capabilities")]
    public string description = "Başlangıç seviyesi çevik ve pratik kargo aracı.";

    [Header("--- ECONOMY & REQUIREMENTS ---")]
    [Tooltip("Purchase price in TL/USD. Set to 0 for free/starter vehicle")]
    public int purchasePrice = 0;

    [Tooltip("Required player level to purchase")]
    public int requiredPlayerLevel = 1;

    [Tooltip("Cargo package storage capacity")]
    public int cargoCapacity = 8;

    [Tooltip("If checked, this vehicle is immediately unlocked from the start (e.g. Pickup Truck)")]
    public bool isUnlockedByDefault = false;

    [Header("--- SPOTS & ANCHORS ---")]
    [Tooltip("Camera position & view when driving inside the cabin")]
    public Transform driverSeatPoint;

    [Tooltip("Spawn point outside the driver door when getting out")]
    public Transform exitPoint;

    [Tooltip("Default showroom/parking spot where vehicle sits")]
    public Transform parkingSpotTransform;

    [Tooltip("Warehouse garage spawn spot when recalled/spawned")]
    public Transform garageSpawnTransform;

    [Header("--- INTERACTION & COMPONENTS ---")]
    [Tooltip("Maximum distance to enter the driver seat")]
    public float enterDistance = 3.0f;
    public CarController carController;
    public VehicleTailgate rearTailgate;

    [Header("--- CURRENT STATE ---")]
    public bool isPlayerInside = false;
    public FPSPlayerController currentPlayer;

    public static event Action<DrivableVehicle> OnVehiclePurchased;
    public static event Action<DrivableVehicle> OnVehicleRecalled;
    public static event Action OnAnyVehicleReset;

    public const string SAVE_PREFIX = "DELIVERY_VEHICLE_UNLOCKED_";

    private Rigidbody rb;
    private float enterTimestamp = 0f;

    /// <summary>
    /// Returns unique ID for saving. Prevents copy-pasted 'pickup_truck' ID on other vehicles.
    /// </summary>
    public string EffectiveVehicleId
    {
        get
        {
            if (string.IsNullOrEmpty(vehicleId) || (vehicleId == "pickup_truck" && !gameObject.name.ToLower().Contains("pickup")))
            {
                return gameObject.name.ToLower().Replace(" ", "_").Replace("(clone)", "").Trim();
            }
            return vehicleId.ToLower().Trim();
        }
    }

    /// <summary>
    /// Returns true if vehicle is free (0 TL) or was purchased in PlayerPrefs.
    /// If purchasePrice > 0, it ALWAYS requires purchase from PlayerPrefs.
    /// </summary>
    public bool IsUnlocked
    {
        get
        {
            if (purchasePrice <= 0)
            {
                return true;
            }

            return PlayerPrefs.GetInt(SAVE_PREFIX + EffectiveVehicleId, 0) == 1;
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (carController == null) carController = GetComponent<CarController>();
        if (rearTailgate == null) rearTailgate = GetComponentInChildren<VehicleTailgate>();

        // Remove old asset pack script if still attached
        CarControl oldScript = GetComponent<CarControl>();
        if (oldScript != null) Destroy(oldScript);

        // Her aracın kendine ait benzersiz ID ve isme sahip olmasını garantiye al
        if (string.IsNullOrEmpty(vehicleId) || (vehicleId == "pickup_truck" && !gameObject.name.ToLower().Contains("pickup")))
        {
            vehicleId = gameObject.name.ToLower().Replace(" ", "_").Replace("(clone)", "").Trim();
        }
        if (string.IsNullOrEmpty(vehicleName) || (vehicleName == "Pickup Truck" && !gameObject.name.ToLower().Contains("pickup")))
        {
            vehicleName = gameObject.name.Replace("(Clone)", "").Trim();
        }

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

    /// <summary>
    /// Checks level and wallet balance, buys and permanently unlocks vehicle.
    /// </summary>
    public bool TryPurchase()
    {
        if (IsUnlocked) return true;

        int currentLvl = PlayerProgressionManager.Instance != null ? PlayerProgressionManager.Instance.PlayerLevel : 1;
        if (currentLvl < requiredPlayerLevel)
        {
            Debug.LogWarning($"[DrivableVehicle] Level yetersiz! Gereken: {requiredPlayerLevel}, Mevcut: {currentLvl}");
            return false;
        }

        int balance = PlayerEconomyManager.Instance != null ? PlayerEconomyManager.Instance.CurrentLiveBalance : 0;
        if (balance < purchasePrice)
        {
            Debug.LogWarning($"[DrivableVehicle] Bakiye yetersiz! Gereken: {purchasePrice}, Mevcut: {balance}");
            return false;
        }

        // Deduct money
        if (PlayerEconomyManager.Instance != null && purchasePrice > 0)
        {
            PlayerEconomyManager.Instance.DeductCash(purchasePrice);
        }

        // Save unlock state
        PlayerPrefs.SetInt(SAVE_PREFIX + EffectiveVehicleId, 1);
        PlayerPrefs.Save();

        Debug.Log($"<color=#32FF64>★ TEBRİKLER! '{vehicleName}' ({purchasePrice} TL) başarıyla satın alındı ve kilidi açıldı! ★</color>");

        OnVehiclePurchased?.Invoke(this);
        return true;
    }

    /// <summary>
    /// Forces vehicle unlock (e.g. dev tool or cheat)
    /// </summary>
    public void ForceUnlock()
    {
        PlayerPrefs.SetInt(SAVE_PREFIX + vehicleId, 1);
        PlayerPrefs.Save();
        OnVehiclePurchased?.Invoke(this);
    }

    /// <summary>
    /// Resets vehicle back to lock state (dev tool)
    /// </summary>
    public void ResetLockState()
    {
        PlayerPrefs.DeleteKey(SAVE_PREFIX + vehicleId);
        PlayerPrefs.DeleteKey(SAVE_PREFIX + gameObject.name.ToLower().Replace(" ", "_"));
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Static global reset helper: Wipes all vehicle purchase keys from PlayerPrefs.
    /// </summary>
    public static void ResetAllVehiclesInGame()
    {
        DrivableVehicle[] allVehicles = UnityEngine.Object.FindObjectsByType<DrivableVehicle>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var v in allVehicles)
        {
            if (v != null)
            {
                v.ResetLockState();
            }
        }

        // Additional common keys wipe
        PlayerPrefs.DeleteKey(SAVE_PREFIX + "pickup_truck");
        PlayerPrefs.DeleteKey(SAVE_PREFIX + "drivable_pickup");
        PlayerPrefs.DeleteKey(SAVE_PREFIX + "cargo_van");
        PlayerPrefs.DeleteKey(SAVE_PREFIX + "drivable_van");
        PlayerPrefs.DeleteKey(SAVE_PREFIX + "driveable_van");
        PlayerPrefs.DeleteKey(SAVE_PREFIX + "cargo_van_01");
        PlayerPrefs.Save();

        OnAnyVehicleReset?.Invoke();
        Debug.Log("<color=#FF3333>★★★ [DEV] F9 TUŞUNA BASILDI: TÜM ARAÇ SATIN ALIMLARI SIFIRLANDI! ★★★</color>");
    }

    /// <summary>
    /// Recalls vehicle safely to the designated garage spawn point or parking point.
    /// </summary>
    public void RecallToGarage()
    {
        Transform targetAnchor = garageSpawnTransform != null ? garageSpawnTransform : parkingSpotTransform;
        if (targetAnchor == null && VehicleShowroomManager.Instance != null)
        {
            targetAnchor = VehicleShowroomManager.Instance.warehouseGarageSpawnPoint;
        }

        if (targetAnchor == null)
        {
            Debug.LogWarning($"[DrivableVehicle] '{vehicleName}' için tanımlı Garage veya Parking noktası bulunamadı!");
            return;
        }

        if (isPlayerInside)
        {
            ExitVehicle();
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        transform.position = targetAnchor.position + Vector3.up * 0.35f;
        transform.rotation = targetAnchor.rotation;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Debug.Log($"[DrivableVehicle] '{vehicleName}' garaj noktasına ({targetAnchor.name}) ışınlandı.");
        OnVehicleRecalled?.Invoke(this);
    }

    private void Update()
    {
        if (isPlayerInside)
        {
            // Debounce to prevent immediate exit on the frame of entry
            if (Time.time - enterTimestamp > 0.35f)
            {
                bool exitPressed = false;
#if ENABLE_INPUT_SYSTEM
                if (Keyboard.current != null && (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.fKey.wasPressedThisFrame))
                {
                    exitPressed = true;
                }
#endif
                try
                {
                    if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F))
                    {
                        exitPressed = true;
                    }
                }
                catch { }

                if (exitPressed)
                {
                    ExitVehicle();
                }
            }
        }
    }

    public void EnterVehicle(FPSPlayerController player)
    {
        if (isPlayerInside || player == null) return;

        if (!IsUnlocked)
        {
            Debug.LogWarning($"[DrivableVehicle] '{vehicleName}' kilitli! Satın almadan binilemez.");
            return;
        }

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
            InteractionPromptHUD.Instance.ShowPrompt("[E] In  |  [V] Kamera Değiştir");
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

        if (InteractionPromptHUD.Instance != null)
        {
            InteractionPromptHUD.Instance.HidePrompt();
        }

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
}
