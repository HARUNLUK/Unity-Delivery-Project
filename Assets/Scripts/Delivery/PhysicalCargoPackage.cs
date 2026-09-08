using UnityEngine;
using TMPro;

public enum CargoDeliveryStatus
{
    Correct,
    WrongAddress,
    Undelivered,
    Broken
}

public struct CargoDeliveryResult
{
    public PhysicalCargoPackage package;
    public CargoDeliveryStatus status;
    public CargoType cargoType;
    public string trackingNumber;
    public string recipientName;
    public string targetAddress;
    public string actualAddress;
    public int moneyChange;
    public int xpAwarded;
    public bool isExpressBonus;
    public bool isBroken;
}

[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class PhysicalCargoPackage : MonoBehaviour
{
    [Header("--- CARGO DATA ---")]
    public CargoItem cargoData;
    public CargoType cargoType = CargoType.Standard;
    public string targetPointId = "1";
    public string recipientName = "John Doe";
    public string targetAddressName = "104 Maple Street";
    public string targetAddress => targetAddressName;
    public string targetAddressDescription = "";
    public int deliveryReward = 100;
    public int wrongPenalty = 30;
    public int wrongDeliveryPenalty => wrongPenalty;
    public int xpReward = 80;

    [Header("--- FRAGILE & EXPRESS SPECS ---")]
    public float health = 100f;
    public float targetDeliveryHour = 13.0f; // Delivery bonus cutoff time (e.g. 13:00)
    public bool isBroken => health <= 0f;
    public bool hasBeenHandledByPlayer = false;
    public bool isBeingCarried = false; // Prevents wall/door friction damage while held!
    
    [Tooltip("Minimum collision impact velocity (m/s) required to begin taking damage. Gentle placement < 2.5 m/s, 1.5m drop ~5.0 m/s, high drop > 7.0 m/s.")]
    public float minDamageSpeedThreshold = 3.5f;

    [Tooltip("Damage multiplier per unit speed above the threshold.")]
    public float damageMultiplier = 16.0f;

    [Tooltip("Damage reduction ratio when colliding with other cargo packages (0.20 = 80% less damage).")]
    public float packageCollisionDamageRatio = 0.20f;

    public float spawnImmunityDuration = 3.5f;
    public float damageCooldown = 0.20f;
    private float spawnImmunityUntil = 0f;
    private float lastDamageTime = 0f;

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

    public bool isCustomPrefab = false;

    private void Awake()
    {
        EnsurePhysicsComponents();
        spawnImmunityUntil = Time.time + spawnImmunityDuration;
    }

    public void EnsurePhysicsComponents()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        if (col == null) col = GetComponent<BoxCollider>();
        if (col == null && GetComponent<Collider>() == null) col = gameObject.AddComponent<BoxCollider>();

        if (boxRenderer == null) boxRenderer = GetComponent<MeshRenderer>();
        if (boxRenderer == null) boxRenderer = GetComponentInChildren<MeshRenderer>();

        if (rb != null)
        {
            rb.mass = 8f;
            rb.linearDamping = 0.05f;
            rb.angularDamping = 0.5f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    public string GetFormattedTargetDeliveryTime()
    {
        int totalMins = Mathf.RoundToInt(targetDeliveryHour * 60f);
        int h = totalMins / 60;
        int m = totalMins % 60;
        return $"{h:D2}:{m:D2}";
    }

    public void SetupPackage(string pointId, string address, string recipient, int reward, int penalty, CargoType type = CargoType.Standard, int xp = 80, string addressDescription = "", Material customMaterial = null, bool customPrefab = false, float expressHour = 13.0f)
    {
        isCustomPrefab = customPrefab;
        targetDeliveryHour = expressHour;
        EnsurePhysicsComponents();

        targetPointId = pointId;
        targetAddressName = address;
        recipientName = string.IsNullOrEmpty(recipient) ? "Resident" : recipient;
        targetAddressDescription = addressDescription;
        deliveryReward = reward;
        wrongPenalty = penalty;
        cargoType = type;
        xpReward = xp;
        health = 100f;
        spawnImmunityUntil = Time.time + spawnImmunityDuration;

        // Apply bonus multiplier for specialty cargo
        if (cargoType == CargoType.Fragile)
        {
            deliveryReward = Mathf.RoundToInt(deliveryReward * 1.5f);
            wrongPenalty = Mathf.RoundToInt(wrongPenalty * 1.5f);
            xpReward = Mathf.RoundToInt(xpReward * 1.4f);
        }
        else if (cargoType == CargoType.Express)
        {
            deliveryReward = Mathf.RoundToInt(deliveryReward * 1.8f);
            xpReward = Mathf.RoundToInt(xpReward * 1.6f);
        }

        if (cargoData == null)
        {
            cargoData = new CargoItem
            {
                trackingNumber = $"PKG-{Random.Range(1000, 9999)}",
                recipientName = recipientName,
                targetPointId = pointId,
                targetAddressName = address,
                targetAddressDescription = addressDescription,
                cargoType = cargoType,
                deliveryReward = deliveryReward,
                wrongDeliveryPenalty = wrongPenalty,
                isDelivered = false
            };
        }
        else
        {
            cargoData.targetAddressDescription = addressDescription;
        }

        if (!isCustomPrefab)
        {
            ApplyRandomDimensionsAndColor(customMaterial);
        }
        else if (customMaterial != null && boxRenderer != null)
        {
            boxRenderer.material = customMaterial;
        }

        BuildShippingLabel();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (cargoType != CargoType.Fragile || isBroken) return;
        if (isBeingCarried) return; // Never take damage while held by player!
        if (Time.time < spawnImmunityUntil) return; // Do not take damage during initial spawn settling (3.5s)
        if (Time.time - lastDamageTime < damageCooldown) return; // Cooldown to prevent multi-hit bounce/rolling frame spam

        float impactSpeed = collision.relativeVelocity.magnitude;
        if (rb != null && collision.impulse.magnitude > 0.01f)
        {
            float impulseSpeed = collision.impulse.magnitude / rb.mass;
            if (impulseSpeed > impactSpeed) impactSpeed = impulseSpeed;
        }

        // Detect package-to-package collisions (cardboard-to-cardboard contact is softer)
        bool hitOtherCargo = collision.gameObject.GetComponent<PhysicalCargoPackage>() != null || 
                             collision.transform.GetComponentInParent<PhysicalCargoPackage>() != null;

        float effectiveThreshold = hitOtherCargo ? (minDamageSpeedThreshold + 1.8f) : minDamageSpeedThreshold;

        // Damage threshold check
        if (impactSpeed > effectiveThreshold)
        {
            lastDamageTime = Time.time;
            float excessSpeed = impactSpeed - effectiveThreshold;

            // Kinetic Energy & Drop Height Damage Formula:
            float damage = (excessSpeed * damageMultiplier) + (excessSpeed * excessSpeed * 3.5f);

            // Reduce damage by 80% for package-on-package collisions
            if (hitOtherCargo)
            {
                damage *= packageCollisionDamageRatio;
            }

            health = Mathf.Max(0f, health - damage);
            UpdateLabelText();

            if (isBroken)
            {
                Debug.LogWarning($"<color=#FF4444>[PhysicalCargoPackage] FRAGILE CARGO BROKEN! Hit: '{collision.gameObject.name}', Impact: {impactSpeed:F1} m/s (Damage: -{damage:F0} HP)</color>");
            }
            else
            {
                Debug.Log($"<color=#FFAA00>[PhysicalCargoPackage] Fragile Cargo Damaged! Hit: '{collision.gameObject.name}', Impact: {impactSpeed:F1} m/s, Damage: -{damage:F0} HP (Health: {health:F0}/100)</color>");
            }
        }
    }

    private void ApplyRandomDimensionsAndColor(Material customMaterial = null)
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

        // 2. Realistic Color or Custom Material
        if (boxRenderer != null)
        {
            if (customMaterial != null)
            {
                boxRenderer.material = customMaterial;
            }
            else
            {
                Color boxColor = CardboardPalette[Random.Range(0, CardboardPalette.Length)];
                boxRenderer.material = CreateLitMaterial(boxColor, 0.15f);
            }
        }
    }

    private void BuildShippingLabel()
    {
        if (shippingLabel != null) Destroy(shippingLabel);

        // Dynamically compute the top surface position and dimensions
        float topY = 0.505f;
        float labelScaleX = 0.85f;
        float labelScaleZ = 0.75f;

        if (col != null)
        {
            topY = col.center.y + (col.size.y * 0.5f) + 0.005f;
            labelScaleX = Mathf.Clamp(col.size.x * 0.75f, 0.25f, 1.2f);
            labelScaleZ = Mathf.Clamp(col.size.z * 0.65f, 0.25f, 1.2f);
        }
        else if (boxRenderer != null)
        {
            Vector3 topWorld = boxRenderer.bounds.center + new Vector3(0f, boxRenderer.bounds.extents.y + 0.005f, 0f);
            topY = transform.InverseTransformPoint(topWorld).y;
            Vector3 localExtents = transform.InverseTransformVector(boxRenderer.bounds.extents);
            labelScaleX = Mathf.Clamp(Mathf.Abs(localExtents.x) * 1.5f, 0.25f, 1.2f);
            labelScaleZ = Mathf.Clamp(Mathf.Abs(localExtents.z) * 1.3f, 0.25f, 1.2f);
        }

        // White shipping label sticker on top of the box
        shippingLabel = new GameObject("ShippingLabel");
        shippingLabel.transform.SetParent(transform, false);
        shippingLabel.transform.localPosition = new Vector3(0f, topY, 0f);
        shippingLabel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        shippingLabel.transform.localScale = new Vector3(labelScaleX, labelScaleZ, 1f);

        // Clean quad mesh without any MeshCollider (fixes dynamic Rigidbody concave warning)
        GameObject quadObj = new GameObject("LabelBackground");
        quadObj.transform.SetParent(shippingLabel.transform, false);
        quadObj.transform.localPosition = Vector3.zero;
        quadObj.transform.localRotation = Quaternion.identity;
        quadObj.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

        MeshFilter mf = quadObj.AddComponent<MeshFilter>();
        mf.sharedMesh = GetQuadMesh();

        MeshRenderer quadRenderer = quadObj.AddComponent<MeshRenderer>();
        if (quadRenderer != null)
        {
            Color labelBgColor = new Color(0.96f, 0.96f, 0.94f);
            if (cargoType == CargoType.Fragile) labelBgColor = new Color(1.0f, 0.92f, 0.88f); // Soft peach/red tint
            else if (cargoType == CargoType.Express) labelBgColor = new Color(0.88f, 0.96f, 1.0f); // Soft cyan tint

            quadRenderer.material = CreateLitMaterial(labelBgColor, 0.05f);
        }

        // Text on the label (Only Recipient Name & Address, auto-sized and truncated with ...)
        GameObject labelTextObj = new GameObject("LabelText");
        labelTextObj.transform.SetParent(shippingLabel.transform, false);
        labelTextObj.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        labelTextObj.transform.localRotation = Quaternion.identity;

        RectTransform textRect = labelTextObj.AddComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(0.82f, 0.82f);

        labelText = labelTextObj.AddComponent<TextMeshPro>();
        labelText.fontSize = 1.35f;
        labelText.fontStyle = FontStyles.Bold;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = new Color(0.12f, 0.12f, 0.15f);
        labelText.enableWordWrapping = true;
        labelText.enableAutoSizing = true;
        labelText.fontSizeMin = 0.65f;
        labelText.fontSizeMax = 1.35f;
        labelText.overflowMode = TextOverflowModes.Ellipsis;
        labelText.margin = new Vector4(0.04f, 0.04f, 0.04f, 0.04f);

        UpdateLabelText();
    }

    private void UpdateLabelText()
    {
        if (labelText == null) return;

        string shortRecipient = TruncateWithEllipsis(recipientName, 15);
        string shortAddress = TruncateWithEllipsis(targetAddressName, 18);

        string badge = "";
        if (isBroken) badge = "<color=#FF2222>[BROKEN / DAMAGED]</color>\n";
        else if (cargoType == CargoType.Fragile)
        {
            if (health < 100f) badge = $"<color=#FF5500>[FRAGILE {Mathf.CeilToInt(health)}%]</color>\n";
            else badge = "<color=#FF5500>[FRAGILE]</color>\n";
        }
        else if (cargoType == CargoType.Express) badge = $"<color=#0088FF>[EXPRESS - {GetFormattedTargetDeliveryTime()}]</color>\n";

        labelText.text = $"{badge}{shortRecipient}\n<size=85%>{shortAddress}</size>";
    }

    private string TruncateWithEllipsis(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return "";
        if (text.Length <= maxLength) return text;
        return text.Substring(0, maxLength - 3).TrimEnd() + "...";
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

    private static Mesh cachedQuadMesh;
    private static Mesh GetQuadMesh()
    {
        if (cachedQuadMesh == null)
        {
            cachedQuadMesh = new Mesh
            {
                name = "StickerQuad",
                vertices = new Vector3[]
                {
                    new Vector3(-0.5f, -0.5f, 0f),
                    new Vector3(0.5f, -0.5f, 0f),
                    new Vector3(-0.5f, 0.5f, 0f),
                    new Vector3(0.5f, 0.5f, 0f)
                },
                uv = new Vector2[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f)
                },
                triangles = new int[] { 0, 2, 1, 2, 3, 1 }
            };
            cachedQuadMesh.RecalculateNormals();
        }
        return cachedQuadMesh;
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
    /// Evaluates the end-of-day package status (location, damage state, express timing).
    /// </summary>
    public CargoDeliveryResult EvaluateEndOfDayResult()
    {
        DeliveryPoint nearbyPoint = FindNearbyDeliveryPoint();
        string tracking = cargoData != null ? cargoData.trackingNumber : $"PKG-{targetPointId}";

        CargoDeliveryResult result = new CargoDeliveryResult
        {
            package = this,
            cargoType = cargoType,
            trackingNumber = tracking,
            recipientName = recipientName,
            targetAddress = targetAddressName,
            isBroken = isBroken,
            isExpressBonus = false
        };

        if (nearbyPoint != null)
        {
            result.actualAddress = nearbyPoint.addressName;
            bool isMatch = string.Equals(targetPointId.Trim(), nearbyPoint.pointId.Trim(), System.StringComparison.OrdinalIgnoreCase);

            if (isMatch)
            {
                if (isBroken)
                {
                    result.status = CargoDeliveryStatus.Broken;
                    result.moneyChange = -wrongPenalty * 2; // Broken fragile penalty
                    result.xpAwarded = 0;
                }
                else
                {
                    result.status = CargoDeliveryStatus.Correct;
                    result.moneyChange = deliveryReward;
                    result.xpAwarded = xpReward;

                    // Check Express timing bonus (before targetDeliveryHour)
                    if (cargoType == CargoType.Express)
                    {
                        if (DayTimeManager.Instance != null)
                        {
                            float currentDecHour = DayTimeManager.Instance.CurrentHour + (DayTimeManager.Instance.CurrentMinute / 60f);
                            if (currentDecHour <= targetDeliveryHour)
                            {
                                int bonus = Mathf.RoundToInt(deliveryReward * 0.4f);
                                result.moneyChange += bonus;
                                result.xpAwarded += 50;
                                result.isExpressBonus = true;
                            }
                        }
                    }
                }
            }
            else
            {
                result.status = CargoDeliveryStatus.WrongAddress;
                result.moneyChange = -wrongPenalty;
                result.xpAwarded = 0;
            }
        }
        else
        {
            result.actualAddress = "Undelivered (Vehicle / Street)";
            result.status = CargoDeliveryStatus.Undelivered;
            result.moneyChange = -wrongPenalty;
            result.xpAwarded = 0;
        }

        return result;
    }
}
