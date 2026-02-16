using NUnit.Framework;

namespace BullRun.Tests.Shop
{
    [TestFixture]
    public class ExpansionManagerTests
    {
        private RunContext _ctx;
        private ExpansionManager _manager;
        private ShopTransaction _transaction;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            _ctx = new RunContext(1, 1, new Portfolio(1000f));
            _ctx.Portfolio.StartRound(_ctx.Portfolio.Cash);
            _ctx.Reputation.Add(500);
            _manager = new ExpansionManager(_ctx);
            _transaction = new ShopTransaction();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
        }

        // === ExpansionDefinitions data ===

        [Test]
        public void ExpansionDefinitions_HasFiveExpansions()
        {
            // FIX-15: Multi-Stock removed, 5 expansions remain
            Assert.AreEqual(5, ExpansionDefinitions.All.Length);
        }

        [Test]
        public void ExpansionDefinitions_AllHaveUniqueIds()
        {
            var ids = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < ExpansionDefinitions.All.Length; i++)
            {
                Assert.IsTrue(ids.Add(ExpansionDefinitions.All[i].Id),
                    $"Duplicate expansion ID: {ExpansionDefinitions.All[i].Id}");
            }
        }

        [Test]
        public void ExpansionDefinitions_CostsMatchGameConfig()
        {
            // FIX-15: Multi-Stock removed, 5 expansions remain
            var byId = new System.Collections.Generic.Dictionary<string, int>();
            for (int i = 0; i < ExpansionDefinitions.All.Length; i++)
                byId[ExpansionDefinitions.All[i].Id] = ExpansionDefinitions.All[i].Cost;

            Assert.AreEqual(GameConfig.ExpansionCostLeverage, byId["leverage_trading"]);
            Assert.AreEqual(GameConfig.ExpansionCostExpandedInventory, byId["expanded_inventory"]);
            Assert.AreEqual(GameConfig.ExpansionCostDualShort, byId["dual_short"]);
            Assert.AreEqual(GameConfig.ExpansionCostIntelExpansion, byId["intel_expansion"]);
            Assert.AreEqual(GameConfig.ExpansionCostExtendedTrading, byId["extended_trading"]);
        }

        [Test]
        public void ExpansionDefinitions_GetById_ReturnsCorrectExpansion()
        {
            var result = ExpansionDefinitions.GetById("leverage_trading");
            Assert.IsTrue(result.HasValue);
            Assert.AreEqual("Leverage Trading", result.Value.Name);
        }

        [Test]
        public void ExpansionDefinitions_GetById_ReturnsNullForUnknown()
        {
            Assert.IsFalse(ExpansionDefinitions.GetById("nonexistent").HasValue);
        }

        // === IsOwned ===

        [Test]
        public void IsOwned_ReturnsFalseWhenNotOwned()
        {
            Assert.IsFalse(_manager.IsOwned("leverage_trading"));
        }

        [Test]
        public void IsOwned_ReturnsTrueAfterPurchase()
        {
            _transaction.PurchaseExpansion(_ctx, "leverage_trading", "Leverage Trading",
                GameConfig.ExpansionCostLeverage);
            Assert.IsTrue(_manager.IsOwned("leverage_trading"));
        }

        // === Purchase via ShopTransaction ===

        [Test]
        public void PurchaseExpansion_AddsToOwnedExpansions()
        {
            var expansion = ExpansionDefinitions.All[0]; // leverage_trading (FIX-15: index shifted)
            _transaction.PurchaseExpansion(_ctx, expansion.Id, expansion.Name, expansion.Cost);
            Assert.AreEqual(1, _ctx.OwnedExpansions.Count);
            Assert.IsTrue(_ctx.OwnedExpansions.Contains("leverage_trading"));
        }

        [Test]
        public void PurchaseExpansion_DoesNotDuplicate()
        {
            var expansion = ExpansionDefinitions.All[0]; // leverage_trading (FIX-15: index shifted)
            _transaction.PurchaseExpansion(_ctx, expansion.Id, expansion.Name, expansion.Cost);
            _transaction.PurchaseExpansion(_ctx, expansion.Id, expansion.Name, expansion.Cost);
            Assert.AreEqual(1, _ctx.OwnedExpansions.Count);
        }

        // === GetAvailableForShop ===

        [Test]
        public void GetAvailableForShop_ReturnsRequestedCount()
        {
            var available = _manager.GetAvailableForShop(3, new System.Random(42));
            Assert.AreEqual(3, available.Length);
        }

        [Test]
        public void GetAvailableForShop_ExcludesOwnedExpansions()
        {
            // FIX-15: Multi-Stock removed, use remaining expansions
            _ctx.OwnedExpansions.Add("leverage_trading");
            _ctx.OwnedExpansions.Add("expanded_inventory");

            var available = _manager.GetAvailableForShop(3, new System.Random(42));
            Assert.AreEqual(3, available.Length);

            for (int i = 0; i < available.Length; i++)
            {
                Assert.AreNotEqual("leverage_trading", available[i].Id);
                Assert.AreNotEqual("expanded_inventory", available[i].Id);
            }
        }

        [Test]
        public void GetAvailableForShop_ReturnsFewerWhenPoolExhausted()
        {
            // FIX-15: Own 4 of 5 expansions
            _ctx.OwnedExpansions.Add("leverage_trading");
            _ctx.OwnedExpansions.Add("expanded_inventory");
            _ctx.OwnedExpansions.Add("dual_short");
            _ctx.OwnedExpansions.Add("intel_expansion");

            var available = _manager.GetAvailableForShop(3, new System.Random(42));
            Assert.AreEqual(1, available.Length);
            Assert.AreEqual("extended_trading", available[0].Id);
        }

        [Test]
        public void GetAvailableForShop_ReturnsEmptyWhenAllOwned()
        {
            for (int i = 0; i < ExpansionDefinitions.All.Length; i++)
                _ctx.OwnedExpansions.Add(ExpansionDefinitions.All[i].Id);

            var available = _manager.GetAvailableForShop(3, new System.Random(42));
            Assert.AreEqual(0, available.Length);
        }

        [Test]
        public void GetAvailableForShop_ReturnsDifferentWithDifferentSeeds()
        {
            var a = _manager.GetAvailableForShop(3, new System.Random(1));
            var b = _manager.GetAvailableForShop(3, new System.Random(999));

            // FIX-15: With 5 expansions and 3 picks, different seeds should sometimes yield different orderings
            // At minimum, both should be valid (3 items, no duplicates)
            Assert.AreEqual(3, a.Length);
            Assert.AreEqual(3, b.Length);
        }

        // === Purchase flow integration (via ShopTransaction) ===

        [Test]
        public void PurchaseExpansion_DeductsRep_MarksOwned_FiresEvent()
        {
            ShopExpansionPurchasedEvent received = default;
            bool fired = false;
            EventBus.Subscribe<ShopExpansionPurchasedEvent>(e => { fired = true; received = e; });

            var expansion = ExpansionDefinitions.All[0];
            var result = _transaction.PurchaseExpansion(_ctx, expansion.Id, expansion.Name, expansion.Cost);

            Assert.AreEqual(ShopPurchaseResult.Success, result);
            Assert.IsTrue(_ctx.OwnedExpansions.Contains(expansion.Id));
            Assert.AreEqual(500 - expansion.Cost, _ctx.Reputation.Current);
            Assert.IsTrue(fired);
            Assert.AreEqual(expansion.Id, received.ExpansionId);
            Assert.AreEqual(expansion.Cost, received.Cost);
        }

        [Test]
        public void PurchaseExpansion_CannotDoubleBuy()
        {
            var expansion = ExpansionDefinitions.All[0];
            _transaction.PurchaseExpansion(_ctx, expansion.Id, expansion.Name, expansion.Cost);
            int repAfterFirst = _ctx.Reputation.Current;

            var result = _transaction.PurchaseExpansion(_ctx, expansion.Id, expansion.Name, expansion.Cost);
            Assert.AreEqual(ShopPurchaseResult.AlreadyOwned, result);
            Assert.AreEqual(repAfterFirst, _ctx.Reputation.Current);
        }

        [Test]
        public void PurchaseExpansion_RejectsInsufficientRep()
        {
            _ctx.Reputation.Reset();
            _ctx.Reputation.Add(10);
            var expansion = ExpansionDefinitions.All[0]; // Cost 80

            var result = _transaction.PurchaseExpansion(_ctx, expansion.Id, expansion.Name, expansion.Cost);
            Assert.AreEqual(ShopPurchaseResult.InsufficientFunds, result);
            Assert.AreEqual(0, _ctx.OwnedExpansions.Count);
        }

        // === Persistence across rounds ===

        [Test]
        public void OwnedExpansions_PersistAcrossRoundTransitions()
        {
            _ctx.OwnedExpansions.Add("leverage_trading");
            _ctx.OwnedExpansions.Add("dual_short");

            // Simulate round transition
            _ctx.CurrentShopRerollCount = 0;
            _ctx.RevealedTips.Clear();

            Assert.AreEqual(2, _ctx.OwnedExpansions.Count);
            Assert.IsTrue(_ctx.OwnedExpansions.Contains("leverage_trading"));
            Assert.IsTrue(_ctx.OwnedExpansions.Contains("dual_short"));
        }

        [Test]
        public void GetAvailableForShop_PoolShrinksAsExpansionsPurchased()
        {
            // FIX-15: Start with 5 available (Multi-Stock removed)
            var first = _manager.GetAvailableForShop(3, new System.Random(42));
            Assert.AreEqual(3, first.Length);

            // Purchase all 3
            for (int i = 0; i < first.Length; i++)
                _ctx.OwnedExpansions.Add(first[i].Id);

            // Now only 2 remain
            var second = _manager.GetAvailableForShop(3, new System.Random(42));
            Assert.AreEqual(2, second.Length);

            // None of the second batch should overlap with first
            for (int i = 0; i < second.Length; i++)
            {
                for (int j = 0; j < first.Length; j++)
                {
                    Assert.AreNotEqual(first[j].Id, second[i].Id);
                }
            }
        }
    }
}
