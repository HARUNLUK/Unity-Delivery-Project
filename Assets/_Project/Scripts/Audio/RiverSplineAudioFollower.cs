using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// River Spline 3D Audio Follower:
/// Attaches to a child GameObject (e.g. 'river_audio_point') under the River Spline object.
/// Smoothly positions the 3D AudioSource at the exact closest point along the river spline relative to the player.
/// Allows Unity's native 3D spatial audio engine to handle distance, volume rolloff, and binaural stereo panning naturally.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class RiverSplineAudioFollower : MonoBehaviour
{
    [Header("--- SPLINE REFERENCE ---")]
    [Tooltip("River's SplineContainer. If left empty, automatically found on parent or in hierarchy.")]
    public SplineContainer splineContainer;

    [Header("--- TARGET LISTENER ---")]
    [Tooltip("Target transform to follow (Player or Camera). If null, automatically tracks player character/camera.")]
    public Transform targetListener;

    [Header("--- MOTION SETTINGS ---")]
    [Tooltip("If true, smoothly moves along the spline instead of teleporting instantaneously")]
    public bool smoothMovement = true;

    [Range(1f, 30f), Tooltip("Smoothing transition speed along spline")]
    public float smoothSpeed = 12f;

    [Tooltip("Height offset above the spline center (e.g. 0.2m for river water surface)")]
    public float heightOffset = 0.2f;

    [Header("--- GIZMOS & DEBUG ---")]
    public bool showGizmos = true;
    public Color gizmoColor = new Color(0.2f, 0.8f, 1.0f, 0.9f);

    private AudioSource audioSource;
    private bool isInitialized = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        ConfigureAudioSource();
        EnsureSplineContainer();
    }

    private void Start()
    {
        EnsureSplineContainer();
        FindListener();

        // Snap immediately to nearest point on start
        if (targetListener != null && splineContainer != null)
        {
            transform.position = CalculateClosestPoint(targetListener.position);
        }

        isInitialized = true;
    }

    private void EnsureSplineContainer()
    {
        if (splineContainer == null)
        {
            splineContainer = GetComponentInParent<SplineContainer>();
        }

        if (splineContainer == null)
        {
            splineContainer = GetComponent<SplineContainer>();
        }

        if (splineContainer == null)
        {
            // Search scene for River spline
            SplineContainer[] containers = Object.FindObjectsByType<SplineContainer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var sc in containers)
            {
                if (sc != null && sc.gameObject.name.ToLower().Contains("river"))
                {
                    splineContainer = sc;
                    break;
                }
            }
        }
    }

    private void ConfigureAudioSource()
    {
        if (audioSource != null)
        {
            audioSource.spatialBlend = 1.0f; // Pure 3D Positional Audio
            audioSource.loop = true;
            if (!audioSource.isPlaying && audioSource.clip != null)
            {
                audioSource.Play();
            }
        }
    }

    private void FindListener()
    {
        if (targetListener != null) return;

        FPSPlayerController player = FPSPlayerController.Instance;
        if (player != null)
        {
            targetListener = player.transform;
            return;
        }

        if (Camera.main != null)
        {
            targetListener = Camera.main.transform;
        }
    }

    private void Update()
    {
        if (targetListener == null)
        {
            FindListener();
            if (targetListener == null) return;
        }

        if (splineContainer == null)
        {
            EnsureSplineContainer();
            if (splineContainer == null) return;
        }

        Vector3 listenerPos = targetListener.position;
        Vector3 targetPoint = CalculateClosestPoint(listenerPos);

        if (!isInitialized || !smoothMovement)
        {
            transform.position = targetPoint;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, targetPoint, Time.deltaTime * smoothSpeed);
        }

        if (audioSource != null)
        {
            float masterAmbience = (AudioManager.Instance != null) ? AudioManager.Instance.ambienceVolume * AudioManager.Instance.masterVolume : 1f;
            audioSource.volume = 0.65f * masterAmbience;
        }
    }

    /// <summary>
    /// Calculates the closest point on the river spline in World Space coordinates.
    /// </summary>
    public Vector3 CalculateClosestPoint(Vector3 worldListenerPos)
    {
        if (splineContainer == null) return transform.position;

        Matrix4x4 localToWorld = splineContainer.transform.localToWorldMatrix;
        Matrix4x4 worldToLocal = splineContainer.transform.worldToLocalMatrix;

        float3 localPoint = worldToLocal.MultiplyPoint3x4(worldListenerPos);
        float minSqrDist = float.MaxValue;
        float3 bestLocalPoint = localPoint;
        bool found = false;

        var splines = splineContainer.Splines;
        if (splines != null && splines.Count > 0)
        {
            for (int i = 0; i < splines.Count; i++)
            {
                var s = splines[i];
                if (s == null) continue;

                float3 nearest;
                float t;
                SplineUtility.GetNearestPoint(s, localPoint, out nearest, out t);

                float sqrDist = math.distancesq(localPoint, nearest);
                if (sqrDist < minSqrDist)
                {
                    minSqrDist = sqrDist;
                    bestLocalPoint = nearest;
                    found = true;
                }
            }
        }
        else if (splineContainer.Spline != null)
        {
            float3 nearest;
            float t;
            SplineUtility.GetNearestPoint(splineContainer.Spline, localPoint, out nearest, out t);
            bestLocalPoint = nearest;
            found = true;
        }

        if (found)
        {
            Vector3 worldPos = localToWorld.MultiplyPoint3x4(bestLocalPoint);
            worldPos.y += heightOffset;
            return worldPos;
        }

        return transform.position;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, 1.2f);
        Gizmos.DrawSphere(transform.position, 0.4f);

        if (targetListener != null)
        {
            Gizmos.color = new Color(0.2f, 1.0f, 0.6f, 0.7f);
            Gizmos.DrawLine(transform.position, targetListener.position);
        }
    }
}
