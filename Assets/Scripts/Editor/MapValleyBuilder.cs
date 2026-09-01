#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MapValleyBuilder
{
    private const string SCENE_PATH = "Assets/Scenes/Map_Valley.unity";

    [MenuItem("Tools/Kargo Oyunu/Yeni Vadi Haritasini Olustur (Map_Valley)", false, 2)]
    public static void GenerateValleyMap()
    {
        // 1. Yeni Sahne Oluştur
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Kök Gruplar
        GameObject envRoot = new GameObject("_ENVIRONMENT");
        GameObject mountainRoot = new GameObject("Mountain_Ring");
        mountainRoot.transform.SetParent(envRoot.transform);

        GameObject cityRoot = new GameObject("Zone1_Downtown_City");
        cityRoot.transform.SetParent(envRoot.transform);

        GameObject suburbsRoot = new GameObject("Zone2_Suburbs_Residential");
        suburbsRoot.transform.SetParent(envRoot.transform);

        GameObject villageRoot = new GameObject("Zone3_Mountain_Village");
        villageRoot.transform.SetParent(envRoot.transform);

        GameObject roadsRoot = new GameObject("Road_Network");
        roadsRoot.transform.SetParent(envRoot.transform);

        GameObject gameplayRoot = new GameObject("_GAMEPLAY");
        GameObject deliveryPointsRoot = new GameObject("Delivery_Points_Pool");
        deliveryPointsRoot.transform.SetParent(gameplayRoot.transform);

        // 2. Ana Zemin (Ground)
        CreateGround(envRoot);

        // 3. Çevre Dağ Çemberi (Doğal Sınırlar)
        BuildMountainRing(mountainRoot);

        // 4. Yol Şebekesi
        BuildRoadNetwork(roadsRoot);

        // 5. Bölge 1: Şehir Merkezi (Downtown)
        BuildDowntownZone(cityRoot, deliveryPointsRoot);

        // 6. Bölge 2: Müstakil Evler & Mahalle (Suburbs)
        BuildSuburbsZone(suburbsRoot, deliveryPointsRoot);

        // 7. Bölge 3: Dağ Yamacı Köy Yeri (Mountain Village)
        BuildMountainVillageZone(villageRoot, deliveryPointsRoot);

        // 8. Işıklandırma & Gökyüzü (Directional Light / Sun)
        GameObject sunObj = new GameObject("Sun_DirectionalLight");
        sunObj.transform.SetParent(envRoot.transform);
        Light sunLight = sunObj.AddComponent<Light>();
        sunLight.type = LightType.Directional;
        sunLight.color = new Color(1f, 0.95f, 0.85f);
        sunLight.intensity = 1.25f;
        sunLight.shadows = LightShadows.Soft;
        sunObj.transform.rotation = Quaternion.Euler(35f, -30f, 0f);

        // 9. Araç (DeliveryVan) Yerleşimi
        GameObject vanPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/DeliveryVan.prefab");
        GameObject carObj = null;
        if (vanPrefab != null)
        {
            carObj = (GameObject)PrefabUtility.InstantiatePrefab(vanPrefab);
            carObj.transform.SetParent(gameplayRoot.transform);
            carObj.transform.position = new Vector3(0f, 0.5f, -80f);
            carObj.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            carObj.name = "DeliveryVan";
        }

        // 10. Kamera (SmoothFollowCamera)
        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        Camera cam = camObj.AddComponent<Camera>();
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 1500f;
        camObj.AddComponent<AudioListener>();

        SmoothFollowCamera followScript = camObj.AddComponent<SmoothFollowCamera>();
        if (carObj != null) followScript.target = carObj.transform;

        // 11. DeliveryManager (Inventory, DayTime, Economy)
        GameObject delivManager = new GameObject("DeliveryManager");
        delivManager.transform.SetParent(gameplayRoot.transform);

        VanInventory inventory = delivManager.AddComponent<VanInventory>();
        inventory.dailyPackageCount = 10;

        DayTimeManager timeMgr = delivManager.AddComponent<DayTimeManager>();
        timeMgr.directionalSun = sunLight;
        timeMgr.realTimeDurationInMinutes = 10f;

        delivManager.AddComponent<PlayerEconomyManager>();

        // 12. UI Arayüzünü İnşa Et
        DeliveryUIBuilder.BuildCleanDeliveryUI();

        // 13. Sahneyi Kaydet ve Build Ayarlarına Ekle
        EditorSceneManager.SaveScene(newScene, SCENE_PATH);
        AddSceneToBuildSettings(SCENE_PATH);

        Debug.Log($"[MapValleyBuilder] Yeni Vadi Haritası başarıyla oluşturuldu ve kaydedildi: {SCENE_PATH}");
    }

    private static void CreateGround(GameObject parent)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Valley_MainGround";
        ground.transform.SetParent(parent.transform);
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(80f, 1f, 80f); // 800m x 800m geniş alan

        Material grassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Grass.mat");
        if (grassMat == null) grassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Downtown Game Studio/Nature Pack - Low Polly Trees & Bushes/Materials/Floor 1.mat");
        if (grassMat != null) ground.GetComponent<Renderer>().sharedMaterial = grassMat;
    }

    private static void BuildMountainRing(GameObject parent)
    {
        GameObject bg1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Downtown Game Studio/Nature Pack - Low Polly Trees & Bushes/Prefabs/Background1.prefab");
        GameObject bg2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Downtown Game Studio/Nature Pack - Low Polly Trees & Bushes/Prefabs/Background2.prefab");

        if (bg1 == null && bg2 == null) return;

        // 4 Tarafı Çevreleyen Dağ Sıraları
        // Kuzey Dağları (Köyün Arkası)
        for (int x = -350; x <= 350; x += 120)
        {
            GameObject m = (GameObject)PrefabUtility.InstantiatePrefab(x % 240 == 0 ? bg1 : bg2);
            m.transform.SetParent(parent.transform);
            m.transform.position = new Vector3(x, 0, 360);
            m.transform.localScale = new Vector3(5f, 6f, 5f);
            m.transform.rotation = Quaternion.Euler(0, 180, 0);
            AddMeshColliderIfMissing(m);
        }

        // Güney Dağları
        for (int x = -350; x <= 350; x += 120)
        {
            GameObject m = (GameObject)PrefabUtility.InstantiatePrefab(x % 240 == 0 ? bg2 : bg1);
            m.transform.SetParent(parent.transform);
            m.transform.position = new Vector3(x, 0, -360);
            m.transform.localScale = new Vector3(5f, 5.5f, 5f);
            m.transform.rotation = Quaternion.Euler(0, 0, 0);
            AddMeshColliderIfMissing(m);
        }

        // Doğu Dağları (Şehrin Arkası)
        for (int z = -300; z <= 300; z += 120)
        {
            GameObject m = (GameObject)PrefabUtility.InstantiatePrefab(z % 240 == 0 ? bg1 : bg2);
            m.transform.SetParent(parent.transform);
            m.transform.position = new Vector3(360, 0, z);
            m.transform.localScale = new Vector3(5f, 5.5f, 5f);
            m.transform.rotation = Quaternion.Euler(0, -90, 0);
            AddMeshColliderIfMissing(m);
        }

        // Batı Dağları (Müstakil Evlerin Arkası)
        for (int z = -300; z <= 300; z += 120)
        {
            GameObject m = (GameObject)PrefabUtility.InstantiatePrefab(z % 240 == 0 ? bg2 : bg1);
            m.transform.SetParent(parent.transform);
            m.transform.position = new Vector3(-360, 0, z);
            m.transform.localScale = new Vector3(5f, 5.5f, 5f);
            m.transform.rotation = Quaternion.Euler(0, 90, 0);
            AddMeshColliderIfMissing(m);
        }
    }

    private static void BuildRoadNetwork(GameObject parent)
    {
        GameObject straight = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Pandazole_Ultimate_Pack/Pandazole City Town Pack/Prefabs/Roads/Env_Road_Straight_01.prefab");
        if (straight == null) straight = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Pandazole_Ultimate_Pack/Pandazole City Town Pack/Prefabs/Roads/Env_Road_Straight_02.prefab");

        // Ana Çapraz ve Bağlantı Bulvarı (Z: -200'den Z: +100'e)
        if (straight != null)
        {
            // Şehir Ana Caddesi (X: 80, Z: -150..50)
            for (int z = -150; z <= 50; z += 20)
            {
                SpawnObject(straight, parent, new Vector3(80, 0.05f, z), Quaternion.Euler(0, 0, 0), Vector3.one);
            }

            // Müstakil Mahalle Ana Caddesi (X: -80, Z: -150..50)
            for (int z = -150; z <= 50; z += 20)
            {
                SpawnObject(straight, parent, new Vector3(-80, 0.05f, z), Quaternion.Euler(0, 0, 0), Vector3.one);
            }

            // Doğu-Batı Bağlantı Bulvarı (Z: -50, X: -140..140)
            for (int x = -140; x <= 140; x += 20)
            {
                SpawnObject(straight, parent, new Vector3(x, 0.05f, -50), Quaternion.Euler(0, 90, 0), Vector3.one);
            }

            // Dağ Köyü Bağlantı Yolu (Z: 50..220)
            for (int z = 50; z <= 220; z += 20)
            {
                float curveX = Mathf.Sin((z - 50) * 0.05f) * 45f;
                float heightY = (z - 50) * 0.12f; // Kademeli yükselen rampa
                SpawnObject(straight, parent, new Vector3(curveX, heightY + 0.05f, z), Quaternion.Euler(curveX > 0 ? 5 : -5, curveX > 0 ? 20 : -20, 0), Vector3.one);
            }
        }
    }

    private static void BuildDowntownZone(GameObject parent, GameObject pointsPool)
    {
        string[] companyPrefabs = new string[]
        {
            "Assets/Pandazole_Ultimate_Pack/Pandazole City Town Pack/Prefabs/Env_CompanyBuilding_01.prefab",
            "Assets/Pandazole_Ultimate_Pack/Pandazole City Town Pack/Prefabs/Env_CompanyBuilding_02.prefab",
            "Assets/Pandazole_Ultimate_Pack/Pandazole City Town Pack/Prefabs/Env_CompanyBuilding_03.prefab",
            "Assets/Pandazole_Ultimate_Pack/Pandazole City Town Pack/Prefabs/Env_CompanyBuilding_04.prefab",
            "Assets/Pandazole_Ultimate_Pack/Pandazole City Town Pack/Prefabs/Env_CommercialBuilding_01.prefab",
            "Assets/Pandazole_Ultimate_Pack/Pandazole City Town Pack/Prefabs/Env_CommercialBuilding_02.prefab",
            "Assets/Pandazole_Ultimate_Pack/Pandazole City Town Pack/Prefabs/Env_CommercialBuilding_03.prefab",
            "Assets/Pandazole_Ultimate_Pack/Pandazole City Town Pack/Prefabs/Env_CommercialBuilding_04.prefab",
            "Assets/Pandazole_Ultimate_Pack/Pandazole City Town Pack/Prefabs/Env_Motel_01.prefab",
            "Assets/Pandazole_Ultimate_Pack/Pandazole City Town Pack/Prefabs/Env_Motel_02.prefab"
        };

        string[] descriptions = new string[]
        {
            "Mavi camlı yüksek şirket plazası, ana giriş kapısı.",
            "Köşedeki 3 katlı ticari iş merkezi, tabela altı.",
            "Şehir meydanındaki sarı cepheli motel, resepsiyon önü.",
            "Gri betonarme finans merkezi, döner kapı önü.",
            "Geniş otoparklı 2 katlı motel binası.",
            "Büyük reklam panolu teknoloji plazası.",
            "Köşe başındaki turuncu çizgili mağaza binası.",
            "Çift girişli modern ofis kulesi.",
            "Girişinde bayrak direkleri olan kurumsal şirket binası.",
            "Şehir caddesi sonundaki butik otel girişi."
        };

        int idCounter = 1;

        // Doğu Tarafı Plazalar
        for (int i = 0; i < 5; i++)
        {
            string pPath = companyPrefabs[i % companyPrefabs.Length];
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pPath);
            Vector3 pos = new Vector3(120f, 0f, -140f + (i * 45f));

            if (prefab != null)
            {
                GameObject b = SpawnObject(prefab, parent, pos, Quaternion.Euler(0, -90, 0), Vector3.one * 1.2f);
                AddBoxColliderIfMissing(b);

                // Teslimat Noktası
                Vector3 dropPos = pos + new Vector3(-12f, 0.2f, 0f);
                CreateDeliveryPoint(pointsPool, $"SHR-{idCounter:D2}", $"Ataturk Bulvari Plaza No: {idCounter * 2}", descriptions[i], dropPos);
                idCounter++;
            }
        }

        // Batı Tarafı Ticari Binalar & Moteller
        for (int i = 5; i < 10; i++)
        {
            string pPath = companyPrefabs[i % companyPrefabs.Length];
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pPath);
            Vector3 pos = new Vector3(40f, 0f, -140f + ((i - 5) * 45f));

            if (prefab != null)
            {
                GameObject b = SpawnObject(prefab, parent, pos, Quaternion.Euler(0, 90, 0), Vector3.one * 1.2f);
                AddBoxColliderIfMissing(b);

                // Teslimat Noktası
                Vector3 dropPos = pos + new Vector3(12f, 0.2f, 0f);
                CreateDeliveryPoint(pointsPool, $"SHR-{idCounter:D2}", $"Inonu Caddesi Is Merkezi No: {idCounter}", descriptions[i], dropPos);
                idCounter++;
            }
        }
    }

    private static void BuildSuburbsZone(GameObject parent, GameObject pointsPool)
    {
        string[] residentPrefabs = new string[]
        {
            "Assets/Pandazole_Ultimate_Pack/Pandazole City Town Pack/Prefabs/Env_ResidentBuilding_01.prefab",
            "Assets/Pandazole_Ultimate_Pack/Pandazole City Town Pack/Prefabs/Env_ResidentBuilding_02.prefab",
            "Assets/Pandazole_Ultimate_Pack/Pandazole City Town Pack/Prefabs/Env_ResidentBuilding_03.prefab",
            "Assets/Pandazole_Ultimate_Pack/Pandazole City Town Pack/Prefabs/Env_ResidentBuilding_04.prefab",
            "Assets/Pandazole_Ultimate_Pack/Pandazole City Town Pack/Prefabs/Env_ResidentBuilding_05.prefab",
            "Assets/Pandazole_Ultimate_Pack/Pandazole City Town Pack/Prefabs/Env_ResidentBuilding_06.prefab"
        };

        // 1. Sokak: Barış Manço Sokak (11 Ev)
        for (int i = 1; i <= 11; i++)
        {
            string pPath = residentPrefabs[i % residentPrefabs.Length];
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pPath);
            Vector3 pos = new Vector3(-120f, 0f, -160f + (i * 28f));

            if (prefab != null)
            {
                GameObject house = SpawnObject(prefab, parent, pos, Quaternion.Euler(0, 90, 0), Vector3.one);
                AddBoxColliderIfMissing(house);

                string desc = (i % 2 == 0) 
                    ? $"Kirmizi catili, on bahcesinde citler olan {i} numarali villa." 
                    : $"Beyaz sundurmali, onunde mavi cicekler acmis {i} numarali ev.";

                Vector3 dropPos = pos + new Vector3(10f, 0.2f, 0f);
                CreateDeliveryPoint(pointsPool, $"SUB-BM-{i:D2}", $"Cumhuriyet Mah. Baris Manco Sokak No: {i}", desc, dropPos);
            }
        }

        // 2. Sokak: Aşık Veysel Sokak (11 Ev)
        for (int i = 1; i <= 11; i++)
        {
            string pPath = residentPrefabs[(i + 2) % residentPrefabs.Length];
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pPath);
            Vector3 pos = new Vector3(-45f, 0f, -160f + (i * 28f));

            if (prefab != null)
            {
                GameObject house = SpawnObject(prefab, parent, pos, Quaternion.Euler(0, -90, 0), Vector3.one);
                AddBoxColliderIfMissing(house);

                string desc = (i % 2 == 0) 
                    ? $"Bahcesinde buyuk cam agaci olan, ahsap balkonlu {i} numarali mustakil ev." 
                    : $"Gri tuglali, bahce kapisi yesil boyali {i} numarali bahceli ev.";

                Vector3 dropPos = pos + new Vector3(-10f, 0.2f, 0f);
                CreateDeliveryPoint(pointsPool, $"SUB-AV-{i:D2}", $"Cumhuriyet Mah. Asik Veysel Sokak No: {i}", desc, dropPos);
            }
        }
    }

    private static void BuildMountainVillageZone(GameObject parent, GameObject pointsPool)
    {
        string[] villagePrefabs = new string[]
        {
            "Assets/Pandazole_Ultimate_Pack/Pandazole City Town Pack/Prefabs/Env_ResidentBuilding_05.prefab",
            "Assets/Pandazole_Ultimate_Pack/Pandazole City Town Pack/Prefabs/Env_ResidentBuilding_06.prefab",
            "Assets/Pandazole_Ultimate_Pack/Pandazole City Town Pack/Prefabs/Env_Motel_05.prefab"
        };

        GameObject treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Downtown Game Studio/Nature Pack - Low Polly Trees & Bushes/Prefabs/tree 1.prefab");
        if (treePrefab == null) treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Downtown Game Studio/Nature Pack - Low Polly Trees & Bushes/Prefabs/Street Tree 1.prefab");

        // Dağ Yamacı Köy Evleri (12 Ev)
        string[] villageDescriptions = new string[]
        {
            "Yokushun basindaki tas temelli koy evi, onunde odun yiginlari var.",
            "Dag yamacinda buyuk cam agaclarinin arasindaki ahsap koy evi.",
            "Tepedeki manzarali ciftlik evi, bahcesinde saman balyalari var.",
            "Virajin ic kismindaki kirmizi kepenkli dağ kulubesi.",
            "Dag yolunun en yuksek noktasindaki beyaz koy konagi.",
            "Orman kenarindaki tek katli ahsap yayla evi.",
            "Koy meydanindaki tas fırınlı eski tas ev.",
            "Yamacta vadiden gorunen iki katli genis dag evi.",
            "Sik agacliklar arasinda patika yolu olan koy evi.",
            "Koyun girisindeki kucuk bahceli kulube.",
            "Yamac virajindaki tahta citli ormanci kulubesi.",
            "En ust zirveye yakin tepe villasi."
        };

        for (int i = 0; i < 12; i++)
        {
            float zPos = 70f + (i * 15f);
            float xPos = (i % 2 == 0) ? -35f - (i * 2.5f) : 35f + (i * 2.5f);
            float yPos = (zPos - 50f) * 0.12f; // Dağ yamacı yüksekliği

            // Tepe Platformu (Toprak zemin yükseltisi)
            GameObject mound = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mound.name = $"Hill_Plateau_{i + 1}";
            mound.transform.SetParent(parent.transform);
            mound.transform.position = new Vector3(xPos, yPos - 0.5f, zPos);
            mound.transform.localScale = new Vector3(25f, 1f, 25f);

            // Köy Evi
            string pPath = villagePrefabs[i % villagePrefabs.Length];
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pPath);

            if (prefab != null)
            {
                GameObject house = SpawnObject(prefab, parent, new Vector3(xPos, yPos + 0.5f, zPos), Quaternion.Euler(0, (i % 2 == 0) ? 90 : -90, 0), Vector3.one * 1.1f);
                AddBoxColliderIfMissing(house);

                Vector3 dropPos = new Vector3(xPos + ((i % 2 == 0) ? 10f : -10f), yPos + 0.6f, zPos);
                CreateDeliveryPoint(pointsPool, $"VIL-{i + 1:D2}", $"Yaylalar Koyu No: {i + 1}", villageDescriptions[i], dropPos);
            }

            // Çevresine Çam Ağaçları Ekle
            if (treePrefab != null)
            {
                for (int t = 0; t < 3; t++)
                {
                    Vector3 treePos = new Vector3(xPos + UnityEngine.Random.Range(-12f, 12f), yPos + 0.5f, zPos + UnityEngine.Random.Range(-12f, 12f));
                    SpawnObject(treePrefab, parent, treePos, Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0), Vector3.one * UnityEngine.Random.Range(1.2f, 2.2f));
                }
            }
        }
    }

    private static void CreateDeliveryPoint(GameObject poolParent, string id, string addressName, string description, Vector3 position)
    {
        GameObject pointObj = new GameObject($"Point_{id}_{addressName}");
        pointObj.transform.SetParent(poolParent.transform);
        pointObj.transform.position = position;

        SphereCollider col = pointObj.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 4.0f; // 4m teslimat algılama yarıçapı

        DeliveryPoint dp = pointObj.AddComponent<DeliveryPoint>();
        dp.pointId = id;
        dp.addressName = addressName;
        dp.addressDescription = description;

        // Görsel Teslimat Alanı Çemberi (Yerde hafif yeşil/sarı parlayan disk)
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = "VisualMarker";
        marker.transform.SetParent(pointObj.transform);
        marker.transform.localPosition = Vector3.zero;
        marker.transform.localScale = new Vector3(4.5f, 0.04f, 4.5f);
        Object.DestroyImmediate(marker.GetComponent<Collider>());

        Renderer ren = marker.GetComponent<Renderer>();
        if (ren != null)
        {
            Material m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.color = new Color(0.2f, 0.85f, 0.4f, 0.6f);
            ren.material = m;
        }

        dp.visualMarker = marker;
    }

    private static GameObject SpawnObject(GameObject prefab, GameObject parent, Vector3 pos, Quaternion rot, Vector3 scale)
    {
        GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        obj.transform.SetParent(parent.transform);
        obj.transform.position = pos;
        obj.transform.rotation = rot;
        obj.transform.localScale = scale;
        return obj;
    }

    private static void AddBoxColliderIfMissing(GameObject obj)
    {
        if (obj.GetComponentInChildren<Collider>() == null)
        {
            BoxCollider bc = obj.AddComponent<BoxCollider>();
            bc.size = new Vector3(10f, 15f, 10f);
            bc.center = new Vector3(0, 7.5f, 0);
        }
    }

    private static void AddMeshColliderIfMissing(GameObject obj)
    {
        MeshRenderer[] renderers = obj.GetComponentsInChildren<MeshRenderer>();
        foreach (var r in renderers)
        {
            if (r.GetComponent<Collider>() == null)
            {
                r.gameObject.AddComponent<MeshCollider>();
            }
        }
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool exists = false;
        foreach (var s in scenes)
        {
            if (s.path == scenePath)
            {
                exists = true;
                break;
            }
        }

        if (!exists)
        {
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
