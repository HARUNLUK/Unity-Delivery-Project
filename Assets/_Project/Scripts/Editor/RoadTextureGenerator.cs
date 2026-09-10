#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class RoadTextureGenerator
{
    [MenuItem("Tools/Delivery Game/Regenerate Pure Sidewalk Texture", false, 65)]
    public static void RegeneratePureSidewalkTexture()
    {
        string dir = "Assets/_Project/Materials/RoadStyles";
        if (!Directory.Exists(dir))
        {
            dir = "Assets/Materials/RoadStyles";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }

        Texture2D texPureSidewalk = CreatePureSidewalkTexture();
        SaveTexture(texPureSidewalk, $"{dir}/Tex_Road_Pure_Sidewalk.png");
        AssetDatabase.Refresh();
        CreateURPLitMaterial($"{dir}/Tex_Road_Pure_Sidewalk.png", $"{dir}/Mat_Road_Pure_Sidewalk.mat");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[RoadTextureGenerator] Seamless Pure Sidewalk texture generated successfully!");
    }

    public static void GenerateRoadMaterials()
    {
        string dir = "Assets/_Project/Materials/RoadStyles";
        if (!Directory.Exists(dir))
        {
            dir = "Assets/Materials/RoadStyles";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }

        // 1. 2-LANE STRIPED ASPHALT
        Texture2D texStriped = CreateStripedRoadTexture();
        SaveTexture(texStriped, $"{dir}/Tex_Road_2Lane_Striped.png");

        // 2. CITY ROAD WITH STANDARD SIDEWALKS
        Texture2D texSidewalk = CreateSidewalkRoadTexture();
        SaveTexture(texSidewalk, $"{dir}/Tex_Road_City_Sidewalks.png");

        // 3. DOWNTOWN PLAIN ASPHALT WITH EXTRA WIDE SIDEWALKS
        Texture2D texWideSidewalk = CreateWideSidewalkRoadTexture();
        SaveTexture(texWideSidewalk, $"{dir}/Tex_Road_City_Wide_Sidewalk.png");

        // 4. MOUNTAIN DIRT VILLAGE ROAD
        Texture2D texDirt = CreateDirtRoadTexture();
        SaveTexture(texDirt, $"{dir}/Tex_Road_Mountain_Dirt.png");

        // 5. PURE SIDEWALK / PEDESTRIAN WALKWAY (SADECE KALDIRIM)
        Texture2D texPureSidewalk = CreatePureSidewalkTexture();
        SaveTexture(texPureSidewalk, $"{dir}/Tex_Road_Pure_Sidewalk.png");

        AssetDatabase.Refresh();

        // Create Materials
        CreateURPLitMaterial($"{dir}/Tex_Road_2Lane_Striped.png", $"{dir}/Mat_Road_2Lane_Striped.mat");
        CreateURPLitMaterial($"{dir}/Tex_Road_City_Sidewalks.png", $"{dir}/Mat_Road_City_Sidewalks.mat");
        CreateURPLitMaterial($"{dir}/Tex_Road_City_Wide_Sidewalk.png", $"{dir}/Mat_Road_City_Wide_Sidewalk.mat");
        CreateURPLitMaterial($"{dir}/Tex_Road_Mountain_Dirt.png", $"{dir}/Mat_Road_Mountain_Dirt.mat");
        CreateURPLitMaterial($"{dir}/Tex_Road_Pure_Sidewalk.png", $"{dir}/Mat_Road_Pure_Sidewalk.mat");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[RoadTextureGenerator] Generated ready-to-use road materials in '{dir}'!");
    }

    private static Texture2D CreateStripedRoadTexture()
    {
        int width = 512;
        int height = 512;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);

        Color asphalt = new Color(0.18f, 0.19f, 0.20f, 1f);
        Color asphaltDark = new Color(0.15f, 0.16f, 0.17f, 1f);
        Color lineWhite = new Color(0.92f, 0.92f, 0.90f, 1f);
        Color lineYellow = new Color(0.95f, 0.75f, 0.1f, 1f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width;
                float v = (float)y / height;

                float noise = Mathf.PerlinNoise(x * 0.15f, y * 0.15f) * 0.05f;
                Color col = Color.Lerp(asphalt, asphaltDark, noise);

                // White Shoulder Lines
                if ((u >= 0.04f && u <= 0.06f) || (u >= 0.94f && u <= 0.96f))
                {
                    col = lineWhite;
                }

                // Dashed Yellow Center Line
                if (u >= 0.485f && u <= 0.515f)
                {
                    float dashPattern = Mathf.Repeat(v * 4f, 1f);
                    if (dashPattern < 0.6f)
                    {
                        col = lineYellow;
                    }
                }

                tex.SetPixel(x, y, col);
            }
        }

        tex.Apply();
        return tex;
    }

    private static Texture2D CreateSidewalkRoadTexture()
    {
        int width = 512;
        int height = 512;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);

        Color asphalt = new Color(0.18f, 0.19f, 0.20f, 1f);
        Color sidewalkConcrete = new Color(0.72f, 0.73f, 0.74f, 1f);
        Color curbBorder = new Color(0.40f, 0.41f, 0.42f, 1f);
        Color lineWhite = new Color(0.92f, 0.92f, 0.90f, 1f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width;
                float v = (float)y / height;

                Color col = asphalt;

                // Left & Right Sidewalk (14%)
                if (u < 0.14f || u > 0.86f)
                {
                    float tileX = Mathf.Repeat(u * 20f, 1f);
                    float tileY = Mathf.Repeat(v * 8f, 1f);
                    bool isJoint = (tileX < 0.06f || tileY < 0.06f);

                    col = isJoint ? new Color(0.55f, 0.55f, 0.55f, 1f) : sidewalkConcrete;
                }
                // Curb Stone (0.14-0.16 & 0.84-0.86)
                else if ((u >= 0.14f && u <= 0.16f) || (u >= 0.84f && u <= 0.86f))
                {
                    float curbSection = Mathf.Repeat(v * 4f, 1f);
                    col = (curbSection < 0.1f) ? new Color(0.25f, 0.25f, 0.25f, 1f) : curbBorder;
                }
                else
                {
                    if (u >= 0.49f && u <= 0.51f)
                    {
                        float dashPattern = Mathf.Repeat(v * 4f, 1f);
                        if (dashPattern < 0.55f) col = lineWhite;
                    }
                }

                tex.SetPixel(x, y, col);
            }
        }

        tex.Apply();
        return tex;
    }

    /// <summary>
    /// Plain dark asphalt in the center with wide pedestrian sidewalks on both sides (Downtown / City Street).
    /// </summary>
    private static Texture2D CreateWideSidewalkRoadTexture()
    {
        int width = 512;
        int height = 512;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);

        Color asphalt = new Color(0.20f, 0.21f, 0.22f, 1f);
        Color asphaltDark = new Color(0.16f, 0.17f, 0.18f, 1f);
        Color sidewalkConcrete = new Color(0.75f, 0.76f, 0.77f, 1f);
        Color curbBorder = new Color(0.42f, 0.43f, 0.44f, 1f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width;
                float v = (float)y / height;

                // Wide Sidewalks (0.00 - 0.22 & 0.78 - 1.00 = 22% on each side!)
                if (u < 0.22f || u > 0.78f)
                {
                    float tileX = Mathf.Repeat(u * 14f, 1f);
                    float tileY = Mathf.Repeat(v * 6f, 1f);
                    bool isJoint = (tileX < 0.05f || tileY < 0.05f);

                    float noise = Mathf.PerlinNoise(x * 0.1f, y * 0.1f) * 0.04f;
                    Color concreteWithNoise = Color.Lerp(sidewalkConcrete, sidewalkConcrete * 0.9f, noise);

                    tex.SetPixel(x, y, isJoint ? new Color(0.52f, 0.52f, 0.52f, 1f) : concreteWithNoise);
                }
                // Solid Curb Stone (0.22 - 0.245 & 0.755 - 0.78)
                else if ((u >= 0.22f && u <= 0.245f) || (u >= 0.755f && u <= 0.78f))
                {
                    float curbSection = Mathf.Repeat(v * 3f, 1f);
                    Color col = (curbSection < 0.08f) ? new Color(0.25f, 0.25f, 0.25f, 1f) : curbBorder;
                    tex.SetPixel(x, y, col);
                }
                else
                {
                    // Plain Clean Downtown Asphalt (No center stripes)
                    float noise = Mathf.PerlinNoise(x * 0.2f, y * 0.2f) * 0.06f;
                    Color col = Color.Lerp(asphalt, asphaltDark, noise);
                    tex.SetPixel(x, y, col);
                }
            }
        }

        tex.Apply();
        return tex;
    }

    private static Texture2D CreateDirtRoadTexture()
    {
        int width = 512;
        int height = 512;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);

        Color dirtBase = new Color(0.48f, 0.38f, 0.28f, 1f);
        Color dirtDark = new Color(0.36f, 0.28f, 0.20f, 1f);
        Color grassEdge = new Color(0.35f, 0.45f, 0.22f, 1f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width;
                float v = (float)y / height;

                float n = Mathf.PerlinNoise(x * 0.08f, y * 0.08f);
                Color col = Color.Lerp(dirtBase, dirtDark, n);

                // Tire Tracks
                if ((u >= 0.26f && u <= 0.38f) || (u >= 0.62f && u <= 0.74f))
                {
                    col = Color.Lerp(col, dirtDark * 0.85f, 0.6f);
                }

                // Grass Edges
                if (u < 0.10f || u > 0.90f)
                {
                    float edgeT = (u < 0.10f) ? (1f - u / 0.10f) : ((u - 0.90f) / 0.10f);
                    col = Color.Lerp(col, grassEdge, edgeT * 0.8f);
                }

                tex.SetPixel(x, y, col);
            }
        }

        tex.Apply();
        return tex;
    }

    /// <summary>
    /// Pure Pedestrian Sidewalk / Walkway / Plaza (Sadece Kaldırım - 100% Sonsuz Dönen Kilitli Parke Taşı).
    /// Generates a 100% seamless running-bond architectural paving tile texture that repeats infinitely across any road width without stretching.
    /// </summary>
    private static Texture2D CreatePureSidewalkTexture()
    {
        int width = 512;
        int height = 512;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);

        Color concreteLight = new Color(0.79f, 0.80f, 0.81f, 1f);
        Color concreteDark = new Color(0.69f, 0.70f, 0.71f, 1f);
        Color jointColor = new Color(0.30f, 0.31f, 0.32f, 1f);

        float numRowsY = 8f; // 8 rows high
        float numColsX = 8f; // 8 cols wide

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width;
                float v = (float)y / height;

                int rowIdx = Mathf.FloorToInt(v * numRowsY);
                float xOffset = (rowIdx % 2 == 1) ? 0.5f : 0.0f; // Staggered running bond brick pattern

                float tileX = Mathf.Repeat((u * numColsX) + xOffset, 1f);
                float tileY = Mathf.Repeat(v * numRowsY, 1f);

                int colIdx = Mathf.FloorToInt((u * numColsX) + xOffset);

                // Deep joint grooves with bevel edge
                bool isJoint = (tileX < 0.06f || tileY < 0.06f);

                // Unique per-stone subtle shade variation
                float stoneHash = (Mathf.Sin((rowIdx * 12.9898f) + (colIdx * 78.233f)) * 43758.5453f);
                float stoneVariation = stoneHash - Mathf.Floor(stoneHash);
                Color slabBase = Color.Lerp(concreteLight, concreteDark, stoneVariation * 0.45f);

                // Subtle edge bevel shading
                float edgeDistX = Mathf.Min(tileX, 1f - tileX);
                float edgeDistY = Mathf.Min(tileY, 1f - tileY);
                float minEdge = Mathf.Min(edgeDistX, edgeDistY);
                if (minEdge < 0.12f && !isJoint)
                {
                    float bevel = Mathf.Clamp01((minEdge - 0.06f) / 0.06f);
                    slabBase = Color.Lerp(slabBase * 0.82f, slabBase, bevel);
                }

                // Fine surface concrete grain
                float grain = Mathf.PerlinNoise(x * 0.25f, y * 0.25f) * 0.05f;
                slabBase = Color.Lerp(slabBase, slabBase * 0.90f, grain);

                Color col = isJoint ? jointColor : slabBase;
                tex.SetPixel(x, y, col);
            }
        }

        tex.Apply();
        return tex;
    }

    private static void SaveTexture(Texture2D tex, string path)
    {
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
    }

    private static void CreateURPLitMaterial(string texPath, string matPath)
    {
        if (File.Exists(matPath)) return;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("URP/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (tex != null)
        {
            mat.SetTexture("_BaseMap", tex);
            mat.SetTexture("_MainTex", tex);
        }

        mat.SetFloat("_Smoothness", 0.15f);
        mat.SetFloat("_Metallic", 0.0f);
        mat.SetFloat("_Cull", 0f); // Two sided
        mat.renderQueue = 2000;

        AssetDatabase.CreateAsset(mat, matPath);
    }
}
#endif
