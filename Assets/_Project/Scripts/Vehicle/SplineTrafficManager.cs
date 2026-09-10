using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// High-performance Manager that spawns, manages, and recycles AI traffic vehicles
/// along Spline Roads within a dynamic proximity bubble around the player.
/// Supports two-way lane traffic, road filtering, and zero-allocation object pooling.
/// </summary>
public class SplineTrafficManager : MonoBehaviour
{
    public static SplineTrafficManager Instance { get; private set; }

    [System.Serializable]
    public class RoadJunctionLink
    {
        public TrafficPathBranch targetBranch;
        public float targetDistanceAlongPath;
        public bool targetIsReverse;
        public string connectionDescription;
    }

    [System.Serializable]
    public class MidBranchTurnoff
    {
        public float triggerDistanceAlongPath;
        public TrafficPathBranch targetBranch;
        public float entryDistanceAlongTarget;
        public bool targetIsReverse;
        public string description;
    }

    [System.Serializable]
    public class TrafficPathBranch
    {
        public SplineRoadBuilder roadBuilder;
        public int branchIndex;
        public string branchName;
        public float roadWidth;
        public List<Vector3> worldPoints = new List<Vector3>();
        public List<float> accumulatedDistances = new List<float>();
        public float totalLength = 0f;

        // Junction links from the END of this branch (when traveling forward)
        public List<RoadJunctionLink> forwardEndJunctions = new List<RoadJunctionLink>();

        // Junction links from the START of this branch (when traveling in reverse)
        public List<RoadJunctionLink> reverseEndJunctions = new List<RoadJunctionLink>();

        // Mid-road side turnoffs for cars on this road to turn into side streets
        public List<MidBranchTurnoff> forwardMidTurnoffs = new List<MidBranchTurnoff>();
        public List<MidBranchTurnoff> reverseMidTurnoffs = new List<MidBranchTurnoff>();

        public bool EvaluatePositionAndForward(float dist, bool isReverse, float laneOffset, out Vector3 pos, out Vector3 forward)
        {
            pos = Vector3.zero;
            forward = Vector3.forward;
            if (worldPoints.Count < 2 || accumulatedDistances.Count != worldPoints.Count) return false;

            float targetDist = isReverse ? (totalLength - dist) : dist;
            targetDist = Mathf.Clamp(targetDist, 0f, totalLength);

            // Binary search for the segment
            int low = 0;
            int high = accumulatedDistances.Count - 1;
            while (low <= high)
            {
                int mid = (low + high) / 2;
                if (accumulatedDistances[mid] < targetDist)
                    low = mid + 1;
                else
                    high = mid - 1;
            }

            int idx1 = Mathf.Clamp(low, 1, worldPoints.Count - 1);
            int idx0 = idx1 - 1;

            float d0 = accumulatedDistances[idx0];
            float d1 = accumulatedDistances[idx1];
            float segLen = Mathf.Max(0.001f, d1 - d0);
            float t = Mathf.Clamp01((targetDist - d0) / segLen);

            Vector3 p0 = worldPoints[idx0];
            Vector3 p1 = worldPoints[idx1];
            Vector3 centerPos = Vector3.Lerp(p0, p1, t);

            // Smooth C1 Continuous Tangents (Eliminates angular snapping and jitter across segment boundaries)
            int prevIdx = Mathf.Max(0, idx0 - 1);
            int nextIdx = Mathf.Min(worldPoints.Count - 1, idx1 + 1);

            Vector3 tan0 = (p1 - worldPoints[prevIdx]).normalized;
            if (tan0.sqrMagnitude < 0.001f) tan0 = (p1 - p0).normalized;

            Vector3 tan1 = (worldPoints[nextIdx] - p0).normalized;
            if (tan1.sqrMagnitude < 0.001f) tan1 = (p1 - p0).normalized;

            Vector3 segForward = Vector3.Slerp(tan0, tan1, t).normalized;
            if (segForward.sqrMagnitude < 0.001f) segForward = (p1 - p0).normalized;
            if (segForward.sqrMagnitude < 0.001f) segForward = Vector3.forward;

            Vector3 right = Vector3.Cross(Vector3.up, segForward).normalized;

            // Forward lane: offset right (+right * laneOffset), faces forward
            // Reverse lane: offset left (-right * laneOffset), faces -forward (opposite direction)
            if (!isReverse)
            {
                pos = centerPos + (right * laneOffset);
                forward = segForward;
            }
            else
            {
                pos = centerPos - (right * laneOffset);
                forward = -segForward;
            }

            return true;
        }
    }

    [Header("--- VEHICLE PREFABS ---")]
    [Tooltip("List of vehicle prefabs to spawn in traffic (Cars, SUVs, Taxis, Trucks, Pickups, Buses)")]
    public List<GameObject> vehiclePrefabs = new List<GameObject>();

    [Header("--- TRAFFIC DENSITY & LIMITS ---")]
    [Tooltip("Maximum active traffic vehicles allowed around the player at one time")]
    [Range(5, 150)]
    public int maxActiveVehicles = 40;

    [Tooltip("Speed range for spawned traffic vehicles in m/s (e.g. 8 m/s = 28 km/h, 15 m/s = 54 km/h)")]
    public Vector2 speedRange = new Vector2(8.0f, 15.0f);

    [Header("--- PLAYER PROXIMITY SPAWN & DESPAWN ---")]
    [Tooltip("Minimum distance from player to spawn a vehicle (prevents sudden pop-in right in front of player)")]
    public float minSpawnDistance = 35.0f;

    [Tooltip("Maximum distance from player to spawn a vehicle")]
    public float maxSpawnDistance = 280.0f;

    [Tooltip("Distance at which vehicles outside player vision are recycled into the pool")]
    public float despawnDistance = 350.0f;

    [Tooltip("How frequently the manager attempts to spawn new vehicles (seconds)")]
    public float spawnInterval = 0.3f;

    [Tooltip("Minimum distance required between two vehicles on the same road")]
    public float minCarSpacing = 16.0f;

    [Header("--- JUNCTION & TURNING BEHAVIOR ---")]
    [Tooltip("Probability (0 to 1) that vehicles on a main road will randomly turn into an intersecting side road (default: 0.35 = 35%)")]
    [Range(0f, 1f)]
    public float sideRoadTurnChance = 0.35f;

    [Header("--- ROAD FILTERING ---")]
    [Tooltip("Include all SplineRoadBuilder instances found in scene automatically")]
    public bool autoFindSceneRoads = true;

    [Tooltip("Specific Spline Roads to include (if autoFindSceneRoads is false)")]
    public List<SplineRoadBuilder> customRoads = new List<SplineRoadBuilder>();

    [Tooltip("Allow traffic on Pure Sidewalk / Pedestrian Plaza roads (usually false)")]
    public bool allowPureSidewalkTraffic = false;

    [Header("--- RUNTIME MONITORING ---")]
    [SerializeField] private int activeVehicleCount = 0;
    [SerializeField] private int totalRoadBuildersDetected = 0;
    [SerializeField] private int totalRoadBranches = 0;

    private List<TrafficPathBranch> trafficBranches = new List<TrafficPathBranch>();
    private List<AITrafficVehicle> activeVehicles = new List<AITrafficVehicle>();
    public IReadOnlyList<AITrafficVehicle> ActiveVehicles => activeVehicles;
    private List<AITrafficVehicle> vehiclePool = new List<AITrafficVehicle>();
    private Transform poolContainer;
    private float nextSpawnTime = 0f;
    private float nextRoadRescanTime = 0f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoEnsureTrafficManager()
    {
        if (Instance == null && Object.FindAnyObjectByType<SplineTrafficManager>() == null)
        {
            SplineRoadBuilder[] roads = Object.FindObjectsByType<SplineRoadBuilder>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (roads != null && roads.Length > 0)
            {
                GameObject managerObj = new GameObject("[TRAFFIC_SYSTEM_AUTO]");
                managerObj.AddComponent<SplineTrafficManager>();
                Debug.Log("[SplineTrafficManager] Auto-created Traffic Manager at runtime for detected Spline Roads.");
            }
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        GameObject container = new GameObject("--- Traffic Vehicle Pool ---");
        container.transform.SetParent(transform);
        poolContainer = container.transform;

#if UNITY_EDITOR
        if (vehiclePrefabs == null || vehiclePrefabs.Count == 0)
        {
            AutoPopulatePrefabsInEditor();
        }
#endif

        BuildTrafficPaths();
    }

    private void Start()
    {
        if (trafficBranches.Count == 0)
        {
            BuildTrafficPaths();
        }

        // Initial traffic population burst around player
        Vector3 pPos = GetPlayerOrCameraPosition();
        for (int i = 0; i < 15; i++)
        {
            if (activeVehicles.Count >= maxActiveVehicles) break;
            TrySpawnTrafficVehicle(pPos);
        }
    }

#if UNITY_EDITOR
    private void AutoPopulatePrefabsInEditor()
    {
        string[] folders = new string[]
        {
            "Assets/_AssetPacks/SimplePoly City - Low Poly Assets/Prefab/Vehicles/Vehicle with Separated Wheels",
            "Assets/_AssetPacks/SimplePoly City - Low Poly Assets/Prefab/Vehicles/Vehicle with Static Wheels"
        };
        foreach (var f in folders)
        {
            if (!System.IO.Directory.Exists(f)) continue;
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new string[] { f });
            foreach (var g in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (p != null && !vehiclePrefabs.Contains(p))
                {
                    vehiclePrefabs.Add(p);
                }
            }
        }
    }
#endif

    /// <summary>
    /// Scans scene roads and builds metric sampled spline paths for high-performance traffic routing.
    /// </summary>
    [ContextMenu("Rebuild Traffic Paths")]
    public void BuildTrafficPaths()
    {
        trafficBranches.Clear();

        List<SplineRoadBuilder> roadsToScan = new List<SplineRoadBuilder>();
        if (autoFindSceneRoads)
        {
            roadsToScan.AddRange(FindObjectsByType<SplineRoadBuilder>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
        }
        else
        {
            roadsToScan.AddRange(customRoads);
        }

        totalRoadBuildersDetected = roadsToScan.Count;

        foreach (var road in roadsToScan)
        {
            if (road == null || road.branches == null) continue;

            // Check if road is pure sidewalk/pedestrian
            if (!allowPureSidewalkTraffic && road.roadMaterial != null)
            {
                string matName = road.roadMaterial.name.ToLower();
                if (matName.Contains("pure_sidewalk") || matName.Contains("sidewalk_only") || matName.Contains("plaza") || matName.Contains("paver"))
                {
                    continue; // Skip pure sidewalk roads
                }
            }

            for (int b = 0; b < road.branches.Count; b++)
            {
                var branch = road.branches[b];
                if (branch.waypoints == null || branch.waypoints.Count < 2) continue;

                TrafficPathBranch tBranch = new TrafficPathBranch();
                tBranch.roadBuilder = road;
                tBranch.branchIndex = b;
                tBranch.branchName = $"{road.gameObject.name} - {branch.branchName}";
                tBranch.roadWidth = road.roadWidth;

                // Sample points smoothly along branch (1.2m interval)
                List<Vector3> samples = SampleSplineBranch(road, branch.waypoints, 1.2f);
                if (samples.Count < 2) continue;

                tBranch.worldPoints = samples;

                float accDist = 0f;
                tBranch.accumulatedDistances.Add(0f);
                for (int i = 1; i < samples.Count; i++)
                {
                    accDist += Vector3.Distance(samples[i], samples[i - 1]);
                    tBranch.accumulatedDistances.Add(accDist);
                }
                tBranch.totalLength = accDist;

                // Include all drivable road branches (minimum 10m)
                if (tBranch.totalLength >= 10.0f)
                {
                    trafficBranches.Add(tBranch);
                }
            }
        }

        // Build Junction & Intersection Network across all branches
        BuildJunctionConnections();

        totalRoadBranches = trafficBranches.Count;
        Debug.Log($"[SplineTrafficManager] Initialized {trafficBranches.Count} drivable traffic branches across {roadsToScan.Count} road builders.");
    }

    /// <summary>
    /// Scans branch endpoints and connects intersecting roads (T-junctions, cross-sections, continuous ends).
    /// </summary>
    private void BuildJunctionConnections()
    {
        for (int a = 0; a < trafficBranches.Count; a++)
        {
            var branchA = trafficBranches[a];
            branchA.forwardEndJunctions.Clear();
            branchA.reverseEndJunctions.Clear();
            branchA.forwardMidTurnoffs.Clear();
            branchA.reverseMidTurnoffs.Clear();
        }

        for (int a = 0; a < trafficBranches.Count; a++)
        {
            var branchA = trafficBranches[a];
            if (branchA.worldPoints.Count < 2) continue;

            Vector3 endAWorld = branchA.worldPoints[branchA.worldPoints.Count - 1];
            Vector3 startAWorld = branchA.worldPoints[0];

            for (int b = 0; b < trafficBranches.Count; b++)
            {
                var branchB = trafficBranches[b];
                if (branchB.worldPoints.Count < 2 || branchA == branchB) continue;

                // 1. Check connections for branchA Forward End (endAWorld)
                FindAndAddJunctionLinks(branchA, endAWorld, branchB, true);

                // 2. Check connections for branchA Reverse End (startAWorld)
                FindAndAddJunctionLinks(branchA, startAWorld, branchB, false);
            }
        }
    }

    private void FindAndAddJunctionLinks(TrafficPathBranch sourceBranch, Vector3 exitPoint, TrafficPathBranch targetBranch, bool isForwardEnd)
    {
        float junctionThreshold = (sourceBranch.roadWidth + targetBranch.roadWidth) * 0.5f + 4.5f;
        float junctionThresholdSqr = junctionThreshold * junctionThreshold;

        // Find closest point on targetBranch to exitPoint
        float closestDistSqr = float.MaxValue;
        int closestIdx = -1;

        for (int i = 0; i < targetBranch.worldPoints.Count; i++)
        {
            float dSqr = (targetBranch.worldPoints[i] - exitPoint).sqrMagnitude;
            if (dSqr < closestDistSqr)
            {
                closestDistSqr = dSqr;
                closestIdx = i;
            }
        }

        if (closestDistSqr <= junctionThresholdSqr && closestIdx >= 0)
        {
            float distAlongTarget = targetBranch.accumulatedDistances[closestIdx];
            if (sourceBranch == targetBranch) return;

            var list = isForwardEnd ? sourceBranch.forwardEndJunctions : sourceBranch.reverseEndJunctions;

            // 1. SIDE ROAD -> MAIN ROAD TRANSITION
            // Entry point on target branch offset in travel direction for smooth corner cutting
            float entryOffsetOnTarget = Mathf.Clamp(sourceBranch.roadWidth * 0.5f + targetBranch.roadWidth * 0.4f + 3.0f, 3.5f, 9.0f);

            // Option A: Join target branch in Forward direction
            if (distAlongTarget < targetBranch.totalLength - 2.0f)
            {
                float forwardEntryDist = Mathf.Clamp(distAlongTarget + entryOffsetOnTarget, 0.5f, targetBranch.totalLength - 0.5f);
                list.Add(new RoadJunctionLink
                {
                    targetBranch = targetBranch,
                    targetDistanceAlongPath = forwardEntryDist,
                    targetIsReverse = false,
                    connectionDescription = $"➔ {targetBranch.branchName} (Forward)"
                });
            }

            // Option B: Join target branch in Reverse direction
            if (distAlongTarget > 2.0f)
            {
                float reverseEntryDist = Mathf.Clamp((targetBranch.totalLength - distAlongTarget) + entryOffsetOnTarget, 0.5f, targetBranch.totalLength - 0.5f);
                list.Add(new RoadJunctionLink
                {
                    targetBranch = targetBranch,
                    targetDistanceAlongPath = reverseEntryDist,
                    targetIsReverse = true,
                    connectionDescription = $"➔ {targetBranch.branchName} (Reverse)"
                });
            }

            // 2. MAIN ROAD -> SIDE ROAD MID-PATH TURNOFFS (EARLY TURN INITIATION)
            float earlyTurnDistance = Mathf.Clamp(targetBranch.roadWidth * 0.5f + sourceBranch.roadWidth * 0.5f + 2.5f, 4.0f, 8.5f);
            float sideEntryDist = Mathf.Clamp(sourceBranch.roadWidth * 0.5f + 3.0f, 1.5f, sourceBranch.totalLength - 1.0f);
            bool sideIsReverse = isForwardEnd;

            // Turnoff when traveling Forward on Main Road (Triggers early before intersection):
            float forwardTriggerDist = Mathf.Max(0.5f, distAlongTarget - earlyTurnDistance);
            targetBranch.forwardMidTurnoffs.Add(new MidBranchTurnoff
            {
                triggerDistanceAlongPath = forwardTriggerDist,
                targetBranch = sourceBranch,
                entryDistanceAlongTarget = sideEntryDist,
                targetIsReverse = sideIsReverse,
                description = $"Turn off into ➔ {sourceBranch.branchName}"
            });

            // Turnoff when traveling Reverse on Main Road (Triggers early before intersection):
            float reverseTriggerDist = Mathf.Max(0.5f, (targetBranch.totalLength - distAlongTarget) - earlyTurnDistance);
            targetBranch.reverseMidTurnoffs.Add(new MidBranchTurnoff
            {
                triggerDistanceAlongPath = reverseTriggerDist,
                targetBranch = sourceBranch,
                entryDistanceAlongTarget = sideEntryDist,
                targetIsReverse = sideIsReverse,
                description = $"Turn off into ➔ {sourceBranch.branchName}"
            });
        }
    }

    /// <summary>
    /// Finds a connecting road branch at a junction/intersection when a vehicle reaches the end of its path.
    /// </summary>
    public bool TryGetNextJunctionPath(
        TrafficPathBranch currentBranch,
        bool isReverseLane,
        out TrafficPathBranch nextBranch,
        out float nextDistance,
        out bool nextReverse)
    {
        nextBranch = null;
        nextDistance = 0f;
        nextReverse = false;

        if (currentBranch == null) return false;

        var junctionList = isReverseLane ? currentBranch.reverseEndJunctions : currentBranch.forwardEndJunctions;
        if (junctionList != null && junctionList.Count > 0)
        {
            // Pick a random junction connection (turn left, turn right, go straight)
            RoadJunctionLink pickedLink = junctionList[Random.Range(0, junctionList.Count)];
            if (pickedLink.targetBranch != null)
            {
                nextBranch = pickedLink.targetBranch;
                nextDistance = pickedLink.targetDistanceAlongPath;
                nextReverse = pickedLink.targetIsReverse;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a vehicle driving on a main road passes an intersection/side road turnoff and rolls a random chance to turn into it.
    /// </summary>
    public bool TryTakeMidBranchTurnoff(
        TrafficPathBranch currentBranch,
        float prevDistance,
        float currDistance,
        bool isReverseLane,
        float turnProbability,
        out TrafficPathBranch nextBranch,
        out float nextDistance,
        out bool nextReverse)
    {
        nextBranch = null;
        nextDistance = 0f;
        nextReverse = false;

        if (currentBranch == null) return false;

        var turnoffList = isReverseLane ? currentBranch.reverseMidTurnoffs : currentBranch.forwardMidTurnoffs;
        if (turnoffList == null || turnoffList.Count == 0) return false;

        for (int i = 0; i < turnoffList.Count; i++)
        {
            var turnoff = turnoffList[i];
            // Check if vehicle traversed across this turnoff's trigger distance during this step
            if (prevDistance < turnoff.triggerDistanceAlongPath && currDistance >= turnoff.triggerDistanceAlongPath)
            {
                // Roll random chance to take the side street turnoff
                if (Random.value < turnProbability)
                {
                    nextBranch = turnoff.targetBranch;
                    nextDistance = turnoff.entryDistanceAlongTarget;
                    nextReverse = turnoff.targetIsReverse;
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Checks for leading vehicles on the same lane or directly ahead in physical space
    /// to guarantee collision-free following without tailgating or overlapping.
    /// </summary>
    public float GetDistanceToLeadingVehicle(
        AITrafficVehicle queryingVehicle,
        TrafficPathBranch branch,
        bool isReverse,
        float currentDist,
        float maxScanDistance)
    {
        float closestDist = maxScanDistance;
        if (queryingVehicle == null) return closestDist;

        Vector3 queryingPos = queryingVehicle.transform.position;
        Vector3 queryingForward = queryingVehicle.transform.forward;

        for (int i = 0; i < activeVehicles.Count; i++)
        {
            var other = activeVehicles[i];
            if (other == null || other == queryingVehicle) continue;

            // 1. Same branch and same lane check (accurate along curved spline)
            if (other.CurrentBranch == branch && other.isReverseLane == isReverse)
            {
                float distAlong = other.currentDistanceAlongPath - currentDist;
                if (distAlong > 0.05f && distAlong < closestDist)
                {
                    closestDist = distAlong;
                }
            }
            else if (other.CurrentBranch != branch)
            {
                // 2. Physical 3D distance check ONLY for vehicles on DIFFERENT branches (crossroads/junctions)
                Vector3 toOther = other.transform.position - queryingPos;
                float sqrDist = toOther.sqrMagnitude;
                if (sqrDist < maxScanDistance * maxScanDistance && sqrDist > 0.01f)
                {
                    // Check if other vehicle is in front (forward cone ~50 degrees)
                    if (Vector3.Dot(toOther.normalized, queryingForward) > 0.65f)
                    {
                        float d = Mathf.Sqrt(sqrDist);
                        if (d < closestDist)
                        {
                            closestDist = d;
                        }
                    }
                }
            }
        }

        // 3. Player Proximity Check
        Vector3 playerPos = GetPlayerOrCameraPosition();
        Vector3 toPlayer = playerPos - queryingPos;
        float sqrPlayerDist = toPlayer.sqrMagnitude;
        if (sqrPlayerDist < maxScanDistance * maxScanDistance && sqrPlayerDist > 0.01f)
        {
            if (Vector3.Dot(toPlayer.normalized, queryingForward) > 0.35f)
            {
                float d = Mathf.Sqrt(sqrPlayerDist);
                if (d < closestDist)
                {
                    closestDist = d;
                }
            }
        }

        return closestDist;
    }

    private List<Vector3> SampleSplineBranch(SplineRoadBuilder road, List<Vector3> localPts, float stepSize)
    {
        List<Vector3> result = new List<Vector3>();
        if (localPts.Count < 2) return result;

        for (int i = 0; i < localPts.Count - 1; i++)
        {
            Vector3 p0 = i > 0 ? localPts[i - 1] : (localPts[0] - (localPts[1] - localPts[0]));
            Vector3 p1 = localPts[i];
            Vector3 p2 = localPts[i + 1];
            Vector3 p3 = (i + 2 < localPts.Count) ? localPts[i + 2] : (localPts[localPts.Count - 1] + (localPts[localPts.Count - 1] - localPts[localPts.Count - 2]));

            float segmentDistance = Vector3.Distance(p1, p2);
            int divisions = Mathf.Max(2, Mathf.CeilToInt(segmentDistance / stepSize));

            for (int d = 0; d < divisions; d++)
            {
                float t = (float)d / divisions;
                Vector3 localSample = GetCatmullRomPosition(t, p0, p1, p2, p3);
                result.Add(road.transform.TransformPoint(localSample));
            }
        }

        if (localPts.Count > 0)
        {
            result.Add(road.transform.TransformPoint(localPts[localPts.Count - 1]));
        }

        return result;
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

    private void Update()
    {
        Vector3 playerPos = GetPlayerOrCameraPosition();

        // 1. DESPAWN FARAWAY VEHICLES
        for (int i = activeVehicles.Count - 1; i >= 0; i--)
        {
            var vehicle = activeVehicles[i];
            if (vehicle == null)
            {
                activeVehicles.RemoveAt(i);
                continue;
            }

            float distToPlayer = Vector3.Distance(vehicle.transform.position, playerPos);
            if (distToPlayer > despawnDistance)
            {
                DespawnVehicle(vehicle);
            }
        }

        activeVehicleCount = activeVehicles.Count;

        // 2. PERIODIC ROAD RESCAN (Catches newly drawn roads during edit/play)
        if (Time.time >= nextRoadRescanTime)
        {
            nextRoadRescanTime = Time.time + 4.0f;
            int currentRoadsInScene = FindObjectsByType<SplineRoadBuilder>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
            if (currentRoadsInScene != totalRoadBuildersDetected)
            {
                BuildTrafficPaths();
            }
        }

        // 3. PERIODIC SPAWN CHECK
        if (Time.time >= nextSpawnTime)
        {
            nextSpawnTime = Time.time + spawnInterval;
            if (activeVehicles.Count < maxActiveVehicles && trafficBranches.Count > 0 && vehiclePrefabs.Count > 0)
            {
                TrySpawnTrafficVehicle(playerPos);
            }
        }
    }

    private Vector3 GetPlayerOrCameraPosition()
    {
        if (FPSPlayerController.Instance != null)
        {
            return FPSPlayerController.Instance.transform.position;
        }

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            return mainCam.transform.position;
        }

        return transform.position;
    }

    private struct SpawnCandidate
    {
        public TrafficPathBranch branch;
        public float distanceAlongPath;
    }

    private List<SpawnCandidate> spawnCandidatesBuffer = new List<SpawnCandidate>();

    /// <summary>
    /// Finds candidate road points in proximity to the player and spawns a vehicle.
    /// Works automatically across multiple spline roads in the scene.
    /// </summary>
    private void TrySpawnTrafficVehicle(Vector3 playerPos)
    {
        spawnCandidatesBuffer.Clear();

        for (int b = 0; b < trafficBranches.Count; b++)
        {
            var branch = trafficBranches[b];
            if (branch.worldPoints.Count < 2) continue;

            for (int i = 0; i < branch.worldPoints.Count; i += 2)
            {
                float d = Vector3.Distance(branch.worldPoints[i], playerPos);
                if (d >= minSpawnDistance && d <= maxSpawnDistance)
                {
                    spawnCandidatesBuffer.Add(new SpawnCandidate
                    {
                        branch = branch,
                        distanceAlongPath = branch.accumulatedDistances[i]
                    });
                }
            }
        }

        if (spawnCandidatesBuffer.Count == 0) return;

        // Try picking from valid candidate points
        int tries = Mathf.Min(10, spawnCandidatesBuffer.Count);
        for (int t = 0; t < tries; t++)
        {
            int randIdx = Random.Range(0, spawnCandidatesBuffer.Count);
            SpawnCandidate candidate = spawnCandidatesBuffer[randIdx];

            bool isReverse = (Random.value > 0.5f);
            float laneOffset = candidate.branch.roadWidth * 0.25f;

            Vector3 candidatePos, candidateForward;
            if (candidate.branch.EvaluatePositionAndForward(candidate.distanceAlongPath, isReverse, laneOffset, out candidatePos, out candidateForward))
            {
                if (IsPositionClearFromOtherVehicles(candidate.branch, candidate.distanceAlongPath, isReverse, candidatePos, minCarSpacing))
                {
                    SpawnVehicleAt(candidate.branch, candidate.distanceAlongPath, isReverse);
                    return;
                }
            }
        }
    }

    private bool IsPositionClearFromOtherVehicles(TrafficPathBranch branch, float distanceAlongPath, bool isReverse, Vector3 pos, float minSpacing)
    {
        // 1. Distance check against all active traffic vehicles
        float sqrSpacing = minSpacing * minSpacing;
        float sameLaneSpacing = minSpacing * 1.6f;
        float sqrSameLaneSpacing = sameLaneSpacing * sameLaneSpacing;

        for (int i = 0; i < activeVehicles.Count; i++)
        {
            var v = activeVehicles[i];
            if (v != null)
            {
                // Enforce extra safety distance if cars share the same lane
                if (v.isReverseLane == isReverse && (v.transform.position - pos).sqrMagnitude < sqrSameLaneSpacing)
                {
                    return false;
                }

                if ((v.transform.position - pos).sqrMagnitude < sqrSpacing)
                {
                    return false;
                }
            }
        }

        // 2. Physical 3D overlap check: ensure no player car, traffic car, or physics obstacle exists at spawn spot
        Collider[] hits = Physics.OverlapSphere(pos + (Vector3.up * 0.8f), 2.5f, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i];
            if (col == null) continue;
            if (col.GetComponentInParent<SplineRoadBuilder>() != null) continue;
            if (col.GetComponent<TerrainCollider>() != null) continue;
            if (col is MeshCollider meshCol && meshCol.sharedMesh != null && meshCol.sharedMesh.name.Contains("Road")) continue;

            // Found an active collider/vehicle at this exact location
            return false;
        }

        return true;
    }

    private void SpawnVehicleAt(TrafficPathBranch branch, float startDist, bool isReverse)
    {
        AITrafficVehicle vehicle = GetOrCreateVehicleFromPool();
        if (vehicle == null) return;

        float randomSpeed = Random.Range(speedRange.x, speedRange.y);
        vehicle.sideRoadTurnChance = sideRoadTurnChance;
        vehicle.Initialize(this, branch, startDist, isReverse, randomSpeed);

        activeVehicles.Add(vehicle);
    }

    private AITrafficVehicle GetOrCreateVehicleFromPool()
    {
        if (vehiclePool.Count > 0)
        {
            int lastIdx = vehiclePool.Count - 1;
            AITrafficVehicle pooled = vehiclePool[lastIdx];
            vehiclePool.RemoveAt(lastIdx);
            if (pooled != null) return pooled;
        }

        // Instantiate new vehicle prefab
        if (vehiclePrefabs.Count == 0) return null;
        GameObject prefab = vehiclePrefabs[Random.Range(0, vehiclePrefabs.Count)];
        if (prefab == null) return null;

        GameObject instance = Instantiate(prefab, poolContainer);
        instance.name = $"Traffic_{prefab.name}";

        AITrafficVehicle vehicleComp = instance.GetComponent<AITrafficVehicle>();
        if (vehicleComp == null)
        {
            vehicleComp = instance.AddComponent<AITrafficVehicle>();
        }

        return vehicleComp;
    }

    /// <summary>
    /// Recycles an active vehicle back into the pool.
    /// </summary>
    public void DespawnVehicle(AITrafficVehicle vehicle)
    {
        if (vehicle == null) return;

        activeVehicles.Remove(vehicle);
        vehicle.gameObject.SetActive(false);
        if (poolContainer != null) vehicle.transform.SetParent(poolContainer);

        if (!vehiclePool.Contains(vehicle))
        {
            vehiclePool.Add(vehicle);
        }
    }

    /// <summary>
    /// Despawns all active vehicles and returns them to the pool.
    /// </summary>
    public void ClearAllActiveVehicles()
    {
        for (int i = activeVehicles.Count - 1; i >= 0; i--)
        {
            DespawnVehicle(activeVehicles[i]);
        }
        activeVehicles.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 pPos = GetPlayerOrCameraPosition();

        // Spawn Bubble Gizmo
        Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.4f);
        Gizmos.DrawWireSphere(pPos, minSpawnDistance);

        Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.3f);
        Gizmos.DrawWireSphere(pPos, maxSpawnDistance);

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.2f);
        Gizmos.DrawWireSphere(pPos, despawnDistance);

        // Draw Traffic Branches & Junction Links
        if (trafficBranches != null)
        {
            foreach (var b in trafficBranches)
            {
                if (b.worldPoints != null && b.worldPoints.Count > 1)
                {
                    Gizmos.color = new Color(0f, 0.8f, 1f, 0.7f);
                    for (int i = 0; i < b.worldPoints.Count - 1; i++)
                    {
                        Gizmos.DrawLine(b.worldPoints[i], b.worldPoints[i + 1]);
                    }

                    // Draw Junction Connection Links
                    if (b.forwardEndJunctions != null && b.forwardEndJunctions.Count > 0)
                    {
                        Gizmos.color = Color.yellow;
                        Vector3 endPt = b.worldPoints[b.worldPoints.Count - 1];
                        Gizmos.DrawWireSphere(endPt, 1.2f);
                    }

                    if (b.reverseEndJunctions != null && b.reverseEndJunctions.Count > 0)
                    {
                        Gizmos.color = Color.green;
                        Vector3 startPt = b.worldPoints[0];
                        Gizmos.DrawWireSphere(startPt, 1.2f);
                    }
                }
            }
        }
    }
}
