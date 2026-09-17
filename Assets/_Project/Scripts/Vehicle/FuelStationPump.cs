using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class FuelStationPump : MonoBehaviour
{
    [Header("--- STATION SETTINGS ---")]
    [Tooltip("Display name of the fuel pump")]
    public string stationName = "Gas Station";

    [Tooltip("Price in USD per litre of fuel")]
    public float pricePerLiter = 35f;

    [Tooltip("How many litres are filled per second while holding key")]
    public float refuelRateLitersPerSecond = 5.5f;

    [Header("--- VISUALS & EFFECTS ---")]
    [Tooltip("Optional light that turns green while pumping fuel")]
    public Light pumpStatusLight;

    private Collider triggerCollider;
    private float accumulatedCost = 0f;
    private bool isActivelyRefueling = false;
    private AudioSource pumpAudioSource;
    private bool wasFullLastFrame = false;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }

        EnsurePumpAudioSource();
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
            pumpAudioSource.spatialBlend = 0.70f; // Clear audible presence inside cabin & nearby
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
    }

    private void OnTriggerStay(Collider other)
    {
        EnsurePumpAudioSource();

        // 1. Detect vehicle in trigger zone
        DrivableVehicle vehicle = other.GetComponentInParent<DrivableVehicle>();
        if (vehicle == null) vehicle = other.GetComponent<DrivableVehicle>();
        if (vehicle == null)
        {
            FPSPlayerController player = other.GetComponentInParent<FPSPlayerController>();
            if (player != null && player.currentVehicleTransform != null)
            {
                vehicle = player.currentVehicleTransform.GetComponent<DrivableVehicle>();
            }
        }

        if (vehicle == null) return;

        // 2. Check if fuel tank is already full
        if (vehicle.currentFuel >= vehicle.maxFuel - 0.05f)
        {
            if (isActivelyRefueling)
            {
                StopPumpingAudio();
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayFuelPumpFinish(transform.position);
                }
            }

            if (InteractionPromptHUD.Instance != null)
            {
                InteractionPromptHUD.Instance.ShowPrompt($"<color=#32FF64>[DEPO DOLU]</color> ({vehicle.maxFuel:F1} / {vehicle.maxFuel:F1} L)");
            }
            SetPumpLightActive(false);
            return;
        }

        int playerBalance = PlayerEconomyManager.Instance != null ? PlayerEconomyManager.Instance.CurrentLiveBalance : 0;
        if (playerBalance <= 0)
        {
            StopPumpingAudio();
            if (InteractionPromptHUD.Instance != null)
            {
                InteractionPromptHUD.Instance.ShowPrompt($"<color=#FF5555>[YETERSİZ BAKİYE]</color> Yakıt için para gerekli (${pricePerLiter}/L)");
            }
            SetPumpLightActive(false);
            return;
        }

        // 3. Check for Hold-to-Refuel Input (F, Space, E, Gamepad, Mouse)
        bool isHoldingRefuelKey = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.fKey.isPressed || Keyboard.current.spaceKey.isPressed || Keyboard.current.eKey.isPressed)
            {
                isHoldingRefuelKey = true;
            }
        }
        if (Gamepad.current != null && (Gamepad.current.buttonSouth.isPressed || Gamepad.current.buttonWest.isPressed))
        {
            isHoldingRefuelKey = true;
        }
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            isHoldingRefuelKey = true;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        try
        {
            if (Input.GetKey(KeyCode.F) || Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.E) || Input.GetMouseButton(0))
            {
                isHoldingRefuelKey = true;
            }
        }
        catch { }
#endif

        if (isHoldingRefuelKey)
        {
            // Execute Refueling
            isActivelyRefueling = true;
            SetPumpLightActive(true);

            PlayPumpingAudio();

            float deltaLiters = refuelRateLitersPerSecond * Time.deltaTime;
            float maxCanAdd = vehicle.maxFuel - vehicle.currentFuel;
            deltaLiters = Mathf.Min(deltaLiters, maxCanAdd);

            float costThisFrame = deltaLiters * pricePerLiter;
            accumulatedCost += costThisFrame;

            if (accumulatedCost >= 1f)
            {
                int intDeduction = Mathf.FloorToInt(accumulatedCost);
                if (PlayerEconomyManager.Instance != null)
                {
                    PlayerEconomyManager.Instance.DeductCash(intDeduction);
                }
                accumulatedCost -= intDeduction;
            }

            vehicle.Refuel(deltaLiters);

            if (InteractionPromptHUD.Instance != null)
            {
                float percent = (vehicle.currentFuel / vehicle.maxFuel) * 100f;
                InteractionPromptHUD.Instance.ShowPrompt($"<color=#32FFFF>[YAKIT ALINIYOR...]</color> {vehicle.currentFuel:F1} / {vehicle.maxFuel:F1} L (%{percent:F0}) - ${pricePerLiter}/L");
            }
        }
        else
        {
            StopPumpingAudio();
            SetPumpLightActive(false);

            if (InteractionPromptHUD.Instance != null)
            {
                InteractionPromptHUD.Instance.ShowPrompt($"<b>{stationName}</b>: <color=#32FF64>[F]</color> veya <color=#32FF64>[Boşluk]</color> Basılı Tut -> <b>Yakıt Doldur</b> (${pricePerLiter}/L)");
            }
        }
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

    private void OnTriggerExit(Collider other)
    {
        DrivableVehicle vehicle = other.GetComponentInParent<DrivableVehicle>();
        if (vehicle != null || other.GetComponentInParent<FPSPlayerController>() != null)
        {
            StopPumpingAudio();
            SetPumpLightActive(false);

            if (accumulatedCost > 0.05f)
            {
                int remainingCost = Mathf.CeilToInt(accumulatedCost);
                if (PlayerEconomyManager.Instance != null)
                {
                    PlayerEconomyManager.Instance.DeductCash(remainingCost);
                }
                accumulatedCost = 0f;
            }

            if (InteractionPromptHUD.Instance != null)
            {
                InteractionPromptHUD.Instance.HidePrompt();
            }
        }
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
