using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gas Canister Shelf / Stand (Benzin Bidonu Rafı).
/// Spawns the Gas_Can prefab directly at each defined canisterSlots position with the specified rotation.
/// When the player picks up a canister, the purchase price is deducted.
/// When the player moves away beyond respawnDistance, empty slots are restocked.
/// </summary>
public class FuelCanisterShelf : MonoBehaviour
{
    [Header("--- 🛢️ SHELF SETTINGS ---")]
    public string shelfDisplayName = "Benzin Bidonu Standı";
    public int canisterPrice = 100;
    public GameObject canisterPrefab;

    [Header("--- 📍 SLOTS & AUTO-RESPAWN ---")]
    [Tooltip("Defined slot transforms on the shelf where gas cans spawn")]
    public Transform[] canisterSlots;

    [Tooltip("Rotation applied to gas cans when placed on the shelf (Default: x -90, y 0, z -180)")]
    public Vector3 shelfCanisterRotation = new Vector3(-90f, 0f, -180f);

    [Tooltip("Distance in meters the player must move away for empty slots to restock")]
    public float respawnDistance = 18f;

    [Header("--- 🎮 INTERACTION ---")]
    public float interactionDistance = 2.8f;
    public Key interactionKey = Key.E;

    private readonly List<GameObject> activeCanisters = new List<GameObject>();
    private readonly List<Vector3> slotPositions = new List<Vector3>();

    public static readonly List<FuelCanisterShelf> AllShelves = new List<FuelCanisterShelf>();

    public GameObject GetCanisterPrefab()
    {
        if (canisterPrefab != null) return canisterPrefab;

#if UNITY_EDITOR
        GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_AssetPacks/ExplosivesPackage/Prefabs/Gas_Can.prefab");
        if (prefab != null) return prefab;
#endif
        GameObject res = Resources.Load<GameObject>("Prefabs/Gas_Can");
        if (res != null) return res;

        return Resources.Load<GameObject>("Prefabs/Gas_Canister");
    }

    private void Awake()
    {
        if (canisterPrefab == null)
        {
            canisterPrefab = GetCanisterPrefab();
        }
    }

    private void OnEnable()
    {
        if (!AllShelves.Contains(this)) AllShelves.Add(this);
    }

    private void OnDisable()
    {
        AllShelves.Remove(this);
    }

    private void Start()
    {
        InitializeShelf();
    }

    public void InitializeShelf()
    {
        slotPositions.Clear();
        activeCanisters.Clear();

        // 1. Gather slot positions
        if (canisterSlots != null && canisterSlots.Length > 0)
        {
            for (int i = 0; i < canisterSlots.Length; i++)
            {
                if (canisterSlots[i] != null)
                {
                    slotPositions.Add(canisterSlots[i].position);
                }
            }
        }
        else
        {
            // Auto-detect children positions (markers or existing cans)
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                string n = child.name.ToLower();
                if (n.Contains("slot") || n.Contains("spawn") || n.Contains("pos") || n.Contains("point") || n.Contains("gas_can"))
                {
                    slotPositions.Add(child.position);
                }
            }
        }

        // Clean up any old existing Gas_Can children so we spawn fresh prefab instances
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name.ToLower().Contains("gas_can"))
            {
                Destroy(child.gameObject);
            }
        }

        // 2. Spawn the GasCan prefab at each slot position with rotation
        for (int i = 0; i < slotPositions.Count; i++)
        {
            GameObject spawned = SpawnCanister(i);
            activeCanisters.Add(spawned);
        }
    }

    private GameObject SpawnCanister(int slotIndex)
    {
        GameObject prefab = GetCanisterPrefab();
        if (prefab == null || slotIndex >= slotPositions.Count) return null;

        Vector3 pos = slotPositions[slotIndex];
        Quaternion rot = Quaternion.Euler(shelfCanisterRotation);

        // Spawn with identity, then FORCE rotation to exactly shelfCanisterRotation.
        // This ignores any rotation baked into the prefab root — only the Inspector
        // value from FuelCanisterShelf is used.
        GameObject canObj = Instantiate(prefab, pos, Quaternion.identity);
        canObj.transform.rotation = Quaternion.Euler(shelfCanisterRotation);

        // Sit on shelf until picked up
        Rigidbody rb = canObj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        FuelCanisterItem item = canObj.GetComponent<FuelCanisterItem>();
        if (item != null)
        {
            item.isForSaleOnShelf = true;
            item.shelfOwner = this;
            item.slotIndex = slotIndex;
        }

        return canObj;
    }

    private void Update()
    {
        CheckAutoRestock();
    }

    private void CheckAutoRestock()
    {
        if (slotPositions.Count == 0) return;

        Camera cam = (FPSPlayerController.Instance != null && FPSPlayerController.Instance.playerCamera != null) ? FPSPlayerController.Instance.playerCamera : Camera.main;
        if (cam == null) return;

        float dist = Vector3.Distance(transform.position, cam.transform.position);

        // Refill empty slots when player is far away
        if (dist >= respawnDistance)
        {
            for (int i = 0; i < slotPositions.Count; i++)
            {
                if (i >= activeCanisters.Count || activeCanisters[i] == null)
                {
                    GameObject spawned = SpawnCanister(i);
                    if (i < activeCanisters.Count)
                    {
                        activeCanisters[i] = spawned;
                    }
                    else
                    {
                        activeCanisters.Add(spawned);
                    }
                }
            }
        }
    }

    private float GetCanisterFuelAmount()
    {
        GameObject prefab = GetCanisterPrefab();
        FuelCanisterItem item = prefab != null ? prefab.GetComponent<FuelCanisterItem>() : null;
        return item != null ? item.fuelAmount : 10f;
    }

    public string GetPromptText()
    {
        string keyName = interactionKey != Key.None ? interactionKey.ToString() : "E";
        return LocalizationManager.GetFormat("prompt_gas_canister_shelf", GetDisplayName(), keyName, canisterPrice, GetCanisterFuelAmount());
    }

    /// <summary>
    /// When player picks up this gas can, deducts the price and gives it into hands.
    /// </summary>
    public bool TryBuyCanister(FuelCanisterItem canister = null)
    {
        if (canister == null)
        {
            for (int i = 0; i < activeCanisters.Count; i++)
            {
                if (activeCanisters[i] != null)
                {
                    canister = activeCanisters[i].GetComponent<FuelCanisterItem>();
                    if (canister != null) break;
                }
            }
        }

        if (canister == null) return false;

        // Check money
        int balance = PlayerEconomyManager.Instance != null ? PlayerEconomyManager.Instance.CurrentLiveBalance : 99999;
        if (balance < canisterPrice && canisterPrice > 0)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayError();
            if (InteractionPromptHUD.Instance != null)
            {
                InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.GetFormat("prompt_gas_station_no_money_canister", canisterPrice, balance));
            }
            return false;
        }

        // Deduct price
        if (PlayerEconomyManager.Instance != null && canisterPrice > 0)
        {
            PlayerEconomyManager.Instance.SpendMoney(canisterPrice);
            PlayerEconomyManager.Instance.SaveLiveBalance();
        }

        // Release from shelf slot
        int idx = canister.slotIndex;
        if (idx >= 0 && idx < activeCanisters.Count && activeCanisters[idx] == canister.gameObject)
        {
            activeCanisters[idx] = null;
        }

        canister.isForSaleOnShelf = false;
        canister.shelfOwner = null;
        canister.slotIndex = -1;

        // Detach from shelf and enable physics
        canister.transform.SetParent(null, true);
        Rigidbody rb = canister.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }

        // Grab into hands
        if (PhysicsGrabber.Instance != null && rb != null)
        {
            if (PhysicsGrabber.Instance.IsHoldingObject)
            {
                PhysicsGrabber.Instance.ReleaseObject();
            }
            PhysicsGrabber.Instance.GrabObject(rb);
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlayMoneySubtract();

        return true;
    }

    public string GetDisplayName()
    {
        if (string.IsNullOrEmpty(shelfDisplayName) || shelfDisplayName == "Benzin Bidonu Standı" || shelfDisplayName == "Gas Canister Shelf")
        {
            return LocalizationManager.Get("shelf_gas_canister_name", "Benzin Bidonu Standı");
        }
        return shelfDisplayName;
    }
}
