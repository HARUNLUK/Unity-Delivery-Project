using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class CargoWarehouseGenerator : MonoBehaviour
{
    [Header("--- GENERATOR SETTINGS ---")]
    [Tooltip("How many packages to generate in this batch")]
    public int packageCount = 6;

    [Tooltip("Physical bounds of the dispatch spawn table/pallet area")]
    public Vector3 spawnAreaSize = new Vector3(3.5f, 0.4f, 2.5f);

    [Tooltip("Automatically spawn packages when the game starts")]
    public bool autoSpawnOnStart = true;

    [Header("--- REWARDS & PENALTIES ---")]
    public int minReward = 80;
    public int maxReward = 160;
    public int wrongPenalty = 40;

    [Header("--- SPAWNED PACKAGES LIST ---")]
    public List<PhysicalCargoPackage> currentPackages = new List<PhysicalCargoPackage>();

    private void Start()
    {
        if (autoSpawnOnStart)
        {
            SpawnCargoBatch();
        }
    }

    [ContextMenu("Generate Cargo Batch")]
    public void SpawnCargoBatch()
    {
        ClearOldPackages();

        DeliveryPoint[] allPoints = Object.FindObjectsByType<DeliveryPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (allPoints == null || allPoints.Length == 0)
        {
            Debug.LogWarning("[CargoWarehouseGenerator] No DeliveryPoint found in the scene! Place some DeliveryPoint objects first.");
            return;
        }

        int toSpawn = Mathf.Max(1, packageCount);

        for (int i = 0; i < toSpawn; i++)
        {
            // Pick a destination point
            DeliveryPoint targetPoint = allPoints[Random.Range(0, allPoints.Length)];

            // Random position inside the spawn area
            float rx = Random.Range(-spawnAreaSize.x * 0.45f, spawnAreaSize.x * 0.45f);
            float rz = Random.Range(-spawnAreaSize.z * 0.45f, spawnAreaSize.z * 0.45f);
            Vector3 spawnPos = transform.position + new Vector3(rx, spawnAreaSize.y + 0.3f + (i * 0.05f), rz);
            Quaternion spawnRot = Quaternion.Euler(0f, Random.Range(-35f, 35f), 0f);

            // Create standard physical cargo cube
            GameObject boxObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boxObj.name = $"Cargo_Package_#{targetPoint.pointId}_{i + 1}";
            boxObj.transform.position = spawnPos;
            boxObj.transform.rotation = spawnRot;
            boxObj.transform.SetParent(transform);

            PhysicalCargoPackage pkg = boxObj.AddComponent<PhysicalCargoPackage>();
            int reward = Random.Range(minReward / 10, (maxReward / 10) + 1) * 10;

            pkg.SetupPackage(targetPoint.pointId, targetPoint.addressName, targetPoint.recipientName, reward, wrongPenalty);
            currentPackages.Add(pkg);
        }

        Debug.Log($"[CargoWarehouseGenerator] Successfully spawned {currentPackages.Count} physical packages ready for pickup!");
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
        Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.4f);
        Gizmos.DrawCube(transform.position + Vector3.up * (spawnAreaSize.y * 0.5f), spawnAreaSize);

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
        Gizmos.DrawWireCube(transform.position + Vector3.up * (spawnAreaSize.y * 0.5f), spawnAreaSize);
    }
}
