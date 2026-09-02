using UnityEngine;

public class PhysicsGrabber : MonoBehaviour
{
    [Header("--- GRAB SETTINGS ---")]
    [Tooltip("Distance in front of camera where grabbed objects float")]
    public float holdDistance = 1.6f;

    [Tooltip("Strength of the force pulling the object towards the hold position")]
    public float grabForce = 250f;

    [Tooltip("Damping to prevent excessive bouncing and wobbling")]
    public float grabDamping = 20f;

    [Tooltip("Maximum mass the player can pick up")]
    public float maxGrabMass = 100f;

    [Header("--- CURRENT GRAB STATE ---")]
    public Rigidbody grabbedRb;
    private float originalDrag;
    private float originalAngularDrag;
    private bool originalUseGravity;

    private Transform holdPoint;
    private Camera playerCam;

    private void Awake()
    {
        playerCam = GetComponentInParent<Camera>();
        if (playerCam == null) playerCam = Camera.main;

        // Create hold anchor
        GameObject holdObj = new GameObject("CargoHoldPoint");
        holdObj.transform.SetParent(transform);
        holdObj.transform.localPosition = new Vector3(0f, -0.15f, holdDistance);
        holdPoint = holdObj.transform;
    }

    public bool IsHoldingObject => grabbedRb != null;

    public void GrabObject(Rigidbody targetRb)
    {
        if (targetRb == null || targetRb.mass > maxGrabMass) return;

        grabbedRb = targetRb;
        originalDrag = grabbedRb.linearDamping;
        originalAngularDrag = grabbedRb.angularDamping;
        originalUseGravity = grabbedRb.useGravity;

        grabbedRb.useGravity = false;
        grabbedRb.linearDamping = 10f;
        grabbedRb.angularDamping = 10f;
    }

    public void ReleaseObject(Vector3 throwForce = default)
    {
        if (grabbedRb == null) return;

        grabbedRb.useGravity = originalUseGravity;
        grabbedRb.linearDamping = originalDrag;
        grabbedRb.angularDamping = originalAngularDrag;

        if (throwForce != Vector3.zero)
        {
            grabbedRb.linearVelocity = throwForce;
        }

        // Check if dropped inside a DeliveryPoint trigger
        PhysicalCargoPackage pkg = grabbedRb.GetComponent<PhysicalCargoPackage>();
        if (pkg != null)
        {
            CheckDeliveryAtPoint(pkg);
        }

        grabbedRb = null;
    }

    private void FixedUpdate()
    {
        if (grabbedRb == null) return;

        // Smooth physics spring force towards hold point
        Vector3 targetPos = holdPoint.position;
        Vector3 forceDir = targetPos - grabbedRb.position;
        float distance = forceDir.magnitude;

        // If dragged too far away (e.g. stuck behind a solid wall), drop it
        if (distance > 4.5f)
        {
            ReleaseObject();
            return;
        }

        Vector3 targetVelocity = forceDir * grabForce;
        grabbedRb.linearVelocity = Vector3.Lerp(grabbedRb.linearVelocity, targetVelocity, Time.fixedDeltaTime * grabDamping);
        grabbedRb.angularVelocity = Vector3.Lerp(grabbedRb.angularVelocity, Vector3.zero, Time.fixedDeltaTime * grabDamping);
    }

    private void CheckDeliveryAtPoint(PhysicalCargoPackage pkg)
    {
        if (pkg == null || grabbedRb == null) return;

        Collider[] hits = Physics.OverlapSphere(grabbedRb.position, 4.0f);
        foreach (var hit in hits)
        {
            DeliveryPoint dp = hit.GetComponentInParent<DeliveryPoint>();
            if (dp != null)
            {
                if (pkg.TryDeliverAtPoint(dp, out string msg, out bool isSuccess))
                {
                    if (DeliveryNotificationHUD.Instance != null)
                    {
                        DeliveryNotificationHUD.Instance.ShowNotification(msg, isSuccess);
                    }
                }
                else
                {
                    if (DeliveryNotificationHUD.Instance != null)
                    {
                        DeliveryNotificationHUD.Instance.ShowNotification(msg, false);
                    }
                }
                break;
            }
        }
    }
}
