#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class DeliveryUIBuilder
{
    [MenuItem("Tools/Delivery Game/Build Complete Delivery UI", false, 20)]
    public static void BuildCleanDeliveryUI()
    {
        // 0. Find/Create DeliveryManager with all required managers
        GameObject deliveryManager = GameObject.Find("DeliveryManager");
        if (deliveryManager == null)
        {
            deliveryManager = new GameObject("DeliveryManager");
        }

        if (deliveryManager.GetComponent<VanInventory>() == null)
        {
            deliveryManager.AddComponent<VanInventory>();
        }

        if (deliveryManager.GetComponent<DayTimeManager>() == null)
        {
            deliveryManager.AddComponent<DayTimeManager>();
        }

        if (deliveryManager.GetComponent<PlayerEconomyManager>() == null)
        {
            deliveryManager.AddComponent<PlayerEconomyManager>();
        }

        if (deliveryManager.GetComponent<BranchManager>() == null && Object.FindAnyObjectByType<BranchManager>() == null)
        {
            deliveryManager.AddComponent<BranchManager>();
        }

        // 1. Find or Create Canvas
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Delivery_Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        else
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
            }
        }

        UnityEngine.EventSystems.EventSystem es = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (es == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            es = esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        }

#if ENABLE_INPUT_SYSTEM
        if (es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
        {
            UnityEngine.EventSystems.StandaloneInputModule oldMod = es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (oldMod != null) Object.DestroyImmediate(oldMod);

            es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
#else
        if (es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>() == null)
        {
            es.gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
#endif

        // Clean old UI roots
        Transform oldTablet = canvas.transform.Find("CargoTabletPanel");
        if (oldTablet != null) Object.DestroyImmediate(oldTablet.gameObject);

        Transform oldSummary = canvas.transform.Find("DaySummaryPanel");
        if (oldSummary != null) Object.DestroyImmediate(oldSummary.gameObject);

        Transform oldDelivery = canvas.transform.Find("DeliveryPanel");
        if (oldDelivery != null) Object.DestroyImmediate(oldDelivery.gameObject);

        Transform oldHud = canvas.transform.Find("DeliveryHUD");
        if (oldHud != null) Object.DestroyImmediate(oldHud.gameObject);

        // ==========================================
        // 0. MAIN TOP STATUS BAR (PERMANENT HUD)
        // ==========================================
        GameObject hudObj = new GameObject("DeliveryHUD");
        hudObj.transform.SetParent(canvas.transform, false);
        RectTransform hudRect = hudObj.AddComponent<RectTransform>();
        hudRect.anchorMin = Vector2.zero;
        hudRect.anchorMax = Vector2.one;
        hudRect.sizeDelta = Vector2.zero;

        // Top Status Bar (1300x85)
        GameObject topBarObj = new GameObject("TopStatusBar");
        topBarObj.transform.SetParent(hudObj.transform, false);
        RectTransform topBarRect = topBarObj.AddComponent<RectTransform>();
        topBarRect.anchorMin = new Vector2(0.5f, 1f);
        topBarRect.anchorMax = new Vector2(0.5f, 1f);
        topBarRect.pivot = new Vector2(0.5f, 1f);
        topBarRect.anchoredPosition = new Vector2(0, -20);
        topBarRect.sizeDelta = new Vector2(1300, 80);

        Image topBarBg = topBarObj.AddComponent<Image>();
        topBarBg.color = new Color(0.06f, 0.08f, 0.12f, 0.95f);

        HorizontalLayoutGroup topBarLayout = topBarObj.AddComponent<HorizontalLayoutGroup>();
        topBarLayout.padding = new RectOffset(15, 15, 10, 10);
        topBarLayout.spacing = 15;
        topBarLayout.childControlWidth = true;
        topBarLayout.childControlHeight = true;
        topBarLayout.childForceExpandWidth = true;
        topBarLayout.childForceExpandHeight = true;

        // 1. Clock Card
        GameObject clockCard = new GameObject("ClockCard");
        clockCard.transform.SetParent(topBarObj.transform, false);
        clockCard.AddComponent<Image>().color = new Color(0.12f, 0.16f, 0.24f, 0.95f);

        GameObject clockTextObj = new GameObject("Text");
        clockTextObj.transform.SetParent(clockCard.transform, false);
        RectTransform clockTextRect = clockTextObj.AddComponent<RectTransform>();
        clockTextRect.anchorMin = Vector2.zero;
        clockTextRect.anchorMax = Vector2.one;
        clockTextRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI clockText = AddTextMeshPro(clockTextObj, "TIME: 09:00", 28, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.2f));

        // 2. Remaining Cargo Card
        GameObject cargoCard = new GameObject("CargoCard");
        cargoCard.transform.SetParent(topBarObj.transform, false);
        cargoCard.AddComponent<Image>().color = new Color(0.12f, 0.16f, 0.24f, 0.95f);

        GameObject cargoTextObj = new GameObject("Text");
        cargoTextObj.transform.SetParent(cargoCard.transform, false);
        RectTransform cargoTextRect = cargoTextObj.AddComponent<RectTransform>();
        cargoTextRect.anchorMin = Vector2.zero;
        cargoTextRect.anchorMax = Vector2.one;
        cargoTextRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI remainingText = AddTextMeshPro(cargoTextObj, "REMAINING: 10 / 10", 28, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

        // 3. Balance Card
        GameObject balanceCard = new GameObject("BalanceCard");
        balanceCard.transform.SetParent(topBarObj.transform, false);
        balanceCard.AddComponent<Image>().color = new Color(0.12f, 0.16f, 0.24f, 0.95f);

        GameObject balanceTextObj = new GameObject("Text");
        balanceTextObj.transform.SetParent(balanceCard.transform, false);
        RectTransform balanceTextRect = balanceTextObj.AddComponent<RectTransform>();
        balanceTextRect.anchorMin = Vector2.zero;
        balanceTextRect.anchorMax = Vector2.one;
        balanceTextRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI balanceText = AddTextMeshPro(balanceTextObj, "BALANCE: 0 $ (+0 $)", 26, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.2f, 1f, 0.4f));

        // MainHUDController
        MainHUDController hudController = canvas.GetComponent<MainHUDController>();
        if (hudController == null) hudController = canvas.gameObject.AddComponent<MainHUDController>();
        hudController.clockText = clockText;
        hudController.remainingCargoText = remainingText;
        hudController.balanceEarningsText = balanceText;

        // Notification Banner
        GameObject notifObj = new GameObject("NotificationBanner");
        notifObj.transform.SetParent(hudObj.transform, false);
        RectTransform notifRect = notifObj.AddComponent<RectTransform>();
        notifRect.anchorMin = new Vector2(0.5f, 1f);
        notifRect.anchorMax = new Vector2(0.5f, 1f);
        notifRect.pivot = new Vector2(0.5f, 1f);
        notifRect.anchoredPosition = new Vector2(0, -110);
        notifRect.sizeDelta = new Vector2(900, 65);

        Image notifImg = notifObj.AddComponent<Image>();
        notifImg.color = new Color(0.85f, 0.15f, 0.15f, 0.98f);

        GameObject notifTextObj = new GameObject("Text");
        notifTextObj.transform.SetParent(notifObj.transform, false);
        RectTransform notifTextRect = notifTextObj.AddComponent<RectTransform>();
        notifTextRect.anchorMin = Vector2.zero;
        notifTextRect.anchorMax = Vector2.one;
        notifTextRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI notifText = AddTextMeshPro(notifTextObj, "[-] SOKAĞA DÜŞTÜ! (-100 $ CEZA)", 22, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

        DeliveryNotificationHUD hudScript = canvas.GetComponent<DeliveryNotificationHUD>();
        if (hudScript == null) hudScript = canvas.gameObject.AddComponent<DeliveryNotificationHUD>();

        hudScript.notificationRoot = notifObj;
        hudScript.notificationText = notifText;
        hudScript.notificationBackground = notifImg;
        hudScript.remainingCargoCounterText = remainingText;
        notifObj.SetActive(false);

        // ==========================================
        // 1. CARGO TABLET (TAB KEY)
        // ==========================================
        GameObject tabObj = new GameObject("CargoTabletPanel");
        tabObj.transform.SetParent(canvas.transform, false);
        RectTransform tabRect = tabObj.AddComponent<RectTransform>();
        tabRect.anchorMin = new Vector2(0.5f, 0.5f);
        tabRect.anchorMax = new Vector2(0.5f, 0.5f);
        tabRect.pivot = new Vector2(0.5f, 0.5f);
        tabRect.sizeDelta = new Vector2(1240, 780);

        Image tabImg = tabObj.AddComponent<Image>();
        tabImg.color = new Color(0.07f, 0.09f, 0.13f, 0.98f);

        // ------------------------------------------
        // TOP NAVIGATION HEADER BAR (Height: 54)
        // ------------------------------------------
        GameObject topBarHeaderObj = new GameObject("TabletTopBar");
        topBarHeaderObj.transform.SetParent(tabObj.transform, false);
        RectTransform tbHeaderRect = topBarHeaderObj.AddComponent<RectTransform>();
        tbHeaderRect.anchorMin = new Vector2(0, 1);
        tbHeaderRect.anchorMax = new Vector2(1, 1);
        tbHeaderRect.pivot = new Vector2(0.5f, 1);
        tbHeaderRect.anchoredPosition = new Vector2(0, 0);
        tbHeaderRect.sizeDelta = new Vector2(0, 54);

        Image tbHeaderImg = topBarHeaderObj.AddComponent<Image>();
        tbHeaderImg.color = new Color(0.11f, 0.15f, 0.22f, 1f);

        // Title on the left of TopBar
        GameObject titleBadgeObj = new GameObject("BrandTitle");
        titleBadgeObj.transform.SetParent(topBarHeaderObj.transform, false);
        RectTransform tbTitleRect = titleBadgeObj.AddComponent<RectTransform>();
        tbTitleRect.anchorMin = new Vector2(0, 0);
        tbTitleRect.anchorMax = new Vector2(0, 1);
        tbTitleRect.pivot = new Vector2(0, 0.5f);
        tbTitleRect.anchoredPosition = new Vector2(16, 0);
        tbTitleRect.sizeDelta = new Vector2(185, 0);
        TextMeshProUGUI titleTmp = AddTextMeshPro(titleBadgeObj, "🚚 <b>LOJİSTİK</b>", 18, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color(0.3f, 0.85f, 1f));
        if (titleTmp != null) titleTmp.enableWordWrapping = false;

        // Tab Buttons Container in the center
        GameObject tabButtonsContainer = new GameObject("TabBar");
        tabButtonsContainer.transform.SetParent(topBarHeaderObj.transform, false);
        RectTransform tbcRect = tabButtonsContainer.AddComponent<RectTransform>();
        tbcRect.anchorMin = new Vector2(0, 0);
        tbcRect.anchorMax = new Vector2(1, 1);
        tbcRect.offsetMin = new Vector2(210, 8);
        tbcRect.offsetMax = new Vector2(-268, -8);

        HorizontalLayoutGroup tbhLayout = tabButtonsContainer.AddComponent<HorizontalLayoutGroup>();
        tbhLayout.spacing = 6;
        tbhLayout.childControlWidth = true;
        tbhLayout.childControlHeight = true;
        tbhLayout.childForceExpandWidth = true;
        tbhLayout.childForceExpandHeight = true;

        // Tab 1: Cargo Button
        GameObject tab1BtnObj = new GameObject("Tab_Cargo");
        tab1BtnObj.transform.SetParent(tabButtonsContainer.transform, false);
        Image t1Img = tab1BtnObj.AddComponent<Image>();
        t1Img.color = new Color(0.12f, 0.53f, 0.9f);
        Button tabCargoBtn = tab1BtnObj.AddComponent<Button>();

        GameObject t1TextObj = new GameObject("Text");
        t1TextObj.transform.SetParent(tab1BtnObj.transform, false);
        RectTransform t1Tr = t1TextObj.AddComponent<RectTransform>();
        t1Tr.anchorMin = Vector2.zero; t1Tr.anchorMax = Vector2.one; t1Tr.sizeDelta = Vector2.zero;
        TextMeshProUGUI t1Tmp = AddTextMeshPro(t1TextObj, "📦 KARGOLAR", 14, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        t1Tmp.raycastTarget = false;
        t1Tmp.enableWordWrapping = false;

        // Tab 2: Vehicles Button
        GameObject tab2BtnObj = new GameObject("Tab_Vehicles");
        tab2BtnObj.transform.SetParent(tabButtonsContainer.transform, false);
        Image t2Img = tab2BtnObj.AddComponent<Image>();
        t2Img.color = new Color(0.09f, 0.13f, 0.19f);
        Button tabVehicleBtn = tab2BtnObj.AddComponent<Button>();

        GameObject t2TextObj = new GameObject("Text");
        t2TextObj.transform.SetParent(tab2BtnObj.transform, false);
        RectTransform t2Tr = t2TextObj.AddComponent<RectTransform>();
        t2Tr.anchorMin = Vector2.zero; t2Tr.anchorMax = Vector2.one; t2Tr.sizeDelta = Vector2.zero;
        TextMeshProUGUI t2Tmp = AddTextMeshPro(t2TextObj, "🚚 ARAÇLAR", 14, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        t2Tmp.raycastTarget = false;
        t2Tmp.enableWordWrapping = false;

        // Tab 3: Branch Button
        GameObject tab3BtnObj = new GameObject("Tab_Branch");
        tab3BtnObj.transform.SetParent(tabButtonsContainer.transform, false);
        Image t3Img = tab3BtnObj.AddComponent<Image>();
        t3Img.color = new Color(0.09f, 0.13f, 0.19f);
        Button tabBranchBtn = tab3BtnObj.AddComponent<Button>();

        GameObject t3TextObj = new GameObject("Text");
        t3TextObj.transform.SetParent(tab3BtnObj.transform, false);
        RectTransform t3Tr = t3TextObj.AddComponent<RectTransform>();
        t3Tr.anchorMin = Vector2.zero; t3Tr.anchorMax = Vector2.one; t3Tr.sizeDelta = Vector2.zero;
        TextMeshProUGUI t3Tmp = AddTextMeshPro(t3TextObj, "🏢 ŞUBE", 14, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        t3Tmp.raycastTarget = false;
        t3Tmp.enableWordWrapping = false;

        // End Shift Button on top-right next to Close Button
        GameObject endShiftBtnObj = new GameObject("EndShiftButton");
        endShiftBtnObj.transform.SetParent(topBarHeaderObj.transform, false);
        RectTransform endShiftRect = endShiftBtnObj.AddComponent<RectTransform>();
        endShiftRect.anchorMin = new Vector2(1, 0.5f);
        endShiftRect.anchorMax = new Vector2(1, 0.5f);
        endShiftRect.pivot = new Vector2(1, 0.5f);
        endShiftRect.anchoredPosition = new Vector2(-115, 0);
        endShiftRect.sizeDelta = new Vector2(145, 36);

        Image endShiftImg = endShiftBtnObj.AddComponent<Image>();
        endShiftImg.color = new Color(0.85f, 0.35f, 0.1f);
        Button endShiftBtn = endShiftBtnObj.AddComponent<Button>();

        GameObject endShiftTextObj = new GameObject("Text");
        endShiftTextObj.transform.SetParent(endShiftBtnObj.transform, false);
        RectTransform esTr = endShiftTextObj.AddComponent<RectTransform>();
        esTr.anchorMin = Vector2.zero; esTr.anchorMax = Vector2.one; esTr.sizeDelta = Vector2.zero;
        TextMeshProUGUI esTmp = AddTextMeshPro(endShiftTextObj, "⏰ GÜNÜ BİTİR", 14, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        esTmp.raycastTarget = false;
        esTmp.enableWordWrapping = false;

        // Close Button on top-right
        GameObject closeBtnObj = new GameObject("CloseButton");
        closeBtnObj.transform.SetParent(topBarHeaderObj.transform, false);
        RectTransform closeRect = closeBtnObj.AddComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1, 0.5f);
        closeRect.anchorMax = new Vector2(1, 0.5f);
        closeRect.pivot = new Vector2(1, 0.5f);
        closeRect.anchoredPosition = new Vector2(-12, 0);
        closeRect.sizeDelta = new Vector2(95, 36);

        Image closeImg = closeBtnObj.AddComponent<Image>();
        closeImg.color = new Color(0.7f, 0.15f, 0.15f);
        Button closeBtn = closeBtnObj.AddComponent<Button>();

        GameObject closeTextObj = new GameObject("Text");
        closeTextObj.transform.SetParent(closeBtnObj.transform, false);
        RectTransform clTr = closeTextObj.AddComponent<RectTransform>();
        clTr.anchorMin = Vector2.zero; clTr.anchorMax = Vector2.one; clTr.sizeDelta = Vector2.zero;
        TextMeshProUGUI clTmp = AddTextMeshPro(closeTextObj, "✖ KAPAT", 14, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        clTmp.raycastTarget = false;
        clTmp.enableWordWrapping = false;

        // ------------------------------------------
        // MAIN CONTENT AREA (Fills space under top bar)
        // ------------------------------------------
        GameObject contentAreaObj = new GameObject("TabletContentArea");
        contentAreaObj.transform.SetParent(tabObj.transform, false);
        RectTransform contentAreaRect = contentAreaObj.AddComponent<RectTransform>();
        contentAreaRect.anchorMin = new Vector2(0, 0);
        contentAreaRect.anchorMax = new Vector2(1, 1);
        contentAreaRect.pivot = new Vector2(0.5f, 0.5f);
        contentAreaRect.offsetMin = new Vector2(16, 16);
        contentAreaRect.offsetMax = new Vector2(-16, -64);

        // ==========================================
        // SUBVIEW 1: CARGO INVENTORY VIEW
        // ==========================================
        GameObject cargoView = new GameObject("CargoViewRoot");
        cargoView.transform.SetParent(contentAreaObj.transform, false);
        RectTransform cViewRect = cargoView.AddComponent<RectTransform>();
        cViewRect.anchorMin = Vector2.zero;
        cViewRect.anchorMax = Vector2.one;
        cViewRect.offsetMin = Vector2.zero;
        cViewRect.offsetMax = Vector2.zero;

        HorizontalLayoutGroup cvLayout = cargoView.AddComponent<HorizontalLayoutGroup>();
        cvLayout.spacing = 16;
        cvLayout.childControlWidth = true;
        cvLayout.childControlHeight = true;
        cvLayout.childForceExpandWidth = false;
        cvLayout.childForceExpandHeight = true;

        // Left Column: Cargo List (Width: 420)
        GameObject leftCargoCol = new GameObject("LeftColumn_List");
        leftCargoCol.transform.SetParent(cargoView.transform, false);
        LayoutElement lCargoElem = leftCargoCol.AddComponent<LayoutElement>();
        lCargoElem.preferredWidth = 430;
        lCargoElem.flexibleWidth = 0;

        leftCargoCol.AddComponent<Image>().color = new Color(0.1f, 0.13f, 0.19f, 0.95f);
        VerticalLayoutGroup lCargoLayout = leftCargoCol.AddComponent<VerticalLayoutGroup>();
        lCargoLayout.padding = new RectOffset(14, 14, 14, 14);
        lCargoLayout.spacing = 10;
        lCargoLayout.childControlWidth = true;
        lCargoLayout.childControlHeight = false;

        GameObject listTitleObj = new GameObject("ListTitle");
        listTitleObj.transform.SetParent(leftCargoCol.transform, false);
        listTitleObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 36);
        AddTextMeshPro(listTitleObj, "📦 ARAÇTAKİ PAKETLER", 22, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.2f));

        GameObject scrollObj = new GameObject("CargoScrollView");
        scrollObj.transform.SetParent(leftCargoCol.transform, false);
        scrollObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 590);
        scrollObj.AddComponent<Image>().color = new Color(0.05f, 0.07f, 0.1f, 0.85f);
        ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 35f;

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
        contentRect.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup contentLayout = contentObj.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(6, 6, 6, 6);
        contentLayout.spacing = 6;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = false;

        ContentSizeFitter fitter = contentObj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewRect;
        scrollRect.content = contentRect;

        // Card Template
        GameObject cardTemplate = new GameObject("CargoCardTemplate");
        cardTemplate.transform.SetParent(contentObj.transform, false);
        cardTemplate.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 84);
        cardTemplate.AddComponent<Image>().color = new Color(0.16f, 0.22f, 0.3f, 1f);

        Button cardBtn = cardTemplate.AddComponent<Button>();
        GameObject cardTextObj = new GameObject("Text");
        cardTextObj.transform.SetParent(cardTemplate.transform, false);
        RectTransform cardTextRect = cardTextObj.AddComponent<RectTransform>();
        cardTextRect.anchorMin = Vector2.zero; cardTextRect.anchorMax = Vector2.one; cardTextRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI cardText = AddTextMeshPro(cardTextObj, "<b>PKG-1</b> - John Doe\n📍 104 Akçaağaç Sokak\n<color=#64B5F6>⏳ DAĞITIMDA</color>", 16, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, Color.white);
        if (cardText != null) cardText.margin = new Vector4(12, 0, 12, 0);

        // Empty list placeholder text
        GameObject emptyObj = new GameObject("EmptyListText");
        emptyObj.transform.SetParent(leftCargoCol.transform, false);
        emptyObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 60);
        TextMeshProUGUI emptyTmp = AddTextMeshPro(emptyObj, "📦 Araçta teslim edilecek paket bulunmuyor.\nDepodan yeni paket yükleyin.", 16, FontStyles.Italic, TextAlignmentOptions.Center, new Color(0.7f, 0.7f, 0.75f));
        emptyObj.SetActive(false);

        // Right Column: Detail & Drop (Flexible)
        GameObject rightCargoCol = new GameObject("RightColumn_Detail");
        rightCargoCol.transform.SetParent(cargoView.transform, false);
        LayoutElement rightCargoElem = rightCargoCol.AddComponent<LayoutElement>();
        rightCargoElem.flexibleWidth = 1;

        rightCargoCol.AddComponent<Image>().color = new Color(0.12f, 0.16f, 0.23f, 0.95f);
        VerticalLayoutGroup rightColLayout = rightCargoCol.AddComponent<VerticalLayoutGroup>();
        rightColLayout.padding = new RectOffset(22, 22, 20, 20);
        rightColLayout.spacing = 13;
        rightColLayout.childControlWidth = true;
        rightColLayout.childControlHeight = false;

        GameObject detailHeader = new GameObject("Header");
        detailHeader.transform.SetParent(rightCargoCol.transform, false);
        detailHeader.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 32);
        AddTextMeshPro(detailHeader, "📋 TESLİMAT AYRINTILARI & İPUCU", 22, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.3f, 0.85f, 1f));

        GameObject trackingObj = new GameObject("TrackingNumberText");
        trackingObj.transform.SetParent(rightCargoCol.transform, false);
        trackingObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 26);
        TextMeshProUGUI trackingText = AddTextMeshPro(trackingObj, "Takip No: #CRG-1001", 20, FontStyles.Bold, TextAlignmentOptions.Left, new Color(1f, 0.85f, 0.2f));

        GameObject recipientObj = new GameObject("RecipientNameText");
        recipientObj.transform.SetParent(rightCargoCol.transform, false);
        recipientObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 24);
        TextMeshProUGUI recipientText = AddTextMeshPro(recipientObj, "Alıcı: John Smith", 19, FontStyles.Normal, TextAlignmentOptions.Left, Color.white);

        GameObject addressObj = new GameObject("TargetAddressText");
        addressObj.transform.SetParent(rightCargoCol.transform, false);
        addressObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 24);
        TextMeshProUGUI addressText = AddTextMeshPro(addressObj, "Adres: 104 Akçaağaç Sokak", 19, FontStyles.Normal, TextAlignmentOptions.Left, new Color(0.4f, 0.85f, 1f));

        GameObject descBoxObj = new GameObject("DescriptionBox");
        descBoxObj.transform.SetParent(rightCargoCol.transform, false);
        descBoxObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 270);
        descBoxObj.AddComponent<Image>().color = new Color(0.05f, 0.07f, 0.1f, 0.98f);

        VerticalLayoutGroup descLayout = descBoxObj.AddComponent<VerticalLayoutGroup>();
        descLayout.padding = new RectOffset(16, 16, 16, 16);
        descLayout.childControlWidth = true;
        descLayout.childControlHeight = true;

        GameObject descTextObj = new GameObject("AddressDescriptionText");
        descTextObj.transform.SetParent(descBoxObj.transform, false);
        TextMeshProUGUI descText = AddTextMeshPro(descTextObj, "<b>Adres İpucu ve Açıklama:</b>\n\n\"Kırmızı çatılı, beyaz çitli ev...\"", 20, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color(1f, 0.95f, 0.75f));

        GameObject infoBoxObj = new GameObject("PhysicalDeliveryTipBox");
        infoBoxObj.transform.SetParent(rightCargoCol.transform, false);
        infoBoxObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 56);
        infoBoxObj.AddComponent<Image>().color = new Color(0.08f, 0.18f, 0.14f, 0.95f);

        GameObject infoTextObj = new GameObject("Text");
        infoTextObj.transform.SetParent(infoBoxObj.transform, false);
        RectTransform infoTextRect = infoTextObj.AddComponent<RectTransform>();
        infoTextRect.anchorMin = Vector2.zero; infoTextRect.anchorMax = Vector2.one; infoTextRect.sizeDelta = new Vector2(-20, 0);
        AddTextMeshPro(infoTextObj, "🚚 <b>Fiziksel Teslimat:</b> Paketi araçtan <b>[E]</b> ile alıp kapıdaki alana bırakınız.", 18, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.35f, 1f, 0.6f));

        // ==========================================
        // SUBVIEW 2: VEHICLE DEALERSHIP VIEW
        // ==========================================
        GameObject vehicleView = new GameObject("VehicleViewRoot");
        vehicleView.transform.SetParent(contentAreaObj.transform, false);
        RectTransform vViewRect = vehicleView.AddComponent<RectTransform>();
        vViewRect.anchorMin = Vector2.zero; vViewRect.anchorMax = Vector2.one; vViewRect.offsetMin = Vector2.zero; vViewRect.offsetMax = Vector2.zero;

        HorizontalLayoutGroup vvLayout = vehicleView.AddComponent<HorizontalLayoutGroup>();
        vvLayout.spacing = 16;
        vvLayout.childControlWidth = true;
        vvLayout.childControlHeight = true;
        vvLayout.childForceExpandWidth = false;
        vvLayout.childForceExpandHeight = true;

        // Left Column: Vehicles List
        GameObject leftVehCol = new GameObject("LeftColumn_Vehicles");
        leftVehCol.transform.SetParent(vehicleView.transform, false);
        LayoutElement lVehElem = leftVehCol.AddComponent<LayoutElement>();
        lVehElem.preferredWidth = 430;
        lVehElem.flexibleWidth = 0;

        leftVehCol.AddComponent<Image>().color = new Color(0.1f, 0.13f, 0.19f, 0.95f);
        VerticalLayoutGroup lVehLayout = leftVehCol.AddComponent<VerticalLayoutGroup>();
        lVehLayout.padding = new RectOffset(14, 14, 14, 14);
        lVehLayout.spacing = 10;
        lVehLayout.childControlWidth = true;
        lVehLayout.childControlHeight = false;

        GameObject vehListTitle = new GameObject("Title");
        vehListTitle.transform.SetParent(leftVehCol.transform, false);
        vehListTitle.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 36);
        AddTextMeshPro(vehListTitle, "🚚 ARAÇ KATALOĞU & FİLO", 22, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.2f));

        GameObject vehScrollObj = new GameObject("VehicleScrollView");
        vehScrollObj.transform.SetParent(leftVehCol.transform, false);
        vehScrollObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 640);
        vehScrollObj.AddComponent<Image>().color = new Color(0.05f, 0.07f, 0.1f, 0.85f);
        ScrollRect vehScrollRect = vehScrollObj.AddComponent<ScrollRect>();
        vehScrollRect.horizontal = false; vehScrollRect.vertical = true; vehScrollRect.scrollSensitivity = 35f;

        GameObject vehViewport = new GameObject("Viewport");
        vehViewport.transform.SetParent(vehScrollObj.transform, false);
        RectTransform vvpRect = vehViewport.AddComponent<RectTransform>();
        vvpRect.anchorMin = Vector2.zero; vvpRect.anchorMax = Vector2.one; vvpRect.sizeDelta = Vector2.zero;
        vehViewport.AddComponent<Image>().color = Color.white;
        Mask vMask = vehViewport.AddComponent<Mask>();
        vMask.showMaskGraphic = false;

        GameObject vehContent = new GameObject("Content");
        vehContent.transform.SetParent(vehViewport.transform, false);
        RectTransform vContRect = vehContent.AddComponent<RectTransform>();
        vContRect.anchorMin = new Vector2(0, 1); vContRect.anchorMax = new Vector2(1, 1);
        vContRect.pivot = new Vector2(0.5f, 1); vContRect.sizeDelta = Vector2.zero;

        VerticalLayoutGroup vcLayout = vehContent.AddComponent<VerticalLayoutGroup>();
        vcLayout.padding = new RectOffset(6, 6, 6, 6);
        vcLayout.spacing = 6;
        vcLayout.childControlWidth = true;
        vcLayout.childControlHeight = false;

        ContentSizeFitter vFitter = vehContent.AddComponent<ContentSizeFitter>();
        vFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        vehScrollRect.viewport = vvpRect;
        vehScrollRect.content = vContRect;

        // Vehicle Card Template
        GameObject vehCardTemplate = new GameObject("VehicleCardTemplate");
        vehCardTemplate.transform.SetParent(vehContent.transform, false);
        vehCardTemplate.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 82);
        vehCardTemplate.AddComponent<Image>().color = new Color(0.16f, 0.22f, 0.3f, 1f);
        vehCardTemplate.AddComponent<Button>();

        GameObject vctObj = new GameObject("Text");
        vctObj.transform.SetParent(vehCardTemplate.transform, false);
        RectTransform vctRect = vctObj.AddComponent<RectTransform>();
        vctRect.anchorMin = Vector2.zero; vctRect.anchorMax = Vector2.one; vctRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI vctTmp = AddTextMeshPro(vctObj, "<b>Heavy Cargo Van</b>\n$2,500 TL (Lvl 2)", 17, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, Color.white);
        if (vctTmp != null) vctTmp.margin = new Vector4(12, 0, 12, 0);

        // Right Column: Vehicle Details
        GameObject rightVehCol = new GameObject("RightColumn_VehicleDetail");
        rightVehCol.transform.SetParent(vehicleView.transform, false);
        LayoutElement rVehElem = rightVehCol.AddComponent<LayoutElement>();
        rVehElem.flexibleWidth = 1;

        rightVehCol.AddComponent<Image>().color = new Color(0.12f, 0.16f, 0.23f, 0.95f);
        VerticalLayoutGroup rvLayout = rightVehCol.AddComponent<VerticalLayoutGroup>();
        rvLayout.padding = new RectOffset(22, 22, 20, 20);
        rvLayout.spacing = 12;
        rvLayout.childControlWidth = true;
        rvLayout.childControlHeight = false;

        GameObject vNameObj = new GameObject("VehicleName");
        vNameObj.transform.SetParent(rightVehCol.transform, false);
        vNameObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 32);
        TextMeshProUGUI vNameTmp = AddTextMeshPro(vNameObj, "Heavy Cargo Van", 24, FontStyles.Bold, TextAlignmentOptions.Left, new Color(1f, 0.85f, 0.2f));

        GameObject vCapObj = new GameObject("CapacityText");
        vCapObj.transform.SetParent(rightVehCol.transform, false);
        vCapObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 25);
        TextMeshProUGUI vCapTmp = AddTextMeshPro(vCapObj, "📦 <b>Koli Kapasitesi:</b> 12 Paket", 19, FontStyles.Normal, TextAlignmentOptions.Left, Color.white);

        GameObject vLvlObj = new GameObject("LevelReqText");
        vLvlObj.transform.SetParent(rightVehCol.transform, false);
        vLvlObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 25);
        TextMeshProUGUI vLvlTmp = AddTextMeshPro(vLvlObj, "🛡️ <b>Gereken Seviye:</b> Seviye 2", 19, FontStyles.Normal, TextAlignmentOptions.Left, Color.white);

        GameObject vPriceObj = new GameObject("PriceText");
        vPriceObj.transform.SetParent(rightVehCol.transform, false);
        vPriceObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 25);
        TextMeshProUGUI vPriceTmp = AddTextMeshPro(vPriceObj, "💰 <b>Fiyat:</b> $2,500 TL", 19, FontStyles.Normal, TextAlignmentOptions.Left, Color.white);

        GameObject vDescBox = new GameObject("DescBox");
        vDescBox.transform.SetParent(rightVehCol.transform, false);
        vDescBox.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 220);
        vDescBox.AddComponent<Image>().color = new Color(0.05f, 0.07f, 0.1f, 0.98f);
        VerticalLayoutGroup vdLayout = vDescBox.AddComponent<VerticalLayoutGroup>();
        vdLayout.padding = new RectOffset(14, 14, 14, 14);

        GameObject vdtObj = new GameObject("DescText");
        vdtObj.transform.SetParent(vDescBox.transform, false);
        TextMeshProUGUI vDescTmp = AddTextMeshPro(vdtObj, "Geniş bagaj hacmine sahip sağlam kargo vanı...", 18, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color(0.9f, 0.9f, 0.95f));

        GameObject vStObj = new GameObject("StatusText");
        vStObj.transform.SetParent(rightVehCol.transform, false);
        vStObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 28);
        TextMeshProUGUI vStTmp = AddTextMeshPro(vStObj, "✓ SATIN ALINABİLİR", 19, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.2f, 1f, 0.4f));

        GameObject vBuyBtnObj = new GameObject("BuyButton");
        vBuyBtnObj.transform.SetParent(rightVehCol.transform, false);
        vBuyBtnObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 56);
        Image vBuyImg = vBuyBtnObj.AddComponent<Image>();
        vBuyImg.color = new Color(0.1f, 0.7f, 0.35f);
        Button vBuyBtn = vBuyBtnObj.AddComponent<Button>();

        GameObject vbbtObj = new GameObject("Text");
        vbbtObj.transform.SetParent(vBuyBtnObj.transform, false);
        RectTransform vbbtRect = vbbtObj.AddComponent<RectTransform>();
        vbbtRect.anchorMin = Vector2.zero; vbbtRect.anchorMax = Vector2.one; vbbtRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI vBuyTxt = AddTextMeshPro(vbbtObj, "🛒 ARACI SATIN AL ($2,500 TL)", 21, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

        // ==========================================
        // SUBVIEW 3: BRANCH OFFICE UPGRADE VIEW
        // ==========================================
        GameObject branchView = new GameObject("BranchViewRoot");
        branchView.transform.SetParent(contentAreaObj.transform, false);
        RectTransform bViewRect = branchView.AddComponent<RectTransform>();
        bViewRect.anchorMin = Vector2.zero; bViewRect.anchorMax = Vector2.one; bViewRect.offsetMin = Vector2.zero; bViewRect.offsetMax = Vector2.zero;

        HorizontalLayoutGroup bvLayout = branchView.AddComponent<HorizontalLayoutGroup>();
        bvLayout.spacing = 16;
        bvLayout.childControlWidth = true;
        bvLayout.childControlHeight = true;
        bvLayout.childForceExpandWidth = true;
        bvLayout.childForceExpandHeight = true;

        // Left Column: Current Branch
        GameObject leftBranchCol = new GameObject("LeftColumn_CurrentBranch");
        leftBranchCol.transform.SetParent(branchView.transform, false);
        LayoutElement lBranchElem = leftBranchCol.AddComponent<LayoutElement>();
        lBranchElem.flexibleWidth = 1;

        leftBranchCol.AddComponent<Image>().color = new Color(0.1f, 0.13f, 0.19f, 0.95f);
        VerticalLayoutGroup lbLayout = leftBranchCol.AddComponent<VerticalLayoutGroup>();
        lbLayout.padding = new RectOffset(22, 22, 20, 20);
        lbLayout.spacing = 12;
        lbLayout.childControlWidth = true;
        lbLayout.childControlHeight = false;

        GameObject lbHeader = new GameObject("Header");
        lbHeader.transform.SetParent(leftBranchCol.transform, false);
        lbHeader.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 32);
        AddTextMeshPro(lbHeader, "🏢 MEVCUT ŞUBE / DEPO", 22, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.3f, 0.85f, 1f));

        GameObject bTitleObj = new GameObject("BranchTitle");
        bTitleObj.transform.SetParent(leftBranchCol.transform, false);
        bTitleObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 30);
        TextMeshProUGUI curBranchTitle = AddTextMeshPro(bTitleObj, "<b>Küçük Dağıtım Kulübesi</b> (Seviye 1)", 21, FontStyles.Bold, TextAlignmentOptions.Left, new Color(1f, 0.85f, 0.2f));

        GameObject bDescBox = new GameObject("DescBox");
        bDescBox.transform.SetParent(leftBranchCol.transform, false);
        bDescBox.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 150);
        bDescBox.AddComponent<Image>().color = new Color(0.05f, 0.07f, 0.1f, 0.98f);
        VerticalLayoutGroup bdLayout = bDescBox.AddComponent<VerticalLayoutGroup>();
        bdLayout.padding = new RectOffset(14, 14, 14, 14);

        GameObject bdtObj = new GameObject("DescText");
        bdtObj.transform.SetParent(bDescBox.transform, false);
        TextMeshProUGUI curBranchDesc = AddTextMeshPro(bdtObj, "Başlangıç seviyesi kargo ofisi...", 18, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color(0.85f, 0.9f, 0.95f));

        GameObject bCapObj = new GameObject("CapText");
        bCapObj.transform.SetParent(leftBranchCol.transform, false);
        bCapObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 26);
        TextMeshProUGUI curBranchCap = AddTextMeshPro(bCapObj, "📦 <b>Günlük Paket Kotası:</b> 4 Paket / Gün", 19, FontStyles.Normal, TextAlignmentOptions.Left, Color.white);

        GameObject bRentObj = new GameObject("RentText");
        bRentObj.transform.SetParent(leftBranchCol.transform, false);
        bRentObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 26);
        TextMeshProUGUI curBranchRent = AddTextMeshPro(bRentObj, "💸 <b>Günlük İşletme Kirası:</b> $50 TL / Gün", 19, FontStyles.Normal, TextAlignmentOptions.Left, Color.white);

        // Right Column: Next Tier / Upgrade
        GameObject rightBranchCol = new GameObject("RightColumn_NextTier");
        rightBranchCol.transform.SetParent(branchView.transform, false);
        LayoutElement rBranchElem = rightBranchCol.AddComponent<LayoutElement>();
        rBranchElem.flexibleWidth = 1;

        rightBranchCol.AddComponent<Image>().color = new Color(0.12f, 0.16f, 0.23f, 0.95f);
        VerticalLayoutGroup rbLayout = rightBranchCol.AddComponent<VerticalLayoutGroup>();
        rbLayout.padding = new RectOffset(22, 22, 20, 20);
        rbLayout.spacing = 12;
        rbLayout.childControlWidth = true;
        rbLayout.childControlHeight = false;

        GameObject rbHeader = new GameObject("Header");
        rbHeader.transform.SetParent(rightBranchCol.transform, false);
        rbHeader.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 32);
        AddTextMeshPro(rbHeader, "🚀 ŞUBE GELİŞTİRME & YENİ SEVİYE", 22, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.3f, 1f, 0.5f));

        GameObject nTitleObj = new GameObject("NextTitle");
        nTitleObj.transform.SetParent(rightBranchCol.transform, false);
        nTitleObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 30);
        TextMeshProUGUI nextBranchTitle = AddTextMeshPro(nTitleObj, "<b>Lojistik Şubesi</b> (Seviye 2)", 21, FontStyles.Bold, TextAlignmentOptions.Left, new Color(1f, 0.85f, 0.2f));

        GameObject nDescBox = new GameObject("NextDescBox");
        nDescBox.transform.SetParent(rightBranchCol.transform, false);
        nDescBox.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 140);
        nDescBox.AddComponent<Image>().color = new Color(0.05f, 0.07f, 0.1f, 0.98f);
        VerticalLayoutGroup ndLayout = nDescBox.AddComponent<VerticalLayoutGroup>();
        ndLayout.padding = new RectOffset(14, 14, 14, 14);

        GameObject ndtObj = new GameObject("DescText");
        ndtObj.transform.SetParent(nDescBox.transform, false);
        TextMeshProUGUI nextBranchDesc = AddTextMeshPro(ndtObj, "Genişletilmiş depo alanı...", 18, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color(0.85f, 0.9f, 0.95f));

        GameObject nCapObj = new GameObject("NextCapText");
        nCapObj.transform.SetParent(rightBranchCol.transform, false);
        nCapObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 25);
        TextMeshProUGUI nextBranchCap = AddTextMeshPro(nCapObj, "📦 <b>Yeni Paket Kotası:</b> 4 ➔ <color=#32FF64>8 Paket (+4)</color>", 18, FontStyles.Normal, TextAlignmentOptions.Left, Color.white);

        GameObject nRentObj = new GameObject("NextRentText");
        nRentObj.transform.SetParent(rightBranchCol.transform, false);
        nRentObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 25);
        TextMeshProUGUI nextBranchRent = AddTextMeshPro(nRentObj, "💸 <b>Yeni Kira Bedeli:</b> $50 ➔ <color=#FFAA33>$120 TL</color>", 18, FontStyles.Normal, TextAlignmentOptions.Left, Color.white);

        GameObject nReqObj = new GameObject("NextReqText");
        nReqObj.transform.SetParent(rightBranchCol.transform, false);
        nReqObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 25);
        TextMeshProUGUI nextBranchReq = AddTextMeshPro(nReqObj, "👤 <b>Gereken Seviye:</b> Seviye 2 (Senin: 1)", 18, FontStyles.Normal, TextAlignmentOptions.Left, Color.white);

        GameObject bUpBtnObj = new GameObject("UpgradeButton");
        bUpBtnObj.transform.SetParent(rightBranchCol.transform, false);
        bUpBtnObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 58);
        Image bUpImg = bUpBtnObj.AddComponent<Image>();
        bUpImg.color = new Color(0.1f, 0.7f, 0.35f);
        Button bUpBtn = bUpBtnObj.AddComponent<Button>();

        GameObject bubtObj = new GameObject("Text");
        bubtObj.transform.SetParent(bUpBtnObj.transform, false);
        RectTransform bubtRect = bubtObj.AddComponent<RectTransform>();
        bubtRect.anchorMin = Vector2.zero; bubtRect.anchorMax = Vector2.one; bubtRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI bUpTxt = AddTextMeshPro(bubtObj, "🏢 ŞUBEYİ GELİŞTİR ($1,200 TL)", 21, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

        GameObject maxBadgeObj = new GameObject("MaxLevelBadge");
        maxBadgeObj.transform.SetParent(rightBranchCol.transform, false);
        maxBadgeObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 45);
        TextMeshProUGUI maxBadgeTxt = AddTextMeshPro(maxBadgeObj, "★ ŞUBE MAKSİMUM SEVİYEYE ULAŞTI ★", 21, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.3f, 1f, 0.6f));
        maxBadgeObj.SetActive(false);

        // Bind all references to CargoTabletUI component
        CargoTabletUI tabletScript = canvas.GetComponent<CargoTabletUI>();
        if (tabletScript == null) tabletScript = canvas.gameObject.AddComponent<CargoTabletUI>();

        tabletScript.tabletPanelRoot = tabObj;
        tabletScript.tabCargoButton = tabCargoBtn;
        tabletScript.tabVehicleButton = tabVehicleBtn;
        tabletScript.tabBranchButton = tabBranchBtn;
        tabletScript.endShiftButton = endShiftBtn;
        tabletScript.closeTabletButton = closeBtn;

        tabletScript.cargoViewRoot = cargoView;
        tabletScript.cargoListContent = contentRect;
        tabletScript.cargoCardTemplate = cardTemplate;
        tabletScript.detailCardRoot = rightCargoCol;
        tabletScript.trackingNumberText = trackingText;
        tabletScript.recipientNameText = recipientText;
        tabletScript.targetAddressText = addressText;
        tabletScript.addressDescriptionText = descText;
        tabletScript.dropCargoButton = null;
        tabletScript.emptyListText = emptyTmp;

        tabletScript.vehicleViewRoot = vehicleView;
        tabletScript.vehicleListContent = vContRect;
        tabletScript.vehicleCardTemplate = vehCardTemplate;
        tabletScript.vehicleDetailRoot = rightVehCol;
        tabletScript.vehicleNameText = vNameTmp;
        tabletScript.vehicleDescText = vDescTmp;
        tabletScript.vehicleCapacityText = vCapTmp;
        tabletScript.vehicleLevelReqText = vLvlTmp;
        tabletScript.vehiclePriceText = vPriceTmp;
        tabletScript.vehicleStatusText = vStTmp;
        tabletScript.vehicleBuyButton = vBuyBtn;
        tabletScript.vehicleBuyButtonText = vBuyTxt;
        tabletScript.vehicleRecallButton = null;
        tabletScript.vehicleRecallButtonText = null;

        tabletScript.branchViewRoot = branchView;
        tabletScript.currentBranchTitleText = curBranchTitle;
        tabletScript.currentBranchDescText = curBranchDesc;
        tabletScript.currentBranchCapacityText = curBranchCap;
        tabletScript.currentBranchRentText = curBranchRent;
        tabletScript.nextBranchInfoRoot = rightBranchCol;
        tabletScript.nextBranchTitleText = nextBranchTitle;
        tabletScript.nextBranchDescText = nextBranchDesc;
        tabletScript.nextBranchCapacityText = nextBranchCap;
        tabletScript.nextBranchRentText = nextBranchRent;
        tabletScript.nextBranchLevelReqText = nextBranchReq;
        tabletScript.branchUpgradeButton = bUpBtn;
        tabletScript.branchUpgradeButtonText = bUpTxt;
        tabletScript.branchMaxLevelBadge = maxBadgeTxt;

        cardTemplate.SetActive(false);
        vehCardTemplate.SetActive(false);
        cargoView.SetActive(true);
        vehicleView.SetActive(false);
        branchView.SetActive(false);
        tabObj.SetActive(false);

        // ==========================================
        // 2. DAY SUMMARY PANEL
        // ==========================================
        GameObject sumObj = new GameObject("DaySummaryPanel");
        sumObj.transform.SetParent(canvas.transform, false);
        RectTransform sumRect = sumObj.AddComponent<RectTransform>();
        sumRect.anchorMin = new Vector2(0.5f, 0.5f);
        sumRect.anchorMax = new Vector2(0.5f, 0.5f);
        sumRect.pivot = new Vector2(0.5f, 0.5f);
        sumRect.sizeDelta = new Vector2(850, 750);

        Image sumImg = sumObj.AddComponent<Image>();
        sumImg.color = new Color(0.08f, 0.1f, 0.14f, 0.98f);

        VerticalLayoutGroup sumLayout = sumObj.AddComponent<VerticalLayoutGroup>();
        sumLayout.padding = new RectOffset(30, 30, 30, 30);
        sumLayout.spacing = 10;
        sumLayout.childControlWidth = true;
        sumLayout.childControlHeight = true;
        sumLayout.childForceExpandWidth = true;
        sumLayout.childForceExpandHeight = false;

        // Title
        GameObject sumTitleObj = new GameObject("SummaryTitle");
        sumTitleObj.transform.SetParent(sumObj.transform, false);
        LayoutElement titleLayout = sumTitleObj.AddComponent<LayoutElement>();
        titleLayout.preferredHeight = 45;
        titleLayout.minHeight = 45;
        AddTextMeshPro(sumTitleObj, "DAY SUMMARY REPORT", 26, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.3f));

        // Total Delivered
        GameObject sumTotObj = new GameObject("TotalDeliveredText");
        sumTotObj.transform.SetParent(sumObj.transform, false);
        LayoutElement totLayout = sumTotObj.AddComponent<LayoutElement>();
        totLayout.preferredHeight = 25;
        totLayout.minHeight = 25;
        TextMeshProUGUI sumTotText = AddTextMeshPro(sumTotObj, "Total Delivered: 0 Packages", 18, FontStyles.Normal, TextAlignmentOptions.Left, Color.white);

        // Correct Deliveries
        GameObject sumCorObj = new GameObject("CorrectDeliveriesText");
        sumCorObj.transform.SetParent(sumObj.transform, false);
        LayoutElement corLayout = sumCorObj.AddComponent<LayoutElement>();
        corLayout.preferredHeight = 25;
        corLayout.minHeight = 25;
        TextMeshProUGUI sumCorText = AddTextMeshPro(sumCorObj, "[+] Correct Deliveries: 0 (+0 $)", 18, FontStyles.Normal, TextAlignmentOptions.Left, new Color(0.3f, 0.95f, 0.4f));

        // Wrong Deliveries
        GameObject sumWrObj = new GameObject("WrongDeliveriesText");
        sumWrObj.transform.SetParent(sumObj.transform, false);
        LayoutElement wrLayout = sumWrObj.AddComponent<LayoutElement>();
        wrLayout.preferredHeight = 25;
        wrLayout.minHeight = 25;
        TextMeshProUGUI sumWrText = AddTextMeshPro(sumWrObj, "[-] Wrong Deliveries: 0 (-0 $ Penalty)", 18, FontStyles.Normal, TextAlignmentOptions.Left, new Color(0.95f, 0.3f, 0.3f));

        // Net Earnings
        GameObject sumEarnObj = new GameObject("NetEarningsText");
        sumEarnObj.transform.SetParent(sumObj.transform, false);
        LayoutElement earnLayout = sumEarnObj.AddComponent<LayoutElement>();
        earnLayout.preferredHeight = 35;
        earnLayout.minHeight = 35;
        TextMeshProUGUI sumEarnText = AddTextMeshPro(sumEarnObj, "TODAY: +0 $ | TOTAL VAULT: 0 $", 24, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.2f, 0.95f, 0.3f));

        // History ScrollView
        GameObject sumScrollObj = new GameObject("HistoryScrollView");
        sumScrollObj.transform.SetParent(sumObj.transform, false);
        LayoutElement scrollLayout = sumScrollObj.AddComponent<LayoutElement>();
        scrollLayout.preferredHeight = 320;
        scrollLayout.minHeight = 250;
        scrollLayout.flexibleHeight = 0;

        sumScrollObj.AddComponent<Image>().color = new Color(0.04f, 0.06f, 0.08f, 0.85f);
        ScrollRect sumScrollRect = sumScrollObj.AddComponent<ScrollRect>();
        sumScrollRect.horizontal = false;
        sumScrollRect.vertical = true;
        sumScrollRect.scrollSensitivity = 35f;

        GameObject sumViewObj = new GameObject("Viewport");
        sumViewObj.transform.SetParent(sumScrollObj.transform, false);
        RectTransform sumViewRect = sumViewObj.AddComponent<RectTransform>();
        sumViewRect.anchorMin = Vector2.zero;
        sumViewRect.anchorMax = Vector2.one;
        sumViewRect.sizeDelta = Vector2.zero;
        sumViewObj.AddComponent<Image>().color = Color.white;
        Mask sumMask = sumViewObj.AddComponent<Mask>();
        sumMask.showMaskGraphic = false;

        GameObject sumContObj = new GameObject("Content");
        sumContObj.transform.SetParent(sumViewObj.transform, false);
        RectTransform sumContRect = sumContObj.AddComponent<RectTransform>();
        sumContRect.anchorMin = new Vector2(0, 1);
        sumContRect.anchorMax = new Vector2(1, 1);
        sumContRect.pivot = new Vector2(0.5f, 1);
        sumContRect.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup sumContLayout = sumContObj.AddComponent<VerticalLayoutGroup>();
        sumContLayout.padding = new RectOffset(8, 8, 8, 8);
        sumContLayout.spacing = 8;
        sumContLayout.childControlWidth = true;
        sumContLayout.childControlHeight = false;

        ContentSizeFitter sumFitter = sumContObj.AddComponent<ContentSizeFitter>();
        sumFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sumScrollRect.viewport = sumViewRect;
        sumScrollRect.content = sumContRect;

        // History Row Template
        GameObject rowTemplate = new GameObject("HistoryRowTemplate");
        rowTemplate.transform.SetParent(sumContObj.transform, false);
        rowTemplate.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 55);
        rowTemplate.AddComponent<Image>().color = new Color(0.15f, 0.35f, 0.2f, 0.8f);

        GameObject rowTextObj = new GameObject("Text");
        rowTextObj.transform.SetParent(rowTemplate.transform, false);
        RectTransform rowTextRect = rowTextObj.AddComponent<RectTransform>();
        rowTextRect.anchorMin = Vector2.zero;
        rowTextRect.anchorMax = Vector2.one;
        rowTextRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI rowText = AddTextMeshPro(rowTextObj, "#CRG-1001\nDelivered to: Grand Ave. No: 1 | Target: Grand Ave. No: 1 [CORRECT]", 15, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, Color.white);
        if (rowText != null) rowText.margin = new Vector4(12, 0, 12, 0);

        // START NEXT DAY BUTTON
        GameObject restartBtnObj = new GameObject("RestartDayButton");
        restartBtnObj.transform.SetParent(sumObj.transform, false);
        LayoutElement btnLayout = restartBtnObj.AddComponent<LayoutElement>();
        btnLayout.preferredHeight = 70;
        btnLayout.minHeight = 70;
        btnLayout.flexibleHeight = 0;

        Image restImg = restartBtnObj.AddComponent<Image>();
        restImg.color = new Color(0.15f, 0.55f, 0.95f);

        Button restBtn = restartBtnObj.AddComponent<Button>();
        ColorBlock btnColors = restBtn.colors;
        btnColors.highlightedColor = new Color(0.25f, 0.7f, 1f);
        btnColors.pressedColor = new Color(0.1f, 0.4f, 0.8f);
        restBtn.colors = btnColors;

        GameObject restTextObj = new GameObject("Text");
        restTextObj.transform.SetParent(restartBtnObj.transform, false);
        RectTransform restTextRect = restTextObj.AddComponent<RectTransform>();
        restTextRect.anchorMin = Vector2.zero;
        restTextRect.anchorMax = Vector2.one;
        restTextRect.sizeDelta = Vector2.zero;

        AddTextMeshPro(restTextObj, "START NEXT DAY", 24, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

        // DaySummaryManager
        DaySummaryManager sumScript = canvas.GetComponent<DaySummaryManager>();
        if (sumScript == null) sumScript = canvas.gameObject.AddComponent<DaySummaryManager>();

        sumScript.summaryPanelRoot = sumObj;
        sumScript.totalDeliveredText = sumTotText;
        sumScript.correctDeliveriesText = sumCorText;
        sumScript.wrongDeliveriesText = sumWrText;
        sumScript.netEarningsText = sumEarnText;
        sumScript.historyListContent = sumContRect;
        sumScript.historyItemTemplate = rowTemplate;
        sumScript.restartDayButton = restBtn;

        rowTemplate.SetActive(false);
        sumObj.SetActive(false);

        // Interaction Prompt HUD (Press E to interact, drive, pick cargo)
        InteractionPromptHUD.CreatePromptHUDTool();

        EditorUtility.SetDirty(canvas.gameObject);
        Debug.Log("[DeliveryUIBuilder] Professional & Clean Tablet UI successfully generated!");
    }

    private static TextMeshProUGUI AddTextMeshPro(GameObject target, string text, float fontSize, FontStyles style, TextAlignmentOptions alignment, Color color)
    {
        if (target == null) return null;

        if (target.GetComponent<CanvasRenderer>() == null)
        {
            target.AddComponent<CanvasRenderer>();
        }

        TextMeshProUGUI tmp = target.GetComponent<TextMeshProUGUI>();
        if (tmp == null)
        {
            tmp = target.AddComponent<TextMeshProUGUI>();
        }

        if (tmp != null)
        {
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.color = color;
        }

        return tmp;
    }
}
#endif
