#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class GlassUIAssetGenerator
{
    private const string UI_TEXTURE_DIR = "Assets/_Project/Textures/UI";

    [MenuItem("Tools/Delivery Game/UI/1. Generate Complete Glassmorphic UI Sprites", false, 10)]
    public static void GenerateAllGlassSprites()
    {
        if (!Directory.Exists(UI_TEXTURE_DIR))
        {
            Directory.CreateDirectory(UI_TEXTURE_DIR);
            AssetDatabase.Refresh();
        }

        // 1. Large Glass Panel (Main Tablet & Day Summary Modals)
        GenerateRoundedGlassSprite("UI_Glass_Panel_Large.png", 256, 256, 28f, 64, 
            new Color(0.05f, 0.07f, 0.11f, 0.94f), new Color(0.12f, 0.18f, 0.28f, 0.75f), 
            new Color(0.28f, 0.42f, 0.65f, 0.95f), 1.5f, 4f, new Color(0.20f, 0.35f, 0.60f, 0.35f));

        // 2. Medium Glass Panel (Top Status Bar, Garage Panels)
        GenerateRoundedGlassSprite("UI_Glass_Panel_Medium.png", 128, 128, 18f, 36, 
            new Color(0.06f, 0.08f, 0.13f, 0.94f), new Color(0.10f, 0.15f, 0.24f, 0.80f), 
            new Color(0.25f, 0.38f, 0.58f, 0.90f), 1.5f, 3f, new Color(0.20f, 0.35f, 0.55f, 0.30f));

        // 3. Glass Card (List items, Vehicle cards, Sub cards)
        GenerateRoundedGlassSprite("UI_Glass_Card.png", 128, 128, 14f, 32, 
            new Color(0.08f, 0.11f, 0.17f, 0.92f), new Color(0.13f, 0.18f, 0.28f, 0.85f), 
            new Color(0.30f, 0.45f, 0.68f, 0.85f), 1.2f, 2f, new Color(0.15f, 0.25f, 0.45f, 0.25f));

        // 4. Clue & Hint Dark Recessed Box
        GenerateRoundedGlassSprite("UI_Glass_ClueBox.png", 128, 128, 12f, 30, 
            new Color(0.04f, 0.06f, 0.09f, 0.98f), new Color(0.06f, 0.09f, 0.14f, 0.95f), 
            new Color(0.18f, 0.55f, 0.85f, 0.90f), 1.5f, 3f, new Color(0.10f, 0.45f, 0.75f, 0.30f));

        // 5. Prompt & Notification Box
        GenerateRoundedGlassSprite("UI_Glass_PromptBox.png", 128, 128, 14f, 32, 
            new Color(0.05f, 0.07f, 0.10f, 0.96f), new Color(0.09f, 0.13f, 0.20f, 0.90f), 
            new Color(0.35f, 0.48f, 0.70f, 0.90f), 1.5f, 3f, new Color(0.25f, 0.40f, 0.65f, 0.30f));

        // 6. Neon Cyan Button / Tab Active
        GenerateRoundedGlassSprite("UI_Button_Neon_Cyan.png", 128, 128, 16f, 32, 
            new Color(0.08f, 0.22f, 0.35f, 0.85f), new Color(0.12f, 0.42f, 0.65f, 0.75f), 
            new Color(0.20f, 0.85f, 1.00f, 1.00f), 2.0f, 5f, new Color(0.00f, 0.80f, 1.00f, 0.55f));

        // 7. Neon Green Button (Purchase, Success, Next Day)
        GenerateRoundedGlassSprite("UI_Button_Neon_Green.png", 128, 128, 16f, 32, 
            new Color(0.06f, 0.28f, 0.15f, 0.85f), new Color(0.10f, 0.52f, 0.28f, 0.75f), 
            new Color(0.20f, 1.00f, 0.45f, 1.00f), 2.0f, 5f, new Color(0.10f, 0.95f, 0.40f, 0.55f));

        // 8. Neon Orange / Amber Button (End Shift, Refuel)
        GenerateRoundedGlassSprite("UI_Button_Neon_Orange.png", 128, 128, 16f, 32, 
            new Color(0.35f, 0.18f, 0.05f, 0.85f), new Color(0.60f, 0.32f, 0.08f, 0.75f), 
            new Color(1.00f, 0.65f, 0.15f, 1.00f), 2.0f, 5f, new Color(1.00f, 0.55f, 0.10f, 0.55f));

        // 9. Neon Red Button (Close, Penalty)
        GenerateRoundedGlassSprite("UI_Button_Neon_Red.png", 128, 128, 16f, 32, 
            new Color(0.35f, 0.08f, 0.08f, 0.85f), new Color(0.58f, 0.14f, 0.14f, 0.75f), 
            new Color(1.00f, 0.25f, 0.25f, 1.00f), 2.0f, 5f, new Color(1.00f, 0.20f, 0.20f, 0.55f));

        // 10. Dark Base Button (Inactive tabs, standard buttons)
        GenerateRoundedGlassSprite("UI_Button_Dark_Base.png", 128, 128, 16f, 32, 
            new Color(0.09f, 0.12f, 0.18f, 0.90f), new Color(0.14f, 0.19f, 0.28f, 0.85f), 
            new Color(0.25f, 0.35f, 0.50f, 0.80f), 1.2f, 2f, new Color(0.15f, 0.22f, 0.35f, 0.20f));

        // 11. Capsule Pill Badge (For status badges: IN TRANSIT, DELIVERED, FRAGILE, EXPRESS)
        GenerateCapsulePillSprite("UI_Capsule_Badge.png", 128, 64, 28f, 32, 
            new Color(0.08f, 0.12f, 0.18f, 0.95f), new Color(0.15f, 0.22f, 0.35f, 0.90f), 
            new Color(0.40f, 0.60f, 0.90f, 0.95f), 1.5f, 3f, new Color(0.20f, 0.50f, 0.90f, 0.40f));

        // 12. Progress Bar Track (Fuel & Condition Track)
        GenerateRoundedGlassSprite("UI_Bar_Track.png", 128, 32, 12f, 16, 
            new Color(0.05f, 0.07f, 0.10f, 0.98f), new Color(0.08f, 0.11f, 0.16f, 0.95f), 
            new Color(0.20f, 0.28f, 0.42f, 0.85f), 1.0f, 2f, new Color(0.10f, 0.18f, 0.30f, 0.25f));

        // 13. Progress Bar Fill (Smooth glow fill)
        GenerateRoundedGlassSprite("UI_Bar_Fill.png", 128, 32, 10f, 14, 
            new Color(0.90f, 0.95f, 1.00f, 0.95f), new Color(0.70f, 0.85f, 1.00f, 0.95f), 
            new Color(1.00f, 1.00f, 1.00f, 1.00f), 1.0f, 3f, new Color(0.40f, 0.80f, 1.00f, 0.60f));

        // 14. Circle Swatch (For paint workshop swatches & crosshairs)
        GenerateCircleSwatchSprite("UI_Circle_Swatch.png", 128, 
            new Color(1f, 1f, 1f, 0.95f), new Color(1f, 1f, 1f, 0.40f), 2.5f);

        // 15. Generate Complete White Transparent UI Icon Sprites
        GenerateAllUIIcons();

        AssetDatabase.Refresh();
        Debug.Log("<color=#32FF64><b>[GlassUIAssetGenerator]</b> 14 Glassmorphic UI Sprites + 9 Clean PNG UI Icons successfully generated in 'Assets/_Project/Textures/'!</color>");
    }

    private static void GenerateAllUIIcons()
    {
        string iconDir = "Assets/_Project/Textures/Icons";
        if (!Directory.Exists(iconDir))
        {
            Directory.CreateDirectory(iconDir);
        }

        // 1. Clock Icon (128x128)
        DrawAndSaveIcon("Icon_Clock.png", iconDir, (x, y, w, h) =>
        {
            float cx = w * 0.5f, cy = h * 0.5f, r = w * 0.42f;
            float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            float ring = Mathf.Abs(d - r);
            if (ring <= 4.5f) return Mathf.Clamp01(4.5f - ring);

            // Center dot
            if (d <= 5f) return 1f;

            // Hour hand (pointing to 9 o'clock)
            if (Mathf.Abs(y - cy) <= 3.5f && x <= cx && x >= cx - r * 0.55f) return 1f;

            // Minute hand (pointing to 12 o'clock)
            if (Mathf.Abs(x - cx) <= 3.5f && y >= cy && y <= cy + r * 0.72f) return 1f;

            return 0f;
        });

        // 2. Package / Parcel Box Icon (128x128)
        DrawAndSaveIcon("Icon_Package.png", iconDir, (x, y, w, h) =>
        {
            float cx = w * 0.5f, cy = h * 0.5f;
            // Isometric box / cube shape
            float nx = (x - cx) / (w * 0.42f);
            float ny = (y - cy) / (h * 0.42f);

            // Outer box boundary
            float boxTop = 0.85f - Mathf.Abs(nx) * 0.45f;
            float boxBottom = -0.85f + Mathf.Abs(nx) * 0.45f;

            if (ny <= boxTop && ny >= boxBottom && Mathf.Abs(nx) <= 0.9f)
            {
                // Top face dividing lines
                float lineCenter = Mathf.Abs(nx);
                float lineV = Mathf.Abs(x - cx);
                if (lineV <= 3f && ny <= 0.4f && ny >= -0.85f) return 1f;

                // Center Y fold line
                float topFold = Mathf.Abs(ny - (0.4f - Mathf.Abs(nx) * 0.45f));
                if (topFold <= 0.08f) return 1f;

                // Outer border
                if (Mathf.Abs(ny - boxTop) <= 0.09f || Mathf.Abs(ny - boxBottom) <= 0.09f || Mathf.Abs(Mathf.Abs(nx) - 0.9f) <= 0.09f)
                    return 1f;

                return 0.15f; // Semi-transparent body
            }
            return 0f;
        });

        // 3. Wallet / Vault Icon (128x128)
        DrawAndSaveIcon("Icon_Wallet.png", iconDir, (x, y, w, h) =>
        {
            float cx = w * 0.5f, cy = h * 0.5f;
            float nx = (x - cx) / (w * 0.42f);
            float ny = (y - cy) / (h * 0.36f);

            if (Mathf.Abs(nx) <= 0.9f && Mathf.Abs(ny) <= 0.8f)
            {
                // Clasp on the right
                float claspDx = nx - 0.65f;
                float claspDy = ny;
                float claspDist = Mathf.Sqrt(claspDx * claspDx * 2f + claspDy * claspDy * 4f);
                if (claspDist <= 0.35f)
                {
                    if (claspDist <= 0.12f) return 1f; // Clasp dot
                    return 0.9f;
                }

                // Border
                if (Mathf.Abs(Mathf.Abs(nx) - 0.9f) <= 0.09f || Mathf.Abs(Mathf.Abs(ny) - 0.8f) <= 0.09f) return 1f;

                // Inner flap line
                if (Mathf.Abs(ny - 0.3f) <= 0.08f) return 1f;

                return 0.15f;
            }
            return 0f;
        });

        // 4. Fuel Pump Icon (128x128)
        DrawAndSaveIcon("Icon_Fuel.png", iconDir, (x, y, w, h) =>
        {
            float cx = w * 0.45f, cy = h * 0.5f;
            float nx = (x - cx) / (w * 0.38f);
            float ny = (y - cy) / (h * 0.44f);

            // Pump main body
            if (nx >= -0.8f && nx <= 0.3f && Mathf.Abs(ny) <= 0.85f)
            {
                // Outer body
                if (Mathf.Abs(nx - (-0.8f)) <= 0.09f || Mathf.Abs(nx - 0.3f) <= 0.09f || Mathf.Abs(Mathf.Abs(ny) - 0.85f) <= 0.09f) return 1f;

                // Meter window
                if (nx >= -0.6f && nx <= 0.1f && ny >= 0.2f && ny <= 0.6f)
                {
                    if (Mathf.Abs(nx - (-0.6f)) <= 0.08f || Mathf.Abs(nx - 0.1f) <= 0.08f || Mathf.Abs(ny - 0.2f) <= 0.08f || Mathf.Abs(ny - 0.6f) <= 0.08f) return 1f;
                    return 0f;
                }
                return 0.15f;
            }

            // Hose & Nozzle on the right
            if (nx >= 0.3f && nx <= 0.85f && ny >= -0.4f && ny <= 0.5f)
            {
                float hx = nx - 0.6f;
                float hy = ny - 0.0f;
                if (Mathf.Abs(hx) <= 0.09f && ny >= -0.4f && ny <= 0.3f) return 1f;
                if (ny >= 0.3f && Mathf.Abs(nx - 0.75f) <= 0.12f) return 1f;
            }

            return 0f;
        });

        // 5. Wrench / Service Icon (128x128)
        DrawAndSaveIcon("Icon_Wrench.png", iconDir, (x, y, w, h) =>
        {
            float cx = w * 0.5f, cy = h * 0.5f;
            // 45 degree diagonal wrench
            float rx = (x - cx) * 0.7071f - (y - cy) * 0.7071f;
            float ry = (x - cx) * 0.7071f + (y - cy) * 0.7071f;

            // Handle
            if (Mathf.Abs(rx) <= 5.5f && Mathf.Abs(ry) <= 42f) return 1f;

            // Head 1 (Top right)
            float h1x = rx;
            float h1y = ry - 36f;
            float d1 = Mathf.Sqrt(h1x * h1x + h1y * h1y);
            if (d1 <= 18f && !(h1y >= 0f && Mathf.Abs(h1x) <= 6.5f)) return 1f;

            // Head 2 (Bottom left)
            float h2x = rx;
            float h2y = ry + 36f;
            float d2 = Mathf.Sqrt(h2x * h2x + h2y * h2y);
            if (d2 <= 14f && !(h2y <= 0f && Mathf.Abs(h2x) <= 5.5f)) return 1f;

            return 0f;
        });

        // 6. Shield / Insurance Icon (128x128)
        DrawAndSaveIcon("Icon_Shield.png", iconDir, (x, y, w, h) =>
        {
            float cx = w * 0.5f, cy = h * 0.52f;
            float nx = (x - cx) / (w * 0.40f);
            float ny = (y - cy) / (h * 0.42f);

            float shieldBottom = -0.9f + (nx * nx) * 0.95f;
            if (Mathf.Abs(nx) <= 0.85f && ny <= 0.85f && ny >= shieldBottom)
            {
                // Checkmark inside shield
                float ckx = nx - 0.05f;
                float cky = ny - 0.05f;
                if ((ckx >= -0.35f && ckx <= -0.05f && Mathf.Abs(cky - (-0.15f - ckx * 1.2f)) <= 0.1f) ||
                    (ckx >= -0.05f && ckx <= 0.45f && Mathf.Abs(cky - (-0.15f + ckx * 1.0f)) <= 0.1f))
                {
                    return 1f;
                }

                if (Mathf.Abs(Mathf.Abs(nx) - 0.85f) <= 0.09f || Mathf.Abs(ny - 0.85f) <= 0.09f || Mathf.Abs(ny - shieldBottom) <= 0.09f) return 1f;
                return 0.15f;
            }
            return 0f;
        });

        // 7. Calendar Check Icon (128x128)
        DrawAndSaveIcon("Icon_Calendar.png", iconDir, (x, y, w, h) =>
        {
            float cx = w * 0.5f, cy = h * 0.48f;
            float nx = (x - cx) / (w * 0.40f);
            float ny = (y - cy) / (h * 0.40f);

            if (Mathf.Abs(nx) <= 0.85f && Mathf.Abs(ny) <= 0.85f)
            {
                if (Mathf.Abs(Mathf.Abs(nx) - 0.85f) <= 0.09f || Mathf.Abs(Mathf.Abs(ny) - 0.85f) <= 0.09f || Mathf.Abs(ny - 0.35f) <= 0.09f) return 1f;

                // Checkmark in body
                float ckx = nx;
                float cky = ny + 0.2f;
                if ((ckx >= -0.3f && ckx <= 0f && Mathf.Abs(cky - (-0.1f - ckx * 1.2f)) <= 0.1f) ||
                    (ckx >= 0f && ckx <= 0.4f && Mathf.Abs(cky - (-0.1f + ckx * 1.1f)) <= 0.1f))
                {
                    return 1f;
                }
                return 0.12f;
            }

            // Top binders
            if (ny >= 0.85f && ny <= 1.1f && (Mathf.Abs(nx - 0.45f) <= 0.09f || Mathf.Abs(nx - (-0.45f)) <= 0.09f)) return 1f;

            return 0f;
        });
    }

    private static void DrawAndSaveIcon(string filename, string dir, System.Func<float, float, int, int, float> pixelSampler)
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float alpha = Mathf.Clamp01(pixelSampler(x, y, size, size));
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        string fullPath = Path.Combine(dir, filename);
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(fullPath, bytes);
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(fullPath, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(fullPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }

    private static void GenerateRoundedGlassSprite(string filename, int width, int height, float cornerRadius, int sliceBorder,
        Color baseColorBottom, Color baseColorTop, Color borderColor, float borderWidth, float glowRadius, Color glowColor)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float halfW = width * 0.5f;
        float halfH = height * 0.5f;
        float r = cornerRadius;

        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            float vNorm = (float)y / (height - 1);
            Color bodyGrad = Color.Lerp(baseColorBottom, baseColorTop, vNorm);

            for (int x = 0; x < width; x++)
            {
                // Signed distance to rounded rectangle box
                float px = Mathf.Abs(x - halfW) - (halfW - r - glowRadius);
                float py = Mathf.Abs(y - halfH) - (halfH - r - glowRadius);

                float dx = Mathf.Max(px, 0f);
                float dy = Mathf.Max(py, 0f);
                float distOutside = Mathf.Sqrt(dx * dx + dy * dy);
                float distInside = Mathf.Min(Mathf.Max(px, py), 0f);
                float dist = distOutside + distInside - r;

                Color finalColor = Color.clear;

                if (dist <= 0f)
                {
                    // Inside the shape
                    float borderT = Mathf.Clamp01((-dist) / borderWidth);
                    // Blend from border color to inner body color
                    Color c = Color.Lerp(borderColor, bodyGrad, borderT);

                    // Add subtle top edge glass highlight
                    if (y > height - r - glowRadius - borderWidth && dist > -borderWidth * 2f)
                    {
                        c = Color.Lerp(c, Color.white, 0.25f);
                    }

                    // Antialiased edge
                    float alphaEdge = Mathf.Clamp01(-dist);
                    c.a *= alphaEdge;
                    finalColor = c;
                }
                else if (dist <= glowRadius)
                {
                    // Outer glow falloff
                    float glowFactor = 1f - (dist / glowRadius);
                    glowFactor = Mathf.Pow(glowFactor, 1.8f);
                    Color g = glowColor;
                    g.a *= glowFactor;
                    finalColor = g;
                }

                pixels[y * width + x] = finalColor;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        SaveAndConfigureSprite(tex, filename, sliceBorder);
    }

    private static void GenerateCapsulePillSprite(string filename, int width, int height, float cornerRadius, int sliceBorder,
        Color baseColorBottom, Color baseColorTop, Color borderColor, float borderWidth, float glowRadius, Color glowColor)
    {
        GenerateRoundedGlassSprite(filename, width, height, cornerRadius, sliceBorder, 
            baseColorBottom, baseColorTop, borderColor, borderWidth, glowRadius, glowColor);
    }

    private static void GenerateCircleSwatchSprite(string filename, int size, Color bodyColor, Color ringColor, float ringWidth)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float center = size * 0.5f;
        float radius = center - 4f;

        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                Color c = Color.clear;
                if (dist <= radius)
                {
                    float edgeDist = radius - dist;
                    float aa = Mathf.Clamp01(edgeDist);

                    if (edgeDist <= ringWidth)
                    {
                        c = ringColor;
                    }
                    else
                    {
                        c = bodyColor;
                    }
                    c.a *= aa;
                }
                pixels[y * size + x] = c;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        SaveAndConfigureSprite(tex, filename, 0);
    }

    private static void SaveAndConfigureSprite(Texture2D tex, string filename, int border)
    {
        string fullPath = Path.Combine(UI_TEXTURE_DIR, filename);
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(fullPath, bytes);
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(fullPath, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(fullPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            if (border > 0)
            {
                importer.spriteBorder = new Vector4(border, border, border, border);
            }

            importer.SaveAndReimport();
        }
    }

    public static Sprite LoadUISprite(string name)
    {
        string path = $"{UI_TEXTURE_DIR}/{name}.png";
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    [MenuItem("Tools/Delivery Game/UI/2. Apply Glassmorphic Style to All Active UI", false, 11)]
    public static void ApplyGlassmorphicStyleToAllUI()
    {
        GenerateAllGlassSprites();

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[GlassUIAssetGenerator] No Canvas found in the scene to apply glassmorphic styles.");
            return;
        }

        Sprite panelLarge = LoadUISprite("UI_Glass_Panel_Large");
        Sprite panelMedium = LoadUISprite("UI_Glass_Panel_Medium");
        Sprite glassCard = LoadUISprite("UI_Glass_Card");
        Sprite clueBox = LoadUISprite("UI_Glass_ClueBox");
        Sprite promptBox = LoadUISprite("UI_Glass_PromptBox");
        Sprite btnCyan = LoadUISprite("UI_Button_Neon_Cyan");
        Sprite btnGreen = LoadUISprite("UI_Button_Neon_Green");
        Sprite btnOrange = LoadUISprite("UI_Button_Neon_Orange");
        Sprite btnRed = LoadUISprite("UI_Button_Neon_Red");
        Sprite btnDark = LoadUISprite("UI_Button_Dark_Base");
        Sprite barTrack = LoadUISprite("UI_Bar_Track");
        Sprite barFill = LoadUISprite("UI_Bar_Fill");
        Sprite circleSwatch = LoadUISprite("UI_Circle_Swatch");

        // 1. Top Status Bar
        Transform topBar = canvas.transform.Find("DeliveryHUD/TopStatusBar");
        if (topBar != null)
        {
            SetImageSprite(topBar.gameObject, panelMedium, Color.white);
            SetImageSprite(topBar.Find("ClockCard")?.gameObject, glassCard, Color.white);
            SetImageSprite(topBar.Find("CargoCard")?.gameObject, glassCard, Color.white);
            SetImageSprite(topBar.Find("BalanceCard")?.gameObject, glassCard, Color.white);
        }

        // 2. Notification Banner
        Transform notif = canvas.transform.Find("DeliveryHUD/NotificationBanner");
        if (notif != null) SetImageSprite(notif.gameObject, promptBox, new Color(0.12f, 0.55f, 0.28f, 0.95f));

        // 3. Crosshair & Prompt Box
        Transform crosshair = canvas.transform.Find("CenterCrosshair");
        if (crosshair != null) SetImageSprite(crosshair.gameObject, circleSwatch, new Color(1f, 1f, 1f, 0.9f), Image.Type.Simple);

        Transform prompt = canvas.transform.Find("InteractionPromptBox");
        if (prompt != null) SetImageSprite(prompt.gameObject, promptBox, Color.white);

        // 4. Held Cargo Side Card (Untouched - user custom styled)

        // 5. Vehicle Dashboard (Fuel & Condition)
        Transform dash = canvas.transform.Find("VehicleDashboardPanel");
        if (dash != null)
        {
            SetImageSprite(dash.Find("FuelGaugeCard")?.gameObject, glassCard, Color.white);
            SetImageSprite(dash.Find("FuelGaugeCard/FuelBarBg")?.gameObject, barTrack, Color.white);
            SetImageSprite(dash.Find("FuelGaugeCard/FuelBarBg/FuelBarFill")?.gameObject, barFill, new Color(0.2f, 0.95f, 0.45f), Image.Type.Filled);

            SetImageSprite(dash.Find("ConditionGaugeCard")?.gameObject, glassCard, Color.white);
            SetImageSprite(dash.Find("ConditionGaugeCard/CondBarBg")?.gameObject, barTrack, Color.white);
            SetImageSprite(dash.Find("ConditionGaugeCard/CondBarBg/CondBarFill")?.gameObject, barFill, new Color(0.0f, 0.9f, 0.7f), Image.Type.Filled);
        }

        // 6. Tablet Panel
        Transform tablet = canvas.transform.Find("CargoTabletPanel");
        if (tablet != null)
        {
            SetImageSprite(tablet.gameObject, panelLarge, Color.white);
            SetImageSprite(tablet.Find("TabletTopBar")?.gameObject, panelMedium, Color.white);
            SetImageSprite(tablet.Find("TabletTopBar/TabBar/Tab_Cargo")?.gameObject, btnCyan, Color.white);
            SetImageSprite(tablet.Find("TabletTopBar/TabBar/Tab_Vehicles")?.gameObject, btnDark, Color.white);
            SetImageSprite(tablet.Find("TabletTopBar/TabBar/Tab_Branch")?.gameObject, btnDark, Color.white);
            SetImageSprite(tablet.Find("TabletTopBar/EndShiftButton")?.gameObject, btnOrange, Color.white);
            SetImageSprite(tablet.Find("TabletTopBar/CloseButton")?.gameObject, btnRed, Color.white);

            // Cargo View
            Transform cv = tablet.Find("TabletContentArea/CargoViewRoot");
            if (cv != null)
            {
                SetImageSprite(cv.Find("LeftColumn_List")?.gameObject, glassCard, Color.white);
                SetImageSprite(cv.Find("RightColumn_Detail")?.gameObject, glassCard, Color.white);
                SetImageSprite(cv.Find("RightColumn_Detail/DescriptionBox")?.gameObject, clueBox, Color.white);
                SetImageSprite(cv.Find("RightColumn_Detail/PhysicalDeliveryTipBox")?.gameObject, clueBox, Color.white);
                SetImageSprite(cv.Find("LeftColumn_List/CargoScrollView/Viewport/Content/CargoCardTemplate")?.gameObject, glassCard, Color.white);
            }

            // Vehicle View
            Transform vv = tablet.Find("TabletContentArea/VehicleViewRoot");
            if (vv != null)
            {
                SetImageSprite(vv.Find("LeftColumn_Vehicles")?.gameObject, glassCard, Color.white);
                SetImageSprite(vv.Find("RightColumn_VehicleDetail")?.gameObject, glassCard, Color.white);
                SetImageSprite(vv.Find("RightColumn_VehicleDetail/DescBox")?.gameObject, clueBox, Color.white);
                SetImageSprite(vv.Find("RightColumn_VehicleDetail/BuyButton")?.gameObject, btnGreen, Color.white);
                SetImageSprite(vv.Find("RightColumn_VehicleDetail/RefuelButton")?.gameObject, btnOrange, Color.white);
                SetImageSprite(vv.Find("RightColumn_VehicleDetail/RecallButton")?.gameObject, btnCyan, Color.white);
                SetImageSprite(vv.Find("LeftColumn_Vehicles/VehicleScrollView/Viewport/Content/VehicleCardTemplate")?.gameObject, glassCard, Color.white);
            }

            // Branch View
            Transform bv = tablet.Find("TabletContentArea/BranchViewRoot");
            if (bv != null)
            {
                SetImageSprite(bv.Find("LeftColumn_CurrentBranch")?.gameObject, glassCard, Color.white);
                SetImageSprite(bv.Find("LeftColumn_CurrentBranch/DescBox")?.gameObject, clueBox, Color.white);
                SetImageSprite(bv.Find("RightColumn_NextTier")?.gameObject, glassCard, Color.white);
                SetImageSprite(bv.Find("RightColumn_NextTier/NextDescBox")?.gameObject, clueBox, Color.white);
                SetImageSprite(bv.Find("RightColumn_NextTier/UpgradeButton")?.gameObject, btnGreen, Color.white);
            }
        }

        // 7. Day Summary Panel
        Transform summary = canvas.transform.Find("DaySummaryPanel");
        if (summary != null)
        {
            SetImageSprite(summary.gameObject, panelLarge, Color.white);
            SetImageSprite(summary.Find("HistoryScrollView")?.gameObject, clueBox, Color.white);
            SetImageSprite(summary.Find("HistoryScrollView/Viewport/Content/HistoryRowTemplate")?.gameObject, glassCard, Color.white);
            SetImageSprite(summary.Find("RestartDayButton")?.gameObject, btnCyan, Color.white);
        }

        // 8. Commercial Hub Panels
        Transform garage = canvas.transform.Find("GarageWorkshopPanel");
        if (garage != null)
        {
            SetImageSprite(garage.gameObject, panelLarge, Color.white);
            SetImageSprite(garage.Find("RepairButton")?.gameObject, btnGreen, Color.white);
            SetImageSprite(garage.Find("DriveButton")?.gameObject, btnCyan, Color.white);
            SetImageSprite(garage.Find("CloseButton")?.gameObject, btnRed, Color.white);
        }

        Transform insurance = canvas.transform.Find("InsuranceAgencyPanel");
        if (insurance != null)
        {
            SetImageSprite(insurance.gameObject, panelLarge, Color.white);
            SetImageSprite(insurance.Find("Tier2Button")?.gameObject, btnCyan, Color.white);
            SetImageSprite(insurance.Find("Tier3Button")?.gameObject, btnOrange, Color.white);
            SetImageSprite(insurance.Find("CloseButton")?.gameObject, btnRed, Color.white);
        }

        Transform dispatch = canvas.transform.Find("PassiveDispatchPanel");
        if (dispatch != null)
        {
            SetImageSprite(dispatch.gameObject, panelLarge, Color.white);
            SetImageSprite(dispatch.Find("Tier2Button")?.gameObject, btnCyan, Color.white);
            SetImageSprite(dispatch.Find("Tier3Button")?.gameObject, btnGreen, Color.white);
            SetImageSprite(dispatch.Find("CloseButton")?.gameObject, btnRed, Color.white);
        }

        Transform propModal = canvas.transform.Find("PropertyPurchaseModal");
        if (propModal != null)
        {
            SetImageSprite(propModal.gameObject, panelLarge, Color.white);
            SetImageSprite(propModal.Find("BuyButton")?.gameObject, btnGreen, Color.white);
            SetImageSprite(propModal.Find("CancelButton")?.gameObject, btnRed, Color.white);
        }

        EditorUtility.SetDirty(canvas.gameObject);
        Debug.Log("<color=#32FF64><b>[GlassUIAssetGenerator]</b> Glassmorphic 9-slice styling successfully applied to all UI panels, buttons, cards, and bars in the active Canvas!</color>");
    }

    private static void SetImageSprite(GameObject target, Sprite sprite, Color color, Image.Type imageType = Image.Type.Sliced)
    {
        if (target == null || sprite == null) return;

        Image img = target.GetComponent<Image>();
        if (img == null) img = target.AddComponent<Image>();

        img.sprite = sprite;
        img.type = imageType;
        img.color = color;
    }
}
#endif
