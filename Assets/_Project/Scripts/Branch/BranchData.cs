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
    [Tooltip("Description of the branch tier and its overview")]
    public string description = "Entry-level parcel warehouse and dispatch office.";

    [Header("--- UNLOCKS & PERKS (INSPECTOR'DAN DÜZENLENEBİLİR) ---")]
    [Tooltip("Bu seviyede açılan özellikler ve avantajlar (Tablet ve geçiş ekranında madde madde listelenir)")]
    public string[] unlockedPerks = new string[0];

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

    [Header("--- KARAKTER SPAWN & BAKIŞ NOKTASI (SEVİYE BAZLI - OPSİYONEL) ---")]
    [Tooltip("Bu seviyeye yükseltildiğinde karakterin dışarıda doğacağı özel Transform noktası (Boşsa BranchManager üzerindeki genel spawn noktası kullanılır)")]
    public Transform exteriorSpawnPoint;

    [Tooltip("Karakterin doğduğunda bakacağı hedef Transform (Boşsa doğrudan şube binasına bakar)")]
    public Transform lookTarget;

    [Tooltip("Dış mekan spawn pozisyonu için şube merkezinden yerel ofset (Spawn noktası Transform'u atanmamışsa kullanılır)")]
    public Vector3 customExteriorOffset = Vector3.zero;

    /// <summary>
    /// Returns the localized tier name based on current language or falls back to tierName.
    /// </summary>
    public string GetLocalizedName()
    {
        string key = $"branch_tier{tierLevel}_name";
        if (LocalizationManager.HasKey(key)) return LocalizationManager.Get(key);
        return tierName;
    }

    /// <summary>
    /// Returns the localized tier description based on current language or falls back to description.
    /// </summary>
    public string GetLocalizedDescription()
    {
        string key = $"branch_tier{tierLevel}_desc";
        if (LocalizationManager.HasKey(key)) return LocalizationManager.Get(key);
        return description;
    }

    /// <summary>
    /// Returns the localized list of perks for this tier.
    /// </summary>
    public string[] GetLocalizedPerks()
    {
        if (unlockedPerks != null && unlockedPerks.Length > 0)
        {
            string[] result = new string[unlockedPerks.Length];
            for (int i = 0; i < unlockedPerks.Length; i++)
            {
                string p = unlockedPerks[i];
                string key = $"branch_tier{tierLevel}_perk{i + 1}";
                if (LocalizationManager.HasKey(key))
                {
                    result[i] = LocalizationManager.Get(key);
                }
                else if (LocalizationManager.HasKey(p))
                {
                    result[i] = LocalizationManager.Get(p);
                }
                else
                {
                    result[i] = p;
                }
            }
            return result;
        }

        var list = new System.Collections.Generic.List<string>();
        for (int i = 1; i <= 6; i++)
        {
            string key = $"branch_tier{tierLevel}_perk{i}";
            if (LocalizationManager.HasKey(key))
            {
                list.Add(LocalizationManager.Get(key));
            }
        }
        return list.ToArray();
    }

    /// <summary>
    /// Formats the unlocked perks into a clean bulleted list string.
    /// </summary>
    public string GetFormattedPerksText()
    {
        string[] perks = GetLocalizedPerks();
        if (perks != null && perks.Length > 0)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var p in perks)
            {
                if (!string.IsNullOrWhiteSpace(p))
                {
                    sb.AppendLine($"• {p.Trim()}");
                }
            }
            return sb.ToString().TrimEnd();
        }
        return string.Empty;
    }
}
