using System;
using UnityEngine;

public class DayTimeManager : MonoBehaviour
{
    public static DayTimeManager Instance { get; private set; }

    [Header("--- ÇALIŞMA SAATLERİ ---")]
    [Tooltip("Mesai başlangıç saati (Örn: 9)")]
    public int startHour = 9;
    public int startMinute = 0;

    [Tooltip("Mesai bitiş saati (Örn: 18)")]
    public int endHour = 18;
    public int endMinute = 0;

    [Tooltip("Gerçek dünyada mesainin kaç dakika süreceği (Örn: 10 dakika)")]
    public float realTimeDurationInMinutes = 10f;

    [Header("--- GÜNEŞ & IŞIK ---")]
    [Tooltip("Sahnedeki Directional Light (Boş bırakılırsa otomatik bulunur)")]
    public Light directionalSun;

    [Tooltip("Sabah 09:00 güneş açısı")]
    public Vector3 morningSunRotation = new Vector3(25f, -30f, 0f);

    [Tooltip("Akşam 18:00 gün batımı açısı")]
    public Vector3 eveningSunRotation = new Vector3(175f, -30f, 0f);

    public float CurrentTimeInSeconds { get; private set; }
    public int CurrentHour { get; private set; }
    public int CurrentMinute { get; private set; }
    public bool IsShiftEnded { get; private set; }

    public static event Action OnShiftEnded;

    private float totalShiftInGameMinutes;
    private float totalRealTimeSeconds;

    private void Awake()
    {
        Instance = this;

        if (directionalSun == null)
        {
            Light[] lights = FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    directionalSun = l;
                    break;
                }
            }
        }
    }

    private void Start()
    {
        totalShiftInGameMinutes = ((endHour * 60) + endMinute) - ((startHour * 60) + startMinute);
        totalRealTimeSeconds = realTimeDurationInMinutes * 60f;

        CurrentTimeInSeconds = 0f;
        CurrentHour = startHour;
        CurrentMinute = startMinute;
        IsShiftEnded = false;

        UpdateSunPosition(0f);
    }

    private void Update()
    {
        if (IsShiftEnded) return;

        CurrentTimeInSeconds += Time.deltaTime;
        float progress = Mathf.Clamp01(CurrentTimeInSeconds / totalRealTimeSeconds);

        // Oyun saati ve dakikasını hesapla
        float currentTotalInGameMinutes = (startHour * 60 + startMinute) + (progress * totalShiftInGameMinutes);
        CurrentHour = Mathf.FloorToInt(currentTotalInGameMinutes / 60f);
        CurrentMinute = Mathf.FloorToInt(currentTotalInGameMinutes % 60f);

        // Güneş açısını lerp ile döndür
        UpdateSunPosition(progress);

        // 18:00 Mesai Bitiş Kontrolü
        if (progress >= 1.0f)
        {
            EndShift();
        }

#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.f8Key.wasPressedThisFrame)
        {
            EndShift();
        }
#endif
    }

    private void UpdateSunPosition(float progress)
    {
        if (directionalSun != null)
        {
            directionalSun.transform.rotation = Quaternion.Euler(Vector3.Lerp(morningSunRotation, eveningSunRotation, progress));
        }
    }

    public string GetFormattedTime()
    {
        return $"{CurrentHour:D2}:{CurrentMinute:D2}";
    }

    private void EndShift()
    {
        if (IsShiftEnded) return;
        IsShiftEnded = true;

        Debug.Log("[DayTimeManager] Saat 18:00 oldu! Mesai bitti. Gün sonu raporu açılıyor...");
        OnShiftEnded?.Invoke();

        if (DaySummaryManager.Instance != null)
        {
            DaySummaryManager.Instance.ShowDaySummary();
        }
    }
}
