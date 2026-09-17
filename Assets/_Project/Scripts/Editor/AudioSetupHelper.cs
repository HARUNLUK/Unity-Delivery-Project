#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AudioSetupHelper
{
    [InitializeOnLoadMethod]
    private static void OnEditorLoad()
    {
        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SetupAndConnectAllAudio();
            }
        };
    }

    [MenuItem("Tools/Audio/Auto Setup & Connect All Audio Files", false, 1)]
    [MenuItem("GameObject/Audio/Setup Scene Audio Manager", false, 10)]
    public static void SetupAndConnectAllAudio()
    {
        // 1. Find or create AudioManager GameObject in the active scene
        AudioManager manager = Object.FindAnyObjectByType<AudioManager>();
        if (manager == null)
        {
            GameObject obj = new GameObject("AudioManager");
            manager = obj.AddComponent<AudioManager>();
            Undo.RegisterCreatedObjectUndo(obj, "Create AudioManager");
        }

        Undo.RecordObject(manager, "Auto Assign Audio Clips");

        // 2. Find all audio clips in Assets/_Project/Audio/
        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new string[] { "Assets/_Project/Audio" });
        Debug.Log($"<color=#32FF64>[AudioSetupHelper] Scanning Assets/_Project/Audio/... Found {guids.Length} audio files.</color>");

        int assignedCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) continue;

            string nameWithoutExt = Path.GetFileNameWithoutExtension(path).Trim();

            if (AssignClip(manager, nameWithoutExt, clip))
            {
                assignedCount++;
                Debug.Log($"<color=#00E5FF>[AudioSetupHelper] Connected: {nameWithoutExt} ({Path.GetExtension(path)})</color>");
            }
        }

        // Smart Fallbacks for consolidated audio design
        if (manager.uiMoneySubtract == null && manager.uiMoneyAdd != null)
        {
            manager.uiMoneySubtract = manager.uiMoneyAdd;
            Debug.Log("<color=#FFD54F>[AudioSetupHelper] Fallback: Assigned uiMoneyAdd as uiMoneySubtract.</color>");
        }

        EditorUtility.SetDirty(manager);

        // 3. Ensure all vehicles in scene have VehicleAudioController
        DrivableVehicle[] vehicles = Object.FindObjectsByType<DrivableVehicle>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var v in vehicles)
        {
            if (v != null && v.GetComponent<VehicleAudioController>() == null)
            {
                Undo.AddComponent<VehicleAudioController>(v.gameObject);
                EditorUtility.SetDirty(v.gameObject);
            }
        }

        // 4. Ensure Vehicle prefabs have VehicleAudioController
        string[] vehiclePrefabGuids = AssetDatabase.FindAssets("t:Prefab", new string[] { "Assets/_Project/Prefabs/Driveables" });
        foreach (string vGuid in vehiclePrefabGuids)
        {
            string vPath = AssetDatabase.GUIDToAssetPath(vGuid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(vPath);
            if (prefab != null && prefab.GetComponent<DrivableVehicle>() != null)
            {
                if (prefab.GetComponent<VehicleAudioController>() == null)
                {
                    prefab.AddComponent<VehicleAudioController>();
                    EditorUtility.SetDirty(prefab);
                    Debug.Log($"<color=#32FF64>[AudioSetupHelper] Added VehicleAudioController to prefab: {prefab.name}</color>");
                }
            }
        }

        // 5. Ensure VehicleTailgate clips are assigned if missing
        VehicleTailgate[] tailgates = Object.FindObjectsByType<VehicleTailgate>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in tailgates)
        {
            if (t != null)
            {
                if (t.openSound == null) t.openSound = manager.vehicleTailgateOpen;
                if (t.closeSound == null) t.closeSound = manager.vehicleTailgateClose;
                EditorUtility.SetDirty(t);
            }
        }

        // 6. Save or update AudioManager prefab in Assets/_Project/Prefabs/
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");
        }
        PrefabUtility.SaveAsPrefabAssetAndConnect(manager.gameObject, "Assets/_Project/Prefabs/AudioManager.prefab", InteractionMode.AutomatedAction);

        Debug.Log($"<color=#32FF64><b>[AudioSetupHelper] Successfully connected {assignedCount} audio clips and updated AudioManager prefab!</b></color>");
    }

    public static bool AssignClip(AudioManager m, string rawName, AudioClip clip)
    {
        if (m == null || clip == null) return false;

        string name = rawName.ToLower().Replace(" ", "_").Replace("-", "_");

        switch (name)
        {
            // --- 🚶 PLAYER ---
            case "player_footstep":
            case "player_step":
            case "footstep":
                m.playerFootstep = clip; return true;

            case "player_jump":
            case "jump":
                m.playerJump = clip; return true;

            case "player_land":
            case "land":
                m.playerLand = clip; return true;

            // --- 📦 CARGO ---
            case "cargo_grab":
            case "box_grab":
                m.cargoGrab = clip; return true;

            case "cargo_drop_light":
            case "cargo_impact_light":
                m.cargoDropLight = clip; return true;

            case "cargo_drop_medium":
            case "cargo_impact_medium":
                m.cargoDropMedium = clip; return true;

            case "cargo_drop_heavy":
            case "cargo_impact_heavy":
                m.cargoDropHeavy = clip; return true;

            case "cargo_throw":
            case "box_throw":
                m.cargoThrow = clip; return true;

            case "cargo_fragile_rattle":
            case "glass_rattle":
                m.cargoFragileRattle = clip; return true;

            case "cargo_fragile_break":
            case "glass_break":
            case "box_break":
                m.cargoFragileBreak = clip; return true;

            case "cargo_explosive_detonation":
            case "explosion":
                m.cargoExplosiveDetonation = clip; return true;

            // --- 🚗 VEHICLE ---
            case "vehicle_engine_idle_loop":
            case "vehicle_engine_idle":
            case "vehicle_engine":
            case "engine_idle":
            case "engine_loop":
                m.vehicleEngineIdleLoop = clip; return true;

            case "vehicle_reverse_beep_loop":
            case "vehicle_reverse_beep":
            case "reverse_beep":
                m.vehicleReverseBeepLoop = clip; return true;

            case "vehicle_brake_squeak":
            case "brake_squeak":
            case "tire_skid":
                m.vehicleBrakeSqueak = clip; return true;

            case "vehicle_door_open":
            case "car_door_open":
                m.vehicleDoorOpen = clip; return true;

            case "vehicle_door_close":
            case "car_door_close":
                m.vehicleDoorClose = clip; return true;

            case "vehicle_tailgate_open":
            case "tailgate_open":
            case "trunk_open":
                m.vehicleTailgateOpen = clip; return true;

            case "vehicle_tailgate_close":
            case "tailgate_close":
            case "trunk_close":
                m.vehicleTailgateClose = clip; return true;

            case "vehicle_crash_light":
            case "car_crash_light":
                m.vehicleCrashLight = clip; return true;

            case "vehicle_crash_heavy":
            case "car_crash_heavy":
                m.vehicleCrashHeavy = clip; return true;

            // --- 🏢 COMMERCIAL ---
            case "fuel_pumping_loop":
            case "fuel_pumping":
                m.fuelPumpingLoop = clip; return true;

            case "fuel_pump_finish_beep":
            case "fuel_pump_finish":
                m.fuelPumpFinishBeep = clip; return true;

            case "garage_repair_wrench":
            case "garage_repair":
            case "wrench":
                m.garageRepairWrench = clip; return true;

            case "garage_paint_spray":
            case "garage_paint":
            case "paint_spray":
                m.garagePaintSpray = clip; return true;

            case "property_purchase_cash":
            case "property_purchase":
            case "cash_register":
                m.propertyPurchaseCash = clip; return true;

            // --- 🖥️ UI ---
            case "ui_tablet_open":
            case "tablet_open":
                m.uiTabletOpen = clip; return true;

            case "ui_tablet_close":
            case "tablet_close":
                m.uiTabletClose = clip; return true;

            case "ui_tab_switch":
            case "tab_switch":
                m.uiTabSwitch = clip; return true;

            case "ui_button_click":
            case "ui_click":
            case "button_click":
                m.uiButtonClick = clip; return true;

            case "ui_notification_popup":
            case "ui_notification":
            case "notification":
                m.uiNotificationPopup = clip; return true;

            case "ui_error_buzzer":
            case "ui_error":
            case "error_buzzer":
                m.uiErrorBuzzer = clip; return true;

            case "ui_money_add":
            case "money_add":
            case "coin":
                m.uiMoneyAdd = clip; return true;

            case "ui_money_subtract":
            case "money_subtract":
                m.uiMoneySubtract = clip; return true;

            case "ui_level_up":
            case "level_up":
            case "fanfare":
                m.uiLevelUp = clip; return true;

            // --- 🌲 AMBIENCE ---
            case "ambient_river_stream_loop":
            case "ambient_river_stream":
            case "river_stream":
                m.ambientRiverStreamLoop = clip; return true;

            case "ambient_day_valley_loop":
            case "ambient_day_valley":
            case "valley_birds_wind":
                m.ambientDayValleyLoop = clip; return true;

            case "ai_traffic_horn":
            case "traffic_horn":
            case "car_horn":
                m.aiTrafficHorn = clip; return true;
        }

        return false;
    }
}
#endif
