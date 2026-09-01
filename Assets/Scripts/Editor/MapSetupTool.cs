#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class MapSetupTool
{
    [MenuItem("Tools/Delivery Game/Setup Map Architecture & Systems", false, 1)]
    public static void SetupSceneArchitecture()
    {
        // 1. Root Folders
        GameObject envRoot = GameObject.Find("_ENVIRONMENT");
        if (envRoot == null) envRoot = new GameObject("_ENVIRONMENT");

        GameObject gameplayRoot = GameObject.Find("_GAMEPLAY");
        if (gameplayRoot == null) gameplayRoot = new GameObject("_GAMEPLAY");

        GameObject pointsPool = GameObject.Find("Delivery_Points_Pool");
        if (pointsPool == null)
        {
            pointsPool = new GameObject("Delivery_Points_Pool");
            pointsPool.transform.SetParent(gameplayRoot.transform);
        }

        // 2. Directional Light (Sun)
        Light sun = Object.FindAnyObjectByType<Light>();
        if (sun == null || sun.type != LightType.Directional)
        {
            GameObject sunObj = new GameObject("Sun_DirectionalLight");
            sunObj.transform.SetParent(envRoot.transform);
            sun = sunObj.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.95f, 0.85f);
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            sunObj.transform.rotation = Quaternion.Euler(35f, -30f, 0f);
        }

        // 3. Delivery Van
        CarController car = Object.FindAnyObjectByType<CarController>();
        if (car == null)
        {
            GameObject vanPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/DeliveryVan.prefab");
            if (vanPrefab != null)
            {
                GameObject carObj = (GameObject)PrefabUtility.InstantiatePrefab(vanPrefab);
                carObj.transform.SetParent(gameplayRoot.transform);
                carObj.transform.position = new Vector3(0f, 0.5f, 0f);
                carObj.name = "DeliveryVan";
                car = carObj.GetComponent<CarController>();
            }
        }

        // 4. Main Camera with SmoothFollowCamera
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            mainCam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }

        SmoothFollowCamera followScript = mainCam.GetComponent<SmoothFollowCamera>();
        if (followScript == null) followScript = mainCam.gameObject.AddComponent<SmoothFollowCamera>();
        if (car != null) followScript.target = car.transform;

        // 5. DeliveryManager
        GameObject delivManager = GameObject.Find("DeliveryManager");
        if (delivManager == null)
        {
            delivManager = new GameObject("DeliveryManager");
            delivManager.transform.SetParent(gameplayRoot.transform);
        }

        VanInventory inv = delivManager.GetComponent<VanInventory>();
        if (inv == null) inv = delivManager.AddComponent<VanInventory>();

        DayTimeManager timeMgr = delivManager.GetComponent<DayTimeManager>();
        if (timeMgr == null) timeMgr = delivManager.AddComponent<DayTimeManager>();
        timeMgr.directionalSun = sun;
        timeMgr.realTimeDurationInMinutes = 10f;

        PlayerEconomyManager eco = delivManager.GetComponent<PlayerEconomyManager>();
        if (eco == null) eco = delivManager.AddComponent<PlayerEconomyManager>();

        // 6. Build UI
        DeliveryUIBuilder.BuildCleanDeliveryUI();

        EditorUtility.SetDirty(delivManager);
        Debug.Log("[MapSetupTool] Scene architecture & delivery systems successfully set up!");
    }

    [MenuItem("Tools/Delivery Game/Add Delivery Point at Selected Object", false, 2)]
    public static void AddDeliveryPointAtSelected()
    {
        Transform selected = Selection.activeTransform;
        Vector3 spawnPos = selected != null ? selected.position + (selected.forward * 3f) + (Vector3.up * 0.1f) : Vector3.zero;

        GameObject pointsPool = GameObject.Find("Delivery_Points_Pool");
        if (pointsPool == null)
        {
            GameObject gameplayRoot = GameObject.Find("_GAMEPLAY");
            if (gameplayRoot == null) gameplayRoot = new GameObject("_GAMEPLAY");
            pointsPool = new GameObject("Delivery_Points_Pool");
            pointsPool.transform.SetParent(gameplayRoot.transform);
        }

        int count = pointsPool.transform.childCount + 1;
        string defaultId = $"DP-{count:D3}";

        GameObject pointObj = new GameObject($"Point_{defaultId}");
        pointObj.transform.SetParent(pointsPool.transform);
        pointObj.transform.position = spawnPos;

        SphereCollider col = pointObj.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 4.0f;

        DeliveryPoint dp = pointObj.AddComponent<DeliveryPoint>();
        dp.pointId = defaultId;
        dp.addressName = $"Street Name No: {count}";
        dp.addressDescription = "House description clue for player (e.g. Red roof villa with white picket fences).";

        // Visual Marker Ring
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = "VisualMarker";
        marker.transform.SetParent(pointObj.transform);
        marker.transform.localPosition = Vector3.zero;
        marker.transform.localScale = new Vector3(4.5f, 0.04f, 4.5f);
        Object.DestroyImmediate(marker.GetComponent<Collider>());

        Renderer ren = marker.GetComponent<Renderer>();
        if (ren != null)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Standard");
            Material m = new Material(s);
            m.color = new Color(0.2f, 0.85f, 0.4f, 0.6f);
            ren.material = m;
        }

        dp.visualMarker = marker;

        Selection.activeGameObject = pointObj;
        Undo.RegisterCreatedObjectUndo(pointObj, "Created Delivery Point");
        Debug.Log($"[MapSetupTool] Delivery Point created: {defaultId}. You can now edit its address and clue in the Inspector.");
    }
}
#endif
