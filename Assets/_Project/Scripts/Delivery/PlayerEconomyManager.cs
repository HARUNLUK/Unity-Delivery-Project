using System;
using UnityEngine;

public class PlayerEconomyManager : MonoBehaviour
{
    public static PlayerEconomyManager Instance { get; private set; }

    private const string BALANCE_KEY = "CARGO_PLAYER_TOTAL_BALANCE";

    [Header("--- VAULT / BALANCE DATA ---")]
    [Tooltip("Accumulated total balance from previous shifts")]
    [SerializeField] private int totalSavedBalance = 0;

    [Tooltip("Total reward earned today")]
    [SerializeField] private int todayEarned = 0;

    [Tooltip("Total penalties incurred today")]
    [SerializeField] private int todayPenalties = 0;

    public int TotalSavedBalance => totalSavedBalance;
    public int TodayEarned => todayEarned;
    public int TodayPenalties => todayPenalties;
    public int TodayNetProfit => todayEarned - todayPenalties;
    public int CurrentLiveBalance => totalSavedBalance + TodayNetProfit;

    public static event Action<int, int> OnEconomyUpdated; // (CurrentLiveBalance, TodayNetProfit)

    private void Awake()
    {
        Instance = this;
        LoadSavedBalance();
    }

    private void OnEnable()
    {
        VanInventory.OnCargoDelivered += HandleCargoDelivered;
    }

    private void OnDisable()
    {
        VanInventory.OnCargoDelivered -= HandleCargoDelivered;
    }

    public void LoadSavedBalance()
    {
        if (PlayerPrefs.HasKey(BALANCE_KEY))
        {
            totalSavedBalance = PlayerPrefs.GetInt(BALANCE_KEY, 0);
        }
        else
        {
            totalSavedBalance = PlayerPrefs.GetInt("Delivery_PlayerCash", 0);
        }
        todayEarned = 0;
        todayPenalties = 0;
    }

    private void HandleCargoDelivered(CargoItem item, bool isCorrect)
    {
        if (isCorrect)
        {
            todayEarned += item.deliveryReward;
        }
        else
        {
            todayPenalties += item.wrongDeliveryPenalty;
        }

        OnEconomyUpdated?.Invoke(CurrentLiveBalance, TodayNetProfit);
    }

    public void AddEarnings(int amount)
    {
        todayEarned += amount;
        if (amount > 0 && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMoneyAdd();
        }
        OnEconomyUpdated?.Invoke(CurrentLiveBalance, TodayNetProfit);
    }

    public void AddPenalty(int amount, bool playSound = true)
    {
        todayPenalties += amount;
        if (playSound && amount > 0 && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMoneySubtract();
        }
        OnEconomyUpdated?.Invoke(CurrentLiveBalance, TodayNetProfit);
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.f7Key.wasPressedThisFrame)
        {
            ResetEntireEconomy();
            ShowResetFeedback();
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        try
        {
            if (Input.GetKeyDown(KeyCode.F7))
            {
                ResetEntireEconomy();
                ShowResetFeedback();
            }
        }
        catch { }
#endif
    }

    private void ShowResetFeedback()
    {
        Debug.Log("<color=yellow>[PlayerEconomyManager] F7 Pressed: Balance ($0) and Player Level (Level 1) reset!</color>");
        if (InteractionPromptHUD.Instance != null)
        {
            InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.Get("prompt_dev_reset_balance"));
        }
    }

    public void AddCash(int amount) => AddEarnings(amount);
    public void DeductCash(int amount, bool playSound = true) => AddPenalty(amount, playSound);

    /// <summary>
    /// Overwrites total vault balance directly (e.g. from FPSPlayerController Inspector or debug commands).
    /// </summary>
    public void SetBalance(int newBalance)
    {
        totalSavedBalance = Mathf.Max(0, newBalance);
        todayEarned = 0;
        todayPenalties = 0;
        PlayerPrefs.SetInt(BALANCE_KEY, totalSavedBalance);
        PlayerPrefs.SetInt("Delivery_PlayerCash", totalSavedBalance);
        PlayerPrefs.Save();
        OnEconomyUpdated?.Invoke(CurrentLiveBalance, TodayNetProfit);
    }

    public bool SpendMoney(int amount)
    {
        if (amount <= 0) return true;

        if (CurrentLiveBalance >= amount)
        {
            int newBalance = CurrentLiveBalance - amount;
            totalSavedBalance = Mathf.Max(0, newBalance);
            todayEarned = 0;
            todayPenalties = 0;

            PlayerPrefs.SetInt(BALANCE_KEY, totalSavedBalance);
            PlayerPrefs.SetInt("Delivery_PlayerCash", totalSavedBalance);
            PlayerPrefs.Save();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMoneySubtract();
            }

            OnEconomyUpdated?.Invoke(CurrentLiveBalance, TodayNetProfit);
            return true;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayError();
        }
        return false;
    }

    public bool TrySpendMoney(int amount) => SpendMoney(amount);

    /// <summary>
    /// Finalizes daily earnings to persistent player vault and saves PlayerPrefs.
    /// </summary>
    public void FinalizeAndSaveDay()
    {
        totalSavedBalance = CurrentLiveBalance;
        todayEarned = 0;
        todayPenalties = 0;
        PlayerPrefs.SetInt(BALANCE_KEY, totalSavedBalance);
        PlayerPrefs.SetInt("Delivery_PlayerCash", totalSavedBalance);
        PlayerPrefs.Save();
        Debug.Log($"[PlayerEconomyManager] End of day saved! New Total Vault: ${totalSavedBalance}");

        if (GameOverManager.Instance != null)
        {
            GameOverManager.Instance.CheckGameOverCondition(totalSavedBalance);
        }
    }

    public void SaveLiveBalance()
    {
        totalSavedBalance = CurrentLiveBalance;
        todayEarned = 0;
        todayPenalties = 0;
        PlayerPrefs.SetInt(BALANCE_KEY, totalSavedBalance);
        PlayerPrefs.SetInt("Delivery_PlayerCash", totalSavedBalance);
        PlayerPrefs.Save();
        Debug.Log($"[PlayerEconomyManager] Live balance saved: ${totalSavedBalance}");
    }

    private void OnApplicationQuit()
    {
        SaveLiveBalance();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveLiveBalance();
        }
    }

    /// <summary>
    /// Resets entire player balance and progression level for testing/debugging.
    /// </summary>
    [ContextMenu("Reset Entire Economy & Level (F7)")]
    public void ResetEntireEconomy()
    {
        PlayerPrefs.DeleteKey(BALANCE_KEY);
        PlayerPrefs.DeleteKey("Delivery_PlayerCash");
        totalSavedBalance = 0;
        todayEarned = 0;
        todayPenalties = 0;
        OnEconomyUpdated?.Invoke(0, 0);

        if (PlayerProgressionManager.Instance != null)
        {
            PlayerProgressionManager.Instance.ResetProgression();
        }

        if (CargoTabletUI.Instance != null && CargoTabletUI.Instance.IsTabletOpen)
        {
            CargoTabletUI.Instance.RefreshUI();
        }
    }
}
