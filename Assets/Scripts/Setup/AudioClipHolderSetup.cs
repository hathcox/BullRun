#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// F5 SetupClass that loads all AudioClip assets from Assets/Audio/ and
/// attaches them to an AudioClipHolder in the scene. Clips survive into Play mode
/// as serialized scene references — no Resources.Load or Addressables needed.
/// </summary>
[SetupClass(SetupPhase.SceneComposition, 15)]
public static class AudioClipHolderSetup
{
    public static void Execute()
    {
        var holderGo = new GameObject("AudioClipHolder");
        var holder = holderGo.AddComponent<AudioClipHolder>();

        // Find all audio clips under Assets/Audio/ (SFX, music, and any subdirectories)
        var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio" });
        var entries = new List<AudioClipHolder.AudioClipEntry>();

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            // Accept any clip under Assets/Audio/ (including subdirectories like Music/, SFX/, etc.)
            string dir = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
            if (!dir.StartsWith("Assets/Audio")) continue;

            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) continue;

            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            entries.Add(new AudioClipHolder.AudioClipEntry { Name = fileName, Clip = clip });
        }

        holder.Clips = entries.ToArray();

        Debug.Log($"[Setup] AudioClipHolder: {entries.Count} clips loaded from Assets/Audio/ (including Music/)");
    }
}
#endif
