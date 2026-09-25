using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class InteractionPromptHUD : MonoBehaviour
{
    private static InteractionPromptHUD instance;
    public static InteractionPromptHUD Instance
    {
        get
        {
            if (instance == null)
            {
                instance = UnityEngine.Object.FindAnyObjectByType<InteractionPromptHUD>(FindObjectsInactive.Include);
                if (instance == null)
                {
                    Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
                    if (canvas == null)
                    {
                        GameObject canvasObj = new GameObject("HUD_Canvas");
                        canvas = canvasObj.AddComponent<Canvas>();
                        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                        scaler.referenceResolution = new Vector2(1920, 1080);
                        scaler.matchWidthOrHeight = 0.5f;
                        canvasObj.AddComponent<GraphicRaycaster>();
                    }
                    instance = canvas.gameObject.AddComponent<InteractionPromptHUD>();
                    instance.EnsureUI();
                }
            }
            if (instance != null && !instance.gameObject.activeSelf)
            {
                instance.gameObject.SetActive(true);
            }
            return instance;
        }
        private set => instance = value;
    }

    [Header("--- UI REFERENCES ---")]
    public GameObject crosshairDot;
    public GameObject promptPanel;
    public TextMeshProUGUI promptText;

    [Header("--- THROW CHARGE SLIDER / BAR ---")]
    public GameObject throwSlideBG;
    public Image throwBarFill;
    public Slider throwBarSlider;
    public RectTransform throwBarRect;

    [Header("--- HELD CARGO SIDE PANEL ---")]
    public GameObject heldCargoPanel;
    public TextMeshProUGUI heldCargoHeaderText;
    public TextMeshProUGUI heldCargoTypeText;
    public TextMeshProUGUI heldCargoStatusText;
    public TextMeshProUGUI heldCargoPercentageText;
    public TextMeshProUGUI heldCargoRecipientLabelText;
    public TextMeshProUGUI heldCargoRecipientText;
    public TextMeshProUGUI heldCargoAddressLabelText;
    public TextMeshProUGUI heldCargoAddressText;
    public TextMeshProUGUI heldCargoClueLabelText;
    public TextMeshProUGUI heldCargoAddressDescText;
    public TextMeshProUGUI heldCargoRewardLabelText;
    public TextMeshProUGUI heldCargoRewardText;
    public TextMeshProUGUI heldCargoPenaltyLabelText;
    public TextMeshProUGUI heldCargoPenaltyText;
    public TextMeshProUGUI heldCargoActionHintText;

    [Header("--- VEHICLE DASHBOARD GAUGE (FUEL & CONDITION) ---")]
    public GameObject vehicleDashboardRoot;
    public GameObject fuelGaugePanel;
    public TextMeshProUGUI fuelLabelText;
    public Image fuelBarFill;
    public TextMeshProUGUI fuelValueText;
    public GameObject conditionGaugePanel;
    public TextMeshProUGUI conditionLabelText;
    public Image conditionBarFill;
    public TextMeshProUGUI conditionValueText;

    [Header("--- PROMPT TIMEOUT SETTINGS ---")]
    [Tooltip("Duration in seconds for on-screen interaction and hint prompts to automatically hide")]
    public float defaultPromptDuration = 2.0f;

    private float currentPromptTimer = 0f;
    private float suppressUntilTime = 0f;
    private CanvasGroup promptCanvasGroup;
    private PhysicalCargoPackage currentHeldPackage;

    private void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
        AddressLocalizationManager.OnLanguageChanged += HandleLanguageChanged;
        RefreshLocalizedUI();
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
        AddressLocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
    }

    private void HandleLanguageChanged(string newLang)
    {
        RefreshLocalizedUI();
    }

    public void RefreshLocalizedUI()
    {
        // 1. Held Cargo Side Card Labels
        if (heldCargoHeaderText != null)
            heldCargoHeaderText.text = LocalizationManager.Get("hud_held_cargo_header", "TAŞINAN KARGO");

        if (heldCargoRecipientLabelText != null)
            heldCargoRecipientLabelText.text = LocalizationManager.Get("hud_held_recipient", "ALICI:");

        if (heldCargoAddressLabelText != null)
            heldCargoAddressLabelText.text = LocalizationManager.Get("hud_held_address", "ADRES:");

        if (heldCargoClueLabelText != null)
            heldCargoClueLabelText.text = LocalizationManager.Get("hud_held_clue", "İPUCU & TARİF:");

        if (heldCargoRewardLabelText != null)
            heldCargoRewardLabelText.text = LocalizationManager.Get("hud_held_reward", "ÖDÜL:");

        if (heldCargoPenaltyLabelText != null)
            heldCargoPenaltyLabelText.text = LocalizationManager.Get("hud_held_penalty", "CEZA:");

        if (heldCargoActionHintText != null)
            heldCargoActionHintText.text = LocalizationManager.Get("hud_held_action_hint", "KONTROLLER: [Sol Tık] Fırlat (Şarjlı) | [Sağ Tık] Yavaşça Bırak");

        // 2. Vehicle Dashboard Gauge Labels
        if (fuelLabelText != null)
            fuelLabelText.text = LocalizationManager.Get("hud_fuel_label", "YAKIT");

        if (conditionLabelText != null)
            conditionLabelText.text = LocalizationManager.Get("hud_condition_label", "KONDİSYON");

        // 3. Re-evaluate currently held package info if card is visible
        if (currentHeldPackage != null && heldCargoPanel != null && heldCargoPanel.activeSelf)
        {
            ShowHeldCargoInfo(currentHeldPackage);
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            EnsureUI();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }

    private void Reset()
    {
        EnsureUI();
    }

    private void Awake()
    {
        Instance = this;
        EnsureUI();
    }

    private void Start()
    {
        EnsureUI();
        HideThrowCharge();
        HidePrompt();
        HideHeldCargoInfo();
        HideFuelHUD();
        RefreshLocalizedUI();
    }

    private void Update()
    {
        if (promptPanel != null && promptPanel.activeSelf)
        {
            CheckPromptDismissInput();

            if (promptPanel != null && promptPanel.activeSelf && currentPromptTimer > 0f)
            {
                currentPromptTimer -= Time.deltaTime;
                if (currentPromptTimer <= 0.45f && promptCanvasGroup != null)
                {
                    promptCanvasGroup.alpha = Mathf.Clamp01(currentPromptTimer / 0.45f);
                }
                if (currentPromptTimer <= 0f)
                {
                    HidePrompt();
                }
            }
        }
    }

    public void SuppressPrompts(float duration = 0.35f)
    {
        suppressUntilTime = Time.unscaledTime + duration;
        HideThrowCharge();
        HidePrompt();
    }

    private void CheckPromptDismissInput()
    {
        if (promptPanel == null || !promptPanel.activeSelf || promptText == null || !promptText.gameObject.activeSelf || string.IsNullOrEmpty(promptText.text)) return;

        string txt = promptText.text;
        bool dismissed = false;

#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        var mouse = Mouse.current;

        if (kb != null)
        {
            if (txt.IndexOf("[E]", System.StringComparison.OrdinalIgnoreCase) >= 0 && kb.eKey.wasPressedThisFrame) dismissed = true;
            else if (txt.IndexOf("[F]", System.StringComparison.OrdinalIgnoreCase) >= 0 && kb.fKey.wasPressedThisFrame) dismissed = true;
            else if (txt.IndexOf("[TAB]", System.StringComparison.OrdinalIgnoreCase) >= 0 && kb.tabKey.wasPressedThisFrame) dismissed = true;
            else if (txt.IndexOf("[V]", System.StringComparison.OrdinalIgnoreCase) >= 0 && kb.vKey.wasPressedThisFrame) dismissed = true;
            else if (txt.IndexOf("[R]", System.StringComparison.OrdinalIgnoreCase) >= 0 && kb.rKey.wasPressedThisFrame) dismissed = true;
            else if (txt.IndexOf("[G]", System.StringComparison.OrdinalIgnoreCase) >= 0 && kb.gKey.wasPressedThisFrame) dismissed = true;
            else if (txt.IndexOf("[Q]", System.StringComparison.OrdinalIgnoreCase) >= 0 && kb.qKey.wasPressedThisFrame) dismissed = true;
            else if (txt.IndexOf("[C]", System.StringComparison.OrdinalIgnoreCase) >= 0 && kb.cKey.wasPressedThisFrame) dismissed = true;
            else if ((txt.IndexOf("[Space]", System.StringComparison.OrdinalIgnoreCase) >= 0 || txt.IndexOf("[Boşluk]", System.StringComparison.OrdinalIgnoreCase) >= 0) && kb.spaceKey.wasPressedThisFrame) dismissed = true;
            else if (txt.IndexOf("[ESC]", System.StringComparison.OrdinalIgnoreCase) >= 0 && kb.escapeKey.wasPressedThisFrame) dismissed = true;
            else if (txt.IndexOf("[F9]", System.StringComparison.OrdinalIgnoreCase) >= 0 && kb.f9Key.wasPressedThisFrame) dismissed = true;
        }

        if (mouse != null && !dismissed)
        {
            if ((txt.IndexOf("[LMB]", System.StringComparison.OrdinalIgnoreCase) >= 0 || txt.IndexOf("[Sol Tık]", System.StringComparison.OrdinalIgnoreCase) >= 0) && mouse.leftButton.wasPressedThisFrame) dismissed = true;
            else if ((txt.IndexOf("[RMB]", System.StringComparison.OrdinalIgnoreCase) >= 0 || txt.IndexOf("[Sağ Tık]", System.StringComparison.OrdinalIgnoreCase) >= 0) && mouse.rightButton.wasPressedThisFrame) dismissed = true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        try
        {
            if (!dismissed)
            {
                if (txt.IndexOf("[E]", System.StringComparison.OrdinalIgnoreCase) >= 0 && Input.GetKeyDown(KeyCode.E)) dismissed = true;
                else if (txt.IndexOf("[F]", System.StringComparison.OrdinalIgnoreCase) >= 0 && Input.GetKeyDown(KeyCode.F)) dismissed = true;
                else if (txt.IndexOf("[TAB]", System.StringComparison.OrdinalIgnoreCase) >= 0 && Input.GetKeyDown(KeyCode.Tab)) dismissed = true;
                else if (txt.IndexOf("[V]", System.StringComparison.OrdinalIgnoreCase) >= 0 && Input.GetKeyDown(KeyCode.V)) dismissed = true;
                else if (txt.IndexOf("[R]", System.StringComparison.OrdinalIgnoreCase) >= 0 && Input.GetKeyDown(KeyCode.R)) dismissed = true;
                else if (txt.IndexOf("[G]", System.StringComparison.OrdinalIgnoreCase) >= 0 && Input.GetKeyDown(KeyCode.G)) dismissed = true;
                else if (txt.IndexOf("[Q]", System.StringComparison.OrdinalIgnoreCase) >= 0 && Input.GetKeyDown(KeyCode.Q)) dismissed = true;
                else if (txt.IndexOf("[C]", System.StringComparison.OrdinalIgnoreCase) >= 0 && Input.GetKeyDown(KeyCode.C)) dismissed = true;
                else if ((txt.IndexOf("[Space]", System.StringComparison.OrdinalIgnoreCase) >= 0 || txt.IndexOf("[Boşluk]", System.StringComparison.OrdinalIgnoreCase) >= 0) && Input.GetKeyDown(KeyCode.Space)) dismissed = true;
                else if (txt.IndexOf("[ESC]", System.StringComparison.OrdinalIgnoreCase) >= 0 && Input.GetKeyDown(KeyCode.Escape)) dismissed = true;
                else if (txt.IndexOf("[F9]", System.StringComparison.OrdinalIgnoreCase) >= 0 && Input.GetKeyDown(KeyCode.F9)) dismissed = true;
                else if ((txt.IndexOf("[LMB]", System.StringComparison.OrdinalIgnoreCase) >= 0 || txt.IndexOf("[Sol Tık]", System.StringComparison.OrdinalIgnoreCase) >= 0) && Input.GetMouseButtonDown(0)) dismissed = true;
                else if ((txt.IndexOf("[RMB]", System.StringComparison.OrdinalIgnoreCase) >= 0 || txt.IndexOf("[Sağ Tık]", System.StringComparison.OrdinalIgnoreCase) >= 0) && Input.GetMouseButtonDown(1)) dismissed = true;
            }
        }
        catch { }
#endif

        if (dismissed)
        {
            SuppressPrompts(0.35f);
        }
    }

    [ContextMenu("Auto-Bind All UI References")]
    public void EnsureUI(bool forceRecreate = false)
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();

        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("HUD_Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        transform.SetParent(canvas.transform, false);

        // 1. Center Crosshair Dot
        if (forceRecreate && crosshairDot != null)
        {
            DestroyImmediate(crosshairDot);
            crosshairDot = null;
        }

        if (crosshairDot == null)
        {
            Transform existingDot = canvas.transform.Find("CenterCrosshair");
            if (existingDot != null && forceRecreate)
            {
                DestroyImmediate(existingDot.gameObject);
                existingDot = null;
            }

            if (existingDot != null)
            {
                crosshairDot = existingDot.gameObject;
            }
            else
            {
                crosshairDot = new GameObject("CenterCrosshair");
                crosshairDot.transform.SetParent(canvas.transform, false);
                RectTransform dotRect = crosshairDot.AddComponent<RectTransform>();
                dotRect.anchorMin = new Vector2(0.5f, 0.5f);
                dotRect.anchorMax = new Vector2(0.5f, 0.5f);
                dotRect.sizeDelta = new Vector2(9, 9);

                Image dotImg = crosshairDot.AddComponent<Image>();
                dotImg.color = new Color(1f, 1f, 1f, 0.9f);
                dotImg.raycastTarget = false;
            }
        }

        // 2. Center Interaction Prompt Box (Large & Clean)
        if (forceRecreate && promptPanel != null)
        {
            DestroyImmediate(promptPanel);
            promptPanel = null;
            promptText = null;
        }

        if (promptPanel == null || promptText == null)
        {
            Transform existingPrompt = canvas.transform.Find("InteractionPromptBox");
            if (existingPrompt != null && forceRecreate)
            {
                DestroyImmediate(existingPrompt.gameObject);
                existingPrompt = null;
            }

            if (existingPrompt != null)
            {
                promptPanel = existingPrompt.gameObject;
                promptText = promptPanel.GetComponentInChildren<TextMeshProUGUI>();
            }
            else
            {
                promptPanel = new GameObject("InteractionPromptBox");
                promptPanel.transform.SetParent(canvas.transform, false);
                RectTransform boxRect = promptPanel.AddComponent<RectTransform>();
                boxRect.anchorMin = new Vector2(0.5f, 0.5f);
                boxRect.anchorMax = new Vector2(0.5f, 0.5f);
                boxRect.pivot = new Vector2(0.5f, 1f);
                boxRect.anchoredPosition = new Vector2(0, -40);
                boxRect.sizeDelta = new Vector2(580, 64);

                Image boxImg = promptPanel.AddComponent<Image>();
                boxImg.color = new Color(0.05f, 0.07f, 0.11f, 0.94f);
                boxImg.raycastTarget = false;

                GameObject textObj = new GameObject("PromptText");
                textObj.transform.SetParent(promptPanel.transform, false);
                RectTransform textRect = textObj.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;

                promptText = textObj.AddComponent<TextMeshProUGUI>();
                promptText.fontSize = 24;
                promptText.fontStyle = FontStyles.Bold;
                promptText.alignment = TextAlignmentOptions.Center;
                promptText.color = new Color(1f, 0.88f, 0.25f);
                promptText.raycastTarget = false;
            }
        }

        // 3. Throw Charge Slider/Bar (Directly under Canvas or inside InteractionPromptBox)
        BindThrowSlideReferences(canvas != null ? canvas.transform : transform);

        // 4. Held Cargo Side Details Panel (Strictly bind to existing scene object, NEVER modify design)
        Transform existingSide = canvas.transform.Find("HeldCargoSideCard");
        if (existingSide == null)
        {
            var allT = canvas.GetComponentsInChildren<Transform>(true);
            foreach (var t in allT)
            {
                if (t != null && t.name.Equals("HeldCargoSideCard", System.StringComparison.OrdinalIgnoreCase))
                {
                    existingSide = t;
                    break;
                }
            }
        }

        if (existingSide != null)
        {
            BindHeldCargoReferences(existingSide);
        }

        // 5. In-Vehicle Fuel & Condition Dashboard Panels (Bottom Right)
        Transform existingDash = vehicleDashboardRoot != null ? vehicleDashboardRoot.transform : null;
        if (existingDash == null && canvas != null)
        {
            existingDash = canvas.transform.Find("VehicleDashboardPanel");
            if (existingDash == null)
            {
                var allT = canvas.GetComponentsInChildren<Transform>(true);
                foreach (var t in allT)
                {
                    if (t != null && t.name.Equals("VehicleDashboardPanel", System.StringComparison.OrdinalIgnoreCase))
                    {
                        existingDash = t;
                        break;
                    }
                }
            }
        }
        if (existingDash == null)
        {
            var allCanvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in allCanvases)
            {
                if (c == null) continue;
                var allT = c.GetComponentsInChildren<Transform>(true);
                foreach (var t in allT)
                {
                    if (t != null && t.name.Equals("VehicleDashboardPanel", System.StringComparison.OrdinalIgnoreCase))
                    {
                        existingDash = t;
                        break;
                    }
                }
                if (existingDash != null) break;
            }
        }

        if (existingDash != null)
        {
            BindVehicleDashboardReferences(existingDash);
        }
    }

    private void BindThrowSlideReferences(Transform root)
    {
        if (root == null) return;

        if (throwSlideBG == null)
        {
            Transform tBG = root.Find("ThrowSlideBG");
            if (tBG == null)
            {
                var allT = root.GetComponentsInChildren<Transform>(true);
                foreach (var t in allT)
                {
                    if (t != null && t.name.Equals("ThrowSlideBG", System.StringComparison.OrdinalIgnoreCase))
                    {
                        tBG = t;
                        break;
                    }
                }
            }

            if (tBG != null)
            {
                throwSlideBG = tBG.gameObject;
            }
        }

        if (throwSlideBG != null)
        {
            Transform barT = throwSlideBG.transform.Find("bar");
            if (barT == null)
            {
                var allB = throwSlideBG.GetComponentsInChildren<Transform>(true);
                foreach (var b in allB)
                {
                    if (b != null && b != throwSlideBG.transform && (b.name.Equals("bar", System.StringComparison.OrdinalIgnoreCase) || b.name.IndexOf("bar", System.StringComparison.OrdinalIgnoreCase) >= 0 || b.name.IndexOf("fill", System.StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        barT = b;
                        break;
                    }
                }
            }

            if (barT != null)
            {
                throwBarFill = barT.GetComponent<Image>();
                throwBarSlider = throwSlideBG.GetComponent<Slider>();
                if (throwBarSlider == null) throwBarSlider = barT.GetComponent<Slider>();
                throwBarRect = barT.GetComponent<RectTransform>();

                if (throwBarFill != null && throwBarSlider == null)
                {
                    if (throwBarFill.type != Image.Type.Filled)
                    {
                        throwBarFill.type = Image.Type.Filled;
                        throwBarFill.fillMethod = Image.FillMethod.Horizontal;
                        throwBarFill.fillOrigin = 0;
                    }
                }
            }

            // Initially ensure throw slide bar is hidden
            if (throwSlideBG.activeSelf)
            {
                throwSlideBG.SetActive(false);
            }
        }
    }

    public void SetThrowCharge(float chargePercent)
    {
        if (throwSlideBG == null)
        {
            EnsureUI();
        }

        // Hide regular prompt box & text so no prompt text is shown during throw charging
        if (promptPanel != null && promptPanel.activeSelf)
        {
            promptPanel.SetActive(false);
        }
        else if (promptText != null && promptText.gameObject.activeSelf)
        {
            promptText.gameObject.SetActive(false);
        }

        // Show throw charge bar
        if (throwSlideBG != null && !throwSlideBG.activeSelf)
        {
            throwSlideBG.SetActive(true);
        }

        chargePercent = Mathf.Clamp01(chargePercent);

        if (throwBarSlider != null)
        {
            throwBarSlider.value = chargePercent;
        }
        else if (throwBarFill != null)
        {
            if (throwBarFill.type != Image.Type.Filled)
            {
                throwBarFill.type = Image.Type.Filled;
                throwBarFill.fillMethod = Image.FillMethod.Horizontal;
                throwBarFill.fillOrigin = 0;
            }
            throwBarFill.fillAmount = chargePercent;
        }
    }

    public void HideThrowCharge()
    {
        if (throwSlideBG != null && throwSlideBG.activeSelf)
        {
            throwSlideBG.SetActive(false);
        }
        if (promptText != null && !promptText.gameObject.activeSelf)
        {
            promptText.gameObject.SetActive(true);
        }
    }

    public void ShowPrompt(string message, float duration = 2.0f)
    {
        if (Time.unscaledTime < suppressUntilTime) return;

        if (string.IsNullOrEmpty(message))
        {
            HidePrompt();
            return;
        }

        if (promptPanel == null || promptText == null)
        {
            EnsureUI();
        }

        HideThrowCharge();

        if (promptPanel != null && promptText != null)
        {
            promptText.gameObject.SetActive(true);
            promptText.text = message;
            if (!promptPanel.activeSelf)
            {
                promptPanel.SetActive(true);
            }
            currentPromptTimer = duration > 0f ? duration : defaultPromptDuration;

            if (promptCanvasGroup == null)
            {
                promptCanvasGroup = promptPanel.GetComponent<CanvasGroup>();
                if (promptCanvasGroup == null) promptCanvasGroup = promptPanel.AddComponent<CanvasGroup>();
            }

            if (promptCanvasGroup != null)
            {
                promptCanvasGroup.alpha = 1f;
            }
        }
    }

    public void HidePrompt()
    {
        currentPromptTimer = 0f;
        HideThrowCharge();
        if (promptCanvasGroup != null)
        {
            promptCanvasGroup.alpha = 1f;
        }
        if (promptPanel != null && promptPanel.activeSelf)
        {
            promptPanel.SetActive(false);
        }
    }

    public void UpdateVehicleHUD(float currentFuel, float maxFuel, bool isLow, float currentCondition, float maxCondition)
    {
        if (vehicleDashboardRoot == null || fuelValueText == null || fuelBarFill == null || conditionValueText == null || conditionBarFill == null || fuelLabelText == null || conditionLabelText == null)
        {
            EnsureUI();
        }

        if (vehicleDashboardRoot != null && !vehicleDashboardRoot.activeSelf)
        {
            vehicleDashboardRoot.SetActive(true);
        }
        if (fuelGaugePanel != null && !fuelGaugePanel.activeSelf)
        {
            fuelGaugePanel.SetActive(true);
        }
        if (conditionGaugePanel != null && !conditionGaugePanel.activeSelf)
        {
            conditionGaugePanel.SetActive(true);
        }

        if (fuelLabelText != null)
            fuelLabelText.text = LocalizationManager.Get("hud_fuel_label", "YAKIT");

        if (conditionLabelText != null)
            conditionLabelText.text = LocalizationManager.Get("hud_condition_label", "KONDİSYON");

        float fuelPct = maxFuel > 0 ? Mathf.Clamp01(currentFuel / maxFuel) : 0f;
        float condPct = maxCondition > 0 ? Mathf.Clamp01(currentCondition / maxCondition) : 0f;

        // 1. Fuel Bar & Value - ONLY adjust width according to fuel percentage, NEVER touch image type or design
        if (fuelBarFill != null)
        {
            SetBarFillWidth(fuelBarFill, fuelPct);
        }

        if (fuelValueText != null)
        {
            fuelValueText.text = $"{currentFuel:F1}/{maxFuel:F1}L";
        }

        // 2. Condition Bar & Value - ONLY adjust width according to condition percentage, NEVER touch image type or design
        if (conditionBarFill != null)
        {
            SetBarFillWidth(conditionBarFill, condPct);
        }

        if (conditionValueText != null)
        {
            conditionValueText.text = $"{(condPct * 100f):F0}%";
        }
    }

    private void SetBarFillWidth(Image fillImg, float fillPct)
    {
        if (fillImg == null) return;
        fillPct = Mathf.Clamp01(fillPct);

        RectTransform rt = fillImg.rectTransform;
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(fillPct, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        if (fillImg.gameObject != null)
        {
            if (fillPct <= 0.001f)
            {
                if (fillImg.gameObject.activeSelf) fillImg.gameObject.SetActive(false);
            }
            else
            {
                if (!fillImg.gameObject.activeSelf) fillImg.gameObject.SetActive(true);
            }
        }
    }

    public void UpdateFuelHUD(float currentFuel, float maxFuel, bool isLow)
    {
        UpdateVehicleHUD(currentFuel, maxFuel, isLow, 100f, 100f);
    }

    public void HideFuelHUD()
    {
        if (vehicleDashboardRoot != null)
        {
            vehicleDashboardRoot.SetActive(false);
        }
        else if (fuelGaugePanel != null)
        {
            fuelGaugePanel.SetActive(false);
        }
    }

    private TextMeshProUGUI CreateTMPChild(GameObject parent, string childName, string defaultText, float fontSize, FontStyles style, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
    {
        if (parent == null) return null;
        GameObject obj = new GameObject(childName);
        obj.transform.SetParent(parent.transform, false);

        if (obj.GetComponent<CanvasRenderer>() == null) obj.AddComponent<CanvasRenderer>();
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = defaultText;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        return tmp;
    }

    private TextMeshProUGUI FindTMPRecursive(Transform root, params string[] searchNames)
    {
        if (root == null) return null;
        var allTexts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var name in searchNames)
        {
            foreach (var t in allTexts)
            {
                if (t != null && t.gameObject.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                    return t;
            }
        }
        foreach (var name in searchNames)
        {
            foreach (var t in allTexts)
            {
                if (t != null && t.gameObject.name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return t;
            }
        }
        return null;
    }

    private Image FindImageRecursive(Transform root, params string[] searchNames)
    {
        if (root == null) return null;
        var allImages = root.GetComponentsInChildren<Image>(true);
        foreach (var name in searchNames)
        {
            foreach (var img in allImages)
            {
                if (img != null && img.gameObject.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                    return img;
            }
        }
        foreach (var name in searchNames)
        {
            foreach (var img in allImages)
            {
                if (img != null && img.gameObject.name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return img;
            }
        }
        return null;
    }

    private TextMeshProUGUI FindTMPDirect(Transform root, params string[] searchNames)
    {
        if (root == null) return null;
        var allTexts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var name in searchNames)
        {
            foreach (var t in allTexts)
            {
                if (t != null && t.gameObject.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                    return t;
            }
        }
        return null;
    }

    private void BindHeldCargoReferences(Transform sideCard)
    {
        if (sideCard == null) return;
        heldCargoPanel = sideCard.gameObject;

        // Top -> Header, Type, Status, Percentage
        Transform top = sideCard.Find("Top");
        if (top != null)
        {
            Transform headerT = top.Find("Header") ?? top.Find("header") ?? top.Find("Title") ?? top.Find("title");
            if (headerT != null) heldCargoHeaderText = headerT.GetComponent<TextMeshProUGUI>();

            Transform horiz = top.Find("Horizontal");
            if (horiz != null)
            {
                Transform typeT = horiz.Find("Type") ?? horiz.Find("type");
                if (typeT != null) heldCargoTypeText = typeT.GetComponent<TextMeshProUGUI>();

                Transform statusT = horiz.Find("Status") ?? horiz.Find("status");
                if (statusT != null) heldCargoStatusText = statusT.GetComponent<TextMeshProUGUI>();

                Transform pctT = horiz.Find("Percentage") ?? horiz.Find("percentage");
                if (pctT != null) heldCargoPercentageText = pctT.GetComponent<TextMeshProUGUI>();
            }
        }

        if (heldCargoHeaderText == null) heldCargoHeaderText = FindTMPRecursive(sideCard, "Header", "PackageHeader", "Title");
        if (heldCargoTypeText == null) heldCargoTypeText = FindTMPDirect(sideCard, "Type", "CargoType");
        if (heldCargoStatusText == null) heldCargoStatusText = FindTMPDirect(sideCard, "Status", "Durum");
        if (heldCargoPercentageText == null) heldCargoPercentageText = FindTMPDirect(sideCard, "Percentage", "Percent");

        // Detail -> recipient / value and address / value
        Transform detail = sideCard.Find("Detail");
        if (detail != null)
        {
            Transform recipGroup = detail.Find("recipient");
            if (recipGroup != null)
            {
                Transform val = recipGroup.Find("value") ?? recipGroup.Find("Value");
                if (val != null) heldCargoRecipientText = val.GetComponent<TextMeshProUGUI>();

                Transform title = recipGroup.Find("title") ?? recipGroup.Find("Title") ?? recipGroup.Find("label") ?? recipGroup.Find("Label") ?? recipGroup.Find("Text") ?? recipGroup.Find("Header");
                if (title != null) heldCargoRecipientLabelText = title.GetComponent<TextMeshProUGUI>();

                if (heldCargoRecipientLabelText == null)
                {
                    var tmps = recipGroup.GetComponentsInChildren<TextMeshProUGUI>(true);
                    foreach (var t in tmps)
                    {
                        if (t != heldCargoRecipientText)
                        {
                            heldCargoRecipientLabelText = t;
                            break;
                        }
                    }
                }
            }

            Transform addrGroup = detail.Find("address");
            if (addrGroup != null)
            {
                Transform val = addrGroup.Find("value") ?? addrGroup.Find("Value");
                if (val != null) heldCargoAddressText = val.GetComponent<TextMeshProUGUI>();

                Transform title = addrGroup.Find("title") ?? addrGroup.Find("Title") ?? addrGroup.Find("label") ?? addrGroup.Find("Label") ?? addrGroup.Find("Text") ?? addrGroup.Find("Header");
                if (title != null) heldCargoAddressLabelText = title.GetComponent<TextMeshProUGUI>();

                if (heldCargoAddressLabelText == null)
                {
                    var tmps = addrGroup.GetComponentsInChildren<TextMeshProUGUI>(true);
                    foreach (var t in tmps)
                    {
                        if (t != heldCargoAddressText)
                        {
                            heldCargoAddressLabelText = t;
                            break;
                        }
                    }
                }
            }
        }
        if (heldCargoRecipientLabelText == null) heldCargoRecipientLabelText = FindTMPRecursive(sideCard, "RecipientLabel", "LabelRecipient", "recipient/title", "recipient/label");
        if (heldCargoRecipientText == null) heldCargoRecipientText = FindTMPRecursive(sideCard, "RecipientValue", "RecipientText", "recipient/value");
        if (heldCargoAddressLabelText == null) heldCargoAddressLabelText = FindTMPRecursive(sideCard, "AddressLabel", "LabelAddress", "address/title", "address/label");
        if (heldCargoAddressText == null) heldCargoAddressText = FindTMPRecursive(sideCard, "AddressValue", "AddressText", "address/value");

        // ClueBox -> ClueTitle and ClueScrollView/Content/AddressDescription
        Transform clueBox = sideCard.Find("ClueBox");
        if (clueBox != null)
        {
            Transform title = clueBox.Find("ClueTitle") ?? clueBox.Find("clueTitle") ?? clueBox.Find("title") ?? clueBox.Find("Title") ?? clueBox.Find("Header") ?? clueBox.Find("label");
            if (title != null) heldCargoClueLabelText = title.GetComponent<TextMeshProUGUI>();

            Transform descT = clueBox.Find("ClueScrollView/Content/AddressDescription") ?? clueBox.Find("Content/AddressDescription") ?? clueBox.Find("AddressDescription");
            if (descT == null)
            {
                var allT = clueBox.GetComponentsInChildren<Transform>(true);
                foreach (var t in allT)
                {
                    if (t.name.IndexOf("Description", System.StringComparison.OrdinalIgnoreCase) >= 0 || t.name.IndexOf("AddressDescription", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        descT = t;
                        break;
                    }
                }
            }
            if (descT != null) heldCargoAddressDescText = descT.GetComponent<TextMeshProUGUI>();

            if (heldCargoClueLabelText == null)
            {
                var tmps = clueBox.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var t in tmps)
                {
                    if (t != heldCargoAddressDescText)
                    {
                        heldCargoClueLabelText = t;
                        break;
                    }
                }
            }
        }
        if (heldCargoClueLabelText == null) heldCargoClueLabelText = FindTMPRecursive(sideCard, "ClueHeader", "ClueLabel", "ClueTitle", "ClueBox/ClueTitle");
        if (heldCargoAddressDescText == null) heldCargoAddressDescText = FindTMPRecursive(sideCard, "AddressDescription", "Description", "ClueDesc");

        // Bottom -> reward / value and penalty / value
        Transform bottom = sideCard.Find("Bottom");
        if (bottom != null)
        {
            Transform rewardGroup = bottom.Find("reward");
            if (rewardGroup != null)
            {
                Transform val = rewardGroup.Find("value") ?? rewardGroup.Find("Value");
                if (val != null) heldCargoRewardText = val.GetComponent<TextMeshProUGUI>();

                Transform title = rewardGroup.Find("title") ?? rewardGroup.Find("Title") ?? rewardGroup.Find("label") ?? rewardGroup.Find("Label") ?? rewardGroup.Find("Text") ?? rewardGroup.Find("Header");
                if (title != null) heldCargoRewardLabelText = title.GetComponent<TextMeshProUGUI>();

                if (heldCargoRewardLabelText == null)
                {
                    var tmps = rewardGroup.GetComponentsInChildren<TextMeshProUGUI>(true);
                    foreach (var t in tmps)
                    {
                        if (t != heldCargoRewardText)
                        {
                            heldCargoRewardLabelText = t;
                            break;
                        }
                    }
                }
            }

            Transform penaltyGroup = bottom.Find("penalty");
            if (penaltyGroup != null)
            {
                Transform val = penaltyGroup.Find("value") ?? penaltyGroup.Find("Value");
                if (val != null) heldCargoPenaltyText = val.GetComponent<TextMeshProUGUI>();

                Transform title = penaltyGroup.Find("title") ?? penaltyGroup.Find("Title") ?? penaltyGroup.Find("label") ?? penaltyGroup.Find("Label") ?? penaltyGroup.Find("Text") ?? penaltyGroup.Find("Header");
                if (title != null) heldCargoPenaltyLabelText = title.GetComponent<TextMeshProUGUI>();

                if (heldCargoPenaltyLabelText == null)
                {
                    var tmps = penaltyGroup.GetComponentsInChildren<TextMeshProUGUI>(true);
                    foreach (var t in tmps)
                    {
                        if (t != heldCargoPenaltyText)
                        {
                            heldCargoPenaltyLabelText = t;
                            break;
                        }
                    }
                }
            }
        }
        if (heldCargoRewardLabelText == null) heldCargoRewardLabelText = FindTMPRecursive(sideCard, "RewardLabel", "LabelReward", "reward/title", "reward/label");
        if (heldCargoRewardText == null) heldCargoRewardText = FindTMPRecursive(sideCard, "RewardValue", "RewardText", "reward/value");
        if (heldCargoPenaltyLabelText == null) heldCargoPenaltyLabelText = FindTMPRecursive(sideCard, "PenaltyLabel", "LabelPenalty", "penalty/title", "penalty/label");
        if (heldCargoPenaltyText == null) heldCargoPenaltyText = FindTMPRecursive(sideCard, "PenaltyValue", "PenaltyText", "penalty/value");

        // Action hint / controls hint
        Transform actionHintT = sideCard.Find("ActionLabel") ?? sideCard.Find("ActionHint") ?? sideCard.Find("Controls");
        if (actionHintT != null) heldCargoActionHintText = actionHintT.GetComponent<TextMeshProUGUI>();
        if (heldCargoActionHintText == null) heldCargoActionHintText = FindTMPRecursive(sideCard, "ActionLabel", "ActionHint", "ActionHintText", "Controls", "Hint");
    }

    private void BindVehicleDashboardReferences(Transform dashRoot)
    {
        if (dashRoot == null) return;
        vehicleDashboardRoot = dashRoot.gameObject;

        // 1. Fuel Card & Components
        Transform fCard = dashRoot.Find("FuelGaugeCard");
        if (fCard == null)
        {
            var allT = dashRoot.GetComponentsInChildren<Transform>(true);
            foreach (var t in allT)
            {
                if (t != null && t.name.Equals("FuelGaugeCard", System.StringComparison.OrdinalIgnoreCase))
                {
                    fCard = t;
                    break;
                }
            }
        }
        fuelGaugePanel = fCard != null ? fCard.gameObject : vehicleDashboardRoot;

        Transform fTitleT = (fCard ?? dashRoot).Find("Horizontal/FuelTitle") ?? (fCard ?? dashRoot).Find("FuelTitle") ?? (fCard ?? dashRoot).Find("Horizontal/FuelLabel") ?? (fCard ?? dashRoot).Find("FuelLabel");
        if (fTitleT != null) fuelLabelText = fTitleT.GetComponent<TextMeshProUGUI>();
        if (fuelLabelText == null) fuelLabelText = FindTMPRecursive(fCard ?? dashRoot, "FuelTitle", "FuelLabel", "FuelHeader");

        Transform fValT = (fCard ?? dashRoot).Find("Horizontal/FuelValue") ?? (fCard ?? dashRoot).Find("FuelValue");
        if (fValT != null) fuelValueText = fValT.GetComponent<TextMeshProUGUI>();
        if (fuelValueText == null) fuelValueText = FindTMPRecursive(fCard ?? dashRoot, "FuelValue", "FuelVal", "FuelAmount", "FuelText");

        Transform fFillT = (fCard ?? dashRoot).Find("FuelBarBg/FuelBarFill") ?? (fCard ?? dashRoot).Find("FuelBarFill");
        if (fFillT != null) fuelBarFill = fFillT.GetComponent<Image>();
        if (fuelBarFill == null) fuelBarFill = FindImageRecursive(fCard ?? dashRoot, "FuelBarFill", "FuelFill", "FuelBar");

        // 2. Condition Card & Components
        Transform cCard = dashRoot.Find("ConditionGaugeCard");
        if (cCard == null)
        {
            var allT = dashRoot.GetComponentsInChildren<Transform>(true);
            foreach (var t in allT)
            {
                if (t != null && t.name.Equals("ConditionGaugeCard", System.StringComparison.OrdinalIgnoreCase))
                {
                    cCard = t;
                    break;
                }
            }
        }
        conditionGaugePanel = cCard != null ? cCard.gameObject : vehicleDashboardRoot;

        Transform cTitleT = (cCard ?? dashRoot).Find("Horizontal/CondTitle") ?? (cCard ?? dashRoot).Find("CondTitle") ?? (cCard ?? dashRoot).Find("Horizontal/CondLabel") ?? (cCard ?? dashRoot).Find("CondLabel");
        if (cTitleT != null) conditionLabelText = cTitleT.GetComponent<TextMeshProUGUI>();
        if (conditionLabelText == null) conditionLabelText = FindTMPRecursive(cCard ?? dashRoot, "CondTitle", "CondLabel", "ConditionTitle", "ConditionLabel");

        Transform cValT = (cCard ?? dashRoot).Find("Horizontal/CondValue") ?? (cCard ?? dashRoot).Find("CondValue");
        if (cValT != null) conditionValueText = cValT.GetComponent<TextMeshProUGUI>();
        if (conditionValueText == null) conditionValueText = FindTMPRecursive(cCard ?? dashRoot, "CondValue", "ConditionValue", "CondVal", "ConditionText", "CondText");

        Transform cFillT = (cCard ?? dashRoot).Find("CondBarBg/CondBarFill") ?? (cCard ?? dashRoot).Find("CondBarFill");
        if (cFillT != null) conditionBarFill = cFillT.GetComponent<Image>();
        if (conditionBarFill == null) conditionBarFill = FindImageRecursive(cCard ?? dashRoot, "CondBarFill", "ConditionBarFill", "CondFill", "ConditionFill");
    }

    public void ShowHeldCargoInfo(PhysicalCargoPackage pkg)
    {
        currentHeldPackage = pkg;
        if (pkg == null)
        {
            HideHeldCargoInfo();
            return;
        }

        if (heldCargoPanel == null || heldCargoRecipientText == null || heldCargoRecipientLabelText == null)
        {
            EnsureUI();
        }

        if (heldCargoPanel != null && !heldCargoPanel.activeSelf)
        {
            heldCargoPanel.SetActive(true);
        }

        // Always ensure static localized labels are up-to-date
        if (heldCargoHeaderText != null)
            heldCargoHeaderText.text = LocalizationManager.Get("hud_held_cargo_header", "TAŞINAN KARGO");

        if (heldCargoRecipientLabelText != null)
            heldCargoRecipientLabelText.text = LocalizationManager.Get("hud_held_recipient", "ALICI:");

        if (heldCargoAddressLabelText != null)
            heldCargoAddressLabelText.text = LocalizationManager.Get("hud_held_address", "ADRES:");

        if (heldCargoClueLabelText != null)
            heldCargoClueLabelText.text = LocalizationManager.Get("hud_held_clue", "İPUCU & TARİF:");

        if (heldCargoRewardLabelText != null)
            heldCargoRewardLabelText.text = LocalizationManager.Get("hud_held_reward", "ÖDÜL:");

        if (heldCargoPenaltyLabelText != null)
            heldCargoPenaltyLabelText.text = LocalizationManager.Get("hud_held_penalty", "CEZA:");

        if (heldCargoActionHintText != null)
            heldCargoActionHintText.text = LocalizationManager.Get("hud_held_action_hint", "KONTROLLER: [Sol Tık] Fırlat (Şarjlı) | [Sağ Tık] Yavaşça Bırak");

        string recipient = !string.IsNullOrEmpty(pkg.EffectiveRecipientName) ? pkg.EffectiveRecipientName : pkg.recipientName;
        string address = !string.IsNullOrEmpty(pkg.EffectiveAddressName) ? pkg.EffectiveAddressName : pkg.targetAddressName;
        string desc = !string.IsNullOrEmpty(pkg.EffectiveAddressDescription) ? pkg.EffectiveAddressDescription : pkg.targetAddressDescription;

        // ONLY UPDATE VALUES - NEVER TOUCH LABELS, DESIGN, OR POSITIONS
        if (heldCargoRecipientText != null)
        {
            heldCargoRecipientText.text = recipient;
        }

        if (heldCargoAddressText != null)
        {
            heldCargoAddressText.text = address;
        }
        
        if (heldCargoAddressDescText != null)
        {
            heldCargoAddressDescText.text = !string.IsNullOrEmpty(desc) ? desc : LocalizationManager.Get("cargo_no_clue", "Bu adres için özel bir ipucu bulunmuyor.");
        }

        if (heldCargoRewardText != null)
        {
            heldCargoRewardText.text = $"+${pkg.deliveryReward}";
        }

        if (heldCargoPenaltyText != null)
        {
            heldCargoPenaltyText.text = $"-${pkg.wrongPenalty}";
        }

        if (heldCargoTypeText != null)
        {
            switch (pkg.cargoType)
            {
                case CargoType.Standard:
                    heldCargoTypeText.text = LocalizationManager.Get("cargo_type_standard", "STANDART");
                    if (heldCargoStatusText != null) heldCargoStatusText.text = "";
                    if (heldCargoPercentageText != null) heldCargoPercentageText.text = "";
                    break;

                case CargoType.Express:
                    heldCargoTypeText.text = LocalizationManager.Get("cargo_type_express", "EKSPRES");
                    if (heldCargoStatusText != null) heldCargoStatusText.text = pkg.GetFormattedTargetDeliveryTime();
                    if (heldCargoPercentageText != null) heldCargoPercentageText.text = "";
                    break;

                case CargoType.Fragile:
                    heldCargoTypeText.text = LocalizationManager.Get("cargo_type_fragile", "KIRILGAN");
                    if (heldCargoStatusText != null) heldCargoStatusText.text = pkg.isBroken ? LocalizationManager.Get("cargo_status_broken", "Kırıldı / Hasarlı") : LocalizationManager.Get("cargo_status_condition", "Kondisyon");
                    if (heldCargoPercentageText != null) heldCargoPercentageText.text = pkg.isBroken ? "0%" : $"{pkg.health:F0}%";
                    break;

                case CargoType.Explosive:
                    heldCargoTypeText.text = LocalizationManager.Get("cargo_type_explosive", "PATLAYICI");
                    if (heldCargoStatusText != null) heldCargoStatusText.text = pkg.isBroken ? LocalizationManager.Get("cargo_status_detonated", "Patladı!") : LocalizationManager.Get("cargo_status_stability", "Stabilite");
                    if (heldCargoPercentageText != null) heldCargoPercentageText.text = pkg.isBroken ? "0%" : $"{pkg.health:F0}%";
                    break;

                default:
                    heldCargoTypeText.text = LocalizationManager.Get("cargo_type_standard", "STANDART");
                    if (heldCargoStatusText != null) heldCargoStatusText.text = "";
                    if (heldCargoPercentageText != null) heldCargoPercentageText.text = "";
                    break;
            }
        }
    }

    public void HideHeldCargoInfo()
    {
        currentHeldPackage = null;
        if (heldCargoPanel != null)
        {
            heldCargoPanel.SetActive(false);
        }
    }

#if UNITY_EDITOR
    [MenuItem("Tools/Delivery Game/Create Interaction Prompt HUD", false, 43)]
    public static void CreatePromptHUDTool()
    {
        Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("HUD_Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        InteractionPromptHUD hud = canvas.GetComponentInChildren<InteractionPromptHUD>(true);
        if (hud == null)
        {
            hud = canvas.gameObject.AddComponent<InteractionPromptHUD>();
        }
        hud.EnsureUI(true);
        EditorUtility.SetDirty(canvas.gameObject);
        EditorUtility.SetDirty(hud.gameObject);
        Debug.Log("<color=#32FF64>[InteractionPromptHUD] Interaction HUD & Held Cargo Card successfully created and refreshed!</color>");
    }
#endif
}
