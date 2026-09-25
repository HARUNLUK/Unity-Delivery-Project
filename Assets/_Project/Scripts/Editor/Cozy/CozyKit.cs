#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Building blocks for the Kurye Defteri UI. Every element gets explicit sizes (LayoutElement) so nothing
/// depends on a sprite's natural size, and every text is bounded (auto-size + ellipsis).
/// Positions follow the design mockup: Unity px at the 1920x1080 reference resolution.
/// </summary>
public static class CozyKit
{
    public const string UndoName = "Build Kurye Defteri UI";
    public static CozyAssets A;

    public enum Anchor { TL, TC, TR, ML, MC, MR, BL, BC, BR }
    public enum TextStyle { Display, H1, H2, H3, Body, BodyBold, Small, Label, Mono, Number }

    // =====================================================================
    // NODES & PLACEMENT
    // =====================================================================

    public static RectTransform Node(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, UndoName);
        go.layer = 5; // UI
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    /// <summary>x / y are distances from the anchored edge(s), like CSS left/top/right/bottom.</summary>
    public static RectTransform Place(RectTransform rt, Anchor a, float x, float y, float w, float h)
    {
        Vector2 anchor;
        switch (a)
        {
            case Anchor.TL: anchor = new Vector2(0f, 1f); break;
            case Anchor.TC: anchor = new Vector2(0.5f, 1f); break;
            case Anchor.TR: anchor = new Vector2(1f, 1f); break;
            case Anchor.ML: anchor = new Vector2(0f, 0.5f); break;
            case Anchor.MR: anchor = new Vector2(1f, 0.5f); break;
            case Anchor.BL: anchor = new Vector2(0f, 0f); break;
            case Anchor.BC: anchor = new Vector2(0.5f, 0f); break;
            case Anchor.BR: anchor = new Vector2(1f, 0f); break;
            default: anchor = new Vector2(0.5f, 0.5f); break;
        }
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        float px = anchor.x == 1f ? -x : x;
        float py = anchor.y == 1f ? -y : y;
        rt.anchoredPosition = new Vector2(px, py);
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    public static RectTransform Stretch(RectTransform rt, float left = 0f, float top = 0f, float right = 0f, float bottom = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
        return rt;
    }

    /// <summary>Full width strip glued to the top of the parent.</summary>
    public static RectTransform TopStrip(RectTransform rt, float height, float top = 0f, float left = 0f, float right = 0f)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(left, -top - height);
        rt.offsetMax = new Vector2(-right, -top);
        return rt;
    }

    public static RectTransform BottomStrip(RectTransform rt, float height, float bottom = 0f, float left = 0f, float right = 0f)
    {
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, bottom + height);
        return rt;
    }

    // =====================================================================
    // LAYOUT
    // =====================================================================

    public static RectOffset Pad(int all) => new RectOffset(all, all, all, all);
    public static RectOffset Pad(int horizontal, int vertical) => new RectOffset(horizontal, horizontal, vertical, vertical);
    public static RectOffset Pad(int left, int right, int top, int bottom) => new RectOffset(left, right, top, bottom);

    public static VerticalLayoutGroup VStack(Component c, float spacing, RectOffset padding = null, TextAnchor align = TextAnchor.UpperLeft)
    {
        VerticalLayoutGroup v = c.gameObject.AddComponent<VerticalLayoutGroup>();
        v.spacing = spacing;
        v.padding = padding ?? new RectOffset();
        v.childAlignment = align;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        return v;
    }

    public static HorizontalLayoutGroup HStack(Component c, float spacing, RectOffset padding = null, TextAnchor align = TextAnchor.MiddleLeft)
    {
        HorizontalLayoutGroup h = c.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.spacing = spacing;
        h.padding = padding ?? new RectOffset();
        h.childAlignment = align;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;
        return h;
    }

    /// <summary>Fixed / flexible size for layout groups. Pass -1 to leave a value unset.</summary>
    public static LayoutElement Size(Component c, float width = -1f, float height = -1f, float flexWidth = -1f, float flexHeight = -1f)
    {
        LayoutElement le = c.GetComponent<LayoutElement>();
        if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
        if (width >= 0f) { le.minWidth = width; le.preferredWidth = width; }
        if (height >= 0f) { le.minHeight = height; le.preferredHeight = height; }
        if (flexWidth >= 0f) le.flexibleWidth = flexWidth;
        if (flexHeight >= 0f) le.flexibleHeight = flexHeight;
        return le;
    }

    public static RectTransform Spacer(Transform parent, float flex = 1f)
    {
        RectTransform rt = Node("Spacer", parent);
        Size(rt, -1f, -1f, flex, flex);
        return rt;
    }

    public static void IgnoreLayout(Component c)
    {
        LayoutElement le = c.GetComponent<LayoutElement>();
        if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
        le.ignoreLayout = true;
    }

    // =====================================================================
    // SURFACES
    // =====================================================================

    public static Image Shape(Component c, Color color, float radius, bool raycast = false)
    {
        Image img = c.GetComponent<Image>();
        if (img == null) img = c.gameObject.AddComponent<Image>();
        if (radius > 0f)
        {
            img.sprite = A.RoundFor(radius, out float mult);
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = mult;
            img.fillCenter = true;
        }
        else
        {
            img.sprite = null;
            img.type = Image.Type.Simple;
        }
        img.color = color;
        img.raycastTarget = raycast;
        return img;
    }

    public static Shadow Lip(Graphic g, Color color, float depth)
    {
        Shadow s = g.gameObject.AddComponent<Shadow>();
        s.effectColor = color;
        s.effectDistance = new Vector2(0f, -depth);
        s.useGraphicAlpha = true;
        return s;
    }

    public static Outline Border(Graphic g, Color color, float width)
    {
        Outline o = g.gameObject.AddComponent<Outline>();
        o.effectColor = color;
        o.effectDistance = new Vector2(width, -width);
        o.useGraphicAlpha = true;
        return o;
    }

    public struct Card
    {
        public RectTransform root; // what scripts show / hide and layouts size
        public RectTransform face; // put content here
        public Image faceImage;
    }

    /// <summary>
    /// Paper card: soft drop shadow + face with a darker "lip" edge underneath.
    /// The root has no graphic, so the shadow can render behind the face.
    /// </summary>
    public static Card PaperCard(Transform parent, string name, Color face, float radius = CozyTheme.RadiusPanel, float lip = CozyTheme.LipPanel, bool softShadow = true, bool raycast = true)
    {
        RectTransform root = Node(name, parent);
        if (softShadow)
        {
            RectTransform sh = Node("Shadow", root);
            Stretch(sh, -26f, -14f, -26f, -40f);
            Image si = sh.gameObject.AddComponent<Image>();
            si.sprite = A.softShadow;
            si.type = Image.Type.Sliced;
            si.color = CozyTheme.SoftShadow;
            si.raycastTarget = false;
        }

        RectTransform f = Node("Face", root);
        Stretch(f);
        Image fi = Shape(f, face, radius, raycast);
        if (lip > 0f) Lip(fi, CozyTheme.PaperEdge, lip);
        return new Card { root = root, face = f, faceImage = fi };
    }

    /// <summary>Flat inner surface (well, list row, info tile) without shadow.</summary>
    public static Image Surface(Component c, Color color, float radius, Color? border = null, float borderWidth = 2f)
    {
        Image img = Shape(c, color, radius);
        if (border.HasValue) Border(img, border.Value, borderWidth);
        return img;
    }

    public static Image Scrim(Transform parent, string name, Color color)
    {
        RectTransform rt = Node(name, parent);
        Stretch(rt);
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = true; // blocks clicks to the world / HUD behind
        return img;
    }

    public static Image DashedLine(Transform parent, string name, Color color, float thickness = 3f)
    {
        RectTransform rt = Node(name, parent);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = A.dash;
        img.type = Image.Type.Tiled;
        img.color = color;
        img.raycastTarget = false;
        img.pixelsPerUnitMultiplier = 6f / Mathf.Max(1f, thickness);
        Size(rt, -1f, thickness, 1f);
        return img;
    }

    // =====================================================================
    // TEXT
    // =====================================================================

    public static void StyleMetrics(TextStyle style, out TMP_FontAsset font, out float size, out FontStyles fontStyle, out float spacing)
    {
        fontStyle = FontStyles.Normal;
        spacing = 0f;
        switch (style)
        {
            case TextStyle.Display: font = A.displayBold; size = 64f; break;
            case TextStyle.H1: font = A.display; size = 40f; break;
            case TextStyle.H2: font = A.display; size = 30f; break;
            case TextStyle.H3: font = A.display; size = 24f; break;
            case TextStyle.BodyBold: font = A.bodyBold; size = 22f; break;
            case TextStyle.Small: font = A.bodyBold; size = 18f; break;
            case TextStyle.Label: font = A.bodyBold; size = 17f; fontStyle = FontStyles.UpperCase; spacing = 6f; break;
            case TextStyle.Mono: font = A.mono; size = 19f; spacing = 3f; break;
            case TextStyle.Number: font = A.displayBold; size = 30f; break;
            default: font = A.body; size = 22f; break;
        }
    }

    /// <summary>Bounded text: shrinks down to 70% of its size, then cuts with "…". Never spills out.</summary>
    public static TextMeshProUGUI Txt(Transform parent, string name, string text, TextStyle style, Color color,
        TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft, float size = 0f, bool wrap = true, bool ink = true)
    {
        RectTransform rt = Node(name, parent);
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        StyleMetrics(style, out TMP_FontAsset font, out float baseSize, out FontStyles fs, out float spacing);
        float s = size > 0f ? size : baseSize;
        t.font = font;
        t.text = text;
        t.fontStyle = fs;
        t.characterSpacing = spacing;
        t.color = color;
        t.alignment = align;
        t.fontSize = s;
        t.enableAutoSizing = true;
        t.fontSizeMax = s;
        t.fontSizeMin = Mathf.Max(13f, Mathf.Round(s * 0.7f));
        t.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Ellipsis;
        t.raycastTarget = false;
        t.richText = true;
        t.margin = Vector4.zero;
        if (ink) rt.gameObject.AddComponent<CozyInkText>();
        return t;
    }

    /// <summary>Text with a fixed height inside a layout group.</summary>
    public static TextMeshProUGUI Line(Transform parent, string name, string text, TextStyle style, Color color, float height,
        TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft, float size = 0f, bool wrap = false, bool ink = true)
    {
        TextMeshProUGUI t = Txt(parent, name, text, style, color, align, size, wrap, ink);
        Size(t, -1f, height, 1f);
        return t;
    }

    // =====================================================================
    // ICONS
    // =====================================================================

    public static Image Icon(Transform parent, string name, string icon, float size, Color color)
    {
        RectTransform rt = Node(name, parent);
        rt.sizeDelta = new Vector2(size, size);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = A.Icon(icon);
        img.color = color;
        img.preserveAspect = true;
        img.raycastTarget = false;
        Size(rt, size, size);
        return img;
    }

    /// <summary>Round badge with an icon in the middle (58% of the badge).</summary>
    public static Image Badge(Transform parent, string name, string icon, float size, Color background, Color foreground)
    {
        RectTransform rt = Node(name, parent);
        rt.sizeDelta = new Vector2(size, size);
        Image bg = rt.gameObject.AddComponent<Image>();
        bg.sprite = A.circle;
        bg.color = background;
        bg.raycastTarget = false;
        Size(rt, size, size);

        Image ic = Icon(rt, "Icon", icon, size * 0.58f, foreground);
        Place((RectTransform)ic.transform, Anchor.MC, 0f, 0f, size * 0.58f, size * 0.58f);
        IgnoreLayout(ic);
        return bg;
    }

    // =====================================================================
    // BUTTONS
    // =====================================================================

    public static Button Btn(Transform parent, string name, string label, CozyTheme.ButtonStyle style,
        float height = 64f, string icon = null, float fontSize = 26f, bool alignLeft = false)
    {
        CozyTheme.ButtonColors bc = CozyTheme.GetButtonColors(style);
        RectTransform rt = Node(name, parent);
        rt.sizeDelta = new Vector2(240f, height);

        Image face = Shape(rt, bc.face, CozyTheme.RadiusButton, true);
        Shadow lip = null;
        if (style == CozyTheme.ButtonStyle.Soft)
        {
            Border(face, CozyTheme.KraftDeep, 2.5f);
        }
        if (style != CozyTheme.ButtonStyle.Ghost) lip = Lip(face, bc.lip, CozyTheme.LipButton);

        UnityEngine.UI.Button btn = rt.gameObject.AddComponent<UnityEngine.UI.Button>();
        btn.targetGraphic = face;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.07f, 1.07f, 1.07f, 1f);
        cb.selectedColor = Color.white;
        cb.pressedColor = new Color(0.93f, 0.93f, 0.93f, 1f);
        cb.disabledColor = new Color(0.78f, 0.76f, 0.74f, 0.65f);
        cb.fadeDuration = 0.08f;
        btn.colors = cb;

        RectTransform inner = Node("Inner", rt);
        Stretch(inner, 20f, 0f, 20f, 0f);

        TextAlignmentOptions align = alignLeft ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Center;
        if (!string.IsNullOrEmpty(icon))
        {
            HStack(inner, 12f, null, alignLeft ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter);
            Icon(inner, "Icon", icon, Mathf.Min(30f, height * 0.44f), bc.text);
            TextMeshProUGUI t = Txt(inner, "Label", label, TextStyle.H3, bc.text, align, fontSize, false, style == CozyTheme.ButtonStyle.Soft || style == CozyTheme.ButtonStyle.Ghost);
            Size(t, -1f, height, alignLeft ? 1f : 0f);
        }
        else
        {
            TextMeshProUGUI t = Txt(inner, "Label", label, TextStyle.H3, bc.text, align, fontSize, true, style == CozyTheme.ButtonStyle.Soft || style == CozyTheme.ButtonStyle.Ghost);
            Stretch(t.rectTransform);
        }

        CozyButton cozy = rt.gameObject.AddComponent<CozyButton>();
        cozy.inner = inner;
        cozy.lip = lip;
        cozy.lipDepth = CozyTheme.LipButton;

        Size(rt, -1f, height);
        return btn;
    }

    public static Button RoundButton(Transform parent, string name, string icon, CozyTheme.ButtonStyle style, float size = 56f)
    {
        CozyTheme.ButtonColors bc = CozyTheme.GetButtonColors(style);
        RectTransform rt = Node(name, parent);
        rt.sizeDelta = new Vector2(size, size);
        Image face = rt.gameObject.AddComponent<Image>();
        face.sprite = A.circle;
        face.color = bc.face;
        face.raycastTarget = true;
        if (style == CozyTheme.ButtonStyle.Soft) Border(face, CozyTheme.KraftDeep, 2.5f);
        Shadow lip = Lip(face, bc.lip, 5f);

        UnityEngine.UI.Button btn = rt.gameObject.AddComponent<UnityEngine.UI.Button>();
        btn.targetGraphic = face;
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(1.07f, 1.07f, 1.07f, 1f);
        cb.pressedColor = new Color(0.93f, 0.93f, 0.93f, 1f);
        btn.colors = cb;

        RectTransform inner = Node("Inner", rt);
        Stretch(inner);
        Image ic = Icon(inner, "Icon", icon, size * 0.46f, bc.text);
        Place((RectTransform)ic.transform, Anchor.MC, 0f, 0f, size * 0.46f, size * 0.46f);

        CozyButton cozy = rt.gameObject.AddComponent<CozyButton>();
        cozy.inner = inner;
        cozy.lip = lip;
        cozy.lipDepth = 5f;
        cozy.pressDepth = 3f;

        Size(rt, size, size);
        return btn;
    }

    public static TextMeshProUGUI LabelOf(UnityEngine.UI.Button b)
    {
        Transform t = b.transform.Find("Inner/Label");
        return t != null ? t.GetComponent<TextMeshProUGUI>() : b.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    // =====================================================================
    // CHIPS, KEY CAPS, BARS
    // =====================================================================

    /// <summary>Pill-shaped status / type tag. Grows with its text.</summary>
    public static Image Chip(Transform parent, string name, string text, CozyTheme.Tone tone, string icon = null, float height = 34f, float fontSize = 17f)
    {
        CozyTheme.GetTone(tone, out Color bg, out Color fg);
        RectTransform rt = Node(name, parent);
        rt.sizeDelta = new Vector2(120f, height);
        Image img = Shape(rt, bg, height * 0.5f);
        if (tone == CozyTheme.Tone.Outline) Border(img, CozyTheme.KraftDeep, 2f);

        HStack(rt, 6f, Pad(14, 14, 0, 0), TextAnchor.MiddleCenter);
        ContentSizeFitter fit = rt.gameObject.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fit.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        if (!string.IsNullOrEmpty(icon)) Icon(rt, "Icon", icon, height * 0.53f, fg);
        TextMeshProUGUI t = Txt(rt, "Text", text, TextStyle.BodyBold, fg, TextAlignmentOptions.Center, fontSize, false, false);
        t.enableAutoSizing = false;
        Size(t, -1f, height);

        Size(rt, -1f, height);
        return img;
    }

    public static RectTransform KeyCap(Transform parent, string key, float size = 42f)
    {
        RectTransform rt = Node("Key_" + key, parent);
        rt.sizeDelta = new Vector2(size, size);
        Image face = Shape(rt, Color.white, CozyTheme.RadiusKey);
        Border(face, CozyTheme.Ink, 2.5f);
        Lip(face, CozyTheme.Ink, 4f);
        HStack(rt, 0f, Pad(10, 10, 0, 0), TextAnchor.MiddleCenter);
        ContentSizeFitter fit = rt.gameObject.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        TextMeshProUGUI t = Txt(rt, "Text", key, TextStyle.H3, CozyTheme.Ink, TextAlignmentOptions.Center, size * 0.48f, false, false);
        t.font = A.displayBold;
        t.enableAutoSizing = false;
        Size(t, -1f, size);
        LayoutElement le = Size(rt, -1f, size);
        le.minWidth = size;
        return rt;
    }

    /// <summary>Rounded progress bar. Returns the fill; its width is driven by anchorMax.x (0..1).</summary>
    public static Image Bar(Transform parent, string name, float fill, Color fillColor, float height = 16f, Color? track = null)
    {
        RectTransform rt = Node(name, parent);
        rt.sizeDelta = new Vector2(200f, height);
        Shape(rt, track ?? CozyTheme.Track, height * 0.5f);
        Size(rt, -1f, height, 1f);

        RectTransform f = Node(name.Replace("Bg", "").Replace("Track", "") + "Fill", rt);
        f.anchorMin = new Vector2(0f, 0f);
        f.anchorMax = new Vector2(Mathf.Clamp01(fill), 1f);
        f.offsetMin = Vector2.zero;
        f.offsetMax = Vector2.zero;
        Image fi = Shape(f, fillColor, height * 0.5f);
        return fi;
    }

    // =====================================================================
    // LISTS
    // =====================================================================

    public static ScrollRect ScrollList(Transform parent, string name, out RectTransform content, float spacing = 8f, int padding = 10, bool well = true)
    {
        RectTransform rt = Node(name, parent);
        if (well) Surface(rt, CozyTheme.Well, 20f);
        else
        {
            Image hit = rt.gameObject.AddComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0f);
        }

        ScrollRect sr = rt.gameObject.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 45f;
        sr.inertia = true;
        sr.decelerationRate = 0.12f;

        RectTransform viewport = Node("Viewport", rt);
        Stretch(viewport, padding, padding, padding + 14f, padding);
        viewport.gameObject.AddComponent<RectMask2D>();
        Image vpHit = viewport.gameObject.AddComponent<Image>();
        vpHit.color = new Color(1f, 1f, 1f, 0f); // lets the wheel scroll over empty space

        content = Node("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 0f);
        VerticalLayoutGroup v = VStack(content, spacing);
        v.childForceExpandWidth = true;
        ContentSizeFitter fit = content.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Slim scrollbar
        RectTransform bar = Node("Scrollbar", rt);
        bar.anchorMin = new Vector2(1f, 0f);
        bar.anchorMax = new Vector2(1f, 1f);
        bar.pivot = new Vector2(1f, 0.5f);
        bar.offsetMin = new Vector2(-padding - 8f, padding);
        bar.offsetMax = new Vector2(-padding, -padding);
        Image barImg = Shape(bar, new Color(CozyTheme.KraftDeep.r, CozyTheme.KraftDeep.g, CozyTheme.KraftDeep.b, 0.35f), 4f, true);
        Scrollbar sb = bar.gameObject.AddComponent<Scrollbar>();
        sb.direction = Scrollbar.Direction.BottomToTop;
        RectTransform area = Node("Sliding Area", bar);
        Stretch(area);
        RectTransform handle = Node("Handle", area);
        Stretch(handle);
        Image handleImg = Shape(handle, CozyTheme.KraftDeep, 4f, true);
        sb.handleRect = handle;
        sb.targetGraphic = handleImg;
        sr.verticalScrollbar = sb;
        sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        sr.viewport = viewport;
        sr.content = content;
        barImg.raycastTarget = true;
        return sr;
    }

    // =====================================================================
    // FORM CONTROLS
    // =====================================================================

    public static Slider SliderCtrl(Transform parent, string name, float height = 34f)
    {
        RectTransform rt = Node(name, parent);
        rt.sizeDelta = new Vector2(400f, height);
        Slider s = rt.gameObject.AddComponent<Slider>();

        RectTransform bg = Node("Background", rt);
        bg.anchorMin = new Vector2(0f, 0.5f);
        bg.anchorMax = new Vector2(1f, 0.5f);
        bg.sizeDelta = new Vector2(0f, 14f);
        Shape(bg, CozyTheme.Track, 7f, true);

        RectTransform fillArea = Node("Fill Area", rt);
        fillArea.anchorMin = new Vector2(0f, 0.5f);
        fillArea.anchorMax = new Vector2(1f, 0.5f);
        fillArea.sizeDelta = new Vector2(0f, 14f);
        RectTransform fill = Node("Fill", fillArea);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = new Vector2(0f, 1f);
        fill.sizeDelta = Vector2.zero;
        Shape(fill, CozyTheme.Sky, 7f);

        RectTransform handleArea = Node("Handle Slide Area", rt);
        Stretch(handleArea, 17f, 0f, 17f, 0f);
        RectTransform handle = Node("Handle", handleArea);
        handle.sizeDelta = new Vector2(34f, 34f);
        handle.anchorMin = new Vector2(0f, 0.5f);
        handle.anchorMax = new Vector2(0f, 0.5f);
        Image h = handle.gameObject.AddComponent<Image>();
        h.sprite = A.circle;
        h.color = Color.white;
        Border(h, CozyTheme.Sky, 3.5f);
        Lip(h, CozyTheme.SkyLip, 3f);

        s.fillRect = fill;
        s.handleRect = handle;
        s.targetGraphic = h;
        s.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
        ColorBlock cb = s.colors;
        cb.highlightedColor = new Color(1.05f, 1.05f, 1.05f, 1f);
        cb.pressedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        s.colors = cb;
        Size(rt, -1f, height, 1f);
        return s;
    }

    public static Toggle ToggleCtrl(Transform parent, string name, float size = 40f)
    {
        RectTransform rt = Node(name, parent);
        rt.sizeDelta = new Vector2(size, size);
        Image bg = Shape(rt, CozyTheme.RowFace, 10f, true);
        Border(bg, CozyTheme.KraftDeep, 2.5f);

        Toggle tg = rt.gameObject.AddComponent<Toggle>();
        RectTransform check = Node("Checkmark", rt);
        Stretch(check, 4f, 4f, 4f, 4f);
        Image ck = Shape(check, CozyTheme.Mint, 7f);
        Image icon = Icon(check, "Icon", "check", size * 0.6f, Color.white);
        Place((RectTransform)icon.transform, Anchor.MC, 0f, 0f, size * 0.6f, size * 0.6f);
        tg.graphic = ck;
        tg.targetGraphic = bg;
        tg.isOn = false;
        Size(rt, size, size);
        return tg;
    }

    public static TMP_Dropdown DropdownCtrl(Transform parent, string name, float height = 52f)
    {
        TMP_DefaultControls.Resources res = new TMP_DefaultControls.Resources
        {
            standard = A.round16,
            background = A.round16,
            inputField = A.round16,
            knob = A.circle,
            checkmark = A.Icon("check"),
            dropdown = A.Icon("chevron"),
            mask = A.round16
        };
        GameObject go = TMP_DefaultControls.CreateDropdown(res);
        Undo.RegisterCreatedObjectUndo(go, UndoName);
        go.name = name;
        go.layer = 5;
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(360f, height);

        TMP_Dropdown dd = go.GetComponent<TMP_Dropdown>();
        Image field = go.GetComponent<Image>();
        Shape(field, CozyTheme.RowFace, 16f, true);
        Border(field, CozyTheme.KraftDeep, 2.5f);

        if (dd.captionText != null)
        {
            StyleTMP(dd.captionText, TextStyle.BodyBold, CozyTheme.Ink, 20f);
            RectTransform cr = dd.captionText.rectTransform;
            cr.offsetMin = new Vector2(18f, 4f);
            cr.offsetMax = new Vector2(-48f, -4f);
        }

        Transform arrow = go.transform.Find("Arrow");
        if (arrow != null)
        {
            Image ai = arrow.GetComponent<Image>();
            ai.color = CozyTheme.InkSoft;
            ai.transform.localEulerAngles = new Vector3(0f, 0f, -90f);
            RectTransform ar = (RectTransform)arrow;
            ar.sizeDelta = new Vector2(22f, 22f);
            ar.anchoredPosition = new Vector2(-22f, 0f);
        }

        RectTransform template = dd.template;
        if (template != null)
        {
            Image ti = template.GetComponent<Image>();
            Shape(ti, CozyTheme.RowFace, 16f, true);
            Border(ti, CozyTheme.KraftDeep, 2.5f);
            template.sizeDelta = new Vector2(0f, 240f);

            Toggle item = template.GetComponentInChildren<Toggle>(true);
            if (item != null)
            {
                RectTransform itemRt = (RectTransform)item.transform;
                itemRt.sizeDelta = new Vector2(itemRt.sizeDelta.x, 44f);
                if (itemRt.parent is RectTransform contentRt) contentRt.sizeDelta = new Vector2(contentRt.sizeDelta.x, 52f);

                if (item.targetGraphic is Image ib)
                {
                    Shape(ib, Color.white, 12f, true);
                    ((RectTransform)ib.transform).offsetMin = new Vector2(4f, 2f);
                    ((RectTransform)ib.transform).offsetMax = new Vector2(-4f, -2f);
                }
                ColorBlock cb = item.colors;
                cb.normalColor = CozyTheme.RowFace;
                cb.highlightedColor = CozyTheme.Kraft;
                cb.selectedColor = CozyTheme.Kraft;
                cb.pressedColor = CozyTheme.KraftDeep;
                item.colors = cb;
                if (item.graphic is Image ck) { ck.color = CozyTheme.Mint; }
            }
            if (dd.itemText != null)
            {
                StyleTMP(dd.itemText, TextStyle.BodyBold, CozyTheme.Ink, 20f);
            }

            Scrollbar sb = template.GetComponentInChildren<Scrollbar>(true);
            if (sb != null)
            {
                Shape(sb.GetComponent<Image>(), new Color(CozyTheme.KraftDeep.r, CozyTheme.KraftDeep.g, CozyTheme.KraftDeep.b, 0.3f), 4f, true);
                if (sb.handleRect != null) Shape(sb.handleRect.GetComponent<Image>(), CozyTheme.KraftDeep, 4f, true);
                ((RectTransform)sb.transform).sizeDelta = new Vector2(10f, 0f);
            }
        }

        Size(rt, -1f, height, 1f);
        return dd;
    }

    public static TMP_InputField SearchField(Transform parent, string name, string placeholder, float height = 60f)
    {
        RectTransform rt = Node(name, parent);
        rt.sizeDelta = new Vector2(400f, height);
        Image bg = Shape(rt, Color.white, 20f, true);
        Border(bg, CozyTheme.Line, 2.5f);

        Image icon = Icon(rt, "SearchIcon", "search", 26f, CozyTheme.InkSoft);
        Place((RectTransform)icon.transform, Anchor.ML, 18f, 0f, 26f, 26f);
        IgnoreLayout(icon);

        RectTransform area = Node("Text Area", rt);
        Stretch(area, 58f, 6f, 18f, 6f);
        area.gameObject.AddComponent<RectMask2D>();

        TextMeshProUGUI ph = Txt(area, "Placeholder", placeholder, TextStyle.Body, CozyTheme.InkSoft, TextAlignmentOptions.MidlineLeft, 22f, false, false);
        Stretch(ph.rectTransform);
        ph.enableAutoSizing = false;
        TextMeshProUGUI txt = Txt(area, "Text", "", TextStyle.BodyBold, CozyTheme.Ink, TextAlignmentOptions.MidlineLeft, 22f, false, false);
        Stretch(txt.rectTransform);
        txt.enableAutoSizing = false;
        txt.overflowMode = TextOverflowModes.Overflow;

        TMP_InputField input = rt.gameObject.AddComponent<TMP_InputField>();
        input.textViewport = area;
        input.textComponent = txt;
        input.placeholder = ph;
        input.targetGraphic = bg;
        input.fontAsset = A.bodyBold;
        input.pointSize = 22f;
        input.caretColor = CozyTheme.Ink;
        input.customCaretColor = true;
        input.selectionColor = new Color(CozyTheme.Honey.r, CozyTheme.Honey.g, CozyTheme.Honey.b, 0.5f);
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.characterLimit = 40;
        Size(rt, -1f, height, 1f);
        return input;
    }

    public static void StyleTMP(TMP_Text t, TextStyle style, Color color, float size = 0f)
    {
        StyleMetrics(style, out TMP_FontAsset font, out float baseSize, out FontStyles fs, out float spacing);
        float s = size > 0f ? size : baseSize;
        t.font = font;
        t.fontStyle = fs;
        t.characterSpacing = spacing;
        t.color = color;
        t.fontSize = s;
        t.enableAutoSizing = true;
        t.fontSizeMax = s;
        t.fontSizeMin = Mathf.Max(13f, Mathf.Round(s * 0.7f));
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Ellipsis;
    }

    // =====================================================================
    // SERIALIZED FIELD BINDING
    // =====================================================================

    public static void Bind(Object target, string field, Object value)
    {
        if (target == null) return;
        SerializedObject so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null)
        {
            Debug.LogWarning($"[CozyKit] {target.GetType().Name} has no field '{field}'.");
            return;
        }
        p.objectReferenceValue = value;
        so.ApplyModifiedProperties();
    }

    public static void BindColor(Object target, string field, Color value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null) return;
        p.colorValue = value;
        so.ApplyModifiedProperties();
    }
}
#endif
