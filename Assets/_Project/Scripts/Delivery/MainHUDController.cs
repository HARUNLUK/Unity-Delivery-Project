using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainHUDController : MonoBehaviour
{
    public static MainHUDController Instance { get; private set; }

    [Header("--- HUD ROOT & TEXT REFERENCES ---")]
    public GameObject hudRoot;
    public TextMeshProUGUI clockText;
    public TextMeshProUGUI remainingCargoText;
    public TextMeshProUGUI balanceEarningsText;

    [Header("--- KURYE DEFTERI HUD (optional) ---")]
    [Tooltip("When set, the day is shown here and clockText shows only the time.")]
    public TextMeshProUGUI dayText;
    [Tooltip("Fill of the shift progress bar (09:00 -> 18:00), driven by anchorMax.x.")]
    public Image dayProgressFill;

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

    private int lastCachedDay = -1;
    private string lastCachedTimeString = "";
    private int lastRemainingCargo = -1;
    private int lastTotalCargo = -1;
    private int lastEarningsBalance = int.MinValue;

    private void Update()
    {
        if (clockText != null && DayTimeManager.Instance != null)
        {
            int currentDay = DayTimeManager.Instance.CurrentDay;
            string currentTime = DayTimeManager.Instance.GetFormattedTime();
            if (currentDay != lastCachedDay || !string.Equals(currentTime, lastCachedTimeString, System.StringComparison.Ordinal))
            {
                lastCachedDay = currentDay;
                lastCachedTimeString = currentTime;
                string dayLabel = LocalizationManager.GetFormat("hud_day", currentDay);
                if (dayText != null)
                {
                    dayText.text = dayLabel;
                    clockText.text = currentTime;
                }
                else
                {
                    clockText.text = $"<size=75%>{dayLabel}</size>  <mspace=0.6em>{currentTime}</mspace>";
                }
                UpdateDayProgress();
            }
        }

        if ((Time.frameCount & 31) == 0)
        {
            RefreshCargoCount();
        }
    }

    private void UpdateDayProgress()
    {
        if (dayProgressFill == null || DayTimeManager.Instance == null) return;
        DayTimeManager dtm = DayTimeManager.Instance;
        float start = dtm.startHour * 60f + dtm.startMinute;
        float end = dtm.endHour * 60f + dtm.endMinute;
        float now = dtm.CurrentHour * 60f + dtm.CurrentMinute;
        float pct = end > start ? Mathf.Clamp01((now - start) / (end - start)) : 0f;
        RectTransform rt = dayProgressFill.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = new Vector2(Mathf.Max(0.04f, pct), 1f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void HandleEconomyUpdated(int liveBalance, int todayNet)
    {
        UpdateEarningsDisplay(liveBalance, todayNet);
    }

    private void RefreshCargoCount()
    {
        if (remainingCargoText == null) return;

        var scenePackages = PhysicalCargoPackage.AllPackages;
        int total = scenePackages.Count;

        if (total > 0)
        {
            int remaining = 0;
            for (int i = 0; i < total; i++)
            {
                var p = scenePackages[i];
                if (p != null && p.FindNearbyDeliveryPoint() == null && !p.isBroken)
                {
                    remaining++;
                }
            }

            if (remaining != lastRemainingCargo || total != lastTotalCargo)
            {
                lastRemainingCargo = remaining;
                lastTotalCargo = total;
                remainingCargoText.text = $"{remaining} / {total}";
            }
            return;
        }

        if (VanInventory.Instance != null)
        {
            int rem = VanInventory.Instance.RemainingCargoCount;
            int tot = VanInventory.Instance.dailyPackageCount;
            if (rem != lastRemainingCargo || tot != lastTotalCargo)
            {
                lastRemainingCargo = rem;
                lastTotalCargo = tot;
                remainingCargoText.text = $"{rem} / {tot}";
            }
        }
    }

    private void UpdateEarningsDisplay(int liveBalance, int todayNet)
    {
        if (balanceEarningsText != null)
        {
            if (liveBalance == lastEarningsBalance) return;
            lastEarningsBalance = liveBalance;

            if (liveBalance >= 0)
            {
                balanceEarningsText.text = $"${liveBalance:N0}";
                balanceEarningsText.color = CozyTheme.Ink;
            }
            else
            {
                balanceEarningsText.text = $"-${Mathf.Abs(liveBalance):N0}";
                balanceEarningsText.color = CozyTheme.RedInk;
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
