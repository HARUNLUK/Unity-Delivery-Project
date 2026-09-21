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
    public TextMeshProUGUI headerTitleText;
    public TextMeshProUGUI totalDeliveredText;
    public TextMeshProUGUI correctDeliveriesText;
    public TextMeshProUGUI wrongDeliveriesText;
    public TextMeshProUGUI netEarningsText;
    public TextMeshProUGUI progressionInfoText;
    public Transform historyListContent;
    public GameObject historyItemTemplate;
    public Button restartDayButton;

    [Header("--- EMERGENCY / HOSPITAL STATUS ---")]
    public string emergencyHospitalReason = "";

    private bool isDayFinalized = false;
    public bool IsSummaryOpen => (summaryPanelRoot != null && summaryPanelRoot.activeSelf) || isDayFinalized;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            if (summaryPanelRoot == null && Instance.summaryPanelRoot != null)
            {
                Destroy(this);
                return;
            }
            if (Instance.summaryPanelRoot == null && summaryPanelRoot != null)
            {
                Destroy(Instance);
                Instance = this;
            }
        }
        else
        {
            Instance = this;
        }

        EnsureSummaryReferences();

        if (summaryPanelRoot != null) summaryPanelRoot.SetActive(false);
        if (historyItemTemplate != null) historyItemTemplate.SetActive(false);
    }

    private void Start()
    {
        isDayFinalized = false;
        EnsureSummaryReferences();
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
        if (IsSummaryOpen)
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

        EnsureSummaryReferences();

        if (summaryPanelRoot != null)
        {
            summaryPanelRoot.SetActive(true);
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayTabletOpen();
            }
        }

        if (CargoTabletUI.Instance != null)
        {
            CargoTabletUI.Instance.CloseTablet();
        }

        FPSPlayerController.LockCursor(false);

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

        // 2. Passive Logistics Hub Income & Daily Warehouse / Branch Rent Expense
        int passiveIncome = 0;
        if (PassiveDispatchManager.Instance != null)
        {
            passiveIncome = PassiveDispatchManager.Instance.CalculateDailyPassiveRevenue();
            totalReward += passiveIncome;
        }

        int dailyRent = PlayerProgressionManager.Instance != null ? PlayerProgressionManager.Instance.GetDailyWarehouseRent() : 50;
        totalPenalty += dailyRent;

        int netProfit = totalReward - totalPenalty;

        // 3. Update and persist economy
        if (PlayerEconomyManager.Instance != null)
        {
            PlayerEconomyManager.Instance.AddEarnings(totalReward);
            PlayerEconomyManager.Instance.AddPenalty(totalPenalty);
            PlayerEconomyManager.Instance.FinalizeAndSaveDay();
        }

        int totalVault = PlayerEconomyManager.Instance != null ? PlayerEconomyManager.Instance.TotalSavedBalance : netProfit;
        int dayNum = DayTimeManager.Instance != null ? DayTimeManager.Instance.CurrentDay : PlayerPrefs.GetInt("Delivery_CurrentDay", 1);

        // 4. Populate UI Text Elements
        if (headerTitleText != null)
        {
            headerTitleText.text = LocalizationManager.GetFormat("summary_title", dayNum);
        }

        if (totalDeliveredText != null)
        {
            if (!string.IsNullOrEmpty(emergencyHospitalReason))
            {
                totalDeliveredText.text = LocalizationManager.GetFormat("summary_emergency_hospital", emergencyHospitalReason, dayNum, totalCount);
            }
            else
            {
                totalDeliveredText.text = LocalizationManager.GetFormat("summary_total_packages", dayNum, totalCount);
            }
        }
        
        string correctText = LocalizationManager.GetFormat("summary_correct_deliveries", correctCount, totalReward - passiveIncome);
        if (passiveIncome > 0)
        {
            correctText += LocalizationManager.GetFormat("summary_passive_income", passiveIncome);
        }
        if (correctDeliveriesText != null) correctDeliveriesText.text = correctText;
        
        string wrongBreakdown = LocalizationManager.GetFormat("summary_penalties", totalPenalty - dailyRent);
        if (brokenCount > 0) wrongBreakdown += LocalizationManager.GetFormat("summary_broken_count", brokenCount);
        wrongBreakdown += LocalizationManager.GetFormat("summary_rent_deduct", dailyRent);
        if (wrongDeliveriesText != null) wrongDeliveriesText.text = wrongBreakdown;
        
        if (netEarningsText != null)
        {
            string profitLabel = netProfit >= 0 ? $"+${netProfit:N0}" : $"-${Mathf.Abs(netProfit):N0}";
            netEarningsText.text = LocalizationManager.GetFormat("summary_today_net", profitLabel, totalVault);
            netEarningsText.color = netProfit >= 0 ? new Color(0.2f, 0.95f, 0.3f) : new Color(0.95f, 0.2f, 0.2f);
        }

        if (progressionInfoText != null)
        {
            if (BranchManager.Instance != null)
            {
                BranchTier currentTier = BranchManager.Instance.CurrentTier;
                string tName = currentTier != null ? currentTier.tierName : "Branch";
                progressionInfoText.text = LocalizationManager.GetFormat("summary_branch_level_badge", BranchManager.Instance.CurrentBranchLevel, tName.ToUpper());
            }
            else
            {
                progressionInfoText.text = string.Empty;
            }
        }

        if (restartDayButton != null)
        {
            var btnText = restartDayButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.text = LocalizationManager.GetFormat("summary_btn_restart_day", dayNum + 1);
            }
        }

        // 5. Detaylı Liste Satırlarını Oluştur
        PopulateResultsList(results);
    }

    private void PopulateResultsList(List<CargoDeliveryResult> results)
    {
        if (historyListContent == null || historyItemTemplate == null) EnsureSummaryReferences();
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

            var deliveredText = FindTMPRecursive(rowObj.transform, "delivered", "DeliveredText", "DeliveredTo", "ActualAddress");
            var targetText = FindTMPRecursive(rowObj.transform, "target", "TargetText", "Destination", "TargetAddress");
            var resultText = FindTMPRecursive(rowObj.transform, "result", "ResultText", "Status", "StatusText");

            string deliveredAddr = !string.IsNullOrEmpty(res.actualAddress) ? res.actualAddress : $"( {LocalizationManager.Get("summary_status_undelivered", "UNDELIVERED")} )";
            string targetAddr = !string.IsNullOrEmpty(res.targetAddress) ? res.targetAddress : "(Unknown)";

            if (deliveredText != null)
            {
                deliveredText.text = LocalizationManager.GetFormat("summary_delivered_to", deliveredAddr);
            }

            if (targetText != null)
            {
                targetText.text = LocalizationManager.GetFormat("summary_target", targetAddr);
            }

            if (resultText != null)
            {
                switch (res.status)
                {
                    case CargoDeliveryStatus.Correct:
                        resultText.text = res.isExpressBonus ? $"<color=#33E0FF>{LocalizationManager.Get("summary_status_express", "EXPRESS")}</color>" : $"<color=#32FF64>{LocalizationManager.Get("summary_status_correct", "CORRECT")}</color>";
                        break;
                    case CargoDeliveryStatus.WrongAddress:
                        resultText.text = $"<color=#FF4444>{LocalizationManager.Get("summary_status_wrong", "WRONG ADDRESS")}</color>";
                        break;
                    case CargoDeliveryStatus.Broken:
                        resultText.text = res.cargoType == CargoType.Explosive ? $"<color=#FF2222>{LocalizationManager.Get("summary_status_exploded", "EXPLODED")}</color>" : $"<color=#FF4444>{LocalizationManager.Get("summary_status_broken", "BROKEN")}</color>";
                        break;
                    default:
                        resultText.text = $"<color=#FFAA22>{LocalizationManager.Get("summary_status_undelivered", "UNDELIVERED")}</color>";
                        break;
                }
            }

            // Fallback for single text label template if separate fields not found
            if (deliveredText == null && targetText == null && resultText == null)
            {
                TextMeshProUGUI rowText = rowObj.GetComponentInChildren<TextMeshProUGUI>();
                if (rowText != null)
                {
                    string statusLabel = res.status == CargoDeliveryStatus.Correct ? $"[{LocalizationManager.Get("summary_status_correct", "CORRECT")}]" : $"[{LocalizationManager.Get("summary_status_broken", "BROKEN")}]";
                    rowText.text = $"{LocalizationManager.GetFormat("summary_delivered_to", deliveredAddr)}\n{LocalizationManager.GetFormat("summary_target", targetAddr)}   {statusLabel}";
                }
            }
        }
    }

    public void EnsureSummaryReferences()
    {
        if (summaryPanelRoot == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform t = FindTransformRecursive(canvas.transform, "DaySummaryPanel");
                if (t != null) summaryPanelRoot = t.gameObject;
            }
            if (summaryPanelRoot == null)
            {
                GameObject found = GameObject.Find("DaySummaryPanel");
                if (found != null) summaryPanelRoot = found;
            }
        }

        if (summaryPanelRoot != null)
        {
            if (headerTitleText == null) headerTitleText = FindTMPRecursive(summaryPanelRoot.transform, "HeaderTitleText", "TitleText", "HeaderTitle", "PanelTitle", "Title");
            if (totalDeliveredText == null) totalDeliveredText = FindTMPRecursive(summaryPanelRoot.transform, "TotalDeliveredText", "TotalText", "TotalDelivered");
            if (correctDeliveriesText == null) correctDeliveriesText = FindTMPRecursive(summaryPanelRoot.transform, "CorrectDeliveriesText", "CorrectText", "CorrectDeliveries");
            if (wrongDeliveriesText == null) wrongDeliveriesText = FindTMPRecursive(summaryPanelRoot.transform, "WrongDeliveriesText", "WrongText", "WrongDeliveries");
            if (netEarningsText == null) netEarningsText = FindTMPRecursive(summaryPanelRoot.transform, "NetEarningsText", "EarningsText", "NetEarnings");
            if (progressionInfoText == null) progressionInfoText = FindTMPRecursive(summaryPanelRoot.transform, "ProgressionInfoText", "BranchText", "ProgressionText");

            if (historyListContent == null)
            {
                Transform content = FindTransformRecursive(summaryPanelRoot.transform, "Content");
                if (content != null) historyListContent = content;
            }

            if (historyItemTemplate == null && historyListContent != null)
            {
                Transform tmpl = historyListContent.Find("HistoryRowTemplate");
                if (tmpl == null) tmpl = historyListContent.Find("HistoryItemTemplate");
                if (tmpl != null) historyItemTemplate = tmpl.gameObject;
            }

            if (restartDayButton == null)
            {
                Button btn = FindButtonRecursive(summaryPanelRoot.transform, "RestartDayButton", "RestartButton", "StartNextDayButton", "NextDayButton");
                if (btn != null) restartDayButton = btn;
            }
        }

        if (restartDayButton != null)
        {
            restartDayButton.onClick.RemoveAllListeners();
            restartDayButton.onClick.AddListener(RestartDay);
        }
    }

    private TextMeshProUGUI FindTMPRecursive(Transform root, params string[] searchNames)
    {
        if (root == null) return null;
        var allTexts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var name in searchNames)
        {
            foreach (var t in allTexts)
            {
                if (t != null && t.gameObject.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                    return t;
            }
        }
        foreach (var name in searchNames)
        {
            foreach (var t in allTexts)
            {
                if (t != null && t.gameObject.name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return t;
            }
        }
        return null;
    }

    private Button FindButtonRecursive(Transform root, params string[] searchNames)
    {
        if (root == null) return null;
        var allBtns = root.GetComponentsInChildren<Button>(true);
        foreach (var name in searchNames)
        {
            foreach (var b in allBtns)
            {
                if (b != null && b.gameObject.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                    return b;
            }
        }
        foreach (var name in searchNames)
        {
            foreach (var b in allBtns)
            {
                if (b != null && b.gameObject.name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return b;
            }
        }
        return null;
    }

    private Transform FindTransformRecursive(Transform root, string childName)
    {
        if (root == null) return null;
        if (root.gameObject.name.Equals(childName, System.StringComparison.OrdinalIgnoreCase)) return root;
        var allTransforms = root.GetComponentsInChildren<Transform>(true);
        foreach (var t in allTransforms)
        {
            if (t != null && t.gameObject.name.Equals(childName, System.StringComparison.OrdinalIgnoreCase))
                return t;
        }
        return null;
    }

    public void RestartDay()
    {
        // 1. Advance to the next calendar day
        if (DayTimeManager.Instance != null)
        {
            DayTimeManager.Instance.AdvanceToNextDay();
        }
        else
        {
            int nextDay = PlayerPrefs.GetInt("Delivery_CurrentDay", 1) + 1;
            PlayerPrefs.SetInt("Delivery_CurrentDay", nextDay);
            PlayerPrefs.Save();
        }

        // 2. Instruct GameMenuManager to skip Main Menu on reload and start directly in gameplay
        GameMenuManager.SkipMainMenuOnNextLoad = true;

        // 3. Reload active scene for next day
        Scene currentScene = SceneManager.GetActiveScene();
        if (currentScene.buildIndex >= 0)
        {
            SceneManager.LoadScene(currentScene.buildIndex);
        }
        else
        {
            SceneManager.LoadScene(currentScene.name);
        }
    }
}
