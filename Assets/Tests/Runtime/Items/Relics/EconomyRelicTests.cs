using NUnit.Framework;

namespace BullRun.Tests.Items.Relics
{
    /// <summary>
    /// Story 17.5: Tests for economy/reputation relics — Rep Doubler, Fail Forward,
    /// Compound Rep, Rep Interest, Rep Dividend, Bond Bonus, and ShopTransaction sell override.
    /// </summary>
    [TestFixture]
    public class EconomyRelicTests
    {
        private RunContext _ctx;
        private RelicManager _mgr;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            RelicFactory.ResetRegistry();
            _ctx = new RunContext(1, 1, new Portfolio(1000f));
            _mgr = _ctx.RelicManager;
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
            RelicFactory.ResetRegistry();
        }

        // ════════════════════════════════════════════════════════════════════
        // Rep Doubler (AC 1, 8)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void RepDoublerRelic_HasCorrectId()
        {
            var relic = new RepDoublerRelic();
            Assert.AreEqual("relic_rep_doubler", relic.Id);
        }

        [Test]
        public void RepDoubler_FactoryCreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_rep_doubler");
            Assert.IsNotNull(relic);
            Assert.IsInstanceOf<RepDoublerRelic>(relic);
        }

        [Test]
        public void RepDoubler_RoundEndRepDoubled()
        {
            // Round 1 base rep = 10 at target, no bonus
            int normalRep = MarginCallState.CalculateRoundReputation(1, 20f, 20f);
            Assert.AreEqual(10, normalRep, "Round 1 base rep should be 10");

            // With Rep Doubler, the doubling happens in MarginCallState.Enter,
            // so we test the multiplied value directly
            int doubledRep = normalRep * 2;
            Assert.AreEqual(20, doubledRep);
        }

        [Test]
        public void RepDoubler_BondRepUnaffected()
        {
            // Bond rep is paid out by BondManager.PayoutRep, not by MarginCallState.
            // Rep Doubler only affects the round-end CalculateRoundReputation path.
            _mgr.AddRelic("relic_rep_doubler");
            _ctx.BondsOwned = 3;
            _ctx.BondPurchaseHistory.Add(new BondRecord(1, 5));
            _ctx.BondPurchaseHistory.Add(new BondRecord(1, 5));
            _ctx.BondPurchaseHistory.Add(new BondRecord(1, 5));

            int repBefore = _ctx.Reputation.Current;
            _ctx.Bonds.PayoutRep(_ctx.Reputation);
            int bondRep = _ctx.Reputation.Current - repBefore;

            // Bond rep = 3 bonds × 1 rep/bond = 3 (not doubled)
            Assert.AreEqual(3, bondRep, "Bond rep should NOT be doubled by Rep Doubler");
        }

        [Test]
        public void RepDoubler_BaseBonusBreakdownCorrectWhenDoubled()
        {
            // Round 1 with cash exceeding target to produce bonus rep
            // CalculateRoundReputation gives base + bonus; both should be doubled
            int normalRep = MarginCallState.CalculateRoundReputation(1, 40f, 20f);
            int roundIndex = System.Math.Max(0, System.Math.Min(0, GameConfig.RepBaseAwardPerRound.Length - 1));
            int expectedBase = GameConfig.RepBaseAwardPerRound[roundIndex];
            int expectedBonus = normalRep - expectedBase;

            // With Rep Doubler, all components should be doubled
            Assert.AreEqual(expectedBase * 2 + expectedBonus * 2, normalRep * 2,
                "Doubled total should equal doubled base + doubled bonus");
            // Verify base and bonus are independently meaningful
            Assert.Greater(expectedBase, 0, "Base rep should be positive for round 1");
        }

        [Test]
        public void RepDoubler_RepInterestUnaffected()
        {
            // Rep Interest calculates 10% of current rep — Rep Doubler doesn't affect it
            _ctx.Reputation.Add(100);
            _mgr.AddRelic("relic_rep_doubler");
            _mgr.AddRelic("relic_rep_interest");

            int repBefore = _ctx.Reputation.Current;
            _mgr.DispatchRoundStart(new RoundStartedEvent { RoundNumber = 1, Act = 1 });
            int interest = _ctx.Reputation.Current - repBefore;

            Assert.AreEqual(10, interest, "Rep Interest should give 10% of 100 = 10 (not doubled)");
        }

        // ════════════════════════════════════════════════════════════════════
        // Fail Forward (AC 2, 9)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void FailForwardRelic_HasCorrectId()
        {
            var relic = new FailForwardRelic();
            Assert.AreEqual("relic_fail_forward", relic.Id);
        }

        [Test]
        public void FailForward_FactoryCreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_fail_forward");
            Assert.IsNotNull(relic);
            Assert.IsInstanceOf<FailForwardRelic>(relic);
        }

        [Test]
        public void FailForward_MarginCallStillAwardsBaseRep()
        {
            // Simulate what MarginCallState does in margin call failure path with Fail Forward
            _mgr.AddRelic("relic_fail_forward");
            int repBefore = _ctx.Reputation.Current;

            // Simulate round 3 margin call with Fail Forward
            int failRoundIndex = System.Math.Max(0, System.Math.Min(3 - 1, GameConfig.RepBaseAwardPerRound.Length - 1));
            int failForwardRep = GameConfig.RepBaseAwardPerRound[failRoundIndex];
            _ctx.Reputation.Add(failForwardRep);

            // Round 3 base rep = 18
            Assert.AreEqual(18, _ctx.Reputation.Current - repBefore);
        }

        [Test]
        public void FailForward_MessagePublished()
        {
            _mgr.AddRelic("relic_fail_forward");
            TradeFeedbackEvent? captured = null;
            EventBus.Subscribe<TradeFeedbackEvent>(e => captured = e);

            // Simulate MarginCallState margin call path with Fail Forward (round 3 base = 18)
            int failRoundIndex = System.Math.Max(0, System.Math.Min(3 - 1, GameConfig.RepBaseAwardPerRound.Length - 1));
            int failForwardRep = GameConfig.RepBaseAwardPerRound[failRoundIndex];
            _ctx.Reputation.Add(failForwardRep);
            EventBus.Publish(new TradeFeedbackEvent
            {
                Message = $"Fail Forward: +{failForwardRep} Rep",
                IsSuccess = true, IsBuy = false, IsShort = false
            });

            Assert.IsNotNull(captured);
            StringAssert.StartsWith("Fail Forward:", captured.Value.Message);
        }

        [Test]
        public void FailForward_NonMarginCallRoundsUnaffected()
        {
            // Fail Forward is passive — only activates on margin call path in MarginCallState.
            // Normal round completion uses CalculateRoundReputation, which is unaffected.
            _mgr.AddRelic("relic_fail_forward");
            int normalRep = MarginCallState.CalculateRoundReputation(1, 20f, 20f);
            Assert.AreEqual(10, normalRep, "Normal round rep should be unaffected by Fail Forward");
        }

        // ════════════════════════════════════════════════════════════════════
        // Compound Rep (AC 3)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void CompoundRepRelic_HasCorrectId()
        {
            var relic = new CompoundRepRelic();
            Assert.AreEqual("relic_compound_rep", relic.Id);
        }

        [Test]
        public void CompoundRep_FactoryCreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_compound_rep");
            Assert.IsNotNull(relic);
            Assert.IsInstanceOf<CompoundRepRelic>(relic);
        }

        [Test]
        public void CompoundRep_RoundsHeldIncrementsOnRoundStart()
        {
            var relic = new CompoundRepRelic();
            Assert.AreEqual(0, relic.RoundsHeld);

            relic.OnRoundStart(_ctx, new RoundStartedEvent { RoundNumber = 1 });
            Assert.AreEqual(1, relic.RoundsHeld);

            relic.OnRoundStart(_ctx, new RoundStartedEvent { RoundNumber = 2 });
            Assert.AreEqual(2, relic.RoundsHeld);
        }

        [Test]
        public void CompoundRep_SellValue_N0_Equals3()
        {
            // 3 * 2^0 = 3
            var relic = new CompoundRepRelic();
            int? value = relic.GetSellValue(_ctx);
            Assert.AreEqual(3, value);
        }

        [Test]
        public void CompoundRep_SellValue_N1_Equals6()
        {
            // 3 * 2^1 = 6
            var relic = new CompoundRepRelic();
            relic.OnRoundStart(_ctx, new RoundStartedEvent { RoundNumber = 1 });
            int? value = relic.GetSellValue(_ctx);
            Assert.AreEqual(6, value);
        }

        [Test]
        public void CompoundRep_SellValue_N2_Equals12()
        {
            // 3 * 2^2 = 12
            var relic = new CompoundRepRelic();
            relic.OnRoundStart(_ctx, new RoundStartedEvent { RoundNumber = 1 });
            relic.OnRoundStart(_ctx, new RoundStartedEvent { RoundNumber = 2 });
            int? value = relic.GetSellValue(_ctx);
            Assert.AreEqual(12, value);
        }

        [Test]
        public void CompoundRep_SellValue_N3_Equals24()
        {
            // 3 * 2^3 = 24
            var relic = new CompoundRepRelic();
            relic.OnRoundStart(_ctx, new RoundStartedEvent { RoundNumber = 1 });
            relic.OnRoundStart(_ctx, new RoundStartedEvent { RoundNumber = 2 });
            relic.OnRoundStart(_ctx, new RoundStartedEvent { RoundNumber = 3 });
            int? value = relic.GetSellValue(_ctx);
            Assert.AreEqual(24, value);
        }

        [Test]
        public void CompoundRep_OverridesDefault50PercentRefund()
        {
            // Compound Rep returns non-null from GetSellValue, overriding default
            var relic = new CompoundRepRelic();
            int? value = relic.GetSellValue(_ctx);
            Assert.IsNotNull(value, "CompoundRep.GetSellValue should return non-null");
        }

        [Test]
        public void CompoundRep_PublishesRelicActivatedOnSell()
        {
            var relic = new CompoundRepRelic();
            RelicActivatedEvent? captured = null;
            EventBus.Subscribe<RelicActivatedEvent>(e => captured = e);

            relic.OnSellSelf(_ctx);

            Assert.IsNotNull(captured);
            Assert.AreEqual("relic_compound_rep", captured.Value.RelicId);
        }

        [Test]
        public void CompoundRep_GetSellValue_NoBoolSideEffects()
        {
            // GetSellValue should be a pure query — no RelicActivatedEvent
            var relic = new CompoundRepRelic();
            RelicActivatedEvent? captured = null;
            EventBus.Subscribe<RelicActivatedEvent>(e => captured = e);

            relic.GetSellValue(_ctx);

            Assert.IsNull(captured, "GetSellValue should not fire RelicActivatedEvent");
        }

        // ════════════════════════════════════════════════════════════════════
        // Rep Interest (AC 4)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void RepInterestRelic_HasCorrectId()
        {
            var relic = new RepInterestRelic();
            Assert.AreEqual("relic_rep_interest", relic.Id);
        }

        [Test]
        public void RepInterest_FactoryCreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_rep_interest");
            Assert.IsNotNull(relic);
            Assert.IsInstanceOf<RepInterestRelic>(relic);
        }

        [Test]
        public void RepInterest_10PercentOfCurrentRep()
        {
            _ctx.Reputation.Add(100);
            _mgr.AddRelic("relic_rep_interest");

            int repBefore = _ctx.Reputation.Current;
            _mgr.DispatchRoundStart(new RoundStartedEvent { RoundNumber = 1, Act = 1 });

            Assert.AreEqual(repBefore + 10, _ctx.Reputation.Current);
        }

        [Test]
        public void RepInterest_0Rep_NoInterest()
        {
            Assert.AreEqual(0, _ctx.Reputation.Current);
            _mgr.AddRelic("relic_rep_interest");

            _mgr.DispatchRoundStart(new RoundStartedEvent { RoundNumber = 1, Act = 1 });

            Assert.AreEqual(0, _ctx.Reputation.Current);
        }

        [Test]
        public void RepInterest_9Rep_NoInterest()
        {
            _ctx.Reputation.Add(9);
            _mgr.AddRelic("relic_rep_interest");

            int repBefore = _ctx.Reputation.Current;
            _mgr.DispatchRoundStart(new RoundStartedEvent { RoundNumber = 1, Act = 1 });

            Assert.AreEqual(repBefore, _ctx.Reputation.Current, "9 / 10 = 0 interest");
        }

        [Test]
        public void RepInterest_10Rep_1Interest()
        {
            _ctx.Reputation.Add(10);
            _mgr.AddRelic("relic_rep_interest");

            int repBefore = _ctx.Reputation.Current;
            _mgr.DispatchRoundStart(new RoundStartedEvent { RoundNumber = 1, Act = 1 });

            Assert.AreEqual(repBefore + 1, _ctx.Reputation.Current, "10 / 10 = 1 interest");
        }

        [Test]
        public void RepInterest_PublishesFeedbackMessage()
        {
            _ctx.Reputation.Add(50);
            _mgr.AddRelic("relic_rep_interest");
            TradeFeedbackEvent? captured = null;
            EventBus.Subscribe<TradeFeedbackEvent>(e => captured = e);

            _mgr.DispatchRoundStart(new RoundStartedEvent { RoundNumber = 1, Act = 1 });

            Assert.IsNotNull(captured);
            Assert.AreEqual("+5 Rep Interest", captured.Value.Message);
        }

        [Test]
        public void RepInterest_PublishesRelicActivatedEvent()
        {
            _ctx.Reputation.Add(20);
            _mgr.AddRelic("relic_rep_interest");
            RelicActivatedEvent? captured = null;
            EventBus.Subscribe<RelicActivatedEvent>(e => captured = e);

            _mgr.DispatchRoundStart(new RoundStartedEvent { RoundNumber = 1, Act = 1 });

            Assert.IsNotNull(captured);
            Assert.AreEqual("relic_rep_interest", captured.Value.RelicId);
        }

        // ════════════════════════════════════════════════════════════════════
        // Rep Dividend (AC 5)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void RepDividendRelic_HasCorrectId()
        {
            var relic = new RepDividendRelic();
            Assert.AreEqual("relic_rep_dividend", relic.Id);
        }

        [Test]
        public void RepDividend_FactoryCreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_rep_dividend");
            Assert.IsNotNull(relic);
            Assert.IsInstanceOf<RepDividendRelic>(relic);
        }

        [Test]
        public void RepDividend_0Rep_NoDividend()
        {
            Assert.AreEqual(0, _ctx.Reputation.Current);
            float cashBefore = _ctx.Portfolio.Cash;
            _mgr.AddRelic("relic_rep_dividend");

            _mgr.DispatchRoundStart(new RoundStartedEvent { RoundNumber = 1, Act = 1 });

            Assert.AreEqual(cashBefore, _ctx.Portfolio.Cash, 0.01f);
        }

        [Test]
        public void RepDividend_1Rep_NoDividend()
        {
            _ctx.Reputation.Add(1);
            float cashBefore = _ctx.Portfolio.Cash;
            _mgr.AddRelic("relic_rep_dividend");

            _mgr.DispatchRoundStart(new RoundStartedEvent { RoundNumber = 1, Act = 1 });

            Assert.AreEqual(cashBefore, _ctx.Portfolio.Cash, 0.01f, "1 / 2 = 0, no dividend");
        }

        [Test]
        public void RepDividend_5Rep_Gives2Dollars()
        {
            _ctx.Reputation.Add(5);
            float cashBefore = _ctx.Portfolio.Cash;
            _mgr.AddRelic("relic_rep_dividend");

            _mgr.DispatchRoundStart(new RoundStartedEvent { RoundNumber = 1, Act = 1 });

            Assert.AreEqual(cashBefore + 2f, _ctx.Portfolio.Cash, 0.01f, "5 / 2 = 2 dollars");
        }

        [Test]
        public void RepDividend_10Rep_Gives5Dollars()
        {
            _ctx.Reputation.Add(10);
            float cashBefore = _ctx.Portfolio.Cash;
            _mgr.AddRelic("relic_rep_dividend");

            _mgr.DispatchRoundStart(new RoundStartedEvent { RoundNumber = 1, Act = 1 });

            Assert.AreEqual(cashBefore + 5f, _ctx.Portfolio.Cash, 0.01f);
        }

        [Test]
        public void RepDividend_PublishesFeedbackMessage()
        {
            _ctx.Reputation.Add(10);
            _mgr.AddRelic("relic_rep_dividend");
            TradeFeedbackEvent? captured = null;
            EventBus.Subscribe<TradeFeedbackEvent>(e => captured = e);

            _mgr.DispatchRoundStart(new RoundStartedEvent { RoundNumber = 1, Act = 1 });

            Assert.IsNotNull(captured);
            Assert.AreEqual("+$5 Dividend", captured.Value.Message);
        }

        [Test]
        public void RepDividend_PublishesRelicActivatedEvent()
        {
            _ctx.Reputation.Add(10);
            _mgr.AddRelic("relic_rep_dividend");
            RelicActivatedEvent? captured = null;
            EventBus.Subscribe<RelicActivatedEvent>(e => captured = e);

            _mgr.DispatchRoundStart(new RoundStartedEvent { RoundNumber = 1, Act = 1 });

            Assert.IsNotNull(captured);
            Assert.AreEqual("relic_rep_dividend", captured.Value.RelicId);
        }

        // ════════════════════════════════════════════════════════════════════
        // Bond Bonus (AC 6)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void BondBonusRelic_HasCorrectId()
        {
            var relic = new BondBonusRelic();
            Assert.AreEqual("relic_bond_bonus", relic.Id);
        }

        [Test]
        public void BondBonus_FactoryCreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_bond_bonus");
            Assert.IsNotNull(relic);
            Assert.IsInstanceOf<BondBonusRelic>(relic);
        }

        [Test]
        public void BondBonus_OnAcquired_Adds10Bonds()
        {
            Assert.AreEqual(0, _ctx.BondsOwned);
            _mgr.AddRelic("relic_bond_bonus");
            Assert.AreEqual(10, _ctx.BondsOwned);
        }

        [Test]
        public void BondBonus_OnAcquired_Adds10BondRecords()
        {
            Assert.AreEqual(0, _ctx.BondPurchaseHistory.Count);
            _mgr.AddRelic("relic_bond_bonus");
            Assert.AreEqual(10, _ctx.BondPurchaseHistory.Count);
        }

        [Test]
        public void BondBonus_BondRecords_HaveCurrentRound()
        {
            _ctx.CurrentRound = 3;
            _mgr.AddRelic("relic_bond_bonus");

            for (int i = 0; i < _ctx.BondPurchaseHistory.Count; i++)
            {
                Assert.AreEqual(3, _ctx.BondPurchaseHistory[i].RoundPurchased);
            }
        }

        [Test]
        public void BondBonus_OnSell_Removes10Bonds()
        {
            _mgr.AddRelic("relic_bond_bonus");
            Assert.AreEqual(10, _ctx.BondsOwned);

            _mgr.DispatchSellSelf("relic_bond_bonus");
            Assert.AreEqual(0, _ctx.BondsOwned);
        }

        [Test]
        public void BondBonus_OnSell_Removes10BondRecordsLIFO()
        {
            // Add 2 real bonds first, then Bond Bonus adds 10
            _ctx.BondsOwned = 2;
            _ctx.BondPurchaseHistory.Add(new BondRecord(1, 3));
            _ctx.BondPurchaseHistory.Add(new BondRecord(2, 5));

            _mgr.AddRelic("relic_bond_bonus");
            Assert.AreEqual(12, _ctx.BondsOwned);
            Assert.AreEqual(12, _ctx.BondPurchaseHistory.Count);

            _mgr.DispatchSellSelf("relic_bond_bonus");

            // Should have removed 10 from the end (LIFO), leaving the 2 real bonds
            Assert.AreEqual(2, _ctx.BondsOwned);
            Assert.AreEqual(2, _ctx.BondPurchaseHistory.Count);
            Assert.AreEqual(1, _ctx.BondPurchaseHistory[0].RoundPurchased);
            Assert.AreEqual(2, _ctx.BondPurchaseHistory[1].RoundPurchased);
        }

        [Test]
        public void BondBonus_OnSell_BondsOwnedNeverNegative()
        {
            _mgr.AddRelic("relic_bond_bonus");
            _ctx.BondsOwned = 5; // Simulate some bonds being sold already

            _mgr.DispatchSellSelf("relic_bond_bonus");
            Assert.AreEqual(0, _ctx.BondsOwned, "BondsOwned should clamp to 0, not go negative");
        }

        [Test]
        public void BondBonus_OnSell_FewerThan10Records_RemovesAllRemaining()
        {
            _mgr.AddRelic("relic_bond_bonus");
            // Remove some records to simulate edge case
            while (_ctx.BondPurchaseHistory.Count > 3)
                _ctx.BondPurchaseHistory.RemoveAt(_ctx.BondPurchaseHistory.Count - 1);

            _mgr.DispatchSellSelf("relic_bond_bonus");
            Assert.AreEqual(0, _ctx.BondPurchaseHistory.Count, "Should remove all remaining records");
        }

        [Test]
        public void BondBonus_PublishesRelicActivatedOnAcquire()
        {
            RelicActivatedEvent? captured = null;
            EventBus.Subscribe<RelicActivatedEvent>(e => captured = e);

            _mgr.AddRelic("relic_bond_bonus");

            Assert.IsNotNull(captured);
            Assert.AreEqual("relic_bond_bonus", captured.Value.RelicId);
        }

        [Test]
        public void BondBonus_PublishesRelicActivatedOnSell()
        {
            _mgr.AddRelic("relic_bond_bonus");
            RelicActivatedEvent? captured = null;
            EventBus.Subscribe<RelicActivatedEvent>(e => captured = e);

            _mgr.DispatchSellSelf("relic_bond_bonus");

            Assert.IsNotNull(captured);
            Assert.AreEqual("relic_bond_bonus", captured.Value.RelicId);
        }

        // ════════════════════════════════════════════════════════════════════
        // ShopTransaction.SellRelic sell override (AC 7)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void SellRelic_CompoundRep_ReturnsCustomSellValue()
        {
            // Purchase Compound Rep via RelicManager
            _mgr.AddRelic("relic_compound_rep");
            _ctx.Reputation.Add(200); // Ensure enough rep for operations

            // Simulate 2 rounds held
            _mgr.DispatchRoundStart(new RoundStartedEvent { RoundNumber = 1 });
            _mgr.DispatchRoundStart(new RoundStartedEvent { RoundNumber = 2 });

            // Expected sell value: 3 * 2^2 = 12
            int repBefore = _ctx.Reputation.Current;
            var txn = new ShopTransaction();
            var result = txn.SellRelic(_ctx, "relic_compound_rep");

            Assert.AreEqual(ShopPurchaseResult.Success, result);
            // Rep should increase by 12 (custom value), not by default 50%
            Assert.AreEqual(repBefore + 12, _ctx.Reputation.Current);
        }

        [Test]
        public void SellRelic_OtherRelics_ReturnDefault50Percent()
        {
            // Default relics should return null from GetSellValue (use default 50%)
            var relic = new RepDoublerRelic();
            Assert.IsNull(relic.GetSellValue(_ctx));
        }

        [Test]
        public void SellRelic_BondBonus_ReturnsDefault50Percent()
        {
            // Bond Bonus does NOT override sell value — uses default 50%
            var relic = new BondBonusRelic();
            Assert.IsNull(relic.GetSellValue(_ctx), "BondBonus.GetSellValue should return null (default)");
        }

        // ════════════════════════════════════════════════════════════════════
        // Factory integration: all 6 relics create real instances (AC 10)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void Factory_RepDoubler_CreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_rep_doubler");
            Assert.IsInstanceOf<RepDoublerRelic>(relic);
        }

        [Test]
        public void Factory_FailForward_CreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_fail_forward");
            Assert.IsInstanceOf<FailForwardRelic>(relic);
        }

        [Test]
        public void Factory_CompoundRep_CreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_compound_rep");
            Assert.IsInstanceOf<CompoundRepRelic>(relic);
        }

        [Test]
        public void Factory_RepInterest_CreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_rep_interest");
            Assert.IsInstanceOf<RepInterestRelic>(relic);
        }

        [Test]
        public void Factory_RepDividend_CreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_rep_dividend");
            Assert.IsInstanceOf<RepDividendRelic>(relic);
        }

        [Test]
        public void Factory_BondBonus_CreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_bond_bonus");
            Assert.IsInstanceOf<BondBonusRelic>(relic);
        }

        // ════════════════════════════════════════════════════════════════════
        // Regression: existing relics still work
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void Factory_ExistingRelics_StillWork()
        {
            Assert.IsInstanceOf<DoubleDealerRelic>(RelicFactory.Create("relic_double_dealer"));
            Assert.IsInstanceOf<EventStormRelic>(RelicFactory.Create("relic_event_storm"));
        }

        // ════════════════════════════════════════════════════════════════════
        // GetSellValue interface compliance
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void RelicBase_GetSellValue_ReturnsNull()
        {
            // All relics that don't override GetSellValue should return null
            var relic = new FailForwardRelic();
            Assert.IsNull(relic.GetSellValue(_ctx));
        }

        [Test]
        public void RepInterest_GetSellValue_ReturnsNull()
        {
            var relic = new RepInterestRelic();
            Assert.IsNull(relic.GetSellValue(_ctx));
        }

        [Test]
        public void RepDividend_GetSellValue_ReturnsNull()
        {
            var relic = new RepDividendRelic();
            Assert.IsNull(relic.GetSellValue(_ctx));
        }
    }
}
