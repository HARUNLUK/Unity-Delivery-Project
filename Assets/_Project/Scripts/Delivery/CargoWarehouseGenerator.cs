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

    [Tooltip("Whether packages automatically spawn at start of day")]
    public bool autoSpawnOnStart = true;

    [Header("--- CARGO TYPE UNLOCK LEVELS & CHANCES ---")]
    [Tooltip("Minimum player level required for Standard cargo")]
    public int standardRequiredLevel = 1;
    [Range(0f, 100f), Tooltip("Standard cargo spawn weight / probability")]
    public float standardSpawnWeight = 50f;

    [Tooltip("Minimum player level required for Fragile cargo")]
    public int fragileRequiredLevel = 5;
    [Range(0f, 100f), Tooltip("Fragile cargo spawn weight / probability")]
    public float fragileSpawnWeight = 30f;

    [Tooltip("Minimum player level required for Express cargo")]
    public int expressRequiredLevel = 8;
    [Range(0f, 100f), Tooltip("Express cargo spawn weight / probability")]
    public float expressSpawnWeight = 20f;

    [Tooltip("Minimum player level required for Explosive cargo")]
    public int explosiveRequiredLevel = 3;
    [Range(0f, 100f), Tooltip("Explosive cargo spawn weight / probability")]
    public float explosiveSpawnWeight = 20f;

    [Header("--- EXPRESS CARGO TIME LIMIT ---")]
    [Tooltip("Minimum target delivery hour for Express cargo (e.g. 10.0 = 10:00)")]
    public float minExpressDeliveryHour = 10.0f;

    [Tooltip("Maximum target delivery hour for Express cargo (e.g. 14.0 = 14:00)")]
    public float maxExpressDeliveryHour = 14.0f;

    [Tooltip("Minute step when picking random express time (e.g. 15 = 10:00, 10:15, 10:30, 10:45...)")]
    [Range(5, 60)]
    public int expressMinuteInterval = 15;

    [Header("--- FRAGILE CARGO TUNING ---")]
    [Tooltip("Minimum impact speed to trigger damage (m/s). Gentle drop < 2.5 m/s, 1.5m drop ~5.0 m/s, high drop > 7.0 m/s.")]
    public float fragileMinDamageSpeedThreshold = 3.5f;

    [Tooltip("Damage multiplier when speed threshold is exceeded.")]
    public float fragileDamageMultiplier = 16.0f;

    [Tooltip("Damage ratio when packages collide with each other (0.20 = 80% less damage).")]
    public float fragilePackageCollisionRatio = 0.20f;

    [Tooltip("Spawn immunity duration (seconds).")]
    public float fragileSpawnImmunityDuration = 3.5f;

    [Header("--- DEFAULT & CUSTOM CARGO PACKAGE PREFABS ---")]
    [Tooltip("Primary default cargo box prefab (Fallback used if specific lists are empty)")]
    public GameObject cargoPackagePrefab;

    [Tooltip("Custom 3D Prefabs for standard packages (drag prefabs from project)")]
    public List<GameObject> packagePrefabs = new List<GameObject>();

    [Tooltip("Custom 3D Prefabs for fragile packages")]
    public List<GameObject> fragilePackagePrefabs = new List<GameObject>();

    [Tooltip("Custom 3D Prefabs for express packages")]
    public List<GameObject> expressPackagePrefabs = new List<GameObject>();

    [Tooltip("Custom 3D Prefabs for explosive packages")]
    public List<GameObject> explosivePackagePrefabs = new List<GameObject>();

    [Header("--- EXPLOSION VFX PREFAB ---")]
    [Tooltip("Custom explosion particle effect prefab (drag prefab from project or leave empty for procedural effect)")]
    public GameObject explosionVfxPrefab;

    [Header("--- PREFAB SCALE MULTIPLIER PER CARGO TYPE ---")]
    [Tooltip("Apply scale multiplier to custom 3D prefabs")]
    public bool enablePrefabScaling = true;

    [Tooltip("Scale X, Y, Z axes independently with random multipliers")]
    public bool randomizeAxesIndependently = false;

    [Header("Standard Cargo Scale")]
    [Tooltip("If true, scales randomly between standardMinScale and standardMaxScale. If false, uses standardFixedScale.")]
    public bool standardRandomScale = true;
    [Range(0.05f, 2.5f), Tooltip("Fixed scale multiplier used when random scaling is disabled")]
    public float standardFixedScale = 0.45f;
    [Range(0.05f, 2.5f), Tooltip("Standard package minimum random scale multiplier")]
    public float standardMinScale = 0.35f;
    [Range(0.05f, 2.5f), Tooltip("Standard package maximum random scale multiplier")]
    public float standardMaxScale = 0.55f;

    [Header("Fragile Cargo Scale")]
    [Tooltip("If true, scales randomly between fragileMinScale and fragileMaxScale. If false, uses fragileFixedScale.")]
    public bool fragileRandomScale = true;
    [Range(0.05f, 2.5f), Tooltip("Fixed scale multiplier used when random scaling is disabled")]
    public float fragileFixedScale = 0.40f;
    [Range(0.05f, 2.5f), Tooltip("Fragile package minimum random scale multiplier")]
    public float fragileMinScale = 0.35f;
    [Range(0.05f, 2.5f), Tooltip("Fragile package maximum random scale multiplier")]
    public float fragileMaxScale = 0.50f;

    [Header("Express Cargo Scale")]
    [Tooltip("If true, scales randomly between expressMinScale and expressMaxScale. If false, uses expressFixedScale.")]
    public bool expressRandomScale = true;
    [Range(0.05f, 2.5f), Tooltip("Fixed scale multiplier used when random scaling is disabled")]
    public float expressFixedScale = 0.35f;
    [Range(0.05f, 2.5f), Tooltip("Express package minimum random scale multiplier")]
    public float expressMinScale = 0.30f;
    [Range(0.05f, 2.5f), Tooltip("Express package maximum random scale multiplier")]
    public float expressMaxScale = 0.45f;

    [Header("Explosive Cargo Scale")]
    [Tooltip("If true, scales randomly between explosiveMinScale and explosiveMaxScale. If false, uses explosiveFixedScale.")]
    public bool explosiveRandomScale = true;
    [Range(0.05f, 2.5f), Tooltip("Fixed scale multiplier used when random scaling is disabled")]
    public float explosiveFixedScale = 0.50f;
    [Range(0.05f, 2.5f), Tooltip("Explosive package minimum random scale multiplier")]
    public float explosiveMinScale = 0.40f;
    [Range(0.05f, 2.5f), Tooltip("Explosive package maximum random scale multiplier")]
    public float explosiveMaxScale = 0.60f;

    // Backward compatibility properties
    public float minPrefabScale { get => standardMinScale; set => standardMinScale = value; }
    public float maxPrefabScale { get => standardMaxScale; set => standardMaxScale = value; }

    [Header("--- CARGO BOX MATERIALS (SKINS) ---")]
    [Tooltip("Cardboard materials for standard packages")]
    public List<Material> cardboardMaterials = new List<Material>();

    [Tooltip("Materials for fragile packages")]
    public List<Material> fragileMaterials = new List<Material>();

    [Tooltip("Materials for express packages")]
    public List<Material> expressMaterials = new List<Material>();

    [Tooltip("Materials for explosive packages")]
    public List<Material> explosiveMaterials = new List<Material>();

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

        int branchLevel = BranchManager.Instance != null ? BranchManager.Instance.CurrentBranchLevel : (PlayerProgressionManager.Instance != null ? PlayerProgressionManager.Instance.WarehouseLevel : 1);
        int toSpawn = BranchManager.Instance != null ? BranchManager.Instance.GetDailyPackageLimit() : (PlayerProgressionManager.Instance != null ? PlayerProgressionManager.Instance.GetDailyPackageLimit() : Mathf.Max(1, packageCount));

        DeliveryPoint[] allPoints = Object.FindObjectsByType<DeliveryPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (allPoints == null || allPoints.Length == 0)
        {
            Debug.LogWarning("[CargoWarehouseGenerator] No DeliveryPoint found in the scene! Place some DeliveryPoint objects first.");
            return;
        }

        // Filter points by branch level
        List<DeliveryPoint> availablePoints = new List<DeliveryPoint>();
        foreach (var p in allPoints)
        {
            if (p.requiredLevel <= branchLevel)
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

            // Determine Cargo Type based on branch level unlock rules & weighted random selection
            CargoType chosenType = DetermineRandomCargoType(branchLevel);

            // Check if user provided custom 3D Prefab model
            GameObject chosenPrefab = GetPrefabForCargoType(chosenType);
            GameObject boxObj;
            bool isCustom = false;

            if (chosenPrefab != null)
            {
                boxObj = Instantiate(chosenPrefab, spawnPos, spawnRot);
                isCustom = true;

                // Scale multiplier applied to original prefab localScale based on cargo type
                if (enablePrefabScaling)
                {
                    var (isRandom, fixedS, minS_raw, maxS_raw) = GetScaleSettingsForCargoType(chosenType);
                    Vector3 origScale = chosenPrefab.transform.localScale;
                    if (origScale == Vector3.zero) origScale = Vector3.one;

                    if (isRandom)
                    {
                        float minS = Mathf.Min(minS_raw, maxS_raw);
                        float maxS = Mathf.Max(minS_raw, maxS_raw);

                        if (minS > 0f && maxS > 0f)
                        {
                            if (randomizeAxesIndependently)
                            {
                                float rx = Random.Range(minS, maxS);
                                float ry = Random.Range(minS, maxS);
                                float rz = Random.Range(minS, maxS);
                                boxObj.transform.localScale = new Vector3(origScale.x * rx, origScale.y * ry, origScale.z * rz);
                            }
                            else
                            {
                                float uniformScale = Random.Range(minS, maxS);
                                boxObj.transform.localScale = origScale * uniformScale;
                            }
                        }
                    }
                    else
                    {
                        float targetScale = fixedS > 0f ? fixedS : 1.0f;
                        boxObj.transform.localScale = origScale * targetScale;
                    }
                }
            }
            else
            {
                // Fallback: Create standard physical cargo cube directly in the scene
                boxObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                boxObj.transform.position = spawnPos;
                boxObj.transform.rotation = spawnRot;
            }

            boxObj.name = $"Cargo_Package_#{targetPoint.pointId}_{i + 1}";
            boxObj.transform.SetParent(null); // Standalone in scene root

            PhysicalCargoPackage pkg = boxObj.GetComponent<PhysicalCargoPackage>();
            if (pkg == null) pkg = boxObj.AddComponent<PhysicalCargoPackage>();

            int reward = Random.Range(minReward / 10, (maxReward / 10) + 1) * 10;
            int xp = 70 + (targetPoint.requiredLevel * 20);

            pkg.minDamageSpeedThreshold = fragileMinDamageSpeedThreshold;
            pkg.damageMultiplier = fragileDamageMultiplier;
            pkg.packageCollisionDamageRatio = fragilePackageCollisionRatio;
            pkg.spawnImmunityDuration = fragileSpawnImmunityDuration;
            pkg.explosionVfxPrefab = explosionVfxPrefab;
            
            Material chosenMaterial = GetMaterialForCargoType(chosenType);

            float expressHour = 13.0f;
            if (chosenType == CargoType.Express)
            {
                expressHour = GenerateRandomExpressDeliveryHour();
            }

            pkg.SetupPackage(targetPoint.pointId, targetPoint.EffectiveAddressName, targetPoint.EffectiveRecipient, reward, wrongPenalty, chosenType, xp, targetPoint.EffectiveDescription, chosenMaterial, isCustom, expressHour);
            currentPackages.Add(pkg);
        }

        Debug.Log($"<color=#32FF64>[CargoWarehouseGenerator] Spawned {currentPackages.Count} packages safely at {transform.position} for Branch Level {branchLevel}!</color>");
    }

    /// <summary>
    /// Returns scale settings (isRandom, fixedScale, minScale, maxScale) for the specified cargo type.
    /// </summary>
    public (bool isRandom, float fixedScale, float minScale, float maxScale) GetScaleSettingsForCargoType(CargoType type)
    {
        switch (type)
        {
            case CargoType.Fragile:
                return (fragileRandomScale, fragileFixedScale, fragileMinScale, fragileMaxScale);
            case CargoType.Express:
                return (expressRandomScale, expressFixedScale, expressMinScale, expressMaxScale);
            case CargoType.Explosive:
                return (explosiveRandomScale, explosiveFixedScale, explosiveMinScale, explosiveMaxScale);
            case CargoType.Standard:
            default:
                return (standardRandomScale, standardFixedScale, standardMinScale, standardMaxScale);
        }
    }

    /// <summary>
    /// Returns the scale multiplier min/max range for the specified cargo type. (Legacy helper)
    /// </summary>
    public (float minScale, float maxScale) GetScaleRangeForCargoType(CargoType type)
    {
        var settings = GetScaleSettingsForCargoType(type);
        return (settings.minScale, settings.maxScale);
    }

    /// <summary>
    /// Generates a random delivery cutoff hour for express shipments within min and max interval (steps of 15 or 30 mins).
    /// </summary>
    public float GenerateRandomExpressDeliveryHour()
    {
        int minTotalMins = Mathf.RoundToInt(Mathf.Min(minExpressDeliveryHour, maxExpressDeliveryHour) * 60f);
        int maxTotalMins = Mathf.RoundToInt(Mathf.Max(minExpressDeliveryHour, maxExpressDeliveryHour) * 60f);
        int step = Mathf.Max(5, expressMinuteInterval);

        int stepsCount = Mathf.Max(1, (maxTotalMins - minTotalMins) / step);
        int chosenStep = Random.Range(0, stepsCount + 1);
        int chosenTotalMins = minTotalMins + (chosenStep * step);
        chosenTotalMins = Mathf.Clamp(chosenTotalMins, minTotalMins, maxTotalMins);

        return chosenTotalMins / 60f;
    }

    /// <summary>
    /// Selects the 3D prefab model suitable for the given cargo type. (Falls back to cube if null)
    /// </summary>
    public GameObject GetPrefabForCargoType(CargoType type)
    {
        if (type == CargoType.Fragile && fragilePackagePrefabs != null && fragilePackagePrefabs.Count > 0)
        {
            var valid = fragilePackagePrefabs.FindAll(p => p != null);
            if (valid.Count > 0) return valid[Random.Range(0, valid.Count)];
        }
        else if (type == CargoType.Express && expressPackagePrefabs != null && expressPackagePrefabs.Count > 0)
        {
            var valid = expressPackagePrefabs.FindAll(p => p != null);
            if (valid.Count > 0) return valid[Random.Range(0, valid.Count)];
        }
        else if (type == CargoType.Explosive && explosivePackagePrefabs != null && explosivePackagePrefabs.Count > 0)
        {
            var valid = explosivePackagePrefabs.FindAll(p => p != null);
            if (valid.Count > 0) return valid[Random.Range(0, valid.Count)];
        }

        if (packagePrefabs != null && packagePrefabs.Count > 0)
        {
            var valid = packagePrefabs.FindAll(p => p != null);
            if (valid.Count > 0) return valid[Random.Range(0, valid.Count)];
        }

        if (cargoPackagePrefab != null)
        {
            return cargoPackagePrefab;
        }

        return null;
    }

    /// <summary>
    /// Selects the cardboard material suitable for the specified cargo type.
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
        else if (type == CargoType.Explosive && explosiveMaterials != null && explosiveMaterials.Count > 0)
        {
            var valid = explosiveMaterials.FindAll(m => m != null);
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
    /// Selects an unlocked cargo type using weighted random probability based on branch level.
    /// </summary>
    public CargoType DetermineRandomCargoType(int branchLevel)
    {
        List<(CargoType type, float weight)> available = new List<(CargoType, float)>();

        if (branchLevel >= standardRequiredLevel && standardSpawnWeight > 0f)
        {
            available.Add((CargoType.Standard, standardSpawnWeight));
        }

        if (branchLevel >= fragileRequiredLevel && fragileSpawnWeight > 0f)
        {
            available.Add((CargoType.Fragile, fragileSpawnWeight));
        }

        if (branchLevel >= expressRequiredLevel && expressSpawnWeight > 0f)
        {
            available.Add((CargoType.Express, expressSpawnWeight));
        }

        if (branchLevel >= explosiveRequiredLevel && explosiveSpawnWeight > 0f)
        {
            available.Add((CargoType.Explosive, explosiveSpawnWeight));
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
