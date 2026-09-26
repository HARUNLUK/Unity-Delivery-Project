#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static CozyKit;

public static partial class CozyUIBuilder
{
    // =====================================================================
    // SHARED MODAL PIECES
    // =====================================================================

    /// <summary>Full-screen root with a soft scrim and a centred paper card that grows in when opened.</summary>
    private static Card Modal(Transform parent, string rootName, float width, float height, out RectTransform root, Color? scrim = null, float offsetX = 0f)
    {
        root = Node(rootName, parent);
        Stretch(root);
        Scrim(root, "Scrim", scrim ?? CozyTheme.Scrim);
        Card card = PaperCard(root, "Card", CozyTheme.Paper);
        Place(card.root, Anchor.MC, offsetX, 0f, width, height);
        card.root.gameObject.AddComponent<CozyPanelIntro>();
        return card;
    }

    /// <summary>Badge + title (+ optional subtitle) + optional round close button.</summary>
    private static RectTransform HeaderRow(Transform face, string icon, Color badge, Color badgeInk,
        string titleName, string title, string titleKey, out TextMeshProUGUI titleText,
        string subtitleName, string subtitle, out TextMeshProUGUI subtitleText, string closeName, out Button close)
    {
        RectTransform row = Node("HeaderRow", face);
        HStack(row, 18f);
        Size(row, -1f, 76f);
        Badge(row, "HeaderBadge", icon, 64f, badge, badgeInk);

        RectTransform col = Node("TitleCol", row);
        VStack(col, 2f, null, TextAnchor.MiddleLeft);
        Size(col, -1f, 76f, 1f);
        titleText = Line(col, titleName, title, TextStyle.H1, CozyTheme.Ink, subtitleName != null ? 46f : 60f, TextAlignmentOptions.MidlineLeft, 38f);
        if (titleKey != null) Loc(titleText, titleKey, title);
        subtitleText = subtitleName != null
            ? Line(col, subtitleName, subtitle, TextStyle.Body, CozyTheme.InkSoft, 28f, TextAlignmentOptions.MidlineLeft, 20f)
            : null;

        close = closeName != null ? RoundButton(row, closeName, "x", CozyTheme.ButtonStyle.Soft, 56f) : null;
        return row;
    }

    private static void AddPersistentClick(Button btn, UnityAction action)
    {
        if (btn == null || action == null) return;
        UnityEventTools.AddPersistentListener(btn.onClick, action);
    }

    // =====================================================================
    // 3. DAY SUMMARY (RECEIPT)
    // =====================================================================

    private static void BuildDaySummary(Transform canvas)
    {
        Remove(canvas, "DaySummaryPanel");
        RectTransform root = Node("DaySummaryPanel", canvas);
        Stretch(root);
        Scrim(root, "Scrim", CozyTheme.ScrimStrong);

        // ---- Receipt ----
        RectTransform receipt = Node("Receipt", root);
        Place(receipt, Anchor.MC, -380f, 0f, 660f, 968f);
        receipt.gameObject.AddComponent<CozyPanelIntro>();
        RectTransform rShadow = Node("Shadow", receipt);
        Stretch(rShadow, -26f, -14f, -26f, -40f);
        Image rsi = rShadow.gameObject.AddComponent<Image>();
        rsi.sprite = A.softShadow;
        rsi.type = Image.Type.Sliced;
        rsi.color = CozyTheme.SoftShadow;
        rsi.raycastTarget = false;

        Color receiptPaper = CozyTheme.Hex("FFFDF6");
        RectTransform top = Node("TopTeeth", receipt);
        TopStrip(top, 14f);
        Image topImg = top.gameObject.AddComponent<Image>();
        topImg.sprite = A.zigzag;
        topImg.type = Image.Type.Tiled;
        topImg.color = receiptPaper;
        topImg.raycastTarget = false;

        RectTransform paper = Node("Paper", receipt);
        Stretch(paper, 0f, 14f, 0f, 14f);
        Image paperImg = paper.gameObject.AddComponent<Image>();
        paperImg.color = receiptPaper;
        VStack(paper, 16f, Pad(48, 48, 36, 36));

        // Flipped around its own centre so it stays glued to the paper's bottom edge.
        RectTransform bottom = Node("BottomTeeth", receipt);
        bottom.anchorMin = new Vector2(0f, 0f);
        bottom.anchorMax = new Vector2(1f, 0f);
        bottom.pivot = new Vector2(0.5f, 0.5f);
        bottom.offsetMin = new Vector2(0f, 0f);
        bottom.offsetMax = new Vector2(0f, 14f);
        bottom.localEulerAngles = new Vector3(0f, 0f, 180f);
        Image botImg = bottom.gameObject.AddComponent<Image>();
        botImg.sprite = A.zigzag;
        botImg.type = Image.Type.Tiled;
        botImg.color = receiptPaper;
        botImg.raycastTarget = false;

        Line(paper, "ShopLine", "WHERE'S MY PACKAGE · KURYE ŞUBESİ", TextStyle.Mono, CozyTheme.InkSoft, 24f, TextAlignmentOptions.Center, 17f);
        TextMeshProUGUI title = Line(paper, "HeaderTitleText", "Gün sonu fişi", TextStyle.H1, CozyTheme.Ink, 50f, TextAlignmentOptions.Center, 40f);
        TextMeshProUGUI subtitle = Line(paper, "TotalDeliveredText", "", TextStyle.Mono, CozyTheme.InkSoft, 26f, TextAlignmentOptions.Center, 18f);
        DashedLine(paper, "Divider1", CozyTheme.KraftDeep, 3f);

        RectTransform lines = Node("Lines", paper);
        VStack(lines, 12f);
        RectTransform lineTpl = Node("LineTemplate", lines);
        HorizontalLayoutGroup lh = HStack(lineTpl, 10f);
        lh.childAlignment = TextAnchor.MiddleLeft;
        Size(lineTpl, -1f, 32f);
        TextMeshProUGUI ll = Txt(lineTpl, "Label", "Doğru teslimat ×0", TextStyle.Mono, CozyTheme.Ink, TextAlignmentOptions.MidlineLeft, 22f, false);
        Size(ll, -1f, 32f);
        RectTransform dotsHolder = Node("Dots", lineTpl);
        Size(dotsHolder, -1f, 32f, 1f);
        Image dots = DashedLine(dotsHolder, "Leader", CozyTheme.KraftDeep, 3f);
        Place((RectTransform)dots.transform, Anchor.BL, 0f, 8f, 0f, 3f);
        ((RectTransform)dots.transform).anchorMax = new Vector2(1f, 0f);
        TextMeshProUGUI la = Txt(lineTpl, "Amount", "+$0", TextStyle.Mono, CozyTheme.MintInk, TextAlignmentOptions.MidlineRight, 22f, false);
        Size(la, -1f, 32f);
        lineTpl.gameObject.SetActive(false);

        DashedLine(paper, "Divider2", CozyTheme.KraftDeep, 3f);

        RectTransform profitRow = Node("ProfitRow", paper);
        HStack(profitRow, 12f);
        Size(profitRow, -1f, 70f);
        TextMeshProUGUI profitLabel = Txt(profitRow, "ProfitLabel", "Günün kârı", TextStyle.H2, CozyTheme.Ink, TextAlignmentOptions.MidlineLeft, 30f, false);
        Loc(profitLabel, "cozy_receipt_profit", "Günün kârı");
        Size(profitLabel, -1f, 70f, 1f);
        TextMeshProUGUI profit = Txt(profitRow, "ProfitValue", "+$0", TextStyle.Display, CozyTheme.MintInk, TextAlignmentOptions.MidlineRight, 56f, false);
        Size(profit, 280f, 70f);

        TextMeshProUGUI vault = KeyValueRow(paper, "VaultRow", "cozy_receipt_vault", "Kasa", TextStyle.H2, 30f);
        TextMeshProUGUI xp = KeyValueRow(paper, "XpRow", "cozy_receipt_xp", "Tecrübe", TextStyle.H3, 24f);

        TextMeshProUGUI note = Txt(paper, "EmergencyNote", "", TextStyle.Small, CozyTheme.RedInk, TextAlignmentOptions.TopLeft, 18f, true);
        Size(note, -1f, 60f);
        note.gameObject.SetActive(false);
        Spacer(paper);

        // Postmark stamp
        RectTransform stamp = Node("Stamp", paper);
        IgnoreLayout(stamp);
        Place(stamp, Anchor.TR, 40f, 700f, 190f, 190f);
        stamp.localEulerAngles = new Vector3(0f, 0f, 14f);
        CanvasGroup sg = stamp.gameObject.AddComponent<CanvasGroup>();
        sg.alpha = 0.85f;
        sg.blocksRaycasts = false;
        Image ring = stamp.gameObject.AddComponent<Image>();
        ring.sprite = A.circle;
        ring.color = CozyTheme.Red;
        ring.raycastTarget = false;
        RectTransform ringInner = Node("Inner", stamp);
        Stretch(ringInner, 6f, 6f, 6f, 6f);
        Image ri = ringInner.gameObject.AddComponent<Image>();
        ri.sprite = A.circle;
        ri.color = receiptPaper;
        ri.raycastTarget = false;
        TextMeshProUGUI stampText = Txt(stamp, "StampText", "GÜN 1\nKAPANDI", TextStyle.Display, CozyTheme.Red, TextAlignmentOptions.Center, 26f, true, false);
        stampText.lineSpacing = -10f;
        Stretch(stampText.rectTransform, 18f, 18f, 18f, 18f);

        // ---- History ----
        Card hist = PaperCard(root, "HistoryCard", CozyTheme.Paper);
        Place(hist.root, Anchor.MC, 355f, 0f, 710f, 888f);
        hist.root.gameObject.AddComponent<CozyPanelIntro>();
        VStack(hist.face, 16f, Pad(32, 32, 30, 32));
        RectTransform hHead = Node("HeadRow", hist.face);
        HStack(hHead, 12f);
        Size(hHead, -1f, 44f);
        TextMeshProUGUI hTitle = Txt(hHead, "HistoryTitle", "Teslimat geçmişi", TextStyle.H2, CozyTheme.Ink, TextAlignmentOptions.MidlineLeft, 30f, false);
        Size(hTitle, -1f, 44f, 1f);
        Loc(hTitle, "cozy_history_title", "Teslimat geçmişi");
        Image countChip = Chip(hHead, "CountChip", "0 / 0", CozyTheme.Tone.Mint);
        TextMeshProUGUI countText = countChip.transform.Find("Text").GetComponent<TextMeshProUGUI>();

        ScrollRect list = ScrollList(hist.face, "HistoryScrollView", out RectTransform listContent, 8f, 10);
        Size(list, -1f, -1f, 1f, 1f);
        RectTransform row = Node("HistoryRowTemplate", listContent);
        Shape(row, CozyTheme.RowFace, CozyTheme.RadiusRow);
        HStack(row, 14f, Pad(16, 20, 0, 0));
        Size(row, -1f, 80f);
        Badge(row, "StatusBadge", "check", 44f, CozyTheme.Mint, Color.white);
        RectTransform texts = Node("Texts", row);
        VStack(texts, 2f, null, TextAnchor.MiddleLeft);
        Size(texts, -1f, 80f, 1f);
        Line(texts, "Recipient", "Alıcı", TextStyle.H3, CozyTheme.Ink, 30f, TextAlignmentOptions.MidlineLeft, 23f);
        Line(texts, "Detail", "", TextStyle.Small, CozyTheme.InkSoft, 24f, TextAlignmentOptions.MidlineLeft, 17f);
        TextMeshProUGUI amount = Txt(row, "Amount", "+$0", TextStyle.H3, CozyTheme.MintInk, TextAlignmentOptions.MidlineRight, 24f, false);
        Size(amount, 120f, 80f);
        row.gameObject.SetActive(false);

        Button restart = Btn(hist.face, "RestartDayButton", "Sonraki güne başla", CozyTheme.ButtonStyle.Primary, 76f, "play", 28f);

        root.gameObject.SetActive(false);

        DaySummaryManager dsm = Find<DaySummaryManager>();
        Wire(dsm,
            ("summaryPanelRoot", root.gameObject), ("headerTitleText", title), ("totalDeliveredText", subtitle),
            ("correctDeliveriesText", null), ("wrongDeliveriesText", null), ("netEarningsText", null), ("progressionInfoText", null),
            ("historyListContent", listContent), ("historyItemTemplate", row.gameObject), ("restartDayButton", restart),
            ("receiptLinesContent", lines), ("receiptLineTemplate", lineTpl.gameObject), ("netProfitText", profit),
            ("vaultText", vault), ("xpText", xp), ("stampText", stampText), ("receiptNoteText", note), ("historyCountText", countText));
        SetBool(dsm, "useKuryeDefteriLayout", true);

        Report.Add("• Gün sonu fişi: kalemler, damga, teslimat geçmişi");
    }

    private static TextMeshProUGUI KeyValueRow(Transform parent, string name, string key, string label, TextStyle valueStyle, float valueSize)
    {
        RectTransform row = Node(name, parent);
        HStack(row, 12f);
        Size(row, -1f, 40f);
        TextMeshProUGUI l = Txt(row, "Label", label, TextStyle.Body, CozyTheme.InkSoft, TextAlignmentOptions.MidlineLeft, 22f, false);
        Size(l, -1f, 40f, 1f);
        Loc(l, key, label);
        TextMeshProUGUI v = Txt(row, "Value", "", valueStyle, CozyTheme.Ink, TextAlignmentOptions.MidlineRight, valueSize, false);
        Size(v, 260f, 40f);
        return v;
    }

    private static void SetBool(UnityEngine.Object target, string field, bool value)
    {
        foreach (UnityEngine.Object instance in SameTypeInstances(target))
        {
            UnityEditor.SerializedObject so = new UnityEditor.SerializedObject(instance);
            UnityEditor.SerializedProperty p = so.FindProperty(field);
            if (p == null) continue;
            p.boolValue = value;
            so.ApplyModifiedProperties();
        }
    }

    // =====================================================================
    // 4. MAIN MENU + NEW GAME MODAL
    // =====================================================================

    private static void BuildMainMenu(Transform canvas)
    {
        Remove(canvas, "MainMenuPanel");
        RectTransform root = Node("MainMenuPanel", canvas);
        Stretch(root);
        Image veil = root.gameObject.AddComponent<Image>();
        veil.color = new Color(0.17f, 0.12f, 0.23f, 0.22f);
        veil.raycastTarget = true;

        Card card = PaperCard(root, "LeftContentCard", CozyTheme.Paper);
        Place(card.root, Anchor.TL, 120f, 130f, 600f, 820f);
        card.root.gameObject.AddComponent<CozyPanelIntro>();
        VStack(card.face, 18f, Pad(48, 48, 46, 40));

        RectTransform tagRow = Node("TagRow", card.face);
        HStack(tagRow, 0f);
        Size(tagRow, -1f, 34f);
        Image tag = Chip(tagRow, "TagChip", "Kurye simülasyonu", CozyTheme.Tone.Kraft, "box");
        Loc(tag.transform.Find("Text").GetComponent<TextMeshProUGUI>(), "cozy_menu_tagline", "Kurye simülasyonu");

        TextMeshProUGUI title = Txt(card.face, "TitleHeader", "Where's\nMy Package?", TextStyle.Display, CozyTheme.Ink, TextAlignmentOptions.TopLeft, 72f, true);
        title.lineSpacing = -12f;
        Size(title, -1f, 170f);
        DashedLine(card.face, "Divider", CozyTheme.Line, 3f);

        RectTransform buttons = Node("ButtonsColumn", card.face);
        VStack(buttons, 16f);
        Button cont = Btn(buttons, "ContinueButton", "Devam et", CozyTheme.ButtonStyle.Primary, 80f, "play", 28f, true);
        Button newGame = Btn(buttons, "NewGameButton", "Yeni oyun", CozyTheme.ButtonStyle.Soft, 64f, "plus", 26f, true);
        Button settings = Btn(buttons, "SettingsButton", "Ayarlar", CozyTheme.ButtonStyle.Soft, 64f, "gear", 26f, true);
        Button quit = Btn(buttons, "QuitButton", "Masaüstüne çık", CozyTheme.ButtonStyle.Ghost, 60f, "door", 24f, true);
        Spacer(card.face);
        Loc(Line(card.face, "Footer", "F1 rehber · ESC duraklat", TextStyle.Small, CozyTheme.InkSoft, 26f), "cozy_menu_footer", "F1 rehber · ESC duraklat");

        // Save tag (luggage label)
        Card saveTag = PaperCard(root, "SaveInfoBadge", CozyTheme.Paper, 24f, 6f);
        Place(saveTag.root, Anchor.BR, 140f, 120f, 540f, 232f);
        saveTag.root.localEulerAngles = new Vector3(0f, 0f, 4f);
        RectTransform hole = Node("TagHole", saveTag.face);
        Place(hole, Anchor.ML, 22f, 0f, 24f, 24f);
        Image holeImg = hole.gameObject.AddComponent<Image>();
        holeImg.sprite = A.circle;
        holeImg.color = new Color(0.72f, 0.64f, 0.84f, 1f);
        Border(holeImg, CozyTheme.KraftDeep, 3f);
        RectTransform tagBody = Node("Body", saveTag.face);
        Stretch(tagBody, 68f, 22f, 26f, 24f);
        VStack(tagBody, 14f);

        RectTransform tagHead = Node("HeadRow", tagBody);
        HStack(tagHead, 10f);
        Size(tagHead, -1f, 36f);
        TextMeshProUGUI saveTitle = Txt(tagHead, "Label", "Son kayıt", TextStyle.Label, CozyTheme.InkSoft, TextAlignmentOptions.MidlineLeft, 18f, false);
        Size(saveTitle, -1f, 36f, 1f);

        RectTransform stats = Node("Stats", tagBody);
        HorizontalLayoutGroup statsLayout = HStack(stats, 10f);
        statsLayout.childForceExpandHeight = true;
        Size(stats, -1f, -1f, 1f, 1f);
        TextMeshProUGUI saveDay = StatTile(stats, "DayTile", "sun", CozyTheme.Honey, CozyTheme.Ink, "cozy_menu_day", "Gün", "1", CozyTheme.Ink, 96f, 0f);
        TextMeshProUGUI saveBranch = StatTile(stats, "BranchTile", "home", CozyTheme.Kraft, CozyTheme.Ink, "cozy_menu_branch", "Şube", "Seviye 1", CozyTheme.Ink, -1f, 1f);
        TextMeshProUGUI saveCash = StatTile(stats, "CashTile", "coin", CozyTheme.MintTint, CozyTheme.MintInk, "cozy_hud_cash", "Kasa", "$0", CozyTheme.MintInk, 132f, 0f);

        // New game confirmation
        Card modal = Modal(root, "NewGameConfirmModal", 880f, 460f, out RectTransform modalRoot);
        VStack(modal.face, 20f, Pad(44, 44, 38, 36));
        HeaderRow(modal.face, "alert", CozyTheme.RedTint, CozyTheme.RedInk, "Title", "Yeni oyun başlat", "modal_new_game_title",
            out _, null, null, out _, null, out _);
        TextMeshProUGUI body = Txt(modal.face, "BodyText", "", TextStyle.Body, CozyTheme.Ink, TextAlignmentOptions.TopLeft, 24f, true);
        Size(body, -1f, -1f, 1f, 1f);
        Loc(body, "modal_new_game_body", "Mevcut kayıt ve tüm şube ilerlemen sıfırlanacak. Emin misin?");
        RectTransform btnRow = Node("ButtonRow", modal.face);
        HorizontalLayoutGroup br = HStack(btnRow, 18f);
        br.childForceExpandWidth = true;
        Size(btnRow, -1f, 68f);
        Button cancel = Btn(btnRow, "CancelBtn", "İptal", CozyTheme.ButtonStyle.Soft, 68f, null, 26f);
        Size(cancel, -1f, 68f, 1f);
        Button confirm = Btn(btnRow, "ConfirmBtn", "Evet, sıfırla", CozyTheme.ButtonStyle.Danger, 68f, null, 26f);
        Size(confirm, -1f, 68f, 1f);
        modalRoot.gameObject.SetActive(false);

        GameMenuManager gm = Find<GameMenuManager>();
        Wire(gm,
            ("mainMenuPanel", root.gameObject), ("mainMenuTitleText", title), ("mainMenuSaveInfoText", null),
            ("continueButton", cont), ("newGameButton", newGame), ("playButton", null), ("mainMenuSettingsButton", settings),
            ("mainMenuQuitButton", quit), ("newGameModalPanel", modalRoot.gameObject), ("confirmNewGameBtn", confirm), ("cancelNewGameBtn", cancel),
            ("saveTitleText", saveTitle), ("saveDayText", saveDay), ("saveBranchText", saveBranch), ("saveBranchLevelText", null), ("saveCashText", saveCash));
        SetBool(gm, "useKuryeDefteriLayout", true);
        Report.Add("• Ana menü + yeni oyun onayı");
    }

    /// <summary>Small stat card: icon badge, tiny label and a big value. Width fixed (or flexible with width -1).</summary>
    private static TextMeshProUGUI StatTile(Transform parent, string name, string icon, Color badge, Color badgeInk, string labelKey, string label,
        string value, Color valueColor, float width, float flexWidth)
    {
        RectTransform tile = Node(name, parent);
        Surface(tile, CozyTheme.Well, 16f, CozyTheme.Line, 2f);
        VStack(tile, 2f, Pad(12, 12, 10, 10), TextAnchor.MiddleLeft);
        Size(tile, width, -1f, flexWidth, 1f);

        RectTransform head = Node("Head", tile);
        HStack(head, 6f);
        Size(head, -1f, 24f);
        Icon(head, "Icon", icon, 20f, badgeInk == CozyTheme.Ink ? CozyTheme.InkSoft : badgeInk);
        TextMeshProUGUI l = Txt(head, "Label", label, TextStyle.Label, CozyTheme.InkSoft, TextAlignmentOptions.MidlineLeft, 15f, false);
        l.characterSpacing = 3f;
        Size(l, -1f, 24f, 1f);
        Loc(l, labelKey, label);

        TextMeshProUGUI v = Line(tile, "Value", value, TextStyle.Number, valueColor, 46f, TextAlignmentOptions.MidlineLeft, 30f);
        Size(v, -1f, 46f, 1f);
        return v;
    }

    // =====================================================================
    // 5. PAUSE
    // =====================================================================

    private static void BuildPauseMenu(Transform canvas)
    {
        Remove(canvas, "PauseMenuPanel");
        Card card = Modal(canvas, "PauseMenuPanel", 540f, 640f, out RectTransform root);
        VStack(card.face, 16f, Pad(40, 40, 36, 36));
        HeaderRow(card.face, "pause", CozyTheme.Kraft, CozyTheme.Ink, "PauseTitle", "Mola", "cozy_pause_title", out _, null, null, out _, null, out _);
        DashedLine(card.face, "Divider", CozyTheme.Line, 3f);
        Button resume = Btn(card.face, "ResumeBtn", "Devam et", CozyTheme.ButtonStyle.Primary, 76f, "play", 28f, true);
        Button settings = Btn(card.face, "SettingsBtn", "Ayarlar", CozyTheme.ButtonStyle.Soft, 64f, "gear", 26f, true);
        Button mainMenu = Btn(card.face, "MainMenuBtn", "Ana menüye dön", CozyTheme.ButtonStyle.Soft, 64f, "home", 26f, true);
        Spacer(card.face);
        Button quit = Btn(card.face, "QuitBtn", "Masaüstüne çık", CozyTheme.ButtonStyle.Ghost, 60f, "door", 24f, true);
        root.gameObject.SetActive(false);

        Wire(Find<GameMenuManager>(),
            ("pauseMenuPanel", root.gameObject), ("resumeButton", resume), ("pauseSettingsButton", settings),
            ("returnToMainMenuButton", mainMenu), ("pauseQuitButton", quit));
        SetBool(Find<GameMenuManager>(), "useKuryeDefteriLayout", true);
        Report.Add("• Duraklatma menüsü");
    }

    // =====================================================================
    // 6. SETTINGS
    // =====================================================================

    private static void BuildSettings(Transform canvas)
    {
        Remove(canvas, "SettingsPanel");
        Card card = Modal(canvas, "SettingsPanel", 1400f, 880f, out RectTransform root);

        RectTransform head = Node("HeaderRow", card.face);
        TopStrip(head, 104f);
        HStack(head, 16f, Pad(36, 36, 0, 0));
        Badge(head, "HeaderBadge", "gear", 60f, CozyTheme.Kraft, CozyTheme.Ink);
        TextMeshProUGUI title = Txt(head, "SettingsTitle", "Ayarlar", TextStyle.H1, CozyTheme.Ink, TextAlignmentOptions.MidlineLeft, 40f, false);
        Size(title, -1f, 104f, 1f);
        Loc(title, "cozy_settings_title", "Ayarlar");
        Image headLine = DashedLine(card.face, "HeaderDivider", CozyTheme.Line, 3f);
        IgnoreLayout(headLine);
        TopStrip((RectTransform)headLine.transform, 3f, 104f, 32f, 32f);

        // Vertical tabs
        RectTransform tabs = Node("TabColumn", card.face);
        Place(tabs, Anchor.TL, 32f, 136f, 300f, 300f);
        VStack(tabs, 8f);
        Button tabAudio = SideTab(tabs, "TabAudio", "volume", "Ses", "cozy_tab_audio", true);
        Button tabGraphics = SideTab(tabs, "TabGraphics", "monitor", "Görüntü ve dil", "cozy_tab_graphics", false);
        Button tabControls = SideTab(tabs, "TabControls", "keys", "Kontroller", "cozy_tab_controls", false);

        // Content well
        RectTransform content = Node("ContentArea", card.face);
        Place(content, Anchor.TL, 364f, 136f, 1004f, 604f);
        Surface(content, CozyTheme.RowFace, 22f, CozyTheme.Line, 2f);

        // ---- Audio ----
        RectTransform audio = Section(content, "AudioSection");
        (Slider master, TextMeshProUGUI masterVal) = SliderRow(audio, "MasterVolRow", "setting_master_vol", "Ana ses");
        (Slider music, TextMeshProUGUI musicVal) = SliderRow(audio, "MusicVolRow", "setting_music_vol", "Müzik");
        (Slider sfx, TextMeshProUGUI sfxVal) = SliderRow(audio, "SfxVolRow", "setting_sfx_vol", "Efektler");
        (Slider amb, TextMeshProUGUI ambVal) = SliderRow(audio, "AmbienceVolRow", "setting_ambience_vol", "Çevre sesi");
        (Slider ui, TextMeshProUGUI uiVal) = SliderRow(audio, "UiVolRow", "setting_ui_vol", "Arayüz ve bildirimler");

        // ---- Graphics & language ----
        RectTransform graphics = Section(content, "GraphicsSection");
        RectTransform langRow = SettingRow(graphics, "LanguageRow", "setting_language", "Oyun dili", out TextMeshProUGUI langLabel);
        HSpacer(langRow);
        RectTransform seg = Node("Segmented", langRow);
        HStack(seg, 10f);
        Size(seg, 420f, 56f);
        Button trBtn = SegmentButton(seg, "TurkishButton", "Türkçe", true);
        Button enBtn = SegmentButton(seg, "EnglishButton", "English", false);
        SettingsLanguageUI langUI = langRow.gameObject.AddComponent<SettingsLanguageUI>();
        langUI.turkishButton = trBtn;
        langUI.turkishButtonBg = trBtn.GetComponent<Image>();
        langUI.turkishButtonText = LabelOf(trBtn);
        langUI.englishButton = enBtn;
        langUI.englishButtonBg = enBtn.GetComponent<Image>();
        langUI.englishButtonText = LabelOf(enBtn);
        langUI.rowLabelText = null; // row label is localized by GameMenuManager
        langUI.activeBtnColor = CozyTheme.Ink;
        langUI.inactiveBtnColor = CozyTheme.RowFace;
        langUI.activeTextColor = CozyTheme.Paper;
        langUI.inactiveTextColor = CozyTheme.InkSoft;

        TMP_Dropdown quality = DropdownRow(graphics, "QualityRow", "setting_quality", "Grafik kalitesi");
        TMP_Dropdown display = DropdownRow(graphics, "FullscreenRow", "setting_display_mode", "Ekran modu");
        TMP_Dropdown resolution = DropdownRow(graphics, "ResolutionRow", "setting_resolution", "Çözünürlük");
        RectTransform vsyncRow = SettingRow(graphics, "VsyncRow", "setting_vsync", "Dikey senkronizasyon", out _);
        HSpacer(vsyncRow);
        Toggle vsync = ToggleCtrl(vsyncRow, "Toggle", 44f);
        TMP_Dropdown fps = DropdownRow(graphics, "FpsLimitRow", "setting_target_fps", "Kare hızı sınırı");

        // ---- Controls ----
        RectTransform controls = Section(content, "ControlsSection");
        (Slider sens, TextMeshProUGUI sensVal) = SliderRow(controls, "MouseSensRow", "setting_mouse_sens", "Fare hassasiyeti");
        RectTransform invertRow = SettingRow(controls, "InvertYRow", "setting_invert_y", "Fare Y eksenini ters çevir", out _);
        HSpacer(invertRow);
        Toggle invert = ToggleCtrl(invertRow, "Toggle", 44f);

        RectTransform kbHead = Node("KeybindingsHeaderRow", controls);
        HStack(kbHead, 12f, Pad(4, 4, 12, 18)); // room above, and below for the button lip
        Size(kbHead, -1f, 78f);
        TextMeshProUGUI kbTitle = Txt(kbHead, "HeaderTitle", "Tuş atamaları", TextStyle.Label, CozyTheme.InkSoft, TextAlignmentOptions.MidlineLeft, 17f, false);
        Size(kbTitle, -1f, 48f, 1f);
        Loc(kbTitle, "cozy_keybindings_title", "Tuş atamaları");
        Button reset = Btn(kbHead, "ResetBindingsBtn", "Varsayılana dön", CozyTheme.ButtonStyle.Soft, 48f, "back", 20f);
        Size(reset, 280f, 48f);

        ScrollRect kbList = ScrollList(controls, "KeybindingsScrollView", out RectTransform kbContent, 6f, 8);
        Size(kbList, -1f, -1f, 1f, 1f);
        foreach (GameAction action in Enum.GetValues(typeof(GameAction)))
        {
            KeyRow(kbContent, action);
        }

        graphics.gameObject.SetActive(false);
        controls.gameObject.SetActive(false);

        // Footer
        RectTransform footer = Node("Footer", card.face);
        Place(footer, Anchor.TL, 364f, 764f, 1004f, 72f);
        HStack(footer, 16f, null, TextAnchor.MiddleRight);
        Button back = Btn(footer, "SettingsBackBtn", "Kaydet ve dön", CozyTheme.ButtonStyle.Primary, 68f, "check", 26f);
        Size(back, 340f, 68f);

        root.gameObject.SetActive(false);

        GameMenuManager gm = Find<GameMenuManager>();
        Wire(gm,
            ("settingsPanel", root.gameObject), ("settingsBackButton", back), ("tabAudioBtn", tabAudio), ("tabGraphicsBtn", tabGraphics),
            ("tabControlsBtn", tabControls), ("audioSection", audio.gameObject), ("graphicsSection", graphics.gameObject),
            ("controlsSection", controls.gameObject), ("masterVolumeSlider", master), ("masterVolumeValText", masterVal),
            ("musicVolumeSlider", music), ("musicVolumeValText", musicVal), ("sfxVolumeSlider", sfx), ("sfxVolumeValText", sfxVal),
            ("ambienceVolumeSlider", amb), ("ambienceVolumeValText", ambVal), ("uiVolumeSlider", ui), ("uiVolumeValText", uiVal),
            ("languageDropdown", null), ("qualityDropdown", quality), ("fullscreenDropdown", display), ("resolutionDropdown", resolution),
            ("vsyncToggle", vsync), ("fpsLimitDropdown", fps), ("mouseSensSlider", sens), ("mouseSensValText", sensVal),
            ("invertYToggle", invert), ("resetKeybindingsBtn", reset), ("keybindingsContent", kbContent));
        SetBool(gm, "useKuryeDefteriLayout", true);
        Report.Add("• Ayarlar: ses, görüntü ve dil, kontroller + tuş atamaları");
    }

    private static Button SideTab(Transform parent, string name, string icon, string label, string key, bool active)
    {
        RectTransform rt = Node(name, parent);
        Image bg = Shape(rt, active ? CozyTheme.Ink : new Color(1f, 1f, 1f, 0f), 18f, true);
        Button b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = bg;
        ColorBlock cb = b.colors;
        cb.highlightedColor = new Color(0.96f, 0.93f, 0.88f, 1f);
        b.colors = cb;
        Size(rt, -1f, 68f);
        RectTransform inner = Node("Inner", rt);
        Stretch(inner, 20f, 0f, 16f, 0f);
        HStack(inner, 14f);
        Color ink = active ? CozyTheme.Paper : CozyTheme.InkSoft;
        Icon(inner, "Icon", icon, 28f, ink);
        // Label text comes from GameMenuManager (tab_audio / tab_graphics / tab_controls), so no LocalizedText here.
        TextMeshProUGUI t = Txt(inner, "Label", label, TextStyle.H3, ink, TextAlignmentOptions.MidlineLeft, 24f, false, false);
        Size(t, -1f, 68f, 1f);
        return b;
    }

    private static RectTransform Section(Transform content, string name)
    {
        RectTransform s = Node(name, content);
        Stretch(s, 32f, 16f, 32f, 16f);
        VStack(s, 0f);
        return s;
    }

    /// <summary>Settings row: "Label" on the left (localized by GameMenuManager), controls added after it.</summary>
    private static RectTransform SettingRow(Transform section, string name, string key, string label, out TextMeshProUGUI labelText)
    {
        RectTransform row = Node(name, section);
        HStack(row, 20f);
        Size(row, -1f, 92f);
        labelText = Txt(row, "Label", label, TextStyle.H3, CozyTheme.Ink, TextAlignmentOptions.MidlineLeft, 24f, false);
        Size(labelText, 380f, 92f);
        Loc(labelText, key, label);

        RectTransform rule = Node("Rule", row);
        IgnoreLayout(rule);
        rule.anchorMin = new Vector2(0f, 0f);
        rule.anchorMax = new Vector2(1f, 0f);
        rule.pivot = new Vector2(0.5f, 0f);
        rule.sizeDelta = new Vector2(0f, 2f);
        Image ri = rule.gameObject.AddComponent<Image>();
        ri.color = CozyTheme.Line;
        ri.raycastTarget = false;
        return row;
    }

    private static (Slider, TextMeshProUGUI) SliderRow(Transform section, string name, string key, string label)
    {
        RectTransform row = SettingRow(section, name, key, label, out _);
        Slider s = SliderCtrl(row, "Slider", 36f);
        TextMeshProUGUI val = Txt(row, "ValText", "%100", TextStyle.H3, CozyTheme.Ink, TextAlignmentOptions.MidlineRight, 24f, false);
        Size(val, 96f, 92f);
        return (s, val);
    }

    private static TMP_Dropdown DropdownRow(Transform section, string name, string key, string label)
    {
        RectTransform row = SettingRow(section, name, key, label, out _);
        HSpacer(row);
        TMP_Dropdown dd = DropdownCtrl(row, "Dropdown", 56f);
        Size(dd, 420f, 56f, 0f);
        return dd;
    }

    private static Button SegmentButton(Transform parent, string name, string label, bool active)
    {
        RectTransform rt = Node(name, parent);
        Image bg = Shape(rt, active ? CozyTheme.Ink : CozyTheme.RowFace, 18f, true);
        Border(bg, CozyTheme.KraftDeep, 2.5f);
        Button b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = bg;
        Size(rt, -1f, 56f, 1f);
        RectTransform inner = Node("Inner", rt);
        Stretch(inner, 12f, 0f, 12f, 0f);
        TextMeshProUGUI t = Txt(inner, "Label", label, TextStyle.H3, active ? CozyTheme.Paper : CozyTheme.InkSoft, TextAlignmentOptions.Center, 22f, false, false);
        Stretch(t.rectTransform);
        return b;
    }

    private static void KeyRow(Transform content, GameAction action)
    {
        RectTransform row = Node("KeyRow_" + action, content);
        Shape(row, Color.white, 16f);
        HStack(row, 16f, Pad(20, 14, 0, 0));
        Size(row, -1f, 66f);
        TextMeshProUGUI label = Txt(row, "ActionLabel", action.ToString(), TextStyle.BodyBold, CozyTheme.Ink, TextAlignmentOptions.MidlineLeft, 21f, false);
        Size(label, -1f, 66f, 1f);

        RectTransform key = Node("KeyButton", row);
        Image face = Shape(key, Color.white, 12f, true);
        Border(face, CozyTheme.Ink, 2.5f);
        Shadow lip = Lip(face, CozyTheme.Ink, 4f);
        Button b = key.gameObject.AddComponent<Button>();
        b.targetGraphic = face;
        ColorBlock cb = b.colors;
        cb.highlightedColor = new Color(0.97f, 0.93f, 0.86f, 1f);
        cb.pressedColor = new Color(0.92f, 0.86f, 0.76f, 1f);
        b.colors = cb;
        Size(key, 240f, 48f);
        RectTransform inner = Node("Inner", key);
        Stretch(inner, 10f, 0f, 10f, 0f);
        TextMeshProUGUI t = Txt(inner, "Text", "?", TextStyle.H3, CozyTheme.Ink, TextAlignmentOptions.Center, 20f, false);
        t.font = A.displayBold;
        Stretch(t.rectTransform);
        t.GetComponent<CozyInkText>().formatKeys = false;
        CozyButton cozy = key.gameObject.AddComponent<CozyButton>();
        cozy.inner = inner;
        cozy.lip = lip;
        cozy.lipDepth = 4f;
        cozy.pressDepth = 3f;
    }

    // =====================================================================
    // 7. FADE OVERLAY
    // =====================================================================

    private static void BuildFadeOverlay(Transform canvas)
    {
        Remove(canvas, "FadeOverlayPanel");
        RectTransform root = Node("FadeOverlayPanel", canvas);
        Stretch(root);
        Image img = root.gameObject.AddComponent<Image>();
        img.color = CozyTheme.Hex("1E1928");
        img.raycastTarget = false;
        CanvasGroup cg = root.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;
        Wire(Find<GameMenuManager>(), ("fadeOverlayCanvasGroup", cg));
        Report.Add("• Geçiş perdesi");
    }

    // =====================================================================
    // 9. SHOPS
    // =====================================================================

    private static void BuildShops(Transform canvas)
    {
        Remove(canvas, "GarageWorkshopPanel", "InsuranceAgencyPanel", "PassiveDispatchPanel", "PropertyPurchaseModal");
        CommercialHubUIManager hub = Find<CommercialHubUIManager>();

        GameObject garage = BuildGarage(canvas);
        GameObject insurance = BuildTierShop(canvas, "InsuranceAgencyPanel", "shield", CozyTheme.Sky, Color.white,
            "Kargo sigortası", false, "shield", "star");
        GameObject dispatch = BuildTierShop(canvas, "PassiveDispatchPanel", "building", CozyTheme.Honey, CozyTheme.Ink,
            "Bölge dağıtım şubesi", true, "truck", "star");
        GameObject property = BuildPropertyModal(canvas);

        Wire(hub, ("garagePanelRoot", garage), ("insurancePanelRoot", insurance), ("dispatchPanelRoot", dispatch), ("propertyModalRoot", property));
        Report.Add("• Dükkanlar: garaj, sigorta, dağıtım şubesi, mülk satın alma");
    }

    private static GameObject BuildGarage(Transform canvas)
    {
        Card card = Modal(canvas, "GarageWorkshopPanel", 1500f, 840f, out RectTransform root);
        VStack(card.face, 24f, Pad(40, 40, 32, 36));
        HeaderRow(card.face, "wrench", CozyTheme.Mint, Color.white, "Title", "Oto tamir ve boya atölyesi", null, out _,
            "Subtitle", "", out _, "CloseBtn", out _);

        RectTransform split = Node("ContentSplit", card.face);
        HorizontalLayoutGroup sl = HStack(split, 24f);
        sl.childForceExpandHeight = true;
        Size(split, -1f, -1f, 1f, 1f);

        // Left: service
        RectTransform left = Node("LeftPanel_VehicleService", split);
        Surface(left, CozyTheme.RowFace, 24f, CozyTheme.Line, 2f);
        VStack(left, 14f, Pad(28, 28, 24, 28));
        Size(left, 640f, -1f);
        Line(left, "LeftHeader", "Araç durumu", TextStyle.Label, CozyTheme.InkSoft, 24f);
        Line(left, "Status", "", TextStyle.H2, CozyTheme.Ink, 40f, TextAlignmentOptions.MidlineLeft, 28f);
        Bar(left, "CondBar_Track", 0.9f, CozyTheme.Mint, 16f);
        RectTransform info = Node("ServiceInfoBox", left);
        Surface(info, CozyTheme.Well, 18f);
        VStack(info, 6f, Pad(20, 20, 16, 16));
        Size(info, -1f, 150f);
        Line(info, "InfoHead", "", TextStyle.Label, CozyTheme.InkSoft, 22f);
        TextMeshProUGUI infoDesc = Txt(info, "InfoDesc", "", TextStyle.Body, CozyTheme.Ink, TextAlignmentOptions.TopLeft, 19f, true);
        Size(infoDesc, -1f, -1f, 1f, 1f);
        Spacer(left);
        Btn(left, "RepairBtn", "Aracı tamir et", CozyTheme.ButtonStyle.Primary, 68f, "wrench", 24f, true);
        // Engine tuning is switched off for now (CommercialHubUIManager.garageTuningEnabled): repair and paint only.
        Btn(left, "DriveBtn", "Aracı çalıştır ve sür", CozyTheme.ButtonStyle.Soft, 64f, "truck", 22f, true);

        // Right: paint shop
        RectTransform right = Node("RightPanel_PaintShop", split);
        Surface(right, CozyTheme.RowFace, 24f, CozyTheme.Line, 2f);
        VStack(right, 12f, Pad(28, 28, 24, 28));
        Size(right, -1f, -1f, 1f);
        Line(right, "RightHeader", "Boya atölyesi", TextStyle.Label, CozyTheme.InkSoft, 24f);
        Line(right, "PaintTitle", "", TextStyle.H2, CozyTheme.Ink, 38f, TextAlignmentOptions.MidlineLeft, 28f);
        Line(right, "PaintDesc", "", TextStyle.Body, CozyTheme.InkSoft, 30f, TextAlignmentOptions.MidlineLeft, 20f);
        Line(right, "CurrentColor", "", TextStyle.BodyBold, CozyTheme.Ink, 34f, TextAlignmentOptions.MidlineLeft, 20f);

        RectTransform grid = Node("PaletteGrid", right);
        GridLayoutGroup g = grid.gameObject.AddComponent<GridLayoutGroup>();
        g.cellSize = new Vector2(128f, 156f);
        g.spacing = new Vector2(12f, 12f);
        g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        g.constraintCount = 5;
        g.childAlignment = TextAnchor.UpperCenter;
        Size(grid, -1f, 330f);

        string[] names = { "Kırmızı", "Mavi", "Siyah", "Beyaz", "Sarı", "Yeşil", "Turuncu", "Mor", "Gri", "Turkuaz" };
        string[] hexes = { "C5221F", "1A73E8", "1E1E24", "F8F9FA", "FBBC04", "1E8E3E", "E8710A", "9334E8", "5F6368", "00BCD4" };
        for (int i = 0; i < names.Length; i++)
        {
            RectTransform tile = Node("PaintBtn_" + i, grid);
            Image face = Shape(tile, Color.white, 18f, true);
            Border(face, CozyTheme.Line, 2f);
            Button b = tile.gameObject.AddComponent<Button>();
            b.targetGraphic = face;
            ColorBlock cb = b.colors;
            cb.highlightedColor = new Color(0.97f, 0.93f, 0.86f, 1f);
            b.colors = cb;
            VStack(tile, 8f, Pad(12, 12, 16, 12), TextAnchor.UpperCenter);
            RectTransform swRow = Node("SwatchRow", tile);
            HStack(swRow, 0f, null, TextAnchor.MiddleCenter);
            Size(swRow, -1f, 80f);
            RectTransform sw = Node("Swatch", swRow);
            Image swImg = sw.gameObject.AddComponent<Image>();
            swImg.sprite = A.circle;
            swImg.color = CozyTheme.Hex(hexes[i]);
            swImg.raycastTarget = false;
            Border(swImg, new Color(0f, 0f, 0f, 0.12f), 2f);
            Size(sw, 76f, 76f);
            Line(tile, "Text", names[i], TextStyle.BodyBold, CozyTheme.Ink, 28f, TextAlignmentOptions.Center, 18f);
        }

        root.gameObject.SetActive(false);
        return root.gameObject;
    }

    private static GameObject BuildTierShop(Transform canvas, string rootName, string icon, Color badge, Color badgeInk, string title,
        bool hasRevenue, string tier2Icon, string tier3Icon)
    {
        Card card = Modal(canvas, rootName, 1000f, hasRevenue ? 560f : 520f, out RectTransform root);
        VStack(card.face, 18f, Pad(40, 40, 32, 36));

        RectTransform head = HeaderRow(card.face, icon, badge, badgeInk, "Title", title, null, out _,
            "Status", "", out _, "CloseBtn", out Button close);
        Image wallet = Chip(head, "WalletChip", "$0", CozyTheme.Tone.Honey, "coin", 46f, 20f);
        wallet.transform.Find("Text").name = "Balance";
        wallet.transform.SetSiblingIndex(close.transform.GetSiblingIndex());

        if (hasRevenue)
        {
            RectTransform rev = Node("RevenueStrip", card.face);
            Shape(rev, CozyTheme.MintTint, 18f);
            HStack(rev, 12f, Pad(18, 18, 0, 0));
            Size(rev, -1f, 56f);
            Icon(rev, "Icon", "coin", 26f, CozyTheme.MintInk);
            TextMeshProUGUI revText = Txt(rev, "Revenue", "", TextStyle.BodyBold, CozyTheme.MintInk, TextAlignmentOptions.MidlineLeft, 21f, false);
            Size(revText, -1f, 56f, 1f);
        }

        PlanButton(card.face, "Tier2Btn", tier2Icon);
        PlanButton(card.face, "Tier3Btn", tier3Icon);

        root.gameObject.SetActive(false);
        return root.gameObject;
    }

    private static void PlanButton(Transform parent, string name, string icon)
    {
        Button b = Btn(parent, name, "", CozyTheme.ButtonStyle.Soft, 108f, icon, 23f, true);
        TextMeshProUGUI label = LabelOf(b);
        label.fontSizeMin = 15f;
        label.textWrappingMode = TextWrappingModes.Normal;
    }

    private static GameObject BuildPropertyModal(Transform canvas)
    {
        Card card = Modal(canvas, "PropertyPurchaseModal", 920f, 600f, out RectTransform root);
        VStack(card.face, 18f, Pad(44, 44, 36, 36));
        HeaderRow(card.face, "building", CozyTheme.Kraft, CozyTheme.Ink, "Title", "Ticari mülk", null, out _, null, null, out _, null, out _);
        TextMeshProUGUI desc = Txt(card.face, "Desc", "", TextStyle.Body, CozyTheme.Ink, TextAlignmentOptions.TopLeft, 22f, true);
        Size(desc, -1f, -1f, 1f, 1f);

        RectTransform facts = Node("FactRow", card.face);
        HorizontalLayoutGroup fl = HStack(facts, 14f);
        fl.childForceExpandWidth = true;
        Size(facts, -1f, 72f);
        FactPill(facts, "ReqTile", "star", "Req");
        FactPill(facts, "CostTile", "coin", "Cost");

        RectTransform row = Node("ButtonRow", card.face);
        HorizontalLayoutGroup br = HStack(row, 18f);
        br.childForceExpandWidth = true;
        Size(row, -1f, 70f);
        Button cancel = Btn(row, "CancelBtn", "Vazgeç", CozyTheme.ButtonStyle.Soft, 70f, null, 26f);
        Size(cancel, -1f, 70f, 1f);
        Button buy = Btn(row, "BuyBtn", "Satın al", CozyTheme.ButtonStyle.Primary, 70f, null, 26f);
        Size(buy, -1f, 70f, 1f);

        root.gameObject.SetActive(false);
        return root.gameObject;
    }

    private static void FactPill(Transform parent, string name, string icon, string textName)
    {
        RectTransform tile = Node(name, parent);
        Surface(tile, Color.white, 18f, CozyTheme.Line, 2f);
        HStack(tile, 12f, Pad(18, 18, 0, 0));
        Size(tile, -1f, 72f, 1f);
        Icon(tile, "Icon", icon, 28f, CozyTheme.InkSoft);
        TextMeshProUGUI t = Txt(tile, textName, "", TextStyle.H3, CozyTheme.Ink, TextAlignmentOptions.MidlineLeft, 22f, false);
        Size(t, -1f, 72f, 1f);
    }

    // =====================================================================
    // 10. GAME OVER
    // =====================================================================

    private static void BuildGameOver(Transform canvas)
    {
        Remove(canvas, "GameOver_Modal_Root");
        Card card = Modal(canvas, "GameOver_Modal_Root", 700f, 640f, out RectTransform root, CozyTheme.ScrimStrong);
        CanvasGroup cg = root.gameObject.AddComponent<CanvasGroup>();
        VStack(card.face, 16f, Pad(48, 48, 44, 40), TextAnchor.UpperCenter);

        RectTransform badgeRow = Node("BadgeRow", card.face);
        HStack(badgeRow, 0f, null, TextAnchor.MiddleCenter);
        Size(badgeRow, -1f, 96f);
        Badge(badgeRow, "AlertBadge", "alert", 96f, CozyTheme.RedTint, CozyTheme.RedInk);

        RectTransform chipRow = Node("ChipRow", card.face);
        HStack(chipRow, 0f, null, TextAnchor.MiddleCenter);
        Size(chipRow, -1f, 34f);
        Image chip = Chip(chipRow, "Header_Badge", "Şirket tasfiyesi", CozyTheme.Tone.Red);
        Loc(chip.transform.Find("Text").GetComponent<TextMeshProUGUI>(), "game_over_badge", "Şirket tasfiyesi");

        TextMeshProUGUI title = Line(card.face, "Title_Text", "İflas ettin", TextStyle.H1, CozyTheme.Ink, 56f, TextAlignmentOptions.Center, 42f);
        TextMeshProUGUI debt = Line(card.face, "Debt_Amount_Text", "", TextStyle.H2, CozyTheme.RedInk, 44f, TextAlignmentOptions.Center, 28f);
        TextMeshProUGUI reason = Txt(card.face, "Reason_Text", "", TextStyle.Body, CozyTheme.InkSoft, TextAlignmentOptions.Top, 21f, true);
        Size(reason, -1f, -1f, 1f, 1f);
        Button restart = Btn(card.face, "Restart_Button", "Yeniden başla (1. gün)", CozyTheme.ButtonStyle.Primary, 72f, "back", 26f);

        root.gameObject.SetActive(false);
        Wire(Find<GameOverManager>(),
            ("gameOverPanelRoot", root.gameObject), ("titleText", title), ("reasonText", reason), ("debtAmountText", debt),
            ("restartButton", restart), ("panelCanvasGroup", cg));
        Report.Add("• İflas ekranı");
    }

    // =====================================================================
    // 11. BRANCH UPGRADE TRANSITION
    // =====================================================================

    private static void BuildBranchTransition(Transform canvas)
    {
        Remove(canvas, "BranchUpgradeTransitionPanel");
        RectTransform root = Node("BranchUpgradeTransitionPanel", canvas);
        Stretch(root);
        // The root Image is the black backdrop: the script fades it away while the card stays in front.
        Image bg = root.gameObject.AddComponent<Image>();
        bg.color = CozyTheme.Hex("1E1928");
        bg.raycastTarget = true;
        CanvasGroup cg = root.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        Card card = PaperCard(root, "Card", CozyTheme.Paper);
        Place(card.root, Anchor.MC, 0f, 0f, 880f, 640f);
        AutoHeight(card);
        CanvasGroup cardGroup = card.root.gameObject.AddComponent<CanvasGroup>();
        VStack(card.face, 16f, Pad(52, 52, 40, 34), TextAnchor.UpperCenter);

        RectTransform badgeRow = Node("BadgeRow", card.face);
        HStack(badgeRow, 0f, null, TextAnchor.MiddleCenter);
        Size(badgeRow, -1f, 96f);
        Badge(badgeRow, "StarBadge", "star", 96f, CozyTheme.Honey, CozyTheme.Ink);

        TextMeshProUGUI badgeText = Line(card.face, "BadgeText", "Şube yükseltildi", TextStyle.Label, CozyTheme.MintInk, 28f, TextAlignmentOptions.Center, 20f);
        TextMeshProUGUI titleText = Line(card.face, "TitleText", "Seviye 2", TextStyle.Display, CozyTheme.Ink, 76f, TextAlignmentOptions.Center, 64f);
        DashedLine(card.face, "Divider", CozyTheme.Line, 3f);

        RectTransform stats = Node("Stats", card.face);
        HorizontalLayoutGroup statsLayout = HStack(stats, 16f);
        statsLayout.childForceExpandWidth = true;
        statsLayout.childForceExpandHeight = true;
        Size(stats, -1f, 96f);
        TextMeshProUGUI capValue = StatTile(stats, "CapTile", "box", CozyTheme.Kraft, CozyTheme.Ink, "cozy_trans_cap_label", "Günlük kargo", "8 koli", CozyTheme.Ink, -1f, 1f);
        TextMeshProUGUI rentValue = StatTile(stats, "RentTile", "coin", CozyTheme.HoneyTint, CozyTheme.HoneyInk, "cozy_trans_rent_label", "Günlük kira", "$120", CozyTheme.HoneyInk, -1f, 1f);

        TextMeshProUGUI perksLabel = Line(card.face, "PerksLabel", "Yeni özellikler", TextStyle.Label, CozyTheme.InkSoft, 26f, TextAlignmentOptions.MidlineLeft, 17f);
        Loc(perksLabel, "cozy_trans_perks", "Yeni özellikler");

        RectTransform perks = Node("Perks", card.face);
        VStack(perks, 8f);
        for (int i = 0; i < 4; i++)
        {
            RectTransform row = Node("Perk_" + i, perks);
            Surface(row, CozyTheme.Well, 16f, CozyTheme.Line, 2f);
            HStack(row, 14f, Pad(16, 18, 10, 10));
            LayoutElement rl = Size(row, -1f, 52f);
            rl.preferredHeight = -1f;
            Badge(row, "Badge", "check", 32f, CozyTheme.Mint, Color.white);
            TextMeshProUGUI t = Txt(row, "Text", "", TextStyle.Body, CozyTheme.Ink, TextAlignmentOptions.MidlineLeft, 21f, true);
            t.enableAutoSizing = false;
            t.overflowMode = TextOverflowModes.Overflow;
            Size(t, -1f, -1f, 1f);
        }

        TextMeshProUGUI hint = Line(card.face, "ContinueHint", "Devam etmek için [Enter]", TextStyle.Small, CozyTheme.InkSoft, 36f, TextAlignmentOptions.Center, 19f);
        hint.enableAutoSizing = false;
        hint.gameObject.SetActive(false);

        root.gameObject.SetActive(false);
        Wire(Find<BranchUpgradeTransitionUI>(),
            ("panelRoot", root.gameObject), ("canvasGroup", cg), ("cardGroup", cardGroup), ("badgeText", badgeText), ("titleText", titleText),
            ("detailsText", null), ("capValueText", capValue), ("rentValueText", rentValue), ("perkRows", perks), ("continueHintText", hint));
        Report.Add("• Şube yükseltme geçişi (şeffaflaşan arka plan, Enter ile geç)");
    }
}
#endif
