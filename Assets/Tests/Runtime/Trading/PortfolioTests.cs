using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace BullRun.Tests.Trading
{
    [TestFixture]
    public class PortfolioTests
    {
        [Test]
        public void Constructor_InitializesWithStartingCapital()
        {
            var portfolio = new Portfolio(GameConfig.StartingCapital);
            Assert.AreEqual(GameConfig.StartingCapital, portfolio.Cash, 0.001f);
        }

        [Test]
        public void CanAfford_SufficientCash_ReturnsTrue()
        {
            var portfolio = new Portfolio(1000f);
            Assert.IsTrue(portfolio.CanAfford(500f));
        }

        [Test]
        public void CanAfford_ExactCash_ReturnsTrue()
        {
            var portfolio = new Portfolio(1000f);
            Assert.IsTrue(portfolio.CanAfford(1000f));
        }

        [Test]
        public void CanAfford_InsufficientCash_ReturnsFalse()
        {
            var portfolio = new Portfolio(1000f);
            Assert.IsFalse(portfolio.CanAfford(1500f));
        }

        [Test]
        public void CanAfford_ZeroCost_ReturnsTrue()
        {
            var portfolio = new Portfolio(1000f);
            Assert.IsTrue(portfolio.CanAfford(0f));
        }

        [Test]
        public void OpenPosition_DeductsCash()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenPosition("ACME", 10, 25.00f);
            Assert.AreEqual(750f, portfolio.Cash, 0.001f); // 1000 - (10*25)
        }

        [Test]
        public void OpenPosition_ReturnsPosition()
        {
            var portfolio = new Portfolio(1000f);
            var pos = portfolio.OpenPosition("ACME", 10, 25.00f);
            Assert.IsNotNull(pos);
            Assert.AreEqual("ACME", pos.StockId);
            Assert.AreEqual(10, pos.Shares);
            Assert.AreEqual(25.00f, pos.AverageBuyPrice, 0.001f);
        }

        [Test]
        public void OpenPosition_AddsSharesToExistingPosition()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenPosition("ACME", 5, 20.00f);
            portfolio.OpenPosition("ACME", 5, 30.00f);

            var pos = portfolio.GetPosition("ACME");
            Assert.AreEqual(10, pos.Shares);
            Assert.AreEqual(25.00f, pos.AverageBuyPrice, 0.001f); // (5*20 + 5*30) / 10 = 25
        }

        [Test]
        public void GetPosition_Exists_ReturnsPosition()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenPosition("ACME", 10, 25.00f);
            var pos = portfolio.GetPosition("ACME");
            Assert.IsNotNull(pos);
            Assert.AreEqual("ACME", pos.StockId);
        }

        [Test]
        public void GetPosition_NotExists_ReturnsNull()
        {
            var portfolio = new Portfolio(1000f);
            var pos = portfolio.GetPosition("NONE");
            Assert.IsNull(pos);
        }

        [Test]
        public void GetAllPositions_ReturnsAllOpenPositions()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenPosition("AAA", 5, 10.00f);
            portfolio.OpenPosition("BBB", 3, 20.00f);
            var positions = portfolio.GetAllPositions();
            Assert.AreEqual(2, positions.Count);
        }

        [Test]
        public void GetAllPositions_Empty_ReturnsEmptyList()
        {
            var portfolio = new Portfolio(1000f);
            var positions = portfolio.GetAllPositions();
            Assert.AreEqual(0, positions.Count);
        }

        // --- GetTotalValue Tests (Story 2.4 Task 1) ---

        [Test]
        public void GetTotalValue_CashOnly_ReturnsCash()
        {
            var portfolio = new Portfolio(1000f);
            float total = portfolio.GetTotalValue(id => 0f);
            Assert.AreEqual(1000f, total, 0.001f);
        }

        [Test]
        public void GetTotalValue_WithLongPositions_ReturnsCashPlusMarketValues()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenPosition("ACME", 10, 25.00f); // costs 250, leaves 750 cash
            float total = portfolio.GetTotalValue(id => 30.00f); // long value = 10*30 = 300
            Assert.AreEqual(1050f, total, 0.001f); // 750 + 300
        }

        [Test]
        public void GetTotalValue_WithShortPosition_UsesMarginPlusUnrealizedPnL()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenShort("ACME", 10, 50.00f); // margin = 250, cash = 750
            // Short at 50, current price 40 => unrealizedPnL = (50-40)*10 = +100
            // Short value = marginHeld(250) + unrealizedPnL(100) = 350
            float total = portfolio.GetTotalValue(id => 40.00f);
            Assert.AreEqual(1100f, total, 0.001f); // 750 + 350
        }

        [Test]
        public void GetTotalValue_WithShortAtLoss_ShortValueCanBeNegative()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenShort("ACME", 10, 50.00f); // margin = 250, cash = 750
            // Short at 50, current price 80 => unrealizedPnL = (50-80)*10 = -300
            // Short value = marginHeld(250) + unrealizedPnL(-300) = -50
            float total = portfolio.GetTotalValue(id => 80.00f);
            Assert.AreEqual(700f, total, 0.001f); // 750 + (-50)
        }

        [Test]
        public void GetTotalValue_MixedLongAndShort_CalculatesBothCorrectly()
        {
            var portfolio = new Portfolio(2000f);
            portfolio.OpenPosition("AAA", 10, 20.00f); // cost 200, cash = 1800
            portfolio.OpenShort("BBB", 5, 40.00f); // margin = 100, cash = 1700
            // AAA long value = 10*25 = 250
            // BBB short: unrealizedPnL = (40-30)*5 = +50, value = 100 + 50 = 150
            float total = portfolio.GetTotalValue(id => id == "AAA" ? 25.00f : 30.00f);
            Assert.AreEqual(2100f, total, 0.001f); // 1700 + 250 + 150
        }

        // --- GetTotalUnrealizedPnL Tests (Story 2.4 Task 1) ---

        [Test]
        public void GetTotalUnrealizedPnL_NoPositions_ReturnsZero()
        {
            var portfolio = new Portfolio(1000f);
            float pnl = portfolio.GetTotalUnrealizedPnL(id => 50.00f);
            Assert.AreEqual(0f, pnl, 0.001f);
        }

        [Test]
        public void GetTotalUnrealizedPnL_LongPosition_ReturnsCorrectPnL()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenPosition("ACME", 10, 25.00f);
            float pnl = portfolio.GetTotalUnrealizedPnL(id => 30.00f);
            Assert.AreEqual(50.00f, pnl, 0.001f); // (30-25)*10
        }

        [Test]
        public void GetTotalUnrealizedPnL_ShortPosition_ReturnsCorrectPnL()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenShort("ACME", 10, 50.00f);
            float pnl = portfolio.GetTotalUnrealizedPnL(id => 40.00f);
            Assert.AreEqual(100.00f, pnl, 0.001f); // (50-40)*10
        }

        [Test]
        public void GetTotalUnrealizedPnL_MultiplePositions_SumsAllPnL()
        {
            var portfolio = new Portfolio(2000f);
            portfolio.OpenPosition("AAA", 10, 20.00f); // pnl = (25-20)*10 = +50
            portfolio.OpenShort("BBB", 5, 40.00f); // pnl = (40-30)*5 = +50
            float pnl = portfolio.GetTotalUnrealizedPnL(id => id == "AAA" ? 25.00f : 30.00f);
            Assert.AreEqual(100.00f, pnl, 0.001f); // 50 + 50
        }

        // --- ClosePosition Tests (Story 2.2) ---

        [Test]
        public void ClosePosition_FullSell_AddsCashBack()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenPosition("ACME", 10, 25.00f); // cash: 750
            portfolio.ClosePosition("ACME", 10, 30.00f); // cash: 750 + 300 = 1050
            Assert.AreEqual(1050f, portfolio.Cash, 0.001f);
        }

        [Test]
        public void ClosePosition_FullSell_RemovesPosition()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenPosition("ACME", 10, 25.00f);
            portfolio.ClosePosition("ACME", 10, 30.00f);
            Assert.IsNull(portfolio.GetPosition("ACME"));
        }

        [Test]
        public void ClosePosition_FullSell_ReturnsRealizedPnL()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenPosition("ACME", 10, 25.00f);
            float pnl = portfolio.ClosePosition("ACME", 10, 30.00f);
            Assert.AreEqual(50.00f, pnl, 0.001f); // (30-25)*10
        }

        [Test]
        public void ClosePosition_PartialSell_ReducesShares()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenPosition("ACME", 10, 25.00f);
            portfolio.ClosePosition("ACME", 5, 30.00f);
            var pos = portfolio.GetPosition("ACME");
            Assert.IsNotNull(pos);
            Assert.AreEqual(5, pos.Shares);
        }

        [Test]
        public void ClosePosition_PartialSell_KeepsAverageBuyPrice()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenPosition("ACME", 10, 25.00f);
            portfolio.ClosePosition("ACME", 5, 30.00f);
            var pos = portfolio.GetPosition("ACME");
            Assert.AreEqual(25.00f, pos.AverageBuyPrice, 0.001f);
        }

        [Test]
        public void ClosePosition_PartialSell_AddsCashForSharesSold()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenPosition("ACME", 10, 25.00f); // cash: 750
            portfolio.ClosePosition("ACME", 5, 30.00f); // cash: 750 + 150 = 900
            Assert.AreEqual(900f, portfolio.Cash, 0.001f);
        }

        [Test]
        public void ClosePosition_NoPosition_ReturnsZero()
        {
            var portfolio = new Portfolio(1000f);
            float pnl = portfolio.ClosePosition("NONE", 10, 30.00f);
            Assert.AreEqual(0f, pnl, 0.001f);
            Assert.AreEqual(1000f, portfolio.Cash, 0.001f);
        }

        [Test]
        public void ClosePosition_MoreSharesThanHeld_ReturnsZero()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenPosition("ACME", 5, 25.00f); // cash: 875
            float pnl = portfolio.ClosePosition("ACME", 10, 30.00f); // trying to sell 10 but only have 5
            Assert.AreEqual(0f, pnl, 0.001f);
            Assert.AreEqual(875f, portfolio.Cash, 0.001f); // unchanged
        }

        [Test]
        public void ClosePosition_AtLoss_ReturnsNegativePnL()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenPosition("ACME", 10, 25.00f); // cash: 750
            float pnl = portfolio.ClosePosition("ACME", 10, 20.00f); // cash: 750 + 200 = 950
            Assert.AreEqual(-50.00f, pnl, 0.001f);
            Assert.AreEqual(950f, portfolio.Cash, 0.001f);
        }

        // --- OpenShort Tests (Story 2.3) ---

        [Test]
        public void OpenShort_DeductsMarginFromCash()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenShort("ACME", 10, 50.00f); // margin = 10*50*0.5 = 250
            Assert.AreEqual(750f, portfolio.Cash, 0.001f);
        }

        [Test]
        public void OpenShort_ReturnsShortPosition()
        {
            var portfolio = new Portfolio(1000f);
            var pos = portfolio.OpenShort("ACME", 10, 50.00f);
            Assert.IsNotNull(pos);
            Assert.IsTrue(pos.IsShort);
            Assert.AreEqual(10, pos.Shares);
            Assert.AreEqual(50.00f, pos.AverageBuyPrice, 0.001f);
        }

        [Test]
        public void OpenShort_SetsMarginHeld()
        {
            var portfolio = new Portfolio(1000f);
            var pos = portfolio.OpenShort("ACME", 10, 50.00f);
            Assert.AreEqual(250.00f, pos.MarginHeld, 0.001f);
        }

        [Test]
        public void OpenShort_InsufficientCash_ReturnsNull()
        {
            var portfolio = new Portfolio(100f);
            var pos = portfolio.OpenShort("ACME", 10, 50.00f); // margin = 250, have 100
            Assert.IsNull(pos);
            Assert.AreEqual(100f, portfolio.Cash, 0.001f);
        }

        // --- CoverShort Tests (Story 2.3) ---

        [Test]
        public void CoverShort_PriceDown_ReturnsMarginPlusProfit()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenShort("ACME", 10, 50.00f); // margin: 250, cash: 750
            float pnl = portfolio.CoverShort("ACME", 10, 30.00f); // pnl = (50-30)*10 = +200
            Assert.AreEqual(200.00f, pnl, 0.001f);
            Assert.AreEqual(1200f, portfolio.Cash, 0.001f); // 750 + 250 + 200
        }

        [Test]
        public void CoverShort_PriceUp_ReturnsMarginMinusLoss()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenShort("ACME", 10, 50.00f); // margin: 250, cash: 750
            float pnl = portfolio.CoverShort("ACME", 10, 60.00f); // pnl = (50-60)*10 = -100
            Assert.AreEqual(-100.00f, pnl, 0.001f);
            Assert.AreEqual(900f, portfolio.Cash, 0.001f); // 750 + 250 - 100
        }

        [Test]
        public void CoverShort_LossExceedsMargin_CashFloorsAtZeroReturn()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenShort("ACME", 10, 50.00f); // margin: 250, cash: 750
            float pnl = portfolio.CoverShort("ACME", 10, 100.00f); // pnl = (50-100)*10 = -500, margin+pnl = 250-500 = -250 -> 0
            Assert.AreEqual(-500.00f, pnl, 0.001f);
            Assert.AreEqual(750f, portfolio.Cash, 0.001f); // 750 + 0 (margin eaten)
        }

        [Test]
        public void CoverShort_RemovesPosition()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenShort("ACME", 10, 50.00f);
            portfolio.CoverShort("ACME", 10, 30.00f);
            Assert.IsNull(portfolio.GetPosition("ACME"));
        }

        [Test]
        public void CoverShort_PartialCover_ReducesShares()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenShort("ACME", 10, 50.00f); // margin: 250
            portfolio.CoverShort("ACME", 5, 30.00f); // cover half
            var pos = portfolio.GetPosition("ACME");
            Assert.IsNotNull(pos);
            Assert.AreEqual(5, pos.Shares);
            Assert.IsTrue(pos.IsShort);
        }

        [Test]
        public void CoverShort_PartialCover_ReturnsProportionalMargin()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenShort("ACME", 10, 50.00f); // margin: 250, cash: 750
            portfolio.CoverShort("ACME", 5, 30.00f); // margin portion: 125, pnl: (50-30)*5 = 100
            // cash = 750 + 125 + 100 = 975
            Assert.AreEqual(975f, portfolio.Cash, 0.001f);
        }

        [Test]
        public void CoverShort_NoShortPosition_ReturnsZero()
        {
            var portfolio = new Portfolio(1000f);
            float pnl = portfolio.CoverShort("NONE", 10, 30.00f);
            Assert.AreEqual(0f, pnl, 0.001f);
        }

        [Test]
        public void CoverShort_LongPosition_ReturnsZero()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenPosition("ACME", 10, 25.00f); // long position
            float pnl = portfolio.CoverShort("ACME", 10, 30.00f);
            Assert.AreEqual(0f, pnl, 0.001f);
        }

        [Test]
        public void CoverShort_MoreThanHeld_ReturnsZero()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenShort("ACME", 5, 50.00f);
            float pnl = portfolio.CoverShort("ACME", 10, 30.00f);
            Assert.AreEqual(0f, pnl, 0.001f);
        }
        // --- Event-Driven Price Cache Tests (Story 2.4 Task 3) ---

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

        [Test]
        public void SubscribeToPriceUpdates_CachesPriceOnEvent()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.SubscribeToPriceUpdates();
            portfolio.OpenPosition("1", 10, 25.00f); // cash: 750

            EventBus.Publish(new PriceUpdatedEvent { StockId = 1, NewPrice = 30.00f });

            float total = portfolio.GetTotalValue();
            Assert.AreEqual(1050f, total, 0.001f); // 750 + 10*30
        }

        [Test]
        public void GetTotalValue_NoArg_UsesCachedPrices()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.SubscribeToPriceUpdates();
            portfolio.OpenPosition("1", 10, 20.00f); // cash: 800

            EventBus.Publish(new PriceUpdatedEvent { StockId = 1, NewPrice = 25.00f });

            Assert.AreEqual(1050f, portfolio.GetTotalValue(), 0.001f); // 800 + 10*25
        }

        [Test]
        public void GetTotalValue_NoArg_NoCachedPrice_UsesZero()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.SubscribeToPriceUpdates();
            portfolio.OpenPosition("1", 10, 20.00f); // cash: 800
            // No price event published — cached price defaults to 0
            Assert.AreEqual(800f, portfolio.GetTotalValue(), 0.001f); // 800 + 10*0
        }

        [Test]
        public void GetTotalUnrealizedPnL_NoArg_UsesCachedPrices()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.SubscribeToPriceUpdates();
            portfolio.OpenPosition("1", 10, 20.00f);

            EventBus.Publish(new PriceUpdatedEvent { StockId = 1, NewPrice = 25.00f });

            Assert.AreEqual(50f, portfolio.GetTotalUnrealizedPnL(), 0.001f); // (25-20)*10
        }

        [Test]
        public void GetRoundProfit_NoArg_UsesCachedPrices()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.SubscribeToPriceUpdates();
            portfolio.StartRound(1000f);
            portfolio.OpenPosition("1", 10, 25.00f); // cash: 750

            EventBus.Publish(new PriceUpdatedEvent { StockId = 1, NewPrice = 30.00f });

            Assert.AreEqual(50f, portfolio.GetRoundProfit(), 0.001f); // (750 + 300) - 1000
        }

        [Test]
        public void SubscribeToPriceUpdates_OnlyUpdatesAffectedStock()
        {
            var portfolio = new Portfolio(2000f);
            portfolio.SubscribeToPriceUpdates();
            portfolio.OpenPosition("1", 10, 20.00f); // cash: 1800
            portfolio.OpenPosition("2", 5, 30.00f);  // cash: 1650

            EventBus.Publish(new PriceUpdatedEvent { StockId = 1, NewPrice = 25.00f });
            // Stock 2 has no cached price yet — uses 0
            Assert.AreEqual(1900f, portfolio.GetTotalValue(), 0.001f); // 1650 + 10*25 + 5*0

            EventBus.Publish(new PriceUpdatedEvent { StockId = 2, NewPrice = 35.00f });
            Assert.AreEqual(2075f, portfolio.GetTotalValue(), 0.001f); // 1650 + 10*25 + 5*35
        }

        [Test]
        public void UnsubscribeFromPriceUpdates_StopsCachingPrices()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.SubscribeToPriceUpdates();
            portfolio.OpenPosition("1", 10, 20.00f); // cash: 800

            EventBus.Publish(new PriceUpdatedEvent { StockId = 1, NewPrice = 25.00f });
            Assert.AreEqual(1050f, portfolio.GetTotalValue(), 0.001f);

            portfolio.UnsubscribeFromPriceUpdates();
            EventBus.Publish(new PriceUpdatedEvent { StockId = 1, NewPrice = 50.00f });
            // Should still have old cached price of 25
            Assert.AreEqual(1050f, portfolio.GetTotalValue(), 0.001f);
        }

        // --- UI Read Accessors Tests (Story 2.4 Task 4) ---

        [Test]
        public void PositionCount_NoPositions_ReturnsZero()
        {
            var portfolio = new Portfolio(1000f);
            Assert.AreEqual(0, portfolio.PositionCount);
        }

        [Test]
        public void PositionCount_WithPositions_ReturnsCount()
        {
            var portfolio = new Portfolio(2000f);
            portfolio.OpenPosition("AAA", 5, 10.00f);
            portfolio.OpenPosition("BBB", 3, 20.00f);
            Assert.AreEqual(2, portfolio.PositionCount);
        }

        [Test]
        public void PositionCount_AfterClosing_Decrements()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenPosition("ACME", 10, 25.00f);
            Assert.AreEqual(1, portfolio.PositionCount);
            portfolio.ClosePosition("ACME", 10, 30.00f);
            Assert.AreEqual(0, portfolio.PositionCount);
        }

        [Test]
        public void GetPositionPnL_LongPosition_ReturnsUnrealizedPnL()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenPosition("ACME", 10, 25.00f);
            float pnl = portfolio.GetPositionPnL("ACME", 30.00f);
            Assert.AreEqual(50.00f, pnl, 0.001f); // (30-25)*10
        }

        [Test]
        public void GetPositionPnL_ShortPosition_ReturnsUnrealizedPnL()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenShort("ACME", 10, 50.00f);
            float pnl = portfolio.GetPositionPnL("ACME", 40.00f);
            Assert.AreEqual(100.00f, pnl, 0.001f); // (50-40)*10
        }

        [Test]
        public void GetPositionPnL_NoPosition_ReturnsZero()
        {
            var portfolio = new Portfolio(1000f);
            float pnl = portfolio.GetPositionPnL("NONE", 30.00f);
            Assert.AreEqual(0f, pnl, 0.001f);
        }

        [Test]
        public void HasPosition_Exists_ReturnsTrue()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenPosition("ACME", 10, 25.00f);
            Assert.IsTrue(portfolio.HasPosition("ACME"));
        }

        [Test]
        public void HasPosition_NotExists_ReturnsFalse()
        {
            var portfolio = new Portfolio(1000f);
            Assert.IsFalse(portfolio.HasPosition("NONE"));
        }

        [Test]
        public void GetAllPositions_ReturnsIReadOnlyCollection()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenPosition("ACME", 10, 25.00f);
            IReadOnlyCollection<Position> positions = portfolio.GetAllPositions();
            Assert.AreEqual(1, positions.Count);
        }

        // --- Cash Floor Tests (Story 2.5 Task 3) ---

        [Test]
        public void OpenPosition_InsufficientCash_ReturnsNull()
        {
            var portfolio = new Portfolio(100f);
            var pos = portfolio.OpenPosition("ACME", 10, 25.00f); // cost 250, exceeds 100
            Assert.IsNull(pos);
            Assert.AreEqual(100f, portfolio.Cash, 0.001f); // cash unchanged
        }

        [Test]
        public void DeductCash_ReducesCash()
        {
            var portfolio = new Portfolio(1000f);
            bool result = portfolio.DeductCash(200f);
            Assert.IsTrue(result);
            Assert.AreEqual(800f, portfolio.Cash, 0.001f);
        }

        [Test]
        public void DeductCash_InsufficientCash_ReturnsFalse()
        {
            var portfolio = new Portfolio(100f);
            bool result = portfolio.DeductCash(200f);
            Assert.IsFalse(result);
            Assert.AreEqual(100f, portfolio.Cash, 0.001f);
        }

        [Test]
        public void DeductCash_ExactAmount_Succeeds()
        {
            var portfolio = new Portfolio(500f);
            bool result = portfolio.DeductCash(500f);
            Assert.IsTrue(result);
            Assert.AreEqual(0f, portfolio.Cash, 0.001f);
        }

        [Test]
        public void Cash_FlooredAtZero_AfterLiquidation_ShortLossExceedsMargin()
        {
            var portfolio = new Portfolio(300f);
            portfolio.OpenShort("ACME", 10, 30.00f); // margin = 150, cash = 150
            // price goes to 100: pnl = (30-100)*10 = -700, margin+pnl = 150-700 = -550 -> cashReturn 0
            float pnl = portfolio.LiquidateAllPositions(id => 100.00f);
            Assert.GreaterOrEqual(portfolio.Cash, 0f);
        }

        // --- Liquidation Tests (Story 2.5 Task 2) ---

        [Test]
        public void LiquidateAllPositions_NoPositions_ReturnsZero()
        {
            var portfolio = new Portfolio(1000f);
            float pnl = portfolio.LiquidateAllPositions(id => 50.00f);
            Assert.AreEqual(0f, pnl, 0.001f);
            Assert.AreEqual(1000f, portfolio.Cash, 0.001f);
        }

        [Test]
        public void LiquidateAllPositions_SingleLongAtProfit_AddsProceedsToCash()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenPosition("ACME", 10, 25.00f); // cash: 750
            float pnl = portfolio.LiquidateAllPositions(id => 30.00f);
            Assert.AreEqual(50.00f, pnl, 0.001f); // (30-25)*10
            Assert.AreEqual(1050f, portfolio.Cash, 0.001f); // 750 + 10*30
        }

        [Test]
        public void LiquidateAllPositions_SingleLongAtLoss_AddsProceedsToCash()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenPosition("ACME", 10, 25.00f); // cash: 750
            float pnl = portfolio.LiquidateAllPositions(id => 20.00f);
            Assert.AreEqual(-50.00f, pnl, 0.001f); // (20-25)*10
            Assert.AreEqual(950f, portfolio.Cash, 0.001f); // 750 + 10*20
        }

        [Test]
        public void LiquidateAllPositions_SingleShortAtProfit_ReturnsMarginPlusPnL()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenShort("ACME", 10, 50.00f); // margin: 250, cash: 750
            float pnl = portfolio.LiquidateAllPositions(id => 40.00f);
            Assert.AreEqual(100.00f, pnl, 0.001f); // (50-40)*10
            Assert.AreEqual(1100f, portfolio.Cash, 0.001f); // 750 + 250 + 100
        }

        [Test]
        public void LiquidateAllPositions_SingleShortAtLoss_ReturnsMarginMinusLoss()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenShort("ACME", 10, 50.00f); // margin: 250, cash: 750
            float pnl = portfolio.LiquidateAllPositions(id => 60.00f);
            Assert.AreEqual(-100.00f, pnl, 0.001f); // (50-60)*10
            Assert.AreEqual(900f, portfolio.Cash, 0.001f); // 750 + 250 - 100
        }

        [Test]
        public void LiquidateAllPositions_ClearsAllPositions()
        {
            var portfolio = new Portfolio(2000f);
            portfolio.OpenPosition("AAA", 10, 20.00f);
            portfolio.OpenShort("BBB", 5, 40.00f);
            portfolio.LiquidateAllPositions(id => 25.00f);
            Assert.AreEqual(0, portfolio.PositionCount);
        }

        [Test]
        public void LiquidateAllPositions_MixedPositions_ReturnsTotalPnL()
        {
            var portfolio = new Portfolio(2000f);
            portfolio.OpenPosition("AAA", 10, 20.00f); // cash: 1800
            portfolio.OpenShort("BBB", 5, 40.00f);     // margin: 100, cash: 1700
            // AAA: pnl = (25-20)*10 = +50, proceeds = 250
            // BBB: pnl = (40-25)*5 = +75, return = 100 + 75 = 175
            float pnl = portfolio.LiquidateAllPositions(id => 25.00f);
            Assert.AreEqual(125.00f, pnl, 0.001f); // 50 + 75
            Assert.AreEqual(2125f, portfolio.Cash, 0.001f); // 1700 + 250 + 175
        }

        // --- Round Profit Tracking Tests (Story 2.4 Task 2) ---

        [Test]
        public void StartRound_CapturesBaselineValue()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.StartRound(1000f);
            float profit = portfolio.GetRoundProfit(id => 0f);
            Assert.AreEqual(0f, profit, 0.001f);
        }

        [Test]
        public void GetRoundProfit_PositiveGain_ReturnsCorrectProfit()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.StartRound(1000f);
            portfolio.OpenPosition("ACME", 10, 25.00f); // cash: 750
            // Total value now: 750 + 10*30 = 1050
            float profit = portfolio.GetRoundProfit(id => 30.00f);
            Assert.AreEqual(50f, profit, 0.001f); // 1050 - 1000
        }

        [Test]
        public void GetRoundProfit_Loss_ReturnsNegativeProfit()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.StartRound(1000f);
            portfolio.OpenPosition("ACME", 10, 25.00f); // cash: 750
            // Total value now: 750 + 10*20 = 950
            float profit = portfolio.GetRoundProfit(id => 20.00f);
            Assert.AreEqual(-50f, profit, 0.001f); // 950 - 1000
        }

        [Test]
        public void GetRoundProfit_WithShortPosition_CalculatesCorrectly()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.StartRound(1000f);
            portfolio.OpenShort("ACME", 10, 50.00f); // margin: 250, cash: 750
            // Short value = 250 + (50-40)*10 = 350, total = 750 + 350 = 1100
            float profit = portfolio.GetRoundProfit(id => 40.00f);
            Assert.AreEqual(100f, profit, 0.001f); // 1100 - 1000
        }
    }
}
