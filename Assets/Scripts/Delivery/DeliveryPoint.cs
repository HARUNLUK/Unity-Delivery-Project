using System;
using UnityEngine;

public class DeliveryPoint : MonoBehaviour
{
    [Header("--- ADRES TANIMI ---")]
    [Tooltip("Bu teslimat noktasının adresi")]
    public string addressName = "Atatürk Cad. No: 1";
    
    [Tooltip("Benzersiz nokta kimliği")]
    public string pointId = "Point_01";

    [Header("--- GÖRSEL İŞARETÇİ ---")]
    [Tooltip("Yerde parlayan çember / efekt objesi")]
    public GameObject visualMarker;

    [Header("--- DURUM ---")]
    [SerializeField] private bool isPlayerInside = false;

    // Araç alana girdiğinde ve çıktığında tetiklenen event'ler
    public static event Action<DeliveryPoint> OnDeliveryZoneEntered;
    public static event Action<DeliveryPoint> OnDeliveryZoneExited;

    public bool IsPlayerInside => isPlayerInside;

    private void OnTriggerEnter(Collider other)
    {
        // Alana giren objenin araç olup olmadığını kontrol et
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
        // Scene ekranında teslimat alanını sarı küre olarak göster
        Gizmos.color = isPlayerInside ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 3f);
    }
}
