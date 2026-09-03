using UnityEngine;
using TMPro;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class InteractionPromptHUD : MonoBehaviour
{
    public static InteractionPromptHUD Instance { get; private set; }

    [Header("--- UI REFERENCES ---")]
    public GameObject crosshairDot;
    public GameObject promptPanel;
    public TextMeshProUGUI promptText;

    [Header("--- HELD CARGO SIDE PANEL ---")]
    public GameObject heldCargoPanel;
    public TextMeshProUGUI heldCargoHeaderText;
    public TextMeshProUGUI heldCargoTrackingText;
    public TextMeshProUGUI heldCargoRecipientText;
    public TextMeshProUGUI heldCargoAddressText;
    public TextMeshProUGUI heldCargoRewardText;
    public TextMeshProUGUI heldCargoPenaltyText;
    public TextMeshProUGUI heldCargoActionHintText;

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
    }

    public void EnsureUI()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = Object.FindAnyObjectByType<Canvas>();

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
                boxRect.sizeDelta = new Vector2(520, 62);

                Image boxImg = promptPanel.AddComponent<Image>();
                boxImg.color = new Color(0.05f, 0.07f, 0.11f, 0.92f);
                boxImg.raycastTarget = false;

                GameObject textObj = new GameObject("PromptText");
                textObj.transform.SetParent(promptPanel.transform, false);
                RectTransform textRect = textObj.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;

                promptText = textObj.AddComponent<TextMeshProUGUI>();
                promptText.fontSize = 26;
                promptText.fontStyle = FontStyles.Bold;
                promptText.alignment = TextAlignmentOptions.Center;
                promptText.color = new Color(1f, 0.88f, 0.25f);
                promptText.raycastTarget = false;
            }
        }

        // 3. Held Cargo Side Details Panel (Büyük & İkonsuz Temiz Tasarım)
        if (heldCargoPanel == null)
        {
            Transform existingSide = canvas.transform.Find("HeldCargoSideCard");
            if (existingSide != null)
            {
                DestroyImmediate(existingSide.gameObject); // Rebuild with large clean layout
            }

            heldCargoPanel = new GameObject("HeldCargoSideCard");
            heldCargoPanel.transform.SetParent(canvas.transform, false);

            RectTransform sideRect = heldCargoPanel.AddComponent<RectTransform>();
            sideRect.anchorMin = new Vector2(1f, 0.5f);
            sideRect.anchorMax = new Vector2(1f, 0.5f);
            sideRect.pivot = new Vector2(1f, 0.5f);
            sideRect.anchoredPosition = new Vector2(-40, 0);
            sideRect.sizeDelta = new Vector2(470, 300);

            Image sideBg = heldCargoPanel.AddComponent<Image>();
            sideBg.color = new Color(0.06f, 0.08f, 0.13f, 0.95f);
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
            heldCargoHeaderText.text = "CARGO DETAILS";
            heldCargoHeaderText.fontSize = 24;
            heldCargoHeaderText.fontStyle = FontStyles.Bold;
            heldCargoHeaderText.color = new Color(1f, 0.82f, 0.2f);
            heldCargoHeaderText.alignment = TextAlignmentOptions.Left;

            // Tracking
            GameObject trackObj = new GameObject("Tracking");
            trackObj.transform.SetParent(heldCargoPanel.transform, false);
            heldCargoTrackingText = trackObj.AddComponent<TextMeshProUGUI>();
            heldCargoTrackingText.fontSize = 22;
            heldCargoTrackingText.fontStyle = FontStyles.Bold;
            heldCargoTrackingText.color = Color.white;

            // Recipient Name
            GameObject recObj = new GameObject("Recipient");
            recObj.transform.SetParent(heldCargoPanel.transform, false);
            heldCargoRecipientText = recObj.AddComponent<TextMeshProUGUI>();
            heldCargoRecipientText.fontSize = 21;
            heldCargoRecipientText.fontStyle = FontStyles.Bold;
            heldCargoRecipientText.color = new Color(0.95f, 0.85f, 0.45f);

            // Destination Address
            GameObject addrObj = new GameObject("Address");
            addrObj.transform.SetParent(heldCargoPanel.transform, false);
            heldCargoAddressText = addrObj.AddComponent<TextMeshProUGUI>();
            heldCargoAddressText.fontSize = 21;
            heldCargoAddressText.fontStyle = FontStyles.Bold;
            heldCargoAddressText.color = new Color(0.35f, 0.85f, 1f);

            // Reward Line
            GameObject rewardObj = new GameObject("Reward");
            rewardObj.transform.SetParent(heldCargoPanel.transform, false);
            heldCargoRewardText = rewardObj.AddComponent<TextMeshProUGUI>();
            heldCargoRewardText.fontSize = 22;
            heldCargoRewardText.fontStyle = FontStyles.Bold;
            heldCargoRewardText.color = new Color(0.35f, 1f, 0.45f);

            // Penalty Line
            GameObject penObj = new GameObject("Penalty");
            penObj.transform.SetParent(heldCargoPanel.transform, false);
            heldCargoPenaltyText = penObj.AddComponent<TextMeshProUGUI>();
            heldCargoPenaltyText.fontSize = 20;
            heldCargoPenaltyText.fontStyle = FontStyles.Bold;
            heldCargoPenaltyText.color = new Color(1f, 0.45f, 0.45f);

            // Action Hint
            GameObject hintObj = new GameObject("Hint");
            hintObj.transform.SetParent(heldCargoPanel.transform, false);
            heldCargoActionHintText = hintObj.AddComponent<TextMeshProUGUI>();
            heldCargoActionHintText.text = "[E] / [LMB] Drop  •  (Hold: Throw)";
            heldCargoActionHintText.fontSize = 18;
            heldCargoActionHintText.fontStyle = FontStyles.Bold;
            heldCargoActionHintText.color = new Color(0.85f, 0.85f, 0.85f);
            heldCargoActionHintText.alignment = TextAlignmentOptions.Center;
        }
    }

    public void ShowPrompt(string message)
    {
        if (promptPanel == null || promptText == null) EnsureUI();
        if (promptPanel != null) promptPanel.SetActive(true);
        if (promptText != null) promptText.text = message;
    }

    public void HidePrompt()
    {
        if (promptPanel != null) promptPanel.SetActive(false);
    }

    public void ShowHeldCargoInfo(PhysicalCargoPackage pkg)
    {
        if (pkg == null) return;
        if (heldCargoPanel == null) EnsureUI();

        if (heldCargoPanel != null)
        {
            heldCargoPanel.SetActive(true);

            string tracking = pkg.cargoData != null ? pkg.cargoData.trackingNumber : $"PKG-#{pkg.targetPointId}";
            if (heldCargoTrackingText != null) heldCargoTrackingText.text = $"Tracking: {tracking}";
            if (heldCargoRecipientText != null) heldCargoRecipientText.text = $"Recipient: {pkg.recipientName}";
            if (heldCargoAddressText != null) heldCargoAddressText.text = $"Destination: {pkg.targetAddressName} (Point #{pkg.targetPointId})";
            if (heldCargoRewardText != null) heldCargoRewardText.text = $"Reward: +${pkg.deliveryReward}";
            if (heldCargoPenaltyText != null) heldCargoPenaltyText.text = $"Penalty: -${pkg.wrongPenalty}";
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
    [MenuItem("Tools/Delivery Game/Create Interaction Prompt HUD", false, 25)]
    public static void CreatePromptHUDTool()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
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
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create HUD Canvas");
        }

        InteractionPromptHUD existing = canvas.GetComponentInChildren<InteractionPromptHUD>();
        if (existing == null)
        {
            GameObject hudObj = new GameObject("InteractionPromptHUD");
            hudObj.transform.SetParent(canvas.transform, false);
            existing = hudObj.AddComponent<InteractionPromptHUD>();
            Undo.RegisterCreatedObjectUndo(hudObj, "Create InteractionPromptHUD");
        }

        existing.EnsureUI();
        Selection.activeGameObject = existing.gameObject;
        Debug.Log("[InteractionPromptHUD] Interaction prompt UI successfully created on Canvas!");
    }
#endif
}
