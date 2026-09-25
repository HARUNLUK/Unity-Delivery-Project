#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Generates everything the Kurye Defteri UI needs: rounded shape sprites, tiled patterns,
/// stroke icons (rasterised from the same SVG paths as the design mockup) and TMP font assets.
/// </summary>
public static class CozyAssetGenerator
{
    public const string SpriteDir = "Assets/_Project/UI/Cozy/Sprites";
    public const string IconDir = "Assets/_Project/UI/Cozy/Icons";
    public const string FontDir = "Assets/_Project/Fonts/Cozy";
    public const string AssetsPath = "Assets/_Project/Resources/CozyUI/CozyAssets.asset";

    private const string FontCharset =
        " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
        "ÇçĞğİıÖöŞşÜüÂâÎîÛûÉéÈèÀà€₺£•–—…‘’“”·×÷−→←↑↓↺✓✕°±©®™ ";

    [MenuItem("Tools/Delivery Game/Cozy UI/1. Generate Assets (fonts, shapes, icons)", false, 1)]
    public static void GenerateMenu()
    {
        CozyAssets assets = GenerateAll(true);
        EditorUtility.DisplayDialog("Kurye Defteri UI", assets != null
            ? "Fontlar, şekiller ve ikonlar üretildi:\n" + AssetsPath
            : "Varlıklar üretilemedi, Console'a bakın.", "Tamam");
    }

    /// <summary>Loads the asset bundle, generating whatever is missing.</summary>
    public static CozyAssets EnsureAssets()
    {
        CozyAssets existing = AssetDatabase.LoadAssetAtPath<CozyAssets>(AssetsPath);
        if (existing != null && existing.display != null && existing.round32 != null && existing.icons.Count > 0) return existing;
        return GenerateAll(false);
    }

    public static CozyAssets GenerateAll(bool force)
    {
        EnsureFolder(SpriteDir);
        EnsureFolder(IconDir);
        EnsureFolder(Path.GetDirectoryName(AssetsPath).Replace('\\', '/'));

        CozyAssets assets = AssetDatabase.LoadAssetAtPath<CozyAssets>(AssetsPath);
        if (assets == null)
        {
            assets = ScriptableObject.CreateInstance<CozyAssets>();
            AssetDatabase.CreateAsset(assets, AssetsPath);
        }

        // ---- Shapes ----
        assets.round16 = RoundedSprite("Cozy_Round16", 16, force);
        assets.round32 = RoundedSprite("Cozy_Round32", 32, force);
        assets.round64 = RoundedSprite("Cozy_Round64", 64, force);
        assets.circle = CircleSprite("Cozy_Circle", 128, force);
        assets.softShadow = ShadowSprite("Cozy_SoftShadow", force);
        assets.dash = PatternSprite("Cozy_Dash", 20, 6, (x, y) => x < 12 ? 1f : 0f, force);
        assets.lined = PatternSprite("Cozy_Lined", 8, 37, (x, y) => y <= 1 ? 1f : 0f, force);
        assets.zigzag = PatternSprite("Cozy_Zigzag", 28, 14, (x, y) =>
        {
            float peak = 14f * (1f - Mathf.Abs(x + 0.5f - 14f) / 14f);
            return Mathf.Clamp01(peak - (y + 0.5f) + 0.5f);
        }, force);

        // ---- Icons ----
        assets.icons.Clear();
        foreach (IconDef def in Icons)
        {
            Sprite s = IconSprite(def, force);
            if (s != null) assets.icons.Add(new CozyAssets.NamedSprite { name = def.name, sprite = s });
        }

        // ---- Fonts ----
        TMP_FontAsset nunitoBold = FontAsset("Nunito-ExtraBold", force);
        TMP_FontAsset nunito = FontAsset("Nunito-SemiBold", force);
        TMP_FontAsset fredoka = FontAsset("Fredoka-SemiBold", force);
        TMP_FontAsset fredokaBold = FontAsset("Fredoka-Bold", force);
        TMP_FontAsset mono = FontAsset("CourierPrime-Bold", force);

        // Fredoka's static files lack ğ / ş / İ: fall back to the equally rounded Nunito.
        AddFallback(fredoka, nunitoBold);
        AddFallback(fredokaBold, nunitoBold);
        AddFallback(nunito, nunitoBold);
        AddFallback(mono, nunitoBold);

        assets.display = fredoka;
        assets.displayBold = fredokaBold;
        assets.body = nunito;
        assets.bodyBold = nunitoBold;
        assets.mono = mono;

        EditorUtility.SetDirty(assets);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return assets;
    }

    // =====================================================================
    // FONTS
    // =====================================================================

    private static TMP_FontAsset FontAsset(string baseName, bool force)
    {
        string ttfPath = $"{FontDir}/{baseName}.ttf";
        string assetPath = $"{FontDir}/{baseName} SDF.asset";

        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (existing != null && !force) return existing;

        Font font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
        if (font == null)
        {
            Debug.LogError("[CozyAssetGenerator] Font file missing: " + ttfPath);
            return existing;
        }

        if (existing != null)
        {
            // Refresh glyphs in place so references in scenes stay valid.
            existing.ClearFontAssetData(true);
            existing.TryAddCharacters(FontCharset, out _);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        TMP_FontAsset fa = TMP_FontAsset.CreateFontAsset(font, 56, 6, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, false);
        if (fa == null)
        {
            Debug.LogError("[CozyAssetGenerator] Could not create font asset for " + baseName);
            return null;
        }

        fa.name = baseName + " SDF";
        AssetDatabase.CreateAsset(fa, assetPath);

        fa.atlasTextures[0].name = baseName + " Atlas";
        AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
        fa.material.name = baseName + " Material";
        AssetDatabase.AddObjectToAsset(fa.material, fa);

        fa.TryAddCharacters(FontCharset, out string missing);
        if (!string.IsNullOrEmpty(missing))
        {
            Debug.Log($"[CozyAssetGenerator] {baseName}: {missing.Length} characters come from the fallback font.");
        }

        EditorUtility.SetDirty(fa);
        AssetDatabase.SaveAssets();
        return fa;
    }

    private static void AddFallback(TMP_FontAsset font, TMP_FontAsset fallback)
    {
        if (font == null || fallback == null || font == fallback) return;
        if (font.fallbackFontAssetTable == null) font.fallbackFontAssetTable = new List<TMP_FontAsset>();
        if (!font.fallbackFontAssetTable.Contains(fallback)) font.fallbackFontAssetTable.Add(fallback);
        EditorUtility.SetDirty(font);
    }

    // =====================================================================
    // SHAPES
    // =====================================================================

    private static Sprite RoundedSprite(string name, int radius, bool force)
    {
        int size = radius * 2 + 4;
        return WriteSprite(SpriteDir, name, size, size, (x, y) =>
        {
            Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
            float d = RoundedBoxDistance(p, new Vector2(size * 0.5f, size * 0.5f), new Vector2(size * 0.5f - 2f, size * 0.5f - 2f), radius);
            return Mathf.Clamp01(0.5f - d);
        }, new Vector4(radius + 2, radius + 2, radius + 2, radius + 2), TextureWrapMode.Clamp, force);
    }

    private static Sprite CircleSprite(string name, int size, bool force)
    {
        float r = size * 0.5f - 1f;
        return WriteSprite(SpriteDir, name, size, size, (x, y) =>
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size * 0.5f, size * 0.5f)) - r;
            return Mathf.Clamp01(0.5f - d);
        }, Vector4.zero, TextureWrapMode.Clamp, force);
    }

    private static Sprite ShadowSprite(string name, bool force)
    {
        const int size = 128;
        const float blur = 26f;
        return WriteSprite(SpriteDir, name, size, size, (x, y) =>
        {
            Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
            float d = RoundedBoxDistance(p, new Vector2(64f, 64f), new Vector2(34f, 34f), 24f);
            float k = Mathf.Clamp01(1f - (d + 4f) / blur);
            return k * k * (3f - 2f * k);
        }, new Vector4(56, 56, 56, 56), TextureWrapMode.Clamp, force);
    }

    private static Sprite PatternSprite(string name, int w, int h, System.Func<int, int, float> alpha, bool force)
    {
        return WriteSprite(SpriteDir, name, w, h, alpha, Vector4.zero, TextureWrapMode.Repeat, force);
    }

    private static float RoundedBoxDistance(Vector2 p, Vector2 center, Vector2 halfSize, float radius)
    {
        Vector2 q = new Vector2(Mathf.Abs(p.x - center.x), Mathf.Abs(p.y - center.y)) - (halfSize - new Vector2(radius, radius));
        Vector2 outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
        return outside.magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
    }

    // =====================================================================
    // ICONS (stroke rasteriser for a small SVG path subset)
    // =====================================================================

    private class IconDef
    {
        public string name;
        public string[] paths = new string[0];
        public Vector3[] circles = new Vector3[0];      // cx, cy, r
        public Vector4[] rects = new Vector4[0];        // x, y, w, h
        public float[] rectRadii = new float[0];
    }

    private static IconDef I(string name, params string[] paths) => new IconDef { name = name, paths = paths };
    private static IconDef IC(string name, Vector3[] circles, params string[] paths) => new IconDef { name = name, paths = paths, circles = circles };

    private static readonly IconDef[] Icons =
    {
        IC("clock", new[] { new Vector3(12, 12, 9) }, "M12 7v5l3 2"),
        IC("sun", new[] { new Vector3(12, 12, 4.2f) }, "M12 2.5v2.2M12 19.3v2.2M2.5 12h2.2M19.3 12h2.2M5.3 5.3l1.6 1.6M17.1 17.1l1.6 1.6M5.3 18.7l1.6-1.6M17.1 6.9l1.6-1.6"),
        I("box", "M3.5 7.8 12 3.2l8.5 4.6v8.4L12 20.8l-8.5-4.6z", "M3.5 7.8 12 12.4l8.5-4.6M12 12.4v8.4M7.8 5.5l8.5 4.6"),
        IC("coin", new[] { new Vector3(12, 12, 9) }, "M14.6 9.4c-.5-.8-1.4-1.3-2.6-1.3-1.5 0-2.6.8-2.6 1.9 0 2.7 5.3 1.4 5.3 4.1 0 1.1-1.1 1.9-2.7 1.9-1.2 0-2.1-.5-2.7-1.3M12 6.3v1.8M12 15.9v1.8"),
        I("fuel", "M4.5 20.5V5.5a2 2 0 0 1 2-2h5.5a2 2 0 0 1 2 2v15M3.5 20.5h11.5M14 9.5h1.8a2 2 0 0 1 2 2v4.8a1.4 1.4 0 0 0 2.8 0V8.3l-2.8-2.8M7.3 8h4"),
        I("wrench", "M14.6 6.2a4 4 0 0 0-5.3 5.2L3.8 16.9l3.3 3.3 5.5-5.5a4 4 0 0 0 5.2-5.3l-2.6 2.6-2.4-.6-.6-2.4z"),
        IC("pin", new[] { new Vector3(12, 9.6f, 2.5f) }, "M12 21s-7-6.1-7-11.4a7 7 0 0 1 14 0C19 14.9 12 21 12 21z"),
        I("check", "m5 12.6 4.4 4.4L19 7.4"),
        I("x", "M6.5 6.5l11 11M17.5 6.5l-11 11"),
        IC("truck", new[] { new Vector3(6.5f, 17.5f, 1.9f), new Vector3(17, 17.5f, 1.9f) }, "M2.5 6h11v10.5h-11zM13.5 10h4l3.5 3.4v3.1h-7.5"),
        I("home", "M3.5 11 12 4.2l8.5 6.8M5.5 9.6v10.4h13V9.6M10 20v-5.5h4V20"),
        IC("search", new[] { new Vector3(11, 11, 6.8f) }, "m20 20-4-4"),
        I("fragile", "M7 3.5h10l-.8 6.2a4.3 4.3 0 0 1-8.4 0zM12 14v6.5M8.5 20.5h7M10.5 3.5l1.3 3-1.6 2.2"),
        I("bolt", "M13.2 2.8 4.8 13.6h6.4l-1 7.6 8.6-10.9h-6.5z"),
        I("flame", "M12 21c-3.9 0-6.5-2.6-6.5-6.1 0-3.9 3.6-5.6 4.1-10.4 3.1 1.9 4.4 4.3 4.3 6.9 1-.6 1.7-1.7 1.9-3 1.7 1.7 2.7 3.9 2.7 6.4 0 3.6-2.6 6.2-6.5 6.2z"),
        I("volume", "M4 9.3v5.4h3.8L12.8 19V5L7.8 9.3zM16 9a4.3 4.3 0 0 1 0 6M18.6 6.5a8 8 0 0 1 0 11"),
        new IconDef { name = "monitor", paths = new[] { "M8.5 20h7M12 16v4" }, rects = new[] { new Vector4(3, 4, 18, 12) }, rectRadii = new[] { 2.2f } },
        new IconDef { name = "keys", paths = new[] { "M6.5 10h.01M10 10h.01M14 10h.01M17.5 10h.01M7.5 14h9" }, rects = new[] { new Vector4(2.5f, 6, 19, 12) }, rectRadii = new[] { 2.4f } },
        IC("globe", new[] { new Vector3(12, 12, 9) }, "M3 12h18M12 3c2.6 2.6 3.8 5.6 3.8 9s-1.2 6.4-3.8 9c-2.6-2.6-3.8-5.6-3.8-9S9.4 5.6 12 3z"),
        I("shield", "M12 3.2 19.5 6v5.7c0 4.6-3.2 7.9-7.5 9.1-4.3-1.2-7.5-4.5-7.5-9.1V6z", "m8.8 12.2 2.3 2.3 4.2-4.3"),
        new IconDef { name = "lock", paths = new[] { "M8.3 10.5V8a3.7 3.7 0 0 1 7.4 0v2.5" }, rects = new[] { new Vector4(5, 10.5f, 14, 10) }, rectRadii = new[] { 2.4f } },
        I("play", "M8 5.5v13l10.5-6.5z"),
        I("plus", "M12 5v14M5 12h14"),
        IC("gear", new[] { new Vector3(15.3f, 7, 2.2f), new Vector3(9.8f, 17, 2.2f) }, "M4 7h9M17.5 7H20M4 17h3.5M12 17h8"),
        I("door", "M14 4H6.5v16H14M10.5 12h10M17.5 8.5 21 12l-3.5 3.5"),
        I("hand", "M8 11.5V5.2a1.6 1.6 0 0 1 3.2 0v5.3M11.2 10V4a1.6 1.6 0 0 1 3.2 0v6M14.4 10.2V5.6a1.6 1.6 0 0 1 3.2 0v7.9c0 4.2-2.6 7-6.4 7-2.6 0-4.1-1.2-5.5-3.3l-2.2-3.4a1.6 1.6 0 0 1 2.6-1.8l1.6 2"),
        I("back", "M4.5 12a7.5 7.5 0 1 0 2.3-5.4M4.5 4.5v3.8h3.8"),
        I("paint", "M18.5 3.5l2 2-9.2 9.2-2.8.8.8-2.8zM8 15.5c-2.2 0-3.5 1.6-3.5 3.6 0 .7-.4 1.2-1 1.4 1 .6 2.1.9 3.3.9 2.4 0 4.2-1.8 4.2-4.2"),
        I("star", "M12 3.5l2.6 5.3 5.9.9-4.3 4.1 1 5.8L12 16.9l-5.2 2.7 1-5.8-4.3-4.1 5.9-.9z"),
        IC("info", new[] { new Vector3(12, 12, 9) }, "M12 11v5.5M12 7.6v.01"),
        I("alert", "M12 4 21 19.5H3z", "M12 10v4.5M12 17.4v.01"),
        I("arrow", "M5 12h14M13 6l6 6-6 6"),
        IC("user", new[] { new Vector3(12, 8.5f, 3.8f) }, "M4.5 20c.8-3.6 3.8-5.6 7.5-5.6s6.7 2 7.5 5.6"),
        I("building", "M4 20.5V8l8-4.5L20 8v12.5M9 20.5v-5h6v5M8 11h.01M12 11h.01M16 11h.01"),
        I("receipt", "M6 3.5h12v17l-2-1.3-2 1.3-2-1.3-2 1.3-2-1.3-2 1.3zM9 8h6M9 12h6M9 16h3"),
        I("pause", "M9 5.5v13M15 5.5v13"),
        I("chevron", "M9 5.5l6.5 6.5L9 18.5"),
    };

    private const int IconSize = 128;
    private const float StrokeWidth = 2.2f;

    private static Sprite IconSprite(IconDef def, bool force)
    {
        List<List<Vector2>> lines = new List<List<Vector2>>();
        foreach (string p in def.paths) lines.AddRange(ParsePath(p));
        foreach (Vector3 c in def.circles) lines.Add(CirclePolyline(c.x, c.y, c.z));
        for (int i = 0; i < def.rects.Length; i++)
        {
            float r = i < def.rectRadii.Length ? def.rectRadii[i] : 0f;
            lines.Add(RoundedRectPolyline(def.rects[i], r));
        }

        List<Vector4> segments = new List<Vector4>();
        foreach (List<Vector2> line in lines)
        {
            if (line.Count == 1) segments.Add(new Vector4(line[0].x, line[0].y, line[0].x, line[0].y));
            for (int i = 0; i + 1 < line.Count; i++) segments.Add(new Vector4(line[i].x, line[i].y, line[i + 1].x, line[i + 1].y));
        }

        float scale = IconSize / 24f;
        float halfWidthPx = StrokeWidth * scale * 0.5f;

        return WriteSprite(IconDir, "Icon_" + def.name, IconSize, IconSize, (x, y) =>
        {
            Vector2 p = new Vector2((x + 0.5f) / scale, (IconSize - y - 0.5f) / scale);
            float best = float.MaxValue;
            foreach (Vector4 s in segments)
            {
                float d = DistanceToSegment(p, new Vector2(s.x, s.y), new Vector2(s.z, s.w));
                if (d < best) best = d;
            }
            return Mathf.Clamp01(halfWidthPx - best * scale + 0.5f);
        }, Vector4.zero, TextureWrapMode.Clamp, force);
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float len2 = ab.sqrMagnitude;
        if (len2 < 1e-8f) return Vector2.Distance(p, a);
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
        return Vector2.Distance(p, a + ab * t);
    }

    private static List<Vector2> CirclePolyline(float cx, float cy, float r)
    {
        List<Vector2> pts = new List<Vector2>();
        const int n = 64;
        for (int i = 0; i <= n; i++)
        {
            float a = i / (float)n * Mathf.PI * 2f;
            pts.Add(new Vector2(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r));
        }
        return pts;
    }

    private static List<Vector2> RoundedRectPolyline(Vector4 rect, float r)
    {
        float x = rect.x, y = rect.y, w = rect.z, h = rect.w;
        List<Vector2> pts = new List<Vector2>();
        void Corner(float cx, float cy, float startDeg)
        {
            for (int i = 0; i <= 8; i++)
            {
                float a = (startDeg + i * 90f / 8f) * Mathf.Deg2Rad;
                pts.Add(new Vector2(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r));
            }
        }
        Corner(x + w - r, y + r, 270f);
        Corner(x + w - r, y + h - r, 0f);
        Corner(x + r, y + h - r, 90f);
        Corner(x + r, y + r, 180f);
        pts.Add(pts[0]);
        return pts;
    }

    private static readonly Regex PathToken = new Regex(@"[MmLlHhVvCcSsAaZz]|-?(?:\d*\.\d+|\d+\.?\d*)(?:[eE][-+]?\d+)?", RegexOptions.Compiled);

    private static List<List<Vector2>> ParsePath(string d)
    {
        List<string> tokens = new List<string>();
        foreach (Match m in PathToken.Matches(d)) tokens.Add(m.Value);

        List<List<Vector2>> result = new List<List<Vector2>>();
        List<Vector2> current = null;
        Vector2 pos = Vector2.zero, start = Vector2.zero, lastCtrl = Vector2.zero;
        bool lastWasCubic = false;
        char cmd = 'M';
        int i = 0;

        float Num() => float.Parse(tokens[i++], CultureInfo.InvariantCulture);
        bool IsCmd(string t) => t.Length == 1 && char.IsLetter(t[0]);

        while (i < tokens.Count)
        {
            if (IsCmd(tokens[i])) cmd = tokens[i++][0];
            bool rel = char.IsLower(cmd);
            char up = char.ToUpperInvariant(cmd);
            bool cubicNow = false;

            switch (up)
            {
                case 'M':
                {
                    float x = Num(), y = Num();
                    pos = rel ? pos + new Vector2(x, y) : new Vector2(x, y);
                    start = pos;
                    current = new List<Vector2> { pos };
                    result.Add(current);
                    cmd = rel ? 'l' : 'L'; // extra pairs are implicit line-tos
                    break;
                }
                case 'L':
                {
                    float x = Num(), y = Num();
                    pos = rel ? pos + new Vector2(x, y) : new Vector2(x, y);
                    current.Add(pos);
                    break;
                }
                case 'H':
                {
                    float x = Num();
                    pos = new Vector2(rel ? pos.x + x : x, pos.y);
                    current.Add(pos);
                    break;
                }
                case 'V':
                {
                    float y = Num();
                    pos = new Vector2(pos.x, rel ? pos.y + y : y);
                    current.Add(pos);
                    break;
                }
                case 'C':
                case 'S':
                {
                    Vector2 c1;
                    if (up == 'S') c1 = lastWasCubic ? pos * 2f - lastCtrl : pos;
                    else { c1 = new Vector2(Num(), Num()); if (rel) c1 += pos; }
                    Vector2 c2 = new Vector2(Num(), Num()), e = new Vector2(Num(), Num());
                    if (rel) { c2 += pos; e += pos; }
                    Vector2 p0 = pos;
                    for (int s = 1; s <= 16; s++)
                    {
                        float t = s / 16f, u = 1f - t;
                        current.Add(u * u * u * p0 + 3f * u * u * t * c1 + 3f * u * t * t * c2 + t * t * t * e);
                    }
                    pos = e;
                    lastCtrl = c2;
                    cubicNow = true;
                    break;
                }
                case 'A':
                {
                    float rx = Num(), ry = Num(), rot = Num();
                    bool large = Num() != 0f, sweep = Num() != 0f;
                    Vector2 e = new Vector2(Num(), Num());
                    if (rel) e += pos;
                    AddArc(current, pos, e, rx, ry, rot, large, sweep);
                    pos = e;
                    break;
                }
                case 'Z':
                {
                    if (current != null) current.Add(start);
                    pos = start;
                    break;
                }
                default:
                    i++;
                    break;
            }
            lastWasCubic = cubicNow;
        }
        return result;
    }

    // SVG endpoint -> center arc conversion (SVG 1.1, appendix F.6.5).
    private static void AddArc(List<Vector2> pts, Vector2 p1, Vector2 p2, float rx, float ry, float rotDeg, bool large, bool sweep)
    {
        if (rx == 0f || ry == 0f) { pts.Add(p2); return; }
        float phi = rotDeg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(phi), sin = Mathf.Sin(phi);
        Vector2 d = (p1 - p2) * 0.5f;
        float x1p = cos * d.x + sin * d.y;
        float y1p = -sin * d.x + cos * d.y;
        rx = Mathf.Abs(rx); ry = Mathf.Abs(ry);
        float lambda = (x1p * x1p) / (rx * rx) + (y1p * y1p) / (ry * ry);
        if (lambda > 1f) { float s = Mathf.Sqrt(lambda); rx *= s; ry *= s; }

        float num = rx * rx * ry * ry - rx * rx * y1p * y1p - ry * ry * x1p * x1p;
        float den = rx * rx * y1p * y1p + ry * ry * x1p * x1p;
        float coef = den == 0f ? 0f : Mathf.Sqrt(Mathf.Max(0f, num / den));
        if (large == sweep) coef = -coef;
        float cxp = coef * rx * y1p / ry;
        float cyp = -coef * ry * x1p / rx;
        Vector2 mid = (p1 + p2) * 0.5f;
        Vector2 c = new Vector2(cos * cxp - sin * cyp + mid.x, sin * cxp + cos * cyp + mid.y);

        float theta1 = Mathf.Atan2((y1p - cyp) / ry, (x1p - cxp) / rx);
        float theta2 = Mathf.Atan2((-y1p - cyp) / ry, (-x1p - cxp) / rx);
        float delta = theta2 - theta1;
        if (sweep && delta < 0f) delta += Mathf.PI * 2f;
        if (!sweep && delta > 0f) delta -= Mathf.PI * 2f;

        int steps = Mathf.Max(6, Mathf.CeilToInt(Mathf.Abs(delta) / (Mathf.PI / 24f)));
        for (int s = 1; s <= steps; s++)
        {
            float t = theta1 + delta * s / steps;
            float ex = rx * Mathf.Cos(t), ey = ry * Mathf.Sin(t);
            pts.Add(new Vector2(cos * ex - sin * ey + c.x, sin * ex + cos * ey + c.y));
        }
    }

    // =====================================================================
    // FILE IO
    // =====================================================================

    private static Sprite WriteSprite(string dir, string name, int w, int h, System.Func<int, int, float> alpha, Vector4 border, TextureWrapMode wrap, bool force)
    {
        string path = $"{dir}/{name}.png";
        if (!force)
        {
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;
        }

        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color32[] px = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha(x, y)) * 255f);
                px[y * w + x] = new Color32(255, 255, 255, a);
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.spriteBorder = border;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = wrap;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    public static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
#endif
