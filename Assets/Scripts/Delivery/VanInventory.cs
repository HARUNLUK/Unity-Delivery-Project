using System;
using System.Collections.Generic;
using UnityEngine;

public class VanInventory : MonoBehaviour
{
    public static VanInventory Instance { get; private set; }

    [Header("--- AYARLAR ---")]
    [Tooltip("Günde dağıtılacak maksimum paket sayısı")]
    public int dailyPackageCount = 10;

    [Tooltip("Yere düşecek fiziksel kargo kutusu prefab'ı (Boş bırakılırsa otomatik küp oluşturulur)")]
    public GameObject cargoPackagePrefab;

    [Header("--- BAGAJDAKİ KARGOLAR ---")]
    [SerializeField] private List<CargoItem> loadedCargoList = new List<CargoItem>();

    [Header("--- TESLİMAT GEÇMİŞİ ---")]
    [SerializeField] private List<CargoItem> deliveredHistory = new List<CargoItem>();

    public static event Action<CargoItem, bool, string> OnCargoDeliveredWithFeedback; // (Kargo, Doğru mu?, Geri bildirim metni)
    public static event Action<CargoItem, bool> OnCargoDelivered;
    public static event Action OnInventoryUpdated;

    public List<CargoItem> LoadedCargoList => loadedCargoList;
    public List<CargoItem> DeliveredHistory => deliveredHistory;
    public int RemainingCargoCount => loadedCargoList.Count;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        GenerateDailyCargoList();
    }

    public void GenerateDailyCargoList()
    {
        loadedCargoList.Clear();
        deliveredHistory.Clear();

        DeliveryPoint[] scenePoints = FindObjectsByType<DeliveryPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        if (scenePoints.Length == 0)
        {
            Debug.LogWarning("[VanInventory] Sahnede hiç DeliveryPoint bulunamadı!");
            return;
        }

        string[] randomNames = new string[]
        {
            "Ahmet Yilmaz", "Ayse Kaya", "Mehmet Demir", "Fatma Celik",
            "Mustafa Sahin", "Zeynep Yildiz", "Emre Ozturk", "Elif Aydin",
            "Burak Arslan", "Selin Dogan", "Can Polat", "Gizem Koc"
        };

        List<DeliveryPoint> pointPool = new List<DeliveryPoint>(scenePoints);
        ShuffleList(pointPool);

        for (int i = 0; i < dailyPackageCount && i < pointPool.Count; i++)
        {
            DeliveryPoint p = pointPool[i];

            string description = string.IsNullOrEmpty(p.addressDescription) 
                ? "Adres tarifi bulunmuyor." 
                : p.addressDescription;

            CargoItem cargo = new CargoItem(
                $"#KRG-10{i + 1:D2}",
                randomNames[UnityEngine.Random.Range(0, randomNames.Length)],
                p.addressName,
                description,
                p.pointId
            );

            loadedCargoList.Add(cargo);
        }

        Debug.Log($"[VanInventory] Güne başlandı! Toplam {loadedCargoList.Count} paket yüklendi.");
        OnInventoryUpdated?.Invoke();
    }

    /// <summary>
    /// Kargo paketini yere bırakır. KESİN KURAL: Yalnızca doğru noktaya düştüyse doğru sayılır, diğer tüm durumlarda CEZA kesilir.
    /// </summary>
    public void DropCargoFromVan(CargoItem item)
    {
        if (item == null || !loadedCargoList.Contains(item)) return;

        loadedCargoList.Remove(item);

        CarController car = FindAnyObjectByType<CarController>();
        Vector3 spawnPos = transform.position;
        Vector3 carVelocity = Vector3.zero;

        if (car != null)
        {
            spawnPos = car.transform.position - (car.transform.forward * 3.8f) + (Vector3.up * 0.8f);
            Rigidbody carRb = car.GetComponent<Rigidbody>();
            if (carRb != null) carVelocity = carRb.linearVelocity * 0.3f;
        }

        // Çevredeki DeliveryPoint'leri tara (6 metre yarıçap)
        Collider[] hits = Physics.OverlapSphere(spawnPos, 6.0f);
        DeliveryPoint reachedPoint = null;

        foreach (var hit in hits)
        {
            DeliveryPoint p = hit.GetComponentInParent<DeliveryPoint>();
            if (p != null)
            {
                reachedPoint = p;
                break;
            }
        }

        // KESİN KURAL KONTROLÜ
        bool isCorrect = false;
        string feedbackMessage = "";

        if (reachedPoint != null)
        {
            item.deliveredToAddressName = reachedPoint.addressName;

            // Point ID tam eşleşiyorsa DOĞRU, değilse HATALI
            if (string.Equals(item.targetPointId.Trim(), reachedPoint.pointId.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                isCorrect = true;
                feedbackMessage = $"[+] DOGRU TESLIMAT! (+{item.deliveryReward} TL)";
                reachedPoint.IsFulfilled = true;
                if (reachedPoint.visualMarker != null) reachedPoint.visualMarker.SetActive(false);
            }
            else
            {
                isCorrect = false;
                feedbackMessage = $"[-] YANLIS ADRESE BIRAKILDI! (-{item.wrongDeliveryPenalty} TL CEZA)";
            }
        }
        else
        {
            // Sokağa / boşluğa bırakıldı
            item.deliveredToAddressName = "Sokak / Bos Alan";
            isCorrect = false;
            feedbackMessage = $"[-] SOKAK ORTASINA BIRAKILDI! (-{item.wrongDeliveryPenalty} TL CEZA)";
        }

        item.isDelivered = true;
        item.isDeliveredCorrectly = isCorrect;
        deliveredHistory.Add(item);

        Debug.Log($"[TESLİMAT SONUCU] {item.trackingNumber} | {feedbackMessage} | Kalan Paket: {loadedCargoList.Count}");

        // Görsel Fiziksel Kutu Oluştur
        GameObject boxObj;
        if (cargoPackagePrefab != null)
        {
            boxObj = Instantiate(cargoPackagePrefab, spawnPos, car != null ? car.transform.rotation : Quaternion.identity);
        }
        else
        {
            boxObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boxObj.name = $"DroppedCargo_{item.trackingNumber}";
            boxObj.transform.position = spawnPos;
            boxObj.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
            
            Renderer ren = boxObj.GetComponent<Renderer>();
            if (ren != null) ren.material.color = isCorrect ? new Color(0.2f, 0.7f, 0.3f) : new Color(0.7f, 0.3f, 0.2f);
        }

        Rigidbody rb = boxObj.GetComponent<Rigidbody>();
        if (rb == null) rb = boxObj.AddComponent<Rigidbody>();
        rb.mass = 15f;
        rb.linearVelocity = carVelocity;
        rb.AddTorque(UnityEngine.Random.insideUnitSphere * 4f, ForceMode.Impulse);

        OnCargoDelivered?.Invoke(item, isCorrect);
        OnCargoDeliveredWithFeedback?.Invoke(item, isCorrect, feedbackMessage);
        OnInventoryUpdated?.Invoke();
    }

    public bool DeliverCargo(CargoItem item, DeliveryPoint currentZone)
    {
        DropCargoFromVan(item);
        return item.isDeliveredCorrectly;
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int rnd = UnityEngine.Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[rnd];
            list[rnd] = temp;
        }
    }
}
