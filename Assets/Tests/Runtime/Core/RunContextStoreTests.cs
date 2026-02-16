using NUnit.Framework;

namespace BullRun.Tests.Core
{
    [TestFixture]
    public class RunContextStoreTests
    {
        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
        }

        // === OwnedRelics initialization ===

        [Test]
        public void Constructor_OwnedRelics_DefaultsToEmptyList()
        {
            var ctx = new RunContext(1, 1, new Portfolio(1000f));
            Assert.IsNotNull(ctx.OwnedRelics);
            Assert.AreEqual(0, ctx.OwnedRelics.Count);
        }

        [Test]
        public void Constructor_OwnedExpansions_DefaultsToEmptyList()
        {
            var ctx = new RunContext(1, 1, new Portfolio(1000f));
            Assert.IsNotNull(ctx.OwnedExpansions);
            Assert.AreEqual(0, ctx.OwnedExpansions.Count);
        }

        [Test]
        public void Constructor_BondsOwned_DefaultsToZero()
        {
            var ctx = new RunContext(1, 1, new Portfolio(1000f));
            Assert.AreEqual(0, ctx.BondsOwned);
        }

        [Test]
        public void Constructor_BondPurchaseHistory_DefaultsToEmptyList()
        {
            var ctx = new RunContext(1, 1, new Portfolio(1000f));
            Assert.IsNotNull(ctx.BondPurchaseHistory);
            Assert.AreEqual(0, ctx.BondPurchaseHistory.Count);
        }

        [Test]
        public void Constructor_CurrentShopRerollCount_DefaultsToZero()
        {
            var ctx = new RunContext(1, 1, new Portfolio(1000f));
            Assert.AreEqual(0, ctx.CurrentShopRerollCount);
        }

        [Test]
        public void Constructor_InsiderTipSlots_DefaultsToGameConfigValue()
        {
            var ctx = new RunContext(1, 1, new Portfolio(1000f));
            Assert.AreEqual(GameConfig.DefaultInsiderTipSlots, ctx.InsiderTipSlots);
            Assert.AreEqual(2, ctx.InsiderTipSlots);
        }

        [Test]
        public void Constructor_RevealedTips_DefaultsToEmptyList()
        {
            var ctx = new RunContext(1, 1, new Portfolio(1000f));
            Assert.IsNotNull(ctx.RevealedTips);
            Assert.AreEqual(0, ctx.RevealedTips.Count);
        }

        // === ItemsCollected derives from OwnedRelics ===

        [Test]
        public void ItemsCollected_ReflectsOwnedRelicsCount()
        {
            var ctx = new RunContext(1, 1, new Portfolio(1000f));
            Assert.AreEqual(0, ctx.ItemsCollected);
            ctx.OwnedRelics.Add("relic-1");
            Assert.AreEqual(1, ctx.ItemsCollected);
            ctx.OwnedRelics.Add("relic-2");
            Assert.AreEqual(2, ctx.ItemsCollected);
        }

        // === Persistence: fields survive round transitions ===

        [Test]
        public void OwnedRelics_PersistsAcrossRounds()
        {
            var ctx = new RunContext(1, 1, new Portfolio(1000f));
            ctx.OwnedRelics.Add("relic-1");
            ctx.OwnedRelics.Add("relic-2");
            ctx.AdvanceRound();
            ctx.AdvanceRound();
            Assert.AreEqual(2, ctx.OwnedRelics.Count);
            Assert.IsTrue(ctx.OwnedRelics.Contains("relic-1"));
            Assert.IsTrue(ctx.OwnedRelics.Contains("relic-2"));
        }

        [Test]
        public void OwnedExpansions_PersistsAcrossRounds()
        {
            var ctx = new RunContext(1, 1, new Portfolio(1000f));
            ctx.OwnedExpansions.Add("expansion-1");
            ctx.AdvanceRound();
            ctx.AdvanceRound();
            Assert.AreEqual(1, ctx.OwnedExpansions.Count);
            Assert.IsTrue(ctx.OwnedExpansions.Contains("expansion-1"));
        }

        [Test]
        public void BondsOwned_PersistsAcrossRounds()
        {
            var ctx = new RunContext(1, 1, new Portfolio(1000f));
            ctx.BondsOwned = 3;
            ctx.AdvanceRound();
            ctx.AdvanceRound();
            Assert.AreEqual(3, ctx.BondsOwned);
        }

        [Test]
        public void BondPurchaseHistory_PersistsAcrossRounds()
        {
            var ctx = new RunContext(1, 1, new Portfolio(1000f));
            ctx.BondPurchaseHistory.Add(new BondRecord(1, 50f));
            ctx.BondPurchaseHistory.Add(new BondRecord(2, 75f));
            ctx.AdvanceRound();
            Assert.AreEqual(2, ctx.BondPurchaseHistory.Count);
            Assert.AreEqual(1, ctx.BondPurchaseHistory[0].RoundPurchased);
            Assert.AreEqual(50f, ctx.BondPurchaseHistory[0].PricePaid, 0.01f);
        }

        [Test]
        public void InsiderTipSlots_PersistsAcrossRounds()
        {
            var ctx = new RunContext(1, 1, new Portfolio(1000f));
            ctx.InsiderTipSlots = 4;
            ctx.AdvanceRound();
            ctx.AdvanceRound();
            Assert.AreEqual(4, ctx.InsiderTipSlots);
        }

        // === ResetForNewRun clears all store fields ===

        [Test]
        public void ResetForNewRun_ClearsOwnedRelics()
        {
            var ctx = new RunContext(1, 1, new Portfolio(1000f));
            ctx.Portfolio.SubscribeToPriceUpdates();
            ctx.OwnedRelics.Add("relic-1");
            ctx.ResetForNewRun();
            Assert.AreEqual(0, ctx.OwnedRelics.Count);
        }

        [Test]
        public void ResetForNewRun_ClearsOwnedExpansions()
        {
            var ctx = new RunContext(1, 1, new Portfolio(1000f));
            ctx.Portfolio.SubscribeToPriceUpdates();
            ctx.OwnedExpansions.Add("expansion-1");
            ctx.ResetForNewRun();
            Assert.AreEqual(0, ctx.OwnedExpansions.Count);
        }

        [Test]
        public void ResetForNewRun_ResetsBondsOwned()
        {
            var ctx = new RunContext(1, 1, new Portfolio(1000f));
            ctx.Portfolio.SubscribeToPriceUpdates();
            ctx.BondsOwned = 5;
            ctx.ResetForNewRun();
            Assert.AreEqual(0, ctx.BondsOwned);
        }

        [Test]
        public void ResetForNewRun_ClearsBondPurchaseHistory()
        {
            var ctx = new RunContext(1, 1, new Portfolio(1000f));
            ctx.Portfolio.SubscribeToPriceUpdates();
            ctx.BondPurchaseHistory.Add(new BondRecord(1, 100f));
            ctx.ResetForNewRun();
            Assert.AreEqual(0, ctx.BondPurchaseHistory.Count);
        }

        [Test]
        public void ResetForNewRun_ResetsCurrentShopRerollCount()
        {
            var ctx = new RunContext(1, 1, new Portfolio(1000f));
            ctx.Portfolio.SubscribeToPriceUpdates();
            ctx.CurrentShopRerollCount = 3;
            ctx.ResetForNewRun();
            Assert.AreEqual(0, ctx.CurrentShopRerollCount);
        }

        [Test]
        public void ResetForNewRun_ResetsInsiderTipSlots()
        {
            var ctx = new RunContext(1, 1, new Portfolio(1000f));
            ctx.Portfolio.SubscribeToPriceUpdates();
            ctx.InsiderTipSlots = 5;
            ctx.ResetForNewRun();
            Assert.AreEqual(GameConfig.DefaultInsiderTipSlots, ctx.InsiderTipSlots);
        }

        [Test]
        public void ResetForNewRun_ClearsRevealedTips()
        {
            var ctx = new RunContext(1, 1, new Portfolio(1000f));
            ctx.Portfolio.SubscribeToPriceUpdates();
            ctx.RevealedTips.Add(new RevealedTip(InsiderTipType.PriceDirection, "Up"));
            ctx.ResetForNewRun();
            Assert.AreEqual(0, ctx.RevealedTips.Count);
        }

        // === StartNewRun initializes all store fields ===

        [Test]
        public void StartNewRun_InitializesAllStoreFields()
        {
            var ctx = RunContext.StartNewRun();
            Assert.IsNotNull(ctx.OwnedRelics);
            Assert.AreEqual(0, ctx.OwnedRelics.Count);
            Assert.IsNotNull(ctx.OwnedExpansions);
            Assert.AreEqual(0, ctx.OwnedExpansions.Count);
            Assert.AreEqual(0, ctx.BondsOwned);
            Assert.IsNotNull(ctx.BondPurchaseHistory);
            Assert.AreEqual(0, ctx.BondPurchaseHistory.Count);
            Assert.AreEqual(0, ctx.CurrentShopRerollCount);
            Assert.AreEqual(GameConfig.DefaultInsiderTipSlots, ctx.InsiderTipSlots);
            Assert.IsNotNull(ctx.RevealedTips);
            Assert.AreEqual(0, ctx.RevealedTips.Count);
        }
    }
}
