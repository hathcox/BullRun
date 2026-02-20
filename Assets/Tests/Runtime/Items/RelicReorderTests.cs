using System.Collections.Generic;
using NUnit.Framework;

namespace BullRun.Tests.Items
{
    /// <summary>
    /// Story 17.9: Tests for RelicManager.ReorderRelic — insert semantics,
    /// OwnedRelics sync, dispatch order after reorder, multi-reorder consistency.
    /// Complements the basic reorder tests in RelicManagerTests.
    /// </summary>
    [TestFixture]
    public class RelicReorderTests
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

        private void AddStubRelics(params string[] ids)
        {
            foreach (var id in ids)
            {
                RelicFactory.Register(id, () => new StubRelic(id));
                _mgr.AddRelic(id);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // Insert semantics — shift, not swap (AC 3, 4)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void ReorderRelic_MoveFirstToLast_ShiftsOthersLeft()
        {
            AddStubRelics("A", "B", "C", "D");

            _mgr.ReorderRelic(0, 3);

            Assert.AreEqual("B", _mgr.OrderedRelics[0].Id);
            Assert.AreEqual("C", _mgr.OrderedRelics[1].Id);
            Assert.AreEqual("D", _mgr.OrderedRelics[2].Id);
            Assert.AreEqual("A", _mgr.OrderedRelics[3].Id);
        }

        [Test]
        public void ReorderRelic_MoveLastToFirst_ShiftsOthersRight()
        {
            AddStubRelics("A", "B", "C", "D");

            _mgr.ReorderRelic(3, 0);

            Assert.AreEqual("D", _mgr.OrderedRelics[0].Id);
            Assert.AreEqual("A", _mgr.OrderedRelics[1].Id);
            Assert.AreEqual("B", _mgr.OrderedRelics[2].Id);
            Assert.AreEqual("C", _mgr.OrderedRelics[3].Id);
        }

        [Test]
        public void ReorderRelic_MoveMiddleForward_InsertsCorrectly()
        {
            AddStubRelics("A", "B", "C", "D", "E");

            _mgr.ReorderRelic(1, 3); // Move B from index 1 to index 3

            Assert.AreEqual("A", _mgr.OrderedRelics[0].Id);
            Assert.AreEqual("C", _mgr.OrderedRelics[1].Id);
            Assert.AreEqual("D", _mgr.OrderedRelics[2].Id);
            Assert.AreEqual("B", _mgr.OrderedRelics[3].Id);
            Assert.AreEqual("E", _mgr.OrderedRelics[4].Id);
        }

        [Test]
        public void ReorderRelic_MoveMiddleBackward_InsertsCorrectly()
        {
            AddStubRelics("A", "B", "C", "D", "E");

            _mgr.ReorderRelic(3, 1); // Move D from index 3 to index 1

            Assert.AreEqual("A", _mgr.OrderedRelics[0].Id);
            Assert.AreEqual("D", _mgr.OrderedRelics[1].Id);
            Assert.AreEqual("B", _mgr.OrderedRelics[2].Id);
            Assert.AreEqual("C", _mgr.OrderedRelics[3].Id);
            Assert.AreEqual("E", _mgr.OrderedRelics[4].Id);
        }

        [Test]
        public void ReorderRelic_AdjacentSwapForward_CorrectOrder()
        {
            AddStubRelics("A", "B", "C");

            _mgr.ReorderRelic(0, 1);

            Assert.AreEqual("B", _mgr.OrderedRelics[0].Id);
            Assert.AreEqual("A", _mgr.OrderedRelics[1].Id);
            Assert.AreEqual("C", _mgr.OrderedRelics[2].Id);
        }

        [Test]
        public void ReorderRelic_AdjacentSwapBackward_CorrectOrder()
        {
            AddStubRelics("A", "B", "C");

            _mgr.ReorderRelic(2, 1);

            Assert.AreEqual("A", _mgr.OrderedRelics[0].Id);
            Assert.AreEqual("C", _mgr.OrderedRelics[1].Id);
            Assert.AreEqual("B", _mgr.OrderedRelics[2].Id);
        }

        // ════════════════════════════════════════════════════════════════════
        // Same-index no-op
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void ReorderRelic_SameIndex_IsNoOp()
        {
            AddStubRelics("A", "B", "C");

            _mgr.ReorderRelic(1, 1);

            Assert.AreEqual("A", _mgr.OrderedRelics[0].Id);
            Assert.AreEqual("B", _mgr.OrderedRelics[1].Id);
            Assert.AreEqual("C", _mgr.OrderedRelics[2].Id);
        }

        // ════════════════════════════════════════════════════════════════════
        // OwnedRelics sync after reorder (AC 11)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void ReorderRelic_OwnedRelicsMatchesOrderedRelics()
        {
            AddStubRelics("A", "B", "C", "D");

            _mgr.ReorderRelic(0, 3);

            Assert.AreEqual(_mgr.OrderedRelics.Count, _ctx.OwnedRelics.Count);
            for (int i = 0; i < _mgr.OrderedRelics.Count; i++)
            {
                Assert.AreEqual(_mgr.OrderedRelics[i].Id, _ctx.OwnedRelics[i],
                    $"Mismatch at index {i}");
            }
        }

        [Test]
        public void MultipleReorders_OwnedRelicsAlwaysInSync()
        {
            AddStubRelics("A", "B", "C", "D", "E");

            _mgr.ReorderRelic(0, 4);
            AssertOwnedRelicsInSync();

            _mgr.ReorderRelic(2, 0);
            AssertOwnedRelicsInSync();

            _mgr.ReorderRelic(1, 3);
            AssertOwnedRelicsInSync();
        }

        // ════════════════════════════════════════════════════════════════════
        // Dispatch order follows new order after reorder (AC 7)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void DispatchRoundStart_AfterReorder_UsesNewOrder()
        {
            var order = new List<string>();
            RelicFactory.Register("A", () => new OrderTrackingRelic("A", order));
            RelicFactory.Register("B", () => new OrderTrackingRelic("B", order));
            RelicFactory.Register("C", () => new OrderTrackingRelic("C", order));

            _mgr.AddRelic("A");
            _mgr.AddRelic("B");
            _mgr.AddRelic("C");

            // Reorder: move A to end → B, C, A
            _mgr.ReorderRelic(0, 2);
            order.Clear();

            _mgr.DispatchRoundStart(new RoundStartedEvent());

            Assert.AreEqual(3, order.Count);
            Assert.AreEqual("B", order[0]);
            Assert.AreEqual("C", order[1]);
            Assert.AreEqual("A", order[2]);
        }

        [Test]
        public void DispatchAfterTrade_AfterReorder_UsesNewOrder()
        {
            var order = new List<string>();
            RelicFactory.Register("X", () => new OrderTrackingRelic("X", order));
            RelicFactory.Register("Y", () => new OrderTrackingRelic("Y", order));
            RelicFactory.Register("Z", () => new OrderTrackingRelic("Z", order));

            _mgr.AddRelic("X");
            _mgr.AddRelic("Y");
            _mgr.AddRelic("Z");

            // Reorder: move Z to front → Z, X, Y
            _mgr.ReorderRelic(2, 0);
            order.Clear();

            _mgr.DispatchAfterTrade(new TradeExecutedEvent());

            Assert.AreEqual(3, order.Count);
            Assert.AreEqual("Z", order[0]);
            Assert.AreEqual("X", order[1]);
            Assert.AreEqual("Y", order[2]);
        }

        // ════════════════════════════════════════════════════════════════════
        // Order persists across round transitions (AC 8)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void ReorderRelic_OrderPersistsAcrossRoundAdvance()
        {
            AddStubRelics("A", "B", "C");

            _mgr.ReorderRelic(2, 0); // C, A, B

            // Simulate round transition
            _ctx.CurrentRound = 2;

            // Verify order unchanged
            Assert.AreEqual("C", _mgr.OrderedRelics[0].Id);
            Assert.AreEqual("A", _mgr.OrderedRelics[1].Id);
            Assert.AreEqual("B", _mgr.OrderedRelics[2].Id);
            Assert.AreEqual("C", _ctx.OwnedRelics[0]);
            Assert.AreEqual("A", _ctx.OwnedRelics[1]);
            Assert.AreEqual("B", _ctx.OwnedRelics[2]);
        }

        // ════════════════════════════════════════════════════════════════════
        // Out-of-bounds safety
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void ReorderRelic_NegativeFrom_DoesNothing()
        {
            AddStubRelics("A", "B");
            _mgr.ReorderRelic(-1, 1);
            Assert.AreEqual("A", _mgr.OrderedRelics[0].Id);
            Assert.AreEqual("B", _mgr.OrderedRelics[1].Id);
        }

        [Test]
        public void ReorderRelic_ToIndexBeyondCount_DoesNothing()
        {
            AddStubRelics("A", "B");
            _mgr.ReorderRelic(0, 5);
            Assert.AreEqual("A", _mgr.OrderedRelics[0].Id);
            Assert.AreEqual("B", _mgr.OrderedRelics[1].Id);
        }

        [Test]
        public void ReorderRelic_EmptyList_DoesNothing()
        {
            _mgr.ReorderRelic(0, 1);
            Assert.AreEqual(0, _mgr.OrderedRelics.Count);
        }

        [Test]
        public void ReorderRelic_SingleRelic_SameIndexNoOp()
        {
            AddStubRelics("A");
            _mgr.ReorderRelic(0, 0);
            Assert.AreEqual(1, _mgr.OrderedRelics.Count);
            Assert.AreEqual("A", _mgr.OrderedRelics[0].Id);
        }

        // ════════════════════════════════════════════════════════════════════
        // Helpers
        // ════════════════════════════════════════════════════════════════════

        private void AssertOwnedRelicsInSync()
        {
            Assert.AreEqual(_mgr.OrderedRelics.Count, _ctx.OwnedRelics.Count,
                "OwnedRelics count mismatch");
            for (int i = 0; i < _mgr.OrderedRelics.Count; i++)
            {
                Assert.AreEqual(_mgr.OrderedRelics[i].Id, _ctx.OwnedRelics[i],
                    $"OwnedRelics out of sync at index {i}");
            }
        }

        /// <summary>
        /// Relic that records its ID when OnRoundStart or OnAfterTrade is called.
        /// </summary>
        private class OrderTrackingRelic : RelicBase
        {
            private readonly string _id;
            private readonly List<string> _order;
            public override string Id => _id;

            public OrderTrackingRelic(string id, List<string> order)
            {
                _id = id;
                _order = order;
            }

            public override void OnRoundStart(RunContext ctx, RoundStartedEvent e) => _order.Add(_id);
            public override void OnAfterTrade(RunContext ctx, TradeExecutedEvent e) => _order.Add(_id);
        }
    }
}
