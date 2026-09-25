#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using static CozyKit;

/// <summary>
/// Rebuilds the whole game UI in the Kurye Defteri style. Each screen replaces the old panel with the same
/// root name (so gameplay scripts keep finding it) and wires every serialized UI reference explicitly.
/// Undo-able with Ctrl+Z; save the scene afterwards.
/// </summary>
public static partial class CozyUIBuilder
{
    // Render order on the main canvas, bottom to top.
    private static readonly string[] SiblingOrder =
    {
        "DeliveryHUD", "CenterCrosshair", "ThrowSlideBG", "InteractionPromptBox", "HeldCargoSideCard", "VehicleDashboardPanel",
        "CargoTabletPanel", "DaySummaryPanel",
        "GarageWorkshopPanel", "InsuranceAgencyPanel", "PassiveDispatchPanel", "PropertyPurchaseModal",
        "BranchUpgradeTransitionPanel", "DeliveryTutorial_Modal_Root", "VehicleTutorial_Modal_Root",
        "PauseMenuPanel", "MainMenuPanel", "SettingsPanel", "GameOver_Modal_Root", "FadeOverlayPanel"
    };

    private static readonly List<string> Report = new List<string>();

    [MenuItem("Tools/Delivery Game/Cozy UI/2. Build ALL Screens (Kurye Defteri)", false, 2)]
    public static void BuildAllMenu()
    {
        if (!EditorUtility.DisplayDialog("Kurye Defteri UI",
            "Sahnedeki tüm oyun arayüzü (HUD, tablet, gün sonu, menüler, ayarlar, rehberler, dükkanlar, iflas ekranı) yeni tasarımla sıfırdan kurulacak.\n\nEski paneller silinir; Ctrl+Z ile geri alınabilir. Devam edilsin mi?",
            "Kur", "İptal")) return;

        Run("Tüm ekranlar", canvas =>
        {
            BuildHUD(canvas);
            BuildTablet(canvas);
            BuildDaySummary(canvas);
            BuildMainMenu(canvas);
            BuildPauseMenu(canvas);
            BuildSettings(canvas);
            BuildFadeOverlay(canvas);
            BuildTutorials(canvas);
            BuildShops(canvas);
            BuildGameOver(canvas);
            BuildBranchTransition(canvas);
        });
    }

    [MenuItem("Tools/Delivery Game/Cozy UI/Screens/HUD", false, 20)] public static void MenuHUD() => Run("HUD", BuildHUD);
    [MenuItem("Tools/Delivery Game/Cozy UI/Screens/Kurye Defteri (Tablet)", false, 21)] public static void MenuTablet() => Run("Tablet", BuildTablet);
    [MenuItem("Tools/Delivery Game/Cozy UI/Screens/Gün Sonu Fişi", false, 22)] public static void MenuSummary() => Run("Gün sonu", BuildDaySummary);
    [MenuItem("Tools/Delivery Game/Cozy UI/Screens/Menüler (Ana, Duraklat, Ayarlar)", false, 23)]
    public static void MenuMenus() => Run("Menüler", c => { BuildMainMenu(c); BuildPauseMenu(c); BuildSettings(c); BuildFadeOverlay(c); });
    [MenuItem("Tools/Delivery Game/Cozy UI/Screens/Rehberler", false, 24)] public static void MenuTutorials() => Run("Rehberler", BuildTutorials);
    [MenuItem("Tools/Delivery Game/Cozy UI/Screens/Dükkanlar (Garaj, Sigorta, Dağıtım, Mülk)", false, 25)] public static void MenuShops() => Run("Dükkanlar", BuildShops);
    [MenuItem("Tools/Delivery Game/Cozy UI/Screens/İflas ve Şube Geçişi", false, 26)] public static void MenuOther() => Run("İflas + şube", c => { BuildGameOver(c); BuildBranchTransition(c); });

    private static void Run(string label, System.Action<Transform> build)
    {
        A = CozyAssetGenerator.EnsureAssets();
        if (A == null || A.display == null)
        {
            EditorUtility.DisplayDialog("Kurye Defteri UI", "Önce 'Generate Assets' çalıştırılmalı; fontlar veya sprite'lar bulunamadı.", "Tamam");
            return;
        }

        Canvas canvas = FindMainCanvas();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Kurye Defteri UI", "Ana Canvas bulunamadı (Delivery_Canvas).", "Tamam");
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName(UndoName);
        int group = Undo.GetCurrentGroup();
        Report.Clear();

        PrepareCanvas(canvas);
        build(canvas.transform);
        ApplySiblingOrder(canvas.transform);

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);

        string summary = string.Join("\n", Report);
        Debug.Log($"<color=#2F9B6F>[Kurye Defteri UI] {label} kuruldu.</color>\n{summary}");
        EditorUtility.DisplayDialog("Kurye Defteri UI", $"{label} kuruldu. Sahneyi kaydetmeyi unutmayın (Ctrl+S).\n\n{summary}", "Tamam");
    }

    // =====================================================================
    // SCENE HELPERS
    // =====================================================================

    private static Canvas FindMainCanvas()
    {
        foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (c.isRootCanvas && c.name == "Delivery_Canvas") return c;
        }
        MainHUDController hud = Object.FindAnyObjectByType<MainHUDController>(FindObjectsInactive.Include);
        if (hud != null) return hud.GetComponentInParent<Canvas>(true);
        return null;
    }

    private static void PrepareCanvas(Canvas canvas)
    {
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = Undo.AddComponent<CanvasScaler>(canvas.gameObject);
        Undo.RecordObject(scaler, UndoName);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 100f;
    }

    /// <summary>Deletes old UI roots (direct children of the canvas) with the given names.</summary>
    private static void Remove(Transform canvas, params string[] names)
    {
        foreach (string n in names)
        {
            // Old builders sometimes created duplicates; remove every direct child with this name.
            for (int i = canvas.childCount - 1; i >= 0; i--)
            {
                Transform child = canvas.GetChild(i);
                if (child.name == n) Undo.DestroyObjectImmediate(child.gameObject);
            }
        }
    }

    private static void ApplySiblingOrder(Transform canvas)
    {
        int index = 0;
        foreach (string n in SiblingOrder)
        {
            Transform t = canvas.Find(n);
            if (t == null) continue;
            Undo.SetTransformParent(t, canvas, UndoName);
            t.SetSiblingIndex(Mathf.Min(index, canvas.childCount - 1));
            index++;
        }
        // Keep the fade overlay above everything else.
        Transform fade = canvas.Find("FadeOverlayPanel");
        if (fade != null) fade.SetAsLastSibling();
    }

    private static T Find<T>() where T : Object
    {
        return Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
    }

    /// <summary>All scene instances of the same component type (some managers exist twice in the scene).</summary>
    private static Object[] SameTypeInstances(Object target)
    {
        if (target == null) return new Object[0];
        return Object.FindObjectsByType(target.GetType(), FindObjectsInactive.Include, FindObjectsSortMode.None);
    }

    private static void Wire(Object target, params (string field, Object value)[] pairs)
    {
        if (target == null)
        {
            Report.Add("  ! Bağlanacak script sahnede bulunamadı (" + (pairs.Length > 0 ? pairs[0].field : "?") + ")");
            return;
        }

        foreach (Object instance in SameTypeInstances(target))
        {
            Undo.RecordObject(instance, UndoName);
            SerializedObject so = new SerializedObject(instance);
            foreach ((string field, Object value) in pairs)
            {
                SerializedProperty p = so.FindProperty(field);
                if (p == null)
                {
                    Debug.LogWarning($"[Kurye Defteri UI] {instance.GetType().Name}.{field} bulunamadı.");
                    continue;
                }
                p.objectReferenceValue = value;
            }
            so.ApplyModifiedProperties();
        }
    }

    /// <summary>Static label that follows the game language.</summary>
    private static TextMeshProUGUI Loc(TextMeshProUGUI t, string key, string fallback)
    {
        LocalizedText lt = t.gameObject.AddComponent<LocalizedText>();
        lt.localizationKey = key;
        lt.fallbackText = fallback;
        t.text = fallback;
        return t;
    }

    private static void SetActive(GameObject go, bool value)
    {
        if (go != null && go.activeSelf != value) go.SetActive(value);
    }

    // =====================================================================
    // 1. HUD
    // =====================================================================

    private static void BuildHUD(Transform canvas)
    {
        Remove(canvas, "DeliveryHUD", "CenterCrosshair", "InteractionPromptBox", "HeldCargoSideCard", "VehicleDashboardPanel", "ThrowSlideBG");

        RectTransform hud = Node("DeliveryHUD", canvas);
        Stretch(hud);

        // --- Status cards (top left) ---
        RectTransform bar = Node("StatusBar", hud);
        Place(bar, Anchor.TL, 48f, 40f, 900f, 84f);
        HorizontalLayoutGroup barLayout = HStack(bar, 16f, null, TextAnchor.UpperLeft);
        barLayout.childForceExpandHeight = true;

        Card clock = PaperCard(bar, "ClockCard", CozyTheme.Paper, 22f, 5f, true, false);
        Size(clock.root, 340f, 84f);
        HStack(clock.face, 14f, Pad(12, 22, 10, 10));
        Badge(clock.face, "SunBadge", "sun", 56f, CozyTheme.Honey, CozyTheme.Ink);
        RectTransform clockCol = Node("Col", clock.face);
        VStack(clockCol, 6f, null, TextAnchor.MiddleLeft);
        Size(clockCol, -1f, 64f, 1f);
        RectTransform clockRow = Node("TimeRow", clockCol);
        HStack(clockRow, 10f);
        Size(clockRow, -1f, 40f);
        TextMeshProUGUI clockText = Txt(clockRow, "ClockText", "09:00", TextStyle.Number, CozyTheme.Ink, TextAlignmentOptions.MidlineLeft, 40f, false);
        Size(clockText, -1f, 40f, 1f);
        TextMeshProUGUI dayText = Txt(clockRow, "DayText", "Gün 1", TextStyle.Small, CozyTheme.InkSoft, TextAlignmentOptions.MidlineRight, 18f, false);
        Size(dayText, 96f, 40f);
        Image dayFill = Bar(clockCol, "DayProgressBg", 0.05f, CozyTheme.Honey, 10f);

        Card cargo = PaperCard(bar, "CargoCard", CozyTheme.Paper, 22f, 5f, true, false);
        Size(cargo.root, 236f, 84f);
        HStack(cargo.face, 14f, Pad(12, 22, 10, 10));
        Badge(cargo.face, "BoxBadge", "box", 56f, CozyTheme.Kraft, CozyTheme.Ink);
        RectTransform cargoCol = Node("Col", cargo.face);
        VStack(cargoCol, 0f, null, TextAnchor.MiddleLeft);
        Size(cargoCol, -1f, 64f, 1f);
        TextMeshProUGUI cargoText = Line(cargoCol, "CargoText", "0 / 0", TextStyle.Number, CozyTheme.Ink, 36f, TextAlignmentOptions.MidlineLeft, 30f);
        Loc(Line(cargoCol, "CargoLabel", "Koli kaldı", TextStyle.Label, CozyTheme.InkSoft, 22f), "cozy_hud_parcels_left", "Koli kaldı");

        Card cash = PaperCard(bar, "BalanceCard", CozyTheme.Paper, 22f, 5f, true, false);
        Size(cash.root, 256f, 84f);
        HStack(cash.face, 14f, Pad(12, 22, 10, 10));
        Badge(cash.face, "CoinBadge", "coin", 56f, CozyTheme.Honey, CozyTheme.Ink);
        RectTransform cashCol = Node("Col", cash.face);
        VStack(cashCol, 0f, null, TextAnchor.MiddleLeft);
        Size(cashCol, -1f, 64f, 1f);
        TextMeshProUGUI cashText = Line(cashCol, "BalanceText", "$0", TextStyle.Number, CozyTheme.Ink, 36f, TextAlignmentOptions.MidlineLeft, 30f);
        Loc(Line(cashCol, "BalanceLabel", "Kasa", TextStyle.Label, CozyTheme.InkSoft, 22f), "cozy_hud_cash", "Kasa");

        // --- Delivery toast (top centre) ---
        Card toast = PaperCard(hud, "NotificationBanner", CozyTheme.Mint, 22f, 5f, true, false);
        Place(toast.root, Anchor.TC, 0f, 40f, 660f, 80f);
        toast.face.GetComponent<Shadow>().effectColor = CozyTheme.MintLip;
        HStack(toast.face, 16f, Pad(14, 26, 12, 12));
        Image toastBadge = Badge(toast.face, "IconBadge", "check", 52f, new Color(1f, 1f, 1f, 0.22f), Color.white);
        TextMeshProUGUI toastText = Txt(toast.face, "Text", "Teslim edildi", TextStyle.H3, Color.white, TextAlignmentOptions.MidlineLeft, 24f, true, false);
        Size(toastText, -1f, 56f, 1f);
        CozyPanelIntro toastIntro = toast.root.gameObject.AddComponent<CozyPanelIntro>();
        toastIntro.style = CozyPanelIntro.Style.SlideDown;
        toastIntro.duration = 0.22f;
        toast.root.gameObject.SetActive(false);

        // --- Key hints (bottom left) ---
        Card hints = PaperCard(hud, "KeyHints", CozyTheme.Paper, 22f, 5f, true, false);
        Place(hints.root, Anchor.BL, 48f, 48f, 560f, 66f);
        HStack(hints.face, 24f, Pad(18, 22, 0, 0));
        AddHint(hints.face, "Tab", "cozy_hint_tablet", "Defter");
        AddHint(hints.face, "F", "cozy_hint_vehicle", "Araca bin");
        AddHint(hints.face, "F1", "cozy_hint_guide", "Rehber");

        // --- Crosshair ---
        RectTransform cross = Node("CenterCrosshair", canvas);
        Place(cross, Anchor.MC, 0f, 0f, 12f, 12f);
        Image crossImg = cross.gameObject.AddComponent<Image>();
        crossImg.sprite = A.circle;
        crossImg.color = Color.white;
        crossImg.raycastTarget = false;
        Border(crossImg, new Color(CozyTheme.Ink.r, CozyTheme.Ink.g, CozyTheme.Ink.b, 0.55f), 2.5f);

        // --- Interaction prompt (below crosshair, grows with its text) ---
        Card prompt = PaperCard(canvas, "InteractionPromptBox", CozyTheme.Paper, 22f, 5f, true, false);
        Place(prompt.root, Anchor.MC, 0f, -110f, 420f, 64f);
        TextMeshProUGUI promptText = Txt(prompt.face, "PromptText", "[E] Koliyi al", TextStyle.H3, CozyTheme.Ink, TextAlignmentOptions.Center, 24f, false);
        Stretch(promptText.rectTransform, 22f, 0f, 22f, 0f);
        prompt.root.gameObject.SetActive(false);

        // --- Throw charge ---
        Card throwCard = PaperCard(canvas, "ThrowSlideBG", CozyTheme.Paper, 22f, 5f, true, false);
        Place(throwCard.root, Anchor.MC, 0f, -196f, 340f, 72f);
        VStack(throwCard.face, 8f, Pad(20, 20, 12, 12), TextAnchor.MiddleCenter);
        Loc(Line(throwCard.face, "Label", "Fırlatma gücü", TextStyle.Label, CozyTheme.InkSoft, 18f, TextAlignmentOptions.Center), "cozy_throw_power", "Fırlatma gücü");
        Slider throwSlider = ThrowBar(throwCard.face, out Image throwFill);
        throwCard.root.gameObject.SetActive(false);

        // --- Held package label (right) ---
        BuildHeldCargoCard(canvas, out HeldCard held);

        // --- Vehicle dashboard (bottom right) ---
        Card dash = PaperCard(canvas, "VehicleDashboardPanel", CozyTheme.Paper, 28f, 7f, true, false);
        Place(dash.root, Anchor.BR, 48f, 48f, 430f, 172f);
        VStack(dash.face, 16f, Pad(24, 24, 22, 22));
        Gauge(dash.face, "FuelGaugeCard", "fuel", CozyTheme.Honey, CozyTheme.Ink, "FuelTitle", "Yakıt", "FuelValue", "FuelBarBg", CozyTheme.Honey,
            out TextMeshProUGUI fuelTitle, out TextMeshProUGUI fuelValue, out Image fuelFill, out RectTransform fuelRow);
        Gauge(dash.face, "ConditionGaugeCard", "wrench", CozyTheme.Mint, Color.white, "CondTitle", "Araç durumu", "CondValue", "CondBarBg", CozyTheme.Mint,
            out TextMeshProUGUI condTitle, out TextMeshProUGUI condValue, out Image condFill, out RectTransform condRow);
        dash.root.gameObject.SetActive(false);

        // --- Wire scripts ---
        Wire(Find<MainHUDController>(),
            ("hudRoot", hud.gameObject), ("clockText", clockText), ("dayText", dayText), ("dayProgressFill", dayFill),
            ("remainingCargoText", cargoText), ("balanceEarningsText", cashText));

        Wire(Find<DeliveryNotificationHUD>(),
            ("notificationRoot", toast.root.gameObject), ("notificationText", toastText), ("notificationBackground", toast.faceImage),
            ("notificationIcon", toastBadge.transform.Find("Icon").GetComponent<Image>()), ("notificationLip", toast.face.GetComponent<Shadow>()),
            ("remainingCargoCounterText", null));

        Wire(Find<InteractionPromptHUD>(),
            ("crosshairDot", cross.gameObject), ("promptPanel", prompt.root.gameObject), ("promptText", promptText),
            ("throwSlideBG", throwCard.root.gameObject), ("throwBarFill", throwFill), ("throwBarSlider", throwSlider), ("throwBarRect", (RectTransform)throwSlider.transform),
            ("heldCargoPanel", held.root), ("heldCargoHeaderText", held.header), ("heldCargoTypeText", held.type), ("heldCargoStatusText", held.status),
            ("heldCargoPercentageText", held.percentage), ("heldCargoRecipientLabelText", held.recipientLabel), ("heldCargoRecipientText", held.recipient),
            ("heldCargoAddressLabelText", held.addressLabel), ("heldCargoAddressText", held.address), ("heldCargoClueLabelText", held.clueLabel),
            ("heldCargoAddressDescText", held.clue), ("heldCargoRewardLabelText", held.rewardLabel), ("heldCargoRewardText", held.reward),
            ("heldCargoPenaltyLabelText", held.penaltyLabel), ("heldCargoPenaltyText", held.penalty), ("heldCargoActionHintText", held.hint),
            ("heldCargoTypeChip", held.typeChip), ("heldCargoTypeIcon", held.typeIcon), ("heldCargoConditionChip", held.conditionChip),
            ("vehicleDashboardRoot", dash.root.gameObject), ("fuelGaugePanel", fuelRow.gameObject), ("fuelLabelText", fuelTitle),
            ("fuelBarFill", fuelFill), ("fuelValueText", fuelValue), ("conditionGaugePanel", condRow.gameObject),
            ("conditionLabelText", condTitle), ("conditionBarFill", condFill), ("conditionValueText", condValue));

        Report.Add("• HUD: saat/koli/kasa kartları, bildirim, tuş ipuçları, etkileşim, koli etiketi, araç göstergeleri");
    }

    private static void AddHint(Transform parent, string key, string locKey, string fallback)
    {
        RectTransform group = Node("Hint_" + key, parent);
        HStack(group, 8f);
        KeyCap(group, key, 40f);
        TextMeshProUGUI t = Txt(group, "Text", fallback, TextStyle.Small, CozyTheme.Ink, TextAlignmentOptions.MidlineLeft, 19f, false);
        Size(t, -1f, 40f);
        t.enableAutoSizing = false;
        Loc(t, locKey, fallback);
    }

    private static Slider ThrowBar(Transform parent, out Image fill)
    {
        RectTransform rt = Node("ThrowBar", parent);
        Size(rt, -1f, 16f, 1f);
        Slider s = rt.gameObject.AddComponent<Slider>();
        s.interactable = false;
        s.transition = Selectable.Transition.None;
        Shape(rt, CozyTheme.Track, 8f);
        RectTransform area = Node("Fill Area", rt);
        Stretch(area);
        RectTransform f = Node("Fill", area);
        f.anchorMin = Vector2.zero;
        f.anchorMax = new Vector2(0.6f, 1f);
        f.sizeDelta = Vector2.zero;
        fill = Shape(f, CozyTheme.Honey, 8f);
        s.fillRect = f;
        s.minValue = 0f;
        s.maxValue = 1f;
        s.value = 0.6f;
        return s;
    }

    private static void Gauge(Transform parent, string rowName, string icon, Color badge, Color badgeInk, string titleName, string title,
        string valueName, string barName, Color fillColor, out TextMeshProUGUI titleText, out TextMeshProUGUI valueText, out Image fill, out RectTransform row)
    {
        row = Node(rowName, parent);
        HStack(row, 14f);
        Size(row, -1f, 52f);
        Badge(row, "Badge", icon, 48f, badge, badgeInk);
        RectTransform col = Node("Col", row);
        VStack(col, 8f, null, TextAnchor.MiddleLeft);
        Size(col, -1f, 52f, 1f);
        RectTransform line = Node("Line", col);
        HStack(line, 8f);
        Size(line, -1f, 28f);
        titleText = Txt(line, titleName, title, TextStyle.H3, CozyTheme.Ink, TextAlignmentOptions.MidlineLeft, 22f, false);
        Size(titleText, -1f, 28f, 1f);
        valueText = Txt(line, valueName, "100%", TextStyle.Small, CozyTheme.InkSoft, TextAlignmentOptions.MidlineRight, 18f, false);
        Size(valueText, 130f, 28f);
        fill = Bar(col, barName, 0.8f, fillColor, 14f);
    }

    private struct HeldCard
    {
        public GameObject root, conditionChip;
        public Image typeChip, typeIcon;
        public TextMeshProUGUI header, type, status, percentage, recipientLabel, recipient, addressLabel, address,
            clueLabel, clue, rewardLabel, reward, penaltyLabel, penalty, hint;
    }

    private static void BuildHeldCargoCard(Transform canvas, out HeldCard h)
    {
        h = new HeldCard();
        Card card = PaperCard(canvas, "HeldCargoSideCard", CozyTheme.Paper, 28f, 7f, true, false);
        Place(card.root, Anchor.TR, 48f, 160f, 440f, 660f);
        h.root = card.root.gameObject;
        VStack(card.face, 0f);

        // Kraft header strip with the type chip
        RectTransform strip = Node("HeaderStrip", card.face);
        Size(strip, -1f, 64f);
        Shape(strip, CozyTheme.Kraft, 28f);
        RectTransform square = Node("SquareBottom", strip);
        square.anchorMin = new Vector2(0f, 0f);
        square.anchorMax = new Vector2(1f, 0.5f);
        square.offsetMin = Vector2.zero;
        square.offsetMax = Vector2.zero;
        Shape(square, CozyTheme.Kraft, 0f);
        IgnoreLayout(square);
        HStack(strip, 10f, Pad(24, 18, 0, 0));
        h.header = Txt(strip, "HeaderText", "Eldeki koli", TextStyle.Label, CozyTheme.Ink, TextAlignmentOptions.MidlineLeft, 17f, false);
        Size(h.header, -1f, 64f, 1f);
        h.typeChip = Chip(strip, "TypeChip", "Standart", CozyTheme.Tone.Sky, "box");
        h.type = h.typeChip.transform.Find("Text").GetComponent<TextMeshProUGUI>();
        h.typeIcon = h.typeChip.transform.Find("Icon").GetComponent<Image>();

        RectTransform body = Node("Body", card.face);
        VStack(body, 14f, Pad(24, 24, 18, 22));
        Size(body, -1f, -1f, 1f, 1f);

        // Condition / deadline chip (hidden for standard parcels)
        RectTransform chipRow = Node("ChipRow", body);
        HStack(chipRow, 8f);
        Size(chipRow, -1f, 34f);
        RectTransform cond = Node("ConditionChip", chipRow);
        Shape(cond, CozyTheme.Well, 17f);
        Border(cond.GetComponent<Image>(), CozyTheme.Line, 2f);
        HStack(cond, 8f, Pad(14, 14, 0, 0), TextAnchor.MiddleCenter);
        ContentSizeFitter condFit = cond.gameObject.AddComponent<ContentSizeFitter>();
        condFit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        Size(cond, -1f, 34f);
        h.status = Txt(cond, "StatusText", "13:00", TextStyle.Small, CozyTheme.Ink, TextAlignmentOptions.MidlineLeft, 17f, false);
        h.status.enableAutoSizing = false;
        Size(h.status, -1f, 34f);
        h.percentage = Txt(cond, "PercentageText", "", TextStyle.Small, CozyTheme.InkSoft, TextAlignmentOptions.MidlineLeft, 17f, false);
        h.percentage.enableAutoSizing = false;
        Size(h.percentage, -1f, 34f);
        h.conditionChip = cond.gameObject;

        RectTransform rec = Node("RecipientGroup", body);
        VStack(rec, 2f);
        h.recipientLabel = Line(rec, "RecipientLabel", "Alıcı", TextStyle.Label, CozyTheme.InkSoft, 20f);
        h.recipient = Line(rec, "RecipientValue", "—", TextStyle.H2, CozyTheme.Ink, 38f, TextAlignmentOptions.MidlineLeft, 30f);

        RectTransform addr = Node("AddressGroup", body);
        VStack(addr, 2f);
        h.addressLabel = Line(addr, "AddressLabel", "Adres", TextStyle.Label, CozyTheme.InkSoft, 20f);
        RectTransform addrRow = Node("AddressRow", addr);
        HStack(addrRow, 10f);
        Size(addrRow, -1f, 32f);
        Icon(addrRow, "PinIcon", "pin", 26f, CozyTheme.Red);
        h.address = Txt(addrRow, "AddressValue", "—", TextStyle.BodyBold, CozyTheme.Ink, TextAlignmentOptions.MidlineLeft, 22f, false);
        Size(h.address, -1f, 32f, 1f);

        h.clueLabel = Line(body, "ClueLabel", "Adres tarifi", TextStyle.Label, CozyTheme.InkSoft, 20f);
        RectTransform note = Node("ClueNote", body);
        Surface(note, CozyTheme.Hex("FFFBF3"), 18f, CozyTheme.Line, 2f);
        Size(note, -1f, 140f);
        h.clue = Txt(note, "AddressDescription", "", TextStyle.Body, CozyTheme.Ink, TextAlignmentOptions.TopLeft, 21f, true);
        Stretch(h.clue.rectTransform, 16f, 12f, 16f, 12f);

        RectTransform money = Node("MoneyRow", body);
        HStack(money, 12f);
        Size(money, -1f, 60f);
        h.rewardLabel = MoneyTile(money, "Reward", "Ücret", CozyTheme.MintInk, out h.reward);
        h.penaltyLabel = MoneyTile(money, "Penalty", "Yanlış adres", CozyTheme.RedInk, out h.penalty);

        h.hint = Txt(body, "ActionHint", "[Sol Tık] Fırlat  ·  [Sağ Tık] Bırak", TextStyle.Small, CozyTheme.InkSoft, TextAlignmentOptions.MidlineLeft, 18f, true);
        Size(h.hint, -1f, 52f);

        card.root.gameObject.SetActive(false);
    }

    private static TextMeshProUGUI MoneyTile(Transform parent, string prefix, string label, Color valueColor, out TextMeshProUGUI value)
    {
        RectTransform tile = Node(prefix + "Group", parent);
        VStack(tile, 2f, null, TextAnchor.MiddleLeft);
        Size(tile, -1f, 60f, 1f);
        TextMeshProUGUI l = Line(tile, prefix + "Label", label, TextStyle.Label, CozyTheme.InkSoft, 20f);
        value = Line(tile, prefix + "Value", "$0", TextStyle.H2, valueColor, 36f, TextAlignmentOptions.MidlineLeft, 30f);
        return l;
    }
}
#endif
