using System;
using UnityEngine;

public class DeliveryPoint : MonoBehaviour
{
    [Header("--- ADDRESS & IDENTITY ---")]
    [Tooltip("Unique Index / ID of this point (e.g. 1, 2, 3...)")]
    public string pointId = "1";

    [Tooltip("Recipient Name (Person or Business receiving the parcel)")]
    public string recipientName = "John Doe";

    [Tooltip("Address name of this delivery destination (e.g. 104 Maple Street)")]
    public string addressName = "104 Maple Street";

    [Tooltip("Visual hint / description of the house for the player")]
    [TextArea(2, 5)]
    public string addressDescription = "Two-story suburban house with front yard and wooden fence.";

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
