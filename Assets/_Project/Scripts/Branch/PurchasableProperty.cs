using System;
using UnityEngine;

public enum PropertyType
{
    VehicleShowroom,     // Araç Galerisi
    InsuranceAgency,     // Kargo Sigorta & Güvenlik Acentesi
    AutoServiceGarage,   // Oto Servis, Tamir & Modifiye Garajı
    LogisticsHub,        // Pasif Bölge Dağıtım Şubesi
    Custom
}

public class PurchasableProperty : MonoBehaviour
{
    [Header("--- PROPERTY IDENTITY ---")]
    [Tooltip("Unique ID for save system")]
    public string propertyId = "property_showroom";

    [Tooltip("Display name shown in prompts and HUD")]
    public string displayName = "Araç Galerisi";

    [TextArea(2, 4)]
    public string description = "Yeni kargo araçları satın alabileceğiniz showroom ve galeri.";

    public PropertyType propertyType = PropertyType.VehicleShowroom;

    [Header("--- REQUIREMENTS & COST ---")]
    [Tooltip("Player level required to purchase this property")]
    public int requiredPlayerLevel = 2;

    [Tooltip("Purchase cost in dollars")]
    public int purchaseCost = 8000;

    [Header("--- VISUAL OBJECTS ---")]
    [Tooltip("Root object containing closed shutters, for sale signs, and warning lights")]
    public GameObject closedStateRoot;

    [Tooltip("Root object containing open doors, interior lights, showroom cars, and terminals")]
    public GameObject unlockedStateRoot;

    [Tooltip("Optional 3D 'SATILIK / FOR SALE' signboard")]
    public GameObject forSaleSignboard;

    [Header("--- INTERACTION PROMPT ---")]
    [Tooltip("Trigger transform where player interacts with the property")]
    public Transform interactionAnchor;

    public float interactionDistance = 3.5f;

    [Header("--- RUNTIME STATE ---")]
    [SerializeField] private bool isUnlocked = false;

    public bool IsUnlocked => isUnlocked;

    public static event Action<PurchasableProperty> OnPropertyUnlocked;

    private const string PREF_KEY_PREFIX = "Property_Unlocked_";

    private void Awake()
    {
        LoadState();
        UpdateVisuals();
    }

    private void Start()
    {
        UpdateVisuals();
    }

    public void LoadState()
    {
        isUnlocked = PlayerPrefs.GetInt(PREF_KEY_PREFIX + propertyId, 0) == 1;
    }

    public void SaveState()
    {
        PlayerPrefs.SetInt(PREF_KEY_PREFIX + propertyId, isUnlocked ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void UpdateVisuals()
    {
        if (closedStateRoot != null) closedStateRoot.SetActive(!isUnlocked);
        if (unlockedStateRoot != null) unlockedStateRoot.SetActive(isUnlocked);
        if (forSaleSignboard != null) forSaleSignboard.SetActive(!isUnlocked);
    }

    public string GetPromptText()
    {
        if (isUnlocked) return string.Empty;

        int branchLevel = BranchManager.Instance != null ? BranchManager.Instance.CurrentBranchLevel : (PlayerProgressionManager.Instance != null ? PlayerProgressionManager.Instance.WarehouseLevel : 1);
        int playerBalance = PlayerEconomyManager.Instance != null ? PlayerEconomyManager.Instance.CurrentLiveBalance : 0;

        if (branchLevel < requiredPlayerLevel)
        {
            return $"<color=#FF4444>[KİLİTLİ] {displayName} - Şube Level {requiredPlayerLevel} Gerekiyor!</color>";
        }
        else if (playerBalance < purchaseCost)
        {
            return $"<color=#FFAA33>[SATILIK] {displayName} - ${purchaseCost:N0} (Bakiye: ${playerBalance:N0})</color>";
        }
        else
        {
            return $"<color=#32FF64>[E] SATIN AL: {displayName} (${purchaseCost:N0})</color>";
        }
    }

    public bool TryPurchase()
    {
        if (isUnlocked) return false;

        int branchLevel = BranchManager.Instance != null ? BranchManager.Instance.CurrentBranchLevel : (PlayerProgressionManager.Instance != null ? PlayerProgressionManager.Instance.WarehouseLevel : 1);
        if (branchLevel < requiredPlayerLevel)
        {
            if (InteractionPromptHUD.Instance != null)
                InteractionPromptHUD.Instance.ShowPrompt($"<color=#FF3333>Şube seviyeniz yetersiz! (Gereken: Level {requiredPlayerLevel})</color>");
            return false;
        }

        if (PlayerEconomyManager.Instance != null)
        {
            if (PlayerEconomyManager.Instance.SpendMoney(purchaseCost))
            {
                UnlockProperty();
                return true;
            }
            else
            {
                if (InteractionPromptHUD.Instance != null)
                    InteractionPromptHUD.Instance.ShowPrompt("<color=#FF3333>Yetersiz Bakiye!</color>");
                return false;
            }
        }

        return false;
    }

    public void UnlockProperty()
    {
        isUnlocked = true;
        SaveState();
        UpdateVisuals();

        Debug.Log($"<color=#32FF64>[PROPERTY UNLOCKED] {displayName} başarıyla satın alındı ve açıldı!</color>");

        if (InteractionPromptHUD.Instance != null)
        {
            InteractionPromptHUD.Instance.ShowPrompt($"<color=#32FF64>🎉 {displayName} Açıldı!</color>", 3.0f);
        }

        OnPropertyUnlocked?.Invoke(this);
    }

    [ContextMenu("Unlock Property (Free Dev)")]
    public void DevForceUnlock()
    {
        isUnlocked = true;
        SaveState();
        UpdateVisuals();
        OnPropertyUnlocked?.Invoke(this);
    }

    [ContextMenu("Reset Property Lock State")]
    public void DevResetLock()
    {
        isUnlocked = false;
        PlayerPrefs.DeleteKey(PREF_KEY_PREFIX + propertyId);
        PlayerPrefs.Save();
        UpdateVisuals();
    }

    public static void ResetAllPropertiesInGame()
    {
        PurchasableProperty[] allProps = UnityEngine.Object.FindObjectsByType<PurchasableProperty>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var p in allProps)
        {
            if (p != null)
            {
                p.DevResetLock();
            }
        }

        string[] defaultPropertyIds = new string[] { "property_showroom", "property_insurance", "property_insurance_agency", "property_service_garage", "property_garage", "property_logistics", "property_logistics_hub" };
        foreach (var id in defaultPropertyIds)
        {
            PlayerPrefs.DeleteKey(PREF_KEY_PREFIX + id);
        }

        if (InsuranceAgencyManager.Instance != null)
        {
            InsuranceAgencyManager.Instance.DevResetTier();
        }

        if (PassiveDispatchManager.Instance != null)
        {
            PassiveDispatchManager.Instance.DevResetLevel();
        }

        PlayerPrefs.Save();
        Debug.Log("<color=#FF5555>[DEV RESET] Tüm dükkanlar ve ticari mülkler sıfırlandı!</color>");
    }
}
