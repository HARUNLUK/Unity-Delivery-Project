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
        totalSavedBalance = PlayerPrefs.GetInt(BALANCE_KEY, 0);
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
        OnEconomyUpdated?.Invoke(CurrentLiveBalance, TodayNetProfit);
    }

    public void AddPenalty(int amount)
    {
        todayPenalties += amount;
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
        try
        {
            if (Input.GetKeyDown(KeyCode.F7))
            {
                ResetEntireEconomy();
                ShowResetFeedback();
            }
        }
        catch { }
    }

    private void ShowResetFeedback()
    {
        Debug.Log("<color=yellow>[PlayerEconomyManager] F7 Pressed: Balance ($0) and Player Level (Level 1) reset!</color>");
        if (InteractionPromptHUD.Instance != null)
        {
            InteractionPromptHUD.Instance.ShowPrompt("<color=#FFAA33>★ Reset to Level 1 & Balance $0! [F7] ★</color>");
        }
    }

    public void AddCash(int amount) => AddEarnings(amount);
    public void DeductCash(int amount) => AddPenalty(amount);

    /// <summary>
    /// Finalizes daily earnings to persistent player vault and saves PlayerPrefs.
    /// </summary>
    public void FinalizeAndSaveDay()
    {
        totalSavedBalance += TodayNetProfit;
        PlayerPrefs.SetInt(BALANCE_KEY, totalSavedBalance);
        PlayerPrefs.Save();
        Debug.Log($"[PlayerEconomyManager] End of day saved! New Total Vault: ${totalSavedBalance}");
    }

    /// <summary>
    /// Resets entire player balance and progression level for testing/debugging.
    /// </summary>
    [ContextMenu("Reset Entire Economy & Level (F7)")]
    public void ResetEntireEconomy()
    {
        PlayerPrefs.DeleteKey(BALANCE_KEY);
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
