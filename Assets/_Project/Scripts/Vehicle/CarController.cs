using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class CarController : MonoBehaviour
{
    [Header("--- MOTOR & BRAKE SETTINGS ---")]
    public float motorForce = 12000f;
    public float reverseForce = 8000f;
    public float footBrakeForce = 160000f;
    public float handBrakeForce = 200000f;

    [Tooltip("Base active braking deceleration in m/s^2 for foot brake (S key)")]
    public float brakeDeceleration = 16.0f;

    [Tooltip("Base active deceleration in m/s^2 for handbrake (Space key)")]
    public float handbrakeDeceleration = 22.0f;

    [Tooltip("Natural engine braking torque when releasing gas")]
    public float engineBrakeTorque = 1500f;

    [Tooltip("Maximum steering angle at low speeds for tight, agile cornering (42° = very responsive)")]
    public float maxSteerAngle = 42f;

    [Tooltip("Steering angle at maximum forward speed (26° = comfortable high speed turning)")]
    public float highSpeedSteerAngle = 26f;

    [Tooltip("Dynamic steering bite / agility assist torque to make turns feel responsive and satisfying")]
    public float steerAgility = 2.5f;

    public Vector3 centerOfMassOffset = new Vector3(0, -0.45f, 0.05f);

    [Header("--- AERODYNAMIC DOWNFORCE & STABILITY ---")]
    [Tooltip("Downward force factor applied as speed increases to keep vehicle glued to the road and prevent flying")]
    public float downforce = 45f;

    [Tooltip("Body roll damping factor to prevent rollover and wheel pinch on sharp turns")]
    public float rollDamping = 300f;

    [Tooltip("Active straight-line yaw stabilizer at high speed to eliminate road pulling / wandering")]
    public bool enableStraightLineStabilizer = true;

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

    [Header("--- PURE PHYSICS HANDBRAKE & DRIFT ---")]
    [Tooltip("Rear wheel sideways friction stiffness when handbraking into a turn (allows momentum to slide rear)")]
    public float driftSidewaysStiffness = 0.5f;

    [Tooltip("Rear wheel forward friction stiffness when handbraking into a turn")]
    public float driftForwardStiffness = 0.6f;

    [Tooltip("Speed at which tire grip recovers back to normal when handbrake is released")]
    public float driftRecoveryRate = 3.5f;

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
    public Vector3 wheelMeshRotationOffset = Vector3.zero;
    public Vector3 wheelMeshPositionOffset = Vector3.zero;

    [Header("--- STEERING WHEEL & SMOOTH STEERING ---")]
    [Tooltip("Steering wheel visual model object to rotate while driving")]
    public Transform steeringWheel;

    [Tooltip("Steering wheel rotation multiplier")]
    public float steeringWheelMultiplier = 3.5f;

    [Tooltip("Steering angle interpolation speed (Degrees/Second)")]
    public float steerSpeed = 260f;

    [Tooltip("Steering return-to-center speed when key is released (Degrees/Second)")]
    public float steerReturnSpeed = 300f;

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
    private WheelFrictionCurve normalRearForwardFriction;
    private float currentRearSidewaysStiffness;
    private float currentRearForwardStiffness;

    public float ForwardSpeed { get; private set; }

    private void Awake()
    {
        // Enforce a much lower engine brake so the car coasts when letting off gas, overriding old prefab values
        if (engineBrakeTorque > 400f) engineBrakeTorque = 150f;

        EnsureBaseSteeringEuler();

        // Ensure all child MeshColliders are convex to prevent physics engine conflicts
        MeshCollider[] meshColliders = GetComponentsInChildren<MeshCollider>();
        foreach (var mc in meshColliders)
        {
            mc.convex = true;
        }

        // Auto-fix any WheelCollider GameObjects that have legacy imported -90° X rotation
        WheelCollider[] wheels = new WheelCollider[] { frontLeftCollider, frontRightCollider, rearLeftCollider, rearRightCollider };
        foreach (var wc in wheels)
        {
            if (wc != null)
            {
                wc.transform.localRotation = Quaternion.identity;
                wc.center = Vector3.zero;
            }
        }
    }

    private void Start()
    {
        EnsureBaseSteeringEuler();

        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.centerOfMass = centerOfMassOffset;
            rb.maxAngularVelocity = 8f;
            rb.linearDamping = 0.08f;
            rb.angularDamping = 3.0f;
            rb.maxDepenetrationVelocity = 5.0f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        // Front wheels: aggressive grip for instant steering bite & zero understeer
        WheelCollider[] frontWheels = new WheelCollider[] { frontLeftCollider, frontRightCollider };
        foreach (var wc in frontWheels)
        {
            if (wc != null)
            {
                JointSpring s = wc.suspensionSpring;
                if (s.spring < 25000f) s.spring = 32000f;
                if (s.damper < 3500f) s.damper = 5500f;
                s.targetPosition = 0.5f;
                wc.suspensionSpring = s;
                wc.wheelDampingRate = 0.5f;
                wc.forceAppPointDistance = 0.12f;

                WheelFrictionCurve side = wc.sidewaysFriction;
                side.extremumSlip = 0.22f;
                side.asymptoteSlip = 0.45f;
                side.extremumValue = 1.15f;
                side.asymptoteValue = 0.9f;
                side.stiffness = 3.0f; // High front grip = turns instantly
                wc.sidewaysFriction = side;

                WheelFrictionCurve fwd = wc.forwardFriction;
                fwd.extremumSlip = 0.25f;
                fwd.asymptoteSlip = 0.5f;
                fwd.extremumValue = 1.0f;
                fwd.asymptoteValue = 0.8f;
                fwd.stiffness = 2.0f;
                wc.forwardFriction = fwd;
            }
        }

        // Rear wheels: balanced grip for smooth rotation and controllable drifts
        WheelCollider[] rearWheels = new WheelCollider[] { rearLeftCollider, rearRightCollider };
        foreach (var wc in rearWheels)
        {
            if (wc != null)
            {
                JointSpring s = wc.suspensionSpring;
                if (s.spring < 25000f) s.spring = 32000f;
                if (s.damper < 3500f) s.damper = 5500f;
                s.targetPosition = 0.5f;
                wc.suspensionSpring = s;
                wc.wheelDampingRate = 0.5f;
                wc.forceAppPointDistance = 0.12f;

                WheelFrictionCurve side = wc.sidewaysFriction;
                side.extremumSlip = 0.25f;
                side.asymptoteSlip = 0.5f;
                side.extremumValue = 1.0f;
                side.asymptoteValue = 0.8f;
                side.stiffness = 2.4f;
                wc.sidewaysFriction = side;

                WheelFrictionCurve fwd = wc.forwardFriction;
                fwd.extremumSlip = 0.25f;
                fwd.asymptoteSlip = 0.5f;
                fwd.extremumValue = 1.0f;
                fwd.asymptoteValue = 0.8f;
                fwd.stiffness = 2.0f;
                wc.forwardFriction = fwd;
            }
        }

        if (rearLeftCollider != null)
        {
            normalRearSidewaysFriction = rearLeftCollider.sidewaysFriction;
            normalRearForwardFriction = rearLeftCollider.forwardFriction;
            currentRearSidewaysStiffness = normalRearSidewaysFriction.stiffness;
            currentRearForwardStiffness = normalRearForwardFriction.stiffness;
        }

        DrivableVehicle dv = GetComponent<DrivableVehicle>();
        if (dv != null)
        {
            int stage = PlayerPrefs.GetInt("Vehicle_TuningStage_" + dv.EffectiveVehicleId, PlayerPrefs.GetInt("Vehicle_TuningStage_" + dv.vehicleId, 0));
            if (stage == 1) tuningTorqueMultiplier = 1.15f;
            else if (stage == 2) tuningTorqueMultiplier = 1.30f;
            else if (stage >= 3) tuningTorqueMultiplier = 1.50f;
            else tuningTorqueMultiplier = 1.00f;

            if (!dv.isPlayerInside)
            {
                enabled = false;
            }
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

        // 1. Zero Throttle across all wheels
        if (frontLeftCollider != null) frontLeftCollider.motorTorque = 0f;
        if (frontRightCollider != null) frontRightCollider.motorTorque = 0f;
        if (rearLeftCollider != null) rearLeftCollider.motorTorque = 0f;
        if (rearRightCollider != null) rearRightCollider.motorTorque = 0f;

        // 2. Straighten steering
        if (frontLeftCollider != null) frontLeftCollider.steerAngle = 0f;
        if (frontRightCollider != null) frontRightCollider.steerAngle = 0f;

        // 3. Deceleration Brake (Smoothly halts vehicle to parked idle)
        float neutralBrake = 3000f;
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
        HandleDriftFrictionTransition();
        ApplyAerodynamicsAndRollDamping();
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

        DrivableVehicle dv = GetComponent<DrivableVehicle>();
        if (dv != null && !dv.isPlayerInside)
        {
            return;
        }

        if (FPSPlayerController.Instance != null && (FPSPlayerController.Instance.IsUIBlockingInput() || FPSPlayerController.Instance.IsOnFoot))
        {
            return;
        }

        if (KeyBindingManager.IsPressed(GameAction.MoveForward)) verticalInput += 1f;
        if (KeyBindingManager.IsPressed(GameAction.MoveBackward)) verticalInput -= 1f;
        if (KeyBindingManager.IsPressed(GameAction.MoveRight)) horizontalInput += 1f;
        if (KeyBindingManager.IsPressed(GameAction.MoveLeft)) horizontalInput -= 1f;
        if (KeyBindingManager.IsPressed(GameAction.Jump)) isHandbraking = true;

#if ENABLE_LEGACY_INPUT_MANAGER
        if (verticalInput == 0f) verticalInput = Input.GetAxis("Vertical");
        if (horizontalInput == 0f) horizontalInput = Input.GetAxis("Horizontal");
        if (!isHandbraking) isHandbraking = Input.GetKey(KeyCode.Space);
#endif
    }

    private void HandleSteering()
    {
        // 1. Clean Input Deadzone (Straight line stability)
        float steerInput = Mathf.Abs(horizontalInput) > 0.02f ? horizontalInput : 0f;

        // 2. Speed-sensitive Max Steer Angle (agile 42° at low speeds, comfortable 26° at 100+ km/h)
        float speedFactor = Mathf.InverseLerp(10f, 35f, Mathf.Abs(ForwardSpeed));
        float currentMaxAngle = Mathf.Lerp(maxSteerAngle, highSpeedSteerAngle, speedFactor);

        float targetSteerAngle = currentMaxAngle * steerInput;
        float speed = Mathf.Abs(steerInput) > 0.01f ? steerSpeed : steerReturnSpeed;
        currentSteerAngle = Mathf.MoveTowards(currentSteerAngle, targetSteerAngle, speed * Time.fixedDeltaTime);

        // Snap to zero when near neutral to ensure laser-straight driving
        if (Mathf.Abs(steerInput) < 0.01f && Mathf.Abs(currentSteerAngle) < 0.08f)
        {
            currentSteerAngle = 0f;
        }

        // 3. Damaged Steering Wobble (ONLY active if condition < 30% and moving)
        float wobbleAngle = 0f;
        if (currentConditionRatio < 0.30f && (Mathf.Abs(ForwardSpeed) > 0.8f || Mathf.Abs(verticalInput) > 0.1f))
        {
            float wobbleIntensity = Mathf.Clamp01((0.30f - currentConditionRatio) / 0.30f);
            float wobbleTime = Time.time * 7.5f;
            wobbleAngle = (Mathf.Sin(wobbleTime) * 0.7f + Mathf.Sin(wobbleTime * 2.3f) * 0.3f) * (currentMaxAngle * 0.25f * wobbleIntensity);
        }

        float effectiveSteerAngle = currentSteerAngle + wobbleAngle;

        // 4. Symmetric Steering Output
        if (Mathf.Abs(effectiveSteerAngle) < 0.05f)
        {
            if (frontLeftCollider != null) frontLeftCollider.steerAngle = 0f;
            if (frontRightCollider != null) frontRightCollider.steerAngle = 0f;
        }
        else
        {
            if (frontLeftCollider != null) frontLeftCollider.steerAngle = effectiveSteerAngle;
            if (frontRightCollider != null) frontRightCollider.steerAngle = effectiveSteerAngle;
        }

        // 5. Straight Line High-Speed Yaw Stabilizer (Cancels micro road bump pulling when driving straight)
        if (enableStraightLineStabilizer && Mathf.Abs(steerInput) < 0.05f && rb != null && Mathf.Abs(ForwardSpeed) > 3f)
        {
            Vector3 localAng = transform.InverseTransformDirection(rb.angularVelocity);
            localAng.y = Mathf.MoveTowards(localAng.y, 0f, Time.fixedDeltaTime * 14f);
            rb.angularVelocity = transform.TransformDirection(localAng);
        }

        // 6. Dynamic Steering Yaw Bite / Agility (Gives intuitive, crisp turning feedback into the corner)
        if (rb != null && Mathf.Abs(steerInput) > 0.05f && rb.linearVelocity.magnitude > 1.2f)
        {
            float directionSign = ForwardSpeed >= -0.2f ? 1f : -1f;
            float steerRatio = effectiveSteerAngle / maxSteerAngle;
            float agilityTorque = steerRatio * steerAgility * 1.6f * directionSign;
            rb.AddTorque(transform.up * agilityTorque, ForceMode.Acceleration);
        }

        // 7. Steering Wheel Visual Model Rotation (only rotates around its primary X steering axis)
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

        // 1. Condition Speed Cap
        bool isMaxDamaged = currentConditionRatio <= 0.02f;
        float activeMaxSpeed = isMaxDamaged ? minConditionMaxSpeed : maxForwardSpeed;
        float baseTorqueMult = isMaxDamaged ? minConditionTorqueMultiplier : 1.0f;

        // Foot Braking / Reverse Logic with Progressive Braking Curve
        if (ForwardSpeed > 0.35f && verticalInput < -0.05f)
        {
            footBrake = footBrakeForce * Mathf.Abs(verticalInput);
            motor = 0f;

            // Progressive braking curve: bite gets stronger as speed drops for firm, non-linear stops
            if (rb != null)
            {
                float speedRatio = Mathf.Clamp01(ForwardSpeed / 25f);
                float progressiveBite = Mathf.Lerp(2.2f, 1.0f, speedRatio); // Up to 2.2x bite at lower speeds
                rb.AddForce(-transform.forward * (brakeDeceleration * progressiveBite * rb.mass * Mathf.Abs(verticalInput)), ForceMode.Force);
            }
        }
        else if (ForwardSpeed < -0.35f && verticalInput > 0.05f)
        {
            footBrake = footBrakeForce * Mathf.Abs(verticalInput);
            motor = 0f;

            if (rb != null)
            {
                float speedRatio = Mathf.Clamp01(Mathf.Abs(ForwardSpeed) / 10f);
                float progressiveBite = Mathf.Lerp(2.2f, 1.0f, speedRatio);
                rb.AddForce(transform.forward * (brakeDeceleration * progressiveBite * rb.mass * Mathf.Abs(verticalInput)), ForceMode.Force);
            }
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
                    motor = 0f;
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
                footBrake = engineBrakeTorque; // Engine braking resistance (stops endless coasting)
            }
        }

        // Full stop clamp ONLY when actively braking to a standstill (never during gas / acceleration)
        if (rb != null && footBrake > 10000f && motor == 0f && Mathf.Abs(ForwardSpeed) < 0.25f)
        {
            rb.linearVelocity = Vector3.MoveTowards(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 20f);
        }

        // 2. Sputter / Shaking when heavily damaged (<30% condition)
        if (currentConditionRatio < 0.30f && (Mathf.Abs(verticalInput) > 0.1f || ForwardSpeed > 1f))
        {
            float shakeRatio = Mathf.Clamp01((0.30f - currentConditionRatio) / 0.30f);
            float sputter = Mathf.Sin(Time.time * 22f) * (0.35f * shakeRatio);
            motor *= Mathf.Clamp01(1f - sputter);

            if (rb != null)
            {
                float pitchRumble = (Mathf.PerlinNoise(Time.time * 26f, 0f) - 0.5f) * (1.0f * shakeRatio);
                float rollRumble = (Mathf.PerlinNoise(0f, Time.time * 26f) - 0.5f) * (0.7f * shakeRatio);
                rb.AddRelativeTorque(new Vector3(pitchRumble, 0f, rollRumble), ForceMode.Acceleration);
            }
        }

        ApplyMotorTorque(motor);
        ApplyBrakes(footBrake);
    }

    /// <summary>
    /// Dual-mode Handbrake:
    /// 1. If steering: Drift mode (locks rear wheels, front rolls free, reduced rear sideways grip).
    /// 2. If straight: Emergency Power Stop (all 4 wheels lock with full tire grip and progressive non-linear deceleration).
    /// </summary>
    private void HandleHandbrake()
    {
        ApplyMotorTorque(0f);

        bool isSteeringDrift = Mathf.Abs(horizontalInput) > 0.1f;

        if (isSteeringDrift)
        {
            // DRIFT MODE: Lock rear wheels, let front roll free, reduce rear sideways grip
            if (frontLeftCollider != null) frontLeftCollider.brakeTorque = 0f;
            if (frontRightCollider != null) frontRightCollider.brakeTorque = 0f;
            if (rearLeftCollider != null) rearLeftCollider.brakeTorque = handBrakeForce;
            if (rearRightCollider != null) rearRightCollider.brakeTorque = handBrakeForce;

            currentRearSidewaysStiffness = Mathf.MoveTowards(currentRearSidewaysStiffness, driftSidewaysStiffness, Time.fixedDeltaTime * 6f);
            currentRearForwardStiffness = Mathf.MoveTowards(currentRearForwardStiffness, driftForwardStiffness, Time.fixedDeltaTime * 6f);
            SetRearFriction(currentRearSidewaysStiffness, currentRearForwardStiffness);
        }
        else
        {
            // EMERGENCY POWER STOP MODE: All 4 wheels lock with full normal tire friction + progressive deceleration
            if (frontLeftCollider != null) frontLeftCollider.brakeTorque = handBrakeForce * 0.85f;
            if (frontRightCollider != null) frontRightCollider.brakeTorque = handBrakeForce * 0.85f;
            if (rearLeftCollider != null) rearLeftCollider.brakeTorque = handBrakeForce;
            if (rearRightCollider != null) rearRightCollider.brakeTorque = handBrakeForce;

            // Retain full normal tire friction for immediate asphalt bite
            float targetSideways = normalRearSidewaysFriction.stiffness > 0.1f ? normalRearSidewaysFriction.stiffness : 2.4f;
            float targetForward = normalRearForwardFriction.stiffness > 0.1f ? normalRearForwardFriction.stiffness : 2.0f;
            currentRearSidewaysStiffness = targetSideways;
            currentRearForwardStiffness = targetForward;
            SetRearFriction(currentRearSidewaysStiffness, currentRearForwardStiffness);

            // Progressive stopping deceleration (gets progressively stronger as vehicle slows down)
            if (rb != null && Mathf.Abs(ForwardSpeed) > 0.3f)
            {
                float speedRatio = Mathf.Clamp01(Mathf.Abs(ForwardSpeed) / 25f);
                float progressiveMultiplier = Mathf.Lerp(2.5f, 1.0f, speedRatio); // Up to 2.5x bite at lower speeds
                float direction = ForwardSpeed > 0 ? -1f : 1f;
                rb.AddForce(transform.forward * (direction * handbrakeDeceleration * progressiveMultiplier * rb.mass), ForceMode.Force);
            }
        }

        // Full stop clamp when near zero speed under handbrake
        if (rb != null && Mathf.Abs(ForwardSpeed) < 0.4f)
        {
            rb.linearVelocity = Vector3.MoveTowards(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 25f);
        }
    }

    /// <summary>
    /// Smoothly restores normal tire grip when handbrake is released.
    /// Uses natural physics friction transition without altering Rigidbody velocity directly.
    /// </summary>
    private void HandleDriftFrictionTransition()
    {
        if (isHandbraking) return;

        float targetSideways = normalRearSidewaysFriction.stiffness > 0.1f ? normalRearSidewaysFriction.stiffness : 2.4f;
        float targetForward = normalRearForwardFriction.stiffness > 0.1f ? normalRearForwardFriction.stiffness : 2.0f;

        if (!Mathf.Approximately(currentRearSidewaysStiffness, targetSideways) || !Mathf.Approximately(currentRearForwardStiffness, targetForward))
        {
            currentRearSidewaysStiffness = Mathf.MoveTowards(currentRearSidewaysStiffness, targetSideways, Time.fixedDeltaTime * driftRecoveryRate);
            currentRearForwardStiffness = Mathf.MoveTowards(currentRearForwardStiffness, targetForward, Time.fixedDeltaTime * driftRecoveryRate);
            SetRearFriction(currentRearSidewaysStiffness, currentRearForwardStiffness);
        }
    }

    private void SetRearFriction(float sidewaysStiffness, float forwardStiffness)
    {
        if (rearLeftCollider != null)
        {
            WheelFrictionCurve sf = rearLeftCollider.sidewaysFriction;
            sf.stiffness = sidewaysStiffness;
            rearLeftCollider.sidewaysFriction = sf;

            WheelFrictionCurve ff = rearLeftCollider.forwardFriction;
            ff.stiffness = forwardStiffness;
            rearLeftCollider.forwardFriction = ff;
        }

        if (rearRightCollider != null)
        {
            WheelFrictionCurve sf = rearRightCollider.sidewaysFriction;
            sf.stiffness = sidewaysStiffness;
            rearRightCollider.sidewaysFriction = sf;

            WheelFrictionCurve ff = rearRightCollider.forwardFriction;
            ff.stiffness = forwardStiffness;
            rearRightCollider.forwardFriction = ff;
        }
    }

    /// <summary>
    /// Applies aerodynamic downforce proportional to speed (glues car to road)
    /// and body roll damping on sharp turns to prevent rollover.
    /// </summary>
    private void ApplyAerodynamicsAndRollDamping()
    {
        if (rb == null) return;

        // 1. Aerodynamic Downforce
        if (rb.linearVelocity.sqrMagnitude > 1.0f)
        {
            float speed = rb.linearVelocity.magnitude;
            rb.AddForce(-transform.up * (downforce * speed), ForceMode.Force);
        }

        // 2. Safe Body Roll Damping (prevents sharp turns from rolling chassis or causing wheel hops)
        if (rollDamping > 0f)
        {
            Vector3 localAngularVel = transform.InverseTransformDirection(rb.angularVelocity);
            rb.AddRelativeTorque(0f, 0f, -localAngularVel.z * rollDamping, ForceMode.Force);
        }
    }

    private void ApplyMotorTorque(float force)
    {
        force *= tuningTorqueMultiplier;

        // 30% Front, 70% Rear AWD for immediate, punchy acceleration from standstill
        float frontForce = force * 0.30f;
        float rearForce = force * 0.70f;

        // If steering sharply at speed, transfer 100% torque to rear so front wheels steer freely
        if (Mathf.Abs(currentSteerAngle) > 15f && ForwardSpeed > 3f)
        {
            frontForce = 0f;
            rearForce = force;
        }

        if (frontLeftCollider != null) frontLeftCollider.motorTorque = frontForce;
        if (frontRightCollider != null) frontRightCollider.motorTorque = frontForce;
        if (rearLeftCollider != null) rearLeftCollider.motorTorque = rearForce;
        if (rearRightCollider != null) rearRightCollider.motorTorque = rearForce;
    }

    private void ApplyBrakes(float force)
    {
        if (frontLeftCollider != null) frontLeftCollider.brakeTorque = force * 0.7f;
        if (frontRightCollider != null) frontRightCollider.brakeTorque = force * 0.7f;
        if (rearLeftCollider != null) rearLeftCollider.brakeTorque = force * 0.5f;
        if (rearRightCollider != null) rearRightCollider.brakeTorque = force * 0.5f;
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
