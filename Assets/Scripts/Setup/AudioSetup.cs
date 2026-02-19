using UnityEngine;
using MoreMountains.Tools;

/// <summary>
/// Static setup class that creates the audio system at runtime.
/// Follows the GameFeelSetup pattern: called from GameRunner.Start().
/// Finds the AudioClipHolder (created during F5), populates AudioClipLibrary,
/// ensures MMSoundManager exists, and initializes AudioManager.
/// </summary>
public static class AudioSetup
{
    public static void Execute()
    {
        // 1. Ensure MMSoundManager singleton exists
        if (MMSoundManager.Instance == null)
        {
            var mmGo = new GameObject("MMSoundManager");
            mmGo.AddComponent<MMSoundManager>();

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[Audio] Created MMSoundManager singleton");
            #endif
        }

        // 2. Find AudioClipHolder in scene (placed by F5 AudioClipHolderSetup)
        var holder = Object.FindFirstObjectByType<AudioClipHolder>();
        if (holder == null)
        {
            Debug.LogWarning("[Audio] AudioClipHolder not found in scene. Run F5 to regenerate. Audio will be silent.");
            return;
        }

        // 3. Populate AudioClipLibrary from holder entries
        var clipLibrary = new AudioClipLibrary();
        clipLibrary.PopulateFromEntries(holder.Clips);

        // 4. Log any expected clips that are missing (AC: 17)
        ValidateExpectedClips(clipLibrary);

        // 5. Create AudioManager MonoBehaviour
        var audioGo = new GameObject("AudioManager");
        var audioManager = audioGo.AddComponent<AudioManager>();
        audioManager.Initialize(clipLibrary);

        // 6. Create MusicManager MonoBehaviour (Story 11.2)
        var musicGo = new GameObject("MusicManager");
        var musicManager = musicGo.AddComponent<MusicManager>();
        musicManager.Initialize(clipLibrary);

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[Audio] Setup complete: {holder.Clips.Length} clips loaded, AudioManager + MusicManager initialized");
        #endif
    }

    private static void ValidateExpectedClips(AudioClipLibrary lib)
    {
        // Check all public AudioClip fields for nulls
        var fields = typeof(AudioClipLibrary).GetFields(
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        int missing = 0;
        foreach (var field in fields)
        {
            if (field.FieldType != typeof(AudioClip)) continue;
            if (field.GetValue(lib) == null)
            {
                Debug.LogWarning($"[Audio] Expected clip missing: {field.Name} — sound will be skipped gracefully");
                missing++;
            }
        }

        if (missing > 0)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"[Audio] {missing} expected clips not found on disk. These sounds will be silent.");
            #endif
        }
    }
}
