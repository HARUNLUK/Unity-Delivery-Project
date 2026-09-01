#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SplineRoadBuilder))]
public class SplineRoadBuilderEditor : Editor
{
    [MenuItem("Tools/Delivery Game/Create Spline Road Drawer (Shift + Click)", false, 3)]
    public static void CreateSplineRoadDrawer()
    {
        SplineRoadBuilder existing = Object.FindAnyObjectByType<SplineRoadBuilder>();
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            Debug.Log($"[SplineRoadBuilder] Sahnede mevcut olan '{existing.gameObject.name}' seçildi! Yeni bir obje oluşturulmadı. Shift + Sol Tık ile çizmeye devam edebilirsin.");
            return;
        }

        GameObject roadObj = new GameObject("Spline_Road");
        roadObj.transform.position = Vector3.zero;

        SplineRoadBuilder builder = roadObj.AddComponent<SplineRoadBuilder>();
        Selection.activeGameObject = roadObj;

        Debug.Log("[SplineRoadBuilder] 'Spline_Road' oluşturuldu! Shift + Sol Tık ile çizmeye başla.");
    }

    public override void OnInspectorGUI()
    {
        SplineRoadBuilder builder = (SplineRoadBuilder)target;

        EditorGUILayout.HelpBox("💡 DOĞRUDAN ÇATAL ÇIKARMA (SIFIR BUTON):\n1. Shift + Sol Tık ile yol noktalarını koy.\n2. Bir noktadan yeni yol ayırmak için Shift ile o noktaya tıkla, ardından boş araziye tıkla!\n3. Aşağıdaki Hazır Stil butonlarıyla yolun görünümünü anında değiştirebilirsin.", MessageType.Info);

        EditorGUILayout.Space(10);

        // 🎨 HAZIR YOL STİLLERİ (MATERYAL ÖN AYARLARI)
        EditorGUILayout.LabelField("🎨 HAZIR YOL STİLLERİ (TEK TIKLA DEĞİŞTİR)", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = new Color(0.9f, 0.7f, 0.2f);
        if (GUILayout.Button("🛣️ Çizgili Asfalt\n(2-Lane Striped)", GUILayout.Height(36)))
        {
            ApplyStylePreset(builder, "Mat_Road_2Lane_Striped.mat", 0.18f);
        }

        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.9f);
        if (GUILayout.Button("🏙️ Kaldırımlı Cadde\n(City Sidewalk)", GUILayout.Height(36)))
        {
            ApplyStylePreset(builder, "Mat_Road_City_Sidewalks.mat", 0.15f);
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = new Color(0.7f, 0.5f, 0.3f);
        if (GUILayout.Button("🏔️ Toprak Köy Yolu\n(Mountain Dirt)", GUILayout.Height(32)))
        {
            ApplyStylePreset(builder, "Mat_Road_Mountain_Dirt.mat", 0.20f);
        }

        GUI.backgroundColor = new Color(0.5f, 0.5f, 0.5f);
        if (GUILayout.Button("⬛ Düz Sade Asfalt\n(Plain Asphalt)", GUILayout.Height(32)))
        {
            ApplyStylePreset(builder, "Road_Asphalt_Material.mat", 0.25f);
        }

        EditorGUILayout.EndHorizontal();
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(12);

        // AKTİF DAL SEÇİCİ
        if (builder.branches.Count > 1)
        {
            EditorGUILayout.LabelField("🛣️ ROAD BRANCHES (KOLLAR / ÇATALLAR)", EditorStyles.boldLabel);
            string[] branchNames = new string[builder.branches.Count];
            for (int i = 0; i < builder.branches.Count; i++)
            {
                branchNames[i] = $"{i + 1}. {builder.branches[i].branchName} ({builder.branches[i].waypoints.Count} nokta)";
            }

            int newBranchIdx = EditorGUILayout.Popup("Çizim Yapılan Aktif Kol:", builder.activeBranchIndex, branchNames);
            if (newBranchIdx != builder.activeBranchIndex)
            {
                builder.activeBranchIndex = newBranchIdx;
                builder.selectedPointIndex = -1;
                EditorUtility.SetDirty(builder);
            }
            EditorGUILayout.Space(10);
        }

        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        if (EditorGUI.EndChangeCheck())
        {
            builder.UpdateRoadAndTerrain();
            EditorUtility.SetDirty(builder);
        }

        EditorGUILayout.Space(10);

        // SEÇİLİ NOKTA DETAYI
        RoadBranch activeBranch = builder.GetActiveBranch();
        if (builder.selectedPointIndex >= 0 && builder.selectedPointIndex < activeBranch.waypoints.Count)
        {
            EditorGUILayout.LabelField($"📍 SEÇİLİ: {activeBranch.branchName} ➔ Nokta {builder.selectedPointIndex + 1}", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.3f, 0.75f, 1f);
            if (GUILayout.Button("➕ Insert Point After Selected", GUILayout.Height(28)))
            {
                Vector3 currentWorld = builder.transform.TransformPoint(activeBranch.waypoints[builder.selectedPointIndex]);
                Vector3 insertWorld = currentWorld + (Vector3.forward * 4f);
                builder.InsertPoint(builder.activeBranchIndex, builder.selectedPointIndex + 1, insertWorld);
            }

            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("🗑️ Delete Selected Point", GUILayout.Height(28)))
            {
                builder.DeletePoint(builder.activeBranchIndex, builder.selectedPointIndex);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.LabelField("🏔️ TERRAIN & ROAD ACTIONS", EditorStyles.boldLabel);

        GUI.backgroundColor = new Color(0.2f, 0.75f, 1f);
        if (GUILayout.Button("🏔️ Snap & Deform Terrain Under Road (Araziyi Yola Yapıştır/Oy)", GUILayout.Height(32)))
        {
            Undo.RegisterCompleteObjectUndo(Terrain.activeTerrain.terrainData, "Deform Terrain Under Road");
            builder.DeformTerrainUnderRoad();
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
        if (GUILayout.Button("🔄 Rebuild Mesh", GUILayout.Height(28)))
        {
            builder.RebuildRoadMesh();
            EditorUtility.SetDirty(builder);
        }

        GUI.backgroundColor = new Color(1f, 0.6f, 0.2f);
        if (GUILayout.Button("↩️ Remove Last Point", GUILayout.Height(28)))
        {
            builder.RemoveLastPointFromActiveBranch();
            EditorUtility.SetDirty(builder);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);
        GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
        if (GUILayout.Button("🗑️ Clear All Roads & Branches", GUILayout.Height(26)))
        {
            if (EditorUtility.DisplayDialog("Clear Spline Road", "Are you sure you want to clear all branches and waypoints?", "Yes", "No"))
            {
                builder.ClearAll();
                EditorUtility.SetDirty(builder);
            }
        }
        GUI.backgroundColor = Color.white;
    }

    private void ApplyStylePreset(SplineRoadBuilder builder, string matFileName, float uvTile)
    {
        string path = $"Assets/Materials/RoadStyles/{matFileName}";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            path = $"Assets/Materials/{matFileName}";
            mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        }

        if (mat == null)
        {
            RoadTextureGenerator.GenerateRoadMaterials();
            path = $"Assets/Materials/RoadStyles/{matFileName}";
            mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        }

        if (mat != null)
        {
            Undo.RecordObject(builder, "Apply Road Style");
            builder.roadMaterial = mat;
            builder.uvTiling = uvTile;
            builder.RebuildRoadMesh();
            EditorUtility.SetDirty(builder);
            Debug.Log($"[SplineRoadBuilder] Yol stili '{mat.name}' olarak değiştirildi!");
        }
    }

    private void OnSceneGUI()
    {
        SplineRoadBuilder builder = (SplineRoadBuilder)target;
        Event currentEvent = Event.current;

        // 1. Shift + Left Click ile Doğrudan Nokta Ekleme veya Noktadan Çatal Çıkarma
        if (currentEvent.shift)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
            
            MeshCollider col = builder.GetComponent<MeshCollider>();
            if (col != null) col.enabled = false;

            RaycastHit hit;
            bool didHit = Physics.Raycast(ray, out hit, 3000f);

            if (col != null) col.enabled = true;

            if (didHit)
            {
                Vector3 targetPoint = hit.point;

                Terrain terrain = Terrain.activeTerrain;
                if (terrain != null)
                {
                    targetPoint.y = terrain.SampleHeight(targetPoint) + terrain.transform.position.y;
                }

                int hoveredBranch = -1;
                int hoveredPoint = -1;
                float closestDist = float.MaxValue;
                float snapThreshold = builder.roadWidth * 0.7f;

                for (int b = 0; b < builder.branches.Count; b++)
                {
                    for (int p = 0; p < builder.branches[b].waypoints.Count; p++)
                    {
                        Vector3 wpWorld = builder.transform.TransformPoint(builder.branches[b].waypoints[p]);
                        float d = Vector3.Distance(targetPoint, wpWorld);
                        if (d < snapThreshold && d < closestDist)
                        {
                            closestDist = d;
                            hoveredBranch = b;
                            hoveredPoint = p;
                        }
                    }
                }

                if (hoveredBranch != -1)
                {
                    Vector3 wpWorld = builder.transform.TransformPoint(builder.branches[hoveredBranch].waypoints[hoveredPoint]);
                    Handles.color = new Color(0.2f, 1f, 0.4f, 0.9f);
                    Handles.DrawWireDisc(wpWorld + (Vector3.up * 0.1f), Vector3.up, builder.roadWidth * 0.6f);
                    Handles.color = new Color(0.2f, 1f, 0.4f, 0.35f);
                    Handles.DrawSolidDisc(wpWorld + (Vector3.up * 0.1f), Vector3.up, builder.roadWidth * 0.6f);
                    Handles.Label(wpWorld + (Vector3.up * 2f), $"🌿 CLICK TO BRANCH FROM HERE ({builder.branches[hoveredBranch].branchName} P{hoveredPoint + 1})");
                    HandleUtility.Repaint();

                    if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
                    {
                        Undo.RecordObject(builder, "Branch Road From Point");
                        builder.CreateBranchFromPoint(hoveredBranch, hoveredPoint);
                        currentEvent.Use();
                    }
                }
                else
                {
                    Handles.color = new Color(0.2f, 1f, 0.4f, 0.8f);
                    Handles.DrawWireDisc(targetPoint + (Vector3.up * 0.05f), Vector3.up, builder.roadWidth * 0.5f);
                    Handles.color = new Color(0.2f, 1f, 0.4f, 0.25f);
                    Handles.DrawSolidDisc(targetPoint + (Vector3.up * 0.05f), Vector3.up, builder.roadWidth * 0.5f);
                    HandleUtility.Repaint();

                    if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
                    {
                        Undo.RecordObject(builder, "Add Spline Waypoint");
                        RoadBranch curBranch = builder.GetActiveBranch();

                        if (builder.selectedPointIndex >= 0 && builder.selectedPointIndex < curBranch.waypoints.Count - 1)
                        {
                            builder.CreateBranchFromPoint(builder.activeBranchIndex, builder.selectedPointIndex);
                            builder.AddPointToActiveBranch(targetPoint);
                        }
                        else
                        {
                            builder.AddPointToActiveBranch(targetPoint);
                        }
                        currentEvent.Use();
                    }
                }
            }
        }

        // 2. Tüm Dallardaki Noktaları Çiz ve Anlık Sürükleme ile Eşzamanlı Güncelle
        bool hasChanges = false;

        for (int b = 0; b < builder.branches.Count; b++)
        {
            RoadBranch branch = builder.branches[b];
            bool isActiveBranch = (b == builder.activeBranchIndex);

            for (int i = 0; i < branch.waypoints.Count; i++)
            {
                Vector3 worldPos = builder.transform.TransformPoint(branch.waypoints[i]);
                bool isSelected = (isActiveBranch && i == builder.selectedPointIndex);

                Handles.color = isSelected 
                    ? new Color(0.2f, 1f, 0.4f) 
                    : (isActiveBranch ? new Color(0.3f, 0.75f, 1f) : new Color(0.7f, 0.7f, 0.7f, 0.6f));

                // Tıklanabilir Seçim Küresi
                if (Handles.Button(worldPos + (Vector3.up * 0.5f), Quaternion.identity, 0.8f, 1.2f, Handles.SphereHandleCap))
                {
                    builder.activeBranchIndex = b;
                    builder.selectedPointIndex = i;
                    Repaint();
                }

                Handles.Label(worldPos + (Vector3.up * 1.6f), $"{branch.branchName} P{i + 1}" + (isSelected ? " [SELECTED]" : ""));

                if (isActiveBranch)
                {
                    EditorGUI.BeginChangeCheck();
                    Vector3 newWorldPos = Handles.PositionHandle(worldPos, Quaternion.identity);

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(builder, "Move Spline Waypoint");
                        Vector3 newLocalPos = builder.transform.InverseTransformPoint(newWorldPos);
                        
                        builder.MoveWaypointSynchronized(b, i, newLocalPos);
                        builder.selectedPointIndex = i;
                        hasChanges = true;
                    }
                }
            }
        }

        if (hasChanges)
        {
            EditorUtility.SetDirty(builder);
        }
    }
}
#endif
