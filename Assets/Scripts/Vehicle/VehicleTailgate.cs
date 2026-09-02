using UnityEngine;

public class VehicleTailgate : MonoBehaviour
{
    public enum DoorMode
    {
        ProceduralRotation, // Açı ile pürüzsüz dönüş (Pickup kasası, bagaj vb.)
        UnityAnimator,      // Animator bileşeni ve klip ile açılış
        ExternalHierarchy   // Kapak başka bir mesh/kemik altında, kendi kendine döner
    }

    [Header("--- ÇALIŞMA MODU ---")]
    public DoorMode doorMode = DoorMode.ProceduralRotation;

    [Header("--- PROSEDÜREL DÖNÜŞ AYARLARI ---")]
    [Tooltip("Dönecek kapak objesi (Örn: Modelin kendi kapı/bagaj meshi)")]
    public Transform doorTransform;

    [Tooltip("Kod ile otomatik döndürsün mü? (Kapıyı sen/animasyon döndürüyorsa FALSE yap)")]
    public bool autoRotateTransform = true;

    [Tooltip("Kapalıyken yerel açı (Euler)")]
    public Vector3 closedRotation = Vector3.zero;

    [Tooltip("Açıkken yerel açı (Euler) - Örn: X ekseninde 90 derece aşağı")]
    public Vector3 openRotation = new Vector3(90f, 0f, 0f);

    [Tooltip("Açılma ve kapanma hızı")]
    public float transitionSpeed = 5.0f;

    [Header("--- UNITY ANIMATOR AYARLARI (İsteğe Bağlı) ---")]
    public Animator doorAnimator;
    [Tooltip("Animator içindeki bool parametresi adı")]
    public string animatorBoolParam = "IsOpen";

    [Header("--- EKRAN METİNLERİ & SES ---")]
    public string openPromptText = "[E] Open Tailgate";
    public string closePromptText = "[E] Close Tailgate";
    public AudioClip openSound;
    public AudioClip closeSound;

    [Header("--- DURUM ---")]
    public bool isOpen = false;

    private AudioSource audioSource;

    private void Awake()
    {
        if (doorTransform == null) doorTransform = transform;
        if (doorAnimator == null) doorAnimator = GetComponentInParent<Animator>();

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

    public void ToggleDoor()
    {
        isOpen = !isOpen;

        if (doorMode == DoorMode.UnityAnimator && doorAnimator != null)
        {
            doorAnimator.SetBool(animatorBoolParam, isOpen);
        }

        if (audioSource != null)
        {
            AudioClip clipToPlay = isOpen ? openSound : closeSound;
            if (clipToPlay != null) audioSource.PlayOneShot(clipToPlay);
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
            Quaternion targetRot = Quaternion.Euler(isOpen ? openRotation : closedRotation);
            doorTransform.localRotation = Quaternion.Slerp(doorTransform.localRotation, targetRot, Time.deltaTime * transitionSpeed);
        }
    }

    public string GetPromptText()
    {
        return isOpen ? closePromptText : openPromptText;
    }
}
