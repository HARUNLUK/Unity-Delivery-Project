#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class GarageWorkshopUIBuilder
{
    private static TMP_FontAsset cachedFont;
    private static Sprite sGlassLarge;
    private static Sprite sGlassCard;
    private static Sprite sBtnCyan;
    private static Sprite sBtnGreen;
    private static Sprite sBtnRed;
    private static Sprite sBtnDark;
    private static Sprite sBarTrack;
    private static Sprite sBarFill;
    private static Sprite sCircleSwatch;

    [MenuItem("Tools/Delivery Game/UI/Build Garage Workshop Panel", false, 30)]
    public static void BuildGarageWorkshopPanelMenuItem()
    {
        BuildGarageWorkshopPanelInActiveScene();
    }

    public static GameObject BuildGarageWorkshopPanelInActiveScene()
    {
        LoadAssets();

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

        Transform oldPanel = canvas.transform.Find("GarageWorkshopPanel");
        if (oldPanel != null)
        {
            Object.DestroyImmediate(oldPanel.gameObject);
        }

        GameObject panelObj = CreateSplitGaragePanel(canvas.transform);

        CommercialHubUIManager hub = canvas.GetComponentInChildren<CommercialHubUIManager>(true);
        if (hub == null)
        {
            hub = canvas.gameObject.AddComponent<CommercialHubUIManager>();
        }
        hub.garagePanelRoot = panelObj;
        hub.EnsureUI();

        panelObj.SetActive(false);
        EditorUtility.SetDirty(canvas.gameObject);
        EditorUtility.SetDirty(hub.gameObject);

        Debug.Log("<color=#32FF64>[GarageWorkshopUIBuilder] AAA Dark Glassmorphic Garage Workshop Panel successfully built and bound!</color>");
        return panelObj;
    }

    private static void LoadAssets()
    {
        // 1. Font
        cachedFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Fonts/Inter-VariableFont_opsz,wght SDF.asset");
        if (cachedFont == null)
        {
            cachedFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        }

        // 2. Sprites
        sGlassLarge = LoadSprite("Assets/_Project/Textures/UI/UI_Glass_Panel_Large.png");
        sGlassCard = LoadSprite("Assets/_Project/Textures/UI/UI_Glass_Card.png");
        sBtnCyan = LoadSprite("Assets/_Project/Textures/UI/UI_Button_Neon_Cyan.png");
        sBtnGreen = LoadSprite("Assets/_Project/Textures/UI/UI_Button_Neon_Green.png");
        sBtnRed = LoadSprite("Assets/_Project/Textures/UI/UI_Button_Neon_Red.png");
        sBtnDark = LoadSprite("Assets/_Project/Textures/UI/UI_Button_Dark_Base.png");
        sBarTrack = LoadSprite("Assets/_Project/Textures/UI/UI_Bar_Track.png");
        sBarFill = LoadSprite("Assets/_Project/Textures/UI/UI_Bar_Fill.png");
        sCircleSwatch = LoadSprite("Assets/_Project/Textures/UI/UI_Circle_Swatch.png");
    }

    private static Sprite LoadSprite(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    public static GameObject CreateSplitGaragePanel(Transform parent)
    {
        LoadAssets();

        // Root Modal Frame (1040 x 530)
        GameObject root = new GameObject("GarageWorkshopPanel");
        root.transform.SetParent(parent, false);

        RectTransform rt = root.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1040, 530);
        rt.anchoredPosition = Vector2.zero;

        root.AddComponent<CanvasRenderer>();
        Image rootImg = root.AddComponent<Image>();
        if (sGlassLarge != null)
        {
            rootImg.sprite = sGlassLarge;
            rootImg.type = Image.Type.Sliced;
            rootImg.color = new Color(0.06f, 0.08f, 0.13f, 0.98f);
        }
        else
        {
            rootImg.color = new Color(0.06f, 0.08f, 0.13f, 0.98f);
        }

        // Top Header Bar (1000 x 48)
        GameObject headerObj = new GameObject("HeaderRow");
        headerObj.transform.SetParent(root.transform, false);
        RectTransform headerRt = headerObj.AddComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0.5f, 1f);
        headerRt.anchorMax = new Vector2(0.5f, 1f);
        headerRt.pivot = new Vector2(0.5f, 1f);
        headerRt.sizeDelta = new Vector2(1000, 48);
        headerRt.anchoredPosition = new Vector2(0, -15);

        // Title Box inside Header
        GameObject titleBox = new GameObject("TitleBox");
        titleBox.transform.SetParent(headerObj.transform, false);
        RectTransform tbRt = titleBox.AddComponent<RectTransform>();
        tbRt.anchorMin = new Vector2(0, 0);
        tbRt.anchorMax = new Vector2(0.82f, 1);
        tbRt.sizeDelta = Vector2.zero;
        tbRt.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup tbLayout = titleBox.AddComponent<VerticalLayoutGroup>();
        tbLayout.spacing = 1;
        tbLayout.childControlWidth = true;
        tbLayout.childControlHeight = true;
        tbLayout.childForceExpandWidth = true;
        tbLayout.childForceExpandHeight = false;

        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(titleBox.transform, false);
        AddTMP(titleObj, "🔧 OTO TAMİR & BOYA ATÖLYESİ", 20, FontStyles.Bold, new Color(0.25f, 0.9f, 1f), TextAlignmentOptions.Left);

        GameObject subtitleObj = new GameObject("Subtitle");
        subtitleObj.transform.SetParent(titleBox.transform, false);
        AddTMP(subtitleObj, "Araç bakım onarımı ve özel fırın boyama merkezi", 12, FontStyles.Normal, new Color(0.58f, 0.68f, 0.82f), TextAlignmentOptions.Left);

        // Close Button in Header
        GameObject closeBtnObj = new GameObject("CloseBtn");
        closeBtnObj.transform.SetParent(headerObj.transform, false);
        RectTransform cbRt = closeBtnObj.AddComponent<RectTransform>();
        cbRt.anchorMin = new Vector2(1, 0.5f);
        cbRt.anchorMax = new Vector2(1, 0.5f);
        cbRt.pivot = new Vector2(1, 0.5f);
        cbRt.sizeDelta = new Vector2(120, 36);
        cbRt.anchoredPosition = Vector2.zero;

        Image cbImg = closeBtnObj.AddComponent<Image>();
        if (sBtnRed != null) { cbImg.sprite = sBtnRed; cbImg.type = Image.Type.Sliced; }
        cbImg.color = new Color(0.85f, 0.2f, 0.2f, 1f);
        Button cbBtn = closeBtnObj.AddComponent<Button>();

        GameObject cbTxtObj = new GameObject("Text");
        cbTxtObj.transform.SetParent(closeBtnObj.transform, false);
        RectTransform cbtRt = cbTxtObj.AddComponent<RectTransform>();
        cbtRt.anchorMin = Vector2.zero; cbtRt.anchorMax = Vector2.one; cbtRt.sizeDelta = Vector2.zero;
        AddTMP(cbTxtObj, "✕ Kapat (ESC)", 13, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);

        // Split Content Container (Horizontal Layout)
        GameObject contentObj = new GameObject("ContentSplit");
        contentObj.transform.SetParent(root.transform, false);
        RectTransform cRt = contentObj.AddComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0.5f, 0);
        cRt.anchorMax = new Vector2(0.5f, 0);
        cRt.pivot = new Vector2(0.5f, 0);
        cRt.sizeDelta = new Vector2(1000, 440);
        cRt.anchoredPosition = new Vector2(0, 16);

        HorizontalLayoutGroup splitLayout = contentObj.AddComponent<HorizontalLayoutGroup>();
        splitLayout.spacing = 16;
        splitLayout.childControlWidth = true;
        splitLayout.childControlHeight = true;
        splitLayout.childForceExpandWidth = true;
        splitLayout.childForceExpandHeight = true;

        // =========================================================================
        // LEFT COLUMN: VEHICLE CONDITION & REPAIR SERVICE
        // =========================================================================
        GameObject leftCol = new GameObject("LeftPanel_VehicleService");
        leftCol.transform.SetParent(contentObj.transform, false);
        Image leftImg = leftCol.AddComponent<Image>();
        if (sGlassCard != null) { leftImg.sprite = sGlassCard; leftImg.type = Image.Type.Sliced; }
        leftImg.color = new Color(0.08f, 0.11f, 0.17f, 0.95f);

        VerticalLayoutGroup leftLayout = leftCol.AddComponent<VerticalLayoutGroup>();
        leftLayout.padding = new RectOffset(18, 18, 16, 16);
        leftLayout.spacing = 12;
        leftLayout.childControlWidth = true;
        leftLayout.childControlHeight = false;
        leftLayout.childForceExpandWidth = true;
        leftLayout.childForceExpandHeight = false;

        // Left Header
        GameObject lHeadObj = new GameObject("LeftHeader");
        lHeadObj.transform.SetParent(leftCol.transform, false);
        lHeadObj.AddComponent<LayoutElement>().preferredHeight = 22;
        AddTMP(lHeadObj, "🛠️ ARAÇ DURUMU & HASAR ONARIMI", 14, FontStyles.Bold, new Color(0.39f, 0.82f, 1f), TextAlignmentOptions.Left);

        // Status Line Box (Condition Only)
        GameObject statusBox = new GameObject("StatusBox");
        statusBox.transform.SetParent(leftCol.transform, false);
        Image sbImg = statusBox.AddComponent<Image>();
        sbImg.color = new Color(0.04f, 0.06f, 0.09f, 0.85f);
        statusBox.AddComponent<LayoutElement>().preferredHeight = 78;

        VerticalLayoutGroup sbLayout = statusBox.AddComponent<VerticalLayoutGroup>();
        sbLayout.padding = new RectOffset(14, 14, 8, 8);
        sbLayout.spacing = 6;
        sbLayout.childControlWidth = true;
        sbLayout.childControlHeight = false;

        GameObject statusObj = new GameObject("Status");
        statusObj.transform.SetParent(statusBox.transform, false);
        AddTMP(statusObj, "<b>Araç Sağlığı:</b> <color=#32FF64>%100 (Mükemmel)</color>", 13, FontStyles.Normal, Color.white, TextAlignmentOptions.Left);

        // Condition Gauge Bar
        GameObject condGaugeObj = new GameObject("CondBar_Track");
        condGaugeObj.transform.SetParent(statusBox.transform, false);
        condGaugeObj.AddComponent<LayoutElement>().preferredHeight = 16;
        Image cgTrackImg = condGaugeObj.AddComponent<Image>();
        if (sBarTrack != null) { cgTrackImg.sprite = sBarTrack; cgTrackImg.type = Image.Type.Sliced; }
        cgTrackImg.color = new Color(0.05f, 0.08f, 0.12f, 0.95f);

        GameObject condFillObj = new GameObject("CondBar_Fill");
        condFillObj.transform.SetParent(condGaugeObj.transform, false);
        RectTransform cdfRt = condFillObj.AddComponent<RectTransform>();
        cdfRt.anchorMin = Vector2.zero;
        cdfRt.anchorMax = Vector2.one;
        cdfRt.sizeDelta = Vector2.zero;
        Image cdfImg = condFillObj.AddComponent<Image>();
        if (sBarFill != null) { cdfImg.sprite = sBarFill; cdfImg.type = Image.Type.Sliced; }
        cdfImg.type = Image.Type.Filled;
        cdfImg.fillMethod = Image.FillMethod.Horizontal;
        cdfImg.fillOrigin = 0;
        cdfImg.fillAmount = 1.0f;
        cdfImg.color = new Color(0.2f, 0.9f, 0.4f);

        // Repair Button (Emerald Green - Large)
        GameObject repBtnObj = new GameObject("RepairBtn");
        repBtnObj.transform.SetParent(leftCol.transform, false);
        repBtnObj.AddComponent<LayoutElement>().preferredHeight = 52;
        Image repImg = repBtnObj.AddComponent<Image>();
        if (sBtnGreen != null) { repImg.sprite = sBtnGreen; repImg.type = Image.Type.Sliced; }
        repImg.color = new Color(0.1f, 0.58f, 0.28f, 1f);
        Button repBtn = repBtnObj.AddComponent<Button>();

        GameObject repTxtObj = new GameObject("Text");
        repTxtObj.transform.SetParent(repBtnObj.transform, false);
        RectTransform rptRt = repTxtObj.AddComponent<RectTransform>();
        rptRt.anchorMin = Vector2.zero; rptRt.anchorMax = Vector2.one; rptRt.sizeDelta = Vector2.zero;
        AddTMP(repTxtObj, "🛠️ Aracı Tamir Et ($150)", 14, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);

        // Service Info Box (Recessed)
        GameObject infoBox = new GameObject("ServiceInfoBox");
        infoBox.transform.SetParent(leftCol.transform, false);
        Image ibImg = infoBox.AddComponent<Image>();
        ibImg.color = new Color(0.04f, 0.06f, 0.09f, 0.85f);
        infoBox.AddComponent<LayoutElement>().preferredHeight = 92;

        VerticalLayoutGroup ibLayout = infoBox.AddComponent<VerticalLayoutGroup>();
        ibLayout.padding = new RectOffset(14, 14, 8, 8);
        ibLayout.spacing = 3;

        GameObject ibHeadObj = new GameObject("InfoHead");
        ibHeadObj.transform.SetParent(infoBox.transform, false);
        AddTMP(ibHeadObj, "💡 SERVİS & HASAR BİLGİSİ", 12, FontStyles.Bold, new Color(1f, 0.78f, 0.2f), TextAlignmentOptions.Left);

        GameObject ibDescObj = new GameObject("InfoDesc");
        ibDescObj.transform.SetParent(infoBox.transform, false);
        AddTMP(ibDescObj, "Araç hasar aldığında maksimum yol tutuş performansı düşebilir. Görevler arasında aracınızı periyodik olarak tamir ettirmeniz önerilir.", 11, FontStyles.Normal, new Color(0.65f, 0.75f, 0.88f), TextAlignmentOptions.Left);

        // Drive Out Button (Cyan - Large)
        GameObject driveBtnObj = new GameObject("DriveBtn");
        driveBtnObj.transform.SetParent(leftCol.transform, false);
        driveBtnObj.AddComponent<LayoutElement>().preferredHeight = 48;
        Image driveImg = driveBtnObj.AddComponent<Image>();
        if (sBtnCyan != null) { driveImg.sprite = sBtnCyan; driveImg.type = Image.Type.Sliced; }
        driveImg.color = new Color(0.12f, 0.45f, 0.75f, 1f);
        Button driveBtn = driveBtnObj.AddComponent<Button>();

        GameObject driveTxtObj = new GameObject("Text");
        driveTxtObj.transform.SetParent(driveBtnObj.transform, false);
        RectTransform drtRt = driveTxtObj.AddComponent<RectTransform>();
        drtRt.anchorMin = Vector2.zero; drtRt.anchorMax = Vector2.one; drtRt.sizeDelta = Vector2.zero;
        AddTMP(driveTxtObj, "🏎️ Aracı Sür & Atölyeden Çık", 14, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);

        // =========================================================================
        // RIGHT COLUMN: PAINT & COLOR WORKSHOP TILES
        // =========================================================================
        GameObject rightCol = new GameObject("RightPanel_PaintShop");
        rightCol.transform.SetParent(contentObj.transform, false);
        Image rightImg = rightCol.AddComponent<Image>();
        if (sGlassCard != null) { rightImg.sprite = sGlassCard; rightImg.type = Image.Type.Sliced; }
        rightImg.color = new Color(0.08f, 0.11f, 0.17f, 0.95f);

        VerticalLayoutGroup rightLayout = rightCol.AddComponent<VerticalLayoutGroup>();
        rightLayout.padding = new RectOffset(18, 18, 16, 16);
        rightLayout.spacing = 12;
        rightLayout.childControlWidth = true;
        rightLayout.childControlHeight = false;
        rightLayout.childForceExpandWidth = true;
        rightLayout.childForceExpandHeight = false;

        // Right Header
        GameObject rHeadObj = new GameObject("RightHeader");
        rHeadObj.transform.SetParent(rightCol.transform, false);
        rHeadObj.AddComponent<LayoutElement>().preferredHeight = 22;
        AddTMP(rHeadObj, "🎨 GÖVDE & PARÇA BOYAMA ATÖLYESİ", 14, FontStyles.Bold, new Color(1f, 0.85f, 0.2f), TextAlignmentOptions.Left);

        // Paint Info Box
        GameObject paintInfoBox = new GameObject("PaintInfoBox");
        paintInfoBox.transform.SetParent(rightCol.transform, false);
        Image pibImg = paintInfoBox.AddComponent<Image>();
        pibImg.color = new Color(0.04f, 0.06f, 0.09f, 0.85f);
        paintInfoBox.AddComponent<LayoutElement>().preferredHeight = 68;

        VerticalLayoutGroup pibLayout = paintInfoBox.AddComponent<VerticalLayoutGroup>();
        pibLayout.padding = new RectOffset(12, 12, 6, 6);
        pibLayout.spacing = 2;

        GameObject pTitleObj = new GameObject("PaintTitle");
        pTitleObj.transform.SetParent(paintInfoBox.transform, false);
        AddTMP(pTitleObj, "Fırın Boya & Renk Değişimi", 12, FontStyles.Bold, Color.white, TextAlignmentOptions.Left);

        GameObject pDescObj = new GameObject("PaintDesc");
        pDescObj.transform.SetParent(paintInfoBox.transform, false);
        AddTMP(pDescObj, "Boyanacak Hedefler: Gövde, Kaput, Kapılar ve Tamponlar", 11, FontStyles.Normal, new Color(0.58f, 0.68f, 0.82f), TextAlignmentOptions.Left);

        GameObject pCurrentObj = new GameObject("CurrentColor");
        pCurrentObj.transform.SetParent(paintInfoBox.transform, false);
        AddTMP(pCurrentObj, "<b>Mevcut Renk:</b> <color=#C5221F>■ #C5221F</color>  |  <b>Boya Ücreti:</b> <color=#32FF64>$250</color>", 12, FontStyles.Bold, new Color(0.3f, 1f, 0.5f), TextAlignmentOptions.Left);

        // Color Swatches Grid (5 columns x 2 rows = 10 Tiles)
        GameObject gridObj = new GameObject("PaletteGrid");
        gridObj.transform.SetParent(rightCol.transform, false);
        gridObj.AddComponent<LayoutElement>().preferredHeight = 265;

        GridLayoutGroup grid = gridObj.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(80, 126);
        grid.spacing = new Vector2(8, 8);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperCenter;

        (string name, string hex, string emoji)[] palette = new (string, string, string)[]
        {
            ("Kırmızı", "#C5221F", "🔴"),
            ("Mavi", "#1A73E8", "🔵"),
            ("Siyah", "#1E1E24", "🖤"),
            ("Beyaz", "#F8F9FA", "⚪"),
            ("Sarı", "#FBBC04", "🟡"),
            ("Yeşil", "#1E8E3E", "🟢"),
            ("Turuncu", "#E8710A", "🟠"),
            ("Mor", "#9334E8", "🟣"),
            ("Gri", "#5F6368", "🔘"),
            ("Turkuaz", "#00BCD4", "🩵")
        };

        for (int i = 0; i < palette.Length; i++)
        {
            var p = palette[i];
            ColorUtility.TryParseHtmlString(p.hex, out Color colVal);

            GameObject tileBtnObj = new GameObject($"PaintBtn_{i}");
            tileBtnObj.transform.SetParent(gridObj.transform, false);

            Image tileImg = tileBtnObj.AddComponent<Image>();
            if (sBtnDark != null) { tileImg.sprite = sBtnDark; tileImg.type = Image.Type.Sliced; }
            tileImg.color = new Color(0.12f, 0.16f, 0.24f, 0.95f);

            Button tileBtn = tileBtnObj.AddComponent<Button>();
            ColorBlock cb = tileBtn.colors;
            cb.highlightedColor = new Color(0.2f, 0.35f, 0.55f, 1f);
            cb.pressedColor = new Color(0.1f, 0.2f, 0.35f, 1f);
            tileBtn.colors = cb;

            VerticalLayoutGroup tileLayout = tileBtnObj.AddComponent<VerticalLayoutGroup>();
            tileLayout.padding = new RectOffset(6, 6, 8, 6);
            tileLayout.spacing = 5;
            tileLayout.childAlignment = TextAnchor.MiddleCenter;
            tileLayout.childControlWidth = true;
            tileLayout.childControlHeight = false;

            // Swatch Preview Circle / Rounded Box
            GameObject swatchObj = new GameObject("Swatch");
            swatchObj.transform.SetParent(tileBtnObj.transform, false);
            swatchObj.AddComponent<LayoutElement>().preferredHeight = 46;
            Image swatchImg = swatchObj.AddComponent<Image>();
            if (sCircleSwatch != null) { swatchImg.sprite = sCircleSwatch; }
            swatchImg.color = colVal;

            // Color Name Text
            GameObject nameObj = new GameObject("Text");
            nameObj.transform.SetParent(tileBtnObj.transform, false);
            nameObj.AddComponent<LayoutElement>().preferredHeight = 20;
            AddTMP(nameObj, p.name, 11, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);

            // Cost Badge
            GameObject costObj = new GameObject("CostBadge");
            costObj.transform.SetParent(tileBtnObj.transform, false);
            costObj.AddComponent<LayoutElement>().preferredHeight = 16;
            AddTMP(costObj, "$250", 10, FontStyles.Normal, new Color(0.4f, 1f, 0.6f), TextAlignmentOptions.Center);
        }

        return root;
    }

    private static TextMeshProUGUI AddTMP(GameObject target, string text, float size, FontStyles style, Color color, TextAlignmentOptions alignment)
    {
        target.AddComponent<CanvasRenderer>();
        TextMeshProUGUI tmp = target.AddComponent<TextMeshProUGUI>();
        if (cachedFont != null) tmp.font = cachedFont;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        return tmp;
    }
}
#endif
