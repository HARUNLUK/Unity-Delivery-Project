using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class AddressLocalizationEntry
{
    public string id;
    public string district;
    public string recipient;
    public string addressName;
    [TextArea(2, 5)]
    public string description;
}

[System.Serializable]
public class AddressLocalizationDatabase
{
    public string language = "en";
    public List<AddressLocalizationEntry> entries = new List<AddressLocalizationEntry>();
}

/// <summary>
/// Global manager that loads and caches localized cargo address descriptions,
/// recipient names, and street names from JSON files in Resources/Localization/.
/// </summary>
public class AddressLocalizationManager : MonoBehaviour
{
    private const string PREF_LANG_KEY = "SelectedLanguage";
    private const string RESOURCE_FOLDER = "Localization";
    private const string FILE_PREFIX = "address_descriptions_";

    public static AddressLocalizationManager Instance { get; private set; }

    [Header("--- CURRENT LANGUAGE ---")]
    [SerializeField] private string currentLanguage = "en"; // "en", "tr", etc.

    public string CurrentLanguage => currentLanguage;

    private static Dictionary<string, AddressLocalizationEntry> addressMap = new Dictionary<string, AddressLocalizationEntry>(StringComparer.OrdinalIgnoreCase);
    private static bool isInitialized = false;

    public static event Action<string> OnLanguageChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        if (!isInitialized)
        {
            string savedLang = PlayerPrefs.GetString(PREF_LANG_KEY, "en");
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
    }

    /// <summary>
    /// Switches the active language, loads corresponding JSON from Resources, and notifies subscribers.
    /// </summary>
    public static void SetLanguage(string langCode)
    {
        if (string.IsNullOrEmpty(langCode)) langCode = "en";
        langCode = langCode.ToLower().Trim();

        LoadLanguageData(langCode);
        PlayerPrefs.SetString(PREF_LANG_KEY, langCode);
        PlayerPrefs.Save();

        if (Instance != null)
        {
            Instance.currentLanguage = langCode;
        }

        OnLanguageChanged?.Invoke(langCode);
        Debug.Log($"<color=#32FF64>[AddressLocalizationManager] Active address language set to: '{langCode}' (Loaded {addressMap.Count} entries)</color>");
    }

    /// <summary>
    /// Loads language JSON file from Resources/Localization/address_descriptions_{langCode}.json
    /// </summary>
    public static bool LoadLanguageData(string langCode)
    {
        string resPath = $"{RESOURCE_FOLDER}/{FILE_PREFIX}{langCode}";
        TextAsset jsonAsset = Resources.Load<TextAsset>(resPath);

        // Fallback to English if requested language file not found
        if (jsonAsset == null && langCode != "en")
        {
            Debug.LogWarning($"[AddressLocalizationManager] Language file '{resPath}' not found. Falling back to English.");
            resPath = $"{RESOURCE_FOLDER}/{FILE_PREFIX}en";
            jsonAsset = Resources.Load<TextAsset>(resPath);
        }

        if (jsonAsset == null)
        {
            Debug.LogWarning($"[AddressLocalizationManager] Failed to load address localization file: '{resPath}'");
            isInitialized = true;
            return false;
        }

        try
        {
            AddressLocalizationDatabase db = JsonUtility.FromJson<AddressLocalizationDatabase>(jsonAsset.text);
            addressMap.Clear();

            if (db != null && db.entries != null)
            {
                foreach (var entry in db.entries)
                {
                    if (entry != null && !string.IsNullOrEmpty(entry.id))
                    {
                        string cleanId = NormalizeId(entry.id);
                        addressMap[cleanId] = entry;

                        // Also store original ID key if different
                        if (!addressMap.ContainsKey(entry.id))
                        {
                            addressMap[entry.id] = entry;
                        }

                        // Also store numeric integer key if applicable (e.g. "DP-005" -> "5")
                        string numOnly = System.Text.RegularExpressions.Regex.Replace(entry.id, @"[^\d]", "");
                        if (!string.IsNullOrEmpty(numOnly) && int.TryParse(numOnly, out int parsedNum))
                        {
                            string simpleNumKey = parsedNum.ToString();
                            addressMap[simpleNumKey] = entry;
                            addressMap[$"DP-{parsedNum:D3}"] = entry;
                            addressMap[$"DP-{parsedNum:D2}"] = entry;
                            addressMap[$"DP-{parsedNum}"] = entry;
                            addressMap[$"DELIVERYPOINT-{parsedNum}"] = entry;
                            addressMap[$"DELIVERYPOINT_{parsedNum}"] = entry;
                            addressMap[$"POINT{parsedNum}"] = entry;
                        }
                    }
                }
            }

            isInitialized = true;
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AddressLocalizationManager] Error parsing localization JSON: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Returns localized visual address clue / description for the given Point ID.
    /// Returns the provided fallback if ID is not found or description is empty.
    /// </summary>
    public static string GetDescription(string pointId, string fallback = "")
    {
        if (!isInitialized) AutoInitialize();
        if (string.IsNullOrEmpty(pointId)) return fallback;

        string normId = NormalizeId(pointId);
        if (addressMap.TryGetValue(normId, out AddressLocalizationEntry entry) && !string.IsNullOrEmpty(entry.description))
        {
            return entry.description;
        }

        if (addressMap.TryGetValue(pointId, out entry) && !string.IsNullOrEmpty(entry.description))
        {
            return entry.description;
        }

        string numOnly = System.Text.RegularExpressions.Regex.Replace(pointId, @"[^\d]", "");
        if (!string.IsNullOrEmpty(numOnly) && addressMap.TryGetValue(numOnly, out entry) && !string.IsNullOrEmpty(entry.description))
        {
            return entry.description;
        }

        return fallback;
    }

    /// <summary>
    /// Returns localized Address Name (e.g. 'Downtown - City Bakery') for the given Point ID.
    /// </summary>
    public static string GetAddressName(string pointId, string fallback = "")
    {
        if (!isInitialized) AutoInitialize();
        if (string.IsNullOrEmpty(pointId)) return fallback;

        string normId = NormalizeId(pointId);
        if (addressMap.TryGetValue(normId, out AddressLocalizationEntry entry) && !string.IsNullOrEmpty(entry.addressName))
        {
            return entry.addressName;
        }

        if (addressMap.TryGetValue(pointId, out entry) && !string.IsNullOrEmpty(entry.addressName))
        {
            return entry.addressName;
        }

        string numOnly = System.Text.RegularExpressions.Regex.Replace(pointId, @"[^\d]", "");
        if (!string.IsNullOrEmpty(numOnly) && addressMap.TryGetValue(numOnly, out entry) && !string.IsNullOrEmpty(entry.addressName))
        {
            return entry.addressName;
        }

        return fallback;
    }

    /// <summary>
    /// Returns localized Recipient Name (e.g. 'Chef Marco') for the given Point ID.
    /// </summary>
    public static string GetRecipient(string pointId, string fallback = "")
    {
        if (!isInitialized) AutoInitialize();
        if (string.IsNullOrEmpty(pointId)) return fallback;

        string normId = NormalizeId(pointId);
        if (addressMap.TryGetValue(normId, out AddressLocalizationEntry entry) && !string.IsNullOrEmpty(entry.recipient))
        {
            return entry.recipient;
        }

        if (addressMap.TryGetValue(pointId, out entry) && !string.IsNullOrEmpty(entry.recipient))
        {
            return entry.recipient;
        }

        string numOnly = System.Text.RegularExpressions.Regex.Replace(pointId, @"[^\d]", "");
        if (!string.IsNullOrEmpty(numOnly) && addressMap.TryGetValue(numOnly, out entry) && !string.IsNullOrEmpty(entry.recipient))
        {
            return entry.recipient;
        }

        return fallback;
    }

    /// <summary>
    /// Returns full localized entry for the given Point ID, or null.
    /// </summary>
    public static AddressLocalizationEntry GetEntry(string pointId)
    {
        if (!isInitialized) AutoInitialize();
        if (string.IsNullOrEmpty(pointId)) return null;

        string normId = NormalizeId(pointId);
        if (addressMap.TryGetValue(normId, out AddressLocalizationEntry entry))
        {
            return entry;
        }

        if (addressMap.TryGetValue(pointId, out entry))
        {
            return entry;
        }

        string numOnly = System.Text.RegularExpressions.Regex.Replace(pointId, @"[^\d]", "");
        if (!string.IsNullOrEmpty(numOnly) && addressMap.TryGetValue(numOnly, out entry))
        {
            return entry;
        }

        return null;
    }

    /// <summary>
    /// Reloads current language data immediately (e.g. after modifying the JSON file).
    /// </summary>
    public static void Reload()
    {
        string current = Instance != null ? Instance.currentLanguage : PlayerPrefs.GetString(PREF_LANG_KEY, "en");
        LoadLanguageData(current);
        OnLanguageChanged?.Invoke(current);
    }

    private static string NormalizeId(string rawId)
    {
        if (string.IsNullOrEmpty(rawId)) return "";
        return rawId.Trim().ToUpper().Replace("_", "-").Replace(" ", "");
    }
}
