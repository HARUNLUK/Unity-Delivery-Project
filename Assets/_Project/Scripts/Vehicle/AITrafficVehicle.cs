using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Autonomous AI Traffic Vehicle that follows Spline Road lanes (forward or reverse),
/// maintains collision avoidance radar with smooth braking, conforms to road/terrain slope,
/// and rotates wheel meshes dynamically based on speed.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class AITrafficVehicle : MonoBehaviour
{
    [Header("--- SPEED & ACCELERATION ---")]
    [Tooltip("Target cruising speed in m/s (e.g. 10 m/s = 36 km/h)")]
    public float cruiseSpeed = 11.0f;

    [Tooltip("Acceleration rate (m/s^2)")]
    public float acceleration = 6.0f;

    [Tooltip("Braking deceleration rate (m/s^2)")]
    public float brakeDeceleration = 18.0f;

    [Header("--- RADAR & COLLISION AVOIDANCE (RAYCAST LASER) ---")]
    [Tooltip("Forward sensor raycast distance for detecting obstacles / other vehicles")]
    public float sensorDistance = 18.0f;

    [Tooltip("Distance at which the vehicle begins to brake")]
    public float slowDistance = 12.0f;

    [Tooltip("Distance at which the vehicle comes to a complete stop")]
    public float stopDistance = 5.5f;

    [Tooltip("Minimum hard safety distance where vehicle speed is clamped to zero to prevent any physical overlap/clipping")]
    public float hardStopDistance = 3.8f;

    [Tooltip("Lateral safety corridor margin added to vehicle half-width for collision detection (meters)")]
    public float lateralSafetyMargin = 0.22f;

    [Tooltip("Lateral spacing between left and right headlight rays")]
    public float raySpreadWidth = 0.75f;

    [Tooltip("Height offset of the radar origin from vehicle base")]
    public float sensorHeight = 0.75f;

    [Tooltip("Layers checked for collision avoidance (Player, Vehicles, Obstacles)")]
    public LayerMask obstacleLayers = ~0;

    [Header("--- ROAD & LANE TRACKING ---")]
    [Tooltip("Offset distance from the road center line to the middle of the lane")]
    public float laneOffset = 2.0f;

    [Tooltip("Maximum steering rotation speed in degrees per second (smooth cornering)")]
    public float maxSteerAngleSpeed = 160.0f;

    [Tooltip("Height offset from ground surface")]
    public float groundOffset = 0.05f;

    [Header("--- WHEEL ANIMATION ---")]
    [Tooltip("Child transforms representing wheels that will rotate with speed")]
    public List<Transform> wheelTransforms = new List<Transform>();

    [Tooltip("Wheel radius in meters used for calculating rotational speed")]
    public float wheelRadius = 0.35f;

    [Header("--- POST-COLLISION & ANTI-STUCK ---")]
    [Tooltip("Duration to pause and wait after colliding with the player (seconds)")]
    public float playerCollisionPauseDuration = 3.5f;

    [Tooltip("Maximum seconds an AI car can remain motionless before auto-despawning (when player is NOT in front)")]
    public float maxStuckDuration = 10.0f;

    [Header("--- JUNCTION & TURNING BEHAVIOR ---")]
    [Tooltip("Probability (0 to 1) of turning into an intersecting side road when passing one on the main road")]
    [Range(0f, 1f)]
    public float sideRoadTurnChance = 0.35f;

    [Header("--- RUNTIME STATUS (READ ONLY) ---")]
    public float currentSpeed = 0f;
    public float targetSpeed = 0f;
    public float currentDistanceAlongPath = 0f;
    public float closestObstacleDistance = float.MaxValue;
    public bool isReverseLane = false;
    public bool isObstacleDetected = false;
    public bool isPlayerInFront = false;
    public bool isStunnedByCollision = false;
    public float stunTimer = 0f;
    public float stoppedDuration = 0f;
    public string activeBranchName = "";

    [Header("--- U-TURN / CUL-DE-SAC (ROUND CAP) ---")]
    public bool isPerformingUTurn = false;
    private float uTurnProgress = 0f;
    private Vector3 uTurnCenter;
    private Vector3 uTurnForward;
    private Vector3 uTurnRight;
    private bool uTurnIsAtEnd;
    private float uTurnRadius;
    private float uTurnLength;

    [Header("--- JUNCTION TRANSITION (SMOOTH BEZIER TURNING) ---")]
    public bool isTransitioningJunction = false;
    private float junctionTransitionProgress = 0f;
    private float junctionTransitionDuration = 1.0f;
    private Vector3 bezierP0, bezierP1, bezierP2, bezierP3;
    private SplineTrafficManager.TrafficPathBranch pendingBranch;
    private float pendingBranchDistance;
    private bool pendingIsReverse;

    public SplineTrafficManager.TrafficPathBranch CurrentBranch => currentBranch;

    private SplineTrafficManager trafficManager;
    private SplineTrafficManager.TrafficPathBranch currentBranch;
    private BoxCollider boxCollider;
    private Rigidbody rb;
    private Collider[] ownColliders;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null) boxCollider = gameObject.AddComponent<BoxCollider>();

        // Auto adjust BoxCollider to encapsulate child meshes if unconfigured
        if (boxCollider.size == Vector3.one && boxCollider.center == Vector3.zero)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    b.Encapsulate(renderers[i].bounds);
                }
                Vector3 localCenter = transform.InverseTransformPoint(b.center);
                Vector3 localSize = transform.InverseTransformVector(b.size);
                boxCollider.center = localCenter;
                boxCollider.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
            }
            else
            {
                boxCollider.center = new Vector3(0f, 0.75f, 0f);
                boxCollider.size = new Vector3(1.8f, 1.5f, 4.2f);
            }
        }

        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        CacheOwnColliders();
        AutoDetectWheels();
    }

    private void CacheOwnColliders()
    {
        ownColliders = GetComponentsInChildren<Collider>(true);
    }

    /// <summary>
    /// Automatically searches child transforms for wheels if not manually assigned.
    /// </summary>
    public void AutoDetectWheels()
    {
        if (wheelTransforms != null && wheelTransforms.Count > 0) return;

        wheelTransforms = new List<Transform>();
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);
        foreach (var t in allChildren)
        {
            if (t == transform) continue;
            string lowerName = t.name.ToLower();
            if (lowerName.Contains("wheel") || lowerName.Contains("teker"))
            {
                wheelTransforms.Add(t);
            }
        }
    }

    /// <summary>
    /// Initializes and activates the vehicle on a given road branch and lane.
    /// </summary>
    public void Initialize(
        SplineTrafficManager manager,
        SplineTrafficManager.TrafficPathBranch branch,
        float startDistance,
        bool reverse,
        float speed)
    {
        trafficManager = manager;
        currentBranch = branch;
        currentDistanceAlongPath = startDistance;
        isReverseLane = reverse;
        cruiseSpeed = speed;
        currentSpeed = speed; // Constant cruising speed immediately upon spawn
        targetSpeed = speed;
        activeBranchName = branch != null ? branch.branchName : "";
        isStunnedByCollision = false;
        stunTimer = 0f;
        stoppedDuration = 0f;
        isPerformingUTurn = false;
        uTurnProgress = 0f;
        isTransitioningJunction = false;
        junctionTransitionProgress = 0f;
        CacheOwnColliders();

        // Calculate lane offset based on road width
        if (branch != null)
        {
            laneOffset = branch.roadWidth * 0.25f;
        }

        // Set initial position & orientation immediately
        if (currentBranch != null)
        {
            Vector3 worldPos, worldForward;
            if (currentBranch.EvaluatePositionAndForward(currentDistanceAlongPath, isReverseLane, laneOffset, out worldPos, out worldForward))
            {
                worldPos = ApplyGroundHeight(worldPos);
                transform.position = worldPos;
                if (worldForward.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(worldForward, Vector3.up);
                }
            }
        }

        gameObject.SetActive(true);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision != null && collision.gameObject != null)
        {
            HandleVehicleCollision(collision.gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other != null && other.gameObject != null)
        {
            HandleVehicleCollision(other.gameObject);
        }
    }

    private void HandleVehicleCollision(GameObject otherObj)
    {
        if (otherObj == null) return;

        // Check if collision was with the Player's vehicle or character
        bool isPlayer = (otherObj.GetComponentInParent<FPSPlayerController>() != null ||
                         otherObj.GetComponentInParent<DrivableVehicle>() != null ||
                         otherObj.GetComponentInParent<CarController>() != null);

        if (isPlayer)
        {
            // Enter post-crash stunned pause
            isStunnedByCollision = true;
            stunTimer = playerCollisionPauseDuration;
            currentSpeed = 0f;
            targetSpeed = 0f;
        }
    }

    private void Update()
    {
        if (currentBranch == null || trafficManager == null) return;

        // 1. POST-COLLISION STUN PAUSE
        if (isStunnedByCollision)
        {
            stunTimer -= Time.deltaTime;
            currentSpeed = 0f;
            targetSpeed = 0f;

            if (stunTimer <= 0f)
            {
                isStunnedByCollision = false;
            }
            else
            {
                return; // Remain stopped after hitting player
            }
        }

        // 2. FORWARD RADAR & COLLISION DETECTION
        CheckFrontRadar();

        // 3. SPEED ADJUSTMENT (ACCELERATION & BRAKING)
        float accelRate = (targetSpeed < currentSpeed) ? brakeDeceleration : acceleration;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accelRate * Time.deltaTime);

        // 4. ANTI-JAM DEADLOCK RECOVERY (Despawns after 10s motionless ONLY if player is NOT in front)
        if (currentSpeed < 0.3f && !isStunnedByCollision)
        {
            if (isPlayerInFront)
            {
                // Player or player's car is in front: patiently wait without despawning!
                stoppedDuration = 0f;
            }
            else
            {
                stoppedDuration += Time.deltaTime;
                if (stoppedDuration >= maxStuckDuration)
                {
                    // Car has been motionless for 10 seconds without player in front: recycle to pool
                    trafficManager.DespawnVehicle(this);
                    return;
                }
            }
        }
        else
        {
            stoppedDuration = 0f;
        }

        // 5. CUL-DE-SAC / ROUND CAP SMOOTH 180-DEGREE FORWARD U-TURN ARC
        if (isPerformingUTurn)
        {
            UpdateCulDeSacUTurn();
            AnimateWheels();
            return;
        }

        // 5.5. JUNCTION / INTERSECTION SMOOTH BEZIER TURNING
        if (isTransitioningJunction)
        {
            UpdateJunctionTransition();
            AnimateWheels();
            return;
        }

        // 6. ADVANCE ALONG SPLINE PATH
        if (isObstacleDetected && closestObstacleDistance <= hardStopDistance)
        {
            // Zero-clipping hard physical stop: vehicle cannot advance when blocked at bumper
            currentSpeed = 0f;
            targetSpeed = 0f;
            AnimateWheels();
            return;
        }

        float moveDelta = currentSpeed * Time.deltaTime;
        float prevDistance = currentDistanceAlongPath;
        currentDistanceAlongPath += moveDelta;

        // Check if passing an intersecting side road (yan yol) and roll chance to turn into it (Triggers early before intersection center)
        SplineTrafficManager.TrafficPathBranch midBranch;
        float midDist;
        bool midReverse;
        if (trafficManager.TryTakeMidBranchTurnoff(currentBranch, prevDistance, currentDistanceAlongPath, isReverseLane, sideRoadTurnChance, out midBranch, out midDist, out midReverse))
        {
            // Smoothly turn into the intersecting side road via Bezier curve
            StartJunctionTransition(midBranch, midDist, midReverse);
            return;
        }

        // Check if approaching road end junction (EARLY TURN ANTICIPATION: starts 4.5m - 7.5m before road boundary to prevent overshooting)
        float junctionAnticipationDist = Mathf.Clamp(currentBranch.roadWidth * 0.65f + 3.0f, 4.0f, 7.5f);
        if (currentDistanceAlongPath >= (currentBranch.totalLength - junctionAnticipationDist))
        {
            SplineTrafficManager.TrafficPathBranch nextBranch;
            float nextDist;
            bool nextReverse;

            if (trafficManager.TryGetNextJunctionPath(currentBranch, isReverseLane, out nextBranch, out nextDist, out nextReverse))
            {
                // Smoothly enter connecting road via Bezier curve BEFORE reaching the edge of the road
                StartJunctionTransition(nextBranch, nextDist, nextReverse);
                return;
            }
        }

        // If no connecting junction exists (dead end / round cap) and reached absolute end of road:
        if (currentDistanceAlongPath >= currentBranch.totalLength || currentDistanceAlongPath < 0f)
        {
            // Dead end road / Round cap: check distance to player
            Vector3 playerPos = FPSPlayerController.Instance != null 
                ? FPSPlayerController.Instance.transform.position 
                : (Camera.main != null ? Camera.main.transform.position : transform.position);

            float distToPlayer = Vector3.Distance(transform.position, playerPos);

            if (distToPlayer > trafficManager.minSpawnDistance * 2.5f)
            {
                // Far away: recycle silently into pool
                trafficManager.DespawnVehicle(this);
                return;
            }
            else
            {
                // Near player: perform smooth 180-degree forward U-turn loop through the round cap bulb
                StartCulDeSacUTurn(!isReverseLane);
                return;
            }
        }

        // 7. POSITION & ORIENTATION UPDATE (SMOOTH STEERING & CORNERING)
        Vector3 targetPos, targetForward;
        if (currentBranch.EvaluatePositionAndForward(currentDistanceAlongPath, isReverseLane, laneOffset, out targetPos, out targetForward))
        {
            targetPos = ApplyGroundHeight(targetPos);

            // Direct continuous position tracking (Zero jitter / Zero micro-stutter)
            transform.position = targetPos;

            if (targetForward.sqrMagnitude > 0.001f)
            {
                // Align vehicle rotation with silky smooth angular steering
                Vector3 groundNormal = GetGroundNormal(targetPos);
                Quaternion targetRot = Quaternion.LookRotation(targetForward, groundNormal);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 14.0f);
            }
        }

        // 8. ANIMATE WHEEL ROTATION
        AnimateWheels();
    }

    private void StartJunctionTransition(SplineTrafficManager.TrafficPathBranch targetBranch, float targetDist, bool targetIsReverse)
    {
        if (targetBranch == null) return;

        float targetLaneOffset = targetBranch.roadWidth * 0.25f;
        float entryDist = Mathf.Clamp(targetDist, 0.5f, targetBranch.totalLength - 0.5f);

        Vector3 destPos, destForward;
        if (!targetBranch.EvaluatePositionAndForward(entryDist, targetIsReverse, targetLaneOffset, out destPos, out destForward))
        {
            // Fallback direct switch if evaluation fails
            currentBranch = targetBranch;
            currentDistanceAlongPath = entryDist;
            isReverseLane = targetIsReverse;
            laneOffset = targetLaneOffset;
            activeBranchName = targetBranch.branchName;
            currentSpeed = cruiseSpeed;
            targetSpeed = cruiseSpeed;
            return;
        }

        destPos = ApplyGroundHeight(destPos);

        bezierP0 = transform.position;
        Vector3 startForward = transform.forward;
        if (startForward.sqrMagnitude < 0.001f) startForward = Vector3.forward;

        bezierP3 = destPos;
        Vector3 endForward = destForward;
        if (endForward.sqrMagnitude < 0.001f) endForward = startForward;

        float chordLen = Vector3.Distance(bezierP0, bezierP3);
        float handleLen = Mathf.Max(2.5f, chordLen * 0.45f);

        bezierP1 = bezierP0 + (startForward * handleLen);
        bezierP2 = bezierP3 - (endForward * handleLen);

        // Maintain constant cruising speed without artificial slowdown during path/point transitions
        float turnSpeed = cruiseSpeed;
        float approxCurveLen = Mathf.Max(2.0f, chordLen * 1.15f);

        junctionTransitionDuration = Mathf.Max(0.2f, approxCurveLen / Mathf.Max(1.0f, turnSpeed));
        junctionTransitionProgress = 0f;
        isTransitioningJunction = true;

        pendingBranch = targetBranch;
        pendingBranchDistance = entryDist;
        pendingIsReverse = targetIsReverse;
        currentSpeed = turnSpeed;
        targetSpeed = cruiseSpeed;
        activeBranchName = $"➔ {targetBranch.branchName}";
    }

    private void UpdateJunctionTransition()
    {
        junctionTransitionProgress += Time.deltaTime;
        float t = Mathf.Clamp01(junctionTransitionProgress / Mathf.Max(0.1f, junctionTransitionDuration));

        // Uniform linear progress (no ease-in/ease-out slowdown at waypoint/junction points!)
        float smoothT = t;

        // Cubic Bezier interpolation
        Vector3 p01 = Vector3.Lerp(bezierP0, bezierP1, smoothT);
        Vector3 p12 = Vector3.Lerp(bezierP1, bezierP2, smoothT);
        Vector3 p23 = Vector3.Lerp(bezierP2, bezierP3, smoothT);

        Vector3 p012 = Vector3.Lerp(p01, p12, smoothT);
        Vector3 p123 = Vector3.Lerp(p12, p23, smoothT);

        Vector3 currentPos = Vector3.Lerp(p012, p123, smoothT);
        Vector3 currentTangent = (p123 - p012).normalized;

        currentPos = ApplyGroundHeight(currentPos);
        transform.position = currentPos;

        if (currentTangent.sqrMagnitude > 0.001f)
        {
            Vector3 groundNormal = GetGroundNormal(currentPos);
            Quaternion targetRot = Quaternion.LookRotation(currentTangent, groundNormal);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 14.0f);
        }

        if (junctionTransitionProgress >= junctionTransitionDuration)
        {
            // Transition smoothly finished at destination lane on new road!
            isTransitioningJunction = false;
            currentBranch = pendingBranch;
            currentDistanceAlongPath = pendingBranchDistance;
            isReverseLane = pendingIsReverse;
            laneOffset = currentBranch.roadWidth * 0.25f;
            activeBranchName = currentBranch.branchName;
            currentSpeed = cruiseSpeed;
            targetSpeed = cruiseSpeed; // Maintain constant cruising speed
        }
    }

    private void StartCulDeSacUTurn(bool atEnd)
    {
        isPerformingUTurn = true;
        uTurnProgress = 0f;
        uTurnIsAtEnd = atEnd;

        if (currentBranch == null || currentBranch.worldPoints == null || currentBranch.worldPoints.Count < 2)
        {
            isReverseLane = !isReverseLane;
            currentDistanceAlongPath = 0.5f;
            isPerformingUTurn = false;
            currentSpeed = cruiseSpeed;
            targetSpeed = cruiseSpeed;
            return;
        }

        int ptCount = currentBranch.worldPoints.Count;
        if (atEnd)
        {
            uTurnCenter = currentBranch.worldPoints[ptCount - 1];
            uTurnForward = (currentBranch.worldPoints[ptCount - 1] - currentBranch.worldPoints[ptCount - 2]).normalized;
        }
        else
        {
            uTurnCenter = currentBranch.worldPoints[0];
            uTurnForward = (currentBranch.worldPoints[1] - currentBranch.worldPoints[0]).normalized;
        }

        uTurnRight = Vector3.Cross(Vector3.up, uTurnForward).normalized;
        uTurnRadius = Mathf.Max(1.5f, laneOffset);
        uTurnLength = Mathf.PI * uTurnRadius;
        currentSpeed = cruiseSpeed;
        targetSpeed = cruiseSpeed;
    }

    private void UpdateCulDeSacUTurn()
    {
        float arcSpeed = cruiseSpeed;
        uTurnProgress += (arcSpeed * Time.deltaTime) / Mathf.Max(1.0f, uTurnLength);

        float theta = Mathf.Clamp01(uTurnProgress) * Mathf.PI; // 0 to 180 degrees
        Vector3 arcPos, arcTangent;

        if (uTurnIsAtEnd)
        {
            // Sweeps from right lane (+right*laneOffset), through bulb (+forward*depth), to left lane (-right*laneOffset)
            Vector3 offset = (Mathf.Cos(theta) * uTurnRight * laneOffset) + (Mathf.Sin(theta) * uTurnForward * (laneOffset * 1.25f));
            arcPos = uTurnCenter + offset;
            arcTangent = (-Mathf.Sin(theta) * uTurnRight * laneOffset) + (Mathf.Cos(theta) * uTurnForward * (laneOffset * 1.25f));
        }
        else
        {
            // Sweeps from left lane (-right*laneOffset), through bulb (-forward*depth), to right lane (+right*laneOffset)
            Vector3 offset = (-Mathf.Cos(theta) * uTurnRight * laneOffset) - (Mathf.Sin(theta) * uTurnForward * (laneOffset * 1.25f));
            arcPos = uTurnCenter + offset;
            arcTangent = (Mathf.Sin(theta) * uTurnRight * laneOffset) - (Mathf.Cos(theta) * uTurnForward * (laneOffset * 1.25f));
        }

        arcTangent.Normalize();
        arcPos = ApplyGroundHeight(arcPos);

        // Direct continuous position along U-turn arc (Zero jitter)
        transform.position = arcPos;

        if (arcTangent.sqrMagnitude > 0.001f)
        {
            Vector3 groundNormal = GetGroundNormal(arcPos);
            Quaternion targetRot = Quaternion.LookRotation(arcTangent, groundNormal);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 14.0f);
        }

        if (uTurnProgress >= 1.0f)
        {
            isPerformingUTurn = false;
            isReverseLane = uTurnIsAtEnd ? true : false;
            currentDistanceAlongPath = 0.5f;
            currentSpeed = cruiseSpeed;
            targetSpeed = cruiseSpeed;
        }
    }

    private struct LaserRay
    {
        public Vector3 origin;
        public Vector3 direction;
        public float maxDistance;
    }

    /// <summary>
    /// Multi-point forward laser Raycast system and direct physical proximity barrier.
    /// Tightly bounded to the vehicle's driving path corridor to eliminate phantom stops from diagonal/sidewalk/opposite lane objects.
    /// </summary>
    private void CheckFrontRadar()
    {
        float closestDist = float.MaxValue;
        bool hitObstacle = false;
        bool hitPlayer = false;

        float forwardExtent = (boxCollider != null && boxCollider.size.z > 0.1f)
            ? (boxCollider.center.z + boxCollider.size.z * 0.5f)
            : 2.0f;
        float carPhysicalHalfWidth = (boxCollider != null && boxCollider.size.x > 0.1f)
            ? (boxCollider.size.x * 0.5f)
            : (raySpreadWidth > 0.1f ? raySpreadWidth : 0.9f);

        // Clearance corridor width: car width + tight safety margin
        float corridorHalfWidth = carPhysicalHalfWidth + lateralSafetyMargin;

        Vector3 frontBumper = transform.position + (transform.forward * (forwardExtent - 0.05f));
        Vector3 mainOrigin = frontBumper + (Vector3.up * sensorHeight);
        Vector3 lowOrigin = frontBumper + (Vector3.up * Mathf.Max(0.25f, sensorHeight * 0.45f));

        float innerRayOffset = carPhysicalHalfWidth * 0.5f;
        float outerRayOffset = carPhysicalHalfWidth * 0.85f;

        // 1. MULTI-POINT FORWARD LASER RAYCAST GRID (Tightly aligned to vehicle body width)
        LaserRay[] rays = new LaserRay[]
        {
            // Main Headlight Level (Center, Inner-Left, Inner-Right, Outer-Left, Outer-Right)
            new LaserRay { origin = mainOrigin, direction = transform.forward, maxDistance = sensorDistance },
            new LaserRay { origin = mainOrigin - (transform.right * innerRayOffset), direction = transform.forward, maxDistance = sensorDistance },
            new LaserRay { origin = mainOrigin + (transform.right * innerRayOffset), direction = transform.forward, maxDistance = sensorDistance },
            new LaserRay { origin = mainOrigin - (transform.right * outerRayOffset), direction = transform.forward, maxDistance = sensorDistance * 0.9f },
            new LaserRay { origin = mainOrigin + (transform.right * outerRayOffset), direction = transform.forward, maxDistance = sensorDistance * 0.9f },

            // Lower Bumper Level (Catches low obstacles, sports cars, pedestrians)
            new LaserRay { origin = lowOrigin, direction = transform.forward, maxDistance = sensorDistance * 0.75f },
            new LaserRay { origin = lowOrigin - (transform.right * innerRayOffset), direction = transform.forward, maxDistance = sensorDistance * 0.75f },
            new LaserRay { origin = lowOrigin + (transform.right * innerRayOffset), direction = transform.forward, maxDistance = sensorDistance * 0.75f },

            // Subtle Lateral Feeler Rays (Only 4.5 degrees, short range for tight turning clearance)
            new LaserRay { origin = mainOrigin - (transform.right * outerRayOffset), direction = (Quaternion.Euler(0, -4.5f, 0) * transform.forward), maxDistance = 4.5f },
            new LaserRay { origin = mainOrigin + (transform.right * outerRayOffset), direction = (Quaternion.Euler(0, 4.5f, 0) * transform.forward), maxDistance = 4.5f }
        };

        for (int i = 0; i < rays.Length; i++)
        {
            var r = rays[i];
            if (Physics.Raycast(r.origin, r.direction, out RaycastHit hit, r.maxDistance, obstacleLayers, QueryTriggerInteraction.Ignore))
            {
                if (!IsValidObstacleHit(hit)) continue;

                // Validate that the hit point is genuinely within the vehicle's driving corridor
                Vector3 toHit = hit.point - transform.position;
                float hitSide = Mathf.Abs(Vector3.Dot(toHit, transform.right));
                if (hitSide > corridorHalfWidth) continue;

                hitObstacle = true;
                if (hit.distance < closestDist)
                {
                    closestDist = hit.distance;
                }

                if (hit.collider.GetComponentInParent<FPSPlayerController>() != null ||
                    hit.collider.GetComponentInParent<DrivableVehicle>() != null ||
                    hit.collider.GetComponentInParent<CarController>() != null)
                {
                    hitPlayer = true;
                }
            }
        }

        // 2. DIRECT SPLINE SAFE FOLLOWING DISTANCE (Maintains accurate distance along spline curvature in same lane)
        if (trafficManager != null)
        {
            float leadingVehicleDist = trafficManager.GetDistanceToLeadingVehicle(this, currentBranch, isReverseLane, currentDistanceAlongPath, sensorDistance);
            if (leadingVehicleDist < sensorDistance)
            {
                hitObstacle = true;
                if (leadingVehicleDist < closestDist)
                {
                    closestDist = leadingVehicleDist;
                }
            }
        }

        // 3. PHYSICAL 3D PROXIMITY BARRIER (Eliminates vehicle overlapping/tailgating before it can start)
        if (trafficManager != null && trafficManager.ActiveVehicles != null)
        {
            var activeList = trafficManager.ActiveVehicles;
            for (int i = 0; i < activeList.Count; i++)
            {
                var other = activeList[i];
                if (other == null || other == this) continue;
                if (other.CurrentBranch == currentBranch && other.isReverseLane != isReverseLane) continue;

                Vector3 toOther = other.transform.position - transform.position;
                float fwdDist = Vector3.Dot(toOther, transform.forward);
                float sideDist = Mathf.Abs(Vector3.Dot(toOther, transform.right));
                float yDist = Mathf.Abs(toOther.y);

                // If other vehicle is inside our driving corridor in front
                if (fwdDist > 0.05f && fwdDist < 5.2f && sideDist <= (corridorHalfWidth + 0.15f) && yDist < 2.5f)
                {
                    hitObstacle = true;
                    if (fwdDist < closestDist)
                    {
                        closestDist = fwdDist;
                    }
                }
            }
        }

        // 4. PLAYER CHARACTER PROXIMITY HARD BARRIER (On-foot)
        Vector3 playerPos = FPSPlayerController.Instance != null 
            ? FPSPlayerController.Instance.transform.position 
            : (Camera.main != null ? Camera.main.transform.position : Vector3.zero);

        if (playerPos != Vector3.zero)
        {
            Vector3 toPlayer = playerPos - transform.position;
            float playerFwd = Vector3.Dot(toPlayer, transform.forward);
            float playerSide = Mathf.Abs(Vector3.Dot(toPlayer, transform.right));
            float playerY = Mathf.Abs(toPlayer.y);

            // Only consider player if physically inside vehicle's driving path corridor (not on sidewalk or flank)
            if (playerFwd > 0.1f && playerFwd < sensorDistance && playerSide <= (corridorHalfWidth + 0.20f) && playerY < 3.5f)
            {
                hitObstacle = true;
                hitPlayer = true;
                if (playerFwd < closestDist)
                {
                    closestDist = playerFwd;
                }
            }
        }

        // 5. PLAYER DRIVABLE VEHICLES PROXIMITY (Player's driven car or parked car blocking the road)
        if (DrivableVehicle.AllDrivableVehicles != null)
        {
            for (int i = 0; i < DrivableVehicle.AllDrivableVehicles.Count; i++)
            {
                var dv = DrivableVehicle.AllDrivableVehicles[i];
                if (dv == null || !dv.gameObject.activeInHierarchy) continue;

                Vector3 toCar = dv.transform.position - transform.position;
                float carFwd = Vector3.Dot(toCar, transform.forward);
                float carSide = Mathf.Abs(Vector3.Dot(toCar, transform.right));
                float carY = Mathf.Abs(toCar.y);

                // Only consider if car is inside our lane/corridor directly ahead (not on sidewalk, opposite lane, or diagonal)
                if (carFwd > 0.1f && carFwd < sensorDistance && carSide <= (corridorHalfWidth + 0.35f) && carY < 3.5f)
                {
                    hitObstacle = true;
                    hitPlayer = true;
                    if (carFwd < closestDist)
                    {
                        closestDist = carFwd;
                    }
                }
            }
        }

        closestObstacleDistance = closestDist;
        isObstacleDetected = hitObstacle;
        isPlayerInFront = hitPlayer;

        // 6. SPEED CONTROL & ABSOLUTE ANTI-CLIPPING CLAMP
        if (hitObstacle)
        {
            if (closestDist <= hardStopDistance)
            {
                // Immediate Hard Stop: clamp speeds to zero to guarantee zero clipping / zero overlap
                targetSpeed = 0f;
                currentSpeed = 0f;
            }
            else if (closestDist <= stopDistance)
            {
                targetSpeed = 0f; // Smooth final stop
            }
            else if (closestDist <= slowDistance)
            {
                float t = Mathf.Clamp01((closestDist - stopDistance) / (slowDistance - stopDistance));
                targetSpeed = Mathf.Lerp(0f, cruiseSpeed * 0.7f, t); // Progressive deceleration
            }
            else
            {
                targetSpeed = cruiseSpeed;
            }
        }
        else
        {
            // Pure constant cruising speed along roads and waypoint paths
            targetSpeed = cruiseSpeed;
        }
    }

    /// <summary>
    /// Validates if a raycast hit represents an actual obstacle in the driving lane.
    /// Excludes road meshes, terrain, sidewalks, curbs, and roadside scenery so cars maintain constant speed.
    /// </summary>
    private bool IsValidObstacleHit(RaycastHit hit)
    {
        Collider col = hit.collider;
        if (col == null) return false;

        // 1. Ignore own vehicle colliders
        if (IsOwnCollider(col)) return false;

        // 2. Ignore terrain, road, ground, sidewalk surface or slope
        if (IsRoadOrTerrainCollider(col, hit.normal)) return false;

        // 3. Ignore vehicles in the opposite lane traveling normally
        if (IsOppositeLaneVehicle(col)) return false;

        // 4. Vehicles & Player are always real obstacles
        if (col.GetComponentInParent<AITrafficVehicle>() != null) return true;
        if (col.GetComponentInParent<FPSPlayerController>() != null || col.CompareTag("Player")) return true;
        if (col.GetComponentInParent<DrivableVehicle>() != null || col.GetComponentInParent<CarController>() != null) return true;

        // 5. Movable dynamic physics objects
        Rigidbody rb = col.GetComponentInParent<Rigidbody>();
        if (rb != null && !rb.isKinematic) return true;

        // 6. Explicit obstacle names
        string colName = col.gameObject.name.ToLower();
        if (colName.Contains("obstacle") || colName.Contains("barrier") || colName.Contains("blockade") || colName.Contains("trafficcone"))
        {
            return true;
        }

        // Static roadside environment objects (buildings, trees, lamp posts, fences, rocks) are not obstacles
        return false;
    }

    private bool IsOppositeLaneVehicle(Collider col)
    {
        if (col == null) return false;
        AITrafficVehicle otherCar = col.GetComponentInParent<AITrafficVehicle>();
        if (otherCar != null && otherCar != this)
        {
            // If other car is on the same road branch but in the opposite lane, ignore it (they are passing normally in 2-way traffic)
            if (otherCar.CurrentBranch == currentBranch && otherCar.isReverseLane != isReverseLane)
            {
                return true;
            }
        }
        return false;
    }

    private bool IsRoadOrTerrainCollider(Collider col, Vector3 hitNormal)
    {
        if (col == null) return true;

        // If ray hits horizontal or inclined ground/road surface (slope normal pointing upward), ignore it
        if (Vector3.Dot(hitNormal, Vector3.up) > 0.45f) return true;

        if (col.GetComponentInParent<SplineRoadBuilder>() != null) return true;
        if (col.GetComponent<TerrainCollider>() != null) return true;
        if (col.GetComponent<Terrain>() != null) return true;

        string colName = col.gameObject.name.ToLower();
        if (colName.Contains("road") || colName.Contains("terrain") || colName.Contains("ground") || colName.Contains("sidewalk") || colName.Contains("yol") || colName.Contains("asphalt") || colName.Contains("kaldirim") || colName.Contains("curb")) return true;

        if (col is MeshCollider meshCol && meshCol.sharedMesh != null)
        {
            string meshName = meshCol.sharedMesh.name.ToLower();
            if (meshName.Contains("road") || meshName.Contains("terrain") || meshName.Contains("ground") || meshName.Contains("sidewalk") || meshName.Contains("yol") || meshName.Contains("asphalt")) return true;
        }

        return false;
    }

    private bool IsOwnCollider(Collider col)
    {
        if (col == null) return true;
        if (col.transform == transform || col.transform.IsChildOf(transform) || col.transform.root == transform) return true;
        if (ownColliders != null)
        {
            for (int i = 0; i < ownColliders.Length; i++)
            {
                if (ownColliders[i] == col) return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Snaps vehicle to road or terrain surface.
    /// </summary>
    private Vector3 ApplyGroundHeight(Vector3 pos)
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain != null)
        {
            float terrainY = terrain.SampleHeight(pos) + terrain.transform.position.y;
            pos.y = Mathf.Max(pos.y, terrainY) + groundOffset;
        }
        else
        {
            pos.y += groundOffset;
        }
        return pos;
    }

    private Vector3 GetGroundNormal(Vector3 pos)
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain != null && terrain.terrainData != null)
        {
            Vector3 tPos = terrain.transform.position;
            Vector3 tSize = terrain.terrainData.size;
            float normX = (pos.x - tPos.x) / tSize.x;
            float normZ = (pos.z - tPos.z) / tSize.z;
            if (normX >= 0f && normX <= 1f && normZ >= 0f && normZ <= 1f)
            {
                return terrain.terrainData.GetInterpolatedNormal(normX, normZ);
            }
        }
        return Vector3.up;
    }

    /// <summary>
    /// Spins wheel meshes based on speed.
    /// </summary>
    private void AnimateWheels()
    {
        if (wheelTransforms == null || wheelTransforms.Count == 0) return;

        float distanceTraveled = currentSpeed * Time.deltaTime;
        float rotationDegrees = (distanceTraveled / (2f * Mathf.PI * Mathf.Max(0.1f, wheelRadius))) * 360f;

        foreach (var wheel in wheelTransforms)
        {
            if (wheel != null)
            {
                wheel.Rotate(Vector3.right, rotationDegrees, Space.Self);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        float forwardExtent = (boxCollider != null && boxCollider.size.z > 0.1f)
            ? (boxCollider.center.z + boxCollider.size.z * 0.5f)
            : 2.0f;
        float carPhysicalHalfWidth = (boxCollider != null && boxCollider.size.x > 0.1f)
            ? (boxCollider.size.x * 0.5f)
            : (raySpreadWidth > 0.1f ? raySpreadWidth : 0.9f);

        float corridorHalfWidth = carPhysicalHalfWidth + lateralSafetyMargin;

        Vector3 frontBumper = transform.position + (transform.forward * (forwardExtent - 0.05f));
        Vector3 mainOrigin = frontBumper + (Vector3.up * sensorHeight);
        Vector3 lowOrigin = frontBumper + (Vector3.up * Mathf.Max(0.25f, sensorHeight * 0.45f));

        float innerRayOffset = carPhysicalHalfWidth * 0.5f;
        float outerRayOffset = carPhysicalHalfWidth * 0.85f;

        LaserRay[] rays = new LaserRay[]
        {
            new LaserRay { origin = mainOrigin, direction = transform.forward, maxDistance = sensorDistance },
            new LaserRay { origin = mainOrigin - (transform.right * innerRayOffset), direction = transform.forward, maxDistance = sensorDistance },
            new LaserRay { origin = mainOrigin + (transform.right * innerRayOffset), direction = transform.forward, maxDistance = sensorDistance },
            new LaserRay { origin = mainOrigin - (transform.right * outerRayOffset), direction = transform.forward, maxDistance = sensorDistance * 0.9f },
            new LaserRay { origin = mainOrigin + (transform.right * outerRayOffset), direction = transform.forward, maxDistance = sensorDistance * 0.9f },
            new LaserRay { origin = lowOrigin, direction = transform.forward, maxDistance = sensorDistance * 0.75f },
            new LaserRay { origin = lowOrigin - (transform.right * innerRayOffset), direction = transform.forward, maxDistance = sensorDistance * 0.75f },
            new LaserRay { origin = lowOrigin + (transform.right * innerRayOffset), direction = transform.forward, maxDistance = sensorDistance * 0.75f },
            new LaserRay { origin = mainOrigin - (transform.right * outerRayOffset), direction = (Quaternion.Euler(0, -4.5f, 0) * transform.forward), maxDistance = 4.5f },
            new LaserRay { origin = mainOrigin + (transform.right * outerRayOffset), direction = (Quaternion.Euler(0, 4.5f, 0) * transform.forward), maxDistance = 4.5f }
        };

        for (int i = 0; i < rays.Length; i++)
        {
            var r = rays[i];
            if (Physics.Raycast(r.origin, r.direction, out RaycastHit hit, r.maxDistance, obstacleLayers, QueryTriggerInteraction.Ignore))
            {
                if (IsValidObstacleHit(hit))
                {
                    Vector3 toHit = hit.point - transform.position;
                    float hitSide = Mathf.Abs(Vector3.Dot(toHit, transform.right));
                    if (hitSide <= corridorHalfWidth)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawLine(r.origin, hit.point);
                        Gizmos.DrawWireSphere(hit.point, 0.15f);
                        continue;
                    }
                }
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(r.origin, r.origin + (r.direction * r.maxDistance));
        }

        // Draw Hard Stop Safety Barrier Box
        Gizmos.color = isObstacleDetected ? Color.red : Color.green;
        Gizmos.DrawWireCube(frontBumper + (transform.forward * (hardStopDistance * 0.5f)) + (Vector3.up * sensorHeight), new Vector3(corridorHalfWidth * 2.0f, 1.2f, hardStopDistance));
    }
}
