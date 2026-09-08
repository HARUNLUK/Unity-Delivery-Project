using System;
using UnityEngine;

public class PlayerProgressionManager : MonoBehaviour
{
    public static PlayerProgressionManager Instance { get; private set; }

    [Header("--- PLAYER LEVEL & XP ---")]
    [SerializeField] private int playerLevel = 1;
    [SerializeField] private int currentXP = 0;

    [Header("--- WAREHOUSE & SHOP TIER ---")]
    [SerializeField] private int warehouseLevel = 1;

    public static event Action<int> OnLevelUp;
    public static event Action<int, int> OnXPGained; // (gainedAmount, totalXP)
    public static event Action<int> OnWarehouseLevelUp;

    private const string PREFS_PLAYER_LEVEL = "Delivery_PlayerLevel";
    private const string PREFS_CURRENT_XP = "Delivery_CurrentXP";
    private const string PREFS_WAREHOUSE_LEVEL = "Delivery_WarehouseLevel";

    public int PlayerLevel => playerLevel;
    public int CurrentXP => currentXP;
    public int WarehouseLevel => BranchManager.Instance != null ? BranchManager.Instance.CurrentBranchLevel : warehouseLevel;

    public int XPForNextLevel => GetXPRequiredForLevel(playerLevel + 1);
    public int XPForCurrentLevel => GetXPRequiredForLevel(playerLevel);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadProgression();
    }

    public static int GetXPRequiredForLevel(int level)
    {
        if (level <= 1) return 0;
        // Exponential smooth progression: Level 2 = 300, Level 3 = 800, Level 4 = 1600, Level 5 = 2700...
        return Mathf.RoundToInt(150f * Mathf.Pow(level - 1, 1.6f));
    }

    public int GetDailyPackageLimit()
    {
        if (BranchManager.Instance != null)
        {
            return BranchManager.Instance.GetDailyPackageLimit();
        }

        switch (warehouseLevel)
        {
            case 1: return 4;
            case 2: return 7;
            case 3: return 12;
            case 4: return 18;
            default: return 4 + (warehouseLevel * 4);
        }
    }

    public int GetDailyWarehouseRent()
    {
        if (BranchManager.Instance != null)
        {
            return BranchManager.Instance.GetDailyRent();
        }

        switch (warehouseLevel)
        {
            case 1: return 50;
            case 2: return 120;
            case 3: return 280;
            case 4: return 550;
            default: return warehouseLevel * 150;
        }
    }

    public void AddXP(int amount)
    {
        if (amount <= 0) return;

        currentXP += amount;
        OnXPGained?.Invoke(amount, currentXP);

        // Check for level ups
        while (currentXP >= GetXPRequiredForLevel(playerLevel + 1))
        {
            playerLevel++;
            Debug.Log($"<color=#32FF64>[LEVEL UP] You reached Player Level {playerLevel}!</color>");
            OnLevelUp?.Invoke(playerLevel);
        }

        SaveProgression();
    }

    public bool UpgradeWarehouse()
    {
        if (BranchManager.Instance != null)
        {
            return BranchManager.Instance.TryUpgradeBranch();
        }

        int upgradeCost = GetWarehouseUpgradeCost(warehouseLevel + 1);
        if (PlayerEconomyManager.Instance != null && PlayerEconomyManager.Instance.TotalSavedBalance >= upgradeCost)
        {
            PlayerEconomyManager.Instance.DeductCash(upgradeCost);
            warehouseLevel++;
            Debug.Log($"<color=#32FFFF>[WAREHOUSE UPGRADED] Reached Level {warehouseLevel}!</color>");
            OnWarehouseLevelUp?.Invoke(warehouseLevel);
            SaveProgression();
            return true;
        }
        return false;
    }

    public static int GetWarehouseUpgradeCost(int targetLevel)
    {
        switch (targetLevel)
        {
            case 2: return 600;
            case 3: return 1800;
            case 4: return 4500;
            default: return targetLevel * 2000;
        }
    }

    public float GetLevelProgress()
    {
        int currentLevelBase = XPForCurrentLevel;
        int nextLevelBase = XPForNextLevel;
        int range = nextLevelBase - currentLevelBase;
        if (range <= 0) return 1f;

        return Mathf.Clamp01((float)(currentXP - currentLevelBase) / range);
    }

    public void SaveProgression()
    {
        PlayerPrefs.SetInt(PREFS_PLAYER_LEVEL, playerLevel);
        PlayerPrefs.SetInt(PREFS_CURRENT_XP, currentXP);
        PlayerPrefs.SetInt(PREFS_WAREHOUSE_LEVEL, warehouseLevel);
        PlayerPrefs.Save();
    }

    public void LoadProgression()
    {
        playerLevel = PlayerPrefs.GetInt(PREFS_PLAYER_LEVEL, 1);
        currentXP = PlayerPrefs.GetInt(PREFS_CURRENT_XP, 0);
        warehouseLevel = PlayerPrefs.GetInt(PREFS_WAREHOUSE_LEVEL, 1);
    }

    [ContextMenu("Reset Progression (Dev)")]
    public void ResetProgression()
    {
        playerLevel = 1;
        currentXP = 0;
        warehouseLevel = 1;
        SaveProgression();
        Debug.Log("[PlayerProgressionManager] Player progression reset to Level 1.");
    }
}
