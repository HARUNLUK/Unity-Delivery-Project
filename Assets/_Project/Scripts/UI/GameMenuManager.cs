using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum GameFlowState
{
    MainMenu,
    Playing,
    Paused,
    Settings,
    Transitioning
}

/// <summary>
/// Central manager for Main Menu, Pause Menu, Settings System, and Screen Fade Transitions.
/// Handles camera switching between Shop Overview and Player Gameplay, and coordinates game pause state.
/// </summary>
public class GameMenuManager : MonoBehaviour
{
    private static GameMenuManager instance;
    public static GameMenuManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = UnityEngine.Object.FindAnyObjectByType<GameMenuManager>(FindObjectsInactive.Include);
                if (instance == null)
                {
                    Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
                    if (canvas != null)
                    {
                        instance = canvas.gameObject.AddComponent<GameMenuManager>();
                        instance.EnsureUI();
                    }
                }
            }
            return instance;
        }
        private set => instance = value;
    }

    [Header("--- INITIAL FLOW STATE ---")]
    [Tooltip("If true, scene launches into the Main Menu with cinematic shop view. If false, starts directly in gameplay.")]
    public bool startInMainMenu = true;

    [Header("--- PANEL ROOTS ---")]
    public GameObject mainMenuPanel;
    public GameObject pauseMenuPanel;
    public GameObject settingsPanel;
    public CanvasGroup fadeOverlayCanvasGroup;

    [Header("--- CAMERA CONTROLLER ---")]
    public MainMenuCameraController menuCameraController;

    [Header("--- TRANSITION TIMING ---")]
    public float fadeOutDuration = 0.55f;
    public float fadeInDuration = 0.65f;
    public float blackoutHoldDuration = 0.20f;

    [Header("--- MAIN MENU UI REFERENCES ---")]
    public TextMeshProUGUI mainMenuTitleText;
    public TextMeshProUGUI mainMenuSaveInfoText;
    public Button playButton;
    public Button mainMenuSettingsButton;
    public Button mainMenuQuitButton;

    [Header("--- PAUSE MENU UI REFERENCES ---")]
    public Button resumeButton;
    public Button pauseSettingsButton;
    public Button returnToMainMenuButton;
    public Button pauseQuitButton;

    [Header("--- SETTINGS UI REFERENCES ---")]
    public Button settingsBackButton;
    public Button tabAudioBtn;
    public Button tabGraphicsBtn;
    public Button tabControlsBtn;
    public GameObject audioSection;
    public GameObject graphicsSection;
    public GameObject controlsSection;

    [Header("Audio Sliders")]
    public Slider masterVolumeSlider;
    public TextMeshProUGUI masterVolumeValText;
    public Slider musicVolumeSlider;
    public TextMeshProUGUI musicVolumeValText;
    public Slider sfxVolumeSlider;
    public TextMeshProUGUI sfxVolumeValText;
    public Slider ambienceVolumeSlider;
    public TextMeshProUGUI ambienceVolumeValText;
    public Slider uiVolumeSlider;
    public TextMeshProUGUI uiVolumeValText;

    [Header("Graphics Selectors")]
    public TMP_Dropdown qualityDropdown;
    public TMP_Dropdown fullscreenDropdown;
    public TMP_Dropdown resolutionDropdown;
    public Toggle vsyncToggle;
    public TMP_Dropdown fpsLimitDropdown;

    [Header("Controls Selectors")]
    public Slider mouseSensSlider;
    public TextMeshProUGUI mouseSensValText;
    public Toggle invertYToggle;

    private GameFlowState currentState = GameFlowState.MainMenu;
    public GameFlowState CurrentState => currentState;

    private bool openedSettingsFromPause = false;
    private Coroutine activeTransitionCoroutine;

    public bool IsMenuOrPauseOpen =>
        currentState == GameFlowState.MainMenu ||
        currentState == GameFlowState.Paused ||
        currentState == GameFlowState.Settings ||
        currentState == GameFlowState.Transitioning;

    public static bool IsActiveMenuOpen()
    {
        return instance != null && instance.IsMenuOrPauseOpen;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitializeOnSceneLoad()
    {
        EnsureInstance();
    }

    public static GameMenuManager EnsureInstance()
    {
        if (Instance != null)
        {
            Instance.EnsureUI();
            return Instance;
        }
        return null;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        EnsureReferences();
        EnsureUI();
    }

    private void Start()
    {
        EnsureReferences();
        EnsureUI();
        BindButtonsAndEvents();
        InitializeSettingsUI();

        if (startInMainMenu)
        {
            InitializeMainMenu();
        }
        else
        {
            InitializeDirectGameplay();
        }
    }

    public void EnsureReferences()
    {
        if (menuCameraController == null)
        {
            menuCameraController = UnityEngine.Object.FindAnyObjectByType<MainMenuCameraController>(FindObjectsInactive.Include);
            if (menuCameraController == null)
            {
                GameObject camHolder = new GameObject("MainMenu_Camera_Controller");
                menuCameraController = camHolder.AddComponent<MainMenuCameraController>();
            }
        }
    }

    public void EnsureUI()
    {
        Canvas canvas = GetComponentInParent<Canvas>() ?? UnityEngine.Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        canvas.enabled = true;
        canvas.gameObject.SetActive(true);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // Check if existing in canvas children
        if (mainMenuPanel == null)
        {
            Transform mmp = canvas.transform.Find("MainMenuPanel");
            if (mmp != null) mainMenuPanel = mmp.gameObject;
        }

        if (pauseMenuPanel == null)
        {
            Transform pmp = canvas.transform.Find("PauseMenuPanel");
            if (pmp != null) pauseMenuPanel = pmp.gameObject;
        }

        if (settingsPanel == null)
        {
            Transform sp = canvas.transform.Find("SettingsPanel");
            if (sp != null) settingsPanel = sp.gameObject;
        }

        if (fadeOverlayCanvasGroup == null)
        {
            Transform fp = canvas.transform.Find("FadeOverlayPanel");
            if (fp != null) fadeOverlayCanvasGroup = fp.GetComponent<CanvasGroup>();
        }

        // If not found, build runtime UI hierarchy
        if (mainMenuPanel == null || pauseMenuPanel == null || settingsPanel == null || fadeOverlayCanvasGroup == null)
        {
            BuildRuntimeUI(canvas);
        }
        else
        {
            FetchChildReferences();
        }
    }

    private void FetchChildReferences()
    {
        if (mainMenuPanel != null)
        {
            mainMenuTitleText = mainMenuPanel.transform.Find("LeftContentCard/TitleHeader")?.GetComponent<TextMeshProUGUI>();
            mainMenuSaveInfoText = mainMenuPanel.transform.Find("LeftContentCard/SaveInfoBadge/SaveInfoText")?.GetComponent<TextMeshProUGUI>();
            playButton = mainMenuPanel.transform.Find("LeftContentCard/ButtonsColumn/PlayButton")?.GetComponent<Button>();
            mainMenuSettingsButton = mainMenuPanel.transform.Find("LeftContentCard/ButtonsColumn/SettingsButton")?.GetComponent<Button>();
            mainMenuQuitButton = mainMenuPanel.transform.Find("LeftContentCard/ButtonsColumn/QuitButton")?.GetComponent<Button>();
        }

        if (pauseMenuPanel != null)
        {
            resumeButton = pauseMenuPanel.transform.Find("PauseCard/PauseButtons/ResumeBtn")?.GetComponent<Button>();
            pauseSettingsButton = pauseMenuPanel.transform.Find("PauseCard/PauseButtons/SettingsBtn")?.GetComponent<Button>();
            returnToMainMenuButton = pauseMenuPanel.transform.Find("PauseCard/PauseButtons/MainMenuBtn")?.GetComponent<Button>();
            pauseQuitButton = pauseMenuPanel.transform.Find("PauseCard/PauseButtons/QuitBtn")?.GetComponent<Button>();
        }

        if (settingsPanel != null)
        {
            settingsBackButton = settingsPanel.transform.Find("SettingsCard/BackButtonHolder/SettingsBackBtn")?.GetComponent<Button>();
            tabAudioBtn = settingsPanel.transform.Find("SettingsCard/TabRow/TabAudio")?.GetComponent<Button>();
            tabGraphicsBtn = settingsPanel.transform.Find("SettingsCard/TabRow/TabGraphics")?.GetComponent<Button>();
            tabControlsBtn = settingsPanel.transform.Find("SettingsCard/TabRow/TabControls")?.GetComponent<Button>();

            audioSection = settingsPanel.transform.Find("SettingsCard/ContentArea/AudioSection")?.gameObject;
            graphicsSection = settingsPanel.transform.Find("SettingsCard/ContentArea/GraphicsSection")?.gameObject;
            controlsSection = settingsPanel.transform.Find("SettingsCard/ContentArea/ControlsSection")?.gameObject;

            if (audioSection != null)
            {
                masterVolumeSlider = audioSection.transform.Find("MasterVolRow/Slider")?.GetComponent<Slider>();
                masterVolumeValText = audioSection.transform.Find("MasterVolRow/ValText")?.GetComponent<TextMeshProUGUI>();
                musicVolumeSlider = audioSection.transform.Find("MusicVolRow/Slider")?.GetComponent<Slider>();
                musicVolumeValText = audioSection.transform.Find("MusicVolRow/ValText")?.GetComponent<TextMeshProUGUI>();
                sfxVolumeSlider = audioSection.transform.Find("SfxVolRow/Slider")?.GetComponent<Slider>();
                sfxVolumeValText = audioSection.transform.Find("SfxVolRow/ValText")?.GetComponent<TextMeshProUGUI>();
                ambienceVolumeSlider = audioSection.transform.Find("AmbienceVolRow/Slider")?.GetComponent<Slider>();
                ambienceVolumeValText = audioSection.transform.Find("AmbienceVolRow/ValText")?.GetComponent<TextMeshProUGUI>();
                uiVolumeSlider = audioSection.transform.Find("UiVolRow/Slider")?.GetComponent<Slider>();
                uiVolumeValText = audioSection.transform.Find("UiVolRow/ValText")?.GetComponent<TextMeshProUGUI>();
            }

            if (graphicsSection != null)
            {
                qualityDropdown = graphicsSection.transform.Find("QualityRow/Dropdown")?.GetComponent<TMP_Dropdown>();
                fullscreenDropdown = graphicsSection.transform.Find("FullscreenRow/Dropdown")?.GetComponent<TMP_Dropdown>();
                resolutionDropdown = graphicsSection.transform.Find("ResolutionRow/Dropdown")?.GetComponent<TMP_Dropdown>();
                vsyncToggle = graphicsSection.transform.Find("VsyncRow/Toggle")?.GetComponent<Toggle>();
                fpsLimitDropdown = graphicsSection.transform.Find("FpsLimitRow/Dropdown")?.GetComponent<TMP_Dropdown>();
            }

            if (controlsSection != null)
            {
                mouseSensSlider = controlsSection.transform.Find("MouseSensRow/Slider")?.GetComponent<Slider>();
                mouseSensValText = controlsSection.transform.Find("MouseSensRow/ValText")?.GetComponent<TextMeshProUGUI>();
                invertYToggle = controlsSection.transform.Find("InvertYRow/Toggle")?.GetComponent<Toggle>();
            }
        }
    }

    private void BuildRuntimeUI(Canvas canvas)
    {
        // 1. Load Font asset if available
        TMP_FontAsset fontAsset = Resources.Load<TMP_FontAsset>("Fonts/Inter-VariableFont_opsz,wght SDF");
        if (fontAsset == null)
        {
            TMP_FontAsset[] allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            if (allFonts != null && allFonts.Length > 0) fontAsset = allFonts[0];
        }

        // 1. Main Menu Panel
        if (mainMenuPanel == null)
        {
            GameObject mp = CreatePanel(canvas.transform, "MainMenuPanel");
            Image mpBg = mp.AddComponent<Image>();
            mpBg.color = new Color(0.02f, 0.03f, 0.05f, 0.40f);

            GameObject mainCard = CreateElement("LeftContentCard", mp.transform, new Vector2(0.08f, 0.5f), new Vector2(0.08f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(560, 660));
            Image cardImg = mainCard.AddComponent<Image>();
            cardImg.color = new Color(0.06f, 0.09f, 0.14f, 0.94f);

            Outline cardOutline = mainCard.AddComponent<Outline>();
            cardOutline.effectColor = new Color(0.2f, 0.8f, 1f, 0.5f);
            cardOutline.effectDistance = new Vector2(2, -2);

            GameObject titleObj = CreateElement("TitleHeader", mainCard.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(500, 90));
            mainMenuTitleText = titleObj.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) mainMenuTitleText.font = fontAsset;
            mainMenuTitleText.text = "<size=130%><b>VALLEY LOGISTICS</b></size>\n<size=55%><color=#32FFFF>✦ KARGO DAĞITIM VE SÜRÜŞ SİMÜLASYONU ✦</color></size>";
            mainMenuTitleText.fontSize = 28;
            mainMenuTitleText.alignment = TextAlignmentOptions.Center;
            mainMenuTitleText.color = Color.white;

            GameObject saveBadge = CreateElement("SaveInfoBadge", mainCard.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -135), new Vector2(480, 50));
            Image badgeImg = saveBadge.AddComponent<Image>();
            badgeImg.color = new Color(0.08f, 0.12f, 0.18f, 0.90f);

            GameObject saveInfoObj = CreateElement("SaveInfoText", saveBadge.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            mainMenuSaveInfoText = saveInfoObj.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) mainMenuSaveInfoText.font = fontAsset;
            mainMenuSaveInfoText.text = "<color=#A0C8FF>Mevcut Şube:</color> <color=#FFFFFF>Lv.1</color>   •   <color=#A0C8FF>Kasa:</color> <color=#32FF64>$500</color>";
            mainMenuSaveInfoText.fontSize = 17;
            mainMenuSaveInfoText.alignment = TextAlignmentOptions.Center;

            GameObject btnCol = CreateElement("ButtonsColumn", mainCard.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(440, 360));
            VerticalLayoutGroup vlg = btnCol.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 18f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            playButton = CreateButton("PlayButton", btnCol.transform, "▶  OYUNA BAŞLA / DEVAM ET", 58, new Color(0.06f, 0.28f, 0.15f, 0.95f), new Color(0.2f, 1.0f, 0.45f), fontAsset);
            mainMenuSettingsButton = CreateButton("SettingsButton", btnCol.transform, "⚙  AYARLAR", 50, new Color(0.09f, 0.12f, 0.18f, 0.95f), Color.white, fontAsset);
            mainMenuQuitButton = CreateButton("QuitButton", btnCol.transform, "⏻  ÇIKIŞ", 50, new Color(0.35f, 0.08f, 0.08f, 0.95f), new Color(1f, 0.35f, 0.35f), fontAsset);

            mainMenuPanel = mp;
        }

        // 2. Pause Menu Panel
        if (pauseMenuPanel == null)
        {
            GameObject pp = CreatePanel(canvas.transform, "PauseMenuPanel");
            Image ppBg = pp.AddComponent<Image>();
            ppBg.color = new Color(0.02f, 0.03f, 0.06f, 0.85f);

            GameObject pCard = CreateElement("PauseCard", pp.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(480, 520));
            Image pCardImg = pCard.AddComponent<Image>();
            pCardImg.color = new Color(0.07f, 0.10f, 0.16f, 0.96f);

            Outline pOutline = pCard.AddComponent<Outline>();
            pOutline.effectColor = new Color(0.2f, 0.8f, 1f, 0.45f);
            pOutline.effectDistance = new Vector2(2, -2);

            GameObject pTitle = CreateElement("PauseTitle", pCard.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -45), new Vector2(400, 50));
            TextMeshProUGUI pTitleText = pTitle.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) pTitleText.font = fontAsset;
            pTitleText.text = "<b>OYUN DURAKLATILDI</b>\n<size=55%><color=#32FFFF>✦ GAME PAUSED ✦</color></size>";
            pTitleText.fontSize = 24;
            pTitleText.alignment = TextAlignmentOptions.Center;
            pTitleText.color = Color.white;

            GameObject pBtnCol = CreateElement("PauseButtons", pCard.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -35), new Vector2(380, 320));
            VerticalLayoutGroup pvlg = pBtnCol.AddComponent<VerticalLayoutGroup>();
            pvlg.spacing = 16f;
            pvlg.childAlignment = TextAnchor.MiddleCenter;
            pvlg.childControlWidth = true;
            pvlg.childControlHeight = false;

            resumeButton = CreateButton("ResumeBtn", pBtnCol.transform, "▶  DEVAM ET", 54, new Color(0.08f, 0.22f, 0.35f, 0.95f), new Color(0.2f, 0.9f, 1.0f), fontAsset);
            pauseSettingsButton = CreateButton("SettingsBtn", pBtnCol.transform, "⚙  AYARLAR", 48, new Color(0.09f, 0.12f, 0.18f, 0.95f), Color.white, fontAsset);
            returnToMainMenuButton = CreateButton("MainMenuBtn", pBtnCol.transform, "🏠  ANA MENÜYE DÖN", 48, new Color(0.09f, 0.12f, 0.18f, 0.95f), new Color(1.0f, 0.8f, 0.4f), fontAsset);
            pauseQuitButton = CreateButton("QuitBtn", pBtnCol.transform, "⏻  MASAÜSTÜNE ÇIK", 48, new Color(0.35f, 0.08f, 0.08f, 0.95f), new Color(1f, 0.35f, 0.35f), fontAsset);

            pauseMenuPanel = pp;
            pp.SetActive(false);
        }

        // 3. Settings Panel
        if (settingsPanel == null)
        {
            GameObject sp = CreatePanel(canvas.transform, "SettingsPanel");
            Image spBg = sp.AddComponent<Image>();
            spBg.color = new Color(0.02f, 0.03f, 0.06f, 0.90f);

            GameObject sCard = CreateElement("SettingsCard", sp.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920, 680));
            Image sCardImg = sCard.AddComponent<Image>();
            sCardImg.color = new Color(0.06f, 0.08f, 0.13f, 0.98f);

            Outline sOutline = sCard.AddComponent<Outline>();
            sOutline.effectColor = new Color(0.2f, 0.8f, 1f, 0.45f);
            sOutline.effectDistance = new Vector2(2, -2);

            GameObject sTitle = CreateElement("SettingsTitle", sCard.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(600, 45));
            TextMeshProUGUI sTitleText = sTitle.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) sTitleText.font = fontAsset;
            sTitleText.text = "<b>⚙  AYARLAR • SETTINGS</b>";
            sTitleText.fontSize = 28;
            sTitleText.alignment = TextAlignmentOptions.Center;
            sTitleText.color = Color.white;

            GameObject tabRow = CreateElement("TabRow", sCard.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -95), new Vector2(800, 45));
            HorizontalLayoutGroup thlg = tabRow.AddComponent<HorizontalLayoutGroup>();
            thlg.spacing = 15f;
            thlg.childControlWidth = true;
            thlg.childControlHeight = true;

            tabAudioBtn = CreateButton("TabAudio", tabRow.transform, "🔊  SES", 45, new Color(0.09f, 0.12f, 0.18f, 0.95f), Color.white, fontAsset);
            tabGraphicsBtn = CreateButton("TabGraphics", tabRow.transform, "🖥  GRAFİK", 45, new Color(0.09f, 0.12f, 0.18f, 0.95f), Color.white, fontAsset);
            tabControlsBtn = CreateButton("TabControls", tabRow.transform, "🎮  KONTROLLER", 45, new Color(0.09f, 0.12f, 0.18f, 0.95f), Color.white, fontAsset);

            GameObject contentArea = CreateElement("ContentArea", sCard.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(820, 430));

            // Audio Section
            audioSection = CreateElement("AudioSection", contentArea.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            VerticalLayoutGroup avlg = audioSection.AddComponent<VerticalLayoutGroup>();
            avlg.spacing = 12f;
            avlg.childControlWidth = true;
            avlg.childControlHeight = false;

            masterVolumeSlider = CreateSlider("MasterVolRow", audioSection.transform, "Ana Ses (Master):", out masterVolumeValText, fontAsset);
            musicVolumeSlider = CreateSlider("MusicVolRow", audioSection.transform, "Müzik Ses Şiddeti:", out musicVolumeValText, fontAsset);
            sfxVolumeSlider = CreateSlider("SfxVolRow", audioSection.transform, "Ses Efektleri (SFX):", out sfxVolumeValText, fontAsset);
            ambienceVolumeSlider = CreateSlider("AmbienceVolRow", audioSection.transform, "Çevre Atmosferi:", out ambienceVolumeValText, fontAsset);
            uiVolumeSlider = CreateSlider("UiVolRow", audioSection.transform, "Arayüz & Bildirimler:", out uiVolumeValText, fontAsset);

            // Graphics Section
            graphicsSection = CreateElement("GraphicsSection", contentArea.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            VerticalLayoutGroup gvlg = graphicsSection.AddComponent<VerticalLayoutGroup>();
            gvlg.spacing = 12f;
            gvlg.childControlWidth = true;
            gvlg.childControlHeight = false;

            qualityDropdown = CreateDropdown("QualityRow", graphicsSection.transform, "Grafik Kalitesi:", fontAsset);
            fullscreenDropdown = CreateDropdown("FullscreenRow", graphicsSection.transform, "Ekran Modu:", fontAsset);
            resolutionDropdown = CreateDropdown("ResolutionRow", graphicsSection.transform, "Çözünürlük:", fontAsset);
            vsyncToggle = CreateToggle("VsyncRow", graphicsSection.transform, "Dikey Senkronizasyon (V-Sync):", fontAsset);
            fpsLimitDropdown = CreateDropdown("FpsLimitRow", graphicsSection.transform, "Hedef FPS Limiti:", fontAsset);
            graphicsSection.SetActive(false);

            // Controls Section
            controlsSection = CreateElement("ControlsSection", contentArea.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            VerticalLayoutGroup cvlg = controlsSection.AddComponent<VerticalLayoutGroup>();
            cvlg.spacing = 12f;
            cvlg.childControlWidth = true;
            cvlg.childControlHeight = false;

            mouseSensSlider = CreateSlider("MouseSensRow", controlsSection.transform, "Fare Bakış Hassasiyeti:", out mouseSensValText, fontAsset, 0.2f, 5.0f);
            invertYToggle = CreateToggle("InvertYRow", controlsSection.transform, "Fare Y-Ekseni Ters Çevir:", fontAsset);

            GameObject keyCard = CreateElement("KeybindingsCard", controlsSection.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800, 220));
            Image kcImg = keyCard.AddComponent<Image>();
            kcImg.color = new Color(0.08f, 0.11f, 0.17f, 0.90f);

            GameObject keyInfo = CreateElement("KeyInfoText", keyCard.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI keyText = keyInfo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) keyText.font = fontAsset;
            keyText.text = "<color=#32FFFF><b>KONTROL ŞEMASI / KEYBINDINGS:</b></color>\n\n" +
                "• <b>W / A / S / D :</b> Yürüme / Araç Sürüş & Direksiyon\n" +
                "• <b>E :</b> Dükkan, Koli ve Kapı Etkileşimi / Kargo Al\n" +
                "• <b>F :</b> Araca Bin / Araçtan İn\n" +
                "• <b>TAB / M :</b> Kargo Tableti & Haritayı Aç/Kapat\n" +
                "• <b>Sol Tık :</b> Koli Fırlat (Basılı Tut = Güçlü Fırlat)\n" +
                "• <b>Sağ Tık :</b> Koliyi Yavaşça Bırak   |   <b>Shift :</b> Koşma\n" +
                "• <b>Boşluk (Space) :</b> Zıplama / Araç El Freni   |   <b>ESC :</b> Duraklatma";
            keyText.fontSize = 16;
            keyText.alignment = TextAlignmentOptions.Center;
            keyText.lineSpacing = 18;
            controlsSection.SetActive(false);

            GameObject backBtnObj = CreateElement("BackButtonHolder", sCard.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 35), new Vector2(300, 48));
            settingsBackButton = CreateButton("SettingsBackBtn", backBtnObj.transform, "✔  KAYDET VE GERİ DÖN", 48, new Color(0.08f, 0.22f, 0.35f, 0.95f), new Color(0.2f, 0.9f, 1f), fontAsset);

            settingsPanel = sp;
            sp.SetActive(false);
        }

        // 4. Fade Overlay Panel
        if (fadeOverlayCanvasGroup == null)
        {
            GameObject fadePanel = CreatePanel(canvas.transform, "FadeOverlayPanel");
            Image fadeImg = fadePanel.AddComponent<Image>();
            fadeImg.color = Color.black;
            fadeOverlayCanvasGroup = fadePanel.AddComponent<CanvasGroup>();
            fadeOverlayCanvasGroup.alpha = 0f;
            fadeOverlayCanvasGroup.blocksRaycasts = false;
            fadeOverlayCanvasGroup.interactable = false;
            fadePanel.SetActive(false);
        }

        Debug.Log("<color=#32FF64><b>[GameMenuManager]</b> Ana Menü, Pause Menüsü ve Ayarlar arayüzleri hazırlandı.</color>");
    }

    private static GameObject CreatePanel(Transform parent, string name)
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

    private static GameObject CreateElement(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
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

    private static Button CreateButton(string name, Transform parent, string label, float height, Color bgColor, Color textColor, TMP_FontAsset font)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, height);
        rt.localScale = Vector3.one;

        Image img = btnObj.AddComponent<Image>();
        img.color = bgColor;

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

    private static Slider CreateSlider(string name, Transform parent, string label, out TextMeshProUGUI valTextOut, TMP_FontAsset font, float minVal = 0f, float maxVal = 1f)
    {
        GameObject row = new GameObject(name, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        RectTransform rrt = row.GetComponent<RectTransform>();
        rrt.sizeDelta = new Vector2(0, 42);
        rrt.localScale = Vector3.one;

        GameObject lblObj = CreateElement("Label", row.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10, 0), new Vector2(280, 36));
        TextMeshProUGUI lbl = lblObj.AddComponent<TextMeshProUGUI>();
        if (font != null) lbl.font = font;
        lbl.text = label;
        lbl.fontSize = 17;
        lbl.alignment = TextAlignmentOptions.Left;
        lbl.color = new Color(0.85f, 0.92f, 1f);

        GameObject valObj = CreateElement("ValText", row.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-10, 0), new Vector2(70, 36));
        TextMeshProUGUI valText = valObj.AddComponent<TextMeshProUGUI>();
        if (font != null) valText.font = font;
        valText.text = "%100";
        valText.fontSize = 17;
        valText.alignment = TextAlignmentOptions.Right;
        valText.color = new Color(0.3f, 1f, 0.6f);
        valTextOut = valText;

        GameObject sliderObj = CreateElement("Slider", row.transform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(100, 0), new Vector2(-380, 24));
        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = minVal;
        slider.maxValue = maxVal;

        GameObject bgTrack = CreateElement("Background", sliderObj.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image bgImg = bgTrack.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.14f, 0.20f, 0.95f);

        GameObject fillArea = CreateElement("Fill Area", sliderObj.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        GameObject fill = CreateElement("Fill", fillArea.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.2f, 0.8f, 1.0f, 0.95f);

        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.targetGraphic = bgImg;

        return slider;
    }

    private static TMP_Dropdown CreateDropdown(string name, Transform parent, string label, TMP_FontAsset font)
    {
        GameObject row = new GameObject(name, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        RectTransform rrt = row.GetComponent<RectTransform>();
        rrt.sizeDelta = new Vector2(0, 42);
        rrt.localScale = Vector3.one;

        GameObject lblObj = CreateElement("Label", row.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10, 0), new Vector2(280, 36));
        TextMeshProUGUI lbl = lblObj.AddComponent<TextMeshProUGUI>();
        if (font != null) lbl.font = font;
        lbl.text = label;
        lbl.fontSize = 17;
        lbl.alignment = TextAlignmentOptions.Left;
        lbl.color = new Color(0.85f, 0.92f, 1f);

        GameObject ddObj = CreateElement("Dropdown", row.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-10, 0), new Vector2(360, 38));
        Image ddImg = ddObj.AddComponent<Image>();
        ddImg.color = new Color(0.10f, 0.14f, 0.22f, 0.95f);

        TMP_Dropdown dd = ddObj.AddComponent<TMP_Dropdown>();
        GameObject captionObj = CreateElement("Label", ddObj.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-20, 0));
        TextMeshProUGUI caption = captionObj.AddComponent<TextMeshProUGUI>();
        if (font != null) caption.font = font;
        caption.fontSize = 16;
        caption.alignment = TextAlignmentOptions.Left;
        caption.color = Color.white;
        dd.captionText = caption;

        return dd;
    }

    private static Toggle CreateToggle(string name, Transform parent, string label, TMP_FontAsset font)
    {
        GameObject row = new GameObject(name, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        RectTransform rrt = row.GetComponent<RectTransform>();
        rrt.sizeDelta = new Vector2(0, 42);
        rrt.localScale = Vector3.one;

        GameObject lblObj = CreateElement("Label", row.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10, 0), new Vector2(380, 36));
        TextMeshProUGUI lbl = lblObj.AddComponent<TextMeshProUGUI>();
        if (font != null) lbl.font = font;
        lbl.text = label;
        lbl.fontSize = 17;
        lbl.alignment = TextAlignmentOptions.Left;
        lbl.color = new Color(0.85f, 0.92f, 1f);

        GameObject toggleObj = CreateElement("Toggle", row.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-10, 0), new Vector2(38, 38));
        Image bgImg = toggleObj.AddComponent<Image>();
        bgImg.color = new Color(0.10f, 0.14f, 0.22f, 0.95f);

        Toggle toggle = toggleObj.AddComponent<Toggle>();
        GameObject checkObj = CreateElement("Checkmark", toggleObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24, 24));
        Image checkImg = checkObj.AddComponent<Image>();
        checkImg.color = new Color(0.2f, 0.9f, 1.0f);

        toggle.graphic = checkImg;
        toggle.targetGraphic = bgImg;

        return toggle;
    }

    #region --- INITIALIZATION MODES ---

    private void InitializeMainMenu()
    {
        currentState = GameFlowState.MainMenu;
        Time.timeScale = 1.0f;

        // 1. Activate Menu Camera overlooking shop on the player camera
        if (MainMenuCameraController.Instance != null)
        {
            MainMenuCameraController.Instance.SetMenuMode(true);
        }

        // 2. Disable FPS Player controls
        FPSPlayerController player = FPSPlayerController.Instance != null ? FPSPlayerController.Instance : UnityEngine.Object.FindAnyObjectByType<FPSPlayerController>();
        if (player != null)
        {
            player.SetOnFootActive(false);
        }

        // 3. Hide HUD
        if (MainHUDController.Instance != null)
        {
            MainHUDController.Instance.SetHUDVisible(false);
        }

        // 4. Update Main Menu save stats
        UpdateMainMenuSaveStats();

        // 5. Setup UI Panels
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
            mainMenuPanel.transform.SetAsLastSibling(); // BRING TO FRONT OF CANVAS
        }
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (fadeOverlayCanvasGroup != null)
        {
            fadeOverlayCanvasGroup.alpha = 0f;
            fadeOverlayCanvasGroup.blocksRaycasts = false;
            fadeOverlayCanvasGroup.interactable = false;
            fadeOverlayCanvasGroup.gameObject.SetActive(false);
        }

        // 6. Free cursor for menu navigation
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void InitializeDirectGameplay()
    {
        currentState = GameFlowState.Playing;
        Time.timeScale = 1.0f;

        if (menuCameraController != null)
        {
            menuCameraController.SetMenuMode(false);
        }

        FPSPlayerController player = FPSPlayerController.Instance != null ? FPSPlayerController.Instance : UnityEngine.Object.FindAnyObjectByType<FPSPlayerController>();
        if (player != null)
        {
            if (player.playerCamera != null) player.playerCamera.enabled = true;
            player.SetOnFootActive(true);
            FPSPlayerController.LockCursor(true);
        }

        if (MainHUDController.Instance != null)
        {
            MainHUDController.Instance.SetHUDVisible(true);
        }

        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    public void UpdateMainMenuSaveStats()
    {
        if (mainMenuSaveInfoText == null) return;

        int cash = PlayerPrefs.GetInt("Delivery_PlayerCash", 500);
        int level = PlayerPrefs.GetInt("Delivery_BranchLevel", 1);
        string tierName = BranchManager.Instance != null && BranchManager.Instance.CurrentTier != null ?
            BranchManager.Instance.CurrentTier.tierName : $"Seviye {level}";

        mainMenuSaveInfoText.text = $"<color=#A0C8FF>Mevcut Şube:</color> <color=#FFFFFF>Lv.{level} ({tierName})</color>   •   <color=#A0C8FF>Kasa Bakiyesi:</color> <color=#32FF64>${cash:N0}</color>";
    }

    #endregion

    #region --- INPUT & UPDATE ---

    private void Update()
    {
        // Enforce visible mouse cursor if in any menu
        if (IsMenuOrPauseOpen && currentState != GameFlowState.Playing)
        {
            if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        HandleInput();
    }

    private void HandleInput()
    {
        bool escPressed = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            escPressed = true;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            escPressed = true;
        }
#endif

        if (!escPressed) return;

        if (currentState == GameFlowState.Playing)
        {
            // Do not intercept if tablet, garage, modal, or day summary is open (they close themselves first)
            if (CargoTabletUI.Instance != null && CargoTabletUI.Instance.IsTabletOpen) return;
            if (CommercialHubUIManager.Instance != null && CommercialHubUIManager.Instance.IsAnyPanelOpen) return;
            if (DaySummaryManager.Instance != null && DaySummaryManager.Instance.IsSummaryOpen) return;
            if (BranchUpgradeTransitionUI.IsTransitioning) return;

            PauseGame();
        }
        else if (currentState == GameFlowState.Paused)
        {
            ResumeGame();
        }
        else if (currentState == GameFlowState.Settings)
        {
            CloseSettings();
        }
    }

    #endregion

    #region --- FLOW ACTIONS (PLAY, PAUSE, RESUME, RETURN, QUIT) ---

    public void OnPlayButtonClicked()
    {
        if (currentState == GameFlowState.Transitioning) return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }

        PlayTransitionSequence(
            onBlackout: () =>
            {
                // 1. Switch to Playing state
                currentState = GameFlowState.Playing;
                Time.timeScale = 1.0f;

                // 2. Restore Camera directly to player
                if (MainMenuCameraController.Instance != null)
                {
                    MainMenuCameraController.Instance.SetMenuMode(false);
                }

                // 3. Enable FPS Player Controls
                FPSPlayerController player = FPSPlayerController.Instance != null ? FPSPlayerController.Instance : UnityEngine.Object.FindAnyObjectByType<FPSPlayerController>();
                if (player != null)
                {
                    player.SetOnFootActive(true);
                    FPSPlayerController.LockCursor(true);
                }

                // 4. Hide Main Menu Panel, Show HUD
                if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
                if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
                if (settingsPanel != null) settingsPanel.SetActive(false);

                if (MainHUDController.Instance != null)
                {
                    MainHUDController.Instance.SetHUDVisible(true);
                    MainHUDController.Instance.RefreshAll();
                }
            },
            onComplete: () =>
            {
                currentState = GameFlowState.Playing;
                FPSPlayerController.LockCursor(true);
            }
        );
    }

    public void PauseGame()
    {
        if (currentState != GameFlowState.Playing) return;

        currentState = GameFlowState.Paused;
        Time.timeScale = 0f;

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
            pauseMenuPanel.transform.SetAsLastSibling();
        }
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayTabletOpen();
        }
    }

    public void ResumeGame()
    {
        if (currentState != GameFlowState.Paused && currentState != GameFlowState.Settings) return;

        currentState = GameFlowState.Playing;
        Time.timeScale = 1.0f;

        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);

        FPSPlayerController.LockCursor(true);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayTabletClose();
        }
    }

    public void ReturnToMainMenu()
    {
        if (currentState == GameFlowState.Transitioning) return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }

        PlayTransitionSequence(
            onBlackout: () =>
            {
                currentState = GameFlowState.MainMenu;
                Time.timeScale = 1.0f;

                // 1. Disable Player Controls & Cargo/Vehicle state
                FPSPlayerController player = FPSPlayerController.Instance != null ? FPSPlayerController.Instance : UnityEngine.Object.FindAnyObjectByType<FPSPlayerController>();
                if (player != null)
                {
                    if (player.grabber != null && player.grabber.IsHoldingObject)
                    {
                        player.grabber.ReleaseObject(Vector3.zero);
                    }
                    if (!player.IsOnFoot && player.currentVehicle != null)
                    {
                        player.currentVehicle.ExitVehicle();
                    }
                    player.SetOnFootActive(false);
                }

                // 2. Set Menu Camera pose on player camera
                if (MainMenuCameraController.Instance != null)
                {
                    MainMenuCameraController.Instance.SetMenuMode(true);
                }

                // 3. Hide HUD
                if (MainHUDController.Instance != null)
                {
                    MainHUDController.Instance.SetHUDVisible(false);
                }

                // 4. Show Main Menu UI
                UpdateMainMenuSaveStats();
                if (mainMenuPanel != null)
                {
                    mainMenuPanel.SetActive(true);
                    mainMenuPanel.transform.SetAsLastSibling();
                }
                if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
                if (settingsPanel != null) settingsPanel.SetActive(false);

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            },
            onComplete: () =>
            {
                currentState = GameFlowState.MainMenu;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        );
    }

    public void QuitGame()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }

        Debug.Log("[GameMenuManager] Quitting application...");
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion

    #region --- SETTINGS MODAL & TABS ---

    public void OpenSettings(bool fromPause)
    {
        openedSettingsFromPause = fromPause;
        currentState = GameFlowState.Settings;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }

        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            settingsPanel.transform.SetAsLastSibling();
        }

        ShowSettingsTab(0); // Default to Audio tab
        SyncSettingsValuesToUI();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseSettings()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SaveAllSettings();
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }

        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (openedSettingsFromPause)
        {
            currentState = GameFlowState.Paused;
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(true);
                pauseMenuPanel.transform.SetAsLastSibling();
            }
        }
        else
        {
            currentState = GameFlowState.MainMenu;
            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(true);
                mainMenuPanel.transform.SetAsLastSibling();
            }
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ShowSettingsTab(int tabIndex)
    {
        if (audioSection != null) audioSection.SetActive(tabIndex == 0);
        if (graphicsSection != null) graphicsSection.SetActive(tabIndex == 1);
        if (controlsSection != null) controlsSection.SetActive(tabIndex == 2);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayTabSwitch();
        }
    }

    private void InitializeSettingsUI()
    {
        if (SettingsManager.Instance == null) return;

        // Populate Resolution Dropdown
        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();
            List<string> options = new List<string>();
            int currentResIdx = 0;

            Resolution[] resList = SettingsManager.Instance.AvailableResolutions;
            if (resList != null && resList.Length > 0)
            {
                for (int i = 0; i < resList.Length; i++)
                {
                    string option = $"{resList[i].width} x {resList[i].height} @ {(int)Math.Round(resList[i].refreshRateRatio.value)}Hz";
                    options.Add(option);
                    if (resList[i].width == Screen.currentResolution.width &&
                        resList[i].height == Screen.currentResolution.height)
                    {
                        currentResIdx = i;
                    }
                }
                resolutionDropdown.AddOptions(options);
                resolutionDropdown.value = SettingsManager.Instance.resolutionIndex >= 0 ? SettingsManager.Instance.resolutionIndex : currentResIdx;
                resolutionDropdown.RefreshShownValue();
            }
        }

        // Populate Quality Dropdown
        if (qualityDropdown != null)
        {
            qualityDropdown.ClearOptions();
            List<string> qOptions = new List<string>(QualitySettings.names);
            qualityDropdown.AddOptions(qOptions);
            qualityDropdown.value = SettingsManager.Instance.qualityLevel;
            qualityDropdown.RefreshShownValue();
        }

        // Populate Fullscreen Dropdown
        if (fullscreenDropdown != null)
        {
            fullscreenDropdown.ClearOptions();
            fullscreenDropdown.AddOptions(new List<string> { "Tam Ekran (Exclusive)", "Kenarlıksız (Borderless)", "Pencereli (Windowed)" });
            fullscreenDropdown.value = SettingsManager.Instance.fullscreenMode;
            fullscreenDropdown.RefreshShownValue();
        }

        // Populate Target FPS Dropdown
        if (fpsLimitDropdown != null)
        {
            fpsLimitDropdown.ClearOptions();
            fpsLimitDropdown.AddOptions(new List<string> { "30 FPS", "60 FPS", "120 FPS", "144 FPS", "Sınırsız (Unlimited)" });
            int curFps = SettingsManager.Instance.targetFps;
            if (curFps == 30) fpsLimitDropdown.value = 0;
            else if (curFps == 60) fpsLimitDropdown.value = 1;
            else if (curFps == 120) fpsLimitDropdown.value = 2;
            else if (curFps == 144) fpsLimitDropdown.value = 3;
            else fpsLimitDropdown.value = 4;
            fpsLimitDropdown.RefreshShownValue();
        }

        SyncSettingsValuesToUI();
    }

    private void SyncSettingsValuesToUI()
    {
        if (SettingsManager.Instance == null) return;
        SettingsManager sm = SettingsManager.Instance;

        // 1. Audio
        if (masterVolumeSlider != null) masterVolumeSlider.value = sm.masterVolume;
        if (masterVolumeValText != null) masterVolumeValText.text = $"%{(int)(sm.masterVolume * 100)}";

        if (musicVolumeSlider != null) musicVolumeSlider.value = sm.musicVolume;
        if (musicVolumeValText != null) musicVolumeValText.text = $"%{(int)(sm.musicVolume * 100)}";

        if (sfxVolumeSlider != null) sfxVolumeSlider.value = sm.sfxVolume;
        if (sfxVolumeValText != null) sfxVolumeValText.text = $"%{(int)(sm.sfxVolume * 100)}";

        if (ambienceVolumeSlider != null) ambienceVolumeSlider.value = sm.ambienceVolume;
        if (ambienceVolumeValText != null) ambienceVolumeValText.text = $"%{(int)(sm.ambienceVolume * 100)}";

        if (uiVolumeSlider != null) uiVolumeSlider.value = sm.uiVolume;
        if (uiVolumeValText != null) uiVolumeValText.text = $"%{(int)(sm.uiVolume * 100)}";

        // 2. Graphics
        if (qualityDropdown != null) qualityDropdown.value = sm.qualityLevel;
        if (fullscreenDropdown != null) fullscreenDropdown.value = sm.fullscreenMode;
        if (vsyncToggle != null) vsyncToggle.isOn = sm.vsyncEnabled;

        // 3. Controls
        if (mouseSensSlider != null) mouseSensSlider.value = sm.mouseSensitivity;
        if (mouseSensValText != null) mouseSensValText.text = $"{sm.mouseSensitivity:F1}x";
        if (invertYToggle != null) invertYToggle.isOn = sm.invertMouseY;
    }

    #endregion

    #region --- BUTTON & SLIDER BINDINGS ---

    private void BindButtonsAndEvents()
    {
        // Main Menu
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnPlayButtonClicked);
        }
        if (mainMenuSettingsButton != null)
        {
            mainMenuSettingsButton.onClick.RemoveAllListeners();
            mainMenuSettingsButton.onClick.AddListener(() => OpenSettings(false));
        }
        if (mainMenuQuitButton != null)
        {
            mainMenuQuitButton.onClick.RemoveAllListeners();
            mainMenuQuitButton.onClick.AddListener(QuitGame);
        }

        // Pause Menu
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(ResumeGame);
        }
        if (pauseSettingsButton != null)
        {
            pauseSettingsButton.onClick.RemoveAllListeners();
            pauseSettingsButton.onClick.AddListener(() => OpenSettings(true));
        }
        if (returnToMainMenuButton != null)
        {
            returnToMainMenuButton.onClick.RemoveAllListeners();
            returnToMainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }
        if (pauseQuitButton != null)
        {
            pauseQuitButton.onClick.RemoveAllListeners();
            pauseQuitButton.onClick.AddListener(QuitGame);
        }

        // Settings Tabs & Back
        if (settingsBackButton != null)
        {
            settingsBackButton.onClick.RemoveAllListeners();
            settingsBackButton.onClick.AddListener(CloseSettings);
        }
        if (tabAudioBtn != null)
        {
            tabAudioBtn.onClick.RemoveAllListeners();
            tabAudioBtn.onClick.AddListener(() => ShowSettingsTab(0));
        }
        if (tabGraphicsBtn != null)
        {
            tabGraphicsBtn.onClick.RemoveAllListeners();
            tabGraphicsBtn.onClick.AddListener(() => ShowSettingsTab(1));
        }
        if (tabControlsBtn != null)
        {
            tabControlsBtn.onClick.RemoveAllListeners();
            tabControlsBtn.onClick.AddListener(() => ShowSettingsTab(2));
        }

        // Audio Sliders
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.RemoveAllListeners();
            masterVolumeSlider.onValueChanged.AddListener(val =>
            {
                SettingsManager.Instance.SetMasterVolume(val);
                if (masterVolumeValText != null) masterVolumeValText.text = $"%{(int)(val * 100)}";
            });
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.RemoveAllListeners();
            musicVolumeSlider.onValueChanged.AddListener(val =>
            {
                SettingsManager.Instance.SetMusicVolume(val);
                if (musicVolumeValText != null) musicVolumeValText.text = $"%{(int)(val * 100)}";
            });
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveAllListeners();
            sfxVolumeSlider.onValueChanged.AddListener(val =>
            {
                SettingsManager.Instance.SetSfxVolume(val);
                if (sfxVolumeValText != null) sfxVolumeValText.text = $"%{(int)(val * 100)}";
            });
        }

        if (ambienceVolumeSlider != null)
        {
            ambienceVolumeSlider.onValueChanged.RemoveAllListeners();
            ambienceVolumeSlider.onValueChanged.AddListener(val =>
            {
                SettingsManager.Instance.SetAmbienceVolume(val);
                if (ambienceVolumeValText != null) ambienceVolumeValText.text = $"%{(int)(val * 100)}";
            });
        }

        if (uiVolumeSlider != null)
        {
            uiVolumeSlider.onValueChanged.RemoveAllListeners();
            uiVolumeSlider.onValueChanged.AddListener(val =>
            {
                SettingsManager.Instance.SetUiVolume(val);
                if (uiVolumeValText != null) uiVolumeValText.text = $"%{(int)(val * 100)}";
            });
        }

        // Graphics Dropdowns & Toggles
        if (qualityDropdown != null)
        {
            qualityDropdown.onValueChanged.RemoveAllListeners();
            qualityDropdown.onValueChanged.AddListener(idx =>
            {
                SettingsManager.Instance.SetQualityLevel(idx);
            });
        }

        if (fullscreenDropdown != null)
        {
            fullscreenDropdown.onValueChanged.RemoveAllListeners();
            fullscreenDropdown.onValueChanged.AddListener(idx =>
            {
                SettingsManager.Instance.SetFullscreenMode(idx);
            });
        }

        if (resolutionDropdown != null)
        {
            resolutionDropdown.onValueChanged.RemoveAllListeners();
            resolutionDropdown.onValueChanged.AddListener(idx =>
            {
                SettingsManager.Instance.SetResolution(idx);
            });
        }

        if (vsyncToggle != null)
        {
            vsyncToggle.onValueChanged.RemoveAllListeners();
            vsyncToggle.onValueChanged.AddListener(isOn =>
            {
                SettingsManager.Instance.SetVSync(isOn);
            });
        }

        if (fpsLimitDropdown != null)
        {
            fpsLimitDropdown.onValueChanged.RemoveAllListeners();
            fpsLimitDropdown.onValueChanged.AddListener(idx =>
            {
                int fps = 60;
                if (idx == 0) fps = 30;
                else if (idx == 1) fps = 60;
                else if (idx == 2) fps = 120;
                else if (idx == 3) fps = 144;
                else fps = -1;
                SettingsManager.Instance.SetTargetFps(fps);
            });
        }

        // Controls
        if (mouseSensSlider != null)
        {
            mouseSensSlider.onValueChanged.RemoveAllListeners();
            mouseSensSlider.onValueChanged.AddListener(val =>
            {
                SettingsManager.Instance.SetMouseSensitivity(val);
                if (mouseSensValText != null) mouseSensValText.text = $"{val:F1}x";
            });
        }

        if (invertYToggle != null)
        {
            invertYToggle.onValueChanged.RemoveAllListeners();
            invertYToggle.onValueChanged.AddListener(isOn =>
            {
                SettingsManager.Instance.SetInvertY(isOn);
            });
        }
    }

    #endregion

    #region --- SMOOTH FADE TRANSITION SEQUENCE ---

    public void PlayTransitionSequence(Action onBlackout, Action onComplete = null)
    {
        if (activeTransitionCoroutine != null)
        {
            StopCoroutine(activeTransitionCoroutine);
        }

        activeTransitionCoroutine = StartCoroutine(TransitionSequenceRoutine(onBlackout, onComplete));
    }

    private IEnumerator TransitionSequenceRoutine(Action onBlackout, Action onComplete)
    {
        currentState = GameFlowState.Transitioning;

        if (fadeOverlayCanvasGroup != null)
        {
            fadeOverlayCanvasGroup.gameObject.SetActive(true);
            fadeOverlayCanvasGroup.transform.SetAsLastSibling();
            fadeOverlayCanvasGroup.blocksRaycasts = true;
            fadeOverlayCanvasGroup.interactable = true;

            // 1. Fade Out to Black
            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeOverlayCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeOutDuration);
                yield return null;
            }
            fadeOverlayCanvasGroup.alpha = 1f;
        }

        // 2. Blackout Execution: Swap cameras, reposition, toggle UI
        onBlackout?.Invoke();

        float holdElapsed = 0f;
        while (holdElapsed < blackoutHoldDuration)
        {
            holdElapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // 3. Fade In to New View
        if (fadeOverlayCanvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeOverlayCanvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / fadeInDuration));
                yield return null;
            }
            fadeOverlayCanvasGroup.alpha = 0f;
            fadeOverlayCanvasGroup.blocksRaycasts = false;
            fadeOverlayCanvasGroup.interactable = false;
            fadeOverlayCanvasGroup.gameObject.SetActive(false);
        }

        onComplete?.Invoke();
        activeTransitionCoroutine = null;
    }

    #endregion
}
