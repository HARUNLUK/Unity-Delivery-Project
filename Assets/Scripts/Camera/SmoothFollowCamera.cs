using UnityEngine;
using UnityEngine.InputSystem;

public class SmoothFollowCamera : MonoBehaviour
{
    [Header("--- TAKİP HEDEFİ ---")]
    [Tooltip("Takip edilecek araç objesi")]
    public Transform target;

    [Header("--- KAMERA MESAFE VE YÜKSEKLİK ---")]
    public float distance = 6.0f;
    public float height = 2.4f;
    public float lookAtHeight = 1.3f;

    [Header("--- TAKİP YUMUŞAKLIĞI ---")]
    public float heightDamping = 4.0f;
    public float rotationDamping = 5.0f;

    [Header("--- FARE İLE ETRAFA BAKIŞ ---")]
    public float mouseSensitivity = 2.0f;
    public float maxHorizontalAngle = 75.0f;
    public float maxVerticalAngle = 25.0f;
    public float autoCenterSpeed = 3.5f;
    public float autoCenterDelay = 1.2f;

    private float currentYawOffset = 0f;
    private float currentPitchOffset = 0f;
    private float lastMouseActivityTime = 0f;

    private void Awake()
    {
        // Sahneye FPS karakteri eklendiyse 3. şahıs araba kamerasını otomatik kapat
        if (Object.FindAnyObjectByType<FPSPlayerController>() != null)
        {
            enabled = false;
            return;
        }
    }

    private void Start()
    {
        if (Object.FindAnyObjectByType<FPSPlayerController>() != null)
        {
            enabled = false;
            return;
        }

        if (target == null)
        {
            CarController car = FindAnyObjectByType<CarController>();
            if (car != null) target = car.transform;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (!target) return;

        HandleMouseLookInput();

        float wantedRotationAngle = target.eulerAngles.y + currentYawOffset;
        float wantedHeight = target.position.y + height + (currentPitchOffset * 0.05f);

        float currentRotationAngle = transform.eulerAngles.y;
        float currentHeight = transform.position.y;

        currentRotationAngle = Mathf.LerpAngle(currentRotationAngle, wantedRotationAngle, rotationDamping * Time.deltaTime);
        currentHeight = Mathf.Lerp(currentHeight, wantedHeight, heightDamping * Time.deltaTime);

        Quaternion currentRotation = Quaternion.Euler(0, currentRotationAngle, 0);

        Vector3 targetPos = target.position;
        targetPos -= currentRotation * Vector3.forward * distance;
        targetPos.y = currentHeight;

        transform.position = targetPos;

        Vector3 lookTarget = target.position + (Vector3.up * lookAtHeight);
        transform.LookAt(lookTarget);
    }

    private void HandleMouseLookInput()
    {
        float mouseX = 0f;
        float mouseY = 0f;

        if (Mouse.current != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue() * (mouseSensitivity * 0.05f);
            mouseX = delta.x;
            mouseY = delta.y;
        }

        if (Mathf.Abs(mouseX) > 0.01f || Mathf.Abs(mouseY) > 0.01f)
        {
            currentYawOffset += mouseX;
            currentPitchOffset -= mouseY;

            currentYawOffset = Mathf.Clamp(currentYawOffset, -maxHorizontalAngle, maxHorizontalAngle);
            currentPitchOffset = Mathf.Clamp(currentPitchOffset, -maxVerticalAngle, maxVerticalAngle);

            lastMouseActivityTime = Time.time;
        }
        else
        {
            if (Time.time - lastMouseActivityTime > autoCenterDelay)
            {
                currentYawOffset = Mathf.Lerp(currentYawOffset, 0f, autoCenterSpeed * Time.deltaTime);
                currentPitchOffset = Mathf.Lerp(currentPitchOffset, 0f, autoCenterSpeed * Time.deltaTime);
            }
        }
    }
}
