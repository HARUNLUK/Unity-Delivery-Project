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

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void OnTriggerStay(Collider other)
    {
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
            if (InteractionPromptHUD.Instance != null)
            {
                InteractionPromptHUD.Instance.ShowPrompt($"<color=#32FF64>[TANK FULL]</color> ({vehicle.maxFuel:F1} / {vehicle.maxFuel:F1} L)");
            }
            SetPumpLightActive(false);
            return;
        }

        int playerBalance = PlayerEconomyManager.Instance != null ? PlayerEconomyManager.Instance.CurrentLiveBalance : 0;
        if (playerBalance <= 0)
        {
            if (InteractionPromptHUD.Instance != null)
            {
                InteractionPromptHUD.Instance.ShowPrompt($"<color=#FF5555>[INSUFFICIENT FUNDS]</color> Need money to refuel (${pricePerLiter}/L)");
            }
            SetPumpLightActive(false);
            return;
        }

        // 3. Check for Hold-to-Refuel Input (F key or Space)
        bool isHoldingRefuelKey = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.fKey.isPressed || Keyboard.current.spaceKey.isPressed)
            {
                isHoldingRefuelKey = true;
            }
        }
#endif
        try
        {
            if (Input.GetKey(KeyCode.F) || Input.GetKey(KeyCode.Space))
            {
                isHoldingRefuelKey = true;
            }
        }
        catch { }

        if (isHoldingRefuelKey)
        {
            // Execute Refueling
            isActivelyRefueling = true;
            SetPumpLightActive(true);

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
                InteractionPromptHUD.Instance.ShowPrompt($"<color=#32FFFF>[REFUELING...]</color> {vehicle.currentFuel:F1} / {vehicle.maxFuel:F1} L ({percent:F0}%) - ${pricePerLiter}/L");
            }
        }
        else
        {
            isActivelyRefueling = false;
            SetPumpLightActive(false);

            if (InteractionPromptHUD.Instance != null)
            {
                InteractionPromptHUD.Instance.ShowPrompt($"<b>{stationName}</b>: Hold [F] -> <b>Refuel</b> (${pricePerLiter}/L)");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        DrivableVehicle vehicle = other.GetComponentInParent<DrivableVehicle>();
        if (vehicle != null || other.GetComponentInParent<FPSPlayerController>() != null)
        {
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
