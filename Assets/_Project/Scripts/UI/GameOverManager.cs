using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Dedicated Game Over Manager.
/// Monitors the player's balance and triggers Game Over when debt reaches or exceeds the bankruptcy limit (-$1000).
/// Provides procedural UI fallback, custom UI inspector hooks, and a clean full-game restart mechanism.
/// </summary>
public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance { get; private set; }

    [Header("--- GAME OVER RULES & THRESHOLDS ---")]
    [Tooltip("Bankruptcy debt threshold where Game Over is triggered (e.g. -1000 means balance <= -1000).")]
    public int bankruptcyDebtLimit = -1000;

    [Tooltip("Starting cash restored when player restarts after Game Over.")]
    public int restartStartingCash = 0;

    [Header("--- TEXT CONTENT ---")]
    public string gameOverTitle = "İFLAS ETTİNİZ!";
    [TextArea(2, 4)]
    public string gameOverReason = "Borç limitini (-$1,000) aştığınız için lojistik şirketiniz iflas etti ve şubeniz kapatıldı.";
    public string restartButtonText = "YENİDEN BAŞLA (GÜN 1)";

    [Header("--- UI REFERENCES (OPTIONAL - AUTO-GENERATED IF EMPTY) ---")]
    public GameObject gameOverPanelRoot;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI reasonText;
    public TextMeshProUGUI debtAmountText;
    public Button restartButton;
    public CanvasGroup panelCanvasGroup;

    [Header("--- AUDIO & EFFECTS ---")]
    public AudioClip gameOverAudioClip;

    public static event Action OnGameOverTriggered;
    public static event Action OnGameRestarted;

    private bool isGameOverActive = false;
    public bool IsGameOver => isGameOverActive;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoEnsureGameOverManager()
    {
        if (Instance == null && UnityEngine.Object.FindAnyObjectByType<GameOverManager>() == null)
        {
            GameObject goObj = new GameObject("[GAME_OVER_MANAGER]");
            goObj.AddComponent<GameOverManager>();
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
        EnsureGameOverUI();
        BindRestartButton();

        if (gameOverPanelRoot != null)
        {
            gameOverPanelRoot.SetActive(false);
        }
    }

    private void OnEnable()
    {
        PlayerEconomyManager.OnEconomyUpdated += HandleEconomyUpdated;
    }

    private void OnDisable()
    {
        PlayerEconomyManager.OnEconomyUpdated -= HandleEconomyUpdated;
    }

    private void Start()
    {
        // Check current balance at start in case saved game is already at bankruptcy threshold
        if (PlayerEconomyManager.Instance != null)
        {
            CheckGameOverCondition(PlayerEconomyManager.Instance.CurrentLiveBalance);
        }
    }

    private void Update()
    {
        if (isGameOverActive)
        {
            if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }

    /// <summary>
    /// The restart click used to be wired only by the procedural fallback UI; a panel assigned in the scene
    /// (Kurye Defteri) never got a listener. Bind it for any assigned button.
    /// </summary>
    private void BindRestartButton()
    {
        if (restartButton == null && gameOverPanelRoot != null)
        {
            restartButton = gameOverPanelRoot.GetComponentInChildren<Button>(true);
        }
        if (restartButton == null) return;

        restartButton.onClick.RemoveListener(RestartGameFromBeginning);
        restartButton.onClick.AddListener(RestartGameFromBeginning);
    }

    private void HandleEconomyUpdated(int liveBalance, int todayNetProfit)
    {
        CheckGameOverCondition(liveBalance);
    }

    /// <summary>
    /// Evaluates if the current balance warrants a Game Over.
    /// </summary>
    public void CheckGameOverCondition(int currentBalance)
    {
        if (isGameOverActive) return;

        if (currentBalance <= bankruptcyDebtLimit)
        {
            TriggerGameOver(currentBalance);
        }
    }

    /// <summary>
    /// Displays the Game Over screen and halts active gameplay.
    /// </summary>
    [ContextMenu("Trigger Game Over (Debug Test)")]
    public void TriggerGameOver(int currentBalance = -1000)
    {
        if (isGameOverActive) return;
        isGameOverActive = true;

        EnsureGameOverUI();
        BindRestartButton();

        if (gameOverPanelRoot != null)
        {
            gameOverPanelRoot.SetActive(true);
            gameOverPanelRoot.transform.SetAsLastSibling();
        }

        if (titleText != null)
        {
            titleText.text = LocalizationManager.Get("game_over_title");
        }

        if (reasonText != null)
        {
            reasonText.text = LocalizationManager.Get("game_over_reason");
        }

        if (debtAmountText != null)
        {
            debtAmountText.text = LocalizationManager.GetFormat("game_over_balance", Mathf.Abs(currentBalance), Mathf.Abs(bankruptcyDebtLimit));
        }

        // Unlock mouse cursor for interaction
        FPSPlayerController.LockCursor(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 1. Wipe All Saved Data Immediately so the player cannot resume or exploit this bankrupt state
        WipeAllSaveDataPermanently();

        // Close other overlapping HUD menus
        if (CargoTabletUI.Instance != null && CargoTabletUI.Instance.IsTabletOpen)
        {
            CargoTabletUI.Instance.CloseTablet();
        }

        if (DaySummaryManager.Instance != null && DaySummaryManager.Instance.summaryPanelRoot != null)
        {
            DaySummaryManager.Instance.summaryPanelRoot.SetActive(false);
        }

        // Play Game Over Audio
        if (gameOverAudioClip != null)
        {
            AudioSource.PlayClipAtPoint(gameOverAudioClip, Camera.main != null ? Camera.main.transform.position : transform.position, 1.0f);
        }
        else if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayError();
        }

        OnGameOverTriggered?.Invoke();
        Debug.LogWarning($"<color=#FF2222>[GAME OVER] Player exceeded bankruptcy limit ({currentBalance} <= {bankruptcyDebtLimit})! All save files permanently deleted.</color>");
    }

    /// <summary>
    /// Deletes all PlayerPrefs keys permanently.
    /// </summary>
    public static void WipeAllSaveDataPermanently()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("<color=#FF3333>[GAME OVER] Permanent Wipe: All save progress and PlayerPrefs erased.</color>");
    }

    /// <summary>
    /// Fully resets player economy, progression, day count, and reloads the game clean from Day 1.
    /// </summary>
    [ContextMenu("Restart Game From Beginning")]
    public void RestartGameFromBeginning()
    {
        isGameOverActive = false;
        Time.timeScale = 1f;

        Debug.Log("<color=#32FF64>[GAME OVER RESTART] Re-initializing fresh Day 1 state and reloading scene...</color>");

        // 1. Ensure save data is completely wiped
        WipeAllSaveDataPermanently();

        // 2. Set Fresh Starting Values
        PlayerPrefs.SetInt("CARGO_PLAYER_TOTAL_BALANCE", restartStartingCash);
        PlayerPrefs.SetInt("Delivery_PlayerCash", restartStartingCash);
        PlayerPrefs.SetInt(DayTimeManager.PREFS_CURRENT_DAY, 1);
        PlayerPrefs.SetInt("Delivery_BranchLevel", 1);
        PlayerPrefs.SetInt("Delivery_PlayerLevel", 1);
        PlayerPrefs.SetInt("Delivery_WarehouseLevel", 1);
        PlayerPrefs.Save();

        if (PlayerEconomyManager.Instance != null)
        {
            PlayerEconomyManager.Instance.SetBalance(restartStartingCash);
        }

        if (DayTimeManager.Instance != null)
        {
            DayTimeManager.Instance.ResetDayCount();
        }

        if (BranchManager.Instance != null)
        {
            BranchManager.Instance.ResetBranchProgression();
        }

        if (PlayerProgressionManager.Instance != null)
        {
            PlayerProgressionManager.Instance.ResetProgression();
        }

        // 3. Reset Purchased Commercial Properties
        string[] propertyKeys = new string[]
        {
            "Property_Unlocked_property_showroom",
            "Property_Unlocked_property_insurance",
            "Property_Unlocked_property_service_garage",
            "Property_Unlocked_property_logistics_hub"
        };
        foreach (var pKey in propertyKeys)
        {
            PlayerPrefs.DeleteKey(pKey);
        }
        PlayerPrefs.Save();

        OnGameRestarted?.Invoke();

        // 4. Reload Active Scene
        Scene currentScene = SceneManager.GetActiveScene();
        if (currentScene.buildIndex >= 0)
        {
            SceneManager.LoadScene(currentScene.buildIndex);
        }
        else
        {
            SceneManager.LoadScene(currentScene.name);
        }
    }

    /// <summary>
    /// Builds procedural modern dark glassmorphism Game Over UI if none is assigned in the Inspector.
    /// </summary>
    public void EnsureGameOverUI()
    {
        if (gameOverPanelRoot != null) return;

        // Find or create Canvas
        Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("GameOver_Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // 1. Root Modal Overlay
        GameObject root = new GameObject("GameOver_Modal_Root");
        root.transform.SetParent(canvas.transform, false);
        RectTransform rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.sizeDelta = Vector2.zero;
        rootRect.anchoredPosition = Vector2.zero;

        Image backdrop = root.AddComponent<Image>();
        backdrop.color = new Color(0.04f, 0.04f, 0.06f, 0.94f); // Deep dark vignette

        panelCanvasGroup = root.AddComponent<CanvasGroup>();

        // 2. Center Card Box
        GameObject card = new GameObject("GameOver_Card");
        card.transform.SetParent(root.transform, false);
        RectTransform cardRect = card.AddComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(620, 480);
        cardRect.anchoredPosition = Vector2.zero;

        Image cardBg = card.AddComponent<Image>();
        cardBg.color = new Color(0.12f, 0.13f, 0.16f, 0.98f);
        Outline outline = card.AddComponent<Outline>();
        outline.effectColor = new Color(0.95f, 0.25f, 0.25f, 0.60f); // Red warning glow
        outline.effectDistance = new Vector2(2, -2);

        // 3. Red Warning Header Badge
        GameObject headerBadge = new GameObject("Header_Badge");
        headerBadge.transform.SetParent(card.transform, false);
        RectTransform badgeRect = headerBadge.AddComponent<RectTransform>();
        badgeRect.sizeDelta = new Vector2(540, 48);
        badgeRect.anchoredPosition = new Vector2(0, 185);

        Image badgeBg = headerBadge.AddComponent<Image>();
        badgeBg.color = new Color(0.85f, 0.15f, 0.15f, 0.25f);

        GameObject badgeTextObj = new GameObject("Badge_Text");
        badgeTextObj.transform.SetParent(headerBadge.transform, false);
        TextMeshProUGUI bText = badgeTextObj.AddComponent<TextMeshProUGUI>();
        bText.text = LocalizationManager.Get("game_over_badge");
        bText.fontSize = 20;
        bText.fontStyle = FontStyles.Bold;
        bText.alignment = TextAlignmentOptions.Center;
        bText.color = new Color(1f, 0.35f, 0.35f);
        RectTransform btRect = badgeTextObj.GetComponent<RectTransform>();
        btRect.anchorMin = Vector2.zero;
        btRect.anchorMax = Vector2.one;
        btRect.sizeDelta = Vector2.zero;

        // 4. Main Title Text
        GameObject titleObj = new GameObject("Title_Text");
        titleObj.transform.SetParent(card.transform, false);
        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = LocalizationManager.Get("game_over_title");
        titleText.fontSize = 42;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(1f, 0.95f, 0.95f);
        RectTransform tRect = titleObj.GetComponent<RectTransform>();
        tRect.sizeDelta = new Vector2(540, 60);
        tRect.anchoredPosition = new Vector2(0, 120);

        // 5. Debt Amount Text
        GameObject debtObj = new GameObject("Debt_Amount_Text");
        debtObj.transform.SetParent(card.transform, false);
        debtAmountText = debtObj.AddComponent<TextMeshProUGUI>();
        debtAmountText.text = LocalizationManager.GetFormat("game_over_balance", 1000, Mathf.Abs(bankruptcyDebtLimit));
        debtAmountText.fontSize = 28;
        debtAmountText.fontStyle = FontStyles.Bold;
        debtAmountText.alignment = TextAlignmentOptions.Center;
        RectTransform dRect = debtObj.GetComponent<RectTransform>();
        dRect.sizeDelta = new Vector2(540, 50);
        dRect.anchoredPosition = new Vector2(0, 55);

        // 6. Reason / Explanation Text
        GameObject reasonObj = new GameObject("Reason_Text");
        reasonObj.transform.SetParent(card.transform, false);
        reasonText = reasonObj.AddComponent<TextMeshProUGUI>();
        reasonText.text = LocalizationManager.Get("game_over_reason");
        reasonText.fontSize = 17;
        reasonText.alignment = TextAlignmentOptions.Center;
        reasonText.color = new Color(0.80f, 0.82f, 0.85f);
        RectTransform rRect = reasonObj.GetComponent<RectTransform>();
        rRect.sizeDelta = new Vector2(500, 80);
        rRect.anchoredPosition = new Vector2(0, -15);

        // 7. Restart Button
        GameObject btnObj = new GameObject("Restart_Button");
        btnObj.transform.SetParent(card.transform, false);
        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(440, 58);
        btnRect.anchoredPosition = new Vector2(0, -150);

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.20f, 0.65f, 0.35f, 1f); // Vibrant Emerald Green

        restartButton = btnObj.AddComponent<Button>();
        restartButton.onClick.AddListener(RestartGameFromBeginning);

        GameObject btnTextObj = new GameObject("Btn_Text");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
        btnText.text = $"🔄 {LocalizationManager.Get("game_over_btn_restart")}";
        btnText.fontSize = 22;
        btnText.fontStyle = FontStyles.Bold;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.color = Color.white;
        RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.sizeDelta = Vector2.zero;

        gameOverPanelRoot = root;
        gameOverPanelRoot.SetActive(false);
    }
}
