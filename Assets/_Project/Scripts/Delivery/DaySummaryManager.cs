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

    [Header("--- KURYE DEFTERI RECEIPT ---")]
    [Tooltip("Set by the Kurye Defteri UI builder: the summary is drawn as an itemised receipt.")]
    public bool useKuryeDefteriLayout = false;
    public Transform receiptLinesContent;
    public GameObject receiptLineTemplate;
    public TextMeshProUGUI netProfitText;
    public TextMeshProUGUI vaultText;
    public TextMeshProUGUI xpText;
    public TextMeshProUGUI stampText;
    public TextMeshProUGUI receiptNoteText;
    public TextMeshProUGUI historyCountText;

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

        // 0. Close and hide all other active panels, modals, prompts, and held/driving player states
        CloseOtherActivePanels();

        if (summaryPanelRoot != null)
        {
            summaryPanelRoot.SetActive(true);
            summaryPanelRoot.transform.SetAsLastSibling();
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayTabletOpen();
            }
        }

        FPSPlayerController.LockCursor(false);
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

        // 2. Passive Logistics Hub Income & Daily Warehouse / Branch Rent Expense
        int passiveIncome = 0;
        if (PassiveDispatchManager.Instance != null)
        {
            passiveIncome = PassiveDispatchManager.Instance.CalculateDailyPassiveRevenue();
            totalReward += passiveIncome;
        }

        int dailyRent = 50;
        if (BranchManager.Instance != null)
        {
            dailyRent = BranchManager.Instance.GetDailyRent();
        }
        else if (PlayerProgressionManager.Instance != null)
        {
            dailyRent = PlayerProgressionManager.Instance.GetDailyWarehouseRent();
        }
        else
        {
            int lvl = PlayerPrefs.GetInt("Delivery_BranchLevel", PlayerPrefs.GetInt("Delivery_WarehouseLevel", 1));
            dailyRent = lvl == 2 ? 120 : (lvl >= 3 ? 280 : 50);
        }
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

        if (useKuryeDefteriLayout)
        {
            PopulateReceiptKurye(results, dayNum, passiveIncome, dailyRent, netProfit, totalVault, totalXP);
            PopulateResultsListKurye(results);
            return;
        }

        // 5. Detaylı Liste Satırlarını Oluştur
        PopulateResultsList(results);
    }

    private void CloseOtherActivePanels()
    {
        // 1. Tablet UI
        if (CargoTabletUI.Instance != null && CargoTabletUI.Instance.IsTabletOpen)
        {
            CargoTabletUI.Instance.CloseTablet();
        }

        // 2. Commercial Hub UI (Garage Workshop, Insurance Agency, Dispatch Hub, Property Purchase Modal)
        if (CommercialHubUIManager.Instance != null && CommercialHubUIManager.Instance.IsAnyPanelOpen)
        {
            CommercialHubUIManager.Instance.CloseAllPanels();
        }

        // 3. Delivery Selection Zone Dropdown
        if (DeliverySelectionUI.Instance != null && DeliverySelectionUI.Instance.IsOpen)
        {
            DeliverySelectionUI.Instance.ClosePanel();
        }

        // 4. In-Game Prompts, Gauges, Charge Bars, and Held Cargo Cards
        if (InteractionPromptHUD.Instance != null)
        {
            InteractionPromptHUD.Instance.HidePrompt();
            InteractionPromptHUD.Instance.HideHeldCargoInfo();
            InteractionPromptHUD.Instance.HideThrowCharge();
            InteractionPromptHUD.Instance.HideFuelHUD();
            InteractionPromptHUD.Instance.SuppressPrompts(9999f);
        }

        // 5. Delivery Feedback Notification Popups
        if (DeliveryNotificationHUD.Instance != null && DeliveryNotificationHUD.Instance.notificationRoot != null)
        {
            DeliveryNotificationHUD.Instance.notificationRoot.SetActive(false);
        }

        // 6. Branch Upgrade Transition Overlay
        if (BranchUpgradeTransitionUI.Instance != null)
        {
            BranchUpgradeTransitionUI.Instance.HideImmediate();
        }

        // 7. Menus & Modals (Pause Menu, Settings, New Game Modal)
        if (GameMenuManager.Instance != null)
        {
            if (GameMenuManager.Instance.pauseMenuPanel != null && GameMenuManager.Instance.pauseMenuPanel.activeSelf)
            {
                GameMenuManager.Instance.pauseMenuPanel.SetActive(false);
            }
            if (GameMenuManager.Instance.settingsPanel != null && GameMenuManager.Instance.settingsPanel.activeSelf)
            {
                GameMenuManager.Instance.settingsPanel.SetActive(false);
            }
            if (GameMenuManager.Instance.newGameModalPanel != null && GameMenuManager.Instance.newGameModalPanel.activeSelf)
            {
                GameMenuManager.Instance.newGameModalPanel.SetActive(false);
            }
        }

        // 8. Player State: safely release any held cargo and exit vehicle if driving
        FPSPlayerController player = FPSPlayerController.Instance != null ? FPSPlayerController.Instance : Object.FindAnyObjectByType<FPSPlayerController>();
        if (player != null)
        {
            if (player.grabber != null && player.grabber.IsHoldingObject)
            {
                player.grabber.ReleaseObject(Vector3.zero);
            }
            if (!player.IsOnFoot && player.currentVehicle != null)
            {
                player.currentVehicle.ExitVehicle();
            }
        }

        // 9. Comprehensive Canvas Panel Sweep (deactivate known active dialogs/modals in scene)
        var allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        string[] panelsToDeactivate = new string[]
        {
            "CargoTabletPanel",
            "GarageWorkshopPanel",
            "InsuranceAgencyPanel",
            "PassiveDispatchPanel",
            "PropertyPurchaseModal",
            "DeliverySelectionPanel",
            "PauseMenuPanel",
            "SettingsPanel",
            "HeldCargoSideCard",
            "InteractionPromptBox",
            "ThrowSlideBG",
            "VehicleDashboardPanel",
            "DeliveryTutorial_Modal_Root",
            "VehicleTutorial_Modal_Root"
        };

        foreach (var canvas in allCanvases)
        {
            if (canvas == null) continue;
            foreach (var panelName in panelsToDeactivate)
            {
                Transform t = FindTransformRecursive(canvas.transform, panelName);
                if (t != null && t.gameObject != summaryPanelRoot && t.gameObject.activeSelf)
                {
                    t.gameObject.SetActive(false);
                }
            }
        }
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

            string deliveredAddr = !string.IsNullOrEmpty(res.actualAddress)
                ? res.actualAddress
                : $"( {LocalizationManager.Get("summary_status_undelivered")} )";
            string targetAddr = !string.IsNullOrEmpty(res.targetAddress)
                ? res.targetAddress
                : LocalizationManager.Get("summary_unknown_address");
            string recipient = !string.IsNullOrEmpty(res.recipientName)
                ? res.recipientName
                : LocalizationManager.Get("recipient_default");

            // Format Express Info & Target Delivery Hour
            string expressInfo = "";
            if (res.cargoType == CargoType.Express)
            {
                string expTime = !string.IsNullOrEmpty(res.formattedDeliveryTime) ? res.formattedDeliveryTime : "13:00";
                if (res.status == CargoDeliveryStatus.Correct)
                {
                    expressInfo = res.isExpressBonus
                        ? $" <color=#33E0FF>[ Express: {expTime} \u2713]</color>"
                        : $" <color=#FFAA22>[ Express: {expTime} ({LocalizationManager.Get("summary_status_express_late")})]</color>";
                }
                else
                {
                    expressInfo = $" <color=#33E0FF>[ Express: {expTime}]</color>";
                }
            }

            string targetLabelPrefix = LocalizationManager.Get("summary_recipient_target_label");
            string recipientTargetCombined = $"{targetLabelPrefix} <b>{recipient}</b> - {targetAddr}{expressInfo}";

            if (targetText != null)
            {
                targetText.text = recipientTargetCombined;
            }

            if (deliveredText != null)
            {
                deliveredText.text = LocalizationManager.GetFormat("summary_delivered_to", deliveredAddr);
            }

            string moneyBadge = res.moneyChange >= 0 ? $"+${res.moneyChange}" : $"-${Mathf.Abs(res.moneyChange)}";
            string statusStr = "";
            switch (res.status)
            {
                case CargoDeliveryStatus.Correct:
                    statusStr = res.isExpressBonus
                        ? $"<color=#33E0FF>[{LocalizationManager.Get("summary_status_express")} {moneyBadge}]</color>"
                        : $"<color=#32FF64>[{LocalizationManager.Get("summary_status_correct")} {moneyBadge}]</color>";
                    break;
                case CargoDeliveryStatus.WrongAddress:
                    statusStr = $"<color=#FF4444>[{LocalizationManager.Get("summary_status_wrong")} {moneyBadge}]</color>";
                    break;
                case CargoDeliveryStatus.Broken:
                    statusStr = res.cargoType == CargoType.Explosive
                        ? $"<color=#FF2222>[{LocalizationManager.Get("summary_status_exploded")} {moneyBadge}]</color>"
                        : $"<color=#FF4444>[{LocalizationManager.Get("summary_status_broken")} {moneyBadge}]</color>";
                    break;
                default:
                    statusStr = $"<color=#FFAA22>[{LocalizationManager.Get("summary_status_undelivered")} {moneyBadge}]</color>";
                    break;
            }

            if (resultText != null)
            {
                resultText.text = statusStr;
            }

            // Fallback for single text label template (HistoryRowTemplate with single Text child)
            if (deliveredText == null && targetText == null && resultText == null)
            {
                TextMeshProUGUI rowText = rowObj.GetComponentInChildren<TextMeshProUGUI>();
                if (rowText != null)
                {
                    string delivLabel = LocalizationManager.Get("summary_delivered_to_label");
                    rowText.text = $"{recipientTargetCombined}\n<size=85%><color=#A0C8FF>{delivLabel}</color> {deliveredAddr}   {statusStr}</size>";
                }
            }
        }
    }

    // ==========================================
    // KURYE DEFTERI RECEIPT
    // ==========================================

    private void PopulateReceiptKurye(List<CargoDeliveryResult> results, int dayNum, int passiveIncome, int dailyRent, int netProfit, int totalVault, int totalXP)
    {
        int correct = 0, correctSum = 0, wrong = 0, wrongSum = 0, broken = 0, brokenSum = 0, missed = 0, missedSum = 0;
        foreach (CargoDeliveryResult r in results)
        {
            switch (r.status)
            {
                case CargoDeliveryStatus.Correct: correct++; correctSum += r.moneyChange; break;
                case CargoDeliveryStatus.WrongAddress: wrong++; wrongSum += Mathf.Abs(r.moneyChange); break;
                case CargoDeliveryStatus.Broken: broken++; brokenSum += Mathf.Abs(r.moneyChange); break;
                default: missed++; missedSum += Mathf.Abs(r.moneyChange); break;
            }
        }

        if (receiptLinesContent != null && receiptLineTemplate != null)
        {
            foreach (Transform child in receiptLinesContent)
            {
                if (child.gameObject != receiptLineTemplate) Destroy(child.gameObject);
            }

            AddReceiptLine(string.Format(LocalizationManager.Get("cozy_receipt_correct", "Doğru teslimat ×{0}"), correct), correctSum);
            if (passiveIncome > 0) AddReceiptLine(LocalizationManager.Get("cozy_receipt_passive", "Dağıtım şubesi geliri"), passiveIncome);
            if (broken > 0) AddReceiptLine(string.Format(LocalizationManager.Get("cozy_receipt_broken", "Kırık koli ×{0}"), broken), -brokenSum);
            if (wrong > 0) AddReceiptLine(string.Format(LocalizationManager.Get("cozy_receipt_wrong", "Yanlış adres ×{0}"), wrong), -wrongSum);
            if (missed > 0) AddReceiptLine(string.Format(LocalizationManager.Get("cozy_receipt_missed", "Teslim edilmeyen ×{0}"), missed), -missedSum);
            AddReceiptLine(LocalizationManager.Get("cozy_receipt_rent", "Şube kirası"), -dailyRent);
        }

        if (netProfitText != null)
        {
            netProfitText.text = (netProfit >= 0 ? "+" : "") + CozyText.Money(netProfit);
            netProfitText.color = netProfit >= 0 ? CozyTheme.MintInk : CozyTheme.RedInk;
        }
        if (vaultText != null) vaultText.text = CozyText.Money(totalVault);
        if (xpText != null) xpText.text = $"+{totalXP} XP";
        if (stampText != null) stampText.text = string.Format(LocalizationManager.Get("cozy_receipt_stamp", "GÜN {0}\nKAPANDI"), dayNum);
        if (historyCountText != null) historyCountText.text = string.Format(LocalizationManager.Get("cozy_history_count", "{0} / {1} doğru"), correct, results.Count);

        if (receiptNoteText != null)
        {
            bool hasNote = !string.IsNullOrEmpty(emergencyHospitalReason);
            receiptNoteText.gameObject.SetActive(hasNote);
            if (hasNote) receiptNoteText.text = LocalizationManager.GetFormat("summary_emergency_hospital", emergencyHospitalReason, dayNum, results.Count);
        }
    }

    private void AddReceiptLine(string label, int amount)
    {
        GameObject line = Instantiate(receiptLineTemplate, receiptLinesContent);
        line.SetActive(true);
        Transform l = line.transform.Find("Label");
        if (l != null && l.TryGetComponent(out TextMeshProUGUI lt)) lt.text = label;
        Transform a = line.transform.Find("Amount");
        if (a != null && a.TryGetComponent(out TextMeshProUGUI at))
        {
            at.text = (amount >= 0 ? "+" : "") + CozyText.Money(amount);
            at.color = amount >= 0 ? CozyTheme.MintInk : CozyTheme.RedInk;
        }
    }

    private void PopulateResultsListKurye(List<CargoDeliveryResult> results)
    {
        if (historyListContent == null || historyItemTemplate == null) return;

        foreach (Transform child in historyListContent)
        {
            if (child.gameObject != historyItemTemplate) Destroy(child.gameObject);
        }

        foreach (CargoDeliveryResult res in results)
        {
            GameObject row = Instantiate(historyItemTemplate, historyListContent);
            row.SetActive(true);

            string recipient = !string.IsNullOrEmpty(res.recipientName) ? res.recipientName : LocalizationManager.Get("recipient_default");
            string target = !string.IsNullOrEmpty(res.targetAddress) ? res.targetAddress : LocalizationManager.Get("summary_unknown_address");

            string icon;
            Color badge, badgeInk;
            string detail;
            switch (res.status)
            {
                case CargoDeliveryStatus.Correct:
                    icon = "check"; badge = CozyTheme.Mint; badgeInk = Color.white;
                    detail = target;
                    if (res.cargoType == CargoType.Express)
                    {
                        detail += res.isExpressBonus
                            ? "  ·  " + LocalizationManager.Get("cozy_history_early", "erken teslim")
                            : "  ·  " + LocalizationManager.Get("cozy_history_late", "geç kaldı");
                    }
                    break;
                case CargoDeliveryStatus.WrongAddress:
                    icon = "x"; badge = CozyTheme.Red; badgeInk = Color.white;
                    detail = string.Format(LocalizationManager.Get("cozy_history_dropped", "Bırakılan: {0}  ·  hedef: {1}"), res.actualAddress, target);
                    break;
                case CargoDeliveryStatus.Broken:
                    bool exploded = res.cargoType == CargoType.Explosive;
                    icon = exploded ? "flame" : "fragile"; badge = exploded ? CozyTheme.Red : CozyTheme.OrangeTint; badgeInk = exploded ? Color.white : CozyTheme.OrangeInk;
                    detail = target + "  ·  " + (exploded ? LocalizationManager.Get("summary_status_exploded", "patladı") : LocalizationManager.Get("cozy_history_broken", "kırık"));
                    break;
                default:
                    icon = "clock"; badge = CozyTheme.HoneyTint; badgeInk = CozyTheme.HoneyInk;
                    detail = target + "  ·  " + LocalizationManager.Get("cozy_history_missed", "teslim edilmedi");
                    break;
            }

            Transform b = row.transform.Find("StatusBadge");
            if (b != null)
            {
                if (b.TryGetComponent(out Image bi)) bi.color = badge;
                Transform ic = b.Find("Icon");
                if (ic != null && ic.TryGetComponent(out Image ici))
                {
                    ici.color = badgeInk;
                    if (CozyAssets.Instance != null) ici.sprite = CozyAssets.Instance.Icon(icon);
                }
            }

            Transform rt = row.transform.Find("Texts/Recipient");
            if (rt != null && rt.TryGetComponent(out TextMeshProUGUI rtt)) rtt.text = recipient;
            Transform dt = row.transform.Find("Texts/Detail");
            if (dt != null && dt.TryGetComponent(out TextMeshProUGUI dtt)) dtt.text = detail;
            Transform am = row.transform.Find("Amount");
            if (am != null && am.TryGetComponent(out TextMeshProUGUI amt))
            {
                amt.text = (res.moneyChange >= 0 ? "+" : "") + CozyText.Money(res.moneyChange);
                amt.color = res.moneyChange >= 0 ? CozyTheme.MintInk : CozyTheme.RedInk;
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

        if (summaryPanelRoot != null && !useKuryeDefteriLayout)
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
