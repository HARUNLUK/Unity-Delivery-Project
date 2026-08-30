using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class CargoTabletUI : MonoBehaviour
{
    public static CargoTabletUI Instance { get; private set; }

    [Header("--- PANEL REFERANSLARI ---")]
    public GameObject tabletPanelRoot;
    public Transform cargoListContent;
    public GameObject cargoCardTemplate;

    [Header("--- DETAY VE TARİF PANELİ ---")]
    public GameObject detailCardRoot;
    public TextMeshProUGUI trackingNumberText;
    public TextMeshProUGUI recipientNameText;
    public TextMeshProUGUI targetAddressText;
    public TextMeshProUGUI addressDescriptionText;
    public Button dropCargoButton;

    [Header("--- BİLGİ & DURUM ---")]
    public TextMeshProUGUI emptyListText;

    private CargoItem currentSelectedCargo;
    private bool isTabletOpen = false;

    public bool IsTabletOpen => isTabletOpen;

    private void Awake()
    {
        Instance = this;

        // Otomatik referans bulucu (Bağlantı kopsa bile garanti bulur)
        if (tabletPanelRoot == null)
        {
            Transform t = transform.Find("CargoTabletPanel");
            if (t != null) tabletPanelRoot = t.gameObject;
        }

        if (tabletPanelRoot != null) tabletPanelRoot.SetActive(false);
        if (cargoCardTemplate != null) cargoCardTemplate.SetActive(false);

        if (dropCargoButton != null)
        {
            dropCargoButton.onClick.RemoveAllListeners();
            dropCargoButton.onClick.AddListener(OnDropCargoClicked);
        }
    }

    private void Start()
    {
        if (tabletPanelRoot != null) tabletPanelRoot.SetActive(false);
    }

    private void OnEnable()
    {
        VanInventory.OnInventoryUpdated += RefreshUI;
    }

    private void OnDisable()
    {
        VanInventory.OnInventoryUpdated -= RefreshUI;
    }

    private void Update()
    {
        // Gün sonu paneli açıksa tableti açma
        if (DaySummaryManager.Instance != null && DaySummaryManager.Instance.summaryPanelRoot != null && DaySummaryManager.Instance.summaryPanelRoot.activeSelf)
        {
            if (isTabletOpen) CloseTablet();
            return;
        }

        // TAB, T, M veya I TUŞLARINI DOĞRUDAN DİNLE (Preprocessor engeli olmadan)
        if (CheckToggleInput())
        {
            ToggleTablet();
        }
    }

    private bool CheckToggleInput()
    {
        // 1. New Input System Kontrolü
        if (Keyboard.current != null)
        {
            if (Keyboard.current.tabKey.wasPressedThisFrame ||
                Keyboard.current.tKey.wasPressedThisFrame ||
                Keyboard.current.mKey.wasPressedThisFrame ||
                Keyboard.current.iKey.wasPressedThisFrame)
            {
                return true;
            }
        }

        // 2. Legacy Input Manager Fallback (Eski Giriş Sistemi)
        try
        {
            if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.T) || Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.I))
            {
                return true;
            }
        }
        catch { }

        return false;
    }

    public void ToggleTablet()
    {
        if (isTabletOpen) CloseTablet();
        else OpenTablet();
    }

    public void OpenTablet()
    {
        if (tabletPanelRoot == null)
        {
            Transform t = transform.Find("CargoTabletPanel");
            if (t != null) tabletPanelRoot = t.gameObject;
        }

        isTabletOpen = true;

        if (tabletPanelRoot != null)
        {
            tabletPanelRoot.SetActive(true);
        }
        else
        {
            Debug.LogError("[CargoTabletUI] tabletPanelRoot bulunamadı! Lütfen Tools -> Kargo Oyunu -> Temiz Teslimat UI Insa Et seçeneğine tıklayın.");
            return;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        RefreshUI();
    }

    public void CloseTablet()
    {
        isTabletOpen = false;
        if (tabletPanelRoot != null) tabletPanelRoot.SetActive(false);

        // Gün sonu açık değilse fareyi sürüşe kilitle
        if (DaySummaryManager.Instance == null || DaySummaryManager.Instance.summaryPanelRoot == null || !DaySummaryManager.Instance.summaryPanelRoot.activeSelf)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void RefreshUI()
    {
        if (!isTabletOpen) return;

        PopulateCargoList();
    }

    private void PopulateCargoList()
    {
        if (cargoListContent == null || cargoCardTemplate == null) return;

        foreach (Transform child in cargoListContent)
        {
            if (child.gameObject != cargoCardTemplate)
            {
                Destroy(child.gameObject);
            }
        }

        List<CargoItem> loadedList = VanInventory.Instance != null ? VanInventory.Instance.LoadedCargoList : new List<CargoItem>();

        if (loadedList.Count == 0)
        {
            if (emptyListText != null) emptyListText.gameObject.SetActive(true);
            if (detailCardRoot != null) detailCardRoot.SetActive(false);
            return;
        }

        if (emptyListText != null) emptyListText.gameObject.SetActive(false);

        foreach (CargoItem cargo in loadedList)
        {
            GameObject cardObj = Instantiate(cargoCardTemplate, cargoListContent);
            cardObj.SetActive(true);

            TextMeshProUGUI label = cardObj.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = $"<b>{cargo.trackingNumber}</b>\n{cargo.recipientName}";
            }

            Button btn = cardObj.GetComponent<Button>();
            if (btn != null)
            {
                CargoItem itemRef = cargo;
                btn.onClick.AddListener(() => DisplayCargoDetail(itemRef));
            }
        }

        // İlk paketi seç ve göster
        if (currentSelectedCargo == null || !loadedList.Contains(currentSelectedCargo))
        {
            DisplayCargoDetail(loadedList[0]);
        }
        else
        {
            DisplayCargoDetail(currentSelectedCargo);
        }
    }

    private void DisplayCargoDetail(CargoItem cargo)
    {
        currentSelectedCargo = cargo;
        if (detailCardRoot != null) detailCardRoot.SetActive(true);

        if (trackingNumberText != null) trackingNumberText.text = $"Takip No: {cargo.trackingNumber}";
        if (recipientNameText != null) recipientNameText.text = $"Alici: {cargo.recipientName}";
        if (targetAddressText != null) targetAddressText.text = $"Kayitli Adres: {cargo.targetAddress}";
        
        if (addressDescriptionText != null)
        {
            addressDescriptionText.text = $"<b>Adres / Ev Tarifi:</b>\n\n\"{cargo.targetAddressDescription}\"";
        }
    }

    private void OnDropCargoClicked()
    {
        if (currentSelectedCargo == null || VanInventory.Instance == null) return;

        CargoItem toDrop = currentSelectedCargo;
        currentSelectedCargo = null;

        // Paketi yere düşür
        VanInventory.Instance.DropCargoFromVan(toDrop);

        // Tableti kapat
        CloseTablet();
    }
}
