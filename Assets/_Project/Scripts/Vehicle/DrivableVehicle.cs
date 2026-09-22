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
    public string description = "Agile and practical entry-level cargo vehicle.";

    [Header("--- ECONOMY & REQUIREMENTS ---")]
    [Tooltip("Purchase price in TL/USD. Set to 0 for free/starter vehicle")]
    public int purchasePrice = 0;

    [Tooltip("Required player level to purchase")]
    public int requiredPlayerLevel = 1;

    [Tooltip("Cargo package storage capacity")]
    public int cargoCapacity = 8;

    [Tooltip("If checked, this vehicle is immediately unlocked from the start (e.g. Pickup Truck)")]
    public bool isUnlockedByDefault = false;

    [Tooltip("Custom fee ($) to recall this vehicle to warehouse garage. Set to -1 to use default fee from VehicleShowroomManager.")]
    public int customRecallFee = -1;

    /// <summary>
    /// Returns the effective recall fee for this vehicle (custom if specified >= 0, otherwise showroom global default).
    /// </summary>
    public int GetRecallFee()
    {
        if (customRecallFee >= 0) return customRecallFee;
        if (VehicleShowroomManager.Instance != null) return VehicleShowroomManager.Instance.defaultRecallFee;
        return 150;
    }

    [Header("--- FUEL & CONSUMPTION ---")]
    [Tooltip("Maximum fuel capacity in Litres")]
    public float maxFuel = 50f;

    [Tooltip("Current fuel in Litres")]
    public float currentFuel = 50f;

    [Tooltip("Fuel consumed per second while driving/accelerating (Litres/sec)")]
    public float fuelBurnRate = 0.06f; // ~3.6 Litres/minute of driving

    [Tooltip("Fuel consumed per second while idling (Litres/sec)")]
    public float idleFuelBurnRate = 0.008f;

    [Header("--- CONDITION & DAMAGE (KONDİSYON VE HASAR) ---")]
    [Tooltip("Maximum vehicle durability / health (100 = Factory New)")]
    public float maxCondition = 100f;

    [Tooltip("Current condition of the vehicle")]
    public float currentCondition = 100f;

    [Tooltip("Condition degradation per second while driving/moving (e.g. 0.015 = 1% per ~66 seconds)")]
    public float drivingWearRate = 0.015f;

    [Tooltip("Condition degradation per second while idling")]
    public float idleWearRate = 0.002f;

    [Tooltip("Minimum collision relative velocity in m/s required to inflict crash damage (e.g. 6.0 m/s ~= 21.6 km/h)")]
    public float minCollisionSpeed = 6.0f;

    [Tooltip("Collision impact damage scaling multiplier")]
    public float collisionDamageMultiplier = 1.2f;

    [Tooltip("Cooldown between taking collision damage hits to prevent multi-contact frame spam")]
    public float collisionDamageCooldown = 0.35f;

    private float lastCollisionDamageTime = 0f;

    public const string CONDITION_SAVE_PREFIX = "DELIVERY_VEHICLE_COND_";

    public float ConditionPercentage => maxCondition > 0 ? Mathf.Clamp01(currentCondition / maxCondition) : 0f;

    [Header("--- DRIVER VISUAL / AVATAR ---")]
    [Tooltip("Göstermelik sürücü karakter modeli / objesi (DriverSeatPoint altına konulabilir veya otomatik 'DriverVisual' ismiyle bulunur)")]
    public GameObject driverVisualObject;

    [Tooltip("İç (FPS) kamera modundayken sürücü modelini gizle (kafa görüşü kapatmasın)")]
    public bool hideDriverInFPS = true;

    [Header("--- SPOTS & CAMERA ANCHORS ---")]
    [Tooltip("In-vehicle FPS driver camera anchor point (Transform). If left empty, DriverSeatPoint is searched automatically inside the vehicle.")]
    public Transform driverSeatPoint;

    [Tooltip("Additional local position offset for in-vehicle FPS camera (X=Right/Left, Y=Up/Down, Z=Forward/Back)")]
    public Vector3 fpsCameraOffset = Vector3.zero;

    [Header("--- TPS CHASE CAMERA (VEHICLE-SPECIFIC THIRD PERSON CAMERA) ---")]
    [Tooltip("Optional: Reference anchor point for the rear TPS camera (If empty, vehicle root is used)")]
    public Transform tpsCameraPoint;

    [Tooltip("Chase follow distance for this vehicle (e.g. Small car: 5.0, Pickup: 6.5, Truck: 8.0)")]
    public float tpsDistance = 6.0f;

    [Tooltip("Camera height for this vehicle (e.g. 2.0)")]
    public float tpsHeight = 2.0f;

    [Tooltip("Target look-at height / pivot for the camera (e.g. 1.2)")]
    public float tpsLookAtHeight = 1.2f;

    [Header("--- SPAWN & PARKING ANCHORS ---")]
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
    public VehicleCargoBed cargoBed;

    [Header("--- CURRENT STATE ---")]
    public bool isPlayerInside = false;
    public FPSPlayerController currentPlayer;

    [Header("--- VEHICLE PAINT & CUSTOMIZATION (BOYA & RENK MODİFİYESİ) ---")]
    [Tooltip("Target mesh renderers and material slots affected by paint customizations")]
    public System.Collections.Generic.List<PaintablePartTarget> paintableTargets = new System.Collections.Generic.List<PaintablePartTarget>();

    [Tooltip("Default factory vehicle color (Vibrant Orange #E8710A)")]
    public Color defaultVehicleColor = new Color(0.9098f, 0.4431f, 0.0392f, 1f);

    public const string COLOR_SAVE_PREFIX = "DELIVERY_VEHICLE_COLOR_";

    public static event Action<DrivableVehicle, Color> OnVehicleColorChanged;
    public static event Action<DrivableVehicle> OnVehiclePurchased;
    public static event Action<DrivableVehicle> OnVehicleRecalled;
    public static event Action OnAnyVehicleReset;

    public static readonly System.Collections.Generic.List<DrivableVehicle> AllDrivableVehicles = new System.Collections.Generic.List<DrivableVehicle>();

    public const string SAVE_PREFIX = "DELIVERY_VEHICLE_UNLOCKED_";
    public const string FUEL_SAVE_PREFIX = "DELIVERY_VEHICLE_FUEL_";

    private Rigidbody rb;
    private float enterTimestamp = 0f;
    private bool wasInGarageBayLastFrame = false;

    private void OnEnable()
    {
        if (!AllDrivableVehicles.Contains(this))
        {
            AllDrivableVehicles.Add(this);
        }
    }

    private void OnDisable()
    {
        AllDrivableVehicles.Remove(this);
        SaveVehicleState();
    }

    private void OnApplicationQuit()
    {
        SaveVehicleState();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveVehicleState();
        }
    }

    public void SaveVehicleState()
    {
        if (string.IsNullOrEmpty(EffectiveVehicleId)) return;
        PlayerPrefs.SetFloat(FUEL_SAVE_PREFIX + EffectiveVehicleId, currentFuel);
        PlayerPrefs.SetFloat(CONDITION_SAVE_PREFIX + EffectiveVehicleId, currentCondition);
        PlayerPrefs.Save();
    }

    public float FuelPercentage => maxFuel > 0 ? Mathf.Clamp01(currentFuel / maxFuel) : 0f;
    public bool HasFuel => currentFuel > 0.05f;

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
        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.maxDepenetrationVelocity = 6.0f;
        }

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
        LoadSavedColor();
    }

    private void Start()
    {
        // Guarantee vehicle controls are OFF on game start
        isPlayerInside = false;
        if (carController != null)
        {
            carController.enabled = false;
        }

        // Load saved fuel, condition & color
        currentFuel = PlayerPrefs.GetFloat(FUEL_SAVE_PREFIX + EffectiveVehicleId, maxFuel);
        currentFuel = Mathf.Clamp(currentFuel, 0f, maxFuel);

        currentCondition = PlayerPrefs.GetFloat(CONDITION_SAVE_PREFIX + EffectiveVehicleId, maxCondition);
        currentCondition = Mathf.Clamp(currentCondition, 0f, maxCondition);

        LoadSavedColor();

        if (carController != null)
        {
            carController.SetConditionRatio(ConditionPercentage);
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

        if (driverVisualObject == null)
        {
            driverVisualObject = FindDriverVisual();
        }

        if (driverVisualObject != null)
        {
            // Sürücü modeli saf görsel olduğu için araç fiziklerini etkilememesi adına collider'larını devre dışı bırak
            Collider[] cols = driverVisualObject.GetComponentsInChildren<Collider>(true);
            foreach (var col in cols)
            {
                if (col != null) col.enabled = false;
            }

            if (!isPlayerInside)
            {
                driverVisualObject.SetActive(false);
            }
        }

        if (cargoBed == null)
        {
            cargoBed = GetComponent<VehicleCargoBed>();
            if (cargoBed == null) cargoBed = gameObject.AddComponent<VehicleCargoBed>();
        }
        if (cargoBed != null)
        {
            cargoBed.EnsureBedTrigger();
        }
    }

    private void Update()
    {
        if (isPlayerInside)
        {
            // 1. Fuel & Condition Degradation
            HandleFuelAndCondition();

            // Check if vehicle is inside the Auto Service Garage bay
            bool inGarage = VehicleServiceGarage.Instance != null &&
                            VehicleServiceGarage.Instance.IsGarageUnlocked() &&
                            VehicleServiceGarage.Instance.IsVehicleInServiceBay(this);

            bool isUIOpen = CommercialHubUIManager.Instance != null && CommercialHubUIManager.Instance.IsAnyPanelOpen;

            if (inGarage)
            {
                if (!wasInGarageBayLastFrame && InteractionPromptHUD.Instance != null && !isUIOpen)
                {
                    InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.Get("prompt_open_menu"), 2.0f);
                }

                VehicleServiceGarage.Instance.CheckGarageShortcutInputs(this);

                bool fPressed = false;
#if ENABLE_INPUT_SYSTEM
                if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame) fPressed = true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                try { if (Input.GetKeyDown(KeyCode.F)) fPressed = true; } catch { }
#endif

                if (fPressed && CommercialHubUIManager.Instance != null)
                {
                    if (isUIOpen)
                    {
                        CommercialHubUIManager.Instance.CloseAllPanels();
                    }
                    else
                    {
                        CommercialHubUIManager.Instance.OpenGarageWorkshopPanel(this);
                    }
                }
            }
            else
            {
                if (wasInGarageBayLastFrame && InteractionPromptHUD.Instance != null)
                {
                    InteractionPromptHUD.Instance.HidePrompt();
                }
            }

            wasInGarageBayLastFrame = inGarage;

            // 2. Debounce to prevent immediate exit on the frame of entry
            if (!isUIOpen && Time.time - enterTimestamp > 0.35f)
            {
                bool exitPressed = KeyBindingManager.WasPressedThisFrame(GameAction.Interact) || KeyBindingManager.WasPressedThisFrame(GameAction.Vehicle);

                if (exitPressed)
                {
                    ExitVehicle();
                }
            }
        }
    }

    private void HandleFuelAndCondition()
    {
        if (carController == null) return;

        bool isDriving = Mathf.Abs(carController.verticalInput) > 0.1f || (rb != null && rb.linearVelocity.magnitude > 0.5f);

        // 1. Fuel
        if (HasFuel)
        {
            float burn = isDriving ? fuelBurnRate : idleFuelBurnRate;
            currentFuel = Mathf.Max(0f, currentFuel - (burn * Time.deltaTime));

            // Enable car controller if it was turned off due to empty fuel
            if (!carController.enabled && isPlayerInside)
            {
                carController.enabled = true;
            }
        }
        else
        {
            // Yakıt bitti: Motor gücünü kes
            if (carController.enabled)
            {
                carController.ClearAllForces();
                carController.enabled = false;
            }

            if (InteractionPromptHUD.Instance != null)
            {
                InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.Get("prompt_out_of_fuel"));
            }
        }

        // 2. Condition Wear
        float wear = isDriving ? drivingWearRate : idleWearRate;
        currentCondition = Mathf.Max(0f, currentCondition - (wear * Time.deltaTime));

        // CarController'a kondisyon oranını aktar (Maksimum hız ve tork buna göre dinamik kısıtlanır)
        if (carController != null)
        {
            carController.SetConditionRatio(ConditionPercentage);
        }

        // 3. Canlı Dashboard HUD Göstergesi (Ayrı Yakıt & Kondisyon Barları)
        if (InteractionPromptHUD.Instance != null)
        {
            bool isLowFuel = currentFuel < (maxFuel * 0.18f);
            InteractionPromptHUD.Instance.UpdateVehicleHUD(currentFuel, maxFuel, isLowFuel, currentCondition, maxCondition);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null) return;
        if (Time.time - lastCollisionDamageTime < collisionDamageCooldown) return;

        // Ignore collisions with player or child objects
        if (currentPlayer != null && collision.transform.IsChildOf(currentPlayer.transform)) return;

        float impactSpeed = collision.relativeVelocity.magnitude;
        if (rb != null && collision.impulse.magnitude > 0.01f)
        {
            float impulseSpeed = collision.impulse.magnitude / rb.mass;
            if (impulseSpeed > impactSpeed) impactSpeed = impulseSpeed;
        }

        if (impactSpeed > minCollisionSpeed)
        {
            lastCollisionDamageTime = Time.time;
            float excess = impactSpeed - minCollisionSpeed;
            // Progressive damage formula (more balanced and durable)
            float damage = (excess * collisionDamageMultiplier) + (excess * excess * 0.12f);
            damage = Mathf.Clamp(damage, 1f, 30f);

            currentCondition = Mathf.Max(0f, currentCondition - damage);

            // CarController'a yeni kondisyon oranını derhal ilet
            if (carController != null)
            {
                carController.SetConditionRatio(ConditionPercentage);
            }

            // Persist condition after heavy impacts
            PlayerPrefs.SetFloat(CONDITION_SAVE_PREFIX + EffectiveVehicleId, currentCondition);

            Debug.Log($"<color=#FFAA33>[DrivableVehicle] Impact damage: -{damage:F1} (Impact Speed: {impactSpeed:F1} m/s). New Condition: {currentCondition:F1}/{maxCondition}</color>");
        }
    }

    /// <summary>
    /// Applies catastrophic explosion damage to vehicle: zeroes condition completely and puts engine into Limp Mode.
    /// </summary>
    public void ApplyExplosionDirectHit(Vector3 explosionOrigin, float explosionForce = 12000f, float explosionRadius = 8f)
    {
        currentCondition = 0f;

        if (carController != null)
        {
            carController.SetConditionRatio(0f);
        }

        PlayerPrefs.SetFloat(CONDITION_SAVE_PREFIX + EffectiveVehicleId, 0f);
        PlayerPrefs.Save();

        if (rb != null)
        {
            rb.AddExplosionForce(explosionForce, explosionOrigin, explosionRadius, 1.2f, ForceMode.Impulse);
        }

        if (InteractionPromptHUD.Instance != null && isPlayerInside)
        {
            bool isLow = currentFuel < (maxFuel * 0.18f);
            InteractionPromptHUD.Instance.UpdateVehicleHUD(currentFuel, maxFuel, isLow, currentCondition, maxCondition);
        }

        Debug.LogWarning($"<color=#FF2222>[DrivableVehicle] EXPLOSION HIT! Vehicle '{EffectiveVehicleId}' condition set to 0%.</color>");
    }

    /// <summary>
    /// Adds fuel to tank and saves state.
    /// </summary>
    public void Refuel(float liters)
    {
        currentFuel = Mathf.Clamp(currentFuel + liters, 0f, maxFuel);
        PlayerPrefs.SetFloat(FUEL_SAVE_PREFIX + EffectiveVehicleId, currentFuel);
        PlayerPrefs.Save();

        if (carController != null && !carController.enabled && isPlayerInside && HasFuel)
        {
            carController.enabled = true;
        }

        if (InteractionPromptHUD.Instance != null && isPlayerInside)
        {
            bool isLowFuel = currentFuel < (maxFuel * 0.18f);
            InteractionPromptHUD.Instance.UpdateVehicleHUD(currentFuel, maxFuel, isLowFuel, currentCondition, maxCondition);
        }
    }

    /// <summary>
    /// Attempts to purchase and unlock this vehicle using live player balance.
    /// </summary>
    public bool TryPurchase()
    {
        if (IsUnlocked) return true;

        int branchLevel = BranchManager.Instance != null ? BranchManager.Instance.CurrentBranchLevel : (PlayerProgressionManager.Instance != null ? PlayerProgressionManager.Instance.WarehouseLevel : 1);
        int balance = PlayerEconomyManager.Instance != null ? PlayerEconomyManager.Instance.CurrentLiveBalance : 0;

        if (branchLevel < requiredPlayerLevel)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayError();
            }
            Debug.LogWarning($"[DrivableVehicle] Branch Level {branchLevel} is too low. Required Level {requiredPlayerLevel}.");
            return false;
        }

        if (balance < purchasePrice)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayError();
            }
            Debug.LogWarning($"[DrivableVehicle] Insufficient funds (${balance}) to purchase '{vehicleName}' (${purchasePrice}).");
            return false;
        }

        // Deduct money
        if (PlayerEconomyManager.Instance != null && purchasePrice > 0)
        {
            PlayerEconomyManager.Instance.SpendMoney(purchasePrice);
        }

        // Save unlock state
        PlayerPrefs.SetInt(SAVE_PREFIX + EffectiveVehicleId, 1);
        PlayerPrefs.Save();

        Debug.Log($"<color=#32FF64>[PURCHASE SUCCESS] '{vehicleName}' ({purchasePrice} currency) was successfully purchased and unlocked!</color>");

        OnVehiclePurchased?.Invoke(this);
        return true;
    }

    /// <summary>
    /// Forces vehicle unlock (e.g. dev tool or cheat)
    /// </summary>
    public void ForceUnlock()
    {
        PlayerPrefs.SetInt(SAVE_PREFIX + EffectiveVehicleId, 1);
        PlayerPrefs.Save();
        OnVehiclePurchased?.Invoke(this);
    }

    /// <summary>
    /// Resets vehicle back to lock state and restores fuel & condition (dev tool)
    /// </summary>
    public void ResetLockState()
    {
        PlayerPrefs.DeleteKey(SAVE_PREFIX + EffectiveVehicleId);
        PlayerPrefs.DeleteKey(SAVE_PREFIX + vehicleId);
        PlayerPrefs.DeleteKey(SAVE_PREFIX + gameObject.name.ToLower().Replace(" ", "_"));

        currentFuel = maxFuel;
        PlayerPrefs.SetFloat(FUEL_SAVE_PREFIX + EffectiveVehicleId, maxFuel);

        currentCondition = maxCondition;
        PlayerPrefs.SetFloat(CONDITION_SAVE_PREFIX + EffectiveVehicleId, maxCondition);
        PlayerPrefs.DeleteKey(CONDITION_SAVE_PREFIX + EffectiveVehicleId);
        PlayerPrefs.DeleteKey(CONDITION_SAVE_PREFIX + vehicleId);
        PlayerPrefs.DeleteKey(CONDITION_SAVE_PREFIX + gameObject.name.ToLower().Replace(" ", "_"));

        if (carController != null)
        {
            carController.SetConditionRatio(1.0f);
        }

        if (isPlayerInside && InteractionPromptHUD.Instance != null)
        {
            InteractionPromptHUD.Instance.UpdateVehicleHUD(currentFuel, maxFuel, false, currentCondition, maxCondition);
        }

        PlayerPrefs.Save();
    }

    /// <summary>
    /// Static global reset helper: Wipes all vehicle purchase keys and restores fuel & condition to 100%.
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

        PlayerPrefs.DeleteKey(CONDITION_SAVE_PREFIX + "pickup_truck");
        PlayerPrefs.DeleteKey(CONDITION_SAVE_PREFIX + "drivable_pickup");
        PlayerPrefs.DeleteKey(CONDITION_SAVE_PREFIX + "cargo_van");
        PlayerPrefs.DeleteKey(CONDITION_SAVE_PREFIX + "drivable_van");
        PlayerPrefs.DeleteKey(CONDITION_SAVE_PREFIX + "driveable_van");
        PlayerPrefs.DeleteKey(CONDITION_SAVE_PREFIX + "cargo_van_01");

        PlayerPrefs.DeleteKey(COLOR_SAVE_PREFIX + "pickup_truck");
        PlayerPrefs.DeleteKey(COLOR_SAVE_PREFIX + "drivable_pickup");
        PlayerPrefs.DeleteKey(COLOR_SAVE_PREFIX + "cargo_van");
        PlayerPrefs.DeleteKey(COLOR_SAVE_PREFIX + "drivable_van");
        PlayerPrefs.DeleteKey(COLOR_SAVE_PREFIX + "driveable_van");
        PlayerPrefs.DeleteKey(COLOR_SAVE_PREFIX + "cargo_van_01");

        VehicleServiceGarage.ResetAllVehicleTuning();

        PlayerPrefs.Save();

        OnAnyVehicleReset?.Invoke();
        Debug.Log("<color=#FF3333>[DEV] F9 PRESSED: ALL VEHICLE PURCHASES, FUELS, CONDITIONS, PAINTS AND TUNINGS RESET!</color>");
    }

    /// <summary>
    /// Teleports the vehicle smoothly to target position and rotation, zeroing physics velocities,
    /// resetting wheel colliders, and safely carrying any packages currently loaded in the cargo bed.
    /// </summary>
    public void TeleportVehicle(Vector3 targetPos, Quaternion targetRot, bool keepCargoInBed = true)
    {
        // 1. Gather packages in bed to carry along with the vehicle
        VehicleCargoBed bed = GetComponentInChildren<VehicleCargoBed>();
        var bedPackages = new System.Collections.Generic.List<(PhysicalCargoPackage pkg, Vector3 localPos, Quaternion localRot)>();

        if (keepCargoInBed && bed != null)
        {
            foreach (var pkg in bed.PackagesInBed)
            {
                if (pkg != null)
                {
                    Vector3 lPos = transform.InverseTransformPoint(pkg.transform.position);
                    Quaternion lRot = Quaternion.Inverse(transform.rotation) * pkg.transform.rotation;
                    bedPackages.Add((pkg, lPos, lRot));
                }
            }
        }

        // 2. Cut engine torque & clear forces
        if (carController != null)
        {
            carController.ClearAllForces();
            carController.enabled = false;
        }

        // 3. Reset physics velocity and move transform
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = targetPos;
            rb.rotation = targetRot;
        }

        transform.position = targetPos;
        transform.rotation = targetRot;

        // 4. Reset wheel colliders to avoid spring tension or spinning
        if (carController != null)
        {
            ResetWheelCollider(carController.frontLeftCollider);
            ResetWheelCollider(carController.frontRightCollider);
            ResetWheelCollider(carController.rearLeftCollider);
            ResetWheelCollider(carController.rearRightCollider);
            carController.enabled = isPlayerInside && HasFuel;
        }

        Physics.SyncTransforms();

        // 5. Relocate bed packages to exact relative offsets on the new vehicle position
        foreach (var item in bedPackages)
        {
            if (item.pkg != null)
            {
                Vector3 newPkgWorldPos = targetPos + (targetRot * item.localPos);
                Quaternion newPkgWorldRot = targetRot * item.localRot;
                item.pkg.RelocateToPosition(newPkgWorldPos, newPkgWorldRot);
            }
        }

        // 6. Zero velocities again after placement
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// Recalls vehicle safely to the designated garage spawn point or parking point.
    /// </summary>
    public void RecallToGarage()
    {
        // 1. Resolve Target Anchor Point with thorough fallbacks
        Transform targetAnchor = garageSpawnTransform != null ? garageSpawnTransform : parkingSpotTransform;
        if (targetAnchor == null && VehicleShowroomManager.Instance != null)
        {
            targetAnchor = VehicleShowroomManager.Instance.warehouseGarageSpawnPoint;
        }

        if (targetAnchor == null)
        {
            GameObject g = GameObject.Find("PickupGaragePoint");
            if (g == null) g = GameObject.Find("Warehouse_Garage_SpawnPoint");
            if (g == null) g = GameObject.Find("GarageSpawnPoint");
            if (g == null) g = GameObject.Find("Warehouse_SpawnPoint");
            if (g == null) g = GameObject.Find("DeliveryPoint_1");
            if (g == null) g = GameObject.Find("Warehouse");
            if (g != null) targetAnchor = g.transform;
        }

        Vector3 targetPos = targetAnchor != null ? targetAnchor.position + Vector3.up * 0.45f : transform.position + Vector3.up * 0.2f;
        Quaternion targetRot = targetAnchor != null ? targetAnchor.rotation : transform.rotation;

        // 2. Perform safe vehicle teleport
        TeleportVehicle(targetPos, targetRot, true);

        // 3. If player was inside the vehicle, safely exit player AT THE NEW GARAGE POSITION
        if (isPlayerInside)
        {
            ExitVehicle();
        }

        string anchorName = targetAnchor != null ? targetAnchor.name : "Default Position";
        Debug.Log($"<color=#32FFFF>[DrivableVehicle] '{vehicleName}' recalled to garage anchor ({anchorName}) at {targetPos}.</color>");

        if (InteractionPromptHUD.Instance != null)
        {
            InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.GetFormat("prompt_garage_recall_success", vehicleName, ""));
        }

        OnVehicleRecalled?.Invoke(this);
    }

    private void ResetWheelCollider(WheelCollider col)
    {
        if (col == null) return;
        col.motorTorque = 0f;
        col.brakeTorque = 2500f;
        col.steerAngle = 0f;
    }

    public void EnterVehicle(FPSPlayerController player)
    {
        if (isPlayerInside || player == null) return;

        if (!IsUnlocked)
        {
            Debug.LogWarning($"[DrivableVehicle] '{vehicleName}' is locked! You must purchase it first.");
            return;
        }

        currentPlayer = player;
        isPlayerInside = true;
        enterTimestamp = Time.time;

        // 1. Disable player on-foot movement and CharacterController
        player.SetOnFootActive(false);

        // 2. Parent player object to driver seat so it travels with the car
        Transform seatAnchor = driverSeatPoint != null ? driverSeatPoint : transform;
        player.transform.SetParent(seatAnchor);
        player.transform.localPosition = Vector3.zero;
        player.transform.localRotation = Quaternion.identity;

        // 3. Attach player camera to driver seat
        player.AttachCameraToSeat(seatAnchor);

        // Update driver visual visibility based on initial camera mode
        UpdateDriverVisibility(player.CurrentVehicleCameraMode);

        // 4. Enable vehicle controls only if has fuel
        if (carController != null)
        {
            carController.enabled = HasFuel;
        }

        if (InteractionPromptHUD.Instance != null)
        {
            bool inGarage = VehicleServiceGarage.Instance != null &&
                            VehicleServiceGarage.Instance.IsGarageUnlocked() &&
                            VehicleServiceGarage.Instance.IsVehicleInServiceBay(this);

            wasInGarageBayLastFrame = inGarage;
            InteractionPromptHUD.Instance.HidePrompt();
            InteractionPromptHUD.Instance.UpdateVehicleHUD(currentFuel, maxFuel, currentFuel < (maxFuel * 0.18f), currentCondition, maxCondition);
        }

        Debug.Log($"[DrivableVehicle] Player entered '{vehicleName}'. Press [E] to exit.");
    }

    public void UpdateDriverVisibility(VehicleCameraMode cameraMode)
    {
        if (driverVisualObject == null)
        {
            driverVisualObject = FindDriverVisual();
        }

        if (driverVisualObject == null) return;

        if (!isPlayerInside)
        {
            driverVisualObject.SetActive(false);
            return;
        }

        if (cameraMode == VehicleCameraMode.FirstPerson && hideDriverInFPS)
        {
            driverVisualObject.SetActive(false);
        }
        else
        {
            driverVisualObject.SetActive(true);
        }
    }

    private GameObject FindDriverVisual()
    {
        if (driverVisualObject != null) return driverVisualObject;

        // 1. Check under DriverSeatPoint with standard keywords
        if (driverSeatPoint != null)
        {
            foreach (Transform child in driverSeatPoint)
            {
                string n = child.name;
                if (n.IndexOf("Driver", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Character", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Avatar", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Man", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Human", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Courier", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Person", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Floreswa", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return child.gameObject;
                }
            }
        }

        // 2. Check direct children of Vehicle Root (Driveable_Pickup etc.)
        foreach (Transform child in transform)
        {
            if (child == driverSeatPoint || child == exitPoint || child == tpsCameraPoint) continue;
            string n = child.name;
            if (n.StartsWith("Wheel") || n.StartsWith("Cargo") || n.StartsWith("Body") || n.StartsWith("Light") || n.StartsWith("Collider") || n.StartsWith("Mesh")) continue;

            if (n.IndexOf("Driver", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Character", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Avatar", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Man", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Human", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Courier", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Person", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Boy", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Girl", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Male", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Female", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Floreswa", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return child.gameObject;
            }
        }

        return null;
    }

    public Vector3 GetSafeExitPosition()
    {
        if (exitPoint != null)
        {
            Vector3 customPos = exitPoint.position;
            if (IsSpawnPositionClear(customPos))
            {
                return customPos;
            }
        }

        float[] candidateSideOffsets = new float[] { -1.6f, -1.2f, -0.9f, 1.6f, 1.2f, 0.9f };
        foreach (float offset in candidateSideOffsets)
        {
            Vector3 worldPos = transform.position + (transform.right * offset) + (Vector3.up * 0.25f);
            if (IsSpawnPositionClear(worldPos))
            {
                return worldPos;
            }
        }

        Vector3 rearPos = transform.position - (transform.forward * 2.8f) + (Vector3.up * 0.25f);
        if (IsSpawnPositionClear(rearPos)) return rearPos;

        Vector3 frontPos = transform.position + (transform.forward * 2.8f) + (Vector3.up * 0.25f);
        if (IsSpawnPositionClear(frontPos)) return frontPos;

        Vector3 fallback = transform.position - (transform.right * 1.3f) + (Vector3.up * 0.35f);
        if (Physics.Raycast(fallback + Vector3.up * 1.0f, Vector3.down, out RaycastHit hit, 3.0f, ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.point + Vector3.up * 0.15f;
        }

        return fallback;
    }

    private bool IsSpawnPositionClear(Vector3 pos)
    {
        Vector3 bottom = pos + Vector3.up * 0.35f;
        Vector3 top = pos + Vector3.up * 1.45f;
        Collider[] overlaps = Physics.OverlapCapsule(bottom, top, 0.28f, ~0, QueryTriggerInteraction.Ignore);
        foreach (var col in overlaps)
        {
            if (col == null || col.isTrigger) continue;
            if (col.transform.IsChildOf(transform) || (rb != null && col.attachedRigidbody == rb)) continue;
            if (currentPlayer != null && col.transform.IsChildOf(currentPlayer.transform)) continue;
            return false; // Solid wall/obstacle
        }
        return true;
    }

    public void ExitVehicle()
    {
        if (!isPlayerInside || currentPlayer == null) return;

        FPSPlayerController player = currentPlayer;

        // 1. Cut all engine torque, center steer and apply neutral coasting deceleration
        if (carController != null)
        {
            carController.ClearAllForces();
            carController.enabled = false;
        }

        // 2. Save remaining fuel and condition
        PlayerPrefs.SetFloat(FUEL_SAVE_PREFIX + EffectiveVehicleId, currentFuel);
        PlayerPrefs.SetFloat(CONDITION_SAVE_PREFIX + EffectiveVehicleId, currentCondition);
        PlayerPrefs.Save();

        // 3. Set debounce safety timer so the player doesn't instantly re-enter the vehicle on the same frame
        player.exitVehicleSafetyTimer = 0.5f;

        // 4. Temporarily disable player on-foot and CharacterController while moving position
        player.SetOnFootActive(false);

        // 5. Unparent player from vehicle
        player.transform.SetParent(null);

        // 6. Find safe collision-free exit position
        Vector3 spawnPos = GetSafeExitPosition();
        player.transform.position = spawnPos;
        player.transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        Physics.SyncTransforms();

        // 7. Detach camera and restore on-foot player
        player.DetachCameraFromSeat();
        player.SetOnFootActive(true);
        FPSPlayerController.LockCursor(true);

        isPlayerInside = false;
        currentPlayer = null;
        wasInGarageBayLastFrame = false;

        if (driverVisualObject != null)
        {
            driverVisualObject.SetActive(false);
        }

        if (InteractionPromptHUD.Instance != null)
        {
            InteractionPromptHUD.Instance.SuppressPrompts(0.4f);
            InteractionPromptHUD.Instance.HideFuelHUD();
        }

        Debug.Log($"[DrivableVehicle] Player exited '{vehicleName}'. On-foot controls restored at {spawnPos}.");
    }

    private void FixedUpdate()
    {
        // Smoothly decelerate to parked state when vehicle is unoccupied
        if (!isPlayerInside && rb != null)
        {
            if (rb.linearVelocity.magnitude > 0.05f)
            {
                rb.linearVelocity = Vector3.MoveTowards(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 2.5f);
                rb.angularVelocity = Vector3.MoveTowards(rb.angularVelocity, Vector3.zero, Time.fixedDeltaTime * 4.0f);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 1. Driver Seat / FPS Camera Anchor
        if (driverSeatPoint != null)
        {
            Vector3 fpsPos = driverSeatPoint.TransformPoint(fpsCameraOffset);
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.85f);
            Gizmos.DrawWireSphere(fpsPos, 0.18f);
            Gizmos.DrawRay(fpsPos, driverSeatPoint.forward * 0.5f);
        }

        // 2. TPS Pivot & Chase Camera Anchor
        Transform pivotOrigin = tpsCameraPoint != null ? tpsCameraPoint : transform;
        Vector3 pivotPos = pivotOrigin.position + Vector3.up * tpsLookAtHeight;
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(pivotPos, 0.25f);

        Vector3 defaultTpsPos = pivotPos - (transform.forward * tpsDistance) + (Vector3.up * tpsHeight * 0.4f);
        Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.7f);
        Gizmos.DrawLine(pivotPos, defaultTpsPos);
        Gizmos.DrawWireSphere(defaultTpsPos, 0.35f);
    }

    #region --- VEHICLE PAINTING & CUSTOMIZATION ---

    /// <summary>
    /// Applies a paint color to all configured paintable targets (body, hood, doors, etc.) on this vehicle.
    /// </summary>
    public void ApplyPaintColor(Color newColor, bool save = true)
    {
        bool hasValidTarget = false;
        if (paintableTargets != null && paintableTargets.Count > 0)
        {
            foreach (var t in paintableTargets)
            {
                if (t != null && t.targetRenderer != null && t.targetRenderer.transform.IsChildOf(transform))
                {
                    hasValidTarget = true;
                    break;
                }
            }
        }

        if (!hasValidTarget)
        {
            AutoDetectPaintableTargets();
        }

        if (paintableTargets != null)
        {
            foreach (var target in paintableTargets)
            {
                if (target == null) continue;

                // Re-bind renderer if missing or detached
                if (target.targetRenderer == null || !target.targetRenderer.transform.IsChildOf(transform))
                {
                    if (!string.IsNullOrEmpty(target.partName))
                    {
                        string cleanName = target.partName.Split(' ')[0];
                        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);
                        foreach (var r in renderers)
                        {
                            if (r.gameObject.name.Equals(cleanName, StringComparison.OrdinalIgnoreCase))
                            {
                                target.targetRenderer = r;
                                break;
                            }
                        }
                    }
                }

                if (target.targetRenderer == null) continue;

                int slot = Mathf.Max(0, target.materialIndex);

                try
                {
                    Material[] mats = Application.isPlaying ? target.targetRenderer.materials : target.targetRenderer.sharedMaterials;
                    if (mats != null && slot < mats.Length && mats[slot] != null)
                    {
                        Material mat = mats[slot];
                        mat.color = newColor;
                        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", newColor);
                        if (mat.HasProperty("_Color")) mat.SetColor("_Color", newColor);
                        if (mat.HasProperty("_ColorDim")) mat.SetColor("_ColorDim", newColor * 0.75f);
                        if (mat.HasProperty("_ColorDimCurve")) mat.SetColor("_ColorDimCurve", newColor * 0.75f);
                        if (mat.HasProperty("_ColorDimExtra")) mat.SetColor("_ColorDimExtra", newColor * 0.45f);
                        if (mat.HasProperty("_ColorDimSteps")) mat.SetColor("_ColorDimSteps", newColor * 0.45f);
                        if (mat.HasProperty("_ColorGradient")) mat.SetColor("_ColorGradient", newColor);

                        if (Application.isPlaying)
                        {
                            target.targetRenderer.materials = mats; // Explicit writeback
                            try { target.targetRenderer.SetPropertyBlock(null, slot); } catch { }
                        }
                        else
                        {
                            target.targetRenderer.sharedMaterials = mats;
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[DrivableVehicle] Material tinting notice: {ex.Message}");
                }
            }
        }

        if (save)
        {
            PlayerPrefs.SetString(COLOR_SAVE_PREFIX + EffectiveVehicleId, "#" + ColorUtility.ToHtmlStringRGBA(newColor));
            PlayerPrefs.Save();
        }

        OnVehicleColorChanged?.Invoke(this, newColor);
    }

    /// <summary>
    /// Returns the currently active or saved paint color for this vehicle.
    /// </summary>
    public Color GetCurrentColor()
    {
        string hex = PlayerPrefs.GetString(COLOR_SAVE_PREFIX + EffectiveVehicleId, string.Empty);
        if (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out Color savedCol))
        {
            return savedCol;
        }

        if (paintableTargets != null && paintableTargets.Count > 0)
        {
            foreach (var target in paintableTargets)
            {
                if (target != null && target.targetRenderer != null)
                {
                    Material[] mats = target.targetRenderer.sharedMaterials;
                    int slot = Mathf.Max(0, target.materialIndex);
                    if (mats != null && slot < mats.Length && mats[slot] != null)
                    {
                        Material m = mats[slot];
                        if (m.HasProperty("_BaseColor")) return m.GetColor("_BaseColor");
                        if (m.HasProperty("_Color")) return m.GetColor("_Color");
                    }
                }
            }
        }

        return defaultVehicleColor;
    }

    /// <summary>
    /// Loads and applies the saved color from PlayerPrefs, or detects default targets.
    /// </summary>
    public void LoadSavedColor()
    {
        if (paintableTargets == null || paintableTargets.Count == 0)
        {
            AutoDetectPaintableTargets();
        }

        string hex = PlayerPrefs.GetString(COLOR_SAVE_PREFIX + EffectiveVehicleId, string.Empty);
        if (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out Color savedCol))
        {
            ApplyPaintColor(savedCol, false);
        }
    }

    /// <summary>
    /// Auto-detects paintable targets (PickupBody [Element 0], Hood [Element 0], DoorL [Element 0], DoorR [Element 0])
    /// </summary>
    public void AutoDetectPaintableTargets()
    {
        if (paintableTargets == null) paintableTargets = new System.Collections.Generic.List<PaintablePartTarget>();
        paintableTargets.Clear();

        MeshRenderer[] allRenderers = GetComponentsInChildren<MeshRenderer>(true);

        // 1. Search for Pickup specific parts
        MeshRenderer bodyRenderer = null;
        MeshRenderer hoodRenderer = null;
        MeshRenderer doorLRenderer = null;
        MeshRenderer doorRRenderer = null;
        MeshRenderer backDoorRenderer = null;

        foreach (var mr in allRenderers)
        {
            string n = mr.gameObject.name;
            if (n.Equals("PickupBody", StringComparison.OrdinalIgnoreCase)) bodyRenderer = mr;
            else if (n.Equals("Hood", StringComparison.OrdinalIgnoreCase)) hoodRenderer = mr;
            else if (n.Equals("DoorL", StringComparison.OrdinalIgnoreCase)) doorLRenderer = mr;
            else if (n.Equals("DoorR", StringComparison.OrdinalIgnoreCase)) doorRRenderer = mr;
            else if (n.Equals("PickupBackDoor", StringComparison.OrdinalIgnoreCase)) backDoorRenderer = mr;
        }

        if (bodyRenderer != null)
        {
            paintableTargets.Add(new PaintablePartTarget { partName = "PickupBody", targetRenderer = bodyRenderer, materialIndex = 0, colorPropertyName = "_BaseColor" });
            if (hoodRenderer != null) paintableTargets.Add(new PaintablePartTarget { partName = "Hood", targetRenderer = hoodRenderer, materialIndex = 0, colorPropertyName = "_BaseColor" });
            if (doorLRenderer != null) paintableTargets.Add(new PaintablePartTarget { partName = "DoorL", targetRenderer = doorLRenderer, materialIndex = 0, colorPropertyName = "_BaseColor" });
            if (doorRRenderer != null) paintableTargets.Add(new PaintablePartTarget { partName = "DoorR", targetRenderer = doorRRenderer, materialIndex = 0, colorPropertyName = "_BaseColor" });
            if (backDoorRenderer != null) paintableTargets.Add(new PaintablePartTarget { partName = "PickupBackDoor", targetRenderer = backDoorRenderer, materialIndex = 0, colorPropertyName = "_BaseColor" });
        }
        else
        {
            // Generic vehicle detection
            foreach (var r in allRenderers)
            {
                string n = r.gameObject.name.ToLower();
                if (n.Contains("body") || n.Contains("hood") || n.Contains("door") || n.Contains("chassis") || n.Contains("cabin"))
                {
                    paintableTargets.Add(new PaintablePartTarget
                    {
                        partName = r.gameObject.name,
                        targetRenderer = r,
                        materialIndex = 0,
                        colorPropertyName = "_BaseColor"
                    });
                }
            }
        }
    }

    #endregion
}

/// <summary>
/// Defines a specific target MeshRenderer and material element index for vehicle paint customization.
/// </summary>
[System.Serializable]
public class PaintablePartTarget
{
    [Tooltip("Description/Name of this paintable part (e.g. Body, Hood, Doors)")]
    public string partName = "Body Part";

    [Tooltip("Target MeshRenderer / SkinnedMeshRenderer to apply paint color to")]
    public Renderer targetRenderer;

    [Tooltip("Target material element index (0 = Element 0)")]
    public int materialIndex = 0;

    [Tooltip("Shader color property name to tint (default: _BaseColor, fallback: _Color)")]
    public string colorPropertyName = "_BaseColor";
}
