using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeliverySelectionUI : MonoBehaviour
{
    [Header("--- PANEL REFERANSLARI ---")]
    public GameObject panelRoot;
    public TextMeshProUGUI addressTitleText;
    public Transform cargoListContent;
    public GameObject cargoCardTemplate;
    public Button deliverButton;
    public TextMeshProUGUI feedbackText;

    private DeliveryPoint currentActiveZone;
    private CargoItem selectedCargoItem;

    private void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (cargoCardTemplate != null) cargoCardTemplate.SetActive(false);

        if (deliverButton != null)
        {
            deliverButton.onClick.RemoveAllListeners();
            deliverButton.onClick.AddListener(OnDeliverButtonClicked);
            deliverButton.interactable = false;
        }
    }

    private void OnEnable()
    {
        DeliveryPoint.OnDeliveryZoneEntered += HandleZoneEntered;
        DeliveryPoint.OnDeliveryZoneExited += HandleZoneExited;
    }

    private void OnDisable()
    {
        DeliveryPoint.OnDeliveryZoneEntered -= HandleZoneEntered;
        DeliveryPoint.OnDeliveryZoneExited -= HandleZoneExited;
    }

    private void HandleZoneEntered(DeliveryPoint zone)
    {
        // Eğer gün sonu paneli açıksa teslimat panelini açma
        if (DaySummaryManager.Instance != null && DaySummaryManager.Instance.summaryPanelRoot != null && DaySummaryManager.Instance.summaryPanelRoot.activeSelf)
        {
            return;
        }

        currentActiveZone = zone;
        selectedCargoItem = null;

        if (panelRoot != null) panelRoot.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (addressTitleText != null)
        {
            addressTitleText.text = $"📍 TESLİMAT NOKTASI: {zone.addressName}";
        }

        if (feedbackText != null)
        {
            feedbackText.text = "Lütfen bagajınızdan bu adrese bırakacağınız paketi seçin:";
        }

        PopulateCargoList();
    }

    private void HandleZoneExited(DeliveryPoint zone)
    {
        if (currentActiveZone == zone)
        {
            currentActiveZone = null;
            selectedCargoItem = null;

            if (panelRoot != null) panelRoot.SetActive(false);

            // Eğer gün sonu paneli açık değilse fareyi kilitle
            if (DaySummaryManager.Instance == null || DaySummaryManager.Instance.summaryPanelRoot == null || !DaySummaryManager.Instance.summaryPanelRoot.activeSelf)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
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
            if (feedbackText != null) feedbackText.text = "Bagajınızda hiç kargo kalmadı!";
            if (deliverButton != null) deliverButton.interactable = false;
            return;
        }

        foreach (CargoItem cargo in loadedList)
        {
            GameObject cardObj = Instantiate(cargoCardTemplate, cargoListContent);
            cardObj.SetActive(true);

            TextMeshProUGUI label = cardObj.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = $"{cargo.trackingNumber} | {cargo.recipientName}\n🎯 Adres: {cargo.targetAddress}";
            }

            Button btn = cardObj.GetComponent<Button>();
            if (btn != null)
            {
                CargoItem itemRef = cargo;
                btn.onClick.AddListener(() => SelectCargo(itemRef, btn));
            }
        }
    }

    private void SelectCargo(CargoItem cargo, Button clickedButton)
    {
        selectedCargoItem = cargo;

        if (deliverButton != null)
        {
            deliverButton.interactable = true;
        }

        if (feedbackText != null)
        {
            feedbackText.text = $"Seçilen Paket: {cargo.trackingNumber} ({cargo.targetAddress})";
        }
    }

    private void OnDeliverButtonClicked()
    {
        if (selectedCargoItem == null || currentActiveZone == null || VanInventory.Instance == null) return;

        DeliveryPoint zoneToDisable = currentActiveZone;
        CargoItem cargoToDeliver = selectedCargoItem;

        // UI'ı kapat
        if (panelRoot != null) panelRoot.SetActive(false);

        // Teslimatı gerçekleştir
        VanInventory.Instance.DeliverCargo(cargoToDeliver, zoneToDisable);

        if (zoneToDisable.visualMarker != null)
        {
            zoneToDisable.visualMarker.SetActive(false);
        }

        // Kalan kargo varsa fareyi sürüş için kilitle, bittiyse serbest bırak
        if (VanInventory.Instance.RemainingCargoCount > 0)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
