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

            // Random position inside the local spawn area
            float rx = Random.Range(-spawnAreaSize.x * 0.42f, spawnAreaSize.x * 0.42f);
            float rz = Random.Range(-spawnAreaSize.z * 0.42f, spawnAreaSize.z * 0.42f);
            float ry = (spawnAreaSize.y * 0.5f) + 0.25f + (i * 0.02f);
            Vector3 localOffset = new Vector3(rx, ry, rz);
            Vector3 spawnPos = transform.TransformPoint(localOffset);
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

            // Determine Cargo Type based on player level
            CargoType chosenType = CargoType.Standard;
            if (playerLevel >= 3)
            {
                float roll = Random.value;
                if (roll < 0.25f) chosenType = CargoType.Express;
                else if (roll < 0.55f) chosenType = CargoType.Fragile;
            }
            else if (playerLevel >= 2)
            {
                if (Random.value < 0.35f) chosenType = CargoType.Fragile;
            }

            pkg.SetupPackage(targetPoint.pointId, targetPoint.addressName, targetPoint.recipientName, reward, wrongPenalty, chosenType, xp);
            currentPackages.Add(pkg);
        }

        Debug.Log($"<color=#32FF64>[CargoWarehouseGenerator] Spawned {currentPackages.Count} packages at {transform.position} for Player Level {playerLevel}!</color>");
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
