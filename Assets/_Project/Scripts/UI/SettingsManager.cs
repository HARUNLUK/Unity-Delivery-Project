using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages and persists all Game Settings (Audio, Graphics, Gameplay Controls).
/// Automatically loads and applies saved settings on Awake/Start.
/// </summary>
public class SettingsManager : MonoBehaviour
{
    private static SettingsManager _instance;
    public static SettingsManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = UnityEngine.Object.FindAnyObjectByType<SettingsManager>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("SettingsManager");
                    _instance = obj.AddComponent<SettingsManager>();
                    DontDestroyOnLoad(obj);
                }
            }
            return _instance;
        }
        private set => _instance = value;
    }

    #region --- PLAYERPREFS KEYS ---
    private const string KEY_MASTER_VOL = "Settings_MasterVolume";
    private const string KEY_MUSIC_VOL = "Settings_MusicVolume";
    private const string KEY_SFX_VOL = "Settings_SfxVolume";
    private const string KEY_AMBIENCE_VOL = "Settings_AmbienceVolume";
    private const string KEY_UI_VOL = "Settings_UiVolume";

    private const string KEY_QUALITY_LEVEL = "Settings_QualityLevel";
    private const string KEY_FULLSCREEN_MODE = "Settings_FullscreenMode";
    private const string KEY_RESOLUTION_INDEX = "Settings_ResolutionIndex";
    private const string KEY_VSYNC = "Settings_VSync";
    private const string KEY_TARGET_FPS = "Settings_TargetFps";

    private const string KEY_MOUSE_SENS = "Settings_MouseSensitivity";
    private const string KEY_INVERT_Y = "Settings_InvertY";
    private const string KEY_LANGUAGE = "SelectedLanguage";
    #endregion

    [Header("--- LANGUAGE SETTINGS ---")]
    public string language = "tr"; // "tr" or "en"

    [Header("--- AUDIO SETTINGS ---")]
    [Range(0f, 1f)] public float masterVolume = 1.0f;
    [Range(0f, 1f)] public float musicVolume = 0.8f;
    [Range(0f, 1f)] public float sfxVolume = 1.0f;
    [Range(0f, 1f)] public float ambienceVolume = 0.6f;
    [Range(0f, 1f)] public float uiVolume = 0.85f;

    [Header("--- GRAPHICS SETTINGS ---")]
    public int qualityLevel = 1; // 0: Mobile (Low), 1: PC (High/Ultra)
    public int fullscreenMode = 0; // 0: Exclusive Fullscreen, 1: Borderless Windowed, 2: Windowed
    public int resolutionIndex = -1;
    public bool vsyncEnabled = true;
    public int targetFps = 60; // 30, 60, 120, 144, -1 (Unlimited)

    [Header("--- GAMEPLAY / CONTROLS ---")]
    [Range(0.2f, 5.0f)] public float mouseSensitivity = 2.0f;
    public bool invertMouseY = false;

    public static event Action OnSettingsChanged;

    private Resolution[] availableResolutions;
    public Resolution[] AvailableResolutions => availableResolutions;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        CacheResolutions();
        LoadAllSettings();
        ApplyAllSettings();
    }

    private void Start()
    {
        ApplyAllSettings();
    }

    private void CacheResolutions()
    {
        availableResolutions = Screen.resolutions;
    }

    public void LoadAllSettings()
    {
        // 1. Audio
        masterVolume = PlayerPrefs.GetFloat(KEY_MASTER_VOL, 1.0f);
        musicVolume = PlayerPrefs.GetFloat(KEY_MUSIC_VOL, 0.8f);
        sfxVolume = PlayerPrefs.GetFloat(KEY_SFX_VOL, 1.0f);
        ambienceVolume = PlayerPrefs.GetFloat(KEY_AMBIENCE_VOL, 0.6f);
        uiVolume = PlayerPrefs.GetFloat(KEY_UI_VOL, 0.85f);

        // 2. Graphics (Default to PC Quality Level 1 on Standalone/Editor)
        int defaultQuality = Mathf.Max(0, QualitySettings.names.Length - 1); // Highest quality (1: PC)
        if (PlayerPrefs.HasKey(KEY_QUALITY_LEVEL))
        {
            qualityLevel = PlayerPrefs.GetInt(KEY_QUALITY_LEVEL, defaultQuality);
            // If saved as 0 on PC/Standalone from previous test, recover to PC quality
            if (qualityLevel < 1 && QualitySettings.names.Length > 1 && Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
            {
                qualityLevel = 1;
            }
        }
        else
        {
            qualityLevel = defaultQuality;
        }

        fullscreenMode = PlayerPrefs.GetInt(KEY_FULLSCREEN_MODE, 0);
        resolutionIndex = PlayerPrefs.GetInt(KEY_RESOLUTION_INDEX, -1);
        vsyncEnabled = PlayerPrefs.GetInt(KEY_VSYNC, 1) == 1;
        targetFps = PlayerPrefs.GetInt(KEY_TARGET_FPS, 60);

        // 3. Gameplay
        mouseSensitivity = PlayerPrefs.GetFloat(KEY_MOUSE_SENS, 2.0f);
        invertMouseY = PlayerPrefs.GetInt(KEY_INVERT_Y, 0) == 1;

        // 4. Language
        string defaultLang = Application.systemLanguage == SystemLanguage.Turkish ? "tr" : "en";
        language = PlayerPrefs.GetString(KEY_LANGUAGE, defaultLang);
    }

    public void SaveAllSettings()
    {
        // 1. Audio
        PlayerPrefs.SetFloat(KEY_MASTER_VOL, masterVolume);
        PlayerPrefs.SetFloat(KEY_MUSIC_VOL, musicVolume);
        PlayerPrefs.SetFloat(KEY_SFX_VOL, sfxVolume);
        PlayerPrefs.SetFloat(KEY_AMBIENCE_VOL, ambienceVolume);
        PlayerPrefs.SetFloat(KEY_UI_VOL, uiVolume);

        // 2. Graphics
        PlayerPrefs.SetInt(KEY_QUALITY_LEVEL, qualityLevel);
        PlayerPrefs.SetInt(KEY_FULLSCREEN_MODE, fullscreenMode);
        PlayerPrefs.SetInt(KEY_RESOLUTION_INDEX, resolutionIndex);
        PlayerPrefs.SetInt(KEY_VSYNC, vsyncEnabled ? 1 : 0);
        PlayerPrefs.SetInt(KEY_TARGET_FPS, targetFps);

        // 3. Gameplay
        PlayerPrefs.SetFloat(KEY_MOUSE_SENS, mouseSensitivity);
        PlayerPrefs.SetInt(KEY_INVERT_Y, invertMouseY ? 1 : 0);

        // 4. Language
        PlayerPrefs.SetString(KEY_LANGUAGE, language);

        PlayerPrefs.Save();
        OnSettingsChanged?.Invoke();
    }

    public void ApplyAllSettings()
    {
        ApplyLanguageSettings();
        ApplyAudioSettings();
        ApplyGraphicsSettings();
        ApplyGameplaySettings();
    }

    #region --- LANGUAGE APPLICATION ---
    public void ApplyLanguageSettings()
    {
        LocalizationManager.SetLanguage(language);
    }

    public void SetLanguage(string langCode)
    {
        if (string.IsNullOrEmpty(langCode)) langCode = "tr";
        language = langCode.ToLower().Trim();
        LocalizationManager.SetLanguage(language);
        SaveAllSettings();
    }
    #endregion

    #region --- AUDIO APPLICATION ---
    public void ApplyAudioSettings()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.masterVolume = masterVolume;
            AudioManager.Instance.musicVolume = musicVolume;
            AudioManager.Instance.sfxVolume = sfxVolume;
            AudioManager.Instance.ambienceVolume = ambienceVolume;
            AudioManager.Instance.uiVolume = uiVolume;
        }
    }

    public void SetMasterVolume(float val)
    {
        masterVolume = Mathf.Clamp01(val);
        ApplyAudioSettings();
    }

    public void SetMusicVolume(float val)
    {
        musicVolume = Mathf.Clamp01(val);
        ApplyAudioSettings();
    }

    public void SetSfxVolume(float val)
    {
        sfxVolume = Mathf.Clamp01(val);
        ApplyAudioSettings();
    }

    public void SetAmbienceVolume(float val)
    {
        ambienceVolume = Mathf.Clamp01(val);
        ApplyAudioSettings();
    }

    public void SetUiVolume(float val)
    {
        uiVolume = Mathf.Clamp01(val);
        ApplyAudioSettings();
    }
    #endregion

    #region --- GRAPHICS APPLICATION ---
    public void ApplyGraphicsSettings()
    {
        // 1. Quality Level
        if (qualityLevel >= 0 && qualityLevel < QualitySettings.names.Length)
        {
            QualitySettings.SetQualityLevel(qualityLevel, true);
        }

        // 2. V-Sync & Target FPS
        QualitySettings.vSyncCount = vsyncEnabled ? 1 : 0;
        Application.targetFrameRate = vsyncEnabled ? -1 : targetFps;

        // 3. Fullscreen & Resolution
        FullScreenMode fsMode = FullScreenMode.ExclusiveFullScreen;
        if (fullscreenMode == 1) fsMode = FullScreenMode.FullScreenWindow;
        else if (fullscreenMode == 2) fsMode = FullScreenMode.Windowed;

        if (availableResolutions != null && resolutionIndex >= 0 && resolutionIndex < availableResolutions.Length)
        {
            Resolution res = availableResolutions[resolutionIndex];
            Screen.SetResolution(res.width, res.height, fsMode, res.refreshRateRatio);
        }
        else
        {
            Screen.fullScreenMode = fsMode;
        }
    }

    public void SetQualityLevel(int level)
    {
        qualityLevel = Mathf.Clamp(level, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
        QualitySettings.SetQualityLevel(qualityLevel, true);
    }

    public void SetFullscreenMode(int mode)
    {
        fullscreenMode = mode;
        FullScreenMode fsMode = FullScreenMode.ExclusiveFullScreen;
        if (fullscreenMode == 1) fsMode = FullScreenMode.FullScreenWindow;
        else if (fullscreenMode == 2) fsMode = FullScreenMode.Windowed;
        Screen.fullScreenMode = fsMode;
    }

    public void SetResolution(int resIdx)
    {
        if (availableResolutions == null || resIdx < 0 || resIdx >= availableResolutions.Length) return;
        resolutionIndex = resIdx;
        Resolution res = availableResolutions[resolutionIndex];

        FullScreenMode fsMode = FullScreenMode.ExclusiveFullScreen;
        if (fullscreenMode == 1) fsMode = FullScreenMode.FullScreenWindow;
        else if (fullscreenMode == 2) fsMode = FullScreenMode.Windowed;

        Screen.SetResolution(res.width, res.height, fsMode, res.refreshRateRatio);
    }

    public void SetVSync(bool enabled)
    {
        vsyncEnabled = enabled;
        QualitySettings.vSyncCount = vsyncEnabled ? 1 : 0;
        Application.targetFrameRate = vsyncEnabled ? -1 : targetFps;
    }

    public void SetTargetFps(int fps)
    {
        targetFps = fps;
        if (!vsyncEnabled)
        {
            Application.targetFrameRate = targetFps;
        }
    }
    #endregion

    #region --- GAMEPLAY APPLICATION ---
    public void ApplyGameplaySettings()
    {
        if (FPSPlayerController.Instance != null)
        {
            FPSPlayerController.Instance.mouseSensitivity = mouseSensitivity;
            FPSPlayerController.Instance.inVehicleMouseSensitivity = mouseSensitivity;
        }
    }

    public void SetMouseSensitivity(float sens)
    {
        mouseSensitivity = Mathf.Clamp(sens, 0.2f, 5.0f);
        ApplyGameplaySettings();
    }

    public void SetInvertY(bool invert)
    {
        invertMouseY = invert;
    }
    #endregion
}
