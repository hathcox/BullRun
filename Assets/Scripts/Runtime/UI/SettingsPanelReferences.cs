using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// References to settings panel UI elements created by UISetup.ExecuteSettingsUI().
/// Story 16.1: Used by MainMenuUI controller to wire slider/toggle/dropdown callbacks.
/// Shared between main menu and pause menu (Story 16.2).
/// </summary>
public class SettingsPanelReferences
{
    public Canvas SettingsCanvas;
    public Slider MasterVolumeSlider;
    public Text MasterVolumeText;
    public Slider MusicVolumeSlider;
    public Text MusicVolumeText;
    public Slider SfxVolumeSlider;
    public Text SfxVolumeText;
    public Toggle FullscreenToggle;
    public Dropdown ResolutionDropdown;
    public Button BackButton;
}
