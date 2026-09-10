using System;
using UnityEngine;

public class InsuranceAgencyManager : MonoBehaviour
{
    public static InsuranceAgencyManager Instance { get; private set; }

    [Header("--- PROPERTY LINK ---")]
    public PurchasableProperty propertyComponent;

    [Header("--- INSURANCE TIERS ---")]
    [SerializeField] private int insuranceTier = 1; // 1: Basic (30%), 2: Silver (60%), 3: Gold (100%)

    public int InsuranceTier => insuranceTier;

    public static event Action<int> OnInsuranceTierUpgraded;

    private const string PREF_INSURANCE_TIER = "Delivery_InsuranceTier";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (propertyComponent == null) propertyComponent = GetComponent<PurchasableProperty>();
        LoadTier();
    }

    public void LoadTier()
    {
        insuranceTier = PlayerPrefs.GetInt(PREF_INSURANCE_TIER, 1);
    }

    public void SaveTier()
    {
        PlayerPrefs.SetInt(PREF_INSURANCE_TIER, insuranceTier);
        PlayerPrefs.Save();
    }

    public bool IsAgencyUnlocked()
    {
        if (propertyComponent != null) return propertyComponent.IsUnlocked;
        return true;
    }

    /// <summary>
    /// Returns the multiplier applied to broken fragile cargo penalties (e.g. 0.70 for Tier 1, 0.40 for Tier 2, 0.0 for Tier 3).
    /// </summary>
    public float GetFragilePenaltyMultiplier()
    {
        if (!IsAgencyUnlocked()) return 1.0f; // No insurance

        switch (insuranceTier)
        {
            case 1: return 0.70f; // 30% discount
            case 2: return 0.40f; // 60% discount
            case 3: return 0.00f; // 100% full coverage (0 penalty!)
            default: return 0.70f;
        }
    }

    /// <summary>
    /// Returns the multiplier applied to wrong address delivery penalties.
    /// </summary>
    public float GetWrongDeliveryPenaltyMultiplier()
    {
        if (!IsAgencyUnlocked()) return 1.0f;

        switch (insuranceTier)
        {
            case 1: return 1.00f; // No wrong address coverage in tier 1
            case 2: return 0.50f; // 50% discount on wrong address
            case 3: return 0.25f; // 75% discount on wrong address
            default: return 1.00f;
        }
    }

    public int GetNextTierCost()
    {
        switch (insuranceTier)
        {
            case 1: return 3000;
            case 2: return 7500;
            default: return 0;
        }
    }

    public bool TryUpgradeTier()
    {
        if (!IsAgencyUnlocked()) return false;
        if (insuranceTier >= 3) return false;

        int cost = GetNextTierCost();
        if (PlayerEconomyManager.Instance != null && PlayerEconomyManager.Instance.SpendMoney(cost))
        {
            insuranceTier++;
            SaveTier();
            OnInsuranceTierUpgraded?.Invoke(insuranceTier);

            if (InteractionPromptHUD.Instance != null)
            {
                string tierName = insuranceTier == 2 ? "Gümüş Kasko (%60 İndirim)" : "Altın Tam Kasko (%100 Koruma)";
                InteractionPromptHUD.Instance.ShowPrompt($"<color=#32FF64>🛡️ Sigorta Yükseltildi: {tierName}</color>");
            }
            return true;
        }

        return false;
    }

    [ContextMenu("Reset Insurance Tier")]
    public void DevResetTier()
    {
        insuranceTier = 1;
        PlayerPrefs.DeleteKey(PREF_INSURANCE_TIER);
        PlayerPrefs.Save();
    }
}
