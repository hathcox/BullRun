using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BullRun.Tests.UI
{
    [TestFixture]
    public class PauseMenuTests
    {
        private GameObject _pauseMenuGo;
        private PauseMenuUI _pauseMenuUI;
        private PauseMenuReferences _pauseRefs;
        private SettingsPanelReferences _settingsRefs;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();

            // Reset timeScale in case previous test left it at 0
            Time.timeScale = 1f;

            // Create minimal pause menu references
            _pauseRefs = new PauseMenuReferences();

            var canvasGo = new GameObject("TestPauseCanvas");
            _pauseRefs.PauseMenuCanvas = canvasGo.AddComponent<Canvas>();
            canvasGo.SetActive(false);

            var panelGo = new GameObject("TestPausePanel");
            panelGo.transform.SetParent(canvasGo.transform);
            _pauseRefs.PausePanel = panelGo;

            _pauseRefs.ContinueButton = new GameObject("Continue").AddComponent<Button>();
            _pauseRefs.ContinueButton.gameObject.AddComponent<Image>();
            _pauseRefs.SettingsButton = new GameObject("Settings").AddComponent<Button>();
            _pauseRefs.SettingsButton.gameObject.AddComponent<Image>();
            _pauseRefs.ReturnToMenuButton = new GameObject("ReturnToMenu").AddComponent<Button>();
            _pauseRefs.ReturnToMenuButton.gameObject.AddComponent<Image>();
            _pauseRefs.ExitButton = new GameObject("Exit").AddComponent<Button>();
            _pauseRefs.ExitButton.gameObject.AddComponent<Image>();

            var confirmGo = new GameObject("ConfirmPopup");
            confirmGo.SetActive(false);
            _pauseRefs.ConfirmationPopup = confirmGo;
            _pauseRefs.ConfirmYesButton = new GameObject("Yes").AddComponent<Button>();
            _pauseRefs.ConfirmYesButton.gameObject.AddComponent<Image>();
            _pauseRefs.ConfirmNoButton = new GameObject("No").AddComponent<Button>();
            _pauseRefs.ConfirmNoButton.gameObject.AddComponent<Image>();

            // Create minimal settings references
            _settingsRefs = new SettingsPanelReferences();
            var settingsCanvasGo = new GameObject("TestSettingsCanvas");
            _settingsRefs.SettingsCanvas = settingsCanvasGo.AddComponent<Canvas>();
            settingsCanvasGo.SetActive(false);
            var masterSliderGo = new GameObject("MasterSlider");
            _settingsRefs.MasterVolumeSlider = masterSliderGo.AddComponent<Slider>();
            var musicSliderGo = new GameObject("MusicSlider");
            _settingsRefs.MusicVolumeSlider = musicSliderGo.AddComponent<Slider>();
            var sfxSliderGo = new GameObject("SfxSlider");
            _settingsRefs.SfxVolumeSlider = sfxSliderGo.AddComponent<Slider>();
            var toggleGo = new GameObject("FullscreenToggle");
            _settingsRefs.FullscreenToggle = toggleGo.AddComponent<Toggle>();
            var backBtnGo = new GameObject("BackButton");
            _settingsRefs.BackButton = backBtnGo.AddComponent<Button>();
            backBtnGo.AddComponent<Image>();

            // Create PauseMenuUI
            _pauseMenuGo = new GameObject("TestPauseMenuUI");
            _pauseMenuUI = _pauseMenuGo.AddComponent<PauseMenuUI>();
            _pauseMenuUI.Initialize(_pauseRefs, _settingsRefs);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            EventBus.Clear();

            // Destroy all test GameObjects
            if (_pauseMenuGo != null) Object.DestroyImmediate(_pauseMenuGo);
            if (_pauseRefs?.PauseMenuCanvas != null) Object.DestroyImmediate(_pauseRefs.PauseMenuCanvas.gameObject);
            if (_pauseRefs?.ContinueButton != null) Object.DestroyImmediate(_pauseRefs.ContinueButton.gameObject);
            if (_pauseRefs?.SettingsButton != null) Object.DestroyImmediate(_pauseRefs.SettingsButton.gameObject);
            if (_pauseRefs?.ReturnToMenuButton != null) Object.DestroyImmediate(_pauseRefs.ReturnToMenuButton.gameObject);
            if (_pauseRefs?.ExitButton != null) Object.DestroyImmediate(_pauseRefs.ExitButton.gameObject);
            if (_pauseRefs?.ConfirmationPopup != null) Object.DestroyImmediate(_pauseRefs.ConfirmationPopup);
            if (_pauseRefs?.ConfirmYesButton != null) Object.DestroyImmediate(_pauseRefs.ConfirmYesButton.gameObject);
            if (_pauseRefs?.ConfirmNoButton != null) Object.DestroyImmediate(_pauseRefs.ConfirmNoButton.gameObject);
            if (_settingsRefs?.SettingsCanvas != null) Object.DestroyImmediate(_settingsRefs.SettingsCanvas.gameObject);
            if (_settingsRefs?.MasterVolumeSlider != null) Object.DestroyImmediate(_settingsRefs.MasterVolumeSlider.gameObject);
            if (_settingsRefs?.MusicVolumeSlider != null) Object.DestroyImmediate(_settingsRefs.MusicVolumeSlider.gameObject);
            if (_settingsRefs?.SfxVolumeSlider != null) Object.DestroyImmediate(_settingsRefs.SfxVolumeSlider.gameObject);
            if (_settingsRefs?.FullscreenToggle != null) Object.DestroyImmediate(_settingsRefs.FullscreenToggle.gameObject);
            if (_settingsRefs?.BackButton != null) Object.DestroyImmediate(_settingsRefs.BackButton.gameObject);

            // Reset MainMenuState
            MainMenuState.MainMenuCanvasGo = null;
            MainMenuState.SettingsCanvasGo = null;
            MainMenuState.GameplayCanvases = null;
        }

        // ── TogglePause Tests ─────────────────────────────────────────

        [Test]
        public void TogglePause_SetsTimeScaleToZero()
        {
            Time.timeScale = 1f;

            _pauseMenuUI.TogglePause();

            Assert.AreEqual(0f, Time.timeScale, 0.001f);
            Assert.IsTrue(_pauseMenuUI.IsPaused);
        }

        [Test]
        public void TogglePause_SecondToggle_RestoresTimeScaleToOne()
        {
            Time.timeScale = 1f;

            _pauseMenuUI.TogglePause(); // pause
            _pauseMenuUI.TogglePause(); // resume

            Assert.AreEqual(1f, Time.timeScale, 0.001f);
            Assert.IsFalse(_pauseMenuUI.IsPaused);
        }

        [Test]
        public void Pause_ShowsPauseCanvas()
        {
            _pauseMenuUI.Pause();

            Assert.IsTrue(_pauseRefs.PauseMenuCanvas.gameObject.activeSelf);
        }

        [Test]
        public void Resume_HidesPauseCanvas()
        {
            _pauseMenuUI.Pause();
            _pauseMenuUI.Resume();

            Assert.IsFalse(_pauseRefs.PauseMenuCanvas.gameObject.activeSelf);
        }

        // ── ESC Layering Tests ────────────────────────────────────────

        [Test]
        public void HandleEscape_WhileSettingsOpen_ClosesSettings_NotPauseMenu()
        {
            _pauseMenuUI.Pause();

            // Simulate opening settings
            _pauseRefs.SettingsButton.onClick.Invoke();

            Assert.IsTrue(_pauseMenuUI.IsSettingsOpen);

            // Press ESC — should close settings, not pause menu
            _pauseMenuUI.HandleEscapePressed();

            Assert.IsFalse(_pauseMenuUI.IsSettingsOpen);
            Assert.IsTrue(_pauseMenuUI.IsPaused); // Still paused
        }

        [Test]
        public void HandleEscape_WhileConfirmationOpen_DismissesConfirmation()
        {
            _pauseMenuUI.Pause();

            // Simulate showing confirmation
            _pauseRefs.ReturnToMenuButton.onClick.Invoke();

            Assert.IsTrue(_pauseMenuUI.IsConfirmationOpen);

            // Press ESC — should dismiss confirmation
            _pauseMenuUI.HandleEscapePressed();

            Assert.IsFalse(_pauseMenuUI.IsConfirmationOpen);
            Assert.IsTrue(_pauseMenuUI.IsPaused); // Still paused
        }

        // ── Event Tests ───────────────────────────────────────────────

        [Test]
        public void Pause_PublishesGamePausedEvent()
        {
            bool received = false;
            EventBus.Subscribe<GamePausedEvent>(evt => received = true);

            _pauseMenuUI.Pause();

            Assert.IsTrue(received);
        }

        [Test]
        public void Resume_PublishesGameResumedEvent()
        {
            bool received = false;
            EventBus.Subscribe<GameResumedEvent>(evt => received = true);

            _pauseMenuUI.Pause();
            _pauseMenuUI.Resume();

            Assert.IsTrue(received);
        }

        [Test]
        public void ReturnToMenu_PublishesReturnToMenuEvent()
        {
            bool received = false;
            EventBus.Subscribe<ReturnToMenuEvent>(evt => received = true);

            _pauseMenuUI.Pause();
            _pauseRefs.ConfirmYesButton.onClick.Invoke();

            Assert.IsTrue(received);
        }

        [Test]
        public void ReturnToMenu_RestoresTimeScaleToOne()
        {
            _pauseMenuUI.Pause();
            Assert.AreEqual(0f, Time.timeScale, 0.001f);

            _pauseRefs.ConfirmYesButton.onClick.Invoke();

            Assert.AreEqual(1f, Time.timeScale, 0.001f);
        }

        // ── MainMenuState Guard Test ──────────────────────────────────

        [Test]
        public void MainMenuState_IsActive_SetCorrectlyOnEnterAndExit()
        {
            // The ESC guard (MainMenuState.IsActive check) lives in GameRunner.HandleEscapeInput().
            // PauseMenuUI doesn't check MainMenuState — GameRunner gates the call.
            // This test verifies the IsActive lifecycle that the guard depends on.
            MainMenuState.MainMenuCanvasGo = null;
            MainMenuState.GameplayCanvases = null;
            var state = new MainMenuState();
            var ctx = new RunContext(1, 1, new Portfolio(10f));

            state.Enter(ctx);
            Assert.IsTrue(MainMenuState.IsActive);

            state.Exit(ctx);
            Assert.IsFalse(MainMenuState.IsActive);
        }

        // ── Settings Back Button Sync Test ───────────────────────────

        [Test]
        public void SettingsBackButton_SyncsIsSettingsOpenFlag()
        {
            _pauseMenuUI.Pause();

            // Open settings from pause menu
            _pauseRefs.SettingsButton.onClick.Invoke();
            Assert.IsTrue(_pauseMenuUI.IsSettingsOpen);

            // Click Back button on settings panel (simulates MainMenuUI + PauseMenuUI both handling it)
            _settingsRefs.BackButton.onClick.Invoke();
            Assert.IsFalse(_pauseMenuUI.IsSettingsOpen, "IsSettingsOpen should sync when Back button clicked");
        }

        [Test]
        public void Resume_ClosesSettingsPanel_IfOpen()
        {
            _pauseMenuUI.Pause();

            // Open settings
            _pauseRefs.SettingsButton.onClick.Invoke();
            Assert.IsTrue(_pauseMenuUI.IsSettingsOpen);

            // Resume should close settings
            _pauseMenuUI.Resume();
            Assert.IsFalse(_pauseMenuUI.IsSettingsOpen);
            Assert.IsFalse(_settingsRefs.SettingsCanvas.gameObject.activeSelf);
        }

        // ── Pause/Resume Event Infrastructure Tests ──────────────────

        [Test]
        public void PauseAndResumeEvents_ArePublishableAndSubscribable()
        {
            // Verifies EventBus infrastructure for pause/resume events.
            // Volume ducking behavior requires AudioSources (PlayMode tests).
            bool pauseReceived = false;
            bool resumeReceived = false;
            EventBus.Subscribe<GamePausedEvent>(evt => pauseReceived = true);
            EventBus.Subscribe<GameResumedEvent>(evt => resumeReceived = true);

            EventBus.Publish(new GamePausedEvent());
            Assert.IsTrue(pauseReceived);

            EventBus.Publish(new GameResumedEvent());
            Assert.IsTrue(resumeReceived);
        }
    }
}
