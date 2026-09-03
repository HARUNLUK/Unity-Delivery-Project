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
        DayTimeManager.OnShiftEnded += ShowDaySummary;
    }

    private void OnDisable()
    {
        DayTimeManager.OnShiftEnded -= ShowDaySummary;
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

    [ContextMenu("Trigger End Of Day Summary")]
    public void ShowDaySummary()
    {
        if (isDayFinalized) return;
        isDayFinalized = true;

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

        // 1. Sahnede bulunan tüm fiziksel kargo paketlerini topla
        PhysicalCargoPackage[] scenePackages = Object.FindObjectsByType<PhysicalCargoPackage>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        List<CargoDeliveryResult> results = new List<CargoDeliveryResult>();

        int totalCount = scenePackages.Length;
        int correctCount = 0;
        int wrongCount = 0;
        int undeliveredCount = 0;
        int totalReward = 0;
        int totalPenalty = 0;

        foreach (var pkg in scenePackages)
        {
            if (pkg == null) continue;

            CargoDeliveryResult res = pkg.EvaluateEndOfDayResult();
            results.Add(res);

            if (res.status == CargoDeliveryStatus.Correct)
            {
                correctCount++;
                totalReward += res.moneyChange;
            }
            else if (res.status == CargoDeliveryStatus.WrongAddress)
            {
                wrongCount++;
                totalPenalty += Mathf.Abs(res.moneyChange);
            }
            else
            {
                undeliveredCount++;
                totalPenalty += Mathf.Abs(res.moneyChange);
            }
        }

        int netProfit = totalReward - totalPenalty;

        // 2. Ekonomiyi güncelle ve kaydet
        if (PlayerEconomyManager.Instance != null)
        {
            PlayerEconomyManager.Instance.AddEarnings(totalReward);
            PlayerEconomyManager.Instance.AddPenalty(totalPenalty);
            PlayerEconomyManager.Instance.FinalizeAndSaveDay();
        }

        int totalVault = PlayerEconomyManager.Instance != null ? PlayerEconomyManager.Instance.TotalSavedBalance : netProfit;

        // 3. UI Metinlerini Doldur
        if (totalDeliveredText != null) totalDeliveredText.text = $"Total Packages Today: {totalCount}";
        if (correctDeliveriesText != null) correctDeliveriesText.text = $"[+] Correct Deliveries: {correctCount} (+${totalReward})";
        if (wrongDeliveriesText != null) wrongDeliveriesText.text = $"[-] Wrong / Undelivered: {wrongCount + undeliveredCount} (-${totalPenalty} Penalty)";
        
        if (netEarningsText != null)
        {
            string profitLabel = netProfit >= 0 ? $"+${netProfit}" : $"-${Mathf.Abs(netProfit)}";
            netEarningsText.text = $"TODAY NET: {profitLabel} | TOTAL VAULT: ${totalVault}";
            netEarningsText.color = netProfit >= 0 ? new Color(0.2f, 0.95f, 0.3f) : new Color(0.95f, 0.2f, 0.2f);
        }

        // 4. Detaylı Liste Satırlarını Oluştur
        PopulateResultsList(results);
    }

    private void PopulateResultsList(List<CargoDeliveryResult> results)
    {
        if (historyListContent == null || historyItemTemplate == null) return;

        foreach (Transform child in historyListContent)
        {
            if (child.gameObject != historyItemTemplate)
            {
                Destroy(child.gameObject);
            }
        }

        foreach (var res in results)
        {
            GameObject rowObj = Instantiate(historyItemTemplate, historyListContent);
            rowObj.SetActive(true);

            TextMeshProUGUI rowText = rowObj.GetComponentInChildren<TextMeshProUGUI>();
            Image rowImg = rowObj.GetComponent<Image>();

            if (rowText != null)
            {
                string statusLabel = "";
                if (res.status == CargoDeliveryStatus.Correct) statusLabel = "<color=#32FF64>[CORRECT DELIVERY]</color>";
                else if (res.status == CargoDeliveryStatus.WrongAddress) statusLabel = "<color=#FF4444>[WRONG ADDRESS]</color>";
                else statusLabel = "<color=#FFAA22>[NOT DELIVERED]</color>";

                string moneyLabel = res.moneyChange >= 0 ? $"+${res.moneyChange}" : $"-${Mathf.Abs(res.moneyChange)}";

                rowText.text = $"{res.trackingNumber} | {statusLabel} ({moneyLabel})\nTarget: {res.targetAddress} | Location: {res.actualAddress}";
            }

            if (rowImg != null)
            {
                if (res.status == CargoDeliveryStatus.Correct)
                    rowImg.color = new Color(0.12f, 0.38f, 0.18f, 0.9f);
                else if (res.status == CargoDeliveryStatus.WrongAddress)
                    rowImg.color = new Color(0.48f, 0.14f, 0.14f, 0.9f);
                else
                    rowImg.color = new Color(0.38f, 0.28f, 0.12f, 0.9f);
            }
        }
    }

    public void RestartDay()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
