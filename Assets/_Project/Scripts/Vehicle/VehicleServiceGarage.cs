using System;
using System.Collections.Generic;
using UnityEngine;

public class VehicleServiceGarage : MonoBehaviour
{
    public static VehicleServiceGarage Instance { get; private set; }

    [Header("--- PROPERTY LINK ---")]
    public PurchasableProperty propertyComponent;

    [Header("--- GARAGE SERVICE COSTS ---")]
    public int repairCost = 150;
    public int paintJobCost = 300;
    public int stage1TuningCost = 1500;  // +15% Tork
    public int stage2TuningCost = 3500;  // +30% Tork
    public int stage3TuningCost = 7000;  // +50% Tork

    [Header("--- GARAGE BAY TRIGGER ---")]
    [Tooltip("Trigger transform where car is placed/parked for service")]
    public Transform serviceBayAnchor;

    public float serviceDistance = 6.5f;

    [Header("--- COLOR PRESETS ---")]
    public List<Color> paintPresets = new List<Color>()
    {
        new Color(0.95f, 0.95f, 0.95f, 1f), // White (Default)
        new Color(0.98f, 0.78f, 0.12f, 1f), // Express Yellow
        new Color(0.85f, 0.15f, 0.15f, 1f), // Crimson Red
        new Color(0.15f, 0.45f, 0.85f, 1f), // Royal Blue
        new Color(0.18f, 0.18f, 0.20f, 1f), // Matte Black
        new Color(0.20f, 0.65f, 0.35f, 1f), // Forest Green
        new Color(0.95f, 0.45f, 0.10f, 1f)  // Sunset Orange
    };

    public static event Action<DrivableVehicle> OnVehicleRepaired;
    public static event Action<DrivableVehicle, Color> OnVehiclePainted;
    public static event Action<DrivableVehicle, int> OnVehicleTuned;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

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
        return transform.position;
    }

    public bool IsVehicleInServiceBay(DrivableVehicle v)
    {
        if (!IsGarageUnlocked() || v == null) return false;
        float d = Vector3.Distance(GetBayPosition(), v.transform.position);
        return d <= serviceDistance;
    }

    public DrivableVehicle FindActiveVehicleInBay()
    {
        if (!IsGarageUnlocked()) return null;

        Vector3 bayPos = GetBayPosition();
        DrivableVehicle[] vehicles = UnityEngine.Object.FindObjectsByType<DrivableVehicle>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        DrivableVehicle closest = null;
        float minDist = serviceDistance;

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
               $"<color=#FFD232>[C] Renk (${paintJobCost})</color> | " +
               $"<color=#FF77FF>{tuneInfo}</color>";
    }

    public void CheckGarageShortcutInputs(DrivableVehicle v)
    {
        if (v == null || !IsGarageUnlocked()) return;

        // [R] Repair
        if (Input.GetKeyDown(KeyCode.R))
        {
            TryRepairVehicle(v);
        }

        // [C] Paint
        if (Input.GetKeyDown(KeyCode.C))
        {
            CycleVehicleColor(v);
        }

        // [T] Tune
        if (Input.GetKeyDown(KeyCode.T))
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
            OnVehicleRepaired?.Invoke(v);

            if (InteractionPromptHUD.Instance != null)
                InteractionPromptHUD.Instance.ShowPrompt($"<color=#32FF64>✅ {v.vehicleName} Tamir Edildi & Bakımı Yapıldı!</color>", 2.5f);

            return true;
        }

        if (InteractionPromptHUD.Instance != null)
            InteractionPromptHUD.Instance.ShowPrompt("<color=#FF3333>Yetersiz Bakiye!</color>", 2.0f);

        return false;
    }

    public bool CycleVehicleColor(DrivableVehicle v)
    {
        if (v == null || paintPresets.Count == 0) return false;

        if (PlayerEconomyManager.Instance != null && PlayerEconomyManager.Instance.SpendMoney(paintJobCost))
        {
            int colorIdx = (PlayerPrefs.GetInt("Vehicle_ColorIdx_" + v.vehicleId, 0) + 1) % paintPresets.Count;
            PlayerPrefs.SetInt("Vehicle_ColorIdx_" + v.vehicleId, colorIdx);
            PlayerPrefs.Save();

            Color newColor = paintPresets[colorIdx];
            ApplyColorToVehicle(v, newColor);
            OnVehiclePainted?.Invoke(v, newColor);

            if (InteractionPromptHUD.Instance != null)
                InteractionPromptHUD.Instance.ShowPrompt($"<color=#32FF64>🎨 {v.vehicleName} Yeni Rengine Boyandı!</color>", 2.5f);

            return true;
        }

        if (InteractionPromptHUD.Instance != null)
            InteractionPromptHUD.Instance.ShowPrompt("<color=#FF3333>Yetersiz Bakiye!</color>", 2.0f);

        return false;
    }

    public void ApplyColorToVehicle(DrivableVehicle v, Color c)
    {
        if (v == null) return;
        Renderer[] renderers = v.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (r.name.ToLower().Contains("body") || r.name.ToLower().Contains("car") || r.name.ToLower().Contains("van"))
            {
                foreach (var mat in r.materials)
                {
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                    else if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
                }
            }
        }
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
