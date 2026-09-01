using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class DaySummaryManager : MonoBehaviour
{
    public static DaySummaryManager Instance { get; private set; }

    [Header("--- PANEL REFERENCES ---")]
    public GameObject summaryPanelRoot;
    public TextMeshProUGUI totalDeliveredText;
    public TextMeshProUGUI correctDeliveriesText;
    public TextMeshProUGUI wrongDeliveriesText;
    public TextMeshProUGUI netEarningsText;
    public Transform historyListContent;
    public GameObject historyItemTemplate;
    public Button restartDayButton;

    private bool isDayFinalized = false;

    private void Awake()
    {
        Instance = this;

        if (summaryPanelRoot == null)
        {
            Transform t = transform.Find("DaySummaryPanel");
            if (t != null) summaryPanelRoot = t.gameObject;
        }

        if (summaryPanelRoot != null) summaryPanelRoot.SetActive(false);
        if (historyItemTemplate != null) historyItemTemplate.SetActive(false);

        if (restartDayButton != null)
        {
            restartDayButton.onClick.RemoveAllListeners();
            restartDayButton.onClick.AddListener(RestartDay);
        }
    }

    private void Start()
    {
        isDayFinalized = false;
        if (summaryPanelRoot != null) summaryPanelRoot.SetActive(false);
    }

    private void OnEnable()
    {
        VanInventory.OnCargoDelivered += CheckForEndOfDay;
    }

    private void OnDisable()
    {
        VanInventory.OnCargoDelivered -= CheckForEndOfDay;
    }

    private void Update()
    {
        if (summaryPanelRoot != null && summaryPanelRoot.activeSelf)
        {
            if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }

    private void CheckForEndOfDay(CargoItem deliveredCargo, bool isCorrect)
    {
        if (VanInventory.Instance == null) return;

        if (VanInventory.Instance.RemainingCargoCount <= 0)
        {
            ShowDaySummary();
        }
    }

    public void ShowDaySummary()
    {
        if (summaryPanelRoot == null)
        {
            Transform t = transform.Find("DaySummaryPanel");
            if (t != null) summaryPanelRoot = t.gameObject;
        }

        if (summaryPanelRoot != null)
        {
            summaryPanelRoot.SetActive(true);
        }

        if (CargoTabletUI.Instance != null)
        {
            CargoTabletUI.Instance.CloseTablet();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        List<CargoItem> history = VanInventory.Instance != null ? VanInventory.Instance.DeliveredHistory : new List<CargoItem>();

        int totalDelivered = history.Count;
        int correctCount = 0;
        int wrongCount = 0;
        int totalReward = 0;
        int totalPenalty = 0;

        foreach (var item in history)
        {
            if (item.isDeliveredCorrectly)
            {
                correctCount++;
                totalReward += item.deliveryReward;
            }
            else
            {
                wrongCount++;
                totalPenalty += item.wrongDeliveryPenalty;
            }
        }

        int netProfit = totalReward - totalPenalty;

        if (!isDayFinalized && PlayerEconomyManager.Instance != null)
        {
            isDayFinalized = true;
            PlayerEconomyManager.Instance.FinalizeAndSaveDay();
        }

        int totalVault = PlayerEconomyManager.Instance != null ? PlayerEconomyManager.Instance.TotalSavedBalance : netProfit;

        if (totalDeliveredText != null) totalDeliveredText.text = $"Total Delivered: {totalDelivered} Packages";
        if (correctDeliveriesText != null) correctDeliveriesText.text = $"[+] Correct Deliveries: {correctCount} (+{totalReward} $)";
        if (wrongDeliveriesText != null) wrongDeliveriesText.text = $"[-] Wrong Deliveries: {wrongCount} (-{totalPenalty} $ Penalty)";
        
        if (netEarningsText != null)
        {
            string profitLabel = netProfit >= 0 ? $"+{netProfit} $" : $"{netProfit} $";
            netEarningsText.text = $"TODAY: {profitLabel} | TOTAL VAULT: {totalVault} $";
            netEarningsText.color = netProfit >= 0 ? new Color(0.2f, 0.95f, 0.3f) : new Color(0.95f, 0.2f, 0.2f);
        }

        PopulateHistoryList(history);
    }

    private void PopulateHistoryList(List<CargoItem> history)
    {
        if (historyListContent == null || historyItemTemplate == null) return;

        foreach (Transform child in historyListContent)
        {
            if (child.gameObject != historyItemTemplate)
            {
                Destroy(child.gameObject);
            }
        }

        foreach (var item in history)
        {
            GameObject rowObj = Instantiate(historyItemTemplate, historyListContent);
            rowObj.SetActive(true);

            TextMeshProUGUI rowText = rowObj.GetComponentInChildren<TextMeshProUGUI>();
            Image rowImg = rowObj.GetComponent<Image>();

            if (rowText != null)
            {
                string statusText = item.isDeliveredCorrectly ? "[CORRECT]" : "[WRONG]";
                rowText.text = $"{item.trackingNumber} ({item.recipientName})\nDelivered to: {item.deliveredToAddressName} | Target: {item.targetAddress} {statusText}";
            }

            if (rowImg != null)
            {
                rowImg.color = item.isDeliveredCorrectly 
                    ? new Color(0.15f, 0.4f, 0.2f, 0.9f)
                    : new Color(0.5f, 0.15f, 0.15f, 0.9f);
            }
        }
    }

    public void RestartDay()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
