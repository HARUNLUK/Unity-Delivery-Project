using UnityEngine;
using TMPro;

public class MainHUDController : MonoBehaviour
{
    public static MainHUDController Instance { get; private set; }

    [Header("--- HUD ROOT & TEXT REFERENCES ---")]
    public GameObject hudRoot;
    public TextMeshProUGUI clockText;
    public TextMeshProUGUI remainingCargoText;
    public TextMeshProUGUI balanceEarningsText;

    private void Awake()
    {
        Instance = this;
        EnsureHudRoot();
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
        EnsureHudRoot();
        RefreshAll();

        if (GameMenuManager.Instance != null && GameMenuManager.Instance.CurrentState == GameFlowState.MainMenu)
        {
            SetHUDVisible(false);
        }
    }

    public void EnsureHudRoot()
    {
        if (hudRoot != null) return;

        Transform dh = transform.Find("DeliveryHUD");
        if (dh == null && transform.parent != null) dh = transform.parent.Find("DeliveryHUD");
        if (dh == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>() ?? Object.FindAnyObjectByType<Canvas>();
            if (canvas != null) dh = canvas.transform.Find("DeliveryHUD");
        }

        if (dh != null)
        {
            hudRoot = dh.gameObject;
        }
        else if (clockText != null)
        {
            Transform curr = clockText.transform;
            while (curr != null && curr.parent != null && curr.parent != transform && curr.parent.GetComponent<Canvas>() == null)
            {
                if (curr.parent.name.Equals("DeliveryHUD", System.StringComparison.OrdinalIgnoreCase))
                {
                    hudRoot = curr.parent.gameObject;
                    break;
                }
                curr = curr.parent;
            }
            if (hudRoot == null && curr != null && curr != transform)
            {
                hudRoot = curr.gameObject;
            }
        }
    }

    private void Update()
    {
        if (clockText != null && DayTimeManager.Instance != null)
        {
            string dayLabel = LocalizationManager.GetFormat("hud_day", DayTimeManager.Instance.CurrentDay);
            clockText.text = $"<size=75%>{dayLabel}</size>  <mspace=0.6em>{DayTimeManager.Instance.GetFormattedTime()}</mspace>";
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

    public void SetHUDVisible(bool visible)
    {
        EnsureHudRoot();

        if (hudRoot != null)
        {
            hudRoot.SetActive(visible);
        }

        if (clockText != null && clockText.transform.parent != null && clockText.transform.parent != transform)
        {
            clockText.transform.parent.gameObject.SetActive(visible);
        }

        if (clockText != null) clockText.gameObject.SetActive(visible);
        if (remainingCargoText != null) remainingCargoText.gameObject.SetActive(visible);
        if (balanceEarningsText != null) balanceEarningsText.gameObject.SetActive(visible);
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
