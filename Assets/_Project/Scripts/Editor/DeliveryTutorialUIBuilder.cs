#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Dedicated Editor Tool and UI Builder for the Delivery Tutorial Guide UI.
/// Creates and configures the tutorial panel in the scene with 1 click under 'Tools > Delivery Game > UI'.
/// </summary>
public class DeliveryTutorialUIBuilder : EditorWindow
{
    private static TMP_FontAsset cachedFont;

    [MenuItem("Tools/Delivery Game/UI/Build or Rebuild Delivery Tutorial Panel", false, 36)]
    [MenuItem("GameObject/Delivery Game/UI/Delivery Tutorial Panel", false, 25)]
    public static void BuildDeliveryTutorialPanelMenuItem()
    {
        BuildTutorialPanelInActiveScene();
    }

    [MenuItem("Tools/Delivery Game/Testing/Open Tutorial Panel (Preview)", false, 117)]
    public static void PreviewTutorialMenuItem()
    {
        DeliveryTutorialUI tutorialUI = Object.FindAnyObjectByType<DeliveryTutorialUI>(FindObjectsInactive.Include);
        if (tutorialUI == null)
        {
            BuildTutorialPanelInActiveScene();
            tutorialUI = Object.FindAnyObjectByType<DeliveryTutorialUI>(FindObjectsInactive.Include);
        }

        if (tutorialUI != null)
        {
            tutorialUI.ShowTutorial();
            Selection.activeGameObject = tutorialUI.gameObject;
            Debug.Log("<color=#32FF64>[DeliveryTutorialUIBuilder] Tutorial Panel opened for preview!</color>");
        }
    }

    [MenuItem("Tools/Delivery Game/Testing/Reset Tutorial Seen State (Allow Auto-Popup)", false, 118)]
    public static void ResetTutorialSeenStateMenuItem()
    {
        PlayerPrefs.DeleteKey(DeliveryTutorialUI.PREF_TUTORIAL_DONT_SHOW);
        PlayerPrefs.Save();
        EditorUtility.DisplayDialog("Tutorial Reset", "Tutorial 'Bir daha gösterme' tercihi sıfırlandı. 1. Günde oyun başladığında rehber tekrar otomatik açılacaktır.", "Tamam");
        Debug.Log("<color=#32FF64>[DeliveryTutorialUIBuilder] Tutorial Don't Show Again flag reset!</color>");
    }

    public static DeliveryTutorialUI BuildTutorialPanelInActiveScene()
    {
        EnsureTMPFont();

        // 1. Find or create main UI Canvas
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Delivery_Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
        }

        // 2. Find or create DeliveryTutorialUI component
        DeliveryTutorialUI tutorialMgr = Object.FindAnyObjectByType<DeliveryTutorialUI>(FindObjectsInactive.Include);
        if (tutorialMgr == null)
        {
            GameObject mgrObj = new GameObject("[DELIVERY_TUTORIAL_UI]");
            tutorialMgr = mgrObj.AddComponent<DeliveryTutorialUI>();
            Undo.RegisterCreatedObjectUndo(mgrObj, "Create DeliveryTutorialUI");
        }

        // If panel root already exists in hierarchy, clean it up for a fresh build
        Transform existingPanel = canvas.transform.Find("DeliveryTutorial_Modal_Root");
        if (existingPanel != null)
        {
            Undo.DestroyObjectImmediate(existingPanel.gameObject);
        }

        // 3. Root Modal Overlay
        GameObject root = new GameObject("DeliveryTutorial_Modal_Root");
        root.transform.SetParent(canvas.transform, false);
        RectTransform rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.sizeDelta = Vector2.zero;
        rootRect.anchoredPosition = Vector2.zero;

        Image backdrop = root.AddComponent<Image>();
        backdrop.color = new Color(0.02f, 0.03f, 0.05f, 0.90f);

        CanvasGroup panelCg = root.AddComponent<CanvasGroup>();
        panelCg.alpha = 1f;

        // 4. Center Modal Container Window (Width: 1060, Height: 640)
        GameObject card = new GameObject("Tutorial_Card_Window");
        card.transform.SetParent(root.transform, false);
        RectTransform cardRect = card.AddComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(1060, 640);
        cardRect.anchoredPosition = Vector2.zero;

        Image cardBg = card.AddComponent<Image>();
        cardBg.color = new Color(0.09f, 0.11f, 0.14f, 0.98f);
        Outline outline = card.AddComponent<Outline>();
        outline.effectColor = new Color(0.18f, 0.65f, 1.0f, 0.50f);
        outline.effectDistance = new Vector2(2, -2);

        // 5. Top Header Bar (Height: 70)
        GameObject headerBar = new GameObject("Header_Bar");
        headerBar.transform.SetParent(card.transform, false);
        RectTransform headerRect = headerBar.AddComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 1);
        headerRect.anchorMax = new Vector2(1, 1);
        headerRect.pivot = new Vector2(0.5f, 1);
        headerRect.sizeDelta = new Vector2(0, 70);
        headerRect.anchoredPosition = Vector2.zero;

        Image headerBg = headerBar.AddComponent<Image>();
        headerBg.color = new Color(0.12f, 0.16f, 0.22f, 0.95f);

        // Header Title Text
        GameObject titleObj = new GameObject("Header_Title_Text");
        titleObj.transform.SetParent(headerBar.transform, false);
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        if (cachedFont != null) titleText.font = cachedFont;
        titleText.text = "<b>LOJİSTİK VE KARGO TESLİMAT REHBERİ</b>";
        titleText.fontSize = 24;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.MidlineLeft;
        titleText.color = new Color(1f, 0.92f, 0.45f);
        RectTransform tRect = titleObj.GetComponent<RectTransform>();
        tRect.anchorMin = new Vector2(0, 0);
        tRect.anchorMax = new Vector2(1, 1);
        tRect.offsetMin = new Vector2(30, 0);
        tRect.offsetMax = new Vector2(-70, 0);

        // Close [X] Button in Header
        GameObject closeBtnObj = new GameObject("Close_X_Button");
        closeBtnObj.transform.SetParent(headerBar.transform, false);
        RectTransform closeRect = closeBtnObj.AddComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1, 0.5f);
        closeRect.anchorMax = new Vector2(1, 0.5f);
        closeRect.pivot = new Vector2(1, 0.5f);
        closeRect.sizeDelta = new Vector2(40, 40);
        closeRect.anchoredPosition = new Vector2(-20, 0);

        Image closeBtnBg = closeBtnObj.AddComponent<Image>();
        closeBtnBg.color = new Color(0.25f, 0.28f, 0.35f, 0.8f);
        Button closeButton = closeBtnObj.AddComponent<Button>();

        GameObject closeTxtObj = new GameObject("X_Text");
        closeTxtObj.transform.SetParent(closeBtnObj.transform, false);
        TextMeshProUGUI closeTxt = closeTxtObj.AddComponent<TextMeshProUGUI>();
        if (cachedFont != null) closeTxt.font = cachedFont;
        closeTxt.text = "✕";
        closeTxt.fontSize = 20;
        closeTxt.alignment = TextAlignmentOptions.Center;
        closeTxt.color = Color.white;
        RectTransform ctRect = closeTxtObj.GetComponent<RectTransform>();
        ctRect.anchorMin = Vector2.zero;
        ctRect.anchorMax = Vector2.one;
        ctRect.sizeDelta = Vector2.zero;

        // 6. Split Content Area (Y: -70 to bottom -80)
        GameObject contentArea = new GameObject("Split_Content_Area");
        contentArea.transform.SetParent(card.transform, false);
        RectTransform contentRect = contentArea.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 0);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.offsetMin = new Vector2(30, 80);
        contentRect.offsetMax = new Vector2(-30, -85);

        // --- LEFT COLUMN: IMAGE CONTAINER (Width: 420) ---
        GameObject leftCol = new GameObject("Left_Image_Column");
        leftCol.transform.SetParent(contentArea.transform, false);
        RectTransform leftRect = leftCol.AddComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0, 0);
        leftRect.anchorMax = new Vector2(0, 1);
        leftRect.pivot = new Vector2(0, 0.5f);
        leftRect.sizeDelta = new Vector2(420, 0);
        leftRect.anchoredPosition = Vector2.zero;

        // Image Frame Box
        GameObject imgFrame = new GameObject("Image_Frame_Box");
        imgFrame.transform.SetParent(leftCol.transform, false);
        RectTransform frameRect = imgFrame.AddComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(0, 0.28f);
        frameRect.anchorMax = new Vector2(1, 1);
        frameRect.offsetMin = Vector2.zero;
        frameRect.offsetMax = Vector2.zero;

        Image frameBg = imgFrame.AddComponent<Image>();
        frameBg.color = new Color(0.06f, 0.08f, 0.10f, 1f);
        Outline frameOutline = imgFrame.AddComponent<Outline>();
        frameOutline.effectColor = new Color(0.20f, 0.65f, 1.0f, 0.40f);
        frameOutline.effectDistance = new Vector2(1.5f, -1.5f);
        imgFrame.AddComponent<RectMask2D>();

        // Image Object (100% Full Fill)
        GameObject imgObj = new GameObject("Delivery_Point_Image");
        imgObj.transform.SetParent(imgFrame.transform, false);
        RectTransform imgRect = imgObj.AddComponent<RectTransform>();
        imgRect.anchorMin = Vector2.zero;
        imgRect.anchorMax = Vector2.one;
        imgRect.offsetMin = Vector2.zero;
        imgRect.offsetMax = Vector2.zero;

        Image deliveryPointImage = imgObj.AddComponent<Image>();
        deliveryPointImage.preserveAspect = tutorialMgr.preserveImageAspect;
        deliveryPointImage.color = tutorialMgr.tutorialImage != null ? Color.white : new Color(0.18f, 0.24f, 0.32f, 0.9f);
        if (tutorialMgr.tutorialImage != null)
        {
            deliveryPointImage.sprite = tutorialMgr.tutorialImage;
        }

        // Left Sub-Caption Box
        GameObject captionObj = new GameObject("Left_Caption_Text");
        captionObj.transform.SetParent(leftCol.transform, false);
        TextMeshProUGUI leftCaption = captionObj.AddComponent<TextMeshProUGUI>();
        if (cachedFont != null) leftCaption.font = cachedFont;
        leftCaption.text = "<b>Teslimat Noktası (Posta Kutusu)</b>\n<size=15><color=#A0C8FF>Kargoları binaların önündeki bu sarı/yeşil teslimat alanına veya posta kutusunun yanına bırakın.</color></size>";
        leftCaption.fontSize = 17;
        leftCaption.alignment = TextAlignmentOptions.TopLeft;
        leftCaption.color = Color.white;
        RectTransform capRect = captionObj.GetComponent<RectTransform>();
        capRect.anchorMin = new Vector2(0, 0);
        capRect.anchorMax = new Vector2(1, 0.25f);
        capRect.offsetMin = new Vector2(4, 0);
        capRect.offsetMax = new Vector2(-4, 0);

        // --- RIGHT COLUMN: OBJECTIVES (Width: remaining 540) ---
        GameObject rightCol = new GameObject("Right_Text_Column");
        rightCol.transform.SetParent(contentArea.transform, false);
        RectTransform rightRect = rightCol.AddComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(0, 0);
        rightRect.anchorMax = new Vector2(1, 1);
        rightRect.offsetMin = new Vector2(450, 0);
        rightRect.offsetMax = Vector2.zero;

        GameObject bodyObj = new GameObject("Objective_Body_Text");
        bodyObj.transform.SetParent(rightCol.transform, false);
        TextMeshProUGUI bodyText = bodyObj.AddComponent<TextMeshProUGUI>();
        if (cachedFont != null) bodyText.font = cachedFont;
        bodyText.fontSize = 16.5f;
        bodyText.lineSpacing = 12f;
        bodyText.alignment = TextAlignmentOptions.TopLeft;
        bodyText.color = new Color(0.92f, 0.94f, 0.96f);
        RectTransform bRect = bodyObj.GetComponent<RectTransform>();
        bRect.anchorMin = Vector2.zero;
        bRect.anchorMax = Vector2.one;
        bRect.sizeDelta = Vector2.zero;

        // 7. Bottom Action Bar (Height: 75)
        GameObject bottomBar = new GameObject("Bottom_Action_Bar");
        bottomBar.transform.SetParent(card.transform, false);
        RectTransform bottomRect = bottomBar.AddComponent<RectTransform>();
        bottomRect.anchorMin = new Vector2(0, 0);
        bottomRect.anchorMax = new Vector2(1, 0);
        bottomRect.pivot = new Vector2(0.5f, 0);
        bottomRect.sizeDelta = new Vector2(0, 75);
        bottomRect.anchoredPosition = Vector2.zero;

        Image bottomBg = bottomBar.AddComponent<Image>();
        bottomBg.color = new Color(0.07f, 0.09f, 0.12f, 0.95f);

        // "F1 ile Rehberi Aç / Kapat" Help Hint on Left
        GameObject f1HintObj = new GameObject("F1_Hint_Text");
        f1HintObj.transform.SetParent(bottomBar.transform, false);
        TextMeshProUGUI f1Text = f1HintObj.AddComponent<TextMeshProUGUI>();
        if (cachedFont != null) f1Text.font = cachedFont;
        f1Text.text = "<color=#64A0FF>[F1]</color> <color=#A0B0C0>Tuşu ile rehberi istediğiniz zaman tekrar açabilirsiniz.</color>";
        f1Text.fontSize = 14.5f;
        f1Text.alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform f1Rect = f1HintObj.GetComponent<RectTransform>();
        f1Rect.anchorMin = new Vector2(0, 0);
        f1Rect.anchorMax = new Vector2(0.6f, 1);
        f1Rect.offsetMin = new Vector2(30, 0);
        f1Rect.offsetMax = Vector2.zero;

        // "ANLADIM, VARDİYAYA BAŞLA" Button on Right
        GameObject startBtnObj = new GameObject("Start_Game_Button");
        startBtnObj.transform.SetParent(bottomBar.transform, false);
        RectTransform sbRect = startBtnObj.AddComponent<RectTransform>();
        sbRect.anchorMin = new Vector2(1, 0.5f);
        sbRect.anchorMax = new Vector2(1, 0.5f);
        sbRect.pivot = new Vector2(1, 0.5f);
        sbRect.sizeDelta = new Vector2(280, 50);
        sbRect.anchoredPosition = new Vector2(-30, 0);

        Image sbBg = startBtnObj.AddComponent<Image>();
        sbBg.color = new Color(0.12f, 0.65f, 0.35f, 1.0f); // Emerald Start Button

        Button startBtn = startBtnObj.AddComponent<Button>();

        GameObject sbTxtObj = new GameObject("Button_Text");
        sbTxtObj.transform.SetParent(startBtnObj.transform, false);
        TextMeshProUGUI sbTxt = sbTxtObj.AddComponent<TextMeshProUGUI>();
        if (cachedFont != null) sbTxt.font = cachedFont;
        sbTxt.text = "<b>ANLADIM, VARDİYAYA BAŞLA</b>";
        sbTxt.fontSize = 17;
        sbTxt.fontStyle = FontStyles.Bold;
        sbTxt.alignment = TextAlignmentOptions.Center;
        sbTxt.color = Color.white;
        RectTransform sbtRect = sbTxtObj.GetComponent<RectTransform>();
        sbtRect.anchorMin = Vector2.zero;
        sbtRect.anchorMax = Vector2.one;
        sbtRect.sizeDelta = Vector2.zero;

        // 8. Bind Inspector References to DeliveryTutorialUI
        tutorialMgr.tutorialPanelRoot = root;
        tutorialMgr.panelCanvasGroup = panelCg;
        tutorialMgr.deliveryPointImageUI = deliveryPointImage;
        tutorialMgr.titleText = titleText;
        tutorialMgr.leftCaptionText = leftCaption;
        tutorialMgr.objectiveBodyText = bodyText;
        tutorialMgr.startButton = startBtn;
        tutorialMgr.closeButton = closeButton;

        startBtn.onClick.RemoveAllListeners();
        startBtn.onClick.AddListener(tutorialMgr.HideTutorial);

        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(tutorialMgr.HideTutorial);

        tutorialMgr.UpdateContent();
        root.SetActive(false); // Inactive by default in editor

        EditorUtility.SetDirty(tutorialMgr);
        EditorUtility.SetDirty(root);
        Undo.RegisterCreatedObjectUndo(root, "Build Delivery Tutorial Panel");

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);

        Debug.Log("<color=#32FF64>[DeliveryTutorialUIBuilder] Delivery Tutorial Panel successfully built and configured in scene!</color>");
        return tutorialMgr;
    }

    private static void EnsureTMPFont()
    {
        if (cachedFont == null)
        {
            cachedFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (cachedFont == null)
            {
                TMP_FontAsset[] allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                if (allFonts != null && allFonts.Length > 0) cachedFont = allFonts[0];
            }
        }
    }
}
#endif
