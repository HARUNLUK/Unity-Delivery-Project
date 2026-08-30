using System;
using UnityEngine;

public class DeliveryPoint : MonoBehaviour
{
    [Header("--- ADRES & KİMLİK TANIMI ---")]
    [Tooltip("Bu teslimat noktasının adresi (Örn: Papatya Sokak No: 4)")]
    public string addressName = "Papatya Sokak No: 4";

    [Tooltip("Oyuncunun evi bulabilmesi için ipucu / adres tarifi")]
    [TextArea(2, 5)]
    public string addressDescription = "Kırmızı çatılı, önünde mavi çiçekler ve beyaz çit olan ev.";
    
    [Tooltip("Bu noktanın benzersiz kimliği (Örn: Point_01, Point_02...)")]
    public string pointId = "Point_01";

    [Header("--- GÖRSEL İŞARETÇİ ---")]
    public GameObject visualMarker;

    [Header("--- DURUM ---")]
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
        Gizmos.DrawWireSphere(transform.position, 3f);
    }
}
