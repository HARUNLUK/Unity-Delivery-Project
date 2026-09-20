using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Dedicated Language Selection Controller for Settings Menu.
/// Pure logic controller: binds strictly to existing UI buttons/dropdowns in the Inspector.
/// Does NOT create, inject, or instantiate any new GameObjects or text elements into the scene.
/// </summary>
public class SettingsLanguageUI : MonoBehaviour
{
    public static SettingsLanguageUI Instance { get; private set; }

    [Header("--- UI BUTTONS (ASSIGN IN INSPECTOR) ---")]
    [Tooltip("Button to select Turkish language")]
    public Button turkishButton;
    public Image turkishButtonBg;
    public TextMeshProUGUI turkishButtonText;

    [Tooltip("Button to select English language")]
    public Button englishButton;
    public Image englishButtonBg;
    public TextMeshProUGUI englishButtonText;

    [Header("--- DROPDOWN (OPTIONAL) ---")]
    [Tooltip("Optional dropdown alternative if using TMP_Dropdown")]
    public TMP_Dropdown languageDropdown;

    [Header("--- OPTIONAL ROW LABEL ---")]
    [Tooltip("Optional existing text component for the row title")]
    public TextMeshProUGUI rowLabelText;

    [Header("--- COLOR HIGHLIGHTS (OPTIONAL) ---")]
    public bool updateColors = true;
    public Color activeBtnColor = new Color(0.12f, 0.55f, 0.85f, 1.0f);
    public Color inactiveBtnColor = new Color(0.14f, 0.17f, 0.22f, 0.95f);
    public Color activeTextColor = Color.white;
    public Color inactiveTextColor = new Color(0.70f, 0.75f, 0.85f, 1.0f);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        FindExistingReferencesIfEmpty();
        SetupListeners();
    }

    private void Start()
    {
        FindExistingReferencesIfEmpty();
        RefreshUIState();
    }

    private void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
        RefreshUIState();
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
    }

    private void HandleLanguageChanged(string newLang)
    {
        RefreshUIState();
    }

    private void SetupListeners()
    {
        if (turkishButton != null)
        {
            turkishButton.onClick.RemoveAllListeners();
            turkishButton.onClick.AddListener(SelectTurkish);
        }

        if (englishButton != null)
        {
            englishButton.onClick.RemoveAllListeners();
            englishButton.onClick.AddListener(SelectEnglish);
        }

        if (languageDropdown != null)
        {
            languageDropdown.onValueChanged.RemoveAllListeners();
            languageDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        }
    }

    public void SelectTurkish()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetLanguage("tr");
        }
        else
        {
            LocalizationManager.SetLanguage("tr");
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }

        RefreshUIState();
    }

    public void SelectEnglish()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetLanguage("en");
        }
        else
        {
            LocalizationManager.SetLanguage("en");
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }

        RefreshUIState();
    }

    public void ToggleLanguage()
    {
        if (LocalizationManager.IsTurkish)
        {
            SelectEnglish();
        }
        else
        {
            SelectTurkish();
        }
    }

    private void OnDropdownValueChanged(int index)
    {
        if (index == 0) SelectTurkish();
        else if (index == 1) SelectEnglish();
    }

    /// <summary>
    /// Updates visuals of existing UI components without creating new elements.
    /// </summary>
    public void RefreshUIState()
    {
        bool isTr = LocalizationManager.IsTurkish;

        // 1. Optional Row Label
        if (rowLabelText != null)
        {
            rowLabelText.text = isTr ? "Oyun Dili" : "Language";
        }

        // 2. Turkish Button Visuals
        if (updateColors)
        {
            if (turkishButtonBg != null) turkishButtonBg.color = isTr ? activeBtnColor : inactiveBtnColor;
            if (turkishButtonText != null) turkishButtonText.color = isTr ? activeTextColor : inactiveTextColor;
        }

        // 3. English Button Visuals
        if (updateColors)
        {
            if (englishButtonBg != null) englishButtonBg.color = !isTr ? activeBtnColor : inactiveBtnColor;
            if (englishButtonText != null) englishButtonText.color = !isTr ? activeTextColor : inactiveTextColor;
        }

        // 4. Dropdown State
        if (languageDropdown != null)
        {
            languageDropdown.SetValueWithoutNotify(isTr ? 0 : 1);
        }
    }

    /// <summary>
    /// Only searches for existing children already placed in the hierarchy. Never instantiates new GameObjects.
    /// </summary>
    public void FindExistingReferencesIfEmpty()
    {
        if (turkishButton == null)
        {
            Transform t = transform.Find("BtnTurkish");
            if (t == null) t = transform.Find("ButtonTurkish");
            if (t == null) t = transform.Find("TurkishBtn");
            if (t != null) turkishButton = t.GetComponent<Button>();
        }

        if (turkishButton != null)
        {
            if (turkishButtonBg == null) turkishButtonBg = turkishButton.GetComponent<Image>();
            if (turkishButtonText == null) turkishButtonText = turkishButton.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (englishButton == null)
        {
            Transform t = transform.Find("BtnEnglish");
            if (t == null) t = transform.Find("ButtonEnglish");
            if (t == null) t = transform.Find("EnglishBtn");
            if (t != null) englishButton = t.GetComponent<Button>();
        }

        if (englishButton != null)
        {
            if (englishButtonBg == null) englishButtonBg = englishButton.GetComponent<Image>();
            if (englishButtonText == null) englishButtonText = englishButton.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (languageDropdown == null)
        {
            Transform d = transform.Find("DropdownLanguage");
            if (d == null) d = transform.Find("LanguageDropdown");
            if (d != null) languageDropdown = d.GetComponent<TMP_Dropdown>();
        }
    }
}
