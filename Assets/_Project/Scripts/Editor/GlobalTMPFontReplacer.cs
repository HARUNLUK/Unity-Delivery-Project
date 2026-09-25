#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TMPro;

public class GlobalTMPFontReplacer : EditorWindow
{
    public TMP_FontAsset targetFontAsset;
    public bool updateSceneObjects = true;
    public bool updatePrefabs = true;

    [MenuItem("Tools/Delivery Game/UI/3. Global Font Replacer Tool", false, 12)]
    public static void OpenWindow()
    {
        GlobalTMPFontReplacer window = GetWindow<GlobalTMPFontReplacer>("TMP Font Replacer");
        window.minSize = new Vector2(400, 240);
        window.AutoFindCustomFont();
        window.Show();
    }

    [MenuItem("Tools/Delivery Game/UI/Quick Apply Custom Font to All UI", false, 13)]
    public static void QuickApplyCustomFont()
    {
        TMP_FontAsset font = FindBestCustomFont();
        if (font == null)
        {
            EditorUtility.DisplayDialog("Font Bulunamadı", 
                "Assets klasöründe bir TextMeshPro Font Asset'i bulunamadı.\nLütfen Fonts klasörüne bir .ttf atıp 'Create -> TextMeshPro -> Font Asset' yapın.", "Tamam");
            OpenWindow();
            return;
        }

        int count = ReplaceFontsInActiveScene(font);
        EditorUtility.DisplayDialog("Fontlar Güncellendi!", 
            $"Sahnedeki toplam {count} adet TextMeshPro yazısı '{font.name}' fontuna başarıyla dönüştürüldü!", "Harika!");
    }

    private void OnEnable()
    {
        AutoFindCustomFont();
    }

    private void AutoFindCustomFont()
    {
        if (targetFontAsset == null)
        {
            targetFontAsset = FindBestCustomFont();
        }
    }

    private static TMP_FontAsset FindBestCustomFont()
    {
        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        TMP_FontAsset fallback = null;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font != null)
            {
                // Prefer user font in _Project folder or named Inter / Rajdhani / Custom
                if (path.Contains("_Project") || path.ToLower().Contains("inter") || path.ToLower().Contains("rajdhani") || path.ToLower().Contains("custom"))
                {
                    return font;
                }
                if (!path.Contains("LiberationSans") && fallback == null)
                {
                    fallback = font;
                }
            }
        }
        return fallback;
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("🔤 Global TextMeshPro Font Değiştirici", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Bu araç, sahnedeki ve projedeki tüm UI yazılarını (TextMeshProUGUI & TextMeshPro) tek tıkla seçeceğiniz yeni fonta dönüştürür.", MessageType.Info);
        
        GUILayout.Space(10);
        targetFontAsset = (TMP_FontAsset)EditorGUILayout.ObjectField("Hedef Font Asset:", targetFontAsset, typeof(TMP_FontAsset), false);

        GUILayout.Space(8);
        updateSceneObjects = EditorGUILayout.Toggle("Açık Sahneyi Güncelle", updateSceneObjects);
        updatePrefabs = EditorGUILayout.Toggle("Prefabları da Güncelle", updatePrefabs);

        GUILayout.Space(15);

        GUI.backgroundColor = new Color(0.2f, 0.85f, 0.4f);
        if (GUILayout.Button("TÜM YAZILARI BU FONTA DÖNÜŞTÜR", GUILayout.Height(42)))
        {
            if (targetFontAsset == null)
            {
                EditorUtility.DisplayDialog("Uyarı", "Lütfen yukarıdaki alana dönüştürmek istediğiniz TMP Font Asset'ini sürükleyin!", "Tamam");
                return;
            }

            int sceneCount = 0;
            int prefabCount = 0;

            if (updateSceneObjects)
            {
                sceneCount = ReplaceFontsInActiveScene(targetFontAsset);
            }

            if (updatePrefabs)
            {
                prefabCount = ReplaceFontsInAllPrefabs(targetFontAsset);
            }

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Tamamlandı!", 
                $"İşlem Başarılı!\n- Sahnedeki Metin Sayısı: {sceneCount}\n- Güncellenen Prefab Sayısı: {prefabCount}\n\nTüm yazılar '{targetFontAsset.name}' fontuna dönüştürüldü.", "Tamam");
        }
        GUI.backgroundColor = Color.white;
    }

    public static int ReplaceFontsInActiveScene(TMP_FontAsset newFont)
    {
        if (newFont == null) return 0;

        // Find all UI TextMeshProUGUI
        TextMeshProUGUI[] uiTexts = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int modifiedCount = 0;

        foreach (var text in uiTexts)
        {
            if (text != null && text.font != newFont)
            {
                Undo.RecordObject(text, "Change TMP Font");
                text.font = newFont;
                EditorUtility.SetDirty(text.gameObject);
                modifiedCount++;
            }
        }

        // Find all 3D TextMeshPro (world space labels, signs, terminals)
        TextMeshPro[] worldTexts = Object.FindObjectsByType<TextMeshPro>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var text in worldTexts)
        {
            if (text != null && text.font != newFont)
            {
                Undo.RecordObject(text, "Change TMP Font");
                text.font = newFont;
                EditorUtility.SetDirty(text.gameObject);
                modifiedCount++;
            }
        }

        if (modifiedCount > 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        }

        Debug.Log($"<color=#32FF64><b>[GlobalTMPFontReplacer]</b> Sahnedeki {modifiedCount} adet TextMeshPro bileşeni '{newFont.name}' fontuna dönüştürüldü!</color>");
        return modifiedCount;
    }

    public static int ReplaceFontsInAllPrefabs(TMP_FontAsset newFont)
    {
        if (newFont == null) return 0;

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project" });
        int modifiedPrefabs = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
            if (prefabRoot == null) continue;

            bool isDirty = false;
            TextMeshProUGUI[] uiTexts = prefabRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in uiTexts)
            {
                if (t.font != newFont)
                {
                    t.font = newFont;
                    isDirty = true;
                }
            }

            TextMeshPro[] worldTexts = prefabRoot.GetComponentsInChildren<TextMeshPro>(true);
            foreach (var t in worldTexts)
            {
                if (t.font != newFont)
                {
                    t.font = newFont;
                    isDirty = true;
                }
            }

            if (isDirty)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                modifiedPrefabs++;
            }

            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        Debug.Log($"<color=#32FF64><b>[GlobalTMPFontReplacer]</b> Toplam {modifiedPrefabs} adet Prefab içerisindeki yazılar '{newFont.name}' fontuna dönüştürüldü!</color>");
        return modifiedPrefabs;
    }
}
#endif
