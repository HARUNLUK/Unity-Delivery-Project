using UnityEngine;
using TMPro;

public class MainHUDController : MonoBehaviour
{
    public static MainHUDController Instance { get; private set; }

    [Header("--- HUD METİN REFERANSLARI ---")]
    public TextMeshProUGUI clockText;
    public TextMeshProUGUI remainingCargoText;
    public TextMeshProUGUI balanceEarningsText;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        PlayerEconomyManager.OnEconomyUpdated += HandleEconomyUpdated;
        VanInventory.OnInventoryUpdated += RefreshCargoCount;
    }

    private void OnDisable()
    {
        PlayerEconomyManager.OnEconomyUpdated -= HandleEconomyUpdated;
        VanInventory.OnInventoryUpdated -= RefreshCargoCount;
    }

    private void Start()
    {
        RefreshAll();
    }

    private void Update()
    {
        if (clockText != null && DayTimeManager.Instance != null)
        {
            clockText.text = $"SAAT: {DayTimeManager.Instance.GetFormattedTime()}";
        }
    }

    private void HandleEconomyUpdated(int liveBalance, int todayNet)
    {
        UpdateEarningsDisplay(liveBalance, todayNet);
    }

    private void RefreshCargoCount()
    {
        if (remainingCargoText != null && VanInventory.Instance != null)
        {
            remainingCargoText.text = $"KALAN: {VanInventory.Instance.RemainingCargoCount} / {VanInventory.Instance.dailyPackageCount}";
        }
    }

    private void UpdateEarningsDisplay(int liveBalance, int todayNet)
    {
        if (balanceEarningsText != null)
        {
            string dailySign = todayNet >= 0 ? $"(+{todayNet} TL)" : $"({todayNet} TL)";
            
            if (liveBalance >= 0)
            {
                balanceEarningsText.text = $"BAKIYE: {liveBalance} TL {dailySign}";
                balanceEarningsText.color = new Color(0.2f, 1f, 0.4f); // Yeşil
            }
            else
            {
                balanceEarningsText.text = $"BAKIYE: {liveBalance} TL {dailySign}";
                balanceEarningsText.color = new Color(1f, 0.25f, 0.25f); // Kırmızı
            }
        }
    }

    public void RefreshAll()
    {
        RefreshCargoCount();
        if (PlayerEconomyManager.Instance != null)
        {
            UpdateEarningsDisplay(PlayerEconomyManager.Instance.CurrentLiveBalance, PlayerEconomyManager.Instance.TodayNetProfit);
        }
    }
}
