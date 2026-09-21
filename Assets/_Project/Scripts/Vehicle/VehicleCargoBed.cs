using System.Collections.Generic;
using UnityEngine;

public class VehicleCargoBed : MonoBehaviour
{
    [Header("--- CARGO BED RETENTION & DETECTION SETTINGS ---")]
    [Tooltip("Trigger collider defining the vehicle trunk / bed area (You can resize/adjust this BoxCollider in Inspector or Scene view)")]
    public BoxCollider bedTrigger;

    [Tooltip("Local center of the cargo bed relative to vehicle root (Used if creating default trigger)")]
    public Vector3 bedCenter = new Vector3(0f, 1.25f, -1.22f);

    [Tooltip("Local size of the cargo bed (Used if creating default trigger)")]
    public Vector3 bedSize = new Vector3(1.85f, 1.4f, 2.0f);

    [Tooltip("Downward stabilizer force so cargo firmly grips the bed while driving fast over hills")]
    public float bedDownforce = 18f;

    private DrivableVehicle vehicle;
    private Rigidbody vehicleRb;
    private readonly HashSet<PhysicalCargoPackage> packagesInBed = new HashSet<PhysicalCargoPackage>();

    public IReadOnlyCollection<PhysicalCargoPackage> PackagesInBed => packagesInBed;
    public int LoadedPackageCount => packagesInBed.Count;

    private void Awake()
    {
        vehicle = GetComponent<DrivableVehicle>();
        if (vehicle == null) vehicle = GetComponentInParent<DrivableVehicle>();
        if (vehicle != null) vehicleRb = vehicle.GetComponent<Rigidbody>();

        EnsureBedTrigger();
    }

    [ContextMenu("Create / Find Cargo Bed Trigger")]
    public void EnsureBedTrigger()
    {
        if (bedTrigger == null)
        {
            // 1. Direct child search
            Transform t = transform.Find("CargoBedTrigger");
            if (t != null) bedTrigger = t.GetComponent<BoxCollider>();

            // 2. Recursive child search by name
            if (bedTrigger == null)
            {
                BoxCollider[] allCols = GetComponentsInChildren<BoxCollider>(true);
                foreach (var c in allCols)
                {
                    if (c.gameObject.name.ToLower().Contains("cargobed") || c.gameObject.name.ToLower().Contains("bedtrigger") || c.gameObject.name.ToLower().Contains("trunktrigger"))
                    {
                        bedTrigger = c;
                        break;
                    }
                }
            }

            // 3. Create default if none found
            if (bedTrigger == null)
            {
                GameObject obj = new GameObject("CargoBedTrigger");
                obj.transform.SetParent(transform, false);
                obj.transform.localPosition = bedCenter;
                obj.transform.localRotation = Quaternion.identity;

                bedTrigger = obj.AddComponent<BoxCollider>();
                bedTrigger.isTrigger = true;
                bedTrigger.size = bedSize;
            }
        }

        if (bedTrigger != null)
        {
            bedTrigger.isTrigger = true;
            int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
            if (ignoreRaycastLayer >= 0)
            {
                bedTrigger.gameObject.layer = ignoreRaycastLayer;
            }
        }
    }

    public bool IsPackageInBed(PhysicalCargoPackage pkg)
    {
        if (pkg == null) return false;
        return packagesInBed.Contains(pkg);
    }

    private void OnTriggerEnter(Collider other)
    {
        PhysicalCargoPackage pkg = other.GetComponent<PhysicalCargoPackage>();
        if (pkg == null) pkg = other.GetComponentInParent<PhysicalCargoPackage>();
        if (pkg == null) pkg = other.GetComponentInChildren<PhysicalCargoPackage>();

        if (pkg != null)
        {
            packagesInBed.Add(pkg);
            pkg.isInVehicleBed = true;

            Rigidbody prb = pkg.GetComponent<Rigidbody>();
            if (prb != null)
            {
                prb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                prb.interpolation = RigidbodyInterpolation.Interpolate;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PhysicalCargoPackage pkg = other.GetComponent<PhysicalCargoPackage>();
        if (pkg == null) pkg = other.GetComponentInParent<PhysicalCargoPackage>();
        if (pkg == null) pkg = other.GetComponentInChildren<PhysicalCargoPackage>();

        if (pkg != null)
        {
            packagesInBed.Remove(pkg);
            pkg.isInVehicleBed = false;
        }
    }

    private void OnDisable()
    {
        foreach (var pkg in packagesInBed)
        {
            if (pkg != null)
            {
                pkg.isInVehicleBed = false;
            }
        }
        packagesInBed.Clear();
    }

    private void FixedUpdate()
    {
        if (vehicleRb == null || packagesInBed.Count == 0) return;

        Vector3 vehicleVel = vehicleRb.linearVelocity;
        Vector3 vehicleAngVel = vehicleRb.angularVelocity;

        packagesInBed.RemoveWhere(p => p == null || !p.gameObject.activeInHierarchy);

        foreach (var pkg in packagesInBed)
        {
            if (pkg == null || pkg.isBeingCarried) continue;

            Rigidbody prb = pkg.GetComponent<Rigidbody>();
            if (prb == null || prb.isKinematic) continue;

            // Calculate precise linear velocity of this point on the vehicle body
            Vector3 r = prb.position - vehicleRb.position;
            Vector3 pointVelocity = vehicleVel + Vector3.Cross(vehicleAngVel, r);

            // Sync horizontal velocity with the truck bed so cargo moves seamlessly with vehicle
            Vector3 currentVel = prb.linearVelocity;
            Vector3 targetVel = new Vector3(pointVelocity.x, currentVel.y, pointVelocity.z);

            // Downward stabilization force when moving fast
            if (vehicleVel.sqrMagnitude > 0.5f)
            {
                prb.AddForce(-transform.up * (bedDownforce * prb.mass), ForceMode.Force);
            }

            prb.linearVelocity = Vector3.Lerp(currentVel, targetVel, Time.fixedDeltaTime * 18f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.30f); // Cyan transluscent fill
        Matrix4x4 oldMat = Gizmos.matrix;

        if (bedTrigger != null)
        {
            Gizmos.matrix = bedTrigger.transform.localToWorldMatrix;
            Gizmos.DrawCube(bedTrigger.center, bedTrigger.size);
            Gizmos.color = new Color(0.1f, 0.9f, 1f, 0.9f);
            Gizmos.DrawWireCube(bedTrigger.center, bedTrigger.size);
        }
        else
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(bedCenter, bedSize);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(bedCenter, bedSize);
        }

        Gizmos.matrix = oldMat;
    }
}
