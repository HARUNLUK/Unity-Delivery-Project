#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class MapSetupTool : MonoBehaviour
{
    [MenuItem("Tools/Delivery Game/Setup Map Architecture & Systems", false, 1)]
    public static void SetupSceneArchitecture()
    {
        // 1. Root Containers
        string[] rootFolders = new string[] {
            "_MANAGERS",
            "_UI",
            "_GAMEPLAY",
            "_ROADS",
            "_ENVIRONMENT",
            "_DISTRICTS"
        };

        foreach (string folder in rootFolders)
        {
            if (GameObject.Find(folder) == null)
            {
                GameObject obj = new GameObject(folder);
                obj.transform.position = Vector3.zero;
                Undo.RegisterCreatedObjectUndo(obj, $"Created {folder}");
            }
        }

        // 2. Districts Sub-Folders
        GameObject districts = GameObject.Find("_DISTRICTS");
        if (districts != null)
        {
            string[] subDistricts = new string[] {
                "Zone1_Downtown",
                "Zone2_Suburbs",
                "Zone3_Highland_Village"
            };

            foreach (string sub in subDistricts)
            {
                if (districts.transform.Find(sub) == null)
                {
                    GameObject subObj = new GameObject(sub);
                    subObj.transform.SetParent(districts.transform);
                    subObj.transform.position = Vector3.zero;
                    Undo.RegisterCreatedObjectUndo(subObj, $"Created {sub}");
                }
            }
        }

        // 3. Delivery Points Pool
        GameObject gameplay = GameObject.Find("_GAMEPLAY");
        if (gameplay != null && gameplay.transform.Find("Delivery_Points_Pool") == null)
        {
            GameObject pointsPool = new GameObject("Delivery_Points_Pool");
            pointsPool.transform.SetParent(gameplay.transform);
            pointsPool.transform.position = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(pointsPool, "Created Delivery_Points_Pool");
        }

        // 4. Managers
        GameObject managers = GameObject.Find("_MANAGERS");
        if (managers != null)
        {
            if (Object.FindAnyObjectByType<DayTimeManager>() == null)
            {
                GameObject dtmObj = new GameObject("DayTimeManager");
                dtmObj.transform.SetParent(managers.transform);
                dtmObj.AddComponent<DayTimeManager>();
                Undo.RegisterCreatedObjectUndo(dtmObj, "Created DayTimeManager");
            }

            if (Object.FindAnyObjectByType<PlayerEconomyManager>() == null)
            {
                GameObject pemObj = new GameObject("PlayerEconomyManager");
                pemObj.transform.SetParent(managers.transform);
                pemObj.AddComponent<PlayerEconomyManager>();
                Undo.RegisterCreatedObjectUndo(pemObj, "Created PlayerEconomyManager");
            }

            if (Object.FindAnyObjectByType<DaySummaryManager>() == null)
            {
                GameObject dsmObj = new GameObject("DaySummaryManager");
                dsmObj.transform.SetParent(managers.transform);
                dsmObj.AddComponent<DaySummaryManager>();
                Undo.RegisterCreatedObjectUndo(dsmObj, "Created DaySummaryManager");
            }
        }

        Debug.Log("[MapSetupTool] Scene architecture & delivery systems successfully set up!");
    }

    [MenuItem("Tools/Delivery Game/Add Delivery Point at Selected Object", false, 2)]
    public static void AddDeliveryPointAtSelected()
    {
        GameObject[] selectedObjects = Selection.gameObjects;

        GameObject pointsPool = GameObject.Find("Delivery_Points_Pool");
        if (pointsPool == null)
        {
            GameObject gameplayRoot = GameObject.Find("_GAMEPLAY");
            if (gameplayRoot == null) gameplayRoot = new GameObject("_GAMEPLAY");
            pointsPool = new GameObject("Delivery_Points_Pool");
            pointsPool.transform.SetParent(gameplayRoot.transform);
            Undo.RegisterCreatedObjectUndo(pointsPool, "Created Delivery_Points_Pool");
        }

        DeliveryPoint[] allExisting = Object.FindObjectsByType<DeliveryPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int highestIndex = 0;
        foreach (var dp in allExisting)
        {
            if (int.TryParse(dp.pointId, out int idVal))
            {
                if (idVal > highestIndex) highestIndex = idVal;
            }
        }
        if (highestIndex == 0) highestIndex = pointsPool.transform.childCount;

        Terrain activeTerrain = Terrain.activeTerrain;
        List<GameObject> createdPoints = new List<GameObject>();

        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            highestIndex++;
            Vector3 spawnPos = Vector3.zero;
            if (SceneView.lastActiveSceneView != null)
            {
                spawnPos = SceneView.lastActiveSceneView.pivot;
            }

            GameObject ptObj = CreateSingleDeliveryPoint(pointsPool.transform, spawnPos, highestIndex, activeTerrain, "Generic Point");
            createdPoints.Add(ptObj);
        }
        else
        {
            List<GameObject> targetBuildings = new List<GameObject>();
            foreach (GameObject go in selectedObjects)
            {
                if (go.GetComponent<DeliveryPoint>() != null || go.name.Contains("VisualMarker") || go.name.Contains("DeliveryPoint_"))
                    continue;

                if (go.transform.childCount > 0 && go.GetComponent<Renderer>() == null && go.GetComponent<MeshFilter>() == null)
                {
                    for (int c = 0; c < go.transform.childCount; c++)
                    {
                        GameObject child = go.transform.GetChild(c).gameObject;
                        if (!targetBuildings.Contains(child)) targetBuildings.Add(child);
                    }
                }
                else
                {
                    if (!targetBuildings.Contains(go)) targetBuildings.Add(go);
                }
            }

            if (targetBuildings.Count == 0)
            {
                targetBuildings.AddRange(selectedObjects);
            }

            foreach (GameObject building in targetBuildings)
            {
                highestIndex++;
                Transform t = building.transform;
                
                Renderer ren = building.GetComponentInChildren<Renderer>();
                Vector3 center = ren != null ? ren.bounds.center : t.position;
                Vector3 forwardDir = t.forward;

                float offsetDist = ren != null ? Mathf.Max(3f, ren.bounds.extents.z + 1.5f) : 3.5f;
                Vector3 spawnPos = center + (forwardDir * offsetDist);

                GameObject ptObj = CreateSingleDeliveryPoint(pointsPool.transform, spawnPos, highestIndex, activeTerrain, building.name);
                createdPoints.Add(ptObj);
            }
        }

        Selection.objects = createdPoints.ToArray();
        Debug.Log($"[MapSetupTool] Successfully created {createdPoints.Count} Delivery Points from Prefab! (IDs: {highestIndex - createdPoints.Count + 1} to {highestIndex})");
    }

    /// <summary>
    /// Sahnede önceden oluşturulmuş tüm DeliveryPoint objelerini verilerini (ID, adres, konum) koruyarak özel prefab ile değiştirir.
    /// </summary>
    [MenuItem("Tools/Delivery Game/Replace Existing Delivery Points With Custom Prefab", false, 3)]
    public static void ReplaceAllDeliveryPointsWithPrefab()
    {
        GameObject prefabAsset = FindDeliveryPointPrefab();
        if (prefabAsset == null)
        {
            EditorUtility.DisplayDialog("Error", "DeliveryPoint prefab could not be found in Assets/Prefabs/!", "OK");
            return;
        }

        DeliveryPoint[] allExisting = Object.FindObjectsByType<DeliveryPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (allExisting == null || allExisting.Length == 0)
        {
            Debug.Log("[MapSetupTool] Sahnede değiştirilecek herhangi bir DeliveryPoint bulunamadı.");
            return;
        }

        int replacedCount = 0;
        List<GameObject> newObjects = new List<GameObject>();

        foreach (DeliveryPoint oldDp in allExisting)
        {
            if (oldDp == null) continue;

            // Zaten hedef prefab ise atla
            GameObject prefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(oldDp.gameObject);
            if (prefabRoot != null && PrefabUtility.GetCorrespondingObjectFromSource(prefabRoot) == prefabAsset)
            {
                continue;
            }

            // Mevcut verileri yedekle
            string pointId = oldDp.pointId;
            string addressName = oldDp.addressName;
            string addressDesc = oldDp.addressDescription;
            Vector3 worldPos = oldDp.transform.position;
            Quaternion worldRot = oldDp.transform.rotation;
            Transform parent = oldDp.transform.parent;
            string objName = oldDp.gameObject.name;
            int siblingIndex = oldDp.transform.GetSiblingIndex();

            // Yeni prefabı aynı hiyerarşik konuma yerleştir
            GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, parent);
            newObj.name = objName;
            newObj.transform.position = worldPos;
            newObj.transform.rotation = worldRot;
            newObj.transform.SetSiblingIndex(siblingIndex);

            // Verileri yeni objeye aktar
            DeliveryPoint newDp = newObj.GetComponent<DeliveryPoint>();
            if (newDp != null)
            {
                newDp.pointId = pointId;
                newDp.addressName = addressName;
                newDp.addressDescription = addressDesc;
            }

            Undo.RegisterCreatedObjectUndo(newObj, "Replaced Delivery Point with Prefab");
            Undo.DestroyObjectImmediate(oldDp.gameObject);

            newObjects.Add(newObj);
            replacedCount++;
        }

        if (newObjects.Count > 0)
        {
            Selection.objects = newObjects.ToArray();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        Debug.Log($"[MapSetupTool] Sahnede bulunan {replacedCount} adet teslimat noktası verileri (ID, Adres, İpucu, Konum) korunarak '{prefabAsset.name}' prefabı ile başarıyla güncellendi!");
    }

    private static GameObject FindDeliveryPointPrefab()
    {
        string[] knownPaths = new string[] {
            "Assets/Prefabs/DeliveryPoint_01 .prefab",
            "Assets/Prefabs/DeliveryPoint_01.prefab",
            "Assets/Prefabs/DeliveryPoint.prefab",
            "Assets/Prefabs/Delivery/DeliveryPoint.prefab",
            "Assets/Prefabs/DeliveryPoint_01"
        };

        foreach (string p in knownPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (prefab != null) return prefab;
        }

        string[] guids = AssetDatabase.FindAssets("DeliveryPoint t:Prefab");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null && prefab.GetComponent<DeliveryPoint>() != null)
            {
                return prefab;
            }
        }

        return null;
    }

    private static GameObject CreateSingleDeliveryPoint(Transform parent, Vector3 worldPos, int index, Terrain terrain, string buildingName)
    {
        if (terrain != null)
        {
            worldPos.y = terrain.SampleHeight(worldPos) + terrain.transform.position.y + 0.05f;
        }

        string idStr = index.ToString();
        GameObject pointObj = null;
        GameObject prefabAsset = FindDeliveryPointPrefab();

        if (prefabAsset != null)
        {
            pointObj = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, parent);
            pointObj.name = $"DeliveryPoint_{idStr}";
            pointObj.transform.position = worldPos;
        }
        else
        {
            pointObj = new GameObject($"DeliveryPoint_{idStr}");
            pointObj.transform.SetParent(parent);
            pointObj.transform.position = worldPos;

            SphereCollider col = pointObj.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 4.0f;

            pointObj.AddComponent<DeliveryPoint>();
        }

        DeliveryPoint dp = pointObj.GetComponent<DeliveryPoint>();
        if (dp != null)
        {
            dp.pointId = idStr;
            dp.addressName = string.IsNullOrEmpty(buildingName) ? $"Street Address #{idStr}" : $"{buildingName} Address #{idStr}";
            dp.addressDescription = "House / shop description clue for the player.";
        }

        Undo.RegisterCreatedObjectUndo(pointObj, $"Created Delivery Point {idStr}");
        return pointObj;
    }
}
#endif
