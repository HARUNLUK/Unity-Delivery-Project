using System;
using System.Collections.Generic;
using UnityEngine;

public class VehicleShowroomManager : MonoBehaviour
{
    public static VehicleShowroomManager Instance { get; private set; }

    [Header("--- REGISTERED VEHICLES ---")]
    [Tooltip("All drivable vehicles present in the scene/world")]
    public List<DrivableVehicle> showroomVehicles = new List<DrivableVehicle>();

    [Header("--- SHARED WAREHOUSE GARAGE SPAWN POINT ---")]
    [Tooltip("Global fallback spawn point outside the warehouse")]
    public Transform warehouseGarageSpawnPoint;

    public static event Action OnShowroomUpdated;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        AutoDetectVehicles();
    }

    private void Start()
    {
        AutoDetectVehicles();
        UpdateAllGarageSpawnPoints();
    }

    private void OnEnable()
    {
        DrivableVehicle.OnVehiclePurchased += HandleVehiclePurchased;
        DrivableVehicle.OnVehicleRecalled += HandleVehicleRecalled;
    }

    private void OnDisable()
    {
        DrivableVehicle.OnVehiclePurchased -= HandleVehiclePurchased;
        DrivableVehicle.OnVehicleRecalled -= HandleVehicleRecalled;
    }

    public void AutoDetectVehicles()
    {
        DrivableVehicle[] found = UnityEngine.Object.FindObjectsByType<DrivableVehicle>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var v in found)
        {
            if (!showroomVehicles.Contains(v))
            {
                showroomVehicles.Add(v);
            }
        }
    }

    public void UpdateAllGarageSpawnPoints()
    {
        if (warehouseGarageSpawnPoint == null)
        {
            GameObject existingSpawn = GameObject.Find("Warehouse_Garage_SpawnPoint");
            if (existingSpawn == null) existingSpawn = GameObject.Find("GarageSpawnPoint");
            if (existingSpawn == null) existingSpawn = GameObject.Find("Warehouse_SpawnPoint");
            if (existingSpawn == null) existingSpawn = GameObject.Find("DeliveryPoint_1");
            if (existingSpawn == null) existingSpawn = GameObject.Find("Warehouse");

            if (existingSpawn != null)
            {
                warehouseGarageSpawnPoint = existingSpawn.transform;
            }
        }

        foreach (var v in showroomVehicles)
        {
            if (v != null && v.garageSpawnTransform == null && warehouseGarageSpawnPoint != null)
            {
                v.garageSpawnTransform = warehouseGarageSpawnPoint;
            }
        }
    }

    private void HandleVehiclePurchased(DrivableVehicle v)
    {
        OnShowroomUpdated?.Invoke();
    }

    private void HandleVehicleRecalled(DrivableVehicle v)
    {
        OnShowroomUpdated?.Invoke();
    }

    public List<DrivableVehicle> GetAllVehicles()
    {
        AutoDetectVehicles();
        return showroomVehicles;
    }

    public DrivableVehicle GetVehicleById(string id)
    {
        return showroomVehicles.Find(v => v != null && v.vehicleId == id);
    }

    private void Update()
    {
        if (CheckF9Input())
        {
            ResetAllVehiclePurchases();
            if (InteractionPromptHUD.Instance != null)
            {
                InteractionPromptHUD.Instance.ShowPrompt("<color=#FF5555>[DEV RESET] ALL VEHICLE PURCHASES RESET (F9)</color>");
            }
        }
    }

    private bool CheckF9Input()
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.f9Key.wasPressedThisFrame)
        {
            return true;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        try
        {
            if (Input.GetKeyDown(KeyCode.F9))
            {
                return true;
            }
        }
        catch { }
#endif

        return false;
    }

    [ContextMenu("Reset All Vehicle Purchases (Dev)")]
    public void ResetAllVehiclePurchases()
    {
        AutoDetectVehicles();
        foreach (var v in showroomVehicles)
        {
            if (v != null) v.ResetLockState();
        }
        OnShowroomUpdated?.Invoke();
        Debug.Log("<color=#FF5555>[DEV] F9 key: All vehicle purchases have been reset and locked.</color>");
    }
}
