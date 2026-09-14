using System;
using UnityEngine;

public class DeliveryPoint : MonoBehaviour
{
    [Header("--- DELIVERY POINT CONFIG ---")]
    [Tooltip("Unique Index / ID of this point matching localization JSON (e.g. 1, 2, 3...)")]
    public string pointId = "1";

    [Tooltip("Required Player / Branch Level to receive deliveries here")]
    public int requiredLevel = 1;

    // Fields hidden from Inspector as they are dynamically resolved from JSON localization
    [HideInInspector] public string districtName = "Maple Town";
    [HideInInspector] public string recipientName = "";
    [HideInInspector] public string addressName = "";
    [HideInInspector] [TextArea(2, 5)] public string addressDescription = "";

    public string EffectiveDescription => AddressLocalizationManager.GetDescription(pointId, addressDescription);
    public string EffectiveAddressName => AddressLocalizationManager.GetAddressName(pointId, string.IsNullOrEmpty(addressName) ? "Maple Town" : addressName);
    public string EffectiveRecipient => AddressLocalizationManager.GetRecipient(pointId, string.IsNullOrEmpty(recipientName) ? "Resident" : recipientName);

    [Header("--- VISUAL MARKER ---")]
    public GameObject visualMarker;

    [Header("--- STATE ---")]
    [SerializeField] private bool isPlayerInside = false;
    [SerializeField] private bool isFulfilled = false;

    public static event Action<DeliveryPoint> OnDeliveryZoneEntered;
    public static event Action<DeliveryPoint> OnDeliveryZoneExited;

    public bool IsPlayerInside => isPlayerInside;
    public bool IsFulfilled
    {
        get => isFulfilled;
        set => isFulfilled = value;
    }

    private void OnTriggerEnter(Collider other)
    {
        CarController car = other.GetComponentInParent<CarController>();
        if (car != null)
        {
            isPlayerInside = true;
            OnDeliveryZoneEntered?.Invoke(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        CarController car = other.GetComponentInParent<CarController>();
        if (car != null)
        {
            isPlayerInside = false;
            OnDeliveryZoneExited?.Invoke(this);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isFulfilled ? Color.gray : (isPlayerInside ? Color.green : Color.yellow);
        Gizmos.DrawWireSphere(transform.position, 3.5f);
    }
}
