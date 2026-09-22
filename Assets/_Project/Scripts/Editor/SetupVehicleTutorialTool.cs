using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public static class SetupVehicleTutorialTool
{
    [MenuItem("Tools/UI/Setup Vehicle Tutorial UI in Scene")]
    public static void SetupVehicleTutorialUI()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Hata", "Sahnede aktif bir Canvas bulunamadı!", "Tamam");
            return;
        }

        // 1. Check if VehicleTutorial_Modal_Root already exists
        Transform existingVehicleRoot = canvas.transform.Find("VehicleTutorial_Modal_Root");
        GameObject vehicleModalRoot = null;

        Transform deliveryTutorialRoot = canvas.transform.Find("DeliveryTutorial_Modal_Root");

        if (existingVehicleRoot != null)
        {
            vehicleModalRoot = existingVehicleRoot.gameObject;
            Undo.RecordObject(vehicleModalRoot, "Update Existing Vehicle Tutorial Root");
        }
        else if (deliveryTutorialRoot != null)
        {
            // Duplicate DeliveryTutorial_Modal_Root to guarantee 100% identical styling, fonts, and materials
            GameObject cloned = Object.Instantiate(deliveryTutorialRoot.gameObject, canvas.transform);
            cloned.name = "VehicleTutorial_Modal_Root";
            Undo.RegisterCreatedObjectUndo(cloned, "Create Vehicle Tutorial from Template");
            vehicleModalRoot = cloned;
        }
        else
        {
            // Create procedural modal matching exact schema
            vehicleModalRoot = CreateProceduralVehicleModal(canvas.gameObject);
            Undo.RegisterCreatedObjectUndo(vehicleModalRoot, "Create Procedural Vehicle Tutorial");
        }

        vehicleModalRoot.SetActive(false);

        // 2. Add or find VehicleTutorialUI component
        VehicleTutorialUI tutorialComponent = Object.FindAnyObjectByType<VehicleTutorialUI>();
        if (tutorialComponent == null)
        {
            GameObject holder = GameObject.Find("[VEHICLE_TUTORIAL_UI]");
            if (holder == null)
            {
                holder = new GameObject("[VEHICLE_TUTORIAL_UI]");
                Undo.RegisterCreatedObjectUndo(holder, "Create [VEHICLE_TUTORIAL_UI]");
            }
            tutorialComponent = holder.AddComponent<VehicleTutorialUI>();
        }

        Undo.RecordObject(tutorialComponent, "Wire Vehicle Tutorial References");

        // 3. Wire inspector references matching the exact cleaned schema
        tutorialComponent.tutorialPanelRoot = vehicleModalRoot;
        tutorialComponent.panelCanvasGroup = vehicleModalRoot.GetComponent<CanvasGroup>();
        if (tutorialComponent.panelCanvasGroup == null)
        {
            tutorialComponent.panelCanvasGroup = vehicleModalRoot.AddComponent<CanvasGroup>();
        }

        // Find elements
        tutorialComponent.titleText = FindTMPRecursive(vehicleModalRoot.transform, "Header_Title_Text", "TitleText", "HeaderTitle", "Title");
        tutorialComponent.leftCaptionText = FindTMPRecursive(vehicleModalRoot.transform, "Left_Caption_Text", "CaptionText", "LeftCaption");
        tutorialComponent.objectiveBodyText = FindTMPRecursive(vehicleModalRoot.transform, "Objective_Body_Text", "BodyText", "ObjectiveText");
        tutorialComponent.vehicleImageUI = FindImageRecursive(vehicleModalRoot.transform, "Delivery_Point_Image", "VehicleImage", "TutorialImage", "Image");
        tutorialComponent.startButton = FindButtonRecursive(vehicleModalRoot.transform, "Start_Game_Button", "StartButton", "OkButton", "ConfirmButton");

        // Remove any legacy close button or toggle from Vehicle modal if present
        Transform closeX = vehicleModalRoot.transform.Find("Tutorial_Card_Window/Header_Bar/Close_X_Button");
        if (closeX != null) Object.DestroyImmediate(closeX.gameObject);

        Transform dontShow = vehicleModalRoot.transform.Find("Tutorial_Card_Window/Bottom_Action_Bar/Dont_Show_Toggle");
        if (dontShow != null) Object.DestroyImmediate(dontShow.gameObject);

        // 4. Update texts to vehicle theme
        tutorialComponent.UpdateContent();

        // 5. Mark scene dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);

        EditorUtility.DisplayDialog("Araç Tutorial Paneli Kuruldu", 
            "Araç Sürüş ve Taşıma Rehberi (VehicleTutorialUI) başarıyla sahneye eklendi ve tüm bağlantıları bağlandı!\n\n" +
            "Oyuncu araca İLK KEZ bindiğinde bu panel açılacak. 'ANLADIM' butonuna bastığında bir daha rahatsız etmeyecek.", 
            "Harika");
    }

    [MenuItem("Tools/UI/Reset Vehicle Tutorial Seen (PlayerPrefs)")]
    public static void ResetVehicleTutorialSeen()
    {
        PlayerPrefs.DeleteKey(VehicleTutorialUI.PREF_VEHICLE_TUTORIAL_SEEN);
        PlayerPrefs.Save();
        Debug.Log("[SetupVehicleTutorialTool] Vehicle tutorial seen flag reset. It will appear on next vehicle entry!");
        EditorUtility.DisplayDialog("Sıfırlandı", "Araç tutorial gösterilme kaydı sıfırlandı. Araca ilk binişte tekrar açılacak.", "Tamam");
    }

    private static GameObject CreateProceduralVehicleModal(GameObject canvasObj)
    {
        GameObject root = new GameObject("VehicleTutorial_Modal_Root");
        root.transform.SetParent(canvasObj.transform, false);
        RectTransform rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.sizeDelta = Vector2.zero;

        Image backdrop = root.AddComponent<Image>();
        backdrop.color = new Color(0.02f, 0.03f, 0.05f, 0.90f);

        CanvasGroup cg = root.AddComponent<CanvasGroup>();
        cg.alpha = 1f;

        // Card Window
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

        // Header Bar
        GameObject headerBar = new GameObject("Header_Bar");
        headerBar.transform.SetParent(card.transform, false);
        RectTransform headerRect = headerBar.AddComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 1);
        headerRect.anchorMax = new Vector2(1, 1);
        headerRect.pivot = new Vector2(0.5f, 1);
        headerRect.sizeDelta = new Vector2(0, 70);

        Image headerBg = headerBar.AddComponent<Image>();
        headerBg.color = new Color(0.12f, 0.16f, 0.22f, 0.95f);

        GameObject titleObj = new GameObject("Header_Title_Text");
        titleObj.transform.SetParent(headerBar.transform, false);
        TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "<b>ARAÇ SÜRÜŞ VE TAŞIMA REHBERİ</b>";
        titleTmp.fontSize = 24;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.MidlineLeft;
        titleTmp.color = new Color(1f, 0.92f, 0.45f);
        RectTransform tRect = titleObj.GetComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero;
        tRect.anchorMax = Vector2.one;
        tRect.offsetMin = new Vector2(30, 0);
        tRect.offsetMax = new Vector2(-30, 0);

        // Split Content
        GameObject contentArea = new GameObject("Split_Content_Area");
        contentArea.transform.SetParent(card.transform, false);
        RectTransform contentRect = contentArea.AddComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(30, 80);
        contentRect.offsetMax = new Vector2(-30, -85);

        // Left Column (Image)
        GameObject leftCol = new GameObject("Left_Image_Column");
        leftCol.transform.SetParent(contentArea.transform, false);
        RectTransform leftRect = leftCol.AddComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0, 0);
        leftRect.anchorMax = new Vector2(0, 1);
        leftRect.sizeDelta = new Vector2(420, 0);
        leftRect.anchoredPosition = new Vector2(210, 0);

        GameObject frameCard = new GameObject("Image_Frame_Card");
        frameCard.transform.SetParent(leftCol.transform, false);
        RectTransform frameRect = frameCard.AddComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(0, 0.28f);
        frameRect.anchorMax = new Vector2(1, 1);
        frameRect.offsetMin = Vector2.zero;
        frameRect.offsetMax = Vector2.zero;
        Image frameBg = frameCard.AddComponent<Image>();
        frameBg.color = new Color(0.04f, 0.06f, 0.08f, 0.9f);

        GameObject imgObj = new GameObject("Delivery_Point_Image");
        imgObj.transform.SetParent(frameCard.transform, false);
        RectTransform imgRect = imgObj.AddComponent<RectTransform>();
        imgRect.anchorMin = Vector2.zero;
        imgRect.anchorMax = Vector2.one;
        imgRect.offsetMin = new Vector2(8, 8);
        imgRect.offsetMax = new Vector2(-8, -8);
        Image vImg = imgObj.AddComponent<Image>();
        vImg.color = Color.white;

        GameObject captionObj = new GameObject("Left_Caption_Text");
        captionObj.transform.SetParent(leftCol.transform, false);
        TextMeshProUGUI capTmp = captionObj.AddComponent<TextMeshProUGUI>();
        capTmp.text = "<b>Teslimat Aracı (Pickup / Van)</b>\n<size=15><color=#A0C8FF>Kargolarınızı kasa veya bagaj kısmına yükleyin, güvenli ve hızlı şekilde taşıyın.</color></size>";
        capTmp.fontSize = 17;
        RectTransform capRect = captionObj.GetComponent<RectTransform>();
        capRect.anchorMin = new Vector2(0, 0);
        capRect.anchorMax = new Vector2(1, 0.25f);
        capRect.offsetMin = new Vector2(4, 0);
        capRect.offsetMax = new Vector2(-4, 0);

        // Right Column (Body)
        GameObject rightCol = new GameObject("Right_Text_Column");
        rightCol.transform.SetParent(contentArea.transform, false);
        RectTransform rightRect = rightCol.AddComponent<RectTransform>();
        rightRect.anchorMin = Vector2.zero;
        rightRect.anchorMax = Vector2.one;
        rightRect.offsetMin = new Vector2(450, 0);
        rightRect.offsetMax = Vector2.zero;

        GameObject bodyObj = new GameObject("Objective_Body_Text");
        bodyObj.transform.SetParent(rightCol.transform, false);
        TextMeshProUGUI bodyTmp = bodyObj.AddComponent<TextMeshProUGUI>();
        bodyTmp.fontSize = 16.5f;
        bodyTmp.lineSpacing = 12f;
        bodyTmp.color = new Color(0.92f, 0.94f, 0.96f);
        RectTransform bRect = bodyObj.GetComponent<RectTransform>();
        bRect.anchorMin = Vector2.zero;
        bRect.anchorMax = Vector2.one;
        bRect.sizeDelta = Vector2.zero;

        // Bottom Bar
        GameObject bottomBar = new GameObject("Bottom_Action_Bar");
        bottomBar.transform.SetParent(card.transform, false);
        RectTransform bottomRect = bottomBar.AddComponent<RectTransform>();
        bottomRect.anchorMin = Vector2.zero;
        bottomRect.anchorMax = new Vector2(1, 0);
        bottomRect.pivot = new Vector2(0.5f, 0);
        bottomRect.sizeDelta = new Vector2(0, 75);

        Image bottomBg = bottomBar.AddComponent<Image>();
        bottomBg.color = new Color(0.07f, 0.09f, 0.12f, 0.95f);

        GameObject startBtnObj = new GameObject("Start_Game_Button");
        startBtnObj.transform.SetParent(bottomBar.transform, false);
        RectTransform sbRect = startBtnObj.AddComponent<RectTransform>();
        sbRect.anchorMin = new Vector2(1, 0.5f);
        sbRect.anchorMax = new Vector2(1, 0.5f);
        sbRect.pivot = new Vector2(1, 0.5f);
        sbRect.sizeDelta = new Vector2(280, 50);
        sbRect.anchoredPosition = new Vector2(-30, 0);

        Image sbBg = startBtnObj.AddComponent<Image>();
        sbBg.color = new Color(0.12f, 0.65f, 0.35f, 1.0f);
        startBtnObj.AddComponent<Button>();

        GameObject sbTxtObj = new GameObject("Button_Text");
        sbTxtObj.transform.SetParent(startBtnObj.transform, false);
        TextMeshProUGUI sbTxt = sbTxtObj.AddComponent<TextMeshProUGUI>();
        sbTxt.text = "<b>ANLADIM, SÜRÜŞE BAŞLA</b>";
        sbTxt.fontSize = 17;
        sbTxt.fontStyle = FontStyles.Bold;
        sbTxt.alignment = TextAlignmentOptions.Center;
        sbTxt.color = Color.white;
        RectTransform sbtRect = sbTxtObj.GetComponent<RectTransform>();
        sbtRect.anchorMin = Vector2.zero;
        sbtRect.anchorMax = Vector2.one;
        sbtRect.sizeDelta = Vector2.zero;

        return root;
    }

    private static TextMeshProUGUI FindTMPRecursive(Transform root, params string[] names)
    {
        if (root == null) return null;
        var list = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var n in names)
        {
            foreach (var t in list)
            {
                if (t != null && t.gameObject.name.Equals(n, System.StringComparison.OrdinalIgnoreCase)) return t;
            }
        }
        foreach (var n in names)
        {
            foreach (var t in list)
            {
                if (t != null && t.gameObject.name.IndexOf(n, System.StringComparison.OrdinalIgnoreCase) >= 0) return t;
            }
        }
        return null;
    }

    private static Image FindImageRecursive(Transform root, params string[] names)
    {
        if (root == null) return null;
        var list = root.GetComponentsInChildren<Image>(true);
        foreach (var n in names)
        {
            foreach (var img in list)
            {
                if (img != null && img.gameObject.name.Equals(n, System.StringComparison.OrdinalIgnoreCase)) return img;
            }
        }
        return null;
    }

    private static Button FindButtonRecursive(Transform root, params string[] names)
    {
        if (root == null) return null;
        var list = root.GetComponentsInChildren<Button>(true);
        foreach (var n in names)
        {
            foreach (var b in list)
            {
                if (b != null && b.gameObject.name.Equals(n, System.StringComparison.OrdinalIgnoreCase)) return b;
            }
        }
        return null;
    }
}
