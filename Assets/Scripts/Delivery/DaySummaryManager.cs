using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class DaySummaryManager : MonoBehaviour
{
    public static DaySummaryManager Instance { get; private set; }

    [Header("--- PANEL REFERENCES ---")]
    public GameObject summaryPanelRoot;
    public TextMeshProUGUI totalDeliveredText;
    public TextMeshProUGUI correctDeliveriesText;
    public TextMeshProUGUI wrongDeliveriesText;
    public TextMeshProUGUI netEarningsText;
    public TextMeshProUGUI progressionInfoText;
    public Transform historyListContent;
    public GameObject historyItemTemplate;
    public Button restartDayButton;

    private bool isDayFinalized = false;

    private void Awake()
    {
        Instance = this;

        if (summaryPanelRoot == null)
        {
            Transform t = transform.Find("DaySummaryPanel");
            if (t != null) summaryPanelRoot = t.gameObject;
        }

        if (summaryPanelRoot != null) summaryPanelRoot.SetActive(false);
        if (historyItemTemplate != null) historyItemTemplate.SetActive(false);

        if (restartDayButton != null)
        {
            restartDayButton.onClick.RemoveAllListeners();
            restartDayButton.onClick.AddListener(RestartDay);
        }
    }

    private void Start()
    {
        isDayFinalized = false;
        if (summaryPanelRoot != null) summaryPanelRoot.SetActive(false);
    }

    private void OnEnable()
    {
        DayTimeManager.OnShiftEnded += ShowDaySummary;
    }

    private void OnDisable()
    {
        DayTimeManager.OnShiftEnded -= ShowDaySummary;
    }

    private void Update()
    {
        if (summaryPanelRoot != null && summaryPanelRoot.activeSelf)
        {
            if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }

    [ContextMenu("Trigger End Of Day Summary")]
    public void ShowDaySummary()
    {
        if (isDayFinalized) return;
        isDayFinalized = true;

        if (summaryPanelRoot == null)
        {
            Transform t = transform.Find("DaySummaryPanel");
            if (t != null) summaryPanelRoot = t.gameObject;
        }

        if (summaryPanelRoot != null)
        {
            summaryPanelRoot.SetActive(true);
        }

        if (CargoTabletUI.Instance != null)
        {
            CargoTabletUI.Instance.CloseTablet();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 1. Gather all physical cargo packages in the scene
        PhysicalCargoPackage[] scenePackages = Object.FindObjectsByType<PhysicalCargoPackage>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        List<CargoDeliveryResult> results = new List<CargoDeliveryResult>();

        int totalCount = scenePackages.Length;
        int correctCount = 0;
        int wrongCount = 0;
        int brokenCount = 0;
        int undeliveredCount = 0;
        int totalReward = 0;
        int totalPenalty = 0;
        int totalXP = 0;

        foreach (var pkg in scenePackages)
        {
            if (pkg == null) continue;

            CargoDeliveryResult res = pkg.EvaluateEndOfDayResult();
            results.Add(res);

            totalXP += res.xpAwarded;

            if (res.status == CargoDeliveryStatus.Correct)
            {
                correctCount++;
                totalReward += res.moneyChange;
            }
            else if (res.status == CargoDeliveryStatus.Broken)
            {
                brokenCount++;
                totalPenalty += Mathf.Abs(res.moneyChange);
            }
            else if (res.status == CargoDeliveryStatus.WrongAddress)
            {
                wrongCount++;
                totalPenalty += Mathf.Abs(res.moneyChange);
            }
            else
            {
                undeliveredCount++;
                totalPenalty += Mathf.Abs(res.moneyChange);
            }
        }

        // 2. Daily Warehouse / Branch Rent Expense
        int dailyRent = PlayerProgressionManager.Instance != null ? PlayerProgressionManager.Instance.GetDailyWarehouseRent() : 50;
        totalPenalty += dailyRent;

        int netProfit = totalReward - totalPenalty;

        // 3. Update XP and Player Level
        if (PlayerProgressionManager.Instance != null)
        {
            PlayerProgressionManager.Instance.AddXP(totalXP);
        }

        // 4. Update and persist economy
        if (PlayerEconomyManager.Instance != null)
        {
            PlayerEconomyManager.Instance.AddEarnings(totalReward);
            PlayerEconomyManager.Instance.AddPenalty(totalPenalty);
            PlayerEconomyManager.Instance.FinalizeAndSaveDay();
        }

        int totalVault = PlayerEconomyManager.Instance != null ? PlayerEconomyManager.Instance.TotalSavedBalance : netProfit;

        // 5. Populate UI Text Elements
        if (totalDeliveredText != null) totalDeliveredText.text = $"Total Packages Today: {totalCount}";
        if (correctDeliveriesText != null) correctDeliveriesText.text = $"[+] Correct Deliveries: {correctCount} (+${totalReward})";
        
        string wrongBreakdown = $"[-] Penalties: -${totalPenalty - dailyRent}";
        if (brokenCount > 0) wrongBreakdown += $" (Broken: {brokenCount})";
        wrongBreakdown += $" | Warehouse Rent: -${dailyRent}";
        if (wrongDeliveriesText != null) wrongDeliveriesText.text = wrongBreakdown;
        
        if (netEarningsText != null)
        {
            string profitLabel = netProfit >= 0 ? $"+${netProfit}" : $"-${Mathf.Abs(netProfit)}";
            netEarningsText.text = $"TODAY NET: {profitLabel} | TOTAL VAULT: ${totalVault}";
            netEarningsText.color = netProfit >= 0 ? new Color(0.2f, 0.95f, 0.3f) : new Color(0.95f, 0.2f, 0.2f);
        }

        if (progressionInfoText != null && PlayerProgressionManager.Instance != null)
        {
            int pLvl = PlayerProgressionManager.Instance.PlayerLevel;
            int cXp = PlayerProgressionManager.Instance.CurrentXP;
            int nXp = PlayerProgressionManager.Instance.XPForNextLevel;
            progressionInfoText.text = $"★ PLAYER LEVEL {pLvl} | XP: {cXp}/{nXp} (+{totalXP} XP Today)";
        }

        // 6. Detaylı Liste Satırlarını Oluştur
        PopulateResultsList(results);
    }

    private void PopulateResultsList(List<CargoDeliveryResult> results)
    {
        if (historyListContent == null || historyItemTemplate == null) return;

        foreach (Transform child in historyListContent)
        {
            if (child.gameObject != historyItemTemplate)
            {
                Destroy(child.gameObject);
            }
        }

        foreach (var res in results)
        {
            GameObject rowObj = Instantiate(historyItemTemplate, historyListContent);
            rowObj.SetActive(true);

            TextMeshProUGUI rowText = rowObj.GetComponentInChildren<TextMeshProUGUI>();
            Image rowImg = rowObj.GetComponent<Image>();

            if (rowText != null)
            {
                string statusLabel = "";
                string typeBadge = res.cargoType == CargoType.Standard ? "" : 
                    (res.cargoType == CargoType.Express && res.package != null ? $" [EXPRESS {res.package.GetFormattedTargetDeliveryTime()}]" : $" [{res.cargoType.ToString().ToUpper()}]");

                if (res.status == CargoDeliveryStatus.Correct)
                {
                    statusLabel = res.isExpressBonus 
                        ? "<color=#00CCFF>[EXPRESS ON-TIME]</color>" 
                        : "<color=#32FF64>[CORRECT DELIVERY]</color>";
                }
                else if (res.status == CargoDeliveryStatus.Broken)
                {
                    statusLabel = "<color=#FF2222>[BROKEN FRAGILE]</color>";
                }
                else if (res.status == CargoDeliveryStatus.WrongAddress)
                {
                    statusLabel = "<color=#FF4444>[WRONG ADDRESS]</color>";
                }
                else
                {
                    statusLabel = "<color=#FFAA22>[NOT DELIVERED]</color>";
                }

                string moneyLabel = res.moneyChange >= 0 ? $"+${res.moneyChange}" : $"-${Mathf.Abs(res.moneyChange)}";
                string xpLabel = res.xpAwarded > 0 ? $" (+{res.xpAwarded} XP)" : "";

                rowText.text = $"{res.trackingNumber}{typeBadge} | {statusLabel} ({moneyLabel}){xpLabel}\nRecipient: {res.recipientName} | Target: {res.targetAddress} | Landed: {res.actualAddress}";
            }

            if (rowImg != null)
            {
                if (res.status == CargoDeliveryStatus.Correct)
                    rowImg.color = new Color(0.12f, 0.38f, 0.18f, 0.9f);
                else if (res.status == CargoDeliveryStatus.Broken)
                    rowImg.color = new Color(0.55f, 0.10f, 0.10f, 0.9f);
                else if (res.status == CargoDeliveryStatus.WrongAddress)
                    rowImg.color = new Color(0.48f, 0.14f, 0.14f, 0.9f);
                else
                    rowImg.color = new Color(0.38f, 0.28f, 0.12f, 0.9f);
            }
        }
    }

    public void RestartDay()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
