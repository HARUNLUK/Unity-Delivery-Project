using System;
using UnityEngine;

public class PassiveDispatchManager : MonoBehaviour
{
    public static PassiveDispatchManager Instance { get; private set; }

    [Header("--- PROPERTY LINK ---")]
    public PurchasableProperty propertyComponent;

    [Header("--- DISPATCH HUB UPGRADE COSTS ($) ---")]
    [Tooltip("Upgrade cost to unlock Level 2 (5 Couriers)")]
    public int level2UpgradeCost = 6000;

    [Tooltip("Upgrade cost to unlock Level 3 (10 Couriers - Mega Hub)")]
    public int level3UpgradeCost = 14000;

    [Header("--- LEVEL 1 STATS (Included with building) ---")]
    [Tooltip("Number of active couriers at Level 1")]
    public int level1Couriers = 2;
    [Tooltip("Daily passive revenue generated at Level 1 ($)")]
    public int level1DailyRevenue = 850;

    [Header("--- LEVEL 2 STATS ---")]
    [Tooltip("Number of active couriers at Level 2")]
    public int level2Couriers = 5;
    [Tooltip("Daily passive revenue generated at Level 2 ($)")]
    public int level2DailyRevenue = 2200;

    [Header("--- LEVEL 3 STATS (Max) ---")]
    [Tooltip("Number of active couriers at Level 3")]
    public int level3Couriers = 10;
    [Tooltip("Daily passive revenue generated at Level 3 ($)")]
    public int level3DailyRevenue = 4800;

    [Header("--- RUNTIME STATE ---")]
    [SerializeField] private int dispatchHubLevel = 1;

    [Header("--- TODAY'S PASSIVE REVENUE ---")]
    [SerializeField] private int todayPassiveEarned = 0;

    public int DispatchHubLevel => dispatchHubLevel;
    public int TodayPassiveEarned => todayPassiveEarned;

    public static event Action<int> OnDispatchHubUpgraded;
    public static event Action<int> OnPassiveIncomeDeposited;

    private const string PREF_DISPATCH_LEVEL = "Delivery_PassiveDispatchLevel";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (propertyComponent == null) propertyComponent = GetComponent<PurchasableProperty>();
        LoadLevel();
    }

    private void OnEnable()
    {
        DayTimeManager.OnShiftEnded += HandleDayEnd;
    }

    private void OnDisable()
    {
        DayTimeManager.OnShiftEnded -= HandleDayEnd;
    }

    public void LoadLevel()
    {
        dispatchHubLevel = PlayerPrefs.GetInt(PREF_DISPATCH_LEVEL, 1);
        todayPassiveEarned = 0;
    }

    public void SaveLevel()
    {
        PlayerPrefs.SetInt(PREF_DISPATCH_LEVEL, dispatchHubLevel);
        PlayerPrefs.Save();
    }

    public bool IsHubUnlocked()
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
                    if (p != null && (p.propertyType == PropertyType.LogisticsHub || p.propertyId.Contains("logistics") || p.propertyId.Contains("dispatch")))
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

    public int GetDailyPassiveRevenue()
    {
        return GetDailyPassiveRevenueForLevel(dispatchHubLevel);
    }

    public int GetDailyPassiveRevenueForLevel(int level)
    {
        if (!IsHubUnlocked()) return 0;

        switch (level)
        {
            case 1: return level1DailyRevenue;
            case 2: return level2DailyRevenue;
            case 3: return level3DailyRevenue;
            default: return level * level1DailyRevenue;
        }
    }

    public int GetCourierCount()
    {
        return GetCourierCountForLevel(dispatchHubLevel);
    }

    public int GetCourierCountForLevel(int level)
    {
        if (!IsHubUnlocked()) return 0;

        switch (level)
        {
            case 1: return level1Couriers;
            case 2: return level2Couriers;
            case 3: return level3Couriers;
            default: return level * level1Couriers;
        }
    }

    public int GetUpgradeCost()
    {
        return GetUpgradeCostForLevel(dispatchHubLevel + 1);
    }

    public int GetUpgradeCostForLevel(int targetLevel)
    {
        switch (targetLevel)
        {
            case 2: return level2UpgradeCost;
            case 3: return level3UpgradeCost;
            default: return 0;
        }
    }

    public string GetUpgradePromptText()
    {
        if (!IsHubUnlocked()) return string.Empty;

        if (dispatchHubLevel >= 3)
        {
            return $"<color=#32FF64>📦 Dağıtım Şubesi: MAKSİMUM SEVİYE ({GetCourierCount()} Kurye - +${GetDailyPassiveRevenue():N0}/Gün)</color>";
        }

        int nextLevel = dispatchHubLevel + 1;
        int nextCost = GetUpgradeCostForLevel(nextLevel);
        int nextRevenue = GetDailyPassiveRevenueForLevel(nextLevel);
        int nextCouriers = GetCourierCountForLevel(nextLevel);
        int balance = PlayerEconomyManager.Instance != null ? PlayerEconomyManager.Instance.CurrentLiveBalance : 0;

        if (balance < nextCost)
        {
            return $"<color=#FFAA33>📦 Seviye {nextLevel} ({nextCouriers} Kurye, +${nextRevenue:N0}/Gün) - ${nextCost:N0} (Bakiye: ${balance:N0})</color>";
        }

        return $"<color=#32FF64>[E] Şubeyi Yükselt: Seviye {nextLevel} ({nextCouriers} Kurye - +${nextRevenue:N0}/Gün) (${nextCost:N0})</color>";
    }

    public bool TryUpgradeHub()
    {
        if (!IsHubUnlocked()) return false;
        if (dispatchHubLevel >= 3) return false;

        int cost = GetUpgradeCost();
        if (PlayerEconomyManager.Instance != null && PlayerEconomyManager.Instance.SpendMoney(cost))
        {
            dispatchHubLevel++;
            SaveLevel();
            OnDispatchHubUpgraded?.Invoke(dispatchHubLevel);

            if (InteractionPromptHUD.Instance != null)
            {
                InteractionPromptHUD.Instance.ShowPrompt($"<color=#32FF64>📦 Pasif Kargo Şubesi Seviye {dispatchHubLevel}'e Yükseltildi! ({GetCourierCount()} Kurye - +${GetDailyPassiveRevenue():N0}/Gün)</color>");
            }
            return true;
        }

        return false;
    }

    public int CalculateDailyPassiveRevenue()
    {
        if (!IsHubUnlocked())
        {
            todayPassiveEarned = 0;
            return 0;
        }

        int revenue = GetDailyPassiveRevenue();
        todayPassiveEarned = revenue;
        return revenue;
    }

    private void HandleDayEnd()
    {
        CalculateDailyPassiveRevenue();
        if (todayPassiveEarned > 0)
        {
            OnPassiveIncomeDeposited?.Invoke(todayPassiveEarned);
            Debug.Log($"<color=#32FF64>[PASSIVE INCOME] Bölge Dağıtım Şubesinden +${todayPassiveEarned:N0} kargo geliri gün sonu özetiyle kasaya eklendi!</color>");
        }
    }

    [ContextMenu("Reset Dispatch Level")]
    public void DevResetLevel()
    {
        dispatchHubLevel = 1;
        todayPassiveEarned = 0;
        PlayerPrefs.DeleteKey(PREF_DISPATCH_LEVEL);
        PlayerPrefs.Save();
    }
}
