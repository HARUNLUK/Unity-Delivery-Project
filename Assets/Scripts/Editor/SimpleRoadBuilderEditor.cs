#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SimpleRoadBuilder))]
public class SimpleRoadBuilderEditor : Editor
{
    private int multiStraightCount = 5;

    // Obsolete menu item removed
    public static void CreateRoadBuilderInScene()
    {
        GameObject existing = GameObject.Find("Road_Builder_Tool");
        if (existing == null)
        {
            existing = new GameObject("Road_Builder_Tool");
            existing.transform.position = Vector3.zero;
        }

        SimpleRoadBuilder builder = existing.GetComponent<SimpleRoadBuilder>();
        if (builder == null) builder = existing.AddComponent<SimpleRoadBuilder>();
        builder.AutoAssignPrefabs();

        Selection.activeGameObject = existing;
        Debug.Log("[RoadBuilder] Road Builder Tool is ready in the Hierarchy! Select it to start laying roads with 1 click.");
    }

    public override void OnInspectorGUI()
    {
        SimpleRoadBuilder builder = (SimpleRoadBuilder)target;
        DrawDefaultInspector();

        if (builder.straightRoadPrefab == null)
        {
            if (GUILayout.Button("🔄 Auto Find & Assign Pandazole Road Prefabs", GUILayout.Height(30)))
            {
                builder.AutoAssignPrefabs();
                EditorUtility.SetDirty(builder);
            }
            return;
        }

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("🚗 QUICK ROAD PLACEMENT (1-CLICK SNAP)", EditorStyles.boldLabel);

        // 1. DÜZ İLERİ & GERİ
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
        if (GUILayout.Button("⬆️ Add Straight (Forward)", GUILayout.Height(38)))
        {
            PlaceRoadPiece(builder, builder.straightRoadPrefab, Vector3.forward, 0f);
        }

        GUI.backgroundColor = new Color(0.4f, 0.7f, 0.9f);
        if (GUILayout.Button("⬇️ Add Straight (Backward)", GUILayout.Height(38)))
        {
            PlaceRoadPiece(builder, builder.straightRoadPrefab, Vector3.back, 0f);
        }
        EditorGUILayout.EndHorizontal();

        // 2. VİRAJLAR (SAĞ & SOL)
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(1f, 0.75f, 0.25f);
        if (GUILayout.Button("↩️ Turn Left Corner", GUILayout.Height(34)))
        {
            PlaceRoadPiece(builder, builder.cornerRoadPrefab, Vector3.forward, -90f);
        }

        if (GUILayout.Button("↪️ Turn Right Corner", GUILayout.Height(34)))
        {
            PlaceRoadPiece(builder, builder.cornerRoadPrefab, Vector3.forward, 0f);
        }
        EditorGUILayout.EndHorizontal();

        // 3. KAVŞAKLAR
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.85f, 0.45f, 0.95f);
        if (GUILayout.Button("➕ 4-Way Cross", GUILayout.Height(32)))
        {
            PlaceRoadPiece(builder, builder.crossRoadPrefab, Vector3.forward, 0f);
        }

        if (GUILayout.Button("🔀 T-Junction (Side)", GUILayout.Height(32)))
        {
            PlaceRoadPiece(builder, builder.sideRoadPrefab, Vector3.forward, 0f);
        }

        GUI.backgroundColor = new Color(0.95f, 0.35f, 0.35f);
        if (GUILayout.Button("🛑 Dead End", GUILayout.Height(32)))
        {
            PlaceRoadPiece(builder, builder.endRoadPrefab, Vector3.forward, 0f);
        }
        EditorGUILayout.EndHorizontal();

        GUI.backgroundColor = Color.white;
        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("⚡ BATCH & UTILITY ACTIONS", EditorStyles.boldLabel);

        // ÇOKLU DÜZ YOL
        EditorGUILayout.BeginHorizontal();
        multiStraightCount = EditorGUILayout.IntSlider("Batch Count:", multiStraightCount, 1, 20);
        if (GUILayout.Button($"Diz ({multiStraightCount} Adet)", GUILayout.Width(110), GUILayout.Height(25)))
        {
            for (int i = 0; i < multiStraightCount; i++)
            {
                PlaceRoadPiece(builder, builder.straightRoadPrefab, Vector3.forward, 0f);
            }
        }
        EditorGUILayout.EndHorizontal();

        // ARAZİYE YAPIŞTIR
        EditorGUILayout.Space(5);
        if (GUILayout.Button("🏔️ Snap All Placed Roads to Terrain Height", GUILayout.Height(28)))
        {
            SnapAllToTerrain(builder);
        }

        // GERİ AL / SİL
        EditorGUILayout.Space(5);
        GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
        if (GUILayout.Button("🗑️ Undo / Delete Last Placed Piece", GUILayout.Height(28)))
        {
            DeleteLastPiece(builder);
        }
        GUI.backgroundColor = Color.white;
    }

    private void PlaceRoadPiece(SimpleRoadBuilder builder, GameObject prefab, Vector3 localDirection, float rotationOffset)
    {
        if (prefab == null) return;

        if (builder.roadContainer == null)
        {
            GameObject container = GameObject.Find("Road_Network");
            if (container == null) container = new GameObject("Road_Network");
            builder.roadContainer = container.transform;
        }

        Vector3 spawnPos = builder.transform.position;
        Quaternion spawnRot = builder.transform.rotation;

        if (builder.lastPlacedPiece != null)
        {
            Transform lastT = builder.lastPlacedPiece.transform;
            Vector3 forwardDir = lastT.forward;

            if (localDirection == Vector3.back) forwardDir = -lastT.forward;

            spawnPos = lastT.position + (forwardDir * builder.segmentLength);
            spawnRot = lastT.rotation * Quaternion.Euler(0, rotationOffset, 0);
        }

        // Terrain Snapping
        if (builder.snapToTerrain)
        {
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(spawnPos.x, 500f, spawnPos.z), Vector3.down, out hit, 1000f))
            {
                spawnPos.y = hit.point.y + builder.terrainOffset;
            }
        }

        GameObject newPiece = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        newPiece.transform.SetParent(builder.roadContainer);
        newPiece.transform.position = spawnPos;
        newPiece.transform.rotation = spawnRot;
        newPiece.name = $"{prefab.name}_{builder.placedRoads.Count + 1}";

        // MeshCollider Güvencesi
        AddMeshCollider(newPiece);

        builder.lastPlacedPiece = newPiece;
        builder.placedRoads.Add(newPiece);

        Undo.RegisterCreatedObjectUndo(newPiece, "Placed Road Piece");
        EditorUtility.SetDirty(builder);
        Selection.activeGameObject = newPiece;
    }

    private void SnapAllToTerrain(SimpleRoadBuilder builder)
    {
        foreach (var road in builder.placedRoads)
        {
            if (road != null)
            {
                RaycastHit hit;
                if (Physics.Raycast(new Vector3(road.transform.position.x, 500f, road.transform.position.z), Vector3.down, out hit, 1000f))
                {
                    Vector3 p = road.transform.position;
                    p.y = hit.point.y + builder.terrainOffset;
                    road.transform.position = p;
                }
            }
        }
    }

    private void DeleteLastPiece(SimpleRoadBuilder builder)
    {
        if (builder.placedRoads.Count > 0)
        {
            int lastIndex = builder.placedRoads.Count - 1;
            GameObject toDelete = builder.placedRoads[lastIndex];
            builder.placedRoads.RemoveAt(lastIndex);

            if (toDelete != null) Undo.DestroyObjectImmediate(toDelete);

            builder.lastPlacedPiece = builder.placedRoads.Count > 0 ? builder.placedRoads[builder.placedRoads.Count - 1] : null;
            EditorUtility.SetDirty(builder);
        }
    }

    private void AddMeshCollider(GameObject obj)
    {
        MeshRenderer[] renderers = obj.GetComponentsInChildren<MeshRenderer>();
        foreach (var r in renderers)
        {
            if (r.GetComponent<Collider>() == null)
            {
                r.gameObject.AddComponent<MeshCollider>();
            }
        }
    }
}
#endif
