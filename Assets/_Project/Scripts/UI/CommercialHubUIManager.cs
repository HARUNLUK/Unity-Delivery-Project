using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class CommercialHubUIManager : MonoBehaviour
{
    private static CommercialHubUIManager instance;
    public static CommercialHubUIManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = UnityEngine.Object.FindAnyObjectByType<CommercialHubUIManager>(FindObjectsInactive.Include);
                if (instance == null)
                {
                    Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
                    if (canvas == null)
                    {
                        GameObject canvasObj = new GameObject("HUD_Canvas");
                        canvas = canvasObj.AddComponent<Canvas>();
                        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                        scaler.referenceResolution = new Vector2(1920, 1080);
                        scaler.matchWidthOrHeight = 0.5f;
                        canvasObj.AddComponent<GraphicRaycaster>();
                    }
                    instance = canvas.gameObject.AddComponent<CommercialHubUIManager>();
                    instance.EnsureUI();
                }
            }
            return instance;
        }
        private set => instance = value;
    }

    [Header("--- PANEL ROOTS ---")]
    public GameObject garagePanelRoot;
    public GameObject insurancePanelRoot;
    public GameObject dispatchPanelRoot;
    public GameObject propertyModalRoot;

    // --- GARAGE UI REFS ---
    private TextMeshProUGUI garageTitleText;
    private TextMeshProUGUI garageVehicleStatusText;
    private TextMeshProUGUI garageTuningInfoText;
    private TextMeshProUGUI garagePaintSectionTitle;
    private TextMeshProUGUI garagePaintDescText;
    private TextMeshProUGUI garageCurrentColorText;
    private Image garageCondBarFill;
    private Button garageRepairBtn;
    private Button garageTuneBtn;
    private Button garageDriveBtn;
    private Button garageCloseBtn;

    // --- INSURANCE UI REFS ---
    private TextMeshProUGUI insuranceStatusText;
    private TextMeshProUGUI insuranceBalanceText;
    private Button insuranceTier2UpgradeBtn;
    private Button insuranceTier3UpgradeBtn;
    private Button insuranceCloseBtn;

    // --- DISPATCH UI REFS ---
    private TextMeshProUGUI dispatchStatusText;
    private TextMeshProUGUI dispatchRevenueInfoText;
    private TextMeshProUGUI dispatchBalanceText;
    private Button dispatchTier2UpgradeBtn;
    private Button dispatchTier3UpgradeBtn;
    private Button dispatchCloseBtn;

    // --- PROPERTY MODAL REFS ---
    private TextMeshProUGUI propertyModalTitleText;
    private TextMeshProUGUI propertyModalDescText;
    private TextMeshProUGUI propertyModalCostText;
    private TextMeshProUGUI propertyModalReqText;
    private Button propertyModalBuyBtn;
    private Button propertyModalCancelBtn;

    private DrivableVehicle activeGarageVehicle;
    private PurchasableProperty activePropertyToBuy;

    public bool IsAnyPanelOpen =>
        (garagePanelRoot != null && garagePanelRoot.activeSelf) ||
        (insurancePanelRoot != null && insurancePanelRoot.activeSelf) ||
        (dispatchPanelRoot != null && dispatchPanelRoot.activeSelf) ||
        (propertyModalRoot != null && propertyModalRoot.activeSelf);

    private void Awake()
    {
        Instance = this;
        EnsureUI();
        CloseAllPanels();
    }

    private void Start()
    {
        CloseAllPanels();
    }

    private void Update()
    {
        if (IsAnyPanelOpen)
        {
            if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            bool closePressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.tabKey.wasPressedThisFrame))
            {
                closePressed = true;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            try { if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab)) closePressed = true; } catch { }
#endif

            if (closePressed)
            {
                CloseAllPanels();
            }
        }
    }

    private void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
    }

    private void HandleLanguageChanged(string newLang)
    {
        if (garagePanelRoot != null && garagePanelRoot.activeSelf) RefreshGarageUI();
        if (insurancePanelRoot != null && insurancePanelRoot.activeSelf) RefreshInsuranceUI();
        if (dispatchPanelRoot != null && dispatchPanelRoot.activeSelf) RefreshDispatchUI();
        if (propertyModalRoot != null && propertyModalRoot.activeSelf && activePropertyToBuy != null)
        {
            OpenPropertyPurchaseModal(activePropertyToBuy);
        }
    }

    // ==========================================
    // 1. GARAGE WORKSHOP PANEL
    // ==========================================
    public void OpenGarageWorkshopPanel(DrivableVehicle v)
    {
        if (v == null) return;
        if (VehicleServiceGarage.Instance != null)
        {
            if (!VehicleServiceGarage.Instance.IsGarageUnlocked())
            {
                if (InteractionPromptHUD.Instance != null)
                    InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.Get("prompt_garage_not_unlocked", "<color=#FF3333>Auto Service Garage is locked or not yet purchased!</color>"), 2.0f);
                return;
            }

            if (!VehicleServiceGarage.Instance.IsVehicleInServiceBay(v))
            {
                if (InteractionPromptHUD.Instance != null)
                    InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.Get("prompt_garage_car_not_in_bay", "<color=#FFAA33>Vehicle must be parked inside the service garage bay!</color>"), 2.0f);
                return;
            }
        }

        EnsureUI();
        CloseAllPanels();

        if (InteractionPromptHUD.Instance != null)
        {
            InteractionPromptHUD.Instance.HidePrompt();
        }

        activeGarageVehicle = v;
        if (garagePanelRoot != null) garagePanelRoot.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayTabletOpen();
        }

        RefreshGarageUI();
    }

    public void RefreshGarageUI()
    {
        if (activeGarageVehicle == null) return;

        if (garageTitleText != null)
        {
            garageTitleText.text = LocalizationManager.Get("garage_title", "AUTO SERVICE & WORKSHOP GARAGE");
        }

        if (garageVehicleStatusText != null)
        {
            int hp = Mathf.RoundToInt(activeGarageVehicle.ConditionPercentage * 100f);
            garageVehicleStatusText.text = LocalizationManager.GetFormat("vehicle_health_status", hp);
        }

        if (garageTuningInfoText != null && VehicleServiceGarage.Instance != null)
        {
            int stage = VehicleServiceGarage.Instance.GetVehicleTuningStage(activeGarageVehicle.EffectiveVehicleId);
            string stageDesc = stage == 0 ? "Stock" : (stage == 1 ? "+15% Torque" : (stage == 2 ? "+30% Torque" : "+45% Torque Max"));
            garageTuningInfoText.text = LocalizationManager.GetFormat("garage_tuning_info", stage, stageDesc);
        }

        if (garageRepairBtn != null && VehicleServiceGarage.Instance != null)
        {
            var txt = garageRepairBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                int cost = VehicleServiceGarage.Instance.repairCost;
                txt.text = LocalizationManager.GetFormat("garage_btn_repair", cost);
            }
        }

        if (garageTuneBtn != null && VehicleServiceGarage.Instance != null)
        {
            var txt = garageTuneBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                int currentStage = VehicleServiceGarage.Instance.GetVehicleTuningStage(activeGarageVehicle.EffectiveVehicleId);
                if (currentStage >= 3)
                {
                    txt.text = LocalizationManager.Get("garage_btn_tune_max", "⭐ MAXIMUM ENGINE PERFORMANCE (STAGE 3)");
                    garageTuneBtn.interactable = false;
                }
                else
                {
                    int nextStage = currentStage + 1;
                    int cost = VehicleServiceGarage.Instance.GetTuningCostForStage(nextStage);
                    txt.text = LocalizationManager.GetFormat("garage_btn_tune", nextStage, cost);
                    garageTuneBtn.interactable = true;
                }
            }
        }

        if (garageDriveBtn != null)
        {
            var txt = garageDriveBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = LocalizationManager.Get("garage_btn_drive", "🚗 DRIVE VEHICLE");
        }

        if (garageCloseBtn != null)
        {
            var txt = garageCloseBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = LocalizationManager.Get("btn_close", "CLOSE (ESC)");
        }

        if (garagePaintSectionTitle != null)
        {
            garagePaintSectionTitle.text = LocalizationManager.Get("garage_paint_title", "CUSTOM BODY PAINT & WRAP");
        }

        if (garagePaintDescText != null)
        {
            garagePaintDescText.text = LocalizationManager.Get("garage_paint_desc", "Paint and finish your delivery vehicle.");
        }

        if (garageCondBarFill != null)
        {
            float pct = Mathf.Clamp01(activeGarageVehicle.ConditionPercentage);
            if (garageCondBarFill.type == Image.Type.Filled)
            {
                garageCondBarFill.fillAmount = pct;
            }
            else
            {
                RectTransform rt = garageCondBarFill.rectTransform;
                if (rt != null)
                {
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = new Vector2(pct, 1f);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                }
            }
        }
    }

    private void OnGarageRepairClicked()
    {
        if (activeGarageVehicle == null || VehicleServiceGarage.Instance == null) return;
        if (VehicleServiceGarage.Instance.TryRepairVehicle(activeGarageVehicle))
        {
            RefreshGarageUI();
        }
        else
        {
            FlashButtonError(garageRepairBtn, new Color(0.12f, 0.55f, 0.35f));
        }
    }

    private void OnGarageTuneClicked()
    {
        if (activeGarageVehicle == null || VehicleServiceGarage.Instance == null) return;
        if (VehicleServiceGarage.Instance.TryTuneVehicle(activeGarageVehicle))
        {
            RefreshGarageUI();
        }
        else
        {
            FlashButtonError(garageTuneBtn, new Color(0.85f, 0.55f, 0.12f));
        }
    }

    private void OnGaragePaintColorClicked(Color color)
    {
        if (activeGarageVehicle == null)
        {
            if (VehicleServiceGarage.Instance != null)
            {
                activeGarageVehicle = VehicleServiceGarage.Instance.FindActiveVehicleInBay();
            }
        }

        if (activeGarageVehicle == null) return;

        bool painted = false;
        if (VehicleServiceGarage.Instance != null)
        {
            painted = VehicleServiceGarage.Instance.TryRepaintVehicle(activeGarageVehicle, color);
        }
        else
        {
            activeGarageVehicle.ApplyPaintColor(color, true);
            painted = true;
        }

        if (painted)
        {
            RefreshGarageUI();
        }
    }

    private void OnGarageDriveClicked()
    {
        DrivableVehicle target = activeGarageVehicle;
        CloseAllPanels();
        if (target != null && FPSPlayerController.Instance != null)
        {
            target.EnterVehicle(FPSPlayerController.Instance);
        }
    }

    // ==========================================
    // 2. INSURANCE AGENCY PANEL
    // ==========================================
    public void OpenInsurancePanel()
    {
        EnsureUI();
        CloseAllPanels();

        if (InteractionPromptHUD.Instance != null)
        {
            InteractionPromptHUD.Instance.HidePrompt();
        }

        if (insurancePanelRoot != null) insurancePanelRoot.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayTabletOpen();
        }

        RefreshInsuranceUI();
    }

    public void RefreshInsuranceUI()
    {
        if (InsuranceAgencyManager.Instance == null) return;

        int tier = InsuranceAgencyManager.Instance.InsuranceTier;
        int balance = PlayerEconomyManager.Instance != null ? PlayerEconomyManager.Instance.CurrentLiveBalance : 0;

        TextMeshProUGUI titleTmp = insurancePanelRoot != null ? FindTMPRecursive(insurancePanelRoot.transform, "Title", "Header") : null;
        if (titleTmp != null)
        {
            titleTmp.text = LocalizationManager.Get("insurance_title", "CARGO INSURANCE AND SECURITY AGENCY");
        }

        if (insuranceStatusText != null)
        {
            insuranceStatusText.text = LocalizationManager.GetFormat("insurance_current_policy", InsuranceAgencyManager.Instance.GetTierName());
        }

        if (insuranceBalanceText != null)
        {
            insuranceBalanceText.text = LocalizationManager.GetFormat("insurance_wallet", balance);
        }

        if (insuranceCloseBtn != null)
        {
            var txt = insuranceCloseBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = LocalizationManager.Get("btn_close", "CLOSE (ESC)");
        }

        InsuranceAgencyManager ins = InsuranceAgencyManager.Instance;
        if (insuranceTier2UpgradeBtn != null)
        {
            TextMeshProUGUI txt = insuranceTier2UpgradeBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (tier >= 2)
            {
                if (txt != null) txt.text = LocalizationManager.Get("insurance_btn_tier2_active", "✅ Silver Insurance Active");
                insuranceTier2UpgradeBtn.interactable = false;
            }
            else
            {
                if (txt != null) txt.text = LocalizationManager.GetFormat("insurance_btn_tier2_buy", ins.tier2UpgradeCost);
                insuranceTier2UpgradeBtn.interactable = true;
            }
        }

        if (insuranceTier3UpgradeBtn != null)
        {
            TextMeshProUGUI txt = insuranceTier3UpgradeBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (tier >= 3)
            {
                if (txt != null) txt.text = LocalizationManager.Get("insurance_btn_tier3_active", "⭐ Gold Full Insurance Active");
                insuranceTier3UpgradeBtn.interactable = false;
            }
            else
            {
                if (txt != null) txt.text = LocalizationManager.GetFormat("insurance_btn_tier3_buy", ins.tier3UpgradeCost);
                insuranceTier3UpgradeBtn.interactable = (tier >= 2);
            }
        }
    }

    private void OnInsuranceUpgradeTierClicked()
    {
        if (InsuranceAgencyManager.Instance == null) return;
        if (InsuranceAgencyManager.Instance.TryUpgradeTier())
        {
            RefreshInsuranceUI();
        }
        else
        {
            int tier = InsuranceAgencyManager.Instance.InsuranceTier;
            Button targetBtn = tier == 1 ? insuranceTier2UpgradeBtn : insuranceTier3UpgradeBtn;
            Color normalCol = tier == 1 ? new Color(0.2f, 0.5f, 0.7f) : new Color(0.85f, 0.65f, 0.1f);
            FlashButtonError(targetBtn, normalCol);
        }
    }

    // ==========================================
    // 3. PASSIVE DISPATCH HUB PANEL
    // ==========================================
    public void OpenDispatchHubPanel()
    {
        EnsureUI();
        CloseAllPanels();

        if (InteractionPromptHUD.Instance != null)
        {
            InteractionPromptHUD.Instance.HidePrompt();
        }

        if (dispatchPanelRoot != null) dispatchPanelRoot.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayTabletOpen();
        }

        RefreshDispatchUI();
    }

    public void RefreshDispatchUI()
    {
        if (PassiveDispatchManager.Instance == null) return;

        PassiveDispatchManager hub = PassiveDispatchManager.Instance;
        int level = hub.DispatchHubLevel;
        int couriers = hub.GetCourierCount();
        int dailyRev = hub.GetDailyPassiveRevenue();
        int balance = PlayerEconomyManager.Instance != null ? PlayerEconomyManager.Instance.CurrentLiveBalance : 0;

        TextMeshProUGUI titleTmp = dispatchPanelRoot != null ? FindTMPRecursive(dispatchPanelRoot.transform, "Title", "Header") : null;
        if (titleTmp != null)
        {
            titleTmp.text = LocalizationManager.Get("dispatch_title", "REGIONAL DISPATCH HUB (PASSIVE FLEET)");
        }

        if (dispatchStatusText != null)
        {
            dispatchStatusText.text = LocalizationManager.GetFormat("dispatch_status", level, couriers);
        }

        if (dispatchRevenueInfoText != null)
        {
            dispatchRevenueInfoText.text = LocalizationManager.GetFormat("dispatch_revenue_info", dailyRev);
        }

        if (dispatchBalanceText != null)
        {
            dispatchBalanceText.text = LocalizationManager.GetFormat("insurance_wallet", balance);
        }

        if (dispatchCloseBtn != null)
        {
            var txt = dispatchCloseBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = LocalizationManager.Get("btn_close", "CLOSE (ESC)");
        }

        if (dispatchTier2UpgradeBtn != null)
        {
            TextMeshProUGUI txt = dispatchTier2UpgradeBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (level >= 2)
            {
                if (txt != null) txt.text = LocalizationManager.GetFormat("dispatch_btn_tier2_active", hub.level2Couriers);
                dispatchTier2UpgradeBtn.interactable = false;
            }
            else
            {
                if (txt != null) txt.text = LocalizationManager.GetFormat("dispatch_btn_tier2_buy", hub.level2Couriers, hub.level2DailyRevenue, hub.level2UpgradeCost);
                dispatchTier2UpgradeBtn.interactable = true;
            }
        }

        if (dispatchTier3UpgradeBtn != null)
        {
            TextMeshProUGUI txt = dispatchTier3UpgradeBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (level >= 3)
            {
                if (txt != null) txt.text = LocalizationManager.GetFormat("dispatch_btn_tier3_active", hub.level3Couriers);
                dispatchTier3UpgradeBtn.interactable = false;
            }
            else
            {
                if (txt != null) txt.text = LocalizationManager.GetFormat("dispatch_btn_tier3_buy", hub.level3Couriers, hub.level3DailyRevenue, hub.level3UpgradeCost);
                dispatchTier3UpgradeBtn.interactable = (level >= 2);
            }
        }
    }

    private void OnDispatchUpgradeClicked()
    {
        if (PassiveDispatchManager.Instance == null) return;
        if (PassiveDispatchManager.Instance.TryUpgradeHub())
        {
            RefreshDispatchUI();
        }
        else
        {
            int lvl = PassiveDispatchManager.Instance.DispatchHubLevel;
            Button targetBtn = lvl == 1 ? dispatchTier2UpgradeBtn : dispatchTier3UpgradeBtn;
            Color normalCol = lvl == 1 ? new Color(0.2f, 0.65f, 0.4f) : new Color(0.85f, 0.65f, 0.1f);
            FlashButtonError(targetBtn, normalCol);
        }
    }

    // ==========================================
    // 4. COMMERCIAL PROPERTY PURCHASE MODAL
    // ==========================================
    public void OpenPropertyPurchaseModal(PurchasableProperty prop)
    {
        if (prop == null || prop.IsUnlocked || prop.disablePurchase) return;
        EnsureUI();
        CloseAllPanels();

        if (InteractionPromptHUD.Instance != null)
        {
            InteractionPromptHUD.Instance.HidePrompt();
        }

        activePropertyToBuy = prop;
        if (propertyModalRoot != null) propertyModalRoot.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayTabletOpen();
        }

        int branchLevel = BranchManager.Instance != null ? BranchManager.Instance.CurrentBranchLevel : (PlayerProgressionManager.Instance != null ? PlayerProgressionManager.Instance.WarehouseLevel : 1);
        int balance = PlayerEconomyManager.Instance != null ? PlayerEconomyManager.Instance.CurrentLiveBalance : 0;

        if (propertyModalTitleText != null)
        {
            propertyModalTitleText.text = prop.GetLocalizedDisplayName().ToUpper();
        }

        if (propertyModalDescText != null)
        {
            propertyModalDescText.text = prop.GetLocalizedDescription();
        }

        if (propertyModalReqText != null)
        {
            propertyModalReqText.text = LocalizationManager.GetFormat("property_req_level", prop.requiredPlayerLevel);
        }

        if (propertyModalCostText != null)
        {
            propertyModalCostText.text = LocalizationManager.GetFormat("property_price", prop.purchaseCost);
        }

        if (propertyModalBuyBtn != null)
        {
            propertyModalBuyBtn.interactable = true;
            var buyTxt = propertyModalBuyBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (buyTxt != null) buyTxt.text = LocalizationManager.Get("property_btn_buy", "PURCHASE PROPERTY");
        }

        if (propertyModalCancelBtn != null)
        {
            var cancelTxt = propertyModalCancelBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (cancelTxt != null) cancelTxt.text = LocalizationManager.Get("btn_cancel", "CANCEL");
        }
    }

    private void OnPropertyConfirmPurchase()
    {
        if (activePropertyToBuy == null || activePropertyToBuy.disablePurchase) return;
        if (activePropertyToBuy.TryPurchase())
        {
            CloseAllPanels();
        }
        else
        {
            FlashButtonError(propertyModalBuyBtn, new Color(0.12f, 0.65f, 0.35f));
        }
    }

    public void CloseAllPanels()
    {
        bool wasOpen = IsAnyPanelOpen;

        if (InteractionPromptHUD.Instance != null)
        {
            InteractionPromptHUD.Instance.SuppressPrompts(0.35f);
        }

        if (garagePanelRoot != null) garagePanelRoot.SetActive(false);
        if (insurancePanelRoot != null) insurancePanelRoot.SetActive(false);
        if (dispatchPanelRoot != null) dispatchPanelRoot.SetActive(false);
        if (propertyModalRoot != null) propertyModalRoot.SetActive(false);

        activeGarageVehicle = null;
        activePropertyToBuy = null;

        if (wasOpen && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayTabletClose();
        }

        FPSPlayerController.LockCursor(true);
    }

    // ==========================================
    // PROCEDURAL UI GENERATOR & SETUP
    // ==========================================
    public void EnsureUI(bool forceRecreate = false)
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();

        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("HUD_Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        transform.SetParent(canvas.transform, false);

        if (forceRecreate)
        {
            // Do not destroy custom-designed panels in the scene (PropertyPurchaseModal, GarageWorkshopPanel, etc.)
        }

        // 1. Build Garage Panel
        if (garagePanelRoot == null)
        {
            Transform existingG = FindTransformRecursive(canvas.transform, "GarageWorkshopPanel");
            if (existingG != null)
            {
                garagePanelRoot = existingG.gameObject;
            }
            else
            {
                garagePanelRoot = CreateGaragePanel(canvas.transform);
            }
        }
        BindGaragePanel(garagePanelRoot);

        // 2. Build Insurance Panel
        if (insurancePanelRoot == null)
        {
            Transform existingI = FindTransformRecursive(canvas.transform, "InsuranceAgencyPanel");
            if (existingI != null)
            {
                insurancePanelRoot = existingI.gameObject;
            }
            else
            {
                insurancePanelRoot = CreateInsurancePanel(canvas.transform);
            }
        }
        BindInsurancePanel(insurancePanelRoot);

        // 3. Build Dispatch Panel
        if (dispatchPanelRoot == null)
        {
            Transform existingD = FindTransformRecursive(canvas.transform, "PassiveDispatchPanel");
            if (existingD != null)
            {
                dispatchPanelRoot = existingD.gameObject;
            }
            else
            {
                dispatchPanelRoot = CreateDispatchPanel(canvas.transform);
            }
        }
        BindDispatchPanel(dispatchPanelRoot);

        // 4. Build Property Purchase Modal
        if (propertyModalRoot == null)
        {
            Transform existingP = FindTransformRecursive(canvas.transform, "PropertyPurchaseModal");
            if (existingP != null)
            {
                propertyModalRoot = existingP.gameObject;
            }
            else
            {
                propertyModalRoot = CreatePropertyModal(canvas.transform);
            }
        }
        BindPropertyModal(propertyModalRoot);
    }

    private void BindGaragePanel(GameObject root)
    {
        if (root == null) return;
        garageTitleText = FindTMPRecursive(root.transform, "Title");
        garageVehicleStatusText = FindTMPRecursive(root.transform, "Status");
        garageTuningInfoText = FindTMPRecursive(root.transform, "TuningInfo");

        garageRepairBtn = FindButtonRecursive(root.transform, "RepairBtn");
        if (garageRepairBtn != null)
        {
            garageRepairBtn.onClick.RemoveAllListeners();
            garageRepairBtn.onClick.AddListener(OnGarageRepairClicked);
        }

        garageTuneBtn = FindButtonRecursive(root.transform, "TuneBtn");
        if (garageTuneBtn != null)
        {
            garageTuneBtn.onClick.RemoveAllListeners();
            garageTuneBtn.onClick.AddListener(OnGarageTuneClicked);
        }

        garageDriveBtn = FindButtonRecursive(root.transform, "DriveBtn");
        if (garageDriveBtn != null)
        {
            garageDriveBtn.onClick.RemoveAllListeners();
            garageDriveBtn.onClick.AddListener(OnGarageDriveClicked);
        }

        garageCloseBtn = FindButtonRecursive(root.transform, "CloseBtn");
        if (garageCloseBtn != null)
        {
            garageCloseBtn.onClick.RemoveAllListeners();
            garageCloseBtn.onClick.AddListener(CloseAllPanels);
        }

        garagePaintSectionTitle = FindTMPRecursive(root.transform, "PaintTitle");
        garagePaintDescText = FindTMPRecursive(root.transform, "PaintDesc");
        garageCurrentColorText = FindTMPRecursive(root.transform, "CurrentColor");

        Transform tCondFill = FindTransformRecursive(root.transform, "CondBar_Fill");
        if (tCondFill != null) garageCondBarFill = tCondFill.GetComponent<Image>();

        (string name, string hex, bool darkText)[] palette = new (string, string, bool)[]
        {
            ("🔴 Kırmızı", "#C5221F", false),
            ("🔵 Mavi", "#1A73E8", false),
            ("🖤 Siyah", "#1E1E24", false),
            ("⚪ Beyaz", "#F8F9FA", true),
            ("🟡 Sarı", "#FBBC04", true),
            ("🟢 Yeşil", "#1E8E3E", false),
            ("🟠 Turuncu", "#E8710A", false),
            ("🟣 Mor", "#9334E8", false),
            ("🔘 Gri", "#5F6368", false),
            ("🩵 Turkuaz", "#00BCD4", false)
        };

        for (int i = 0; i < palette.Length; i++)
        {
            Transform tP = FindTransformRecursive(root.transform, $"PaintBtn_{i}");
            if (tP != null)
            {
                Button pBtn = tP.GetComponent<Button>();
                if (pBtn != null)
                {
                    var item = palette[i];
                    ColorUtility.TryParseHtmlString(item.hex, out Color colVal);
                    Color capturedColor = colVal;
                    pBtn.onClick.RemoveAllListeners();
                    pBtn.onClick.AddListener(() => OnGaragePaintColorClicked(capturedColor));
                }
            }
        }
    }

    private void BindInsurancePanel(GameObject root)
    {
        if (root == null) return;
        insuranceStatusText = FindTMPRecursive(root.transform, "Status");
        insuranceBalanceText = FindTMPRecursive(root.transform, "Balance");

        insuranceTier2UpgradeBtn = FindButtonRecursive(root.transform, "Tier2Btn");
        if (insuranceTier2UpgradeBtn != null)
        {
            insuranceTier2UpgradeBtn.onClick.RemoveAllListeners();
            insuranceTier2UpgradeBtn.onClick.AddListener(OnInsuranceUpgradeTierClicked);
        }

        insuranceTier3UpgradeBtn = FindButtonRecursive(root.transform, "Tier3Btn");
        if (insuranceTier3UpgradeBtn != null)
        {
            insuranceTier3UpgradeBtn.onClick.RemoveAllListeners();
            insuranceTier3UpgradeBtn.onClick.AddListener(OnInsuranceUpgradeTierClicked);
        }

        insuranceCloseBtn = FindButtonRecursive(root.transform, "CloseBtn");
        if (insuranceCloseBtn != null)
        {
            insuranceCloseBtn.onClick.RemoveAllListeners();
            insuranceCloseBtn.onClick.AddListener(CloseAllPanels);
        }
    }

    private void BindDispatchPanel(GameObject root)
    {
        if (root == null) return;
        dispatchStatusText = FindTMPRecursive(root.transform, "Status");
        dispatchRevenueInfoText = FindTMPRecursive(root.transform, "Revenue");
        dispatchBalanceText = FindTMPRecursive(root.transform, "Balance");

        dispatchTier2UpgradeBtn = FindButtonRecursive(root.transform, "Tier2Btn");
        if (dispatchTier2UpgradeBtn != null)
        {
            dispatchTier2UpgradeBtn.onClick.RemoveAllListeners();
            dispatchTier2UpgradeBtn.onClick.AddListener(OnDispatchUpgradeClicked);
        }

        dispatchTier3UpgradeBtn = FindButtonRecursive(root.transform, "Tier3Btn");
        if (dispatchTier3UpgradeBtn != null)
        {
            dispatchTier3UpgradeBtn.onClick.RemoveAllListeners();
            dispatchTier3UpgradeBtn.onClick.AddListener(OnDispatchUpgradeClicked);
        }

        dispatchCloseBtn = FindButtonRecursive(root.transform, "CloseBtn");
        if (dispatchCloseBtn != null)
        {
            dispatchCloseBtn.onClick.RemoveAllListeners();
            dispatchCloseBtn.onClick.AddListener(CloseAllPanels);
        }
    }

    private void BindPropertyModal(GameObject root)
    {
        if (root == null) return;
        propertyModalTitleText = FindTMPRecursive(root.transform, "Title", "Header", "ModalTitle");
        propertyModalDescText = FindTMPRecursive(root.transform, "Desc", "Description", "Info");
        propertyModalReqText = FindTMPRecursive(root.transform, "Req", "Requirement", "LevelReq", "RequiredLevel");
        propertyModalCostText = FindTMPRecursive(root.transform, "Cost", "Price", "Fiyat", "Fee");

        propertyModalBuyBtn = FindButtonRecursive(root.transform, "BuyBtn", "BuyButton", "ConfirmBtn", "PurchaseBtn");
        if (propertyModalBuyBtn != null)
        {
            propertyModalBuyBtn.onClick.RemoveAllListeners();
            propertyModalBuyBtn.onClick.AddListener(OnPropertyConfirmPurchase);
        }

        propertyModalCancelBtn = FindButtonRecursive(root.transform, "CancelBtn", "CancelButton", "CloseBtn", "CloseButton");
        if (propertyModalCancelBtn != null)
        {
            propertyModalCancelBtn.onClick.RemoveAllListeners();
            propertyModalCancelBtn.onClick.AddListener(CloseAllPanels);
        }
    }

    private GameObject CreateGaragePanel(Transform parent)
    {
        GameObject root = CreateDarkPanel(parent, "GarageWorkshopPanel", new Vector2(1040, 640));
        return root;
    }

    private GameObject CreateInsurancePanel(Transform parent)
    {
        GameObject root = CreateDarkPanel(parent, "InsuranceAgencyPanel", new Vector2(850, 580));

        CreateTMPText(root, "Title", "🛡️ KARGO SİGORTA & GÜVENLİK ACENTESİ", 28, FontStyles.Bold, new Color(1f, 0.82f, 0.2f), TextAlignmentOptions.Center);

        insuranceStatusText = CreateTMPText(root, "Status", "Mevcut Poliçeniz: Temel Kasko (%30 Hasar İndirimi)", 22, FontStyles.Bold, new Color(0.3f, 1f, 0.4f), TextAlignmentOptions.Center);
        SetRectAnchors(insuranceStatusText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -85), new Vector2(800, 35));

        insuranceBalanceText = CreateTMPText(root, "Balance", "Cüzdan Bakiyeniz: $0", 20, FontStyles.Normal, Color.white, TextAlignmentOptions.Center);
        SetRectAnchors(insuranceBalanceText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -125), new Vector2(800, 30));

        // Tier 2 Button
        insuranceTier2UpgradeBtn = CreateButton(root, "Tier2Btn", "🛡️ Gümüş Kasko Satın Al ($3.000)\n<size=16>%60 Hasar İndirimi + %50 Yanlış Adres Koruması</size>", new Vector2(0, -200), new Vector2(750, 75), new Color(0.2f, 0.5f, 0.7f));
        insuranceTier2UpgradeBtn.onClick.AddListener(OnInsuranceUpgradeTierClicked);

        // Tier 3 Button
        insuranceTier3UpgradeBtn = CreateButton(root, "Tier3Btn", "⭐ Altın Tam Kasko Satın Al ($7.500)\n<size=16>%100 Hasar Koruması ($0 Ceza) + %75 Yanlış Adres Koruması</size>", new Vector2(0, -300), new Vector2(750, 75), new Color(0.85f, 0.65f, 0.1f));
        insuranceTier3UpgradeBtn.onClick.AddListener(OnInsuranceUpgradeTierClicked);

        // Close
        insuranceCloseBtn = CreateButton(root, "CloseBtn", "❌ Kapat (ESC)", new Vector2(0, -420), new Vector2(750, 56), new Color(0.35f, 0.38f, 0.45f));
        insuranceCloseBtn.onClick.AddListener(CloseAllPanels);

        return root;
    }

    private GameObject CreateDispatchPanel(Transform parent)
    {
        GameObject root = CreateDarkPanel(parent, "PassiveDispatchPanel", new Vector2(850, 580));

        CreateTMPText(root, "Title", "📦 BÖLGE DAĞITIM ŞUBESİ & PASİF GELİR MERKEZİ", 28, FontStyles.Bold, new Color(0.3f, 0.9f, 0.5f), TextAlignmentOptions.Center);

        dispatchStatusText = CreateTMPText(root, "Status", "Şube Seviyesi: Seviye 1  |  Kurye Sayısı: 2 Kurye", 22, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        SetRectAnchors(dispatchStatusText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -85), new Vector2(800, 35));

        dispatchRevenueInfoText = CreateTMPText(root, "Revenue", "Günlük Pasif Gelir: +$850 / Gün", 20, FontStyles.Normal, new Color(0.3f, 1f, 0.4f), TextAlignmentOptions.Center);
        SetRectAnchors(dispatchRevenueInfoText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -125), new Vector2(800, 30));

        dispatchBalanceText = CreateTMPText(root, "Balance", "Cüzdan Bakiyeniz: $0", 20, FontStyles.Normal, Color.white, TextAlignmentOptions.Center);
        SetRectAnchors(dispatchBalanceText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -165), new Vector2(800, 30));

        // Tier 2
        dispatchTier2UpgradeBtn = CreateButton(root, "Tier2Btn", "📦 Seviye 2'ye Yükselt (5 Kurye - +$2.200/Gün) [$6.000]", new Vector2(0, -240), new Vector2(750, 65), new Color(0.2f, 0.6f, 0.35f));
        dispatchTier2UpgradeBtn.onClick.AddListener(OnDispatchUpgradeClicked);

        // Tier 3
        dispatchTier3UpgradeBtn = CreateButton(root, "Tier3Btn", "⭐ Seviye 3'e Yükselt (10 Kurye - +$4.800/Gün) [$14.000]", new Vector2(0, -325), new Vector2(750, 65), new Color(0.7f, 0.45f, 0.15f));
        dispatchTier3UpgradeBtn.onClick.AddListener(OnDispatchUpgradeClicked);

        // Close
        dispatchCloseBtn = CreateButton(root, "CloseBtn", "❌ Kapat (ESC)", new Vector2(0, -420), new Vector2(750, 56), new Color(0.35f, 0.38f, 0.45f));
        dispatchCloseBtn.onClick.AddListener(CloseAllPanels);

        return root;
    }

    private GameObject CreatePropertyModal(Transform parent)
    {
        GameObject root = CreateDarkPanel(parent, "PropertyPurchaseModal", new Vector2(750, 480));
        return root;
    }

    private GameObject CreateDarkPanel(Transform parent, string name, Vector2 size)
    {
        GameObject pObj = new GameObject(name);
        pObj.transform.SetParent(parent, false);

        RectTransform rt = pObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;

        pObj.AddComponent<CanvasRenderer>();
        Image img = pObj.AddComponent<Image>();
        img.color = new Color(0.06f, 0.08f, 0.12f, 0.96f);
        img.raycastTarget = true;

        pObj.SetActive(false);
        return pObj;
    }

    private TextMeshProUGUI CreateTMPText(GameObject parent, string name, string text, float size, FontStyles style, Color color, TextAlignmentOptions align)
    {
        GameObject tObj = new GameObject(name);
        tObj.transform.SetParent(parent.transform, false);
        tObj.AddComponent<CanvasRenderer>();
        TextMeshProUGUI tmp = tObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        return tmp;
    }

    private Button CreateButton(GameObject parent, string name, string label, Vector2 pos, Vector2 size, Color bgColor)
    {
        GameObject bObj = new GameObject(name);
        bObj.transform.SetParent(parent.transform, false);

        RectTransform rt = bObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        bObj.AddComponent<CanvasRenderer>();
        Image img = bObj.AddComponent<Image>();
        img.color = bgColor;
        img.raycastTarget = true;

        Button btn = bObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(Mathf.Min(1f, bgColor.r + 0.15f), Mathf.Min(1f, bgColor.g + 0.15f), Mathf.Min(1f, bgColor.b + 0.15f), 1f);
        cb.pressedColor = new Color(bgColor.r * 0.7f, bgColor.g * 0.7f, bgColor.b * 0.7f, 1f);
        cb.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.6f);
        btn.colors = cb;

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(bObj.transform, false);
        RectTransform tRt = txtObj.AddComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.sizeDelta = Vector2.zero;

        txtObj.AddComponent<CanvasRenderer>();
        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 20;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        return btn;
    }

    private void SetRectAnchors(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    public void FlashButtonError(Button btn, Color normalColor)
    {
        if (btn == null) return;
        StartCoroutine(FlashButtonRoutine(btn, normalColor));
    }

    private System.Collections.IEnumerator FlashButtonRoutine(Button btn, Color normalColor)
    {
        Image img = btn.GetComponent<Image>();
        if (img == null) yield break;

        Color errorColor = new Color(0.85f, 0.2f, 0.2f, 1f);
        img.color = errorColor;
        yield return new WaitForSecondsRealtime(0.45f);
        if (img != null)
        {
            img.color = normalColor;
        }
    }

    private TextMeshProUGUI FindTMPRecursive(Transform root, params string[] searchNames)
    {
        if (root == null || searchNames == null) return null;
        TextMeshProUGUI[] tmps = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var name in searchNames)
        {
            foreach (var t in tmps)
            {
                if (t != null && t.gameObject.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return t;
            }
        }
        foreach (var name in searchNames)
        {
            foreach (var t in tmps)
            {
                if (t != null && t.gameObject.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                    return t;
            }
        }
        return null;
    }

    private Button FindButtonRecursive(Transform root, params string[] searchNames)
    {
        if (root == null || searchNames == null) return null;
        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        foreach (var name in searchNames)
        {
            foreach (var b in buttons)
            {
                if (b != null && b.gameObject.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return b;
            }
        }
        foreach (var name in searchNames)
        {
            foreach (var b in buttons)
            {
                if (b != null && b.gameObject.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                    return b;
            }
        }
        return null;
    }

    private Transform FindTransformRecursive(Transform root, string targetName)
    {
        if (root == null) return null;
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        foreach (var t in transforms)
        {
            if (t.gameObject.name.Equals(targetName, StringComparison.OrdinalIgnoreCase))
                return t;
        }
        return null;
    }

#if UNITY_EDITOR
    [MenuItem("Tools/Delivery Game/Create Commercial Hub UI", false, 44)]
    public static void CreateCommercialHubUITool()
    {
        Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("HUD_Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        CommercialHubUIManager ui = canvas.GetComponentInChildren<CommercialHubUIManager>(true);
        if (ui == null)
        {
            ui = canvas.gameObject.AddComponent<CommercialHubUIManager>();
        }
        ui.EnsureUI(true);
        EditorUtility.SetDirty(canvas.gameObject);
        EditorUtility.SetDirty(ui.gameObject);
        Debug.Log("<color=#32FF64>[CommercialHubUIManager] Shop UI Panels (Garage, Insurance, Dispatch, Property Modal) successfully built!</color>");
    }
#endif
}
