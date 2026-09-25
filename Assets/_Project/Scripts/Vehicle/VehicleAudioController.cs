using UnityEngine;

/// <summary>
/// Handles strictly 3D spatial vehicle audio:
/// - Continuous Idle Engine Loop running in 3D space.
/// - Dynamic Pitch & Volume modulation based on speed and throttle while driving.
/// - Engine REMAINS IDLING in 3D space when player exits the car (audible nearby in 3D).
/// - Configurable base volume & pitch sliders directly in Inspector.
/// - 3D Reverse Safety Beeper loop.
/// - 3D Brake Squeak & Tire Slide loop.
/// - 3D Door Open / Close SFX and Collision impacts.
/// </summary>
[RequireComponent(typeof(DrivableVehicle))]
public class VehicleAudioController : MonoBehaviour
{
    private DrivableVehicle vehicle;
    private CarController carController;
    private Rigidbody rb;

    [Header("--- 3D AUDIO SOURCES ---")]
    public AudioSource engineSource;
    public AudioSource reverseSource;
    public AudioSource brakeSource;

    [Header("--- ENGINE VOLUME & PITCH SETTINGS ---")]
    [Range(0f, 2f), Tooltip("Rölanti motor sesi temel şiddeti (Base Idle Volume)")]
    public float engineIdleVolume = 0.55f;

    [Range(0f, 2f), Tooltip("Maksimum hız/gazda motor sesi şiddeti (Max Driving Volume)")]
    public float engineMaxVolume = 0.95f;

    [Range(0.5f, 2f), Tooltip("Rölanti motor sesi tonu/pitch (1.0 = orijinal, 0.85 = daha tok/kalın)")]
    public float idlePitch = 0.90f;

    [Range(1f, 3f), Tooltip("Maksimum sürat ve tam gazdaki motor sesi tonu (Max Pitch)")]
    public float maxPitch = 2.10f;

    [Range(1f, 10f), Tooltip("Gaza basıldığında ses tonunun yükselme hızı")]
    public float pitchTransitionSpeed = 3.5f;

    [Range(1f, 10f), Tooltip("Ses şiddetinin artış hızı")]
    public float volumeTransitionSpeed = 3.0f;

    [Header("--- GLOBAL TOGGLE ---")]
    [Tooltip("Global toggle to enable/disable vehicle audio during development")]
    public static bool enableVehicleAudio = true;

    private float currentEnginePitch;
    private float currentEngineVolume;
    private bool wasPlayerInside = false;

    private void Awake()
    {
        vehicle = GetComponent<DrivableVehicle>();
        carController = GetComponent<CarController>();
        rb = GetComponent<Rigidbody>();

        EnsureVehicleAudioSources();
    }

    private void Start()
    {
        currentEnginePitch = idlePitch;
        currentEngineVolume = engineIdleVolume;

        if (enableVehicleAudio && vehicle != null)
        {
            StartIdleEngine();
        }
    }

    private void EnsureVehicleAudioSources()
    {
        if (engineSource == null)
        {
            GameObject engObj = new GameObject("Audio_3D_VehicleEngine");
            engObj.transform.SetParent(transform, false);
            engineSource = engObj.AddComponent<AudioSource>();
            Configure3DSource(engineSource, true, 2.0f, 35.0f);
        }

        if (reverseSource == null)
        {
            GameObject revObj = new GameObject("Audio_3D_VehicleReverse");
            revObj.transform.SetParent(transform, false);
            reverseSource = revObj.AddComponent<AudioSource>();
            Configure3DSource(reverseSource, true, 1.5f, 25.0f);
        }

        if (brakeSource == null)
        {
            GameObject brkObj = new GameObject("Audio_3D_VehicleBrake");
            brkObj.transform.SetParent(transform, false);
            brakeSource = brkObj.AddComponent<AudioSource>();
            Configure3DSource(brakeSource, true, 1.5f, 25.0f);
        }
    }

    private void Configure3DSource(AudioSource src, bool loop, float minDistance, float maxDistance)
    {
        src.spatialBlend = 1.0f; // Pure 3D Positional Audio
        src.loop = loop;
        src.playOnAwake = false;
        src.minDistance = minDistance;
        src.maxDistance = maxDistance;
        src.rolloffMode = AudioRolloffMode.Logarithmic;
        src.dopplerLevel = 0.5f;
    }

    private void Update()
    {
        if (vehicle == null) return;

        if (!enableVehicleAudio)
        {
            if (engineSource != null && engineSource.isPlaying) engineSource.Stop();
            if (reverseSource != null && reverseSource.isPlaying) reverseSource.Stop();
            if (brakeSource != null && brakeSource.isPlaying) brakeSource.Stop();
            return;
        }

        bool isPlayerInside = vehicle.isPlayerInside;

        // Player entered vehicle
        if (isPlayerInside && !wasPlayerInside)
        {
            OnPlayerEnteredVehicle();
        }
        // Player exited vehicle
        else if (!isPlayerInside && wasPlayerInside)
        {
            OnPlayerExitedVehicle();
        }

        wasPlayerInside = isPlayerInside;

        if (isPlayerInside)
        {
            UpdateEngineAudioDriving();
            UpdateReverseAudio();
            UpdateBrakeAudio();
        }
        else
        {
            UpdateEngineAudioIdlingOutside();
        }
    }

    private void OnPlayerEnteredVehicle()
    {
        // 1. Play 3D Door Open
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayVehicleDoorOpen(transform.position);
        }

        // 2. Ensure continuous idle engine is active
        StartIdleEngine();
    }

    private void OnPlayerExitedVehicle()
    {
        // 1. Play 3D Door Close
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayVehicleDoorClose(transform.position);
        }

        // 2. Stop driving sound effects
        if (reverseSource != null && reverseSource.isPlaying) reverseSource.Stop();
        if (brakeSource != null && brakeSource.isPlaying) brakeSource.Stop();

        // 3. Engine REMAINS IDLING in 3D space
    }

    public void StartIdleEngine()
    {
        if (engineSource == null) return;

        AudioClip idleClip = (AudioManager.Instance != null) ? AudioManager.Instance.vehicleEngineIdleLoop : null;

        if (idleClip != null && (!engineSource.isPlaying || engineSource.clip != idleClip))
        {
            engineSource.clip = idleClip;
            engineSource.loop = true;
            engineSource.pitch = idlePitch;
            float managerVol = AudioManager.Instance != null ? AudioManager.Instance.vehicleEngineVolume * AudioManager.Instance.sfxVolume * AudioManager.Instance.masterVolume : 1f;
            engineSource.volume = engineIdleVolume * managerVol;
            engineSource.Play();
        }
    }

    private void UpdateEngineAudioDriving()
    {
        if (engineSource == null) return;
        if (!engineSource.isPlaying) StartIdleEngine();

        float forwardSpeed = carController != null ? Mathf.Abs(carController.ForwardSpeed) : (rb != null ? rb.linearVelocity.magnitude : 0f);
        float maxSpeed = carController != null ? carController.maxForwardSpeed : 35f;
        float speedRatio = Mathf.Clamp01(forwardSpeed / Mathf.Max(1f, maxSpeed));

        float verticalInput = carController != null ? Mathf.Abs(carController.VerticalInput) : 0f;

        // Dynamic 3D Engine Modulation Formula (Speed 70% + Gas Throttle 30%)
        float targetPitch = Mathf.Lerp(idlePitch, maxPitch, (speedRatio * 0.70f) + (verticalInput * 0.30f));
        float targetVolume = Mathf.Lerp(engineIdleVolume, engineMaxVolume, (speedRatio * 0.65f) + (verticalInput * 0.35f));

        currentEnginePitch = Mathf.MoveTowards(currentEnginePitch, targetPitch, Time.deltaTime * pitchTransitionSpeed);
        currentEngineVolume = Mathf.MoveTowards(currentEngineVolume, targetVolume, Time.deltaTime * volumeTransitionSpeed);

        float managerVol = AudioManager.Instance != null ? AudioManager.Instance.vehicleEngineVolume * AudioManager.Instance.sfxVolume * AudioManager.Instance.masterVolume : 1f;
        engineSource.pitch = currentEnginePitch;
        engineSource.volume = currentEngineVolume * managerVol;
    }

    private void UpdateEngineAudioIdlingOutside()
    {
        if (engineSource == null) return;
        if (!engineSource.isPlaying) StartIdleEngine();

        // Settle smoothly back to idle pitch & idle volume
        currentEnginePitch = Mathf.MoveTowards(currentEnginePitch, idlePitch, Time.deltaTime * 2.0f);
        currentEngineVolume = Mathf.MoveTowards(currentEngineVolume, engineIdleVolume, Time.deltaTime * 2.0f);

        float managerVol = AudioManager.Instance != null ? AudioManager.Instance.vehicleEngineVolume * AudioManager.Instance.sfxVolume * AudioManager.Instance.masterVolume : 1f;
        engineSource.pitch = currentEnginePitch;
        engineSource.volume = currentEngineVolume * managerVol;
    }

    private void UpdateReverseAudio()
    {
        if (reverseSource == null) return;

        bool isReversing = false;
        if (carController != null)
        {
            isReversing = (carController.VerticalInput < -0.1f && carController.ForwardSpeed <= 0.5f);
        }

        if (isReversing)
        {
            float managerVol = AudioManager.Instance != null ? AudioManager.Instance.vehicleReverseBeepVolume * AudioManager.Instance.sfxVolume * AudioManager.Instance.masterVolume : 0.65f;
            if (!reverseSource.isPlaying && AudioManager.Instance != null && AudioManager.Instance.vehicleReverseBeepLoop != null)
            {
                reverseSource.clip = AudioManager.Instance.vehicleReverseBeepLoop;
                reverseSource.volume = managerVol;
                reverseSource.Play();
            }
            else if (reverseSource.isPlaying)
            {
                reverseSource.volume = managerVol;
            }
        }
        else
        {
            if (reverseSource.isPlaying)
            {
                reverseSource.Stop();
            }
        }
    }

    private void UpdateBrakeAudio()
    {
        if (brakeSource == null) return;

        float forwardSpeed = carController != null ? Mathf.Abs(carController.ForwardSpeed) : (rb != null ? rb.linearVelocity.magnitude : 0f);
        bool isBraking = false;

        if (carController != null)
        {
            isBraking = (carController.IsHandbraking || (carController.VerticalInput < -0.1f && carController.ForwardSpeed > 2.5f)) && forwardSpeed > 2.0f;
        }

        if (isBraking)
        {
            float managerVol = AudioManager.Instance != null ? AudioManager.Instance.vehicleBrakeSqueakVolume * AudioManager.Instance.sfxVolume * AudioManager.Instance.masterVolume : 0.70f;
            if (!brakeSource.isPlaying && AudioManager.Instance != null && AudioManager.Instance.vehicleBrakeSqueak != null)
            {
                brakeSource.clip = AudioManager.Instance.vehicleBrakeSqueak;
                brakeSource.loop = true;
                brakeSource.volume = managerVol;
                brakeSource.pitch = Random.Range(0.96f, 1.04f);
                brakeSource.Play();
            }
            else if (brakeSource.isPlaying)
            {
                brakeSource.volume = managerVol;
            }
        }
        else
        {
            if (brakeSource.isPlaying)
            {
                brakeSource.Stop();
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.contactCount == 0) return;

        // Ignore collisions with own child objects (e.g. PickupBackDoor swinging around)
        if (collision.transform.IsChildOf(transform) || transform.IsChildOf(collision.transform)) return;

        // Calculate velocity along the contact normal to ignore scraping/drifting tangential velocity
        float normalVelocity = Mathf.Abs(Vector3.Dot(collision.relativeVelocity, collision.contacts[0].normal));

        if (normalVelocity < 3.5f) return;

        if (AudioManager.Instance != null)
        {
            Debug.Log($"<color=#FF5555>[VehicleAudio] Crash sound played! Collided with: {collision.gameObject.name} (Normal Velocity: {normalVelocity:F1})</color>");
            AudioManager.Instance.PlayVehicleCrash(collision.contacts[0].point, normalVelocity);
        }
    }
}
