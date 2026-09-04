using UnityEngine;

public class PhysicsGrabber : MonoBehaviour
{
    [Header("--- GRAB SETTINGS ---")]
    [Tooltip("Distance in front of camera where grabbed objects float")]
    public float holdDistance = 1.6f;

    [Tooltip("Smooth follow damping speed (higher = snappier, lower = smoother)")]
    public float grabFollowSpeed = 22f;

    [Tooltip("Smooth rotation follow speed")]
    public float grabRotateSpeed = 16f;

    [Tooltip("Maximum mass the player can pick up")]
    public float maxGrabMass = 100f;

    [Header("--- HOLD ORIENTATION ---")]
    [Tooltip("Kutu tutulduğunda yazılı üst yüzeyin oyuncuya bakması için rotasyon ofseti")]
    public Vector3 holdRotationOffset = new Vector3(-60f, 0f, 0f);

    [Header("--- CURRENT GRAB STATE ---")]
    public Rigidbody grabbedRb;
    private float originalLinearDamping;
    private float originalAngularDamping;
    private bool originalUseGravity;
    private RigidbodyInterpolation originalInterpolation;

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

        // Set optimal carry physics
        grabbedRb.useGravity = false;
        grabbedRb.linearDamping = 8f;
        grabbedRb.angularDamping = 8f;
        grabbedRb.interpolation = RigidbodyInterpolation.Interpolate;

        // Ignore collision between grabbed object and player body (eliminates jitter completely!)
        SetCollisionWithPlayer(false);

        // Show side UI card with held cargo details
        PhysicalCargoPackage pkg = targetRb.GetComponent<PhysicalCargoPackage>();
        if (pkg == null) pkg = targetRb.GetComponentInParent<PhysicalCargoPackage>();
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

        PhysicalCargoPackage pkg = grabbedRb.GetComponent<PhysicalCargoPackage>();
        if (pkg == null) pkg = grabbedRb.GetComponentInParent<PhysicalCargoPackage>();
        if (pkg != null)
        {
            pkg.isBeingCarried = false;
        }

        // Restore collision with player body
        SetCollisionWithPlayer(true);

        // Hide side UI card
        if (InteractionPromptHUD.Instance != null)
        {
            InteractionPromptHUD.Instance.HideHeldCargoInfo();
        }

        grabbedRb.useGravity = originalUseGravity;
        grabbedRb.linearDamping = originalLinearDamping;
        grabbedRb.angularDamping = originalAngularDamping;
        grabbedRb.interpolation = originalInterpolation;

        if (throwForce != Vector3.zero)
        {
            grabbedRb.linearVelocity = throwForce;
        }

        grabbedRb = null;
    }

    private void SetCollisionWithPlayer(bool enable)
    {
        if (grabbedRb == null) return;

        Collider[] objCols = grabbedRb.GetComponentsInChildren<Collider>();
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

    private void FixedUpdate()
    {
        if (grabbedRb == null) return;

        Camera cam = playerCam != null ? playerCam : Camera.main;
        if (cam == null) return;

        // Target hold position in front of camera
        Vector3 targetPos = cam.transform.position + (cam.transform.forward * holdDistance) + (cam.transform.up * -0.22f);
        
        // Auto-orient package so the top shipping label tilts directly towards player's eyes
        Quaternion targetRot = cam.transform.rotation * Quaternion.Euler(holdRotationOffset);

        Vector3 forceDir = targetPos - grabbedRb.position;
        float distance = forceDir.magnitude;

        // If dragged too far away (e.g. trapped behind a solid obstacle), drop it
        if (distance > 4.5f)
        {
            ReleaseObject();
            return;
        }

        // Buttery-smooth spring velocity without any jitter or shaking
        grabbedRb.linearVelocity = forceDir * grabFollowSpeed;
        grabbedRb.angularVelocity = Vector3.zero;
        grabbedRb.rotation = Quaternion.Slerp(grabbedRb.rotation, targetRot, Time.fixedDeltaTime * grabRotateSpeed);
    }
}
