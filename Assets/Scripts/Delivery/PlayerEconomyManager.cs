using System;
using UnityEngine;

public class PlayerEconomyManager : MonoBehaviour
{
    public static PlayerEconomyManager Instance { get; private set; }

    private const string BALANCE_KEY = "CARGO_PLAYER_TOTAL_BALANCE";

    [Header("--- KASA BİLGİLERİ ---")]
    [Tooltip("Önceki günlerden biriken toplam para")]
    [SerializeField] private int totalSavedBalance = 0;

    [Tooltip("Bugün kazanılan toplam ödül")]
    [SerializeField] private int todayEarned = 0;

    [Tooltip("Bugün kesilen toplam ceza")]
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

    public void AddCash(int amount) => AddEarnings(amount);
    public void DeductCash(int amount) => AddPenalty(amount);

    /// <summary>
    /// Gün bittiğinde günlük kazancı kalıcı olarak toplam kasaya aktarır ve kaydeder.
    /// </summary>
    public void FinalizeAndSaveDay()
    {
        totalSavedBalance += TodayNetProfit;
        PlayerPrefs.SetInt(BALANCE_KEY, totalSavedBalance);
        PlayerPrefs.Save();
        Debug.Log($"[PlayerEconomyManager] Gün sonu kaydedildi! Yeni Toplam Kasa: {totalSavedBalance} TL");
    }

    /// <summary>
    /// Test için bütçeyi sıfırlar.
    /// </summary>
    public void ResetEntireEconomy()
    {
        PlayerPrefs.DeleteKey(BALANCE_KEY);
        totalSavedBalance = 0;
        todayEarned = 0;
        todayPenalties = 0;
        OnEconomyUpdated?.Invoke(0, 0);
    }
}
