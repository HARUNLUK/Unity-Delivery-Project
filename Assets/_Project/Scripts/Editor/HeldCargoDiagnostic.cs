using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public static class HeldCargoDiagnostic
{
    [MenuItem("Tools/UI/Diagnose and Fix HeldCargoCard Layout")]
    public static void RunDiagnosticAndFix()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[HeldCargoDiagnostic] No Canvas found in scene!");
            return;
        }

        Transform sideCard = null;
        var allT = canvas.GetComponentsInChildren<Transform>(true);
        foreach (var t in allT)
        {
            if (t.name.Equals("HeldCargoSideCard", System.StringComparison.OrdinalIgnoreCase))
            {
                sideCard = t;
                break;
            }
        }

        if (sideCard == null)
        {
            Debug.LogError("[HeldCargoDiagnostic] HeldCargoSideCard not found under Canvas!");
            return;
        }

        Debug.Log($"<color=#FFAA00>=== DIAGNOSING HELDCARGOSIDECARD ({sideCard.name}) ===</color>");

        // 1. Check & Fix HeldCargoSideCard
        RectTransform sideRect = sideCard.GetComponent<RectTransform>();
        Debug.Log($"SideCard Rect: SizeDelta={sideRect.sizeDelta}, Anchors=({sideRect.anchorMin}, {sideRect.anchorMax})");

        VerticalLayoutGroup sideVGroup = sideCard.GetComponent<VerticalLayoutGroup>();
        if (sideVGroup != null)
        {
            sideVGroup.childControlWidth = true;
            sideVGroup.childControlHeight = true;
            sideVGroup.childForceExpandWidth = true;
            sideVGroup.childForceExpandHeight = false;
            Debug.Log($"SideCard VGroup updated: childControlHeight=true, childForceExpandHeight=false");
        }

        // 2. Inspect all direct children
        for (int i = 0; i < sideCard.childCount; i++)
        {
            Transform child = sideCard.GetChild(i);
            var le = child.GetComponent<LayoutElement>();
            var csf = child.GetComponent<ContentSizeFitter>();
            var vlg = child.GetComponent<VerticalLayoutGroup>();
            var hlg = child.GetComponent<HorizontalLayoutGroup>();
            var tmp = child.GetComponent<TextMeshProUGUI>();

            Debug.Log($"-> Child [{i}] '{child.name}': LE={(le != null ? $"prefH={le.preferredHeight}, flexH={le.flexibleHeight}" : "null")}, CSF={(csf != null ? $"{csf.verticalFit}" : "null")}, VLG={(vlg != null ? "yes" : "null")}, HLG={(hlg != null ? "yes" : "null")}, TMP={(tmp != null ? $"'{tmp.text}'" : "null")}");

            child.localScale = Vector3.one;

            // If this is ClueBox or contains Clue
            if (child.name.IndexOf("Clue", System.StringComparison.OrdinalIgnoreCase) >= 0 || child.name.IndexOf("Desc", System.StringComparison.OrdinalIgnoreCase) >= 0 || child.name.IndexOf("Box", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Debug.Log($"<color=#32FF64>Found Clue Container: {child.name}</color>");
                child.localScale = Vector3.one;
                
                // Ensure VerticalLayoutGroup
                if (vlg == null) vlg = child.gameObject.AddComponent<VerticalLayoutGroup>();
                vlg.padding = new RectOffset(16, 16, 12, 12);
                vlg.spacing = 6;
                vlg.childControlWidth = true;
                vlg.childControlHeight = true;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;

                // Ensure ContentSizeFitter
                if (csf == null) csf = child.gameObject.AddComponent<ContentSizeFitter>();
                csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                // Configure LayoutElement on ClueBox
                if (le == null) le = child.gameObject.AddComponent<LayoutElement>();
                le.flexibleWidth = 1;
                le.flexibleHeight = 0;
                le.minHeight = 0;
                le.preferredHeight = -1; // Auto from ContentSizeFitter!

                // Configure all children TMP inside ClueBox
                var subTMPs = child.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var st in subTMPs)
                {
                    st.transform.localScale = Vector3.one;
                    st.enableWordWrapping = true;
                    st.overflowMode = TextOverflowModes.Overflow;
                    var subLe = st.GetComponent<LayoutElement>();
                    if (subLe != null)
                    {
                        subLe.preferredHeight = -1; // Unconstrain!
                    }
                    Debug.Log($"   --> Configured ClueBox Sub-Text: '{st.gameObject.name}' | Wrapping=True, Overflow=Overflow");
                }
            }
        }

        EditorUtility.SetDirty(sideCard.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log("<color=#32FF64>=== DIAGNOSTIC & FIX COMPLETE! Check Scene & Game View ===</color>");
    }
}
