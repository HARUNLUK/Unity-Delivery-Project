#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SplineTrafficManager))]
public class SplineTrafficManagerEditor : Editor
{
    [MenuItem("Tools/Delivery Game/Setup Traffic Manager in Scene", false, 70)]
    public static void SetupTrafficManagerInScene()
    {
        SplineTrafficManager existing = Object.FindAnyObjectByType<SplineTrafficManager>();
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            Debug.Log("[SplineTrafficManager] Selected existing Traffic Manager in scene.");
            return;
        }

        GameObject managerObj = new GameObject("[TRAFFIC_SYSTEM]");
        managerObj.transform.position = Vector3.zero;
        SplineTrafficManager manager = managerObj.AddComponent<SplineTrafficManager>();

        // Auto populate vehicle prefabs
        PopulateDefaultVehiclePrefabs(manager);
        manager.BuildTrafficPaths();

        Selection.activeGameObject = managerObj;
        Undo.RegisterCreatedObjectUndo(managerObj, "Create Traffic Manager");
        Debug.Log("[SplineTrafficManager] Created new Traffic Manager with default vehicle prefabs successfully!");
    }

    public override void OnInspectorGUI()
    {
        SplineTrafficManager manager = (SplineTrafficManager)target;

        EditorGUILayout.HelpBox(
            "DYNAMIC AI TRAFFIC SYSTEM:\n" +
            "• Automatically spawns two-way traffic vehicles along Spline Roads.\n" +
            "• Proximity-based spawning & despawning around the player (Zero FPS loss).\n" +
            "• Vehicles feature front radar collision avoidance & terrain conforming.",
            MessageType.Info);

        EditorGUILayout.Space(8);

        // PREFAB QUICK ACTIONS
        EditorGUILayout.LabelField("VEHICLE PREFAB PRESETS", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = new Color(0.2f, 0.85f, 0.5f);
        if (GUILayout.Button("🚗 Auto-Populate City Vehicle Prefabs\n(SimplePoly City Cars, Taxis, Trucks, SUVs)", GUILayout.Height(38)))
        {
            Undo.RecordObject(manager, "Populate Vehicle Prefabs");
            PopulateDefaultVehiclePrefabs(manager);
            EditorUtility.SetDirty(manager);
        }

        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
        if (GUILayout.Button("Clear Prefab List", GUILayout.Height(38), GUILayout.Width(110)))
        {
            Undo.RecordObject(manager, "Clear Prefabs");
            manager.vehiclePrefabs.Clear();
            EditorUtility.SetDirty(manager);
        }

        EditorGUILayout.EndHorizontal();
        GUI.backgroundColor = Color.white;

        // DISTANCE & DENSITY PRESETS
        EditorGUILayout.LabelField("TRAFFIC RANGE & DENSITY PRESETS (ONE-CLICK)", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = new Color(0.9f, 0.8f, 0.3f);
        if (GUILayout.Button("⚡ Yakın Çevre\n(20m - 120m | 25 Araç)", GUILayout.Height(36)))
        {
            Undo.RecordObject(manager, "Apply Close Proximity Traffic");
            manager.minSpawnDistance = 20f;
            manager.maxSpawnDistance = 120f;
            manager.despawnDistance = 160f;
            manager.maxActiveVehicles = 25;
            EditorUtility.SetDirty(manager);
        }

        GUI.backgroundColor = new Color(0.3f, 0.85f, 1f);
        if (GUILayout.Button("🏙️ Şehir & Ufuk\n(40m - 350m | 50 Araç)", GUILayout.Height(36)))
        {
            Undo.RecordObject(manager, "Apply Medium City Traffic");
            manager.minSpawnDistance = 40f;
            manager.maxSpawnDistance = 350f;
            manager.despawnDistance = 450f;
            manager.maxActiveVehicles = 50;
            EditorUtility.SetDirty(manager);
        }

        GUI.backgroundColor = new Color(0.4f, 1f, 0.5f);
        if (GUILayout.Button("🌍 Tüm Harita / Açık Dünya\n(50m - 700m | 80 Araç)", GUILayout.Height(36)))
        {
            Undo.RecordObject(manager, "Apply Full Map Traffic");
            manager.minSpawnDistance = 50f;
            manager.maxSpawnDistance = 700f;
            manager.despawnDistance = 900f;
            manager.maxActiveVehicles = 80;
            EditorUtility.SetDirty(manager);
        }

        EditorGUILayout.EndHorizontal();
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(8);

        // TRAFFIC CONTROL BUTTONS
        EditorGUILayout.LabelField("TRAFFIC RUNTIME ACTIONS", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = new Color(0.3f, 0.75f, 1f);
        if (GUILayout.Button("Rebuild Traffic Paths", GUILayout.Height(32)))
        {
            Undo.RecordObject(manager, "Rebuild Traffic Paths");
            manager.BuildTrafficPaths();
            EditorUtility.SetDirty(manager);
        }

        if (Application.isPlaying)
        {
            GUI.backgroundColor = new Color(1f, 0.6f, 0.2f);
            if (GUILayout.Button("Clear Active Traffic", GUILayout.Height(32)))
            {
                manager.ClearAllActiveVehicles();
            }
        }

        EditorGUILayout.EndHorizontal();
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(10);

        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(manager);
        }
    }

    private static void PopulateDefaultVehiclePrefabs(SplineTrafficManager manager)
    {
        if (manager == null) return;

        string[] searchFolders = new string[]
        {
            "Assets/_AssetPacks/SimplePoly City - Low Poly Assets/Prefab/Vehicles/Vehicle with Separated Wheels",
            "Assets/_AssetPacks/SimplePoly City - Low Poly Assets/Prefab/Vehicles/Vehicle with Static Wheels",
            "Assets/_AssetPacks/SimplePoly City/Prefabs/Vehicles",
            "Assets/Prefabs/Vehicles"
        };

        List<GameObject> foundPrefabs = new List<GameObject>();

        foreach (string folder in searchFolders)
        {
            if (!Directory.Exists(folder)) continue;

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new string[] { folder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && !foundPrefabs.Contains(prefab))
                {
                    // Filter out non-drivable objects
                    string name = prefab.name.ToLower();
                    if (name.Contains("car") || name.Contains("suv") || name.Contains("taxi") ||
                        name.Contains("truck") || name.Contains("bus") || name.Contains("police") ||
                        name.Contains("ambulance") || name.Contains("pickup") || name.Contains("pick up"))
                    {
                        foundPrefabs.Add(prefab);
                    }
                }
            }
        }

        if (foundPrefabs.Count > 0)
        {
            manager.vehiclePrefabs.Clear();
            manager.vehiclePrefabs.AddRange(foundPrefabs);
            Debug.Log($"[SplineTrafficManagerEditor] Added {foundPrefabs.Count} vehicle prefabs to Traffic Manager.");
        }
        else
        {
            Debug.LogWarning("[SplineTrafficManagerEditor] No vehicle prefabs found in default asset pack directories.");
        }
    }
}
#endif
