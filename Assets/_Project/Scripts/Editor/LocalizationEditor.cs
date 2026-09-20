#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class LocalizationEditor
{
    private const string LOCALIZATION_RES_PATH = "Assets/Resources/Localization";

    [MenuItem("Tools/Delivery Game/Localization/Switch to Turkish (Türkçe)", false, 10)]
    public static void SwitchToTurkish()
    {
        LocalizationManager.SetLanguage(SystemLanguage.Turkish);
        AddressLocalizationManager.SetLanguage("tr");
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetLanguage("tr");
        }
        Debug.Log("<color=#32FF64>[LocalizationEditor] Active game language switched to Turkish (TR)!</color>");
    }

    [MenuItem("Tools/Delivery Game/Localization/Switch to English (English)", false, 11)]
    public static void SwitchToEnglish()
    {
        LocalizationManager.SetLanguage(SystemLanguage.English);
        AddressLocalizationManager.SetLanguage("en");
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetLanguage("en");
        }
        Debug.Log("<color=#32FF64>[LocalizationEditor] Active game language switched to English (EN)!</color>");
    }

    [MenuItem("Tools/Delivery Game/Localization/Reload All Localization Files", false, 20)]
    public static void ReloadAllLanguageFiles()
    {
        LocalizationManager.Reload();
        AddressLocalizationManager.Reload();
        Debug.Log("<color=#32FF64>[LocalizationEditor] All localization databases and address files reloaded successfully!</color>");
    }

    [MenuItem("Tools/Delivery Game/Localization/Validate Database Parity (TR vs EN)", false, 30)]
    public static void ValidateDatabases()
    {
        string trPath = Path.Combine(Application.dataPath, "Resources/Localization/localization_tr.json");
        string enPath = Path.Combine(Application.dataPath, "Resources/Localization/localization_en.json");

        if (!File.Exists(trPath) || !File.Exists(enPath))
        {
            EditorUtility.DisplayDialog("Localization Error", "Missing localization database files in Assets/Resources/Localization/", "OK");
            return;
        }

        string trJson = File.ReadAllText(trPath);
        string enJson = File.ReadAllText(enPath);

        LocalizationDatabase trDb = JsonUtility.FromJson<LocalizationDatabase>(trJson);
        LocalizationDatabase enDb = JsonUtility.FromJson<LocalizationDatabase>(enJson);

        HashSet<string> trKeys = new HashSet<string>();
        foreach (var entry in trDb.entries)
        {
            if (!string.IsNullOrEmpty(entry.key))
            {
                trKeys.Add(entry.key);
            }
        }

        HashSet<string> enKeys = new HashSet<string>();
        foreach (var entry in enDb.entries)
        {
            if (!string.IsNullOrEmpty(entry.key))
            {
                enKeys.Add(entry.key);
            }
        }

        List<string> missingInEn = new List<string>();
        foreach (var k in trKeys)
        {
            if (!enKeys.Contains(k)) missingInEn.Add(k);
        }

        List<string> missingInTr = new List<string>();
        foreach (var k in enKeys)
        {
            if (!trKeys.Contains(k)) missingInTr.Add(k);
        }

        if (missingInEn.Count == 0 && missingInTr.Count == 0)
        {
            string msg = $"Success! Both Turkish and English databases are in 100% parity ({trKeys.Count} keys validated).";
            Debug.Log($"<color=#32FF64>[LocalizationEditor] {msg}</color>");
            EditorUtility.DisplayDialog("Localization Validation Passed", msg, "OK");
        }
        else
        {
            string err = $"Discrepancies found:\n- Total TR Keys: {trKeys.Count}\n- Total EN Keys: {enKeys.Count}\n";
            if (missingInEn.Count > 0)
            {
                err += $"\nMissing in EN ({missingInEn.Count}):\n" + string.Join("\n", missingInEn.GetRange(0, Mathf.Min(10, missingInEn.Count)));
            }
            if (missingInTr.Count > 0)
            {
                err += $"\nMissing in TR ({missingInTr.Count}):\n" + string.Join("\n", missingInTr.GetRange(0, Mathf.Min(10, missingInTr.Count)));
            }

            Debug.LogError($"[LocalizationEditor] {err}");
            EditorUtility.DisplayDialog("Localization Parity Warning", err, "OK");
        }
    }

    [MenuItem("Tools/Delivery Game/Localization/Open Turkish Database JSON", false, 50)]
    public static void OpenTurkishJson()
    {
        string path = Path.Combine(Application.dataPath, "Resources/Localization/localization_tr.json");
        if (File.Exists(path)) EditorUtility.OpenWithDefaultApp(path);
    }

    [MenuItem("Tools/Delivery Game/Localization/Open English Database JSON", false, 51)]
    public static void OpenEnglishJson()
    {
        string path = Path.Combine(Application.dataPath, "Resources/Localization/localization_en.json");
        if (File.Exists(path)) EditorUtility.OpenWithDefaultApp(path);
    }
}
#endif
