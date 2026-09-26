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
    private readonly HashSet<CarriableItem> itemsInBed = new HashSet<CarriableItem>();

    public int LoadedItemCount => itemsInBed.Count;
    public IReadOnlyCollection<CarriableItem> ItemsInBed => itemsInBed;

    public IReadOnlyCollection<PhysicalCargoPackage> PackagesInBed => packagesInBed;
    public int LoadedPackageCount => packagesInBed.Count;

    private void Awake()
    {
        vehicle = GetComponent<DrivableVehicle>();
        if (vehicle == null) vehicle = GetComponentInParent<DrivableVehicle>();
        if (vehicle != null) vehicleRb = vehicle.GetComponent<Rigidbody>();

        EnsureBedTrigger();
    }

    private void Start()
    {
        // Benzin bidonu kayıt yükleme kaldırıldı.
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

    public void RemovePackage(PhysicalCargoPackage pkg)
    {
        if (pkg == null) return;
        packagesInBed.Remove(pkg);
        pkg.isInVehicleBed = false;
        pkg.UpdateLabelText();
        if (pkg.currentCargoBed == this)
        {
            pkg.currentCargoBed = null;
        }
    }

    public void RemoveItem(CarriableItem item)
    {
        if (item == null) return;
        itemsInBed.Remove(item);
        item.isInVehicleBed = false;
        if (item.currentCargoBed == this) item.currentCargoBed = null;
    }

    private void CaptureItem(CarriableItem item)
    {
        // Held or just-thrown items are not captured
        if (item == null || item.isBeingCarried || item.IsRecentlyThrown) return;
        if (itemsInBed.Add(item))
        {
            item.isInVehicleBed = true;
            item.currentCargoBed = this;
            Rigidbody irb = item.GetComponent<Rigidbody>();
            if (irb != null)
            {
                irb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                irb.interpolation = RigidbodyInterpolation.Interpolate;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        CaptureItem(other.GetComponentInParent<CarriableItem>());

        PhysicalCargoPackage pkg = other.GetComponent<PhysicalCargoPackage>();
        if (pkg == null) pkg = other.GetComponentInParent<PhysicalCargoPackage>();
        if (pkg == null) pkg = other.GetComponentInChildren<PhysicalCargoPackage>();

        if (pkg != null)
        {
            // Do not capture packages that are currently held or were just thrown
            if (pkg.isBeingCarried || pkg.IsRecentlyThrown) return;

            packagesInBed.Add(pkg);
            pkg.isInVehicleBed = true;
            pkg.UpdateLabelText();
            pkg.currentCargoBed = this;

            Rigidbody prb = pkg.GetComponent<Rigidbody>();
            if (prb != null)
            {
                prb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                prb.interpolation = RigidbodyInterpolation.Interpolate;
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        CaptureItem(other.GetComponentInParent<CarriableItem>());

        PhysicalCargoPackage pkg = other.GetComponent<PhysicalCargoPackage>();
        if (pkg == null) pkg = other.GetComponentInParent<PhysicalCargoPackage>();
        if (pkg == null) pkg = other.GetComponentInChildren<PhysicalCargoPackage>();

        if (pkg != null && !pkg.isBeingCarried && !pkg.IsRecentlyThrown)
        {
            if (!packagesInBed.Contains(pkg))
            {
                packagesInBed.Add(pkg);
                pkg.isInVehicleBed = true;
                pkg.UpdateLabelText();
                pkg.currentCargoBed = this;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        CarriableItem leavingItem = other.GetComponentInParent<CarriableItem>();
        if (leavingItem != null) RemoveItem(leavingItem);

        PhysicalCargoPackage pkg = other.GetComponent<PhysicalCargoPackage>();
        if (pkg == null) pkg = other.GetComponentInParent<PhysicalCargoPackage>();
        if (pkg == null) pkg = other.GetComponentInChildren<PhysicalCargoPackage>();

        if (pkg != null)
        {
            packagesInBed.Remove(pkg);
            pkg.isInVehicleBed = false;
            pkg.UpdateLabelText();
            if (pkg.currentCargoBed == this)
            {
                pkg.currentCargoBed = null;
            }
        }
    }

    private void OnDisable()
    {
        foreach (var pkg in packagesInBed)
        {
            if (pkg != null)
            {
                pkg.isInVehicleBed = false;
                pkg.UpdateLabelText();
                if (pkg.currentCargoBed == this)
                {
                    pkg.currentCargoBed = null;
                }
            }
        }
        packagesInBed.Clear();

        foreach (var item in itemsInBed)
        {
            if (item == null) continue;
            item.isInVehicleBed = false;
            if (item.currentCargoBed == this) item.currentCargoBed = null;
        }
        itemsInBed.Clear();
    }

    private void OnApplicationPause(bool pauseStatus) { }

    private void OnApplicationQuit() { }

    /// <summary>Benzin bidonu kayıt sistemi devre dışı bırakıldı.</summary>
    public void SaveBedCanisters() { }

    /// <summary>Benzin bidonu yükleme sistemi devre dışı bırakıldı.</summary>
    public void LoadBedCanisters() { }

    private GameObject GetGasCanisterPrefab()
    {
        GameObject prefab = Resources.Load<GameObject>("Prefabs/Gas_Can");
#if UNITY_EDITOR
        if (prefab == null)
        {
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_AssetPacks/ExplosivesPackage/Prefabs/Gas_Can.prefab");
        }
#endif
        if (prefab == null)
        {
            prefab = Resources.Load<GameObject>("Prefabs/Gas_Canister");
        }
        return prefab;
    }

    /// <summary>Same bed stabilizer as parcels: items ride along with the vehicle instead of sliding around.</summary>
    private void StabilizeItems(Vector3 vehicleVel, Vector3 vehicleAngVel)
    {
        itemsInBed.RemoveWhere(i => i == null || !i.gameObject.activeInHierarchy);
        bool isVehicleMoving = vehicleVel.sqrMagnitude > 0.15f || vehicleAngVel.sqrMagnitude > 0.05f;
        if (!isVehicleMoving) return;

        foreach (var item in itemsInBed)
        {
            if (item.isBeingCarried || item.IsRecentlyThrown) continue;
            Rigidbody irb = item.GetComponent<Rigidbody>();
            if (irb == null || irb.isKinematic) continue;

            Vector3 r = irb.position - vehicleRb.position;
            Vector3 pointVelocity = vehicleVel + Vector3.Cross(vehicleAngVel, r);
            Vector3 currentVel = irb.linearVelocity;
            Vector3 targetVel = new Vector3(pointVelocity.x, currentVel.y, pointVelocity.z);

            if (vehicleVel.sqrMagnitude > 0.5f)
            {
                irb.AddForce(-transform.up * (bedDownforce * irb.mass), ForceMode.Force);
            }
            irb.linearVelocity = Vector3.Lerp(currentVel, targetVel, Time.fixedDeltaTime * 18f);
        }
    }

    private void FixedUpdate()
    {
        if (vehicleRb != null && itemsInBed.Count > 0) StabilizeItems(vehicleRb.linearVelocity, vehicleRb.angularVelocity);
        if (vehicleRb == null || packagesInBed.Count == 0) return;

        Vector3 vehicleVel = vehicleRb.linearVelocity;
        Vector3 vehicleAngVel = vehicleRb.angularVelocity;

        packagesInBed.RemoveWhere(p => p == null || !p.gameObject.activeInHierarchy);

        // When the vehicle is stopped / parked, let PhysX naturally hold cargo in place.
        // DO NOT artificially zero or drag horizontal velocities when the vehicle is stationary!
        bool isVehicleMoving = vehicleVel.sqrMagnitude > 0.15f || vehicleAngVel.sqrMagnitude > 0.05f;

        foreach (var pkg in packagesInBed)
        {
            if (pkg == null || pkg.isBeingCarried) continue;

            // If the package was thrown, ignore bed stabilizer and let it fly freely!
            if (pkg.IsRecentlyThrown) continue;

            Rigidbody prb = pkg.GetComponent<Rigidbody>();
            if (prb == null || prb.isKinematic) continue;

            // If vehicle is stationary, let normal physics take over (free throw, pushing, sliding, settling)
            if (!isVehicleMoving) continue;

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

[System.Serializable]
public class SavedBedCanisterData
{
    public float localPosX;
    public float localPosY;
    public float localPosZ;
    public float localRotX;
    public float localRotY;
    public float localRotZ;
    public float localRotW;
    public float fuelAmount;
}

[System.Serializable]
public class SavedBedCanistersList
{
    public List<SavedBedCanisterData> canisters = new List<SavedBedCanisterData>();
}
