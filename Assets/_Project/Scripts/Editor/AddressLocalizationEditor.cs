#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public class AddressLocalizationEditor
{
    private const string JSON_DIR = "Assets/Resources/Localization";

    [MenuItem("Tools/Delivery Game/Localization/Reload Address Language Files", false, 60)]
    public static void ReloadLanguageFiles()
    {
        AddressLocalizationManager.Reload();
        Debug.Log("<color=#32FF64>[AddressLocalizationEditor] Address localization language files reloaded successfully!</color>");
    }

    [MenuItem("Tools/Delivery Game/Localization/Switch Language to English (EN)", false, 61)]
    public static void SwitchToEnglish()
    {
        AddressLocalizationManager.SetLanguage("en");
        Debug.Log("<color=#32FF64>[AddressLocalizationEditor] Active language switched to English (EN)!</color>");
    }

    [MenuItem("Tools/Delivery Game/Localization/Switch Language to Turkish (TR)", false, 62)]
    public static void SwitchToTurkish()
    {
        AddressLocalizationManager.SetLanguage("tr");
        Debug.Log("<color=#32FF64>[AddressLocalizationEditor] Active language switched to Turkish (TR)!</color>");
    }

    [MenuItem("Tools/Delivery Game/Localization/Open English Address File (address_descriptions_en.json)", false, 70)]
    public static void OpenEnglishFile()
    {
        string path = Path.Combine(Application.dataPath, "Resources/Localization/address_descriptions_en.json");
        if (File.Exists(path))
        {
            EditorUtility.OpenWithDefaultApp(path);
        }
        else
        {
            Debug.LogWarning($"[AddressLocalizationEditor] File not found at: {path}");
        }
    }

    [MenuItem("Tools/Delivery Game/Localization/Open Turkish Address File (address_descriptions_tr.json)", false, 71)]
    public static void OpenTurkishFile()
    {
        string path = Path.Combine(Application.dataPath, "Resources/Localization/address_descriptions_tr.json");
        if (File.Exists(path))
        {
            EditorUtility.OpenWithDefaultApp(path);
        }
        else
        {
            Debug.LogWarning($"[AddressLocalizationEditor] File not found at: {path}");
        }
    }
}
#endif
