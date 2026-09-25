#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Unity Car Project - Otomatik Çarpışma (Collision / Collider) Kurulum Aracı.
/// Haritadaki binalar, dükkanlar, parseller (Plots) ve Terrain üzerindeki ağaçlara
/// tek tıkla doğru collider (MeshCollider / CapsuleCollider) bileşenlerini ekler ve yapılandırır.
/// </summary>
public static class ColliderSetupHelper
{
    // =========================================================================
    // 1. SEÇİLİ NESNELERE (DÜKKANLAR / BİNALAR) MESH COLLIDER EKLEME
    // =========================================================================
    [MenuItem("Tools/Delivery Game/Colliders/1. Secili Nesnelere ve Cocuklarina MeshCollider Ekle", false, 100)]
    public static void AddMeshCollidersToSelected()
    {
        GameObject[] selection = Selection.gameObjects;
        if (selection == null || selection.Length == 0)
        {
            EditorUtility.DisplayDialog("Uyarı", "Lütfen Hierarchy veya Project panelinden en az bir nesne (Dükkan, Bina veya Klasör) seçin!", "Tamam");
            return;
        }

        int addedCount = 0;
        foreach (GameObject root in selection)
        {
            addedCount += AddMeshCollidersRecursively(root);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"<color=#32FF64>[ColliderSetupHelper] Seçilen objelerde toplam {addedCount} adet eksik MeshCollider başarıyla eklendi.</color>");
        EditorUtility.DisplayDialog("İşlem Başarılı", $"Seçili nesnelere toplam {addedCount} adet MeshCollider eklendi!", "Harika");
    }

    // =========================================================================
    // 2. DÜKKAN / İŞLETME PREFABLARI (PLOTS) ÇARPIŞMALARINI DÜZELT
    // =========================================================================
    [MenuItem("Tools/Delivery Game/Colliders/2. Tum Ticari Dukkan Prefablarinin Carpisma (Collider) Ayarlarini Yap", false, 101)]
    public static void FixAllShopPrefabs()
    {
        string[] shopPrefabGuids = AssetDatabase.FindAssets("t:Prefab", new string[] { "Assets/_Project/Prefabs/Plots", "Assets/_Project/Prefabs/Branch", "Assets/_Project/Prefabs/Modular" });

        int totalFixed = 0;
        int prefabsModified = 0;

        foreach (string guid in shopPrefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            // Prefab içeriğini açıp düzenle
            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

            int addedInPrefab = AddMeshCollidersRecursively(prefabRoot);
            if (addedInPrefab > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                totalFixed += addedInPrefab;
                prefabsModified++;
                Debug.Log($"[ColliderSetupHelper] '{prefab.name}' dükkan/bina prefabına {addedInPrefab} collider eklendi.");
            }

            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=#32FF64>[ColliderSetupHelper] {prefabsModified} dükkan prefabında toplam {totalFixed} MeshCollider başarıyla güncellendi!</color>");
        EditorUtility.DisplayDialog("Dükkanlar Güncellendi", $"{prefabsModified} adet dükkan/işletme prefabına toplam {totalFixed} adet eksik MeshCollider eklendi ve kaydedildi!", "Tamam");
    }

    // =========================================================================
    // 3. TERRAIN AĞAÇLARININ COLLIDERLARINI DÜZELT (CAPSULE COLLIDER)
    // =========================================================================
    [MenuItem("Tools/Delivery Game/Colliders/3. Terrain Agaclarina Otomatik CapsuleCollider Ekle (Agac Carpmalari)", false, 102)]
    public static void FixTerrainTreeColliders()
    {
        Terrain[] terrains = Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (terrains == null || terrains.Length == 0)
        {
            EditorUtility.DisplayDialog("Uyarı", "Sahnede aktif bir Terrain bulunamadı!", "Tamam");
            return;
        }

        int fixedTreePrefabs = 0;
        HashSet<GameObject> processedPrefabs = new HashSet<GameObject>();

        foreach (Terrain terrain in terrains)
        {
            // Terrain Collider kontrolü ve Enable Tree Colliders ayarı
            TerrainCollider terrainCol = terrain.GetComponent<TerrainCollider>();
            if (terrainCol == null)
            {
                terrainCol = terrain.gameObject.AddComponent<TerrainCollider>();
                terrainCol.terrainData = terrain.terrainData;
            }
            terrainCol.enabled = true;
            
            // Unity TerrainData ağaç prototipleri
            TerrainData tData = terrain.terrainData;
            if (tData == null || tData.treePrototypes == null) continue;

            TreePrototype[] prototypes = tData.treePrototypes;
            bool prototypesChanged = false;

            for (int i = 0; i < prototypes.Length; i++)
            {
                GameObject treePrefab = prototypes[i].prefab;
                if (treePrefab == null || processedPrefabs.Contains(treePrefab)) continue;

                processedPrefabs.Add(treePrefab);

                // Tree prefabında Collider var mı kontrol et
                Collider existingCol = treePrefab.GetComponentInChildren<Collider>();
                if (existingCol == null)
                {
                    string pPath = AssetDatabase.GetAssetPath(treePrefab);
                    if (!string.IsNullOrEmpty(pPath))
                    {
                        GameObject root = PrefabUtility.LoadPrefabContents(pPath);
                        
                        // Gövde boyutunu hesapla
                        MeshRenderer mr = root.GetComponentInChildren<MeshRenderer>();
                        Bounds bounds = (mr != null) ? mr.bounds : new Bounds(Vector3.up * 2.5f, new Vector3(1f, 5f, 1f));

                        CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
                        float treeHeight = Mathf.Max(bounds.size.y, 3f);
                        capsule.height = treeHeight;
                        capsule.radius = Mathf.Clamp(bounds.size.x * 0.15f, 0.25f, 0.6f); // Gövde yarıçapı
                        capsule.center = new Vector3(0f, treeHeight * 0.5f, 0f);

                        PrefabUtility.SaveAsPrefabAsset(root, pPath);
                        PrefabUtility.UnloadPrefabContents(root);

                        fixedTreePrefabs++;
                        prototypesChanged = true;
                        Debug.Log($"[ColliderSetupHelper] '{treePrefab.name}' ağacına CapsuleCollider eklendi (Yükseklik: {treeHeight:F1}m).");
                    }
                }
            }

            // Prototipleri yenileyerek Terrain'in çarpışma motorunu tetikle
            if (prototypesChanged)
            {
                tData.treePrototypes = prototypes;
                tData.RefreshPrototypes();
                EditorUtility.SetDirty(tData);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=#32FF64>[ColliderSetupHelper] {fixedTreePrefabs} adet ağaç prefabına CapsuleCollider eklendi ve Terrain güncellendi.</color>");
        EditorUtility.DisplayDialog("Ağaçlar Güncellendi", $"{fixedTreePrefabs} adet ağaç modeline gövde collider'ı eklendi!\n\nArtık araç veya oyuncu Terrain'deki boyanmış ağaçlara çarpacaktır.", "Harika");
    }

    // =========================================================================
    // 4. TÜM SAHNEYİ TEK TIKLA TARA VE DÜZELT (MASTER FIX)
    // =========================================================================
    [MenuItem("Tools/Delivery Game/Colliders/4. TEK TIKLA TUM HARITAYI DUZELT (Binalar + Dukkanlar + Agaclar)", false, 103)]
    public static void FixEntireSceneCollisions()
    {
        int totalAdded = 0;

        // 1. Terrain Ağaçlarını Düzelt
        FixTerrainTreeColliders();

        // 2. Dükkan Prefablarını Düzelt
        FixAllShopPrefabs();

        // 3. Sahnedeki Çevre Kök Objelerini Tara
        string[] searchRoots = new string[] { "_ENVIRONMENT", "_DISTRICTS", "_GAMEPLAY", "Buildings", "Shops", "Plots", "Zone1_Downtown", "Zone2_Suburbs", "Zone3_Mountain_Village" };

        foreach (string rootName in searchRoots)
        {
            GameObject obj = GameObject.Find(rootName);
            if (obj != null)
            {
                totalAdded += AddMeshCollidersRecursively(obj);
            }
        }

        // 4. Sahnedeki tüm PurchasableProperty (Dükkanlar) nesnelerini tara
        var props = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var p in props)
        {
            if (p.GetType().Name == "PurchasableProperty" || p.GetType().Name == "VehicleServiceGarage")
            {
                totalAdded += AddMeshCollidersRecursively(p.gameObject);
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"<color=#32FF64>[ColliderSetupHelper] Master Fix tamamlandı! Sahnede toplam {totalAdded} eksik nesneye çarpışma eklendi.</color>");
        EditorUtility.DisplayDialog("Master Fix Tamamlandı", $"Haritadaki tüm ağaçlar, dükkanlar ve binalar tarandı.\n\nToplam {totalAdded} yeni çarpışma yüzeyi aktif edildi!", "Harika");
    }

    // =========================================================================
    // YARDIMCI RECURSIVE METOT
    // =========================================================================
    private static int AddMeshCollidersRecursively(GameObject target)
    {
        if (target == null) return 0;

        int addedCount = 0;
        MeshRenderer[] renderers = target.GetComponentsInChildren<MeshRenderer>(true);

        foreach (MeshRenderer mr in renderers)
        {
            GameObject go = mr.gameObject;

            // Zaten bir collider var mı kontrol et (BoxCollider, MeshCollider, CapsuleCollider vb.)
            if (go.GetComponent<Collider>() != null) continue;

            // MeshFilter var mı?
            MeshFilter mf = go.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            // UI, Trigger, Particle veya FX objelerini atla
            if (go.name.Contains("Trigger") || go.name.Contains("Anchor") || go.name.Contains("UI") || go.name.Contains("Particle")) continue;

            // MeshCollider ekle
            MeshCollider mc = Undo.AddComponent<MeshCollider>(go);
            mc.sharedMesh = mf.sharedMesh;
            mc.convex = false; // Binalar ve statik nesneler için hassas katı geometri

            addedCount++;
        }

        return addedCount;
    }
}
#endif
