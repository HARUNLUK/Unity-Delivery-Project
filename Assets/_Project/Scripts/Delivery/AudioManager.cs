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
    public AudioClip uiTabletClose;
    public AudioClip uiTabSwitch;
    public AudioClip uiButtonClick;
    public AudioClip uiNotificationPopup;
    public AudioClip uiErrorBuzzer;
    public AudioClip uiMoneyAdd;
    public AudioClip uiMoneySubtract;
    public AudioClip uiLevelUp;

    [Header("--- 🌲 AMBIENCE & TRAFFIC SFX ---")]
    [Tooltip("2D Looping valley birds & gentle wind")]
    public AudioClip ambientDayValleyLoop;
    [Tooltip("3D Positional river water stream loop (audible only near water)")]
    public AudioClip ambientRiverStreamLoop;
    public AudioClip aiTrafficHorn;

    [Header("--- VOLUME CHANNELS ---")]
    [Range(0f, 1f)] public float masterVolume = 1.0f;
    [Range(0f, 1f)] public float sfxVolume = 1.0f;
    [Range(0f, 1f)] public float uiVolume = 0.85f;
    [Range(0f, 1f)] public float ambienceVolume = 0.45f;

    // 2D Audio Sources
    private AudioSource uiAudioSource;
    private AudioSource playerFootstepSource;
    private AudioSource ambienceAudioSource;

    // 3D River Audio Source
    private AudioSource riverAudioSource;
    private readonly List<Bounds> cachedWaterBounds = new List<Bounds>();
    private float waterScanTimer = 0f;

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
        ScanSceneWaterBodies();
        PlayAmbientLoops();
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

        // 4. 3D River AudioSource (spatialBlend = 1.0, strictly positional at river boundaries)
        if (riverAudioSource == null && ambientRiverStreamLoop != null)
        {
            GameObject riverObj = new GameObject("Audio_3D_RiverSource");
            riverObj.transform.SetParent(transform);
            riverAudioSource = riverObj.AddComponent<AudioSource>();
            riverAudioSource.spatialBlend = 1.0f; // 3D Spatial
            riverAudioSource.minDistance = 3.0f;
            riverAudioSource.maxDistance = 22.0f; // Audible only within 22 meters of water!
            riverAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            riverAudioSource.loop = true;
            riverAudioSource.playOnAwake = false;
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

    private void PlayAmbientLoops()
    {
        // Temporarily muted to allow clear isolated testing of player movement audio
        /*
        if (ambienceAudioSource != null && ambientDayValleyLoop != null)
        {
            ambienceAudioSource.clip = ambientDayValleyLoop;
            ambienceAudioSource.volume = ambienceVolume * masterVolume;
            ambienceAudioSource.Play();
        }

        if (riverAudioSource != null && ambientRiverStreamLoop != null)
        {
            riverAudioSource.clip = ambientRiverStreamLoop;
            riverAudioSource.volume = ambienceVolume * 0.95f * masterVolume;
            riverAudioSource.Play();
        }
        */
    }

    private void Update()
    {
        // Periodically rescan or update river 3D position to closest water surface point
        waterScanTimer += Time.deltaTime;
        if (waterScanTimer > 0.5f)
        {
            waterScanTimer = 0f;
            UpdateRiver3DPosition();
        }
    }

    private void ScanSceneWaterBodies()
    {
        cachedWaterBounds.Clear();

        // 1. Check WaterRespawnZone colliders
        WaterRespawnZone[] respawnZones = Object.FindObjectsByType<WaterRespawnZone>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var zone in respawnZones)
        {
            if (zone != null)
            {
                Collider c = zone.GetComponent<Collider>();
                if (c != null) cachedWaterBounds.Add(c.bounds);
                else cachedWaterBounds.Add(new Bounds(zone.transform.position, new Vector3(80f, 4f, 80f)));
            }
        }

        // 2. Check MeshRenderers with Water/River in name or materials
        MeshRenderer[] renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var mr in renderers)
        {
            if (mr == null) continue;
            string objName = mr.gameObject.name.ToLower();
            if (objName.Contains("water") || objName.Contains("river") || objName.Contains("dere") || objName.Contains("akarsu"))
            {
                cachedWaterBounds.Add(mr.bounds);
            }
            else if (mr.sharedMaterial != null)
            {
                string matName = mr.sharedMaterial.name.ToLower();
                if (matName.Contains("water") || matName.Contains("river") || matName.Contains("ocean"))
                {
                    cachedWaterBounds.Add(mr.bounds);
                }
            }
        }
    }

    private void UpdateRiver3DPosition()
    {
        if (riverAudioSource == null || !riverAudioSource.isPlaying) return;

        FPSPlayerController player = FPSPlayerController.Instance;
        Vector3 listenerPos = (player != null) ? player.transform.position : (Camera.main != null ? Camera.main.transform.position : Vector3.zero);

        if (cachedWaterBounds.Count == 0)
        {
            ScanSceneWaterBodies();
        }

        if (cachedWaterBounds.Count > 0)
        {
            Vector3 closestPoint = cachedWaterBounds[0].ClosestPoint(listenerPos);
            float minSqrDist = (closestPoint - listenerPos).sqrMagnitude;

            for (int i = 1; i < cachedWaterBounds.Count; i++)
            {
                Vector3 pt = cachedWaterBounds[i].ClosestPoint(listenerPos);
                float sqrDist = (pt - listenerPos).sqrMagnitude;
                if (sqrDist < minSqrDist)
                {
                    minSqrDist = sqrDist;
                    closestPoint = pt;
                }
            }

            // Position the 3D AudioSource exactly on the closest water surface point
            riverAudioSource.transform.position = closestPoint;
        }
        else
        {
            // If no water objects exist in the scene, place far away so it doesn't play everywhere
            riverAudioSource.transform.position = new Vector3(9999f, 9999f, 9999f);
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
            availableSource.volume = volumeScale * sfxVolume * masterVolume;
            availableSource.PlayOneShot(clip);
        }
        else
        {
            AudioSource.PlayClipAtPoint(clip, position, volumeScale * sfxVolume * masterVolume);
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
            Play3DSound(aiTrafficHorn, position, 0.90f, 2.0f, 45.0f, Random.Range(0.95f, 1.05f));
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
        if (uiTabletOpen != null) Play2DSound(uiTabletOpen, 0.85f);
    }

    public void PlayTabletClose()
    {
        if (uiTabletClose != null) Play2DSound(uiTabletClose, 0.75f);
    }

    public void PlayTabSwitch()
    {
        if (uiTabSwitch != null) Play2DSound(uiTabSwitch, 0.70f);
    }

    public void PlayButtonClick()
    {
        if (uiButtonClick != null) Play2DSound(uiButtonClick, 0.80f);
    }

    public void PlayNotification()
    {
        if (uiNotificationPopup != null) Play2DSound(uiNotificationPopup, 0.80f);
    }

    public void PlayError()
    {
        if (uiErrorBuzzer != null) Play2DSound(uiErrorBuzzer, 0.85f);
    }

    public void PlayMoneyAdd()
    {
        if (uiMoneyAdd != null) Play2DSound(uiMoneyAdd, 0.85f);
    }

    public void PlayMoneySubtract()
    {
        if (uiMoneySubtract != null) Play2DSound(uiMoneySubtract, 0.80f);
    }

    public void PlayLevelUp()
    {
        if (uiLevelUp != null) Play2DSound(uiLevelUp, 1.0f);
    }

    #endregion
}
