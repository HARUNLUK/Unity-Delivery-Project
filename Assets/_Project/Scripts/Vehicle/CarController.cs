using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class CarController : MonoBehaviour
{
    [Header("--- MOTOR & BRAKE SETTINGS ---")]
    public float motorForce = 16000f;
    public float reverseForce = 11000f;
    public float footBrakeForce = 100000f;
    public float handBrakeForce = 150000f;
    public float maxSteerAngle = 40f;
    public float turnAssistTorque = 3.5f;
    public Vector3 centerOfMassOffset = new Vector3(0, -1.0f, 0);

    [Header("--- HANDBRAKE & DRIFT SETTINGS ---")]
    public float driftSidewaysStiffness = 0.25f;
    public float driftYawBoost = 4.0f;

    [Header("--- DRIFT RECOVERY ASSIST ---")]
    [Tooltip("Recovery rate to straighten vehicle when accelerating after drift")]
    public float driftRecoveryRate = 8.0f;

    [Header("--- WHEEL COLLIDERS (Physics Wheels) ---")]
    public WheelCollider frontLeftCollider;
    public WheelCollider frontRightCollider;
    public WheelCollider rearLeftCollider;
    public WheelCollider rearRightCollider;

    [Header("--- WHEEL MESHES (Visual Wheel Models) ---")]
    public Transform frontLeftMesh;
    public Transform frontRightMesh;
    public Transform rearLeftMesh;
    public Transform rearRightMesh;

    [Header("--- WHEEL MODEL ROTATION & POSITION OFFSET ---")]
    public Vector3 wheelMeshRotationOffset = new Vector3(-90f, 0f, 0f);
    public Vector3 wheelMeshPositionOffset = Vector3.zero;

    [Header("--- STEERING WHEEL & SMOOTH STEERING ---")]
    [Tooltip("Steering wheel visual model object to rotate while driving")]
    public Transform steeringWheel;

    [Tooltip("Steering wheel rotation multiplier")]
    public float steeringWheelMultiplier = 3.5f;

    [Tooltip("Steering angle interpolation speed (Degrees/Second)")]
    public float steerSpeed = 160f;

    [Tooltip("Steering return-to-center speed when key is released (Degrees/Second)")]
    public float steerReturnSpeed = 220f;

    [Tooltip("If checked, forces initial steering wheel rotation to customInitialSteeringEuler")]
    public bool overrideInitialSteeringEuler = false;

    [Tooltip("Custom initial Euler angles (e.g. for Pickup: X: 0, Y: -90, Z: 0)")]
    public Vector3 customInitialSteeringEuler = new Vector3(0f, -90f, 0f);

    private Vector3 baseSteeringEuler = Vector3.zero;
    private bool isBaseEulerCaptured = false;

    private Rigidbody rb;
    private float currentSteerAngle;
    public float horizontalInput;
    public float verticalInput;
    public bool isHandbraking;

    public float VerticalInput => verticalInput;
    public float HorizontalInput => horizontalInput;
    public bool IsHandbraking => isHandbraking;

    private WheelFrictionCurve normalRearSidewaysFriction;
    private WheelFrictionCurve driftRearSidewaysFriction;
    private float currentRearStiffness;

    public float ForwardSpeed { get; private set; }

    private void Awake()
    {
        EnsureBaseSteeringEuler();

        // Ensure all child MeshColliders are convex to prevent physics engine conflicts
        MeshCollider[] meshColliders = GetComponentsInChildren<MeshCollider>();
        foreach (var mc in meshColliders)
        {
            mc.convex = true;
        }
    }

    private void Start()
    {
        EnsureBaseSteeringEuler();

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

    public void EnsureBaseSteeringEuler()
    {
        if (isBaseEulerCaptured || steeringWheel == null) return;

        if (overrideInitialSteeringEuler)
        {
            baseSteeringEuler = customInitialSteeringEuler;
            steeringWheel.localEulerAngles = baseSteeringEuler;
        }
        else
        {
            baseSteeringEuler = steeringWheel.localEulerAngles;
            customInitialSteeringEuler = baseSteeringEuler;
        }

        isBaseEulerCaptured = true;
    }

    private void OnDisable()
    {
        ClearAllForces();
    }

    public void ClearAllForces()
    {
        horizontalInput = 0f;
        verticalInput = 0f;
        isHandbraking = false;
        currentSteerAngle = 0f;

        // 1. Zero Throttle (Cut motor torque across all wheels)
        if (frontLeftCollider != null) frontLeftCollider.motorTorque = 0f;
        if (frontRightCollider != null) frontRightCollider.motorTorque = 0f;
        if (rearLeftCollider != null) rearLeftCollider.motorTorque = 0f;
        if (rearRightCollider != null) rearRightCollider.motorTorque = 0f;

        // 2. Straighten steering
        if (frontLeftCollider != null) frontLeftCollider.steerAngle = 0f;
        if (frontRightCollider != null) frontRightCollider.steerAngle = 0f;

        // 3. Natural Deceleration Brake (Smoothly halts vehicle like idling to park)
        float neutralBrake = 2000f;
        if (frontLeftCollider != null) frontLeftCollider.brakeTorque = neutralBrake;
        if (frontRightCollider != null) frontRightCollider.brakeTorque = neutralBrake;
        if (rearLeftCollider != null) rearLeftCollider.brakeTorque = neutralBrake;
        if (rearRightCollider != null) rearRightCollider.brakeTorque = neutralBrake;

        if (steeringWheel != null && isBaseEulerCaptured)
        {
            steeringWheel.localEulerAngles = baseSteeringEuler;
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
        // 1. Smooth Steering Interpolation (Tekerleklerin ve direksiyonun kademeli, yumuşak dönmesi)
        float targetSteerAngle = maxSteerAngle * horizontalInput;
        float speed = Mathf.Abs(horizontalInput) > 0.05f ? steerSpeed : steerReturnSpeed;
        currentSteerAngle = Mathf.MoveTowards(currentSteerAngle, targetSteerAngle, speed * Time.fixedDeltaTime);

        // 2. Ackermann Steering Angle Differential (İç tekerlek daha geniş döner, tekerlek kasması sıfırlanır)
        if (currentSteerAngle > 0.05f)
        {
            if (frontLeftCollider != null) frontLeftCollider.steerAngle = currentSteerAngle * 0.85f;
            if (frontRightCollider != null) frontRightCollider.steerAngle = currentSteerAngle * 1.05f;
        }
        else if (currentSteerAngle < -0.05f)
        {
            if (frontLeftCollider != null) frontLeftCollider.steerAngle = currentSteerAngle * 1.05f;
            if (frontRightCollider != null) frontRightCollider.steerAngle = currentSteerAngle * 0.85f;
        }
        else
        {
            if (frontLeftCollider != null) frontLeftCollider.steerAngle = 0f;
            if (frontRightCollider != null) frontRightCollider.steerAngle = 0f;
        }

        // 3. Agile Yaw Torque Assist (Dönüşlerde araca çeviklik desteği)
        if (rb != null && Mathf.Abs(currentSteerAngle) > 0.5f && rb.linearVelocity.magnitude > 0.5f)
        {
            float directionSign = ForwardSpeed >= -0.2f ? 1f : -1f;
            float steerRatio = currentSteerAngle / maxSteerAngle;
            rb.AddTorque(transform.up * (steerRatio * turnAssistTorque * directionSign), ForceMode.Acceleration);
        }

        // 4. Direksiyon Modeli Rotasyonu (SADECE X açısını değiştirir; Y ve Z açıları daima korunur)
        if (steeringWheel != null)
        {
            EnsureBaseSteeringEuler();
            float steerRot = currentSteerAngle * steeringWheelMultiplier;
            float targetX = baseSteeringEuler.x - steerRot;
            steeringWheel.localEulerAngles = new Vector3(targetX, baseSteeringEuler.y, baseSteeringEuler.z);
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
        // AWD Torque Distribution (Front wheels pull towards steering angle, preventing sluggishness)
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
