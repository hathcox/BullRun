using NUnit.Framework;

namespace BullRun.Tests.Shop
{
    /// <summary>
    /// Tests for BondManager (Story 13.6).
    /// Covers purchase, sell (LIFO), price escalation, Rep payout, and Round 8 blocking.
    /// </summary>
    [TestFixture]
    public class BondManagerTests
    {
        private RunContext _ctx;
        private BondManager _bonds;
        private ShopTransaction _transaction;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            _ctx = new RunContext(1, 1, new Portfolio(100f));
            _ctx.Portfolio.StartRound(_ctx.Portfolio.Cash);
            _ctx.Reputation.Add(100);
            _bonds = _ctx.Bonds;
            _transaction = new ShopTransaction();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
        }

        // === AC 4: Bond price escalation ===

        [Test]
        public void GetCurrentPrice_ReturnsCorrectPricePerRound()
        {
            Assert.AreEqual(3, BondManager.GetCurrentPrice(1));
            Assert.AreEqual(5, BondManager.GetCurrentPrice(2));
            Assert.AreEqual(8, BondManager.GetCurrentPrice(3));
            Assert.AreEqual(12, BondManager.GetCurrentPrice(4));
            Assert.AreEqual(17, BondManager.GetCurrentPrice(5));
            Assert.AreEqual(23, BondManager.GetCurrentPrice(6));
            Assert.AreEqual(30, BondManager.GetCurrentPrice(7));
            Assert.AreEqual(0, BondManager.GetCurrentPrice(8));
        }

        // === AC 3: Bond purchase deducts cash ===

        [Test]
        public void Purchase_DeductsCashFromPortfolio()
        {
            float cashBefore = _ctx.Portfolio.Cash;
            var result = _bonds.Purchase(1, _ctx.Portfolio);

            Assert.AreEqual(ShopPurchaseResult.Success, result);
            Assert.AreEqual(cashBefore - 3f, _ctx.Portfolio.Cash, 0.01f);
            Assert.AreEqual(1, _ctx.BondsOwned);
        }

        [Test]
        public void Purchase_RecordsBondHistory()
        {
            _bonds.Purchase(1, _ctx.Portfolio);

            Assert.AreEqual(1, _ctx.BondPurchaseHistory.Count);
            Assert.AreEqual(1, _ctx.BondPurchaseHistory[0].RoundPurchased);
            Assert.AreEqual(3f, _ctx.BondPurchaseHistory[0].PricePaid, 0.01f);
        }

        [Test]
        public void Purchase_InsufficientCash_ReturnsInsufficientFunds()
        {
            var ctx = new RunContext(1, 7, new Portfolio(5f));
            ctx.Portfolio.StartRound(ctx.Portfolio.Cash);
            var bonds = ctx.Bonds;

            // Round 7 costs $30, only have $5
            var result = bonds.Purchase(7, ctx.Portfolio);

            Assert.AreEqual(ShopPurchaseResult.InsufficientFunds, result);
            Assert.AreEqual(0, ctx.BondsOwned);
            Assert.AreEqual(5f, ctx.Portfolio.Cash, 0.01f);
        }

        // === AC 5: Cannot purchase on Round 8 ===

        [Test]
        public void Purchase_Round8_ReturnsError()
        {
            var ctx = new RunContext(4, 8, new Portfolio(1000f));
            ctx.Portfolio.StartRound(ctx.Portfolio.Cash);
            var bonds = ctx.Bonds;

            var result = bonds.Purchase(8, ctx.Portfolio);

            Assert.AreEqual(ShopPurchaseResult.Error, result);
            Assert.AreEqual(0, ctx.BondsOwned);
        }

        [Test]
        public void CanPurchase_Round8_ReturnsFalse()
        {
            Assert.IsFalse(_bonds.CanPurchase(8, 1000f));
        }

        [Test]
        public void CanPurchase_Round1WithEnoughCash_ReturnsTrue()
        {
            Assert.IsTrue(_bonds.CanPurchase(1, 10f));
        }

        [Test]
        public void CanPurchase_Round1WithInsufficientCash_ReturnsFalse()
        {
            Assert.IsFalse(_bonds.CanPurchase(1, 2f));
        }

        // === AC 9: Sell returns half price ===

        [Test]
        public void Sell_ReturnsHalfPurchasePrice()
        {
            _bonds.Purchase(1, _ctx.Portfolio); // Buy for $3
            float cashAfterBuy = _ctx.Portfolio.Cash;

            var result = _bonds.Sell(_ctx.Portfolio);

            Assert.AreEqual(ShopPurchaseResult.Success, result);
            Assert.AreEqual(cashAfterBuy + 1.5f, _ctx.Portfolio.Cash, 0.01f); // Half of $3 = $1.50
            Assert.AreEqual(0, _ctx.BondsOwned);
        }

        [Test]
        public void GetSellPrice_ReturnsHalfOfMostRecentBondPrice()
        {
            _bonds.Purchase(1, _ctx.Portfolio); // $3
            Assert.AreEqual(1.5f, _bonds.GetSellPrice(), 0.01f);
        }

        [Test]
        public void GetSellPrice_NoBonds_ReturnsZero()
        {
            Assert.AreEqual(0f, _bonds.GetSellPrice(), 0.01f);
        }

        // === AC 10: Sell LIFO — removes most recent bond ===

        [Test]
        public void Sell_RemovesMostRecentBond_LIFO()
        {
            _bonds.Purchase(1, _ctx.Portfolio); // $3
            _ctx.CurrentRound = 2;
            _bonds.Purchase(2, _ctx.Portfolio); // $5

            Assert.AreEqual(2, _ctx.BondsOwned);

            // Sell should remove the R2 bond ($5) first (LIFO)
            _bonds.Sell(_ctx.Portfolio);

            Assert.AreEqual(1, _ctx.BondsOwned);
            Assert.AreEqual(1, _ctx.BondPurchaseHistory.Count);
            Assert.AreEqual(1, _ctx.BondPurchaseHistory[0].RoundPurchased);
        }

        [Test]
        public void Sell_NoBonds_ReturnsError()
        {
            var result = _bonds.Sell(_ctx.Portfolio);
            Assert.AreEqual(ShopPurchaseResult.Error, result);
        }

        // === AC 6, 7: Rep payout math ===

        [Test]
        public void GetRepPerRound_ReturnsCorrectAmount()
        {
            Assert.AreEqual(0, _bonds.GetRepPerRound());

            _bonds.Purchase(1, _ctx.Portfolio);
            Assert.AreEqual(1, _bonds.GetRepPerRound());

            _ctx.CurrentRound = 2;
            _bonds.Purchase(2, _ctx.Portfolio);
            Assert.AreEqual(2, _bonds.GetRepPerRound());
        }

        [Test]
        public void PayoutRep_AddsRepToReputationManager()
        {
            _bonds.Purchase(1, _ctx.Portfolio);
            int repBefore = _ctx.Reputation.Current;

            _bonds.PayoutRep(_ctx.Reputation);

            Assert.AreEqual(repBefore + 1, _ctx.Reputation.Current);
        }

        [Test]
        public void PayoutRep_NoBonds_DoesNothing()
        {
            int repBefore = _ctx.Reputation.Current;
            _bonds.PayoutRep(_ctx.Reputation);
            Assert.AreEqual(repBefore, _ctx.Reputation.Current);
        }

        // === AC 7: Cumulative Rep example ===

        [Test]
        public void CumulativeRep_BuyR1AndR2_R3PayoutIs2Rep()
        {
            // Buy bond R1
            _bonds.Purchase(1, _ctx.Portfolio);

            // Simulate R2 start: payout +1 Rep
            int repBefore = _ctx.Reputation.Current;
            _bonds.PayoutRep(_ctx.Reputation);
            Assert.AreEqual(repBefore + 1, _ctx.Reputation.Current);

            // Buy another bond R2
            _ctx.CurrentRound = 2;
            _bonds.Purchase(2, _ctx.Portfolio);

            // Simulate R3 start: payout +2 Rep (2 bonds)
            repBefore = _ctx.Reputation.Current;
            _bonds.PayoutRep(_ctx.Reputation);
            Assert.AreEqual(repBefore + 2, _ctx.Reputation.Current);

            // R4 start: still +2 Rep
            repBefore = _ctx.Reputation.Current;
            _bonds.PayoutRep(_ctx.Reputation);
            Assert.AreEqual(repBefore + 2, _ctx.Reputation.Current);
        }

        // === AC 15: Events fire ===

        [Test]
        public void Purchase_FiresBondPurchasedEvent()
        {
            BondPurchasedEvent received = default;
            bool eventFired = false;
            EventBus.Subscribe<BondPurchasedEvent>(e =>
            {
                eventFired = true;
                received = e;
            });

            _bonds.Purchase(1, _ctx.Portfolio);

            Assert.IsTrue(eventFired);
            Assert.AreEqual(1, received.Round);
            Assert.AreEqual(3f, received.PricePaid, 0.01f);
            Assert.AreEqual(1, received.TotalBondsOwned);
            Assert.AreEqual(_ctx.Portfolio.Cash, received.RemainingCash, 0.01f);
        }

        [Test]
        public void Sell_FiresBondSoldEvent()
        {
            _bonds.Purchase(1, _ctx.Portfolio);

            BondSoldEvent received = default;
            bool eventFired = false;
            EventBus.Subscribe<BondSoldEvent>(e =>
            {
                eventFired = true;
                received = e;
            });

            _bonds.Sell(_ctx.Portfolio);

            Assert.IsTrue(eventFired);
            Assert.AreEqual(1.5f, received.SellPrice, 0.01f);
            Assert.AreEqual(0, received.TotalBondsOwned);
        }

        [Test]
        public void PayoutRep_FiresBondRepPaidEvent()
        {
            _bonds.Purchase(1, _ctx.Portfolio);

            BondRepPaidEvent received = default;
            bool eventFired = false;
            EventBus.Subscribe<BondRepPaidEvent>(e =>
            {
                eventFired = true;
                received = e;
            });

            _bonds.PayoutRep(_ctx.Reputation);

            Assert.IsTrue(eventFired);
            Assert.AreEqual(1, received.BondsOwned);
            Assert.AreEqual(1, received.RepEarned);
            Assert.AreEqual(_ctx.Reputation.Current, received.TotalReputation);
        }

        // === ShopTransaction bond tests ===

        [Test]
        public void ShopTransaction_PurchaseBond_FiresBondPurchasedEvent()
        {
            BondPurchasedEvent received = default;
            bool eventFired = false;
            EventBus.Subscribe<BondPurchasedEvent>(e =>
            {
                eventFired = true;
                received = e;
            });

            var result = _transaction.PurchaseBond(_ctx, 3f);

            Assert.AreEqual(ShopPurchaseResult.Success, result);
            Assert.IsTrue(eventFired);
            Assert.AreEqual(1, received.TotalBondsOwned);
        }

        [Test]
        public void ShopTransaction_SellBond_LIFO_FiresBondSoldEvent()
        {
            _transaction.PurchaseBond(_ctx, 3f);
            _ctx.CurrentRound = 2;
            _transaction.PurchaseBond(_ctx, 5f);

            BondSoldEvent received = default;
            bool eventFired = false;
            EventBus.Subscribe<BondSoldEvent>(e =>
            {
                eventFired = true;
                received = e;
            });

            var result = _transaction.SellBond(_ctx);

            Assert.AreEqual(ShopPurchaseResult.Success, result);
            Assert.IsTrue(eventFired);
            Assert.AreEqual(2.5f, received.SellPrice, 0.01f); // Half of $5
            Assert.AreEqual(1, received.TotalBondsOwned);

            // Verify remaining bond is the R1 one
            Assert.AreEqual(1, _ctx.BondPurchaseHistory.Count);
            Assert.AreEqual(1, _ctx.BondPurchaseHistory[0].RoundPurchased);
        }

        [Test]
        public void ShopTransaction_SellBond_NoBonds_ReturnsError()
        {
            var result = _transaction.SellBond(_ctx);
            Assert.AreEqual(ShopPurchaseResult.Error, result);
        }
    }
}
