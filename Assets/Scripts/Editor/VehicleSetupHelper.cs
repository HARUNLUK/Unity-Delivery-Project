#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class VehicleSetupHelper
{
    private const string PREFAB_DIR = "Assets/Prefabs";

    [MenuItem("Tools/Delivery Game/Setup Selected Object as Drivable Vehicle", false, 40)]
    public static void SetupSelectedAsDrivable()
    {
        GameObject target = Selection.activeGameObject;
        if (target == null)
        {
            // If no vehicle selected, try to find one named Pickup or Car in scene
            DrivableVehicle existingDrivable = Object.FindAnyObjectByType<DrivableVehicle>();
            if (existingDrivable != null) target = existingDrivable.gameObject;
            else
            {
                CarControl cc = Object.FindAnyObjectByType<CarControl>();
                if (cc != null) target = cc.gameObject;
            }
        }

        if (target == null)
        {
            EditorUtility.DisplayDialog("Select Vehicle", "Please select a vehicle GameObject in the Scene or Hierarchy first!", "OK");
            return;
        }

        if (!Directory.Exists(PREFAB_DIR)) Directory.CreateDirectory(PREFAB_DIR);

        // 1. Remove all conflicting old pack scripts from target and scene
        CarControl[] allCarControls = Object.FindObjectsByType<CarControl>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var oldCC in allCarControls) Object.DestroyImmediate(oldCC);

        CameraControl[] allCamControls = Object.FindObjectsByType<CameraControl>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var oldCam in allCamControls) Object.DestroyImmediate(oldCam);

        SmoothFollowCamera[] allSfc = Object.FindObjectsByType<SmoothFollowCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var sfc in allSfc) sfc.gameObject.SetActive(false);

        // 2. Ensure Rigidbody with stable center of mass
        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb == null) rb = target.AddComponent<Rigidbody>();
        rb.mass = 1200f;
        rb.linearDamping = 0.05f;
        rb.angularDamping = 2.0f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // 3. Ensure CarController and bind wheels automatically
        CarController car = target.GetComponent<CarController>();
        if (car == null) car = target.AddComponent<CarController>();
        car.motorForce = 18000f;
        car.reverseForce = 12000f;
        car.footBrakeForce = 60000f;
        car.handBrakeForce = 90000f;
        car.maxSteerAngle = 32f;
        car.centerOfMassOffset = new Vector3(0f, -0.6f, 0f);

        // Auto-detect Wheel Colliders and Meshes
        WheelCollider[] allWheels = target.GetComponentsInChildren<WheelCollider>();
        if (allWheels != null && allWheels.Length >= 4)
        {
            System.Array.Sort(allWheels, (a, b) => b.transform.position.z.CompareTo(a.transform.position.z));
            
            WheelCollider fl = allWheels[0].transform.position.x < allWheels[1].transform.position.x ? allWheels[0] : allWheels[1];
            WheelCollider fr = allWheels[0].transform.position.x < allWheels[1].transform.position.x ? allWheels[1] : allWheels[0];
            WheelCollider rl = allWheels[2].transform.position.x < allWheels[3].transform.position.x ? allWheels[2] : allWheels[3];
            WheelCollider rr = allWheels[2].transform.position.x < allWheels[3].transform.position.x ? allWheels[3] : allWheels[2];

            car.frontLeftCollider = fl;
            car.frontRightCollider = fr;
            car.rearLeftCollider = rl;
            car.rearRightCollider = rr;

            foreach (var wc in allWheels)
            {
                JointSpring spring = wc.suspensionSpring;
                spring.spring = 35000f;
                spring.damper = 4500f;
                spring.targetPosition = 0.5f;
                wc.suspensionSpring = spring;
                wc.suspensionDistance = 0.22f;
                wc.mass = 20f;
                wc.wheelDampingRate = 0.25f;

                WheelFrictionCurve fwd = wc.forwardFriction;
                fwd.stiffness = 1.5f;
                wc.forwardFriction = fwd;

                WheelFrictionCurve side = wc.sidewaysFriction;
                side.stiffness = 1.6f;
                wc.sidewaysFriction = side;
            }
        }

        MeshRenderer[] renderers = target.GetComponentsInChildren<MeshRenderer>();
        foreach (var mr in renderers)
        {
            string n = mr.gameObject.name.ToLower();
            if (n.Contains("wheel") || n.Contains("fl_") || n.Contains("fr_") || n.Contains("rl_") || n.Contains("rr_"))
            {
                if (n.Contains("fl") || (n.Contains("front") && n.Contains("l"))) car.frontLeftMesh = mr.transform;
                else if (n.Contains("fr") || (n.Contains("front") && n.Contains("r"))) car.frontRightMesh = mr.transform;
                else if (n.Contains("rl") || (n.Contains("rear") && n.Contains("l"))) car.rearLeftMesh = mr.transform;
                else if (n.Contains("rr") || (n.Contains("rear") && n.Contains("r"))) car.rearRightMesh = mr.transform;
            }
        }

        // Auto-detect Steering Wheel
        Transform stWheel = target.transform.Find("SteeringWheel");
        if (stWheel == null)
        {
            Transform[] allChildren = target.GetComponentsInChildren<Transform>();
            foreach (var child in allChildren)
            {
                if (child.name.ToLower().Contains("steering"))
                {
                    stWheel = child;
                    break;
                }
            }
        }
        if (stWheel != null)
        {
            car.steeringWheel = stWheel;
        }

        // 4. Ensure DrivableVehicle
        DrivableVehicle drivable = target.GetComponent<DrivableVehicle>();
        if (drivable == null) drivable = target.AddComponent<DrivableVehicle>();
        drivable.carController = car;
        drivable.vehicleName = target.name.Replace("(Clone)", "").Trim();
        drivable.isPlayerInside = false;
        car.enabled = false; // Strictly OFF until player enters!

        // 5. Setup Anchors
        Transform seat = target.transform.Find("DriverSeatPoint");
        if (seat == null)
        {
            GameObject seatObj = new GameObject("DriverSeatPoint");
            seatObj.transform.SetParent(target.transform);
            seatObj.transform.localPosition = new Vector3(-0.45f, 1.35f, 0.15f);
            seatObj.transform.localRotation = Quaternion.identity;
            seat = seatObj.transform;
        }
        drivable.driverSeatPoint = seat;

        Transform exit = target.transform.Find("ExitPoint");
        if (exit == null)
        {
            GameObject exitObj = new GameObject("ExitPoint");
            exitObj.transform.SetParent(target.transform);
            exitObj.transform.localPosition = new Vector3(-1.8f, 0.2f, 0.2f);
            exitObj.transform.localRotation = Quaternion.identity;
            exit = exitObj.transform;
        }
        drivable.exitPoint = exit;

        // 6. Setup Rear Tailgate
        VehicleTailgate existingTg = target.GetComponentInChildren<VehicleTailgate>();
        if (existingTg == null)
        {
            GameObject tgObj = new GameObject("RearTailgate");
            tgObj.transform.SetParent(target.transform);
            tgObj.transform.localPosition = new Vector3(0f, 0.8f, -2.1f);
            tgObj.transform.localRotation = Quaternion.identity;

            BoxCollider tgCol = tgObj.AddComponent<BoxCollider>();
            tgCol.size = new Vector3(1.6f, 0.6f, 0.2f);

            existingTg = tgObj.AddComponent<VehicleTailgate>();
        }
        drivable.rearTailgate = existingTg;

        // 7. Save / Connect as Project Prefab
        string prefabPath = $"{PREFAB_DIR}/Drivable_{target.name.Replace("(Clone)", "").Trim()}.prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(target, prefabPath, InteractionMode.UserAction);

        Undo.RegisterCreatedObjectUndo(target, "Setup Drivable Vehicle");
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[VehicleSetupHelper] Configured '{target.name}' as Drivable Vehicle and saved to '{prefabPath}'!");
    }

    [MenuItem("Tools/Delivery Game/Spawn FPS Player", false, 50)]
    public static void CreateFPSPlayer()
    {
        if (!Directory.Exists(PREFAB_DIR)) Directory.CreateDirectory(PREFAB_DIR);
        string playerPrefabPath = $"{PREFAB_DIR}/FPS_Player.prefab";

        // Find and clean up any loose external scene cameras so only FPS Player has the camera
        Camera[] sceneCameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Camera targetCamera = null;

        FPSPlayerController existingPlayer = Object.FindAnyObjectByType<FPSPlayerController>();
        if (existingPlayer != null)
        {
            Selection.activeGameObject = existingPlayer.gameObject;
            Debug.Log($"[VehicleSetupHelper] Selected existing '{existingPlayer.gameObject.name}'.");
            return;
        }

        // Determine spawn position (near vehicle if in scene, or scene view pivot)
        Vector3 spawnPos = Vector3.zero;
        DrivableVehicle vehicle = Object.FindAnyObjectByType<DrivableVehicle>();
        if (vehicle != null)
        {
            spawnPos = vehicle.transform.position - (vehicle.transform.right * 3.5f) + (Vector3.up * 0.5f);
        }
        else if (SceneView.lastActiveSceneView != null)
        {
            spawnPos = SceneView.lastActiveSceneView.pivot + (Vector3.up * 0.5f);
        }

        GameObject playerObj = new GameObject("FPS_Player");
        playerObj.transform.position = spawnPos;

        CharacterController cc = playerObj.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.35f;
        cc.center = new Vector3(0f, 0.9f, 0f);

        GameObject holderObj = new GameObject("CameraHolder");
        holderObj.transform.SetParent(playerObj.transform);
        holderObj.transform.localPosition = new Vector3(0f, 1.7f, 0f);
        holderObj.transform.localRotation = Quaternion.identity;

        // Clean up scene camera and place it into FPS Player
        foreach (var cam in sceneCameras)
        {
            if (cam != null && cam.gameObject.name == "Main Camera")
            {
                // Remove pack camera script
                CameraControl ccScript = cam.GetComponent<CameraControl>();
                if (ccScript != null) Object.DestroyImmediate(ccScript);

                cam.transform.SetParent(holderObj.transform);
                cam.transform.localPosition = Vector3.zero;
                cam.transform.localRotation = Quaternion.identity;
                cam.tag = "MainCamera";
                cam.enabled = true;
                targetCamera = cam;
                break;
            }
        }

        if (targetCamera == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.transform.SetParent(holderObj.transform);
            camObj.transform.localPosition = Vector3.zero;
            camObj.transform.localRotation = Quaternion.identity;
            targetCamera = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
            camObj.tag = "MainCamera";
        }

        FPSPlayerController fps = playerObj.AddComponent<FPSPlayerController>();
        fps.playerCamera = targetCamera;
        fps.cameraHolder = holderObj.transform;

        // Ensure Interaction Prompt Canvas
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("HUD_Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        if (canvas.GetComponentInChildren<InteractionPromptHUD>() == null)
        {
            canvas.gameObject.AddComponent<InteractionPromptHUD>();
        }

        PrefabUtility.SaveAsPrefabAssetAndConnect(playerObj, playerPrefabPath, InteractionMode.UserAction);

        Selection.activeGameObject = playerObj;
        Undo.RegisterCreatedObjectUndo(playerObj, "Created FPS Player Prefab");
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[VehicleSetupHelper] Spawned FPS Player, transferred Main Camera, and saved '{playerPrefabPath}'!");
    }
}
#endif
