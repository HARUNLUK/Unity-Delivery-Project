using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class CarController : MonoBehaviour
{
    [Header("--- MOTOR & FREN AYARLARI ---")]
    public float motorForce = 16000f;
    public float reverseForce = 11000f;
    public float footBrakeForce = 100000f;
    public float handBrakeForce = 150000f;
    public float maxSteerAngle = 40f;
    public float turnAssistTorque = 3.5f;
    public Vector3 centerOfMassOffset = new Vector3(0, -1.0f, 0);

    [Header("--- EL FRENİ & DRİFT AYARLARI ---")]
    public float driftSidewaysStiffness = 0.25f;
    public float driftYawBoost = 4.0f;

    [Header("--- DRİFT TOPARLANMA DESTEĞİ ---")]
    [Tooltip("Gaza basınca aracın yan kaymadan düz hatta toparlanma hızı")]
    public float driftRecoveryRate = 8.0f;

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

    [Header("--- DİREKSİYON (STEERING WHEEL) ---")]
    [Tooltip("Sürüş sırasında dönecek direksiyon modeli objesi")]
    public Transform steeringWheel;

    [Tooltip("Direksiyon dönme çarpanı")]
    public float steeringWheelMultiplier = 4.0f;

    [Tooltip("Direksiyonun kendi etrafında döneceği yerel eksen")]
    public Vector3 steeringWheelRotationAxis = Vector3.forward;

    private Quaternion initialSteeringWheelRotation;

    private Rigidbody rb;
    private float currentSteerAngle;
    private float horizontalInput;
    private float verticalInput;
    private bool isHandbraking;

    private WheelFrictionCurve normalRearSidewaysFriction;
    private WheelFrictionCurve driftRearSidewaysFriction;
    private float currentRearStiffness;

    public float ForwardSpeed { get; private set; }

    private void Awake()
    {
        // Araç üzerindeki tüm MeshCollider'ları otomatik Convex yap (Fizik motoru çakışmasını engelle)
        MeshCollider[] meshColliders = GetComponentsInChildren<MeshCollider>();
        foreach (var mc in meshColliders)
        {
            mc.convex = true;
        }
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.centerOfMass += centerOfMassOffset;
            rb.maxAngularVelocity = 7f;
        }

        if (rearLeftCollider != null)
        {
            normalRearSidewaysFriction = rearLeftCollider.sidewaysFriction;
            driftRearSidewaysFriction = rearLeftCollider.sidewaysFriction;
            currentRearStiffness = normalRearSidewaysFriction.stiffness;
        }

        if (steeringWheel != null)
        {
            initialSteeringWheelRotation = steeringWheel.localRotation;
        }
    }

    private void Update()
    {
        GetInput();
    }

    private void FixedUpdate()
    {
        CalculateSpeed();
        HandleSteering();
        HandleMotorAndBrakes();
        HandleDriftRecovery();
        UpdateWheelMeshes();
    }

    private void CalculateSpeed()
    {
        if (rb != null)
        {
            ForwardSpeed = Vector3.Dot(transform.forward, rb.linearVelocity);
        }
        else
        {
            ForwardSpeed = 0f;
        }
    }

    private void GetInput()
    {
        horizontalInput = 0f;
        verticalInput = 0f;
        isHandbraking = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) verticalInput += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) verticalInput -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontalInput += 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontalInput -= 1f;
            if (Keyboard.current.spaceKey.isPressed) isHandbraking = true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (verticalInput == 0f) verticalInput = Input.GetAxis("Vertical");
        if (horizontalInput == 0f) horizontalInput = Input.GetAxis("Horizontal");
        if (!isHandbraking) isHandbraking = Input.GetKey(KeyCode.Space);
#endif
    }

    private void HandleSteering()
    {
        currentSteerAngle = maxSteerAngle * horizontalInput;

        // Ackermann Steering Angle Differential (İç tekerlek daha geniş döner, tekerlek kasması sıfırlanır)
        if (horizontalInput > 0.05f)
        {
            if (frontLeftCollider != null) frontLeftCollider.steerAngle = currentSteerAngle * 0.85f;
            if (frontRightCollider != null) frontRightCollider.steerAngle = currentSteerAngle * 1.05f;
        }
        else if (horizontalInput < -0.05f)
        {
            if (frontLeftCollider != null) frontLeftCollider.steerAngle = currentSteerAngle * 1.05f;
            if (frontRightCollider != null) frontRightCollider.steerAngle = currentSteerAngle * 0.85f;
        }
        else
        {
            if (frontLeftCollider != null) frontLeftCollider.steerAngle = 0f;
            if (frontRightCollider != null) frontRightCollider.steerAngle = 0f;
        }

        // Agile Yaw Torque Assist (Dönüşlerde araca çeviklik desteği vererek ağırlık hissini ortadan kaldırır)
        if (rb != null && Mathf.Abs(horizontalInput) > 0.05f && rb.linearVelocity.magnitude > 0.5f)
        {
            float directionSign = ForwardSpeed >= -0.2f ? 1f : -1f;
            rb.AddTorque(transform.up * (horizontalInput * turnAssistTorque * directionSign), ForceMode.Acceleration);
        }

        if (steeringWheel != null)
        {
            float steerRot = currentSteerAngle * steeringWheelMultiplier;
            steeringWheel.localRotation = initialSteeringWheelRotation * Quaternion.AngleAxis(-steerRot, steeringWheelRotationAxis);
        }
    }

    private void HandleMotorAndBrakes()
    {
        float motor = 0f;
        float footBrake = 0f;

        if (isHandbraking)
        {
            HandleHandbrake();
            return;
        }

        if (ForwardSpeed > 1.0f && verticalInput < -0.05f)
        {
            footBrake = footBrakeForce * Mathf.Abs(verticalInput);
            motor = 0f;
        }
        else if (ForwardSpeed < -1.0f && verticalInput > 0.05f)
        {
            footBrake = footBrakeForce * Mathf.Abs(verticalInput);
            motor = 0f;
        }
        else
        {
            footBrake = 0f;

            if (verticalInput > 0.05f)
            {
                motor = verticalInput * motorForce;
            }
            else if (verticalInput < -0.05f)
            {
                motor = verticalInput * reverseForce;
            }
            else
            {
                motor = 0f;
                footBrake = 500f;
            }
        }

        ApplyMotorTorque(motor);
        ApplyBrakes(footBrake);
    }

    private void HandleHandbrake()
    {
        bool isSteering = Mathf.Abs(horizontalInput) > 0.1f;

        if (isSteering)
        {
            currentRearStiffness = driftSidewaysStiffness;
            SetRearStiffness(currentRearStiffness);

            ApplyMotorTorque(0f);
            if (rearLeftCollider != null) rearLeftCollider.brakeTorque = handBrakeForce * 0.5f;
            if (rearRightCollider != null) rearRightCollider.brakeTorque = handBrakeForce * 0.5f;
            if (frontLeftCollider != null) frontLeftCollider.brakeTorque = 0f;
            if (frontRightCollider != null) frontRightCollider.brakeTorque = 0f;

            if (rb != null && rb.linearVelocity.magnitude > 4f)
            {
                rb.AddTorque(transform.up * horizontalInput * driftYawBoost, ForceMode.Acceleration);
            }
        }
        else
        {
            currentRearStiffness = normalRearSidewaysFriction.stiffness;
            SetRearStiffness(currentRearStiffness);

            ApplyMotorTorque(0f);
            ApplyBrakes(handBrakeForce);

            if (rb != null)
            {
                if (rb.linearVelocity.magnitude > 0.2f)
                {
                    rb.linearVelocity = Vector3.MoveTowards(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 20f);
                }
                else
                {
                    rb.linearVelocity = Vector3.zero;
                }
            }
        }
    }

    private void HandleDriftRecovery()
    {
        if (isHandbraking) return;

        currentRearStiffness = Mathf.MoveTowards(currentRearStiffness, normalRearSidewaysFriction.stiffness, Time.fixedDeltaTime * 2.5f);
        SetRearStiffness(currentRearStiffness);

        // Yalnızca düz giderken toparla (dönüş yaparken dönüş açısını KISITLAMA!)
        if (Mathf.Abs(horizontalInput) < 0.1f && verticalInput > 0.1f && rb != null && ForwardSpeed > 2f)
        {
            Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);

            if (Mathf.Abs(localVel.x) > 0.5f)
            {
                localVel.x = Mathf.MoveTowards(localVel.x, 0f, Time.fixedDeltaTime * driftRecoveryRate);
                rb.linearVelocity = transform.TransformDirection(localVel);

                Vector3 angularVel = rb.angularVelocity;
                angularVel.y = Mathf.MoveTowards(angularVel.y, 0f, Time.fixedDeltaTime * 4f);
                rb.angularVelocity = angularVel;
            }
        }
    }

    private void SetRearStiffness(float stiffness)
    {
        if (rearLeftCollider != null)
        {
            WheelFrictionCurve f = rearLeftCollider.sidewaysFriction;
            f.stiffness = stiffness;
            rearLeftCollider.sidewaysFriction = f;
        }

        if (rearRightCollider != null)
        {
            WheelFrictionCurve f = rearRightCollider.sidewaysFriction;
            f.stiffness = stiffness;
            rearRightCollider.sidewaysFriction = f;
        }
    }

    private void ApplyMotorTorque(float force)
    {
        // AWD Tork Dağılımı (Ön tekerlekler dönüş yönüne doğru aracı çeker, hantallığı bitirir)
        float frontForce = force * 0.35f;
        float rearForce = force * 0.65f;

        if (frontLeftCollider != null) frontLeftCollider.motorTorque = frontForce;
        if (frontRightCollider != null) frontRightCollider.motorTorque = frontForce;
        if (rearLeftCollider != null) rearLeftCollider.motorTorque = rearForce;
        if (rearRightCollider != null) rearRightCollider.motorTorque = rearForce;
    }

    private void ApplyBrakes(float force)
    {
        if (frontLeftCollider != null) frontLeftCollider.brakeTorque = force;
        if (frontRightCollider != null) frontRightCollider.brakeTorque = force;
        if (rearLeftCollider != null) rearLeftCollider.brakeTorque = force;
        if (rearRightCollider != null) rearRightCollider.brakeTorque = force;
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
