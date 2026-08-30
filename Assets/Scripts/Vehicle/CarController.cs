using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class CarController : MonoBehaviour
{
    [Header("--- MOTOR & FREN AYARLARI ---")]
    public float motorForce = 15000f;
    public float brakeForce = 25000f;
    public float maxSteerAngle = 35f;
    public Vector3 centerOfMassOffset = new Vector3(0, -1.2f, 0);

    [Header("--- EL FRENİ & DRİFT AYARLARI ---")]
    [Tooltip("El frenine basınca arka tekerleklerin kayma katsayısı (0.2 - 0.4 arası dengelidir)")]
    public float driftSidewaysStiffness = 0.25f;
    
    [Tooltip("Drift esnasında dönüşe verilen hafif ekstra açı desteği")]
    public float driftYawBoost = 4.0f;

    [Header("--- WHEEL COLLIDERS (Fizik Tekerlekleri) ---")]
    public WheelCollider frontLeftCollider;
    public WheelCollider frontRightCollider;
    public WheelCollider rearLeftCollider;
    public WheelCollider rearRightCollider;

    [Header("--- WHEEL MESHES (Görsel Tekerlek Modelleri) ---")]
    public Transform frontLeftMesh;
    public Transform frontRightMesh;
    public Transform rearLeftMesh;
    public Transform rearRightMesh;

    [Header("--- TEKERLEK MODEL AÇI VE POZİSYON AYARI ---")]
    public Vector3 wheelMeshRotationOffset = new Vector3(-90f, 0f, 0f);
    public Vector3 wheelMeshPositionOffset = Vector3.zero;

    private Rigidbody rb;
    private float currentSteerAngle;
    private float currentMotorForce;

    private float horizontalInput;
    private float verticalInput;
    private bool isBraking;

    private WheelFrictionCurve normalRearSidewaysFriction;
    private WheelFrictionCurve driftRearSidewaysFriction;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.centerOfMass += centerOfMassOffset;
            rb.maxAngularVelocity = 7f; // Kontrolsüz fırıldak gibi dönmeyi engeller
        }

        if (rearLeftCollider != null)
        {
            normalRearSidewaysFriction = rearLeftCollider.sidewaysFriction;
            driftRearSidewaysFriction = rearLeftCollider.sidewaysFriction;
            driftRearSidewaysFriction.stiffness = driftSidewaysStiffness;
        }
    }

    private void Update()
    {
        GetInput();
    }

    private void FixedUpdate()
    {
        HandleMotor();
        HandleSteering();
        HandleDriftAndBrakes();
        UpdateWheelMeshes();
    }

    private void GetInput()
    {
        horizontalInput = 0f;
        verticalInput = 0f;
        isBraking = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) verticalInput += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) verticalInput -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontalInput += 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontalInput -= 1f;
            if (Keyboard.current.spaceKey.isPressed) isBraking = true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (verticalInput == 0f) verticalInput = Input.GetAxis("Vertical");
        if (horizontalInput == 0f) horizontalInput = Input.GetAxis("Horizontal");
        if (!isBraking) isBraking = Input.GetKey(KeyCode.Space);
#endif
    }

    private void HandleMotor()
    {
        currentMotorForce = verticalInput * motorForce;

        if (rearLeftCollider != null) rearLeftCollider.motorTorque = currentMotorForce;
        if (rearRightCollider != null) rearRightCollider.motorTorque = currentMotorForce;
    }

    private void HandleSteering()
    {
        currentSteerAngle = maxSteerAngle * horizontalInput;
        if (frontLeftCollider != null) frontLeftCollider.steerAngle = currentSteerAngle;
        if (frontRightCollider != null) frontRightCollider.steerAngle = currentSteerAngle;
    }

    private void HandleDriftAndBrakes()
    {
        if (isBraking)
        {
            // Arka sürtünmeyi düşür
            driftRearSidewaysFriction.stiffness = driftSidewaysStiffness;
            if (rearLeftCollider != null) rearLeftCollider.sidewaysFriction = driftRearSidewaysFriction;
            if (rearRightCollider != null) rearRightCollider.sidewaysFriction = driftRearSidewaysFriction;

            // Arka tekerleklere fren uygula
            ApplyBrakes(rearLeftCollider, brakeForce * 0.7f);
            ApplyBrakes(rearRightCollider, brakeForce * 0.7f);
            ApplyBrakes(frontLeftCollider, 0f);
            ApplyBrakes(frontRightCollider, 0f);

            // Sadece araç hareket halindeyse kontrollü yanlama torku ver
            if (Mathf.Abs(horizontalInput) > 0.1f && rb != null && rb.linearVelocity.magnitude > 5f)
            {
                rb.AddTorque(transform.up * horizontalInput * driftYawBoost, ForceMode.Acceleration);
            }
        }
        else
        {
            // Normal sürtünmeye dön
            if (rearLeftCollider != null) rearLeftCollider.sidewaysFriction = normalRearSidewaysFriction;
            if (rearRightCollider != null) rearRightCollider.sidewaysFriction = normalRearSidewaysFriction;

            // Frenleri bırak
            ApplyBrakes(frontLeftCollider, 0f);
            ApplyBrakes(frontRightCollider, 0f);
            ApplyBrakes(rearLeftCollider, 0f);
            ApplyBrakes(rearRightCollider, 0f);
        }
    }

    private void ApplyBrakes(WheelCollider col, float force)
    {
        if (col != null)
        {
            col.brakeTorque = force;
        }
    }

    private void UpdateWheelMeshes()
    {
        SyncSingleWheel(frontLeftCollider, frontLeftMesh);
        SyncSingleWheel(frontRightCollider, frontRightMesh);
        SyncSingleWheel(rearLeftCollider, rearLeftMesh);
        SyncSingleWheel(rearRightCollider, rearRightMesh);
    }

    private void SyncSingleWheel(WheelCollider col, Transform mesh)
    {
        if (col == null || mesh == null) return;

        Vector3 pos;
        Quaternion rot;
        col.GetWorldPose(out pos, out rot);

        mesh.position = pos + mesh.TransformDirection(wheelMeshPositionOffset);
        mesh.rotation = rot * Quaternion.Euler(wheelMeshRotationOffset);
    }
}
