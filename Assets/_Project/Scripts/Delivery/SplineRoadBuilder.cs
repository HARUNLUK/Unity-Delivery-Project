using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class RoadBranch
{
    public string branchName = "Main Road";
    public List<Vector3> waypoints = new List<Vector3>();

    public RoadBranch(string name)
    {
        branchName = name;
        waypoints = new List<Vector3>();
    }
}

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SplineRoadBuilder : MonoBehaviour
{
    [Header("--- ROAD GEOMETRY ---")]
    [Tooltip("Road width in meters")]
    public float roadWidth = 8.0f;

    [Tooltip("Distance between road cross-sections")]
    [Range(0.4f, 2.0f)]
    public float resolution = 0.8f;

    [Tooltip("Terrain height offset. (-) Carves road into trench. (+) Elevates road.")]
    [Range(-5.0f, 5.0f)]
    public float terrainOffset = 0.0f;

    [Tooltip("Render priority when overlapping other roads. Higher values (1, 2, 3...) sit on top with zero flickering.")]
    [Range(0, 10)]
    public int renderPriority = 0;

    [Tooltip("UV Texture tiling")]
    public float uvTiling = 0.25f;

    [Header("--- TERRAIN SCULPTING (ORGANIC SMOOTH & ZERO LAG) ---")]
    [Tooltip("Automatically sculpt and pull the terrain up/down to match all branches")]
    public bool autoDeformTerrain = true;

    [Tooltip("Width of the gentle natural slope blending the road into surrounding terrain (in meters)")]
    [Range(4.0f, 40.0f)]
    public float blendMargin = 12.0f;

    [Header("--- MATERIAL ---")]
    public Material roadMaterial;

    [Header("--- UNIFIED ROAD BRANCHES ---")]
    [SerializeField] public List<RoadBranch> branches = new List<RoadBranch>();
    public int activeBranchIndex = 0;
    public int selectedPointIndex = -1;

    [SerializeField] public List<Vector3> waypoints = new List<Vector3>();

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;

    private void Awake()
    {
        InitComponents();
        MigrateLegacyWaypoints();
    }

    private void OnValidate()
    {
        MigrateLegacyWaypoints();
        if (HasAnyPoints())
        {
            UpdateRoadAndTerrain();
        }
    }

    public void UpdateRoadAndTerrain()
    {
        if (HasAnyPoints())
        {
            if (autoDeformTerrain)
            {
                DeformTerrainUnderRoad();
            }
            RebuildRoadMesh();
        }
    }

    private void MigrateLegacyWaypoints()
    {
        if (branches.Count == 0)
        {
            RoadBranch mainBranch = new RoadBranch("Main Road");
            if (waypoints != null && waypoints.Count > 0)
            {
                mainBranch.waypoints.AddRange(waypoints);
            }
            branches.Add(mainBranch);
            activeBranchIndex = 0;
        }
    }

    private void InitComponents()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();
        if (meshCollider == null) meshCollider = gameObject.AddComponent<MeshCollider>();

        if (roadMaterial == null)
        {
#if UNITY_EDITOR
            roadMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/RoadStyles/Mat_Road_2Lane_Striped.mat");
            if (roadMaterial == null)
                roadMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Road_Asphalt_Material.mat");
#endif
        }

        if (roadMaterial != null && meshRenderer != null)
        {
            meshRenderer.sharedMaterial = roadMaterial;
        }
    }

    public RoadBranch GetActiveBranch()
    {
        MigrateLegacyWaypoints();
        if (activeBranchIndex < 0 || activeBranchIndex >= branches.Count) activeBranchIndex = 0;
        return branches[activeBranchIndex];
    }

    public bool HasAnyPoints()
    {
        foreach (var b in branches)
        {
            if (b.waypoints.Count >= 2) return true;
        }
        return false;
    }

    public void AddPointToActiveBranch(Vector3 worldPoint)
    {
        Vector3 localPt = transform.InverseTransformPoint(worldPoint);
        RoadBranch branch = GetActiveBranch();
        branch.waypoints.Add(localPt);
        selectedPointIndex = branch.waypoints.Count - 1;

        UpdateRoadAndTerrain();
    }

    public void CreateBranchFromPoint(int branchIdx, int pointIdx)
    {
        if (branchIdx < 0 || branchIdx >= branches.Count) return;
        RoadBranch parentBranch = branches[branchIdx];
        if (pointIdx < 0 || pointIdx >= parentBranch.waypoints.Count) return;

        Vector3 junctionLocalPos = parentBranch.waypoints[pointIdx];

        RoadBranch newBranch = new RoadBranch($"Branch {branches.Count + 1}");
        newBranch.waypoints.Add(junctionLocalPos);
        branches.Add(newBranch);

        activeBranchIndex = branches.Count - 1;
        selectedPointIndex = 0;

        UpdateRoadAndTerrain();
    }

    public void MoveWaypointSynchronized(int branchIdx, int pointIdx, Vector3 newLocalPos)
    {
        if (branchIdx < 0 || branchIdx >= branches.Count) return;
        RoadBranch targetBranch = branches[branchIdx];
        if (pointIdx < 0 || pointIdx >= targetBranch.waypoints.Count) return;

        Vector3 oldPos = targetBranch.waypoints[pointIdx];
        targetBranch.waypoints[pointIdx] = newLocalPos;

        for (int b = 0; b < branches.Count; b++)
        {
            if (b == branchIdx) continue;
            for (int p = 0; p < branches[b].waypoints.Count; p++)
            {
                if (Vector3.Distance(branches[b].waypoints[p], oldPos) < 0.25f)
                {
                    branches[b].waypoints[p] = newLocalPos;
                }
            }
        }

        UpdateRoadAndTerrain();
    }

    public void InsertPoint(int branchIdx, int pointIdx, Vector3 worldPoint)
    {
        if (branchIdx < 0 || branchIdx >= branches.Count) return;
        RoadBranch branch = branches[branchIdx];
        Vector3 localPt = transform.InverseTransformPoint(worldPoint);

        if (pointIdx >= 0 && pointIdx <= branch.waypoints.Count)
        {
            branch.waypoints.Insert(pointIdx, localPt);
            selectedPointIndex = pointIdx;

            UpdateRoadAndTerrain();
        }
    }

    public void DeletePoint(int branchIdx, int pointIdx)
    {
        if (branchIdx < 0 || branchIdx >= branches.Count) return;
        RoadBranch branch = branches[branchIdx];

        if (pointIdx >= 0 && pointIdx < branch.waypoints.Count)
        {
            branch.waypoints.RemoveAt(pointIdx);
            selectedPointIndex = Mathf.Clamp(pointIdx - 1, 0, branch.waypoints.Count - 1);

            UpdateRoadAndTerrain();
        }
    }

    public void RemoveLastPointFromActiveBranch()
    {
        RoadBranch branch = GetActiveBranch();
        if (branch.waypoints.Count > 0)
        {
            branch.waypoints.RemoveAt(branch.waypoints.Count - 1);
            selectedPointIndex = branch.waypoints.Count - 1;

            UpdateRoadAndTerrain();
        }
    }

    public void ClearAll()
    {
        branches.Clear();
        MigrateLegacyWaypoints();
        selectedPointIndex = -1;

        if (meshFilter != null && meshFilter.sharedMesh != null) meshFilter.sharedMesh.Clear();
        if (meshCollider != null) meshCollider.sharedMesh = null;
    }

    /// <summary>
    /// Rebuilds the road mesh ribbon with surface snapping.
    /// </summary>
    [ContextMenu("Rebuild Road Mesh")]
    public void RebuildRoadMesh()
    {
        InitComponents();

        List<Vector3> allVertices = new List<Vector3>();
        List<Vector3> allNormals = new List<Vector3>();
        List<Vector2> allUvs = new List<Vector2>();
        List<int> allTriangles = new List<int>();

        Terrain activeTerrain = Terrain.activeTerrain;
        if (activeTerrain == null) activeTerrain = UnityEngine.Object.FindAnyObjectByType<Terrain>();

        float halfWidth = roadWidth * 0.5f;

        if (meshCollider != null) meshCollider.enabled = false;

        foreach (var branch in branches)
        {
            if (branch.waypoints.Count < 2) continue;

            List<Vector3> samplePoints = GenerateSplineSamples(branch.waypoints, resolution);
            if (samplePoints.Count < 2) continue;

            int branchVertexOffset = allVertices.Count;
            float currentLength = 0f;

            for (int i = 0; i < samplePoints.Count; i++)
            {
                Vector3 pt = samplePoints[i];
                Vector3 forward;

                if (i < samplePoints.Count - 1)
                    forward = (samplePoints[i + 1] - pt).normalized;
                else
                    forward = (pt - samplePoints[i - 1]).normalized;

                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
                Vector3 normal = Vector3.Cross(forward, right).normalized;

                Vector3 localLeft = pt - (right * halfWidth);
                Vector3 localRight = pt + (right * halfWidth);

                Vector3 worldLeft = transform.TransformPoint(localLeft);
                Vector3 worldRight = transform.TransformPoint(localRight);

                float priorityOffset = renderPriority * 0.02f;
                float splineY = transform.TransformPoint(pt).y + terrainOffset + priorityOffset;

                if (activeTerrain != null)
                {
                    float leftGroundY = activeTerrain.SampleHeight(worldLeft) + activeTerrain.transform.position.y;
                    float rightGroundY = activeTerrain.SampleHeight(worldRight) + activeTerrain.transform.position.y;

                    worldLeft.y = Mathf.Max(splineY, leftGroundY + priorityOffset) + 0.04f;
                    worldRight.y = Mathf.Max(splineY, rightGroundY + priorityOffset) + 0.04f;
                }
                else
                {
                    worldLeft.y = splineY + 0.04f;
                    worldRight.y = splineY + 0.04f;
                }

                allVertices.Add(transform.InverseTransformPoint(worldLeft));  // V0
                allVertices.Add(transform.InverseTransformPoint(worldRight)); // V1

                allNormals.Add(normal);
                allNormals.Add(normal);

                if (i > 0) currentLength += Vector3.Distance(samplePoints[i], samplePoints[i - 1]);

                float vCoord = currentLength * uvTiling;
                allUvs.Add(new Vector2(0f, vCoord));
                allUvs.Add(new Vector2(1f, vCoord));

                if (i < samplePoints.Count - 1)
                {
                    int r0 = branchVertexOffset + (i * 2);
                    int r1 = branchVertexOffset + ((i + 1) * 2);

                    allTriangles.Add(r0 + 0);
                    allTriangles.Add(r1 + 0);
                    allTriangles.Add(r0 + 1);

                    allTriangles.Add(r0 + 1);
                    allTriangles.Add(r1 + 0);
                    allTriangles.Add(r1 + 1);
                }
            }
        }

        Mesh roadMesh = new Mesh();
        roadMesh.name = "Unified_MultiBranch_SplineRoadMesh";
        roadMesh.SetVertices(allVertices);
        roadMesh.SetNormals(allNormals);
        roadMesh.SetUVs(0, allUvs);
        roadMesh.SetTriangles(allTriangles, 0);
        roadMesh.RecalculateBounds();
        roadMesh.RecalculateNormals();

        if (meshFilter != null) meshFilter.sharedMesh = roadMesh;

        if (meshRenderer != null)
        {
            if (roadMaterial != null) meshRenderer.sharedMaterial = roadMaterial;
        }

        if (meshCollider != null)
        {
            meshCollider.sharedMesh = roadMesh;
            meshCollider.enabled = true;
        }
    }

    /// <summary>
    /// Segment-Bounded Distance Buffer + 5th-Order SmootherStep:
    /// Keskin hatları sıfırlayan, kasma ve donma yapmayan ultra hızlı (1-2 ms) pürüzsüz arazi deformasyonu.
    /// </summary>
    [ContextMenu("Deform Terrain Under Road")]
    public void DeformTerrainUnderRoad()
    {
        if (!HasAnyPoints()) return;

        Terrain[] allTerrains = Terrain.activeTerrains;
        if (allTerrains == null || allTerrains.Length == 0)
        {
            allTerrains = UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        }

        if (allTerrains == null || allTerrains.Length == 0) return;

        float totalDist = (roadWidth * 0.5f) + blendMargin;

        List<List<Vector3>> branchSamples = new List<List<Vector3>>();
        float minWX = float.MaxValue, maxWX = float.MinValue;
        float minWZ = float.MaxValue, maxWZ = float.MinValue;

        foreach (var branch in branches)
        {
            if (branch.waypoints.Count < 2) continue;
            List<Vector3> samples = GenerateSplineSamples(branch.waypoints, 1.0f);
            List<Vector3> worldPts = new List<Vector3>();

            foreach (var s in samples)
            {
                Vector3 w = transform.TransformPoint(s);
                worldPts.Add(w);
                if (w.x < minWX) minWX = w.x;
                if (w.x > maxWX) maxWX = w.x;
                if (w.z < minWZ) minWZ = w.z;
                if (w.z > maxWZ) maxWZ = w.z;
            }
            branchSamples.Add(worldPts);
        }

        if (branchSamples.Count == 0) return;

        minWX -= totalDist + 2f; maxWX += totalDist + 2f;
        minWZ -= totalDist + 2f; maxWZ += totalDist + 2f;

        foreach (Terrain terrain in allTerrains)
        {
            if (terrain == null || terrain.terrainData == null) continue;

            TerrainData tData = terrain.terrainData;
            int hRes = tData.heightmapResolution;
            Vector3 tPos = terrain.transform.position;
            Vector3 tSize = tData.size;

            float cellX = tSize.x / (hRes - 1);
            float cellZ = tSize.z / (hRes - 1);

            if (maxWX < tPos.x || minWX > tPos.x + tSize.x || maxWZ < tPos.z || minWZ > tPos.z + tSize.z)
                continue;

            int minX = Mathf.Clamp(Mathf.FloorToInt((minWX - tPos.x) / cellX), 0, hRes - 1);
            int maxX = Mathf.Clamp(Mathf.CeilToInt((maxWX - tPos.x) / cellX), 0, hRes - 1);
            int minZ = Mathf.Clamp(Mathf.FloorToInt((minWZ - tPos.z) / cellZ), 0, hRes - 1);
            int maxZ = Mathf.Clamp(Mathf.CeilToInt((maxWZ - tPos.z) / cellZ), 0, hRes - 1);

            int width = (maxX - minX) + 1;
            int height = (maxZ - minZ) + 1;

            if (width <= 0 || height <= 0) continue;
            if (minX + width > hRes) width = hRes - minX;
            if (minZ + height > hRes) height = hRes - minZ;

            float[,] heights = tData.GetHeights(minX, minZ, width, height);
            float[,] originalHeights = (float[,])heights.Clone();

            float[,] minDistanceBuffer = new float[height, width];
            float[,] bestTargetYBuffer = new float[height, width];

            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    minDistanceBuffer[z, x] = float.MaxValue;
                }
            }

            // 1. HIZLI SINIRLI MESAFE MATRİSİ (Yalnızca ilgili pikselleri tarar)
            foreach (var worldPts in branchSamples)
            {
                for (int i = 0; i < worldPts.Count - 1; i++)
                {
                    Vector3 wA = worldPts[i];
                    Vector3 wB = worldPts[i + 1];

                    Vector2 a2D = new Vector2(wA.x, wA.z);
                    Vector2 b2D = new Vector2(wB.x, wB.z);

                    float segMinX = Mathf.Min(wA.x, wB.x) - totalDist;
                    float segMaxX = Mathf.Max(wA.x, wB.x) + totalDist;
                    float segMinZ = Mathf.Min(wA.z, wB.z) - totalDist;
                    float segMaxZ = Mathf.Max(wA.z, wB.z) + totalDist;

                    int startX = Mathf.Clamp(Mathf.FloorToInt((segMinX - tPos.x) / cellX) - minX, 0, width - 1);
                    int endX = Mathf.Clamp(Mathf.CeilToInt((segMaxX - tPos.x) / cellX) - minX, 0, width - 1);
                    int startZ = Mathf.Clamp(Mathf.FloorToInt((segMinZ - tPos.z) / cellZ) - minZ, 0, height - 1);
                    int endZ = Mathf.Clamp(Mathf.CeilToInt((segMaxZ - tPos.z) / cellZ) - minZ, 0, height - 1);

                    for (int z = startZ; z <= endZ; z++)
                    {
                        float worldZ = tPos.z + (minZ + z) * cellZ;
                        for (int x = startX; x <= endX; x++)
                        {
                            float worldX = tPos.x + (minX + x) * cellX;
                            Vector2 cellPos = new Vector2(worldX, worldZ);

                            Vector2 closest = ClosestPointOnSegment(cellPos, a2D, b2D, out float t);
                            float dist = Vector2.Distance(cellPos, closest);

                            if (dist < minDistanceBuffer[z, x])
                            {
                                minDistanceBuffer[z, x] = dist;
                                bestTargetYBuffer[z, x] = Mathf.Lerp(wA.y, wB.y, t) + terrainOffset;
                            }
                        }
                    }
                }
            }

            // 2. TEK GEÇİŞLİ PÜRÜZSÜZ S-CURVE UYGULAMASI (Sıfır kasma, sıfır basamak)
            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dist = minDistanceBuffer[z, x];
                    if (dist <= totalDist)
                    {
                        float targetNormY = Mathf.Clamp01((bestTargetYBuffer[z, x] - tPos.y) / tSize.y);
                        float originalNormY = originalHeights[z, x];

                        // 5. Derece SmootherStep (Kenarlarda teğet sıfır kırılma)
                        float t = Mathf.Clamp01(dist / totalDist);
                        float smoothFactor = t * t * t * (t * (6f * t - 15f) + 10f);

                        heights[z, x] = Mathf.Lerp(targetNormY, originalNormY, smoothFactor);
                    }
                }
            }

            tData.SetHeightsDelayLOD(minX, minZ, heights);
            tData.SyncHeightmap();
#if UNITY_EDITOR
            EditorUtility.SetDirty(tData);
#endif
        }

#if UNITY_EDITOR
        SceneView.RepaintAll();
#endif
    }

    private Vector2 ClosestPointOnSegment(Vector2 p, Vector2 a, Vector2 b, out float t)
    {
        Vector2 ab = b - a;
        float abSqr = ab.sqrMagnitude;
        if (abSqr < 0.0001f)
        {
            t = 0f;
            return a;
        }
        t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / abSqr);
        return a + (t * ab);
    }

    private List<Vector3> GenerateSplineSamples(List<Vector3> pts, float stepSize)
    {
        List<Vector3> samples = new List<Vector3>();
        if (pts.Count < 2) return samples;

        for (int i = 0; i < pts.Count - 1; i++)
        {
            Vector3 p0 = i > 0 ? pts[i - 1] : pts[i];
            Vector3 p1 = pts[i];
            Vector3 p2 = pts[i + 1];
            Vector3 p3 = (i + 2 < pts.Count) ? pts[i + 2] : p2;

            float segmentDistance = Vector3.Distance(p1, p2);
            int divisions = Mathf.Max(2, Mathf.CeilToInt(segmentDistance / stepSize));

            for (int d = 0; d < divisions; d++)
            {
                float t = (float)d / divisions;
                samples.Add(GetCatmullRomPosition(t, p0, p1, p2, p3));
            }
        }

        if (pts.Count > 0)
        {
            samples.Add(pts[pts.Count - 1]);
        }

        return samples;
    }

    private Vector3 GetCatmullRomPosition(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * (t * t) +
            (-p0 + 3f * p1 - 3f * p2 + p3) * (t * t * t)
        );
    }
}
