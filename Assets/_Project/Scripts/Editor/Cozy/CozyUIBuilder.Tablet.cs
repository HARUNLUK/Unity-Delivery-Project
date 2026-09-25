#if UNITY_EDITOR
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static CozyKit;

public static partial class CozyUIBuilder
{
    // =====================================================================
    // 2. KURYE DEFTERI (TABLET)
    // =====================================================================

    private static void BuildTablet(Transform canvas)
    {
        Remove(canvas, "CargoTabletPanel");

        RectTransform root = Node("CargoTabletPanel", canvas);
        Stretch(root);
        Scrim(root, "Scrim", CozyTheme.Scrim);

        RectTransform book = Node("Notebook", root);
        Place(book, Anchor.MC, 0f, 0f, 1600f, 930f);
        book.gameObject.AddComponent<CozyPanelIntro>();

        RectTransform shadow = Node("Shadow", book);
        Stretch(shadow, -26f, -14f, -26f, -40f);
        Image shadowImg = shadow.gameObject.AddComponent<Image>();
        shadowImg.sprite = A.softShadow;
        shadowImg.type = Image.Type.Sliced;
        shadowImg.color = CozyTheme.SoftShadow;
        shadowImg.raycastTarget = false;

        // --- Tab strip ---
        RectTransform strip = Node("TabStrip", book);
        TopStrip(strip, 108f);
        Shape(strip, CozyTheme.KraftStrip, 28f, true);
        HorizontalLayoutGroup stripLayout = HStack(strip, 16f, Pad(24, 24, 0, 0));
        stripLayout.childForceExpandHeight = true;

        RectTransform tabs = Node("Tabs", strip);
        HStack(tabs, 8f, Pad(0, 0, 0, -12), TextAnchor.LowerLeft);
        Size(tabs, -1f, -1f, 1f);
        Button tabCargo = FolderTab(tabs, "Tab_Cargo", "box", "Teslimatlar", true, out TextMeshProUGUI tabCount);
        Button tabVehicles = FolderTab(tabs, "Tab_Vehicles", "truck", "Araçlar", false, out _);
        Button tabBranch = FolderTab(tabs, "Tab_Branch", "home", "Şube", false, out _);

        RectTransform tools = Node("Tools", strip);
        HStack(tools, 14f, null, TextAnchor.MiddleRight);
        Image clockChip = Chip(tools, "ClockChip", "09:00", CozyTheme.Tone.Kraft, "clock", 48f, 22f);
        clockChip.color = CozyTheme.Paper;
        TextMeshProUGUI clockText = clockChip.transform.Find("Text").GetComponent<TextMeshProUGUI>();
        Image cashChip = Chip(tools, "CashChip", "$0", CozyTheme.Tone.Kraft, "coin", 48f, 22f);
        cashChip.color = CozyTheme.Paper;
        TextMeshProUGUI cashText = cashChip.transform.Find("Text").GetComponent<TextMeshProUGUI>();
        Button endShift = Btn(tools, "EndShiftButton", "Vardiyayı bitir", CozyTheme.ButtonStyle.Warning, 52f, null, 22f);
        Size(endShift, 230f, 52f);
        Button close = RoundButton(tools, "CloseButton", "x", CozyTheme.ButtonStyle.Soft, 52f);

        // --- Page ---
        RectTransform page = Node("Page", book);
        Stretch(page, 0f, 100f, 0f, 0f);
        Image pageImg = Shape(page, CozyTheme.Paper, 28f, true);
        Lip(pageImg, CozyTheme.PaperEdge, CozyTheme.LipPanel);

        RectTransform content = Node("TabletContentArea", page);
        Stretch(content, 32f, 32f, 32f, 32f);

        CargoView cv = BuildCargoView(content);
        VehicleView vv = BuildVehicleView(content);
        BranchView bv = BuildBranchView(content);
        vv.root.SetActive(false);
        bv.root.SetActive(false);

        root.gameObject.SetActive(false);

        CargoTabletUI tablet = Find<CargoTabletUI>();
        Wire(tablet,
            ("tabletPanelRoot", root.gameObject), ("cargoListContent", cv.listContent), ("cargoCardTemplate", cv.template),
            ("detailCardRoot", cv.detailRoot), ("detailHeaderTitleText", cv.detailHeader), ("trackingNumberText", cv.tracking),
            ("recipientNameText", cv.recipient), ("targetAddressText", cv.address), ("addressHintTitleText", cv.hintTitle),
            ("addressDescriptionText", cv.description), ("dropCargoButton", null), ("emptyListText", cv.empty),
            ("cargoViewRoot", cv.root), ("vehicleViewRoot", vv.root), ("vehicleListContent", vv.listContent), ("vehicleCardTemplate", vv.template),
            ("vehicleDetailRoot", vv.detailRoot), ("vehicleNameText", vv.name), ("vehicleDescText", vv.desc),
            ("vehicleCapacityText", null), ("vehicleLevelReqText", null), ("vehiclePriceText", null), ("vehicleStatusText", null),
            ("vehicleFuelStatusText", null), ("vehicleBuyButton", vv.buy), ("vehicleBuyButtonText", LabelOf(vv.buy)),
            ("vehicleRefuelButton", vv.refuel), ("vehicleRefuelButtonText", LabelOf(vv.refuel)),
            ("vehicleRecallButton", vv.recall), ("vehicleRecallButtonText", LabelOf(vv.recall)),
            ("branchViewRoot", bv.root), ("currentBranchTitleText", bv.curTitle), ("currentBranchDescText", bv.curDesc),
            ("currentBranchCapacityText", bv.curCap), ("currentBranchRentText", bv.curRent), ("nextBranchInfoRoot", bv.nextRoot),
            ("nextDescBoxRoot", bv.nextDescBox), ("nextBranchTitleText", bv.nextTitle), ("nextBranchDescText", bv.nextDesc),
            ("nextBranchCapacityText", bv.nextCap), ("nextBranchRentText", bv.nextRent), ("nextBranchLevelReqText", null),
            ("branchUpgradeButton", bv.upgrade), ("branchUpgradeButtonText", LabelOf(bv.upgrade)), ("branchMaxLevelBadge", bv.maxBadge),
            ("tabCargoButton", tabCargo), ("tabVehicleButton", tabVehicles), ("tabBranchButton", tabBranch),
            ("endShiftButton", endShift), ("closeTabletButton", close), ("activeTabSprite", null), ("inactiveTabSprite", null),
            ("cargoSearchInput", cv.search), ("filterAllButton", cv.filterAll), ("filterInVehicleButton", cv.filterVehicle),
            ("filterExpressButton", cv.filterExpress), ("filterFragileButton", cv.filterFragile),
            ("detailTypeText", cv.factType), ("detailDeadlineText", cv.factDeadline), ("detailRewardText", cv.factReward),
            ("detailStatusText", cv.factStatus), ("detailTipRoot", cv.tipRoot), ("detailTipIcon", cv.tipIcon), ("detailTipText", cv.tipText),
            ("vehicleFuelBarFill", vv.fuelFill), ("vehicleConditionBarFill", vv.condFill),
            ("vehicleFuelValueText", vv.fuelValue), ("vehicleConditionValueText", vv.condValue),
            ("tabletClockText", clockText), ("tabletCashText", cashText), ("cargoTabCountText", tabCount));

        SetBool(tablet, "useKuryeDefteriLayout", true);

        Report.Add("• Kurye Defteri: teslimatlar (arama, filtre, sıralama), araçlar, şube");
    }

    private static Button FolderTab(Transform parent, string name, string icon, string label, bool active, out TextMeshProUGUI countText)
    {
        RectTransform rt = Node(name, parent);
        Image face = Shape(rt, active ? CozyTheme.Paper : CozyTheme.TabIdle, 20f, true);
        Button btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = face;
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(1.04f, 1.04f, 1.04f, 1f);
        cb.pressedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        btn.colors = cb;
        LayoutElement le = Size(rt, -1f, active ? 92f : 82f);
        le.minWidth = 220f;

        RectTransform inner = Node("Inner", rt);
        Stretch(inner, 26f, 0f, 26f, 12f);
        HStack(inner, 10f, null, TextAnchor.MiddleCenter);
        Color ink = active ? CozyTheme.Ink : CozyTheme.InkSoft;
        Icon(inner, "Icon", icon, 28f, ink);
        TextMeshProUGUI t = Txt(inner, "Label", label, TextStyle.H3, ink, TextAlignmentOptions.MidlineLeft, 24f, false, false);
        t.enableAutoSizing = false;
        Size(t, -1f, 40f);

        countText = null;
        if (name == "Tab_Cargo")
        {
            RectTransform badge = Node("CountBadge", inner);
            Shape(badge, CozyTheme.Red, 14f);
            HStack(badge, 0f, Pad(10, 10, 0, 0), TextAnchor.MiddleCenter);
            Size(badge, -1f, 28f).minWidth = 28f;
            countText = Txt(badge, "Count", "0", TextStyle.BodyBold, Color.white, TextAlignmentOptions.Center, 16f, false, false);
            countText.enableAutoSizing = false;
            Size(countText, -1f, 28f);
        }

        // Sizes the tab to its content (icon + label + badge + padding).
        ContentSizeFitter fit = rt.gameObject.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        le.preferredWidth = name == "Tab_Cargo" ? 270f : 220f;
        return btn;
    }

    // ---------------------------------------------------------------------

    private struct CargoView
    {
        public GameObject root, template, detailRoot, tipRoot;
        public Transform listContent;
        public TMP_InputField search;
        public Button filterAll, filterVehicle, filterExpress, filterFragile;
        public TextMeshProUGUI empty, detailHeader, tracking, recipient, address, hintTitle, description,
            factType, factDeadline, factReward, factStatus, tipText;
        public Image tipIcon;
    }

    private static CargoView BuildCargoView(Transform parent)
    {
        CargoView v = new CargoView();
        RectTransform root = Node("CargoViewRoot", parent);
        Stretch(root);
        v.root = root.gameObject;

        // ---- Left: search, filters, list ----
        RectTransform left = Node("LeftColumn_List", root);
        left.anchorMin = new Vector2(0f, 0f);
        left.anchorMax = new Vector2(0f, 1f);
        left.pivot = new Vector2(0f, 0.5f);
        left.sizeDelta = new Vector2(640f, 0f);
        left.anchoredPosition = Vector2.zero;
        VStack(left, 14f);

        v.search = SearchField(left, "SearchField", "Alıcı veya sokak ara", 60f);
        Loc(v.search.placeholder as TextMeshProUGUI, "cozy_tablet_search", "Alıcı veya sokak ara");

        RectTransform filters = Node("FilterRow", left);
        HStack(filters, 8f);
        Size(filters, -1f, 42f);
        v.filterAll = FilterChip(filters, "Filter_All", "Tümü 0", true);
        v.filterVehicle = FilterChip(filters, "Filter_InVehicle", "Araçta 0", false);
        v.filterExpress = FilterChip(filters, "Filter_Express", "Ekspres 0", false);
        v.filterFragile = FilterChip(filters, "Filter_Fragile", "Kırılacak 0", false);

        RectTransform headRow = Node("ListHeadRow", left);
        HStack(headRow, 8f);
        Size(headRow, -1f, 26f);
        TextMeshProUGUI today = Txt(headRow, "TodayHeader", "Bugünkü koliler", TextStyle.Label, CozyTheme.InkSoft, TextAlignmentOptions.MidlineLeft, 17f, false);
        Size(today, -1f, 26f, 1f);
        Loc(today, "cozy_tablet_today", "Bugünkü koliler");
        TextMeshProUGUI sortHint = Txt(headRow, "SortHint", "Sıra: teslim saati", TextStyle.Small, CozyTheme.InkSoft, TextAlignmentOptions.MidlineRight, 17f, false);
        Size(sortHint, 260f, 26f);
        Loc(sortHint, "cozy_tablet_sort", "Sıra: teslim saati");

        ScrollRect list = ScrollList(left, "CargoScrollView", out RectTransform listContent, 8f, 10);
        Size(list, -1f, -1f, 1f, 1f);
        v.listContent = listContent;
        v.template = CargoRowTemplate(listContent).gameObject;

        v.empty = Txt(list.transform, "EmptyListText", "", TextStyle.Body, CozyTheme.InkSoft, TextAlignmentOptions.Center, 22f, true);
        Stretch(v.empty.rectTransform, 40f, 40f, 40f, 40f);
        v.empty.gameObject.SetActive(false);

        // ---- Right: detail ----
        RectTransform right = Node("RightColumn_Detail", root);
        Stretch(right, 672f, 0f, 0f, 0f);
        VStack(right, 18f);
        v.detailRoot = right.gameObject;

        RectTransform head = Node("DetailHeadRow", right);
        HStack(head, 8f);
        Size(head, -1f, 26f);
        v.detailHeader = Txt(head, "DetailLabel", "Teslimat detayı", TextStyle.Label, CozyTheme.InkSoft, TextAlignmentOptions.MidlineLeft, 17f, false);
        Size(v.detailHeader, -1f, 26f, 1f);
        Loc(v.detailHeader, "cozy_tablet_detail", "Teslimat detayı");
        v.tracking = Txt(head, "TrackingCode", "TR-0000", TextStyle.Mono, CozyTheme.InkSoft, TextAlignmentOptions.MidlineRight, 19f, false);
        Size(v.tracking, 220f, 26f);

        v.recipient = Line(right, "RecipientBig", "—", TextStyle.H1, CozyTheme.Ink, 52f, TextAlignmentOptions.MidlineLeft, 40f);

        RectTransform addrRow = Node("AddressRow", right);
        HStack(addrRow, 10f);
        Size(addrRow, -1f, 36f);
        Icon(addrRow, "PinIcon", "pin", 28f, CozyTheme.Red);
        v.address = Txt(addrRow, "AddressLine", "—", TextStyle.BodyBold, CozyTheme.Ink, TextAlignmentOptions.MidlineLeft, 24f, false);
        Size(v.address, -1f, 36f, 1f);

        RectTransform facts = Node("FactTiles", right);
        HorizontalLayoutGroup fl = HStack(facts, 14f);
        fl.childForceExpandWidth = true;
        Size(facts, -1f, 92f);
        v.factType = FactTile(facts, "Fact_Type", "cozy_fact_type", "Tür");
        v.factDeadline = FactTile(facts, "Fact_Deadline", "cozy_fact_deadline", "Son saat");
        v.factReward = FactTile(facts, "Fact_Reward", "cozy_fact_reward", "Ücret");
        v.factStatus = FactTile(facts, "Fact_Status", "cozy_fact_status", "Durum");

        v.hintTitle = Line(right, "HintLabel", "Adres tarifi", TextStyle.Label, CozyTheme.InkSoft, 22f);
        Loc(v.hintTitle, "cozy_tablet_hint", "Adres tarifi");

        RectTransform note = Node("DescriptionNote", right);
        Surface(note, CozyTheme.Hex("FFFBF3"), 18f, CozyTheme.Line, 2f);
        Size(note, -1f, -1f, 1f, 1f);
        v.description = Txt(note, "AddressDescriptionText", "", TextStyle.Body, CozyTheme.Ink, TextAlignmentOptions.TopLeft, 26f, true);
        v.description.lineSpacing = 8f;
        Stretch(v.description.rectTransform, 24f, 18f, 24f, 18f);

        RectTransform tip = Node("TipStrip", right);
        Shape(tip, CozyTheme.SkyTint, 18f);
        HStack(tip, 12f, Pad(18, 18, 0, 0));
        Size(tip, -1f, 64f);
        v.tipIcon = Icon(tip, "TipIcon", "bolt", 28f, CozyTheme.SkyInk);
        v.tipText = Txt(tip, "TipText", "", TextStyle.Body, CozyTheme.SkyInk, TextAlignmentOptions.MidlineLeft, 21f, true, false);
        Size(v.tipText, -1f, 58f, 1f);
        v.tipRoot = tip.gameObject;
        return v;
    }

    private static Button FilterChip(Transform parent, string name, string label, bool active)
    {
        RectTransform rt = Node(name, parent);
        Image bg = Shape(rt, active ? CozyTheme.Ink : new Color(1f, 1f, 1f, 0f), 21f, true);
        Outline o = Border(bg, CozyTheme.KraftDeep, 2f);
        o.enabled = !active;
        Button btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = bg;
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.97f, 0.95f, 0.92f, 1f);
        btn.colors = cb;
        HStack(rt, 0f, Pad(16, 16, 0, 0), TextAnchor.MiddleCenter);
        TextMeshProUGUI t = Txt(rt, "Text", label, TextStyle.BodyBold, active ? CozyTheme.Paper : CozyTheme.InkSoft, TextAlignmentOptions.Center, 18f, false, false);
        t.enableAutoSizing = false;
        Size(t, -1f, 42f);
        Size(rt, -1f, 42f);
        return btn;
    }

    private static TextMeshProUGUI FactTile(Transform parent, string name, string labelKey, string label)
    {
        RectTransform tile = Node(name, parent);
        Surface(tile, Color.white, 18f, CozyTheme.Line, 2f);
        VStack(tile, 4f, Pad(18, 18, 14, 14), TextAnchor.MiddleLeft);
        Size(tile, -1f, 92f, 1f);
        Loc(Line(tile, "Label", label, TextStyle.Label, CozyTheme.InkSoft, 22f), labelKey, label);
        return Line(tile, "Value", "—", TextStyle.H3, CozyTheme.Ink, 32f, TextAlignmentOptions.MidlineLeft, 26f);
    }

    private static RectTransform CargoRowTemplate(Transform content)
    {
        RectTransform row = Node("CargoCardTemplate", content);
        Image face = Shape(row, CozyTheme.RowFace, CozyTheme.RadiusRow, true);
        Outline sel = Border(face, CozyTheme.Ink, 2.5f);
        sel.enabled = false;
        Button btn = row.gameObject.AddComponent<Button>();
        btn.targetGraphic = face;
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.98f, 0.96f, 0.92f, 1f);
        cb.pressedColor = new Color(0.94f, 0.91f, 0.86f, 1f);
        btn.colors = cb;
        HStack(row, 16f, Pad(16, 18, 0, 0));
        Size(row, -1f, 88f);

        Badge(row, "TypeBadge", "box", 52f, CozyTheme.Kraft, CozyTheme.Ink);

        RectTransform texts = Node("Texts", row);
        VStack(texts, 2f, null, TextAnchor.MiddleLeft);
        Size(texts, -1f, 88f, 1f);
        Line(texts, "RecipientName", "Alıcı", TextStyle.H3, CozyTheme.Ink, 30f, TextAlignmentOptions.MidlineLeft, 24f);
        Line(texts, "Address", "Adres", TextStyle.Small, CozyTheme.InkSoft, 24f, TextAlignmentOptions.MidlineLeft, 18f);

        RectTransform right = Node("Right", row);
        VerticalLayoutGroup rl = VStack(right, 4f, null, TextAnchor.MiddleRight);
        rl.childForceExpandWidth = false;
        Size(right, 150f, 88f);
        Chip(right, "StatusChip", "Araçta", CozyTheme.Tone.Mint);
        TextMeshProUGUI dl = Line(right, "Deadline", "18:00", TextStyle.Small, CozyTheme.InkSoft, 22f, TextAlignmentOptions.MidlineRight, 18f);
        Size(dl, 150f, 22f);

        row.gameObject.SetActive(false);
        return row;
    }

    // ---------------------------------------------------------------------

    private struct VehicleView
    {
        public GameObject root, template, detailRoot;
        public Transform listContent;
        public TextMeshProUGUI name, desc, fuelValue, condValue;
        public Image fuelFill, condFill;
        public Button buy, refuel, recall;
    }

    private static VehicleView BuildVehicleView(Transform parent)
    {
        VehicleView v = new VehicleView();
        RectTransform root = Node("VehicleViewRoot", parent);
        Stretch(root);
        v.root = root.gameObject;

        RectTransform left = Node("LeftColumn_Vehicles", root);
        left.anchorMin = new Vector2(0f, 0f);
        left.anchorMax = new Vector2(0f, 1f);
        left.pivot = new Vector2(0f, 0.5f);
        left.sizeDelta = new Vector2(560f, 0f);
        VStack(left, 14f);
        Loc(Line(left, "FleetLabel", "Araç filosu", TextStyle.Label, CozyTheme.InkSoft, 26f), "cozy_tablet_fleet", "Araç filosu");
        ScrollRect list = ScrollList(left, "VehicleScrollView", out RectTransform listContent, 8f, 10);
        Size(list, -1f, -1f, 1f, 1f);
        v.listContent = listContent;

        RectTransform row = Node("VehicleCardTemplate", listContent);
        Image face = Shape(row, CozyTheme.RowFace, CozyTheme.RadiusRow, true);
        Border(face, CozyTheme.Ink, 2.5f).enabled = false;
        Button rowBtn = row.gameObject.AddComponent<Button>();
        rowBtn.targetGraphic = face;
        ColorBlock cb = rowBtn.colors;
        cb.highlightedColor = new Color(0.98f, 0.96f, 0.92f, 1f);
        rowBtn.colors = cb;
        HStack(row, 16f, Pad(16, 18, 0, 0));
        Size(row, -1f, 88f);
        Badge(row, "Badge", "truck", 52f, CozyTheme.SkyTint, CozyTheme.SkyInk);
        RectTransform texts = Node("Texts", row);
        VStack(texts, 2f, null, TextAnchor.MiddleLeft);
        Size(texts, -1f, 88f, 1f);
        Line(texts, "VehicleName", "Araç", TextStyle.H3, CozyTheme.Ink, 30f, TextAlignmentOptions.MidlineLeft, 24f);
        Line(texts, "VehicleSub", "", TextStyle.Small, CozyTheme.InkSoft, 24f, TextAlignmentOptions.MidlineLeft, 18f);
        Chip(row, "StatusChip", "Sende", CozyTheme.Tone.Mint);
        row.gameObject.SetActive(false);
        v.template = row.gameObject;

        RectTransform right = Node("RightColumn_VehicleDetail", root);
        Stretch(right, 592f, 0f, 0f, 0f);
        VStack(right, 18f);
        v.detailRoot = right.gameObject;

        v.name = Line(right, "VehicleName", "—", TextStyle.H1, CozyTheme.Ink, 52f, TextAlignmentOptions.MidlineLeft, 40f);
        RectTransform descBox = Node("DescBox", right);
        Surface(descBox, CozyTheme.Hex("FFFBF3"), 18f, CozyTheme.Line, 2f);
        Size(descBox, -1f, 150f);
        v.desc = Txt(descBox, "DescText", "", TextStyle.Body, CozyTheme.Ink, TextAlignmentOptions.TopLeft, 22f, true);
        Stretch(v.desc.rectTransform, 22f, 16f, 22f, 16f);

        v.fuelFill = DetailGauge(right, "FuelRow", "fuel", CozyTheme.Honey, CozyTheme.Ink, "cozy_vehicle_fuel", "Yakıt", out v.fuelValue);
        v.condFill = DetailGauge(right, "ConditionRow", "wrench", CozyTheme.Mint, Color.white, "cozy_vehicle_condition", "Araç durumu", out v.condValue);

        Spacer(right);
        v.buy = Btn(right, "BuyButton", "Satın al", CozyTheme.ButtonStyle.Primary, 72f, null, 28f);
        v.refuel = Btn(right, "RefuelButton", "Acil yakıt", CozyTheme.ButtonStyle.Warning, 64f, null, 24f);
        v.recall = Btn(right, "RecallButton", "Garaja çağır", CozyTheme.ButtonStyle.Info, 64f, null, 24f);
        return v;
    }

    private static Image DetailGauge(Transform parent, string name, string icon, Color badge, Color badgeInk, string key, string label, out TextMeshProUGUI value)
    {
        RectTransform row = Node(name, parent);
        HStack(row, 16f);
        Size(row, -1f, 60f);
        Badge(row, "Badge", icon, 52f, badge, badgeInk);
        RectTransform col = Node("Col", row);
        VStack(col, 8f, null, TextAnchor.MiddleLeft);
        Size(col, -1f, 60f, 1f);
        RectTransform line = Node("Line", col);
        HStack(line, 8f);
        Size(line, -1f, 30f);
        TextMeshProUGUI t = Txt(line, "Label", label, TextStyle.H3, CozyTheme.Ink, TextAlignmentOptions.MidlineLeft, 24f, false);
        Size(t, -1f, 30f, 1f);
        Loc(t, key, label);
        value = Txt(line, "Value", "", TextStyle.Small, CozyTheme.InkSoft, TextAlignmentOptions.MidlineRight, 18f, false);
        Size(value, 180f, 30f);
        return Bar(col, name + "Bg", 0.7f, badge, 16f);
    }

    // ---------------------------------------------------------------------

    private struct BranchView
    {
        public GameObject root, nextRoot, nextDescBox;
        public TextMeshProUGUI curTitle, curDesc, curCap, curRent, nextTitle, nextDesc, nextCap, nextRent, maxBadge;
        public Button upgrade;
    }

    private static BranchView BuildBranchView(Transform parent)
    {
        BranchView v = new BranchView();
        RectTransform root = Node("BranchViewRoot", parent);
        Stretch(root);
        HorizontalLayoutGroup h = HStack(root, 24f);
        h.childForceExpandWidth = true;
        h.childForceExpandHeight = true;
        v.root = root.gameObject;

        RectTransform cur = BranchColumn(root, "LeftColumn_CurrentBranch", "cozy_branch_current", "Mevcut şube", "home",
            out v.curTitle, out v.curCap, out v.curRent, out _, out v.curDesc);

        RectTransform next = BranchColumn(root, "RightColumn_NextTier", "cozy_branch_next", "Sonraki kademe", "star",
            out v.nextTitle, out v.nextCap, out v.nextRent, out RectTransform nextBox, out v.nextDesc);
        v.nextRoot = next.gameObject;
        v.nextDescBox = nextBox.gameObject;
        v.upgrade = Btn(next, "UpgradeButton", "Şubeyi yükselt", CozyTheme.ButtonStyle.Primary, 72f, null, 26f);
        v.maxBadge = Line(next, "MaxLevelBadge", "En yüksek kademedesin", TextStyle.H3, CozyTheme.HoneyInk, 64f, TextAlignmentOptions.Center, 24f);
        v.maxBadge.gameObject.SetActive(false);
        return v;
    }

    private static RectTransform BranchColumn(Transform parent, string name, string key, string label, string icon,
        out TextMeshProUGUI title, out TextMeshProUGUI cap, out TextMeshProUGUI rent, out RectTransform descBox, out TextMeshProUGUI desc)
    {
        RectTransform col = Node(name, parent);
        Surface(col, CozyTheme.RowFace, 24f, CozyTheme.Line, 2f);
        VStack(col, 12f, Pad(28, 28, 26, 28));
        Size(col, -1f, -1f, 1f, 1f);

        RectTransform head = Node("HeadRow", col);
        HStack(head, 12f);
        Size(head, -1f, 44f);
        Badge(head, "Badge", icon, 44f, CozyTheme.Kraft, CozyTheme.Ink);
        TextMeshProUGUI l = Txt(head, "SectionLabel", label, TextStyle.Label, CozyTheme.InkSoft, TextAlignmentOptions.MidlineLeft, 17f, false);
        Size(l, -1f, 44f, 1f);
        Loc(l, key, label);

        title = Line(col, name == "RightColumn_NextTier" ? "NextTitle" : "BranchTitle", "—", TextStyle.H2, CozyTheme.Ink, 42f, TextAlignmentOptions.MidlineLeft, 30f);
        cap = Line(col, name == "RightColumn_NextTier" ? "NextCapText" : "CapText", "", TextStyle.Body, CozyTheme.Ink, 32f, TextAlignmentOptions.MidlineLeft, 22f);
        rent = Line(col, name == "RightColumn_NextTier" ? "NextRentText" : "RentText", "", TextStyle.Body, CozyTheme.Ink, 32f, TextAlignmentOptions.MidlineLeft, 22f);

        descBox = Node(name == "RightColumn_NextTier" ? "NextDescBox" : "DescBox", col);
        Surface(descBox, CozyTheme.Hex("FFFBF3"), 18f, CozyTheme.Line, 2f);
        Size(descBox, -1f, -1f, 1f, 1f);
        desc = Txt(descBox, "DescText", "", TextStyle.Body, CozyTheme.Ink, TextAlignmentOptions.TopLeft, 21f, true);
        Stretch(desc.rectTransform, 22f, 16f, 22f, 16f);
        return col;
    }
}
#endif
