using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using TMPro;

public class CargoTabletUI : MonoBehaviour
{
    public static CargoTabletUI Instance { get; private set; }

    [Header("--- PANEL REFERENCES ---")]
    public GameObject tabletPanelRoot;
    public Transform cargoListContent;
    public GameObject cargoCardTemplate;

    [Header("--- DETAIL & CLUE PANEL ---")]
    public GameObject detailCardRoot;
    public TextMeshProUGUI trackingNumberText;
    public TextMeshProUGUI recipientNameText;
    public TextMeshProUGUI targetAddressText;
    public TextMeshProUGUI addressDescriptionText;
    public Button dropCargoButton;

    [Header("--- INFO & STATUS ---")]
    public TextMeshProUGUI emptyListText;

    private CargoItem currentSelectedCargo;
    private bool isTabletOpen = false;

    public bool IsTabletOpen => isTabletOpen;

    private void Awake()
    {
        Instance = this;

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

        EnsureEventSystemAndRaycaster();
    }

    private void Start()
    {
        if (tabletPanelRoot != null) tabletPanelRoot.SetActive(false);
        EnsureEventSystemAndRaycaster();
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
        if (DaySummaryManager.Instance != null && DaySummaryManager.Instance.summaryPanelRoot != null && DaySummaryManager.Instance.summaryPanelRoot.activeSelf)
        {
            if (isTabletOpen) CloseTablet();
            return;
        }

        if (CheckToggleInput())
        {
            ToggleTablet();
        }
    }

    private bool CheckToggleInput()
    {
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
        EnsureEventSystemAndRaycaster();

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

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        RefreshUI();
    }

    public void CloseTablet()
    {
        isTabletOpen = false;
        if (tabletPanelRoot != null) tabletPanelRoot.SetActive(false);

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

            Image cardImg = cardObj.GetComponent<Image>();
            if (cardImg != null) cardImg.raycastTarget = true;

            TextMeshProUGUI label = cardObj.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = $"<b>{cargo.trackingNumber}</b>\n{cargo.recipientName}";
                label.raycastTarget = false; // Metnin tıklamayı engellemesini önler
            }

            Button btn = cardObj.GetComponent<Button>();
            if (btn == null) btn = cardObj.AddComponent<Button>();

            btn.interactable = true;
            btn.onClick.RemoveAllListeners();

            CargoItem itemRef = cargo;
            btn.onClick.AddListener(() => {
                DisplayCargoDetail(itemRef);
            });
        }

        if (currentSelectedCargo == null || !loadedList.Contains(currentSelectedCargo))
        {
            DisplayCargoDetail(loadedList[0]);
        }
        else
        {
            DisplayCargoDetail(currentSelectedCargo);
        }
    }

    public void DisplayCargoDetail(CargoItem cargo)
    {
        currentSelectedCargo = cargo;
        if (detailCardRoot != null) detailCardRoot.SetActive(true);

        if (trackingNumberText != null) trackingNumberText.text = $"Tracking No: {cargo.trackingNumber}";
        if (recipientNameText != null) recipientNameText.text = $"Recipient: {cargo.recipientName}";
        if (targetAddressText != null) targetAddressText.text = $"Address: {cargo.targetAddress}";
        
        if (addressDescriptionText != null)
        {
            addressDescriptionText.text = $"<b>Delivery Clue / Description:</b>\n\n\"{cargo.targetAddressDescription}\"";
        }
    }

    private void OnDropCargoClicked()
    {
        if (currentSelectedCargo == null || VanInventory.Instance == null) return;

        CargoItem toDrop = currentSelectedCargo;
        currentSelectedCargo = null;

        VanInventory.Instance.DropCargoFromVan(toDrop);
        CloseTablet();
    }

    /// <summary>
    /// EventSystem ve GraphicRaycaster bileşenlerinin fare tıklamalarını yakalayabilmesini garantiye alır.
    /// </summary>
    public void EnsureEventSystemAndRaycaster()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = Object.FindAnyObjectByType<Canvas>();

        if (canvas != null)
        {
            GraphicRaycaster gr = canvas.GetComponent<GraphicRaycaster>();
            if (gr == null) canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        EventSystem es = Object.FindAnyObjectByType<EventSystem>();
        if (es == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            es = esObj.AddComponent<EventSystem>();
        }

#if ENABLE_INPUT_SYSTEM
        if (es.GetComponent<InputSystemUIInputModule>() == null)
        {
            StandaloneInputModule oldMod = es.GetComponent<StandaloneInputModule>();
            if (oldMod != null) DestroyImmediate(oldMod);

            es.gameObject.AddComponent<InputSystemUIInputModule>();
        }
#else
        if (es.GetComponent<StandaloneInputModule>() == null)
        {
            es.gameObject.AddComponent<StandaloneInputModule>();
        }
#endif
    }
}
