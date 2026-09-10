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

    [Header("--- CARGO TYPE UNLOCK LEVELS & SPAWN CHANCES ---")]
    [Tooltip("Minimum branch level required for Standard cargo")]
    public int standardRequiredLevel = 1;
    [Range(0f, 100f), Tooltip("Standard cargo spawn weight / probability")]
    public float standardSpawnWeight = 50f;

    [Tooltip("Minimum branch level required for Fragile cargo")]
    public int fragileRequiredLevel = 5;
    [Range(0f, 100f), Tooltip("Fragile cargo spawn weight / probability")]
    public float fragileSpawnWeight = 30f;

    [Tooltip("Minimum branch level required for Express cargo")]
    public int expressRequiredLevel = 8;
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

    [Header("--- PREFAB SCALE MULTIPLIER PER CARGO TYPE ---")]
    [Tooltip("Apply scale multiplier to custom 3D prefabs")]
    public bool enablePrefabScaling = true;

    [Tooltip("Scale X, Y, Z axes independently with random multipliers")]
    public bool randomizeAxesIndependently = false;

    [Header("Standard Cargo Scale")]
    [Tooltip("If true, scales randomly between standardMinScale and standardMaxScale. If false, uses standardFixedScale.")]
    public bool standardRandomScale = true;
    [Range(0.05f, 2.5f), Tooltip("Fixed scale multiplier used when random scaling is disabled")]
    public float standardFixedScale = 0.45f;
    [Range(0.05f, 2.5f), Tooltip("Standard package minimum random scale multiplier")]
    public float standardMinScale = 0.35f;
    [Range(0.05f, 2.5f), Tooltip("Standard package maximum random scale multiplier")]
    public float standardMaxScale = 0.55f;

    [Header("Fragile Cargo Scale")]
    [Tooltip("If true, scales randomly between fragileMinScale and fragileMaxScale. If false, uses fragileFixedScale.")]
    public bool fragileRandomScale = true;
    [Range(0.05f, 2.5f), Tooltip("Fixed scale multiplier used when random scaling is disabled")]
    public float fragileFixedScale = 0.40f;
    [Range(0.05f, 2.5f), Tooltip("Fragile package minimum random scale multiplier")]
    public float fragileMinScale = 0.35f;
    [Range(0.05f, 2.5f), Tooltip("Fragile package maximum random scale multiplier")]
    public float fragileMaxScale = 0.50f;

    [Header("Express Cargo Scale")]
    [Tooltip("If true, scales randomly between expressMinScale and expressMaxScale. If false, uses expressFixedScale.")]
    public bool expressRandomScale = true;
    [Range(0.05f, 2.5f), Tooltip("Fixed scale multiplier used when random scaling is disabled")]
    public float expressFixedScale = 0.35f;
    [Range(0.05f, 2.5f), Tooltip("Express package minimum random scale multiplier")]
    public float expressMinScale = 0.30f;
    [Range(0.05f, 2.5f), Tooltip("Express package maximum random scale multiplier")]
    public float expressMaxScale = 0.45f;

    [Header("Explosive Cargo Scale")]
    [Tooltip("If true, scales randomly between explosiveMinScale and explosiveMaxScale. If false, uses explosiveFixedScale.")]
    public bool explosiveRandomScale = true;
    [Range(0.05f, 2.5f), Tooltip("Fixed scale multiplier used when random scaling is disabled")]
    public float explosiveFixedScale = 0.50f;
    [Range(0.05f, 2.5f), Tooltip("Explosive package minimum random scale multiplier")]
    public float explosiveMinScale = 0.40f;
    [Range(0.05f, 2.5f), Tooltip("Explosive package maximum random scale multiplier")]
    public float explosiveMaxScale = 0.60f;

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
        if (branchTiers == null || branchTiers.Count == 0) return;

        foreach (var t in branchTiers)
        {
            if (t == null) continue;
            switch (t.tierLevel)
            {
                case 1:
                    if (string.IsNullOrEmpty(t.tierName) || t.tierName.Contains("Kul") || t.tierName.Contains("Dağ") || t.tierName.Contains("Başlangıç"))
                    {
                        t.tierName = "Starter Garage";
                        t.description = "Entry-level parcel warehouse and garage. Basic package volume and low operating rent.";
                    }
                    if (t.dailyPackageCapacity == 10 || t.dailyPackageCapacity == 4)
                    {
                        t.dailyPackageCapacity = 5;
                    }
                    break;
                case 2:
                    if (string.IsNullOrEmpty(t.tierName) || t.tierName.Contains("Şube") || t.tierName.Contains("Lojistik") || t.tierName.Contains("Orta"))
                    {
                        t.tierName = "Regional Hub";
                        t.description = "Expanded logistics hub with improved daily parcel limits and high earnings potential.";
                    }
                    if (t.dailyPackageCapacity == 10)
                    {
                        t.dailyPackageCapacity = 8;
                    }
                    break;
                case 3:
                    if (string.IsNullOrEmpty(t.tierName) || t.tierName.Contains("Bölge") || t.tierName.Contains("Merkez") || t.tierName.Contains("Büyük"))
                    {
                        t.tierName = "District Distribution Center";
                        t.description = "Large scale logistics center with high yield for express and fragile shipments.";
                    }
                    break;
                case 4:
                    if (string.IsNullOrEmpty(t.tierName) || t.tierName.Contains("Kompleks") || t.tierName.Contains("Filo") || t.tierName.Contains("Mega Lojistik"))
                    {
                        t.tierName = "Mega Logistics Complex";
                        t.description = "Ultimate fleet management headquarters and maximum throughput capacity.";
                    }
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
                description = "Entry-level parcel warehouse and garage. Basic package volume and low operating rent.",
                upgradeCost = 0,
                requiredPlayerLevel = 1,
                dailyPackageCapacity = 5,
                dailyRent = 50
            },
            new BranchTier
            {
                tierLevel = 2,
                tierName = "Regional Hub",
                description = "Expanded logistics hub with improved daily parcel limits and high earnings potential.",
                upgradeCost = 1200,
                requiredPlayerLevel = 2,
                dailyPackageCapacity = 8,
                dailyRent = 120
            },
            new BranchTier
            {
                tierLevel = 3,
                tierName = "District Distribution Center",
                description = "Large scale logistics center with high yield for express and fragile shipments.",
                upgradeCost = 3500,
                requiredPlayerLevel = 4,
                dailyPackageCapacity = 14,
                dailyRent = 280
            },
            new BranchTier
            {
                tierLevel = 4,
                tierName = "Mega Logistics Complex",
                description = "Ultimate fleet management headquarters and maximum throughput capacity.",
                upgradeCost = 8000,
                requiredPlayerLevel = 6,
                dailyPackageCapacity = 22,
                dailyRent = 550
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
    /// Attempts to upgrade the branch to the next tier. Deducts money and applies visual transformations.
    /// </summary>
    public bool TryUpgradeBranch()
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
            Debug.LogWarning($"[BranchManager] Insufficient balance! Required: ${next.upgradeCost}, Balance: ${balance}");
            return false;
        }

        // Deduct Upgrade Cost
        if (PlayerEconomyManager.Instance != null)
        {
            PlayerEconomyManager.Instance.DeductCash(next.upgradeCost);
        }

        currentBranchLevel++;
        SaveBranchLevel();

        // Update visuals without altering user-designed generator positions
        ApplyTierVisuals(false);

        Debug.Log($"<color=#32FFFF>[BRANCH UPGRADED] Level {currentBranchLevel} ({CurrentTier.tierName})!</color>");
        OnBranchUpgraded?.Invoke(currentBranchLevel, CurrentTier);

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

    /// <summary>
    /// Returns scale settings (isRandom, fixedScale, minScale, maxScale) for the specified cargo type.
    /// </summary>
    public (bool isRandom, float fixedScale, float minScale, float maxScale) GetScaleSettingsForCargoType(CargoType type)
    {
        switch (type)
        {
            case CargoType.Fragile:
                return (fragileRandomScale, fragileFixedScale, fragileMinScale, fragileMaxScale);
            case CargoType.Express:
                return (expressRandomScale, expressFixedScale, expressMinScale, expressMaxScale);
            case CargoType.Explosive:
                return (explosiveRandomScale, explosiveFixedScale, explosiveMinScale, explosiveMaxScale);
            case CargoType.Standard:
            default:
                return (standardRandomScale, standardFixedScale, standardMinScale, standardMaxScale);
        }
    }

    /// <summary>
    /// Returns the scale multiplier min/max range for the specified cargo type.
    /// </summary>
    public (float minScale, float maxScale) GetScaleRangeForCargoType(CargoType type)
    {
        var settings = GetScaleSettingsForCargoType(type);
        return (settings.minScale, settings.maxScale);
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
