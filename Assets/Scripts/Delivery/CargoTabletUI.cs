using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using TMPro;

public enum TabletTab
{
    CargoInventory,
    VehicleDealership,
    BranchOffice
}

public class CargoTabletUI : MonoBehaviour
{
    public static CargoTabletUI Instance { get; private set; }

    [Header("--- PANEL REFERENCES ---")]
    public GameObject tabletPanelRoot;
    public Transform cargoListContent;
    public GameObject cargoCardTemplate;

    [Header("--- DETAIL & CLUE PANEL ---")]
    public GameObject detailCardRoot;
    public TextMeshProUGUI trackingNumberText;
    public TextMeshProUGUI recipientNameText;
    public TextMeshProUGUI targetAddressText;
    public TextMeshProUGUI addressDescriptionText;
    public Button dropCargoButton;

    [Header("--- INFO & STATUS ---")]
    public TextMeshProUGUI emptyListText;

    [Header("--- VEHICLE DEALERSHIP & GARAGE TAB ---")]
    public GameObject cargoViewRoot;
    public GameObject vehicleViewRoot;
    public Transform vehicleListContent;
    public GameObject vehicleCardTemplate;
    public GameObject vehicleDetailRoot;
    public TextMeshProUGUI vehicleNameText;
    public TextMeshProUGUI vehicleDescText;
    public TextMeshProUGUI vehicleCapacityText;
    public TextMeshProUGUI vehicleLevelReqText;
    public TextMeshProUGUI vehiclePriceText;
    public TextMeshProUGUI vehicleStatusText;
    public Button vehicleBuyButton;
    public TextMeshProUGUI vehicleBuyButtonText;
    public Button vehicleRecallButton;
    public TextMeshProUGUI vehicleRecallButtonText;

    [Header("--- BRANCH OFFICE UPGRADE TAB ---")]
    public GameObject branchViewRoot;
    public TextMeshProUGUI currentBranchTitleText;
    public TextMeshProUGUI currentBranchDescText;
    public TextMeshProUGUI currentBranchCapacityText;
    public TextMeshProUGUI currentBranchRentText;
    public GameObject nextBranchInfoRoot;
    public TextMeshProUGUI nextBranchTitleText;
    public TextMeshProUGUI nextBranchDescText;
    public TextMeshProUGUI nextBranchCapacityText;
    public TextMeshProUGUI nextBranchRentText;
    public TextMeshProUGUI nextBranchLevelReqText;
    public Button branchUpgradeButton;
    public TextMeshProUGUI branchUpgradeButtonText;
    public TextMeshProUGUI branchMaxLevelBadge;

    [Header("--- TAB SWITCHING & CLOSE BUTTONS ---")]
    public Button tabCargoButton;
    public Button tabVehicleButton;
    public Button tabBranchButton;
    public Button closeTabletButton;

    private CargoItem currentSelectedCargo;
    private DrivableVehicle currentSelectedVehicle;
    private TabletTab currentTab = TabletTab.CargoInventory;
    private bool isTabletOpen = false;

    public bool IsTabletOpen => isTabletOpen;
    public TabletTab CurrentTab => currentTab;

    private void Awake()
    {
        Instance = this;

        if (tabletPanelRoot == null)
        {
            Transform t = transform.Find("CargoTabletPanel");
            if (t != null) tabletPanelRoot = t.gameObject;
        }

        if (tabletPanelRoot != null) tabletPanelRoot.SetActive(false);
        if (cargoCardTemplate != null) cargoCardTemplate.SetActive(false);
        if (vehicleCardTemplate != null) vehicleCardTemplate.SetActive(false);

        if (dropCargoButton != null)
        {
            dropCargoButton.onClick.RemoveAllListeners();
            dropCargoButton.gameObject.SetActive(false);
        }

        EnsureEventSystemAndRaycaster();
    }

    private void Start()
    {
        if (tabletPanelRoot != null) tabletPanelRoot.SetActive(false);
        EnsureEventSystemAndRaycaster();
        EnsureTabletStructure();
    }

    private void OnEnable()
    {
        VanInventory.OnInventoryUpdated += RefreshUI;
        DrivableVehicle.OnVehiclePurchased += HandleVehiclePurchased;
        DrivableVehicle.OnVehicleRecalled += HandleVehicleRecalled;
        DrivableVehicle.OnAnyVehicleReset += HandleAnyVehicleReset;
        BranchManager.OnBranchUpgraded += HandleBranchUpgraded;
        BranchManager.OnBranchReset += HandleBranchReset;
    }

    private void OnDisable()
    {
        VanInventory.OnInventoryUpdated -= RefreshUI;
        DrivableVehicle.OnVehiclePurchased -= HandleVehiclePurchased;
        DrivableVehicle.OnVehicleRecalled -= HandleVehicleRecalled;
        DrivableVehicle.OnAnyVehicleReset -= HandleAnyVehicleReset;
        BranchManager.OnBranchUpgraded -= HandleBranchUpgraded;
        BranchManager.OnBranchReset -= HandleBranchReset;
    }

    private void HandleBranchUpgraded(int lvl, BranchTier tier)
    {
        if (isTabletOpen && currentTab == TabletTab.BranchOffice)
        {
            PopulateBranchInfo();
        }
    }

    private void HandleBranchReset()
    {
        if (isTabletOpen && currentTab == TabletTab.BranchOffice)
        {
            PopulateBranchInfo();
        }
    }

    private void HandleAnyVehicleReset()
    {
        if (isTabletOpen && currentTab == TabletTab.VehicleDealership)
        {
            PopulateVehicleList();
        }
    }

    private void HandleVehiclePurchased(DrivableVehicle v)
    {
        if (isTabletOpen && currentTab == TabletTab.VehicleDealership)
        {
            PopulateVehicleList();
        }
    }

    private void HandleVehicleRecalled(DrivableVehicle v)
    {
        if (isTabletOpen && currentTab == TabletTab.VehicleDealership)
        {
            PopulateVehicleList();
        }
    }

    private void Update()
    {
        if (DaySummaryManager.Instance != null && DaySummaryManager.Instance.summaryPanelRoot != null && DaySummaryManager.Instance.summaryPanelRoot.activeSelf)
        {
            if (isTabletOpen) CloseTablet();
            return;
        }

        if (CheckToggleInput())
        {
            ToggleTablet();
        }
    }

    private bool CheckToggleInput()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.tabKey.wasPressedThisFrame ||
                Keyboard.current.tKey.wasPressedThisFrame ||
                Keyboard.current.mKey.wasPressedThisFrame ||
                Keyboard.current.iKey.wasPressedThisFrame)
            {
                return true;
            }

            if (isTabletOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                return true;
            }
        }

        try
        {
            if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.T) || Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.I))
            {
                return true;
            }
            if (isTabletOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                return true;
            }
        }
        catch { }

        return false;
    }

    public void ToggleTablet()
    {
        if (isTabletOpen) CloseTablet();
        else OpenTablet();
    }

    public void OpenTablet()
    {
        EnsureEventSystemAndRaycaster();
        EnsureTabletStructure();

        if (tabletPanelRoot == null)
        {
            Transform t = transform.Find("CargoTabletPanel");
            if (t != null) tabletPanelRoot = t.gameObject;
        }

        isTabletOpen = true;

        if (tabletPanelRoot != null)
        {
            tabletPanelRoot.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SwitchTab(currentTab);
    }

    public void CloseTablet()
    {
        isTabletOpen = false;
        if (tabletPanelRoot != null) tabletPanelRoot.SetActive(false);

        if (DaySummaryManager.Instance == null || DaySummaryManager.Instance.summaryPanelRoot == null || !DaySummaryManager.Instance.summaryPanelRoot.activeSelf)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void SwitchTab(TabletTab tab)
    {
        currentTab = tab;
        EnsureTabletStructure();

        if (cargoViewRoot != null) cargoViewRoot.SetActive(currentTab == TabletTab.CargoInventory);
        if (vehicleViewRoot != null) vehicleViewRoot.SetActive(currentTab == TabletTab.VehicleDealership);
        if (branchViewRoot != null) branchViewRoot.SetActive(currentTab == TabletTab.BranchOffice);

        Color activeColor = new Color(0.12f, 0.53f, 0.9f, 1f); // Vibrant Cyan/Blue #1E88E5
        Color inactiveColor = new Color(0.09f, 0.13f, 0.19f, 0.95f); // Dark Slate

        // Update tab buttons appearance
        if (tabCargoButton != null)
        {
            Image img = tabCargoButton.GetComponent<Image>();
            if (img != null) img.color = (currentTab == TabletTab.CargoInventory) ? activeColor : inactiveColor;
        }
        if (tabVehicleButton != null)
        {
            Image img = tabVehicleButton.GetComponent<Image>();
            if (img != null) img.color = (currentTab == TabletTab.VehicleDealership) ? activeColor : inactiveColor;
        }
        if (tabBranchButton != null)
        {
            Image img = tabBranchButton.GetComponent<Image>();
            if (img != null) img.color = (currentTab == TabletTab.BranchOffice) ? activeColor : inactiveColor;
        }

        RefreshUI();
    }

    public void RefreshUI()
    {
        if (!isTabletOpen) return;

        if (currentTab == TabletTab.CargoInventory)
        {
            PopulateCargoList();
        }
        else if (currentTab == TabletTab.VehicleDealership)
        {
            PopulateVehicleList();
        }
        else if (currentTab == TabletTab.BranchOffice)
        {
            PopulateBranchInfo();
        }
    }

    // ==========================================
    // CARGO INVENTORY TAB
    // ==========================================
    private void PopulateCargoList()
    {
        if (cargoListContent == null || cargoCardTemplate == null) return;

        foreach (Transform child in cargoListContent)
        {
            if (child.gameObject != cargoCardTemplate)
            {
                Destroy(child.gameObject);
            }
        }

        List<CargoItem> loadedList = VanInventory.Instance != null ? VanInventory.Instance.LoadedCargoList : new List<CargoItem>();

        if (loadedList.Count == 0)
        {
            if (emptyListText != null) emptyListText.gameObject.SetActive(true);
            if (detailCardRoot != null) detailCardRoot.SetActive(false);
            return;
        }

        if (emptyListText != null) emptyListText.gameObject.SetActive(false);

        foreach (CargoItem cargo in loadedList)
        {
            GameObject cardObj = Instantiate(cargoCardTemplate, cargoListContent);
            cardObj.SetActive(true);

            Image cardImg = cardObj.GetComponent<Image>();
            if (cardImg != null) cardImg.raycastTarget = true;

            TextMeshProUGUI label = cardObj.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = $"<b>{cargo.trackingNumber}</b>\n{cargo.recipientName}";
                label.raycastTarget = false;
            }

            Button btn = cardObj.GetComponent<Button>();
            if (btn == null) btn = cardObj.AddComponent<Button>();

            btn.interactable = true;
            btn.onClick.RemoveAllListeners();

            CargoItem itemRef = cargo;
            btn.onClick.AddListener(() => {
                DisplayCargoDetail(itemRef);
            });
        }

        if (currentSelectedCargo == null || !loadedList.Contains(currentSelectedCargo))
        {
            DisplayCargoDetail(loadedList[0]);
        }
        else
        {
            DisplayCargoDetail(currentSelectedCargo);
        }
    }

    public void DisplayCargoDetail(CargoItem cargo)
    {
        currentSelectedCargo = cargo;
        if (detailCardRoot != null) detailCardRoot.SetActive(true);

        if (trackingNumberText != null) trackingNumberText.text = $"Takip No: {cargo.trackingNumber}";
        if (recipientNameText != null) recipientNameText.text = $"Alıcı: {cargo.recipientName}";
        if (targetAddressText != null) targetAddressText.text = $"Adres: {cargo.targetAddress}";
        
        if (addressDescriptionText != null)
        {
            addressDescriptionText.text = $"<b>Adres İpucu ve Açıklaması:</b>\n\n\"{cargo.targetAddressDescription}\"";
        }
    }

    private void OnDropCargoClicked()
    {
        if (currentSelectedCargo == null || VanInventory.Instance == null) return;

        CargoItem toDrop = currentSelectedCargo;
        currentSelectedCargo = null;

        VanInventory.Instance.DropCargoFromVan(toDrop);
        CloseTablet();
    }

    // ==========================================
    // VEHICLE DEALERSHIP & GARAGE TAB
    // ==========================================
    public void PopulateVehicleList()
    {
        if (vehicleListContent == null || vehicleCardTemplate == null) return;

        foreach (Transform child in vehicleListContent)
        {
            if (child.gameObject != vehicleCardTemplate)
            {
                Destroy(child.gameObject);
            }
        }

        List<DrivableVehicle> vehicles = VehicleShowroomManager.Instance != null ?
            VehicleShowroomManager.Instance.GetAllVehicles() :
            new List<DrivableVehicle>(UnityEngine.Object.FindObjectsByType<DrivableVehicle>(FindObjectsInactive.Include, FindObjectsSortMode.None));

        if (vehicles.Count == 0)
        {
            if (vehicleDetailRoot != null) vehicleDetailRoot.SetActive(false);
            return;
        }

        foreach (DrivableVehicle v in vehicles)
        {
            if (v == null) continue;

            GameObject cardObj = Instantiate(vehicleCardTemplate, vehicleListContent);
            cardObj.SetActive(true);

            Image cardImg = cardObj.GetComponent<Image>();
            if (cardImg != null)
            {
                cardImg.raycastTarget = true;
                cardImg.color = v.IsUnlocked ? new Color(0.1f, 0.22f, 0.15f, 0.95f) : new Color(0.15f, 0.15f, 0.18f, 0.95f);
            }

            TextMeshProUGUI label = cardObj.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                string statusTag = v.IsUnlocked ? "<color=#32FF64>✓ SAHİP OLUNDU</color>" : $"<color=#FFAA33>${v.purchasePrice} TL</color> (Lvl {v.requiredPlayerLevel})";
                label.text = $"<b>{v.vehicleName}</b>\n{statusTag}";
                label.raycastTarget = false;
            }

            Button btn = cardObj.GetComponent<Button>();
            if (btn == null) btn = cardObj.AddComponent<Button>();

            btn.interactable = true;
            btn.onClick.RemoveAllListeners();

            DrivableVehicle vRef = v;
            btn.onClick.AddListener(() => {
                DisplayVehicleDetail(vRef);
            });
        }

        if (currentSelectedVehicle == null || !vehicles.Contains(currentSelectedVehicle))
        {
            DisplayVehicleDetail(vehicles[0]);
        }
        else
        {
            DisplayVehicleDetail(currentSelectedVehicle);
        }
    }

    public void DisplayVehicleDetail(DrivableVehicle v)
    {
        currentSelectedVehicle = v;
        if (v == null)
        {
            if (vehicleDetailRoot != null) vehicleDetailRoot.SetActive(false);
            return;
        }

        if (vehicleDetailRoot != null) vehicleDetailRoot.SetActive(true);

        int playerLvl = PlayerProgressionManager.Instance != null ? PlayerProgressionManager.Instance.PlayerLevel : 1;
        int balance = PlayerEconomyManager.Instance != null ? PlayerEconomyManager.Instance.CurrentLiveBalance : 0;

        if (vehicleNameText != null) vehicleNameText.text = v.vehicleName;
        if (vehicleDescText != null) vehicleDescText.text = $"<b>Açıklama:</b>\n{v.description}";
        if (vehicleCapacityText != null) vehicleCapacityText.text = $"📦 <b>Koli Kapasitesi:</b> {v.cargoCapacity} Paket";
        
        if (vehicleLevelReqText != null)
        {
            bool lvlOk = playerLvl >= v.requiredPlayerLevel;
            string lvlColor = lvlOk ? "#32FF64" : "#FF5555";
            vehicleLevelReqText.text = $"🛡️ <b>Gereken Seviye:</b> <color={lvlColor}>Seviye {v.requiredPlayerLevel}</color> (Mevcut: Seviye {playerLvl})";
        }

        if (vehiclePriceText != null)
        {
            bool cashOk = balance >= v.purchasePrice;
            string cashColor = cashOk ? "#32FF64" : "#FF5555";
            vehiclePriceText.text = $"💰 <b>Fiyat:</b> <color={cashColor}>${v.purchasePrice} TL</color> (Bakiye: ${balance} TL)";
        }

        if (vehicleStatusText != null)
        {
            if (v.IsUnlocked)
            {
                vehicleStatusText.text = "<color=#32FF64>★ Bu araca sahipsiniz. Dilediğinizde sürebilir veya şube garajına çağırabilirsiniz. ★</color>";
            }
            else
            {
                if (playerLvl < v.requiredPlayerLevel)
                {
                    vehicleStatusText.text = $"<color=#FF5555>🔒 KİLİTLİ: Satın almak için Seviye {v.requiredPlayerLevel} olmalısınız.</color>";
                }
                else if (balance < v.purchasePrice)
                {
                    vehicleStatusText.text = $"<color=#FFAA33>🔒 KİLİTLİ: Yetersiz bakiye! ${(v.purchasePrice - balance)} TL daha gerekli.</color>";
                }
                else
                {
                    vehicleStatusText.text = "<color=#32FFFF>✓ SATIN ALINABİLİR: Aracı hemen satın alabilirsiniz!</color>";
                }
            }
        }

        // Buy & Recall Button states
        if (vehicleBuyButton != null)
        {
            if (v.IsUnlocked)
            {
                vehicleBuyButton.gameObject.SetActive(false);
            }
            else
            {
                vehicleBuyButton.gameObject.SetActive(true);
                bool canAfford = (playerLvl >= v.requiredPlayerLevel) && (balance >= v.purchasePrice);
                vehicleBuyButton.interactable = canAfford;

                Image btnImg = vehicleBuyButton.GetComponent<Image>();
                if (btnImg != null) btnImg.color = canAfford ? new Color(0.1f, 0.7f, 0.35f) : new Color(0.3f, 0.3f, 0.35f);

                if (vehicleBuyButtonText != null)
                {
                    vehicleBuyButtonText.text = canAfford ? $"🛒 ARACI SATIN AL (${v.purchasePrice} TL)" : $"YETERSİZ ŞARTLAR (${v.purchasePrice} TL)";
                }

                vehicleBuyButton.onClick.RemoveAllListeners();
                vehicleBuyButton.onClick.AddListener(() => {
                    if (v.TryPurchase())
                    {
                        DisplayVehicleDetail(v);
                        PopulateVehicleList();
                    }
                });
            }
        }

        if (vehicleRecallButton != null)
        {
            vehicleRecallButton.gameObject.SetActive(false);
        }
    }

    // ==========================================
    // BRANCH OFFICE UPGRADE TAB
    // ==========================================
    private void PopulateBranchInfo()
    {
        if (BranchManager.Instance == null) return;

        BranchTier current = BranchManager.Instance.CurrentTier;
        BranchTier next = BranchManager.Instance.NextTier;

        if (current != null)
        {
            if (currentBranchTitleText != null) currentBranchTitleText.text = $"<b>{current.tierName}</b> <color=#32FFFF>(Seviye {current.tierLevel})</color>";
            if (currentBranchDescText != null) currentBranchDescText.text = current.description;
            if (currentBranchCapacityText != null) currentBranchCapacityText.text = $"📦 <b>Günlük Paket Kapasitesi:</b> {current.dailyPackageCapacity} Paket";
            if (currentBranchRentText != null) currentBranchRentText.text = $"💸 <b>Günlük İşletme Kirası:</b> ${current.dailyRent} TL / gün";
        }

        if (next != null)
        {
            if (nextBranchTitleText != null)
            {
                nextBranchTitleText.gameObject.SetActive(true);
                nextBranchTitleText.text = $"<b>{next.tierName}</b> <color=#32FF64>(Seviye {next.tierLevel})</color>";
            }
            if (nextBranchDescText != null)
            {
                nextBranchDescText.gameObject.SetActive(true);
                nextBranchDescText.text = next.description;
            }
            if (nextBranchCapacityText != null)
            {
                nextBranchCapacityText.gameObject.SetActive(true);
                nextBranchCapacityText.text = $"📦 <b>Yeni Paket Kotası:</b> {current.dailyPackageCapacity} ➔ <color=#32FF64>{next.dailyPackageCapacity} Paket</color> (+{next.dailyPackageCapacity - current.dailyPackageCapacity})";
            }
            if (nextBranchRentText != null)
            {
                nextBranchRentText.gameObject.SetActive(true);
                nextBranchRentText.text = $"💸 <b>Yeni Kira Bedeli:</b> ${current.dailyRent} ➔ <color=#FFAA33>${next.dailyRent} TL</color>";
            }

            int playerLvl = PlayerProgressionManager.Instance != null ? PlayerProgressionManager.Instance.PlayerLevel : 1;
            int balance = PlayerEconomyManager.Instance != null ? PlayerEconomyManager.Instance.CurrentLiveBalance : 0;

            if (nextBranchLevelReqText != null)
            {
                nextBranchLevelReqText.gameObject.SetActive(true);
                string lvlTag = playerLvl >= next.requiredPlayerLevel ? "<color=#32FF64>" : "<color=#FF5555>";
                nextBranchLevelReqText.text = $"👤 <b>Gerekli Seviye:</b> {lvlTag}Seviye {next.requiredPlayerLevel} (Senin: {playerLvl})</color>";
            }

            if (branchUpgradeButton != null)
            {
                branchUpgradeButton.gameObject.SetActive(true);
                bool canAfford = balance >= next.upgradeCost;
                bool canLevel = playerLvl >= next.requiredPlayerLevel;

                branchUpgradeButton.interactable = canAfford && canLevel;

                Image btnImg = branchUpgradeButton.GetComponent<Image>();
                if (btnImg != null)
                {
                    btnImg.color = (canAfford && canLevel) ? new Color(0.1f, 0.7f, 0.35f) : new Color(0.3f, 0.3f, 0.35f);
                }

                if (branchUpgradeButtonText != null)
                {
                    if (!canLevel) branchUpgradeButtonText.text = $"🔒 SEVİYE {next.requiredPlayerLevel} GEREKLİ";
                    else if (!canAfford) branchUpgradeButtonText.text = $"🔒 YETERSİZ BAKİYE (${next.upgradeCost} TL)";
                    else branchUpgradeButtonText.text = $"🏢 ŞUBEYİ GELİŞTİR (${next.upgradeCost} TL)";
                }
            }

            if (branchMaxLevelBadge != null) branchMaxLevelBadge.gameObject.SetActive(false);
        }
        else
        {
            // Max level reached
            if (nextBranchTitleText != null) nextBranchTitleText.gameObject.SetActive(false);
            if (nextBranchDescText != null) nextBranchDescText.gameObject.SetActive(false);
            if (nextBranchCapacityText != null) nextBranchCapacityText.gameObject.SetActive(false);
            if (nextBranchRentText != null) nextBranchRentText.gameObject.SetActive(false);
            if (nextBranchLevelReqText != null) nextBranchLevelReqText.gameObject.SetActive(false);
            if (branchUpgradeButton != null) branchUpgradeButton.gameObject.SetActive(false);
            if (branchMaxLevelBadge != null) branchMaxLevelBadge.gameObject.SetActive(true);
        }
    }

    private void OnUpgradeBranchClicked()
    {
        if (BranchManager.Instance != null)
        {
            bool ok = BranchManager.Instance.TryUpgradeBranch();
            if (ok)
            {
                PopulateBranchInfo();
                if (InteractionPromptHUD.Instance != null)
                {
                    InteractionPromptHUD.Instance.ShowPrompt("<color=#32FFFF>★ ŞUBE BAŞARIYLA GELİŞTİRİLDİ! ★</color>");
                }
            }
        }
    }

    /// <summary>
    /// Ensures tablet hierarchy is structured with a TopBar and ContentArea containing 3 separate views.
    /// </summary>
    public void EnsureTabletStructure()
    {
        if (tabletPanelRoot == null)
        {
            Transform t = transform.Find("CargoTabletPanel");
            if (t != null) tabletPanelRoot = t.gameObject;
        }

        if (tabletPanelRoot == null) return;

        // Remove any old conflicting layout group directly on tabletPanelRoot
        HorizontalLayoutGroup hlg = tabletPanelRoot.GetComponent<HorizontalLayoutGroup>();
        if (hlg != null) DestroyImmediate(hlg);

        VerticalLayoutGroup vlg = tabletPanelRoot.GetComponent<VerticalLayoutGroup>();
        if (vlg != null) DestroyImmediate(vlg);

        // Find or locate TabletTopBar
        Transform topBarTransform = tabletPanelRoot.transform.Find("TabletTopBar");
        if (topBarTransform == null)
        {
            // check legacy TabBar or TabletTabBar
            Transform oldBar = tabletPanelRoot.transform.Find("TabletTabBar");
            if (oldBar == null) oldBar = tabletPanelRoot.transform.Find("TabBar");
            if (oldBar != null)
            {
                oldBar.name = "TabletTopBar";
                topBarTransform = oldBar;
            }
        }

        // Find or locate TabletContentArea
        Transform contentAreaTransform = tabletPanelRoot.transform.Find("TabletContentArea");

        // Hook up tab buttons if present in TopBar
        if (topBarTransform != null)
        {
            if (tabCargoButton == null)
            {
                Transform b = topBarTransform.Find("TabBar/Tab_Cargo");
                if (b == null) b = topBarTransform.Find("Tab_Cargo");
                if (b != null) tabCargoButton = b.GetComponent<Button>();
            }

            if (tabVehicleButton == null)
            {
                Transform b = topBarTransform.Find("TabBar/Tab_Vehicles");
                if (b == null) b = topBarTransform.Find("Tab_Vehicles");
                if (b != null) tabVehicleButton = b.GetComponent<Button>();
            }

            if (tabBranchButton == null)
            {
                Transform b = topBarTransform.Find("TabBar/Tab_Branch");
                if (b == null) b = topBarTransform.Find("Tab_Branch");
                if (b != null) tabBranchButton = b.GetComponent<Button>();
            }

            if (closeTabletButton == null)
            {
                Transform b = topBarTransform.Find("CloseButton");
                if (b != null) closeTabletButton = b.GetComponent<Button>();
            }
        }

        // Bind button listeners safely
        if (tabCargoButton != null)
        {
            tabCargoButton.onClick.RemoveAllListeners();
            tabCargoButton.onClick.AddListener(() => SwitchTab(TabletTab.CargoInventory));
        }

        if (tabVehicleButton != null)
        {
            tabVehicleButton.onClick.RemoveAllListeners();
            tabVehicleButton.onClick.AddListener(() => SwitchTab(TabletTab.VehicleDealership));
        }

        if (tabBranchButton != null)
        {
            tabBranchButton.onClick.RemoveAllListeners();
            tabBranchButton.onClick.AddListener(() => SwitchTab(TabletTab.BranchOffice));
        }

        if (closeTabletButton != null)
        {
            closeTabletButton.onClick.RemoveAllListeners();
            closeTabletButton.onClick.AddListener(CloseTablet);
        }

        // Locate views
        if (cargoViewRoot == null)
        {
            Transform v = tabletPanelRoot.transform.Find("TabletContentArea/CargoViewRoot");
            if (v == null) v = tabletPanelRoot.transform.Find("CargoViewRoot");
            if (v != null) cargoViewRoot = v.gameObject;
        }

        if (vehicleViewRoot == null)
        {
            Transform v = tabletPanelRoot.transform.Find("TabletContentArea/VehicleViewRoot");
            if (v == null) v = tabletPanelRoot.transform.Find("VehicleDealershipView");
            if (v == null) v = tabletPanelRoot.transform.Find("VehicleViewRoot");
            if (v != null) vehicleViewRoot = v.gameObject;
        }

        if (branchViewRoot == null)
        {
            Transform v = tabletPanelRoot.transform.Find("TabletContentArea/BranchViewRoot");
            if (v == null) v = tabletPanelRoot.transform.Find("BranchViewRoot");
            if (v != null) branchViewRoot = v.gameObject;
        }

        // Bind branch upgrade button listener if not already bound
        if (branchUpgradeButton != null)
        {
            branchUpgradeButton.onClick.RemoveAllListeners();
            branchUpgradeButton.onClick.AddListener(OnUpgradeBranchClicked);
        }

        // Disable legacy cargo drop button
        if (dropCargoButton != null)
        {
            dropCargoButton.onClick.RemoveAllListeners();
            dropCargoButton.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// EventSystem and GraphicRaycaster verification
    /// </summary>
    public void EnsureEventSystemAndRaycaster()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();

        if (canvas != null)
        {
            GraphicRaycaster gr = canvas.GetComponent<GraphicRaycaster>();
            if (gr == null) canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        EventSystem es = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
        if (es == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            es = esObj.AddComponent<EventSystem>();
        }

#if ENABLE_INPUT_SYSTEM
        if (es.GetComponent<InputSystemUIInputModule>() == null)
        {
            StandaloneInputModule oldMod = es.GetComponent<StandaloneInputModule>();
            if (oldMod != null) DestroyImmediate(oldMod);

            es.gameObject.AddComponent<InputSystemUIInputModule>();
        }
#else
        if (es.GetComponent<StandaloneInputModule>() == null)
        {
            es.gameObject.AddComponent<StandaloneInputModule>();
        }
#endif
    }
}
