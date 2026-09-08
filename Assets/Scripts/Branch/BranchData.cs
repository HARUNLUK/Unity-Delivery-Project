using System;
using UnityEngine;

[Serializable]
public class BranchTier
{
    [Header("--- TIER IDENTITY ---")]
    [Tooltip("Unique branch level index (1, 2, 3...)")]
    public int tierLevel = 1;

    [Tooltip("Display name of the branch tier")]
    public string tierName = "Starter Garage";

    [TextArea(2, 4)]
    [Tooltip("Description of the branch tier and its perks")]
    public string description = "Entry-level parcel warehouse and dispatch office.";

    [Header("--- ECONOMY & REQUIREMENTS ---")]
    [Tooltip("Cost in TL to upgrade TO this tier (0 for starter level 1)")]
    public int upgradeCost = 0;

    [Tooltip("Required player level to unlock and upgrade to this tier")]
    public int requiredPlayerLevel = 1;

    [Tooltip("Daily maximum package generation limit")]
    public int dailyPackageCapacity = 4;

    [Tooltip("Daily warehouse rent deducted at the end of the day")]
    public int dailyRent = 50;

    [Header("--- 3D PREFAB & VISUALS ---")]
    [Tooltip("Prefab instantiated/enabled when this tier is active")]
    public GameObject branchPrefab;

    [Tooltip("If already placed in scene, direct scene GameObject reference for this tier")]
    public GameObject sceneBuildingRoot;
}
