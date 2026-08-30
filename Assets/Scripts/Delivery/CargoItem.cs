using System;
using UnityEngine;

[System.Serializable]
public class CargoItem
{
    [Header("--- KARGO BİLGİLERİ ---")]
    public string trackingNumber;          // Örn: "#KRG-1042"
    public string recipientName;           // Örn: "Ahmet Yılmaz"
    public string targetAddress;           // Örn: "Papatya Sokak No: 4"
    public string targetAddressDescription;// Örn: "Kırmızı çatılı, bahçesinde mavi çiçekler olan ev"
    public string targetPointId;           // Eşleşen DeliveryPoint ID'si
    
    [Header("--- EKONOMİ ---")]
    public int deliveryReward = 50;        // Doğru teslimat ödülü (TL)
    public int wrongDeliveryPenalty = 100; // Hatalı teslimat cezası (TL)

    [Header("--- DURUM ---")]
    public bool isDelivered = false;
    public bool isDeliveredCorrectly = false;
    public string deliveredToAddressName = "";

    public CargoItem(string tracking, string recipient, string address, string description, string pointId, int reward = 50, int penalty = 100)
    {
        this.trackingNumber = tracking;
        this.recipientName = recipient;
        this.targetAddress = address;
        this.targetAddressDescription = description;
        this.targetPointId = pointId;
        this.deliveryReward = reward;
        this.wrongDeliveryPenalty = penalty;
        this.isDelivered = false;
        this.isDeliveredCorrectly = false;
        this.deliveredToAddressName = "";
    }
}
