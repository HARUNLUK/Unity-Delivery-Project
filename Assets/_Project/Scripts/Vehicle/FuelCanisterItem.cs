using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Portable fuel canister (Jerrycan / Benzin Bidonu).
/// Can be bought from gas stations, physically stored in the vehicle cargo bed (bagaj/kasa),
/// held with PhysicsGrabber, and automatically refuels the vehicle when approached with the can in hand.
/// </summary>
[RequireComponent(typeof(CarriableItem))]
[RequireComponent(typeof(Rigidbody))]
public class FuelCanisterItem : MonoBehaviour
{
    [Header("--- FUEL CANISTER SETTINGS ---")]
    [Tooltip("Amount of fuel in liters to restore to the vehicle (Default: 10L)")]
    public float fuelAmount = 10f;

    [Tooltip("Maximum distance from the vehicle to trigger refueling")]
    public float refuelDistance = 5.0f;

    [Tooltip("Cooldown after being picked up before refueling can trigger (prevents accidental instant trigger)")]
    public float grabCooldown = 0.35f;

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
    private float grabbedTime = -999f;
    private bool wasCarriedLastFrame = false;
    private bool isConsuming = false;
    private float lastFullWarningTime = 0f;

    private void Awake()
    {
        carriableItem = GetComponent<CarriableItem>();
        rb = GetComponent<Rigidbody>();

        if (carriableItem != null)
        {
            carriableItem.itemId = "fuel_canister";
            carriableItem.nameKey = "item_fuel_canister_name";
            carriableItem.fallbackName = "Benzin Bidonu";
            carriableItem.useCustomHoldRotation = true;
            carriableItem.customHoldRotation = holdRotationOffset;
        }
    }

    public DrivableVehicle NearbyVehicle { get; private set; }

    private void Update()
    {
        if (isConsuming) return;
        if (carriableItem == null || rb == null) return;

        bool isHeld = carriableItem.isBeingCarried || (PhysicsGrabber.Instance != null && PhysicsGrabber.Instance.grabbedRb == rb);

        // When stored in the vehicle cargo bed and NOT held in hands, never refuel!
        if (carriableItem.isInVehicleBed && !isHeld)
        {
            wasCarriedLastFrame = false;
            NearbyVehicle = null;
            return;
        }

        if (isHeld && !wasCarriedLastFrame)
        {
            grabbedTime = Time.time;
        }
        wasCarriedLastFrame = isHeld;

        // Must be actively carried in player's hand
        if (!isHeld)
        {
            NearbyVehicle = null;
            return;
        }

        // Prevent instant trigger right on the first frames of grabbing
        if (Time.time - grabbedTime < grabCooldown) return;

        // Find nearest drivable vehicle
        DrivableVehicle targetVehicle = FindNearbyVehicle();
        NearbyVehicle = targetVehicle;

        // When FPSPlayerController is present, it centrally manages showing the prompt and checking [E] key
        if (FPSPlayerController.Instance != null)
        {
            return;
        }

        // Standalone fallback (if FPSPlayerController is not present in scene):
        if (targetVehicle != null)
        {
            // If tank is already 100% full, don't waste the canister!
            if (targetVehicle.currentFuel >= targetVehicle.maxFuel - 0.05f)
            {
                if (Time.time - lastFullWarningTime > 2.0f)
                {
                    lastFullWarningTime = Time.time;
                    if (InteractionPromptHUD.Instance != null)
                    {
                        InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.Get("prompt_gas_station_full", "<color=#32FF64>[DEPO DOLU] Aracın yakıt deposu zaten tamamen dolu!</color>"));
                    }
                }
                return;
            }

            // Show prompt: [E] Benzin Doldur (+10L)
            if (InteractionPromptHUD.Instance != null)
            {
                InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.GetFormat("prompt_refuel_vehicle_with_canister", targetVehicle.vehicleName, fuelAmount));
            }

            // Only perform refuel when [E] is pressed
            if (CheckRefuelInput())
            {
                PerformRefuel(targetVehicle);
            }
        }
    }

    private bool CheckRefuelInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) return true;
        if (Gamepad.current != null && (Gamepad.current.buttonWest.wasPressedThisFrame || Gamepad.current.buttonNorth.wasPressedThisFrame)) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        try
        {
            if (Input.GetKeyDown(KeyCode.E)) return true;
        }
        catch { }
#endif
        return false;
    }

    public string GetDisplayName()
    {
        if (carriableItem != null) return carriableItem.GetLocalizedName();
        return LocalizationManager.Get("item_fuel_canister_name", "Benzin Bidonu");
    }

    public DrivableVehicle FindNearbyVehicle()
    {
        if (FPSPlayerController.Instance == null || FPSPlayerController.Instance.playerCamera == null) return null;

        Camera cam = FPSPlayerController.Instance.playerCamera;
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        // 1. Raycast check: must look directly at the vehicle within refuelDistance (e.g. 2.8m)
        if (Physics.Raycast(ray, out RaycastHit hit, refuelDistance, ~0, QueryTriggerInteraction.Collide))
        {
            // Do not refuel if aiming into the cargo bed or at the tailgate (player wants to put the can in the trunk)
            if (hit.collider.GetComponentInParent<VehicleCargoBed>() == null &&
                hit.collider.GetComponent<VehicleCargoBed>() == null &&
                hit.collider.GetComponentInParent<VehicleTailgate>() == null &&
                hit.collider.GetComponent<VehicleTailgate>() == null)
            {
                DrivableVehicle v = hit.collider.GetComponentInParent<DrivableVehicle>();
                if (v == null) v = hit.collider.GetComponent<DrivableVehicle>();
                if (v != null && v.gameObject.activeInHierarchy) return v;
            }
        }

        // 2. Proximity fallback: must be very close (2.4m) AND directly facing the vehicle
        Vector3 playerPos = FPSPlayerController.Instance.transform.position;
        DrivableVehicle[] vehicles = UnityEngine.Object.FindObjectsByType<DrivableVehicle>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var veh in vehicles)
        {
            if (veh == null || !veh.gameObject.activeInHierarchy) continue;

            float dist = Vector3.Distance(playerPos, veh.transform.position);
            if (dist <= 2.4f)
            {
                Vector3 dirToVeh = (veh.transform.position - cam.transform.position).normalized;
                if (Vector3.Dot(cam.transform.forward, dirToVeh) > 0.45f)
                {
                    return veh;
                }
            }
        }

        return null;
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
            string msg = LocalizationManager.Get("prompt_fuel_canister_used", "<color=#32FF64>✔ Benzin Eklendi</color>");
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
