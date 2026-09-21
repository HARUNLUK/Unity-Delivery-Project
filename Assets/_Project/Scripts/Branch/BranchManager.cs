using System;
using System.Collections.Generic;
using UnityEngine;

public class BranchManager : MonoBehaviour
{
    public static BranchManager Instance { get; private set; }

    [Header("--- BRANCH TIERS CONFIGURATION ---")]
    [Tooltip("List of all branch levels and building configurations")]
    public List<BranchTier> branchTiers = new List<BranchTier>();

    [Header("--- SCENE ANCHORS & REFERENCES (OPTIONAL) ---")]
    [Tooltip("Root transform where active building is placed/spawned")]
    public Transform buildingContainer;

    [Tooltip("Optional fallback cargo package generator in the warehouse")]
    public CargoWarehouseGenerator warehouseGenerator;

    [Tooltip("Optional fallback physical in-world upgrade terminal / desk")]
    public BranchUpgradeTerminal upgradeTerminal;

    [Header("--- KARAKTERİN DIŞARIDA SPAWN OLACAĞI NOKTA (EXTERIOR SPAWN) ---")]
    [Tooltip("If true, triggers a smooth transition screen and teleports the character outside looking at the upgraded branch")]
    public bool enableUpgradeTransition = true;

    [Tooltip("Karakterin yükseltme sonrasında dışarıda doğacağı Spawn Noktası (Sahnedeki bir Transform nesnesini buraya sürükleyebilirsiniz)")]
    public Transform playerExteriorSpawnPoint;

    [Tooltip("Karakterin spawn olduğunda bakacağı hedef Transform (Boş bırakılırsa doğrudan şube binasına bakar)")]
    public Transform playerLookTarget;

    [Tooltip("Eğer sahnede bir Transform atanmamışsa, şube merkezinden yerel ofset")]
    public Vector3 defaultExteriorOffset = new Vector3(0f, 0f, -14f);

    [Tooltip("Otomatik spawn için şube binası önü mesafesi")]
    public float autoExteriorDistance = 14f;

    [Tooltip("Kameranın şube binasına bakış yükseklik ofseti")]
    public float lookAtHeightOffset = 2.5f;

    [Header("--- CARGO TYPE UNLOCK LEVELS & SPAWN CHANCES ---")]
    [Tooltip("Minimum branch level required for Standard cargo")]
    public int standardRequiredLevel = 1;
    [Range(0f, 100f), Tooltip("Standard cargo spawn weight / probability")]
    public float standardSpawnWeight = 50f;

    [Tooltip("Minimum branch level required for Fragile cargo")]
    public int fragileRequiredLevel = 2;
    [Range(0f, 100f), Tooltip("Fragile cargo spawn weight / probability")]
    public float fragileSpawnWeight = 30f;

    [Tooltip("Minimum branch level required for Express cargo")]
    public int expressRequiredLevel = 2;
    [Range(0f, 100f), Tooltip("Express cargo spawn weight / probability")]
    public float expressSpawnWeight = 20f;

    [Tooltip("Minimum branch level required for Explosive cargo")]
    public int explosiveRequiredLevel = 3;
    [Range(0f, 100f), Tooltip("Explosive cargo spawn weight / probability")]
    public float explosiveSpawnWeight = 20f;

    [Header("--- EXPRESS CARGO TIME LIMIT ---")]
    [Tooltip("Minimum target delivery hour for Express cargo (e.g. 10.0 = 10:00)")]
    public float minExpressDeliveryHour = 10.0f;

    [Tooltip("Maximum target delivery hour for Express cargo (e.g. 14.0 = 14:00)")]
    public float maxExpressDeliveryHour = 14.0f;

    [Tooltip("Minute step when picking random express time (e.g. 15 = 10:00, 10:15, 10:30, 10:45...)")]
    [Range(5, 60)]
    public int expressMinuteInterval = 15;

    [Header("--- FRAGILE & EXPLOSIVE TUNING ---")]
    [Tooltip("Minimum impact speed to trigger damage (m/s). Gentle drop < 2.5 m/s, 1.5m drop ~5.0 m/s, high drop > 7.0 m/s.")]
    public float fragileMinDamageSpeedThreshold = 3.5f;

    [Tooltip("Damage multiplier when speed threshold is exceeded.")]
    public float fragileDamageMultiplier = 16.0f;

    [Tooltip("Damage ratio when packages collide with each other (0.20 = 80% less damage).")]
    public float fragilePackageCollisionRatio = 0.20f;

    [Tooltip("Spawn immunity duration (seconds).")]
    public float fragileSpawnImmunityDuration = 3.5f;

    [Header("--- DEFAULT & CUSTOM CARGO PACKAGE PREFABS ---")]
    [Tooltip("Primary default cargo box prefab (Fallback used if specific lists are empty)")]
    public GameObject cargoPackagePrefab;

    [Tooltip("Custom 3D Prefabs for standard packages (drag prefabs from project)")]
    public List<GameObject> packagePrefabs = new List<GameObject>();

    [Tooltip("Custom 3D Prefabs for fragile packages")]
    public List<GameObject> fragilePackagePrefabs = new List<GameObject>();

    [Tooltip("Custom 3D Prefabs for express packages")]
    public List<GameObject> expressPackagePrefabs = new List<GameObject>();

    [Tooltip("Custom 3D Prefabs for explosive packages")]
    public List<GameObject> explosivePackagePrefabs = new List<GameObject>();

    [Header("--- EXPLOSION VFX PREFAB ---")]
    [Tooltip("Custom explosion particle effect prefab (drag prefab from project or leave empty for procedural effect)")]
    public GameObject explosionVfxPrefab;

    public enum ElongatedAxisMode
    {
        HorizontalOnly_XZ, // Stretches randomly X or Z (keeps vertical height normal)
        Forward_Z_Only,    // Always stretches length/depth (Z)
        Width_X_Only,      // Always stretches width (X)
        Height_Y_Only,     // Always stretches vertical height (Y)
        Random_AnyAxis     // Stretches any of X, Y, or Z randomly
    }

    [Header("--- PREFAB SCALE MULTIPLIER PER CARGO TYPE ---")]
    [Tooltip("Apply scale multiplier to custom 3D prefabs")]
    public bool enablePrefabScaling = true;

    [Tooltip("Scale X, Y, Z axes independently with random multipliers")]
    public bool randomizeAxesIndependently = false;

    [Header("Standard Cargo Scale & Elongation")]
    [Tooltip("If true, scales randomly between standardMinScale and standardMaxScale. If false, uses standardFixedScale.")]
    public bool standardRandomScale = true;
    [Range(0.05f, 2.5f), Tooltip("Fixed scale multiplier used when random scaling is disabled")]
    public float standardFixedScale = 0.45f;
    [Range(0.05f, 2.5f), Tooltip("Standard package minimum random scale multiplier")]
    public float standardMinScale = 0.35f;
    [Range(0.05f, 2.5f), Tooltip("Standard package maximum random scale multiplier")]
    public float standardMaxScale = 0.55f;
    [Tooltip("Enable single-axis elongation for Standard cargo (e.g. length 5, width 1)")]
    public bool standardEnableElongated = true;
    [Range(0f, 100f), Tooltip("Standard cargo elongated spawn chance percentage")]
    public float standardElongatedChance = 25f;
    [Range(1.0f, 10.0f), Tooltip("Standard cargo minimum elongated multiplier for single axis")]
    public float standardElongatedMinMultiplier = 2.0f;
    [Range(1.0f, 10.0f), Tooltip("Standard cargo maximum elongated multiplier for single axis")]
    public float standardElongatedMaxMultiplier = 4.5f;
    [Tooltip("Which axis should be elongated for Standard packages")]
    public ElongatedAxisMode standardElongatedAxisMode = ElongatedAxisMode.HorizontalOnly_XZ;

    [Header("Fragile Cargo Scale & Elongation")]
    [Tooltip("If true, scales randomly between fragileMinScale and fragileMaxScale. If false, uses fragileFixedScale.")]
    public bool fragileRandomScale = true;
    [Range(0.05f, 2.5f), Tooltip("Fixed scale multiplier used when random scaling is disabled")]
    public float fragileFixedScale = 0.40f;
    [Range(0.05f, 2.5f), Tooltip("Fragile package minimum random scale multiplier")]
    public float fragileMinScale = 0.35f;
    [Range(0.05f, 2.5f), Tooltip("Fragile package maximum random scale multiplier")]
    public float fragileMaxScale = 0.50f;
    [Tooltip("Enable single-axis elongation for Fragile cargo")]
    public bool fragileEnableElongated = true;
    [Range(0f, 100f), Tooltip("Fragile cargo elongated spawn chance percentage")]
    public float fragileElongatedChance = 20f;
    [Range(1.0f, 10.0f), Tooltip("Fragile cargo minimum elongated multiplier for single axis")]
    public float fragileElongatedMinMultiplier = 1.8f;
    [Range(1.0f, 10.0f), Tooltip("Fragile cargo maximum elongated multiplier for single axis")]
    public float fragileElongatedMaxMultiplier = 3.5f;
    [Tooltip("Which axis should be elongated for Fragile packages")]
    public ElongatedAxisMode fragileElongatedAxisMode = ElongatedAxisMode.HorizontalOnly_XZ;

    [Header("Express Cargo Scale & Elongation")]
    [Tooltip("If true, scales randomly between expressMinScale and expressMaxScale. If false, uses expressFixedScale.")]
    public bool expressRandomScale = true;
    [Range(0.05f, 2.5f), Tooltip("Fixed scale multiplier used when random scaling is disabled")]
    public float expressFixedScale = 0.35f;
    [Range(0.05f, 2.5f), Tooltip("Express package minimum random scale multiplier")]
    public float expressMinScale = 0.30f;
    [Range(0.05f, 2.5f), Tooltip("Express package maximum random scale multiplier")]
    public float expressMaxScale = 0.45f;
    [Tooltip("Enable single-axis elongation for Express cargo")]
    public bool expressEnableElongated = true;
    [Range(0f, 100f), Tooltip("Express cargo elongated spawn chance percentage")]
    public float expressElongatedChance = 30f;
    [Range(1.0f, 10.0f), Tooltip("Express cargo minimum elongated multiplier for single axis")]
    public float expressElongatedMinMultiplier = 2.0f;
    [Range(1.0f, 10.0f), Tooltip("Express cargo maximum elongated multiplier for single axis")]
    public float expressElongatedMaxMultiplier = 4.0f;
    [Tooltip("Which axis should be elongated for Express packages")]
    public ElongatedAxisMode expressElongatedAxisMode = ElongatedAxisMode.HorizontalOnly_XZ;

    [Header("Explosive Cargo Scale & Elongation")]
    [Tooltip("If true, scales randomly between explosiveMinScale and explosiveMaxScale. If false, uses explosiveFixedScale.")]
    public bool explosiveRandomScale = true;
    [Range(0.05f, 2.5f), Tooltip("Fixed scale multiplier used when random scaling is disabled")]
    public float explosiveFixedScale = 0.50f;
    [Range(0.05f, 2.5f), Tooltip("Explosive package minimum random scale multiplier")]
    public float explosiveMinScale = 0.40f;
    [Range(0.05f, 2.5f), Tooltip("Explosive package maximum random scale multiplier")]
    public float explosiveMaxScale = 0.60f;
    [Tooltip("Enable single-axis elongation for Explosive cargo")]
    public bool explosiveEnableElongated = true;
    [Range(0f, 100f), Tooltip("Explosive cargo elongated spawn chance percentage")]
    public float explosiveElongatedChance = 15f;
    [Range(1.0f, 10.0f), Tooltip("Explosive cargo minimum elongated multiplier for single axis")]
    public float explosiveElongatedMinMultiplier = 1.8f;
    [Range(1.0f, 10.0f), Tooltip("Explosive cargo maximum elongated multiplier for single axis")]
    public float explosiveElongatedMaxMultiplier = 3.0f;
    [Tooltip("Which axis should be elongated for Explosive packages")]
    public ElongatedAxisMode explosiveElongatedAxisMode = ElongatedAxisMode.HorizontalOnly_XZ;

    [Header("--- CARGO BOX MATERIALS (SKINS) ---")]
    [Tooltip("Cardboard materials for standard packages")]
    public List<Material> cardboardMaterials = new List<Material>();

    [Tooltip("Materials for fragile packages")]
    public List<Material> fragileMaterials = new List<Material>();

    [Tooltip("Materials for express packages")]
    public List<Material> expressMaterials = new List<Material>();

    [Tooltip("Materials for explosive packages")]
    public List<Material> explosiveMaterials = new List<Material>();

    public static event Action<int, BranchTier> OnBranchUpgraded;
    public static event Action OnBranchReset;

    private const string PREFS_BRANCH_LEVEL = "Delivery_BranchLevel";
    private int currentBranchLevel = 1;
    private GameObject activeBuildingInstance;

    public int CurrentBranchLevel => currentBranchLevel;
    public BranchTier CurrentTier => GetTierByLevel(currentBranchLevel);
    public BranchTier NextTier => GetTierByLevel(currentBranchLevel + 1);
    public bool HasNextTier => NextTier != null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (branchTiers == null || branchTiers.Count == 0)
        {
            PopulateDefaultTiers();
        }
        else
        {
            SanitizeTierNamesAndDescriptions();
        }

        EnsureReferences();
        LoadBranchLevel();
        ApplyTierVisuals(false);
    }

    private void Start()
    {
        EnsureReferences();
        if (activeBuildingInstance == null)
        {
            ApplyTierVisuals(false);
        }
    }

    private void OnValidate()
    {
        SanitizeTierNamesAndDescriptions();
    }

    public void EnsureReferences()
    {
        SanitizeTierNamesAndDescriptions();

        if (buildingContainer == null)
        {
            buildingContainer = transform;
        }

        if (warehouseGenerator == null)
        {
            warehouseGenerator = UnityEngine.Object.FindAnyObjectByType<CargoWarehouseGenerator>();
        }

        if (upgradeTerminal == null)
        {
            upgradeTerminal = UnityEngine.Object.FindAnyObjectByType<BranchUpgradeTerminal>();
        }
    }

    public void SanitizeTierNamesAndDescriptions()
    {
        if (branchTiers == null || branchTiers.Count == 0)
        {
            PopulateDefaultTiers();
            return;
        }

        // Populate missing default names or fix mismatched descriptions
        foreach (var t in branchTiers)
        {
            if (t == null) continue;
            switch (t.tierLevel)
            {
                case 1:
                    if (string.IsNullOrEmpty(t.tierName)) t.tierName = "Starter Garage";
                    if (string.IsNullOrEmpty(t.description) || t.description.StartsWith("Starter branch.")) t.description = "Temel koli depolama alanı ve başlangıç dağıtım ofisi.";
                    if (t.dailyPackageCapacity <= 0 || t.dailyPackageCapacity == 5) t.dailyPackageCapacity = 8;
                    if (t.dailyRent <= 0) t.dailyRent = 50;
                    t.unlockedPerks = new string[]
                    {
                        "8 Koli / Gün Kapasitesi",
                        "Standart Kargo Dağıtımı ($80 - $180)",
                        "Maple Town Bölgesi Teslimatları"
                    };
                    break;
                case 2:
                    if (string.IsNullOrEmpty(t.tierName)) t.tierName = "Regional Hub";
                    t.description = "Genişletilmiş koli kapasitesi ve oto tamir/servis garajı erişimi.";
                    if (t.dailyPackageCapacity <= 0 || t.dailyPackageCapacity == 10) t.dailyPackageCapacity = 14;
                    if (t.dailyRent <= 0) t.dailyRent = 120;
                    if (t.upgradeCost <= 0) t.upgradeCost = 1200;
                    if (t.requiredPlayerLevel <= 0) t.requiredPlayerLevel = 2;
                    t.unlockedPerks = new string[]
                    {
                        "14 Koli / Gün Kapasitesi (+6 Koli)",
                        "Oto Sanayi & Tamir Garajı Seviye Yeterliliği"
                    };
                    break;
                case 3:
                    if (string.IsNullOrEmpty(t.tierName) || t.tierName == "Regional Hub") t.tierName = "District Distribution Center";
                    t.description = "Maksimum koli kapasitesi ve yüksek kazançlı patlayıcı kargo sevkiyatı.";
                    if (t.dailyPackageCapacity <= 0) t.dailyPackageCapacity = 20;
                    if (t.dailyRent <= 0) t.dailyRent = 280;
                    if (t.upgradeCost <= 0) t.upgradeCost = 3500;
                    if (t.requiredPlayerLevel <= 0) t.requiredPlayerLevel = 3;
                    t.unlockedPerks = new string[]
                    {
                        "20 Koli / Gün Kapasitesi (+6 Koli)",
                        "Patlayıcı Kargo Açılır (TNT / Kimyasal - Yüksek Kazanç)"
                    };
                    break;
            }
        }
    }

    public void PopulateDefaultTiers()
    {
        branchTiers = new List<BranchTier>()
        {
            new BranchTier
            {
                tierLevel = 1,
                tierName = "Starter Garage",
                description = "Temel koli depolama alanı ve başlangıç dağıtım ofisi.",
                unlockedPerks = new string[]
                {
                    "8 Koli / Gün Kapasitesi",
                    "Standart Kargo Dağıtımı ($80 - $180)",
                    "Maple Town Bölgesi Teslimatları"
                },
                upgradeCost = 0,
                requiredPlayerLevel = 1,
                dailyPackageCapacity = 8,
                dailyRent = 50
            },
            new BranchTier
            {
                tierLevel = 2,
                tierName = "Regional Hub",
                description = "Genişletilmiş koli kapasitesi ve oto tamir/servis garajı erişimi.",
                unlockedPerks = new string[]
                {
                    "14 Koli / Gün Kapasitesi (+6 Koli)",
                    "Oto Sanayi & Tamir Garajı Seviye Yeterliliği"
                },
                upgradeCost = 1200,
                requiredPlayerLevel = 2,
                dailyPackageCapacity = 14,
                dailyRent = 120
            },
            new BranchTier
            {
                tierLevel = 3,
                tierName = "District Distribution Center",
                description = "Maksimum koli kapasitesi ve yüksek kazançlı patlayıcı kargo sevkiyatı.",
                unlockedPerks = new string[]
                {
                    "20 Koli / Gün Kapasitesi (+6 Koli)",
                    "Patlayıcı Kargo Açılır (TNT / Kimyasal - Yüksek Kazanç)"
                },
                upgradeCost = 3500,
                requiredPlayerLevel = 3,
                dailyPackageCapacity = 20,
                dailyRent = 280
            }
        };
    }

    public BranchTier GetTierByLevel(int level)
    {
        if (branchTiers == null) return null;
        return branchTiers.Find(t => t.tierLevel == level);
    }

    public int GetDailyPackageLimit()
    {
        BranchTier tier = CurrentTier;
        return tier != null ? tier.dailyPackageCapacity : 4;
    }

    public int GetDailyRent()
    {
        BranchTier tier = CurrentTier;
        return tier != null ? tier.dailyRent : 50;
    }

    public CargoWarehouseGenerator GetActiveWarehouseGenerator()
    {
        BranchTier current = CurrentTier;
        if (current != null)
        {
            // 1. Check inside instantiated building prefab
            if (activeBuildingInstance != null)
            {
                CargoWarehouseGenerator gen = activeBuildingInstance.GetComponentInChildren<CargoWarehouseGenerator>();
                if (gen != null && gen.gameObject.activeInHierarchy) return gen;
            }

            // 2. Check inside scene building root
            if (current.sceneBuildingRoot != null && current.sceneBuildingRoot.activeInHierarchy)
            {
                CargoWarehouseGenerator gen = current.sceneBuildingRoot.GetComponentInChildren<CargoWarehouseGenerator>();
                if (gen != null && gen.gameObject.activeInHierarchy) return gen;
            }
        }

        // 3. Fallback to assigned reference or active in scene
        if (warehouseGenerator != null && warehouseGenerator.gameObject.activeInHierarchy) return warehouseGenerator;
        return UnityEngine.Object.FindAnyObjectByType<CargoWarehouseGenerator>();
    }

    public BranchUpgradeTerminal GetActiveUpgradeTerminal()
    {
        BranchTier current = CurrentTier;
        if (current != null)
        {
            if (activeBuildingInstance != null)
            {
                BranchUpgradeTerminal t = activeBuildingInstance.GetComponentInChildren<BranchUpgradeTerminal>();
                if (t != null && t.gameObject.activeInHierarchy) return t;
            }

            if (current.sceneBuildingRoot != null && current.sceneBuildingRoot.activeInHierarchy)
            {
                BranchUpgradeTerminal t = current.sceneBuildingRoot.GetComponentInChildren<BranchUpgradeTerminal>();
                if (t != null && t.gameObject.activeInHierarchy) return t;
            }
        }

        if (upgradeTerminal != null && upgradeTerminal.gameObject.activeInHierarchy) return upgradeTerminal;
        return UnityEngine.Object.FindAnyObjectByType<BranchUpgradeTerminal>();
    }

    /// <summary>
    /// Calculates the outside viewpoint position and target look position for the given branch tier.
    /// Prioritizes tier-specific spawn points first, then BranchManager's playerExteriorSpawnPoint.
    /// </summary>
    public (Vector3 spawnPos, Vector3 lookAtPos) GetExteriorViewpoint(BranchTier tier)
    {
        Transform searchRoot = activeBuildingInstance != null ? activeBuildingInstance.transform : (tier != null && tier.sceneBuildingRoot != null ? tier.sceneBuildingRoot.transform : (buildingContainer != null ? buildingContainer : transform));
        Vector3 branchOrigin = searchRoot != null ? searchRoot.position : transform.position;
        Vector3 defaultLookTarget = branchOrigin + Vector3.up * lookAtHeightOffset;

        // 1. Check if Tier has a direct exteriorSpawnPoint assigned
        if (tier != null && tier.exteriorSpawnPoint != null)
        {
            Vector3 sPos = tier.exteriorSpawnPoint.position;
            Vector3 lPos = tier.lookTarget != null ? tier.lookTarget.position : defaultLookTarget;
            return (sPos, lPos);
        }

        // 2. Check if BranchManager has a global playerExteriorSpawnPoint assigned in Inspector
        if (playerExteriorSpawnPoint != null)
        {
            Vector3 sPos = playerExteriorSpawnPoint.position;
            Vector3 lPos = playerLookTarget != null ? playerLookTarget.position : defaultLookTarget;
            return (sPos, lPos);
        }

        // 3. Check if active building prefab or scene root has an ExteriorViewpoint child anchor
        if (searchRoot != null)
        {
            Transform[] allChildren = searchRoot.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in allChildren)
            {
                if (child == searchRoot) continue;
                string n = child.name;
                if (n.IndexOf("Exterior", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Outside", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Spawn", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Entrance", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Viewpoint", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return (child.position, defaultLookTarget);
                }
            }
        }

        // 4. Procedural outside viewpoint in front of building
        Vector3 forwardDir = searchRoot != null ? searchRoot.forward : transform.forward;
        Vector3 offset = (tier != null && tier.customExteriorOffset != Vector3.zero) ? tier.customExteriorOffset : defaultExteriorOffset;
        Vector3 rawSpawnPos;

        if (offset != Vector3.zero)
        {
            rawSpawnPos = branchOrigin + (searchRoot != null ? searchRoot.TransformDirection(offset) : offset);
        }
        else
        {
            rawSpawnPos = branchOrigin - (forwardDir * autoExteriorDistance);
        }

        // Raycast to snap cleanly to ground
        Vector3 safeSpawnPos = rawSpawnPos;
        if (Physics.Raycast(rawSpawnPos + Vector3.up * 8f, Vector3.down, out RaycastHit hit, 20f, ~0, QueryTriggerInteraction.Ignore))
        {
            safeSpawnPos = hit.point + Vector3.up * 0.1f;
        }

        return (safeSpawnPos, defaultLookTarget);
    }

    [ContextMenu("Create Exterior Spawn Point Anchor in Scene")]
    public void CreateExteriorSpawnPointAnchor()
    {
        Transform existing = transform.Find("Player_Exterior_SpawnPoint");
        if (existing == null)
        {
            GameObject spawnObj = new GameObject("Player_Exterior_SpawnPoint");
            spawnObj.transform.SetParent(transform, false);
            spawnObj.transform.localPosition = defaultExteriorOffset != Vector3.zero ? defaultExteriorOffset : new Vector3(0f, 0f, -autoExteriorDistance);
            spawnObj.transform.localRotation = Quaternion.identity;
            existing = spawnObj.transform;
        }

        playerExteriorSpawnPoint = existing;
        Debug.Log($"<color=#32FF64>[BranchManager] Spawn Point Anchor created and assigned: '{existing.name}'</color>");
    }

    private void OnDrawGizmosSelected()
    {
        // Visual Gizmo for exterior spawn point in Scene view
        Vector3 sPos = Vector3.zero;
        Vector3 lPos = Vector3.zero;
        bool hasPoint = false;

        BranchTier current = CurrentTier;
        if (current != null && current.exteriorSpawnPoint != null)
        {
            sPos = current.exteriorSpawnPoint.position;
            lPos = current.lookTarget != null ? current.lookTarget.position : (transform.position + Vector3.up * lookAtHeightOffset);
            hasPoint = true;
        }
        else if (playerExteriorSpawnPoint != null)
        {
            sPos = playerExteriorSpawnPoint.position;
            lPos = playerLookTarget != null ? playerLookTarget.position : (transform.position + Vector3.up * lookAtHeightOffset);
            hasPoint = true;
        }
        else
        {
            (sPos, lPos) = GetExteriorViewpoint(current);
            hasPoint = true;
        }

        if (hasPoint)
        {
            // Draw spawn position sphere
            Gizmos.color = new Color(0.1f, 0.9f, 1.0f, 0.85f);
            Gizmos.DrawWireSphere(sPos + Vector3.up * 0.9f, 0.45f);
            Gizmos.DrawLine(sPos, sPos + Vector3.up * 1.8f);

            // Draw line to look target
            Gizmos.color = new Color(0.2f, 1.0f, 0.4f, 0.75f);
            Gizmos.DrawLine(sPos + Vector3.up * 1.7f, lPos);
            Gizmos.DrawWireSphere(lPos, 0.35f);
        }
    }

    /// <summary>
    /// Attempts to upgrade the branch to the next tier. Deducts money, plays transition screen, applies visual transformations, and teleports player outside.
    /// </summary>
    public bool TryUpgradeBranch(bool withTransition = true)
    {
        if (!HasNextTier)
        {
            Debug.Log("[BranchManager] Branch is already at maximum tier!");
            return false;
        }

        BranchTier next = NextTier;
        int balance = PlayerEconomyManager.Instance != null ? PlayerEconomyManager.Instance.CurrentLiveBalance : 0;

        if (balance < next.upgradeCost)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayError();
            }
            Debug.LogWarning($"[BranchManager] Insufficient balance! Required: ${next.upgradeCost}, Balance: ${balance}");
            return false;
        }

        // Deduct Upgrade Cost
        if (PlayerEconomyManager.Instance != null)
        {
            PlayerEconomyManager.Instance.SpendMoney(next.upgradeCost);
        }

        int oldLevel = currentBranchLevel;
        currentBranchLevel++;
        SaveBranchLevel();

        BranchTier upgradedTier = CurrentTier;

        // Check if cinematic transition UI should play
        BranchUpgradeTransitionUI transitionUI = BranchUpgradeTransitionUI.Instance;
        if (transitionUI == null) transitionUI = UnityEngine.Object.FindAnyObjectByType<BranchUpgradeTransitionUI>();

        if (withTransition && enableUpgradeTransition && Application.isPlaying)
        {
            if (transitionUI != null)
            {
                transitionUI.PlayUpgradeSequence(
                    oldLevel,
                    currentBranchLevel,
                    upgradedTier,
                    onBlackoutAction: () =>
                    {
                        // 1. Swap building visuals under blackout
                        ApplyTierVisuals(false);

                        // 2. Relocate player outside and aim camera at the new branch
                        (Vector3 spawnPos, Vector3 lookAtPos) = GetExteriorViewpoint(upgradedTier);
                        FPSPlayerController player = FPSPlayerController.Instance != null ? FPSPlayerController.Instance : UnityEngine.Object.FindAnyObjectByType<FPSPlayerController>();
                        if (player != null)
                        {
                            player.TeleportAndLookAt(spawnPos, lookAtPos);
                        }

                        Debug.Log($"<color=#32FFFF>[BRANCH UPGRADED] Level {currentBranchLevel} ({upgradedTier.tierName})! Player relocated to outside viewpoint: {spawnPos}</color>");
                        OnBranchUpgraded?.Invoke(currentBranchLevel, upgradedTier);
                    }
                );
                return true;
            }
        }

        // Fallback immediate upgrade (e.g. In Edit mode or if transitions disabled)
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayLevelUp();
        }

        ApplyTierVisuals(false);

        (Vector3 fallbackSpawn, Vector3 fallbackLook) = GetExteriorViewpoint(upgradedTier);
        FPSPlayerController fallbackPlayer = FPSPlayerController.Instance != null ? FPSPlayerController.Instance : UnityEngine.Object.FindAnyObjectByType<FPSPlayerController>();
        if (fallbackPlayer != null)
        {
            fallbackPlayer.TeleportAndLookAt(fallbackSpawn, fallbackLook);
        }

        Debug.Log($"<color=#32FFFF>[BRANCH UPGRADED] Level {currentBranchLevel} ({upgradedTier.tierName})!</color>");
        OnBranchUpgraded?.Invoke(currentBranchLevel, upgradedTier);

        return true;
    }

    public void ApplyTierVisuals(bool respawnPackages = false)
    {
        EnsureReferences();
        BranchTier current = CurrentTier;
        if (current == null) return;

        Transform parent = buildingContainer != null ? buildingContainer : transform;

        // 1. If using branchPrefab, clean any pre-existing editor preview objects under parent
        if (current.branchPrefab != null)
        {
            // Safety: Rescue all existing cargo packages so upgrading the branch NEVER deletes generated packages!
            PhysicalCargoPackage[] existingPkgs = parent.GetComponentsInChildren<PhysicalCargoPackage>(true);
            foreach (var pkg in existingPkgs)
            {
                if (pkg != null)
                {
                    pkg.transform.SetParent(null, true); // Keep exact world position and rotation!
                }
            }

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (activeBuildingInstance != null && child.gameObject == activeBuildingInstance)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            if (activeBuildingInstance != null)
            {
                if (Application.isPlaying) Destroy(activeBuildingInstance);
                else DestroyImmediate(activeBuildingInstance);
            }

            activeBuildingInstance = Instantiate(current.branchPrefab, parent);
            activeBuildingInstance.transform.localPosition = current.branchPrefab.transform.localPosition;
            activeBuildingInstance.transform.localRotation = current.branchPrefab.transform.localRotation;
            activeBuildingInstance.transform.localScale = current.branchPrefab.transform.localScale;
        }
        else
        {
            // Manage Direct Scene Building Roots (if assigned without prefabs)
            foreach (var t in branchTiers)
            {
                if (t.sceneBuildingRoot != null)
                {
                    t.sceneBuildingRoot.SetActive(t.tierLevel == currentBranchLevel);
                }
            }
        }

        // 2. Update Terminal visuals if present
        BranchUpgradeTerminal activeTerm = GetActiveUpgradeTerminal();
        if (activeTerm != null)
        {
            activeTerm.UpdateTerminalVisuals();
        }
    }

    public void SaveBranchLevel()
    {
        PlayerPrefs.SetInt(PREFS_BRANCH_LEVEL, currentBranchLevel);
        PlayerPrefs.Save();
    }

    public void LoadBranchLevel()
    {
        currentBranchLevel = PlayerPrefs.GetInt(PREFS_BRANCH_LEVEL, 1);
        if (currentBranchLevel < 1) currentBranchLevel = 1;
    }

    [ContextMenu("Reset Branch Progression (Dev)")]
    public void ResetBranchProgression()
    {
        currentBranchLevel = 1;
        SaveBranchLevel();
        ApplyTierVisuals(false);
        OnBranchReset?.Invoke();
        Debug.Log("<color=#FF3333>[DEV] BRANCH PROGRESSION RESET TO LEVEL 1!</color>");
    }

    // =========================================================================
    // CENTRALIZED CARGO QUERY & CONFIGURATION HELPERS
    // =========================================================================

    /// <summary>
    /// Selects an unlocked cargo type using weighted random probability based on the current branch level.
    /// </summary>
    public CargoType DetermineRandomCargoType(int branchLevel = -1)
    {
        int level = branchLevel > 0 ? branchLevel : currentBranchLevel;
        List<(CargoType type, float weight)> available = new List<(CargoType, float)>();

        if (level >= standardRequiredLevel && standardSpawnWeight > 0f)
        {
            available.Add((CargoType.Standard, standardSpawnWeight));
        }

        if (level >= fragileRequiredLevel && fragileSpawnWeight > 0f)
        {
            available.Add((CargoType.Fragile, fragileSpawnWeight));
        }

        if (level >= expressRequiredLevel && expressSpawnWeight > 0f)
        {
            available.Add((CargoType.Express, expressSpawnWeight));
        }

        if (level >= explosiveRequiredLevel && explosiveSpawnWeight > 0f)
        {
            available.Add((CargoType.Explosive, explosiveSpawnWeight));
        }

        if (available.Count == 0)
        {
            return CargoType.Standard;
        }

        float totalWeight = 0f;
        for (int i = 0; i < available.Count; i++)
        {
            totalWeight += available[i].weight;
        }

        if (totalWeight <= 0f)
        {
            return CargoType.Standard;
        }

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = 0f;

        for (int i = 0; i < available.Count; i++)
        {
            cumulative += available[i].weight;
            if (roll <= cumulative)
            {
                return available[i].type;
            }
        }

        return available[0].type;
    }

    /// <summary>
    /// Selects the 3D prefab model suitable for the given cargo type.
    /// </summary>
    public GameObject GetPrefabForCargoType(CargoType type)
    {
        if (type == CargoType.Fragile && fragilePackagePrefabs != null && fragilePackagePrefabs.Count > 0)
        {
            var valid = fragilePackagePrefabs.FindAll(p => p != null);
            if (valid.Count > 0) return valid[UnityEngine.Random.Range(0, valid.Count)];
        }
        else if (type == CargoType.Express && expressPackagePrefabs != null && expressPackagePrefabs.Count > 0)
        {
            var valid = expressPackagePrefabs.FindAll(p => p != null);
            if (valid.Count > 0) return valid[UnityEngine.Random.Range(0, valid.Count)];
        }
        else if (type == CargoType.Explosive && explosivePackagePrefabs != null && explosivePackagePrefabs.Count > 0)
        {
            var valid = explosivePackagePrefabs.FindAll(p => p != null);
            if (valid.Count > 0) return valid[UnityEngine.Random.Range(0, valid.Count)];
        }

        if (packagePrefabs != null && packagePrefabs.Count > 0)
        {
            var valid = packagePrefabs.FindAll(p => p != null);
            if (valid.Count > 0) return valid[UnityEngine.Random.Range(0, valid.Count)];
        }

        if (cargoPackagePrefab != null)
        {
            return cargoPackagePrefab;
        }

        return null;
    }

    /// <summary>
    /// Selects the cardboard material suitable for the specified cargo type.
    /// </summary>
    public Material GetMaterialForCargoType(CargoType type)
    {
        if (type == CargoType.Fragile && fragileMaterials != null && fragileMaterials.Count > 0)
        {
            var valid = fragileMaterials.FindAll(m => m != null);
            if (valid.Count > 0) return valid[UnityEngine.Random.Range(0, valid.Count)];
        }
        else if (type == CargoType.Express && expressMaterials != null && expressMaterials.Count > 0)
        {
            var valid = expressMaterials.FindAll(m => m != null);
            if (valid.Count > 0) return valid[UnityEngine.Random.Range(0, valid.Count)];
        }
        else if (type == CargoType.Explosive && explosiveMaterials != null && explosiveMaterials.Count > 0)
        {
            var valid = explosiveMaterials.FindAll(m => m != null);
            if (valid.Count > 0) return valid[UnityEngine.Random.Range(0, valid.Count)];
        }

        if (cardboardMaterials != null && cardboardMaterials.Count > 0)
        {
            var valid = cardboardMaterials.FindAll(m => m != null);
            if (valid.Count > 0) return valid[UnityEngine.Random.Range(0, valid.Count)];
        }

        return null;
    }

    public struct CargoScaleConfig
    {
        public bool isRandom;
        public float fixedScale;
        public float minScale;
        public float maxScale;
        public bool enableElongated;
        public float elongatedChance;
        public float elongatedMinMultiplier;
        public float elongatedMaxMultiplier;
        public ElongatedAxisMode elongatedAxisMode;
    }

    /// <summary>
    /// Returns all scale and elongated configuration settings for the specified cargo type.
    /// </summary>
    public CargoScaleConfig GetScaleConfigForCargoType(CargoType type)
    {
        switch (type)
        {
            case CargoType.Fragile:
                return new CargoScaleConfig
                {
                    isRandom = fragileRandomScale,
                    fixedScale = fragileFixedScale,
                    minScale = fragileMinScale,
                    maxScale = fragileMaxScale,
                    enableElongated = fragileEnableElongated,
                    elongatedChance = fragileElongatedChance,
                    elongatedMinMultiplier = fragileElongatedMinMultiplier,
                    elongatedMaxMultiplier = fragileElongatedMaxMultiplier,
                    elongatedAxisMode = fragileElongatedAxisMode
                };
            case CargoType.Express:
                return new CargoScaleConfig
                {
                    isRandom = expressRandomScale,
                    fixedScale = expressFixedScale,
                    minScale = expressMinScale,
                    maxScale = expressMaxScale,
                    enableElongated = expressEnableElongated,
                    elongatedChance = expressElongatedChance,
                    elongatedMinMultiplier = expressElongatedMinMultiplier,
                    elongatedMaxMultiplier = expressElongatedMaxMultiplier,
                    elongatedAxisMode = expressElongatedAxisMode
                };
            case CargoType.Explosive:
                return new CargoScaleConfig
                {
                    isRandom = explosiveRandomScale,
                    fixedScale = explosiveFixedScale,
                    minScale = explosiveMinScale,
                    maxScale = explosiveMaxScale,
                    enableElongated = explosiveEnableElongated,
                    elongatedChance = explosiveElongatedChance,
                    elongatedMinMultiplier = explosiveElongatedMinMultiplier,
                    elongatedMaxMultiplier = explosiveElongatedMaxMultiplier,
                    elongatedAxisMode = explosiveElongatedAxisMode
                };
            case CargoType.Standard:
            default:
                return new CargoScaleConfig
                {
                    isRandom = standardRandomScale,
                    fixedScale = standardFixedScale,
                    minScale = standardMinScale,
                    maxScale = standardMaxScale,
                    enableElongated = standardEnableElongated,
                    elongatedChance = standardElongatedChance,
                    elongatedMinMultiplier = standardElongatedMinMultiplier,
                    elongatedMaxMultiplier = standardElongatedMaxMultiplier,
                    elongatedAxisMode = standardElongatedAxisMode
                };
        }
    }

    /// <summary>
    /// Returns scale settings (isRandom, fixedScale, minScale, maxScale) for the specified cargo type.
    /// </summary>
    public (bool isRandom, float fixedScale, float minScale, float maxScale) GetScaleSettingsForCargoType(CargoType type)
    {
        var cfg = GetScaleConfigForCargoType(type);
        return (cfg.isRandom, cfg.fixedScale, cfg.minScale, cfg.maxScale);
    }

    /// <summary>
    /// Returns the scale multiplier min/max range for the specified cargo type.
    /// </summary>
    public (float minScale, float maxScale) GetScaleRangeForCargoType(CargoType type)
    {
        var cfg = GetScaleConfigForCargoType(type);
        return (cfg.minScale, cfg.maxScale);
    }

    /// <summary>
    /// Computes the final localScale for a cargo package based on base scale, cargo type min/max limits,
    /// and type-specific single-axis elongation (ince-uzun kargo boyutu).
    /// </summary>
    public Vector3 CalculateCargoScale(CargoType type, Vector3 baseScale)
    {
        if (baseScale == Vector3.zero) baseScale = Vector3.one;

        if (!enablePrefabScaling) return baseScale;

        CargoScaleConfig cfg = GetScaleConfigForCargoType(type);
        Vector3 finalScale;

        if (cfg.isRandom)
        {
            float minS = Mathf.Min(cfg.minScale, cfg.maxScale);
            float maxS = Mathf.Max(cfg.minScale, cfg.maxScale);

            if (minS > 0f && maxS > 0f)
            {
                if (randomizeAxesIndependently)
                {
                    float rx = UnityEngine.Random.Range(minS, maxS);
                    float ry = UnityEngine.Random.Range(minS, maxS);
                    float rz = UnityEngine.Random.Range(minS, maxS);
                    finalScale = new Vector3(baseScale.x * rx, baseScale.y * ry, baseScale.z * rz);
                }
                else
                {
                    float uniformScale = UnityEngine.Random.Range(minS, maxS);
                    finalScale = baseScale * uniformScale;
                }
            }
            else
            {
                finalScale = baseScale;
            }
        }
        else
        {
            float targetScale = cfg.fixedScale > 0f ? cfg.fixedScale : 1.0f;
            finalScale = baseScale * targetScale;
        }

        // Apply type-specific single-axis elongation variation if enabled and chance rolled
        if (cfg.enableElongated && cfg.elongatedChance > 0f && UnityEngine.Random.Range(0f, 100f) <= cfg.elongatedChance)
        {
            float minMult = Mathf.Min(cfg.elongatedMinMultiplier, cfg.elongatedMaxMultiplier);
            float maxMult = Mathf.Max(cfg.elongatedMinMultiplier, cfg.elongatedMaxMultiplier);
            float stretchFactor = UnityEngine.Random.Range(minMult, maxMult);

            switch (cfg.elongatedAxisMode)
            {
                case ElongatedAxisMode.Forward_Z_Only:
                    finalScale.z *= stretchFactor;
                    break;
                case ElongatedAxisMode.Width_X_Only:
                    finalScale.x *= stretchFactor;
                    break;
                case ElongatedAxisMode.Height_Y_Only:
                    finalScale.y *= stretchFactor;
                    break;
                case ElongatedAxisMode.HorizontalOnly_XZ:
                    if (UnityEngine.Random.value < 0.5f) finalScale.z *= stretchFactor;
                    else finalScale.x *= stretchFactor;
                    break;
                case ElongatedAxisMode.Random_AnyAxis:
                default:
                    int axis = UnityEngine.Random.Range(0, 3);
                    if (axis == 0) finalScale.x *= stretchFactor;
                    else if (axis == 1) finalScale.y *= stretchFactor;
                    else finalScale.z *= stretchFactor;
                    break;
            }
        }

        return finalScale;
    }

    /// <summary>
    /// Generates a random delivery cutoff hour for express shipments within min and max interval.
    /// </summary>
    public float GenerateRandomExpressDeliveryHour()
    {
        int minTotalMins = Mathf.RoundToInt(Mathf.Min(minExpressDeliveryHour, maxExpressDeliveryHour) * 60f);
        int maxTotalMins = Mathf.RoundToInt(Mathf.Max(minExpressDeliveryHour, maxExpressDeliveryHour) * 60f);
        int step = Mathf.Max(5, expressMinuteInterval);

        int stepsCount = Mathf.Max(1, (maxTotalMins - minTotalMins) / step);
        int chosenStep = UnityEngine.Random.Range(0, stepsCount + 1);
        int chosenTotalMins = minTotalMins + (chosenStep * step);
        chosenTotalMins = Mathf.Clamp(chosenTotalMins, minTotalMins, maxTotalMins);

        return chosenTotalMins / 60f;
    }
}
