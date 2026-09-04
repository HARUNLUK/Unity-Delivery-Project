using System;
using UnityEngine;

public enum CargoType
{
    Standard,
    Fragile,
    Express
}

[System.Serializable]
public class CargoItem
{
    [Header("--- KARGO BİLGİLERİ ---")]
    public string trackingNumber;          // Örn: "#KRG-1042"
    public string recipientName;           // Örn: "Ahmet Yılmaz"
    public string targetAddress;           // Örn: "Papatya Sokak No: 4"
    public string targetAddressDescription;// Örn: "Kırmızı çatılı, bahçesinde mavi çiçekler olan ev"
    public string targetPointId;           // Eşleşen DeliveryPoint ID'si
    public CargoType cargoType = CargoType.Standard;
    
    [Header("--- EKONOMİ ---")]
    public int deliveryReward = 50;        // Doğru teslimat ödülü (TL)
    public int wrongDeliveryPenalty = 100; // Hatalı teslimat cezası (TL)
    public int bonusReward = 0;

    [Header("--- DURUM ---")]
    public bool isDelivered = false;
    public bool isDeliveredCorrectly = false;
    public string deliveredToAddressName = "";

    public string targetAddressName
    {
        get => targetAddress;
        set => targetAddress = value;
    }

    public CargoItem()
    {
        this.trackingNumber = "#PKG-1001";
        this.recipientName = "Customer";
        this.targetAddress = "Main Street";
        this.targetAddressDescription = "";
        this.targetPointId = "1";
        this.deliveryReward = 50;
        this.wrongDeliveryPenalty = 100;
        this.isDelivered = false;
        this.isDeliveredCorrectly = false;
        this.deliveredToAddressName = "";
    }

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
