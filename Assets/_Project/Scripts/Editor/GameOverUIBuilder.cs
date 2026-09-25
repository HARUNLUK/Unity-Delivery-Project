#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Dedicated Editor Tool and UI Builder for the Game Over Screen.
/// Allows level designers and developers to build, preview, customize, and test the Game Over screen with 1 click.
/// </summary>
public class GameOverUIBuilder : EditorWindow
{
    private static TMP_FontAsset cachedFont;
    private static Sprite sGlassLarge;
    private static Sprite sGlassCard;
    private static Sprite sBtnRed;
    private static Sprite sBtnGreen;

    [MenuItem("Tools/Delivery Game/UI/Build or Rebuild Game Over Screen", false, 35)]
    public static void BuildGameOverScreenMenuItem()
    {
        BuildGameOverScreenInActiveScene();
    }

    [MenuItem("Tools/Delivery Game/Windows/Game Over Designer & Tool", false, 15)]
    public static void OpenGameOverDesignerWindow()
    {
        GameOverUIBuilder window = GetWindow<GameOverUIBuilder>("Game Over Designer");
        window.minSize = new Vector2(460, 520);
        window.Show();
    }

    [MenuItem("Tools/Delivery Game/Testing/Simulate Bankruptcy (-$1000)", false, 115)]
    public static void SimulateBankruptcyMenuItem()
    {
        if (Application.isPlaying)
        {
            if (PlayerEconomyManager.Instance != null)
            {
                PlayerEconomyManager.Instance.SetBalance(-1050);
            }
            if (GameOverManager.Instance != null)
            {
                GameOverManager.Instance.TriggerGameOver(-1050);
            }
            Debug.LogWarning("<color=#FF2222>[TEST] Simulated Bankruptcy (-$1050) triggered!</color>");
        }
        else
        {
            EditorUtility.DisplayDialog("Play Mode Required", "Oyunu simüle etmek için lütfen önce Unity'de PLAY tuşuna basın.", "Tamam");
        }
    }

    [MenuItem("Tools/Delivery Game/Testing/Wipe All Save Data (Permanent Reset)", false, 116)]
    public static void WipeAllSaveDataMenuItem()
    {
        if (EditorUtility.DisplayDialog("Tüm Kayıtları Kalıcı Sil", "Tüm PlayerPrefs kayıtları (Kasa, Şube Seviyesi, Gün Sayısı, Açılan Dükkanlar) KALICI OLARAK silinecek. Emin misiniz?", "Evet, Hepsini Sil", "İptal"))
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("<color=#FF3333>[RESET] Tüm kayıtlar (PlayerPrefs) kalıcı olarak silindi!</color>");
        }
    }

    public static GameObject BuildGameOverScreenInActiveScene()
    {
        LoadAssets();

        // 1. Ensure Canvas
        Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
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
        }

        // 2. Remove old Game Over panel if exists
        Transform oldPanel = canvas.transform.Find("GameOver_Modal_Root");
        if (oldPanel != null)
        {
            DestroyImmediate(oldPanel.gameObject);
        }

        // 3. Create AAA Dark Glassmorphic Game Over Modal
        GameObject root = new GameObject("GameOver_Modal_Root");
        root.transform.SetParent(canvas.transform, false);
        RectTransform rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.sizeDelta = Vector2.zero;
        rootRect.anchoredPosition = Vector2.zero;

        Image backdrop = root.AddComponent<Image>();
        backdrop.color = new Color(0.04f, 0.04f, 0.06f, 0.94f); // Deep dark vignette

        CanvasGroup cg = root.AddComponent<CanvasGroup>();

        // Center Card Box (640 x 500)
        GameObject card = new GameObject("GameOver_Card");
        card.transform.SetParent(root.transform, false);
        RectTransform cardRect = card.AddComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(640, 500);
        cardRect.anchoredPosition = Vector2.zero;

        Image cardBg = card.AddComponent<Image>();
        if (sGlassCard != null)
        {
            cardBg.sprite = sGlassCard;
            cardBg.type = Image.Type.Sliced;
            cardBg.color = new Color(0.12f, 0.13f, 0.16f, 0.98f);
        }
        else
        {
            cardBg.color = new Color(0.12f, 0.13f, 0.16f, 0.98f);
        }

        Outline outline = card.AddComponent<Outline>();
        outline.effectColor = new Color(0.95f, 0.25f, 0.25f, 0.65f); // Red warning neon glow
        outline.effectDistance = new Vector2(2.5f, -2.5f);

        // Header Warning Badge
        GameObject headerBadge = new GameObject("Header_Badge");
        headerBadge.transform.SetParent(card.transform, false);
        RectTransform badgeRect = headerBadge.AddComponent<RectTransform>();
        badgeRect.sizeDelta = new Vector2(560, 46);
        badgeRect.anchoredPosition = new Vector2(0, 195);

        Image badgeBg = headerBadge.AddComponent<Image>();
        badgeBg.color = new Color(0.90f, 0.18f, 0.18f, 0.22f);

        GameObject badgeTextObj = new GameObject("Badge_Text");
        badgeTextObj.transform.SetParent(headerBadge.transform, false);
        TextMeshProUGUI bText = badgeTextObj.AddComponent<TextMeshProUGUI>();
        if (cachedFont != null) bText.font = cachedFont;
        bText.text = "ŞİRKET TASFİYESİ & İFLAS";
        bText.fontSize = 19;
        bText.fontStyle = FontStyles.Bold;
        bText.alignment = TextAlignmentOptions.Center;
        bText.color = new Color(1f, 0.35f, 0.35f);
        RectTransform btRect = badgeTextObj.GetComponent<RectTransform>();
        btRect.anchorMin = Vector2.zero;
        btRect.anchorMax = Vector2.one;
        btRect.sizeDelta = Vector2.zero;

        // Title Text
        GameObject titleObj = new GameObject("Title_Text");
        titleObj.transform.SetParent(card.transform, false);
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        if (cachedFont != null) titleText.font = cachedFont;
        titleText.text = "İFLAS ETTİNİZ!";
        titleText.fontSize = 42;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(1f, 0.95f, 0.95f);
        RectTransform tRect = titleObj.GetComponent<RectTransform>();
        tRect.sizeDelta = new Vector2(560, 55);
        tRect.anchoredPosition = new Vector2(0, 130);

        // Debt Amount Text
        GameObject debtObj = new GameObject("Debt_Amount_Text");
        debtObj.transform.SetParent(card.transform, false);
        TextMeshProUGUI debtAmountText = debtObj.AddComponent<TextMeshProUGUI>();
        if (cachedFont != null) debtAmountText.font = cachedFont;
        debtAmountText.text = "<color=#FF4444>Mevcut Bakiye: -$1,000</color>\n<size=70%><color=#FFAA33>(Borç Limiti: -$1,000)</color></size>";
        debtAmountText.fontSize = 26;
        debtAmountText.fontStyle = FontStyles.Bold;
        debtAmountText.alignment = TextAlignmentOptions.Center;
        RectTransform dRect = debtObj.GetComponent<RectTransform>();
        dRect.sizeDelta = new Vector2(560, 60);
        dRect.anchoredPosition = new Vector2(0, 60);

        // Reason / Explanation Text
        GameObject reasonObj = new GameObject("Reason_Text");
        reasonObj.transform.SetParent(card.transform, false);
        TextMeshProUGUI reasonText = reasonObj.AddComponent<TextMeshProUGUI>();
        if (cachedFont != null) reasonText.font = cachedFont;
        reasonText.text = "Borç limitini (-$1,000) aştığınız için lojistik şirketiniz iflas etti ve şubeniz kapatıldı.\nTüm kayıtlar sıfırlandı.";
        reasonText.fontSize = 16;
        reasonText.alignment = TextAlignmentOptions.Center;
        reasonText.color = new Color(0.80f, 0.82f, 0.85f);
        RectTransform rRect = reasonObj.GetComponent<RectTransform>();
        rRect.sizeDelta = new Vector2(520, 75);
        rRect.anchoredPosition = new Vector2(0, -15);

        // Restart Button
        GameObject btnObj = new GameObject("Restart_Button");
        btnObj.transform.SetParent(card.transform, false);
        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(460, 58);
        btnRect.anchoredPosition = new Vector2(0, -160);

        Image btnImg = btnObj.AddComponent<Image>();
        if (sBtnGreen != null)
        {
            btnImg.sprite = sBtnGreen;
            btnImg.type = Image.Type.Sliced;
        }
        else
        {
            btnImg.color = new Color(0.20f, 0.65f, 0.35f, 1f);
        }

        Button restartButton = btnObj.AddComponent<Button>();

        GameObject btnTextObj = new GameObject("Btn_Text");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
        if (cachedFont != null) btnText.font = cachedFont;
        btnText.text = "🔄 YENİDEN BAŞLA (GÜN 1)";
        btnText.fontSize = 21;
        btnText.fontStyle = FontStyles.Bold;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.color = Color.white;
        RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.sizeDelta = Vector2.zero;

        // 4. Bind references to GameOverManager
        GameOverManager manager = UnityEngine.Object.FindAnyObjectByType<GameOverManager>();
        if (manager == null)
        {
            GameObject managerObj = new GameObject("[GAME_OVER_MANAGER]");
            manager = managerObj.AddComponent<GameOverManager>();
        }

        manager.gameOverPanelRoot = root;
        manager.titleText = titleText;
        manager.reasonText = reasonText;
        manager.debtAmountText = debtAmountText;
        manager.restartButton = restartButton;
        manager.panelCanvasGroup = cg;

        restartButton.onClick.RemoveAllListeners();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(restartButton.onClick, manager.RestartGameFromBeginning);

        root.SetActive(false);

        EditorUtility.SetDirty(canvas.gameObject);
        EditorUtility.SetDirty(manager.gameObject);

        Debug.Log("<color=#32FF64>[GameOverUIBuilder] AAA Dark Glassmorphic Game Over Screen successfully built & bound to GameOverManager!</color>");
        return root;
    }

    private static void LoadAssets()
    {
        cachedFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Fonts/Inter-VariableFont_opsz,wght SDF.asset");
        if (cachedFont == null)
        {
            cachedFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        }

        sGlassLarge = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Textures/UI/UI_Glass_Panel_Large.png");
        sGlassCard = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Textures/UI/UI_Glass_Card.png");
        sBtnRed = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Textures/UI/UI_Button_Neon_Red.png");
        sBtnGreen = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Textures/UI/UI_Button_Neon_Green.png");
    }

    // --- GUI WINDOW CONTROLS ---
    private int debtLimit = -1000;
    private int startCash = 100;
    private string customTitle = "İFLAS ETTİNİZ!";
    private string customReason = "Borç limitini (-$1,000) aştığınız için lojistik şirketiniz iflas etti ve şubeniz kapatıldı.";
    private bool previewInScene = false;

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("💀 GAME OVER EKRANI & İFLAS YÖNETİCİSİ", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Bu araç; -$1,000 borç limitine ulaşıldığında belirecek olan Game Over ekranını oluşturur, özelleştirir ve kayıt silme mekanizmasını test etmenizi sağlar.", MessageType.Info);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("1. İflas & Para Ayarları", EditorStyles.boldLabel);
        debtLimit = EditorGUILayout.IntField("İflas Borç Limiti ($)", debtLimit);
        startCash = EditorGUILayout.IntField("Baştan Başlama Sermayesi ($)", startCash);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("2. Metin İçerikleri", EditorStyles.boldLabel);
        customTitle = EditorGUILayout.TextField("Başlık", customTitle);
        customReason = EditorGUILayout.TextArea(customReason, GUILayout.Height(50));

        EditorGUILayout.Space(15);
        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.4f);
        if (GUILayout.Button("Game Over Ekranını Sahneye Kur / Yenile", GUILayout.Height(38)))
        {
            GameObject created = BuildGameOverScreenInActiveScene();
            GameOverManager mgr = UnityEngine.Object.FindAnyObjectByType<GameOverManager>();
            if (mgr != null)
            {
                mgr.bankruptcyDebtLimit = debtLimit;
                mgr.restartStartingCash = startCash;
                mgr.gameOverTitle = customTitle;
                mgr.gameOverReason = customReason;
                EditorUtility.SetDirty(mgr);
            }
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("3. Editör & Sahne Önizleme", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        previewInScene = EditorGUILayout.Toggle("Sahne Ekranında Önizle (Preview)", previewInScene);
        if (EditorGUI.EndChangeCheck())
        {
            GameOverManager mgr = UnityEngine.Object.FindAnyObjectByType<GameOverManager>();
            if (mgr != null && mgr.gameOverPanelRoot != null)
            {
                mgr.gameOverPanelRoot.SetActive(previewInScene);
                if (previewInScene) mgr.gameOverPanelRoot.transform.SetAsLastSibling();
            }
        }

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("4. Test & Kayıt Sıfırlama Kısayolları", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
        if (GUILayout.Button("İflas Simüle Et (-$1,000)", GUILayout.Height(32)))
        {
            SimulateBankruptcyMenuItem();
        }
        GUI.backgroundColor = new Color(1f, 0.6f, 0.2f);
        if (GUILayout.Button("🗑️ Tüm Kayıtları Kalıcı Sil", GUILayout.Height(32)))
        {
            WipeAllSaveDataMenuItem();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
    }
}
#endif
