#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class MenuUIBuilder
{
    private const string FONT_PATH = "Assets/_Project/Fonts/Inter-VariableFont_opsz,wght SDF.asset";
    private const string SPRITE_PANEL_LARGE = "Assets/_Project/Textures/UI/UI_Glass_Panel_Large.png";
    private const string SPRITE_PANEL_MED = "Assets/_Project/Textures/UI/UI_Glass_Panel_Medium.png";
    private const string SPRITE_CARD = "Assets/_Project/Textures/UI/UI_Glass_Card.png";
    private const string SPRITE_BTN_CYAN = "Assets/_Project/Textures/UI/UI_Button_Neon_Cyan.png";
    private const string SPRITE_BTN_GREEN = "Assets/_Project/Textures/UI/UI_Button_Neon_Green.png";
    private const string SPRITE_BTN_DARK = "Assets/_Project/Textures/UI/UI_Button_Dark_Base.png";
    private const string SPRITE_BTN_RED = "Assets/_Project/Textures/UI/UI_Button_Neon_Red.png";
    private const string SPRITE_BAR_TRACK = "Assets/_Project/Textures/UI/UI_Bar_Track.png";
    private const string SPRITE_BAR_FILL = "Assets/_Project/Textures/UI/UI_Bar_Fill.png";

    [MenuItem("Tools/Delivery Game/UI/Generate Complete Menu System (Main, Pause, Settings)", false, 5)]
    public static void GenerateMenuSystem()
    {
        // 1. Ensure Canvas
        Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("HUD_Canvas", typeof(RectTransform));
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // 2. Load Font & Sprites
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
        Sprite panelLargeSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_PANEL_LARGE);
        Sprite panelMedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_PANEL_MED);
        Sprite cardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_CARD);
        Sprite btnCyanSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_BTN_CYAN);
        Sprite btnGreenSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_BTN_GREEN);
        Sprite btnDarkSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_BTN_DARK);
        Sprite btnRedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_BTN_RED);
        Sprite trackSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_BAR_TRACK);
        Sprite fillSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_BAR_FILL);

        // 3. Ensure GameMenuManager on Canvas
        GameMenuManager menuMgr = canvas.GetComponent<GameMenuManager>();
        if (menuMgr == null) menuMgr = canvas.gameObject.AddComponent<GameMenuManager>();

        // 4. Ensure SettingsManager in scene
        SettingsManager settingsMgr = UnityEngine.Object.FindAnyObjectByType<SettingsManager>();
        if (settingsMgr == null)
        {
            GameObject sObj = new GameObject("SettingsManager");
            settingsMgr = sObj.AddComponent<SettingsManager>();
        }

        // 5. Ensure MainMenuCameraController in scene
        MainMenuCameraController camCtrl = UnityEngine.Object.FindAnyObjectByType<MainMenuCameraController>();
        if (camCtrl == null)
        {
            GameObject camHolder = new GameObject("MainMenu_Camera_Controller");
            camCtrl = camHolder.AddComponent<MainMenuCameraController>();
        }
        menuMgr.menuCameraController = camCtrl;

        // Clean up previous Menu panels if existing
        Transform oldMain = canvas.transform.Find("MainMenuPanel");
        if (oldMain != null) UnityEngine.Object.DestroyImmediate(oldMain.gameObject);

        Transform oldPause = canvas.transform.Find("PauseMenuPanel");
        if (oldPause != null) UnityEngine.Object.DestroyImmediate(oldPause.gameObject);

        Transform oldSettings = canvas.transform.Find("SettingsPanel");
        if (oldSettings != null) UnityEngine.Object.DestroyImmediate(oldSettings.gameObject);

        Transform oldFade = canvas.transform.Find("FadeOverlayPanel");
        if (oldFade != null) UnityEngine.Object.DestroyImmediate(oldFade.gameObject);

        // ==========================================
        // BUILD 1: MAIN MENU PANEL
        // ==========================================
        GameObject mainPanel = CreateFullscreenPanel(canvas.transform, "MainMenuPanel");
        Image mainVignette = mainPanel.AddComponent<Image>();
        mainVignette.color = new Color(0.02f, 0.03f, 0.05f, 0.40f);

        // Left Content Column Card
        GameObject mainCard = CreateUIElement("LeftContentCard", mainPanel.transform, new Vector2(0.08f, 0.5f), new Vector2(0.08f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(560, 660));
        Image cardImg = mainCard.AddComponent<Image>();
        if (panelMedSprite != null) { cardImg.sprite = panelMedSprite; cardImg.type = Image.Type.Sliced; }
        else cardImg.color = new Color(0.06f, 0.09f, 0.14f, 0.94f);

        Outline cardOutline = mainCard.AddComponent<Outline>();
        cardOutline.effectColor = new Color(0.2f, 0.8f, 1f, 0.5f);
        cardOutline.effectDistance = new Vector2(2, -2);

        // Title Header
        GameObject titleObj = CreateUIElement("TitleHeader", mainCard.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(500, 90));
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) titleText.font = fontAsset;
        titleText.text = "<size=130%><b>VALLEY LOGISTICS</b></size>\n<size=55%><color=#32FFFF>KARGO DAĞITIM VE SÜRÜŞ SİMÜLASYONU</color></size>";
        titleText.fontSize = 28;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        menuMgr.mainMenuTitleText = titleText;

        // Save Status Badge Card
        GameObject saveBadge = CreateUIElement("SaveInfoBadge", mainCard.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -135), new Vector2(480, 50));
        Image badgeImg = saveBadge.AddComponent<Image>();
        if (cardSprite != null) { badgeImg.sprite = cardSprite; badgeImg.type = Image.Type.Sliced; }
        else badgeImg.color = new Color(0.08f, 0.12f, 0.18f, 0.90f);

        GameObject saveInfoObj = CreateUIElement("SaveInfoText", saveBadge.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI saveText = saveInfoObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) saveText.font = fontAsset;
        saveText.text = "<color=#A0C8FF>Mevcut Şube:</color> <color=#FFFFFF>Lv.1 (Starter Garage)</color>  |  <color=#A0C8FF>Kasa:</color> <color=#32FF64>$500</color>";
        saveText.fontSize = 17;
        saveText.alignment = TextAlignmentOptions.Center;
        menuMgr.mainMenuSaveInfoText = saveText;

        // Action Buttons Column
        GameObject btnCol = CreateUIElement("ButtonsColumn", mainCard.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(440, 360));
        VerticalLayoutGroup vlg = btnCol.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 14f;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;

        Button continueBtn = CreateStyledButton("ContinueButton", btnCol.transform, "DEVAM ET", 54, btnGreenSprite != null ? btnGreenSprite : btnCyanSprite, new Color(0.1f, 0.95f, 0.45f), fontAsset);
        Button newGameBtn = CreateStyledButton("NewGameButton", btnCol.transform, "YENİ OYUN", 48, btnDarkSprite, new Color(0.3f, 0.9f, 1f), fontAsset);
        Button settingsBtn = CreateStyledButton("SettingsButton", btnCol.transform, "AYARLAR", 48, btnDarkSprite, Color.white, fontAsset);
        Button quitBtn = CreateStyledButton("QuitButton", btnCol.transform, "ÇIKIŞ", 48, btnRedSprite != null ? btnRedSprite : btnDarkSprite, new Color(1f, 0.4f, 0.4f), fontAsset);

        menuMgr.continueButton = continueBtn;
        menuMgr.newGameButton = newGameBtn;
        menuMgr.playButton = continueBtn;
        menuMgr.mainMenuSettingsButton = settingsBtn;
        menuMgr.mainMenuQuitButton = quitBtn;

        // New Game Confirmation Modal
        GameObject modalPanel = CreateFullscreenPanel(mainPanel.transform, "NewGameConfirmModal");
        Image mBg = modalPanel.AddComponent<Image>();
        mBg.color = new Color(0f, 0f, 0f, 0.75f);

        GameObject mCard = CreateUIElement("ModalCard", modalPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(540, 260));
        Image mCardBg = mCard.AddComponent<Image>();
        if (panelMedSprite != null) { mCardBg.sprite = panelMedSprite; mCardBg.type = Image.Type.Sliced; }
        else mCardBg.color = new Color(0.08f, 0.11f, 0.17f, 0.98f);

        Outline mCardOutline = mCard.AddComponent<Outline>();
        mCardOutline.effectColor = new Color(1f, 0.6f, 0.2f, 0.65f);
        mCardOutline.effectDistance = new Vector2(2, -2);

        GameObject mTitleObj = CreateUIElement("Title", mCard.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(480, 40));
        TextMeshProUGUI mTitle = mTitleObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) mTitle.font = fontAsset;
        mTitle.text = "<b>YENİ OYUN BAŞLAT</b>";
        mTitle.fontSize = 22;
        mTitle.alignment = TextAlignmentOptions.Center;
        mTitle.color = new Color(1f, 0.75f, 0.25f);

        GameObject mBodyObj = CreateUIElement("BodyText", mCard.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(480, 80));
        TextMeshProUGUI mBody = mBodyObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) mBody.font = fontAsset;
        mBody.text = "Mevcut kayıt ve tüm şube ilerlemeniz sıfırlanarak 1. Seviyeden yeni bir kariyere başlanacaktır.\n\n<b>Emin misiniz?</b>";
        mBody.fontSize = 15;
        mBody.alignment = TextAlignmentOptions.Center;
        mBody.color = new Color(0.85f, 0.92f, 1.0f);

        GameObject mBtnRow = CreateUIElement("ButtonRow", mCard.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(480, 45));
        HorizontalLayoutGroup mHlg = mBtnRow.AddComponent<HorizontalLayoutGroup>();
        mHlg.spacing = 20f;
        mHlg.childControlWidth = true;
        mHlg.childControlHeight = true;

        Button confirmBtn = CreateStyledButton("ConfirmBtn", mBtnRow.transform, "EVET, SIFIRLA VE BAŞLA", 45, btnRedSprite != null ? btnRedSprite : btnDarkSprite, new Color(1f, 0.45f, 0.45f), fontAsset);
        Button cancelBtn = CreateStyledButton("CancelBtn", mBtnRow.transform, "İPTAL", 45, btnDarkSprite, Color.white, fontAsset);

        menuMgr.newGameModalPanel = modalPanel;
        menuMgr.confirmNewGameBtn = confirmBtn;
        menuMgr.cancelNewGameBtn = cancelBtn;
        modalPanel.SetActive(false);

        menuMgr.mainMenuPanel = mainPanel;

        // Footer version text
        GameObject footerObj = CreateUIElement("FooterVersion", mainCard.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 20), new Vector2(400, 30));
        TextMeshProUGUI footerText = footerObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) footerText.font = fontAsset;
        footerText.text = "<color=#6080A0>v1.0.0 - PC Standalone URP Edition</color>";
        footerText.fontSize = 14;
        footerText.alignment = TextAlignmentOptions.Center;

        // ==========================================
        // BUILD 2: PAUSE MENU PANEL
        // ==========================================
        GameObject pausePanel = CreateFullscreenPanel(canvas.transform, "PauseMenuPanel");
        Image pauseBg = pausePanel.AddComponent<Image>();
        pauseBg.color = new Color(0.02f, 0.03f, 0.06f, 0.85f);

        GameObject pauseCard = CreateUIElement("PauseCard", pausePanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(480, 520));
        Image pCardImg = pauseCard.AddComponent<Image>();
        if (panelMedSprite != null) { pCardImg.sprite = panelMedSprite; pCardImg.type = Image.Type.Sliced; }
        else pCardImg.color = new Color(0.07f, 0.10f, 0.16f, 0.95f);

        Outline pOutline = pauseCard.AddComponent<Outline>();
        pOutline.effectColor = new Color(0.2f, 0.8f, 1f, 0.45f);
        pOutline.effectDistance = new Vector2(2, -2);

        GameObject pTitle = CreateUIElement("PauseTitle", pauseCard.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -45), new Vector2(400, 50));
        TextMeshProUGUI pTitleText = pTitle.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) pTitleText.font = fontAsset;
        pTitleText.text = "<b>OYUN DURAKLATILDI</b>\n<size=55%><color=#32FFFF>GAME PAUSED</color></size>";
        pTitleText.fontSize = 24;
        pTitleText.alignment = TextAlignmentOptions.Center;
        pTitleText.color = Color.white;

        // Pause Buttons
        GameObject pBtnCol = CreateUIElement("PauseButtons", pauseCard.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -35), new Vector2(380, 320));
        VerticalLayoutGroup pvlg = pBtnCol.AddComponent<VerticalLayoutGroup>();
        pvlg.spacing = 16f;
        pvlg.childAlignment = TextAnchor.MiddleCenter;
        pvlg.childControlWidth = true;
        pvlg.childControlHeight = false;

        Button pResumeBtn = CreateStyledButton("ResumeBtn", pBtnCol.transform, "DEVAM ET", 54, btnCyanSprite, new Color(0.2f, 0.9f, 1.0f), fontAsset);
        Button pSettingsBtn = CreateStyledButton("SettingsBtn", pBtnCol.transform, "AYARLAR", 48, btnDarkSprite, Color.white, fontAsset);
        Button pMainMenuBtn = CreateStyledButton("MainMenuBtn", pBtnCol.transform, "ANA MENÜYE DÖN", 48, btnDarkSprite, new Color(1.0f, 0.8f, 0.4f), fontAsset);
        Button pQuitBtn = CreateStyledButton("QuitBtn", pBtnCol.transform, "MASAÜSTÜNE ÇIK", 48, btnRedSprite != null ? btnRedSprite : btnDarkSprite, new Color(1f, 0.4f, 0.4f), fontAsset);

        menuMgr.resumeButton = pResumeBtn;
        menuMgr.pauseSettingsButton = pSettingsBtn;
        menuMgr.returnToMainMenuButton = pMainMenuBtn;
        menuMgr.pauseQuitButton = pQuitBtn;
        menuMgr.pauseMenuPanel = pausePanel;

        // ==========================================
        // BUILD 3: SETTINGS PANEL
        // ==========================================
        GameObject settingsPanel = CreateFullscreenPanel(canvas.transform, "SettingsPanel");
        Image setBg = settingsPanel.AddComponent<Image>();
        setBg.color = new Color(0.02f, 0.03f, 0.06f, 0.90f);

        GameObject setCard = CreateUIElement("SettingsCard", settingsPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920, 680));
        Image sCardImg = setCard.AddComponent<Image>();
        if (panelLargeSprite != null) { sCardImg.sprite = panelLargeSprite; sCardImg.type = Image.Type.Sliced; }
        else sCardImg.color = new Color(0.06f, 0.08f, 0.13f, 0.97f);

        Outline sOutline = setCard.AddComponent<Outline>();
        sOutline.effectColor = new Color(0.2f, 0.8f, 1f, 0.45f);
        sOutline.effectDistance = new Vector2(2, -2);

        // Settings Header
        GameObject sTitle = CreateUIElement("SettingsTitle", setCard.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(600, 45));
        TextMeshProUGUI sTitleText = sTitle.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) sTitleText.font = fontAsset;
        sTitleText.text = "<b>AYARLAR - SETTINGS</b>";
        sTitleText.fontSize = 28;
        sTitleText.alignment = TextAlignmentOptions.Center;
        sTitleText.color = Color.white;

        // Tab Navigation Bar
        GameObject tabRow = CreateUIElement("TabRow", setCard.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -95), new Vector2(800, 45));
        HorizontalLayoutGroup thlg = tabRow.AddComponent<HorizontalLayoutGroup>();
        thlg.spacing = 15f;
        thlg.childControlWidth = true;
        thlg.childControlHeight = true;

        Button tabAudio = CreateStyledButton("TabAudio", tabRow.transform, "SES", 45, btnDarkSprite, Color.white, fontAsset);
        Button tabGraphics = CreateStyledButton("TabGraphics", tabRow.transform, "GRAFİK", 45, btnDarkSprite, Color.white, fontAsset);
        Button tabControls = CreateStyledButton("TabControls", tabRow.transform, "KONTROLLER", 45, btnDarkSprite, Color.white, fontAsset);

        menuMgr.tabAudioBtn = tabAudio;
        menuMgr.tabGraphicsBtn = tabGraphics;
        menuMgr.tabControlsBtn = tabControls;

        // Content Area for Tabs
        GameObject contentArea = CreateUIElement("ContentArea", setCard.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(820, 430));

        // 3.1 AUDIO SECTION
        GameObject audioSec = CreateUIElement("AudioSection", contentArea.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        VerticalLayoutGroup avlg = audioSec.AddComponent<VerticalLayoutGroup>();
        avlg.spacing = 12f;
        avlg.childControlWidth = true;
        avlg.childControlHeight = false;

        menuMgr.masterVolumeSlider = CreateSliderRow("MasterVolRow", audioSec.transform, "Ana Ses (Master):", out menuMgr.masterVolumeValText, trackSprite, fillSprite, fontAsset);
        menuMgr.musicVolumeSlider = CreateSliderRow("MusicVolRow", audioSec.transform, "Müzik Ses Şiddeti:", out menuMgr.musicVolumeValText, trackSprite, fillSprite, fontAsset);
        menuMgr.sfxVolumeSlider = CreateSliderRow("SfxVolRow", audioSec.transform, "Ses Efektleri (SFX):", out menuMgr.sfxVolumeValText, trackSprite, fillSprite, fontAsset);
        menuMgr.ambienceVolumeSlider = CreateSliderRow("AmbienceVolRow", audioSec.transform, "Çevre & Vadi Atmosferi:", out menuMgr.ambienceVolumeValText, trackSprite, fillSprite, fontAsset);
        menuMgr.uiVolumeSlider = CreateSliderRow("UiVolRow", audioSec.transform, "Arayüz & Bildirimler:", out menuMgr.uiVolumeValText, trackSprite, fillSprite, fontAsset);
        menuMgr.audioSection = audioSec;

        // 3.2 GRAPHICS SECTION
        GameObject gfxSec = CreateUIElement("GraphicsSection", contentArea.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        VerticalLayoutGroup gvlg = gfxSec.AddComponent<VerticalLayoutGroup>();
        gvlg.spacing = 12f;
        gvlg.childControlWidth = true;
        gvlg.childControlHeight = false;

        menuMgr.qualityDropdown = CreateDropdownRow("QualityRow", gfxSec.transform, "Grafik Kalitesi:", fontAsset);
        menuMgr.fullscreenDropdown = CreateDropdownRow("FullscreenRow", gfxSec.transform, "Ekran Modu:", fontAsset);
        menuMgr.resolutionDropdown = CreateDropdownRow("ResolutionRow", gfxSec.transform, "Çözünürlük:", fontAsset);
        menuMgr.vsyncToggle = CreateToggleRow("VsyncRow", gfxSec.transform, "Dikey Senkronizasyon (V-Sync):", fontAsset);
        menuMgr.fpsLimitDropdown = CreateDropdownRow("FpsLimitRow", gfxSec.transform, "Hedef Kare Hızı (FPS Limit):", fontAsset);
        menuMgr.graphicsSection = gfxSec;
        gfxSec.SetActive(false);

        // 3.3 CONTROLS SECTION
        GameObject ctrlSec = CreateUIElement("ControlsSection", contentArea.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        VerticalLayoutGroup cvlg = ctrlSec.AddComponent<VerticalLayoutGroup>();
        cvlg.spacing = 8f;
        cvlg.childControlWidth = true;
        cvlg.childControlHeight = false;

        menuMgr.mouseSensSlider = CreateSliderRow("MouseSensRow", ctrlSec.transform, "Fare Bakış Hassasiyeti:", out menuMgr.mouseSensValText, trackSprite, fillSprite, fontAsset, 0.2f, 5.0f);
        menuMgr.invertYToggle = CreateToggleRow("InvertYRow", ctrlSec.transform, "Fare Y-Ekseni Ters Çevir:", fontAsset);

        // Keybindings Header Row
        GameObject kbHeader = CreateUIElement("KeybindingsHeaderRow", ctrlSec.transform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0, 36));
        GameObject kbTitleObj = CreateUIElement("HeaderTitle", kbHeader.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8, 0), new Vector2(480, 32));
        TextMeshProUGUI kbTitle = kbTitleObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) kbTitle.font = fontAsset;
        kbTitle.text = "<color=#32FFFF><b>TUŞ ATAMALARI:</b></color> <size=80%><color=#85A8C8>(Değiştirmek istediğiniz tuşa tıklayın)</color></size>";
        kbTitle.fontSize = 16;
        kbTitle.alignment = TextAlignmentOptions.Left;

        GameObject resetBtnObj = CreateUIElement("ResetBindingsBtn", kbHeader.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-6, 0), new Vector2(180, 32));
        menuMgr.resetKeybindingsBtn = CreateStyledButton("ResetBtn", resetBtnObj.transform, "Varsayılana Sıfırla", 32, btnDarkSprite, new Color(1.0f, 0.85f, 0.4f), fontAsset);

        // Keybindings ScrollView
        var (svObj, contentTr) = CreateKeybindingsScrollViewElement(ctrlSec.transform, cardSprite, trackSprite, fillSprite);
        menuMgr.keybindingsContent = contentTr;

        foreach (GameAction action in System.Enum.GetValues(typeof(GameAction)))
        {
            string desc = KeyBindingManager.GetActionDescription(action);
            string keyName = KeyBindingManager.GetBinding(action).GetDisplayName();
            CreateKeyRowElement(contentTr, desc, action.ToString(), keyName, btnCyanSprite != null ? btnCyanSprite : btnDarkSprite, fontAsset);
        }

        menuMgr.controlsSection = ctrlSec;
        ctrlSec.SetActive(false);

        // Back / Save Button
        GameObject backBtnObj = CreateUIElement("BackButtonHolder", setCard.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 35), new Vector2(300, 48));
        Button backBtn = CreateStyledButton("SettingsBackBtn", backBtnObj.transform, "KAYDET VE GERİ DÖN", 48, btnCyanSprite, new Color(0.2f, 0.9f, 1f), fontAsset);
        menuMgr.settingsBackButton = backBtn;
        menuMgr.settingsPanel = settingsPanel;

        // ==========================================
        // BUILD 4: FADE OVERLAY PANEL
        // ==========================================
        GameObject fadePanel = CreateFullscreenPanel(canvas.transform, "FadeOverlayPanel");
        Image fadeImg = fadePanel.AddComponent<Image>();
        fadeImg.color = Color.black;
        CanvasGroup fadeCg = fadePanel.AddComponent<CanvasGroup>();
        fadeCg.alpha = 0f;
        fadeCg.blocksRaycasts = false;
        fadeCg.interactable = false;
        menuMgr.fadeOverlayCanvasGroup = fadeCg;
        fadePanel.SetActive(false);

        // Set initial visibility
        mainPanel.SetActive(true);
        mainPanel.transform.SetAsLastSibling();
        pausePanel.SetActive(false);
        settingsPanel.SetActive(false);

        EditorUtility.SetDirty(canvas.gameObject);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = canvas.gameObject;

        Debug.Log("<color=#32FF64><b>[MenuUIBuilder]</b> Ana Ekran, Pause Menüsü, Ayarlar ve Fade Geçişi başarıyla oluşturuldu ve GameMenuManager'a bağlandı!</color>");
    }

    #region --- UI HELPER BUILDERS ---

    private static GameObject CreateFullscreenPanel(Transform parent, string name)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;
        return panel;
    }

    private static GameObject CreateUIElement(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        rt.localScale = Vector3.one;
        return obj;
    }

    private static Button CreateStyledButton(string name, Transform parent, string label, float height, Sprite btnSprite, Color textColor, TMP_FontAsset font)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, height);
        rt.localScale = Vector3.one;

        Image img = btnObj.AddComponent<Image>();
        if (btnSprite != null) { img.sprite = btnSprite; img.type = Image.Type.Sliced; }
        else img.color = new Color(0.12f, 0.16f, 0.24f, 0.95f);

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
        cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        btn.colors = cb;

        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform trt = textObj.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        trt.anchoredPosition = Vector2.zero;
        trt.localScale = Vector3.one;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = label;
        tmp.fontSize = 18;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = textColor;

        return btn;
    }

    private static Slider CreateSliderRow(string name, Transform parent, string label, out TextMeshProUGUI valTextOut, Sprite trackSprite, Sprite fillSprite, TMP_FontAsset font, float minVal = 0f, float maxVal = 1f)
    {
        GameObject row = new GameObject(name, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        RectTransform rrt = row.GetComponent<RectTransform>();
        rrt.sizeDelta = new Vector2(0, 42);
        rrt.localScale = Vector3.one;

        // Label
        GameObject lblObj = CreateUIElement("Label", row.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10, 0), new Vector2(280, 36));
        TextMeshProUGUI lbl = lblObj.AddComponent<TextMeshProUGUI>();
        if (font != null) lbl.font = font;
        lbl.text = label;
        lbl.fontSize = 17;
        lbl.alignment = TextAlignmentOptions.Left;
        lbl.color = new Color(0.85f, 0.92f, 1f);

        // Value text
        GameObject valObj = CreateUIElement("ValText", row.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-10, 0), new Vector2(70, 36));
        TextMeshProUGUI valText = valObj.AddComponent<TextMeshProUGUI>();
        if (font != null) valText.font = font;
        valText.text = "%100";
        valText.fontSize = 17;
        valText.alignment = TextAlignmentOptions.Right;
        valText.color = new Color(0.3f, 1f, 0.6f);
        valTextOut = valText;

        // Slider Object
        GameObject sliderObj = CreateUIElement("Slider", row.transform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(100, 0), new Vector2(-380, 24));
        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = minVal;
        slider.maxValue = maxVal;

        // Background Track
        GameObject bgTrack = CreateUIElement("Background", sliderObj.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image bgImg = bgTrack.AddComponent<Image>();
        if (trackSprite != null) { bgImg.sprite = trackSprite; bgImg.type = Image.Type.Sliced; }
        else bgImg.color = new Color(0.1f, 0.14f, 0.20f, 0.95f);

        // Fill Area
        GameObject fillArea = CreateUIElement("Fill Area", sliderObj.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        GameObject fill = CreateUIElement("Fill", fillArea.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image fillImg = fill.AddComponent<Image>();
        if (fillSprite != null) { fillImg.sprite = fillSprite; fillImg.type = Image.Type.Sliced; }
        else fillImg.color = new Color(0.2f, 0.8f, 1.0f, 0.95f);

        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.targetGraphic = bgImg;

        return slider;
    }

    private static TMP_Dropdown CreateDropdownRow(string name, Transform parent, string label, TMP_FontAsset font)
    {
        GameObject row = new GameObject(name, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        RectTransform rrt = row.GetComponent<RectTransform>();
        rrt.sizeDelta = new Vector2(0, 42);
        rrt.localScale = Vector3.one;

        GameObject lblObj = CreateUIElement("Label", row.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10, 0), new Vector2(280, 36));
        TextMeshProUGUI lbl = lblObj.AddComponent<TextMeshProUGUI>();
        if (font != null) lbl.font = font;
        lbl.text = label;
        lbl.fontSize = 17;
        lbl.alignment = TextAlignmentOptions.Left;
        lbl.color = new Color(0.85f, 0.92f, 1f);

        GameObject ddObj = CreateUIElement("Dropdown", row.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-10, 0), new Vector2(360, 38));
        Image ddImg = ddObj.AddComponent<Image>();
        ddImg.color = new Color(0.10f, 0.14f, 0.22f, 0.95f);

        TMP_Dropdown dd = ddObj.AddComponent<TMP_Dropdown>();

        GameObject captionObj = CreateUIElement("Label", ddObj.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-20, 0));
        TextMeshProUGUI caption = captionObj.AddComponent<TextMeshProUGUI>();
        if (font != null) caption.font = font;
        caption.fontSize = 16;
        caption.alignment = TextAlignmentOptions.Left;
        caption.color = Color.white;
        dd.captionText = caption;

        return dd;
    }

    private static Toggle CreateToggleRow(string name, Transform parent, string label, TMP_FontAsset font)
    {
        GameObject row = new GameObject(name, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        RectTransform rrt = row.GetComponent<RectTransform>();
        rrt.sizeDelta = new Vector2(0, 42);
        rrt.localScale = Vector3.one;

        GameObject lblObj = CreateUIElement("Label", row.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10, 0), new Vector2(380, 36));
        TextMeshProUGUI lbl = lblObj.AddComponent<TextMeshProUGUI>();
        if (font != null) lbl.font = font;
        lbl.text = label;
        lbl.fontSize = 17;
        lbl.alignment = TextAlignmentOptions.Left;
        lbl.color = new Color(0.85f, 0.92f, 1f);

        GameObject toggleObj = CreateUIElement("Toggle", row.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-10, 0), new Vector2(38, 38));
        Image bgImg = toggleObj.AddComponent<Image>();
        bgImg.color = new Color(0.10f, 0.14f, 0.22f, 0.95f);

        Toggle toggle = toggleObj.AddComponent<Toggle>();

        GameObject checkObj = CreateUIElement("Checkmark", toggleObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24, 24));
        Image checkImg = checkObj.AddComponent<Image>();
        checkImg.color = new Color(0.2f, 0.9f, 1.0f);

        toggle.graphic = checkImg;
        toggle.targetGraphic = bgImg;

        return toggle;
    }

    private static (GameObject scrollView, Transform content) CreateKeybindingsScrollViewElement(Transform parent, Sprite bgSprite, Sprite trackSprite, Sprite fillSprite)
    {
        GameObject svObj = CreateUIElement("KeybindingsScrollView", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820, 260));
        Image svBg = svObj.AddComponent<Image>();
        if (bgSprite != null) { svBg.sprite = bgSprite; svBg.type = Image.Type.Sliced; }
        else svBg.color = new Color(0.04f, 0.06f, 0.10f, 0.90f);

        Outline svOutline = svObj.AddComponent<Outline>();
        svOutline.effectColor = new Color(0.2f, 0.8f, 1f, 0.30f);
        svOutline.effectDistance = new Vector2(1, -1);

        ScrollRect sr = svObj.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 28f;

        // Viewport
        GameObject vpObj = CreateUIElement("Viewport", svObj.transform, Vector2.zero, Vector2.one, new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
        RectTransform vpRt = vpObj.GetComponent<RectTransform>();
        vpRt.offsetMin = new Vector2(6, 6);
        vpRt.offsetMax = new Vector2(-20, -6);
        vpObj.AddComponent<RectMask2D>();
        sr.viewport = vpRt;

        // Content
        GameObject contentObj = CreateUIElement("Content", vpObj.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
        RectTransform contentRt = contentObj.GetComponent<RectTransform>();
        contentRt.anchoredPosition = Vector2.zero;
        VerticalLayoutGroup cvlg = contentObj.AddComponent<VerticalLayoutGroup>();
        cvlg.spacing = 6f;
        cvlg.padding = new RectOffset(6, 6, 6, 6);
        cvlg.childControlWidth = true;
        cvlg.childControlHeight = false;
        cvlg.childForceExpandWidth = true;
        cvlg.childForceExpandHeight = false;

        ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sr.content = contentRt;

        // Scrollbar
        GameObject sbObj = CreateUIElement("Scrollbar", svObj.transform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(14, 0));
        RectTransform sbRt = sbObj.GetComponent<RectTransform>();
        sbRt.offsetMin = new Vector2(-16, 6);
        sbRt.offsetMax = new Vector2(-4, -6);
        Image sbTrack = sbObj.AddComponent<Image>();
        if (trackSprite != null) { sbTrack.sprite = trackSprite; sbTrack.type = Image.Type.Sliced; }
        else sbTrack.color = new Color(0.07f, 0.10f, 0.16f, 0.90f);

        GameObject handleObj = CreateUIElement("Handle", sbObj.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image handleImg = handleObj.AddComponent<Image>();
        if (fillSprite != null) { handleImg.sprite = fillSprite; handleImg.type = Image.Type.Sliced; }
        else handleImg.color = new Color(0.2f, 0.8f, 1.0f, 0.80f);

        Scrollbar sb = sbObj.AddComponent<Scrollbar>();
        sb.direction = Scrollbar.Direction.BottomToTop;
        sb.targetGraphic = handleImg;
        sb.handleRect = handleObj.GetComponent<RectTransform>();

        sr.verticalScrollbar = sb;
        sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

        return (svObj, contentObj.transform);
    }

    private static GameObject CreateKeyRowElement(Transform parent, string actionLabel, string actionKeyId, string currentKeyText, Sprite btnSprite, TMP_FontAsset fontAsset)
    {
        GameObject row = CreateUIElement("KeyRow_" + actionKeyId, parent, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0, 38));
        Image rowBg = row.AddComponent<Image>();
        rowBg.color = new Color(0.06f, 0.09f, 0.15f, 0.85f);

        Outline rowOutline = row.AddComponent<Outline>();
        rowOutline.effectColor = new Color(0.2f, 0.8f, 1f, 0.20f);
        rowOutline.effectDistance = new Vector2(1, -1);

        // Left Action Label
        GameObject lblObj = CreateUIElement("ActionLabel", row.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14, 0), new Vector2(460, 32));
        TextMeshProUGUI lbl = lblObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) lbl.font = fontAsset;
        lbl.text = $"<b>{actionLabel}</b>";
        lbl.fontSize = 15;
        lbl.alignment = TextAlignmentOptions.Left;
        lbl.color = new Color(0.9f, 0.95f, 1.0f);

        // Right Key Button
        GameObject btnObj = CreateUIElement("KeyButton", row.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-12, 0), new Vector2(170, 30));
        Image btnImg = btnObj.AddComponent<Image>();
        if (btnSprite != null) { btnImg.sprite = btnSprite; btnImg.type = Image.Type.Sliced; }
        else btnImg.color = new Color(0.10f, 0.16f, 0.25f, 0.95f);

        Outline btnOutline = btnObj.AddComponent<Outline>();
        btnOutline.effectColor = new Color(0.2f, 0.8f, 1f, 0.45f);
        btnOutline.effectDistance = new Vector2(1, -1);

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
        cb.pressedColor = new Color(0.7f, 0.9f, 1.0f, 1f);
        btn.colors = cb;

        GameObject textObj = CreateUIElement("Text", btnObj.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI txt = textObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) txt.font = fontAsset;
        txt.text = $"[ {currentKeyText} ]";
        txt.fontSize = 15;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = new Color(0.25f, 0.95f, 1.0f);

        return row;
    }

    #endregion
}
#endif
