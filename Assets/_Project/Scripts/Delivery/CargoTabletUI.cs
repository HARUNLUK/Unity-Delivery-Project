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
    public TextMeshProUGUI vehicleFuelStatusText;
    public Button vehicleBuyButton;
    public TextMeshProUGUI vehicleBuyButtonText;
    public Button vehicleRefuelButton;
    public TextMeshProUGUI vehicleRefuelButtonText;
    public Button vehicleRecallButton;
    public TextMeshProUGUI vehicleRecallButtonText;

    [Header("--- BRANCH OFFICE UPGRADE TAB ---")]
    public GameObject branchViewRoot;
    public TextMeshProUGUI currentBranchTitleText;
    public TextMeshProUGUI currentBranchDescText;
    public TextMeshProUGUI currentBranchCapacityText;
    public TextMeshProUGUI currentBranchRentText;
    public GameObject nextBranchInfoRoot;
    public GameObject nextDescBoxRoot;
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
    public Button endShiftButton;
    public Button closeTabletButton;

    private PhysicalCargoPackage currentSelectedPackage;
    private CargoItem currentSelectedCargo;
    private DrivableVehicle currentSelectedVehicle;
    private TabletTab currentTab = TabletTab.CargoInventory;
    private bool isTabletOpen = false;
    private bool isTerminalMode = false;

    public bool IsTabletOpen => isTabletOpen;
    public bool IsTerminalMode => isTerminalMode;
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
        AddressLocalizationManager.OnLanguageChanged += HandleLanguageChanged;
        LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
    }

    private void OnDisable()
    {
        VanInventory.OnInventoryUpdated -= RefreshUI;
        DrivableVehicle.OnVehiclePurchased -= HandleVehiclePurchased;
        DrivableVehicle.OnVehicleRecalled -= HandleVehicleRecalled;
        DrivableVehicle.OnAnyVehicleReset -= HandleAnyVehicleReset;
        BranchManager.OnBranchUpgraded -= HandleBranchUpgraded;
        BranchManager.OnBranchReset -= HandleBranchReset;
        AddressLocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
        LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
    }

    private void HandleLanguageChanged(string newLang)
    {
        RefreshLocalizedUI();
        if (isTabletOpen)
        {
            RefreshUI();
            if (currentTab == TabletTab.CargoInventory)
            {
                if (currentSelectedPackage != null)
                {
                    DisplayCargoDetail(currentSelectedPackage);
                }
                else if (currentSelectedCargo != null)
                {
                    DisplayCargoDetail(currentSelectedCargo);
                }
            }
        }
    }

    public void RefreshLocalizedUI()
    {
        if (tabCargoButton != null)
        {
            var tmp = tabCargoButton.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = LocalizationManager.Get("tablet_tab_cargo", "DELIVERIES");
        }
        if (tabVehicleButton != null)
        {
            var tmp = tabVehicleButton.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = LocalizationManager.Get("tablet_tab_vehicles", "VEHICLES");
        }
        if (tabBranchButton != null)
        {
            var tmp = tabBranchButton.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = LocalizationManager.Get("tablet_tab_branch", "BRANCH");
        }
        if (endShiftButton != null)
        {
            var tmp = endShiftButton.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = LocalizationManager.Get("tablet_btn_end_shift", "END SHIFT");
        }
        if (closeTabletButton != null)
        {
            var tmp = closeTabletButton.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = LocalizationManager.Get("tablet_btn_close", "CLOSE");
        }
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

        if (CommercialHubUIManager.Instance != null && CommercialHubUIManager.Instance.IsAnyPanelOpen)
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
            if (KeyBindingManager.WasPressedThisFrame(GameAction.Tablet) ||
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

#if ENABLE_LEGACY_INPUT_MANAGER
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
#endif

        return false;
    }

    public void ToggleTablet()
    {
        if (isTabletOpen) CloseTablet();
        else OpenTablet();
    }

    public void OpenTablet()
    {
        isTerminalMode = false;

        if (InteractionPromptHUD.Instance != null)
        {
            InteractionPromptHUD.Instance.SuppressPrompts(0.35f);
        }

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

        // Keep standard tablet tabs visible, but branch tab hidden
        if (tabCargoButton != null) tabCargoButton.gameObject.SetActive(true);
        if (tabVehicleButton != null) tabVehicleButton.gameObject.SetActive(true);
        if (tabBranchButton != null) tabBranchButton.gameObject.SetActive(false);
        if (endShiftButton != null) endShiftButton.gameObject.SetActive(true);

        // Never open directly into BranchOffice from general tablet hotkeys
        if (currentTab == TabletTab.BranchOffice)
        {
            currentTab = TabletTab.CargoInventory;
        }

        FPSPlayerController.LockCursor(false);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayTabletOpen();
        }

        SwitchTab(currentTab);
    }

    /// <summary>
    /// Opens the Branch Upgrade Terminal UI exclusively when interacting with the in-world terminal object.
    /// In this mode, regular tablet tabs are hidden, isolating the screen to the branch management dashboard.
    /// </summary>
    public void OpenBranchUpgradeTerminalUI()
    {
        if (InteractionPromptHUD.Instance != null)
        {
            InteractionPromptHUD.Instance.SuppressPrompts(0.35f);
        }

        EnsureEventSystemAndRaycaster();
        EnsureTabletStructure();

        if (tabletPanelRoot == null)
        {
            Transform t = transform.Find("CargoTabletPanel");
            if (t != null) tabletPanelRoot = t.gameObject;
        }

        isTerminalMode = true;
        isTabletOpen = true;

        if (tabletPanelRoot != null)
        {
            tabletPanelRoot.SetActive(true);
        }

        // Hide regular tablet navigation tabs while using the physical branch terminal
        if (tabCargoButton != null) tabCargoButton.gameObject.SetActive(false);
        if (tabVehicleButton != null) tabVehicleButton.gameObject.SetActive(false);
        if (tabBranchButton != null) tabBranchButton.gameObject.SetActive(false);
        if (endShiftButton != null) endShiftButton.gameObject.SetActive(false);

        currentTab = TabletTab.BranchOffice;

        if (cargoViewRoot != null) cargoViewRoot.SetActive(false);
        if (vehicleViewRoot != null) vehicleViewRoot.SetActive(false);
        if (branchViewRoot != null) branchViewRoot.SetActive(true);

        FPSPlayerController.LockCursor(false);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayTabletOpen();
        }

        PopulateBranchInfo();
    }

    public void CloseTablet()
    {
        if (InteractionPromptHUD.Instance != null)
        {
            InteractionPromptHUD.Instance.SuppressPrompts(0.35f);
        }

        if (isTabletOpen && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayTabletClose();
        }

        isTabletOpen = false;
        isTerminalMode = false;
        if (tabletPanelRoot != null) tabletPanelRoot.SetActive(false);

        // Restore tab buttons state
        if (tabCargoButton != null) tabCargoButton.gameObject.SetActive(true);
        if (tabVehicleButton != null) tabVehicleButton.gameObject.SetActive(true);
        if (tabBranchButton != null) tabBranchButton.gameObject.SetActive(false);
        if (endShiftButton != null) endShiftButton.gameObject.SetActive(true);

        FPSPlayerController.LockCursor(true);
    }

    public void SwitchTab(TabletTab tab)
    {
        // BranchOffice is exclusive to in-world terminal interaction
        if (tab == TabletTab.BranchOffice && !isTerminalMode)
        {
            tab = TabletTab.CargoInventory;
        }

        currentTab = tab;
        EnsureTabletStructure();

        if (AudioManager.Instance != null && isTabletOpen)
        {
            AudioManager.Instance.PlayTabSwitch();
        }

        if (cargoViewRoot != null) cargoViewRoot.SetActive(currentTab == TabletTab.CargoInventory);
        if (vehicleViewRoot != null) vehicleViewRoot.SetActive(currentTab == TabletTab.VehicleDealership);
        if (branchViewRoot != null) branchViewRoot.SetActive(currentTab == TabletTab.BranchOffice);

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
    // 1. CARGO INVENTORY TAB
    // ==========================================
    private void PopulateCargoList()
    {
        if (cargoListContent == null || cargoCardTemplate == null) EnsureTabletStructure();
        if (cargoListContent == null || cargoCardTemplate == null) return;

        foreach (Transform child in cargoListContent)
        {
            if (child.gameObject != cargoCardTemplate)
            {
                Destroy(child.gameObject);
            }
        }

        PhysicalCargoPackage[] scenePackages = UnityEngine.Object.FindObjectsByType<PhysicalCargoPackage>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        if (scenePackages == null || scenePackages.Length == 0)
        {
            if (emptyListText != null)
            {
                emptyListText.gameObject.SetActive(true);
                emptyListText.text = LocalizationManager.Get("tablet_empty_cargo", "No packages in vehicle to deliver.\nLoad new packages from the warehouse.");
            }
            if (detailCardRoot != null) detailCardRoot.SetActive(false);
            return;
        }

        if (emptyListText != null) emptyListText.gameObject.SetActive(false);

        // Sort by pointId or tracking number for consistent order
        System.Array.Sort(scenePackages, (a, b) =>
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;
            int aId, bId;
            if (int.TryParse(a.targetPointId, out aId) && int.TryParse(b.targetPointId, out bId))
            {
                return aId.CompareTo(bId);
            }
            return string.Compare(a.targetPointId, b.targetPointId, System.StringComparison.OrdinalIgnoreCase);
        });

        PhysicalCargoPackage packageToSelect = null;

        foreach (PhysicalCargoPackage pkg in scenePackages)
        {
            if (pkg == null) continue;

            GameObject cardObj = Instantiate(cargoCardTemplate, cargoListContent);
            cardObj.SetActive(true);

            DeliveryPoint nearbyPoint = pkg.FindNearbyDeliveryPoint();
            bool isAtDeliveryZone = nearbyPoint != null;
            bool isBroken = pkg.isBroken;

            var recText = FindTMPRecursive(cardObj.transform, "RecipientName", "Recipient", "Name");
            var addrText = FindTMPRecursive(cardObj.transform, "Address", "TargetAddress");
            var hintText = FindTMPRecursive(cardObj.transform, "Hint", "Status", "Badge", "Type");

            if (recText != null) recText.text = pkg.EffectiveRecipientName;
            if (addrText != null) addrText.text = pkg.EffectiveAddressName;
            if (hintText != null)
            {
                if (isBroken) hintText.text = $"<color=#FF5555>{LocalizationManager.Get("cargo_status_broken", "DAMAGED")}</color>";
                else if (isAtDeliveryZone) hintText.text = $"<color=#FFD700>{LocalizationManager.Get("cargo_status_at_zone", "AT ZONE")}</color>";
                else if (pkg.cargoType == CargoType.Express) hintText.text = $"<color=#33E0FF>{LocalizationManager.Get("cargo_type_express", "EXP")} {pkg.GetFormattedTargetDeliveryTime()}</color>";
                else if (pkg.cargoType == CargoType.Fragile) hintText.text = $"<color=#FFAA44>{LocalizationManager.Get("cargo_type_fragile", "FRAGILE")}</color>";
                else if (pkg.cargoType == CargoType.Explosive) hintText.text = $"<color=#FF3300>{LocalizationManager.Get("cargo_type_explosive", "EXPLOSIVE")}</color>";
                else hintText.text = LocalizationManager.Get("cargo_status_in_transit", "IN TRANSIT");
            }

            // Fallback for single text label card template
            if (recText == null && addrText == null && hintText == null)
            {
                TextMeshProUGUI label = cardObj.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    string statusBadge = isBroken ? $"[{LocalizationManager.Get("cargo_status_broken", "DAMAGED")}]" : (isAtDeliveryZone ? $"[{LocalizationManager.Get("cargo_status_at_zone", "AT ZONE")}]" : $"[{LocalizationManager.Get("cargo_status_in_transit", "IN TRANSIT")}]");
                    label.text = $"<b>{pkg.EffectiveRecipientName}</b>\n<size=85%>{pkg.EffectiveAddressName}</size>\n<size=80%>{statusBadge}</size>";
                }
            }

            Button btn = cardObj.GetComponent<Button>();
            if (btn == null) btn = cardObj.AddComponent<Button>();

            btn.interactable = true;
            btn.onClick.RemoveAllListeners();

            PhysicalCargoPackage pkgRef = pkg;
            btn.onClick.AddListener(() => {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
                DisplayCargoDetail(pkgRef);
            });

            if (packageToSelect == null && currentSelectedPackage != null && currentSelectedPackage == pkg)
            {
                packageToSelect = pkg;
            }
        }

        if (packageToSelect == null && scenePackages.Length > 0)
        {
            packageToSelect = scenePackages[0];
        }

        if (packageToSelect != null)
        {
            DisplayCargoDetail(packageToSelect);
        }
    }

    public void DisplayCargoDetail(PhysicalCargoPackage pkg)
    {
        currentSelectedPackage = pkg;
        if (pkg == null)
        {
            if (detailCardRoot != null) detailCardRoot.SetActive(false);
            return;
        }

        if (detailCardRoot != null) detailCardRoot.SetActive(true);

        if (recipientNameText != null)
        {
            recipientNameText.text = LocalizationManager.GetFormat("tablet_recipient", "Recipient: {0}", pkg.EffectiveRecipientName);
        }

        if (targetAddressText != null)
        {
            targetAddressText.text = LocalizationManager.GetFormat("tablet_address", "Address: {0}", pkg.EffectiveAddressName);
        }

        if (addressDescriptionText != null)
        {
            string desc = pkg.EffectiveAddressDescription;
            addressDescriptionText.text = !string.IsNullOrEmpty(desc) ? desc : LocalizationManager.Get("cargo_no_clue", "No specific visual clue available for this address.");
        }
    }

    public void DisplayCargoDetail(CargoItem cargo)
    {
        currentSelectedCargo = cargo;
        if (detailCardRoot != null) detailCardRoot.SetActive(true);

        if (trackingNumberText != null) trackingNumberText.text = LocalizationManager.GetFormat("tablet_tracking_no", "Tracking #: {0}", cargo.trackingNumber);
        if (recipientNameText != null) recipientNameText.text = LocalizationManager.GetFormat("tablet_recipient", "Recipient: {0}", cargo.recipientName);
        if (targetAddressText != null) targetAddressText.text = LocalizationManager.GetFormat("tablet_address", "Address: {0}", cargo.targetAddress);

        if (addressDescriptionText != null)
        {
            string desc = AddressLocalizationManager.GetDescription(cargo.targetPointId, cargo.targetAddressDescription);
            addressDescriptionText.text = !string.IsNullOrEmpty(desc) ? desc : LocalizationManager.Get("cargo_no_clue", "No specific visual clue available for this address.");
        }
    }

    public void OnEndShiftButtonClicked()
    {
        CloseTablet();
        if (DayTimeManager.Instance != null)
        {
            DayTimeManager.Instance.EndShift();
        }
    }

    // ==========================================
    // 2. VEHICLE DEALERSHIP & GARAGE TAB
    // ==========================================
    public void PopulateVehicleList()
    {
        if (vehicleListContent == null || vehicleCardTemplate == null) EnsureTabletStructure();
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

            TextMeshProUGUI label = cardObj.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                if (v.IsUnlocked)
                {
                    label.text = $"<b>{v.vehicleName}</b>\n[{LocalizationManager.Get("vehicle_status_owned", "OWNED")}]";
                }
                else
                {
                    label.text = $"<b>{v.vehicleName}</b>\n${v.purchasePrice:N0} ({LocalizationManager.GetFormat("vehicle_lvl_req", "Lvl {0}", v.requiredPlayerLevel)})";
                }
                label.raycastTarget = false;
            }

            Button btn = cardObj.GetComponent<Button>();
            if (btn == null) btn = cardObj.AddComponent<Button>();

            btn.interactable = true;
            btn.onClick.RemoveAllListeners();

            DrivableVehicle vRef = v;
            btn.onClick.AddListener(() => {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
                DisplayVehicleDetail(vRef);
            });
        }

        DrivableVehicle playerInsideVeh = vehicles.Find(v => v != null && v.isPlayerInside);
        if (currentSelectedVehicle == null || !vehicles.Contains(currentSelectedVehicle))
        {
            DisplayVehicleDetail(playerInsideVeh != null ? playerInsideVeh : vehicles[0]);
        }
        else
        {
            if (playerInsideVeh != null && currentSelectedVehicle != playerInsideVeh)
            {
                DisplayVehicleDetail(playerInsideVeh);
            }
            else
            {
                DisplayVehicleDetail(currentSelectedVehicle);
            }
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

        if (vehicleNameText != null) vehicleNameText.text = v.vehicleName;
        if (vehicleDescText != null) vehicleDescText.text = v.description;

        float fuelPct = v.maxFuel > 0 ? (v.currentFuel / v.maxFuel) * 100f : 0f;
        string fuelColor = v.currentFuel <= 0.05f ? "#FF4444" : (fuelPct < 25f ? "#FFAA33" : "#32FF64");
        float condPct = v.ConditionPercentage * 100f;
        string condColor = condPct < 25f ? "#FF4444" : (condPct < 70f ? "#FFAA33" : "#32FF64");

        if (vehicleFuelStatusText != null)
        {
            string fuelTag = v.currentFuel <= 0.05f ? $"<color=#FF4444>[{LocalizationManager.Get("vehicle_status_empty", "EMPTY")}]</color>" : (fuelPct < 25f ? $"<color=#FFAA33>[{LocalizationManager.Get("vehicle_status_low", "LOW")}]</color>" : $"<color=#32FF64>[{LocalizationManager.Get("vehicle_status_ok", "OK")}]</color>");
            vehicleFuelStatusText.text = $"<b>{LocalizationManager.Get("vehicle_fuel_tank", "Fuel Tank:")}</b> <color={fuelColor}>{v.currentFuel:F1} / {v.maxFuel:F1} L ({fuelPct:F0}%)</color> {fuelTag}  |  <b>{LocalizationManager.Get("vehicle_condition", "Condition:")}</b> <color={condColor}>%{condPct:F0}</color>";
        }

        // Buy Button
        if (vehicleBuyButton != null)
        {
            if (v.IsUnlocked)
            {
                vehicleBuyButton.gameObject.SetActive(false);
            }
            else
            {
                vehicleBuyButton.gameObject.SetActive(true);
                vehicleBuyButton.interactable = true;

                if (vehicleBuyButtonText != null)
                {
                    vehicleBuyButtonText.text = LocalizationManager.GetFormat("vehicle_btn_purchase", "PURCHASE VEHICLE (${0:N0})", v.purchasePrice);
                }

                vehicleBuyButton.onClick.RemoveAllListeners();
                vehicleBuyButton.onClick.AddListener(() => {
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
                    if (v.TryPurchase())
                    {
                        DisplayVehicleDetail(v);
                        PopulateVehicleList();
                    }
                    else
                    {
                        FlashButtonError(vehicleBuyButton, new Color(0.1f, 0.7f, 0.35f));
                    }
                });
            }
        }

        // Emergency Roadside Refuel Button
        if (vehicleRefuelButton != null)
        {
            if (!v.IsUnlocked)
            {
                vehicleRefuelButton.gameObject.SetActive(false);
            }
            else
            {
                vehicleRefuelButton.gameObject.SetActive(true);
                vehicleRefuelButton.interactable = true;
                float fuelToAdd = Mathf.Min(15f, v.maxFuel - v.currentFuel);
                int fuelCost = Mathf.RoundToInt(fuelToAdd * 3f);

                if (fuelToAdd <= 0.2f)
                {
                    if (vehicleRefuelButtonText != null)
                    {
                        vehicleRefuelButtonText.text = LocalizationManager.Get("vehicle_btn_fuel_full", "FUEL TANK FULL (100%)");
                    }

                    vehicleRefuelButton.onClick.RemoveAllListeners();
                    vehicleRefuelButton.onClick.AddListener(() => {
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
                        if (InteractionPromptHUD.Instance != null)
                        {
                            InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.Get("prompt_gas_station_full", "<color=#32FF64>[TANK FULL] Your fuel tank is completely full.</color>"));
                        }
                    });
                }
                else
                {
                    if (vehicleRefuelButtonText != null)
                    {
                        vehicleRefuelButtonText.text = LocalizationManager.GetFormat("vehicle_btn_emergency_fuel", "ORDER EMERGENCY REFUEL (+{0:F0}L / ${1})", fuelToAdd, fuelCost);
                    }

                    vehicleRefuelButton.onClick.RemoveAllListeners();
                    vehicleRefuelButton.onClick.AddListener(() => {
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
                        if (PlayerEconomyManager.Instance != null && PlayerEconomyManager.Instance.SpendMoney(fuelCost))
                        {
                            v.Refuel(fuelToAdd);
                            DisplayVehicleDetail(v);
                            PopulateVehicleList();
                            if (InteractionPromptHUD.Instance != null)
                            {
                                InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.GetFormat("prompt_emergency_fuel_delivered", "<color=#32FF64>[EMERGENCY FUEL] +{0:F0}L Roadside Fuel Delivered to {1}! (-${2})</color>", fuelToAdd, v.vehicleName, fuelCost));
                            }
                        }
                        else
                        {
                            FlashButtonError(vehicleRefuelButton, new Color(0.12f, 0.65f, 0.35f));
                            if (InteractionPromptHUD.Instance != null)
                            {
                                InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.GetFormat("prompt_insufficient_funds_fuel", "<color=#FF5555>[INSUFFICIENT FUNDS] Need ${0} for emergency fuel delivery!</color>", fuelCost));
                            }
                        }
                    });
                }
            }
        }

        // Warehouse Garage Recall Button
        if (vehicleRecallButton != null)
        {
            if (!v.IsUnlocked)
            {
                vehicleRecallButton.gameObject.SetActive(false);
            }
            else
            {
                vehicleRecallButton.gameObject.SetActive(true);
                vehicleRecallButton.interactable = true;
                int fee = v.GetRecallFee();

                if (vehicleRecallButtonText != null)
                {
                    vehicleRecallButtonText.text = fee > 0 ? LocalizationManager.GetFormat("vehicle_btn_recall_fee", "RECALL TO GARAGE (${0})", fee) : LocalizationManager.Get("vehicle_btn_recall_free", "RECALL TO WAREHOUSE GARAGE");
                }

                vehicleRecallButton.onClick.RemoveAllListeners();
                vehicleRecallButton.onClick.AddListener(() => {
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
                    int currentFee = v.GetRecallFee();
                    int curBalance = PlayerEconomyManager.Instance != null ? PlayerEconomyManager.Instance.CurrentLiveBalance : 0;

                    if (currentFee > 0)
                    {
                        if (PlayerEconomyManager.Instance == null || !PlayerEconomyManager.Instance.SpendMoney(currentFee))
                        {
                            FlashButtonError(vehicleRecallButton, new Color(0.15f, 0.45f, 0.75f));
                            if (InteractionPromptHUD.Instance != null)
                            {
                                InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.GetFormat("prompt_insufficient_funds_recall", "<color=#FF4444>[INSUFFICIENT FUNDS] Need ${0} to recall {1}! (Balance: ${2})</color>", currentFee, v.vehicleName, curBalance));
                            }
                            return;
                        }
                    }

                    v.RecallToGarage();
                    CloseTablet();
                    if (InteractionPromptHUD.Instance != null)
                    {
                        string feeMsg = currentFee > 0 ? $" (-${currentFee})" : "";
                        InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.GetFormat("prompt_garage_recall_success", "<color=#32FFFF>[GARAGE RECALL] {0} recovered to warehouse garage!{1}</color>", v.vehicleName, feeMsg));
                    }
                });
            }
        }
    }

    // ==========================================
    // 3. BRANCH OFFICE UPGRADE TAB
    // ==========================================
    private void PopulateBranchInfo()
    {
        if (BranchManager.Instance == null) return;

        BranchTier current = BranchManager.Instance.CurrentTier;
        BranchTier next = BranchManager.Instance.NextTier;

        if (current != null)
        {
            if (currentBranchTitleText != null) currentBranchTitleText.text = $"<b>{current.tierName}</b> ({LocalizationManager.GetFormat("branch_required_level", "Level {0}", current.tierLevel)})";
            if (currentBranchDescText != null) currentBranchDescText.text = current.description;
            if (currentBranchCapacityText != null) currentBranchCapacityText.text = $"<b>{LocalizationManager.Get("branch_daily_capacity", "Daily Parcel Limit:")}</b> {current.dailyPackageCapacity} {LocalizationManager.Get("unit_packages_per_day", "Packages / Day")}";
            if (currentBranchRentText != null) currentBranchRentText.text = $"<b>{LocalizationManager.Get("branch_daily_rent", "Daily Rent:")}</b> ${current.dailyRent} / {LocalizationManager.Get("unit_day", "Day")}";
        }

        if (next != null)
        {
            if (nextBranchInfoRoot != null) nextBranchInfoRoot.SetActive(true);
            if (nextDescBoxRoot != null) nextDescBoxRoot.SetActive(true);
            if (nextBranchTitleText != null)
            {
                nextBranchTitleText.gameObject.SetActive(true);
                nextBranchTitleText.text = $"<b>{next.tierName}</b> ({LocalizationManager.GetFormat("branch_required_level", "Level {0}", next.tierLevel)})";
            }
            if (nextBranchDescText != null)
            {
                nextBranchDescText.gameObject.SetActive(true);
                nextBranchDescText.text = next.description;
            }
            if (nextBranchCapacityText != null)
            {
                nextBranchCapacityText.gameObject.SetActive(true);
                nextBranchCapacityText.text = $"<b>{LocalizationManager.Get("branch_next_capacity", "New Parcel Limit:")}</b> {current.dailyPackageCapacity} -> <color=#32FF64>{next.dailyPackageCapacity} {LocalizationManager.Get("unit_packages", "Packages")} (+{next.dailyPackageCapacity - current.dailyPackageCapacity})</color>";
            }
            if (nextBranchRentText != null)
            {
                nextBranchRentText.gameObject.SetActive(true);
                nextBranchRentText.text = $"<b>{LocalizationManager.Get("branch_next_rent", "New Daily Rent:")}</b> ${current.dailyRent} -> <color=#FFAA33>${next.dailyRent}</color>";
            }

            if (branchUpgradeButton != null)
            {
                branchUpgradeButton.gameObject.SetActive(true);
                branchUpgradeButton.interactable = true;
                if (branchUpgradeButtonText != null)
                {
                    branchUpgradeButtonText.text = LocalizationManager.GetFormat("branch_btn_upgrade", "UPGRADE BRANCH (${0:N0})", next.upgradeCost);
                }
            }

            if (branchMaxLevelBadge != null)
            {
                branchMaxLevelBadge.gameObject.SetActive(false);
                branchMaxLevelBadge.text = LocalizationManager.Get("branch_badge_max", "MAXIMUM BRANCH LEVEL REACHED");
            }
        }
        else
        {
            // Max level reached
            if (nextDescBoxRoot != null) nextDescBoxRoot.SetActive(false);
            if (nextBranchTitleText != null) nextBranchTitleText.gameObject.SetActive(false);
            if (nextBranchDescText != null) nextBranchDescText.gameObject.SetActive(false);
            if (nextBranchCapacityText != null) nextBranchCapacityText.gameObject.SetActive(false);
            if (nextBranchRentText != null) nextBranchRentText.gameObject.SetActive(false);
            if (branchUpgradeButton != null) branchUpgradeButton.gameObject.SetActive(false);
            if (branchMaxLevelBadge != null)
            {
                branchMaxLevelBadge.gameObject.SetActive(true);
                branchMaxLevelBadge.text = LocalizationManager.Get("branch_badge_max", "MAXIMUM BRANCH LEVEL REACHED");
            }
        }
    }

    private void OnUpgradeBranchClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        if (BranchManager.Instance != null)
        {
            bool ok = BranchManager.Instance.TryUpgradeBranch();
            if (ok)
            {
                PopulateBranchInfo();
                if (InteractionPromptHUD.Instance != null)
                {
                    InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.Get("prompt_branch_active_new", "<color=#32FFFF>[BRANCH SUCCESSFULLY UPGRADED!]</color>"));
                }
            }
            else
            {
                FlashButtonError(branchUpgradeButton, new Color(0.1f, 0.7f, 0.35f));
            }
        }
    }

    // ==========================================
    // RECURSIVE BINDING & HELPERS
    // ==========================================
    public void EnsureTabletStructure()
    {
        if (tabletPanelRoot == null)
        {
            Transform t = transform.Find("CargoTabletPanel");
            if (t == null)
            {
                var canvas = GetComponentInParent<Canvas>();
                if (canvas != null) t = canvas.transform.Find("CargoTabletPanel");
            }
            if (t != null) tabletPanelRoot = t.gameObject;
        }

        if (tabletPanelRoot == null) return;

        // 1. TabletTopBar & Buttons
        Transform topBarTransform = tabletPanelRoot.transform.Find("TabletTopBar");
        if (topBarTransform == null) topBarTransform = tabletPanelRoot.transform.Find("TabBar");

        if (topBarTransform != null)
        {
            if (tabCargoButton == null) tabCargoButton = FindButtonRecursive(topBarTransform, "Tab_Cargo", "TabCargo");
            if (tabVehicleButton == null) tabVehicleButton = FindButtonRecursive(topBarTransform, "Tab_Vehicles", "TabVehicles");
            if (tabBranchButton == null) tabBranchButton = FindButtonRecursive(topBarTransform, "Tab_Branch", "TabBranch");
            if (endShiftButton == null) endShiftButton = FindButtonRecursive(topBarTransform, "EndShiftButton", "EndShift");
            if (closeTabletButton == null) closeTabletButton = FindButtonRecursive(topBarTransform, "CloseButton", "Close");
        }

        if (endShiftButton != null)
        {
            endShiftButton.onClick.RemoveAllListeners();
            endShiftButton.onClick.AddListener(OnEndShiftButtonClicked);
        }

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
            tabBranchButton.gameObject.SetActive(false);
        }

        if (closeTabletButton != null)
        {
            closeTabletButton.onClick.RemoveAllListeners();
            closeTabletButton.onClick.AddListener(CloseTablet);
        }

        RefreshLocalizedUI();

        // 2. Cargo View Root
        Transform cView = FindTransformRecursive(tabletPanelRoot.transform, "CargoViewRoot");
        if (cView != null)
        {
            cargoViewRoot = cView.gameObject;
            Transform cContent = FindTransformRecursive(cView, "Content");
            if (cContent != null) cargoListContent = cContent;

            if (cargoCardTemplate == null && cargoListContent != null)
            {
                Transform ct = cargoListContent.Find("CargoCardTemplate");
                if (ct != null) cargoCardTemplate = ct.gameObject;
            }

            emptyListText = FindTMPRecursive(cView, "EmptyListText", "EmptyText");

            Transform rDetail = FindTransformRecursive(cView, "RightColumn_Detail");
            if (rDetail != null)
            {
                detailCardRoot = rDetail.gameObject;
                recipientNameText = FindTMPRecursive(rDetail, "RecipientNameText", "RecipientName", "Recipient");
                targetAddressText = FindTMPRecursive(rDetail, "TargetAddressText", "TargetAddress", "Address");
                addressDescriptionText = FindTMPRecursive(rDetail, "AddressDescriptionText", "AddressDescription", "DescText", "Description");
            }
        }

        // 3. Vehicle View Root
        Transform vView = FindTransformRecursive(tabletPanelRoot.transform, "VehicleViewRoot");
        if (vView != null)
        {
            vehicleViewRoot = vView.gameObject;
            Transform vContent = FindTransformRecursive(vView, "Content");
            if (vContent != null) vehicleListContent = vContent;

            if (vehicleCardTemplate == null && vehicleListContent != null)
            {
                Transform vt = vehicleListContent.Find("VehicleCardTemplate");
                if (vt != null) vehicleCardTemplate = vt.gameObject;
            }

            Transform vDetail = FindTransformRecursive(vView, "RightColumn_VehicleDetail");
            if (vDetail != null)
            {
                vehicleDetailRoot = vDetail.gameObject;
                vehicleNameText = FindTMPRecursive(vDetail, "VehicleName", "Name");
                vehicleFuelStatusText = FindTMPRecursive(vDetail, "FuelStatusText", "FuelStatus", "FuelText", "StatusText");
                vehicleDescText = FindTMPRecursive(vDetail, "DescText", "Description", "Desc");
                vehicleBuyButton = FindButtonRecursive(vDetail, "BuyButton", "PurchaseButton");
                vehicleBuyButtonText = vehicleBuyButton != null ? vehicleBuyButton.GetComponentInChildren<TextMeshProUGUI>() : null;
                vehicleRefuelButton = FindButtonRecursive(vDetail, "RefuelButton", "EmergencyRefuelButton");
                vehicleRefuelButtonText = vehicleRefuelButton != null ? vehicleRefuelButton.GetComponentInChildren<TextMeshProUGUI>() : null;
                vehicleRecallButton = FindButtonRecursive(vDetail, "RecallButton", "GarageRecallButton");
                vehicleRecallButtonText = vehicleRecallButton != null ? vehicleRecallButton.GetComponentInChildren<TextMeshProUGUI>() : null;
            }
        }

        // 4. Branch View Root
        Transform bView = FindTransformRecursive(tabletPanelRoot.transform, "BranchViewRoot");
        if (bView != null)
        {
            branchViewRoot = bView.gameObject;
            Transform lBranch = FindTransformRecursive(bView, "LeftColumn_CurrentBranch");
            if (lBranch != null)
            {
                currentBranchTitleText = FindTMPRecursive(lBranch, "BranchTitle", "Title");
                currentBranchCapacityText = FindTMPRecursive(lBranch, "CapText", "CapacityText", "Capacity");
                currentBranchRentText = FindTMPRecursive(lBranch, "RentText", "DailyRentText", "Rent");
                currentBranchDescText = FindTMPRecursive(lBranch, "DescText", "Description");
            }

            Transform rBranch = FindTransformRecursive(bView, "RightColumn_NextTier");
            if (rBranch != null)
            {
                nextBranchInfoRoot = rBranch.gameObject;
                Transform nextBox = FindTransformRecursive(rBranch, "NextDescBox");
                if (nextBox != null) nextDescBoxRoot = nextBox.gameObject;
                nextBranchTitleText = FindTMPRecursive(rBranch, "NextTitle", "NextBranchTitle");
                nextBranchCapacityText = FindTMPRecursive(rBranch, "NextCapText", "NextCapacityText");
                nextBranchRentText = FindTMPRecursive(rBranch, "NextRentText", "NextRent");
                nextBranchDescText = FindTMPRecursive(rBranch, "DescText", "Description");
                branchUpgradeButton = FindButtonRecursive(rBranch, "UpgradeButton", "BranchUpgradeButton");
                branchUpgradeButtonText = branchUpgradeButton != null ? branchUpgradeButton.GetComponentInChildren<TextMeshProUGUI>() : null;
                branchMaxLevelBadge = FindTMPRecursive(rBranch, "MaxLevelBadge", "MaxBadge");

                if (branchUpgradeButton != null)
                {
                    branchUpgradeButton.onClick.RemoveAllListeners();
                    branchUpgradeButton.onClick.AddListener(OnUpgradeBranchClicked);
                }
            }
        }

        // Disable legacy cargo drop button if still hanging
        if (dropCargoButton != null)
        {
            dropCargoButton.onClick.RemoveAllListeners();
            dropCargoButton.gameObject.SetActive(false);
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
}
