#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class BranchTransitionTool
{
    [MenuItem("Tools/Delivery Game/Add Branch Upgrade Transition Panel", false, 49)]
    public static void CreateTransitionPanel()
    {
        // 1. Find Canvas
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[BranchTransitionTool] Sahnede Canvas bulunamadı!");
            return;
        }

        // 2. Check if already exists under canvas
        Transform existing = canvas.transform.Find("BranchUpgradeTransitionPanel");
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            Debug.Log("<color=#32FFFF>[BranchTransitionTool] 'BranchUpgradeTransitionPanel' zaten Canvas altında mevcut.</color>");
            return;
        }

        // 3. Create Root Transition Panel strictly as a child of Canvas
        GameObject rootObj = new GameObject("BranchUpgradeTransitionPanel");
        rootObj.transform.SetParent(canvas.transform, false);
        Undo.RegisterCreatedObjectUndo(rootObj, "Create Branch Upgrade Transition Panel");

        RectTransform rootRect = rootObj.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.sizeDelta = Vector2.zero;

        CanvasGroup cg = rootObj.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;

        // Fullscreen Dark Background Overlay
        Image bgImg = rootObj.AddComponent<Image>();
        bgImg.color = new Color(0.04f, 0.05f, 0.08f, 0.98f);

        // 4. Center Presentation Card
        GameObject cardObj = new GameObject("Card");
        cardObj.transform.SetParent(rootObj.transform, false);
        RectTransform cardRect = cardObj.AddComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(920, 330);

        Image cardBg = cardObj.AddComponent<Image>();
        cardBg.color = new Color(0.08f, 0.11f, 0.17f, 0.95f);

        Outline outline = cardObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.2f, 0.8f, 1f, 0.45f);
        outline.effectDistance = new Vector2(2, -2);

        // 4.1 Badge Text
        GameObject badgeObj = new GameObject("BadgeText");
        badgeObj.transform.SetParent(cardObj.transform, false);
        RectTransform badgeRect = badgeObj.AddComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(0f, 1f);
        badgeRect.anchorMax = new Vector2(1f, 1f);
        badgeRect.pivot = new Vector2(0.5f, 1f);
        badgeRect.anchoredPosition = new Vector2(0, -25);
        badgeRect.sizeDelta = new Vector2(0, 35);

        TextMeshProUGUI badgeText = badgeObj.AddComponent<TextMeshProUGUI>();
        badgeText.text = "✦ ŞUBE YÜKSELTİLDİ • BRANCH UPGRADE ✦";
        badgeText.fontSize = 20;
        badgeText.fontStyle = FontStyles.Bold;
        badgeText.alignment = TextAlignmentOptions.Center;
        badgeText.color = new Color(0.3f, 1f, 0.6f);

        // 4.2 Title Text
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(cardObj.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -70);
        titleRect.sizeDelta = new Vector2(0, 60);

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "LEVEL 2: REGIONAL HUB";
        titleText.fontSize = 36;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;

        // 4.3 Decorative Accent Line
        GameObject lineObj = new GameObject("AccentLine");
        lineObj.transform.SetParent(cardObj.transform, false);
        RectTransform lineRect = lineObj.AddComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0.15f, 1f);
        lineRect.anchorMax = new Vector2(0.85f, 1f);
        lineRect.pivot = new Vector2(0.5f, 1f);
        lineRect.anchoredPosition = new Vector2(0, -135);
        lineRect.sizeDelta = new Vector2(0, 3);
        Image lineImg = lineObj.AddComponent<Image>();
        lineImg.color = new Color(0.2f, 0.8f, 1f, 0.7f);

        // 4.4 Details Text
        GameObject detailsObj = new GameObject("DetailsText");
        detailsObj.transform.SetParent(cardObj.transform, false);
        RectTransform detailsRect = detailsObj.AddComponent<RectTransform>();
        detailsRect.anchorMin = new Vector2(0f, 0f);
        detailsRect.anchorMax = new Vector2(1f, 1f);
        detailsRect.offsetMin = new Vector2(40, 30);
        detailsRect.offsetMax = new Vector2(-40, -150);

        TextMeshProUGUI detailsText = detailsObj.AddComponent<TextMeshProUGUI>();
        detailsText.text = "<b>Kapasite:</b> 8 Paket/Gün  |  <b>Kira:</b> $120/Gün\n<size=85%>Genişletilmiş lojistik alanı ve artırılmış teslimat hacmi.</size>";
        detailsText.fontSize = 20;
        detailsText.alignment = TextAlignmentOptions.Center;
        detailsText.color = new Color(0.85f, 0.92f, 1f);

        // 5. Connect component
        BranchUpgradeTransitionUI transScript = canvas.GetComponent<BranchUpgradeTransitionUI>();
        if (transScript == null) transScript = canvas.gameObject.AddComponent<BranchUpgradeTransitionUI>();

        transScript.panelRoot = rootObj;
        transScript.canvasGroup = cg;
        transScript.badgeText = badgeText;
        transScript.titleText = titleText;
        transScript.detailsText = detailsText;

        rootObj.SetActive(false);

        EditorUtility.SetDirty(canvas.gameObject);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = rootObj;

        Debug.Log("<color=#32FF64>[BranchTransitionTool] Sadece 'BranchUpgradeTransitionPanel' nesnesi oluşturuldu. Diğer hiçbir panele dokunulmadı.</color>");
    }
}
#endif
