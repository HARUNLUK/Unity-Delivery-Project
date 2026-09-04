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
    VehicleDealership
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

    [Header("--- TAB SWITCHING BUTTONS ---")]
    public Button tabCargoButton;
    public Button tabVehicleButton;

    private CargoItem currentSelectedCargo;
    private DrivableVehicle currentSelectedVehicle;
    private TabletTab currentTab = TabletTab.CargoInventory;
    private bool isTabletOpen = false;

    public bool IsTabletOpen => isTabletOpen;

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
            dropCargoButton.onClick.AddListener(OnDropCargoClicked);
        }

        EnsureEventSystemAndRaycaster();
    }

    private void Start()
    {
        if (tabletPanelRoot != null) tabletPanelRoot.SetActive(false);
        EnsureEventSystemAndRaycaster();
        EnsureVehicleUIElements();
    }

    private void OnEnable()
    {
        VanInventory.OnInventoryUpdated += RefreshUI;
        DrivableVehicle.OnVehiclePurchased += HandleVehiclePurchased;
        DrivableVehicle.OnVehicleRecalled += HandleVehicleRecalled;
        DrivableVehicle.OnAnyVehicleReset += HandleAnyVehicleReset;
    }

    private void OnDisable()
    {
        VanInventory.OnInventoryUpdated -= RefreshUI;
        DrivableVehicle.OnVehiclePurchased -= HandleVehiclePurchased;
        DrivableVehicle.OnVehicleRecalled -= HandleVehicleRecalled;
        DrivableVehicle.OnAnyVehicleReset -= HandleAnyVehicleReset;
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
        }

        try
        {
            if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.T) || Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.I))
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
        EnsureVehicleUIElements();

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
        EnsureVehicleUIElements();

        if (cargoViewRoot != null) cargoViewRoot.SetActive(currentTab == TabletTab.CargoInventory);
        if (vehicleViewRoot != null) vehicleViewRoot.SetActive(currentTab == TabletTab.VehicleDealership);

        // Update tab buttons appearance
        if (tabCargoButton != null)
        {
            Image img = tabCargoButton.GetComponent<Image>();
            if (img != null) img.color = (currentTab == TabletTab.CargoInventory) ? new Color(0.2f, 0.6f, 0.9f) : new Color(0.12f, 0.15f, 0.2f);
        }
        if (tabVehicleButton != null)
        {
            Image img = tabVehicleButton.GetComponent<Image>();
            if (img != null) img.color = (currentTab == TabletTab.VehicleDealership) ? new Color(0.2f, 0.6f, 0.9f) : new Color(0.12f, 0.15f, 0.2f);
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
        else
        {
            PopulateVehicleList();
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

        if (trackingNumberText != null) trackingNumberText.text = $"Tracking No: {cargo.trackingNumber}";
        if (recipientNameText != null) recipientNameText.text = $"Recipient: {cargo.recipientName}";
        if (targetAddressText != null) targetAddressText.text = $"Address: {cargo.targetAddress}";
        
        if (addressDescriptionText != null)
        {
            addressDescriptionText.text = $"<b>Delivery Clue / Description:</b>\n\n\"{cargo.targetAddressDescription}\"";
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
                cardImg.color = v.IsUnlocked ? new Color(0.12f, 0.22f, 0.16f, 0.95f) : new Color(0.18f, 0.14f, 0.14f, 0.95f);
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
            if (v.IsUnlocked)
            {
                vehicleRecallButton.gameObject.SetActive(true);
                vehicleRecallButton.onClick.RemoveAllListeners();
                vehicleRecallButton.onClick.AddListener(() => {
                    v.RecallToGarage();
                    CloseTablet();
                });
            }
            else
            {
                vehicleRecallButton.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Ensures tabs, vehicle view and dealership UI are dynamically built if missing in hierarchy.
    /// </summary>
    public void EnsureVehicleUIElements()
    {
        if (tabletPanelRoot == null) return;

        // 1. Identify or Create Navigation Tabs Header
        Transform tabHeader = tabletPanelRoot.transform.Find("TabletTabBar");
        if (tabHeader == null)
        {
            GameObject headerObj = new GameObject("TabletTabBar");
            headerObj.transform.SetParent(tabletPanelRoot.transform, false);
            headerObj.transform.SetAsFirstSibling();
            RectTransform hRect = headerObj.AddComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0, 1);
            hRect.anchorMax = new Vector2(1, 1);
            hRect.pivot = new Vector2(0.5f, 1);
            hRect.anchoredPosition = new Vector2(0, 48);
            hRect.sizeDelta = new Vector2(0, 42);

            HorizontalLayoutGroup hLayout = headerObj.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 10;
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = true;

            // Tab 1: Cargo
            GameObject btn1 = new GameObject("Tab_Cargo");
            btn1.transform.SetParent(headerObj.transform, false);
            btn1.AddComponent<Image>().color = new Color(0.2f, 0.6f, 0.9f);
            tabCargoButton = btn1.AddComponent<Button>();
            GameObject t1 = new GameObject("Text");
            t1.transform.SetParent(btn1.transform, false);
            RectTransform tr1 = t1.AddComponent<RectTransform>();
            tr1.anchorMin = Vector2.zero; tr1.anchorMax = Vector2.one; tr1.sizeDelta = Vector2.zero;
            TextMeshProUGUI tmp1 = t1.AddComponent<TextMeshProUGUI>();
            tmp1.text = "📦 KARGO LİSTESİ";
            tmp1.fontSize = 18;
            tmp1.fontStyle = FontStyles.Bold;
            tmp1.alignment = TextAlignmentOptions.Center;
            tmp1.color = Color.white;
            tmp1.raycastTarget = false;

            // Tab 2: Vehicle Dealership & Garage
            GameObject btn2 = new GameObject("Tab_Vehicles");
            btn2.transform.SetParent(headerObj.transform, false);
            btn2.AddComponent<Image>().color = new Color(0.12f, 0.15f, 0.2f);
            tabVehicleButton = btn2.AddComponent<Button>();
            GameObject t2 = new GameObject("Text");
            t2.transform.SetParent(btn2.transform, false);
            RectTransform tr2 = t2.AddComponent<RectTransform>();
            tr2.anchorMin = Vector2.zero; tr2.anchorMax = Vector2.one; tr2.sizeDelta = Vector2.zero;
            TextMeshProUGUI tmp2 = t2.AddComponent<TextMeshProUGUI>();
            tmp2.text = "🚚 ARAÇ GALERİSİ & GARAJ";
            tmp2.fontSize = 18;
            tmp2.fontStyle = FontStyles.Bold;
            tmp2.alignment = TextAlignmentOptions.Center;
            tmp2.color = Color.white;
            tmp2.raycastTarget = false;

            tabCargoButton.onClick.AddListener(() => SwitchTab(TabletTab.CargoInventory));
            tabVehicleButton.onClick.AddListener(() => SwitchTab(TabletTab.VehicleDealership));
        }

        // 2. Identify Cargo View
        if (cargoViewRoot == null)
        {
            Transform leftCol = tabletPanelRoot.transform.Find("LeftColumn_List");
            Transform rightCol = tabletPanelRoot.transform.Find("RightColumn_Detail");
            if (leftCol != null && rightCol != null)
            {
                // Both columns form the cargo inventory view
                cargoViewRoot = leftCol.gameObject;
            }
        }

        // 3. Build Vehicle Dealership View if missing
        if (vehicleViewRoot == null)
        {
            Transform existingVeh = tabletPanelRoot.transform.Find("VehicleDealershipView");
            if (existingVeh != null)
            {
                vehicleViewRoot = existingVeh.gameObject;
            }
            else
            {
                vehicleViewRoot = new GameObject("VehicleDealershipView");
                vehicleViewRoot.transform.SetParent(tabletPanelRoot.transform, false);

                RectTransform vRect = vehicleViewRoot.AddComponent<RectTransform>();
                vRect.anchorMin = Vector2.zero;
                vRect.anchorMax = Vector2.one;
                vRect.sizeDelta = Vector2.zero;

                HorizontalLayoutGroup vLayout = vehicleViewRoot.AddComponent<HorizontalLayoutGroup>();
                vLayout.padding = new RectOffset(20, 20, 20, 20);
                vLayout.spacing = 20;
                vLayout.childControlWidth = true;
                vLayout.childControlHeight = true;

                // Left Column: Vehicle List
                GameObject leftVehCol = new GameObject("LeftColumn_Vehicles");
                leftVehCol.transform.SetParent(vehicleViewRoot.transform, false);
                LayoutElement leftElem = leftVehCol.AddComponent<LayoutElement>();
                leftElem.preferredWidth = 420;
                leftElem.flexibleWidth = 0;

                VerticalLayoutGroup leftL = leftVehCol.AddComponent<VerticalLayoutGroup>();
                leftL.spacing = 10;
                leftL.childControlWidth = true;
                leftL.childControlHeight = false;

                GameObject titleObj = new GameObject("Title");
                titleObj.transform.SetParent(leftVehCol.transform, false);
                titleObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 40);
                TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
                titleText.text = "ARAÇ KATALOĞU";
                titleText.fontSize = 24;
                titleText.fontStyle = FontStyles.Bold;
                titleText.alignment = TextAlignmentOptions.Center;
                titleText.color = new Color(1f, 0.8f, 0.2f);

                // Scroll View
                GameObject scrollObj = new GameObject("VehicleScrollView");
                scrollObj.transform.SetParent(leftVehCol.transform, false);
                scrollObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 580);
                scrollObj.AddComponent<Image>().color = new Color(0.04f, 0.06f, 0.08f, 0.85f);
                ScrollRect sRect = scrollObj.AddComponent<ScrollRect>();
                sRect.horizontal = false;
                sRect.vertical = true;
                sRect.scrollSensitivity = 35f;

                GameObject viewportObj = new GameObject("Viewport");
                viewportObj.transform.SetParent(scrollObj.transform, false);
                RectTransform viewRect = viewportObj.AddComponent<RectTransform>();
                viewRect.anchorMin = Vector2.zero; viewRect.anchorMax = Vector2.one; viewRect.sizeDelta = Vector2.zero;
                viewportObj.AddComponent<Image>().color = Color.white;
                Mask mask = viewportObj.AddComponent<Mask>();
                mask.showMaskGraphic = false;

                GameObject contentObj = new GameObject("Content");
                contentObj.transform.SetParent(viewportObj.transform, false);
                RectTransform contentRect = contentObj.AddComponent<RectTransform>();
                contentRect.anchorMin = new Vector2(0, 1);
                contentRect.anchorMax = new Vector2(1, 1);
                contentRect.pivot = new Vector2(0.5f, 1);
                contentRect.sizeDelta = Vector2.zero;

                VerticalLayoutGroup cLayout = contentObj.AddComponent<VerticalLayoutGroup>();
                cLayout.padding = new RectOffset(8, 8, 8, 8);
                cLayout.spacing = 8;
                cLayout.childControlWidth = true;
                cLayout.childControlHeight = false;

                ContentSizeFitter fitter = contentObj.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                sRect.viewport = viewRect;
                sRect.content = contentRect;
                vehicleListContent = contentRect;

                // Vehicle Card Template
                GameObject cardTemplate = new GameObject("VehicleCardTemplate");
                cardTemplate.transform.SetParent(contentObj.transform, false);
                cardTemplate.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 85);
                cardTemplate.AddComponent<Image>().color = new Color(0.16f, 0.2f, 0.26f, 1f);
                cardTemplate.AddComponent<Button>();

                GameObject cardTextObj = new GameObject("Text");
                cardTextObj.transform.SetParent(cardTemplate.transform, false);
                RectTransform ctRect = cardTextObj.AddComponent<RectTransform>();
                ctRect.anchorMin = Vector2.zero; ctRect.anchorMax = Vector2.one; ctRect.sizeDelta = Vector2.zero;
                TextMeshProUGUI cText = cardTextObj.AddComponent<TextMeshProUGUI>();
                cText.text = "<b>Heavy Cargo Van</b>\n$2,500 TL (Lvl 2)";
                cText.fontSize = 18;
                cText.alignment = TextAlignmentOptions.MidlineLeft;
                cText.color = Color.white;
                cText.margin = new Vector4(12, 0, 12, 0);
                cText.raycastTarget = false;

                vehicleCardTemplate = cardTemplate;
                cardTemplate.SetActive(false);

                // Right Column: Details & Actions
                GameObject rightVehCol = new GameObject("RightColumn_VehicleDetail");
                rightVehCol.transform.SetParent(vehicleViewRoot.transform, false);
                LayoutElement rightElem = rightVehCol.AddComponent<LayoutElement>();
                rightElem.flexibleWidth = 1;

                rightVehCol.AddComponent<Image>().color = new Color(0.12f, 0.15f, 0.2f, 0.95f);
                VerticalLayoutGroup rLayout = rightVehCol.AddComponent<VerticalLayoutGroup>();
                rLayout.padding = new RectOffset(24, 24, 24, 24);
                rLayout.spacing = 16;
                rLayout.childControlWidth = true;
                rLayout.childControlHeight = false;

                vehicleDetailRoot = rightVehCol;

                // Name
                GameObject nObj = new GameObject("VehicleName");
                nObj.transform.SetParent(rightVehCol.transform, false);
                nObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 35);
                vehicleNameText = nObj.AddComponent<TextMeshProUGUI>();
                vehicleNameText.text = "Heavy Cargo Van";
                vehicleNameText.fontSize = 26;
                vehicleNameText.fontStyle = FontStyles.Bold;
                vehicleNameText.color = new Color(1f, 0.85f, 0.2f);

                // Stats
                GameObject capObj = new GameObject("CapacityText");
                capObj.transform.SetParent(rightVehCol.transform, false);
                capObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 26);
                vehicleCapacityText = capObj.AddComponent<TextMeshProUGUI>();
                vehicleCapacityText.fontSize = 20;

                GameObject reqObj = new GameObject("LevelReqText");
                reqObj.transform.SetParent(rightVehCol.transform, false);
                reqObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 26);
                vehicleLevelReqText = reqObj.AddComponent<TextMeshProUGUI>();
                vehicleLevelReqText.fontSize = 20;

                GameObject prObj = new GameObject("PriceText");
                prObj.transform.SetParent(rightVehCol.transform, false);
                prObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 26);
                vehiclePriceText = prObj.AddComponent<TextMeshProUGUI>();
                vehiclePriceText.fontSize = 20;

                // Description Box
                GameObject dBox = new GameObject("DescBox");
                dBox.transform.SetParent(rightVehCol.transform, false);
                dBox.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 150);
                dBox.AddComponent<Image>().color = new Color(0.05f, 0.07f, 0.1f, 0.98f);
                VerticalLayoutGroup dbLayout = dBox.AddComponent<VerticalLayoutGroup>();
                dbLayout.padding = new RectOffset(14, 14, 14, 14);

                GameObject dtObj = new GameObject("DescText");
                dtObj.transform.SetParent(dBox.transform, false);
                vehicleDescText = dtObj.AddComponent<TextMeshProUGUI>();
                vehicleDescText.fontSize = 19;
                vehicleDescText.color = new Color(0.9f, 0.9f, 0.95f);

                // Status Banner
                GameObject stObj = new GameObject("StatusText");
                stObj.transform.SetParent(rightVehCol.transform, false);
                stObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 30);
                vehicleStatusText = stObj.AddComponent<TextMeshProUGUI>();
                vehicleStatusText.fontSize = 20;
                vehicleStatusText.fontStyle = FontStyles.Bold;

                // Buy Button
                GameObject bBtnObj = new GameObject("BuyButton");
                bBtnObj.transform.SetParent(rightVehCol.transform, false);
                bBtnObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 60);
                bBtnObj.AddComponent<Image>().color = new Color(0.1f, 0.7f, 0.35f);
                vehicleBuyButton = bBtnObj.AddComponent<Button>();

                GameObject bbtObj = new GameObject("Text");
                bbtObj.transform.SetParent(bBtnObj.transform, false);
                RectTransform bbtRect = bbtObj.AddComponent<RectTransform>();
                bbtRect.anchorMin = Vector2.zero; bbtRect.anchorMax = Vector2.one; bbtRect.sizeDelta = Vector2.zero;
                vehicleBuyButtonText = bbtObj.AddComponent<TextMeshProUGUI>();
                vehicleBuyButtonText.text = "🛒 ARACI SATIN AL ($2,500 TL)";
                vehicleBuyButtonText.fontSize = 22;
                vehicleBuyButtonText.fontStyle = FontStyles.Bold;
                vehicleBuyButtonText.alignment = TextAlignmentOptions.Center;
                vehicleBuyButtonText.color = Color.white;
                vehicleBuyButtonText.raycastTarget = false;

                // Recall Button
                GameObject rBtnObj = new GameObject("RecallButton");
                rBtnObj.transform.SetParent(rightVehCol.transform, false);
                rBtnObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 55);
                rBtnObj.AddComponent<Image>().color = new Color(0.15f, 0.5f, 0.85f);
                vehicleRecallButton = rBtnObj.AddComponent<Button>();

                GameObject rbtObj = new GameObject("Text");
                rbtObj.transform.SetParent(rBtnObj.transform, false);
                RectTransform rbtRect = rbtObj.AddComponent<RectTransform>();
                rbtRect.anchorMin = Vector2.zero; rbtRect.anchorMax = Vector2.one; rbtRect.sizeDelta = Vector2.zero;
                vehicleRecallButtonText = rbtObj.AddComponent<TextMeshProUGUI>();
                vehicleRecallButtonText.text = "📍 ŞUBEYE ÇAĞIR / GARAJA ÇEK (SPAWN)";
                vehicleRecallButtonText.fontSize = 20;
                vehicleRecallButtonText.fontStyle = FontStyles.Bold;
                vehicleRecallButtonText.alignment = TextAlignmentOptions.Center;
                vehicleRecallButtonText.color = Color.white;
                vehicleRecallButtonText.raycastTarget = false;

                vehicleViewRoot.SetActive(false);
            }
        }
    }

    /// <summary>
    /// EventSystem ve GraphicRaycaster bileşenlerinin fare tıklamalarını yakalayabilmesini garantiye alır.
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
