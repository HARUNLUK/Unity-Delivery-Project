using UnityEngine;
using TMPro;

public class MainHUDController : MonoBehaviour
{
    public static MainHUDController Instance { get; private set; }

    [Header("--- HUD TEXT REFERENCES ---")]
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
            clockText.text = $"<mspace=0.6em>{DayTimeManager.Instance.GetFormattedTime()}</mspace>";
        }

        if (Time.frameCount % 30 == 0)
        {
            RefreshCargoCount();
        }
    }

    private void HandleEconomyUpdated(int liveBalance, int todayNet)
    {
        UpdateEarningsDisplay(liveBalance, todayNet);
    }

    private void RefreshCargoCount()
    {
        if (remainingCargoText == null) return;

        PhysicalCargoPackage[] scenePackages = Object.FindObjectsByType<PhysicalCargoPackage>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (scenePackages != null && scenePackages.Length > 0)
        {
            int remaining = 0;
            foreach (var p in scenePackages)
            {
                if (p != null && p.FindNearbyDeliveryPoint() == null && !p.isBroken)
                {
                    remaining++;
                }
            }
            remainingCargoText.text = $"{remaining} / {scenePackages.Length}";
            return;
        }

        if (VanInventory.Instance != null)
        {
            remainingCargoText.text = $"{VanInventory.Instance.RemainingCargoCount} / {VanInventory.Instance.dailyPackageCount}";
        }
    }

    private void UpdateEarningsDisplay(int liveBalance, int todayNet)
    {
        if (balanceEarningsText != null)
        {
            if (liveBalance >= 0)
            {
                balanceEarningsText.text = $"${liveBalance:N0}";
                balanceEarningsText.color = new Color(0.2f, 1f, 0.4f); // Green
            }
            else
            {
                balanceEarningsText.text = $"-${Mathf.Abs(liveBalance):N0}";
                balanceEarningsText.color = new Color(1f, 0.25f, 0.25f); // Red
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
