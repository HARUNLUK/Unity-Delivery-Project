#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Dedicated Editor Tool to add the Language Dropdown to Settings UI.
/// Creates a 100% complete, fully working TMP_Dropdown (with template, viewport, item toggles, scrollview)
/// matching the exact visual style, colors, fonts, and dimensions of existing Settings rows.
/// </summary>
public static class SettingsLanguageUIBuilder
{
    private const string FONT_PATH = "Assets/_Project/Fonts/Inter-VariableFont_opsz,wght SDF.asset";
    private const string SPRITE_BTN_DARK = "Assets/_Project/Textures/UI/UI_Button_Dark_Base.png";
    private const string SPRITE_CARD = "Assets/_Project/Textures/UI/UI_Glass_Card.png";

    [MenuItem("Tools/Delivery Game/UI/Add Language Dropdown to Settings UI", false, 30)]
    [MenuItem("GameObject/Delivery Game/Add Language Dropdown to Settings UI", false, 20)]
    public static void AddLanguageDropdownToSettingsUI()
    {
        // 1. Locate SettingsPanel in the current scene
        GameObject settingsPanel = FindSettingsPanelInScene();
        if (settingsPanel == null)
        {
            EditorUtility.DisplayDialog(
                "Settings Panel Not Found",
                "Could not find 'SettingsPanel' in the active scene.\nPlease ensure Main_Scene is open and contains SettingsPanel.",
                "OK"
            );
            return;
        }

        // 2. Find target parent section (GraphicsSection or ContentArea)
        Transform targetParent = FindTargetParentSection(settingsPanel.transform);
        if (targetParent == null)
        {
            EditorUtility.DisplayDialog(
                "Section Not Found",
                "Could not find 'GraphicsSection' or 'ContentArea' inside SettingsPanel.",
                "OK"
            );
            return;
        }

        // 3. Remove any previous LanguageRow (buttons or broken dropdowns) to ensure a clean build
        Transform existingRow = targetParent.Find("LanguageRow");
        if (existingRow == null)
        {
            existingRow = settingsPanel.transform.Find("SettingsCard/ContentArea/GraphicsSection/LanguageRow");
            if (existingRow == null)
            {
                existingRow = settingsPanel.transform.Find("SettingsCard/ContentArea/LanguageRow");
            }
        }

        if (existingRow != null)
        {
            Undo.DestroyObjectImmediate(existingRow.gameObject);
        }

        // 4. Create new LanguageRow
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
        Sprite btnDarkSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_BTN_DARK);
        Sprite cardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_CARD);

        GameObject row = new GameObject("LanguageRow", typeof(RectTransform));
        row.transform.SetParent(targetParent, false);
        Undo.RegisterCreatedObjectUndo(row, "Add Language Dropdown to Settings UI");

        RectTransform rrt = row.GetComponent<RectTransform>();
        rrt.sizeDelta = new Vector2(0, 42);
        rrt.localScale = Vector3.one;
        row.transform.SetSiblingIndex(0); // Position at the top of Graphics section

        // 4.1 Label on Left
        GameObject lblObj = new GameObject("Label", typeof(RectTransform));
        lblObj.transform.SetParent(row.transform, false);
        RectTransform lblRt = lblObj.GetComponent<RectTransform>();
        lblRt.anchorMin = new Vector2(0f, 0.5f);
        lblRt.anchorMax = new Vector2(0f, 0.5f);
        lblRt.pivot = new Vector2(0f, 0.5f);
        lblRt.anchoredPosition = new Vector2(10, 0);
        lblRt.sizeDelta = new Vector2(280, 36);

        TextMeshProUGUI lbl = lblObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) lbl.font = fontAsset;
        lbl.text = "Oyun Dili (Language):";
        lbl.fontSize = 17;
        lbl.alignment = TextAlignmentOptions.Left;
        lbl.color = new Color(0.85f, 0.92f, 1f);

        LocalizedText loc = lblObj.AddComponent<LocalizedText>();
        loc.localizationKey = "setting_language";
        loc.fallbackText = "Oyun Dili (Language):";

        // 4.2 Dropdown on Right (Complete with Popup Template)
        GameObject ddObj = CreateFullTMPDropdown("Dropdown", row.transform, fontAsset, btnDarkSprite, cardSprite);
        TMP_Dropdown dropdown = ddObj.GetComponent<TMP_Dropdown>();

        // 5. Attach SettingsLanguageUI controller on row
        SettingsLanguageUI langUI = row.AddComponent<SettingsLanguageUI>();
        langUI.languageDropdown = dropdown;
        langUI.rowLabelText = lbl;
        EditorUtility.SetDirty(langUI);

        // 6. Connect to GameMenuManager
        GameMenuManager menuMgr = Object.FindAnyObjectByType<GameMenuManager>(FindObjectsInactive.Include);
        if (menuMgr != null && dropdown != null)
        {
            Undo.RecordObject(menuMgr, "Assign Language Dropdown to GameMenuManager");
            menuMgr.languageDropdown = dropdown;
            EditorUtility.SetDirty(menuMgr);
            Debug.Log("<color=#32FF64>[SettingsLanguageUIBuilder] Assigned languageDropdown to GameMenuManager!</color>");
        }

        // 7. Select & Ping
        Selection.activeGameObject = row;
        EditorGUIUtility.PingObject(row);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("<color=#32FF64>[SettingsLanguageUIBuilder] Successfully created Language Dropdown in Settings UI!</color>");
        EditorUtility.DisplayDialog(
            "Language Dropdown Added",
            "Dil Seçimi Dropdown'ı ('LanguageRow') başarıyla Settings ekranına eklendi!\n\nSeçenekler: Türkçe (TR) / English (EN)\nKonum: GraphicsSection (En Üst)\nBağlantı: GameMenuManager.languageDropdown",
            "Tamam"
        );
    }

    private static GameObject CreateFullTMPDropdown(string name, Transform parent, TMP_FontAsset font, Sprite bgSprite, Sprite cardSprite)
    {
        // 1. Create TMP_Dropdown using TextMeshPro DefaultControls factory (ensures 100% valid templates and toggles)
        TMP_DefaultControls.Resources res = new TMP_DefaultControls.Resources();
        GameObject ddObj = TMP_DefaultControls.CreateDropdown(res);
        ddObj.name = name;
        ddObj.transform.SetParent(parent, false);

        // 2. Adjust RectTransform for Settings row layout
        RectTransform ddRt = ddObj.GetComponent<RectTransform>();
        ddRt.anchorMin = new Vector2(1f, 0.5f);
        ddRt.anchorMax = new Vector2(1f, 0.5f);
        ddRt.pivot = new Vector2(1f, 0.5f);
        ddRt.anchoredPosition = new Vector2(-10, 0);
        ddRt.sizeDelta = new Vector2(360, 38);

        // 3. Style Dropdown Background Image
        Image ddImg = ddObj.GetComponent<Image>();
        if (ddImg != null)
        {
            if (bgSprite != null) { ddImg.sprite = bgSprite; ddImg.type = Image.Type.Sliced; }
            ddImg.color = new Color(0.10f, 0.14f, 0.22f, 0.95f);
        }

        TMP_Dropdown dropdown = ddObj.GetComponent<TMP_Dropdown>();

        // 4. Style Caption Text
        if (dropdown.captionText != null)
        {
            if (font != null) dropdown.captionText.font = font;
            dropdown.captionText.fontSize = 16;
            dropdown.captionText.alignment = TextAlignmentOptions.Left;
            dropdown.captionText.color = Color.white;
            dropdown.captionText.text = LocalizationManager.IsTurkish ? "Türkçe (TR)" : "English (EN)";

            RectTransform capRt = dropdown.captionText.GetComponent<RectTransform>();
            capRt.offsetMin = new Vector2(14, 0);
            capRt.offsetMax = new Vector2(-36, 0);
        }

        // 5. Style Template & Popup Scroll View
        if (dropdown.template != null)
        {
            RectTransform tempRt = dropdown.template;
            tempRt.anchorMin = new Vector2(0f, 0f);
            tempRt.anchorMax = new Vector2(1f, 0f);
            tempRt.pivot = new Vector2(0.5f, 1f);
            tempRt.anchoredPosition = new Vector2(0, -4);
            tempRt.sizeDelta = new Vector2(0, 84); // Height for 2 items

            Image tempImg = dropdown.template.GetComponent<Image>();
            if (tempImg != null)
            {
                if (cardSprite != null) { tempImg.sprite = cardSprite; tempImg.type = Image.Type.Sliced; }
                tempImg.color = new Color(0.06f, 0.09f, 0.15f, 0.98f);
            }
        }

        // 6. Style Item Label inside Template
        if (dropdown.itemText != null)
        {
            if (font != null) dropdown.itemText.font = font;
            dropdown.itemText.fontSize = 15;
            dropdown.itemText.alignment = TextAlignmentOptions.Left;
            dropdown.itemText.color = new Color(0.9f, 0.95f, 1f);

            RectTransform itemTextRt = dropdown.itemText.GetComponent<RectTransform>();
            itemTextRt.offsetMin = new Vector2(28, 0);
            itemTextRt.offsetMax = new Vector2(-10, 0);
        }

        // 7. Populate Options and Initial Value
        dropdown.ClearOptions();
        dropdown.AddOptions(new List<string> { "Türkçe (TR)", "English (EN)" });
        dropdown.value = LocalizationManager.IsTurkish ? 0 : 1;
        dropdown.RefreshShownValue();

        return ddObj;
    }

    private static GameObject FindSettingsPanelInScene()
    {
        GameObject panel = GameObject.Find("SettingsPanel");
        if (panel != null) return panel;

        GameMenuManager menuMgr = Object.FindAnyObjectByType<GameMenuManager>(FindObjectsInactive.Include);
        if (menuMgr != null && menuMgr.settingsPanel != null)
        {
            return menuMgr.settingsPanel;
        }

        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.hideFlags != HideFlags.None) continue;
            if (!obj.scene.IsValid() || !obj.scene.isLoaded) continue;

            if (obj.name == "SettingsPanel" || obj.name == "SettingsCard")
            {
                return obj.name == "SettingsCard" && obj.transform.parent != null ? obj.transform.parent.gameObject : obj;
            }
        }

        return null;
    }

    private static Transform FindTargetParentSection(Transform settingsPanel)
    {
        Transform gfx = settingsPanel.Find("SettingsCard/ContentArea/GraphicsSection");
        if (gfx != null) return gfx;

        Transform contentArea = settingsPanel.Find("SettingsCard/ContentArea");
        if (contentArea != null)
        {
            Transform innerGfx = contentArea.Find("GraphicsSection");
            if (innerGfx != null) return innerGfx;
            return contentArea;
        }

        foreach (Transform child in settingsPanel.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "GraphicsSection") return child;
        }

        foreach (Transform child in settingsPanel.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "ContentArea") return child;
        }

        return settingsPanel;
    }
}
#endif
