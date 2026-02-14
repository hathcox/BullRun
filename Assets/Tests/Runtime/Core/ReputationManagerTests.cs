using NUnit.Framework;

namespace BullRun.Tests.Core
{
    /// <summary>
    /// FIX-12: Tests for ReputationManager — shop currency tracking.
    /// Covers AC 1-5, 11, 12.
    /// </summary>
    [TestFixture]
    public class ReputationManagerTests
    {
        private ReputationManager _rep;

        [SetUp]
        public void SetUp()
        {
            _rep = new ReputationManager();
        }

        // === AC 12: Reputation starts at 0 for a new run ===

        [Test]
        public void New_StartsAtZero()
        {
            Assert.AreEqual(0, _rep.Current);
        }

        // === AC 1: Add increases balance correctly ===

        [Test]
        public void Add_IncreasesBalance()
        {
            _rep.Add(100);
            Assert.AreEqual(100, _rep.Current);
        }

        [Test]
        public void Add_AccumulatesMultipleCalls()
        {
            _rep.Add(50);
            _rep.Add(75);
            _rep.Add(25);
            Assert.AreEqual(150, _rep.Current);
        }

        [Test]
        public void Add_IgnoresZeroOrNegative()
        {
            _rep.Add(100);
            _rep.Add(0);
            _rep.Add(-50);
            Assert.AreEqual(100, _rep.Current, "Zero and negative amounts should be ignored");
        }

        // === AC 1: Spend decreases balance correctly ===

        [Test]
        public void Spend_DecreasesBalance()
        {
            _rep.Add(500);
            bool result = _rep.Spend(200);

            Assert.IsTrue(result);
            Assert.AreEqual(300, _rep.Current);
        }

        [Test]
        public void Spend_ToZero()
        {
            _rep.Add(100);
            bool result = _rep.Spend(100);

            Assert.IsTrue(result);
            Assert.AreEqual(0, _rep.Current);
        }

        // === Edge: Spend rejects if insufficient (balance unchanged) ===

        [Test]
        public void Spend_RejectsIfInsufficient()
        {
            _rep.Add(50);
            bool result = _rep.Spend(100);

            Assert.IsFalse(result);
            Assert.AreEqual(50, _rep.Current, "Balance should be unchanged on rejection");
        }

        [Test]
        public void Spend_ZeroAmount_IsNoOpSuccess()
        {
            _rep.Add(100);
            bool result = _rep.Spend(0);

            Assert.IsTrue(result, "Spending 0 should succeed (no-op) to match CanAfford(0)=true");
            Assert.AreEqual(100, _rep.Current, "Balance should be unchanged");
        }

        [Test]
        public void Spend_RejectsNegativeAmount()
        {
            _rep.Add(100);
            bool result = _rep.Spend(-10);

            Assert.IsFalse(result);
            Assert.AreEqual(100, _rep.Current);
        }

        // === AC 1: CanAfford returns true/false correctly ===

        [Test]
        public void CanAfford_ReturnsTrueWhenSufficient()
        {
            _rep.Add(200);
            Assert.IsTrue(_rep.CanAfford(150));
        }

        [Test]
        public void CanAfford_ReturnsTrueWhenExact()
        {
            _rep.Add(100);
            Assert.IsTrue(_rep.CanAfford(100));
        }

        [Test]
        public void CanAfford_ReturnsFalseWhenInsufficient()
        {
            _rep.Add(50);
            Assert.IsFalse(_rep.CanAfford(100));
        }

        [Test]
        public void CanAfford_ReturnsTrueForZeroCost()
        {
            Assert.IsTrue(_rep.CanAfford(0));
        }

        [Test]
        public void CanAfford_ReturnsFalseForNegativeCost()
        {
            _rep.Add(100);
            Assert.IsFalse(_rep.CanAfford(-1));
        }

        // === Reset restores to 0 ===

        [Test]
        public void Reset_SetsToZero()
        {
            _rep.Add(999);
            _rep.Reset();
            Assert.AreEqual(0, _rep.Current);
        }

        // === AC 2: Reputation persists across rounds (not reset on round start) ===

        [Test]
        public void Reputation_PersistsAcrossRounds()
        {
            var ctx = new RunContext(1, 1, new Portfolio(1000f));
            ctx.Reputation.Add(500);

            // Simulate round advancement (does NOT reset Reputation)
            ctx.Portfolio.StartRound(ctx.Portfolio.Cash);
            ctx.AdvanceRound();

            Assert.AreEqual(500, ctx.Reputation.Current, "Rep should persist across rounds");
        }

        // === AC 5: Shop purchase deducts Rep, NOT Portfolio.Cash ===

        [Test]
        public void ShopPurchase_DeductsRepNotCash()
        {
            EventBus.Clear();
            var ctx = new RunContext(1, 1, new Portfolio(1000f));
            ctx.Portfolio.StartRound(ctx.Portfolio.Cash);
            ctx.Reputation.Add(500);

            var transaction = new ShopTransaction();
            var item = new ShopItemDef("test", "Test", "desc", 200,
                ItemRarity.Common, ItemCategory.TradingTool);

            var result = transaction.TryPurchase(ctx, item);

            Assert.AreEqual(ShopPurchaseResult.Success, result);
            Assert.AreEqual(300, ctx.Reputation.Current, "Rep should be deducted");
            Assert.AreEqual(1000f, ctx.Portfolio.Cash, 0.01f, "Cash must NOT be touched");
            EventBus.Clear();
        }

        // === AC 12: Reputation starts at 0 on new run (via RunContext) ===

        [Test]
        public void RunContext_StartsWithZeroReputation()
        {
            EventBus.Clear();
            var ctx = RunContext.StartNewRun();
            Assert.AreEqual(0, ctx.Reputation.Current);
            EventBus.Clear();
        }

        [Test]
        public void RunContext_ResetForNewRun_ResetsReputation()
        {
            EventBus.Clear();
            var ctx = new RunContext(1, 1, new Portfolio(1000f));
            ctx.Reputation.Add(999);

            ctx.ResetForNewRun();

            Assert.AreEqual(0, ctx.Reputation.Current, "Reputation should reset on new run");
            EventBus.Clear();
        }

        // === AC 11: BUY button disabled when insufficient Rep ===
        // (This is a UI behavior verified in ShopUI, but we verify the underlying CanAfford logic)

        [Test]
        public void CanAfford_AfterSpend_ReflectsNewBalance()
        {
            _rep.Add(300);
            _rep.Spend(200);

            Assert.IsTrue(_rep.CanAfford(100));
            Assert.IsFalse(_rep.CanAfford(200));
        }
    }
}
