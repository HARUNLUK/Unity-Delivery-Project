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

    public void EnsureUI()
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
        if (crosshairDot == null)
        {
            Transform existingDot = canvas.transform.Find("CenterCrosshair");
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
        if (promptPanel == null || promptText == null)
        {
            Transform existingPrompt = canvas.transform.Find("InteractionPromptBox");
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

        // 3. Held Cargo Side Details Panel (Always verify AddressDescription exists)
        Transform existingSide = canvas.transform.Find("HeldCargoSideCard");
        if (existingSide != null && (heldCargoAddressDescText == null || existingSide.Find("ClueBox") == null))
        {
            DestroyImmediate(existingSide.gameObject);
            heldCargoPanel = null;
            heldCargoAddressDescText = null;
        }

        if (heldCargoPanel == null || heldCargoAddressDescText == null)
        {
            if (existingSide != null && heldCargoPanel == null)
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
            GameObject headObj = new GameObject("Header");
            headObj.transform.SetParent(heldCargoPanel.transform, false);
            heldCargoHeaderText = headObj.AddComponent<TextMeshProUGUI>();
            heldCargoHeaderText.text = "HELD PACKAGE";
            heldCargoHeaderText.fontSize = 24;
            heldCargoHeaderText.fontStyle = FontStyles.Bold;
            heldCargoHeaderText.color = new Color(1f, 0.82f, 0.2f);
            heldCargoHeaderText.alignment = TextAlignmentOptions.Left;

            // Tracking & Type Row
            GameObject trackObj = new GameObject("Tracking");
            trackObj.transform.SetParent(heldCargoPanel.transform, false);
            heldCargoTrackingText = trackObj.AddComponent<TextMeshProUGUI>();
            heldCargoTrackingText.fontSize = 20;
            heldCargoTrackingText.fontStyle = FontStyles.Bold;
            heldCargoTrackingText.color = Color.white;

            // Type
            GameObject typeObj = new GameObject("Type");
            typeObj.transform.SetParent(heldCargoPanel.transform, false);
            heldCargoTypeText = typeObj.AddComponent<TextMeshProUGUI>();
            heldCargoTypeText.fontSize = 19;
            heldCargoTypeText.fontStyle = FontStyles.Bold;
            heldCargoTypeText.color = new Color(1f, 0.6f, 0.2f);

            // Recipient Name
            GameObject recObj = new GameObject("Recipient");
            recObj.transform.SetParent(heldCargoPanel.transform, false);
            heldCargoRecipientText = recObj.AddComponent<TextMeshProUGUI>();
            heldCargoRecipientText.fontSize = 20;
            heldCargoRecipientText.fontStyle = FontStyles.Bold;
            heldCargoRecipientText.color = new Color(0.95f, 0.85f, 0.45f);

            // Destination Address
            GameObject addrObj = new GameObject("Address");
            addrObj.transform.SetParent(heldCargoPanel.transform, false);
            heldCargoAddressText = addrObj.AddComponent<TextMeshProUGUI>();
            heldCargoAddressText.fontSize = 21;
            heldCargoAddressText.fontStyle = FontStyles.Bold;
            heldCargoAddressText.color = new Color(0.35f, 0.88f, 1f);

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

            GameObject clueTitleObj = new GameObject("ClueTitle");
            clueTitleObj.transform.SetParent(clueBoxObj.transform, false);
            TextMeshProUGUI clueTitle = clueTitleObj.AddComponent<TextMeshProUGUI>();
            clueTitle.text = "<b>DESTINATION & VISUAL CLUE:</b>";
            clueTitle.fontSize = 19;
            clueTitle.fontStyle = FontStyles.Bold;
            clueTitle.color = new Color(1f, 0.85f, 0.25f);

            GameObject descObj = new GameObject("AddressDescription");
            descObj.transform.SetParent(clueBoxObj.transform, false);
            heldCargoAddressDescText = descObj.AddComponent<TextMeshProUGUI>();
            heldCargoAddressDescText.fontSize = 24; // Large & prominent!
            heldCargoAddressDescText.fontStyle = FontStyles.Bold;
            heldCargoAddressDescText.color = new Color(1f, 0.96f, 0.78f); // High-contrast warm cream
            heldCargoAddressDescText.enableWordWrapping = true;
            heldCargoAddressDescText.lineSpacing = 1.25f;

            // Reward Line
            GameObject rewardObj = new GameObject("Reward");
            rewardObj.transform.SetParent(heldCargoPanel.transform, false);
            heldCargoRewardText = rewardObj.AddComponent<TextMeshProUGUI>();
            heldCargoRewardText.fontSize = 20;
            heldCargoRewardText.fontStyle = FontStyles.Bold;
            heldCargoRewardText.color = new Color(0.3f, 1f, 0.4f);

            // Penalty Line
            GameObject penObj = new GameObject("Penalty");
            penObj.transform.SetParent(heldCargoPanel.transform, false);
            heldCargoPenaltyText = penObj.AddComponent<TextMeshProUGUI>();
            heldCargoPenaltyText.fontSize = 19;
            heldCargoPenaltyText.fontStyle = FontStyles.Bold;
            heldCargoPenaltyText.color = new Color(1f, 0.4f, 0.4f);

            // Action Hint
            GameObject hintObj = new GameObject("ActionHint");
            hintObj.transform.SetParent(heldCargoPanel.transform, false);
            heldCargoActionHintText = hintObj.AddComponent<TextMeshProUGUI>();
            heldCargoActionHintText.text = "<b>[E]</b> Drop  |  <b>Hold:</b> Throw";
            heldCargoActionHintText.fontSize = 18;
            heldCargoActionHintText.color = new Color(0.85f, 0.85f, 0.85f);
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

    public void ShowHeldCargoInfo(PhysicalCargoPackage pkg)
    {
        if (pkg == null)
        {
            HideHeldCargoInfo();
            return;
        }

        if (heldCargoPanel == null || heldCargoAddressDescText == null) EnsureUI();
        if (heldCargoPanel != null) heldCargoPanel.SetActive(true);

        if (heldCargoTrackingText != null) heldCargoTrackingText.text = $"<b>Tracking #:</b> #{pkg.targetPointId}";
        if (heldCargoRecipientText != null) heldCargoRecipientText.text = $"<b>Recipient:</b> {pkg.EffectiveRecipientName}";
        if (heldCargoAddressText != null) heldCargoAddressText.text = $"<b>Address:</b> {pkg.EffectiveAddressName}";
        
        if (heldCargoAddressDescText != null)
        {
            string desc = pkg.EffectiveAddressDescription;
            if (!string.IsNullOrEmpty(desc))
            {
                heldCargoAddressDescText.text = $"\"{desc}\"";
            }
            else
            {
                heldCargoAddressDescText.text = "\"(No address visual clue available)\"";
            }
        }

        if (heldCargoRewardText != null) heldCargoRewardText.text = $"<b>Reward:</b> +${pkg.deliveryReward}  <color=#32FFFF>(+{pkg.xpReward} XP)</color>";
        if (heldCargoPenaltyText != null) heldCargoPenaltyText.text = $"<b>Penalty:</b> -${pkg.wrongPenalty}";

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
        }
    }

    public void HideHeldCargoInfo()
    {
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
        hud.EnsureUI();
        EditorUtility.SetDirty(hud.gameObject);
    }
#endif
}
