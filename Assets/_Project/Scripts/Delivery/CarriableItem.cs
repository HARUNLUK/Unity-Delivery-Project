using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A loose item (weapon crate prop, tools, ...) that can be found in the world, picked up with the same grab
/// as parcels, thrown / dropped, and loaded into a vehicle's cargo bed. It is not a delivery parcel:
/// it has no recipient, never appears in the notebook or the day-end receipt, and costs no penalty.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CarriableItem : MonoBehaviour
{
    public static readonly List<CarriableItem> All = new List<CarriableItem>();

    [Header("--- ITEM ---")]
    public string itemId = "weapon";

    [Tooltip("Localization key of the display name (falls back to Fallback Name).")]
    public string nameKey = "item_weapon_name";
    public string fallbackName = "Silah";

    [Header("--- CARRY STATE (runtime) ---")]
    public bool isBeingCarried;
    public bool isInVehicleBed;
    public VehicleCargoBed currentCargoBed;
    public float throwExemptionUntil;

    [Header("--- HOLD ORIENTATION ---")]
    [Tooltip("If true, overrides PhysicsGrabber default hold rotation with customHoldRotation")]
    public bool useCustomHoldRotation = false;

    [Tooltip("Custom rotation Euler offset relative to camera when held in player's hands")]
    public Vector3 customHoldRotation = Vector3.zero;

    public bool IsRecentlyThrown => Time.time < throwExemptionUntil;

    private void OnEnable()
    {
        if (!All.Contains(this)) All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
        if (currentCargoBed != null) currentCargoBed.RemoveItem(this);
    }

    /// <summary>Thrown items ignore the cargo bed stabilizer for a moment so they can fly freely.</summary>
    public void MarkAsThrown(float duration = 1.5f)
    {
        throwExemptionUntil = Time.time + duration;
        isInVehicleBed = false;
        if (currentCargoBed != null)
        {
            currentCargoBed.RemoveItem(this);
            currentCargoBed = null;
        }
    }

    public string GetLocalizedName()
    {
        return LocalizationManager.Get(nameKey, fallbackName);
    }
}
