using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DeliveryNotificationHUD : MonoBehaviour
{
    public static DeliveryNotificationHUD Instance { get; private set; }

    [Header("--- BİLDİRİM PANELİ ---")]
    public GameObject notificationRoot;
    public TextMeshProUGUI notificationText;
    public Image notificationBackground;

    [Header("--- CANLI SAYAÇ ---")]
    public TextMeshProUGUI remainingCargoCounterText;

    private float hideTimer = 0f;

    private void Awake()
    {
        Instance = this;
        if (notificationRoot != null) notificationRoot.SetActive(false);
    }

    private void OnEnable()
    {
        VanInventory.OnCargoDeliveredWithFeedback += ShowNotification;
        VanInventory.OnInventoryUpdated += UpdateRemainingCount;
    }

    private void OnDisable()
    {
        VanInventory.OnCargoDeliveredWithFeedback -= ShowNotification;
        VanInventory.OnInventoryUpdated -= UpdateRemainingCount;
    }

    private void Update()
    {
        if (hideTimer > 0f)
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0f && notificationRoot != null)
            {
                notificationRoot.SetActive(false);
            }
        }
    }

    public void ShowNotification(string message, bool isCorrect)
    {
        ShowNotification(null, isCorrect, message);
    }

    public void ShowNotification(CargoItem item, bool isCorrect, string message)
    {
        if (notificationRoot == null || notificationText == null) return;

        notificationRoot.SetActive(true);
        notificationText.text = message;
        hideTimer = 3.5f;

        if (notificationBackground != null)
        {
            notificationBackground.color = isCorrect 
                ? new Color(0.1f, 0.6f, 0.2f, 0.9f)  // Yeşil
                : new Color(0.75f, 0.15f, 0.15f, 0.95f); // Kırmızı
        }
    }

    private void UpdateRemainingCount()
    {
        if (remainingCargoCounterText != null && VanInventory.Instance != null)
        {
            remainingCargoCounterText.text = $"📦 Kalan Kargo: {VanInventory.Instance.RemainingCargoCount} / 10";
        }
    }
}
