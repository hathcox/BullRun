using System.Collections.Generic;
using NUnit.Framework;

namespace BullRun.Tests.Core
{
    [TestFixture]
    public class RunContextTests
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

        [Test]
        public void Constructor_SetsCurrentAct()
        {
            var portfolio = new Portfolio(1000f);
            var ctx = new RunContext(1, 1, portfolio);
            Assert.AreEqual(1, ctx.CurrentAct);
        }

        [Test]
        public void Constructor_SetsCurrentRound()
        {
            var portfolio = new Portfolio(1000f);
            var ctx = new RunContext(1, 3, portfolio);
            Assert.AreEqual(3, ctx.CurrentRound);
        }

        [Test]
        public void Constructor_SetsPortfolio()
        {
            var portfolio = new Portfolio(1000f);
            var ctx = new RunContext(1, 1, portfolio);
            Assert.IsNotNull(ctx.Portfolio);
            Assert.AreEqual(1000f, ctx.Portfolio.Cash, 0.001f);
        }

        [Test]
        public void ActiveItems_DefaultsToEmptyList()
        {
            var portfolio = new Portfolio(1000f);
            var ctx = new RunContext(1, 1, portfolio);
            Assert.IsNotNull(ctx.ActiveItems);
            Assert.AreEqual(0, ctx.ActiveItems.Count);
        }

        [Test]
        public void ActiveItems_CanAddItems()
        {
            var portfolio = new Portfolio(1000f);
            var ctx = new RunContext(1, 1, portfolio);
            ctx.ActiveItems.Add("TestItem");
            Assert.AreEqual(1, ctx.ActiveItems.Count);
            Assert.AreEqual("TestItem", ctx.ActiveItems[0]);
        }
        // --- Run Initialization Tests (Story 2.5 Task 1) ---

        [Test]
        public void StartNewRun_CreatesPortfolioWithStartingCapital()
        {
            var ctx = RunContext.StartNewRun();
            Assert.AreEqual(GameConfig.StartingCapital, ctx.Portfolio.Cash, 0.001f);
        }

        [Test]
        public void StartNewRun_SetsActToOne()
        {
            var ctx = RunContext.StartNewRun();
            Assert.AreEqual(1, ctx.CurrentAct);
        }

        [Test]
        public void StartNewRun_SetsRoundToOne()
        {
            var ctx = RunContext.StartNewRun();
            Assert.AreEqual(1, ctx.CurrentRound);
        }

        [Test]
        public void StartNewRun_InitializesEmptyActiveItems()
        {
            var ctx = RunContext.StartNewRun();
            Assert.IsNotNull(ctx.ActiveItems);
            Assert.AreEqual(0, ctx.ActiveItems.Count);
        }

        [Test]
        public void GetCurrentCash_ReturnsPortfolioCash()
        {
            var ctx = RunContext.StartNewRun();
            Assert.AreEqual(GameConfig.StartingCapital, ctx.GetCurrentCash(), 0.001f);
        }

        [Test]
        public void GetCurrentCash_ReflectsPortfolioChanges()
        {
            var ctx = RunContext.StartNewRun();
            ctx.Portfolio.OpenPosition("ACME", 10, 25.00f);
            Assert.AreEqual(GameConfig.StartingCapital - 250f, ctx.GetCurrentCash(), 0.001f);
        }
        [Test]
        public void StartNewRun_PublishesRunStartedEvent()
        {
            RunStartedEvent received = default;
            bool wasCalled = false;
            EventBus.Subscribe<RunStartedEvent>(e =>
            {
                received = e;
                wasCalled = true;
            });

            RunContext.StartNewRun();

            Assert.IsTrue(wasCalled);
            Assert.AreEqual(GameConfig.StartingCapital, received.StartingCapital, 0.001f);
        }

        // --- Round Transition Tests (Story 2.5 Task 4) ---

        [Test]
        public void PrepareForNextRound_IncrementsRound()
        {
            var ctx = RunContext.StartNewRun();
            Assert.AreEqual(1, ctx.CurrentRound);
            ctx.PrepareForNextRound();
            Assert.AreEqual(2, ctx.CurrentRound);
        }

        [Test]
        public void PrepareForNextRound_CashCarriesForward()
        {
            var ctx = RunContext.StartNewRun();
            ctx.Portfolio.OpenPosition("ACME", 10, 25.00f); // cash: 750
            ctx.Portfolio.ClosePosition("ACME", 10, 30.00f); // cash: 1050
            float cashBefore = ctx.GetCurrentCash();
            ctx.PrepareForNextRound();
            Assert.AreEqual(cashBefore, ctx.GetCurrentCash(), 0.001f);
        }

        [Test]
        public void PrepareForNextRound_UpdatesActCorrectly()
        {
            var ctx = RunContext.StartNewRun();
            ctx.CurrentRound = 2; // Round 2 → 3 = Act 1 → Act 2
            ctx.PrepareForNextRound();
            Assert.AreEqual(2, ctx.CurrentAct);
        }

        [Test]
        public void PrepareForNextRound_MultipleRounds_CashCompounds()
        {
            var ctx = RunContext.StartNewRun(); // cash: 1000
            // Round 1: earn profit
            ctx.Portfolio.OpenPosition("ACME", 10, 25.00f); // cash: 750
            ctx.Portfolio.LiquidateAllPositions(id => 30.00f); // cash: 1050
            ctx.PrepareForNextRound();
            Assert.AreEqual(2, ctx.CurrentRound);
            Assert.AreEqual(1050f, ctx.GetCurrentCash(), 0.001f);

            // Round 2: earn more
            ctx.Portfolio.OpenPosition("ACME", 10, 30.00f); // cash: 750
            ctx.Portfolio.LiquidateAllPositions(id => 40.00f); // cash: 1150
            ctx.PrepareForNextRound();
            Assert.AreEqual(3, ctx.CurrentRound);
            Assert.AreEqual(1150f, ctx.GetCurrentCash(), 0.001f);
        }

        // --- AdvanceRound Tests (Story 4.5 Task 2) ---

        [Test]
        public void AdvanceRound_IncrementsRoundNumber()
        {
            var ctx = RunContext.StartNewRun();
            Assert.AreEqual(1, ctx.CurrentRound);
            ctx.AdvanceRound();
            Assert.AreEqual(2, ctx.CurrentRound);
        }

        [Test]
        public void AdvanceRound_UpdatesActAtBoundary_Round2ToRound3()
        {
            var ctx = RunContext.StartNewRun();
            ctx.CurrentRound = 2;
            ctx.AdvanceRound(); // Round 3 = Act 2
            Assert.AreEqual(3, ctx.CurrentRound);
            Assert.AreEqual(2, ctx.CurrentAct);
        }

        [Test]
        public void AdvanceRound_UpdatesActAtBoundary_Round4ToRound5()
        {
            var ctx = RunContext.StartNewRun();
            ctx.CurrentRound = 4;
            ctx.CurrentAct = 2;
            ctx.AdvanceRound(); // Round 5 = Act 3
            Assert.AreEqual(5, ctx.CurrentRound);
            Assert.AreEqual(3, ctx.CurrentAct);
        }

        [Test]
        public void AdvanceRound_UpdatesActAtBoundary_Round6ToRound7()
        {
            var ctx = RunContext.StartNewRun();
            ctx.CurrentRound = 6;
            ctx.CurrentAct = 3;
            ctx.AdvanceRound(); // Round 7 = Act 4
            Assert.AreEqual(7, ctx.CurrentRound);
            Assert.AreEqual(4, ctx.CurrentAct);
        }

        [Test]
        public void AdvanceRound_NoActChangeWithinSameAct()
        {
            var ctx = RunContext.StartNewRun();
            ctx.AdvanceRound(); // Round 1 → 2, still Act 1
            Assert.AreEqual(2, ctx.CurrentRound);
            Assert.AreEqual(1, ctx.CurrentAct);
        }

        [Test]
        public void AdvanceRound_ReturnsTrueWhenActChanged()
        {
            var ctx = RunContext.StartNewRun();
            ctx.CurrentRound = 2;
            bool actChanged = ctx.AdvanceRound(); // Round 2 → 3 = Act 1 → 2
            Assert.IsTrue(actChanged);
        }

        [Test]
        public void AdvanceRound_ReturnsFalseWhenActNotChanged()
        {
            var ctx = RunContext.StartNewRun();
            bool actChanged = ctx.AdvanceRound(); // Round 1 → 2, still Act 1
            Assert.IsFalse(actChanged);
        }

        // --- GetCurrentAct Tests (Story 4.5 Task 2) ---

        [Test]
        public void GetCurrentAct_Round1_ReturnsAct1()
        {
            Assert.AreEqual(1, RunContext.GetActForRound(1));
        }

        [Test]
        public void GetCurrentAct_Round2_ReturnsAct1()
        {
            Assert.AreEqual(1, RunContext.GetActForRound(2));
        }

        [Test]
        public void GetCurrentAct_Round3_ReturnsAct2()
        {
            Assert.AreEqual(2, RunContext.GetActForRound(3));
        }

        [Test]
        public void GetCurrentAct_Round4_ReturnsAct2()
        {
            Assert.AreEqual(2, RunContext.GetActForRound(4));
        }

        [Test]
        public void GetCurrentAct_Round5_ReturnsAct3()
        {
            Assert.AreEqual(3, RunContext.GetActForRound(5));
        }

        [Test]
        public void GetCurrentAct_Round6_ReturnsAct3()
        {
            Assert.AreEqual(3, RunContext.GetActForRound(6));
        }

        [Test]
        public void GetCurrentAct_Round7_ReturnsAct4()
        {
            Assert.AreEqual(4, RunContext.GetActForRound(7));
        }

        [Test]
        public void GetCurrentAct_Round8_ReturnsAct4()
        {
            Assert.AreEqual(4, RunContext.GetActForRound(8));
        }

        // --- GetCurrentTier Tests (Story 4.5 Task 2) ---

        [Test]
        public void GetCurrentTier_Act1_ReturnsPenny()
        {
            Assert.AreEqual(StockTier.Penny, RunContext.GetTierForAct(1));
        }

        [Test]
        public void GetCurrentTier_Act2_ReturnsLowValue()
        {
            Assert.AreEqual(StockTier.LowValue, RunContext.GetTierForAct(2));
        }

        [Test]
        public void GetCurrentTier_Act3_ReturnsMidValue()
        {
            Assert.AreEqual(StockTier.MidValue, RunContext.GetTierForAct(3));
        }

        [Test]
        public void GetCurrentTier_Act4_ReturnsBlueChip()
        {
            Assert.AreEqual(StockTier.BlueChip, RunContext.GetTierForAct(4));
        }

        // --- IsRunComplete Tests (Story 4.5 Task 2) ---

        [Test]
        public void IsRunComplete_Round8_ReturnsTrue()
        {
            var ctx = RunContext.StartNewRun();
            ctx.CurrentRound = 8;
            ctx.AdvanceRound(); // Round 9 — run is now past round 8
            Assert.IsTrue(ctx.IsRunComplete());
        }

        [Test]
        public void IsRunComplete_Round7_ReturnsFalse()
        {
            var ctx = RunContext.StartNewRun();
            ctx.CurrentRound = 7;
            Assert.IsFalse(ctx.IsRunComplete());
        }

        [Test]
        public void IsRunComplete_Round1_ReturnsFalse()
        {
            var ctx = RunContext.StartNewRun();
            Assert.IsFalse(ctx.IsRunComplete());
        }

        [Test]
        public void IsRunComplete_ExactlyRound8_ReturnsFalse()
        {
            // Round 8 is the last playable round — not complete until AFTER round 8
            var ctx = RunContext.StartNewRun();
            ctx.CurrentRound = 8;
            Assert.IsFalse(ctx.IsRunComplete());
        }
    }
}
