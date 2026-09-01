using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class SimpleRoadBuilder : MonoBehaviour
{
    [Header("--- ROAD PREFABS ---")]
    public GameObject straightRoadPrefab;
    public GameObject cornerRoadPrefab;
    public GameObject crossRoadPrefab;
    public GameObject sideRoadPrefab;
    public GameObject endRoadPrefab;

    [Header("--- SETTINGS ---")]
    [Tooltip("Distance between road segments in units")]
    public float segmentLength = 20.0f;

    [Tooltip("Snap newly placed road piece onto the terrain height")]
    public bool snapToTerrain = true;

    [Tooltip("Height offset above terrain to prevent z-fighting")]
    public float terrainOffset = 0.05f;

    [Header("--- ROAD HIERARCHY ---")]
    public Transform roadContainer;

    [Header("--- LAST PLACED PIECE ---")]
    public GameObject lastPlacedPiece;

    public List<GameObject> placedRoads = new List<GameObject>();

    private void Reset()
    {
        AutoAssignPrefabs();
    }

    public void AutoAssignPrefabs()
    {
#if UNITY_EDITOR
        if (straightRoadPrefab == null)
            straightRoadPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Pandazole_Ultimate_Pack/Pandazole City Town Pack/Prefabs/Roads/Env_Road_Straight_01.prefab");

        if (cornerRoadPrefab == null)
            cornerRoadPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Pandazole_Ultimate_Pack/Pandazole City Town Pack/Prefabs/Roads/Env_Road_Cornor_01.prefab");

        if (crossRoadPrefab == null)
            crossRoadPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Pandazole_Ultimate_Pack/Pandazole City Town Pack/Prefabs/Roads/Env_Road_Cross_01.prefab");

        if (sideRoadPrefab == null)
            sideRoadPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Pandazole_Ultimate_Pack/Pandazole City Town Pack/Prefabs/Roads/Env_Road_Side_01.prefab");

        if (endRoadPrefab == null)
            endRoadPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Pandazole_Ultimate_Pack/Pandazole City Town Pack/Prefabs/Roads/Env_Road_End_01.prefab");
#endif
    }
}
