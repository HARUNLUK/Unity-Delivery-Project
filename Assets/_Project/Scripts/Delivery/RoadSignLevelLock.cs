using System;
using UnityEngine;
using TMPro;

[ExecuteAlways]
[AddComponentMenu("Delivery/Road Sign Level Lock")]
[SelectionBase]
public class RoadSignLevelLock : MonoBehaviour
{
    [Header("--- LEVEL REQUIREMENT ---")]
    [Tooltip("Minimum Player / Branch Level required for this road, district, or zone.")]
    [Range(1, 10)]
    public int requiredLevel = 2;

    [Header("--- TEXT DISPLAY & FORMATTING ---")]
    [Tooltip("Text format to display when locked. {0} will be replaced with the required level number.")]
    public string lockTextFormat = "LEVEL {0} REQUIRED";

    [Tooltip("Color of the required level lock text.")]
    public Color lockTextColor = new Color(1f, 0.25f, 0.25f, 1f); // Warning red

    [Header("--- TARGET REFERENCES (OPTIONAL) ---")]
    [Tooltip("Direct reference to the 3D TextMeshPro component showing the lock requirement. If empty, will be auto-found or auto-created.")]
    public TextMeshPro lockTextMesh;

    [Tooltip("Optional parent badge / plate GameObject containing the lock visual. Will be toggled on/off based on unlock status.")]
    public GameObject lockVisualContainer;

    [Header("--- BEHAVIOR OPTIONS ---")]
    [Tooltip("If true, automatically creates a stylized sub-badge and 3D TextMeshPro under the sign if none is assigned.")]
    public bool autoCreateBadgeIfMissing = true;

    [Tooltip("If true, completely hides/deactivates the lock text and badge once the player reaches or exceeds the required level.")]
    public bool hideWhenUnlocked = true;

    [Header("--- EDITOR PREVIEW ---")]
    [Tooltip("In Edit Mode (outside Play Mode), check this to preview the sign in its UNLOCKED (clean) state.")]
    public bool previewUnlockedInEditor = false;

    public static event Action<RoadSignLevelLock, bool> OnSignLockStateChanged;

    /// <summary>
    /// Returns true if the player has reached or exceeded the required level.
    /// In Editor mode, respects the 'previewUnlockedInEditor' toggle.
    /// </summary>
    public bool IsUnlocked
    {
        get
        {
            if (!Application.isPlaying)
            {
                return previewUnlockedInEditor || requiredLevel <= 1;
            }
            return GetCurrentPlayerLevel() >= requiredLevel;
        }
    }

    private void Awake()
    {
        EnsureReferences();
    }

    private void Start()
    {
        EnsureReferences();
        UpdateVisuals();
    }

    private void OnEnable()
    {
        SubscribeEvents();
        UpdateVisuals();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void OnValidate()
    {
        UpdateVisuals();
    }

    private void SubscribeEvents()
    {
        BranchManager.OnBranchUpgraded += HandleBranchUpgraded;
        PlayerProgressionManager.OnLevelUp += HandleLevelUp;
        PlayerProgressionManager.OnWarehouseLevelUp += HandleLevelUp;
        PurchasableProperty.OnPropertyUnlocked += HandlePropertyUnlocked;
    }

    private void UnsubscribeEvents()
    {
        BranchManager.OnBranchUpgraded -= HandleBranchUpgraded;
        PlayerProgressionManager.OnLevelUp -= HandleLevelUp;
        PlayerProgressionManager.OnWarehouseLevelUp -= HandleLevelUp;
        PurchasableProperty.OnPropertyUnlocked -= HandlePropertyUnlocked;
    }

    private void HandleBranchUpgraded(int newLevel, BranchTier tier) => UpdateVisuals();
    private void HandleLevelUp(int newLevel) => UpdateVisuals();
    private void HandlePropertyUnlocked(PurchasableProperty prop) => UpdateVisuals();

    /// <summary>
    /// Fetches the current live player or branch level.
    /// </summary>
    public int GetCurrentPlayerLevel()
    {
        if (BranchManager.Instance != null)
        {
            return BranchManager.Instance.CurrentBranchLevel;
        }

        if (PlayerProgressionManager.Instance != null)
        {
            return PlayerProgressionManager.Instance.PlayerLevel;
        }

        return PlayerPrefs.GetInt("Delivery_BranchLevel", PlayerPrefs.GetInt("Delivery_PlayerLevel", 1));
    }

    /// <summary>
    /// Updates the lock badge/text visual state based on current level progress.
    /// </summary>
    public void UpdateVisuals()
    {
        EnsureReferences();

        bool unlocked = IsUnlocked;

        if (unlocked)
        {
            if (hideWhenUnlocked)
            {
                if (lockVisualContainer != null)
                {
                    lockVisualContainer.SetActive(false);
                }
                else if (lockTextMesh != null)
                {
                    lockTextMesh.gameObject.SetActive(false);
                }
            }
        }
        else
        {
            if (lockVisualContainer != null)
            {
                lockVisualContainer.SetActive(true);
            }

            if (lockTextMesh != null)
            {
                lockTextMesh.gameObject.SetActive(true);
                lockTextMesh.text = string.Format(lockTextFormat, requiredLevel);
                lockTextMesh.color = lockTextColor;
            }
        }

        OnSignLockStateChanged?.Invoke(this, unlocked);
    }

    /// <summary>
    /// Ensures that the lock visual references exist without altering or overwriting any user-defined sign texts.
    /// </summary>
    public void EnsureReferences()
    {
        // 1. If references already set, do not re-search
        if (lockTextMesh != null && lockVisualContainer != null) return;

        // 2. Search for existing lock badge in children
        Transform existingBadge = transform.Find("LevelRequiredBadge");
        if (existingBadge == null)
        {
            existingBadge = transform.Find("LockBadge");
        }
        if (existingBadge == null)
        {
            // Also check under child Cube if sign has standard hierarchy
            Transform cubeChild = transform.Find("Cube");
            if (cubeChild != null)
            {
                existingBadge = cubeChild.Find("LevelRequiredBadge");
            }
        }

        if (existingBadge != null)
        {
            lockVisualContainer = existingBadge.gameObject;
            if (lockTextMesh == null)
            {
                lockTextMesh = existingBadge.GetComponentInChildren<TextMeshPro>(true);
            }
            return;
        }

        // 3. Auto-create if enabled and in Editor or runtime
        if (autoCreateBadgeIfMissing && (lockTextMesh == null || lockVisualContainer == null))
        {
            CreateDefaultLockBadge();
        }
    }

    /// <summary>
    /// Creates a dedicated, non-intrusive sub-badge under the road sign board for the level lock requirement.
    /// Never modifies existing TextMeshPro texts on the sign.
    /// </summary>
    public void CreateDefaultLockBadge()
    {
        // Find suitable anchor (prefer Cube/Board child, fallback to this transform)
        Transform anchor = transform.Find("Cube");
        if (anchor == null) anchor = transform;

        // Create Container
        GameObject badgeObj = new GameObject("LevelRequiredBadge");
        badgeObj.transform.SetParent(anchor, false);

        // Position: Place neatly at bottom or top of sign board
        if (anchor != transform)
        {
            // Under local Cube coordinates
            badgeObj.transform.localPosition = new Vector3(0f, -0.6f, 0f);
            badgeObj.transform.localRotation = Quaternion.identity;
            badgeObj.transform.localScale = Vector3.one;
        }
        else
        {
            badgeObj.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            badgeObj.transform.localRotation = Quaternion.identity;
            badgeObj.transform.localScale = Vector3.one;
        }

        // Create Text GameObject
        GameObject textObj = new GameObject("LockText (TMP)");
        textObj.transform.SetParent(badgeObj.transform, false);

        // Find existing TMP font to maintain visual consistency
        TextMeshPro existingTmp = anchor.GetComponentInChildren<TextMeshPro>(true);

        // Match local rotation & orientation of existing TMP if present
        if (existingTmp != null)
        {
            textObj.transform.localRotation = existingTmp.transform.localRotation;
            textObj.transform.localPosition = new Vector3(existingTmp.transform.localPosition.x, existingTmp.transform.localPosition.y - 0.45f, existingTmp.transform.localPosition.z);
            textObj.transform.localScale = existingTmp.transform.localScale * 0.75f;
        }
        else
        {
            textObj.transform.localRotation = Quaternion.Euler(0, 90, 0);
            textObj.transform.localPosition = Vector3.zero;
            textObj.transform.localScale = Vector3.one * 0.1f;
        }

        TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
        if (existingTmp != null && existingTmp.font != null)
        {
            tmp.font = existingTmp.font;
            tmp.fontSharedMaterial = existingTmp.fontSharedMaterial;
        }

        tmp.text = string.Format(lockTextFormat, requiredLevel);
        tmp.color = lockTextColor;
        tmp.fontSize = 20f;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 10f;
        tmp.fontSizeMax = 32f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;

        lockVisualContainer = badgeObj;
        lockTextMesh = tmp;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorUtility.SetDirty(gameObject);
        }
#endif
    }
}
