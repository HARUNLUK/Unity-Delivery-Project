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
                    break;
                case 2:
                    if (string.IsNullOrEmpty(t.tierName) || t.tierName.Contains("Şube") || t.tierName.Contains("Lojistik") || t.tierName.Contains("Orta"))
                    {
                        t.tierName = "Regional Hub";
                        t.description = "Expanded logistics hub with improved daily parcel limits and high earnings potential.";
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
                dailyPackageCapacity = 4,
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
}
