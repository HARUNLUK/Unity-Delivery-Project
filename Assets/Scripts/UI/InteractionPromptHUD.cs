using UnityEngine;
using TMPro;
using UnityEngine.UI;

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

    private void EnsureUI()
    {
        if (promptPanel == null || promptText == null || crosshairDot == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = Object.FindAnyObjectByType<Canvas>();

            if (canvas != null)
            {
                // 1. Center Crosshair Dot
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
                    dotImg.color = new Color(1f, 1f, 1f, 0.75f);
                    dotImg.raycastTarget = false;
                }

                // 2. Interaction Prompt Box
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
                    boxRect.sizeDelta = new Vector2(380, 48);

                    Image boxImg = promptPanel.AddComponent<Image>();
                    boxImg.color = new Color(0.08f, 0.1f, 0.14f, 0.85f);
                    boxImg.raycastTarget = false;

                    GameObject textObj = new GameObject("PromptText");
                    textObj.transform.SetParent(promptPanel.transform, false);
                    RectTransform textRect = textObj.AddComponent<RectTransform>();
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.sizeDelta = Vector2.zero;

                    promptText = textObj.AddComponent<TextMeshProUGUI>();
                    promptText.fontSize = 20;
                    promptText.fontStyle = FontStyles.Bold;
                    promptText.alignment = TextAlignmentOptions.Center;
                    promptText.color = new Color(1f, 0.85f, 0.3f);
                    promptText.raycastTarget = false;
                }
            }
        }
    }

    public void ShowPrompt(string message)
    {
        if (promptPanel != null) promptPanel.SetActive(true);
        if (promptText != null) promptText.text = message;
    }

    public void HidePrompt()
    {
        if (promptPanel != null) promptPanel.SetActive(false);
    }
}
