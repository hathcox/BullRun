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
            // $1000 / $30 = 33.33 → 33
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
            // $1000 / ($30 * 0.5) = 1000 / 15 = 66.67 → 66
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
            // isBuy=true, isShort=false → CalculateMaxBuy: $1000 / $25 = 40
            var portfolio = new Portfolio(1000f);
            Assert.AreEqual(40, QuantitySelector.CalculateMax(true, false, 1000f, 25f, portfolio, "ACME"));
        }

        [Test]
        public void CalculateMax_SellNotShort_DelegatesToMaxSell()
        {
            // isBuy=false, isShort=false → CalculateMaxSell: 15 shares held
            var portfolio = new Portfolio(1000f);
            portfolio.OpenPosition("ACME", 15, 10f);
            Assert.AreEqual(15, QuantitySelector.CalculateMax(false, false, portfolio.Cash, 10f, portfolio, "ACME"));
        }

        [Test]
        public void CalculateMax_ShortNotBuy_DelegatesToMaxShort()
        {
            // isBuy=false, isShort=true → CalculateMaxShort: $1000 / ($25 * 0.5) = 80
            var portfolio = new Portfolio(1000f);
            Assert.AreEqual(80, QuantitySelector.CalculateMax(false, true, 1000f, 25f, portfolio, "ACME"));
        }

        [Test]
        public void CalculateMax_CoverBuyAndShort_DelegatesToMaxCover()
        {
            // isBuy=true, isShort=true → CalculateMaxCover: 8 short shares
            var portfolio = new Portfolio(1000f);
            portfolio.OpenShort("ACME", 8, 20f);
            Assert.AreEqual(8, QuantitySelector.CalculateMax(true, true, portfolio.Cash, 20f, portfolio, "ACME"));
        }

        // --- PresetValues ---

        [Test]
        public void PresetValues_CorrectMapping()
        {
            Assert.AreEqual(1, QuantitySelector.PresetValues[(int)QuantitySelector.Preset.One]);
            Assert.AreEqual(5, QuantitySelector.PresetValues[(int)QuantitySelector.Preset.Five]);
            Assert.AreEqual(10, QuantitySelector.PresetValues[(int)QuantitySelector.Preset.Ten]);
            Assert.AreEqual(0, QuantitySelector.PresetValues[(int)QuantitySelector.Preset.Max]);
        }

        // --- GetCurrentQuantity (requires MonoBehaviour, tested via static helpers) ---

        // The following tests validate the static calculation methods that
        // GetCurrentQuantity delegates to. Full integration with the MonoBehaviour
        // preset state is verified by testing through the Portfolio/trade flow.

        [Test]
        public void PartialFill_BuyPreset10_OnlyAfford7_Returns7()
        {
            // $175 cash / $25 price = 7 affordable, preset is 10
            int max = QuantitySelector.CalculateMaxBuy(175f, 25f);
            int qty = Mathf.Min(10, max);
            Assert.AreEqual(7, qty);
        }

        [Test]
        public void PartialFill_SellPreset10_Only5Shares_Returns5()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenPosition("ACME", 5, 25f);
            int max = QuantitySelector.CalculateMaxSell(portfolio, "ACME");
            int qty = Mathf.Min(10, max);
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

        // --- Cycle order ---

        [Test]
        public void CyclePreset_OrderIsCorrect()
        {
            // Verify the mathematical cycle: Ten(2) → Max(3) → One(0) → Five(1) → Ten(2)
            int current = (int)QuantitySelector.Preset.Ten; // 2
            current = (current + 1) % 4; // 3 = Max
            Assert.AreEqual((int)QuantitySelector.Preset.Max, current);

            current = (current + 1) % 4; // 0 = One
            Assert.AreEqual((int)QuantitySelector.Preset.One, current);

            current = (current + 1) % 4; // 1 = Five
            Assert.AreEqual((int)QuantitySelector.Preset.Five, current);

            current = (current + 1) % 4; // 2 = Ten
            Assert.AreEqual((int)QuantitySelector.Preset.Ten, current);
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
    }
}
