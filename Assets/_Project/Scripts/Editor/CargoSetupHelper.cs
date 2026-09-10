#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CargoSetupHelper
{
    private const string PREFAB_DIR = "Assets/_Project/Prefabs/Cargo";
    private const string MAT_DIR = "Assets/_Project/Materials/Cargo";

    [MenuItem("Tools/Delivery Game/Create Cargo Prefabs & Explosion VFX", false, 44)]
    public static void CreateCargoPrefabsAndVFX()
    {
        EnsureDirectories();

        // 1. Create Materials
        Material stdMat = GetOrCreateMaterial("Mat_Cargo_Standard", new Color(0.72f, 0.54f, 0.35f));
        Material fragileMat = GetOrCreateMaterial("Mat_Cargo_Fragile", new Color(0.85f, 0.42f, 0.28f));
        Material expressMat = GetOrCreateMaterial("Mat_Cargo_Express", new Color(0.25f, 0.55f, 0.85f));
        Material explosiveMat = GetOrCreateMaterial("Mat_Cargo_Explosive", new Color(0.92f, 0.35f, 0.12f));

        // 2. Create Explosion Particle VFX Prefab
        GameObject explosionPrefab = CreateExplosionParticlePrefab();

        // 3. Create Package Prefabs for each cargo type
        GameObject stdPrefab = CreateCargoPackagePrefab("Cargo_Package_Standard", CargoType.Standard, stdMat, explosionPrefab);
        GameObject fragilePrefab = CreateCargoPackagePrefab("Cargo_Package_Fragile", CargoType.Fragile, fragileMat, explosionPrefab);
        GameObject expressPrefab = CreateCargoPackagePrefab("Cargo_Package_Express", CargoType.Express, expressMat, explosionPrefab);
        GameObject explosivePrefab = CreateCargoPackagePrefab("Cargo_Package_Explosive", CargoType.Explosive, explosiveMat, explosionPrefab);

        // 4. Auto-configure BranchManager in the active scene if present
        BranchManager[] branchManagers = Object.FindObjectsByType<BranchManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var bm in branchManagers)
        {
            if (bm == null) continue;

            bm.cargoPackagePrefab = stdPrefab;
            bm.explosionVfxPrefab = explosionPrefab;

            if (!bm.packagePrefabs.Contains(stdPrefab)) bm.packagePrefabs.Add(stdPrefab);
            if (!bm.fragilePackagePrefabs.Contains(fragilePrefab)) bm.fragilePackagePrefabs.Add(fragilePrefab);
            if (!bm.expressPackagePrefabs.Contains(expressPrefab)) bm.expressPackagePrefabs.Add(expressPrefab);
            if (!bm.explosivePackagePrefabs.Contains(explosivePrefab)) bm.explosivePackagePrefabs.Add(explosivePrefab);

            if (!bm.cardboardMaterials.Contains(stdMat)) bm.cardboardMaterials.Add(stdMat);
            if (!bm.fragileMaterials.Contains(fragileMat)) bm.fragileMaterials.Add(fragileMat);
            if (!bm.expressMaterials.Contains(expressMat)) bm.expressMaterials.Add(expressMat);
            if (!bm.explosiveMaterials.Contains(explosiveMat)) bm.explosiveMaterials.Add(explosiveMat);

            EditorUtility.SetDirty(bm.gameObject);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (branchManagers.Length > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        Debug.Log("<color=#32FF64><b>[CargoSetupHelper]</b> Successfully created Cargo Prefabs, Explosion VFX, and linked to BranchManager!</color>");
        EditorUtility.DisplayDialog("Cargo & Explosion Setup", "Successfully created and configured:\n\n1. Explosion VFX Prefab (VFX_Explosion)\n2. Standard, Fragile, Express & Explosive Cargo Prefabs\n3. Linked all prefabs & materials to BranchManager in the scene!", "OK");
    }

    private static void EnsureDirectories()
    {
        if (!Directory.Exists(PREFAB_DIR)) Directory.CreateDirectory(PREFAB_DIR);
        if (!Directory.Exists(MAT_DIR)) Directory.CreateDirectory(MAT_DIR);
        AssetDatabase.Refresh();
    }

    private static Material GetOrCreateMaterial(string name, Color baseColor)
    {
        string path = $"{MAT_DIR}/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            mat = new Material(shader);
            mat.color = baseColor;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.15f);

            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }

    private static GameObject CreateExplosionParticlePrefab()
    {
        string path = "Assets/_Project/Prefabs/VFX_Explosion.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        GameObject root = new GameObject("VFX_Explosion");

        // 1. Main Fire Explosion Particles
        ParticleSystem ps = root.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 1.0f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.75f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 14f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 2.2f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.45f, 0.05f), new Color(1f, 0.15f, 0.02f));
        main.gravityModifier = -0.2f;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 35) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.6f;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.3f);
        sizeCurve.AddKey(0.4f, 1.0f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.8f, 0.2f), 0f), new GradientColorKey(new Color(0.9f, 0.2f, 0.05f), 0.5f), new GradientColorKey(new Color(0.2f, 0.2f, 0.2f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.85f, 0.6f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

        // 2. Light Flash
        GameObject lightObj = new GameObject("LightFlash");
        lightObj.transform.SetParent(root.transform, false);
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.5f, 0.1f);
        light.range = 16f;
        light.intensity = 10f;

        // 3. Shockwave expanding mesh
        GameObject shockwave = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shockwave.name = "ShockwaveMesh";
        shockwave.transform.SetParent(root.transform, false);
        Object.DestroyImmediate(shockwave.GetComponent<Collider>());
        ExplosionShockwaveAnim anim = shockwave.AddComponent<ExplosionShockwaveAnim>();
        anim.maxRadius = 6.5f;
        anim.duration = 0.55f;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        return prefab;
    }

    private static GameObject CreateCargoPackagePrefab(string prefabName, CargoType cargoType, Material mat, GameObject explosionVfx)
    {
        string path = $"{PREFAB_DIR}/{prefabName}.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = prefabName;
        cube.transform.localScale = new Vector3(0.55f, 0.42f, 0.45f);

        MeshRenderer mr = cube.GetComponent<MeshRenderer>();
        if (mr != null && mat != null)
        {
            mr.material = mat;
        }

        PhysicalCargoPackage pkg = cube.AddComponent<PhysicalCargoPackage>();
        pkg.cargoType = cargoType;
        pkg.isCustomPrefab = true;
        pkg.explosionVfxPrefab = explosionVfx;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(cube, path);
        Object.DestroyImmediate(cube);

        return prefab;
    }
}
#endif
