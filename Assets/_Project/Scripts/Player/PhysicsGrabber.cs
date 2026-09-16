using System.Collections;
using UnityEngine;

public class PhysicsGrabber : MonoBehaviour
{
    [Header("--- GRAB SETTINGS ---")]
    [Tooltip("Base distance in front of camera where grabbed objects float")]
    public float holdDistance = 1.6f;

    [Tooltip("Smooth follow damping speed (higher = snappier, lower = smoother)")]
    public float grabFollowSpeed = 22f;

    [Tooltip("Smooth rotation follow speed")]
    public float grabRotateSpeed = 16f;

    [Tooltip("Maximum mass the player can pick up")]
    public float maxGrabMass = 100f;

    [Header("--- HOLD ORIENTATION ---")]
    [Tooltip("Rotation offset so that the package label tilts towards the player when held")]
    public Vector3 holdRotationOffset = new Vector3(-60f, 0f, 0f);

    [Header("--- CURRENT GRAB STATE ---")]
    public Rigidbody grabbedRb;
    private float originalLinearDamping;
    private float originalAngularDamping;
    private bool originalUseGravity;
    private RigidbodyInterpolation originalInterpolation;
    private float effectiveHoldDistance = 1.6f;

    private Camera playerCam;
    private Collider[] playerColliders;

    private void Awake()
    {
        playerCam = GetComponentInParent<Camera>();
        if (playerCam == null) playerCam = Camera.main;

        // Cache player colliders to ignore collision while carrying
        playerColliders = transform.root.GetComponentsInChildren<Collider>();
    }

    public bool IsHoldingObject => grabbedRb != null;

    public void GrabObject(Rigidbody targetRb)
    {
        if (targetRb == null || targetRb.mass > maxGrabMass) return;

        grabbedRb = targetRb;
        originalLinearDamping = grabbedRb.linearDamping;
        originalAngularDamping = grabbedRb.angularDamping;
        originalUseGravity = grabbedRb.useGravity;
        originalInterpolation = grabbedRb.interpolation;

        // Dynamically compute safe hold distance for elongated / large cargo
        float extent = CalculateObjectExtent(targetRb);
        effectiveHoldDistance = Mathf.Max(holdDistance, 0.95f + extent);

        // Set optimal carry physics
        grabbedRb.useGravity = false;
        grabbedRb.linearDamping = 8f;
        grabbedRb.angularDamping = 8f;
        grabbedRb.interpolation = RigidbodyInterpolation.Interpolate;

        // Ignore collision between grabbed object and player body (eliminates jitter completely!)
        SetCollisionWithPlayer(grabbedRb, false);

        // Show side UI card with held cargo details
        PhysicalCargoPackage pkg = targetRb.GetComponent<PhysicalCargoPackage>();
        if (pkg == null) pkg = targetRb.GetComponentInParent<PhysicalCargoPackage>();
        if (pkg == null) pkg = targetRb.GetComponentInChildren<PhysicalCargoPackage>();
        if (pkg != null)
        {
            pkg.hasBeenHandledByPlayer = true;
            pkg.isBeingCarried = true;
            if (InteractionPromptHUD.Instance != null)
            {
                InteractionPromptHUD.Instance.ShowHeldCargoInfo(pkg);
            }
        }
    }

    public void ReleaseObject(Vector3 throwForce = default)
    {
        if (grabbedRb == null) return;

        Rigidbody releasedRb = grabbedRb;
        grabbedRb = null;

        PhysicalCargoPackage pkg = releasedRb.GetComponent<PhysicalCargoPackage>();
        if (pkg == null) pkg = releasedRb.GetComponentInParent<PhysicalCargoPackage>();
        if (pkg == null) pkg = releasedRb.GetComponentInChildren<PhysicalCargoPackage>();
        if (pkg != null)
        {
            pkg.isBeingCarried = false;
        }

        // Hide side UI card
        if (InteractionPromptHUD.Instance != null)
        {
            InteractionPromptHUD.Instance.HideHeldCargoInfo();
        }

        releasedRb.useGravity = originalUseGravity;
        releasedRb.linearDamping = originalLinearDamping;
        releasedRb.angularDamping = originalAngularDamping;
        releasedRb.interpolation = originalInterpolation;

        if (throwForce != Vector3.zero)
        {
            releasedRb.linearVelocity = throwForce;
        }
        else
        {
            // Reset spring/carry momentum to prevent launching when dropped while walking
            releasedRb.linearVelocity = Vector3.zero;
            releasedRb.angularVelocity = Vector3.zero;
        }

        // Delay re-enabling collision with player body so overlap doesn't fling the object!
        StartCoroutine(ReenableCollisionWithPlayerRoutine(releasedRb, 0.55f));
    }

    private float CalculateObjectExtent(Rigidbody rb)
    {
        Collider[] cols = rb.GetComponentsInChildren<Collider>();
        if (cols == null || cols.Length == 0) return 0.5f;

        Vector3 extents = Vector3.zero;
        foreach (var c in cols)
        {
            if (c != null && !c.isTrigger)
            {
                extents = Vector3.Max(extents, c.bounds.extents);
            }
        }
        return Mathf.Max(extents.x, extents.y, extents.z);
    }

    private void SetCollisionWithPlayer(Rigidbody rb, bool enable)
    {
        if (rb == null) return;

        Collider[] objCols = rb.GetComponentsInChildren<Collider>();
        if (playerColliders == null || playerColliders.Length == 0)
        {
            playerColliders = transform.root.GetComponentsInChildren<Collider>();
        }

        if (playerColliders == null) return;

        foreach (var oc in objCols)
        {
            foreach (var pc in playerColliders)
            {
                if (oc != null && pc != null && oc != pc)
                {
                    Physics.IgnoreCollision(oc, pc, !enable);
                }
            }
        }
    }

    private IEnumerator ReenableCollisionWithPlayerRoutine(Rigidbody targetRb, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (targetRb != null)
        {
            SetCollisionWithPlayer(targetRb, true);
        }
    }

    private void FixedUpdate()
    {
        if (grabbedRb == null) return;

        Camera cam = playerCam != null ? playerCam : Camera.main;
        if (cam == null) return;

        // Target hold position in front of camera using dynamic safe hold distance
        Vector3 targetPos = cam.transform.position + (cam.transform.forward * effectiveHoldDistance) + (cam.transform.up * -0.22f);
        
        // Auto-orient package so the top shipping label tilts directly towards player's eyes
        Quaternion targetRot = cam.transform.rotation * Quaternion.Euler(holdRotationOffset);

        Vector3 forceDir = targetPos - grabbedRb.position;
        float distance = forceDir.magnitude;

        // If dragged too far away (e.g. trapped behind a solid obstacle), drop it
        if (distance > Mathf.Max(4.5f, effectiveHoldDistance + 2.5f))
        {
            ReleaseObject();
            return;
        }

        // Smooth spring velocity capped to prevent sudden high velocity slingshots
        Vector3 targetVelocity = forceDir * grabFollowSpeed;
        grabbedRb.linearVelocity = Vector3.ClampMagnitude(targetVelocity, 18f);
        grabbedRb.angularVelocity = Vector3.zero;
        grabbedRb.rotation = Quaternion.Slerp(grabbedRb.rotation, targetRot, Time.fixedDeltaTime * grabRotateSpeed);
    }
}
