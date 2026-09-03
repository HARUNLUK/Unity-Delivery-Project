using UnityEngine;
using TMPro;

public enum CargoDeliveryStatus
{
    Correct,
    WrongAddress,
    Undelivered
}

public struct CargoDeliveryResult
{
    public PhysicalCargoPackage package;
    public CargoDeliveryStatus status;
    public string trackingNumber;
    public string targetAddress;
    public string actualAddress;
    public int moneyChange;
}

[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class PhysicalCargoPackage : MonoBehaviour
{
    [Header("--- CARGO DATA ---")]
    public CargoItem cargoData;
    public string targetPointId = "1";
    public string targetAddressName = "104 Maple Street";
    public int deliveryReward = 100;
    public int wrongPenalty = 30;

    [Header("--- VISUAL COMPONENTS ---")]
    public MeshRenderer boxRenderer;
    public TextMeshPro labelText;
    public GameObject shippingLabel;

    private Rigidbody rb;
    private BoxCollider col;

    // Realistic Cardboard & Logistics Color Palettes
    private static readonly Color[] CardboardPalette = new Color[]
    {
        new Color(0.76f, 0.61f, 0.38f), // Kraft Brown
        new Color(0.62f, 0.46f, 0.24f), // Dark Kraft
        new Color(0.83f, 0.72f, 0.58f), // Light Sand
        new Color(0.55f, 0.52f, 0.47f), // Recycled Gray-Cardboard
        new Color(0.68f, 0.56f, 0.42f), // Heavy Duty Tan
        new Color(0.32f, 0.42f, 0.50f), // Logistics Dull Navy
        new Color(0.36f, 0.44f, 0.34f)  // Eco Dull Green
    };

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<BoxCollider>();
        if (boxRenderer == null) boxRenderer = GetComponent<MeshRenderer>();

        if (rb != null)
        {
            rb.mass = 8f;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 1.0f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    public void SetupPackage(string pointId, string address, int reward, int penalty)
    {
        targetPointId = pointId;
        targetAddressName = address;
        deliveryReward = reward;
        wrongPenalty = penalty;

        if (cargoData == null)
        {
            cargoData = new CargoItem
            {
                trackingNumber = $"PKG-{Random.Range(1000, 9999)}",
                targetPointId = pointId,
                targetAddressName = address,
                deliveryReward = reward,
                wrongDeliveryPenalty = penalty,
                isDelivered = false
            };
        }

        ApplyRandomDimensionsAndColor();
        BuildShippingLabel();
    }

    private void ApplyRandomDimensionsAndColor()
    {
        // 1. Varied Realistic Dimensions
        Vector3[] dimensionPresets = new Vector3[]
        {
            new Vector3(0.55f, 0.42f, 0.45f), // Standard Medium Box
            new Vector3(0.70f, 0.28f, 0.50f), // Flat Wide Parcel
            new Vector3(0.40f, 0.65f, 0.40f), // Tall Carton
            new Vector3(0.35f, 0.30f, 0.35f), // Small Compact Box
            new Vector3(0.60f, 0.50f, 0.55f), // Cube Cargo Box
            new Vector3(0.80f, 0.35f, 0.45f)  // Long Courier Box
        };

        Vector3 chosenSize = dimensionPresets[Random.Range(0, dimensionPresets.Length)];
        chosenSize.x *= Random.Range(0.92f, 1.08f);
        chosenSize.y *= Random.Range(0.92f, 1.08f);
        chosenSize.z *= Random.Range(0.92f, 1.08f);

        transform.localScale = chosenSize;
        if (col != null) col.size = Vector3.one;

        // 2. Realistic Color Palette
        if (boxRenderer != null)
        {
            Color boxColor = CardboardPalette[Random.Range(0, CardboardPalette.Length)];
            boxRenderer.material = CreateLitMaterial(boxColor, 0.15f);
        }
    }

    private void BuildShippingLabel()
    {
        if (shippingLabel != null) Destroy(shippingLabel);

        // White shipping label sticker on top of the box
        shippingLabel = new GameObject("ShippingLabel");
        shippingLabel.transform.SetParent(transform, false);
        shippingLabel.transform.localPosition = new Vector3(0f, 0.505f, 0f);
        shippingLabel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        shippingLabel.transform.localScale = new Vector3(0.85f, 0.75f, 1f);

        GameObject quadObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quadObj.transform.SetParent(shippingLabel.transform, false);
        quadObj.transform.localPosition = Vector3.zero;
        quadObj.transform.localRotation = Quaternion.identity;
        quadObj.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
        Destroy(quadObj.GetComponent<Collider>());

        MeshRenderer quadRenderer = quadObj.GetComponent<MeshRenderer>();
        if (quadRenderer != null)
        {
            quadRenderer.material = CreateLitMaterial(new Color(0.95f, 0.95f, 0.93f), 0.05f);
        }

        // Text on the label
        GameObject labelTextObj = new GameObject("LabelText");
        labelTextObj.transform.SetParent(shippingLabel.transform, false);
        labelTextObj.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        labelTextObj.transform.localRotation = Quaternion.identity;

        labelText = labelTextObj.AddComponent<TextMeshPro>();
        labelText.fontSize = 2.4f;
        labelText.fontStyle = FontStyles.Bold;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = new Color(0.1f, 0.1f, 0.12f);
        labelText.text = $"📦 #{targetPointId}\n<size=75%>{targetAddressName}</size>\n<size=65%>Reward: ${deliveryReward}</size>";
    }

    private static Material CreateLitMaterial(Color color, float smoothness = 0.15f)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Diffuse");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        Material mat = new Material(shader != null ? shader : Shader.Find("Hidden/InternalErrorShader"));
        mat.color = color;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        return mat;
    }

    public DeliveryPoint FindNearbyDeliveryPoint()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 4.5f);
        foreach (var hit in hits)
        {
            DeliveryPoint dp = hit.GetComponentInParent<DeliveryPoint>();
            if (dp != null) return dp;
        }
        return null;
    }

    /// <summary>
    /// Gün sonunda paketin konumunu kontrol eder ve kazanç/ceza sonucunu üretir.
    /// </summary>
    public CargoDeliveryResult EvaluateEndOfDayResult()
    {
        DeliveryPoint nearbyPoint = FindNearbyDeliveryPoint();
        string tracking = cargoData != null ? cargoData.trackingNumber : $"#PKG-{targetPointId}";

        CargoDeliveryResult result = new CargoDeliveryResult
        {
            package = this,
            trackingNumber = tracking,
            targetAddress = targetAddressName
        };

        if (nearbyPoint != null)
        {
            result.actualAddress = nearbyPoint.addressName;
            bool isMatch = string.Equals(targetPointId.Trim(), nearbyPoint.pointId.Trim(), System.StringComparison.OrdinalIgnoreCase);

            if (isMatch)
            {
                result.status = CargoDeliveryStatus.Correct;
                result.moneyChange = deliveryReward;
            }
            else
            {
                result.status = CargoDeliveryStatus.WrongAddress;
                result.moneyChange = -wrongPenalty;
            }
        }
        else
        {
            result.actualAddress = "Undelivered (Vehicle / Street)";
            result.status = CargoDeliveryStatus.Undelivered;
            result.moneyChange = -wrongPenalty;
        }

        return result;
    }
}
