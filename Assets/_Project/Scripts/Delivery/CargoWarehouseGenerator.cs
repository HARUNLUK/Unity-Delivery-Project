using System.Collections.Generic;
using UnityEngine;

public class CargoWarehouseGenerator : MonoBehaviour
{
    [Header("--- PLATFORM & GENERATION SETTINGS ---")]
    [Tooltip("Size of the platform spawn area (Width, Height, Length)")]
    public Vector3 spawnAreaSize = new Vector3(4.5f, 0.4f, 4.5f);

    [Tooltip("Default fallback package count if progression manager or branch manager is missing")]
    public int packageCount = 5;

    [Tooltip("Reward range for standard packages")]
    public int minReward = 80;
    public int maxReward = 180;
    public int wrongPenalty = 40;

    [Tooltip("Whether packages automatically spawn at start of day")]
    public bool autoSpawnOnStart = true;

    [Header("--- CURRENT ACTIVE PACKAGES ---")]
    public List<PhysicalCargoPackage> currentPackages = new List<PhysicalCargoPackage>();

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

        BranchManager bm = BranchManager.Instance;

        for (int i = 0; i < toSpawn; i++)
        {
            // Pick a destination point
            DeliveryPoint targetPoint = availablePoints[Random.Range(0, availablePoints.Count)];

            // Calculate safe collision-checked spawn position
            Vector3 spawnPos = GetSafeSpawnPosition(i);
            Quaternion spawnRot = transform.rotation * Quaternion.Euler(0f, Random.Range(-25f, 25f), 0f);

            // Determine Cargo Type via BranchManager
            CargoType chosenType = bm != null ? bm.DetermineRandomCargoType(branchLevel) : CargoType.Standard;

            // Check if 3D Prefab model is provided
            GameObject chosenPrefab = bm != null ? bm.GetPrefabForCargoType(chosenType) : null;
            GameObject boxObj;
            bool isCustom = false;

            if (chosenPrefab != null)
            {
                boxObj = Instantiate(chosenPrefab, spawnPos, spawnRot);
                isCustom = true;

                // Scale multiplier applied to original prefab localScale based on cargo type
                if (bm != null && bm.enablePrefabScaling)
                {
                    var (isRandom, fixedS, minS_raw, maxS_raw) = bm.GetScaleSettingsForCargoType(chosenType);
                    Vector3 origScale = chosenPrefab.transform.localScale;
                    if (origScale == Vector3.zero) origScale = Vector3.one;

                    if (isRandom)
                    {
                        float minS = Mathf.Min(minS_raw, maxS_raw);
                        float maxS = Mathf.Max(minS_raw, maxS_raw);

                        if (minS > 0f && maxS > 0f)
                        {
                            if (bm.randomizeAxesIndependently)
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

            if (bm != null)
            {
                pkg.minDamageSpeedThreshold = bm.fragileMinDamageSpeedThreshold;
                pkg.damageMultiplier = bm.fragileDamageMultiplier;
                pkg.packageCollisionDamageRatio = bm.fragilePackageCollisionRatio;
                pkg.spawnImmunityDuration = bm.fragileSpawnImmunityDuration;
                pkg.explosionVfxPrefab = bm.explosionVfxPrefab;
            }

            Material chosenMaterial = bm != null ? bm.GetMaterialForCargoType(chosenType) : null;

            float expressHour = 13.0f;
            if (chosenType == CargoType.Express)
            {
                expressHour = bm != null ? bm.GenerateRandomExpressDeliveryHour() : 13.0f;
            }

            pkg.SetupPackage(targetPoint.pointId, targetPoint.EffectiveAddressName, targetPoint.EffectiveRecipient, reward, wrongPenalty, chosenType, xp, targetPoint.EffectiveDescription, chosenMaterial, isCustom, expressHour);
            currentPackages.Add(pkg);
        }

        Debug.Log($"<color=#32FF64>[CargoWarehouseGenerator] Spawned {currentPackages.Count} packages safely at {transform.position} for Branch Level {branchLevel}!</color>");
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
