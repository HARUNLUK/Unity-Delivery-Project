using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class FuelStationPump : MonoBehaviour
{
    [Header("--- STATION SETTINGS ---")]
    [Tooltip("Display name of the fuel pump")]
    public string stationName = "Petrol İstasyonu";

    [Tooltip("Price in USD per litre of fuel")]
    public float pricePerLiter = 5.0f;

    [Tooltip("How many litres are filled per second while holding key")]
    public float refuelRateLitersPerSecond = 6.0f;

    [Header("--- 🛢️ GAS CANISTER (BIDON) SETTINGS ---")]
    [Tooltip("Price in USD to purchase one gas canister ($)")]
    public int canisterPrice = 100;

    [Tooltip("How many liters of fuel each canister restores (L)")]
    public float canisterFuelAmount = 10f;

    [Tooltip("Prefab of the Gas Canister (Gas_Can with FuelCanisterItem)")]
    public GameObject canisterPrefab;

    [Tooltip("Optional spawn point where purchased canister appears")]
    public Transform canisterSpawnTransform;

    [Tooltip("Key to buy canister when on foot near pump (Default: E)")]
    public Key canisterBuyKey = Key.E;

    [Header("--- VISUALS & EFFECTS ---")]
    [Tooltip("Optional light that turns green while pumping fuel")]
    public Light pumpStatusLight;

    [Tooltip("Maximum interaction and detection distance from pump center in meters")]
    public float interactionDistance = 8.5f;

    private Collider triggerCollider;
    private float accumulatedCost = 0f;
    private bool isActivelyRefueling = false;
    private AudioSource pumpAudioSource;
    private readonly HashSet<Collider> insideColliders = new HashSet<Collider>();
    private bool wasShowingPrompt = false;
    private float lastErrorSoundTime = 0f;

    private static readonly List<FuelStationPump> activePumps = new List<FuelStationPump>();

    public static bool IsVehicleNearAnyPump(DrivableVehicle veh)
    {
        if (veh == null) return false;
        for (int i = 0; i < activePumps.Count; i++)
        {
            var p = activePumps[i];
            if (p != null && p.gameObject.activeInHierarchy)
            {
                if (Vector3.Distance(p.transform.position, veh.transform.position) <= p.interactionDistance)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public static bool IsAnyPumpRefuelingVehicle(DrivableVehicle veh)
    {
        if (veh == null) return false;
        for (int i = 0; i < activePumps.Count; i++)
        {
            var p = activePumps[i];
            if (p != null && p.gameObject.activeInHierarchy && p.isActivelyRefueling)
            {
                if (Vector3.Distance(p.transform.position, veh.transform.position) <= p.interactionDistance)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }

        EnsurePumpAudioSource();
        SetPumpLightActive(false);
    }

    private void OnEnable()
    {
        if (!activePumps.Contains(this))
        {
            activePumps.Add(this);
        }
    }

    private void EnsurePumpAudioSource()
    {
        if (pumpAudioSource == null)
        {
            pumpAudioSource = GetComponent<AudioSource>();
            if (pumpAudioSource == null)
            {
                pumpAudioSource = gameObject.AddComponent<AudioSource>();
            }
            pumpAudioSource.spatialBlend = 0.70f;
            pumpAudioSource.minDistance = 6.0f;
            pumpAudioSource.maxDistance = 50.0f;
            pumpAudioSource.rolloffMode = AudioRolloffMode.Linear;
            pumpAudioSource.loop = true;
            pumpAudioSource.playOnAwake = false;
        }
    }

    private void OnDisable()
    {
        activePumps.Remove(this);
        StopPumpingAudio();
        SetPumpLightActive(false);
        isActivelyRefueling = false;
        if (wasShowingPrompt && InteractionPromptHUD.Instance != null)
        {
            InteractionPromptHUD.Instance.HidePrompt();
        }
        wasShowingPrompt = false;
        insideColliders.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other != null)
        {
            insideColliders.Add(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other != null)
        {
            insideColliders.Remove(other);
        }
    }

    public string GetStationDisplayName()
    {
        if (string.IsNullOrEmpty(stationName) || stationName == "Petrol İstasyonu" || stationName == "Gas Station")
        {
            return LocalizationManager.Get("prompt_gas_station_name");
        }
        return stationName;
    }

    private void Update()
    {
        // 1. Clean up stale/destroyed colliders and those beyond interaction distance
        insideColliders.RemoveWhere(c => c == null || !c.gameObject.activeInHierarchy || Vector3.Distance(transform.position, c.transform.position) > interactionDistance);

        // 2. Identify target vehicle and whether player/car is present at this pump
        DrivableVehicle targetVehicle = FindActiveVehicle();
        bool isPlayerPresent = IsPlayerNearPump();

        // Strict distance re-verification
        if (targetVehicle != null && Vector3.Distance(transform.position, targetVehicle.transform.position) > interactionDistance)
        {
            targetVehicle = null;
        }

        if (targetVehicle == null && !isPlayerPresent)
        {
            // Nothing near the pump: cleanup
            if (isActivelyRefueling)
            {
                StopPumpingAudio();
                SetPumpLightActive(false);
                isActivelyRefueling = false;
                if (PlayerEconomyManager.Instance != null) PlayerEconomyManager.Instance.SaveLiveBalance();
            }

            if (accumulatedCost >= 0.05f)
            {
                int remainingCost = Mathf.CeilToInt(accumulatedCost);
                if (PlayerEconomyManager.Instance != null)
                {
                    // Deduct silently (no coin sound spam)
                    PlayerEconomyManager.Instance.DeductCash(remainingCost, false);
                    PlayerEconomyManager.Instance.SaveLiveBalance();
                }
                accumulatedCost = 0f;
            }

            if (wasShowingPrompt)
            {
                if (InteractionPromptHUD.Instance != null)
                {
                    InteractionPromptHUD.Instance.HidePrompt();
                }
                wasShowingPrompt = false;
            }
            return;
        }

        bool isOnFoot = FPSPlayerController.Instance == null || FPSPlayerController.Instance.IsOnFoot;

        // ========================================================
        // 1. ARACA BİNMİYORKEN (YAYA / ON FOOT):
        // Pompa doğrudan araç doldurmaya yarar; bidonlar özel raftan (FuelCanisterShelf) alınır.
        // ========================================================
        if (isOnFoot)
        {
            if (isActivelyRefueling)
            {
                StopPumpingAudio();
                SetPumpLightActive(false);
                isActivelyRefueling = false;
                if (PlayerEconomyManager.Instance != null) PlayerEconomyManager.Instance.SaveLiveBalance();
            }

            if (wasShowingPrompt)
            {
                if (InteractionPromptHUD.Instance != null) InteractionPromptHUD.Instance.HidePrompt();
                wasShowingPrompt = false;
            }
            return;
        }

        // ========================================================
        // 2. ARAÇTAYKEN (SÜRÜCÜ KOLTUĞUNDA / IN VEHICLE):
        // Pompadan araca doğrudan yakıt doldurma seçeneği çıkar.
        // ========================================================
        if (targetVehicle == null)
        {
            if (isActivelyRefueling)
            {
                StopPumpingAudio();
                SetPumpLightActive(false);
                isActivelyRefueling = false;
                if (PlayerEconomyManager.Instance != null) PlayerEconomyManager.Instance.SaveLiveBalance();
            }

            if (InteractionPromptHUD.Instance != null)
            {
                InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.GetFormat("prompt_gas_station_align", GetStationDisplayName()));
                wasShowingPrompt = true;
            }
            return;
        }

        // Depo tamamen dolu mu?
        if (targetVehicle.currentFuel >= targetVehicle.maxFuel - 0.05f)
        {
            if (isActivelyRefueling)
            {
                StopPumpingAudio();
                isActivelyRefueling = false;
                if (PlayerEconomyManager.Instance != null) PlayerEconomyManager.Instance.SaveLiveBalance();
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayFuelPumpFinish(transform.position);
                }
            }

            if (InteractionPromptHUD.Instance != null)
            {
                InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.GetFormat("prompt_gas_station_full", targetVehicle.maxFuel, targetVehicle.maxFuel));
                wasShowingPrompt = true;
            }
            SetPumpLightActive(false);
            return;
        }

        // Bakiye kontrolü
        int playerBalance = PlayerEconomyManager.Instance != null ? PlayerEconomyManager.Instance.CurrentLiveBalance : 99999;
        bool isHoldingRefuelKey = CheckRefuelInput();

        if (playerBalance <= 0 && pricePerLiter > 0)
        {
            if (isActivelyRefueling)
            {
                StopPumpingAudio();
                isActivelyRefueling = false;
                if (PlayerEconomyManager.Instance != null) PlayerEconomyManager.Instance.SaveLiveBalance();
            }

            if (isHoldingRefuelKey && Time.time - lastErrorSoundTime > 0.8f)
            {
                lastErrorSoundTime = Time.time;
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayError();
                }
            }

            if (InteractionPromptHUD.Instance != null)
            {
                InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.GetFormat("prompt_gas_station_no_money", pricePerLiter));
                wasShowingPrompt = true;
            }
            SetPumpLightActive(false);
            return;
        }

        // Araçtayken tuşa basılı tutarak aracı doldurma
        if (isHoldingRefuelKey)
        {
            float deltaLiters = refuelRateLitersPerSecond * Time.deltaTime;
            float maxCanAdd = targetVehicle.maxFuel - targetVehicle.currentFuel;

            if (pricePerLiter > 0)
            {
                float affordableLiters = playerBalance / pricePerLiter;
                maxCanAdd = Mathf.Min(maxCanAdd, affordableLiters);
            }

            if (maxCanAdd <= 0.01f && playerBalance <= 0)
            {
                if (isActivelyRefueling)
                {
                    StopPumpingAudio();
                    isActivelyRefueling = false;
                    if (PlayerEconomyManager.Instance != null) PlayerEconomyManager.Instance.SaveLiveBalance();
                }
                if (Time.time - lastErrorSoundTime > 0.8f)
                {
                    lastErrorSoundTime = Time.time;
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayError();
                }
                SetPumpLightActive(false);
                return;
            }

            isActivelyRefueling = true;
            SetPumpLightActive(true);
            PlayPumpingAudio();

            deltaLiters = Mathf.Clamp(deltaLiters, 0f, maxCanAdd);

            if (deltaLiters > 0f)
            {
                float costThisFrame = deltaLiters * pricePerLiter;
                accumulatedCost += costThisFrame;

                if (accumulatedCost >= 1f)
                {
                    int intDeduction = Mathf.FloorToInt(accumulatedCost);
                    if (PlayerEconomyManager.Instance != null)
                    {
                        PlayerEconomyManager.Instance.DeductCash(intDeduction, false);
                    }
                    accumulatedCost -= intDeduction;
                }

                targetVehicle.Refuel(deltaLiters);
            }

            if (InteractionPromptHUD.Instance != null)
            {
                float percent = (targetVehicle.currentFuel / targetVehicle.maxFuel) * 100f;
                InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.GetFormat("prompt_gas_station_refueling", targetVehicle.currentFuel, targetVehicle.maxFuel, percent, pricePerLiter));
                wasShowingPrompt = true;
            }
        }
        else
        {
            if (isActivelyRefueling)
            {
                StopPumpingAudio();
                isActivelyRefueling = false;
                if (PlayerEconomyManager.Instance != null) PlayerEconomyManager.Instance.SaveLiveBalance();
            }
            SetPumpLightActive(false);

            if (InteractionPromptHUD.Instance != null)
            {
                InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.GetFormat("prompt_gas_station_hold_refuel_in_car", GetStationDisplayName(), pricePerLiter));
                wasShowingPrompt = true;
            }
        }
    }

    private DrivableVehicle FindActiveVehicle()
    {
        // 1. Is player currently driving a vehicle inside or near the pump?
        if (FPSPlayerController.Instance != null && !FPSPlayerController.Instance.IsOnFoot && FPSPlayerController.Instance.currentVehicleTransform != null)
        {
            DrivableVehicle drivenVeh = FPSPlayerController.Instance.currentVehicleTransform.GetComponent<DrivableVehicle>();
            if (drivenVeh != null && Vector3.Distance(transform.position, drivenVeh.transform.position) <= interactionDistance)
            {
                return drivenVeh;
            }
        }

        // 2. Search inside tracked trigger colliders within distance
        foreach (Collider col in insideColliders)
        {
            if (col == null) continue;
            if (Vector3.Distance(transform.position, col.transform.position) > interactionDistance) continue;
            DrivableVehicle v = col.GetComponentInParent<DrivableVehicle>();
            if (v == null) v = col.GetComponent<DrivableVehicle>();
            if (v != null && Vector3.Distance(transform.position, v.transform.position) <= interactionDistance) return v;
        }

        // 3. If player is on foot near the pump, find nearest vehicle within interactionDistance
        if (IsPlayerNearPump())
        {
            DrivableVehicle[] allVehicles = UnityEngine.Object.FindObjectsByType<DrivableVehicle>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            DrivableVehicle closest = null;
            float minDist = interactionDistance;

            foreach (var veh in allVehicles)
            {
                if (veh == null) continue;
                float d = Vector3.Distance(transform.position, veh.transform.position);
                if (d < minDist)
                {
                    minDist = d;
                    closest = veh;
                }
            }

            return closest;
        }

        return null;
    }

    private bool IsPlayerNearPump()
    {
        if (FPSPlayerController.Instance == null) return false;

        float dist = Vector3.Distance(transform.position, FPSPlayerController.Instance.transform.position);
        if (dist > interactionDistance) return false;

        // If player is driving a vehicle, check vehicle distance rather than player position
        if (!FPSPlayerController.Instance.IsOnFoot && FPSPlayerController.Instance.currentVehicleTransform != null)
        {
            float vDist = Vector3.Distance(transform.position, FPSPlayerController.Instance.currentVehicleTransform.position);
            return vDist <= interactionDistance;
        }

        return dist <= interactionDistance;
    }

    private bool IsInsideTrigger(GameObject obj)
    {
        if (obj == null) return false;
        if (Vector3.Distance(transform.position, obj.transform.position) > interactionDistance) return false;

        foreach (var col in insideColliders)
        {
            if (col != null && (col.gameObject == obj || col.transform.IsChildOf(obj.transform)))
            {
                return true;
            }
        }

        if (triggerCollider != null)
        {
            return triggerCollider.bounds.Contains(obj.transform.position);
        }

        return false;
    }

    private bool CheckRefuelInput()
    {
        bool isHolding = false;
        bool isOnFoot = FPSPlayerController.Instance != null && FPSPlayerController.Instance.IsOnFoot;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.fKey.isPressed || Keyboard.current.spaceKey.isPressed)
            {
                isHolding = true;
            }
            // Only allow E if player is on foot, preventing accidental vehicle exit while driving
            if (isOnFoot && Keyboard.current.eKey.isPressed)
            {
                isHolding = true;
            }
        }

        if (Gamepad.current != null && (Gamepad.current.buttonSouth.isPressed || Gamepad.current.buttonWest.isPressed))
        {
            isHolding = true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            isHolding = true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        try
        {
            if (Input.GetKey(KeyCode.F) || Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0))
            {
                isHolding = true;
            }
            if (isOnFoot && Input.GetKey(KeyCode.E))
            {
                isHolding = true;
            }
        }
        catch { }
#endif

        return isHolding;
    }

    private void PlayPumpingAudio()
    {
        if (AudioManager.Instance == null || AudioManager.Instance.fuelPumpingLoop == null) return;
        EnsurePumpAudioSource();

        if (pumpAudioSource != null)
        {
            if (pumpAudioSource.clip != AudioManager.Instance.fuelPumpingLoop)
            {
                pumpAudioSource.clip = AudioManager.Instance.fuelPumpingLoop;
            }

            float vol = AudioManager.Instance.fuelPumpingVolume * AudioManager.Instance.sfxVolume * AudioManager.Instance.masterVolume;
            pumpAudioSource.volume = vol;

            if (!pumpAudioSource.isPlaying)
            {
                pumpAudioSource.Play();
            }
        }
    }

    private void StopPumpingAudio()
    {
        if (pumpAudioSource != null && pumpAudioSource.isPlaying)
        {
            pumpAudioSource.Stop();
        }
        isActivelyRefueling = false;
    }

    private void SetPumpLightActive(bool active)
    {
        if (pumpStatusLight != null)
        {
            pumpStatusLight.color = active ? Color.green : Color.yellow;
            pumpStatusLight.intensity = active ? 2.5f : 1.0f;
        }
    }

    public string GetCanisterKeyDisplayName()
    {
        if (canisterBuyKey == Key.E) return "E";
        return canisterBuyKey != Key.None ? canisterBuyKey.ToString() : "E";
    }

    private bool CheckCanisterBuyInput()
    {
        bool pressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (canisterBuyKey != Key.None)
            {
                var keyCtrl = Keyboard.current[canisterBuyKey];
                if (keyCtrl != null && keyCtrl.wasPressedThisFrame) pressed = true;
            }
            if (Keyboard.current.eKey.wasPressedThisFrame) pressed = true;
            if (Keyboard.current.bKey.wasPressedThisFrame) pressed = true;
        }

        if (Gamepad.current != null && (Gamepad.current.buttonNorth.wasPressedThisFrame || Gamepad.current.buttonWest.wasPressedThisFrame))
        {
            pressed = true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        try
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.G))
            {
                pressed = true;
            }
        }
        catch { }
#endif

        return pressed;
    }

    public bool TryBuyGasCanister()
    {
        int playerBalance = PlayerEconomyManager.Instance != null ? PlayerEconomyManager.Instance.CurrentLiveBalance : 99999;
        if (playerBalance < canisterPrice && canisterPrice > 0)
        {
            if (Time.time - lastErrorSoundTime > 0.8f)
            {
                lastErrorSoundTime = Time.time;
                if (AudioManager.Instance != null) AudioManager.Instance.PlayError();
            }

            if (InteractionPromptHUD.Instance != null)
            {
                InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.GetFormat("prompt_gas_station_no_money_canister", canisterPrice, playerBalance));
                wasShowingPrompt = true;
            }
            return false;
        }

        // Deduct canister price
        if (PlayerEconomyManager.Instance != null && canisterPrice > 0)
        {
            PlayerEconomyManager.Instance.SpendMoney(canisterPrice);
            PlayerEconomyManager.Instance.SaveLiveBalance();
        }

        // Determine spawn location
        Vector3 spawnPos;
        Quaternion spawnRot = Quaternion.identity;

        if (canisterSpawnTransform != null)
        {
            spawnPos = canisterSpawnTransform.position;
            spawnRot = canisterSpawnTransform.rotation;
        }
        else
        {
            if (FPSPlayerController.Instance != null)
            {
                Vector3 pFwd = FPSPlayerController.Instance.transform.forward;
                pFwd.y = 0f;
                spawnPos = FPSPlayerController.Instance.transform.position + pFwd.normalized * 1.2f + Vector3.up * 0.6f;
            }
            else
            {
                spawnPos = transform.position + transform.forward * 1.5f + Vector3.up * 0.5f;
            }
        }

        GameObject prefabToSpawn = canisterPrefab;
        if (prefabToSpawn == null)
        {
            prefabToSpawn = Resources.Load<GameObject>("Prefabs/Gas_Can");
        }
#if UNITY_EDITOR
        if (prefabToSpawn == null)
        {
            prefabToSpawn = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_AssetPacks/ExplosivesPackage/Prefabs/Gas_Can.prefab");
        }
#endif
        if (prefabToSpawn == null)
        {
            prefabToSpawn = Resources.Load<GameObject>("Prefabs/Gas_Canister");
        }

        if (prefabToSpawn != null)
        {
            GameObject canObj = Instantiate(prefabToSpawn, spawnPos, spawnRot);
            canObj.name = "Gas_Can";

            // Ensure Rigidbody
            Rigidbody canRb = canObj.GetComponent<Rigidbody>();
            if (canRb == null)
            {
                canRb = canObj.AddComponent<Rigidbody>();
                canRb.mass = 4.5f;
                canRb.linearDamping = 0.2f;
                canRb.angularDamping = 0.5f;
                canRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                canRb.interpolation = RigidbodyInterpolation.Interpolate;
            }

            // Ensure CarriableItem
            CarriableItem carriable = canObj.GetComponent<CarriableItem>();
            if (carriable == null)
            {
                carriable = canObj.AddComponent<CarriableItem>();
                carriable.itemId = "fuel_canister";
                carriable.nameKey = "item_fuel_canister_name";
                carriable.fallbackName = "Benzin Bidonu";
                carriable.useCustomHoldRotation = true;
                carriable.customHoldRotation = new Vector3(-90f, 90f, 0f);
            }

            // Ensure FuelCanisterItem
            FuelCanisterItem canisterComp = canObj.GetComponent<FuelCanisterItem>();
            if (canisterComp == null) canisterComp = canObj.AddComponent<FuelCanisterItem>();
            canisterComp.fuelAmount = canisterFuelAmount;
            canisterComp.refuelDistance = 5.0f;
            canisterComp.holdRotationOffset = new Vector3(-90f, 90f, 0f);

            // Direct spawn into player's hands!
            if (PhysicsGrabber.Instance != null && canRb != null)
            {
                if (PhysicsGrabber.Instance.IsHoldingObject)
                {
                    PhysicsGrabber.Instance.ReleaseObject();
                }
                PhysicsGrabber.Instance.GrabObject(canRb);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMoneySubtract();
            }

            if (InteractionPromptHUD.Instance != null)
            {
                InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.GetFormat("prompt_gas_station_canister_bought", canisterFuelAmount, canisterPrice));
                wasShowingPrompt = true;
            }


            return true;
        }
        else
        {
            Debug.LogError("[FuelStationPump] Gas Canister prefab is not assigned and could not be loaded!");
            return false;
        }
    }
}
