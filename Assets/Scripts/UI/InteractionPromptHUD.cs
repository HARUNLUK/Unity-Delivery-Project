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

    private void Awake()
    {
        Instance = this;
        EnsureUI();
    }

    private void Start()
    {
        EnsureUI();
        HidePrompt();
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
                dotRect.sizeDelta = new Vector2(8, 8);

                Image dotImg = crosshairDot.AddComponent<Image>();
                dotImg.color = new Color(1f, 1f, 1f, 0.85f);
                dotImg.raycastTarget = false;
            }
        }

        // 2. Interaction Prompt Box
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
                boxRect.anchoredPosition = new Vector2(0, -35);
                boxRect.sizeDelta = new Vector2(420, 52);

                Image boxImg = promptPanel.AddComponent<Image>();
                boxImg.color = new Color(0.06f, 0.08f, 0.12f, 0.88f);
                boxImg.raycastTarget = false;

                GameObject textObj = new GameObject("PromptText");
                textObj.transform.SetParent(promptPanel.transform, false);
                RectTransform textRect = textObj.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;

                promptText = textObj.AddComponent<TextMeshProUGUI>();
                promptText.fontSize = 22;
                promptText.fontStyle = FontStyles.Bold;
                promptText.alignment = TextAlignmentOptions.Center;
                promptText.color = new Color(1f, 0.88f, 0.25f);
                promptText.raycastTarget = false;
            }
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
