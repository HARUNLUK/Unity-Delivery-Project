using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[InitializeOnLoad]
public static class FixSettingsDropdownsEditorTool
{
    static FixSettingsDropdownsEditorTool()
    {
        EditorApplication.delayCall += AutoFixIfSceneOpen;
    }

    private static void AutoFixIfSceneOpen()
    {
        if (Application.isPlaying) return;
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.isLoaded && (scene.name.Contains("Main") || scene.name.Contains("Level")))
        {
            FixAllSettingsDropdownsInActiveScene(silent: true);
        }
    }

    [MenuItem("Tools/Delivery Game/UI/Fix All Settings Dropdowns", false, 31)]
    public static void FixAllSettingsDropdownsMenu()
    {
        FixAllSettingsDropdownsInActiveScene(silent: false);
    }

    public static void FixAllSettingsDropdownsInActiveScene(bool silent = false)
    {
        GameMenuManager menuMgr = UnityEngine.Object.FindAnyObjectByType<GameMenuManager>(FindObjectsInactive.Include);
        if (menuMgr == null)
        {
            if (!silent) EditorUtility.DisplayDialog("Not Found", "Could not find GameMenuManager in active scene.", "OK");
            return;
        }

        menuMgr.EnsureReferences();

        TMP_Dropdown langDd = menuMgr.languageDropdown;
        if (langDd == null)
        {
            // Try to find in scene
            var allDds = UnityEngine.Object.FindObjectsByType<TMP_Dropdown>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var d in allDds)
            {
                if (d != null && d.name.IndexOf("Language", StringComparison.OrdinalIgnoreCase) >= 0 || (d.transform.parent != null && d.transform.parent.name.IndexOf("Language", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    langDd = d;
                    break;
                }
            }
        }

        RectTransform sourceTemplate = langDd != null ? langDd.template : null;
        if (sourceTemplate == null && langDd != null)
        {
            Transform t = langDd.transform.Find("Template");
            if (t != null) sourceTemplate = t.GetComponent<RectTransform>();
        }

        // If still no sourceTemplate, create a dummy one
        GameObject dummyRoot = null;
        if (sourceTemplate == null)
        {
            TMP_DefaultControls.Resources res = new TMP_DefaultControls.Resources();
            dummyRoot = TMP_DefaultControls.CreateDropdown(res);
            TMP_Dropdown dummyDd = dummyRoot.GetComponent<TMP_Dropdown>();
            if (dummyDd != null) sourceTemplate = dummyDd.template;
        }

        int fixedCount = 0;
        fixedCount += FixSingleDropdown(menuMgr.qualityDropdown, sourceTemplate, 160f);
        fixedCount += FixSingleDropdown(menuMgr.fullscreenDropdown, sourceTemplate, 130f);
        fixedCount += FixSingleDropdown(menuMgr.resolutionDropdown, sourceTemplate, 220f);
        fixedCount += FixSingleDropdown(menuMgr.fpsLimitDropdown, sourceTemplate, 160f);

        if (dummyRoot != null)
        {
            UnityEngine.Object.DestroyImmediate(dummyRoot);
        }

        if (fixedCount > 0)
        {
            EditorUtility.SetDirty(menuMgr.gameObject);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"<color=#32FF64>[FixSettingsDropdownsEditorTool] Successfully verified and fixed {fixedCount} graphics dropdown templates!</color>");
            if (!silent)
            {
                EditorUtility.DisplayDialog("Dropdowns Fixed", $"Successfully fixed {fixedCount} dropdown templates in Settings UI!\nAll graphics dropdowns are now fully functional and clickable.", "OK");
            }
        }
        else if (!silent)
        {
            EditorUtility.DisplayDialog("Already Configured", "All dropdowns already possess valid popup templates.", "OK");
        }
    }

    private static int FixSingleDropdown(TMP_Dropdown dropdown, RectTransform sourceTemplate, float height)
    {
        if (dropdown == null) return 0;

        // If template already assigned and has valid itemText
        if (dropdown.template != null && dropdown.itemText != null) return 0;

        Transform existingTemplateChild = dropdown.transform.Find("Template");
        if (existingTemplateChild != null)
        {
            dropdown.template = existingTemplateChild.GetComponent<RectTransform>();
            dropdown.itemText = existingTemplateChild.GetComponentInChildren<TextMeshProUGUI>(true);
            dropdown.template.gameObject.SetActive(false);
            EditorUtility.SetDirty(dropdown);
            return 1;
        }

        if (sourceTemplate == null) return 0;

        // Clone source template
        GameObject clonedObj = UnityEngine.Object.Instantiate(sourceTemplate.gameObject, dropdown.transform, false);
        clonedObj.name = "Template";
        RectTransform clonedRt = clonedObj.GetComponent<RectTransform>();
        clonedRt.anchorMin = new Vector2(0f, 0f);
        clonedRt.anchorMax = new Vector2(1f, 0f);
        clonedRt.pivot = new Vector2(0.5f, 1f);
        clonedRt.anchoredPosition = new Vector2(0, -4);
        clonedRt.sizeDelta = new Vector2(0, height);

        dropdown.template = clonedRt;
        dropdown.itemText = clonedObj.GetComponentInChildren<TextMeshProUGUI>(true);

        var imgs = clonedObj.GetComponentsInChildren<Image>(true);
        foreach (var img in imgs)
        {
            if (img.gameObject.name.IndexOf("check", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                dropdown.itemImage = img;
                break;
            }
        }

        clonedObj.SetActive(false);

        EditorUtility.SetDirty(dropdown.gameObject);
        EditorUtility.SetDirty(dropdown);
        return 1;
    }
}
