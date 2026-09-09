using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum RoadEndCapStyle
{
    RoundedCap,   // Smooth 180-degree half-circle with seamless sidewalk & curb wrap
    SquareCap,    // Flat perpendicular closed border with sidewalk & curb
    FlatOpen      // Cut flat with no end cap
}

[System.Serializable]
public class RoadBranch
{
    public string branchName = "Main Road";
    public List<Vector3> waypoints = new List<Vector3>();

    // Indices of waypoints that have Round End Cap explicitly enabled by user
    [SerializeField] public List<int> roundCappedPointIndices = new List<int>();

    public RoadBranch(string name)
    {
        branchName = name;
        waypoints = new List<Vector3>();
        roundCappedPointIndices = new List<int>();
    }

    public bool IsPointRoundCapped(int pointIndex)
    {
        if (roundCappedPointIndices == null) roundCappedPointIndices = new List<int>();
        return roundCappedPointIndices.Contains(pointIndex);
    }

    public void SetPointRoundCapped(int pointIndex, bool capped)
    {
        if (roundCappedPointIndices == null) roundCappedPointIndices = new List<int>();
        if (capped && !roundCappedPointIndices.Contains(pointIndex))
        {
            roundCappedPointIndices.Add(pointIndex);
        }
        else if (!capped && roundCappedPointIndices.Contains(pointIndex))
        {
            roundCappedPointIndices.Remove(pointIndex);
        }
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

    [Header("--- ROAD END CAPS (Kaldırım & Uç Kapatma Ayarları) ---")]
    [Tooltip("End cap shape style applied to points marked as Round: RoundedCap (Yarım Daire), SquareCap (Kare)")]
    public RoadEndCapStyle endCapStyle = RoadEndCapStyle.RoundedCap;

    [Tooltip("Number of radial segments for curved half-circle end caps")]
    [Range(8, 36)]
    public int capSegments = 20;

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

        foreach (var b in branches)
        {
            if (b.roundCappedPointIndices == null)
            {
                b.roundCappedPointIndices = new List<int>();
            }
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
            roadMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/RoadStyles/Mat_Road_City_Sidewalks.mat");
            if (roadMaterial == null)
                roadMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/RoadStyles/Mat_Road_2Lane_Striped.mat");
            if (roadMaterial == null)
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

            // Shift cap indices
            if (branch.roundCappedPointIndices != null)
            {
                for (int i = 0; i < branch.roundCappedPointIndices.Count; i++)
                {
                    if (branch.roundCappedPointIndices[i] >= pointIdx)
                    {
                        branch.roundCappedPointIndices[i]++;
                    }
                }
            }

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

            // Remove/shift cap indices
            if (branch.roundCappedPointIndices != null)
            {
                branch.roundCappedPointIndices.Remove(pointIdx);
                for (int i = 0; i < branch.roundCappedPointIndices.Count; i++)
                {
                    if (branch.roundCappedPointIndices[i] > pointIdx)
                    {
                        branch.roundCappedPointIndices[i]--;
                    }
                }
            }

            selectedPointIndex = Mathf.Clamp(pointIdx - 1, 0, branch.waypoints.Count - 1);
            UpdateRoadAndTerrain();
        }
    }

    public void RemoveLastPointFromActiveBranch()
    {
        RoadBranch branch = GetActiveBranch();
        if (branch.waypoints.Count > 0)
        {
            int lastIdx = branch.waypoints.Count - 1;
            if (branch.roundCappedPointIndices != null) branch.roundCappedPointIndices.Remove(lastIdx);
            branch.waypoints.RemoveAt(lastIdx);
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
    /// Rebuilds the road mesh ribbon and caps endpoints that have Round Cap enabled.
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

        for (int b = 0; b < branches.Count; b++)
        {
            var branch = branches[b];
            if (branch.waypoints.Count < 2) continue;

            List<Vector3> samplePoints = GenerateSplineSamples(branch.waypoints, resolution);
            if (samplePoints.Count < 2) continue;

            bool isStartRound = branch.IsPointRoundCapped(0);
            bool isEndRound = branch.IsPointRoundCapped(branch.waypoints.Count - 1);

            // 1. START CAP (If start point is marked as Round)
            if (isStartRound && endCapStyle != RoadEndCapStyle.FlatOpen)
            {
                Vector3 forwardStart = (samplePoints[1] - samplePoints[0]).normalized;
                BuildCapGeometry(samplePoints[0], -forwardStart, 0f, true, allVertices, allNormals, allUvs, allTriangles, activeTerrain);
            }

            // 2. MAIN ROAD RIBBON
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

            // 3. END CAP (If end point is marked as Round)
            if (isEndRound && endCapStyle != RoadEndCapStyle.FlatOpen)
            {
                Vector3 forwardEnd = (samplePoints[samplePoints.Count - 1] - samplePoints[samplePoints.Count - 2]).normalized;
                BuildCapGeometry(samplePoints[samplePoints.Count - 1], forwardEnd, currentLength, false, allVertices, allNormals, allUvs, allTriangles, activeTerrain);
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
    /// Builds procedural 180-degree half-circle end cap geometry with continuous sidewalk and curb wrap.
    /// Uses a clean UV seam at the apex to guarantee zero gap/hole where the two sidewalks meet.
    /// </summary>
    private void BuildCapGeometry(
        Vector3 centerPt,
        Vector3 forwardDir,
        float baseLength,
        bool isStartCap,
        List<Vector3> allVertices,
        List<Vector3> allNormals,
        List<Vector2> allUvs,
        List<int> allTriangles,
        Terrain activeTerrain)
    {
        Vector3 rightDir = Vector3.Cross(Vector3.up, forwardDir).normalized;
        float halfWidth = roadWidth * 0.5f;
        float priorityOffset = renderPriority * 0.02f;

        // Sidewalk and curb thickness fractions
        float swFrac = 0.14f;
        float curbFrac = 0.16f;

        if (roadMaterial != null)
        {
            string matName = roadMaterial.name.ToLower();
            if (matName.Contains("wide"))
            {
                swFrac = 0.22f;
                curbFrac = 0.245f;
            }
            else if (matName.Contains("dirt"))
            {
                swFrac = 0.10f;
                curbFrac = 0.15f;
            }
            else if (matName.Contains("striped"))
            {
                swFrac = 0.06f;
                curbFrac = 0.08f;
            }
        }

        // =========================================================================
        // STYLE A: 180° HALF-CIRCLE ROUND CAP (Yarım Daire - Uçta Sıfır Boşluk)
        // =========================================================================
        if (endCapStyle == RoadEndCapStyle.RoundedCap)
        {
            float R = halfWidth;
            float swWidth = roadWidth * swFrac;
            float curbWidth = roadWidth * curbFrac;

            float r0 = R;                                     // Outer Sidewalk edge
            float r1 = Mathf.Max(0.2f, R - swWidth);         // Sidewalk / Curb boundary
            float r2 = Mathf.Max(0.1f, R - curbWidth);       // Curb / Asphalt boundary

            float[] radii = new float[] { r0, r1, r2 };
            float[] leftUOffsets = new float[] { 0.0f, swFrac, curbFrac };
            float[] rightUOffsets = new float[] { 1.0f, 1.0f - swFrac, 1.0f - curbFrac };

            int halfSegments = Mathf.Max(6, capSegments / 2);
            int ringCount = radii.Length;

            int AddCapVertex(Vector3 localP, float u, float v)
            {
                Vector3 worldP = transform.TransformPoint(localP);
                float splineY = transform.TransformPoint(centerPt).y + terrainOffset + priorityOffset;
                if (activeTerrain != null)
                {
                    float groundY = activeTerrain.SampleHeight(worldP) + activeTerrain.transform.position.y;
                    worldP.y = Mathf.Max(splineY, groundY + priorityOffset) + 0.04f;
                }
                else
                {
                    worldP.y = splineY + 0.04f;
                }

                int idx = allVertices.Count;
                allVertices.Add(transform.InverseTransformPoint(worldP));
                allNormals.Add(Vector3.up);
                allUvs.Add(new Vector2(u, v));
                return idx;
            }

            // Center vertex (Ring 3 - Asphalt center)
            int centerVIdx = AddCapVertex(centerPt, 0.5f, baseLength * uvTiling);

            // 1. LEFT HALF: Sweeps from -90° (left road edge) to 0° (front apex)
            int[,] leftVertIndices = new int[halfSegments + 1, ringCount];
            for (int s = 0; s <= halfSegments; s++)
            {
                float t = (float)s / halfSegments;
                float angle = -Mathf.PI * 0.5f + (t * Mathf.PI * 0.5f); // -90 deg to 0 deg
                Vector3 radialDir = (Mathf.Sin(angle) * rightDir + Mathf.Cos(angle) * forwardDir).normalized;

                for (int k = 0; k < ringCount; k++)
                {
                    Vector3 localP = centerPt + (radialDir * radii[k]);
                    float vOffset = Mathf.Cos(angle) * radii[k];
                    float vCoord = (isStartCap ? (-vOffset) : (baseLength + vOffset)) * uvTiling;

                    leftVertIndices[s, k] = AddCapVertex(localP, leftUOffsets[k], vCoord);
                }
            }

            // Generate Triangles for Left Half
            for (int s = 0; s < halfSegments; s++)
            {
                for (int k = 0; k < ringCount - 1; k++)
                {
                    int p_s_k = leftVertIndices[s, k];
                    int p_next_k = leftVertIndices[s + 1, k];
                    int p_s_nextK = leftVertIndices[s, k + 1];
                    int p_next_nextK = leftVertIndices[s + 1, k + 1];

                    allTriangles.Add(p_s_k);
                    allTriangles.Add(p_next_k);
                    allTriangles.Add(p_s_nextK);

                    allTriangles.Add(p_s_nextK);
                    allTriangles.Add(p_next_k);
                    allTriangles.Add(p_next_nextK);
                }

                int inner_s = leftVertIndices[s, ringCount - 1];
                int inner_next = leftVertIndices[s + 1, ringCount - 1];
                allTriangles.Add(inner_s);
                allTriangles.Add(inner_next);
                allTriangles.Add(centerVIdx);
            }

            // 2. RIGHT HALF: Sweeps from 0° (front apex) to +90° (right road edge)
            int[,] rightVertIndices = new int[halfSegments + 1, ringCount];
            for (int s = 0; s <= halfSegments; s++)
            {
                float t = (float)s / halfSegments;
                float angle = 0f + (t * Mathf.PI * 0.5f); // 0 deg to +90 deg
                Vector3 radialDir = (Mathf.Sin(angle) * rightDir + Mathf.Cos(angle) * forwardDir).normalized;

                for (int k = 0; k < ringCount; k++)
                {
                    Vector3 localP = centerPt + (radialDir * radii[k]);
                    float vOffset = Mathf.Cos(angle) * radii[k];
                    float vCoord = (isStartCap ? (-vOffset) : (baseLength + vOffset)) * uvTiling;

                    rightVertIndices[s, k] = AddCapVertex(localP, rightUOffsets[k], vCoord);
                }
            }

            // Generate Triangles for Right Half
            for (int s = 0; s < halfSegments; s++)
            {
                for (int k = 0; k < ringCount - 1; k++)
                {
                    int p_s_k = rightVertIndices[s, k];
                    int p_next_k = rightVertIndices[s + 1, k];
                    int p_s_nextK = rightVertIndices[s, k + 1];
                    int p_next_nextK = rightVertIndices[s + 1, k + 1];

                    allTriangles.Add(p_s_k);
                    allTriangles.Add(p_next_k);
                    allTriangles.Add(p_s_nextK);

                    allTriangles.Add(p_s_nextK);
                    allTriangles.Add(p_next_k);
                    allTriangles.Add(p_next_nextK);
                }

                int inner_s = rightVertIndices[s, ringCount - 1];
                int inner_next = rightVertIndices[s + 1, ringCount - 1];
                allTriangles.Add(inner_s);
                allTriangles.Add(inner_next);
                allTriangles.Add(centerVIdx);
            }
        }
        // ==========================================
        // STYLE B: SQUARE CAP (Düz Perpendicular Kapama)
        // ==========================================
        else if (endCapStyle == RoadEndCapStyle.SquareCap)
        {
            float swWidth = roadWidth * swFrac;
            float curbWidth = roadWidth * curbFrac;
            float extDepth = Mathf.Max(swWidth * 1.5f, 1.2f);

            // Corner positions in local space
            Vector3 leftBack = centerPt - (rightDir * halfWidth);
            Vector3 rightBack = centerPt + (rightDir * halfWidth);
            Vector3 leftFront = leftBack + (forwardDir * extDepth);
            Vector3 rightFront = rightBack + (forwardDir * extDepth);

            Vector3 leftBackInner = centerPt - (rightDir * (halfWidth - curbWidth));
            Vector3 rightBackInner = centerPt + (rightDir * (halfWidth - curbWidth));
            Vector3 leftFrontInner = leftBackInner + (forwardDir * (extDepth - curbWidth));
            Vector3 rightFrontInner = rightBackInner + (forwardDir * (extDepth - curbWidth));

            int AddCapVertex(Vector3 localPos, float u, float v)
            {
                Vector3 worldP = transform.TransformPoint(localPos);
                float splineY = transform.TransformPoint(centerPt).y + terrainOffset + priorityOffset;
                if (activeTerrain != null)
                {
                    float groundY = activeTerrain.SampleHeight(worldP) + activeTerrain.transform.position.y;
                    worldP.y = Mathf.Max(splineY, groundY + priorityOffset) + 0.04f;
                }
                else
                {
                    worldP.y = splineY + 0.04f;
                }
                int idx = allVertices.Count;
                allVertices.Add(transform.InverseTransformPoint(worldP));
                allNormals.Add(Vector3.up);
                allUvs.Add(new Vector2(u, v));
                return idx;
            }

            float vBase = baseLength * uvTiling;
            float vExt = (isStartCap ? (-extDepth) : (baseLength + extDepth)) * uvTiling;
            float vExtInner = (isStartCap ? (-extDepth + curbWidth) : (baseLength + extDepth - curbWidth)) * uvTiling;

            int vLB = AddCapVertex(leftBack, 0.0f, vBase);
            int vRB = AddCapVertex(rightBack, 1.0f, vBase);
            int vLF = AddCapVertex(leftFront, 0.0f, vExt);
            int vRF = AddCapVertex(rightFront, 1.0f, vExt);

            int vLBI = AddCapVertex(leftBackInner, curbFrac, vBase);
            int vRBI = AddCapVertex(rightBackInner, 1.0f - curbFrac, vBase);
            int vLFI = AddCapVertex(leftFrontInner, curbFrac, vExtInner);
            int vRFI = AddCapVertex(rightFrontInner, 1.0f - curbFrac, vExtInner);

            // Left sidewalk border
            allTriangles.Add(vLB); allTriangles.Add(vLF); allTriangles.Add(vLBI);
            allTriangles.Add(vLBI); allTriangles.Add(vLF); allTriangles.Add(vLFI);

            // Front sidewalk border
            allTriangles.Add(vLF); allTriangles.Add(vRF); allTriangles.Add(vLFI);
            allTriangles.Add(vLFI); allTriangles.Add(vRF); allTriangles.Add(vRFI);

            // Right sidewalk border
            allTriangles.Add(vRFI); allTriangles.Add(vRF); allTriangles.Add(vRBI);
            allTriangles.Add(vRBI); allTriangles.Add(vRF); allTriangles.Add(vRB);

            // Center Asphalt Interior
            allTriangles.Add(vLBI); allTriangles.Add(vLFI); allTriangles.Add(vRBI);
            allTriangles.Add(vRBI); allTriangles.Add(vLFI); allTriangles.Add(vRFI);
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

        for (int b = 0; b < branches.Count; b++)
        {
            var branch = branches[b];
            if (branch.waypoints.Count < 2) continue;
            List<Vector3> samples = GenerateSplineSamples(branch.waypoints, 1.0f);
            List<Vector3> worldPts = new List<Vector3>();

            // Include Start Cap in terrain deformation if marked as Round
            if (branch.IsPointRoundCapped(0) && endCapStyle != RoadEndCapStyle.FlatOpen && samples.Count >= 2)
            {
                Vector3 fwd = (samples[1] - samples[0]).normalized;
                Vector3 startCapLocal = samples[0] - (fwd * (roadWidth * 0.5f));
                worldPts.Add(transform.TransformPoint(startCapLocal));
            }

            foreach (var s in samples)
            {
                Vector3 w = transform.TransformPoint(s);
                worldPts.Add(w);
                if (w.x < minWX) minWX = w.x;
                if (w.x > maxWX) maxWX = w.x;
                if (w.z < minWZ) minWZ = w.z;
                if (w.z > maxWZ) maxWZ = w.z;
            }

            // Include End Cap in terrain deformation if marked as Round
            if (branch.IsPointRoundCapped(branch.waypoints.Count - 1) && endCapStyle != RoadEndCapStyle.FlatOpen && samples.Count >= 2)
            {
                Vector3 fwd = (samples[samples.Count - 1] - samples[samples.Count - 2]).normalized;
                Vector3 endCapLocal = samples[samples.Count - 1] + (fwd * (roadWidth * 0.5f));
                Vector3 w = transform.TransformPoint(endCapLocal);
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
