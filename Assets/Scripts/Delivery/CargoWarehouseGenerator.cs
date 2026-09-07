using System.Collections.Generic;
using UnityEngine;

public class CargoWarehouseGenerator : MonoBehaviour
{
    [Header("--- GENERATION SETTINGS ---")]
    [Tooltip("Size of the platform spawn area (Width, Height, Length)")]
    public Vector3 spawnAreaSize = new Vector3(4.5f, 0.4f, 4.5f);

    [Tooltip("Default fallback package count if progression manager is missing")]
    public int packageCount = 4;

    [Tooltip("Reward range for standard packages")]
    public int minReward = 80;
    public int maxReward = 180;
    public int wrongPenalty = 40;

    [Tooltip("Günün başında paketlerin otomatik üretilip üretilmeyeceği")]
    public bool autoSpawnOnStart = true;

    [Header("--- CARGO TYPE UNLOCK LEVELS & CHANCES (SEVİYE KİLİTLERİ) ---")]
    [Tooltip("Standart kargonun aktif olduğu minimum oyuncu seviyesi")]
    public int standardRequiredLevel = 1;
    [Range(0f, 100f), Tooltip("Standart kargo çıkma ağırlığı / şansı")]
    public float standardSpawnWeight = 50f;

    [Tooltip("Kırılabilir (Fragile) kargonun aktif olduğu minimum oyuncu seviyesi")]
    public int fragileRequiredLevel = 5;
    [Range(0f, 100f), Tooltip("Kırılabilir kargo çıkma ağırlığı / şansı")]
    public float fragileSpawnWeight = 30f;

    [Tooltip("Zamanlı / Ekspres (Express) kargonun aktif olduğu minimum oyuncu seviyesi")]
    public int expressRequiredLevel = 8;
    [Range(0f, 100f), Tooltip("Zamanlı / Ekspres kargo çıkma ağırlığı / şansı")]
    public float expressSpawnWeight = 20f;

    [Header("--- FRAGILE CARGO TUNING (KIRILMA HASSASİYETİ) ---")]
    [Tooltip("Hasar almak için gereken minimum çarpma hızı (m/s). Yere nazikçe koyma < 2.5 m/s, 1.5m elden düşüş ~5.0 m/s, yüksekten düşüş > 7.0 m/s. (Önerilen: 3.0 - 4.0)")]
    public float fragileMinDamageSpeedThreshold = 3.5f;

    [Tooltip("Eşik hız aşıldığında hız başına alınan hasar çarpanı. (Önerilen: 14 - 20)")]
    public float fragileDamageMultiplier = 16.0f;

    [Tooltip("Kargolar birbirine çarptığında alınan hasar çarpanı (0.20 = %80 daha az hasar).")]
    public float fragilePackageCollisionRatio = 0.20f;

    [Tooltip("Paket ilk oluştuğunda kaç saniye boyunca hasar almaz.")]
    public float fragileSpawnImmunityDuration = 3.5f;

    [Header("--- CARGO BOX MATERIALS (SKINS) ---")]
    [Tooltip("Standart kargo paketleri için karton kaplama materyalleri (Inspector'dan materyal sürükleyebilirsiniz, birden fazla ise rastgele seçilir)")]
    public List<Material> cardboardMaterials = new List<Material>();

    [Tooltip("Kırılabilir (Fragile) kargolar için özel materyaller (Boş bırakılırsa standart materyaller kullanılır)")]
    public List<Material> fragileMaterials = new List<Material>();

    [Tooltip("Zamanlı / Ekspres (Express) kargolar için özel materyaller (Boş bırakılırsa standart materyaller kullanılır)")]
    public List<Material> expressMaterials = new List<Material>();

    [Header("--- CURRENT ACTIVE PACKAGES ---")]
    public List<PhysicalCargoPackage> currentPackages = new List<PhysicalCargoPackage>();

    private void Awake()
    {
        // Sahne veya prefab üzerinde eski 13.0f veya 8.0f gibi değerler kayıtlı kaldıysa otomatik düzelt
        if (fragileMinDamageSpeedThreshold > 5.0f || fragileMinDamageSpeedThreshold < 1.0f)
        {
            fragileMinDamageSpeedThreshold = 3.5f;
        }
        if (fragileDamageMultiplier < 10.0f)
        {
            fragileDamageMultiplier = 16.0f;
        }
    }

    private void Start()
    {
        if (!autoSpawnOnStart) return;

        // Prevent duplicate/double spawning: check if packages are already in the scene!
        PhysicalCargoPackage[] existing = Object.FindObjectsByType<PhysicalCargoPackage>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (existing != null && existing.Length > 0)
        {
            return;
        }

        SpawnCargoBatch();
    }

    [ContextMenu("Generate Cargo Batch")]
    public void SpawnCargoBatch()
    {
        // Don't spawn if packages are already active in the scene
        PhysicalCargoPackage[] existing = Object.FindObjectsByType<PhysicalCargoPackage>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (existing != null && existing.Length > 0)
        {
            return;
        }

        ClearOldPackages();

        int playerLevel = PlayerProgressionManager.Instance != null ? PlayerProgressionManager.Instance.PlayerLevel : 1;
        int toSpawn = PlayerProgressionManager.Instance != null ? PlayerProgressionManager.Instance.GetDailyPackageLimit() : Mathf.Max(1, packageCount);

        DeliveryPoint[] allPoints = Object.FindObjectsByType<DeliveryPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (allPoints == null || allPoints.Length == 0)
        {
            Debug.LogWarning("[CargoWarehouseGenerator] No DeliveryPoint found in the scene! Place some DeliveryPoint objects first.");
            return;
        }

        // Filter points by player level
        List<DeliveryPoint> availablePoints = new List<DeliveryPoint>();
        foreach (var p in allPoints)
        {
            if (p.requiredLevel <= playerLevel)
            {
                availablePoints.Add(p);
            }
        }

        // Fallback to all points if no points match level
        if (availablePoints.Count == 0)
        {
            availablePoints.AddRange(allPoints);
        }

        for (int i = 0; i < toSpawn; i++)
        {
            // Pick a destination point
            DeliveryPoint targetPoint = availablePoints[Random.Range(0, availablePoints.Count)];

            // Calculate safe collision-checked spawn position
            Vector3 spawnPos = GetSafeSpawnPosition(i);
            Quaternion spawnRot = transform.rotation * Quaternion.Euler(0f, Random.Range(-25f, 25f), 0f);

            // Create standard physical cargo cube directly in the scene (no extra containers)
            GameObject boxObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boxObj.name = $"Cargo_Package_#{targetPoint.pointId}_{i + 1}";
            boxObj.transform.position = spawnPos;
            boxObj.transform.rotation = spawnRot;
            boxObj.transform.SetParent(null); // Standalone in scene root

            PhysicalCargoPackage pkg = boxObj.AddComponent<PhysicalCargoPackage>();
            int reward = Random.Range(minReward / 10, (maxReward / 10) + 1) * 10;
            int xp = 70 + (targetPoint.requiredLevel * 20);

            // Determine Cargo Type based on player level unlock rules & weighted random selection
            CargoType chosenType = DetermineRandomCargoType(playerLevel);

            pkg.minDamageSpeedThreshold = fragileMinDamageSpeedThreshold;
            pkg.damageMultiplier = fragileDamageMultiplier;
            pkg.packageCollisionDamageRatio = fragilePackageCollisionRatio;
            pkg.spawnImmunityDuration = fragileSpawnImmunityDuration;
            
            Material chosenMaterial = GetMaterialForCargoType(chosenType);
            pkg.SetupPackage(targetPoint.pointId, targetPoint.addressName, targetPoint.recipientName, reward, wrongPenalty, chosenType, xp, targetPoint.addressDescription, chosenMaterial);
            currentPackages.Add(pkg);
        }

        Debug.Log($"<color=#32FF64>[CargoWarehouseGenerator] Spawned {currentPackages.Count} packages safely at {transform.position} for Player Level {playerLevel}!</color>");
    }

    /// <summary>
    /// Belirtilen kargo türüne uygun karton materyalini seçer. (Tanımlı değilse varsayılan renk paletine düşer)
    /// </summary>
    public Material GetMaterialForCargoType(CargoType type)
    {
        if (type == CargoType.Fragile && fragileMaterials != null && fragileMaterials.Count > 0)
        {
            var valid = fragileMaterials.FindAll(m => m != null);
            if (valid.Count > 0) return valid[Random.Range(0, valid.Count)];
        }
        else if (type == CargoType.Express && expressMaterials != null && expressMaterials.Count > 0)
        {
            var valid = expressMaterials.FindAll(m => m != null);
            if (valid.Count > 0) return valid[Random.Range(0, valid.Count)];
        }

        if (cardboardMaterials != null && cardboardMaterials.Count > 0)
        {
            var valid = cardboardMaterials.FindAll(m => m != null);
            if (valid.Count > 0) return valid[Random.Range(0, valid.Count)];
        }

        return null;
    }

    /// <summary>
    /// Oyuncu seviyesine göre açık olan kargo türlerini ağırlıklı rastgele (weighted random) seçer.
    /// </summary>
    public CargoType DetermineRandomCargoType(int playerLevel)
    {
        List<(CargoType type, float weight)> available = new List<(CargoType, float)>();

        if (playerLevel >= standardRequiredLevel && standardSpawnWeight > 0f)
        {
            available.Add((CargoType.Standard, standardSpawnWeight));
        }

        if (playerLevel >= fragileRequiredLevel && fragileSpawnWeight > 0f)
        {
            available.Add((CargoType.Fragile, fragileSpawnWeight));
        }

        if (playerLevel >= expressRequiredLevel && expressSpawnWeight > 0f)
        {
            available.Add((CargoType.Express, expressSpawnWeight));
        }

        if (available.Count == 0)
        {
            return CargoType.Standard;
        }

        float totalWeight = 0f;
        for (int i = 0; i < available.Count; i++)
        {
            totalWeight += available[i].weight;
        }

        if (totalWeight <= 0f)
        {
            return CargoType.Standard;
        }

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        for (int i = 0; i < available.Count; i++)
        {
            cumulative += available[i].weight;
            if (roll <= cumulative)
            {
                return available[i].type;
            }
        }

        return available[0].type;
    }

    private Vector3 GetSafeSpawnPosition(int index)
    {
        Vector3 boxExtents = new Vector3(0.35f, 0.35f, 0.35f);

        for (int attempt = 0; attempt < 20; attempt++)
        {
            float rx = Random.Range(-spawnAreaSize.x * 0.32f, spawnAreaSize.x * 0.32f);
            float rz = Random.Range(-spawnAreaSize.z * 0.32f, spawnAreaSize.z * 0.32f);
            float ry = (spawnAreaSize.y * 0.5f) + 0.3f + (index * 0.03f);

            Vector3 worldPos = transform.TransformPoint(new Vector3(rx, ry, rz));

            // Check if position overlaps with existing wall/static collider
            Collider[] hits = Physics.OverlapBox(worldPos, boxExtents, transform.rotation, ~0, QueryTriggerInteraction.Ignore);
            bool blocked = false;
            foreach (var h in hits)
            {
                if (h.isTrigger || h.transform == transform) continue;
                // Ignore if it's other cargo package
                if (h.GetComponent<PhysicalCargoPackage>() != null) continue;
                blocked = true;
                break;
            }

            if (!blocked)
            {
                return worldPos;
            }
        }

        // Center fallback
        return transform.TransformPoint(new Vector3(0f, (spawnAreaSize.y * 0.5f) + 0.3f + (index * 0.04f), 0f));
    }

    [ContextMenu("Clear Packages")]
    public void ClearOldPackages()
    {
        for (int i = currentPackages.Count - 1; i >= 0; i--)
        {
            if (currentPackages[i] != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(currentPackages[i].gameObject);
                else Destroy(currentPackages[i].gameObject);
#else
                Destroy(currentPackages[i].gameObject);
#endif
            }
        }
        currentPackages.Clear();
    }

    private void OnDrawGizmos()
    {
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
        Gizmos.DrawCube(new Vector3(0, spawnAreaSize.y * 0.5f, 0), spawnAreaSize);
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
        Gizmos.DrawWireCube(new Vector3(0, spawnAreaSize.y * 0.5f, 0), spawnAreaSize);
        Gizmos.matrix = oldMatrix;
    }
}
