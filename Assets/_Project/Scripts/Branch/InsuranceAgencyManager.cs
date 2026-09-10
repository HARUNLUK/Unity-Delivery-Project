using System;
using UnityEngine;

public class InsuranceAgencyManager : MonoBehaviour
{
    public static InsuranceAgencyManager Instance { get; private set; }

    [Header("--- PROPERTY LINK ---")]
    public PurchasableProperty propertyComponent;

    [Header("--- INSURANCE TIER UPGRADE COSTS ($) ---")]
    [Tooltip("Upgrade cost to unlock Tier 2 (Silver Insurance)")]
    public int tier2UpgradeCost = 3000;

    [Tooltip("Upgrade cost to unlock Tier 3 (Gold Full Insurance)")]
    public int tier3UpgradeCost = 7500;

    [Header("--- TIER 1 STATS (Included with building) ---")]
    [Range(0f, 1f), Tooltip("Penalty multiplier for broken fragile cargo (e.g. 0.70 = 30% discount)")]
    public float tier1FragileMultiplier = 0.70f;
    [Range(0f, 1f), Tooltip("Penalty multiplier for wrong address deliveries (1.0 = no discount)")]
    public float tier1WrongAddressMultiplier = 1.00f;
    [Range(0f, 1f), Tooltip("Penalty multiplier for undelivered / lost cargo at day end (1.0 = no discount)")]
    public float tier1UndeliveredMultiplier = 1.00f;

    [Header("--- TIER 2 STATS (Silver) ---")]
    [Range(0f, 1f), Tooltip("Penalty multiplier for broken fragile cargo (e.g. 0.40 = 60% discount)")]
    public float tier2FragileMultiplier = 0.40f;
    [Range(0f, 1f), Tooltip("Penalty multiplier for wrong address deliveries (e.g. 0.50 = 50% discount)")]
    public float tier2WrongAddressMultiplier = 0.50f;
    [Range(0f, 1f), Tooltip("Penalty multiplier for undelivered / lost cargo at day end (e.g. 0.50 = 50% discount)")]
    public float tier2UndeliveredMultiplier = 0.50f;

    [Header("--- TIER 3 STATS (Gold Full Coverage) ---")]
    [Range(0f, 1f), Tooltip("Penalty multiplier for broken fragile cargo (0.0 = 100% full coverage)")]
    public float tier3FragileMultiplier = 0.00f;
    [Range(0f, 1f), Tooltip("Penalty multiplier for wrong address deliveries (e.g. 0.25 = 75% discount)")]
    public float tier3WrongAddressMultiplier = 0.25f;
    [Range(0f, 1f), Tooltip("Penalty multiplier for undelivered / lost cargo at day end (0.0 = 100% full coverage / 0 penalty)")]
    public float tier3UndeliveredMultiplier = 0.00f;

    [Header("--- RUNTIME STATE ---")]
    [SerializeField] private int insuranceTier = 1; // 1: Basic, 2: Silver, 3: Gold

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
        if (propertyComponent == null)
        {
            propertyComponent = GetComponent<PurchasableProperty>();
            if (propertyComponent == null) propertyComponent = GetComponentInParent<PurchasableProperty>();
            if (propertyComponent == null) propertyComponent = GetComponentInChildren<PurchasableProperty>();
            if (propertyComponent == null)
            {
                PurchasableProperty[] all = UnityEngine.Object.FindObjectsByType<PurchasableProperty>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var p in all)
                {
                    if (p != null && (p.propertyType == PropertyType.InsuranceAgency || p.propertyId.Contains("insurance")))
                    {
                        propertyComponent = p;
                        break;
                    }
                }
            }
        }

        if (propertyComponent != null) return propertyComponent.IsUnlocked;
        return false;
    }

    /// <summary>
    /// Returns the multiplier applied to broken fragile cargo penalties (e.g. 0.70 for Tier 1, 0.40 for Tier 2, 0.0 for Tier 3).
    /// </summary>
    public float GetFragilePenaltyMultiplier()
    {
        if (!IsAgencyUnlocked()) return 1.0f; // No insurance

        switch (insuranceTier)
        {
            case 1: return tier1FragileMultiplier;
            case 2: return tier2FragileMultiplier;
            case 3: return tier3FragileMultiplier;
            default: return tier1FragileMultiplier;
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
            case 1: return tier1WrongAddressMultiplier;
            case 2: return tier2WrongAddressMultiplier;
            case 3: return tier3WrongAddressMultiplier;
            default: return 1.00f;
        }
    }

    /// <summary>
    /// Returns the multiplier applied to undelivered / lost cargo penalties at day end.
    /// </summary>
    public float GetUndeliveredPenaltyMultiplier()
    {
        if (!IsAgencyUnlocked()) return 1.0f;

        switch (insuranceTier)
        {
            case 1: return tier1UndeliveredMultiplier;
            case 2: return tier2UndeliveredMultiplier;
            case 3: return tier3UndeliveredMultiplier;
            default: return 1.00f;
        }
    }

    public int GetNextTierCost()
    {
        switch (insuranceTier)
        {
            case 1: return tier2UpgradeCost;
            case 2: return tier3UpgradeCost;
            default: return 0;
        }
    }

    public string GetTierName()
    {
        switch (insuranceTier)
        {
            case 1:
                int d1 = Mathf.RoundToInt((1f - tier1FragileMultiplier) * 100f);
                return $"Temel Kasko (%{d1} Hasar İndirimi)";
            case 2:
                int d2Frag = Mathf.RoundToInt((1f - tier2FragileMultiplier) * 100f);
                int d2Wrong = Mathf.RoundToInt((1f - tier2WrongAddressMultiplier) * 100f);
                int d2Undel = Mathf.RoundToInt((1f - tier2UndeliveredMultiplier) * 100f);
                return $"Gümüş Kasko (%{d2Frag} Hasar, %{d2Wrong} Yanlış Adres, %{d2Undel} Teslimat Koruması)";
            case 3:
                int d3Wrong = Mathf.RoundToInt((1f - tier3WrongAddressMultiplier) * 100f);
                return $"Altın Tam Kasko (%100 Tam Koruma & %{d3Wrong} Yanlış Adres İndirimi)";
            default:
                return "Kasko";
        }
    }

    public string GetUpgradePromptText()
    {
        if (!IsAgencyUnlocked()) return string.Empty;

        if (insuranceTier >= 3)
        {
            return "<color=#32FF64>🛡️ Altın Tam Kasko: MAKSİMUM SEVİYE (%100 Hasar Koruması)</color>";
        }

        int nextCost = GetNextTierCost();
        int targetTier = insuranceTier + 1;
        string nextTierName = targetTier == 2 ? $"Gümüş Kasko (%{Mathf.RoundToInt((1f - tier2FragileMultiplier) * 100f)} İndirim)" : $"Altın Tam Kasko (%100 Koruma)";
        int balance = PlayerEconomyManager.Instance != null ? PlayerEconomyManager.Instance.CurrentLiveBalance : 0;

        if (balance < nextCost)
        {
            return $"<color=#FFAA33>🛡️ {nextTierName} - ${nextCost:N0} (Bakiye: ${balance:N0})</color>";
        }

        return $"<color=#32FF64>[E] Kaskoyu Yükselt: {nextTierName} (${nextCost:N0})</color>";
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
                InteractionPromptHUD.Instance.ShowPrompt($"<color=#32FF64>🛡️ Sigorta Yükseltildi: {GetTierName()}</color>");
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
