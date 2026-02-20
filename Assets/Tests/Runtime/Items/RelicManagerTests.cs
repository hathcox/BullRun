using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BullRun.Tests.Items
{
    /// <summary>
    /// Story 17.1: Tests for RelicManager — ordered relic collection and dispatch.
    /// </summary>
    [TestFixture]
    public class RelicManagerTests
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

        // ════════════════════════════════════════════════════════════════
        // Add / Remove
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void AddRelic_AppearsInOrderedRelics()
        {
            _mgr.AddRelic("relic_stop_loss");
            Assert.AreEqual(1, _mgr.OrderedRelics.Count);
            Assert.AreEqual("relic_stop_loss", _mgr.OrderedRelics[0].Id);
        }

        [Test]
        public void AddRelic_SyncsWithRunContextOwnedRelics()
        {
            _mgr.AddRelic("relic_stop_loss");
            Assert.AreEqual(1, _ctx.OwnedRelics.Count);
            Assert.AreEqual("relic_stop_loss", _ctx.OwnedRelics[0]);
        }

        [Test]
        public void AddRelic_UnknownId_DoesNotAdd()
        {
            _mgr.AddRelic("unknown_relic");
            Assert.AreEqual(0, _mgr.OrderedRelics.Count);
        }

        [Test]
        public void AddRelic_CallsOnAcquired()
        {
            bool acquired = false;
            RelicFactory.Register("test_acq", () => new CallbackRelic("test_acq", onAcquired: () => acquired = true));

            _mgr.AddRelic("test_acq");
            Assert.IsTrue(acquired);
        }

        [Test]
        public void RemoveRelic_RemovesFromList()
        {
            _mgr.AddRelic("relic_stop_loss");
            _mgr.AddRelic("relic_speed_trader");
            _mgr.RemoveRelic("relic_stop_loss");

            Assert.AreEqual(1, _mgr.OrderedRelics.Count);
            Assert.AreEqual("relic_speed_trader", _mgr.OrderedRelics[0].Id);
        }

        [Test]
        public void RemoveRelic_SyncsWithRunContextOwnedRelics()
        {
            _mgr.AddRelic("relic_stop_loss");
            _mgr.AddRelic("relic_speed_trader");
            _mgr.RemoveRelic("relic_stop_loss");

            Assert.AreEqual(1, _ctx.OwnedRelics.Count);
            Assert.AreEqual("relic_speed_trader", _ctx.OwnedRelics[0]);
        }

        [Test]
        public void RemoveRelic_CallsOnRemoved()
        {
            bool removed = false;
            RelicFactory.Register("test_rem", () => new CallbackRelic("test_rem", onRemoved: () => removed = true));

            _mgr.AddRelic("test_rem");
            _mgr.RemoveRelic("test_rem");
            Assert.IsTrue(removed);
        }

        [Test]
        public void RemoveRelic_NonexistentId_DoesNothing()
        {
            _mgr.AddRelic("relic_stop_loss");
            _mgr.RemoveRelic("nonexistent");
            Assert.AreEqual(1, _mgr.OrderedRelics.Count);
        }

        // ════════════════════════════════════════════════════════════════
        // Reorder
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void ReorderRelic_MovesCorrectly()
        {
            _mgr.AddRelic("relic_stop_loss");
            _mgr.AddRelic("relic_speed_trader");
            _mgr.AddRelic("relic_insider_tip");

            _mgr.ReorderRelic(0, 2);

            Assert.AreEqual("relic_speed_trader", _mgr.OrderedRelics[0].Id);
            Assert.AreEqual("relic_insider_tip", _mgr.OrderedRelics[1].Id);
            Assert.AreEqual("relic_stop_loss", _mgr.OrderedRelics[2].Id);
        }

        [Test]
        public void ReorderRelic_SyncsOwnedRelics()
        {
            _mgr.AddRelic("relic_stop_loss");
            _mgr.AddRelic("relic_speed_trader");
            _mgr.ReorderRelic(0, 1);

            Assert.AreEqual("relic_speed_trader", _ctx.OwnedRelics[0]);
            Assert.AreEqual("relic_stop_loss", _ctx.OwnedRelics[1]);
        }

        [Test]
        public void ReorderRelic_InvalidIndices_DoesNothing()
        {
            _mgr.AddRelic("relic_stop_loss");
            _mgr.ReorderRelic(-1, 0);
            _mgr.ReorderRelic(0, 5);
            Assert.AreEqual(1, _mgr.OrderedRelics.Count);
            Assert.AreEqual("relic_stop_loss", _mgr.OrderedRelics[0].Id);
        }

        // ════════════════════════════════════════════════════════════════
        // GetRelicById
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void GetRelicById_ReturnsCorrectRelic()
        {
            _mgr.AddRelic("relic_stop_loss");
            _mgr.AddRelic("relic_speed_trader");

            var relic = _mgr.GetRelicById("relic_speed_trader");
            Assert.IsNotNull(relic);
            Assert.AreEqual("relic_speed_trader", relic.Id);
        }

        [Test]
        public void GetRelicById_UnknownId_ReturnsNull()
        {
            _mgr.AddRelic("relic_stop_loss");
            Assert.IsNull(_mgr.GetRelicById("nonexistent"));
        }

        // ════════════════════════════════════════════════════════════════
        // Dispatch order: left-to-right
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void DispatchRoundStart_LeftToRightOrder()
        {
            var order = new System.Collections.Generic.List<string>();
            RelicFactory.Register("r1", () => new CallbackRelic("r1", onRoundStart: () => order.Add("r1")));
            RelicFactory.Register("r2", () => new CallbackRelic("r2", onRoundStart: () => order.Add("r2")));
            RelicFactory.Register("r3", () => new CallbackRelic("r3", onRoundStart: () => order.Add("r3")));

            _mgr.AddRelic("r1");
            _mgr.AddRelic("r2");
            _mgr.AddRelic("r3");

            _mgr.DispatchRoundStart(new RoundStartedEvent());

            Assert.AreEqual(3, order.Count);
            Assert.AreEqual("r1", order[0]);
            Assert.AreEqual("r2", order[1]);
            Assert.AreEqual("r3", order[2]);
        }

        // ════════════════════════════════════════════════════════════════
        // Try-catch: one failing relic doesn't break others
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void DispatchRoundStart_FailingRelic_DoesNotBlockOthers()
        {
            bool thirdCalled = false;
            RelicFactory.Register("good1", () => new StubRelic("good1"));
            RelicFactory.Register("bad", () => new ThrowingRelic("bad", throwOn: "OnRoundStart"));
            RelicFactory.Register("good2", () => new CallbackRelic("good2", onRoundStart: () => thirdCalled = true));

            _mgr.AddRelic("good1");
            _mgr.AddRelic("bad");
            _mgr.AddRelic("good2");

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("bad\\.OnRoundStart"));

            Assert.DoesNotThrow(() => _mgr.DispatchRoundStart(new RoundStartedEvent()));
            Assert.IsTrue(thirdCalled, "Relic after the failing one should still be dispatched");
        }

        [Test]
        public void DispatchAfterTrade_FailingRelic_DoesNotBlockOthers()
        {
            bool thirdCalled = false;
            RelicFactory.Register("good1", () => new StubRelic("good1"));
            RelicFactory.Register("bad", () => new ThrowingRelic("bad", throwOn: "OnAfterTrade"));
            RelicFactory.Register("good2", () => new CallbackRelic("good2", onAfterTrade: () => thirdCalled = true));

            _mgr.AddRelic("good1");
            _mgr.AddRelic("bad");
            _mgr.AddRelic("good2");

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("bad\\.OnAfterTrade"));

            Assert.DoesNotThrow(() => _mgr.DispatchAfterTrade(new TradeExecutedEvent()));
            Assert.IsTrue(thirdCalled, "Relic after the failing one should still be dispatched");
        }

        // ════════════════════════════════════════════════════════════════
        // All dispatch methods fire correctly
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void DispatchRoundEnd_CallsOnRoundEnd()
        {
            bool called = false;
            RelicFactory.Register("t", () => new CallbackRelic("t", onRoundEnd: () => called = true));
            _mgr.AddRelic("t");
            _mgr.DispatchRoundEnd(new MarketClosedEvent());
            Assert.IsTrue(called);
        }

        [Test]
        public void DispatchBeforeTrade_CallsOnBeforeTrade()
        {
            bool called = false;
            RelicFactory.Register("t", () => new CallbackRelic("t", onBeforeTrade: () => called = true));
            _mgr.AddRelic("t");
            _mgr.DispatchBeforeTrade(new TradeExecutedEvent());
            Assert.IsTrue(called);
        }

        [Test]
        public void DispatchAfterTrade_CallsOnAfterTrade()
        {
            bool called = false;
            RelicFactory.Register("t", () => new CallbackRelic("t", onAfterTrade: () => called = true));
            _mgr.AddRelic("t");
            _mgr.DispatchAfterTrade(new TradeExecutedEvent());
            Assert.IsTrue(called);
        }

        [Test]
        public void DispatchMarketEvent_CallsOnMarketEventFired()
        {
            bool called = false;
            RelicFactory.Register("t", () => new CallbackRelic("t", onMarketEvent: () => called = true));
            _mgr.AddRelic("t");
            _mgr.DispatchMarketEvent(new MarketEventFiredEvent());
            Assert.IsTrue(called);
        }

        [Test]
        public void DispatchReputationChanged_CallsOnReputationChanged()
        {
            bool called = false;
            int capturedOld = -1, capturedNew = -1;
            RelicFactory.Register("t", () => new CallbackRelic("t", onRepChanged: (o, n) => { called = true; capturedOld = o; capturedNew = n; }));
            _mgr.AddRelic("t");
            _mgr.DispatchReputationChanged(10, 20);
            Assert.IsTrue(called);
            Assert.AreEqual(10, capturedOld);
            Assert.AreEqual(20, capturedNew);
        }

        [Test]
        public void DispatchShopOpen_CallsOnShopOpen()
        {
            bool called = false;
            RelicFactory.Register("t", () => new CallbackRelic("t", onShopOpen: () => called = true));
            _mgr.AddRelic("t");
            _mgr.DispatchShopOpen();
            Assert.IsTrue(called);
        }

        [Test]
        public void DispatchSellSelf_CallsOnSellSelf()
        {
            bool called = false;
            RelicFactory.Register("t", () => new CallbackRelic("t", onSellSelf: () => called = true));
            _mgr.AddRelic("t");
            _mgr.DispatchSellSelf("t");
            Assert.IsTrue(called);
        }

        [Test]
        public void DispatchSellSelf_OnlyTargetRelicIsCalled()
        {
            bool otherCalled = false;
            bool targetCalled = false;
            RelicFactory.Register("other", () => new CallbackRelic("other", onSellSelf: () => otherCalled = true));
            RelicFactory.Register("target", () => new CallbackRelic("target", onSellSelf: () => targetCalled = true));

            _mgr.AddRelic("other");
            _mgr.AddRelic("target");

            _mgr.DispatchSellSelf("target");

            Assert.IsTrue(targetCalled, "Target relic's OnSellSelf should be called");
            Assert.IsFalse(otherCalled, "Other relics' OnSellSelf should NOT be called");
        }

        // ════════════════════════════════════════════════════════════════
        // Test helpers
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Relic that invokes callbacks for specific hooks, for testing dispatch.
        /// </summary>
        private class CallbackRelic : RelicBase
        {
            private readonly string _id;
            private readonly Action _onAcquired;
            private readonly Action _onRemoved;
            private readonly Action _onRoundStart;
            private readonly Action _onRoundEnd;
            private readonly Action _onBeforeTrade;
            private readonly Action _onAfterTrade;
            private readonly Action _onMarketEvent;
            private readonly Action<int, int> _onRepChanged;
            private readonly Action _onShopOpen;
            private readonly Action _onSellSelf;

            public override string Id => _id;

            public CallbackRelic(
                string id,
                Action onAcquired = null,
                Action onRemoved = null,
                Action onRoundStart = null,
                Action onRoundEnd = null,
                Action onBeforeTrade = null,
                Action onAfterTrade = null,
                Action onMarketEvent = null,
                Action<int, int> onRepChanged = null,
                Action onShopOpen = null,
                Action onSellSelf = null)
            {
                _id = id;
                _onAcquired = onAcquired;
                _onRemoved = onRemoved;
                _onRoundStart = onRoundStart;
                _onRoundEnd = onRoundEnd;
                _onBeforeTrade = onBeforeTrade;
                _onAfterTrade = onAfterTrade;
                _onMarketEvent = onMarketEvent;
                _onRepChanged = onRepChanged;
                _onShopOpen = onShopOpen;
                _onSellSelf = onSellSelf;
            }

            public override void OnAcquired(RunContext ctx) => _onAcquired?.Invoke();
            public override void OnRemoved(RunContext ctx) => _onRemoved?.Invoke();
            public override void OnRoundStart(RunContext ctx, RoundStartedEvent e) => _onRoundStart?.Invoke();
            public override void OnRoundEnd(RunContext ctx, MarketClosedEvent e) => _onRoundEnd?.Invoke();
            public override void OnBeforeTrade(RunContext ctx, TradeExecutedEvent e) => _onBeforeTrade?.Invoke();
            public override void OnAfterTrade(RunContext ctx, TradeExecutedEvent e) => _onAfterTrade?.Invoke();
            public override void OnMarketEventFired(RunContext ctx, MarketEventFiredEvent e) => _onMarketEvent?.Invoke();
            public override void OnReputationChanged(RunContext ctx, int oldRep, int newRep) => _onRepChanged?.Invoke(oldRep, newRep);
            public override void OnShopOpen(RunContext ctx) => _onShopOpen?.Invoke();
            public override void OnSellSelf(RunContext ctx) => _onSellSelf?.Invoke();
        }

        /// <summary>
        /// Relic that throws on a configurable hook, for testing try-catch isolation.
        /// </summary>
        private class ThrowingRelic : RelicBase
        {
            private readonly string _id;
            private readonly string _throwOn;
            public override string Id => _id;

            public ThrowingRelic(string id, string throwOn = "OnRoundStart")
            {
                _id = id;
                _throwOn = throwOn;
            }

            public override void OnRoundStart(RunContext ctx, RoundStartedEvent e)
            {
                if (_throwOn == "OnRoundStart")
                    throw new InvalidOperationException("Test exception from ThrowingRelic");
            }

            public override void OnAfterTrade(RunContext ctx, TradeExecutedEvent e)
            {
                if (_throwOn == "OnAfterTrade")
                    throw new InvalidOperationException("Test exception from ThrowingRelic");
            }
        }
    }
}
