using System;
using System.Collections.Generic;
using UnityEngine;

public class VanInventory : MonoBehaviour
{
    public static VanInventory Instance { get; private set; }

    [Header("--- SETTINGS ---")]
    [Tooltip("Maximum packages to distribute per day")]
    public int dailyPackageCount = 10;

    [Tooltip("Prefab of the physical cargo box dropped on the ground (Optional, creates URP box if empty)")]
    public GameObject cargoPackagePrefab;

    [Header("--- LOADED CARGO IN VAN ---")]
    [SerializeField] private List<CargoItem> loadedCargoList = new List<CargoItem>();

    [Header("--- DELIVERY HISTORY ---")]
    [SerializeField] private List<CargoItem> deliveredHistory = new List<CargoItem>();

    public static event Action<CargoItem, bool, string> OnCargoDeliveredWithFeedback;
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
            Debug.LogWarning("[VanInventory] No DeliveryPoint found in the scene!");
            return;
        }

        string[] englishNames = new string[]
        {
            "John Smith", "Emma Watson", "Michael Brown", "Sarah Davis",
            "David Wilson", "Emily Taylor", "James Anderson", "Olivia Martinez",
            "William Thomas", "Sophia Jackson", "Daniel White", "Ava Harris"
        };

        List<DeliveryPoint> pointPool = new List<DeliveryPoint>(scenePoints);
        ShuffleList(pointPool);

        for (int i = 0; i < dailyPackageCount && i < pointPool.Count; i++)
        {
            DeliveryPoint p = pointPool[i];

            string description = string.IsNullOrEmpty(p.addressDescription) 
                ? "No address description provided." 
                : p.addressDescription;

            CargoItem cargo = new CargoItem(
                $"#CRG-10{i + 1:D2}",
                englishNames[UnityEngine.Random.Range(0, englishNames.Length)],
                p.addressName,
                description,
                p.pointId
            );

            loadedCargoList.Add(cargo);
        }

        Debug.Log($"[VanInventory] Day started! Loaded {loadedCargoList.Count} packages.");
        OnInventoryUpdated?.Invoke();
    }

    /// <summary>
    /// Drops package from van. STRICT RULE: Only exact target point is correct (+50 $), everything else is penalized (-100 $).
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

        // Scan nearby delivery points within 6 meters
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

        bool isCorrect = false;
        string feedbackMessage = "";

        if (reachedPoint != null)
        {
            item.deliveredToAddressName = reachedPoint.addressName;

            if (string.Equals(item.targetPointId.Trim(), reachedPoint.pointId.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                isCorrect = true;
                feedbackMessage = $"[+] SUCCESSFUL DELIVERY! (+{item.deliveryReward} $)";
                reachedPoint.IsFulfilled = true;
                if (reachedPoint.visualMarker != null) reachedPoint.visualMarker.SetActive(false);
            }
            else
            {
                isCorrect = false;
                feedbackMessage = $"[-] WRONG ADDRESS! (-{item.wrongDeliveryPenalty} $ PENALTY)";
            }
        }
        else
        {
            item.deliveredToAddressName = "Street / Empty Area";
            isCorrect = false;
            feedbackMessage = $"[-] DROPPED ON THE STREET! (-{item.wrongDeliveryPenalty} $ PENALTY)";
        }

        item.isDelivered = true;
        item.isDeliveredCorrectly = isCorrect;
        deliveredHistory.Add(item);

        Debug.Log($"[DELIVERY RESULT] {item.trackingNumber} | {feedbackMessage} | Remaining: {loadedCargoList.Count}");

        // Physical Cargo Box Spawn
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
            if (ren != null)
            {
                Shader s = Shader.Find("Universal Render Pipeline/Lit");
                if (s == null) s = Shader.Find("Universal Render Pipeline/Simple Lit");
                if (s == null) s = Shader.Find("Standard");

                Material runtimeMat = (s != null) ? new Material(s) : null;
                if (runtimeMat == null)
                {
                    Renderer anyRen = FindAnyObjectByType<Renderer>();
                    if (anyRen != null && anyRen.sharedMaterial != null)
                    {
                        runtimeMat = new Material(anyRen.sharedMaterial);
                    }
                }

                if (runtimeMat != null)
                {
                    runtimeMat.color = isCorrect 
                        ? new Color(0.3f, 0.75f, 0.35f) 
                        : new Color(0.72f, 0.52f, 0.32f);
                    
                    ren.material = runtimeMat;
                }
            }
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
