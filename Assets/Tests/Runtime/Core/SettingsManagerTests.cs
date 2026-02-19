using NUnit.Framework;
using UnityEngine;

namespace BullRun.Tests.Core
{
    [TestFixture]
    public class SettingsManagerTests
    {
        [SetUp]
        public void SetUp()
        {
            SettingsManager.ResetToDefaults();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up any PlayerPrefs written during tests
            PlayerPrefs.DeleteKey("Settings_MasterVolume");
            PlayerPrefs.DeleteKey("Settings_MusicVolume");
            PlayerPrefs.DeleteKey("Settings_SfxVolume");
            PlayerPrefs.DeleteKey("Settings_Fullscreen");
            PlayerPrefs.DeleteKey("Settings_ResolutionIndex");
        }

        // ── Default Values ──────────────────────────────────────────

        [Test]
        public void DefaultMasterVolume_Is1()
        {
            Assert.AreEqual(1.0f, SettingsManager.MasterVolume, 0.001f);
        }

        [Test]
        public void DefaultMusicVolume_Is07()
        {
            Assert.AreEqual(0.7f, SettingsManager.MusicVolume, 0.001f);
        }

        [Test]
        public void DefaultSfxVolume_Is08()
        {
            Assert.AreEqual(0.8f, SettingsManager.SfxVolume, 0.001f);
        }

        [Test]
        public void DefaultFullscreen_IsTrue()
        {
            Assert.IsTrue(SettingsManager.Fullscreen);
        }

        [Test]
        public void DefaultResolutionIndex_IsNegativeOne()
        {
            Assert.AreEqual(-1, SettingsManager.ResolutionIndex);
        }

        // ── Save/Load Round-Trip ────────────────────────────────────

        [Test]
        public void SaveLoad_RoundTrips_MasterVolume()
        {
            SettingsManager.MasterVolume = 0.42f;
            SettingsManager.Save();
            SettingsManager.MasterVolume = 0f; // Reset before load
            SettingsManager.Load();
            Assert.AreEqual(0.42f, SettingsManager.MasterVolume, 0.001f);
        }

        [Test]
        public void SaveLoad_RoundTrips_MusicVolume()
        {
            SettingsManager.MusicVolume = 0.55f;
            SettingsManager.Save();
            SettingsManager.MusicVolume = 0f;
            SettingsManager.Load();
            Assert.AreEqual(0.55f, SettingsManager.MusicVolume, 0.001f);
        }

        [Test]
        public void SaveLoad_RoundTrips_SfxVolume()
        {
            SettingsManager.SfxVolume = 0.33f;
            SettingsManager.Save();
            SettingsManager.SfxVolume = 0f;
            SettingsManager.Load();
            Assert.AreEqual(0.33f, SettingsManager.SfxVolume, 0.001f);
        }

        [Test]
        public void SaveLoad_RoundTrips_Fullscreen()
        {
            SettingsManager.Fullscreen = false;
            SettingsManager.Save();
            SettingsManager.Fullscreen = true;
            SettingsManager.Load();
            Assert.IsFalse(SettingsManager.Fullscreen);
        }

        [Test]
        public void SaveLoad_RoundTrips_ResolutionIndex()
        {
            SettingsManager.ResolutionIndex = 3;
            SettingsManager.Save();
            SettingsManager.ResolutionIndex = -1;
            SettingsManager.Load();
            Assert.AreEqual(3, SettingsManager.ResolutionIndex);
        }

        // ── Volume Clamping ─────────────────────────────────────────

        [Test]
        public void Load_Clamps_MasterVolume_ToMax1()
        {
            PlayerPrefs.SetFloat("Settings_MasterVolume", 1.5f);
            PlayerPrefs.Save();
            SettingsManager.Load();
            Assert.AreEqual(1.0f, SettingsManager.MasterVolume, 0.001f);
        }

        [Test]
        public void Load_Clamps_MasterVolume_ToMin0()
        {
            PlayerPrefs.SetFloat("Settings_MasterVolume", -0.5f);
            PlayerPrefs.Save();
            SettingsManager.Load();
            Assert.AreEqual(0.0f, SettingsManager.MasterVolume, 0.001f);
        }

        [Test]
        public void Load_Clamps_MusicVolume_ToRange()
        {
            PlayerPrefs.SetFloat("Settings_MusicVolume", 2.0f);
            PlayerPrefs.Save();
            SettingsManager.Load();
            Assert.AreEqual(1.0f, SettingsManager.MusicVolume, 0.001f);
        }

        [Test]
        public void Load_Clamps_SfxVolume_ToRange()
        {
            PlayerPrefs.SetFloat("Settings_SfxVolume", -1.0f);
            PlayerPrefs.Save();
            SettingsManager.Load();
            Assert.AreEqual(0.0f, SettingsManager.SfxVolume, 0.001f);
        }

        // ── ResetToDefaults ─────────────────────────────────────────

        [Test]
        public void ResetToDefaults_RestoresAllValues()
        {
            SettingsManager.MasterVolume = 0.1f;
            SettingsManager.MusicVolume = 0.2f;
            SettingsManager.SfxVolume = 0.3f;
            SettingsManager.Fullscreen = false;
            SettingsManager.ResolutionIndex = 5;

            SettingsManager.ResetToDefaults();

            Assert.AreEqual(GameConfig.MasterVolume, SettingsManager.MasterVolume, 0.001f);
            Assert.AreEqual(GameConfig.MusicVolume, SettingsManager.MusicVolume, 0.001f);
            Assert.AreEqual(GameConfig.SfxVolume, SettingsManager.SfxVolume, 0.001f);
            Assert.IsTrue(SettingsManager.Fullscreen);
            Assert.AreEqual(-1, SettingsManager.ResolutionIndex);
        }
    }
}
