using System;
using UnityEngine;

public class PassiveDispatchManager : MonoBehaviour
{
    public static PassiveDispatchManager Instance { get; private set; }

    [Header("--- PROPERTY LINK ---")]
    public PurchasableProperty propertyComponent;

    [Header("--- DISPATCH HUB TIERS ---")]
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
        if (propertyComponent != null) return propertyComponent.IsUnlocked;
        return false;
    }

    public int GetDailyPassiveRevenue()
    {
        if (!IsHubUnlocked()) return 0;

        switch (dispatchHubLevel)
        {
            case 1: return 850;   // 2 kurye
            case 2: return 2200;  // 5 kurye
            case 3: return 4800;  // 10 kurye
            default: return dispatchHubLevel * 1600;
        }
    }

    public int GetCourierCount()
    {
        if (!IsHubUnlocked()) return 0;

        switch (dispatchHubLevel)
        {
            case 1: return 2;
            case 2: return 5;
            case 3: return 10;
            default: return dispatchHubLevel * 3;
        }
    }

    public int GetUpgradeCost()
    {
        switch (dispatchHubLevel)
        {
            case 1: return 6000;
            case 2: return 14000;
            default: return 0;
        }
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
