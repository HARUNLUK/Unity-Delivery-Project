using UnityEngine;

/// <summary>
/// Deniz / Su düzleminin altına veya içine yerleştirilen Trigger.
/// Oyuncu veya Araç denize düştüğünde en yakın güvenli sahile veya şubeye geri ışınlar.
/// </summary>
public class WaterRespawnZone : MonoBehaviour
{
    [Header("--- RESPAWN SETTINGS ---")]
    [Tooltip("Aracın veya oyuncunun denize düştüğünde ışınlanacağı güvenli nokta (Boş bırakılırsa Şube veya Başlangıç pozisyonu alınır)")]
    public Transform safeRespawnPoint;

    [Tooltip("Işınlanma sonrası aracın durdurulması")]
    public bool resetVehicleVelocity = true;

    private void OnTriggerEnter(Collider other)
    {
        // 1. DrivableVehicle (Araç denize düşerse)
        DrivableVehicle vehicle = other.GetComponentInParent<DrivableVehicle>();
        if (vehicle != null)
        {
            RespawnVehicle(vehicle);
            return;
        }

        // 2. FPS Player (Oyuncu yürüyerek denize düşerse)
        FPSPlayerController player = other.GetComponentInParent<FPSPlayerController>();
        if (player != null)
        {
            RespawnPlayer(player);
            return;
        }

        // 3. Kargo paketi denize düşerse (Sahile veya şubeye geri al)
        PhysicalCargoPackage pkg = other.GetComponent<PhysicalCargoPackage>();
        if (pkg != null)
        {
            RespawnPackage(pkg);
        }
    }

    private Vector3 GetSafePosition()
    {
        if (safeRespawnPoint != null) return safeRespawnPoint.position;

        if (BranchManager.Instance != null)
        {
            return BranchManager.Instance.transform.position + (Vector3.up * 1.5f) + (Vector3.forward * 4.0f);
        }

        return new Vector3(0f, 15f, 0f);
    }

    private Quaternion GetSafeRotation()
    {
        if (safeRespawnPoint != null) return safeRespawnPoint.rotation;
        if (BranchManager.Instance != null) return BranchManager.Instance.transform.rotation;
        return Quaternion.identity;
    }

    private void RespawnVehicle(DrivableVehicle vehicle)
    {
        Vector3 targetPos = GetSafePosition();
        Quaternion targetRot = GetSafeRotation();

        vehicle.transform.position = targetPos;
        vehicle.transform.rotation = targetRot;

        Rigidbody rb = vehicle.GetComponent<Rigidbody>();
        if (rb != null && resetVehicleVelocity)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (InteractionPromptHUD.Instance != null)
        {
            InteractionPromptHUD.Instance.ShowPrompt("<color=#44AAFF>🌊 Araç suya düştü ve güvenli yola çekildi!</color>");
        }

        Debug.Log($"<color=#00AAFF>[WaterRespawnZone] Vehicle '{vehicle.name}' rescued from water to {targetPos}!</color>");
    }

    private void RespawnPlayer(FPSPlayerController player)
    {
        Vector3 targetPos = GetSafePosition() + Vector3.right * 2f;
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        player.transform.position = targetPos;

        if (cc != null) cc.enabled = true;

        if (InteractionPromptHUD.Instance != null)
        {
            InteractionPromptHUD.Instance.ShowPrompt("<color=#44AAFF>🌊 Sudan kurtarıldınız!</color>");
        }
    }

    private void RespawnPackage(PhysicalCargoPackage pkg)
    {
        Vector3 targetPos = GetSafePosition() + Vector3.up * 0.5f;
        pkg.transform.position = targetPos;
        Rigidbody rb = pkg.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}
