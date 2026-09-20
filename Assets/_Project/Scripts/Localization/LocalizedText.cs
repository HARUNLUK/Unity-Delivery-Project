using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Helper component that automatically updates TextMeshProUGUI / TextMeshPro / Text
/// whenever active game language is changed.
/// </summary>
[ExecuteAlways]
public class LocalizedText : MonoBehaviour
{
    [Tooltip("Localization key from localization_{lang}.json")]
    public string localizationKey = "";

    [Tooltip("Fallback text if key is not found")]
    public string fallbackText = "";

    [Tooltip("Optional prefix added before localized string")]
    public string prefix = "";

    [Tooltip("Optional suffix added after localized string")]
    public string suffix = "";

    [Tooltip("Transform text to all uppercase")]
    public bool toUpperCase = false;

    private TextMeshProUGUI tmpUgui;
    private TextMeshPro tmp3D;
    private Text legacyText;

    private void Awake()
    {
        CacheComponents();
    }

    private void OnEnable()
    {
        CacheComponents();
        LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
        Refresh();
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
    }

    private void Start()
    {
        Refresh();
    }

    private void CacheComponents()
    {
        if (tmpUgui == null) tmpUgui = GetComponent<TextMeshProUGUI>();
        if (tmp3D == null && tmpUgui == null) tmp3D = GetComponent<TextMeshPro>();
        if (legacyText == null && tmpUgui == null && tmp3D == null) legacyText = GetComponent<Text>();
    }

    private void HandleLanguageChanged(string lang)
    {
        Refresh();
    }

    /// <summary>
    /// Updates displayed text according to the current active language.
    /// </summary>
    public void Refresh()
    {
        if (string.IsNullOrEmpty(localizationKey)) return;
        CacheComponents();

        string localized = LocalizationManager.Get(localizationKey, fallbackText);
        if (string.IsNullOrEmpty(localized) && !string.IsNullOrEmpty(fallbackText))
        {
            localized = fallbackText;
        }

        string finalResult = prefix + localized + suffix;
        if (toUpperCase) finalResult = finalResult.ToUpper();

        if (tmpUgui != null)
        {
            tmpUgui.text = finalResult;
        }
        else if (tmp3D != null)
        {
            tmp3D.text = finalResult;
        }
        else if (legacyText != null)
        {
            legacyText.text = finalResult;
        }
    }

    /// <summary>
    /// Sets a new key dynamically and updates the text immediately.
    /// </summary>
    public void SetKey(string newKey, string newFallback = "")
    {
        localizationKey = newKey;
        if (!string.IsNullOrEmpty(newFallback)) fallbackText = newFallback;
        Refresh();
    }
}
