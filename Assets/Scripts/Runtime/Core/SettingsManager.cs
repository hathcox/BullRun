using UnityEngine;

/// <summary>
/// Runtime-mutable settings for audio volumes and display.
/// Initializes from GameConfig defaults, overrides from PlayerPrefs if available.
/// Story 16.1: Provides runtime volume control for AudioManager/MusicManager.
/// </summary>
public static class SettingsManager
{
    // PlayerPrefs keys
    private const string KeyMasterVolume = "Settings_MasterVolume";
    private const string KeyMusicVolume = "Settings_MusicVolume";
    private const string KeySfxVolume = "Settings_SfxVolume";
    private const string KeyFullscreen = "Settings_Fullscreen";
    private const string KeyResolutionIndex = "Settings_ResolutionIndex";

    // Current runtime values
    public static float MasterVolume { get; set; } = 1.0f;
    public static float MusicVolume { get; set; } = 0.7f;
    public static float SfxVolume { get; set; } = 0.8f;
    public static bool Fullscreen { get; set; } = true;
    public static int ResolutionIndex { get; set; } = -1; // -1 = current resolution

    /// <summary>
    /// Loads settings from PlayerPrefs. Called early in GameRunner.Awake.
    /// Falls back to GameConfig defaults if no saved settings exist.
    /// </summary>
    public static void Load()
    {
        MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KeyMasterVolume, GameConfig.MasterVolume));
        MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KeyMusicVolume, GameConfig.MusicVolume));
        SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KeySfxVolume, GameConfig.SfxVolume));
        Fullscreen = PlayerPrefs.GetInt(KeyFullscreen, Screen.fullScreen ? 1 : 0) == 1;
        ResolutionIndex = PlayerPrefs.GetInt(KeyResolutionIndex, -1);

        ApplyDisplay();

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[Settings] Loaded: Master={MasterVolume:F2}, Music={MusicVolume:F2}, SFX={SfxVolume:F2}, Fullscreen={Fullscreen}");
        #endif
    }

    /// <summary>
    /// Saves current settings to PlayerPrefs.
    /// </summary>
    public static void Save()
    {
        PlayerPrefs.SetFloat(KeyMasterVolume, MasterVolume);
        PlayerPrefs.SetFloat(KeyMusicVolume, MusicVolume);
        PlayerPrefs.SetFloat(KeySfxVolume, SfxVolume);
        PlayerPrefs.SetInt(KeyFullscreen, Fullscreen ? 1 : 0);
        PlayerPrefs.SetInt(KeyResolutionIndex, ResolutionIndex);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Applies display settings (fullscreen mode and resolution).
    /// </summary>
    public static void ApplyDisplay()
    {
        var resolutions = Screen.resolutions;
        if (ResolutionIndex >= 0 && ResolutionIndex < resolutions.Length)
        {
            var res = resolutions[ResolutionIndex];
            Screen.SetResolution(res.width, res.height,
                Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        }
        else
        {
            Screen.fullScreenMode = Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        }
    }

    /// <summary>
    /// Resets all settings to defaults (for testing or first-run).
    /// </summary>
    internal static void ResetToDefaults()
    {
        MasterVolume = GameConfig.MasterVolume;
        MusicVolume = GameConfig.MusicVolume;
        SfxVolume = GameConfig.SfxVolume;
        Fullscreen = true;
        ResolutionIndex = -1;
    }
}
