using UnityEngine;
using TMPro;
using UnityEngine.UI;
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
            return instance;
        }
        private set => instance = value;
    }

    [Header("--- UI REFERENCES ---")]
    public GameObject crosshairDot;
    public GameObject promptPanel;
    public TextMeshProUGUI promptText;

    [Header("--- HELD CARGO SIDE PANEL ---")]
    public GameObject heldCargoPanel;
    public TextMeshProUGUI heldCargoHeaderText;
    public TextMeshProUGUI heldCargoTypeText;
    public TextMeshProUGUI heldCargoStatusText;
    public TextMeshProUGUI heldCargoPercentageText;
    public TextMeshProUGUI heldCargoRecipientText;
    public TextMeshProUGUI heldCargoAddressText;
    public TextMeshProUGUI heldCargoAddressDescText;
    public TextMeshProUGUI heldCargoRewardText;
    public TextMeshProUGUI heldCargoPenaltyText;
    public TextMeshProUGUI heldCargoActionHintText;

    [Header("--- VEHICLE DASHBOARD GAUGE (FUEL & CONDITION) ---")]
    public GameObject vehicleDashboardRoot;
    public GameObject fuelGaugePanel;
    public Image fuelBarFill;
    public TextMeshProUGUI fuelValueText;
    public GameObject conditionGaugePanel;
    public Image conditionBarFill;
    public TextMeshProUGUI conditionValueText;

    [Header("--- PROMPT TIMEOUT SETTINGS ---")]
    [Tooltip("Duration in seconds for on-screen interaction and hint prompts to automatically hide")]
    public float defaultPromptDuration = 5.0f;

    private float currentPromptTimer = 0f;
    private CanvasGroup promptCanvasGroup;
    private PhysicalCargoPackage currentHeldPackage;

    private void OnEnable()
    {
        AddressLocalizationManager.OnLanguageChanged += HandleLanguageChanged;
    }

    private void OnDisable()
    {
        AddressLocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
    }

    private void HandleLanguageChanged(string newLang)
    {
        if (currentHeldPackage != null && heldCargoPanel != null && heldCargoPanel.activeSelf)
        {
            ShowHeldCargoInfo(currentHeldPackage);
        }
    }

    private void Awake()
    {
        Instance = this;
        EnsureUI();
    }

    private void Start()
    {
        EnsureUI();
        HidePrompt();
        HideHeldCargoInfo();
        HideFuelHUD();
    }

    private void Update()
    {
        if (promptPanel != null && promptPanel.activeSelf)
        {
            if (currentPromptTimer > 0f)
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

        // 3. Held Cargo Side Details Panel (Strictly bind to existing scene object, NEVER modify design)
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

        // 4. In-Vehicle Fuel & Condition Dashboard Panels (Bottom Right)
        if (vehicleDashboardRoot == null || fuelGaugePanel == null || conditionGaugePanel == null || forceRecreate)
        {
            if (forceRecreate)
            {
                Transform oldSinglePanel = canvas.transform.Find("VehicleFuelGaugePanel");
                if (oldSinglePanel != null) DestroyImmediate(oldSinglePanel.gameObject);

                Transform oldDash = canvas.transform.Find("VehicleDashboardPanel");
                if (oldDash != null) DestroyImmediate(oldDash.gameObject);
            }

            Transform existingDash = canvas.transform.Find("VehicleDashboardPanel");
            if (existingDash != null && !forceRecreate)
            {
                vehicleDashboardRoot = existingDash.gameObject;
                Transform fCard = vehicleDashboardRoot.transform.Find("FuelGaugeCard");
                if (fCard != null)
                {
                    fuelGaugePanel = fCard.gameObject;
                    fuelBarFill = fCard.Find("FuelBarBg/FuelBarFill")?.GetComponent<Image>();
                    fuelValueText = fCard.Find("FuelValue")?.GetComponent<TextMeshProUGUI>();
                }

                Transform cCard = vehicleDashboardRoot.transform.Find("ConditionGaugeCard");
                if (cCard != null)
                {
                    conditionGaugePanel = cCard.gameObject;
                    conditionBarFill = cCard.Find("CondBarBg/CondBarFill")?.GetComponent<Image>();
                    conditionValueText = cCard.Find("CondValue")?.GetComponent<TextMeshProUGUI>();
                }
            }
            else
            {
                // Root Container
                vehicleDashboardRoot = new GameObject("VehicleDashboardPanel");
                vehicleDashboardRoot.transform.SetParent(canvas.transform, false);

                RectTransform dashRect = vehicleDashboardRoot.AddComponent<RectTransform>();
                dashRect.anchorMin = new Vector2(1f, 0f);
                dashRect.anchorMax = new Vector2(1f, 0f);
                dashRect.pivot = new Vector2(1f, 0f);
                dashRect.anchoredPosition = new Vector2(-40, 40);
                dashRect.sizeDelta = new Vector2(340, 160);

                VerticalLayoutGroup vLayout = vehicleDashboardRoot.AddComponent<VerticalLayoutGroup>();
                vLayout.padding = new RectOffset(0, 0, 0, 0);
                vLayout.spacing = 10;
                vLayout.childControlWidth = true;
                vLayout.childControlHeight = false;
                vLayout.childForceExpandWidth = true;
                vLayout.childForceExpandHeight = false;

                // --- 1. SEPARATE FUEL CARD ---
                fuelGaugePanel = new GameObject("FuelGaugeCard");
                fuelGaugePanel.transform.SetParent(vehicleDashboardRoot.transform, false);

                LayoutElement fuelLayout = fuelGaugePanel.AddComponent<LayoutElement>();
                fuelLayout.preferredHeight = 72;
                fuelLayout.minHeight = 72;
                fuelLayout.flexibleHeight = 0;

                Image fuelBg = fuelGaugePanel.AddComponent<Image>();
                fuelBg.color = new Color(0.06f, 0.08f, 0.12f, 0.94f);
                fuelBg.raycastTarget = false;

                // Fuel Title (Left)
                GameObject fuelTitleObj = new GameObject("FuelTitle");
                fuelTitleObj.transform.SetParent(fuelGaugePanel.transform, false);
                RectTransform ftTitleRect = fuelTitleObj.AddComponent<RectTransform>();
                ftTitleRect.anchorMin = new Vector2(0f, 0.5f);
                ftTitleRect.anchorMax = new Vector2(0.45f, 1f);
                ftTitleRect.anchoredPosition = new Vector2(14, -2);
                ftTitleRect.sizeDelta = new Vector2(0, 0);

                TextMeshProUGUI ftTitleTmp = fuelTitleObj.AddComponent<TextMeshProUGUI>();
                ftTitleTmp.text = "⛽ YAKIT";
                ftTitleTmp.fontSize = 15;
                ftTitleTmp.fontStyle = FontStyles.Bold;
                ftTitleTmp.alignment = TextAlignmentOptions.MidlineLeft;
                ftTitleTmp.color = new Color(1f, 0.85f, 0.25f);
                ftTitleTmp.raycastTarget = false;

                // Fuel Value (Right)
                GameObject fuelValObj = new GameObject("FuelValue");
                fuelValObj.transform.SetParent(fuelGaugePanel.transform, false);
                RectTransform ftValRect = fuelValObj.AddComponent<RectTransform>();
                ftValRect.anchorMin = new Vector2(0.45f, 0.5f);
                ftValRect.anchorMax = new Vector2(1f, 1f);
                ftValRect.anchoredPosition = new Vector2(-14, -2);
                ftValRect.sizeDelta = new Vector2(0, 0);

                fuelValueText = fuelValObj.AddComponent<TextMeshProUGUI>();
                fuelValueText.text = "50.0 / 50.0 L (100%)";
                fuelValueText.fontSize = 15;
                fuelValueText.fontStyle = FontStyles.Bold;
                fuelValueText.alignment = TextAlignmentOptions.MidlineRight;
                fuelValueText.color = Color.white;
                fuelValueText.raycastTarget = false;

                // Fuel Bar Background
                GameObject fuelBarBgObj = new GameObject("FuelBarBg");
                fuelBarBgObj.transform.SetParent(fuelGaugePanel.transform, false);
                RectTransform fbbgRect = fuelBarBgObj.AddComponent<RectTransform>();
                fbbgRect.anchorMin = new Vector2(0f, 0f);
                fbbgRect.anchorMax = new Vector2(1f, 0f);
                fbbgRect.pivot = new Vector2(0.5f, 0f);
                fbbgRect.anchoredPosition = new Vector2(0, 12);
                fbbgRect.sizeDelta = new Vector2(-28, 10);

                Image fBarBgImg = fuelBarBgObj.AddComponent<Image>();
                fBarBgImg.color = new Color(0.12f, 0.16f, 0.22f, 0.95f);
                fBarBgImg.raycastTarget = false;

                // Fuel Bar Fill
                GameObject fuelBarFillObj = new GameObject("FuelBarFill");
                fuelBarFillObj.transform.SetParent(fuelBarBgObj.transform, false);
                RectTransform fbFillRect = fuelBarFillObj.AddComponent<RectTransform>();
                fbFillRect.anchorMin = Vector2.zero;
                fbFillRect.anchorMax = Vector2.one;
                fbFillRect.sizeDelta = Vector2.zero;

                fuelBarFill = fuelBarFillObj.AddComponent<Image>();
                fuelBarFill.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
                fuelBarFill.color = new Color(0.2f, 0.9f, 0.45f, 1f);
                fuelBarFill.type = Image.Type.Filled;
                fuelBarFill.fillMethod = Image.FillMethod.Horizontal;
                fuelBarFill.fillOrigin = 0;
                fuelBarFill.fillAmount = 1f;
                fuelBarFill.raycastTarget = false;

                // --- 2. SEPARATE CONDITION CARD ---
                conditionGaugePanel = new GameObject("ConditionGaugeCard");
                conditionGaugePanel.transform.SetParent(vehicleDashboardRoot.transform, false);

                LayoutElement condLayout = conditionGaugePanel.AddComponent<LayoutElement>();
                condLayout.preferredHeight = 72;
                condLayout.minHeight = 72;
                condLayout.flexibleHeight = 0;

                Image condBg = conditionGaugePanel.AddComponent<Image>();
                condBg.color = new Color(0.06f, 0.08f, 0.12f, 0.94f);
                condBg.raycastTarget = false;

                // Condition Title (Left)
                GameObject condTitleObj = new GameObject("CondTitle");
                condTitleObj.transform.SetParent(conditionGaugePanel.transform, false);
                RectTransform ctTitleRect = condTitleObj.AddComponent<RectTransform>();
                ctTitleRect.anchorMin = new Vector2(0f, 0.5f);
                ctTitleRect.anchorMax = new Vector2(0.45f, 1f);
                ctTitleRect.anchoredPosition = new Vector2(14, -2);
                ctTitleRect.sizeDelta = new Vector2(0, 0);

                TextMeshProUGUI ctTitleTmp = condTitleObj.AddComponent<TextMeshProUGUI>();
                ctTitleTmp.text = "🔧 KONDİSYON";
                ctTitleTmp.fontSize = 15;
                ctTitleTmp.fontStyle = FontStyles.Bold;
                ctTitleTmp.alignment = TextAlignmentOptions.MidlineLeft;
                ctTitleTmp.color = new Color(0.0f, 0.9f, 0.64f);
                ctTitleTmp.raycastTarget = false;

                // Condition Value (Right)
                GameObject condValObj = new GameObject("CondValue");
                condValObj.transform.SetParent(conditionGaugePanel.transform, false);
                RectTransform ctValRect = condValObj.AddComponent<RectTransform>();
                ctValRect.anchorMin = new Vector2(0.45f, 0.5f);
                ctValRect.anchorMax = new Vector2(1f, 1f);
                ctValRect.anchoredPosition = new Vector2(-14, -2);
                ctValRect.sizeDelta = new Vector2(0, 0);

                conditionValueText = condValObj.AddComponent<TextMeshProUGUI>();
                conditionValueText.text = "%100 [İYİ]";
                conditionValueText.fontSize = 15;
                conditionValueText.fontStyle = FontStyles.Bold;
                conditionValueText.alignment = TextAlignmentOptions.MidlineRight;
                conditionValueText.color = new Color(0.3f, 0.95f, 0.45f);
                conditionValueText.raycastTarget = false;

                // Condition Bar Background
                GameObject condBarBgObj = new GameObject("CondBarBg");
                condBarBgObj.transform.SetParent(conditionGaugePanel.transform, false);
                RectTransform cbbgRect = condBarBgObj.AddComponent<RectTransform>();
                cbbgRect.anchorMin = new Vector2(0f, 0f);
                cbbgRect.anchorMax = new Vector2(1f, 0f);
                cbbgRect.pivot = new Vector2(0.5f, 0f);
                cbbgRect.anchoredPosition = new Vector2(0, 12);
                cbbgRect.sizeDelta = new Vector2(-28, 10);

                Image cBarBgImg = condBarBgObj.AddComponent<Image>();
                cBarBgImg.color = new Color(0.12f, 0.16f, 0.22f, 0.95f);
                cBarBgImg.raycastTarget = false;

                // Condition Bar Fill
                GameObject condBarFillObj = new GameObject("CondBarFill");
                condBarFillObj.transform.SetParent(condBarBgObj.transform, false);
                RectTransform cbFillRect = condBarFillObj.AddComponent<RectTransform>();
                cbFillRect.anchorMin = Vector2.zero;
                cbFillRect.anchorMax = Vector2.one;
                cbFillRect.sizeDelta = Vector2.zero;

                conditionBarFill = condBarFillObj.AddComponent<Image>();
                conditionBarFill.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
                conditionBarFill.color = new Color(0.0f, 0.9f, 0.64f, 1f);
                conditionBarFill.type = Image.Type.Filled;
                conditionBarFill.fillMethod = Image.FillMethod.Horizontal;
                conditionBarFill.fillOrigin = 0;
                conditionBarFill.fillAmount = 1f;
                conditionBarFill.raycastTarget = false;

                vehicleDashboardRoot.SetActive(false);
            }
        }
    }

    public void ShowPrompt(string message, float duration = 2.8f)
    {
        if (string.IsNullOrEmpty(message))
        {
            HidePrompt();
            return;
        }

        if (promptPanel == null || promptText == null)
        {
            EnsureUI();
        }

        if (promptPanel != null && promptText != null)
        {
            promptText.text = message;
            promptPanel.SetActive(true);
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
        if (promptCanvasGroup != null)
        {
            promptCanvasGroup.alpha = 1f;
        }
        if (promptPanel != null)
        {
            promptPanel.SetActive(false);
        }
    }

    public void UpdateVehicleHUD(float currentFuel, float maxFuel, bool isLow, float currentCondition, float maxCondition)
    {
        if (vehicleDashboardRoot == null || fuelGaugePanel == null || conditionGaugePanel == null) EnsureUI();

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

        float fuelPct = maxFuel > 0 ? Mathf.Clamp01(currentFuel / maxFuel) : 0f;
        float condPct = maxCondition > 0 ? Mathf.Clamp01(currentCondition / maxCondition) : 0f;

        // 1. Fuel Bar & Text
        if (fuelBarFill != null)
        {
            if (fuelBarFill.sprite == null)
            {
                fuelBarFill.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
            }
            fuelBarFill.fillAmount = fuelPct;

            if (fuelPct > 0.40f) fuelBarFill.color = new Color(0.2f, 0.9f, 0.45f);
            else if (fuelPct > 0.15f) fuelBarFill.color = new Color(1f, 0.75f, 0.2f);
            else fuelBarFill.color = new Color(1f, 0.25f, 0.25f);
        }

        if (fuelValueText != null)
        {
            string colorTag = isLow ? "<color=#FF4444>" : "<color=#FFFFFF>";
            fuelValueText.text = $"{colorTag}{currentFuel:F1} / {maxFuel:F1} L ({(fuelPct * 100):F0}%)</color>";
        }

        // 2. Condition Bar & Text
        if (conditionBarFill != null)
        {
            if (conditionBarFill.sprite == null)
            {
                conditionBarFill.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
            }
            conditionBarFill.fillAmount = condPct;

            if (condPct > 0.60f) conditionBarFill.color = new Color(0.0f, 0.9f, 0.64f);
            else if (condPct > 0.25f) conditionBarFill.color = new Color(1f, 0.65f, 0.15f);
            else conditionBarFill.color = new Color(1f, 0.25f, 0.25f);
        }

        if (conditionValueText != null)
        {
            string condTag = condPct <= 0.25f ? "<color=#FF4444>[AĞIR HASARLI]</color>" : (condPct <= 0.60f ? "<color=#FFAA33>[ORTA]</color>" : "<color=#32FF64>[İYİ]</color>");
            conditionValueText.text = $"%{(condPct * 100):F0}  {condTag}";
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

    private void BindHeldCargoReferences(Transform sideCard)
    {
        if (sideCard == null) return;
        heldCargoPanel = sideCard.gameObject;
        heldCargoHeaderText = FindTMPRecursive(sideCard, "Header", "Title", "PackageHeader", "HeldHeader");
        heldCargoTypeText = FindTMPRecursive(sideCard, "Type", "CargoType", "Badge", "ParcelType");
        heldCargoStatusText = FindTMPRecursive(sideCard, "Status", "Durum", "Condition", "TimeStatus");
        heldCargoPercentageText = FindTMPRecursive(sideCard, "Percentage", "Percent", "Yuzde", "HealthPercentage", "Ratio");
        heldCargoRecipientText = FindTMPRecursive(sideCard, "Recipient", "Alici", "Customer", "Name");
        heldCargoAddressText = FindTMPRecursive(sideCard, "Address", "Adres", "TargetAddress", "Destination");
        heldCargoAddressDescText = FindTMPRecursive(sideCard, "AddressDescription", "Description", "Clue", "VisualClue", "Ipucu", "Desc");
        heldCargoRewardText = FindTMPRecursive(sideCard, "Reward", "Odul", "Earnings", "Price", "Money", "Income");
        heldCargoPenaltyText = FindTMPRecursive(sideCard, "Penalty", "Ceza", "Fine");
        heldCargoActionHintText = FindTMPRecursive(sideCard, "ActionHint", "Hint", "Prompt", "Controls", "KeyHint");
    }

    public void ShowHeldCargoInfo(PhysicalCargoPackage pkg)
    {
        currentHeldPackage = pkg;
        if (pkg == null)
        {
            HideHeldCargoInfo();
            return;
        }

        if (heldCargoPanel == null)
        {
            EnsureUI();
        }

        if (heldCargoPanel != null)
        {
            heldCargoPanel.SetActive(true);
            BindHeldCargoReferences(heldCargoPanel.transform);
        }

        string recipient = !string.IsNullOrEmpty(pkg.EffectiveRecipientName) ? pkg.EffectiveRecipientName : pkg.recipientName;
        string address = !string.IsNullOrEmpty(pkg.EffectiveAddressName) ? pkg.EffectiveAddressName : pkg.targetAddressName;
        string desc = !string.IsNullOrEmpty(pkg.EffectiveAddressDescription) ? pkg.EffectiveAddressDescription : pkg.targetAddressDescription;

        if (heldCargoRecipientText != null)
        {
            heldCargoRecipientText.text = $"<b>Recipient:</b> {recipient}";
        }

        if (heldCargoAddressText != null)
        {
            heldCargoAddressText.text = $"<b>Address:</b> {address}";
        }
        
        if (heldCargoAddressDescText != null)
        {
            heldCargoAddressDescText.text = !string.IsNullOrEmpty(desc) ? $"\"{desc}\"" : "\"(No address visual clue available)\"";
        }

        if (heldCargoRewardText != null)
        {
            heldCargoRewardText.text = $"<b>Reward:</b> +${pkg.deliveryReward}";
        }

        if (heldCargoPenaltyText != null)
        {
            heldCargoPenaltyText.text = $"<b>Penalty:</b> -${pkg.wrongPenalty}";
        }

        // Cargo Type Row Handling (Type, Status, Percentage)
        if (heldCargoTypeText != null)
        {
            switch (pkg.cargoType)
            {
                case CargoType.Standard:
                    // Standard: Sadece STANDARD yazar, Status ve Percentage boş
                    heldCargoTypeText.text = "STANDARD";
                    if (heldCargoStatusText != null) heldCargoStatusText.text = "";
                    if (heldCargoPercentageText != null) heldCargoPercentageText.text = "";
                    break;

                case CargoType.Express:
                    // Express: EXPRESS ve Status'te teslimat saati (Örn: 13:00), Percentage boş
                    heldCargoTypeText.text = "EXPRESS";
                    if (heldCargoStatusText != null) heldCargoStatusText.text = pkg.GetFormattedTargetDeliveryTime();
                    if (heldCargoPercentageText != null) heldCargoPercentageText.text = "";
                    break;

                case CargoType.Fragile:
                    // Fragile: FRAGILE, Status'te "Broken" / "Condition", Percentage'da "0%" / "%100"
                    heldCargoTypeText.text = "FRAGILE";
                    if (heldCargoStatusText != null) heldCargoStatusText.text = pkg.isBroken ? "Broken" : "Condition";
                    if (heldCargoPercentageText != null) heldCargoPercentageText.text = pkg.isBroken ? "0%" : $"{pkg.health:F0}%";
                    break;

                case CargoType.Explosive:
                    // Explosive: EXPLOSIVE, Status'te "Detonated" / "Stability", Percentage'da "0%" / "50%"
                    heldCargoTypeText.text = "EXPLOSIVE";
                    if (heldCargoStatusText != null) heldCargoStatusText.text = pkg.isBroken ? "Detonated" : "Stability";
                    if (heldCargoPercentageText != null) heldCargoPercentageText.text = pkg.isBroken ? "0%" : $"{pkg.health:F0}%";
                    break;

                default:
                    heldCargoTypeText.text = "STANDARD";
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
