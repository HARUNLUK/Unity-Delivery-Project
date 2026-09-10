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

    [Header("--- GARAGE SERVICE COSTS ---")]
    public int repairCost = 150;
    public int stage1TuningCost = 1500;  // +15% Tork
    public int stage2TuningCost = 3500;  // +30% Tork
    public int stage3TuningCost = 7000;  // +50% Tork

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
        if (propertyComponent != null) return propertyComponent.IsUnlocked;
        return true;
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
        if (v == null) return false;

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
        if (v == null) return string.Empty;
        int stage = GetVehicleTuningStage(v.vehicleId);
        int nextCost = GetTuningCostForStage(stage + 1);
        string tuneInfo = stage < 3 ? $"[T] Tork (${nextCost:N0})" : "[T] Tork: MAX";

        return $"<color=#32D2FF>[R] Tamir (${repairCost})</color> | " +
               $"<color=#FF77FF>{tuneInfo}</color>";
    }

    public void CheckGarageShortcutInputs(DrivableVehicle v)
    {
        if (v == null) return;

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
        if (v == null) return false;

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
            case 1: return 1.15f; // +15%
            case 2: return 1.30f; // +30%
            case 3: return 1.50f; // +50%
            default: return 1.00f;
        }
    }

    public bool TryTuneVehicle(DrivableVehicle v)
    {
        if (v == null) return false;
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
