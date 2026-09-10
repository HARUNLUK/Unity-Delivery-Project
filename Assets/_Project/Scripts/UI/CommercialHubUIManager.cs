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

            bool escPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                escPressed = true;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            try { if (Input.GetKeyDown(KeyCode.Escape)) escPressed = true; } catch { }
#endif

            if (escPressed)
            {
                CloseAllPanels();
            }
        }
    }

    // ==========================================
    // 1. GARAGE WORKSHOP PANEL
    // ==========================================
    public void OpenGarageWorkshopPanel(DrivableVehicle v)
    {
        if (v == null) return;
        if (VehicleServiceGarage.Instance != null && !VehicleServiceGarage.Instance.IsGarageUnlocked())
        {
            if (InteractionPromptHUD.Instance != null)
                InteractionPromptHUD.Instance.ShowPrompt("<color=#FF3333>Oto Servis Garajı henüz satın alınmadı veya kilitli!</color>", 2.5f);
            return;
        }

        EnsureUI();
        CloseAllPanels();

        activeGarageVehicle = v;
        if (garagePanelRoot != null) garagePanelRoot.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        RefreshGarageUI();
    }

    public void RefreshGarageUI()
    {
        if (activeGarageVehicle == null) return;

        if (garageTitleText != null)
        {
            garageTitleText.text = $"🔧 OTO SERVİS & MODİFİYE ATÖLYESİ - <color=#32FFFF>{activeGarageVehicle.vehicleName}</color>";
        }

        if (garageVehicleStatusText != null)
        {
            float fuelPct = activeGarageVehicle.FuelPercentage * 100f;
            float condPct = activeGarageVehicle.ConditionPercentage * 100f;
            string condColor = condPct < 25f ? "#FF4444" : (condPct < 70f ? "#FFAA33" : "#32FF64");
            garageVehicleStatusText.text = $"<b>Yakıt Durumu:</b> {activeGarageVehicle.currentFuel:F1} / {activeGarageVehicle.maxFuel:F1} L (%{fuelPct:F0})  |  <b>Kondisyon:</b> <color={condColor}>%{condPct:F0}</color>";
        }

        int tuningStage = VehicleServiceGarage.Instance != null ? VehicleServiceGarage.Instance.GetVehicleTuningStage(activeGarageVehicle.vehicleId) : 0;
        int nextTuneCost = VehicleServiceGarage.Instance != null ? VehicleServiceGarage.Instance.GetTuningCostForStage(tuningStage + 1) : 0;
        float torqueMult = VehicleServiceGarage.Instance != null ? VehicleServiceGarage.Instance.GetTorqueMultiplierForStage(tuningStage) : 1f;

        if (garageTuningInfoText != null)
        {
            string bonusStr = tuningStage > 0 ? $"(+{(torqueMult - 1f) * 100:0}% Tork)" : "(Standart Fabrika Çıkışı)";
            garageTuningInfoText.text = $"<b>Motor Performansı:</b> Stage {tuningStage} {bonusStr}";
        }

        if (garageRepairBtn != null)
        {
            int repCost = VehicleServiceGarage.Instance != null ? VehicleServiceGarage.Instance.repairCost : 150;
            TextMeshProUGUI repairTxt = garageRepairBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (repairTxt != null) repairTxt.text = $"🔧 Tamir & Bakım Yap (${repCost:N0})";
        }

        if (garageTuneBtn != null)
        {
            TextMeshProUGUI btnTxt = garageTuneBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (tuningStage >= 3)
            {
                if (btnTxt != null) btnTxt.text = "🚀 Motor: MAKSİMUM SEVİYE";
                garageTuneBtn.interactable = false;
            }
            else
            {
                if (btnTxt != null) btnTxt.text = $"🚀 Stage {tuningStage + 1} Tork Yükselt (${nextTuneCost:N0})";
                garageTuneBtn.interactable = true;
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
    }

    private void OnGarageTuneClicked()
    {
        if (activeGarageVehicle == null || VehicleServiceGarage.Instance == null) return;
        if (VehicleServiceGarage.Instance.TryTuneVehicle(activeGarageVehicle))
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

        if (insurancePanelRoot != null) insurancePanelRoot.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        RefreshInsuranceUI();
    }

    public void RefreshInsuranceUI()
    {
        if (InsuranceAgencyManager.Instance == null) return;

        int tier = InsuranceAgencyManager.Instance.InsuranceTier;
        int balance = PlayerEconomyManager.Instance != null ? PlayerEconomyManager.Instance.CurrentLiveBalance : 0;

        if (insuranceStatusText != null)
        {
            insuranceStatusText.text = $"<b>Mevcut Poliçeniz:</b> <color=#32FF64>{InsuranceAgencyManager.Instance.GetTierName()}</color>";
        }

        if (insuranceBalanceText != null)
        {
            insuranceBalanceText.text = $"<b>Cüzdan Bakiyeniz:</b> <color=#32FF64>${balance:N0}</color>";
        }

        InsuranceAgencyManager ins = InsuranceAgencyManager.Instance;
        if (insuranceTier2UpgradeBtn != null)
        {
            TextMeshProUGUI txt = insuranceTier2UpgradeBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (tier >= 2)
            {
                if (txt != null) txt.text = "✅ Gümüş Kasko Aktif";
                insuranceTier2UpgradeBtn.interactable = false;
            }
            else
            {
                if (txt != null) txt.text = $"🛡️ Gümüş Kasko Satın Al (${ins.tier2UpgradeCost:N0})";
                insuranceTier2UpgradeBtn.interactable = (balance >= ins.tier2UpgradeCost);
            }
        }

        if (insuranceTier3UpgradeBtn != null)
        {
            TextMeshProUGUI txt = insuranceTier3UpgradeBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (tier >= 3)
            {
                if (txt != null) txt.text = "⭐ Altın Tam Kasko Aktif";
                insuranceTier3UpgradeBtn.interactable = false;
            }
            else
            {
                if (txt != null) txt.text = $"🛡️ Altın Tam Kasko Satın Al (${ins.tier3UpgradeCost:N0})";
                insuranceTier3UpgradeBtn.interactable = (tier == 2 && balance >= ins.tier3UpgradeCost);
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
    }

    // ==========================================
    // 3. PASSIVE DISPATCH HUB PANEL
    // ==========================================
    public void OpenDispatchHubPanel()
    {
        EnsureUI();
        CloseAllPanels();

        if (dispatchPanelRoot != null) dispatchPanelRoot.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

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

        if (dispatchStatusText != null)
        {
            dispatchStatusText.text = $"<b>Şube Seviyesi:</b> Seviye {level}  |  <b>Kurye Sayısı:</b> {couriers} Kurye";
        }

        if (dispatchRevenueInfoText != null)
        {
            dispatchRevenueInfoText.text = $"<b>Günlük Pasif Gelir:</b> <color=#32FF64>+${dailyRev:N0} / Gün</color> (Her gün 18:00'de otomatik yatırılır)";
        }

        if (dispatchBalanceText != null)
        {
            dispatchBalanceText.text = $"<b>Cüzdan Bakiyeniz:</b> <color=#32FF64>${balance:N0}</color>";
        }

        if (dispatchTier2UpgradeBtn != null)
        {
            TextMeshProUGUI txt = dispatchTier2UpgradeBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (level >= 2)
            {
                if (txt != null) txt.text = $"✅ Seviye 2 Aktif ({hub.level2Couriers} Kurye)";
                dispatchTier2UpgradeBtn.interactable = false;
            }
            else
            {
                if (txt != null) txt.text = $"📦 Seviye 2'ye Yükselt ({hub.level2Couriers} Kurye - +${hub.level2DailyRevenue:N0}/Gün) [${hub.level2UpgradeCost:N0}]";
                dispatchTier2UpgradeBtn.interactable = (balance >= hub.level2UpgradeCost);
            }
        }

        if (dispatchTier3UpgradeBtn != null)
        {
            TextMeshProUGUI txt = dispatchTier3UpgradeBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (level >= 3)
            {
                if (txt != null) txt.text = $"⭐ Seviye 3 Maksimum Filo ({hub.level3Couriers} Kurye)";
                dispatchTier3UpgradeBtn.interactable = false;
            }
            else
            {
                if (txt != null) txt.text = $"📦 Seviye 3'e Yükselt ({hub.level3Couriers} Kurye - +${hub.level3DailyRevenue:N0}/Gün) [${hub.level3UpgradeCost:N0}]";
                dispatchTier3UpgradeBtn.interactable = (level == 2 && balance >= hub.level3UpgradeCost);
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
    }

    // ==========================================
    // 4. COMMERCIAL PROPERTY PURCHASE MODAL
    // ==========================================
    public void OpenPropertyPurchaseModal(PurchasableProperty prop)
    {
        if (prop == null || prop.IsUnlocked) return;
        EnsureUI();
        CloseAllPanels();

        activePropertyToBuy = prop;
        if (propertyModalRoot != null) propertyModalRoot.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        int branchLevel = BranchManager.Instance != null ? BranchManager.Instance.CurrentBranchLevel : (PlayerProgressionManager.Instance != null ? PlayerProgressionManager.Instance.WarehouseLevel : 1);
        int balance = PlayerEconomyManager.Instance != null ? PlayerEconomyManager.Instance.CurrentLiveBalance : 0;

        if (propertyModalTitleText != null) propertyModalTitleText.text = $"🏢 {prop.displayName.ToUpper()}";
        if (propertyModalDescText != null) propertyModalDescText.text = prop.description;
        if (propertyModalCostText != null) propertyModalCostText.text = $"<b>Fiyat:</b> <color=#32FF64>${prop.purchaseCost:N0}</color>  |  <b>Bakiyeniz:</b> ${balance:N0}";
        if (propertyModalReqText != null)
        {
            string lvlColor = branchLevel >= prop.requiredPlayerLevel ? "#32FF64" : "#FF4444";
            propertyModalReqText.text = $"<b>Gereken Şube Seviyesi:</b> <color={lvlColor}>Level {prop.requiredPlayerLevel}</color> (Mevcut: Level {branchLevel})";
        }

        if (propertyModalBuyBtn != null)
        {
            bool canAfford = balance >= prop.purchaseCost && branchLevel >= prop.requiredPlayerLevel;
            propertyModalBuyBtn.interactable = canAfford;
            TextMeshProUGUI bTxt = propertyModalBuyBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (bTxt != null)
            {
                bTxt.text = canAfford ? $"Satın Al (${prop.purchaseCost:N0})" : "Yetersiz Şartlar";
            }
        }
    }

    private void OnPropertyConfirmPurchase()
    {
        if (activePropertyToBuy == null) return;
        if (activePropertyToBuy.TryPurchase())
        {
            CloseAllPanels();
        }
    }

    public void CloseAllPanels()
    {
        if (garagePanelRoot != null) garagePanelRoot.SetActive(false);
        if (insurancePanelRoot != null) insurancePanelRoot.SetActive(false);
        if (dispatchPanelRoot != null) dispatchPanelRoot.SetActive(false);
        if (propertyModalRoot != null) propertyModalRoot.SetActive(false);

        activeGarageVehicle = null;
        activePropertyToBuy = null;

        if (FPSPlayerController.Instance != null && FPSPlayerController.Instance.IsOnFoot)
        {
            FPSPlayerController.LockCursor(true);
        }
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
            if (garagePanelRoot != null) DestroyImmediate(garagePanelRoot);
            if (insurancePanelRoot != null) DestroyImmediate(insurancePanelRoot);
            if (dispatchPanelRoot != null) DestroyImmediate(dispatchPanelRoot);
            if (propertyModalRoot != null) DestroyImmediate(propertyModalRoot);
        }

        // 1. Build Garage Panel
        if (garagePanelRoot == null)
        {
            Transform existingG = canvas.transform.Find("GarageWorkshopPanel");
            if (existingG != null)
            {
                garagePanelRoot = existingG.gameObject;
                BindGaragePanel(garagePanelRoot);
            }
            else
            {
                garagePanelRoot = CreateGaragePanel(canvas.transform);
            }
        }
        else
        {
            BindGaragePanel(garagePanelRoot);
        }

        // 2. Build Insurance Panel
        if (insurancePanelRoot == null)
        {
            Transform existingI = canvas.transform.Find("InsuranceAgencyPanel");
            if (existingI != null)
            {
                insurancePanelRoot = existingI.gameObject;
                BindInsurancePanel(insurancePanelRoot);
            }
            else
            {
                insurancePanelRoot = CreateInsurancePanel(canvas.transform);
            }
        }
        else
        {
            BindInsurancePanel(insurancePanelRoot);
        }

        // 3. Build Dispatch Panel
        if (dispatchPanelRoot == null)
        {
            Transform existingD = canvas.transform.Find("PassiveDispatchPanel");
            if (existingD != null)
            {
                dispatchPanelRoot = existingD.gameObject;
                BindDispatchPanel(dispatchPanelRoot);
            }
            else
            {
                dispatchPanelRoot = CreateDispatchPanel(canvas.transform);
            }
        }
        else
        {
            BindDispatchPanel(dispatchPanelRoot);
        }

        // 4. Build Property Purchase Modal
        if (propertyModalRoot == null)
        {
            Transform existingP = canvas.transform.Find("PropertyPurchaseModal");
            if (existingP != null)
            {
                propertyModalRoot = existingP.gameObject;
                BindPropertyModal(propertyModalRoot);
            }
            else
            {
                propertyModalRoot = CreatePropertyModal(canvas.transform);
            }
        }
        else
        {
            BindPropertyModal(propertyModalRoot);
        }
    }

    private void BindGaragePanel(GameObject root)
    {
        if (root == null) return;
        Transform tTitle = root.transform.Find("Title");
        if (tTitle != null) garageTitleText = tTitle.GetComponent<TextMeshProUGUI>();

        Transform tStatus = root.transform.Find("Status");
        if (tStatus != null) garageVehicleStatusText = tStatus.GetComponent<TextMeshProUGUI>();

        Transform tTuneInfo = root.transform.Find("TuningInfo");
        if (tTuneInfo != null) garageTuningInfoText = tTuneInfo.GetComponent<TextMeshProUGUI>();

        Transform tRepair = root.transform.Find("RepairBtn");
        if (tRepair != null)
        {
            garageRepairBtn = tRepair.GetComponent<Button>();
            if (garageRepairBtn != null)
            {
                garageRepairBtn.onClick.RemoveAllListeners();
                garageRepairBtn.onClick.AddListener(OnGarageRepairClicked);
            }
        }

        Transform tTune = root.transform.Find("TuneBtn");
        if (tTune != null)
        {
            garageTuneBtn = tTune.GetComponent<Button>();
            if (garageTuneBtn != null)
            {
                garageTuneBtn.onClick.RemoveAllListeners();
                garageTuneBtn.onClick.AddListener(OnGarageTuneClicked);
            }
        }

        Transform tDrive = root.transform.Find("DriveBtn");
        if (tDrive != null)
        {
            garageDriveBtn = tDrive.GetComponent<Button>();
            if (garageDriveBtn != null)
            {
                garageDriveBtn.onClick.RemoveAllListeners();
                garageDriveBtn.onClick.AddListener(OnGarageDriveClicked);
            }
        }

        Transform tClose = root.transform.Find("CloseBtn");
        if (tClose != null)
        {
            garageCloseBtn = tClose.GetComponent<Button>();
            if (garageCloseBtn != null)
            {
                garageCloseBtn.onClick.RemoveAllListeners();
                garageCloseBtn.onClick.AddListener(CloseAllPanels);
            }
        }
    }

    private void BindInsurancePanel(GameObject root)
    {
        if (root == null) return;
        Transform tStatus = root.transform.Find("Status");
        if (tStatus != null) insuranceStatusText = tStatus.GetComponent<TextMeshProUGUI>();

        Transform tBal = root.transform.Find("Balance");
        if (tBal != null) insuranceBalanceText = tBal.GetComponent<TextMeshProUGUI>();

        Transform t2 = root.transform.Find("Tier2Btn");
        if (t2 != null)
        {
            insuranceTier2UpgradeBtn = t2.GetComponent<Button>();
            if (insuranceTier2UpgradeBtn != null)
            {
                insuranceTier2UpgradeBtn.onClick.RemoveAllListeners();
                insuranceTier2UpgradeBtn.onClick.AddListener(OnInsuranceUpgradeTierClicked);
            }
        }

        Transform t3 = root.transform.Find("Tier3Btn");
        if (t3 != null)
        {
            insuranceTier3UpgradeBtn = t3.GetComponent<Button>();
            if (insuranceTier3UpgradeBtn != null)
            {
                insuranceTier3UpgradeBtn.onClick.RemoveAllListeners();
                insuranceTier3UpgradeBtn.onClick.AddListener(OnInsuranceUpgradeTierClicked);
            }
        }

        Transform tClose = root.transform.Find("CloseBtn");
        if (tClose != null)
        {
            insuranceCloseBtn = tClose.GetComponent<Button>();
            if (insuranceCloseBtn != null)
            {
                insuranceCloseBtn.onClick.RemoveAllListeners();
                insuranceCloseBtn.onClick.AddListener(CloseAllPanels);
            }
        }
    }

    private void BindDispatchPanel(GameObject root)
    {
        if (root == null) return;
        Transform tStatus = root.transform.Find("Status");
        if (tStatus != null) dispatchStatusText = tStatus.GetComponent<TextMeshProUGUI>();

        Transform tRev = root.transform.Find("Revenue");
        if (tRev != null) dispatchRevenueInfoText = tRev.GetComponent<TextMeshProUGUI>();

        Transform tBal = root.transform.Find("Balance");
        if (tBal != null) dispatchBalanceText = tBal.GetComponent<TextMeshProUGUI>();

        Transform t2 = root.transform.Find("Tier2Btn");
        if (t2 != null)
        {
            dispatchTier2UpgradeBtn = t2.GetComponent<Button>();
            if (dispatchTier2UpgradeBtn != null)
            {
                dispatchTier2UpgradeBtn.onClick.RemoveAllListeners();
                dispatchTier2UpgradeBtn.onClick.AddListener(OnDispatchUpgradeClicked);
            }
        }

        Transform t3 = root.transform.Find("Tier3Btn");
        if (t3 != null)
        {
            dispatchTier3UpgradeBtn = t3.GetComponent<Button>();
            if (dispatchTier3UpgradeBtn != null)
            {
                dispatchTier3UpgradeBtn.onClick.RemoveAllListeners();
                dispatchTier3UpgradeBtn.onClick.AddListener(OnDispatchUpgradeClicked);
            }
        }

        Transform tClose = root.transform.Find("CloseBtn");
        if (tClose != null)
        {
            dispatchCloseBtn = tClose.GetComponent<Button>();
            if (dispatchCloseBtn != null)
            {
                dispatchCloseBtn.onClick.RemoveAllListeners();
                dispatchCloseBtn.onClick.AddListener(CloseAllPanels);
            }
        }
    }

    private void BindPropertyModal(GameObject root)
    {
        if (root == null) return;
        Transform tTitle = root.transform.Find("Title");
        if (tTitle != null) propertyModalTitleText = tTitle.GetComponent<TextMeshProUGUI>();

        Transform tDesc = root.transform.Find("Desc");
        if (tDesc != null) propertyModalDescText = tDesc.GetComponent<TextMeshProUGUI>();

        Transform tReq = root.transform.Find("Req");
        if (tReq != null) propertyModalReqText = tReq.GetComponent<TextMeshProUGUI>();

        Transform tCost = root.transform.Find("Cost");
        if (tCost != null) propertyModalCostText = tCost.GetComponent<TextMeshProUGUI>();

        Transform tBuy = root.transform.Find("BuyBtn");
        if (tBuy != null)
        {
            propertyModalBuyBtn = tBuy.GetComponent<Button>();
            if (propertyModalBuyBtn != null)
            {
                propertyModalBuyBtn.onClick.RemoveAllListeners();
                propertyModalBuyBtn.onClick.AddListener(OnPropertyConfirmPurchase);
            }
        }

        Transform tCancel = root.transform.Find("CancelBtn");
        if (tCancel != null)
        {
            propertyModalCancelBtn = tCancel.GetComponent<Button>();
            if (propertyModalCancelBtn != null)
            {
                propertyModalCancelBtn.onClick.RemoveAllListeners();
                propertyModalCancelBtn.onClick.AddListener(CloseAllPanels);
            }
        }
    }

    private GameObject CreateGaragePanel(Transform parent)
    {
        GameObject root = CreateDarkPanel(parent, "GarageWorkshopPanel", new Vector2(850, 480));

        // Header
        garageTitleText = CreateTMPText(root, "Title", "🔧 OTO SERVİS & MODİFİYE ATÖLYESİ", 28, FontStyles.Bold, new Color(0.2f, 0.9f, 1f), TextAlignmentOptions.Center);
        SetRectAnchors(garageTitleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -35), new Vector2(800, 45));

        // Status Line
        garageVehicleStatusText = CreateTMPText(root, "Status", "Yakıt: 50.0 / 50.0 L (%100)  |  Kondisyon: %100", 20, FontStyles.Normal, Color.white, TextAlignmentOptions.Center);
        SetRectAnchors(garageVehicleStatusText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -80), new Vector2(800, 35));

        // Repair Button
        garageRepairBtn = CreateButton(root, "RepairBtn", "🛠️ Tamir & Depo Doldur ($150)", new Vector2(0, -145), new Vector2(750, 56), new Color(0.12f, 0.55f, 0.35f));
        garageRepairBtn.onClick.AddListener(OnGarageRepairClicked);

        // Tuning Info
        garageTuningInfoText = CreateTMPText(root, "TuningInfo", "Motor Performansı: Stage 0 (Standart)", 20, FontStyles.Normal, Color.white, TextAlignmentOptions.Left);
        SetRectAnchors(garageTuningInfoText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -225), new Vector2(750, 30));

        // Tuning Button
        garageTuneBtn = CreateButton(root, "TuneBtn", "🚀 Stage 1 Tork Yükselt ($1.500)", new Vector2(0, -275), new Vector2(750, 56), new Color(0.6f, 0.2f, 0.7f));
        garageTuneBtn.onClick.AddListener(OnGarageTuneClicked);

        // Drive Button
        garageDriveBtn = CreateButton(root, "DriveBtn", "🚗 Aracı Sür & Çıkış Yap", new Vector2(-195, -365), new Vector2(350, 56), new Color(0.15f, 0.45f, 0.85f));
        garageDriveBtn.onClick.AddListener(OnGarageDriveClicked);

        // Close Button
        garageCloseBtn = CreateButton(root, "CloseBtn", "❌ Kapat (ESC)", new Vector2(195, -365), new Vector2(350, 56), new Color(0.35f, 0.38f, 0.45f));
        garageCloseBtn.onClick.AddListener(CloseAllPanels);

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

        propertyModalTitleText = CreateTMPText(root, "Title", "🏢 TİCARİ MÜLK SATIN ALMA", 28, FontStyles.Bold, new Color(1f, 0.85f, 0.2f), TextAlignmentOptions.Center);
        SetRectAnchors(propertyModalTitleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -35), new Vector2(700, 45));

        propertyModalDescText = CreateTMPText(root, "Desc", "Açıklama...", 20, FontStyles.Normal, Color.white, TextAlignmentOptions.Center);
        SetRectAnchors(propertyModalDescText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -100), new Vector2(680, 80));

        propertyModalReqText = CreateTMPText(root, "Req", "Gereken Seviye: Level 2", 20, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        SetRectAnchors(propertyModalReqText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -195), new Vector2(680, 35));

        propertyModalCostText = CreateTMPText(root, "Cost", "Fiyat: $8.000", 22, FontStyles.Bold, new Color(0.3f, 1f, 0.4f), TextAlignmentOptions.Center);
        SetRectAnchors(propertyModalCostText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -240), new Vector2(680, 35));

        propertyModalBuyBtn = CreateButton(root, "BuyBtn", "✅ Satın Al", new Vector2(-165, -340), new Vector2(300, 56), new Color(0.12f, 0.65f, 0.35f));
        propertyModalBuyBtn.onClick.AddListener(OnPropertyConfirmPurchase);

        propertyModalCancelBtn = CreateButton(root, "CancelBtn", "❌ Vazgeç (ESC)", new Vector2(165, -340), new Vector2(300, 56), new Color(0.45f, 0.35f, 0.35f));
        propertyModalCancelBtn.onClick.AddListener(CloseAllPanels);

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
