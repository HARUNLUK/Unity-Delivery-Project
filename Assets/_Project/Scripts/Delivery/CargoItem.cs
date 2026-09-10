using System;
using UnityEngine;

public enum CargoType
{
    Standard,
    Fragile,
    Express,
    Explosive
}

[System.Serializable]
public class CargoItem
{
    [Header("--- CARGO DATA ---")]
    public string trackingNumber;          // e.g. "#CRG-1042"
    public string recipientName;           // e.g. "John Smith"
    public string targetAddress;           // e.g. "104 Maple Street"
    public string targetAddressDescription;// e.g. "Red roof house with white fences"
    public string targetPointId;           // Matching DeliveryPoint ID
    public CargoType cargoType = CargoType.Standard;
    
    [Header("--- ECONOMY ---")]
    public int deliveryReward = 50;        // Reward for correct delivery ($)
    public int wrongDeliveryPenalty = 100; // Penalty for incorrect delivery ($)
    public int bonusReward = 0;

    [Header("--- STATUS ---")]
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
