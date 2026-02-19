using NUnit.Framework;
using UnityEngine;

namespace BullRun.Tests.UI
{
    [TestFixture]
    public class MainMenuTests
    {
        // ── MainMenuState Tests ─────────────────────────────────────

        [Test]
        public void MainMenuState_Enter_SetsIsActiveTrue()
        {
            var state = new MainMenuState();
            MainMenuState.MainMenuCanvasGo = null;
            MainMenuState.GameplayCanvases = null;
            var ctx = new RunContext(1, 1, new Portfolio(10f));

            state.Enter(ctx);

            Assert.IsTrue(MainMenuState.IsActive);
        }

        [Test]
        public void MainMenuState_Exit_SetsIsActiveFalse()
        {
            var state = new MainMenuState();
            MainMenuState.MainMenuCanvasGo = null;
            MainMenuState.SettingsCanvasGo = null;
            MainMenuState.GameplayCanvases = null;
            var ctx = new RunContext(1, 1, new Portfolio(10f));

            state.Enter(ctx);
            state.Exit(ctx);

            Assert.IsFalse(MainMenuState.IsActive);
        }

        [Test]
        public void MainMenuState_Enter_ActivatesMainMenuCanvas()
        {
            var canvasGo = new GameObject("TestMainMenuCanvas");
            canvasGo.SetActive(false);
            MainMenuState.MainMenuCanvasGo = canvasGo;
            MainMenuState.GameplayCanvases = null;

            var state = new MainMenuState();
            state.Enter(new RunContext(1, 1, new Portfolio(10f)));

            Assert.IsTrue(canvasGo.activeSelf);

            Object.DestroyImmediate(canvasGo);
        }

        [Test]
        public void MainMenuState_Exit_DeactivatesMainMenuCanvas()
        {
            var canvasGo = new GameObject("TestMainMenuCanvas");
            canvasGo.SetActive(true);
            MainMenuState.MainMenuCanvasGo = canvasGo;
            MainMenuState.SettingsCanvasGo = null;
            MainMenuState.GameplayCanvases = null;

            var state = new MainMenuState();
            var ctx = new RunContext(1, 1, new Portfolio(10f));
            state.Enter(ctx);
            state.Exit(ctx);

            Assert.IsFalse(canvasGo.activeSelf);

            Object.DestroyImmediate(canvasGo);
        }

        [Test]
        public void MainMenuState_Enter_HidesGameplayCanvases()
        {
            var gameplayGo = new GameObject("TestGameplayCanvas");
            gameplayGo.SetActive(true);
            MainMenuState.MainMenuCanvasGo = null;
            MainMenuState.GameplayCanvases = new[] { gameplayGo };

            var state = new MainMenuState();
            state.Enter(new RunContext(1, 1, new Portfolio(10f)));

            Assert.IsFalse(gameplayGo.activeSelf);

            Object.DestroyImmediate(gameplayGo);
        }

        [Test]
        public void MainMenuState_Exit_ShowsGameplayCanvases()
        {
            var gameplayGo = new GameObject("TestGameplayCanvas");
            gameplayGo.SetActive(false);
            MainMenuState.MainMenuCanvasGo = null;
            MainMenuState.SettingsCanvasGo = null;
            MainMenuState.GameplayCanvases = new[] { gameplayGo };

            var state = new MainMenuState();
            var ctx = new RunContext(1, 1, new Portfolio(10f));
            state.Enter(ctx);
            state.Exit(ctx);

            Assert.IsTrue(gameplayGo.activeSelf);

            Object.DestroyImmediate(gameplayGo);
        }

        // ── StartGameRequestedEvent Tests ───────────────────────────

        [Test]
        public void StartGameRequestedEvent_IsPublishable()
        {
            bool received = false;
            EventBus.Subscribe<StartGameRequestedEvent>(evt => received = true);
            EventBus.Publish(new StartGameRequestedEvent());

            Assert.IsTrue(received);

            EventBus.Clear();
        }

        // ── GameConfig.SfxVolume Default ────────────────────────────

        [Test]
        public void GameConfig_SfxVolume_Is08()
        {
            Assert.AreEqual(0.8f, GameConfig.SfxVolume, 0.001f);
        }

        // ── SettingsManager Default Matches GameConfig ──────────────

        [Test]
        public void SettingsManager_DefaultSfx_MatchesGameConfig()
        {
            SettingsManager.ResetToDefaults();
            Assert.AreEqual(GameConfig.SfxVolume, SettingsManager.SfxVolume, 0.001f);
        }

        [Test]
        public void SettingsManager_DefaultMusic_MatchesGameConfig()
        {
            SettingsManager.ResetToDefaults();
            Assert.AreEqual(GameConfig.MusicVolume, SettingsManager.MusicVolume, 0.001f);
        }

        [Test]
        public void SettingsManager_DefaultMaster_MatchesGameConfig()
        {
            SettingsManager.ResetToDefaults();
            Assert.AreEqual(GameConfig.MasterVolume, SettingsManager.MasterVolume, 0.001f);
        }
    }
}
