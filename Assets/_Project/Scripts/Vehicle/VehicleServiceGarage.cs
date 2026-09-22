using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class VehicleServiceGarage : MonoBehaviour
{
    private static VehicleServiceGarage instance;
    public static VehicleServiceGarage Instance
    {
        get
        {
            if (instance == null)
            {
                instance = UnityEngine.Object.FindAnyObjectByType<VehicleServiceGarage>(FindObjectsInactive.Include);
            }
            return instance;
        }
        private set => instance = value;
    }

    [Header("--- PROPERTY LINK ---")]
    public PurchasableProperty propertyComponent;

    [Header("--- GARAGE SERVICE COSTS ($) ---")]
    [Tooltip("Cost to fully repair and refuel vehicle ($)")]
    public int repairCost = 150;
    [Tooltip("Cost to custom paint and lacquer vehicle ($)")]
    public int repaintCost = 250;
    [Tooltip("Cost for Stage 1 Engine Torque Tuning ($)")]
    public int stage1TuningCost = 1500;
    [Tooltip("Cost for Stage 2 Engine Torque Tuning ($)")]
    public int stage2TuningCost = 3500;
    [Tooltip("Cost for Stage 3 Engine Torque Tuning ($)")]
    public int stage3TuningCost = 7000;

    [Header("--- ENGINE TUNING TORQUE MULTIPLIERS ---")]
    [Tooltip("Torque multiplier for Stage 1 (e.g. 1.15 = +15% Torque)")]
    public float stage1TorqueMultiplier = 1.15f;
    [Tooltip("Torque multiplier for Stage 2 (e.g. 1.30 = +30% Torque)")]
    public float stage2TorqueMultiplier = 1.30f;
    [Tooltip("Torque multiplier for Stage 3 (e.g. 1.50 = +50% Torque - MAX)")]
    public float stage3TorqueMultiplier = 1.50f;

    [Header("--- GARAGE BAY TRIGGER ---")]
    [Tooltip("Trigger transform where car is placed/parked for service")]
    public Transform serviceBayAnchor;

    public float serviceDistance = 6.5f;

    public static event Action<DrivableVehicle> OnVehicleRepaired;
    public static event Action<DrivableVehicle, int> OnVehicleTuned;
    public static event Action<DrivableVehicle, Color> OnVehicleRepainted;

    private void Awake()
    {
        Instance = this;
        if (propertyComponent == null) propertyComponent = GetComponent<PurchasableProperty>();
    }

    public bool IsGarageUnlocked()
    {
        if (propertyComponent == null)
        {
            propertyComponent = GetComponent<PurchasableProperty>();
            if (propertyComponent == null) propertyComponent = GetComponentInParent<PurchasableProperty>();
            if (propertyComponent == null) propertyComponent = GetComponentInChildren<PurchasableProperty>();
            if (propertyComponent == null)
            {
                PurchasableProperty[] all = UnityEngine.Object.FindObjectsByType<PurchasableProperty>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var p in all)
                {
                    if (p != null && (p.propertyType == PropertyType.AutoServiceGarage || p.propertyId.Contains("service") || p.propertyId.Contains("garage")))
                    {
                        propertyComponent = p;
                        break;
                    }
                }
            }
        }

        if (propertyComponent != null) return propertyComponent.IsUnlocked;
        return false;
    }

    public Vector3 GetBayPosition()
    {
        if (serviceBayAnchor != null) return serviceBayAnchor.position;

        Transform bayChild = transform.Find("ServiceBayAnchor");
        if (bayChild == null) bayChild = transform.Find("ServiceBay");
        if (bayChild == null) bayChild = transform.Find("InteractionAnchor");
        if (bayChild != null) return bayChild.position;

        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null) return transform.TransformPoint(box.center);

        return transform.position;
    }

    public bool IsVehicleInServiceBay(DrivableVehicle v)
    {
        if (v == null || !IsGarageUnlocked()) return false;

        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null && box.bounds.Contains(v.transform.position))
        {
            return true;
        }

        Vector3 bayPos = GetBayPosition();
        Vector3 diff = v.transform.position - bayPos;
        float horizontalDist = new Vector2(diff.x, diff.z).magnitude;
        float verticalDist = Mathf.Abs(diff.y);

        float maxRadius = serviceDistance > 0 ? Mathf.Min(serviceDistance, 5.0f) : 4.5f;
        if (horizontalDist <= maxRadius && verticalDist <= 2.8f)
        {
            return true;
        }

        return false;
    }

    public DrivableVehicle FindActiveVehicleInBay()
    {
        if (!IsGarageUnlocked()) return null;

        DrivableVehicle[] vehicles = UnityEngine.Object.FindObjectsByType<DrivableVehicle>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var v in vehicles)
        {
            if (v != null && IsVehicleInServiceBay(v))
            {
                return v;
            }
        }
        return null;
    }

    public string GetGaragePromptForVehicle(DrivableVehicle v)
    {
        if (v == null || !IsGarageUnlocked()) return string.Empty;
        return LocalizationManager.Get("prompt_open_menu", "<color=#FFD232>[F] Open Menu</color>");
    }

    public void CheckGarageShortcutInputs(DrivableVehicle v)
    {
        if (v == null || !IsGarageUnlocked()) return;

        bool rPressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            rPressed = Keyboard.current.rKey.wasPressedThisFrame;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        try
        {
            if (Input.GetKeyDown(KeyCode.R)) rPressed = true;
        }
        catch { }
#endif

        if (rPressed)
        {
            TryRepairVehicle(v);
        }
    }

    public bool TryRepairVehicle(DrivableVehicle v)
    {
        if (v == null || !IsGarageUnlocked()) return false;

        if (PlayerEconomyManager.Instance != null && PlayerEconomyManager.Instance.SpendMoney(repairCost))
        {
            v.currentCondition = v.maxCondition;
            PlayerPrefs.SetFloat(DrivableVehicle.CONDITION_SAVE_PREFIX + v.EffectiveVehicleId, v.maxCondition);
            PlayerPrefs.Save();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayGarageRepair(v.transform.position);
            }

            OnVehicleRepaired?.Invoke(v);

            if (InteractionPromptHUD.Instance != null)
            {
                InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.GetFormat("prompt_garage_repaired", v.vehicleName), 2.5f);
                InteractionPromptHUD.Instance.UpdateVehicleHUD(v.currentFuel, v.maxFuel, v.currentFuel < (v.maxFuel * 0.18f), v.currentCondition, v.maxCondition);
            }

            return true;
        }

        if (InteractionPromptHUD.Instance != null)
            InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.Get("prompt_insufficient_funds", "<color=#FF3333>Insufficient Funds!</color>"), 2.0f);

        return false;
    }

    public int GetVehicleTuningStage(string vehicleId)
    {
        if (string.IsNullOrEmpty(vehicleId)) return 0;
        string cleanId = vehicleId.ToLower().Trim();
        if (PlayerPrefs.HasKey("Vehicle_TuningStage_" + cleanId))
        {
            return PlayerPrefs.GetInt("Vehicle_TuningStage_" + cleanId, 0);
        }
        return PlayerPrefs.GetInt("Vehicle_TuningStage_" + vehicleId, 0);
    }

    public int GetTuningCostForStage(int stage)
    {
        switch (stage)
        {
            case 1: return stage1TuningCost;
            case 2: return stage2TuningCost;
            case 3: return stage3TuningCost;
            default: return 0;
        }
    }

    public float GetTorqueMultiplierForStage(int stage)
    {
        switch (stage)
        {
            case 1: return stage1TorqueMultiplier;
            case 2: return stage2TorqueMultiplier;
            case 3: return stage3TorqueMultiplier;
            default: return 1.00f;
        }
    }

    public bool TryTuneVehicle(DrivableVehicle v)
    {
        if (v == null || !IsGarageUnlocked()) return false;
        int currentStage = GetVehicleTuningStage(v.EffectiveVehicleId);
        if (currentStage >= 3)
        {
            if (InteractionPromptHUD.Instance != null)
                InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.Get("prompt_garage_tune_max", "<color=#FFAA33>This vehicle has reached maximum engine tuning!</color>"), 2.0f);
            return false;
        }

        int nextStage = currentStage + 1;
        int cost = GetTuningCostForStage(nextStage);

        if (PlayerEconomyManager.Instance != null && PlayerEconomyManager.Instance.SpendMoney(cost))
        {
            PlayerPrefs.SetInt("Vehicle_TuningStage_" + v.EffectiveVehicleId, nextStage);
            PlayerPrefs.SetInt("Vehicle_TuningStage_" + v.vehicleId, nextStage);
            PlayerPrefs.Save();

            CarController cc = v.GetComponent<CarController>();
            if (cc != null)
            {
                cc.tuningTorqueMultiplier = GetTorqueMultiplierForStage(nextStage);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayGarageRepair(v.transform.position);
            }

            OnVehicleTuned?.Invoke(v, nextStage);

            if (InteractionPromptHUD.Instance != null)
                InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.GetFormat("prompt_garage_tuned_success", v.vehicleName, nextStage, (GetTorqueMultiplierForStage(nextStage) - 1f) * 100f), 2.5f);

            return true;
        }

        if (InteractionPromptHUD.Instance != null)
            InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.Get("prompt_insufficient_funds", "<color=#FF3333>Insufficient Funds!</color>"), 2.0f);

        return false;
    }

    [ContextMenu("Reset All Vehicle Tuning")]
    public static void ResetAllVehicleTuning()
    {
        DrivableVehicle[] allVehicles = UnityEngine.Object.FindObjectsByType<DrivableVehicle>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var v in allVehicles)
        {
            if (v != null)
            {
                PlayerPrefs.DeleteKey("Vehicle_TuningStage_" + v.EffectiveVehicleId);
                PlayerPrefs.DeleteKey("Vehicle_TuningStage_" + v.vehicleId);
                CarController cc = v.GetComponent<CarController>();
                if (cc != null) cc.tuningTorqueMultiplier = 1.0f;
            }
        }
        string[] commonIds = new string[] { "pickup_truck", "cargo_van", "driveable_van", "drivable_pickup", "drivable_van", "cargo_van_01" };
        foreach (var id in commonIds)
        {
            PlayerPrefs.DeleteKey("Vehicle_TuningStage_" + id);
        }
        PlayerPrefs.Save();
        Debug.Log("<color=#FF5555>[DEV RESET] All vehicle tuning stages reset to 0.</color>");
    }

    public bool TryRepaintVehicle(DrivableVehicle v, Color chosenColor)
    {
        if (v == null || !IsGarageUnlocked()) return false;

        // Check if player has enough money for repainting
        if (PlayerEconomyManager.Instance != null && repaintCost > 0)
        {
            if (!PlayerEconomyManager.Instance.SpendMoney(repaintCost))
            {
                if (InteractionPromptHUD.Instance != null)
                    InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.GetFormat("prompt_insufficient_funds_fuel", repaintCost), 2.5f);
                return false;
            }
        }

        // Apply color immediately to the vehicle
        v.ApplyPaintColor(chosenColor, true);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGaragePaint(v.transform.position);
        }

        OnVehicleRepainted?.Invoke(v, chosenColor);

        string hex = ColorUtility.ToHtmlStringRGB(chosenColor);
        if (InteractionPromptHUD.Instance != null)
            InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.GetFormat("prompt_garage_painted_success", hex, v.vehicleName), 2.5f);

        return true;
    }
}
