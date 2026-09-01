#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
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
        // Get all selected GameObjects directly
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

        // Find existing highest index in scene
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

        // If no objects selected, place 1 at Scene View center
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
            // Expand selection: if user selected a parent group containing multiple buildings, process them
            List<GameObject> targetBuildings = new List<GameObject>();
            foreach (GameObject go in selectedObjects)
            {
                // Skip already created DeliveryPoints or VisualMarkers
                if (go.GetComponent<DeliveryPoint>() != null || go.name.Contains("VisualMarker") || go.name.Contains("DeliveryPoint_"))
                    continue;

                // If selected object is a container with child buildings (and no mesh itself)
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

            // Create 1 delivery point for each building
            foreach (GameObject building in targetBuildings)
            {
                highestIndex++;
                Transform t = building.transform;
                
                // Calculate position in front of building based on bounds or forward vector
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
        Debug.Log($"[MapSetupTool] Successfully created {createdPoints.Count} Delivery Points! (IDs: {highestIndex - createdPoints.Count + 1} to {highestIndex})");
    }

    private static GameObject CreateSingleDeliveryPoint(Transform parent, Vector3 worldPos, int index, Terrain terrain, string buildingName)
    {
        if (terrain != null)
        {
            worldPos.y = terrain.SampleHeight(worldPos) + terrain.transform.position.y + 0.05f;
        }

        string idStr = index.ToString();
        GameObject pointObj = new GameObject($"DeliveryPoint_{idStr}");
        pointObj.transform.SetParent(parent);
        pointObj.transform.position = worldPos;

        SphereCollider col = pointObj.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 4.0f;

        DeliveryPoint dp = pointObj.AddComponent<DeliveryPoint>();
        dp.pointId = idStr;
        dp.addressName = string.IsNullOrEmpty(buildingName) ? $"Street Address #{idStr}" : $"{buildingName} Address #{idStr}";
        dp.addressDescription = "House / shop description clue for the player.";

        // Visual Marker Cylinder Ring
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = "VisualMarker";
        marker.transform.SetParent(pointObj.transform);
        marker.transform.localPosition = Vector3.zero;
        marker.transform.localScale = new Vector3(4.5f, 0.04f, 4.5f);
        Object.DestroyImmediate(marker.GetComponent<Collider>());

        Renderer ren = marker.GetComponent<Renderer>();
        if (ren != null)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Standard");
            Material m = new Material(s);
            m.color = new Color(0.2f, 0.9f, 0.4f, 0.65f);
            ren.material = m;
        }

        dp.visualMarker = marker;

        Undo.RegisterCreatedObjectUndo(pointObj, $"Created Delivery Point {idStr}");
        return pointObj;
    }
}
#endif
