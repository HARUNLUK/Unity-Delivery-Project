using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmoothFollowCamera : MonoBehaviour
{
    [Header("--- TAKİP HEDEFİ ---")]
    [Tooltip("Takip edilecek araç objesi")]
    public Transform target;

    [Header("--- KAMERA MESAFE AYARLARI ---")]
    [Tooltip("Aracın arkasından olan mesafe")]
    public float distance = 6.0f;

    [Tooltip("Aracın üstünden olan yükseklik")]
    public float height = 2.5f;

    [Tooltip("Yükseklik takip yumuşaklığı")]
    public float heightDamping = 2.0f;

    [Tooltip("Dönüş takip yumuşaklığı")]
    public float rotationDamping = 3.0f;

    private void LateUpdate()
    {
        if (!target) return;

        // Hedeflenen yükseklik ve dönüş açısı
        float wantedRotationAngle = target.eulerAngles.y;
        float wantedHeight = target.position.y + height;

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

        // Kamerayı her zaman araca baktırma
        transform.LookAt(target.position + Vector3.up * 1.2f);
    }
}
