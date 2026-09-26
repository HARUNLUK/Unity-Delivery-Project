#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Creates the weapon carriable item (placeholder pistol built from primitives until a real model is imported)
/// and helps placing it in the scene: directly, or as a spawn point.
/// Tools > Delivery Game > Items
/// </summary>
public static class CarriableItemTools
{
    private const string PrefabDir = "Assets/_Project/Prefabs/Items";
    private const string MaterialDir = "Assets/_Project/Materials/Items";
    public const string WeaponPrefabPath = PrefabDir + "/Item_Weapon.prefab";
    public const string GasCanisterPrefabPath = "Assets/_AssetPacks/ExplosivesPackage/Prefabs/Gas_Can.prefab";
    private const string SourceGasCanPrefabPath = "Assets/_AssetPacks/ExplosivesPackage/Prefabs/Gas_Can.prefab";

    [MenuItem("Tools/Delivery Game/Items/1. Create Weapon Item Prefab", false, 200)]
    public static void CreateWeaponPrefabMenu()
    {
        GameObject prefab = EnsureWeaponPrefab(true);
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log("<color=#2F9B6F>[Items]</color> Silah eşyası hazır: " + WeaponPrefabPath);
    }

    [MenuItem("Tools/Delivery Game/Items/4. Configure Gas Can Prefab", false, 203)]
    public static void CreateGasCanisterMenu()
    {
        GameObject prefab = EnsureGasCanisterPrefab(true);
        if (prefab != null)
        {
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            Debug.Log("<color=#2F9B6F>[Items]</color> Gas_Can prefabı hazırlandı: " + GasCanisterPrefabPath);
        }
    }

    [MenuItem("Tools/Delivery Game/Items/5. Setup Fuel Station Pump With Canister", false, 204)]
    public static void SetupFuelStationPumpWithCanister()
    {
        GameObject canPrefab = EnsureGasCanisterPrefab(false);
        string pumpPrefabPath = "Assets/_Project/Prefabs/Plots/Fuel_Station_Pump.prefab";

        GameObject pumpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(pumpPrefabPath);
        if (pumpPrefab == null)
        {
            return;
        }

        try
        {
            GameObject pumpRoot = PrefabUtility.LoadPrefabContents(pumpPrefabPath);
            FuelStationPump pumpScript = pumpRoot.GetComponent<FuelStationPump>();
            if (pumpScript == null) pumpScript = pumpRoot.AddComponent<FuelStationPump>();

            pumpScript.canisterPrice = 100;
            pumpScript.canisterFuelAmount = 10f;
            if (canPrefab != null) pumpScript.canisterPrefab = canPrefab;

            Transform stand = pumpRoot.transform.Find("Pump_Dispenser_Stand");
            Transform spawnPoint = pumpRoot.transform.Find("CanisterSpawnPoint");
            if (spawnPoint == null)
            {
                GameObject spObj = new GameObject("CanisterSpawnPoint");
                spObj.transform.SetParent(pumpRoot.transform, false);
                if (stand != null)
                {
                    spObj.transform.localPosition = stand.localPosition + new Vector3(0f, 0.4f, 1.0f);
                }
                else
                {
                    spObj.transform.localPosition = new Vector3(2.5f, 0.5f, 0f);
                }
                spawnPoint = spObj.transform;
            }
            pumpScript.canisterSpawnTransform = spawnPoint;

            PrefabUtility.SaveAsPrefabAsset(pumpRoot, pumpPrefabPath);
            PrefabUtility.UnloadPrefabContents(pumpRoot);

            Debug.Log("<color=#2F9B6F>[Items]</color> Fuel_Station_Pump.prefab Gas_Can ile başarıyla yapılandırıldı!");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[Items] SetupFuelStationPumpWithCanister notice: " + ex.Message);
        }
    }

    public static GameObject EnsureGasCanisterPrefab(bool rebuild)
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(GasCanisterPrefabPath);
        if (existing == null)
        {
            Debug.LogWarning("[Items] Gas_Can prefab not found at: " + GasCanisterPrefabPath);
            return null;
        }

        // Ensure Rigidbody, CarriableItem, and FuelCanisterItem directly on Gas_Can.prefab
        // without touching user's scale, box collider, material, or adding children!
        try
        {
            GameObject root = PrefabUtility.LoadPrefabContents(GasCanisterPrefabPath);
            bool modified = false;

            Rigidbody rb = root.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = root.AddComponent<Rigidbody>();
                rb.mass = 4.5f;
                rb.linearDamping = 0.2f;
                rb.angularDamping = 0.5f;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                modified = true;
            }

            CarriableItem carriable = root.GetComponent<CarriableItem>();
            if (carriable == null)
            {
                carriable = root.AddComponent<CarriableItem>();
                carriable.itemId = "fuel_canister";
                carriable.nameKey = "item_fuel_canister_name";
                carriable.fallbackName = "Benzin Bidonu";
                carriable.useCustomHoldRotation = true;
                carriable.customHoldRotation = new Vector3(-90f, 90f, 0f);
                modified = true;
            }
            else
            {
                carriable.itemId = "fuel_canister";
                carriable.useCustomHoldRotation = true;
                carriable.customHoldRotation = new Vector3(-90f, 90f, 0f);
                modified = true;
            }

            FuelCanisterItem canister = root.GetComponent<FuelCanisterItem>();
            if (canister == null)
            {
                canister = root.AddComponent<FuelCanisterItem>();
                canister.fuelAmount = 10f;
                canister.refuelDistance = 5.0f;
                canister.holdRotationOffset = new Vector3(-90f, 90f, 0f);
                modified = true;
            }
            else
            {
                canister.holdRotationOffset = new Vector3(-90f, 90f, 0f);
                modified = true;
            }

            if (modified)
            {
                PrefabUtility.SaveAsPrefabAsset(root, GasCanisterPrefabPath);
            }
            PrefabUtility.UnloadPrefabContents(root);

            // Also keep Resources copy updated with the exact user prefab
            EnsureFolder("Assets/Resources/Prefabs");
            AssetDatabase.CopyAsset(GasCanisterPrefabPath, "Assets/Resources/Prefabs/Gas_Can.prefab");
            AssetDatabase.CopyAsset(GasCanisterPrefabPath, "Assets/Resources/Prefabs/Gas_Canister.prefab");
            AssetDatabase.Refresh();

            Debug.Log("<color=#2F9B6F>[Items]</color> Gas_Can prefabı doğrulandı ve güncellendi: " + GasCanisterPrefabPath);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[Items] EnsureGasCanisterPrefab error: " + ex.Message);
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(GasCanisterPrefabPath);
    }

    [MenuItem("Tools/Delivery Game/Items/2. Place Weapon At Scene View", false, 201)]
    public static void PlaceWeaponMenu()
    {
        GameObject prefab = EnsureWeaponPrefab(false);
        Vector3 pos = SceneViewGroundPoint();

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(instance, "Place Weapon Item");
        instance.transform.position = pos + Vector3.up * 0.2f;
        instance.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        Selection.activeGameObject = instance;
        EditorSceneManager.MarkSceneDirty(instance.scene);
    }

    [MenuItem("Tools/Delivery Game/Items/3. Create Weapon Spawn Point At Scene View", false, 202)]
    public static void CreateSpawnPointMenu()
    {
        GameObject prefab = EnsureWeaponPrefab(false);
        Vector3 pos = SceneViewGroundPoint();

        GameObject go = new GameObject("WeaponSpawnPoint");
        Undo.RegisterCreatedObjectUndo(go, "Create Weapon Spawn Point");
        go.transform.position = pos;
        CarriableItemSpawnPoint sp = go.AddComponent<CarriableItemSpawnPoint>();
        sp.itemPrefabs = new[] { prefab };
        sp.spawnChance = 1f;
        Selection.activeGameObject = go;
        EditorSceneManager.MarkSceneDirty(go.scene);
    }

    private static Vector3 SceneViewGroundPoint()
    {
        SceneView view = SceneView.lastActiveSceneView;
        if (view == null) return Vector3.zero;

        Transform cam = view.camera.transform;
        if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, 200f)) return hit.point;
        return view.pivot;
    }

    // =====================================================================
    // PREFAB
    // =====================================================================

    public static GameObject EnsureWeaponPrefab(bool rebuild)
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(WeaponPrefabPath);
        if (existing != null && !rebuild) return existing;

        EnsureFolder(PrefabDir);
        EnsureFolder(MaterialDir);

        Material metal = EnsureMaterial("Item_Weapon_Metal", new Color(0.16f, 0.17f, 0.20f), 0.85f, 0.65f);
        Material grip = EnsureMaterial("Item_Weapon_Grip", new Color(0.30f, 0.20f, 0.13f), 0.0f, 0.25f);

        GameObject root = new GameObject("Item_Weapon");
        const float s = 1.5f; // a bit larger than a real pistol so it is easy to spot and grab

        AddPart(root.transform, PrimitiveType.Cube, "Slide", new Vector3(0.00f, 0.060f, 0f), new Vector3(0.230f, 0.046f, 0.032f), 0f, metal, s);
        AddPart(root.transform, PrimitiveType.Cylinder, "Barrel", new Vector3(0.130f, 0.060f, 0f), new Vector3(0.026f, 0.030f, 0.026f), 90f, metal, s);
        AddPart(root.transform, PrimitiveType.Cube, "Frame", new Vector3(-0.005f, 0.030f, 0f), new Vector3(0.170f, 0.030f, 0.028f), 0f, metal, s);
        AddPart(root.transform, PrimitiveType.Cube, "Grip", new Vector3(-0.075f, -0.020f, 0f), new Vector3(0.048f, 0.115f, 0.030f), -12f, grip, s);
        AddPart(root.transform, PrimitiveType.Cube, "TriggerGuard", new Vector3(0.020f, 0.008f, 0f), new Vector3(0.050f, 0.008f, 0.020f), 0f, metal, s);

        // One simple box collider for the whole item (cheap and stable for physics).
        BoxCollider col = root.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, 0.015f, 0f) * s;
        col.size = new Vector3(0.245f, 0.150f, 0.040f) * s;

        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.mass = 1.1f;
        rb.linearDamping = 0.1f;
        rb.angularDamping = 0.5f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        CarriableItem item = root.AddComponent<CarriableItem>();
        item.itemId = "weapon";
        item.nameKey = "item_weapon_name";
        item.fallbackName = "Silah";

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, WeaponPrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        return prefab;
    }

    private static void AddPart(Transform parent, PrimitiveType type, string name, Vector3 pos, Vector3 size, float zRot, Material mat, float scale)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        Object.DestroyImmediate(part.GetComponent<Collider>()); // the root box collider covers the whole item
        part.transform.SetParent(parent, false);
        part.transform.localPosition = pos * scale;
        part.transform.localRotation = Quaternion.Euler(0f, 0f, zRot);
        // Cylinder primitives are 2 units tall along Y; after the 90° roll their length runs along X.
        part.transform.localScale = size * scale;
        part.GetComponent<Renderer>().sharedMaterial = mat;
    }

    private static Material EnsureMaterial(string name, Color color, float metallic, float smoothness)
    {
        string path = $"{MaterialDir}/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.color = color;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
#endif
