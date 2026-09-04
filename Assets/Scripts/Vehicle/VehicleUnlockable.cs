using System;
using UnityEngine;

[RequireComponent(typeof(DrivableVehicle))]
public class VehicleUnlockable : MonoBehaviour
{
    [Header("--- IDENTITY & CATALOG ---")]
    [Tooltip("Unique ID for saving purchase state (e.g. pickup_starter, cargo_van_01)")]
    public string vehicleId = "cargo_van_01";

    [Tooltip("Display name in UI and HUD")]
    public string displayName = "Heavy Cargo Van";

    [TextArea(2, 4)]
    [Tooltip("Short description of the vehicle")]
    public string description = "Yüksek koli taşıma kapasitesine sahip ticari dağıtım aracı.";

    [Header("--- ECONOMY & REQUIREMENTS ---")]
    [Tooltip("Purchase price in TL/USD")]
    public int purchasePrice = 2500;

    [Tooltip("Required player progression level to buy")]
    public int requiredPlayerLevel = 1;

    [Tooltip("Approximate cargo package capacity")]
    public int cargoCapacity = 14;

    [Tooltip("Starter vehicle is unlocked immediately")]
    public bool isUnlockedByDefault = false;

    [Header("--- SPOTS & ANCHORS ---")]
    [Tooltip("Default showroom/parking spot where vehicle sits")]
    public Transform parkingSpotTransform;

    [Tooltip("Warehouse garage spawn spot when recalled/spawned")]
    public Transform garageSpawnTransform;

    public static event Action<VehicleUnlockable> OnVehiclePurchased;
    public static event Action<VehicleUnlockable> OnVehicleRecalled;

    private const string SAVE_PREFIX = "DELIVERY_VEHICLE_UNLOCKED_";

    private DrivableVehicle drivableVehicle;
    private Rigidbody rb;

    public bool IsUnlocked
    {
        get
        {
            if (isUnlockedByDefault) return true;
            return PlayerPrefs.GetInt(SAVE_PREFIX + vehicleId, 0) == 1;
        }
    }

    private void Awake()
    {
        drivableVehicle = GetComponent<DrivableVehicle>();
        rb = GetComponent<Rigidbody>();
        
        // Sadece kimlik boş bırakılmışsa otomatik doldur
        if (string.IsNullOrEmpty(vehicleId))
        {
            vehicleId = gameObject.name.ToLower().Replace(" ", "_");
        }
        if (string.IsNullOrEmpty(displayName))
        {
            displayName = gameObject.name;
        }
    }

    private void Reset()
    {
        // Editörde component ilk eklendiğinde akıllı varsayılanları ata
        drivableVehicle = GetComponent<DrivableVehicle>();
        string n = (gameObject.name + " " + (drivableVehicle != null ? drivableVehicle.vehicleName : "")).ToLower();
        
        if (n.Contains("pickup"))
        {
            vehicleId = "pickup_truck";
            displayName = "Pickup Truck";
            purchasePrice = 0;
            isUnlockedByDefault = true;
            cargoCapacity = 8;
            requiredPlayerLevel = 1;
            description = "Başlangıç seviyesi çevik ve pratik kargo kamyoneti.";
        }
        else if (n.Contains("van"))
        {
            vehicleId = "cargo_van";
            displayName = "Delivery Van";
            purchasePrice = 1000;
            isUnlockedByDefault = false;
            cargoCapacity = 16;
            requiredPlayerLevel = 1;
            description = "Geniş kapalı bagaj hacmi ile yüksek kapasiteli teslimatlar için ideal kargo aracı.";
        }
    }

    private void Start()
    {
        UpdateLockVisualsAndPhysics();
    }

    public void UpdateLockVisualsAndPhysics()
    {
        // If locked and player is somehow inside, kick player out
        if (!IsUnlocked && drivableVehicle != null)
        {
            if (drivableVehicle.isPlayerInside && drivableVehicle.currentPlayer != null)
            {
                drivableVehicle.ExitVehicle();
            }

            if (drivableVehicle.carController != null)
            {
                drivableVehicle.carController.enabled = false;
            }
        }
    }

    /// <summary>
    /// Checks level and wallet balance, buys and permanently unlocks vehicle.
    /// </summary>
    public bool TryPurchase()
    {
        if (IsUnlocked) return true;

        int currentLvl = PlayerProgressionManager.Instance != null ? PlayerProgressionManager.Instance.PlayerLevel : 1;
        if (currentLvl < requiredPlayerLevel)
        {
            Debug.LogWarning($"[VehicleUnlockable] Level yetersiz! Gereken: {requiredPlayerLevel}, Mevcut: {currentLvl}");
            return false;
        }

        int balance = PlayerEconomyManager.Instance != null ? PlayerEconomyManager.Instance.CurrentLiveBalance : 0;
        if (balance < purchasePrice)
        {
            Debug.LogWarning($"[VehicleUnlockable] Bakiye yetersiz! Gereken: {purchasePrice}, Mevcut: {balance}");
            return false;
        }

        // Deduct money
        if (PlayerEconomyManager.Instance != null)
        {
            PlayerEconomyManager.Instance.DeductCash(purchasePrice);
        }

        // Save unlock state
        PlayerPrefs.SetInt(SAVE_PREFIX + vehicleId, 1);
        PlayerPrefs.Save();

        UpdateLockVisualsAndPhysics();
        Debug.Log($"<color=#32FF64>★ TEBRİKLER! '{displayName}' başarıyla satın alındı ve kilidi açıldı! ★</color>");

        OnVehiclePurchased?.Invoke(this);
        return true;
    }

    /// <summary>
    /// Forces vehicle unlock (e.g. dev tool or cheat)
    /// </summary>
    public void ForceUnlock()
    {
        PlayerPrefs.SetInt(SAVE_PREFIX + vehicleId, 1);
        PlayerPrefs.Save();
        UpdateLockVisualsAndPhysics();
        OnVehiclePurchased?.Invoke(this);
    }

    /// <summary>
    /// Resets vehicle back to lock state (dev tool)
    /// </summary>
    public void ResetLockState()
    {
        if (!isUnlockedByDefault)
        {
            PlayerPrefs.DeleteKey(SAVE_PREFIX + vehicleId);
            PlayerPrefs.Save();
            UpdateLockVisualsAndPhysics();
        }
    }

    /// <summary>
    /// Recalls vehicle safely to the designated garage spawn point or parking point.
    /// Clears velocities and puts wheels flat on the ground.
    /// </summary>
    public void RecallToGarage()
    {
        Transform targetAnchor = garageSpawnTransform != null ? garageSpawnTransform : parkingSpotTransform;
        if (targetAnchor == null)
        {
            Debug.LogWarning($"[VehicleUnlockable] '{displayName}' için tanımlı Garage veya Parking noktası bulunamadı!");
            return;
        }

        if (drivableVehicle != null && drivableVehicle.isPlayerInside)
        {
            drivableVehicle.ExitVehicle();
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        transform.position = targetAnchor.position + Vector3.up * 0.35f;
        transform.rotation = targetAnchor.rotation;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Debug.Log($"[VehicleUnlockable] '{displayName}' garaj noktasına ({targetAnchor.name}) ışınlandı.");
        OnVehicleRecalled?.Invoke(this);
    }
}
