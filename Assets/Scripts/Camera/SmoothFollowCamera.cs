using UnityEngine;
using UnityEngine.InputSystem;

public class SmoothFollowCamera : MonoBehaviour
{
    [Header("--- TAKİP HEDEFİ ---")]
    [Tooltip("Takip edilecek araç objesi")]
    public Transform target;

    [Header("--- KAMERA MESAFE VE YÜKSEKLİK ---")]
    [Tooltip("Aracın arkasından olan mesafe")]
    public float distance = 6.0f;

    [Tooltip("Aracın üstünden olan yükseklik")]
    public float height = 2.4f;

    [Tooltip("Kameranın odaklanacağı araç dikey noktası")]
    public float lookAtHeight = 1.3f;

    [Header("--- TAKİP YUMUŞAKLIĞI ---")]
    [Tooltip("Yükseklik takip yumuşaklığı")]
    public float heightDamping = 4.0f;

    [Tooltip("Dönüş takip yumuşaklığı")]
    public float rotationDamping = 5.0f;

    [Header("--- FARE İLE ETRAFA BAKIŞ (ORBIT / FREE-LOOK) ---")]
    [Tooltip("Fare hassasiyeti")]
    public float mouseSensitivity = 2.0f;

    [Tooltip("Maksimum sağa/sola bakış açısı (Derece cinsinden limit)")]
    public float maxHorizontalAngle = 75.0f;

    [Tooltip("Maksimum yukarı/aşağı bakış açısı")]
    public float maxVerticalAngle = 25.0f;

    [Tooltip("Fare bırakıldığında kameranın merkeze geri dönme hızı")]
    public float autoCenterSpeed = 3.5f;

    [Tooltip("Fare hareketsiz kaldıktan kaç saniye sonra merkeze dönsün")]
    public float autoCenterDelay = 1.2f;

    private float currentYawOffset = 0f;
    private float currentPitchOffset = 0f;
    private float lastMouseActivityTime = 0f;

    private void Start()
    {
        if (target == null)
        {
            CarController car = FindAnyObjectByType<CarController>();
            if (car != null) target = car.transform;
        }

        // Oyun başlangıcında fareyi kilitle
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (!target) return;

        HandleMouseLookInput();

        // Hedeflenen yükseklik ve dönüş açısı (Araç açısı + Fare ofseti)
        float wantedRotationAngle = target.eulerAngles.y + currentYawOffset;
        float wantedHeight = target.position.y + height + (currentPitchOffset * 0.05f);

        // Mevcut kamera açısı ve yüksekliği
        float currentRotationAngle = transform.eulerAngles.y;
        float currentHeight = transform.position.y;

        // Yumuşak geçiş (Lerp)
        currentRotationAngle = Mathf.LerpAngle(currentRotationAngle, wantedRotationAngle, rotationDamping * Time.deltaTime);
        currentHeight = Mathf.Lerp(currentHeight, wantedHeight, heightDamping * Time.deltaTime);

        // Açıyı rotasyona dönüştürme
        Quaternion currentRotation = Quaternion.Euler(0, currentRotationAngle, 0);

        // Kameranın pozisyonunu aracın arkasına ayarlama
        Vector3 newPos = target.position;
        newPos -= currentRotation * Vector3.forward * distance;
        newPos.y = currentHeight;
        transform.position = newPos;

        // Kamerayı araca baktırma
        Vector3 lookTarget = target.position + (Vector3.up * lookAtHeight);
        transform.LookAt(lookTarget);
    }

    private void HandleMouseLookInput()
    {
        // UI (Tablet veya Gün Sonu) açıkken kamera dönmesin
        if (CargoTabletUI.Instance != null && CargoTabletUI.Instance.IsTabletOpen) return;
        if (DaySummaryManager.Instance != null && DaySummaryManager.Instance.summaryPanelRoot != null && DaySummaryManager.Instance.summaryPanelRoot.activeSelf) return;

        float mouseX = 0f;
        float mouseY = 0f;

        // 1. New Input System Kontrolü
        if (Mouse.current != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            mouseX = delta.x * 0.1f * mouseSensitivity;
            mouseY = delta.y * 0.1f * mouseSensitivity;
        }
        else
        {
            // 2. Legacy Fallback
            try
            {
                mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
                mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
            }
            catch { }
        }

        // Fare hareketi varsa ofseti güncelle ve zamanı sıfırla
        if (Mathf.Abs(mouseX) > 0.05f || Mathf.Abs(mouseY) > 0.05f)
        {
            currentYawOffset += mouseX;
            currentPitchOffset -= mouseY;

            // Açıyı kesin sınırlarla sınırla (Belli açıdan sonra dönmez)
            currentYawOffset = Mathf.Clamp(currentYawOffset, -maxHorizontalAngle, maxHorizontalAngle);
            currentPitchOffset = Mathf.Clamp(currentPitchOffset, -maxVerticalAngle, maxVerticalAngle);

            lastMouseActivityTime = Time.time;
        }
        else
        {
            // Belli süre fare oynatılmazsa kamerayı otomatik olarak arkaya toparla
            if (Time.time - lastMouseActivityTime > autoCenterDelay)
            {
                currentYawOffset = Mathf.Lerp(currentYawOffset, 0f, autoCenterSpeed * Time.deltaTime);
                currentPitchOffset = Mathf.Lerp(currentPitchOffset, 0f, autoCenterSpeed * Time.deltaTime);
            }
        }
    }
}
