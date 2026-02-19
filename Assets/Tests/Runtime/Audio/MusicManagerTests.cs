using NUnit.Framework;

namespace BullRun.Tests.Audio
{
    [TestFixture]
    public class MusicManagerTests
    {
        // ════════════════════════════════════════════════════════════════
        // Act-Round Mapping (AC 2)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void GetActForRound_Round1_ReturnsAct1()
        {
            Assert.AreEqual(1, MusicManager.GetActForRound(1));
        }

        [Test]
        public void GetActForRound_Round2_ReturnsAct1()
        {
            Assert.AreEqual(1, MusicManager.GetActForRound(2));
        }

        [Test]
        public void GetActForRound_Round3_ReturnsAct2()
        {
            Assert.AreEqual(2, MusicManager.GetActForRound(3));
        }

        [Test]
        public void GetActForRound_Round4_ReturnsAct2()
        {
            Assert.AreEqual(2, MusicManager.GetActForRound(4));
        }

        [Test]
        public void GetActForRound_Round5_ReturnsAct3()
        {
            Assert.AreEqual(3, MusicManager.GetActForRound(5));
        }

        [Test]
        public void GetActForRound_Round6_ReturnsAct3()
        {
            Assert.AreEqual(3, MusicManager.GetActForRound(6));
        }

        [Test]
        public void GetActForRound_Round7_ReturnsAct4()
        {
            Assert.AreEqual(4, MusicManager.GetActForRound(7));
        }

        [Test]
        public void GetActForRound_Round8_ReturnsAct4()
        {
            Assert.AreEqual(4, MusicManager.GetActForRound(8));
        }

        // ════════════════════════════════════════════════════════════════
        // GameConfig Music Constants Validation (AC 16)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void GameConfig_MusicCrossfadeDuration_Is2Seconds()
        {
            Assert.AreEqual(2.0f, GameConfig.MusicCrossfadeDuration);
        }

        [Test]
        public void GameConfig_MusicUrgencyFadeIn_Is1Second()
        {
            Assert.AreEqual(1.0f, GameConfig.MusicUrgencyFadeIn);
        }

        [Test]
        public void GameConfig_MusicCriticalFadeIn_Is03Seconds()
        {
            Assert.AreEqual(0.3f, GameConfig.MusicCriticalFadeIn);
        }

        [Test]
        public void GameConfig_MusicEventDuckVolume_Is30Percent()
        {
            Assert.AreEqual(0.3f, GameConfig.MusicEventDuckVolume);
        }

        [Test]
        public void GameConfig_MusicEventRestoreFade_Is1Second()
        {
            Assert.AreEqual(1.0f, GameConfig.MusicEventRestoreFade);
        }

        [Test]
        public void GameConfig_MusicShopCrossfade_Is15Seconds()
        {
            Assert.AreEqual(1.5f, GameConfig.MusicShopCrossfade);
        }

        [Test]
        public void GameConfig_MusicUrgencyVolume_Is50Percent()
        {
            Assert.AreEqual(0.5f, GameConfig.MusicUrgencyVolume);
        }

        [Test]
        public void GameConfig_MusicCriticalVolume_Is60Percent()
        {
            Assert.AreEqual(0.6f, GameConfig.MusicCriticalVolume);
        }

        [Test]
        public void GameConfig_MusicTitleAmbientVolume_Is15Percent()
        {
            Assert.AreEqual(0.15f, GameConfig.MusicTitleAmbientVolume);
        }

        [Test]
        public void GameConfig_MusicVolume_IsSet()
        {
            Assert.Greater(GameConfig.MusicVolume, 0f, "MusicVolume should be positive");
            Assert.LessOrEqual(GameConfig.MusicVolume, 1f, "MusicVolume should not exceed 1");
        }

        // ════════════════════════════════════════════════════════════════
        // MusicState Enum Values
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void MusicState_HasExpectedValues()
        {
            // Verify all expected music states exist
            Assert.IsTrue(System.Enum.IsDefined(typeof(MusicManager.MusicState), "None"));
            Assert.IsTrue(System.Enum.IsDefined(typeof(MusicManager.MusicState), "TitleScreen"));
            Assert.IsTrue(System.Enum.IsDefined(typeof(MusicManager.MusicState), "Trading"));
            Assert.IsTrue(System.Enum.IsDefined(typeof(MusicManager.MusicState), "Shop"));
            Assert.IsTrue(System.Enum.IsDefined(typeof(MusicManager.MusicState), "Victory"));
            Assert.IsTrue(System.Enum.IsDefined(typeof(MusicManager.MusicState), "Defeat"));
            Assert.IsTrue(System.Enum.IsDefined(typeof(MusicManager.MusicState), "ActTransition"));
        }

        [Test]
        public void MusicState_HasSevenValues()
        {
            var values = System.Enum.GetValues(typeof(MusicManager.MusicState));
            Assert.AreEqual(7, values.Length, "MusicState should have exactly 7 values");
        }

        // ════════════════════════════════════════════════════════════════
        // AudioClipLibrary Music Fields (AC 1)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void AudioClipLibrary_HasAllMusicFields()
        {
            var lib = new AudioClipLibrary();
            var type = typeof(AudioClipLibrary);
            var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;

            // Act music
            Assert.IsNotNull(type.GetField("MusicAct1Penny", flags), "Missing MusicAct1Penny");
            Assert.IsNotNull(type.GetField("MusicAct2LowValue", flags), "Missing MusicAct2LowValue");
            Assert.IsNotNull(type.GetField("MusicAct3MidValue", flags), "Missing MusicAct3MidValue");
            Assert.IsNotNull(type.GetField("MusicAct4BlueChip", flags), "Missing MusicAct4BlueChip");

            // Layers
            Assert.IsNotNull(type.GetField("MusicUrgencyLayer", flags), "Missing MusicUrgencyLayer");
            Assert.IsNotNull(type.GetField("MusicCriticalLayer", flags), "Missing MusicCriticalLayer");

            // Overrides
            Assert.IsNotNull(type.GetField("MusicMarketCrashOverride", flags), "Missing MusicMarketCrashOverride");
            Assert.IsNotNull(type.GetField("MusicBullRunOverride", flags), "Missing MusicBullRunOverride");

            // Phase music
            Assert.IsNotNull(type.GetField("MusicTitleScreen", flags), "Missing MusicTitleScreen");
            Assert.IsNotNull(type.GetField("MusicTitleAmbientBed", flags), "Missing MusicTitleAmbientBed");
            Assert.IsNotNull(type.GetField("MusicShop", flags), "Missing MusicShop");
            Assert.IsNotNull(type.GetField("MusicVictoryScreen", flags), "Missing MusicVictoryScreen");
            Assert.IsNotNull(type.GetField("MusicDefeatScreen", flags), "Missing MusicDefeatScreen");

            // Stingers
            Assert.IsNotNull(type.GetField("MusicVictoryFanfare", flags), "Missing MusicVictoryFanfare");
            Assert.IsNotNull(type.GetField("MusicDefeat", flags), "Missing MusicDefeat");
            Assert.IsNotNull(type.GetField("MusicMarginCall", flags), "Missing MusicMarginCall");
            Assert.IsNotNull(type.GetField("MusicActTransition", flags), "Missing MusicActTransition");
            Assert.IsNotNull(type.GetField("MusicRoundVictory", flags), "Missing MusicRoundVictory");
        }

        // ════════════════════════════════════════════════════════════════
        // Act Configuration Alignment (AC 2)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void ActMapping_MatchesGameConfig()
        {
            // Verify our act mapping aligns with GameConfig.Acts
            for (int round = 1; round <= GameConfig.TotalRounds; round++)
            {
                int expectedAct = RunContext.GetActForRound(round);
                int musicAct = MusicManager.GetActForRound(round);
                Assert.AreEqual(expectedAct, musicAct,
                    $"Act mismatch for round {round}: GameConfig says {expectedAct}, MusicManager says {musicAct}");
            }
        }

        [Test]
        public void ActMapping_AllRoundsHaveActs()
        {
            for (int round = 1; round <= 8; round++)
            {
                int act = MusicManager.GetActForRound(round);
                Assert.GreaterOrEqual(act, 1, $"Round {round} should map to act >= 1");
                Assert.LessOrEqual(act, 4, $"Round {round} should map to act <= 4");
            }
        }
    }
}
