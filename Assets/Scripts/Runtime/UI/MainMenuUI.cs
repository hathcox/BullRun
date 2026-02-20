using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Main menu controller. Wires button callbacks, handles settings panel,
/// and manages show/hide of the main menu overlay.
/// Story 16.1: Created by GameRunner.Start, initialized with UI references.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    private MainMenuReferences _menuRefs;
    private SettingsPanelReferences _settingsRefs;
    private List<Dropdown.OptionData> _resolutionOptions;
    private Resolution[] _resolutions;

    public void Initialize(MainMenuReferences menuRefs, SettingsPanelReferences settingsRefs)
    {
        _menuRefs = menuRefs;
        _settingsRefs = settingsRefs;

        // Wire main menu buttons
        _menuRefs.StartGameButton.onClick.AddListener(OnStartGame);
        _menuRefs.UnlocksButton.onClick.AddListener(OnUnlocks);
        _menuRefs.SettingsButton.onClick.AddListener(OnSettings);
        _menuRefs.ExitButton.onClick.AddListener(OnExit);
        _menuRefs.PopupBackButton.onClick.AddListener(OnPopupBack);

        // Add button feel (hover/click animations)
        AddButtonFeel(_menuRefs.StartGameButton);
        AddButtonFeel(_menuRefs.UnlocksButton);
        AddButtonFeel(_menuRefs.SettingsButton);
        AddButtonFeel(_menuRefs.ExitButton);
        AddButtonFeel(_menuRefs.PopupBackButton);

        // Wire settings panel
        _settingsRefs.MasterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        _settingsRefs.MusicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        _settingsRefs.SfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        _settingsRefs.FullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        _settingsRefs.ResolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        _settingsRefs.BackButton.onClick.AddListener(OnSettingsBack);
        AddButtonFeel(_settingsRefs.BackButton);

        // Populate resolution dropdown
        PopulateResolutions();

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[MainMenuUI] Initialized — all buttons and settings wired");
        #endif
    }

    // ════════════════════════════════════════════════════════════════════
    // MAIN MENU BUTTON HANDLERS
    // ════════════════════════════════════════════════════════════════════

    private void OnStartGame()
    {
        EventBus.Publish(new StartGameRequestedEvent());
    }

    private void OnUnlocks()
    {
        if (_menuRefs.ComingSoonPopup != null)
            _menuRefs.ComingSoonPopup.SetActive(true);
    }

    private void OnSettings()
    {
        // Initialize slider values from SettingsManager
        _settingsRefs.MasterVolumeSlider.value = SettingsManager.MasterVolume;
        _settingsRefs.MusicVolumeSlider.value = SettingsManager.MusicVolume;
        _settingsRefs.SfxVolumeSlider.value = SettingsManager.SfxVolume;
        _settingsRefs.FullscreenToggle.isOn = SettingsManager.Fullscreen;
        UpdatePercentageTexts();

        // Refresh resolution dropdown selection
        PopulateResolutions();

        if (_settingsRefs.SettingsCanvas != null)
            _settingsRefs.SettingsCanvas.gameObject.SetActive(true);

        AudioManager.Instance?.PlayPanelOpen();
    }

    private void OnExit()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    private void OnPopupBack()
    {
        if (_menuRefs.ComingSoonPopup != null)
            _menuRefs.ComingSoonPopup.SetActive(false);

        AudioManager.Instance?.PlayCancel();
    }

    // ════════════════════════════════════════════════════════════════════
    // SETTINGS PANEL HANDLERS
    // ════════════════════════════════════════════════════════════════════

    private void OnMasterVolumeChanged(float value)
    {
        SettingsManager.MasterVolume = value;
        UpdatePercentageTexts();

        // Update currently-playing music volume in real-time (AC 9)
        MusicManager.Instance?.UpdateVolumes();
    }

    private void OnMusicVolumeChanged(float value)
    {
        SettingsManager.MusicVolume = value;
        UpdatePercentageTexts();

        // Update currently-playing music volume in real-time (AC 9)
        MusicManager.Instance?.UpdateVolumes();
    }

    private void OnSfxVolumeChanged(float value)
    {
        SettingsManager.SfxVolume = value;
        UpdatePercentageTexts();
    }

    private void OnFullscreenChanged(bool isOn)
    {
        SettingsManager.Fullscreen = isOn;
    }

    private void OnResolutionChanged(int index)
    {
        SettingsManager.ResolutionIndex = index;
    }

    private void OnSettingsBack()
    {
        // Save settings and apply display changes
        SettingsManager.Save();
        SettingsManager.ApplyDisplay();

        if (_settingsRefs.SettingsCanvas != null)
            _settingsRefs.SettingsCanvas.gameObject.SetActive(false);

        AudioManager.Instance?.PlayPanelClose();
    }

    // ════════════════════════════════════════════════════════════════════
    // SHOW / HIDE
    // ════════════════════════════════════════════════════════════════════

    public void Show()
    {
        if (_menuRefs?.MainMenuCanvas != null)
            _menuRefs.MainMenuCanvas.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (_menuRefs?.MainMenuCanvas != null)
            _menuRefs.MainMenuCanvas.gameObject.SetActive(false);

        // Also hide settings if open
        if (_settingsRefs?.SettingsCanvas != null)
            _settingsRefs.SettingsCanvas.gameObject.SetActive(false);

        // Hide popup if open
        if (_menuRefs?.ComingSoonPopup != null)
            _menuRefs.ComingSoonPopup.SetActive(false);
    }

    // ════════════════════════════════════════════════════════════════════
    // HELPERS
    // ════════════════════════════════════════════════════════════════════

    private void UpdatePercentageTexts()
    {
        if (_settingsRefs.MasterVolumeText != null)
            _settingsRefs.MasterVolumeText.text = $"{Mathf.RoundToInt(SettingsManager.MasterVolume * 100)}%";
        if (_settingsRefs.MusicVolumeText != null)
            _settingsRefs.MusicVolumeText.text = $"{Mathf.RoundToInt(SettingsManager.MusicVolume * 100)}%";
        if (_settingsRefs.SfxVolumeText != null)
            _settingsRefs.SfxVolumeText.text = $"{Mathf.RoundToInt(SettingsManager.SfxVolume * 100)}%";
    }

    private void PopulateResolutions()
    {
        _resolutions = Screen.resolutions;
        _settingsRefs.ResolutionDropdown.ClearOptions();

        _resolutionOptions = new List<Dropdown.OptionData>();
        int currentIndex = 0;

        for (int i = 0; i < _resolutions.Length; i++)
        {
            var res = _resolutions[i];
            _resolutionOptions.Add(new Dropdown.OptionData($"{res.width}x{res.height}"));

            if (res.width == Screen.currentResolution.width &&
                res.height == Screen.currentResolution.height)
            {
                currentIndex = i;
            }
        }

        _settingsRefs.ResolutionDropdown.AddOptions(_resolutionOptions);

        // Select current resolution or saved preference
        int selectedIndex = SettingsManager.ResolutionIndex >= 0 ? SettingsManager.ResolutionIndex : currentIndex;
        if (selectedIndex < _resolutions.Length)
            _settingsRefs.ResolutionDropdown.value = selectedIndex;
    }

    /// <summary>
    /// Adds hover scale + click punch feel to a button, matching ShopUI pattern.
    /// </summary>
    private static void AddButtonFeel(Button btn)
    {
        if (btn == null) return;

        // Hover: scale up on enter, restore on exit
        var trigger = btn.gameObject.GetComponent<EventTrigger>()
                   ?? btn.gameObject.AddComponent<EventTrigger>();

        trigger.triggers.RemoveAll(e => e.eventID == EventTriggerType.PointerEnter
                                      || e.eventID == EventTriggerType.PointerExit);

        var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener(_ =>
        {
            btn.transform.DOKill();
            btn.transform.localScale = Vector3.one;
            btn.transform.DOScale(1.05f, 0.15f).SetUpdate(true);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonHover();
        });
        trigger.triggers.Add(enterEntry);

        var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener(_ =>
        {
            btn.transform.DOKill();
            btn.transform.localScale = Vector3.one;
        });
        trigger.triggers.Add(exitEntry);

        // Click: punch scale
        btn.onClick.AddListener(() =>
        {
            btn.transform.DOKill();
            btn.transform.localScale = Vector3.one;
            btn.transform.DOPunchScale(Vector3.one * 0.12f, 0.18f, 6, 0.5f).SetUpdate(true);
        });
    }
}
