using UnityEngine;

/// <summary>
/// Portable fuel canister (Jerrycan / Benzin Bidonu).
/// Bought from the gas station stand. The moment it touches a vehicle (held, thrown or dropped)
/// it pours its fuel into the tank and is consumed. A full tank keeps the canister.
/// Canisters stored in a cargo bed never auto-refuel.
/// </summary>
[RequireComponent(typeof(CarriableItem))]
[RequireComponent(typeof(Rigidbody))]
public class FuelCanisterItem : MonoBehaviour
{
    [Header("--- FUEL CANISTER SETTINGS ---")]
    [Tooltip("Amount of fuel in liters to restore to the vehicle (Default: 10L)")]
    public float fuelAmount = 10f;

    [Tooltip("Seconds after spawning during which contact is ignored (bed restore / shelf spawn overlaps)")]
    public float spawnGraceTime = 1.0f;

    [Header("--- HOLD ORIENTATION ---")]
    [Tooltip("Hold rotation offset relative to camera when held in hands (Default: (-90, 90, 0) for Gas_Can)")]
    public Vector3 holdRotationOffset = new Vector3(-90f, 90f, 0f);

    [Header("--- 🛒 SHELF SALE STATE ---")]
    [Tooltip("If true, this canister is on a store shelf and requires purchase before being taken")]
    public bool isForSaleOnShelf = false;
    public FuelCanisterShelf shelfOwner = null;
    public int slotIndex = -1;

    private CarriableItem carriableItem;
    private Rigidbody rb;
    private bool isConsuming = false;
    private float lastFullWarningTime = 0f;
    private float spawnTime;

    private void Awake()
    {
        carriableItem = GetComponent<CarriableItem>();
        rb = GetComponent<Rigidbody>();
        spawnTime = Time.time;

        if (carriableItem != null)
        {
            carriableItem.itemId = "fuel_canister";
            carriableItem.nameKey = "item_fuel_canister_name";
            carriableItem.fallbackName = "Benzin Bidonu";
            carriableItem.useCustomHoldRotation = true;
            carriableItem.customHoldRotation = holdRotationOffset;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isConsuming || isForSaleOnShelf) return;
        if (Time.time - spawnTime < spawnGraceTime) return;
        if (carriableItem != null && carriableItem.isInVehicleBed) return;

        DrivableVehicle vehicle = collision.collider.GetComponentInParent<DrivableVehicle>();
        if (vehicle == null || !vehicle.gameObject.activeInHierarchy) return;

        if (vehicle.currentFuel >= vehicle.maxFuel - 0.05f)
        {
            if (Time.time - lastFullWarningTime > 2.0f)
            {
                lastFullWarningTime = Time.time;
                if (InteractionPromptHUD.Instance != null)
                {
                    InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.GetFormat("prompt_gas_station_full", vehicle.currentFuel, vehicle.maxFuel));
                }
            }
            return;
        }

        PerformRefuel(vehicle);
    }

    public string GetDisplayName()
    {
        if (carriableItem != null) return carriableItem.GetLocalizedName();
        return LocalizationManager.Get("item_fuel_canister_name", "Benzin Bidonu");
    }

    public void PerformRefuel(DrivableVehicle vehicle)
    {
        if (isConsuming) return;
        if (vehicle == null) return;
        isConsuming = true;

        // Release from player grabber safely
        if (PhysicsGrabber.Instance != null && PhysicsGrabber.Instance.grabbedRb == rb)
        {
            PhysicsGrabber.Instance.ReleaseObject();
        }

        float beforeFuel = vehicle.currentFuel;
        vehicle.Refuel(fuelAmount);
        float addedFuel = vehicle.currentFuel - beforeFuel;

        // Play audio feedback
        if (AudioManager.Instance != null)
        {
            if (AudioManager.Instance.fuelPumpFinishBeep != null)
            {
                AudioManager.Instance.PlayFuelPumpFinish(vehicle.transform.position);
            }
            else
            {
                AudioManager.Instance.PlayNotification();
            }
        }

        // Show HUD prompt
        if (InteractionPromptHUD.Instance != null)
        {
            string msg = LocalizationManager.GetFormat("prompt_fuel_canister_used", addedFuel, vehicle.currentFuel, vehicle.maxFuel, vehicle.FuelPercentage * 100f);
            InteractionPromptHUD.Instance.ShowPrompt(msg);
        }

        // Remove from cargo bed if stored before destroying
        if (carriableItem != null && carriableItem.currentCargoBed != null)
        {
            carriableItem.currentCargoBed.RemoveItem(carriableItem);
        }

        // Destroy the used canister
        Destroy(gameObject);
    }
}
