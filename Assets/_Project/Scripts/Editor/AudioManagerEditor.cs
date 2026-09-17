#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AudioManager))]
public class AudioManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        AudioManager manager = (AudioManager)target;

        EditorGUILayout.Space(6);
        GUI.backgroundColor = new Color(0.2f, 0.85f, 0.4f, 1f);
        if (GUILayout.Button("🔄 Auto Scan & Connect All Audio Files", GUILayout.Height(36)))
        {
            AudioSetupHelper.SetupAndConnectAllAudio();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.HelpBox("Automatically finds all .wav, .mp3, .ogg, .flac clips in Assets/_Project/Audio and binds them directly to AudioManager and vehicle controllers.", MessageType.Info);
        EditorGUILayout.Space(6);

        DrawDefaultInspector();
    }
}
#endif
