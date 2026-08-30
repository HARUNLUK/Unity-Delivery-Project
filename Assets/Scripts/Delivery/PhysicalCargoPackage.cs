using UnityEngine;

public class PhysicalCargoPackage : MonoBehaviour
{
    [Header("--- KARGO VERİSİ ---")]
    public CargoItem cargoData;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.mass = 15f;
    }

    public void InitializePackage(CargoItem item, Vector3 launchVelocity)
    {
        cargoData = item;
        if (rb != null)
        {
            rb.linearVelocity = launchVelocity;
            rb.AddTorque(Random.insideUnitSphere * 4f, ForceMode.Impulse);
        }
    }
}
