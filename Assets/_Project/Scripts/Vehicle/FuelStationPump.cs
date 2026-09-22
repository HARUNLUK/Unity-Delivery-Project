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

    [Header("--- VISUALS & EFFECTS ---")]
    [Tooltip("Optional light that turns green while pumping fuel")]
    public Light pumpStatusLight;

    private Collider triggerCollider;
    private float accumulatedCost = 0f;
    private bool isActivelyRefueling = false;
    private AudioSource pumpAudioSource;
    private readonly HashSet<Collider> insideColliders = new HashSet<Collider>();
    private bool wasShowingPrompt = false;
    private float lastErrorSoundTime = 0f;

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
        StopPumpingAudio();
        if (wasShowingPrompt && InteractionPromptHUD.Instance != null)
        {
            InteractionPromptHUD.Instance.HidePrompt();
        }
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
        // 1. Clean up stale/destroyed colliders
        insideColliders.RemoveWhere(c => c == null || !c.gameObject.activeInHierarchy);

        // 2. Identify target vehicle and whether player/car is present at this pump
        DrivableVehicle targetVehicle = FindActiveVehicle();
        bool isPlayerPresent = IsPlayerNearPump();

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

        // 3. If player is present on foot but no vehicle is close enough
        if (targetVehicle == null)
        {
            if (isActivelyRefueling)
            {
                StopPumpingAudio();
                SetPumpLightActive(false);
            }

            if (InteractionPromptHUD.Instance != null)
            {
                InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.GetFormat("prompt_gas_station_align", GetStationDisplayName()));
                wasShowingPrompt = true;
            }
            return;
        }

        // 4. Check if vehicle fuel tank is already full
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

        // 5. Check economy balance
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

        // 6. Check hold-to-refuel inputs
        if (isHoldingRefuelKey)
        {
            float deltaLiters = refuelRateLitersPerSecond * Time.deltaTime;
            float maxCanAdd = targetVehicle.maxFuel - targetVehicle.currentFuel;

            // Restrict by balance if needed
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
                        // CRITICAL: Deduct silently without spamming coin sound during pumping!
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
                InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.GetFormat("prompt_gas_station_hold_refuel", GetStationDisplayName(), pricePerLiter));
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
            if (drivenVeh != null)
            {
                if (IsInsideTrigger(drivenVeh.gameObject) || Vector3.Distance(transform.position, drivenVeh.transform.position) <= 12f)
                {
                    return drivenVeh;
                }
            }
        }

        // 2. Search inside tracked trigger colliders
        foreach (Collider col in insideColliders)
        {
            if (col == null) continue;
            DrivableVehicle v = col.GetComponentInParent<DrivableVehicle>();
            if (v == null) v = col.GetComponent<DrivableVehicle>();
            if (v != null) return v;
        }

        // 3. If player is on foot near the pump, find nearest vehicle in vicinity
        if (IsPlayerNearPump())
        {
            DrivableVehicle[] allVehicles = UnityEngine.Object.FindObjectsByType<DrivableVehicle>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            DrivableVehicle closest = null;
            float minDist = 11.0f; // 11 meters detection bubble around pump

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

        // Check if player collider is inside trigger
        if (IsInsideTrigger(FPSPlayerController.Instance.gameObject)) return true;

        // Proximity fallback
        float dist = Vector3.Distance(transform.position, FPSPlayerController.Instance.transform.position);
        return dist <= 8.5f;
    }

    private bool IsInsideTrigger(GameObject obj)
    {
        if (obj == null) return false;

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
}
