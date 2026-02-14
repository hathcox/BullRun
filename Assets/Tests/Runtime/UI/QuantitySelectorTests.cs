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
        public void CalculateMaxShort_StandardCase_AccountsForMarginRequirement()
        {
            // $1000 / ($25 * 0.5) = 1000 / 12.5 = 80 shares
            Assert.AreEqual(80, QuantitySelector.CalculateMaxShort(1000f, 25f));
        }

        [Test]
        public void CalculateMaxShort_NonEvenDivision_FloorsResult()
        {
            // $1000 / ($30 * 0.5) = 1000 / 15 = 66.67 -> 66
            Assert.AreEqual(66, QuantitySelector.CalculateMaxShort(1000f, 30f));
        }

        [Test]
        public void CalculateMaxShort_ZeroPrice_ReturnsZero()
        {
            Assert.AreEqual(0, QuantitySelector.CalculateMaxShort(1000f, 0f));
        }

        [Test]
        public void CalculateMaxShort_ZeroCash_ReturnsZero()
        {
            Assert.AreEqual(0, QuantitySelector.CalculateMaxShort(0f, 25f));
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
            // isBuy=false, isShort=true -> CalculateMaxShort: $1000 / ($25 * 0.5) = 80
            var portfolio = new Portfolio(1000f);
            Assert.AreEqual(80, QuantitySelector.CalculateMax(false, true, 1000f, 25f, portfolio, "ACME"));
        }

        [Test]
        public void CalculateMax_CoverBuyAndShort_DelegatesToMaxCover()
        {
            // isBuy=true, isShort=true -> CalculateMaxCover: 8 short shares
            var portfolio = new Portfolio(1000f);
            portfolio.OpenShort("ACME", 8, 20f);
            Assert.AreEqual(8, QuantitySelector.CalculateMax(true, true, portfolio.Cash, 20f, portfolio, "ACME"));
        }

        // --- PresetValues (FIX-6: updated to [5, 10, 15, 25]) ---

        [Test]
        public void PresetValues_CorrectMapping()
        {
            Assert.AreEqual(5, QuantitySelector.PresetValues[(int)QuantitySelector.Preset.Five]);
            Assert.AreEqual(10, QuantitySelector.PresetValues[(int)QuantitySelector.Preset.Ten]);
            Assert.AreEqual(15, QuantitySelector.PresetValues[(int)QuantitySelector.Preset.Fifteen]);
            Assert.AreEqual(25, QuantitySelector.PresetValues[(int)QuantitySelector.Preset.TwentyFive]);
        }

        [Test]
        public void PresetLabels_CorrectMapping()
        {
            Assert.AreEqual("x5", QuantitySelector.PresetLabels[(int)QuantitySelector.Preset.Five]);
            Assert.AreEqual("x10", QuantitySelector.PresetLabels[(int)QuantitySelector.Preset.Ten]);
            Assert.AreEqual("x15", QuantitySelector.PresetLabels[(int)QuantitySelector.Preset.Fifteen]);
            Assert.AreEqual("x25", QuantitySelector.PresetLabels[(int)QuantitySelector.Preset.TwentyFive]);
        }

        [Test]
        public void PresetValues_NoMaxPreset()
        {
            // Verify there are exactly 4 presets with no zero/MAX placeholder
            Assert.AreEqual(4, QuantitySelector.PresetValues.Length);
            foreach (int val in QuantitySelector.PresetValues)
                Assert.Greater(val, 0, "All preset values should be positive (no MAX/0 placeholder)");
        }

        // --- Partial fill tests (FIX-6: updated preset values) ---

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
        public void PartialFill_ShortPreset5_OnlyAfford3_Returns3()
        {
            // $45 cash / ($30 * 0.5 margin) = 45 / 15 = 3
            int max = QuantitySelector.CalculateMaxShort(45f, 30f);
            int qty = Mathf.Min(5, max);
            Assert.AreEqual(3, qty);
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

        // --- DefaultTradeQuantity in GameConfig ---

        [Test]
        public void GameConfig_DefaultTradeQuantity_Is10()
        {
            Assert.AreEqual(10, GameConfig.DefaultTradeQuantity);
        }

        // --- Smart routing tests (FIX-6) ---
        // These test the static logic that ExecuteSmartBuy/ExecuteSmartSell rely on.
        // The routing decision is: check position type -> determine trade type -> get quantity.

        [Test]
        public void SmartBuy_NoPosition_RoutesBuy()
        {
            // No position -> smart buy should route to regular buy
            var portfolio = new Portfolio(1000f);
            var position = portfolio.GetPosition("ACME");
            // No position: isBuy=true, isShort=false
            Assert.IsNull(position);
            int max = QuantitySelector.CalculateMax(true, false, 1000f, 25f, portfolio, "ACME");
            Assert.AreEqual(40, max); // $1000/$25 = 40 buyable
        }

        [Test]
        public void SmartBuy_LongPosition_RoutesBuy()
        {
            // Has long position -> smart buy should still route to buy (add to position)
            var portfolio = new Portfolio(1000f);
            portfolio.OpenPosition("ACME", 5, 25f); // cost $125, cash = $875
            var position = portfolio.GetPosition("ACME");
            Assert.IsNotNull(position);
            Assert.IsFalse(position.IsShort);
            int max = QuantitySelector.CalculateMax(true, false, portfolio.Cash, 25f, portfolio, "ACME");
            Assert.AreEqual(35, max); // $875/$25 = 35 buyable
        }

        [Test]
        public void SmartBuy_ShortPosition_RoutesCover()
        {
            // Has short position -> smart buy should route to cover
            var portfolio = new Portfolio(1000f);
            portfolio.OpenShort("ACME", 10, 20f);
            var position = portfolio.GetPosition("ACME");
            Assert.IsNotNull(position);
            Assert.IsTrue(position.IsShort);
            int max = QuantitySelector.CalculateMax(true, true, portfolio.Cash, 20f, portfolio, "ACME");
            Assert.AreEqual(10, max); // 10 short shares coverable
        }

        [Test]
        public void SmartSell_LongPosition_RoutesSell()
        {
            // Has long position -> smart sell should route to sell
            var portfolio = new Portfolio(1000f);
            portfolio.OpenPosition("ACME", 15, 10f);
            var position = portfolio.GetPosition("ACME");
            Assert.IsNotNull(position);
            Assert.IsFalse(position.IsShort);
            Assert.Greater(position.Shares, 0);
            int max = QuantitySelector.CalculateMax(false, false, portfolio.Cash, 10f, portfolio, "ACME");
            Assert.AreEqual(15, max); // 15 shares sellable
        }

        [Test]
        public void SmartSell_NoPosition_RoutesShort()
        {
            // No position -> smart sell should route to short (implicit shorting)
            var portfolio = new Portfolio(1000f);
            var position = portfolio.GetPosition("ACME");
            Assert.IsNull(position);
            int max = QuantitySelector.CalculateMax(false, true, 1000f, 25f, portfolio, "ACME");
            Assert.AreEqual(80, max); // $1000/($25*0.5) = 80 shortable
        }

        [Test]
        public void SmartSell_ShortPosition_RoutesShort()
        {
            // Has short position -> smart sell should route to short (add to short)
            var portfolio = new Portfolio(1000f);
            portfolio.OpenShort("ACME", 5, 20f); // margin $50, cash = $950
            var position = portfolio.GetPosition("ACME");
            Assert.IsNotNull(position);
            Assert.IsTrue(position.IsShort);
            int max = QuantitySelector.CalculateMax(false, true, portfolio.Cash, 20f, portfolio, "ACME");
            // $950 / ($20 * 0.5) = 950 / 10 = 95 additional shortable
            Assert.AreEqual(95, max);
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

        // --- GetCurrentQuantity integration tests (FIX-6: verifies instance method with preset + partial fill) ---

        [Test]
        public void GetCurrentQuantity_DefaultPreset_BuyFullAmount_Returns10()
        {
            var go = new GameObject("TestQS");
            try
            {
                var qs = go.AddComponent<QuantitySelector>();
                qs.Initialize(null, null, null);
                var portfolio = new Portfolio(1000f);
                int qty = qs.GetCurrentQuantity(true, false, "ACME", 25f, portfolio);
                Assert.AreEqual(10, qty); // Can afford 40, preset is 10
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void GetCurrentQuantity_DefaultPreset_BuyPartialFill_Returns7()
        {
            var go = new GameObject("TestQS");
            try
            {
                var qs = go.AddComponent<QuantitySelector>();
                qs.Initialize(null, null, null);
                var portfolio = new Portfolio(175f);
                int qty = qs.GetCurrentQuantity(true, false, "ACME", 25f, portfolio);
                Assert.AreEqual(7, qty); // Can afford 7, preset is 10, clamped to 7
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void GetCurrentQuantity_Preset25_SellPartialFill_Returns5()
        {
            var go = new GameObject("TestQS");
            try
            {
                var qs = go.AddComponent<QuantitySelector>();
                qs.Initialize(null, null, null);
                qs.SelectPreset(QuantitySelector.Preset.TwentyFive);
                var portfolio = new Portfolio(1000f);
                portfolio.OpenPosition("ACME", 5, 25f);
                int qty = qs.GetCurrentQuantity(false, false, "ACME", 25f, portfolio);
                Assert.AreEqual(5, qty); // Only 5 shares held, preset is 25, clamped to 5
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void GetCurrentQuantity_Preset5_CoverPartialFill_Returns4()
        {
            var go = new GameObject("TestQS");
            try
            {
                var qs = go.AddComponent<QuantitySelector>();
                qs.Initialize(null, null, null);
                qs.SelectPreset(QuantitySelector.Preset.Five);
                var portfolio = new Portfolio(1000f);
                portfolio.OpenShort("ACME", 4, 25f);
                int qty = qs.GetCurrentQuantity(true, true, "ACME", 25f, portfolio);
                Assert.AreEqual(4, qty); // Only 4 short shares, preset is 5, clamped to 4
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
