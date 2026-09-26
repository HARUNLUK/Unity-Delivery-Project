using UnityEngine;

/// <summary>
/// Marks a spot where a carriable item can be found (a weapon lying in a shed, behind a house, ...).
/// Spawns one of the given prefabs when the scene starts, with a chance.
/// </summary>
public class CarriableItemSpawnPoint : MonoBehaviour
{
    [Tooltip("Prefabs that can appear here (one is picked at random).")]
    public GameObject[] itemPrefabs;

    [Range(0f, 1f)]
    [Tooltip("Chance that something is lying here when the scene starts.")]
    public float spawnChance = 1f;

    [Tooltip("Spawn again on every new day when the previous item is gone (destroyed).")]
    public bool respawnEachDay = false;

    private GameObject spawned;

    private void OnEnable()
    {
        DayTimeManager.OnDayAdvanced += HandleDayAdvanced;
    }

    private void OnDisable()
    {
        DayTimeManager.OnDayAdvanced -= HandleDayAdvanced;
    }

    private void Start()
    {
        TrySpawn();
    }

    private void HandleDayAdvanced(int day)
    {
        if (respawnEachDay && spawned == null) TrySpawn();
    }

    private void TrySpawn()
    {
        if (spawned != null || itemPrefabs == null || itemPrefabs.Length == 0) return;
        if (Random.value > spawnChance) return;

        GameObject prefab = itemPrefabs[Random.Range(0, itemPrefabs.Length)];
        if (prefab == null) return;

        spawned = Instantiate(prefab, transform.position + Vector3.up * 0.15f, transform.rotation);
        spawned.name = prefab.name;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.75f, 0.2f, 0.9f);
        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.1f, new Vector3(0.5f, 0.2f, 0.25f));
        Gizmos.DrawSphere(transform.position + Vector3.up * 0.4f, 0.06f);
    }
}
