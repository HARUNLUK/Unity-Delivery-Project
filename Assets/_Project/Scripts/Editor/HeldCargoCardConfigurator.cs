using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class HeldCargoCardConfigurator
{
    [MenuItem("Tools/UI/Configure ClueBox Auto-Height")]
    public static void AutoConfigureClueBox()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        Transform sideCard = canvas.transform.Find("HeldCargoSideCard");
        if (sideCard == null)
        {
            var allT = canvas.GetComponentsInChildren<Transform>(true);
            foreach (var t in allT)
            {
                if (t.name.Equals("HeldCargoSideCard", System.StringComparison.OrdinalIgnoreCase))
                {
                    sideCard = t;
                    break;
                }
            }
        }

        if (sideCard == null) return;

        // 1. Configure HeldCargoSideCard VerticalLayoutGroup
        VerticalLayoutGroup sideVGroup = sideCard.GetComponent<VerticalLayoutGroup>();
        if (sideVGroup != null)
        {
            sideVGroup.childControlWidth = true;
            sideVGroup.childControlHeight = true;
            sideVGroup.childForceExpandWidth = true;
            sideVGroup.childForceExpandHeight = false;
            EditorUtility.SetDirty(sideVGroup);
        }

        // 2. Find ClueBox
        Transform clueBox = sideCard.Find("ClueBox");
        if (clueBox == null)
        {
            var allChildren = sideCard.GetComponentsInChildren<Transform>(true);
            foreach (var c in allChildren)
            {
                if (c.name.IndexOf("Clue", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    clueBox = c;
            }
        }

        if (clueBox != null)
        {
            // VerticalLayoutGroup on ClueBox
            VerticalLayoutGroup cbLayout = clueBox.GetComponent<VerticalLayoutGroup>();
            if (cbLayout == null) cbLayout = clueBox.gameObject.AddComponent<VerticalLayoutGroup>();
            cbLayout.padding = new RectOffset(16, 16, 12, 12);
            cbLayout.spacing = 6;
            cbLayout.childControlWidth = true;
            cbLayout.childControlHeight = true;
            cbLayout.childForceExpandWidth = true;
            cbLayout.childForceExpandHeight = false;
            EditorUtility.SetDirty(cbLayout);

            // ContentSizeFitter on ClueBox
            ContentSizeFitter csf = clueBox.GetComponent<ContentSizeFitter>();
            if (csf == null) csf = clueBox.gameObject.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            EditorUtility.SetDirty(csf);

            // LayoutElement on ClueBox
            LayoutElement le = clueBox.GetComponent<LayoutElement>();
            if (le == null) le = clueBox.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.flexibleHeight = 0;
            le.minHeight = 0;
            le.preferredHeight = -1;
            EditorUtility.SetDirty(le);

            // AddressDescription inside ClueBox
            var tmps = clueBox.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var tmp in tmps)
            {
                if (tmp.gameObject.name.IndexOf("Title", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    tmp.enableWordWrapping = true;
                }
                else
                {
                    // Address description
                    tmp.enableWordWrapping = true;
                    tmp.overflowMode = TextOverflowModes.Overflow;
                    tmp.lineSpacing = 1.2f;
                }
                EditorUtility.SetDirty(tmp);
            }

            EditorUtility.SetDirty(clueBox.gameObject);
        }

        EditorUtility.SetDirty(sideCard.gameObject);
        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.isLoaded && !Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(activeScene);
        }
        Debug.Log("<color=#00F5FF>[HeldCargoCardConfigurator] ClueBox & AddressDescription auto-height settings successfully configured!</color>");
    }
}
