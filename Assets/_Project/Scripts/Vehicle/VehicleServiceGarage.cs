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

        Vector3 bayPos = GetBayPosition();
        float d = Vector3.Distance(bayPos, v.transform.position);
        float effectiveRadius = Mathf.Max(serviceDistance, 16.0f);
        if (d <= effectiveRadius) return true;

        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null && box.bounds.Contains(v.transform.position))
        {
            return true;
        }

        return false;
    }

    public DrivableVehicle FindActiveVehicleInBay()
    {
        if (!IsGarageUnlocked()) return null;

        Vector3 bayPos = GetBayPosition();
        DrivableVehicle[] vehicles = UnityEngine.Object.FindObjectsByType<DrivableVehicle>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        DrivableVehicle closest = null;
        float minDist = Mathf.Max(serviceDistance, 16.0f);

        foreach (var v in vehicles)
        {
            if (v == null) continue;
            float d = Vector3.Distance(bayPos, v.transform.position);
            if (d <= minDist)
            {
                minDist = d;
                closest = v;
            }
        }
        return closest;
    }

    public string GetGaragePromptForVehicle(DrivableVehicle v)
    {
        if (v == null || !IsGarageUnlocked()) return string.Empty;
        int stage = GetVehicleTuningStage(v.vehicleId);
        int nextCost = GetTuningCostForStage(stage + 1);
        string tuneInfo = stage < 3 ? $"[T] Tork (${nextCost:N0})" : "[T] Tork: MAX";

        return $"<color=#32D2FF>[R] Tamir (${repairCost})</color> | " +
               $"<color=#FF77FF>{tuneInfo}</color>";
    }

    public void CheckGarageShortcutInputs(DrivableVehicle v)
    {
        if (v == null || !IsGarageUnlocked()) return;

        bool rPressed = false;
        bool tPressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            rPressed = Keyboard.current.rKey.wasPressedThisFrame;
            tPressed = Keyboard.current.tKey.wasPressedThisFrame;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        try
        {
            if (Input.GetKeyDown(KeyCode.R)) rPressed = true;
            if (Input.GetKeyDown(KeyCode.T)) tPressed = true;
        }
        catch { }
#endif

        if (rPressed)
        {
            TryRepairVehicle(v);
        }
        if (tPressed)
        {
            TryTuneVehicle(v);
        }
    }

    public bool TryRepairVehicle(DrivableVehicle v)
    {
        if (v == null || !IsGarageUnlocked()) return false;

        if (PlayerEconomyManager.Instance != null && PlayerEconomyManager.Instance.SpendMoney(repairCost))
        {
            v.currentFuel = v.maxFuel;
            v.currentCondition = v.maxCondition;
            PlayerPrefs.SetFloat(DrivableVehicle.FUEL_SAVE_PREFIX + v.EffectiveVehicleId, v.maxFuel);
            PlayerPrefs.SetFloat(DrivableVehicle.CONDITION_SAVE_PREFIX + v.EffectiveVehicleId, v.maxCondition);
            PlayerPrefs.Save();

            OnVehicleRepaired?.Invoke(v);

            if (InteractionPromptHUD.Instance != null)
                InteractionPromptHUD.Instance.ShowPrompt($"<color=#32FF64>✅ {v.vehicleName} Tamir Edildi & Bakımı Yapıldı!</color>", 2.5f);

            return true;
        }

        if (InteractionPromptHUD.Instance != null)
            InteractionPromptHUD.Instance.ShowPrompt("<color=#FF3333>Yetersiz Bakiye!</color>", 2.0f);

        return false;
    }

    public int GetVehicleTuningStage(string vehicleId)
    {
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
        int currentStage = GetVehicleTuningStage(v.vehicleId);
        if (currentStage >= 3)
        {
            if (InteractionPromptHUD.Instance != null)
                InteractionPromptHUD.Instance.ShowPrompt("<color=#FFAA33>Bu araç maksimum motor modifiyesine ulaştı!</color>", 2.0f);
            return false;
        }

        int nextStage = currentStage + 1;
        int cost = GetTuningCostForStage(nextStage);

        if (PlayerEconomyManager.Instance != null && PlayerEconomyManager.Instance.SpendMoney(cost))
        {
            PlayerPrefs.SetInt("Vehicle_TuningStage_" + v.vehicleId, nextStage);
            PlayerPrefs.Save();

            CarController cc = v.GetComponent<CarController>();
            if (cc != null)
            {
                cc.tuningTorqueMultiplier = GetTorqueMultiplierForStage(nextStage);
            }

            OnVehicleTuned?.Invoke(v, nextStage);

            if (InteractionPromptHUD.Instance != null)
                InteractionPromptHUD.Instance.ShowPrompt($"<color=#32FF64>🚀 {v.vehicleName} Motor Stage {nextStage} Yapıldı! (+{(GetTorqueMultiplierForStage(nextStage)-1f)*100:0}% Tork)</color>", 2.5f);

            return true;
        }

        if (InteractionPromptHUD.Instance != null)
            InteractionPromptHUD.Instance.ShowPrompt("<color=#FF3333>Yetersiz Bakiye!</color>", 2.0f);

        return false;
    }
}
