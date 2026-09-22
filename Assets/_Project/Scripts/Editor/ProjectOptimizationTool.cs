#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Comprehensive Performance & FPS Optimization Tool for Unity Car Project.
/// Provides one-click batch operations for:
/// 1. GPU Instancing on all project materials.
/// 2. Static flags setup for Occlusion Culling and Static Batching on scene geometry.
/// 3. Texture size clamping (max 1024/512) and texture compression.
/// 4. Light shadow optimization (disabling point/spot dynamic shadows).
/// </summary>
public class ProjectOptimizationTool : EditorWindow
{
    private Vector2 scrollPos;
    private int maxTextureSizeLimit = 1024;
    private bool enableCrunchedCompression = true;
    private int compressionQuality = 75;

    [MenuItem("Tools/Car Project/Performance & Optimization Tool", false, 50)]
    public static void OpenWindow()
    {
        var win = GetWindow<ProjectOptimizationTool>("Performance Optimizer");
        win.minSize = new Vector2(480, 560);
        win.Show();
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.Space(8);
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.LabelField("⚡ Performance & FPS Optimizer", titleStyle);
        EditorGUILayout.LabelField("One-click CPU & GPU Optimization Suite", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.Space(12);

        // --- MASTER ONE-CLICK BUTTON ---
        GUI.backgroundColor = new Color(0.2f, 0.9f, 0.4f);
        if (GUILayout.Button("🚀 RUN ALL OPTIMIZATIONS (ONE-CLICK BOOST)", GUILayout.Height(42)))
        {
            if (EditorUtility.DisplayDialog("Run All Optimizations?",
                "This will optimize all materials for GPU Instancing, clamp oversized textures, configure scene static flags for Occlusion Culling, and optimize local light shadows.\n\nProceed?", "Yes, Optimize Everything", "Cancel"))
            {
                RunAllOptimizations();
            }
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(16);
        EditorGUILayout.LabelField("Individual Optimization Modules", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("You can also run specific optimization modules individually below.", MessageType.Info);

        EditorGUILayout.Space(8);

        // --- MODULE 1: GPU INSTANCING ---
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("1. GPU Instancing on Materials", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Enables GPU Instancing on all materials to dramatically reduce draw calls (SetPass calls).", EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.Space(4);
        if (GUILayout.Button("✨ Enable GPU Instancing on All Materials", GUILayout.Height(30)))
        {
            BatchEnableGPUInstancing();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(8);

        // --- MODULE 2: STATIC FLAGS & OCCLUSION ---
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("2. Scene Static Flags & Occlusion Culling", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Recursively marks all static environment geometry (houses, roads, props, fences) as Occluder/Occludee Static and Batching Static in the active scene.", EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.Space(4);
        if (GUILayout.Button("🏛️ Setup Static Flags for Occlusion & Batching", GUILayout.Height(30)))
        {
            SetupSceneStaticFlags();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(8);

        // --- MODULE 3: TEXTURE COMPRESSION ---
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("3. Texture Size & Compression Optimizer", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Clamps oversized 2K/4K textures to optimal size (e.g. 1024 or 512) and applies hardware GPU compression to save VRAM and memory bandwidth.", EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.Space(4);
        maxTextureSizeLimit = EditorGUILayout.IntPopup("Max Texture Size:", maxTextureSizeLimit, new string[] { "512 x 512 (Extreme Performance)", "1024 x 1024 (Balanced / Recommended)", "2048 x 2048 (High)" }, new int[] { 512, 1024, 2048 });
        enableCrunchedCompression = EditorGUILayout.Toggle("Crunch Compression (Smaller Build)", enableCrunchedCompression);
        if (enableCrunchedCompression)
        {
            compressionQuality = EditorGUILayout.IntSlider("Crunch Quality:", compressionQuality, 20, 100);
        }
        EditorGUILayout.Space(4);
        if (GUILayout.Button("🖼️ Optimize & Compress All Textures", GUILayout.Height(30)))
        {
            BatchOptimizeTextures(maxTextureSizeLimit, enableCrunchedCompression, compressionQuality);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(8);

        // --- MODULE 4: LIGHT SHADOW OPTIMIZATION ---
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("4. Light Shadow Optimization", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Disables real-time dynamic shadows on all point/spot lights in the scene, keeping only the Directional Sun shadows active. Saves massive GPU fill-rate.", EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.Space(4);
        if (GUILayout.Button("💡 Optimize Local Light Shadows", GUILayout.Height(30)))
        {
            OptimizeLightShadows();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(8);

        // --- MODULE 5: VIBRANT VISUALS & OPTIMIZED BLOOM ---
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("5. Vibrant Visuals & Optimized Bloom", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Applies ACES Film Tonemapping, vibrant color contrast (+18) and saturation (+22), optimized soft Bloom (0.85), and cinematic Vignette for rich, stylized lighting.", EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.Space(4);
        if (GUILayout.Button("🎨 Apply Vibrant Indie Visuals & Bloom", GUILayout.Height(30)))
        {
            ApplyVibrantVisualsPreset();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(12);
        EditorGUILayout.EndScrollView();
    }

    public static void RunAllOptimizations()
    {
        try
        {
            EditorUtility.DisplayProgressBar("Optimizing Project", "Enabling GPU Instancing on materials...", 0.2f);
            int matCount = BatchEnableGPUInstancing(false);

            EditorUtility.DisplayProgressBar("Optimizing Project", "Setting up scene static flags...", 0.45f);
            int staticObjCount = SetupSceneStaticFlags(false);

            EditorUtility.DisplayProgressBar("Optimizing Project", "Optimizing light shadows...", 0.7f);
            int lightCount = OptimizeLightShadows(false);

            EditorUtility.DisplayProgressBar("Optimizing Project", "Compressing oversized textures...", 0.85f);
            int texCount = BatchOptimizeTextures(1024, true, 75, false);

            EditorUtility.DisplayProgressBar("Optimizing Project", "Applying vibrant visuals & bloom preset...", 0.95f);
            bool visualsApplied = ApplyVibrantVisualsPreset(false);

            EditorUtility.ClearProgressBar();

            EditorUtility.DisplayDialog("Optimization Complete!",
                $"✅ Project Optimization Successfully Applied:\n\n" +
                $"• Materials updated with GPU Instancing: {matCount}\n" +
                $"• Scene objects configured for Occlusion/Batching: {staticObjCount}\n" +
                $"• Local light shadows optimized: {lightCount}\n" +
                $"• Textures checked and compressed: {texCount}\n" +
                $"• Vibrant ACES Tonemapping & Bloom: {(visualsApplied ? "Active" : "Skipped")}\n\n" +
                $"Your project is now fully configured for maximum FPS and stunning visuals!", "Awesome");
        }
        catch (Exception ex)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError($"[ProjectOptimizationTool] Error during optimization: {ex}");
        }
    }

    public static int BatchEnableGPUInstancing(bool showDialog = true)
    {
        string[] guids = AssetDatabase.FindAssets("t:Material", new string[] { "Assets/_Project", "Assets/_AssetPacks" });
        int updatedCount = 0;

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            if (!mat.enableInstancing)
            {
                mat.enableInstancing = true;
                EditorUtility.SetDirty(mat);
                updatedCount++;
            }
        }

        if (updatedCount > 0)
        {
            AssetDatabase.SaveAssets();
        }

        Debug.Log($"[ProjectOptimizationTool] GPU Instancing enabled on {updatedCount} materials (out of {guids.Length} checked).");

        if (showDialog)
        {
            EditorUtility.DisplayDialog("GPU Instancing Complete",
                $"Successfully enabled GPU Instancing on {updatedCount} materials.\nTotal materials scanned: {guids.Length}", "OK");
        }

        return updatedCount;
    }

    public static int SetupSceneStaticFlags(bool showDialog = true)
    {
        var allGameObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int markedCount = 0;

        foreach (var go in allGameObjects)
        {
            if (go == null) continue;

            // Skip dynamic gameplay elements
            if (go.GetComponent<CharacterController>() != null ||
                go.GetComponent<Rigidbody>() != null ||
                go.GetComponentInParent<FPSPlayerController>() != null ||
                go.GetComponentInParent<DrivableVehicle>() != null ||
                go.GetComponentInParent<AITrafficVehicle>() != null ||
                go.GetComponentInParent<PhysicalCargoPackage>() != null ||
                go.GetComponent<Camera>() != null ||
                go.GetComponent<Canvas>() != null)
            {
                continue;
            }

            // If object has a MeshRenderer or Terrain, mark as static
            if (go.GetComponent<MeshRenderer>() != null || go.GetComponent<Terrain>() != null)
            {
                StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(go);
                StaticEditorFlags targetFlags = flags |
                    StaticEditorFlags.OccluderStatic |
                    StaticEditorFlags.OccludeeStatic |
                    StaticEditorFlags.BatchingStatic |
                    StaticEditorFlags.ContributeGI;

                if (flags != targetFlags)
                {
                    GameObjectUtility.SetStaticEditorFlags(go, targetFlags);
                    EditorUtility.SetDirty(go);
                    markedCount++;
                }
            }
        }

        if (markedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        Debug.Log($"[ProjectOptimizationTool] Configured static flags for {markedCount} environment renderers in active scene.");

        if (showDialog)
        {
            EditorUtility.DisplayDialog("Static Flags Complete",
                $"Configured static flags for {markedCount} environment objects in '{EditorSceneManager.GetActiveScene().name}'.\n\nYou can now bake Occlusion Culling via 'Window > Rendering > Occlusion Culling'.", "OK");
        }

        return markedCount;
    }

    public static int OptimizeLightShadows(bool showDialog = true)
    {
        Light[] allLights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int optimizedCount = 0;

        foreach (var l in allLights)
        {
            if (l == null) continue;

            // Keep Directional Sun shadow, disable expensive Point and Spot shadows
            if (l.type == LightType.Point || l.type == LightType.Spot)
            {
                if (l.shadows != LightShadows.None)
                {
                    l.shadows = LightShadows.None;
                    EditorUtility.SetDirty(l);
                    optimizedCount++;
                }
            }
        }

        if (optimizedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        Debug.Log($"[ProjectOptimizationTool] Disabled point/spot shadows on {optimizedCount} local lights.");

        if (showDialog)
        {
            EditorUtility.DisplayDialog("Light Optimization Complete",
                $"Disabled dynamic real-time shadows on {optimizedCount} local point/spot lights.\nDirectional sunlight shadows remain active.", "OK");
        }

        return optimizedCount;
    }

    public static int BatchOptimizeTextures(int maxSize = 1024, bool crunch = true, int crunchQuality = 75, bool showDialog = true)
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new string[] { "Assets/_Project", "Assets/_AssetPacks" });
        int updatedCount = 0;

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            bool modified = false;

            // Clamp max texture size if higher than threshold
            if (importer.maxTextureSize > maxSize)
            {
                importer.maxTextureSize = maxSize;
                modified = true;
            }

            // Ensure compressed format
            if (importer.textureCompression == TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Compressed;
                modified = true;
            }

            if (crunch)
            {
                if (!importer.crunchedCompression)
                {
                    importer.crunchedCompression = true;
                    modified = true;
                }
                if (importer.compressionQuality != crunchQuality)
                {
                    importer.compressionQuality = crunchQuality;
                    modified = true;
                }
            }

            if (modified)
            {
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
                updatedCount++;
            }
        }

        Debug.Log($"[ProjectOptimizationTool] Optimized {updatedCount} textures (max size: {maxSize}, crunch: {crunch}).");

        if (showDialog)
        {
            EditorUtility.DisplayDialog("Texture Optimization Complete",
                $"Successfully optimized {updatedCount} textures (clamped to max {maxSize}x{maxSize} and compressed).\nTotal textures scanned: {guids.Length}", "OK");
        }

        return updatedCount;
    }

    public static bool ApplyVibrantVisualsPreset(bool showDialog = true)
    {
        string[] guids = AssetDatabase.FindAssets("t:VolumeProfile");
        int appliedProfiles = 0;

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var profile = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(path);
            if (profile == null) continue;

            if (profile.TryGet<UnityEngine.Rendering.Universal.Tonemapping>(out var tone))
            {
                tone.mode.overrideState = true;
                tone.mode.value = UnityEngine.Rendering.Universal.TonemappingMode.ACES;
                EditorUtility.SetDirty(tone);
            }

            if (profile.TryGet<UnityEngine.Rendering.Universal.ColorAdjustments>(out var color))
            {
                color.postExposure.overrideState = true;
                color.postExposure.value = 0.15f;
                color.contrast.overrideState = true;
                color.contrast.value = 18f;
                color.saturation.overrideState = true;
                color.saturation.value = 22f;
                EditorUtility.SetDirty(color);
            }

            if (profile.TryGet<UnityEngine.Rendering.Universal.Bloom>(out var bloom))
            {
                bloom.threshold.overrideState = true;
                bloom.threshold.value = 0.95f;
                bloom.intensity.overrideState = true;
                bloom.intensity.value = 0.85f;
                bloom.scatter.overrideState = true;
                bloom.scatter.value = 0.65f;
                EditorUtility.SetDirty(bloom);
            }

            if (profile.TryGet<UnityEngine.Rendering.Universal.Vignette>(out var vig))
            {
                vig.intensity.overrideState = true;
                vig.intensity.value = 0.22f;
                vig.smoothness.overrideState = true;
                vig.smoothness.value = 0.45f;
                EditorUtility.SetDirty(vig);
            }

            EditorUtility.SetDirty(profile);
            appliedProfiles++;
        }

        if (appliedProfiles > 0)
        {
            AssetDatabase.SaveAssets();
        }

        Debug.Log($"[ProjectOptimizationTool] Applied Vibrant Visuals & Bloom preset across {appliedProfiles} volume profiles.");

        if (showDialog)
        {
            EditorUtility.DisplayDialog("Visuals Preset Applied",
                $"Successfully configured ACES Tonemapping, vibrant contrast (+18), saturation (+22), optimized Bloom (0.85), and soft Vignette across {appliedProfiles} Volume Profiles!", "OK");
        }

        return appliedProfiles > 0;
    }
}
#endif
