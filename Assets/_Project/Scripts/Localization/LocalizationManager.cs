using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class LocalizationEntry
{
    public string key;
    [TextArea(2, 5)]
    public string value;
}

[Serializable]
public class LocalizationDatabase
{
    public string language = "tr";
    public List<LocalizationEntry> entries = new List<LocalizationEntry>();
}

/// <summary>
/// Central localization manager that loads key-value translation dictionaries from Resources/Localization/
/// and manages language state (Turkish 'tr', English 'en') across the entire game.
/// </summary>
public class LocalizationManager : MonoBehaviour
{
    public const string PREF_LANG_KEY = "SelectedLanguage";
    private const string RESOURCE_FOLDER = "Localization";
    private const string FILE_PREFIX = "localization_";

    public static LocalizationManager Instance { get; private set; }

    [Header("--- ACTIVE LANGUAGE ---")]
    [SerializeField] private string currentLanguage = "tr";

    private static Dictionary<string, string> localizedStrings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private static bool isInitialized = false;

    public static event Action<string> OnLanguageChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        AutoInitialize();
    }

    public static void EnsureInitialized()
    {
        AutoInitialize();
    }

    private static void AutoInitialize()
    {
        if (!isInitialized)
        {
            string defaultLang = "tr";
            if (Application.systemLanguage != SystemLanguage.Turkish)
            {
                // If system language is English, default to en, otherwise tr
                if (Application.systemLanguage == SystemLanguage.English) defaultLang = "en";
            }
            string savedLang = PlayerPrefs.GetString(PREF_LANG_KEY, defaultLang);
            LoadLanguageData(savedLang);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!isInitialized)
        {
            string savedLang = PlayerPrefs.GetString(PREF_LANG_KEY, currentLanguage);
            SetLanguage(savedLang);
        }
        else
        {
            currentLanguage = PlayerPrefs.GetString(PREF_LANG_KEY, currentLanguage);
        }
    }

    /// <summary>
    /// Changes active language, reloads dictionaries, updates PlayerPrefs and notifies all UI subscribers.
    /// </summary>
    public static void SetLanguage(string langCode)
    {
        if (string.IsNullOrEmpty(langCode)) langCode = "tr";
        langCode = langCode.ToLower().Trim();

        LoadLanguageData(langCode);
        PlayerPrefs.SetString(PREF_LANG_KEY, langCode);
        PlayerPrefs.Save();

        if (Instance != null)
        {
            Instance.currentLanguage = langCode;
        }

        // Also sync AddressLocalizationManager
        AddressLocalizationManager.SetLanguage(langCode);

        OnLanguageChanged?.Invoke(langCode);
        Debug.Log($"<color=#32FF64>[LocalizationManager] Active game language switched to: '{langCode}' (Loaded {localizedStrings.Count} UI keys)</color>");
    }

    /// <summary>
    /// Changes active language using Unity's SystemLanguage enum.
    /// </summary>
    public static void SetLanguage(SystemLanguage lang)
    {
        string code = (lang == SystemLanguage.Turkish) ? "tr" : "en";
        SetLanguage(code);
    }

    /// <summary>
    /// Loads language JSON file from Resources/Localization/localization_{langCode}.json
    /// </summary>
    public static bool LoadLanguageData(string langCode)
    {
        string resPath = $"{RESOURCE_FOLDER}/{FILE_PREFIX}{langCode}";
        TextAsset jsonAsset = Resources.Load<TextAsset>(resPath);

        // Fallback to English if requested language file not found
        if (jsonAsset == null && langCode != "en")
        {
            Debug.LogWarning($"[LocalizationManager] Language file '{resPath}' not found. Falling back to English.");
            resPath = $"{RESOURCE_FOLDER}/{FILE_PREFIX}en";
            jsonAsset = Resources.Load<TextAsset>(resPath);
        }

        if (jsonAsset == null)
        {
            Debug.LogWarning($"[LocalizationManager] Failed to load UI localization file: '{resPath}'");
            isInitialized = true;
            return false;
        }

        try
        {
            LocalizationDatabase db = JsonUtility.FromJson<LocalizationDatabase>(jsonAsset.text);
            localizedStrings.Clear();

            if (db != null && db.entries != null)
            {
                foreach (var entry in db.entries)
                {
                    if (entry != null && !string.IsNullOrEmpty(entry.key))
                    {
                        localizedStrings[entry.key] = entry.value ?? "";
                    }
                }
            }

            isInitialized = true;
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LocalizationManager] Error parsing localization JSON '{resPath}': {ex.Message}");
            return false;
        }
    }

    public static string CurrentLanguage => GetCurrentLanguage();
    public static SystemLanguage CurrentSystemLanguage => IsTurkish ? SystemLanguage.Turkish : SystemLanguage.English;

    /// <summary>
    /// Returns the localized string for the specified key. If key is missing, returns the fallback.
    /// </summary>
    public static string Get(string key, string fallback = "")
    {
        if (!isInitialized) AutoInitialize();
        if (string.IsNullOrEmpty(key)) return fallback;

        if (localizedStrings.TryGetValue(key, out string val) && !string.IsNullOrEmpty(val))
        {
            return val;
        }

        return !string.IsNullOrEmpty(fallback) ? fallback : key;
    }

    /// <summary>
    /// Returns the formatted localized string for the specified key using string.Format.
    /// If key is found in the dictionary, formats its localized string with args.
    /// If key is not found, formats key as raw template with args.
    /// </summary>
    public static string GetFormat(string key, params object[] args)
    {
        string raw = Get(key, key);
        if (args == null || args.Length == 0) return raw;

        try
        {
            return string.Format(raw, args);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[LocalizationManager] Format error for key '{key}' with text '{raw}': {ex.Message}");
            return raw;
        }
    }

    /// <summary>
    /// Checks if a translation key exists in the active dictionary.
    /// </summary>
    public static bool HasKey(string key)
    {
        if (!isInitialized) AutoInitialize();
        if (string.IsNullOrEmpty(key)) return false;
        return localizedStrings.ContainsKey(key);
    }

    /// <summary>
    /// Gets active language code ('tr' or 'en').
    /// </summary>
    public static string GetCurrentLanguage()
    {
        if (Instance != null) return Instance.currentLanguage;
        return PlayerPrefs.GetString(PREF_LANG_KEY, "tr");
    }

    /// <summary>
    /// Returns true if currently active language is Turkish.
    /// </summary>
    public static bool IsTurkish => GetCurrentLanguage().Equals("tr", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Returns true if currently active language is English.
    /// </summary>
    public static bool IsEnglish => GetCurrentLanguage().Equals("en", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Reloads current language data immediately.
    /// </summary>
    public static void Reload()
    {
        string current = GetCurrentLanguage();
        LoadLanguageData(current);
        AddressLocalizationManager.Reload();
        OnLanguageChanged?.Invoke(current);
    }
}
