#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static CozyKit;

public static partial class CozyUIBuilder
{
    [MenuItem("Tools/Delivery Game/Testing/Reset Tutorials (play all tips again)", false, 119)]
    public static void ResetTutorialsMenu()
    {
        TutorialManager.ResetProgress();
        Debug.Log("<color=#2F9B6F>[Tutorial]</color> Tüm rehberler sıfırlandı; oyunda yeniden görünecekler.");
    }

    // =====================================================================
    // TUTORIAL SYSTEM: coach card, day-end window, F1 guide list
    // =====================================================================

    private static void BuildTutorialSystem(Transform canvas)
    {
        Remove(canvas, "TutorialCoachCard", "TutorialModal", "TutorialGuidePanel", "DeliveryTutorial_Modal_Root", "VehicleTutorial_Modal_Root");
        RetireOldGuides();

        TutorialManager manager = Find<TutorialManager>();
        if (manager == null)
        {
            GameObject go = new GameObject("[TUTORIAL_MANAGER]");
            Undo.RegisterCreatedObjectUndo(go, UndoName);
            manager = go.AddComponent<TutorialManager>();
        }

        // ---------------- Coach card (left, non-blocking) ----------------
        Card coach = PaperCard(canvas, "TutorialCoachCard", CozyTheme.Paper, 28f, 7f, true, false);
        // Centred on screen, lifted a little so the crosshair and the interaction prompt below it stay visible.
        Place(coach.root, Anchor.MC, 0f, 150f, 520f, 220f);
        AutoHeight(coach);
        CozyPanelIntro intro = coach.root.gameObject.AddComponent<CozyPanelIntro>();
        intro.style = CozyPanelIntro.Style.Grow;
        VStack(coach.face, 12f, Pad(24, 24, 20, 18));

        RectTransform head = Node("HeadRow", coach.face);
        HStack(head, 14f);
        Size(head, -1f, 60f);
        Image coachBadge = Badge(head, "Badge", "box", 56f, CozyTheme.Honey, CozyTheme.Ink);
        Image coachIcon = coachBadge.transform.Find("Icon").GetComponent<Image>();
        RectTransform headCol = Node("Col", head);
        VStack(headCol, 0f, null, TextAnchor.MiddleLeft);
        Size(headCol, -1f, 60f, 1f);
        TextMeshProUGUI label = Line(headCol, "Label", "Rehber", TextStyle.Label, CozyTheme.InkSoft, 22f, TextAlignmentOptions.MidlineLeft, 16f);
        TextMeshProUGUI title = Line(headCol, "Title", "", TextStyle.H2, CozyTheme.Ink, 38f, TextAlignmentOptions.MidlineLeft, 28f);

        TextMeshProUGUI body = Txt(coach.face, "Body", "", TextStyle.Body, CozyTheme.Ink, TextAlignmentOptions.TopLeft, 22f, true);
        body.enableAutoSizing = false;
        body.lineSpacing = 6f;

        RectTransform footer = Node("Footer", coach.face);
        HStack(footer, 12f);
        Size(footer, -1f, 24f);
        Image timerFill = Bar(footer, "TimerBg", 1f, CozyTheme.Honey, 8f);
        RectTransform timerRoot = (RectTransform)timerFill.transform.parent;
        TextMeshProUGUI skip = Txt(footer, "SkipHint", "[Enter] ile geç", TextStyle.Small, CozyTheme.InkSoft, TextAlignmentOptions.MidlineRight, 16f, false);
        skip.enableAutoSizing = false;
        Size(skip, 170f, 24f);
        coach.root.gameObject.SetActive(false);

        // ---------------- Modal (day-end receipt tip) ----------------
        Card modal = Modal(canvas, "TutorialModal", 820f, 560f, out RectTransform modalRoot, CozyTheme.Scrim);
        AutoHeight(modal);
        VStack(modal.face, 18f, Pad(44, 44, 36, 36));

        RectTransform mHead = Node("HeaderRow", modal.face);
        HStack(mHead, 18f);
        Size(mHead, -1f, 76f);
        Image modalBadge = Badge(mHead, "Badge", "receipt", 72f, CozyTheme.Honey, CozyTheme.Ink);
        Image modalIcon = modalBadge.transform.Find("Icon").GetComponent<Image>();
        RectTransform mCol = Node("Col", mHead);
        VStack(mCol, 2f, null, TextAnchor.MiddleLeft);
        Size(mCol, -1f, 76f, 1f);
        TextMeshProUGUI mTitle = Line(mCol, "Title", "", TextStyle.H1, CozyTheme.Ink, 46f, TextAlignmentOptions.MidlineLeft, 36f);
        TextMeshProUGUI mSubtitle = Line(mCol, "Subtitle", "", TextStyle.Body, CozyTheme.InkSoft, 28f, TextAlignmentOptions.MidlineLeft, 20f);

        RectTransform rows = Node("Rows", modal.face);
        VStack(rows, 12f);
        int maxRows = 0;
        foreach (TutorialDef d in TutorialCatalog.All) maxRows = Mathf.Max(maxRows, d.rows.Count);
        for (int i = 0; i < maxRows; i++)
        {
            RectTransform row = Node("Row_" + i, rows);
            Surface(row, CozyTheme.Well, 18f, CozyTheme.Line, 2f);
            HStack(row, 16f, Pad(20, 22, 14, 14));
            LayoutElement rowLayout = Size(row, -1f, 78f);
            rowLayout.preferredHeight = -1f; // at least 78 tall, but grows with longer (wrapped) text
            Badge(row, "Badge", "check", 52f, CozyTheme.Mint, Color.white);
            TextMeshProUGUI t = Txt(row, "Text", "", TextStyle.Body, CozyTheme.Ink, TextAlignmentOptions.MidlineLeft, 22f, true);
            t.enableAutoSizing = false;
            t.overflowMode = TextOverflowModes.Overflow;
            Size(t, -1f, -1f, 1f);
        }

        Button ok = Btn(modal.face, "OkButton", "Anladım", CozyTheme.ButtonStyle.Primary, 72f, "check", 28f);
        Loc(LabelOf(ok), "tut_btn_ok", "Anladım");
        modalRoot.gameObject.SetActive(false);

        // ---------------- Guide list (F1) ----------------
        Card guide = Modal(canvas, "TutorialGuidePanel", 1000f, 640f, out RectTransform guideRoot, CozyTheme.Scrim);
        AutoHeight(guide);
        VStack(guide.face, 20f, Pad(40, 40, 32, 30));

        RectTransform gHead = Node("HeaderRow", guide.face);
        HStack(gHead, 18f);
        Size(gHead, -1f, 76f);
        Badge(gHead, "Badge", "hand", 64f, CozyTheme.Honey, CozyTheme.Ink);
        RectTransform gCol = Node("Col", gHead);
        VStack(gCol, 2f, null, TextAnchor.MiddleLeft);
        Size(gCol, -1f, 76f, 1f);
        Loc(Line(gCol, "Title", "Rehber", TextStyle.H1, CozyTheme.Ink, 46f, TextAlignmentOptions.MidlineLeft, 38f), "tut_guide_title", "Rehber");
        Loc(Line(gCol, "Subtitle", "İstediğin ipucunu yeniden izleyebilirsin.", TextStyle.Body, CozyTheme.InkSoft, 28f, TextAlignmentOptions.MidlineLeft, 20f), "tut_guide_sub", "İstediğin ipucunu yeniden izleyebilirsin.");
        Button close = RoundButton(gHead, "CloseButton", "x", CozyTheme.ButtonStyle.Soft, 56f);

        RectTransform grid = Node("Grid", guide.face);
        GridLayoutGroup gl = grid.gameObject.AddComponent<GridLayoutGroup>();
        gl.cellSize = new Vector2(446f, 92f);
        gl.spacing = new Vector2(16f, 14f);
        gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gl.constraintCount = 2;
        gl.childAlignment = TextAnchor.UpperLeft;
        int gridRows = Mathf.CeilToInt(TutorialCatalog.All.Length / 2f);
        Size(grid, -1f, gridRows * 92f + (gridRows - 1) * 14f);

        foreach (TutorialDef d in TutorialCatalog.All) GuideCell(grid, d);

        TextMeshProUGUI closeHint = Txt(guide.face, "CloseHint", "[F1] ile kapat", TextStyle.Small, CozyTheme.InkSoft, TextAlignmentOptions.Center, 17f, false);
        Size(closeHint, -1f, 28f);
        Loc(closeHint, "tut_guide_close", "[F1] ile kapat");
        guideRoot.gameObject.SetActive(false);

        // ---------------- Wire ----------------
        Wire(manager,
            ("coachRoot", coach.root.gameObject), ("coachBadge", coachBadge), ("coachIcon", coachIcon), ("coachLabel", label),
            ("coachTitle", title), ("coachBody", body), ("coachSkipHint", skip), ("coachTimerRoot", timerRoot.gameObject), ("coachTimerFill", timerFill),
            ("modalRoot", modalRoot.gameObject), ("modalBadge", modalBadge), ("modalIcon", modalIcon), ("modalTitle", mTitle),
            ("modalSubtitle", mSubtitle), ("modalRows", rows), ("modalOkButton", ok),
            ("guideRoot", guideRoot.gameObject), ("guideGrid", grid), ("guideCloseButton", close));

        Report.Add("• Rehber sistemi: küçük rehber kartı, gün sonu penceresi, F1 rehber listesi (8 rehber)");
    }

    /// <summary>Card whose height follows its content; the shadow layer stays out of the layout.</summary>
    private static void AutoHeight(Card card)
    {
        VerticalLayoutGroup v = card.root.gameObject.AddComponent<VerticalLayoutGroup>();
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        ContentSizeFitter fit = card.root.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        Transform shadow = card.root.Find("Shadow");
        if (shadow != null) IgnoreLayout(shadow);
    }

    private static void GuideCell(Transform grid, TutorialDef def)
    {
        RectTransform cell = Node("Guide_" + def.id, grid);
        Image face = Shape(cell, CozyTheme.RowFace, CozyTheme.RadiusRow, true);
        Border(face, CozyTheme.Line, 2.5f);
        Button b = cell.gameObject.AddComponent<Button>();
        b.targetGraphic = face;
        ColorBlock cb = b.colors;
        cb.highlightedColor = new Color(0.98f, 0.94f, 0.86f, 1f);
        cb.pressedColor = new Color(0.94f, 0.88f, 0.78f, 1f);
        b.colors = cb;
        HStack(cell, 14f, Pad(16, 16, 0, 0));

        Badge(cell, "Badge", def.icon, 52f, CozyTheme.Honey, CozyTheme.Ink);
        RectTransform texts = Node("Texts", cell);
        VStack(texts, 2f, null, TextAnchor.MiddleLeft);
        Size(texts, -1f, 92f, 1f);
        Line(texts, "Title", "", TextStyle.H3, CozyTheme.Ink, 30f, TextAlignmentOptions.MidlineLeft, 23f);
        Line(texts, "Sub", "", TextStyle.Small, CozyTheme.InkSoft, 26f, TextAlignmentOptions.MidlineLeft, 16f);
        Chip(cell, "Chip", "Yeni", CozyTheme.Tone.Honey, null, 30f, 16f);
    }

    /// <summary>The old full-screen guides are replaced by the tutorial system: switch their managers off.</summary>
    private static void RetireOldGuides()
    {
        foreach (DeliveryTutorialUI old in Object.FindObjectsByType<DeliveryTutorialUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Undo.RecordObject(old.gameObject, UndoName);
            old.gameObject.SetActive(false);
        }
        foreach (VehicleTutorialUI old in Object.FindObjectsByType<VehicleTutorialUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Undo.RecordObject(old.gameObject, UndoName);
            old.gameObject.SetActive(false);
        }
    }
}
#endif
