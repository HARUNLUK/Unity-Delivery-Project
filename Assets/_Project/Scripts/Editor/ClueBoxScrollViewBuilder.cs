using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class ClueBoxScrollViewBuilder
{
    [MenuItem("Tools/UI/Setup ClueBox ScrollView (Mouse Scrollable)")]
    public static void SetupClueScrollView()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[ClueBoxScrollViewBuilder] Canvas not found!");
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
            Debug.LogError("[ClueBoxScrollViewBuilder] HeldCargoSideCard not found!");
            return;
        }

        // Find existing ClueBox
        Transform clueBox = null;
        foreach (Transform child in sideCard)
        {
            if (child.name.IndexOf("Clue", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                child.name.IndexOf("Box", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                clueBox = child;
                break;
            }
        }

        if (clueBox == null)
        {
            Debug.LogError("[ClueBoxScrollViewBuilder] ClueBox container not found under HeldCargoSideCard!");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(sideCard.gameObject, "Setup ClueBox ScrollView");

        // 1. Configure ClueBox container
        clueBox.localScale = Vector3.one;
        
        // Remove ContentSizeFitter from outer ClueBox so its max height is bounded
        ContentSizeFitter outerCsf = clueBox.GetComponent<ContentSizeFitter>();
        if (outerCsf != null) Object.DestroyImmediate(outerCsf);

        LayoutElement clueBoxLe = clueBox.GetComponent<LayoutElement>();
        if (clueBoxLe == null) clueBoxLe = clueBox.gameObject.AddComponent<LayoutElement>();
        clueBoxLe.flexibleWidth = 1;
        clueBoxLe.flexibleHeight = 0;
        clueBoxLe.preferredHeight = 150f; // Fixed clean max height
        clueBoxLe.minHeight = 120f;

        VerticalLayoutGroup clueVlg = clueBox.GetComponent<VerticalLayoutGroup>();
        if (clueVlg == null) clueVlg = clueBox.gameObject.AddComponent<VerticalLayoutGroup>();
        clueVlg.padding = new RectOffset(16, 16, 12, 12);
        clueVlg.spacing = 6;
        clueVlg.childControlWidth = true;
        clueVlg.childControlHeight = true;
        clueVlg.childForceExpandWidth = true;
        clueVlg.childForceExpandHeight = false;

        // 2. Identify or Create Title Text
        Transform titleTrans = null;
        Transform descTrans = null;
        var allTmps = clueBox.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var tmp in allTmps)
        {
            if (tmp.gameObject.name.IndexOf("Title", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                tmp.gameObject.name.IndexOf("Header", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                titleTrans = tmp.transform;
            }
            else
            {
                descTrans = tmp.transform;
            }
        }

        if (titleTrans != null)
        {
            titleTrans.SetParent(clueBox, false);
            titleTrans.SetSiblingIndex(0);
            titleTrans.localScale = Vector3.one;
            LayoutElement titleLe = titleTrans.GetComponent<LayoutElement>();
            if (titleLe == null) titleLe = titleTrans.gameObject.AddComponent<LayoutElement>();
            titleLe.preferredHeight = 24f;
            titleLe.flexibleHeight = 0;
            titleLe.flexibleWidth = 1;
        }

        // 3. Create or Configure ScrollView
        Transform scrollObj = clueBox.Find("ClueScrollView");
        if (scrollObj == null)
        {
            GameObject sGo = new GameObject("ClueScrollView");
            sGo.transform.SetParent(clueBox, false);
            scrollObj = sGo.transform;
        }
        scrollObj.localScale = Vector3.one;

        LayoutElement scrollLe = scrollObj.GetComponent<LayoutElement>();
        if (scrollLe == null) scrollLe = scrollObj.gameObject.AddComponent<LayoutElement>();
        scrollLe.flexibleWidth = 1;
        scrollLe.flexibleHeight = 1;

        // Invisible Image with raycastTarget = true so EventSystem receives mouse scroll events
        Image scrollRaycastImg = scrollObj.GetComponent<Image>();
        if (scrollRaycastImg == null) scrollRaycastImg = scrollObj.gameObject.AddComponent<Image>();
        scrollRaycastImg.color = new Color(0, 0, 0, 0);
        scrollRaycastImg.raycastTarget = true;

        ScrollRect scrollRect = scrollObj.GetComponent<ScrollRect>();
        if (scrollRect == null) scrollRect = scrollObj.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 35f;
        scrollRect.inertia = true;

        // Add Direct Mouse Wheel Listener
        MouseWheelScrollHelper wheelHelper = scrollObj.GetComponent<MouseWheelScrollHelper>();
        if (wheelHelper == null) wheelHelper = scrollObj.gameObject.AddComponent<MouseWheelScrollHelper>();
        wheelHelper.scrollSpeed = 0.25f;

        RectMask2D mask = scrollObj.GetComponent<RectMask2D>();
        if (mask == null) mask = scrollObj.gameObject.AddComponent<RectMask2D>();

        // 4. Create / Configure Content Container
        Transform contentTrans = scrollObj.Find("Content");
        if (contentTrans == null)
        {
            GameObject cGo = new GameObject("Content");
            cGo.transform.SetParent(scrollObj, false);
            contentTrans = cGo.transform;
        }
        contentTrans.localScale = Vector3.one;

        RectTransform cRect = contentTrans.GetComponent<RectTransform>();
        if (cRect == null) cRect = contentTrans.gameObject.AddComponent<RectTransform>();
        cRect.anchorMin = new Vector2(0, 1);
        cRect.anchorMax = new Vector2(1, 1);
        cRect.pivot = new Vector2(0.5f, 1f);
        cRect.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup cVlg = contentTrans.GetComponent<VerticalLayoutGroup>();
        if (cVlg == null) cVlg = contentTrans.gameObject.AddComponent<VerticalLayoutGroup>();
        cVlg.padding = new RectOffset(4, 6, 10, 12);
        cVlg.spacing = 0;
        cVlg.childControlWidth = true;
        cVlg.childControlHeight = true;
        cVlg.childForceExpandWidth = true;
        cVlg.childForceExpandHeight = false;

        ContentSizeFitter cCsf = contentTrans.GetComponent<ContentSizeFitter>();
        if (cCsf == null) cCsf = contentTrans.gameObject.AddComponent<ContentSizeFitter>();
        cCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        cCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = cRect;
        scrollRect.viewport = scrollObj.GetComponent<RectTransform>();

        // 5. Move AddressDescription into Content
        if (descTrans != null)
        {
            descTrans.SetParent(contentTrans, false);
            descTrans.localScale = Vector3.one;
            TextMeshProUGUI descTmp = descTrans.GetComponent<TextMeshProUGUI>();
            if (descTmp != null)
            {
                descTmp.enableAutoSizing = false; // Never auto-shrink user font size!
                descTmp.enableWordWrapping = true;
                descTmp.overflowMode = TextOverflowModes.Overflow;
                descTmp.lineSpacing = 1.2f;
                descTmp.margin = new Vector4(2, 4, 2, 4);
            }
            LayoutElement descLe = descTrans.GetComponent<LayoutElement>();
            if (descLe == null) descLe = descTrans.gameObject.AddComponent<LayoutElement>();
            descLe.flexibleWidth = 1;
            descLe.flexibleHeight = 0;
            descLe.preferredHeight = -1;
        }

        EditorUtility.SetDirty(sideCard.gameObject);
        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.isLoaded && !Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(activeScene);
        }

        Debug.Log("<color=#00F5FF>[ClueBoxScrollViewBuilder] Scrollable ClueBox successfully created! Mouse-wheel scrolling active.</color>");
    }
}
