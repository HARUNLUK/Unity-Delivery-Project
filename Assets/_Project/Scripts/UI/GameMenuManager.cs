using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
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
/// Handles camera switching between Shop Overview and Player Gameplay, coordinates game pause state,
/// and manages dynamic keybindings rebind UI.
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

    public static bool SkipMainMenuOnNextLoad { get; set; } = false;

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
    public Button continueButton;
    public Button newGameButton;
    public Button playButton;
    public Button mainMenuSettingsButton;
    public Button mainMenuQuitButton;

    [Header("--- NEW GAME CONFIRMATION MODAL ---")]
    public GameObject newGameModalPanel;
    public Button confirmNewGameBtn;
    public Button cancelNewGameBtn;

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
    public TMP_Dropdown languageDropdown;
    public TMP_Dropdown qualityDropdown;
    public TMP_Dropdown fullscreenDropdown;
    public TMP_Dropdown resolutionDropdown;
    public Toggle vsyncToggle;
    public TMP_Dropdown fpsLimitDropdown;

    [Header("Controls Selectors")]
    public Slider mouseSensSlider;
    public TextMeshProUGUI mouseSensValText;
    public Toggle invertYToggle;

    [Header("Keybindings UI")]
    public Button resetKeybindingsBtn;
    public Transform keybindingsContent;

    private readonly Dictionary<GameAction, (Button button, TextMeshProUGUI text)> keybindingRowMap = new Dictionary<GameAction, (Button button, TextMeshProUGUI text)>();
    private GameAction? activeRebindingAction = null;
    private TextMeshProUGUI activeRebindingText = null;
    private Button activeRebindingButton = null;
    private float rebindDebounceTimer = 0f;

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
        KeyBindingManager.EnsureInitialized();
        EnsureReferences();
        EnsureUI();

        if (SkipMainMenuOnNextLoad)
        {
            currentState = GameFlowState.Playing;
        }
    }

    private void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
        KeyBindingManager.OnBindingsChanged += RefreshAllKeybindingUI;
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
        KeyBindingManager.OnBindingsChanged -= RefreshAllKeybindingUI;
    }

    private void HandleLanguageChanged(string lang)
    {
        RefreshLocalizedUI();
    }

    private void Start()
    {
        EnsureReferences();
        EnsureUI();
        BindButtonsAndEvents();
        InitializeSettingsUI();
        RefreshLocalizedUI();

        if (SkipMainMenuOnNextLoad)
        {
            SkipMainMenuOnNextLoad = false;
            InitializeDirectGameplay();
        }
        else if (startInMainMenu)
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
            continueButton = mainMenuPanel.transform.Find("LeftContentCard/ButtonsColumn/ContinueButton")?.GetComponent<Button>();
            newGameButton = mainMenuPanel.transform.Find("LeftContentCard/ButtonsColumn/NewGameButton")?.GetComponent<Button>();
            playButton = mainMenuPanel.transform.Find("LeftContentCard/ButtonsColumn/PlayButton")?.GetComponent<Button>();
            if (continueButton == null && playButton != null) continueButton = playButton;
            mainMenuSettingsButton = mainMenuPanel.transform.Find("LeftContentCard/ButtonsColumn/SettingsButton")?.GetComponent<Button>();
            mainMenuQuitButton = mainMenuPanel.transform.Find("LeftContentCard/ButtonsColumn/QuitButton")?.GetComponent<Button>();

            Transform modal = mainMenuPanel.transform.Find("NewGameConfirmModal");
            if (modal != null)
            {
                newGameModalPanel = modal.gameObject;
                confirmNewGameBtn = modal.Find("ModalCard/ButtonRow/ConfirmBtn")?.GetComponent<Button>();
                cancelNewGameBtn = modal.Find("ModalCard/ButtonRow/CancelBtn")?.GetComponent<Button>();
            }
        }

        if (pauseMenuPanel != null)
        {
            resumeButton = pauseMenuPanel.transform.Find("PauseCard/PauseButtons/ResumeBtn")?.GetComponent<Button>() ??
                           pauseMenuPanel.transform.Find("PauseCard/PauseButtons/ResumeButton")?.GetComponent<Button>();
            pauseSettingsButton = pauseMenuPanel.transform.Find("PauseCard/PauseButtons/SettingsBtn")?.GetComponent<Button>() ??
                                  pauseMenuPanel.transform.Find("PauseCard/PauseButtons/SettingsButton")?.GetComponent<Button>();
            returnToMainMenuButton = pauseMenuPanel.transform.Find("PauseCard/PauseButtons/MainMenuBtn")?.GetComponent<Button>() ??
                                     pauseMenuPanel.transform.Find("PauseCard/PauseButtons/MainMenuButton")?.GetComponent<Button>();
            pauseQuitButton = pauseMenuPanel.transform.Find("PauseCard/PauseButtons/QuitBtn")?.GetComponent<Button>() ??
                              pauseMenuPanel.transform.Find("PauseCard/PauseButtons/QuitButton")?.GetComponent<Button>();
        }

        if (settingsPanel != null)
        {
            settingsBackButton = settingsPanel.transform.Find("SettingsCard/BackButtonHolder/SettingsBackBtn")?.GetComponent<Button>() ??
                                 settingsPanel.transform.Find("SettingsCard/BackButtonHolder/BackButton")?.GetComponent<Button>();
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
                languageDropdown = graphicsSection.transform.Find("LanguageRow/Dropdown")?.GetComponent<TMP_Dropdown>();
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

                resetKeybindingsBtn = controlsSection.transform.Find("KeybindingsHeaderRow/ResetBindingsBtn/ResetBtn")?.GetComponent<Button>() ??
                                      controlsSection.transform.Find("KeybindingsHeaderRow/ResetBindingsBtn")?.GetComponent<Button>();

                Transform content = controlsSection.transform.Find("KeybindingsScrollView/Viewport/Content");
                if (content != null)
                {
                    keybindingsContent = content;
                    keybindingRowMap.Clear();
                    foreach (GameAction action in Enum.GetValues(typeof(GameAction)))
                    {
                        Transform row = content.Find("KeyRow_" + action.ToString());
                        if (row != null)
                        {
                            Button btn = row.Find("KeyButton")?.GetComponent<Button>();
                            TextMeshProUGUI txt = row.Find("KeyButton/Text")?.GetComponent<TextMeshProUGUI>();
                            if (btn != null && txt != null)
                            {
                                keybindingRowMap[action] = (btn, txt);
                            }
                        }
                    }
                }
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
            mainMenuTitleText.text = "<b>Where's</b>\n<size=40><b>My Package</b></size>";
            mainMenuTitleText.fontSize = 28;
            mainMenuTitleText.alignment = TextAlignmentOptions.Center;
            mainMenuTitleText.color = Color.white;

            GameObject saveBadge = CreateElement("SaveInfoBadge", mainCard.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -135), new Vector2(480, 50));
            Image badgeImg = saveBadge.AddComponent<Image>();
            badgeImg.color = new Color(0.08f, 0.12f, 0.18f, 0.90f);

            GameObject saveInfoObj = CreateElement("SaveInfoText", saveBadge.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            mainMenuSaveInfoText = saveInfoObj.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) mainMenuSaveInfoText.font = fontAsset;
            mainMenuSaveInfoText.text = "<color=#A0C8FF>Mevcut Şube:</color> <color=#FFFFFF>Lv.1</color>  |  <color=#A0C8FF>Kasa:</color> <color=#32FF64>$0</color>";
            mainMenuSaveInfoText.fontSize = 17;
            mainMenuSaveInfoText.alignment = TextAlignmentOptions.Center;

            GameObject btnCol = CreateElement("ButtonsColumn", mainCard.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(440, 360));
            VerticalLayoutGroup vlg = btnCol.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 18f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            continueButton = CreateButton("ContinueButton", btnCol.transform, "DEVAM ET", 54, new Color(0.06f, 0.28f, 0.15f, 0.95f), new Color(0.2f, 1.0f, 0.45f), fontAsset);
            newGameButton = CreateButton("NewGameButton", btnCol.transform, "YENİ OYUN", 48, new Color(0.08f, 0.20f, 0.32f, 0.95f), new Color(0.3f, 0.9f, 1f), fontAsset);
            mainMenuSettingsButton = CreateButton("SettingsButton", btnCol.transform, "AYARLAR", 48, new Color(0.09f, 0.12f, 0.18f, 0.95f), Color.white, fontAsset);
            mainMenuQuitButton = CreateButton("QuitButton", btnCol.transform, "ÇIKIŞ", 48, new Color(0.35f, 0.08f, 0.08f, 0.95f), new Color(1f, 0.35f, 0.35f), fontAsset);
            playButton = continueButton;

            // New Game Confirmation Modal
            GameObject modalObj = CreatePanel(mp.transform, "NewGameConfirmModal");
            Image mBg = modalObj.AddComponent<Image>();
            mBg.color = new Color(0f, 0f, 0f, 0.75f);

            GameObject mCard = CreateElement("ModalCard", modalObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(540, 260));
            Image mCardBg = mCard.AddComponent<Image>();
            mCardBg.color = new Color(0.08f, 0.11f, 0.17f, 0.98f);
            Outline mCardOutline = mCard.AddComponent<Outline>();
            mCardOutline.effectColor = new Color(1f, 0.6f, 0.2f, 0.65f);
            mCardOutline.effectDistance = new Vector2(2, -2);

            GameObject mTitleObj = CreateElement("Title", mCard.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(480, 40));
            TextMeshProUGUI mTitle = mTitleObj.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) mTitle.font = fontAsset;
            mTitle.text = "<b>YENİ OYUN BAŞLAT</b>";
            mTitle.fontSize = 22;
            mTitle.alignment = TextAlignmentOptions.Center;
            mTitle.color = new Color(1f, 0.75f, 0.25f);

            GameObject mBodyObj = CreateElement("BodyText", mCard.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(480, 80));
            TextMeshProUGUI mBody = mBodyObj.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) mBody.font = fontAsset;
            mBody.text = "Mevcut kayıt ve tüm şube ilerlemeniz sıfırlanarak 1. Seviyeden yeni bir kariyere başlanacaktır.\n\n<b>Emin misiniz?</b>";
            mBody.fontSize = 15;
            mBody.alignment = TextAlignmentOptions.Center;
            mBody.color = new Color(0.85f, 0.92f, 1.0f);

            GameObject mBtnRow = CreateElement("ButtonRow", mCard.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(480, 45));
            HorizontalLayoutGroup mHlg = mBtnRow.AddComponent<HorizontalLayoutGroup>();
            mHlg.spacing = 20f;
            mHlg.childControlWidth = true;
            mHlg.childControlHeight = true;

            confirmNewGameBtn = CreateButton("ConfirmBtn", mBtnRow.transform, "EVET, SIFIRLA VE BAŞLA", 45, new Color(0.35f, 0.10f, 0.10f, 0.95f), new Color(1f, 0.45f, 0.45f), fontAsset);
            cancelNewGameBtn = CreateButton("CancelBtn", mBtnRow.transform, "İPTAL", 45, new Color(0.10f, 0.14f, 0.20f, 0.95f), Color.white, fontAsset);

            newGameModalPanel = modalObj;
            modalObj.SetActive(false);

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
            pTitleText.text = "<b>OYUN DURAKLATILDI</b>\n<size=55%><color=#32FFFF>GAME PAUSED</color></size>";
            pTitleText.fontSize = 24;
            pTitleText.alignment = TextAlignmentOptions.Center;
            pTitleText.color = Color.white;

            GameObject pBtnCol = CreateElement("PauseButtons", pCard.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -35), new Vector2(380, 320));
            VerticalLayoutGroup pvlg = pBtnCol.AddComponent<VerticalLayoutGroup>();
            pvlg.spacing = 16f;
            pvlg.childAlignment = TextAnchor.MiddleCenter;
            pvlg.childControlWidth = true;
            pvlg.childControlHeight = false;

            resumeButton = CreateButton("ResumeBtn", pBtnCol.transform, "DEVAM ET", 54, new Color(0.08f, 0.22f, 0.35f, 0.95f), new Color(0.2f, 0.9f, 1.0f), fontAsset);
            pauseSettingsButton = CreateButton("SettingsBtn", pBtnCol.transform, "AYARLAR", 48, new Color(0.09f, 0.12f, 0.18f, 0.95f), Color.white, fontAsset);
            returnToMainMenuButton = CreateButton("MainMenuBtn", pBtnCol.transform, "ANA MENÜYE DÖN", 48, new Color(0.09f, 0.12f, 0.18f, 0.95f), new Color(1.0f, 0.8f, 0.4f), fontAsset);
            pauseQuitButton = CreateButton("QuitBtn", pBtnCol.transform, "MASAÜSTÜNE ÇIK", 48, new Color(0.35f, 0.08f, 0.08f, 0.95f), new Color(1f, 0.35f, 0.35f), fontAsset);

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
            sTitleText.text = "<b>AYARLAR - SETTINGS</b>";
            sTitleText.fontSize = 28;
            sTitleText.alignment = TextAlignmentOptions.Center;
            sTitleText.color = Color.white;

            GameObject tabRow = CreateElement("TabRow", sCard.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -95), new Vector2(800, 45));
            HorizontalLayoutGroup thlg = tabRow.AddComponent<HorizontalLayoutGroup>();
            thlg.spacing = 15f;
            thlg.childControlWidth = true;
            thlg.childControlHeight = true;

            tabAudioBtn = CreateButton("TabAudio", tabRow.transform, "SES", 45, new Color(0.09f, 0.12f, 0.18f, 0.95f), Color.white, fontAsset);
            tabGraphicsBtn = CreateButton("TabGraphics", tabRow.transform, "GRAFİK", 45, new Color(0.09f, 0.12f, 0.18f, 0.95f), Color.white, fontAsset);
            tabControlsBtn = CreateButton("TabControls", tabRow.transform, "KONTROLLER", 45, new Color(0.09f, 0.12f, 0.18f, 0.95f), Color.white, fontAsset);

            GameObject contentArea = CreateElement("ContentArea", sCard.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(820, 430));

            // 3.1 Audio Section
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

            // 3.2 Graphics Section
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

            // 3.3 Controls Section
            controlsSection = CreateElement("ControlsSection", contentArea.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            VerticalLayoutGroup cvlg = controlsSection.AddComponent<VerticalLayoutGroup>();
            cvlg.spacing = 8f;
            cvlg.childControlWidth = true;
            cvlg.childControlHeight = false;

            mouseSensSlider = CreateSlider("MouseSensRow", controlsSection.transform, "Fare Bakış Hassasiyeti:", out mouseSensValText, fontAsset, 0.2f, 5.0f);
            invertYToggle = CreateToggle("InvertYRow", controlsSection.transform, "Fare Y-Ekseni Ters Çevir:", fontAsset);

            // Keybindings Header Row
            GameObject kbHeaderRow = CreateElement("KeybindingsHeaderRow", controlsSection.transform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0, 36));
            GameObject kbTitleObj = CreateElement("HeaderTitle", kbHeaderRow.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10, 0), new Vector2(480, 32));
            TextMeshProUGUI kbTitle = kbTitleObj.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) kbTitle.font = fontAsset;
            kbTitle.text = "<color=#32FFFF><b>TUŞ ATAMALARI:</b></color> <size=80%><color=#85A8C8>(Değiştirmek istediğiniz tuşa tıklayın)</color></size>";
            kbTitle.fontSize = 16;
            kbTitle.alignment = TextAlignmentOptions.Left;

            GameObject resetBtnHolder = CreateElement("ResetBindingsBtn", kbHeaderRow.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-10, 0), new Vector2(190, 32));
            resetKeybindingsBtn = CreateButton("ResetBtn", resetBtnHolder.transform, "Varsayılana Sıfırla", 32, new Color(0.18f, 0.12f, 0.08f, 0.95f), new Color(1.0f, 0.85f, 0.4f), fontAsset);

            // ScrollView for Keybindings
            var (svObj, contentTr, _) = CreateKeybindingsScrollView(controlsSection.transform, fontAsset);
            keybindingsContent = contentTr;

            keybindingRowMap.Clear();
            foreach (GameAction action in Enum.GetValues(typeof(GameAction)))
            {
                string desc = KeyBindingManager.GetActionDescription(action);
                string keyName = KeyBindingManager.GetBinding(action).GetDisplayName();
                CreateKeybindingRow(keybindingsContent, desc, action.ToString(), keyName, out Button btn, out TextMeshProUGUI txt, fontAsset);
                keybindingRowMap[action] = (btn, txt);
            }

            controlsSection.SetActive(false);

            GameObject backBtnObj = CreateElement("BackButtonHolder", sCard.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 35), new Vector2(300, 48));
            settingsBackButton = CreateButton("SettingsBackBtn", backBtnObj.transform, "KAYDET VE GERİ DÖN", 48, new Color(0.08f, 0.22f, 0.35f, 0.95f), new Color(0.2f, 0.9f, 1f), fontAsset);

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
        rrt.sizeDelta = new Vector2(0, 40);
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
        rrt.sizeDelta = new Vector2(0, 40);
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
        rrt.sizeDelta = new Vector2(0, 40);
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

    private static (GameObject scrollView, Transform content, Scrollbar scrollbar) CreateKeybindingsScrollView(Transform parent, TMP_FontAsset fontAsset)
    {
        GameObject svObj = CreateElement("KeybindingsScrollView", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820, 260));
        Image svBg = svObj.AddComponent<Image>();
        svBg.color = new Color(0.04f, 0.06f, 0.10f, 0.90f);

        Outline svOutline = svObj.AddComponent<Outline>();
        svOutline.effectColor = new Color(0.2f, 0.8f, 1f, 0.30f);
        svOutline.effectDistance = new Vector2(1, -1);

        ScrollRect sr = svObj.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 28f;

        // Viewport
        GameObject vpObj = CreateElement("Viewport", svObj.transform, Vector2.zero, Vector2.one, new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
        RectTransform vpRt = vpObj.GetComponent<RectTransform>();
        vpRt.offsetMin = new Vector2(6, 6);
        vpRt.offsetMax = new Vector2(-20, -6);
        vpObj.AddComponent<RectMask2D>();
        sr.viewport = vpRt;

        // Content
        GameObject contentObj = CreateElement("Content", vpObj.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
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
        GameObject sbObj = CreateElement("Scrollbar", svObj.transform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(14, 0));
        RectTransform sbRt = sbObj.GetComponent<RectTransform>();
        sbRt.offsetMin = new Vector2(-16, 6);
        sbRt.offsetMax = new Vector2(-4, -6);
        Image sbTrack = sbObj.AddComponent<Image>();
        sbTrack.color = new Color(0.07f, 0.10f, 0.16f, 0.90f);

        GameObject handleObj = CreateElement("Handle", sbObj.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image handleImg = handleObj.AddComponent<Image>();
        handleImg.color = new Color(0.2f, 0.8f, 1.0f, 0.80f);

        Scrollbar sb = sbObj.AddComponent<Scrollbar>();
        sb.direction = Scrollbar.Direction.BottomToTop;
        sb.targetGraphic = handleImg;
        sb.handleRect = handleObj.GetComponent<RectTransform>();

        sr.verticalScrollbar = sb;
        sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

        return (svObj, contentObj.transform, sb);
    }

    private static GameObject CreateKeybindingRow(Transform parent, string actionLabel, string actionKeyId, string currentKeyText, out Button btnOut, out TextMeshProUGUI textOut, TMP_FontAsset fontAsset)
    {
        GameObject row = new GameObject("KeyRow_" + actionKeyId, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        RectTransform rrt = row.GetComponent<RectTransform>();
        rrt.sizeDelta = new Vector2(0, 38);
        rrt.localScale = Vector3.one;

        Image rowBg = row.AddComponent<Image>();
        rowBg.color = new Color(0.06f, 0.09f, 0.15f, 0.85f);

        Outline rowOutline = row.AddComponent<Outline>();
        rowOutline.effectColor = new Color(0.2f, 0.8f, 1f, 0.20f);
        rowOutline.effectDistance = new Vector2(1, -1);

        // Left Action Label
        GameObject lblObj = CreateElement("ActionLabel", row.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14, 0), new Vector2(460, 32));
        TextMeshProUGUI lbl = lblObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) lbl.font = fontAsset;
        lbl.text = $"<b>{actionLabel}</b>";
        lbl.fontSize = 15;
        lbl.alignment = TextAlignmentOptions.Left;
        lbl.color = new Color(0.9f, 0.95f, 1.0f);

        // Right Key Button
        GameObject btnObj = CreateElement("KeyButton", row.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-12, 0), new Vector2(170, 30));
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.10f, 0.16f, 0.25f, 0.95f);

        Outline btnOutline = btnObj.AddComponent<Outline>();
        btnOutline.effectColor = new Color(0.2f, 0.8f, 1f, 0.45f);
        btnOutline.effectDistance = new Vector2(1, -1);

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
        cb.pressedColor = new Color(0.7f, 0.9f, 1.0f, 1f);
        btn.colors = cb;

        GameObject textObj = CreateElement("Text", btnObj.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI txt = textObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) txt.font = fontAsset;
        txt.text = $"[ {currentKeyText} ]";
        txt.fontSize = 15;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = new Color(0.25f, 0.95f, 1.0f);

        btnOut = btn;
        textOut = txt;
        return row;
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
        else
        {
            Canvas c = GetComponentInParent<Canvas>() ?? UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (c != null)
            {
                Transform dh = c.transform.Find("DeliveryHUD");
                if (dh != null) dh.gameObject.SetActive(false);
            }
        }

        // 4. Update Main Menu save stats and button states
        UpdateMainMenuSaveStats();
        UpdateMainMenuButtons();

        // 5. Setup UI Panels
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
            mainMenuPanel.transform.SetAsLastSibling();
        }
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (newGameModalPanel != null) newGameModalPanel.SetActive(false);
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
        else
        {
            Canvas c = GetComponentInParent<Canvas>() ?? UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (c != null)
            {
                Transform dh = c.transform.Find("DeliveryHUD");
                if (dh != null) dh.gameObject.SetActive(true);
            }
        }

        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (newGameModalPanel != null) newGameModalPanel.SetActive(false);
    }

    public static bool HasSaveData()
    {
        if (PlayerPrefs.HasKey("Delivery_HasSaveGame") && PlayerPrefs.GetInt("Delivery_HasSaveGame", 0) == 1)
        {
            return true;
        }

        if (PlayerPrefs.GetInt("Delivery_BranchLevel", 1) > 1) return true;
        if (PlayerPrefs.GetInt("WAREHOUSE_PLAYER_LEVEL", 1) > 1) return true;
        if (PlayerPrefs.GetInt("Delivery_CurrentDay", 1) > 1) return true;
        if (PlayerPrefs.GetInt("CARGO_PLAYER_TOTAL_BALANCE", 0) > 0) return true;
        if (PlayerPrefs.GetInt("Delivery_PlayerCash", 0) > 0) return true;

        if (PlayerPrefs.GetInt("DELIVERY_VEHICLE_UNLOCKED_cargo_van", 0) == 1 ||
            PlayerPrefs.GetInt("DELIVERY_VEHICLE_UNLOCKED_driveable_van", 0) == 1 ||
            PlayerPrefs.GetInt("DELIVERY_VEHICLE_UNLOCKED_drivable_van", 0) == 1) return true;

        if (PlayerPrefs.GetInt("Property_Unlocked_property_showroom", 0) == 1 ||
            PlayerPrefs.GetInt("Property_Unlocked_property_service_garage", 0) == 1 ||
            PlayerPrefs.GetInt("Property_Unlocked_property_insurance", 0) == 1 ||
            PlayerPrefs.GetInt("Property_Unlocked_property_logistics", 0) == 1) return true;

        return false;
    }

    public void UpdateMainMenuButtons()
    {
        bool hasSave = HasSaveData();

        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(hasSave);
        }

        if (newGameButton != null)
        {
            newGameButton.gameObject.SetActive(true);
            // newGameButton text is not modified via script
        }

        if (playButton != null && playButton != continueButton && playButton != newGameButton)
        {
            playButton.gameObject.SetActive(!hasSave);
        }

        if (newGameModalPanel != null)
        {
            newGameModalPanel.SetActive(false);
        }
    }

    public void UpdateMainMenuSaveStats()
    {
        if (mainMenuSaveInfoText == null) return;

        bool hasSave = HasSaveData();
        int cash = PlayerEconomyManager.Instance != null ? PlayerEconomyManager.Instance.CurrentLiveBalance : PlayerPrefs.GetInt("CARGO_PLAYER_TOTAL_BALANCE", PlayerPrefs.GetInt("Delivery_PlayerCash", 0));
        int level = BranchManager.Instance != null ? BranchManager.Instance.CurrentBranchLevel : PlayerPrefs.GetInt("Delivery_BranchLevel", 1);
        int day = DayTimeManager.Instance != null ? DayTimeManager.Instance.CurrentDay : PlayerPrefs.GetInt("Delivery_CurrentDay", 1);
        string tierName = BranchManager.Instance != null && BranchManager.Instance.CurrentTier != null ?
            BranchManager.Instance.CurrentTier.tierName : $"Lv.{level}";

        if (hasSave)
        {
            mainMenuSaveInfoText.text = LocalizationManager.GetFormat("main_menu_save_info", day, level, tierName, cash);
        }
        else
        {
            mainMenuSaveInfoText.text = LocalizationManager.GetFormat("main_menu_new_info", tierName, cash);
        }
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

        if (activeRebindingAction.HasValue)
        {
            HandleKeyRebindInput();
            return;
        }

        HandleInput();
    }

    private void HandleKeyRebindInput()
    {
        if (!activeRebindingAction.HasValue) return;

        if (rebindDebounceTimer > 0f)
        {
            rebindDebounceTimer -= Time.unscaledDeltaTime;
            return;
        }

#if ENABLE_INPUT_SYSTEM
        // 1. ESC cancels rebinding
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CancelKeyRebind();
            return;
        }

        // 2. Mouse buttons detection (Left, Right, Middle, Forward, Back)
        if (Mouse.current != null)
        {
            CustomMouseButton pressedMouse = CustomMouseButton.None;
            if (Mouse.current.leftButton.wasPressedThisFrame) pressedMouse = CustomMouseButton.Left;
            else if (Mouse.current.rightButton.wasPressedThisFrame) pressedMouse = CustomMouseButton.Right;
            else if (Mouse.current.middleButton.wasPressedThisFrame) pressedMouse = CustomMouseButton.Middle;
            else if (Mouse.current.forwardButton.wasPressedThisFrame) pressedMouse = CustomMouseButton.Forward;
            else if (Mouse.current.backButton.wasPressedThisFrame) pressedMouse = CustomMouseButton.Back;

            if (pressedMouse != CustomMouseButton.None)
            {
                KeyBindingManager.SetMouse(activeRebindingAction.Value, pressedMouse);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
                RefreshAllKeybindingUI();
                activeRebindingAction = null;
                activeRebindingButton = null;
                activeRebindingText = null;
                return;
            }
        }

        // 3. Keyboard keys detection
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            foreach (var control in Keyboard.current.allControls)
            {
                if (control is KeyControl kc && kc.wasPressedThisFrame)
                {
                    if (kc.keyCode != Key.None && kc.keyCode != Key.Escape)
                    {
                        KeyBindingManager.SetKey(activeRebindingAction.Value, kc.keyCode);
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
                        RefreshAllKeybindingUI();
                        activeRebindingAction = null;
                        activeRebindingButton = null;
                        activeRebindingText = null;
                        return;
                    }
                }
            }
        }
#endif
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

    #region --- FLOW ACTIONS (PLAY, CONTINUE, NEW GAME, PAUSE, RESUME, RETURN, QUIT) ---

    public void OnPlayButtonClicked()
    {
        OnContinueButtonClicked();
    }

    public void OnContinueButtonClicked()
    {
        if (currentState == GameFlowState.Transitioning) return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }

        StartGameplayTransition();
    }

    public void OnNewGameButtonClicked()
    {
        if (currentState == GameFlowState.Transitioning) return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }

        bool hasSave = HasSaveData();
        if (hasSave)
        {
            if (newGameModalPanel != null)
            {
                newGameModalPanel.SetActive(true);
                newGameModalPanel.transform.SetAsLastSibling();
            }
            else
            {
                ExecuteNewGameResetAndStart();
            }
        }
        else
        {
            ExecuteNewGameResetAndStart();
        }
    }

    public void ExecuteNewGameResetAndStart()
    {
        if (newGameModalPanel != null)
        {
            newGameModalPanel.SetActive(false);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }

        Debug.Log("<color=#FFAA33>[GameMenuManager] Resetting all game progression, economy, day, properties, and vehicle states for NEW GAME...</color>");

        // 1. Reset all vehicles, fuels, conditions, colors and tuning stages
        DrivableVehicle.ResetAllVehiclesInGame();
        VehicleServiceGarage.ResetAllVehicleTuning();

        // 2. Reset all commercial properties (lock them)
        PurchasableProperty.ResetAllPropertiesInGame();

        // 3. Reset Branch progression to Level 1
        if (BranchManager.Instance != null)
        {
            BranchManager.Instance.ResetBranchProgression();
        }
        else
        {
            PlayerPrefs.SetInt("Delivery_BranchLevel", 1);
        }

        // 4. Reset Warehouse level to 1
        if (PlayerProgressionManager.Instance != null)
        {
            PlayerProgressionManager.Instance.ResetProgression();
        }
        else
        {
            PlayerPrefs.SetInt("WAREHOUSE_PLAYER_LEVEL", 1);
            PlayerPrefs.SetInt("Delivery_PlayerLevel", 1);
            PlayerPrefs.SetInt("Delivery_CurrentXP", 0);
        }

        // 5. Reset Economy to starter balance ($0)
        if (PlayerEconomyManager.Instance != null)
        {
            PlayerEconomyManager.Instance.SetBalance(0);
        }
        else
        {
            PlayerPrefs.SetInt("CARGO_PLAYER_TOTAL_BALANCE", 0);
            PlayerPrefs.SetInt("Delivery_PlayerCash", 0);
        }

        // 6. Reset Day Count to Day 1
        if (DayTimeManager.Instance != null)
        {
            DayTimeManager.Instance.ResetDayCount();
        }
        else
        {
            PlayerPrefs.SetInt("Delivery_CurrentDay", 1);
        }
        PlayerPrefs.DeleteKey("DaySummary_DayCount");

        // 7. Reset Insurance and Passive Dispatch tiers
        if (InsuranceAgencyManager.Instance != null)
        {
            InsuranceAgencyManager.Instance.DevResetTier();
        }
        else
        {
            PlayerPrefs.SetInt("Delivery_InsuranceTier", 1);
        }

        if (PassiveDispatchManager.Instance != null)
        {
            PassiveDispatchManager.Instance.DevResetLevel();
        }
        else
        {
            PlayerPrefs.SetInt("Delivery_PassiveDispatchLevel", 1);
        }

        // 8. Set Save Game flag
        PlayerPrefs.SetInt("Delivery_HasSaveGame", 1);
        PlayerPrefs.Save();

        // 9. Update stats display and buttons
        UpdateMainMenuSaveStats();
        UpdateMainMenuButtons();

        // 10. Start game transition
        StartGameplayTransition();
    }

    private void StartGameplayTransition()
    {
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
                if (newGameModalPanel != null) newGameModalPanel.SetActive(false);

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

        EnsureUI();

        if (pauseMenuPanel == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>() ?? UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform pmp = canvas.transform.Find("PauseMenuPanel");
                if (pmp != null) pauseMenuPanel = pmp.gameObject;
            }
        }

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
            pauseMenuPanel.transform.SetAsLastSibling();
        }
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (newGameModalPanel != null) newGameModalPanel.SetActive(false);

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
        if (newGameModalPanel != null) newGameModalPanel.SetActive(false);

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
                UpdateMainMenuButtons();
                if (mainMenuPanel != null)
                {
                    mainMenuPanel.SetActive(true);
                    mainMenuPanel.transform.SetAsLastSibling();
                }
                if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
                if (settingsPanel != null) settingsPanel.SetActive(false);
                if (newGameModalPanel != null) newGameModalPanel.SetActive(false);

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

        EnsureUI();

        // 1. Deactivate Pause Menu Panel completely
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
        else
        {
            Canvas canvas = GetComponentInParent<Canvas>() ?? UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform pmp = canvas.transform.Find("PauseMenuPanel");
                if (pmp != null)
                {
                    pauseMenuPanel = pmp.gameObject;
                    pauseMenuPanel.SetActive(false);
                }
            }
        }

        // 2. Deactivate Main Menu & Modals
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }
        else
        {
            Canvas canvas = GetComponentInParent<Canvas>() ?? UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform mmp = canvas.transform.Find("MainMenuPanel");
                if (mmp != null)
                {
                    mainMenuPanel = mmp.gameObject;
                    mainMenuPanel.SetActive(false);
                }
            }
        }

        if (newGameModalPanel != null) newGameModalPanel.SetActive(false);

        // 3. Activate Settings Panel
        if (settingsPanel == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>() ?? UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform sp = canvas.transform.Find("SettingsPanel");
                if (sp != null) settingsPanel = sp.gameObject;
            }
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            settingsPanel.transform.SetAsLastSibling();
        }

        ShowSettingsTab(0); // Default to Audio tab
        SyncSettingsValuesToUI();
        RefreshAllKeybindingUI();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseSettings()
    {
        CancelKeyRebind();

        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SaveAllSettings();
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }

        EnsureUI();

        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (openedSettingsFromPause)
        {
            currentState = GameFlowState.Paused;
            if (pauseMenuPanel == null)
            {
                Canvas canvas = GetComponentInParent<Canvas>() ?? UnityEngine.Object.FindAnyObjectByType<Canvas>();
                if (canvas != null)
                {
                    Transform pmp = canvas.transform.Find("PauseMenuPanel");
                    if (pmp != null) pauseMenuPanel = pmp.gameObject;
                }
            }

            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(true);
                pauseMenuPanel.transform.SetAsLastSibling();
            }
        }
        else
        {
            currentState = GameFlowState.MainMenu;
            if (mainMenuPanel == null)
            {
                Canvas canvas = GetComponentInParent<Canvas>() ?? UnityEngine.Object.FindAnyObjectByType<Canvas>();
                if (canvas != null)
                {
                    Transform mmp = canvas.transform.Find("MainMenuPanel");
                    if (mmp != null) mainMenuPanel = mmp.gameObject;
                }
            }

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
        CancelKeyRebind();

        if (audioSection != null) audioSection.SetActive(tabIndex == 0);
        if (graphicsSection != null) graphicsSection.SetActive(tabIndex == 1);
        if (controlsSection != null) controlsSection.SetActive(tabIndex == 2);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayTabSwitch();
        }

        if (tabIndex == 2)
        {
            RefreshAllKeybindingUI();
        }
    }

    private void InitializeSettingsUI()
    {
        if (SettingsManager.Instance == null) return;

        // Populate Language Dropdown
        if (languageDropdown != null)
        {
            languageDropdown.onValueChanged.RemoveAllListeners();
            languageDropdown.ClearOptions();
            languageDropdown.AddOptions(new List<string> { "Türkçe (TR)", "English (EN)" });
            languageDropdown.value = LocalizationManager.IsTurkish ? 0 : 1;
            languageDropdown.RefreshShownValue();
            languageDropdown.onValueChanged.AddListener((int val) =>
            {
                string chosen = val == 0 ? "tr" : "en";
                if (SettingsManager.Instance != null)
                {
                    SettingsManager.Instance.SetLanguage(chosen);
                }
                else
                {
                    LocalizationManager.SetLanguage(chosen);
                }
            });
        }

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
            fullscreenDropdown.AddOptions(new List<string> {
                LocalizationManager.Get("disp_fullscreen", "Tam Ekran (Exclusive)"),
                LocalizationManager.Get("disp_borderless", "Kenarlıksız (Borderless)"),
                LocalizationManager.Get("disp_windowed", "Pencereli (Windowed)")
            });
            fullscreenDropdown.value = SettingsManager.Instance.fullscreenMode;
            fullscreenDropdown.RefreshShownValue();
        }

        // Populate Target FPS Dropdown
        if (fpsLimitDropdown != null)
        {
            fpsLimitDropdown.ClearOptions();
            fpsLimitDropdown.AddOptions(new List<string> { "30 FPS", "60 FPS", "120 FPS", "144 FPS", LocalizationManager.Get("fps_unlimited", "Sınırsız (Unlimited)") });
            int curFps = SettingsManager.Instance.targetFps;
            if (curFps == 30) fpsLimitDropdown.value = 0;
            else if (curFps == 60) fpsLimitDropdown.value = 1;
            else if (curFps == 120) fpsLimitDropdown.value = 2;
            else if (curFps == 144) fpsLimitDropdown.value = 3;
            else fpsLimitDropdown.value = 4;
            fpsLimitDropdown.RefreshShownValue();
        }

        SyncSettingsValuesToUI();
        RefreshAllKeybindingUI();
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

        // 2. Graphics & Language
        if (languageDropdown != null) languageDropdown.value = LocalizationManager.IsTurkish ? 0 : 1;
        if (qualityDropdown != null) qualityDropdown.value = sm.qualityLevel;
        if (fullscreenDropdown != null) fullscreenDropdown.value = sm.fullscreenMode;
        if (vsyncToggle != null) vsyncToggle.isOn = sm.vsyncEnabled;

        // 3. Controls
        if (mouseSensSlider != null) mouseSensSlider.value = sm.mouseSensitivity;
        if (mouseSensValText != null) mouseSensValText.text = $"{sm.mouseSensitivity:F1}x";
        if (invertYToggle != null) invertYToggle.isOn = sm.invertMouseY;
    }

    public void RefreshLocalizedUI()
    {
        // 1. Main Menu Texts
        if (mainMenuTitleText != null)
        {
            mainMenuTitleText.text = LocalizationManager.Get("game_title", "<b>Where's</b>\n<size=40><b>My Package</b></size>");
        }

        UpdateMainMenuSaveStats();

        SetButtonText(continueButton, LocalizationManager.Get("btn_continue", "DEVAM ET"));
        SetButtonText(newGameButton, LocalizationManager.Get("btn_new_game", "YENİ OYUN"));
        SetButtonText(playButton, LocalizationManager.Get("btn_play", "OYUNA BAŞLA"));
        SetButtonText(mainMenuSettingsButton, LocalizationManager.Get("btn_settings", "AYARLAR"));
        SetButtonText(mainMenuQuitButton, LocalizationManager.Get("btn_quit", "ÇIKIŞ"));

        // 2. New Game Confirmation Modal
        if (newGameModalPanel != null)
        {
            var mTitle = newGameModalPanel.transform.Find("ModalCard/Title")?.GetComponent<TextMeshProUGUI>();
            if (mTitle != null) mTitle.text = LocalizationManager.Get("modal_new_game_title", "<b>YENİ OYUN BAŞLAT</b>");

            var mBody = newGameModalPanel.transform.Find("ModalCard/BodyText")?.GetComponent<TextMeshProUGUI>();
            if (mBody != null) mBody.text = LocalizationManager.Get("modal_new_game_body", "Mevcut kayıt ve tüm şube ilerlemeniz sıfırlanarak 1. Seviyeden yeni bir kariyere başlanacaktır.\n\n<b>Emin misiniz?</b>");

            SetButtonText(confirmNewGameBtn, LocalizationManager.Get("btn_confirm_reset", "EVET, SIFIRLA VE BAŞLA"));
            SetButtonText(cancelNewGameBtn, LocalizationManager.Get("btn_cancel", "İPTAL"));
        }

        // 3. Pause Menu Texts
        if (pauseMenuPanel != null)
        {
            var pTitle = pauseMenuPanel.transform.Find("PauseCard/PauseTitle")?.GetComponent<TextMeshProUGUI>();
            if (pTitle != null) pTitle.text = LocalizationManager.Get("pause_title", "<b>OYUN DURAKLATILDI</b>\n<size=55%><color=#32FFFF>GAME PAUSED</color></size>");

            SetButtonText(resumeButton, LocalizationManager.Get("btn_resume", "DEVAM ET"));
            SetButtonText(pauseSettingsButton, LocalizationManager.Get("btn_settings", "AYARLAR"));
            SetButtonText(returnToMainMenuButton, LocalizationManager.Get("btn_return_main_menu", "ANA MENÜYE DÖN"));
            SetButtonText(pauseQuitButton, LocalizationManager.Get("btn_quit_desktop", "MASAÜSTÜNE ÇIK"));
        }

        // 4. Settings Panel Texts
        if (settingsPanel != null)
        {
            var sTitle = settingsPanel.transform.Find("SettingsCard/SettingsTitle")?.GetComponent<TextMeshProUGUI>();
            if (sTitle != null) sTitle.text = LocalizationManager.Get("settings_title", "<b>AYARLAR - SETTINGS</b>");

            SetButtonText(tabAudioBtn, LocalizationManager.Get("tab_audio", "SES"));
            SetButtonText(tabGraphicsBtn, LocalizationManager.Get("tab_graphics", "GRAFİK"));
            SetButtonText(tabControlsBtn, LocalizationManager.Get("tab_controls", "KONTROLLER"));
            SetButtonText(settingsBackButton, LocalizationManager.Get("btn_save_return", "KAYDET VE GERİ DÖN"));
            SetButtonText(resetKeybindingsBtn, LocalizationManager.Get("btn_reset_defaults", "Varsayılana Sıfırla"));

            var kbTitle = settingsPanel.transform.Find("SettingsCard/ContentArea/ControlsSection/KeybindingsHeaderRow/HeaderTitle")?.GetComponent<TextMeshProUGUI>();
            if (kbTitle != null) kbTitle.text = LocalizationManager.Get("setting_keybindings_title", "<color=#32FFFF><b>TUŞ ATAMALARI:</b></color> <size=80%><color=#85A8C8>(Değiştirmek istediğiniz tuşa tıklayın)</color></size>");

            // Refresh Fullscreen options text
            if (fullscreenDropdown != null)
            {
                int curVal = fullscreenDropdown.value;
                fullscreenDropdown.ClearOptions();
                fullscreenDropdown.AddOptions(new List<string> {
                    LocalizationManager.Get("disp_fullscreen", "Tam Ekran (Exclusive)"),
                    LocalizationManager.Get("disp_borderless", "Kenarlıksız (Borderless)"),
                    LocalizationManager.Get("disp_windowed", "Pencereli (Windowed)")
                });
                fullscreenDropdown.value = curVal;
                fullscreenDropdown.RefreshShownValue();
            }

            if (fpsLimitDropdown != null)
            {
                int curVal = fpsLimitDropdown.value;
                fpsLimitDropdown.ClearOptions();
                fpsLimitDropdown.AddOptions(new List<string> { "30 FPS", "60 FPS", "120 FPS", "144 FPS", LocalizationManager.Get("fps_unlimited", "Sınırsız (Unlimited)") });
                fpsLimitDropdown.value = curVal;
                fpsLimitDropdown.RefreshShownValue();
            }

            if (languageDropdown != null)
            {
                languageDropdown.value = LocalizationManager.IsTurkish ? 0 : 1;
                languageDropdown.RefreshShownValue();
            }
        }

        RefreshAllKeybindingUI();
    }

    private static void SetButtonText(Button btn, string txt)
    {
        if (btn == null) return;
        TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = txt;
    }

    #endregion

    #region --- KEYBINDINGS REBINDING METHODS ---

    public void StartKeyRebind(GameAction action, Button btn, TextMeshProUGUI btnText)
    {
        if (activeRebindingAction.HasValue && activeRebindingText != null)
        {
            InputBindingData prev = KeyBindingManager.GetBinding(activeRebindingAction.Value);
            activeRebindingText.text = prev.IsAssigned ? $"[ {prev.GetDisplayName()} ]" : "<color=#FF5555><b>[ Atanmadı ]</b></color>";
            activeRebindingText.color = prev.IsAssigned ? new Color(0.25f, 0.95f, 1.0f) : Color.white;
        }

        activeRebindingAction = action;
        activeRebindingButton = btn;
        activeRebindingText = btnText;
        rebindDebounceTimer = 0.15f;

        btnText.text = "<color=#FFE600><b>[ Tuşa / Fareye Basın... ]</b></color>";
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
    }

    public void CancelKeyRebind()
    {
        if (activeRebindingAction.HasValue && activeRebindingText != null)
        {
            InputBindingData prev = KeyBindingManager.GetBinding(activeRebindingAction.Value);
            activeRebindingText.text = prev.IsAssigned ? $"[ {prev.GetDisplayName()} ]" : "<color=#FF5555><b>[ Atanmadı ]</b></color>";
            activeRebindingText.color = prev.IsAssigned ? new Color(0.25f, 0.95f, 1.0f) : Color.white;
        }

        activeRebindingAction = null;
        activeRebindingButton = null;
        activeRebindingText = null;
        rebindDebounceTimer = 0f;
    }

    public void RefreshAllKeybindingUI()
    {
        foreach (var kvp in keybindingRowMap)
        {
            if (kvp.Value.text != null)
            {
                InputBindingData binding = KeyBindingManager.GetBinding(kvp.Key);
                if (binding.IsAssigned)
                {
                    kvp.Value.text.text = $"[ {binding.GetDisplayName()} ]";
                    kvp.Value.text.color = new Color(0.25f, 0.95f, 1.0f);
                }
                else
                {
                    kvp.Value.text.text = "<color=#FF5555><b>[ Atanmadı ]</b></color>";
                }
            }
        }
    }

    public void ResetKeybindingsToDefault()
    {
        CancelKeyRebind();
        KeyBindingManager.ResetToDefaults();
        RefreshAllKeybindingUI();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
    }

    #endregion

    #region --- BUTTON & SLIDER BINDINGS ---

    private void BindButtonsAndEvents()
    {
        // Main Menu Continue & New Game
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueButtonClicked);
        }
        if (newGameButton != null)
        {
            newGameButton.onClick.RemoveAllListeners();
            newGameButton.onClick.AddListener(OnNewGameButtonClicked);
        }
        if (playButton != null && playButton != continueButton && playButton != newGameButton)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnPlayButtonClicked);
        }
        if (confirmNewGameBtn != null)
        {
            confirmNewGameBtn.onClick.RemoveAllListeners();
            confirmNewGameBtn.onClick.AddListener(ExecuteNewGameResetAndStart);
        }
        if (cancelNewGameBtn != null)
        {
            cancelNewGameBtn.onClick.RemoveAllListeners();
            cancelNewGameBtn.onClick.AddListener(() =>
            {
                if (newGameModalPanel != null) newGameModalPanel.SetActive(false);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
            });
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

        // Keybindings Reset
        if (resetKeybindingsBtn != null)
        {
            resetKeybindingsBtn.onClick.RemoveAllListeners();
            resetKeybindingsBtn.onClick.AddListener(ResetKeybindingsToDefault);
        }

        // Keybindings Row Buttons
        foreach (var kvp in keybindingRowMap)
        {
            GameAction action = kvp.Key;
            Button btn = kvp.Value.button;
            TextMeshProUGUI txt = kvp.Value.text;
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => StartKeyRebind(action, btn, txt));
            }
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
