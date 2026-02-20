using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;

/// <summary>
/// Pause menu controller. Handles ESC toggle, button actions, settings panel layering,
/// confirmation popup, and return-to-menu flow.
/// Story 16.2: Created by GameRunner.Start, initialized with UI references.
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    private PauseMenuReferences _pauseRefs;
    private SettingsPanelReferences _settingsRefs;

    private bool _isPaused;
    private bool _isSettingsOpen;
    private bool _isConfirmationOpen;

    public bool IsPaused => _isPaused;
    internal bool IsSettingsOpen => _isSettingsOpen;
    internal bool IsConfirmationOpen => _isConfirmationOpen;

    public void Initialize(PauseMenuReferences pauseRefs, SettingsPanelReferences settingsRefs)
    {
        _pauseRefs = pauseRefs;
        _settingsRefs = settingsRefs;

        // Wire button click listeners
        _pauseRefs.ContinueButton.onClick.AddListener(OnContinue);
        _pauseRefs.SettingsButton.onClick.AddListener(OnSettings);
        _pauseRefs.ReturnToMenuButton.onClick.AddListener(OnReturnToMenu);
        _pauseRefs.ExitButton.onClick.AddListener(OnExit);
        _pauseRefs.ConfirmYesButton.onClick.AddListener(OnConfirmReturnToMenu);
        _pauseRefs.ConfirmNoButton.onClick.AddListener(OnCancelReturnToMenu);

        // Wire settings Back button so _isSettingsOpen syncs when closed via Back (not just ESC)
        if (_settingsRefs.BackButton != null)
            _settingsRefs.BackButton.onClick.AddListener(OnSettingsBackFromPause);

        // Add button feel (hover/click animations) — same pattern as MainMenuUI
        AddButtonFeel(_pauseRefs.ContinueButton);
        AddButtonFeel(_pauseRefs.SettingsButton);
        AddButtonFeel(_pauseRefs.ReturnToMenuButton);
        AddButtonFeel(_pauseRefs.ExitButton);
        AddButtonFeel(_pauseRefs.ConfirmYesButton);
        AddButtonFeel(_pauseRefs.ConfirmNoButton);

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[PauseMenuUI] Initialized — all buttons wired");
        #endif
    }

    // ════════════════════════════════════════════════════════════════════
    // ESC KEY HANDLING (layered)
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called by GameRunner when ESC is pressed during gameplay.
    /// Handles layered ESC behavior: settings → confirmation → toggle pause.
    /// </summary>
    public void HandleEscapePressed()
    {
        // Layer 1: If settings panel is open → close settings, return to pause menu
        if (_isSettingsOpen)
        {
            CloseSettings();
            return;
        }

        // Layer 2: If confirmation popup is open → dismiss confirmation
        if (_isConfirmationOpen)
        {
            OnCancelReturnToMenu();
            return;
        }

        // Layer 3: Toggle pause
        TogglePause();
    }

    // ════════════════════════════════════════════════════════════════════
    // PAUSE / RESUME
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Toggles between paused and unpaused states.
    /// </summary>
    public void TogglePause()
    {
        if (_isPaused)
            Resume();
        else
            Pause();
    }

    /// <summary>
    /// Pauses the game: freezes time, shows pause panel, ducks music.
    /// </summary>
    public void Pause()
    {
        if (_isPaused) return;

        _isPaused = true;
        Time.timeScale = 0f;

        // Show pause panel
        if (_pauseRefs.PauseMenuCanvas != null)
            _pauseRefs.PauseMenuCanvas.gameObject.SetActive(true);

        // Publish pause event (MusicManager ducks volume)
        EventBus.Publish(new GamePausedEvent());

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[PauseMenuUI] Game paused — timeScale=0");
        #endif
    }

    /// <summary>
    /// Resumes the game: restores time, hides pause panel, restores music.
    /// </summary>
    public void Resume()
    {
        if (!_isPaused) return;

        // Close settings panel if it was open (saves settings, hides canvas)
        if (_isSettingsOpen)
            CloseSettings();

        _isPaused = false;
        Time.timeScale = 1f;

        // Hide everything
        if (_pauseRefs.PauseMenuCanvas != null)
            _pauseRefs.PauseMenuCanvas.gameObject.SetActive(false);
        if (_pauseRefs.ConfirmationPopup != null)
            _pauseRefs.ConfirmationPopup.SetActive(false);

        _isSettingsOpen = false;
        _isConfirmationOpen = false;

        // Publish resume event (MusicManager restores volume)
        EventBus.Publish(new GameResumedEvent());

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[PauseMenuUI] Game resumed — timeScale=1");
        #endif
    }

    // ════════════════════════════════════════════════════════════════════
    // BUTTON HANDLERS
    // ════════════════════════════════════════════════════════════════════

    private void OnContinue()
    {
        Resume();
    }

    private void OnSettings()
    {
        // Initialize slider values from SettingsManager
        _settingsRefs.MasterVolumeSlider.value = SettingsManager.MasterVolume;
        _settingsRefs.MusicVolumeSlider.value = SettingsManager.MusicVolume;
        _settingsRefs.SfxVolumeSlider.value = SettingsManager.SfxVolume;
        _settingsRefs.FullscreenToggle.isOn = SettingsManager.Fullscreen;

        // Show shared settings panel
        if (_settingsRefs.SettingsCanvas != null)
            _settingsRefs.SettingsCanvas.gameObject.SetActive(true);

        _isSettingsOpen = true;

        AudioManager.Instance?.PlayPanelOpen();
    }

    private void CloseSettings()
    {
        SettingsManager.Save();
        SettingsManager.ApplyDisplay();

        if (_settingsRefs.SettingsCanvas != null)
            _settingsRefs.SettingsCanvas.gameObject.SetActive(false);

        _isSettingsOpen = false;

        AudioManager.Instance?.PlayPanelClose();
    }

    /// <summary>
    /// Called when the shared settings panel's Back button is clicked while pause menu owns it.
    /// MainMenuUI also wires this button — this listener syncs PauseMenuUI's state flag.
    /// </summary>
    private void OnSettingsBackFromPause()
    {
        if (_isSettingsOpen)
            _isSettingsOpen = false;
    }

    private void OnReturnToMenu()
    {
        // Show confirmation popup
        if (_pauseRefs.ConfirmationPopup != null)
            _pauseRefs.ConfirmationPopup.SetActive(true);

        _isConfirmationOpen = true;
    }

    private void OnConfirmReturnToMenu()
    {
        // Restore timeScale before transitioning
        Time.timeScale = 1f;
        _isPaused = false;
        _isSettingsOpen = false;
        _isConfirmationOpen = false;

        // Hide pause menu
        if (_pauseRefs.PauseMenuCanvas != null)
            _pauseRefs.PauseMenuCanvas.gameObject.SetActive(false);
        if (_pauseRefs.ConfirmationPopup != null)
            _pauseRefs.ConfirmationPopup.SetActive(false);

        // Publish events: resume music, then return to menu
        EventBus.Publish(new GameResumedEvent());
        EventBus.Publish(new ReturnToMenuEvent());

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[PauseMenuUI] Returning to main menu — run abandoned");
        #endif
    }

    private void OnCancelReturnToMenu()
    {
        if (_pauseRefs.ConfirmationPopup != null)
            _pauseRefs.ConfirmationPopup.SetActive(false);

        _isConfirmationOpen = false;

        AudioManager.Instance?.PlayCancel();
    }

    private void OnExit()
    {
        // Restore timeScale before quitting
        Time.timeScale = 1f;

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    // ════════════════════════════════════════════════════════════════════
    // HELPERS
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Adds hover scale + click punch feel to a button, matching MainMenuUI pattern.
    /// Uses SetUpdate(true) to work during timeScale=0.
    /// </summary>
    private static void AddButtonFeel(Button btn)
    {
        if (btn == null) return;

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

        btn.onClick.AddListener(() =>
        {
            btn.transform.DOKill();
            btn.transform.localScale = Vector3.one;
            btn.transform.DOPunchScale(Vector3.one * 0.12f, 0.18f, 6, 0.5f).SetUpdate(true);
        });
    }
}
