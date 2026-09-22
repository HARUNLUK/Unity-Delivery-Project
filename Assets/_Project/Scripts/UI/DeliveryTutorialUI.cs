using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// Delivery Tutorial & Game Objective Guide UI.
/// Displays an introductory modal at the start of the game explaining:
/// - Core objective: Load cargo into vehicle and deliver to correct house/building delivery points (mailboxes / drop zones).
/// - Day end evaluation: Correct delivery rewards vs wrong/broken penalties.
/// - Left side image container for the user's custom delivery point / mailbox sprite.
/// - Right side rich formatted objectives and guidelines.
/// </summary>
public class DeliveryTutorialUI : MonoBehaviour
{
    public static DeliveryTutorialUI Instance { get; private set; }

    public const string PREF_TUTORIAL_DONT_SHOW = "Delivery_Tutorial_DontShowAgain";

    [Header("--- TUTORIAL ACTIVATION SETTINGS ---")]
    [Tooltip("If true, automatically opens when game starts on Day 1 (if not marked as Don't Show Again)")]
    public bool autoShowOnGameStart = true;

    [Tooltip("Shortcut key to toggle/reopen the tutorial guide at any time during gameplay")]
    public KeyCode toggleKey = KeyCode.F1;

    [Header("--- CUSTOM IMAGE (POSTA KUTUSU / TESLİMAT NOKTASI RESMİ) ---")]
    [Tooltip("Drag & drop your custom delivery point / mailbox image sprite here")]
    public Sprite tutorialImage;

    [Tooltip("If true, keeps original aspect ratio. If false (default), image stretches to completely fill the frame box.")]
    public bool preserveImageAspect = false;

    [Header("--- INSPECTOR UI REFERENCES (OPTIONAL - AUTO-GENERATED IF NULL) ---")]
    public GameObject tutorialPanelRoot;
    public Image deliveryPointImageUI;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI leftCaptionText;
    public TextMeshProUGUI objectiveBodyText;
    public Button startButton;
    public Button closeButton;
    public Toggle dontShowAgainToggle;
    public CanvasGroup panelCanvasGroup;

    private bool isTutorialOpen = false;
    public bool IsOpen => isTutorialOpen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoEnsureTutorialUI()
    {
        if (Instance == null && UnityEngine.Object.FindAnyObjectByType<DeliveryTutorialUI>() == null)
        {
            GameObject goObj = new GameObject("[DELIVERY_TUTORIAL_UI]");
            goObj.AddComponent<DeliveryTutorialUI>();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureTutorialUI();
        BindButtons();

        if (tutorialPanelRoot != null)
        {
            tutorialPanelRoot.SetActive(false);
        }
    }

    private void OnEnable()
    {
        BindButtons();
    }

    private void Start()
    {
        BindButtons();

        // Check if tutorial should automatically open on game start
        if (autoShowOnGameStart)
        {
            // CRITICAL CHECK: Do NOT show tutorial if game is starting in Main Menu or any menu state!
            if (GameMenuManager.Instance != null && GameMenuManager.Instance.CurrentState != GameFlowState.Playing)
            {
                return;
            }

            CheckAndShowOnGameplayStart(isNewGame: false);
        }
    }

    /// <summary>
    /// Called when the player actually enters the gameplay state (e.g. from Main Menu 'New Game' or 'Continue', or direct scene play).
    /// </summary>
    public void CheckAndShowOnGameplayStart(bool isNewGame = false)
    {
        if (!autoShowOnGameStart) return;

        // If game menu or pause is currently open, do not pop up
        if (GameMenuManager.Instance != null && GameMenuManager.Instance.CurrentState != GameFlowState.Playing)
        {
            return;
        }

        if (isNewGame)
        {
            // For a brand new game, always show tutorial
            StartCoroutine(DelayedShowTutorial(0.45f));
            return;
        }

        bool dontShow = PlayerPrefs.GetInt(PREF_TUTORIAL_DONT_SHOW, 0) == 1;
        int currentDay = DayTimeManager.Instance != null ? DayTimeManager.Instance.CurrentDay : PlayerPrefs.GetInt("Delivery_CurrentDay", 1);

        // If it's Day 1 and player hasn't opted out, display tutorial with a tiny delay so scene settles smoothly
        if (!dontShow && currentDay == 1)
        {
            StartCoroutine(DelayedShowTutorial(0.45f));
        }
    }

    private IEnumerator DelayedShowTutorial(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        // Double check state before actually showing
        if (GameMenuManager.Instance != null && GameMenuManager.Instance.CurrentState != GameFlowState.Playing)
        {
            yield break;
        }

        ShowTutorial();
    }

    private void Update()
    {
        // Don't process tutorial keys or remain open if in main menu
        if (GameMenuManager.Instance != null && GameMenuManager.Instance.CurrentState == GameFlowState.MainMenu)
        {
            if (isTutorialOpen) HideTutorial();
            return;
        }

        bool togglePressed = false;
        bool escapePressed = false;

        if (Keyboard.current != null)
        {
            togglePressed = Keyboard.current.f1Key.wasPressedThisFrame;
            escapePressed = Keyboard.current.escapeKey.wasPressedThisFrame;
        }
        else
        {
            try
            {
                togglePressed = Input.GetKeyDown(toggleKey);
                escapePressed = Input.GetKeyDown(KeyCode.Escape);
            }
            catch { }
        }

        // Toggle hotkey
        if (togglePressed)
        {
            if (isTutorialOpen)
            {
                HideTutorial();
            }
            else
            {
                ShowTutorial();
            }
        }

        // Close on ESC if open
        if (isTutorialOpen && escapePressed)
        {
            HideTutorial();
        }

        if (isTutorialOpen)
        {
            if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }

    public void ShowTutorial()
    {
        EnsureTutorialUI();
        BindButtons();

        if (tutorialPanelRoot == null) return;

        tutorialPanelRoot.SetActive(true);
        isTutorialOpen = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMenuOpen();
        }

        if (panelCanvasGroup != null)
        {
            StopAllCoroutines();
            StartCoroutine(FadeCanvasGroup(panelCanvasGroup, panelCanvasGroup.alpha, 1f, 0.22f));
        }
    }

    public void HideTutorial()
    {
        if (!isTutorialOpen && (tutorialPanelRoot == null || !tutorialPanelRoot.activeSelf)) return;

        // Save Don't Show Again preference if toggle exists
        if (dontShowAgainToggle != null && dontShowAgainToggle.isOn)
        {
            PlayerPrefs.SetInt(PREF_TUTORIAL_DONT_SHOW, 1);
            PlayerPrefs.Save();
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMenuClose();
        }

        if (panelCanvasGroup != null && gameObject.activeInHierarchy)
        {
            StopAllCoroutines();
            StartCoroutine(FadeCanvasGroup(panelCanvasGroup, panelCanvasGroup.alpha, 0f, 0.18f, () =>
            {
                if (tutorialPanelRoot != null) tutorialPanelRoot.SetActive(false);
                isTutorialOpen = false;
                RestoreCursorState();
            }));
        }
        else
        {
            if (tutorialPanelRoot != null) tutorialPanelRoot.SetActive(false);
            isTutorialOpen = false;
            RestoreCursorState();
        }
    }

    public void ToggleTutorial()
    {
        if (isTutorialOpen) HideTutorial();
        else ShowTutorial();
    }

    private void RestoreCursorState()
    {
        if (FPSPlayerController.IsAnyUIOpen())
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void SetTutorialImage(Sprite sprite)
    {
        tutorialImage = sprite;
        if (deliveryPointImageUI != null && sprite != null)
        {
            deliveryPointImageUI.sprite = sprite;
        }
    }

    public void BindButtons()
    {
        if (startButton == null && tutorialPanelRoot != null)
        {
            startButton = tutorialPanelRoot.GetComponentInChildren<Button>(true);
        }

        if (startButton != null)
        {
            startButton.onClick.RemoveListener(HideTutorial);
            startButton.onClick.AddListener(HideTutorial);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HideTutorial);
            closeButton.onClick.AddListener(HideTutorial);
        }
    }

    public void UpdateContent()
    {
        // Intentionally empty: styles, texts, and visuals are customized directly in Unity Editor by the user.
    }

    public void EnsureTutorialUI()
    {
        if (tutorialPanelRoot != null)
        {
            BindButtons();
            return;
        }

        // Try to find existing panel in active Canvas first before creating anything new
        Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
        if (canvas != null)
        {
            Transform foundRoot = canvas.transform.Find("DeliveryTutorial_Modal_Root") ??
                                  canvas.transform.Find("DeliveryTutorial_Canvas") ??
                                  canvas.transform.Find("Tutorial_Card_Window");
            if (foundRoot != null)
            {
                tutorialPanelRoot = foundRoot.gameObject;
                panelCanvasGroup = tutorialPanelRoot.GetComponent<CanvasGroup>();
                BindButtons();
                return;
            }
        }

        // Find or create Canvas if none exists
        if (canvas == null)
        {
            canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
        }

        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("DeliveryTutorial_Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // 1. Root Modal Overlay
        GameObject root = new GameObject("DeliveryTutorial_Modal_Root");
        root.transform.SetParent(canvas.transform, false);
        RectTransform rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.sizeDelta = Vector2.zero;
        rootRect.anchoredPosition = Vector2.zero;

        Image backdrop = root.AddComponent<Image>();
        backdrop.color = new Color(0.02f, 0.03f, 0.05f, 0.90f); // Sleek dark vignette backdrop

        panelCanvasGroup = root.AddComponent<CanvasGroup>();
        panelCanvasGroup.alpha = 1f;

        // 2. Center Modal Container Window (Width: 1060, Height: 640)
        GameObject card = new GameObject("Tutorial_Card_Window");
        card.transform.SetParent(root.transform, false);
        RectTransform cardRect = card.AddComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(1060, 640);
        cardRect.anchoredPosition = Vector2.zero;

        Image cardBg = card.AddComponent<Image>();
        cardBg.color = new Color(0.09f, 0.11f, 0.14f, 0.98f);
        Outline outline = card.AddComponent<Outline>();
        outline.effectColor = new Color(0.18f, 0.65f, 1.0f, 0.50f); // Cyan subtle glow border
        outline.effectDistance = new Vector2(2, -2);

        // 3. Top Header Bar
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
        titleText = titleObj.AddComponent<TextMeshProUGUI>();
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
        closeButton = closeBtnObj.AddComponent<Button>();
        closeButton.onClick.AddListener(HideTutorial);

        GameObject closeTxtObj = new GameObject("X_Text");
        closeTxtObj.transform.SetParent(closeBtnObj.transform, false);
        TextMeshProUGUI closeTxt = closeTxtObj.AddComponent<TextMeshProUGUI>();
        closeTxt.text = "✕";
        closeTxt.fontSize = 20;
        closeTxt.alignment = TextAlignmentOptions.Center;
        closeTxt.color = Color.white;
        RectTransform ctRect = closeTxtObj.GetComponent<RectTransform>();
        ctRect.anchorMin = Vector2.zero;
        ctRect.anchorMax = Vector2.one;
        ctRect.sizeDelta = Vector2.zero;

        // 4. Split Content Area (Y: -70 to bottom -80)
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
        imgFrame.AddComponent<RectMask2D>(); // Cleanly clips the image to the exact frame boundaries

        // Image Object (100% Full Fill)
        GameObject imgObj = new GameObject("Delivery_Point_Image");
        imgObj.transform.SetParent(imgFrame.transform, false);
        RectTransform imgRect = imgObj.AddComponent<RectTransform>();
        imgRect.anchorMin = Vector2.zero;
        imgRect.anchorMax = Vector2.one;
        imgRect.offsetMin = Vector2.zero;
        imgRect.offsetMax = Vector2.zero;

        deliveryPointImageUI = imgObj.AddComponent<Image>();
        deliveryPointImageUI.preserveAspect = preserveImageAspect;
        if (tutorialImage != null)
        {
            deliveryPointImageUI.sprite = tutorialImage;
            deliveryPointImageUI.color = Color.white;
        }
        else
        {
            deliveryPointImageUI.color = new Color(0.18f, 0.24f, 0.32f, 0.9f);
        }

        // Left Sub-Caption Box
        GameObject captionObj = new GameObject("Left_Caption_Text");
        captionObj.transform.SetParent(leftCol.transform, false);
        leftCaptionText = captionObj.AddComponent<TextMeshProUGUI>();
        leftCaptionText.text = "<b>Teslimat Noktası (Posta Kutusu)</b>\n<size=15><color=#A0C8FF>Kargoları binaların önündeki bu sarı/yeşil teslimat alanına veya posta kutusunun yanına bırakın.</color></size>";
        leftCaptionText.fontSize = 17;
        leftCaptionText.alignment = TextAlignmentOptions.TopLeft;
        leftCaptionText.color = Color.white;
        RectTransform capRect = captionObj.GetComponent<RectTransform>();
        capRect.anchorMin = new Vector2(0, 0);
        capRect.anchorMax = new Vector2(1, 0.25f);
        capRect.offsetMin = new Vector2(4, 0);
        capRect.offsetMax = new Vector2(-4, 0);

        // --- RIGHT COLUMN: OBJECTIVE & INSTRUCTIONS (Width: remaining 540) ---
        GameObject rightCol = new GameObject("Right_Text_Column");
        rightCol.transform.SetParent(contentArea.transform, false);
        RectTransform rightRect = rightCol.AddComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(0, 0);
        rightRect.anchorMax = new Vector2(1, 1);
        rightRect.offsetMin = new Vector2(450, 0);
        rightRect.offsetMax = Vector2.zero;

        GameObject bodyObj = new GameObject("Objective_Body_Text");
        bodyObj.transform.SetParent(rightCol.transform, false);
        objectiveBodyText = bodyObj.AddComponent<TextMeshProUGUI>();
        objectiveBodyText.fontSize = 16.5f;
        objectiveBodyText.lineSpacing = 12f;
        objectiveBodyText.alignment = TextAlignmentOptions.TopLeft;
        objectiveBodyText.color = new Color(0.92f, 0.94f, 0.96f);
        RectTransform bRect = bodyObj.GetComponent<RectTransform>();
        bRect.anchorMin = Vector2.zero;
        bRect.anchorMax = Vector2.one;
        bRect.sizeDelta = Vector2.zero;

        // 5. Bottom Action Bar (Height: 70)
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

        startButton = startBtnObj.AddComponent<Button>();
        startButton.onClick.AddListener(HideTutorial);

        GameObject sbTxtObj = new GameObject("Button_Text");
        sbTxtObj.transform.SetParent(startBtnObj.transform, false);
        TextMeshProUGUI sbTxt = sbTxtObj.AddComponent<TextMeshProUGUI>();
        sbTxt.text = "<b>ANLADIM, VARDİYAYA BAŞLA</b>";
        sbTxt.fontSize = 17;
        sbTxt.fontStyle = FontStyles.Bold;
        sbTxt.alignment = TextAlignmentOptions.Center;
        sbTxt.color = Color.white;
        RectTransform sbtRect = sbTxtObj.GetComponent<RectTransform>();
        sbtRect.anchorMin = Vector2.zero;
        sbtRect.anchorMax = Vector2.one;
        sbtRect.sizeDelta = Vector2.zero;

        tutorialPanelRoot = root;
        UpdateContent();
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration, Action onComplete = null)
    {
        float elapsed = 0f;
        cg.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        cg.alpha = to;
        onComplete?.Invoke();
    }
}
