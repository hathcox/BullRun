using NUnit.Framework;
using UnityEngine;

namespace BullRun.Tests.UI
{
    [TestFixture]
    public class QuantitySelectorTests
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

        // --- CalculateMaxBuy ---

        [Test]
        public void CalculateMaxBuy_StandardCase_ReturnsFloorDivision()
        {
            // $1000 / $25 = 40 shares
            Assert.AreEqual(40, QuantitySelector.CalculateMaxBuy(1000f, 25f));
        }

        [Test]
        public void CalculateMaxBuy_NonEvenDivision_FloorsResult()
        {
            // $1000 / $30 = 33.33 -> 33
            Assert.AreEqual(33, QuantitySelector.CalculateMaxBuy(1000f, 30f));
        }

        [Test]
        public void CalculateMaxBuy_ZeroPrice_ReturnsZero()
        {
            Assert.AreEqual(0, QuantitySelector.CalculateMaxBuy(1000f, 0f));
        }

        [Test]
        public void CalculateMaxBuy_NegativePrice_ReturnsZero()
        {
            Assert.AreEqual(0, QuantitySelector.CalculateMaxBuy(1000f, -5f));
        }

        [Test]
        public void CalculateMaxBuy_ZeroCash_ReturnsZero()
        {
            Assert.AreEqual(0, QuantitySelector.CalculateMaxBuy(0f, 25f));
        }

        [Test]
        public void CalculateMaxBuy_CheapStock_ReturnsHighCount()
        {
            // $1000 / $0.50 = 2000 shares (penny stock scenario)
            Assert.AreEqual(2000, QuantitySelector.CalculateMaxBuy(1000f, 0.5f));
        }

        // --- CalculateMaxShort ---

        [Test]
        public void CalculateMaxShort_AlwaysReturnsBaseShares()
        {
            // No capital required — always returns ShortBaseShares (1)
            Assert.AreEqual(GameConfig.ShortBaseShares, QuantitySelector.CalculateMaxShort(1000f, 25f));
        }

        [Test]
        public void CalculateMaxShort_DifferentPrice_StillReturnsBaseShares()
        {
            // No capital required — always returns ShortBaseShares (1)
            Assert.AreEqual(GameConfig.ShortBaseShares, QuantitySelector.CalculateMaxShort(1000f, 30f));
        }

        [Test]
        public void CalculateMaxShort_ZeroPrice_ReturnsZero()
        {
            Assert.AreEqual(0, QuantitySelector.CalculateMaxShort(1000f, 0f));
        }

        [Test]
        public void CalculateMaxShort_ZeroCash_StillReturnsBaseShares()
        {
            // No capital required — cash is irrelevant
            Assert.AreEqual(GameConfig.ShortBaseShares, QuantitySelector.CalculateMaxShort(0f, 25f));
        }

        // --- CalculateMaxSell ---

        [Test]
        public void CalculateMaxSell_LongPosition_ReturnsShareCount()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenPosition("ACME", 15, 10f);
            Assert.AreEqual(15, QuantitySelector.CalculateMaxSell(portfolio, "ACME"));
        }

        [Test]
        public void CalculateMaxSell_NoPosition_ReturnsZero()
        {
            var portfolio = new Portfolio(1000f);
            Assert.AreEqual(0, QuantitySelector.CalculateMaxSell(portfolio, "ACME"));
        }

        [Test]
        public void CalculateMaxSell_ShortPosition_ReturnsZero()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenShort("ACME", 10, 20f);
            Assert.AreEqual(0, QuantitySelector.CalculateMaxSell(portfolio, "ACME"));
        }

        // --- CalculateMaxCover ---

        [Test]
        public void CalculateMaxCover_ShortPosition_ReturnsShareCount()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenShort("ACME", 8, 20f);
            Assert.AreEqual(8, QuantitySelector.CalculateMaxCover(portfolio, "ACME"));
        }

        [Test]
        public void CalculateMaxCover_NoPosition_ReturnsZero()
        {
            var portfolio = new Portfolio(1000f);
            Assert.AreEqual(0, QuantitySelector.CalculateMaxCover(portfolio, "ACME"));
        }

        [Test]
        public void CalculateMaxCover_LongPosition_ReturnsZero()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenPosition("ACME", 10, 25f);
            Assert.AreEqual(0, QuantitySelector.CalculateMaxCover(portfolio, "ACME"));
        }

        // --- CalculateMax (routing logic) ---

        [Test]
        public void CalculateMax_BuyNotShort_DelegatesToMaxBuy()
        {
            // isBuy=true, isShort=false -> CalculateMaxBuy: $1000 / $25 = 40
            var portfolio = new Portfolio(1000f);
            Assert.AreEqual(40, QuantitySelector.CalculateMax(true, false, 1000f, 25f, portfolio, "ACME"));
        }

        [Test]
        public void CalculateMax_SellNotShort_DelegatesToMaxSell()
        {
            // isBuy=false, isShort=false -> CalculateMaxSell: 15 shares held
            var portfolio = new Portfolio(1000f);
            portfolio.OpenPosition("ACME", 15, 10f);
            Assert.AreEqual(15, QuantitySelector.CalculateMax(false, false, portfolio.Cash, 10f, portfolio, "ACME"));
        }

        [Test]
        public void CalculateMax_ShortNotBuy_DelegatesToMaxShort()
        {
            // isBuy=false, isShort=true -> CalculateMaxShort: ShortBaseShares (1)
            var portfolio = new Portfolio(1000f);
            Assert.AreEqual(GameConfig.ShortBaseShares, QuantitySelector.CalculateMax(false, true, 1000f, 25f, portfolio, "ACME"));
        }

        [Test]
        public void CalculateMax_CoverBuyAndShort_DelegatesToMaxCover()
        {
            // isBuy=true, isShort=true -> CalculateMaxCover: 8 short shares
            var portfolio = new Portfolio(1000f);
            portfolio.OpenShort("ACME", 8, 20f);
            Assert.AreEqual(8, QuantitySelector.CalculateMax(true, true, portfolio.Cash, 20f, portfolio, "ACME"));
        }

        // --- FIX-13: Default quantity is now 1 ---

        [Test]
        public void GameConfig_DefaultTradeQuantity_Is1()
        {
            Assert.AreEqual(1, GameConfig.DefaultTradeQuantity);
        }

        // --- Button colors ---

        [Test]
        public void ActiveButtonColor_IsGreen()
        {
            Assert.AreEqual(new Color(0f, 0.5f, 0.25f, 1f), QuantitySelector.ActiveButtonColor);
        }

        [Test]
        public void InactiveButtonColor_IsDarkBlue()
        {
            Assert.AreEqual(new Color(0.12f, 0.14f, 0.25f, 0.8f), QuantitySelector.InactiveButtonColor);
        }

        // --- Smart routing tests (FIX-6) ---

        [Test]
        public void SmartBuy_NoPosition_RoutesBuy()
        {
            var portfolio = new Portfolio(1000f);
            var position = portfolio.GetPosition("ACME");
            Assert.IsNull(position);
            int max = QuantitySelector.CalculateMax(true, false, 1000f, 25f, portfolio, "ACME");
            Assert.AreEqual(40, max);
        }

        [Test]
        public void SmartBuy_LongPosition_RoutesBuy()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenPosition("ACME", 5, 25f);
            var position = portfolio.GetPosition("ACME");
            Assert.IsNotNull(position);
            Assert.IsFalse(position.IsShort);
            int max = QuantitySelector.CalculateMax(true, false, portfolio.Cash, 25f, portfolio, "ACME");
            Assert.AreEqual(35, max);
        }

        [Test]
        public void SmartBuy_ShortPosition_RoutesCover()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenShort("ACME", 10, 20f);
            var position = portfolio.GetShortPosition("ACME");
            Assert.IsNotNull(position);
            Assert.IsTrue(position.IsShort);
            int max = QuantitySelector.CalculateMax(true, true, portfolio.Cash, 20f, portfolio, "ACME");
            Assert.AreEqual(10, max);
        }

        [Test]
        public void SmartSell_LongPosition_RoutesSell()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenPosition("ACME", 15, 10f);
            var position = portfolio.GetPosition("ACME");
            Assert.IsNotNull(position);
            Assert.IsFalse(position.IsShort);
            Assert.Greater(position.Shares, 0);
            int max = QuantitySelector.CalculateMax(false, false, portfolio.Cash, 10f, portfolio, "ACME");
            Assert.AreEqual(15, max);
        }

        [Test]
        public void SmartSell_NoPosition_RoutesShort()
        {
            var portfolio = new Portfolio(1000f);
            var position = portfolio.GetPosition("ACME");
            Assert.IsNull(position);
            int max = QuantitySelector.CalculateMax(false, true, 1000f, 25f, portfolio, "ACME");
            Assert.AreEqual(GameConfig.ShortBaseShares, max);
        }

        [Test]
        public void SmartSell_ShortPosition_RoutesShort()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenShort("ACME", 5, 20f);
            var position = portfolio.GetShortPosition("ACME");
            Assert.IsNotNull(position);
            Assert.IsTrue(position.IsShort);
            int max = QuantitySelector.CalculateMax(false, true, portfolio.Cash, 20f, portfolio, "ACME");
            Assert.AreEqual(GameConfig.ShortBaseShares, max);
        }

        // --- TradeButtonPressedEvent ---

        [Test]
        public void TradeButtonPressedEvent_BuyPublishAndSubscribe()
        {
            TradeButtonPressedEvent received = default;
            bool eventFired = false;
            EventBus.Subscribe<TradeButtonPressedEvent>(e => { received = e; eventFired = true; });

            EventBus.Publish(new TradeButtonPressedEvent { IsBuy = true });

            Assert.IsTrue(eventFired);
            Assert.IsTrue(received.IsBuy);
        }

        [Test]
        public void TradeButtonPressedEvent_SellPublishAndSubscribe()
        {
            TradeButtonPressedEvent received = default;
            bool eventFired = false;
            EventBus.Subscribe<TradeButtonPressedEvent>(e => { received = e; eventFired = true; });

            EventBus.Publish(new TradeButtonPressedEvent { IsBuy = false });

            Assert.IsTrue(eventFired);
            Assert.IsFalse(received.IsBuy);
        }

        // --- GetCurrentQuantity integration tests (FIX-13: default is x1) ---

        [Test]
        public void GetCurrentQuantity_DefaultX1_BuyFullAmount_Returns1()
        {
            var go = new GameObject("TestQS");
            try
            {
                var qs = go.AddComponent<QuantitySelector>();
                qs.Initialize();
                var portfolio = new Portfolio(1000f);
                int qty = qs.GetCurrentQuantity(true, false, "ACME", 25f, portfolio);
                Assert.AreEqual(1, qty); // FIX-13: Default is now x1
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void GetCurrentQuantity_DefaultX1_BuyPartialFill_Returns0()
        {
            var go = new GameObject("TestQS");
            try
            {
                var qs = go.AddComponent<QuantitySelector>();
                qs.Initialize();
                var portfolio = new Portfolio(0f);
                int qty = qs.GetCurrentQuantity(true, false, "ACME", 25f, portfolio);
                Assert.AreEqual(0, qty); // Can't afford even 1 share
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        // --- Partial fill tests ---

        [Test]
        public void PartialFill_BuyPreset10_OnlyAfford7_Returns7()
        {
            // $175 cash / $25 price = 7 affordable, preset is 10
            int max = QuantitySelector.CalculateMaxBuy(175f, 25f);
            int qty = Mathf.Min(10, max);
            Assert.AreEqual(7, qty);
        }

        [Test]
        public void PartialFill_BuyPreset25_OnlyAfford12_Returns12()
        {
            // $300 cash / $25 price = 12 affordable, preset is 25
            int max = QuantitySelector.CalculateMaxBuy(300f, 25f);
            int qty = Mathf.Min(25, max);
            Assert.AreEqual(12, qty);
        }

        [Test]
        public void PartialFill_SellPreset15_Only5Shares_Returns5()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenPosition("ACME", 5, 25f);
            int max = QuantitySelector.CalculateMaxSell(portfolio, "ACME");
            int qty = Mathf.Min(15, max);
            Assert.AreEqual(5, qty);
        }

        [Test]
        public void PartialFill_Short_AlwaysBaseShares()
        {
            // No capital required — always ShortBaseShares (1)
            int max = QuantitySelector.CalculateMaxShort(45f, 30f);
            int qty = Mathf.Min(5, max);
            Assert.AreEqual(GameConfig.ShortBaseShares, qty);
        }

        [Test]
        public void PartialFill_CoverPreset10_Only4Shorted_Returns4()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenShort("ACME", 4, 25f);
            int max = QuantitySelector.CalculateMaxCover(portfolio, "ACME");
            int qty = Mathf.Min(10, max);
            Assert.AreEqual(4, qty);
        }

        // --- Quantity persistence and reset via EventBus ---

        [Test]
        public void RoundStartedEvent_PublishAndSubscribe()
        {
            RoundStartedEvent received = default;
            bool eventFired = false;
            EventBus.Subscribe<RoundStartedEvent>(e => { received = e; eventFired = true; });

            EventBus.Publish(new RoundStartedEvent
            {
                RoundNumber = 2, Act = 1,
                TierDisplayName = "Penny Stocks",
                MarginCallTarget = 200f, TimeLimit = 60f
            });

            Assert.IsTrue(eventFired);
            Assert.AreEqual(2, received.RoundNumber);
        }
    }
}
