using UnityEngine;

public class VehicleTailgate : MonoBehaviour
{
    public enum DoorMode
    {
        ProceduralRotation, // Açı ile pürüzsüz dönüş (Pickup kasası, bagaj vb.)
        UnityAnimator,      // Animator bileşeni ve klip ile açılış
        ExternalHierarchy   // Kapak başka bir mesh/kemik altında, kendi kendine döner
    }

    [Header("--- OPERATION MODE ---")]
    public DoorMode doorMode = DoorMode.ProceduralRotation;

    [Header("--- PROCEDURAL ROTATION SETTINGS ---")]
    [Tooltip("Door/tailgate transform to rotate")]
    public Transform doorTransform;

    [Tooltip("Should code automatically rotate transform? (Set FALSE if using custom animator)")]
    public bool autoRotateTransform = true;

    [Tooltip("Local Euler rotation when closed")]
    public Vector3 closedRotation = Vector3.zero;

    [Tooltip("Local Euler rotation when open (e.g. 90 on X-axis)")]
    public Vector3 openRotation = new Vector3(90f, 0f, 0f);

    [Tooltip("Opening and closing speed")]
    public float transitionSpeed = 5.0f;

    [Header("--- UNITY ANIMATOR SETTINGS (Optional) ---")]
    public Animator doorAnimator;
    [Tooltip("Bool parameter name inside Animator")]
    public string animatorBoolParam = "IsOpen";

    [Header("--- PROMPT TEXTS & AUDIO ---")]
    public string openPromptText = "[E] Open Tailgate";
    public string closePromptText = "[E] Close Tailgate";
    public AudioClip openSound;
    public AudioClip closeSound;

    [Header("--- STATE ---")]
    public bool isOpen = false;

    private AudioSource audioSource;

    private Rigidbody doorRb;

    private void Awake()
    {
        if (doorTransform == null) doorTransform = transform;
        if (doorAnimator == null) doorAnimator = GetComponentInParent<Animator>();

        if (doorMode == DoorMode.ProceduralRotation && autoRotateTransform && doorTransform != null)
        {
            doorRb = doorTransform.GetComponent<Rigidbody>();
            if (doorRb == null)
            {
                doorRb = doorTransform.gameObject.AddComponent<Rigidbody>();
            }
            doorRb.isKinematic = true;
            doorRb.useGravity = false;
            doorRb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1.0f;

        // Ensure interactive collider exists on interaction zone
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            BoxCollider bc = gameObject.AddComponent<BoxCollider>();
            bc.size = new Vector3(1.6f, 0.6f, 0.4f);
        }
    }

    private void Start()
    {
        // Ignore collisions between the moving tailgate and the player to prevent physics glitches/lifting the car
        FPSPlayerController player = FPSPlayerController.Instance != null ? FPSPlayerController.Instance : Object.FindAnyObjectByType<FPSPlayerController>();
        if (player != null && doorTransform != null)
        {
            Collider[] doorColliders = doorTransform.GetComponentsInChildren<Collider>(true);
            Collider[] playerColliders = player.GetComponentsInChildren<Collider>(true);

            foreach (var dc in doorColliders)
            {
                if (dc.isTrigger) continue; // Don't ignore interaction triggers unnecessarily, just solid colliders
                foreach (var pc in playerColliders)
                {
                    if (pc != null && dc != null)
                    {
                        Physics.IgnoreCollision(dc, pc, true);
                    }
                }
            }
        }

        // Also ignore collisions between the moving tailgate and the vehicle itself to prevent bouncing/phantom forces
        DrivableVehicle vehicle = GetComponentInParent<DrivableVehicle>();
        if (vehicle != null && doorTransform != null)
        {
            Collider[] doorColliders = doorTransform.GetComponentsInChildren<Collider>(true);
            Collider[] vehicleColliders = vehicle.GetComponentsInChildren<Collider>(true);

            foreach (var dc in doorColliders)
            {
                if (dc.isTrigger) continue;
                foreach (var vc in vehicleColliders)
                {
                    if (vc != null && dc != null && !vc.transform.IsChildOf(doorTransform))
                    {
                        Physics.IgnoreCollision(dc, vc, true);
                    }
                }
            }
        }
    }

    public void ToggleDoor()
    {
        isOpen = !isOpen;

        if (doorMode == DoorMode.UnityAnimator && doorAnimator != null)
        {
            doorAnimator.SetBool(animatorBoolParam, isOpen);
        }

        if (audioSource != null)
        {
            AudioClip clipToPlay = isOpen 
                ? (openSound != null ? openSound : (AudioManager.Instance != null ? AudioManager.Instance.vehicleTailgateOpen : null))
                : (closeSound != null ? closeSound : (AudioManager.Instance != null ? AudioManager.Instance.vehicleTailgateClose : null));

            if (clipToPlay != null)
            {
                float masterSfx = (AudioManager.Instance != null) ? AudioManager.Instance.sfxVolume * AudioManager.Instance.masterVolume : 1f;
                audioSource.PlayOneShot(clipToPlay, masterSfx);
            }
        }
    }

    public void SetDoorState(bool open)
    {
        isOpen = open;
        if (doorMode == DoorMode.UnityAnimator && doorAnimator != null)
        {
            doorAnimator.SetBool(animatorBoolParam, isOpen);
        }
    }

    private void Update()
    {
        if (doorMode == DoorMode.ProceduralRotation && autoRotateTransform && doorTransform != null)
        {
            Quaternion targetLocalRot = Quaternion.Euler(isOpen ? openRotation : closedRotation);
            doorTransform.localRotation = Quaternion.Slerp(doorTransform.localRotation, targetLocalRot, Time.deltaTime * transitionSpeed);

            // Wake up any cargos in the vehicle bed while the door is actively moving so they get pushed smoothly!
            if (Quaternion.Angle(doorTransform.localRotation, targetLocalRot) > 0.5f)
            {
                VehicleCargoBed bed = GetComponentInParent<VehicleCargoBed>();
                if (bed != null)
                {
                    foreach (var pkg in bed.PackagesInBed)
                    {
                        if (pkg != null)
                        {
                            Rigidbody prb = pkg.GetComponent<Rigidbody>();
                            if (prb != null && prb.IsSleeping())
                            {
                                prb.WakeUp();
                            }
                        }
                    }
                }
            }
        }
    }

    public string GetPromptText()
    {
        if (isOpen)
        {
            if (string.IsNullOrEmpty(closePromptText) || closePromptText == "[E] Close Tailgate" || closePromptText == "[E] Bagaj Kapağını Kapat")
            {
                return LocalizationManager.Get("prompt_close_tailgate");
            }
            return closePromptText;
        }
        else
        {
            if (string.IsNullOrEmpty(openPromptText) || openPromptText == "[E] Open Tailgate" || openPromptText == "[E] Bagaj Kapağını Aç")
            {
                return LocalizationManager.Get("prompt_open_tailgate");
            }
            return openPromptText;
        }
    }
}
