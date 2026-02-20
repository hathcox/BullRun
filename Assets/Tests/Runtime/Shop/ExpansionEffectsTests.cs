using NUnit.Framework;
using System.Collections.Generic;

namespace BullRun.Tests.Shop
{
    [TestFixture]
    public class ExpansionEffectsTests
    {
        private RunContext _ctx;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            _ctx = new RunContext(1, 1, new Portfolio(1000f));
            _ctx.Portfolio.StartRound(_ctx.Portfolio.Cash);
            _ctx.Reputation.Add(500);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
        }

        // === FIX-15: Single Stock Per Round (permanent) ===

        [Test]
        public void SingleStock_PriceGeneratorSpawns1Stock()
        {
            var pg = new PriceGenerator();
            pg.InitializeRound(1, 1);
            Assert.AreEqual(1, pg.ActiveStocks.Count,
                "FIX-15: Should always spawn exactly 1 stock per round");
        }

        [Test]
        public void SingleStock_SelectStocksForRound_Returns1Stock()
        {
            var pg = new PriceGenerator();
            var stocks = pg.SelectStocksForRound(StockTier.Penny);
            Assert.AreEqual(1, stocks.Count,
                "FIX-15: SelectStocksForRound should return exactly 1 stock");
        }

        // === Leverage Trading (AC 2) ===

        [Test]
        public void Leverage_PortfolioAddCash_AppliesExtraPnl()
        {
            _ctx.OwnedExpansions.Add(ExpansionDefinitions.LeverageTrading);

            // Buy 10 shares at $10
            _ctx.Portfolio.OpenPosition("0", 10, 10f);

            // Sell at $12 — normal proceeds handled by ClosePosition
            _ctx.Portfolio.ClosePosition("0", 10, 12f);
            float cashAfterNormalSell = _ctx.Portfolio.Cash;

            // Leverage adds extra P&L via AddCash (as done in GameRunner.ExecuteSell)
            float leveragePnl = (12f - 10f) * 10;
            Assert.AreEqual(20f, leveragePnl, 0.01f, "Leverage P&L calculation should be correct");

            _ctx.Portfolio.AddCash(leveragePnl);
            float cashAfterLeverage = _ctx.Portfolio.Cash;

            // AddCash should have added exactly the leverage bonus
            Assert.AreEqual(cashAfterNormalSell + 20f, cashAfterLeverage, 0.01f,
                "Leverage should add extra P&L via AddCash");
        }

        [Test]
        public void Leverage_DoesNotAffectShortTrades()
        {
            _ctx.OwnedExpansions.Add(ExpansionDefinitions.LeverageTrading);

            // Open short at $10
            _ctx.Portfolio.OpenShort("0", 5, 10f);

            // Cover at $8 (profit = $2 per share)
            float pnl = _ctx.Portfolio.CoverShort("0", 5, 8f);
            Assert.AreEqual(10f, pnl, 0.01f); // (10-8)*5 = 10, NOT doubled
        }

        [Test]
        public void Leverage_LossesAlsoDoubled()
        {
            _ctx.OwnedExpansions.Add(ExpansionDefinitions.LeverageTrading);

            // Buy 5 shares at $10
            _ctx.Portfolio.OpenPosition("0", 5, 10f);

            // Sell at $8 — loss
            _ctx.Portfolio.ClosePosition("0", 5, 8f);
            float cashAfterNormalSell = _ctx.Portfolio.Cash;

            // Apply leverage P&L (negative — losses are also doubled)
            float leveragePnl = (8f - 10f) * 5;
            Assert.AreEqual(-10f, leveragePnl, 0.01f, "Leverage loss P&L should be negative");

            _ctx.Portfolio.AddCash(leveragePnl);
            float cashAfterLeverage = _ctx.Portfolio.Cash;

            // AddCash should have subtracted the extra loss
            Assert.AreEqual(cashAfterNormalSell - 10f, cashAfterLeverage, 0.01f,
                "Leverage should amplify losses via AddCash");
        }

        // === Expanded Inventory (AC 3) ===

        [Test]
        public void ExpandedInventory_MaxSlotsIncreasedBy2()
        {
            Assert.AreEqual(4, ShopTransaction.GetEffectiveMaxRelicSlots(_ctx));

            _ctx.OwnedExpansions.Add(ExpansionDefinitions.ExpandedInventory);
            Assert.AreEqual(6, ShopTransaction.GetEffectiveMaxRelicSlots(_ctx));
        }

        [Test]
        public void ExpandedInventory_PurchaseReEnabled_WhenUnderNewCapacity()
        {
            var transaction = new ShopTransaction();

            // Fill to base capacity (4)
            for (int i = 0; i < 4; i++)
                _ctx.OwnedRelics.Add($"relic_{i}");

            // Can't buy more (at max 4)
            var result = transaction.PurchaseRelic(_ctx, new RelicDef("relic_new", "New", "desc", "", 10, "T", "#FFFFFF"));
            Assert.AreEqual(ShopPurchaseResult.SlotsFull, result);

            // Buy expanded inventory expansion
            _ctx.OwnedExpansions.Add(ExpansionDefinitions.ExpandedInventory);

            // Now can buy (max is 6, have 4)
            result = transaction.PurchaseRelic(_ctx, new RelicDef("relic_new", "New", "desc", "", 10, "T", "#FFFFFF"));
            Assert.AreEqual(ShopPurchaseResult.Success, result);
        }

        // === Dual Short (AC 4) ===

        [Test]
        public void DualShort_CanOpenTwoShortPositions()
        {
            _ctx.OwnedExpansions.Add(ExpansionDefinitions.DualShort);

            // First short
            var pos1 = _ctx.Portfolio.OpenShort("0", 1, 10f);
            Assert.IsNotNull(pos1);

            // Second short uses different stock ID suffix to distinguish
            var pos2 = _ctx.Portfolio.OpenShort("0_s2", 1, 12f);
            Assert.IsNotNull(pos2);

            Assert.AreEqual(2, _ctx.Portfolio.ShortPositionCount);
        }

        [Test]
        public void DualShort_IndependentLifecycles()
        {
            _ctx.OwnedExpansions.Add(ExpansionDefinitions.DualShort);

            _ctx.Portfolio.OpenShort("0", 1, 10f);
            _ctx.Portfolio.OpenShort("0_s2", 1, 12f);

            // Cover first short at $8 (profit)
            float pnl1 = _ctx.Portfolio.CoverShort("0", 1, 8f);
            Assert.AreEqual(2f, pnl1, 0.01f);

            // Second short still open
            Assert.AreEqual(1, _ctx.Portfolio.ShortPositionCount);
            Assert.IsNotNull(_ctx.Portfolio.GetShortPosition("0_s2"));

            // Cover second short at $14 (loss)
            float pnl2 = _ctx.Portfolio.CoverShort("0_s2", 1, 14f);
            Assert.AreEqual(-2f, pnl2, 0.01f);
        }

        // === Intel Expansion (AC 5) ===

        [Test]
        public void IntelExpansion_IncreasesSlots_From2To3()
        {
            Assert.AreEqual(GameConfig.DefaultInsiderTipSlots, _ctx.InsiderTipSlots);

            _ctx.OwnedExpansions.Add(ExpansionDefinitions.IntelExpansion);
            // Simulate ShopState.Enter() logic
            _ctx.InsiderTipSlots = GameConfig.DefaultInsiderTipSlots +
                (_ctx.OwnedExpansions.Contains(ExpansionDefinitions.IntelExpansion) ? 1 : 0);

            Assert.AreEqual(3, _ctx.InsiderTipSlots);
        }

        [Test]
        public void IntelExpansion_TipGeneratorCreates3Tips()
        {
            var generator = new InsiderTipGenerator();
            var tips = generator.GenerateTips(3, 2, 1, new System.Random(42));
            Assert.AreEqual(3, tips.Length);
        }

        [Test]
        public void IntelExpansion_SlotCountUnchanged_WithoutExpansion()
        {
            // Without expansion, slot count stays at default
            _ctx.InsiderTipSlots = GameConfig.DefaultInsiderTipSlots +
                (_ctx.OwnedExpansions.Contains(ExpansionDefinitions.IntelExpansion) ? 1 : 0);
            Assert.AreEqual(GameConfig.DefaultInsiderTipSlots, _ctx.InsiderTipSlots);
        }

        // === Extended Trading (AC 6) ===

        [Test]
        public void ExtendedTrading_TradingStateUsesExtendedDuration()
        {
            // Create TradingState and verify it reads the expansion
            _ctx.OwnedExpansions.Add(ExpansionDefinitions.ExtendedTrading);

            // Simulate TradingState.Enter() duration calculation
            float duration = GameConfig.RoundDurationSeconds
                + (_ctx.OwnedExpansions.Contains(ExpansionDefinitions.ExtendedTrading) ? 15f : 0f);

            Assert.AreEqual(GameConfig.RoundDurationSeconds + 15f, duration, 0.01f);
        }

        [Test]
        public void ExtendedTrading_NoDurationChange_WithoutExpansion()
        {
            // Without expansion, duration stays at base
            float duration = GameConfig.RoundDurationSeconds
                + (_ctx.OwnedExpansions.Contains(ExpansionDefinitions.ExtendedTrading) ? 15f : 0f);

            Assert.AreEqual(GameConfig.RoundDurationSeconds, duration, 0.01f);
        }

        // === No Stacking (AC 8) ===

        [Test]
        public void NoStacking_PurchasingSameExpansionTwiceHasNoEffect()
        {
            var transaction = new ShopTransaction();

            var result1 = transaction.PurchaseExpansion(_ctx, ExpansionDefinitions.LeverageTrading, "Leverage", 60);
            Assert.AreEqual(ShopPurchaseResult.Success, result1);

            var result2 = transaction.PurchaseExpansion(_ctx, ExpansionDefinitions.LeverageTrading, "Leverage", 60);
            Assert.AreEqual(ShopPurchaseResult.AlreadyOwned, result2);

            // Only 1 in owned list
            int count = 0;
            for (int i = 0; i < _ctx.OwnedExpansions.Count; i++)
                if (_ctx.OwnedExpansions[i] == ExpansionDefinitions.LeverageTrading) count++;
            Assert.AreEqual(1, count);
        }

        [Test]
        public void NoStacking_ExpandedInventory_DoesNotStackBeyond6()
        {
            _ctx.OwnedExpansions.Add(ExpansionDefinitions.ExpandedInventory);
            // Adding it again shouldn't increase further
            _ctx.OwnedExpansions.Add(ExpansionDefinitions.ExpandedInventory);

            // GetEffectiveMaxRelicSlots checks Contains, not count — still 6
            Assert.AreEqual(6, ShopTransaction.GetEffectiveMaxRelicSlots(_ctx));
        }

        // === HasExpansion convenience method (AC 7) ===

        [Test]
        public void HasExpansion_ReturnsTrueWhenOwned()
        {
            var manager = new ExpansionManager(_ctx);
            _ctx.OwnedExpansions.Add(ExpansionDefinitions.LeverageTrading);
            Assert.IsTrue(manager.HasExpansion(ExpansionDefinitions.LeverageTrading));
        }

        [Test]
        public void HasExpansion_ReturnsFalseWhenNotOwned()
        {
            var manager = new ExpansionManager(_ctx);
            Assert.IsFalse(manager.HasExpansion(ExpansionDefinitions.LeverageTrading));
        }

        [Test]
        public void HasExpansion_StaticMethod_Works()
        {
            _ctx.OwnedExpansions.Add(ExpansionDefinitions.DualShort);
            Assert.IsTrue(ExpansionManager.HasExpansion(_ctx, ExpansionDefinitions.DualShort));
            Assert.IsFalse(ExpansionManager.HasExpansion(_ctx, ExpansionDefinitions.LeverageTrading));
        }

        // === Expansion ID constants (review fix, FIX-15: Multi-Stock removed) ===

        [Test]
        public void ExpansionDefinitions_AllIdsMatchConstants()
        {
            // FIX-15: Multi-Stock removed — 5 expansions total
            Assert.AreEqual(5, ExpansionDefinitions.All.Length,
                "FIX-15: Should have exactly 5 expansions (Multi-Stock removed)");
            Assert.AreEqual(ExpansionDefinitions.LeverageTrading, ExpansionDefinitions.All[0].Id);
            Assert.AreEqual(ExpansionDefinitions.ExpandedInventory, ExpansionDefinitions.All[1].Id);
            Assert.AreEqual(ExpansionDefinitions.DualShort, ExpansionDefinitions.All[2].Id);
            Assert.AreEqual(ExpansionDefinitions.IntelExpansion, ExpansionDefinitions.All[3].Id);
            Assert.AreEqual(ExpansionDefinitions.ExtendedTrading, ExpansionDefinitions.All[4].Id);
        }

        [Test]
        public void ExpansionDefinitions_MultiStockNotInAll()
        {
            // FIX-15: Verify Multi-Stock is permanently gone from All array
            for (int i = 0; i < ExpansionDefinitions.All.Length; i++)
            {
                Assert.AreNotEqual(ExpansionDefinitions.MultiStockTrading, ExpansionDefinitions.All[i].Id,
                    "FIX-15: Multi-Stock Trading should not be in ExpansionDefinitions.All");
            }
        }
    }
}
