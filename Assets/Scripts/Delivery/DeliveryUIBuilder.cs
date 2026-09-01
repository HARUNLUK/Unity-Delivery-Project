#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class DeliveryUIBuilder
{
    [MenuItem("Tools/Delivery Game/Build Clean Delivery UI", false, 1)]
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
        topBarRect.sizeDelta = new Vector2(1300, 85);

        Image topBarBg = topBarObj.AddComponent<Image>();
        topBarBg.color = new Color(0.05f, 0.07f, 0.1f, 0.96f);

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
        clockCard.AddComponent<Image>().color = new Color(0.12f, 0.16f, 0.22f, 0.95f);

        GameObject clockTextObj = new GameObject("Text");
        clockTextObj.transform.SetParent(clockCard.transform, false);
        RectTransform clockTextRect = clockTextObj.AddComponent<RectTransform>();
        clockTextRect.anchorMin = Vector2.zero;
        clockTextRect.anchorMax = Vector2.one;
        clockTextRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI clockText = AddTextMeshPro(clockTextObj, "TIME: 09:00", 30, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.2f));

        // 2. Remaining Cargo Card
        GameObject cargoCard = new GameObject("CargoCard");
        cargoCard.transform.SetParent(topBarObj.transform, false);
        cargoCard.AddComponent<Image>().color = new Color(0.12f, 0.16f, 0.22f, 0.95f);

        GameObject cargoTextObj = new GameObject("Text");
        cargoTextObj.transform.SetParent(cargoCard.transform, false);
        RectTransform cargoTextRect = cargoTextObj.AddComponent<RectTransform>();
        cargoTextRect.anchorMin = Vector2.zero;
        cargoTextRect.anchorMax = Vector2.one;
        cargoTextRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI remainingText = AddTextMeshPro(cargoTextObj, "REMAINING: 10 / 10", 30, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

        // 3. Balance Card
        GameObject balanceCard = new GameObject("BalanceCard");
        balanceCard.transform.SetParent(topBarObj.transform, false);
        balanceCard.AddComponent<Image>().color = new Color(0.12f, 0.16f, 0.22f, 0.95f);

        GameObject balanceTextObj = new GameObject("Text");
        balanceTextObj.transform.SetParent(balanceCard.transform, false);
        RectTransform balanceTextRect = balanceTextObj.AddComponent<RectTransform>();
        balanceTextRect.anchorMin = Vector2.zero;
        balanceTextRect.anchorMax = Vector2.one;
        balanceTextRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI balanceText = AddTextMeshPro(balanceTextObj, "BALANCE: 0 $ (+0 $)", 28, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.2f, 1f, 0.4f));

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
        notifRect.anchoredPosition = new Vector2(0, -115);
        notifRect.sizeDelta = new Vector2(900, 70);

        Image notifImg = notifObj.AddComponent<Image>();
        notifImg.color = new Color(0.85f, 0.15f, 0.15f, 0.98f);

        GameObject notifTextObj = new GameObject("Text");
        notifTextObj.transform.SetParent(notifObj.transform, false);
        RectTransform notifTextRect = notifTextObj.AddComponent<RectTransform>();
        notifTextRect.anchorMin = Vector2.zero;
        notifTextRect.anchorMax = Vector2.one;
        notifTextRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI notifText = AddTextMeshPro(notifTextObj, "[-] DROPPED ON THE STREET! (-100 $ PENALTY)", 24, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

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
        tabRect.sizeDelta = new Vector2(1200, 750);

        Image tabImg = tabObj.AddComponent<Image>();
        tabImg.color = new Color(0.08f, 0.1f, 0.14f, 0.98f);

        HorizontalLayoutGroup tabLayout = tabObj.AddComponent<HorizontalLayoutGroup>();
        tabLayout.padding = new RectOffset(25, 25, 25, 25);
        tabLayout.spacing = 20;
        tabLayout.childControlWidth = true;
        tabLayout.childControlHeight = true;

        // Left Column: List
        GameObject leftCol = new GameObject("LeftColumn_List");
        leftCol.transform.SetParent(tabObj.transform, false);
        LayoutElement leftLayout = leftCol.AddComponent<LayoutElement>();
        leftLayout.preferredWidth = 420;
        leftLayout.flexibleWidth = 0;

        VerticalLayoutGroup leftColLayout = leftCol.AddComponent<VerticalLayoutGroup>();
        leftColLayout.spacing = 12;
        leftColLayout.childControlWidth = true;
        leftColLayout.childControlHeight = false;

        GameObject listTitleObj = new GameObject("ListTitle");
        listTitleObj.transform.SetParent(leftCol.transform, false);
        listTitleObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 45);
        AddTextMeshPro(listTitleObj, "CARGO IN TRUNK", 24, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.8f, 0.2f));

        GameObject scrollObj = new GameObject("CargoScrollView");
        scrollObj.transform.SetParent(leftCol.transform, false);
        scrollObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 600);
        scrollObj.AddComponent<Image>().color = new Color(0.04f, 0.06f, 0.08f, 0.85f);
        ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 35f;

        GameObject viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(scrollObj.transform, false);
        RectTransform viewRect = viewportObj.AddComponent<RectTransform>();
        viewRect.anchorMin = Vector2.zero;
        viewRect.anchorMax = Vector2.one;
        viewRect.sizeDelta = Vector2.zero;
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
        contentLayout.padding = new RectOffset(8, 8, 8, 8);
        contentLayout.spacing = 8;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = false;

        ContentSizeFitter fitter = contentObj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewRect;
        scrollRect.content = contentRect;

        // Card Template
        GameObject cardTemplate = new GameObject("CargoCardTemplate");
        cardTemplate.transform.SetParent(contentObj.transform, false);
        cardTemplate.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 80);
        cardTemplate.AddComponent<Image>().color = new Color(0.16f, 0.2f, 0.26f, 1f);

        Button cardBtn = cardTemplate.AddComponent<Button>();
        ColorBlock colors = cardBtn.colors;
        colors.highlightedColor = new Color(0.25f, 0.35f, 0.45f);
        colors.pressedColor = new Color(0.1f, 0.5f, 0.8f);
        colors.selectedColor = new Color(0.1f, 0.55f, 0.9f);
        cardBtn.colors = colors;

        GameObject cardTextObj = new GameObject("Text");
        cardTextObj.transform.SetParent(cardTemplate.transform, false);
        RectTransform cardTextRect = cardTextObj.AddComponent<RectTransform>();
        cardTextRect.anchorMin = Vector2.zero;
        cardTextRect.anchorMax = Vector2.one;
        cardTextRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI cardText = AddTextMeshPro(cardTextObj, "<b>#CRG-1001</b>\nJohn Smith", 18, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, Color.white);
        if (cardText != null) cardText.margin = new Vector4(12, 0, 12, 0);

        // Right Column: Detail & Drop
        GameObject rightCol = new GameObject("RightColumn_Detail");
        rightCol.transform.SetParent(tabObj.transform, false);
        LayoutElement rightLayout = rightCol.AddComponent<LayoutElement>();
        rightLayout.flexibleWidth = 1;

        Image rightBg = rightCol.AddComponent<Image>();
        rightBg.color = new Color(0.12f, 0.15f, 0.2f, 0.95f);

        VerticalLayoutGroup rightColLayout = rightCol.AddComponent<VerticalLayoutGroup>();
        rightColLayout.padding = new RectOffset(20, 20, 20, 20);
        rightColLayout.spacing = 15;
        rightColLayout.childControlWidth = true;
        rightColLayout.childControlHeight = false;

        GameObject trackingObj = new GameObject("TrackingNumberText");
        trackingObj.transform.SetParent(rightCol.transform, false);
        trackingObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 30);
        TextMeshProUGUI trackingText = AddTextMeshPro(trackingObj, "Tracking No: #CRG-1001", 22, FontStyles.Bold, TextAlignmentOptions.Left, new Color(1f, 0.8f, 0.2f));

        GameObject recipientObj = new GameObject("RecipientNameText");
        recipientObj.transform.SetParent(rightCol.transform, false);
        recipientObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 25);
        TextMeshProUGUI recipientText = AddTextMeshPro(recipientObj, "Recipient: John Smith", 20, FontStyles.Normal, TextAlignmentOptions.Left, Color.white);

        GameObject addressObj = new GameObject("TargetAddressText");
        addressObj.transform.SetParent(rightCol.transform, false);
        addressObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 25);
        TextMeshProUGUI addressText = AddTextMeshPro(addressObj, "Address: 104 Maple Street", 20, FontStyles.Normal, TextAlignmentOptions.Left, new Color(0.4f, 0.8f, 1f));

        GameObject descBoxObj = new GameObject("DescriptionBox");
        descBoxObj.transform.SetParent(rightCol.transform, false);
        descBoxObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 300);
        descBoxObj.AddComponent<Image>().color = new Color(0.05f, 0.07f, 0.1f, 0.98f);

        VerticalLayoutGroup descLayout = descBoxObj.AddComponent<VerticalLayoutGroup>();
        descLayout.padding = new RectOffset(18, 18, 18, 18);
        descLayout.childControlWidth = true;
        descLayout.childControlHeight = true;

        GameObject descTextObj = new GameObject("AddressDescriptionText");
        descTextObj.transform.SetParent(descBoxObj.transform, false);
        TextMeshProUGUI descText = AddTextMeshPro(descTextObj, "<b>Delivery Clue / Description:</b>\n\n\"Red roof house with white picket fences.\"", 22, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color(1f, 0.95f, 0.75f));

        GameObject dropBtnObj = new GameObject("DropCargoButton");
        dropBtnObj.transform.SetParent(rightCol.transform, false);
        dropBtnObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 70);
        dropBtnObj.AddComponent<Image>().color = new Color(0.1f, 0.7f, 0.35f);
        Button dropBtn = dropBtnObj.AddComponent<Button>();

        GameObject dropTextObj = new GameObject("Text");
        dropTextObj.transform.SetParent(dropBtnObj.transform, false);
        RectTransform dropTextRect = dropTextObj.AddComponent<RectTransform>();
        dropTextRect.anchorMin = Vector2.zero;
        dropTextRect.anchorMax = Vector2.one;
        dropTextRect.sizeDelta = Vector2.zero;

        AddTextMeshPro(dropTextObj, "DROP THIS PACKAGE", 22, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

        // CargoTabletUI
        CargoTabletUI tabletScript = canvas.GetComponent<CargoTabletUI>();
        if (tabletScript == null) tabletScript = canvas.gameObject.AddComponent<CargoTabletUI>();

        tabletScript.tabletPanelRoot = tabObj;
        tabletScript.cargoListContent = contentRect;
        tabletScript.cargoCardTemplate = cardTemplate;
        tabletScript.detailCardRoot = rightCol;
        tabletScript.trackingNumberText = trackingText;
        tabletScript.recipientNameText = recipientText;
        tabletScript.targetAddressText = addressText;
        tabletScript.addressDescriptionText = descText;
        tabletScript.dropCargoButton = dropBtn;

        cardTemplate.SetActive(false);
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

        EditorUtility.SetDirty(canvas.gameObject);
        Debug.Log("[DeliveryUIBuilder] English UI successfully generated!");
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
