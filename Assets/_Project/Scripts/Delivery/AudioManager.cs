using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Centralized Audio Management System.
/// - 2D Non-Spatial Audio: Valley Ambience, Player Footsteps/Jump/Land, and all UI sounds (Tablet, Clicks, Tabs, Money, Notifications).
/// - 3D Positional Audio: River Water (emits strictly from river water boundaries), Cargo Physics impacts/throws, Vehicle Crashes, Commercial Tools, and Traffic Horns.
/// </summary>
public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Object.FindAnyObjectByType<AudioManager>();
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("--- 🚶 PLAYER SFX (2D Normal) ---")]
    public AudioClip playerFootstep;
    [Range(0f, 2f), Tooltip("Yürüme adım sesi şiddeti (Base Walk Volume)")]
    public float playerFootstepWalkVolume = 0.60f;
    [Range(0f, 2f), Tooltip("Koşma adım sesi şiddeti (Base Sprint Volume)")]
    public float playerFootstepSprintVolume = 0.85f;
    [Range(0.5f, 2.0f), Tooltip("Adım sesi temel ton/pitch (1.0 = orijinal)")]
    public float playerFootstepBasePitch = 1.0f;
    [Range(0f, 0.4f), Tooltip("Adımlar arası rastgele ton çeşitliliği (+/-)")]
    public float playerFootstepPitchVariation = 0.07f;

    public AudioClip playerJump;
    [Range(0f, 2f), Tooltip("Zıplama sesi şiddeti (Base Jump Volume)")]
    public float playerJumpVolume = 0.70f;

    public AudioClip playerLand;
    [Range(0f, 2f), Tooltip("Yere iniş/düşme sesi şiddeti (Base Land Volume)")]
    public float playerLandVolume = 0.85f;

    [Header("--- 📦 CARGO & PHYSICS SFX (3D Spatial) ---")]
    public AudioClip cargoGrab;
    public AudioClip cargoDropLight;
    public AudioClip cargoDropMedium;
    public AudioClip cargoDropHeavy;
    public AudioClip cargoThrow;
    public AudioClip cargoFragileRattle;
    public AudioClip cargoFragileBreak;
    public AudioClip cargoExplosiveDetonation;

    [Header("--- 🚗 VEHICLE SFX (3D Spatial) ---")]
    public AudioClip vehicleEngineIdleLoop;
    [Range(0f, 2f), Tooltip("Araç motor rölanti/sürüş genel ses çarpanı")]
    public float vehicleEngineVolume = 1.0f;

    public AudioClip vehicleReverseBeepLoop;
    [Range(0f, 2f), Tooltip("Geri vites bip sesi şiddeti")]
    public float vehicleReverseBeepVolume = 0.65f;

    public AudioClip vehicleBrakeSqueak;
    [Range(0f, 2f), Tooltip("Fren balata ve kayma sesi şiddeti")]
    public float vehicleBrakeSqueakVolume = 0.70f;

    public AudioClip vehicleDoorOpen;
    [Range(0f, 2f), Tooltip("Kapı açılış sesi şiddeti")]
    public float vehicleDoorOpenVolume = 0.85f;

    public AudioClip vehicleDoorClose;
    [Range(0f, 2f), Tooltip("Kapı kapanış sesi şiddeti")]
    public float vehicleDoorCloseVolume = 0.90f;

    public AudioClip vehicleTailgateOpen;
    public AudioClip vehicleTailgateClose;
    public AudioClip vehicleCrashLight;
    public AudioClip vehicleCrashHeavy;
    [Range(0f, 2f), Tooltip("Kaza darbe sesi şiddeti")]
    public float vehicleCrashVolume = 0.90f;

    [Header("--- 🏢 COMMERCIAL & GARAGE SFX (3D Spatial) ---")]
    public AudioClip fuelPumpingLoop;
    [Range(0f, 2f), Tooltip("Yakıt doldurma akış sesi şiddeti")]
    public float fuelPumpingVolume = 0.95f;

    public AudioClip fuelPumpFinishBeep;
    [Range(0f, 2f), Tooltip("Depo doldu ikaz bip sesi şiddeti")]
    public float fuelPumpFinishBeepVolume = 0.90f;

    public AudioClip garageRepairWrench;
    [Range(0f, 2f), Tooltip("Garaj tamir anahtar sesi şiddeti")]
    public float garageRepairVolume = 0.85f;

    public AudioClip garagePaintSpray;
    [Range(0f, 2f), Tooltip("Garaj boya sprey sesi şiddeti")]
    public float garagePaintVolume = 0.85f;

    public AudioClip propertyPurchaseCash;
    [Range(0f, 2f), Tooltip("Bina/mülk satın alma nakit sesi şiddeti")]
    public float propertyPurchaseVolume = 0.90f;

    [Header("--- 🖥️ UI & SYSTEM SFX (2D Normal) ---")]
    public AudioClip uiTabletOpen;
    [Range(0f, 2f), Tooltip("Tablet açılış sesi şiddeti")]
    public float uiTabletOpenVolume = 0.85f;

    public AudioClip uiTabletClose;
    [Range(0f, 2f), Tooltip("Tablet kapanış sesi şiddeti")]
    public float uiTabletCloseVolume = 0.75f;

    public AudioClip uiTabSwitch;
    [Range(0f, 2f), Tooltip("Tablet sekme değiştirme sesi şiddeti")]
    public float uiTabSwitchVolume = 0.75f;

    public AudioClip uiButtonClick;
    [Range(0f, 2f), Tooltip("Buton tıklama sesi şiddeti")]
    public float uiButtonClickVolume = 0.80f;

    public AudioClip uiNotificationPopup;
    [Range(0f, 2f), Tooltip("Bildirim açılış sesi şiddeti")]
    public float uiNotificationVolume = 0.80f;

    public AudioClip uiErrorBuzzer;
    [Range(0f, 2f), Tooltip("Hata/uyarı sesi şiddeti")]
    public float uiErrorVolume = 0.85f;

    public AudioClip uiMoneyAdd;
    [Range(0f, 2f), Tooltip("Para kazanma sesi şiddeti")]
    public float uiMoneyAddVolume = 0.85f;

    public AudioClip uiMoneySubtract;
    [Range(0f, 2f), Tooltip("Para harcama sesi şiddeti")]
    public float uiMoneySubtractVolume = 0.80f;

    public AudioClip uiLevelUp;
    [Range(0f, 2f), Tooltip("Seviye atlama sesi şiddeti")]
    public float uiLevelUpVolume = 1.0f;

    [Header("--- 🎵 BACKGROUND MUSIC (2D BGM) ---")]
    [Tooltip("Arka planda çalan ana fon müziği (2D Stereo Loop)")]
    public AudioClip backgroundMusic;
    [Range(0f, 2f), Tooltip("Arka plan müzik ses şiddeti (Base Music Volume)")]
    public float backgroundMusicVolume = 0.50f;
    [Tooltip("Oyun başladığında müziği otomatik başlat")]
    public bool playMusicOnStart = true;

    [Header("--- 🌲 AMBIENCE & TRAFFIC SFX ---")]
    [Tooltip("2D Looping valley birds & gentle wind")]
    public AudioClip ambientDayValleyLoop;
    [Range(0f, 2f), Tooltip("2D Vadi rüzgar ve kuş atmosfer ses şiddeti")]
    public float ambientDayValleyVolume = 0.40f;

    public AudioClip aiTrafficHorn;
    [Range(0f, 2f), Tooltip("Yapay zeka araç korna sesi şiddeti")]
    public float aiTrafficHornVolume = 0.90f;

    [Header("--- VOLUME CHANNELS ---")]
    [Range(0f, 1f)] public float masterVolume = 1.0f;
    [Range(0f, 1f)] public float musicVolume = 0.80f;
    [Range(0f, 1f)] public float sfxVolume = 1.0f;
    [Range(0f, 1f)] public float uiVolume = 0.85f;
    [Range(0f, 1f)] public float ambienceVolume = 0.50f;

    // 2D Audio Sources
    private AudioSource uiAudioSource;
    private AudioSource playerFootstepSource;
    private AudioSource ambienceAudioSource;
    private AudioSource musicAudioSource;

    // 3D Audio Source Pool
    private readonly List<AudioSource> audioSourcePool = new List<AudioSource>();
    private const int POOL_SIZE = 16;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureAudioSources();
    }

    private void Start()
    {
        PlayAmbientLoops();
        if (playMusicOnStart && backgroundMusic != null)
        {
            PlayBackgroundMusic();
        }
    }

    private void EnsureAudioSources()
    {
        // 1. 2D UI AudioSource (spatialBlend = 0)
        if (uiAudioSource == null)
        {
            GameObject uiObj = new GameObject("Audio_2D_UISource");
            uiObj.transform.SetParent(transform);
            uiAudioSource = uiObj.AddComponent<AudioSource>();
            uiAudioSource.spatialBlend = 0f; // 2D Stereo
            uiAudioSource.playOnAwake = false;
        }

        // 2. 2D Player Footsteps/Jump/Land AudioSource (spatialBlend = 0)
        if (playerFootstepSource == null)
        {
            GameObject stepObj = new GameObject("Audio_2D_PlayerFootsteps");
            stepObj.transform.SetParent(transform);
            playerFootstepSource = stepObj.AddComponent<AudioSource>();
            playerFootstepSource.spatialBlend = 0f; // 2D Stereo
            playerFootstepSource.playOnAwake = false;
        }

        // 3. 2D Ambience AudioSource (Valley & Wind) (spatialBlend = 0)
        if (ambienceAudioSource == null)
        {
            GameObject ambObj = new GameObject("Audio_2D_AmbienceValley");
            ambObj.transform.SetParent(transform);
            ambienceAudioSource = ambObj.AddComponent<AudioSource>();
            ambienceAudioSource.spatialBlend = 0f; // 2D Background
            ambienceAudioSource.loop = true;
            ambienceAudioSource.playOnAwake = false;
        }

        // 4. 2D Background Music AudioSource (spatialBlend = 0)
        if (musicAudioSource == null)
        {
            GameObject musicObj = new GameObject("Audio_2D_MusicSource");
            musicObj.transform.SetParent(transform);
            musicAudioSource = musicObj.AddComponent<AudioSource>();
            musicAudioSource.spatialBlend = 0f; // 2D Stereo
            musicAudioSource.loop = true;
            musicAudioSource.playOnAwake = false;
        }

        // 5. 3D World SFX Pool (spatialBlend = 1.0)
        if (audioSourcePool.Count == 0)
        {
            for (int i = 0; i < POOL_SIZE; i++)
            {
                GameObject poolObj = new GameObject($"Audio_3D_PoolSource_{i}");
                poolObj.transform.SetParent(transform);
                AudioSource src = poolObj.AddComponent<AudioSource>();
                src.spatialBlend = 1.0f; // 3D
                src.playOnAwake = false;
                src.minDistance = 1.5f;
                src.maxDistance = 35.0f;
                src.rolloffMode = AudioRolloffMode.Logarithmic;
                audioSourcePool.Add(src);
            }
        }
    }

    public void PlayAmbientLoops()
    {
        EnsureAudioSources();

        if (ambienceAudioSource != null && ambientDayValleyLoop != null)
        {
            if (ambienceAudioSource.clip != ambientDayValleyLoop)
            {
                ambienceAudioSource.clip = ambientDayValleyLoop;
            }
            ambienceAudioSource.volume = ambientDayValleyVolume * ambienceVolume * masterVolume;
            if (!ambienceAudioSource.isPlaying)
            {
                ambienceAudioSource.Play();
            }
        }
    }

    public void PlayBackgroundMusic()
    {
        EnsureAudioSources();

        if (musicAudioSource != null && backgroundMusic != null)
        {
            if (musicAudioSource.clip != backgroundMusic)
            {
                musicAudioSource.clip = backgroundMusic;
            }
            musicAudioSource.volume = backgroundMusicVolume * musicVolume * masterVolume;
            if (!musicAudioSource.isPlaying)
            {
                musicAudioSource.Play();
            }
        }
    }

    public void StopBackgroundMusic()
    {
        if (musicAudioSource != null && musicAudioSource.isPlaying)
        {
            musicAudioSource.Stop();
        }
    }

    public void SetMusicTrack(AudioClip newTrack, bool autoPlay = true)
    {
        backgroundMusic = newTrack;
        if (autoPlay && newTrack != null)
        {
            PlayBackgroundMusic();
        }
    }

    private void Update()
    {
        // Dynamic volume synchronization for 2D valley ambience loop
        if (ambienceAudioSource != null && ambienceAudioSource.isPlaying)
        {
            ambienceAudioSource.volume = ambientDayValleyVolume * ambienceVolume * masterVolume;
        }

        // Dynamic volume synchronization for 2D background music loop
        if (musicAudioSource != null && musicAudioSource.isPlaying)
        {
            musicAudioSource.volume = backgroundMusicVolume * musicVolume * masterVolume;
        }
    }

    public void SetVolumeChannels(float master, float music, float sfx, float ambience, float ui)
    {
        masterVolume = Mathf.Clamp01(master);
        musicVolume = Mathf.Clamp01(music);
        sfxVolume = Mathf.Clamp01(sfx);
        ambienceVolume = Mathf.Clamp01(ambience);
        uiVolume = Mathf.Clamp01(ui);

        AudioListener.volume = masterVolume;

        if (ambienceAudioSource != null && ambienceAudioSource.isPlaying)
        {
            ambienceAudioSource.volume = ambientDayValleyVolume * ambienceVolume * masterVolume;
        }

        if (musicAudioSource != null && musicAudioSource.isPlaying)
        {
            musicAudioSource.volume = backgroundMusicVolume * musicVolume * masterVolume;
        }
    }

    #region --- 2D & 3D PLAYBACK CORE HELPERS ---

    public void Play2DSound(AudioClip clip, float volumeScale = 1.0f, float pitch = 1.0f)
    {
        if (clip == null) return;
        EnsureAudioSources();

        if (uiAudioSource != null)
        {
            uiAudioSource.pitch = pitch;
            uiAudioSource.PlayOneShot(clip, volumeScale * uiVolume * masterVolume);
        }
    }

    public void Play3DSound(AudioClip clip, Vector3 position, float volumeScale = 1.0f, float minDistance = 1.5f, float maxDistance = 35.0f, float pitch = 1.0f)
    {
        if (clip == null) return;
        EnsureAudioSources();

        float effectiveVol = Mathf.Clamp01(volumeScale * sfxVolume * masterVolume);

        AudioSource availableSource = null;
        for (int i = 0; i < audioSourcePool.Count; i++)
        {
            if (!audioSourcePool[i].isPlaying)
            {
                availableSource = audioSourcePool[i];
                break;
            }
        }

        if (availableSource == null && audioSourcePool.Count > 0)
        {
            availableSource = audioSourcePool[0]; // Recycle oldest
        }

        if (availableSource != null)
        {
            availableSource.transform.position = position;
            availableSource.minDistance = minDistance;
            availableSource.maxDistance = maxDistance;
            availableSource.pitch = pitch;
            availableSource.volume = effectiveVol;
            availableSource.PlayOneShot(clip, effectiveVol);
        }
        else
        {
            AudioSource.PlayClipAtPoint(clip, position, effectiveVol);
        }
    }

    #endregion

    #region --- 🚶 PLAYER METHODS (2D Normal) ---

    public void PlayFootstep(bool isSprinting)
    {
        if (playerFootstep == null) return;
        EnsureAudioSources();

        if (playerFootstepSource != null)
        {
            float pitchOffset = Random.Range(-playerFootstepPitchVariation, playerFootstepPitchVariation);
            float currentPitch = (playerFootstepBasePitch + pitchOffset) * (isSprinting ? 1.08f : 1.0f);
            float baseVol = isSprinting ? playerFootstepSprintVolume : playerFootstepWalkVolume;
            float volume = baseVol * sfxVolume * masterVolume;

            playerFootstepSource.pitch = currentPitch;
            playerFootstepSource.PlayOneShot(playerFootstep, volume);
        }
    }

    public void PlayJump()
    {
        if (playerJump == null) return;
        EnsureAudioSources();

        if (playerFootstepSource != null)
        {
            playerFootstepSource.pitch = Random.Range(0.96f, 1.04f);
            playerFootstepSource.PlayOneShot(playerJump, playerJumpVolume * sfxVolume * masterVolume);
        }
    }

    public void PlayLand()
    {
        if (playerLand == null) return;
        EnsureAudioSources();

        if (playerFootstepSource != null)
        {
            playerFootstepSource.pitch = Random.Range(0.94f, 1.06f);
            playerFootstepSource.PlayOneShot(playerLand, playerLandVolume * sfxVolume * masterVolume);
        }
    }

    #endregion

    #region --- 📦 CARGO METHODS (3D Spatial at Package Position) ---

    public void PlayCargoGrab(Vector3 position)
    {
        if (cargoGrab != null)
        {
            Play3DSound(cargoGrab, position, 0.75f, 1.0f, 18.0f, Random.Range(0.95f, 1.05f));
        }
    }

    public void PlayCargoThrow(Vector3 position)
    {
        if (cargoThrow != null)
        {
            Play3DSound(cargoThrow, position, 0.8f, 1.0f, 20.0f, Random.Range(0.96f, 1.05f));
        }
    }

    public void PlayCargoDrop(Vector3 position, float impactSpeed)
    {
        if (impactSpeed < 1.5f)
        {
            if (cargoDropLight != null) Play3DSound(cargoDropLight, position, 0.55f, 1.0f, 20.0f, Random.Range(0.95f, 1.05f));
        }
        else if (impactSpeed < 5.0f)
        {
            if (cargoDropMedium != null) Play3DSound(cargoDropMedium, position, 0.75f, 1.0f, 25.0f, Random.Range(0.93f, 1.07f));
            else if (cargoDropLight != null) Play3DSound(cargoDropLight, position, 0.85f, 1.0f, 20.0f);
        }
        else
        {
            if (cargoDropHeavy != null) Play3DSound(cargoDropHeavy, position, 1.0f, 1.5f, 35.0f, Random.Range(0.92f, 1.05f));
            else if (cargoDropMedium != null) Play3DSound(cargoDropMedium, position, 1.0f, 1.0f, 25.0f);
        }
    }

    public void PlayCargoFragileRattle(Vector3 position)
    {
        if (cargoFragileRattle != null)
        {
            Play3DSound(cargoFragileRattle, position, 0.75f, 1.0f, 22.0f, Random.Range(0.95f, 1.05f));
        }
    }

    public void PlayCargoFragileBreak(Vector3 position)
    {
        if (cargoFragileBreak != null)
        {
            Play3DSound(cargoFragileBreak, position, 1.0f, 2.0f, 40.0f, Random.Range(0.96f, 1.04f));
        }
    }

    public void PlayCargoExplosion(Vector3 position)
    {
        if (cargoExplosiveDetonation != null)
        {
            Play3DSound(cargoExplosiveDetonation, position, 1.0f, 5.0f, 80.0f, Random.Range(0.95f, 1.05f));
        }
    }

    #endregion

    #region --- 🚗 VEHICLE METHODS (3D Spatial at Vehicle Position) ---

    public void PlayVehicleCrash(Vector3 position, float impactSpeed)
    {
        float vol = vehicleCrashVolume * (impactSpeed > 10.0f ? 1.0f : 0.8f);
        if (impactSpeed > 10.0f && vehicleCrashHeavy != null)
        {
            Play3DSound(vehicleCrashHeavy, position, vol, 3.0f, 50.0f, Random.Range(0.95f, 1.05f));
        }
        else if (vehicleCrashLight != null)
        {
            Play3DSound(vehicleCrashLight, position, vol, 2.0f, 35.0f, Random.Range(0.95f, 1.05f));
        }
    }

    public void PlayVehicleDoorOpen(Vector3 position)
    {
        if (vehicleDoorOpen != null)
        {
            Play3DSound(vehicleDoorOpen, position, vehicleDoorOpenVolume, 1.5f, 25.0f, Random.Range(0.97f, 1.03f));
        }
    }

    public void PlayVehicleDoorClose(Vector3 position)
    {
        if (vehicleDoorClose != null)
        {
            Play3DSound(vehicleDoorClose, position, vehicleDoorCloseVolume, 1.5f, 25.0f, Random.Range(0.97f, 1.03f));
        }
    }

    public void PlayTrafficHorn(Vector3 position)
    {
        if (aiTrafficHorn != null)
        {
            Play3DSound(aiTrafficHorn, position, aiTrafficHornVolume, 3.0f, 55.0f, Random.Range(0.95f, 1.05f));
        }
    }

    #endregion

    #region --- 🏢 COMMERCIAL METHODS (3D Spatial at Station Position) ---

    public void PlayFuelPumpFinish(Vector3 position)
    {
        if (fuelPumpFinishBeep != null)
        {
            Play3DSound(fuelPumpFinishBeep, position, fuelPumpFinishBeepVolume, 2.5f, 35.0f);
        }
    }

    public void PlayGarageRepair(Vector3 position)
    {
        if (garageRepairWrench != null)
        {
            Play3DSound(garageRepairWrench, position, garageRepairVolume, 2.0f, 30.0f);
        }
    }

    public void PlayGaragePaint(Vector3 position)
    {
        if (garagePaintSpray != null)
        {
            Play3DSound(garagePaintSpray, position, garagePaintVolume, 2.0f, 30.0f);
        }
    }

    public void PlayPropertyPurchase(Vector3 position)
    {
        if (propertyPurchaseCash != null)
        {
            Play3DSound(propertyPurchaseCash, position, propertyPurchaseVolume, 2.0f, 35.0f);
        }
        PlayMoneyAdd();
    }

    #endregion

    #region --- 🖥️ UI METHODS (2D Normal) ---

    public void PlayTabletOpen()
    {
        if (uiTabletOpen != null) Play2DSound(uiTabletOpen, uiTabletOpenVolume);
    }

    public void PlayTabletClose()
    {
        if (uiTabletClose != null) Play2DSound(uiTabletClose, uiTabletCloseVolume);
    }

    public void PlayMenuOpen()
    {
        if (uiTabletOpen != null) Play2DSound(uiTabletOpen, uiTabletOpenVolume);
        else PlayButtonClick();
    }

    public void PlayMenuClose()
    {
        if (uiTabletClose != null) Play2DSound(uiTabletClose, uiTabletCloseVolume);
        else PlayButtonClick();
    }

    public void PlayTabSwitch()
    {
        if (uiTabSwitch != null) Play2DSound(uiTabSwitch, uiTabSwitchVolume);
    }

    public void PlayButtonClick()
    {
        if (uiButtonClick != null) Play2DSound(uiButtonClick, uiButtonClickVolume);
    }

    public void PlayNotification()
    {
        if (uiNotificationPopup != null) Play2DSound(uiNotificationPopup, uiNotificationVolume);
    }

    public void PlayError()
    {
        if (uiErrorBuzzer != null) Play2DSound(uiErrorBuzzer, uiErrorVolume);
    }

    public void PlayMoneyAdd()
    {
        if (uiMoneyAdd != null) Play2DSound(uiMoneyAdd, uiMoneyAddVolume);
    }

    public void PlayMoneySubtract()
    {
        if (uiMoneySubtract != null) Play2DSound(uiMoneySubtract, uiMoneySubtractVolume);
    }

    public void PlayLevelUp()
    {
        if (uiLevelUp != null) Play2DSound(uiLevelUp, uiLevelUpVolume);
    }

    #endregion
}
