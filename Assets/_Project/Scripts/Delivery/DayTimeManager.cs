using System;
using UnityEngine;

public class DayTimeManager : MonoBehaviour
{
    public static DayTimeManager Instance { get; private set; }

    [Header("--- WORKING HOURS ---")]
    [Tooltip("Shift start hour (e.g. 9)")]
    public int startHour = 9;
    public int startMinute = 0;

    [Tooltip("Shift end hour (e.g. 18)")]
    public int endHour = 18;
    public int endMinute = 0;

    [Tooltip("Real-time duration of the shift in minutes (e.g. 10 minutes)")]
    public float realTimeDurationInMinutes = 10f;

    [Header("--- SUN & LIGHTING ---")]
    [Tooltip("Directional Light in the scene (Auto-located if left empty)")]
    public Light directionalSun;

    [Tooltip("Morning 09:00 sun rotation")]
    public Vector3 morningSunRotation = new Vector3(25f, -30f, 0f);

    [Tooltip("Evening 18:00 sunset rotation")]
    public Vector3 eveningSunRotation = new Vector3(175f, -30f, 0f);

    [Header("--- SKYBOX & ATMOSPHERE (BOXOPHOBIC) ---")]
    [Tooltip("Skybox material (If empty, automatically uses RenderSettings.skybox)")]
    public Material skyboxMaterial;

    [Tooltip("Automatically animate the skybox Day-to-Night transition")]
    public bool autoControlSkybox = true;

    [Tooltip("Skybox Day-to-Night transition curve (0 = Day, 1 = Night)")]
    public AnimationCurve skyboxBlendCurve;

    [Tooltip("Sunlight color gradient throughout the day")]
    public Gradient sunColorGradient;

    [Tooltip("Sunlight intensity curve throughout the day")]
    public AnimationCurve sunIntensityCurve;

    [Tooltip("Horizon fog color gradient throughout the day")]
    public Gradient fogColorGradient;

    public float CurrentTimeInSeconds { get; private set; }
    public int CurrentHour { get; private set; }
    public int CurrentMinute { get; private set; }
    public bool IsShiftEnded { get; private set; }

    public static event Action OnShiftEnded;

    private float totalShiftInGameMinutes;
    private float totalRealTimeSeconds;
    private static readonly int CubemapTransitionId = Shader.PropertyToID("_CubemapTransition");

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

        if (skyboxMaterial == null)
        {
            skyboxMaterial = RenderSettings.skybox;
        }

        SetupDefaultCurves();
    }

    private void SetupDefaultCurves()
    {
        // Default Skybox Blend Curve: 0.0 at 09:00 -> 0.0 at 14:00 -> 0.4 at 17:00 -> 0.85 at 18:00
        if (skyboxBlendCurve == null || skyboxBlendCurve.length == 0)
        {
            skyboxBlendCurve = new AnimationCurve(
                new Keyframe(0f, 0f),       // 09:00 Morning
                new Keyframe(0.55f, 0.05f), // 14:00 Midday
                new Keyframe(0.85f, 0.45f), // 17:00 Late Afternoon / Sunset
                new Keyframe(1.0f, 0.85f)   // 18:00 Evening Dusk
            );
        }

        // Default Sun Intensity Curve
        if (sunIntensityCurve == null || sunIntensityCurve.length == 0)
        {
            sunIntensityCurve = new AnimationCurve(
                new Keyframe(0f, 1.0f),
                new Keyframe(0.5f, 1.25f),
                new Keyframe(0.85f, 0.9f),
                new Keyframe(1.0f, 0.4f)
            );
        }

        // Default Sun Color Gradient
        if (sunColorGradient == null || sunColorGradient.colorKeys.Length == 0)
        {
            sunColorGradient = new Gradient();
            sunColorGradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 0.92f, 0.82f), 0.0f),  // Morning Warm Gold
                    new GradientColorKey(new Color(1f, 0.98f, 0.95f), 0.4f),  // Noon White Sunlight
                    new GradientColorKey(new Color(1f, 0.65f, 0.35f), 0.85f), // Sunset Orange
                    new GradientColorKey(new Color(0.85f, 0.45f, 0.45f), 1.0f) // Dusk Red/Violet
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(1.0f, 1.0f)
                }
            );
        }

        // Default Fog Color Gradient
        if (fogColorGradient == null || fogColorGradient.colorKeys.Length == 0)
        {
            fogColorGradient = new Gradient();
            fogColorGradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0.95f, 0.85f, 0.75f), 0.0f), // Morning Horizon Peach
                    new GradientColorKey(new Color(0.82f, 0.90f, 1.0f), 0.4f),  // Noon Sky Tint
                    new GradientColorKey(new Color(1.0f, 0.60f, 0.40f), 0.85f), // Sunset Warm Gold
                    new GradientColorKey(new Color(0.35f, 0.30f, 0.50f), 1.0f)  // Night Twilight
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(1.0f, 1.0f)
                }
            );
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

        // Calculate game hour and minute
        float currentTotalInGameMinutes = (startHour * 60 + startMinute) + (progress * totalShiftInGameMinutes);
        CurrentHour = Mathf.FloorToInt(currentTotalInGameMinutes / 60f);
        CurrentMinute = Mathf.FloorToInt(currentTotalInGameMinutes % 60f);

        // Interpolate sun angle, colors, and skybox blend smoothly
        UpdateSunPosition(progress);

        // 18:00 End of Shift Check
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
        // 1. Sun Rotation
        if (directionalSun != null)
        {
            directionalSun.transform.rotation = Quaternion.Euler(Vector3.Lerp(morningSunRotation, eveningSunRotation, progress));

            if (sunColorGradient != null)
            {
                directionalSun.color = sunColorGradient.Evaluate(progress);
            }

            if (sunIntensityCurve != null)
            {
                directionalSun.intensity = sunIntensityCurve.Evaluate(progress);
            }
        }

        // 2. BOXOPHOBIC Skybox Day/Night Blend
        if (autoControlSkybox)
        {
            if (skyboxMaterial == null)
            {
                skyboxMaterial = RenderSettings.skybox;
            }

            if (skyboxMaterial != null && skyboxMaterial.HasProperty(CubemapTransitionId))
            {
                float blendVal = skyboxBlendCurve != null ? skyboxBlendCurve.Evaluate(progress) : progress;
                skyboxMaterial.SetFloat(CubemapTransitionId, blendVal);
            }
        }

        // 3. Dynamic Fog Color Harmonization
        if (RenderSettings.fog && fogColorGradient != null)
        {
            RenderSettings.fogColor = fogColorGradient.Evaluate(progress);
        }
    }

    public string GetFormattedTime()
    {
        return $"{CurrentHour:D2}:{CurrentMinute:D2}";
    }

    public void EndShift()
    {
        if (IsShiftEnded) return;
        IsShiftEnded = true;

        Debug.Log("[DayTimeManager] It's 18:00! Shift has ended. Opening Day Summary report...");
        OnShiftEnded?.Invoke();

        if (DaySummaryManager.Instance != null)
        {
            DaySummaryManager.Instance.ShowDaySummary();
        }
    }
}
