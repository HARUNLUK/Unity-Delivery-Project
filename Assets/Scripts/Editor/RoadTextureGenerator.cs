#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class RoadTextureGenerator
{
    [MenuItem("Tools/Delivery Game/Generate Ready-To-Use Road Materials", false, 10)]
    public static void GenerateRoadMaterials()
    {
        string dir = "Assets/Materials/RoadStyles";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        // 1. ÇİFT ŞERİTLİ ÇİZGİLİ ASFALT (2-Lane Striped Road)
        Texture2D texStriped = CreateStripedRoadTexture();
        SaveTexture(texStriped, $"{dir}/Tex_Road_2Lane_Striped.png");

        // 2. SAĞI VE SOLU KALDIRIMLI ŞEHİR CADDESİ (City Road with Sidewalks & Curbs)
        Texture2D texSidewalk = CreateSidewalkRoadTexture();
        SaveTexture(texSidewalk, $"{dir}/Tex_Road_City_Sidewalks.png");

        // 3. DAĞ / TOPRAK KÖY YOLU (Mountain Dirt Road with Tire Tracks)
        Texture2D texDirt = CreateDirtRoadTexture();
        SaveTexture(texDirt, $"{dir}/Tex_Road_Mountain_Dirt.png");

        AssetDatabase.Refresh();

        // Material'ları oluştur
        CreateURPLitMaterial($"{dir}/Tex_Road_2Lane_Striped.png", $"{dir}/Mat_Road_2Lane_Striped.mat");
        CreateURPLitMaterial($"{dir}/Tex_Road_City_Sidewalks.png", $"{dir}/Mat_Road_City_Sidewalks.mat");
        CreateURPLitMaterial($"{dir}/Tex_Road_Mountain_Dirt.png", $"{dir}/Mat_Road_Mountain_Dirt.mat");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[RoadTextureGenerator] 3 farklı hazır yol materyali 'Assets/Materials/RoadStyles' klasörüne başarıyla oluşturuldu!");
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

                // Asfalt gren/gürültü deseni
                float noise = Mathf.PerlinNoise(x * 0.15f, y * 0.15f) * 0.05f;
                Color col = Color.Lerp(asphalt, asphaltDark, noise);

                // Sol ve Sağ Beyaz Emniyet Çizgileri
                if ((u >= 0.04f && u <= 0.06f) || (u >= 0.94f && u <= 0.96f))
                {
                    col = lineWhite;
                }

                // Orta Sarı Kesikli Şerit Çizgisi (Dashed Yellow Center Line)
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

                // Sol Kaldırım (0.00 - 0.15) ve Sağ Kaldırım (0.85 - 1.00)
                if (u < 0.14f || u > 0.86f)
                {
                    // Kaldırım karo derz çizgileri
                    float tileX = Mathf.Repeat(u * 20f, 1f);
                    float tileY = Mathf.Repeat(v * 8f, 1f);
                    bool isJoint = (tileX < 0.06f || tileY < 0.06f);

                    col = isJoint ? new Color(0.55f, 0.55f, 0.55f, 1f) : sidewalkConcrete;
                }
                // Bordür Taşı (Curb Stone: 0.14-0.16 ve 0.84-0.86)
                else if ((u >= 0.14f && u <= 0.16f) || (u >= 0.84f && u <= 0.86f))
                {
                    float curbSection = Mathf.Repeat(v * 4f, 1f);
                    col = (curbSection < 0.1f) ? new Color(0.25f, 0.25f, 0.25f, 1f) : curbBorder;
                }
                else
                {
                    // Orta Asfalt Yol ve Beyaz Kesikli Çizgi
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

                // Tekerlek İzleri (Sol: 0.28-0.38, Sağ: 0.62-0.72)
                if ((u >= 0.26f && u <= 0.38f) || (u >= 0.62f && u <= 0.74f))
                {
                    col = Color.Lerp(col, dirtDark * 0.85f, 0.6f);
                }

                // Kenar Otları (Grass Edges)
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

    private static void SaveTexture(Texture2D tex, string path)
    {
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
    }

    private static void CreateURPLitMaterial(string texPath, string matPath)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
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
        mat.renderQueue = 2005;

        AssetDatabase.CreateAsset(mat, matPath);
    }
}
#endif
