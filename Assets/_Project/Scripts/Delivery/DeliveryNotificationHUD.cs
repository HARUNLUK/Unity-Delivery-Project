using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DeliveryNotificationHUD : MonoBehaviour
{
    public static DeliveryNotificationHUD Instance { get; private set; }

    [Header("--- NOTIFICATION PANEL ---")]
    public GameObject notificationRoot;
    public TextMeshProUGUI notificationText;
    public Image notificationBackground;
    [Tooltip("Optional: check / cross icon and the darker lip edge of the toast card.")]
    public Image notificationIcon;
    public Shadow notificationLip;

    [Header("--- LIVE COUNTER ---")]
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

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayNotification();
        }

        if (notificationBackground != null)
        {
            notificationBackground.color = isCorrect ? CozyTheme.Mint : CozyTheme.Red;
        }
        if (notificationLip != null)
        {
            notificationLip.effectColor = isCorrect ? CozyTheme.MintLip : CozyTheme.RedLip;
        }
        if (notificationIcon != null && CozyAssets.Instance != null)
        {
            Sprite icon = CozyAssets.Instance.Icon(isCorrect ? "check" : "x");
            if (icon != null) notificationIcon.sprite = icon;
        }
    }

    private void UpdateRemainingCount()
    {
        if (remainingCargoCounterText != null && VanInventory.Instance != null)
        {
            remainingCargoCounterText.text = $"{VanInventory.Instance.RemainingCargoCount} / {VanInventory.Instance.dailyPackageCount}";
        }
    }
}
