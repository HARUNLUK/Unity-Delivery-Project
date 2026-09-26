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

        SpawnExtraPackages(toSpawn, 0, branchLevel);

        Debug.Log($"<color=#32FF64>[CargoWarehouseGenerator] Spawned {currentPackages.Count} packages safely at {transform.position} for Branch Level {branchLevel}!</color>");
    }

    /// <summary>
    /// Spawns extra packages on the warehouse platform starting from a given slot index.
    /// Used for initial generation as well as filling remaining capacity on branch upgrades.
    /// </summary>
    public List<PhysicalCargoPackage> SpawnExtraPackages(int count, int startSlotIndex, int branchLevel)
    {
        List<PhysicalCargoPackage> newlySpawned = new List<PhysicalCargoPackage>();
        if (count <= 0) return newlySpawned;

        List<DeliveryPoint> availablePoints = GetAvailableDeliveryPoints(branchLevel);
        if (availablePoints.Count == 0)
        {
            Debug.LogWarning("[CargoWarehouseGenerator] No DeliveryPoint found in the scene! Cannot spawn packages.");
            return newlySpawned;
        }

        for (int i = 0; i < count; i++)
        {
            int slotIdx = startSlotIndex + i;
            PhysicalCargoPackage pkg = SpawnSinglePackage(branchLevel, slotIdx, availablePoints);
            if (pkg != null)
            {
                newlySpawned.Add(pkg);
                if (!currentPackages.Contains(pkg))
                {
                    currentPackages.Add(pkg);
                }
            }
        }

        return newlySpawned;
    }

    /// <summary>
    /// Spawns a single cargo package tailored to the given branch level and positions it safely at the designated slot index.
    /// </summary>
    public PhysicalCargoPackage SpawnSinglePackage(int branchLevel, int slotIndex, List<DeliveryPoint> pointsPool = null)
    {
        if (pointsPool == null || pointsPool.Count == 0)
        {
            pointsPool = GetAvailableDeliveryPoints(branchLevel);
        }

        if (pointsPool.Count == 0)
        {
            Debug.LogWarning("[CargoWarehouseGenerator] No DeliveryPoint found in the scene for package spawn!");
            return null;
        }

        DeliveryPoint targetPoint = pointsPool[Random.Range(0, pointsPool.Count)];
        Vector3 spawnPos = GetSafeSpawnPosition(slotIndex);
        Quaternion spawnRot = transform.rotation * Quaternion.Euler(0f, Random.Range(-25f, 25f), 0f);

        BranchManager bm = BranchManager.Instance;
        CargoType chosenType = bm != null ? bm.DetermineRandomCargoType(branchLevel) : CargoType.Standard;
        GameObject chosenPrefab = bm != null ? bm.GetPrefabForCargoType(chosenType) : null;
        GameObject boxObj;
        bool isCustom = false;

        if (chosenPrefab != null)
        {
            boxObj = Instantiate(chosenPrefab, spawnPos, spawnRot);
            isCustom = true;

            if (bm != null)
            {
                boxObj.transform.localScale = bm.CalculateCargoScale(chosenType, chosenPrefab.transform.localScale);
            }
        }
        else
        {
            boxObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boxObj.transform.position = spawnPos;
            boxObj.transform.rotation = spawnRot;

            if (bm != null)
            {
                boxObj.transform.localScale = bm.CalculateCargoScale(chosenType, new Vector3(0.55f, 0.42f, 0.45f));
                isCustom = true;
            }
        }

        boxObj.name = $"Cargo_Package_#{targetPoint.pointId}_{slotIndex + 1}";
        boxObj.transform.SetParent(null); // Standalone in scene root

        PhysicalCargoPackage pkg = boxObj.GetComponent<PhysicalCargoPackage>();
        if (pkg == null) pkg = boxObj.AddComponent<PhysicalCargoPackage>();

        int reward;
        int penalty;
        if (bm != null)
        {
            bm.GetRewardAndPenalty(chosenType, out reward, out penalty);
        }
        else
        {
            reward = Random.Range(minReward / 10, (maxReward / 10) + 1) * 10;
            penalty = wrongPenalty;
        }

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

        pkg.SetupPackage(targetPoint.pointId, targetPoint.EffectiveAddressName, targetPoint.EffectiveRecipient, reward, penalty, chosenType, xp, targetPoint.EffectiveDescription, chosenMaterial, isCustom, expressHour);
        return pkg;
    }

    /// <summary>
    /// Relocates uncollected/unloaded cargo packages from an old branch generator platform to this upgraded platform.
    /// </summary>
    public void MigrateUncollectedPackages(List<PhysicalCargoPackage> packagesToMove, int newBranchLevel)
    {
        if (packagesToMove == null || packagesToMove.Count == 0) return;

        currentPackages.RemoveAll(p => p == null);

        int slotIdx = 0;
        foreach (var pkg in packagesToMove)
        {
            if (pkg == null) continue;

            Vector3 safePos = GetSafeSpawnPosition(slotIdx);
            Quaternion safeRot = transform.rotation * Quaternion.Euler(0f, Random.Range(-25f, 25f), 0f);

            pkg.RelocateToPosition(safePos, safeRot);

            if (!currentPackages.Contains(pkg))
            {
                currentPackages.Add(pkg);
            }

            slotIdx++;
        }

        Debug.Log($"<color=#32FF64>[CargoWarehouseGenerator] Successfully migrated {packagesToMove.Count} packages to upgraded platform at {transform.position}!</color>");
    }

    public List<DeliveryPoint> GetAvailableDeliveryPoints(int branchLevel)
    {
        DeliveryPoint[] allPoints = Object.FindObjectsByType<DeliveryPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        List<DeliveryPoint> availablePoints = new List<DeliveryPoint>();

        if (allPoints == null || allPoints.Length == 0)
        {
            return availablePoints;
        }

        foreach (var p in allPoints)
        {
            if (p.requiredLevel <= branchLevel)
            {
                availablePoints.Add(p);
            }
        }

        if (availablePoints.Count == 0)
        {
            availablePoints.AddRange(allPoints);
        }

        return availablePoints;
    }

    public Vector3 GetSafeSpawnPosition(int index)
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
