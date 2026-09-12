using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RoadSignLevelLock))]
[CanEditMultipleObjects]
public class RoadSignLevelLockEditor : Editor
{
    private SerializedProperty requiredLevelProp;
    private SerializedProperty lockTextFormatProp;
    private SerializedProperty lockTextColorProp;
    private SerializedProperty lockTextMeshProp;
    private SerializedProperty lockVisualContainerProp;
    private SerializedProperty autoCreateBadgeProp;
    private SerializedProperty hideWhenUnlockedProp;
    private SerializedProperty previewUnlockedProp;

    private void OnEnable()
    {
        requiredLevelProp = serializedObject.FindProperty("requiredLevel");
        lockTextFormatProp = serializedObject.FindProperty("lockTextFormat");
        lockTextColorProp = serializedObject.FindProperty("lockTextColor");
        lockTextMeshProp = serializedObject.FindProperty("lockTextMesh");
        lockVisualContainerProp = serializedObject.FindProperty("lockVisualContainer");
        autoCreateBadgeProp = serializedObject.FindProperty("autoCreateBadgeIfMissing");
        hideWhenUnlockedProp = serializedObject.FindProperty("hideWhenUnlocked");
        previewUnlockedProp = serializedObject.FindProperty("previewUnlockedInEditor");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        RoadSignLevelLock sign = (RoadSignLevelLock)target;

        int playerLevel = sign.GetCurrentPlayerLevel();
        bool isUnlocked = sign.IsUnlocked;

        // --- STATUS BANNER ---
        EditorGUILayout.Space(6);
        GUIStyle bannerStyle = new GUIStyle(EditorStyles.helpBox);
        bannerStyle.fontSize = 12;
        bannerStyle.fontStyle = FontStyle.Bold;
        bannerStyle.alignment = TextAnchor.MiddleCenter;
        bannerStyle.padding = new RectOffset(10, 10, 8, 8);

        if (isUnlocked)
        {
            GUI.backgroundColor = new Color(0.3f, 0.9f, 0.4f, 1f);
            string bannerMsg = Application.isPlaying
                ? $"🟢 KİLİT AÇIK (Oyuncu Seviyesi: {playerLevel} >= Gereken: {sign.requiredLevel})\n\"Level Required\" Yazısı Gizlendi."
                : $"🟢 EDİTÖR ÖNİZLEMESİ: KİLİT AÇIK\n(Gereken: Level {sign.requiredLevel} - Kilit yazısı gizli)";
            EditorGUILayout.LabelField(bannerMsg, bannerStyle, GUILayout.MinHeight(36));
        }
        else
        {
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f, 1f);
            string bannerMsg = Application.isPlaying
                ? $"🔴 KİLİTLİ (Gereken Seviye: {sign.requiredLevel} | Oyuncu: {playerLevel})\nTabela Üzerinde \"{string.Format(sign.lockTextFormat, sign.requiredLevel)}\" Gösteriliyor."
                : $"🔴 KİLİTLİ (Gereken Seviye: {sign.requiredLevel})\nTabela Üzerinde \"{string.Format(sign.lockTextFormat, sign.requiredLevel)}\" Gösteriliyor.";
            EditorGUILayout.LabelField(bannerMsg, bannerStyle, GUILayout.MinHeight(36));
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.Space(6);

        // --- LEVEL REQUIREMENT ---
        EditorGUILayout.LabelField("Seviye Ayarları", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(requiredLevelProp, new GUIContent("Gereken Seviye (Level)"));
        EditorGUILayout.PropertyField(lockTextFormatProp, new GUIContent("Kilit Yazı Formatı"));
        EditorGUILayout.PropertyField(lockTextColorProp, new GUIContent("Kilit Yazı Rengi"));

        EditorGUILayout.Space(8);

        // --- BEHAVIOR ---
        EditorGUILayout.LabelField("Görsel & Davranış Seçenekleri", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(autoCreateBadgeProp, new GUIContent("Otomatik Rozet Oluştur"));
        EditorGUILayout.PropertyField(hideWhenUnlockedProp, new GUIContent("Açılınca Yazıyı Gizle"));

        EditorGUILayout.Space(8);

        // --- REFERENCES ---
        EditorGUILayout.LabelField("Bileşen Referansları (Opsiyonel)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(lockTextMeshProp, new GUIContent("Kilit TextMeshPro"));
        EditorGUILayout.PropertyField(lockVisualContainerProp, new GUIContent("Kilit Görsel Kökü"));

        EditorGUILayout.Space(8);

        // --- PREVIEW ---
        EditorGUILayout.LabelField("Editör Önizleme", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(previewUnlockedProp, new GUIContent("Kilit Açık Önizle"));

        serializedObject.ApplyModifiedProperties();

        // --- HELPER BUTTONS ---
        EditorGUILayout.Space(12);
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("🔄 Kilit Rozetini Hizala / Yenile", GUILayout.Height(28)))
        {
            foreach (var t in targets)
            {
                if (t is RoadSignLevelLock s)
                {
                    Undo.RegisterFullObjectHierarchyUndo(s.gameObject, "Align Lock Badge");
                    s.CreateDefaultLockBadge();
                    s.UpdateVisuals();
                }
            }
        }

        if (GUILayout.Button(sign.previewUnlockedInEditor ? "🔒 Kilitli Önizle" : "🔓 Açık Önizle", GUILayout.Height(28)))
        {
            foreach (var t in targets)
            {
                if (t is RoadSignLevelLock s)
                {
                    Undo.RecordObject(s, "Toggle Preview");
                    s.previewUnlockedInEditor = !s.previewUnlockedInEditor;
                    s.UpdateVisuals();
                }
            }
        }

        GUILayout.EndHorizontal();

        // Play mode quick test
        if (Application.isPlaying)
        {
            EditorGUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("🧪 Şube Seviyesini +1 Arttır"))
            {
                if (BranchManager.Instance != null)
                {
                    BranchManager.Instance.TryUpgradeBranch();
                }
            }
            if (GUILayout.Button("🧪 Seviyeyi 1'e Sıfırla"))
            {
                if (BranchManager.Instance != null)
                {
                    BranchManager.Instance.ResetBranchProgression();
                }
            }
            GUILayout.EndHorizontal();
        }
    }
}
