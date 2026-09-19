using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attached to road barriers, invisible walls, or zone boundary colliders.
/// Blocks the player and player's vehicles, but allows AI traffic vehicles to pass freely
/// without slowing down, stopping, or colliding.
/// </summary>
[AddComponentMenu("Delivery/Player Only Barrier")]
[RequireComponent(typeof(Collider))]
public class PlayerOnlyBarrier : MonoBehaviour
{
    [Header("--- BARRIER CONFIGURATION ---")]
    [Tooltip("If true, automatically sets this object's layer to 'PlayerOnlyWall' (if the layer exists).")]
    public bool autoAssignLayer = true;

    [Tooltip("If true, explicitly ignores collision with all AITrafficVehicle colliders.")]
    public bool ignoreAITrafficColliders = true;

    private Collider ownCollider;
    private static readonly List<PlayerOnlyBarrier> activeBarriers = new List<PlayerOnlyBarrier>();
    public static IReadOnlyList<PlayerOnlyBarrier> ActiveBarriers => activeBarriers;

    private void Awake()
    {
        ownCollider = GetComponent<Collider>();
        SetupBarrierLayerAndCollisions();
    }

    private void OnEnable()
    {
        if (!activeBarriers.Contains(this))
        {
            activeBarriers.Add(this);
        }
        SetupBarrierLayerAndCollisions();
    }

    private void OnDisable()
    {
        activeBarriers.Remove(this);
    }

    private void Start()
    {
        SetupBarrierLayerAndCollisions();
    }

    /// <summary>
    /// Configures layer and ignores collisions against AI traffic vehicles.
    /// </summary>
    public void SetupBarrierLayerAndCollisions()
    {
        if (ownCollider == null) ownCollider = GetComponent<Collider>();

        // 1. Auto-assign Layer if layer is defined in TagManager
        if (autoAssignLayer)
        {
            string[] preferredLayerNames = new string[] { "PlayerOnlyWall", "PlayerBarrier", "DemoBarrier", "InvisibleWall" };
            foreach (var lName in preferredLayerNames)
            {
                int layerId = LayerMask.NameToLayer(lName);
                if (layerId >= 0)
                {
                    gameObject.layer = layerId;
                    break;
                }
            }
        }

        // 2. Ignore collision with all existing and active AI Traffic Vehicles
        if (ignoreAITrafficColliders && ownCollider != null)
        {
            AITrafficVehicle[] aiVehicles = UnityEngine.Object.FindObjectsByType<AITrafficVehicle>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (aiVehicles != null)
            {
                for (int i = 0; i < aiVehicles.Length; i++)
                {
                    IgnoreCollisionWithVehicle(aiVehicles[i]);
                }
            }
        }
    }

    /// <summary>
    /// Explicitly tells Unity physics engine to ignore all collisions between this barrier and the given traffic vehicle.
    /// </summary>
    public void IgnoreCollisionWithVehicle(AITrafficVehicle aiCar)
    {
        if (aiCar == null || ownCollider == null) return;

        Collider[] carColliders = aiCar.GetComponentsInChildren<Collider>(true);
        for (int c = 0; c < carColliders.Length; c++)
        {
            if (carColliders[c] != null && carColliders[c] != ownCollider)
            {
                Physics.IgnoreCollision(ownCollider, carColliders[c], true);
            }
        }
    }

    /// <summary>
    /// Static callback when a new AI car spawns in the world.
    /// </summary>
    public static void NotifyVehicleSpawned(AITrafficVehicle aiCar)
    {
        if (aiCar == null) return;
        for (int i = 0; i < activeBarriers.Count; i++)
        {
            if (activeBarriers[i] != null)
            {
                activeBarriers[i].IgnoreCollisionWithVehicle(aiCar);
            }
        }
    }
}
