using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class CarController : MonoBehaviour
{
    [Header("--- MOTOR & FREN AYARLARI ---")]
    public float motorForce = 15000f;
    public float reverseForce = 10000f;
    public float footBrakeForce = 100000f;
    public float handBrakeForce = 150000f;
    public float maxSteerAngle = 35f;
    public Vector3 centerOfMassOffset = new Vector3(0, -1.2f, 0);

    [Header("--- EL FRENİ & DRİFT AYARLARI ---")]
    public float driftSidewaysStiffness = 0.25f;
    public float driftYawBoost = 4.0f;

    [Header("--- DRİFT TOPARLANMA DESTEĞİ ---")]
    [Tooltip("Gaza basınca aracın yan kaymadan düz hatta toparlanma hızı (Yüksek = Hızlı toparlar)")]
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

    private Rigidbody rb;
    private float currentSteerAngle;
    private float horizontalInput;
    private float verticalInput;
    private bool isHandbraking;

    private WheelFrictionCurve normalRearSidewaysFriction;
    private WheelFrictionCurve driftRearSidewaysFriction;
    private float currentRearStiffness;

    public float ForwardSpeed { get; private set; }

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
        if (frontLeftCollider != null) frontLeftCollider.steerAngle = currentSteerAngle;
        if (frontRightCollider != null) frontRightCollider.steerAngle = currentSteerAngle;
    }

    private void HandleMotorAndBrakes()
    {
        float motor = 0f;
        float footBrake = 0f;

        // 1. SPACE (EL FRENİ)
        if (isHandbraking)
        {
            HandleHandbrake();
            return;
        }

        // 2. İLERİ GİDERKEN S'YE BASILIRSA -> AYAK FRENİ
        if (ForwardSpeed > 1.0f && verticalInput < -0.05f)
        {
            footBrake = footBrakeForce * Mathf.Abs(verticalInput);
            motor = 0f;
        }
        // 3. GERİ GİDERKEN W'YA BASILIRSA -> AYAK FRENİ
        else if (ForwardSpeed < -1.0f && verticalInput > 0.05f)
        {
            footBrake = footBrakeForce * Mathf.Abs(verticalInput);
            motor = 0f;
        }
        // 4. NORMAL SÜRÜŞ
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
                footBrake = 500f; // Doğal motor direnci
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
            // Dönüşlü El Freni -> Drift
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
            // Düz El Freni -> Tam Durdurma
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

    /// <summary>
    /// Drift sonrasında gaza basıldığında aracın hızla çizgiye oturmasını ve kendini toplamasını sağlar
    /// </summary>
    private void HandleDriftRecovery()
    {
        if (isHandbraking) return;

        // El freni bırakıldığında arka tekerlek tutuşunu yumuşakça normale çek
        currentRearStiffness = Mathf.MoveTowards(currentRearStiffness, normalRearSidewaysFriction.stiffness, Time.fixedDeltaTime * 2.5f);
        SetRearStiffness(currentRearStiffness);

        // Gaza basılıyorsa ve araç yan kayıyorsa burnunu sürüş yönüne hızla toparla
        if (verticalInput > 0.1f && rb != null && ForwardSpeed > 2f)
        {
            Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);

            // Yan kayma hızını (X ekseni) sıfıra doğru sönümle (önden çekiş doğrultma etkisi)
            if (Mathf.Abs(localVel.x) > 0.5f)
            {
                localVel.x = Mathf.MoveTowards(localVel.x, 0f, Time.fixedDeltaTime * driftRecoveryRate);
                rb.linearVelocity = transform.TransformDirection(localVel);

                // Aşırı savrulma açısal hızını dengele
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
        if (rearLeftCollider != null) rearLeftCollider.motorTorque = force;
        if (rearRightCollider != null) rearRightCollider.motorTorque = force;
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
