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
    public TextMeshProUGUI heldCargoTrackingText;
    public TextMeshProUGUI heldCargoTypeText;
    public TextMeshProUGUI heldCargoRecipientText;
    public TextMeshProUGUI heldCargoAddressText;
    public TextMeshProUGUI heldCargoAddressDescText;
    public TextMeshProUGUI heldCargoRewardText;
    public TextMeshProUGUI heldCargoPenaltyText;
    public TextMeshProUGUI heldCargoActionHintText;

    [Header("--- VEHICLE FUEL GAUGE ---")]
    public GameObject fuelGaugePanel;
    public Image fuelBarFill;
    public TextMeshProUGUI fuelValueText;

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

        // 3. Held Cargo Side Details Panel
        Transform existingSide = canvas.transform.Find("HeldCargoSideCard");
        if (existingSide != null && forceRecreate)
        {
            DestroyImmediate(existingSide.gameObject);
            existingSide = null;
            heldCargoPanel = null;
        }

        if (existingSide != null)
        {
            heldCargoPanel = existingSide.gameObject;
            heldCargoHeaderText = existingSide.Find("Header")?.GetComponent<TextMeshProUGUI>();
            heldCargoTrackingText = existingSide.Find("Tracking")?.GetComponent<TextMeshProUGUI>();
            heldCargoTypeText = existingSide.Find("Type")?.GetComponent<TextMeshProUGUI>();
            heldCargoRecipientText = existingSide.Find("Recipient")?.GetComponent<TextMeshProUGUI>();
            heldCargoAddressText = existingSide.Find("Address")?.GetComponent<TextMeshProUGUI>();
            heldCargoAddressDescText = existingSide.Find("ClueBox/AddressDescription")?.GetComponent<TextMeshProUGUI>();
            heldCargoRewardText = existingSide.Find("Reward")?.GetComponent<TextMeshProUGUI>();
            heldCargoPenaltyText = existingSide.Find("Penalty")?.GetComponent<TextMeshProUGUI>();
            heldCargoActionHintText = existingSide.Find("ActionHint")?.GetComponent<TextMeshProUGUI>();
        }

        if (heldCargoPanel == null || heldCargoTrackingText == null || heldCargoRecipientText == null || heldCargoAddressText == null || heldCargoAddressDescText == null)
        {
            if (existingSide != null)
            {
                DestroyImmediate(existingSide.gameObject);
            }

            heldCargoPanel = new GameObject("HeldCargoSideCard");
            heldCargoPanel.transform.SetParent(canvas.transform, false);

            RectTransform sideRect = heldCargoPanel.AddComponent<RectTransform>();
            sideRect.anchorMin = new Vector2(1f, 0.5f);
            sideRect.anchorMax = new Vector2(1f, 0.5f);
            sideRect.pivot = new Vector2(1f, 0.5f);
            sideRect.anchoredPosition = new Vector2(-40, 0);
            sideRect.sizeDelta = new Vector2(560, 530);

            Image sideBg = heldCargoPanel.AddComponent<Image>();
            sideBg.color = new Color(0.06f, 0.08f, 0.13f, 0.96f);
            sideBg.raycastTarget = false;

            VerticalLayoutGroup vLayout = heldCargoPanel.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(24, 24, 20, 20);
            vLayout.spacing = 9;
            vLayout.childControlHeight = true;
            vLayout.childControlWidth = true;
            vLayout.childForceExpandHeight = false;

            // Header
            heldCargoHeaderText = CreateTMPChild(heldCargoPanel, "Header", "HELD PACKAGE", 24, FontStyles.Bold, new Color(1f, 0.82f, 0.2f), TextAlignmentOptions.Left);

            // Tracking
            heldCargoTrackingText = CreateTMPChild(heldCargoPanel, "Tracking", "Tracking #: #1", 20, FontStyles.Bold, Color.white, TextAlignmentOptions.Left);

            // Type
            heldCargoTypeText = CreateTMPChild(heldCargoPanel, "Type", "[STANDARD PARCEL]", 19, FontStyles.Bold, new Color(1f, 0.6f, 0.2f), TextAlignmentOptions.Left);

            // Recipient Name
            heldCargoRecipientText = CreateTMPChild(heldCargoPanel, "Recipient", "Recipient: Resident", 20, FontStyles.Bold, new Color(0.95f, 0.85f, 0.45f), TextAlignmentOptions.Left);

            // Destination Address
            heldCargoAddressText = CreateTMPChild(heldCargoPanel, "Address", "Address: Main Street", 21, FontStyles.Bold, new Color(0.35f, 0.88f, 1f), TextAlignmentOptions.Left);

            // --- PROMINENT LARGE ADDRESS CLUE & DESCRIPTION BOX ---
            GameObject clueBoxObj = new GameObject("ClueBox");
            clueBoxObj.transform.SetParent(heldCargoPanel.transform, false);
            RectTransform clueBoxRect = clueBoxObj.AddComponent<RectTransform>();
            clueBoxRect.sizeDelta = new Vector2(0, 160);

            Image clueBoxBg = clueBoxObj.AddComponent<Image>();
            clueBoxBg.color = new Color(0.04f, 0.06f, 0.09f, 0.98f);
            clueBoxBg.raycastTarget = false;

            VerticalLayoutGroup cbLayout = clueBoxObj.AddComponent<VerticalLayoutGroup>();
            cbLayout.padding = new RectOffset(16, 16, 14, 14);
            cbLayout.spacing = 6;
            cbLayout.childControlWidth = true;
            cbLayout.childControlHeight = true;
            cbLayout.childForceExpandHeight = false;

            CreateTMPChild(clueBoxObj, "ClueTitle", "<b>DESTINATION & VISUAL CLUE:</b>", 19, FontStyles.Bold, new Color(1f, 0.85f, 0.25f), TextAlignmentOptions.Left);

            heldCargoAddressDescText = CreateTMPChild(clueBoxObj, "AddressDescription", "\"Look for the house at the corner...\"", 24, FontStyles.Bold, new Color(1f, 0.96f, 0.78f), TextAlignmentOptions.Left);
            if (heldCargoAddressDescText != null)
            {
                heldCargoAddressDescText.enableWordWrapping = true;
                heldCargoAddressDescText.lineSpacing = 1.25f;
            }

            // Reward Line
            heldCargoRewardText = CreateTMPChild(heldCargoPanel, "Reward", "Reward: +$100 (+80 XP)", 20, FontStyles.Bold, new Color(0.3f, 1f, 0.4f), TextAlignmentOptions.Left);

            // Penalty Line
            heldCargoPenaltyText = CreateTMPChild(heldCargoPanel, "Penalty", "Penalty: -$30", 19, FontStyles.Bold, new Color(1f, 0.4f, 0.4f), TextAlignmentOptions.Left);

            // Action Hint
            heldCargoActionHintText = CreateTMPChild(heldCargoPanel, "ActionHint", "<b>[E]</b> Drop  |  <b>Hold:</b> Throw", 18, FontStyles.Normal, new Color(0.85f, 0.85f, 0.85f), TextAlignmentOptions.Left);
        }

        // 4. In-Vehicle Fuel Gauge Panel (Bottom Right)
        if (fuelGaugePanel == null)
        {
            Transform existingFuel = canvas.transform.Find("VehicleFuelGaugePanel");
            if (existingFuel != null)
            {
                fuelGaugePanel = existingFuel.gameObject;
                fuelBarFill = fuelGaugePanel.transform.Find("FuelBarBg/FuelBarFill")?.GetComponent<Image>();
                fuelValueText = fuelGaugePanel.GetComponentInChildren<TextMeshProUGUI>();
            }
            else
            {
                fuelGaugePanel = new GameObject("VehicleFuelGaugePanel");
                fuelGaugePanel.transform.SetParent(canvas.transform, false);

                RectTransform fRect = fuelGaugePanel.AddComponent<RectTransform>();
                fRect.anchorMin = new Vector2(1f, 0f);
                fRect.anchorMax = new Vector2(1f, 0f);
                fRect.pivot = new Vector2(1f, 0f);
                fRect.anchoredPosition = new Vector2(-40, 40);
                fRect.sizeDelta = new Vector2(280, 56);

                Image fBg = fuelGaugePanel.AddComponent<Image>();
                fBg.color = new Color(0.06f, 0.08f, 0.12f, 0.92f);
                fBg.raycastTarget = false;

                // Fuel Bar Background
                GameObject barBgObj = new GameObject("FuelBarBg");
                barBgObj.transform.SetParent(fuelGaugePanel.transform, false);
                RectTransform bbgRect = barBgObj.AddComponent<RectTransform>();
                bbgRect.anchorMin = new Vector2(0f, 0f);
                bbgRect.anchorMax = new Vector2(1f, 0f);
                bbgRect.pivot = new Vector2(0.5f, 0f);
                bbgRect.anchoredPosition = new Vector2(0, 6);
                bbgRect.sizeDelta = new Vector2(-24, 10);

                Image barBgImg = barBgObj.AddComponent<Image>();
                barBgImg.color = new Color(0.15f, 0.18f, 0.22f, 0.9f);
                barBgImg.raycastTarget = false;

                // Fuel Bar Fill
                GameObject barFillObj = new GameObject("FuelBarFill");
                barFillObj.transform.SetParent(barBgObj.transform, false);
                RectTransform bFillRect = barFillObj.AddComponent<RectTransform>();
                bFillRect.anchorMin = Vector2.zero;
                bFillRect.anchorMax = Vector2.one;
                bFillRect.sizeDelta = Vector2.zero;

                fuelBarFill = barFillObj.AddComponent<Image>();
                fuelBarFill.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
                fuelBarFill.color = new Color(0.2f, 0.85f, 0.4f, 1f);
                fuelBarFill.type = Image.Type.Filled;
                fuelBarFill.fillMethod = Image.FillMethod.Horizontal;
                fuelBarFill.fillOrigin = 0;
                fuelBarFill.fillAmount = 1f;
                fuelBarFill.raycastTarget = false;

                // Text
                GameObject textObj = new GameObject("FuelText");
                textObj.transform.SetParent(fuelGaugePanel.transform, false);
                RectTransform tRect = textObj.AddComponent<RectTransform>();
                tRect.anchorMin = new Vector2(0, 0);
                tRect.anchorMax = new Vector2(1, 1);
                tRect.anchoredPosition = new Vector2(0, 5);
                tRect.sizeDelta = new Vector2(-24, 0);

                fuelValueText = textObj.AddComponent<TextMeshProUGUI>();
                fuelValueText.text = "FUEL: 50.0 / 50.0 L (100%)";
                fuelValueText.fontSize = 19;
                fuelValueText.fontStyle = FontStyles.Bold;
                fuelValueText.alignment = TextAlignmentOptions.MidlineLeft;
                fuelValueText.color = Color.white;
                fuelValueText.raycastTarget = false;

                fuelGaugePanel.SetActive(false);
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

    public void UpdateFuelHUD(float currentFuel, float maxFuel, bool isLow)
    {
        if (fuelGaugePanel == null) EnsureUI();
        if (fuelGaugePanel != null && !fuelGaugePanel.activeSelf)
        {
            fuelGaugePanel.SetActive(true);
        }

        float pct = maxFuel > 0 ? Mathf.Clamp01(currentFuel / maxFuel) : 0f;

        if (fuelBarFill != null)
        {
            if (fuelBarFill.sprite == null)
            {
                fuelBarFill.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
            }

            fuelBarFill.fillAmount = pct;

            // Direct RectTransform scaling guarantee
            RectTransform fillRt = fuelBarFill.rectTransform;
            if (fillRt != null)
            {
                fillRt.anchorMin = Vector2.zero;
                fillRt.anchorMax = new Vector2(pct, 1f);
                fillRt.sizeDelta = Vector2.zero;
            }

            if (pct > 0.4f) fuelBarFill.color = new Color(0.2f, 0.85f, 0.4f);
            else if (pct > 0.15f) fuelBarFill.color = new Color(1f, 0.75f, 0.2f);
            else fuelBarFill.color = new Color(1f, 0.25f, 0.25f);
        }

        if (fuelValueText != null)
        {
            string colorTag = isLow ? "<color=#FF4444>" : "<color=#FFFFFF>";
            fuelValueText.text = $"FUEL: {colorTag}{currentFuel:F1} / {maxFuel:F1} L ({(pct * 100):F0}%)</color>";
        }
    }

    public void HideFuelHUD()
    {
        if (fuelGaugePanel != null)
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

    public void ShowHeldCargoInfo(PhysicalCargoPackage pkg)
    {
        currentHeldPackage = pkg;
        if (pkg == null)
        {
            HideHeldCargoInfo();
            return;
        }

        if (heldCargoPanel == null || heldCargoTrackingText == null || heldCargoAddressText == null || heldCargoAddressDescText == null)
        {
            EnsureUI();
        }

        if (heldCargoPanel != null)
        {
            heldCargoPanel.SetActive(true);

            // Re-bind fallbacks if any field is still unassigned
            if (heldCargoTrackingText == null) heldCargoTrackingText = heldCargoPanel.transform.Find("Tracking")?.GetComponent<TextMeshProUGUI>();
            if (heldCargoRecipientText == null) heldCargoRecipientText = heldCargoPanel.transform.Find("Recipient")?.GetComponent<TextMeshProUGUI>();
            if (heldCargoAddressText == null) heldCargoAddressText = heldCargoPanel.transform.Find("Address")?.GetComponent<TextMeshProUGUI>();
            if (heldCargoAddressDescText == null) heldCargoAddressDescText = heldCargoPanel.transform.Find("ClueBox/AddressDescription")?.GetComponent<TextMeshProUGUI>();
            if (heldCargoRewardText == null) heldCargoRewardText = heldCargoPanel.transform.Find("Reward")?.GetComponent<TextMeshProUGUI>();
            if (heldCargoPenaltyText == null) heldCargoPenaltyText = heldCargoPanel.transform.Find("Penalty")?.GetComponent<TextMeshProUGUI>();
            if (heldCargoTypeText == null) heldCargoTypeText = heldCargoPanel.transform.Find("Type")?.GetComponent<TextMeshProUGUI>();
            if (heldCargoActionHintText == null) heldCargoActionHintText = heldCargoPanel.transform.Find("ActionHint")?.GetComponent<TextMeshProUGUI>();
        }

        string recipient = !string.IsNullOrEmpty(pkg.EffectiveRecipientName) ? pkg.EffectiveRecipientName : pkg.recipientName;
        string address = !string.IsNullOrEmpty(pkg.EffectiveAddressName) ? pkg.EffectiveAddressName : pkg.targetAddressName;
        string desc = !string.IsNullOrEmpty(pkg.EffectiveAddressDescription) ? pkg.EffectiveAddressDescription : pkg.targetAddressDescription;

        if (heldCargoTrackingText != null)
        {
            heldCargoTrackingText.text = $"<b>Tracking #:</b> #{pkg.targetPointId}";
            heldCargoTrackingText.SetVerticesDirty();
            heldCargoTrackingText.SetLayoutDirty();
        }

        if (heldCargoRecipientText != null)
        {
            heldCargoRecipientText.text = $"<b>Recipient:</b> {recipient}";
            heldCargoRecipientText.SetVerticesDirty();
            heldCargoRecipientText.SetLayoutDirty();
        }

        if (heldCargoAddressText != null)
        {
            heldCargoAddressText.text = $"<b>Address:</b> {address}";
            heldCargoAddressText.SetVerticesDirty();
            heldCargoAddressText.SetLayoutDirty();
        }
        
        if (heldCargoAddressDescText != null)
        {
            heldCargoAddressDescText.text = !string.IsNullOrEmpty(desc) ? $"\"{desc}\"" : "\"(No address visual clue available)\"";
            heldCargoAddressDescText.SetVerticesDirty();
            heldCargoAddressDescText.SetLayoutDirty();
        }

        if (heldCargoRewardText != null)
        {
            heldCargoRewardText.text = $"<b>Reward:</b> +${pkg.deliveryReward}  <color=#32FFFF>(+{pkg.xpReward} XP)</color>";
            heldCargoRewardText.SetVerticesDirty();
            heldCargoRewardText.SetLayoutDirty();
        }

        if (heldCargoPenaltyText != null)
        {
            heldCargoPenaltyText.text = $"<b>Penalty:</b> -${pkg.wrongPenalty}";
            heldCargoPenaltyText.SetVerticesDirty();
            heldCargoPenaltyText.SetLayoutDirty();
        }

        if (heldCargoTypeText != null)
        {
            switch (pkg.cargoType)
            {
                case CargoType.Fragile:
                    heldCargoTypeText.text = pkg.isBroken ?
                        "<color=#FF4444>[FRAGILE] (BROKEN! - Reward Cancelled)</color>" :
                        $"<color=#FFAA33>[FRAGILE] (Condition: {pkg.health:F0}%)</color>";
                    break;
                case CargoType.Express:
                    heldCargoTypeText.text = $"<color=#32FFFF>[EXPRESS] (Before {pkg.GetFormattedTargetDeliveryTime()} +40% Bonus)</color>";
                    break;
                default:
                    heldCargoTypeText.text = "<color=#AAAAAA>[STANDARD PARCEL]</color>";
                    break;
            }
            heldCargoTypeText.SetVerticesDirty();
            heldCargoTypeText.SetLayoutDirty();
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
