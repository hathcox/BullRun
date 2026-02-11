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
        public void PrepareForNextRound_DoesNotResetAct()
        {
            var ctx = RunContext.StartNewRun();
            ctx.CurrentAct = 2;
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
    }
}
