#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BranchSetupHelper
{
    [MenuItem("Tools/Delivery Game/Setup Branch Manager", false, 48)]
    public static void SetupBranchManagerInScene()
    {
        // 1. Find or create Branch_Manager root
        BranchManager bm = Object.FindAnyObjectByType<BranchManager>();
        GameObject branchObj;
        if (bm != null)
        {
            branchObj = bm.gameObject;
        }
        else
        {
            branchObj = new GameObject("Branch_Manager");
            branchObj.transform.position = Vector3.zero;
            bm = branchObj.AddComponent<BranchManager>();
            Undo.RegisterCreatedObjectUndo(branchObj, "Created Branch Manager");
        }

        // Clean any extra generated child objects under Branch_Manager if any exist
        for (int i = branchObj.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = branchObj.transform.GetChild(i);
            Object.DestroyImmediate(child.gameObject);
        }

        // 2. Ensure default tiers are populated
        if (bm.branchTiers == null || bm.branchTiers.Count == 0)
        {
            bm.PopulateDefaultTiers();
        }

        Selection.activeGameObject = branchObj;
        EditorUtility.SetDirty(branchObj);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        Debug.Log("<color=#32FF64>[BranchSetupHelper] Branch_Manager successfully created and configured.</color>");
    }
}
#endif
