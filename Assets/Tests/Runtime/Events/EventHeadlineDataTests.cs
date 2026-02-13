using NUnit.Framework;

namespace BullRun.Tests.Events
{
    [TestFixture]
    public class EventHeadlineDataTests
    {
        [Test]
        public void GetHeadline_EarningsBeat_ContainsTicker()
        {
            var random = new System.Random(42);
            string headline = EventHeadlineData.GetHeadline(MarketEventType.EarningsBeat, "ACME", random);

            Assert.IsNotNull(headline);
            Assert.IsTrue(headline.Contains("ACME"), "Headline should contain the ticker symbol");
        }

        [Test]
        public void GetHeadline_EarningsMiss_ContainsTicker()
        {
            var random = new System.Random(42);
            string headline = EventHeadlineData.GetHeadline(MarketEventType.EarningsMiss, "ZETA", random);

            Assert.IsNotNull(headline);
            Assert.IsTrue(headline.Contains("ZETA"), "Headline should contain the ticker symbol");
        }

        [Test]
        public void GetHeadline_Deterministic_WithSameSeed()
        {
            var random1 = new System.Random(123);
            var random2 = new System.Random(123);

            string headline1 = EventHeadlineData.GetHeadline(MarketEventType.EarningsBeat, "TEST", random1);
            string headline2 = EventHeadlineData.GetHeadline(MarketEventType.EarningsBeat, "TEST", random2);

            Assert.AreEqual(headline1, headline2, "Same seed should produce same headline");
        }

        [Test]
        public void GetHeadline_AllEventTypes_HaveAtLeastOneTemplate()
        {
            var random = new System.Random(42);
            var allTypes = (MarketEventType[])System.Enum.GetValues(typeof(MarketEventType));

            foreach (var eventType in allTypes)
            {
                string headline = EventHeadlineData.GetHeadline(eventType, "TEST", random);
                Assert.IsNotNull(headline, $"Event type {eventType} should have at least one headline template");
                Assert.IsTrue(headline.Length > 0, $"Event type {eventType} headline should not be empty");
            }
        }

        [Test]
        public void GetHeadline_FallbackForUnspecificTypes()
        {
            var random = new System.Random(42);
            // PumpAndDump should have at least a fallback headline
            string headline = EventHeadlineData.GetHeadline(MarketEventType.PumpAndDump, "PUMP", random);

            Assert.IsNotNull(headline);
            Assert.IsTrue(headline.Contains("PUMP"), "Fallback headline should still contain ticker");
        }

        [Test]
        public void GetHeadline_NoTickerPlaceholderRemaining()
        {
            var random = new System.Random(42);
            var allTypes = (MarketEventType[])System.Enum.GetValues(typeof(MarketEventType));

            foreach (var eventType in allTypes)
            {
                string headline = EventHeadlineData.GetHeadline(eventType, "ACME", random);
                Assert.IsFalse(headline.Contains("{ticker}"),
                    $"Headline for {eventType} should have {"{ticker}"} substituted, got: {headline}");
            }
        }

        [Test]
        public void EarningsBeatHeadlines_HasMultipleTemplates()
        {
            Assert.GreaterOrEqual(EventHeadlineData.EarningsBeatHeadlines.Length, 3,
                "EarningsBeat should have at least 3 headline templates");
        }

        [Test]
        public void EarningsMissHeadlines_HasMultipleTemplates()
        {
            Assert.GreaterOrEqual(EventHeadlineData.EarningsMissHeadlines.Length, 3,
                "EarningsMiss should have at least 3 headline templates");
        }

        // --- IsPositiveEvent tests ---

        [Test]
        public void IsPositiveEvent_EarningsBeat_ReturnsTrue()
        {
            Assert.IsTrue(EventHeadlineData.IsPositiveEvent(MarketEventType.EarningsBeat));
        }

        [Test]
        public void IsPositiveEvent_EarningsMiss_ReturnsFalse()
        {
            Assert.IsFalse(EventHeadlineData.IsPositiveEvent(MarketEventType.EarningsMiss));
        }

        [Test]
        public void IsPositiveEvent_PumpAndDump_ReturnsTrue()
        {
            Assert.IsTrue(EventHeadlineData.IsPositiveEvent(MarketEventType.PumpAndDump));
        }

        [Test]
        public void IsPositiveEvent_SECInvestigation_ReturnsFalse()
        {
            Assert.IsFalse(EventHeadlineData.IsPositiveEvent(MarketEventType.SECInvestigation));
        }

        [Test]
        public void IsPositiveEvent_SectorRotation_ReturnsFalse()
        {
            Assert.IsFalse(EventHeadlineData.IsPositiveEvent(MarketEventType.SectorRotation));
        }

        [Test]
        public void IsPositiveEvent_MergerRumor_ReturnsTrue()
        {
            Assert.IsTrue(EventHeadlineData.IsPositiveEvent(MarketEventType.MergerRumor));
        }

        [Test]
        public void IsPositiveEvent_MarketCrash_ReturnsFalse()
        {
            Assert.IsFalse(EventHeadlineData.IsPositiveEvent(MarketEventType.MarketCrash));
        }

        [Test]
        public void IsPositiveEvent_BullRun_ReturnsTrue()
        {
            Assert.IsTrue(EventHeadlineData.IsPositiveEvent(MarketEventType.BullRun));
        }

        [Test]
        public void IsPositiveEvent_FlashCrash_ReturnsFalse()
        {
            Assert.IsFalse(EventHeadlineData.IsPositiveEvent(MarketEventType.FlashCrash));
        }

        [Test]
        public void IsPositiveEvent_ShortSqueeze_ReturnsTrue()
        {
            Assert.IsTrue(EventHeadlineData.IsPositiveEvent(MarketEventType.ShortSqueeze));
        }

        // --- Story 5-3: Tier-specific headline tests ---

        [Test]
        public void PumpAndDumpHeadlines_HasMultipleTemplates()
        {
            Assert.GreaterOrEqual(EventHeadlineData.PumpAndDumpHeadlines.Length, 3,
                "PumpAndDump should have at least 3 headline templates");
        }

        [Test]
        public void SECInvestigationHeadlines_HasMultipleTemplates()
        {
            Assert.GreaterOrEqual(EventHeadlineData.SECInvestigationHeadlines.Length, 3,
                "SECInvestigation should have at least 3 headline templates");
        }

        [Test]
        public void SectorRotationHeadlines_HasMultipleTemplates()
        {
            Assert.GreaterOrEqual(EventHeadlineData.SectorRotationHeadlines.Length, 3,
                "SectorRotation should have at least 3 headline templates");
        }

        [Test]
        public void MergerRumorHeadlines_HasMultipleTemplates()
        {
            Assert.GreaterOrEqual(EventHeadlineData.MergerRumorHeadlines.Length, 3,
                "MergerRumor should have at least 3 headline templates");
        }

        [Test]
        public void GetHeadline_PumpAndDump_ContainsTicker()
        {
            var random = new System.Random(42);
            string headline = EventHeadlineData.GetHeadline(MarketEventType.PumpAndDump, "MEME", random);
            Assert.IsTrue(headline.Contains("MEME"), $"PumpAndDump headline should contain ticker, got: {headline}");
        }

        [Test]
        public void GetHeadline_SECInvestigation_ContainsTicker()
        {
            var random = new System.Random(42);
            string headline = EventHeadlineData.GetHeadline(MarketEventType.SECInvestigation, "PUMP", random);
            Assert.IsTrue(headline.Contains("PUMP"), $"SECInvestigation headline should contain ticker, got: {headline}");
        }

        [Test]
        public void GetHeadline_SectorRotation_ContainsTicker()
        {
            var random = new System.Random(42);
            string headline = EventHeadlineData.GetHeadline(MarketEventType.SectorRotation, "NOVA", random);
            Assert.IsTrue(headline.Contains("NOVA"), $"SectorRotation headline should contain ticker, got: {headline}");
        }

        [Test]
        public void GetHeadline_MergerRumor_ContainsTicker()
        {
            var random = new System.Random(42);
            string headline = EventHeadlineData.GetHeadline(MarketEventType.MergerRumor, "TITN", random);
            Assert.IsTrue(headline.Contains("TITN"), $"MergerRumor headline should contain ticker, got: {headline}");
        }
    }
}
