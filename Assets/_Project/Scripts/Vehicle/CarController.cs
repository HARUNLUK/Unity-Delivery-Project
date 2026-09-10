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

    [Header("--- TUNING & PERFORMANCE BOOST ---")]
    public float tuningTorqueMultiplier = 1.0f;

    [Header("--- SPEED LIMITS & CONDITION SCALING ---")]
    [Tooltip("Maximum forward speed at 100% condition in m/s (35 m/s ~= 126 km/h)")]
    public float maxForwardSpeed = 35f;

    [Tooltip("Maximum reverse speed in m/s (10 m/s ~= 36 km/h)")]
    public float maxReverseSpeed = 10f;

    [Tooltip("Minimum forward speed cap when vehicle condition is at 0% / Limp Mode in m/s (8.5 m/s ~= 30 km/h)")]
    public float minConditionMaxSpeed = 8.5f;

    [Tooltip("Motor torque multiplier when condition is at 0% / Limp Mode")]
    public float minConditionTorqueMultiplier = 0.35f;

    [Range(0f, 1f)]
    public float currentConditionRatio = 1.0f; // 1.0 = 100%, 0.0 = 0%

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
            rb.centerOfMass = new Vector3(0f, -0.5f, 0f);
            rb.maxAngularVelocity = 6f;
            rb.linearDamping = 0.05f;
            rb.angularDamping = 1.0f;
            rb.maxDepenetrationVelocity = 5.0f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        WheelCollider[] wheels = new WheelCollider[] { frontLeftCollider, frontRightCollider, rearLeftCollider, rearRightCollider };
        foreach (var wc in wheels)
        {
            if (wc != null)
            {
                JointSpring s = wc.suspensionSpring;
                if (s.spring < 25000f) s.spring = 35000f;
                if (s.damper < 3000f) s.damper = 4500f;
                s.targetPosition = 0.5f;
                wc.suspensionSpring = s;
                wc.wheelDampingRate = 0.5f;
            }
        }

        if (rearLeftCollider != null)
        {
            normalRearSidewaysFriction = rearLeftCollider.sidewaysFriction;
            driftRearSidewaysFriction = rearLeftCollider.sidewaysFriction;
            currentRearStiffness = normalRearSidewaysFriction.stiffness;
        }

        DrivableVehicle dv = GetComponent<DrivableVehicle>();
        if (dv != null)
        {
            int stage = PlayerPrefs.GetInt("Vehicle_TuningStage_" + dv.vehicleId, 0);
            if (stage == 1) tuningTorqueMultiplier = 1.15f;
            else if (stage == 2) tuningTorqueMultiplier = 1.30f;
            else if (stage >= 3) tuningTorqueMultiplier = 1.50f;
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

    public void SetConditionRatio(float ratio)
    {
        currentConditionRatio = Mathf.Clamp01(ratio);
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

        if (FPSPlayerController.Instance != null && FPSPlayerController.Instance.IsUIBlockingInput())
        {
            return;
        }

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

        // 2. Hasarlı Direksiyon Yalpalaması (SADECE Kondisyon %30'un altına düştüğünde ve araç hareket halindeyken sağ-sol yalpalama)
        float wobbleAngle = 0f;
        if (currentConditionRatio < 0.30f && (Mathf.Abs(ForwardSpeed) > 0.8f || Mathf.Abs(verticalInput) > 0.1f))
        {
            float wobbleIntensity = Mathf.Clamp01((0.30f - currentConditionRatio) / 0.30f); // 0 (%30'da) -> 1.0 (%0'da)
            float wobbleTime = Time.time * 7.5f;
            // Organik sağ-sol yalpalama salınımı
            wobbleAngle = (Mathf.Sin(wobbleTime) * 0.7f + Mathf.Sin(wobbleTime * 2.3f) * 0.3f) * (maxSteerAngle * 0.25f * wobbleIntensity);
        }

        float effectiveSteerAngle = currentSteerAngle + wobbleAngle;

        // 3. Ackermann Steering Angle Differential (İç tekerlek daha geniş döner, tekerlek kasması sıfırlanır)
        if (effectiveSteerAngle > 0.05f)
        {
            if (frontLeftCollider != null) frontLeftCollider.steerAngle = effectiveSteerAngle * 0.85f;
            if (frontRightCollider != null) frontRightCollider.steerAngle = effectiveSteerAngle * 1.05f;
        }
        else if (effectiveSteerAngle < -0.05f)
        {
            if (frontLeftCollider != null) frontLeftCollider.steerAngle = effectiveSteerAngle * 1.05f;
            if (frontRightCollider != null) frontRightCollider.steerAngle = effectiveSteerAngle * 0.85f;
        }
        else
        {
            if (frontLeftCollider != null) frontLeftCollider.steerAngle = 0f;
            if (frontRightCollider != null) frontRightCollider.steerAngle = 0f;
        }

        // 4. Agile Yaw Torque Assist (Dönüşlerde araca çeviklik desteği)
        if (rb != null && Mathf.Abs(effectiveSteerAngle) > 0.5f && rb.linearVelocity.magnitude > 0.5f)
        {
            if (rb.angularVelocity.magnitude < 2.5f)
            {
                float directionSign = ForwardSpeed >= -0.2f ? 1f : -1f;
                float steerRatio = effectiveSteerAngle / maxSteerAngle;
                rb.AddTorque(transform.up * (steerRatio * turnAssistTorque * directionSign), ForceMode.Acceleration);
            }
        }

        // 5. Direksiyon Modeli Rotasyonu (SADECE X açısını değiştirir; Y ve Z açıları daima korunur)
        if (steeringWheel != null)
        {
            EnsureBaseSteeringEuler();
            float steerRot = effectiveSteerAngle * steeringWheelMultiplier;
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

        // 1. SADECE %0 Kondisyonda (Maks Hasar / Limp Mode) maksimum hızı kısıtla
        bool isMaxDamaged = currentConditionRatio <= 0.02f;
        float activeMaxSpeed = isMaxDamaged ? minConditionMaxSpeed : maxForwardSpeed;
        float baseTorqueMult = isMaxDamaged ? minConditionTorqueMultiplier : 1.0f;

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
                if (ForwardSpeed < activeMaxSpeed)
                {
                    motor = verticalInput * motorForce * tuningTorqueMultiplier * baseTorqueMult;
                }
                else
                {
                    motor = 0f; // Hız sınırına ulaşınca torku kes (sarsıntısız, düzgün)
                }
            }
            else if (verticalInput < -0.05f)
            {
                if (Mathf.Abs(ForwardSpeed) < maxReverseSpeed)
                {
                    motor = verticalInput * reverseForce * tuningTorqueMultiplier * baseTorqueMult;
                }
                else
                {
                    motor = 0f;
                }
            }
            else
            {
                motor = 0f;
                footBrake = 500f;
            }
        }

        // 2. SADECE Kondisyon %30'un ALTINA düştüğünde araba hasarlı motor teklemeleriyle sallanmaya başlasın
        if (currentConditionRatio < 0.30f && (Mathf.Abs(verticalInput) > 0.1f || ForwardSpeed > 1f))
        {
            float shakeRatio = Mathf.Clamp01((0.30f - currentConditionRatio) / 0.30f); // 0 (at 30%) -> 1 (at 0%)

            // Motor teklemesi (Tork dalgalanması)
            float sputter = Mathf.Sin(Time.time * 22f) * (0.35f * shakeRatio);
            motor *= Mathf.Clamp01(1f - sputter);

            // Fiziksel motor/şasi sarsıntısı
            if (rb != null)
            {
                float pitchRumble = (Mathf.PerlinNoise(Time.time * 26f, 0f) - 0.5f) * (1.2f * shakeRatio);
                float rollRumble = (Mathf.PerlinNoise(0f, Time.time * 26f) - 0.5f) * (0.8f * shakeRatio);
                rb.AddRelativeTorque(new Vector3(pitchRumble, 0f, rollRumble), ForceMode.Acceleration);
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

        // Yalnızca düz giderken toparla (dönüş yaparken veya duvara çarpınca fizik motorunu kitleme!)
        if (Mathf.Abs(horizontalInput) < 0.1f && verticalInput > 0.1f && rb != null && ForwardSpeed > 2f)
        {
            Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);

            if (Mathf.Abs(localVel.x) > 0.5f && Mathf.Abs(localVel.x) < 8.0f)
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
        force *= tuningTorqueMultiplier;

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
