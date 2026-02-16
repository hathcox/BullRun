using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace BullRun.Tests.Core.GameStates
{
    /// <summary>
    /// FIX-12: Updated to use Reputation currency instead of Portfolio.Cash for shop purchases.
    /// </summary>
    [TestFixture]
    public class ShopStateTests
    {
        private RunContext _ctx;
        private GameStateMachine _sm;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            _ctx = new RunContext(1, 1, new Portfolio(1000f));
            _ctx.Portfolio.StartRound(_ctx.Portfolio.Cash);
            // FIX-12: Seed Reputation for shop tests
            _ctx.Reputation.Add(10000);
            _sm = new GameStateMachine(_ctx);
            ShopState.ShopUIInstance = null;
        }

        [TearDown]
        public void TearDown()
        {
            ShopState.NextConfig = null;
            ShopState.ShopUIInstance = null;
            TierTransitionState.NextConfig = null;
            EventBus.Clear();
        }

        private ShopState EnterShop()
        {
            ShopState.NextConfig = new ShopStateConfig
            {
                StateMachine = _sm,
                PriceGenerator = null,
                TradeExecutor = null,
                EventScheduler = null
            };
            _sm.TransitionTo<ShopState>();
            return (ShopState)_sm.CurrentState;
        }

        private void InvokeCloseShop(ShopState state, RunContext ctx)
        {
            var method = typeof(ShopState).GetMethod("CloseShop",
                BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(state, new object[] { ctx });
        }

        [Test]
        public void Enter_DoesNotThrow()
        {
            ShopState.NextConfig = new ShopStateConfig
            {
                StateMachine = _sm,
                PriceGenerator = null,
                TradeExecutor = null,
                EventScheduler = null
            };
            var state = new ShopState();
            Assert.DoesNotThrow(() => state.Enter(_ctx));
        }

        [Test]
        public void ImplementsIGameState()
        {
            var state = new ShopState();
            Assert.IsInstanceOf<IGameState>(state);
        }

        [Test]
        public void Enter_PublishesShopOpenedEvent()
        {
            bool eventFired = false;
            EventBus.Subscribe<ShopOpenedEvent>(e =>
            {
                eventFired = true;
                Assert.AreEqual(1, e.RoundNumber);
                Assert.AreEqual(3, e.AvailableItems.Length);
            });

            EnterShop();
            Assert.IsTrue(eventFired, "ShopOpenedEvent was not published");
        }

        // FIX-12: ShopOpenedEvent now carries Reputation, not cash
        [Test]
        public void Enter_PublishesShopOpenedEvent_WithCurrentReputation()
        {
            ShopOpenedEvent received = default;
            EventBus.Subscribe<ShopOpenedEvent>(e => received = e);

            EnterShop();

            Assert.AreEqual(10000, received.CurrentReputation);
        }

        [Test]
        public void Enter_GeneratesThreeItems_OnePerCategory()
        {
            ShopOpenedEvent received = default;
            EventBus.Subscribe<ShopOpenedEvent>(e => received = e);

            EnterShop();

            Assert.AreEqual(3, received.AvailableItems.Length);
            Assert.AreEqual(ItemCategory.TradingTool, received.AvailableItems[0].Category);
            Assert.AreEqual(ItemCategory.MarketIntel, received.AvailableItems[1].Category);
            Assert.AreEqual(ItemCategory.PassivePerk, received.AvailableItems[2].Category);
        }

        [Test]
        public void Enter_StaysInShopState()
        {
            EnterShop();
            Assert.IsInstanceOf<ShopState>(_sm.CurrentState);
        }

        [Test]
        public void Enter_DoesNotAdvanceRound_BeforeClosed()
        {
            Assert.AreEqual(1, _ctx.CurrentRound);
            EnterShop();
            Assert.AreEqual(1, _ctx.CurrentRound, "Round should not advance until shop closes");
        }

        [Test]
        public void Exit_DoesNotThrow_WithoutUI()
        {
            var state = new ShopState();
            ShopState.NextConfig = new ShopStateConfig { StateMachine = _sm };
            state.Enter(_ctx);
            Assert.DoesNotThrow(() => state.Exit(_ctx));
        }

        // === Purchase flow tests (FIX-12: uses Reputation) ===

        [Test]
        public void OnPurchaseRequested_DeductsReputationAndTracksItem()
        {
            var shopState = EnterShop();
            int startRep = _ctx.Reputation.Current;

            ShopItemPurchasedEvent receivedEvent = default;
            bool eventFired = false;
            EventBus.Subscribe<ShopItemPurchasedEvent>(e =>
            {
                eventFired = true;
                receivedEvent = e;
            });

            shopState.OnPurchaseRequested(_ctx, 0);

            Assert.IsTrue(eventFired, "ShopItemPurchasedEvent was not fired");
            Assert.Less(_ctx.Reputation.Current, startRep, "Rep should have been deducted");
            Assert.AreEqual(receivedEvent.Cost, startRep - _ctx.Reputation.Current,
                "Deducted amount should match item cost");
            Assert.AreEqual(1, _ctx.OwnedRelics.Count, "One item should be tracked");
            // FIX-12 AC 5: Cash must be untouched
            Assert.AreEqual(1000f, _ctx.Portfolio.Cash, 0.01f, "Cash must not be deducted by shop");
        }

        [Test]
        public void OnPurchaseRequested_PublishesEventWithItemName()
        {
            var shopState = EnterShop();

            ShopItemPurchasedEvent received = default;
            EventBus.Subscribe<ShopItemPurchasedEvent>(e => received = e);

            shopState.OnPurchaseRequested(_ctx, 0);

            Assert.IsNotNull(received.ItemName, "ItemName should be populated");
            Assert.IsNotEmpty(received.ItemName, "ItemName should not be empty");
        }

        // FIX-12: Insufficient Reputation (not cash) rejects purchase
        [Test]
        public void OnPurchaseRequested_RejectsWhenInsufficientReputation()
        {
            var poorCtx = new RunContext(1, 1, new Portfolio(10000f));
            poorCtx.Portfolio.StartRound(poorCtx.Portfolio.Cash);
            // 0 Reputation — can't afford anything
            var poorSm = new GameStateMachine(poorCtx);

            ShopState.NextConfig = new ShopStateConfig
            {
                StateMachine = poorSm,
                PriceGenerator = null,
                TradeExecutor = null,
                EventScheduler = null
            };
            poorSm.TransitionTo<ShopState>();
            var shopState = (ShopState)poorSm.CurrentState;

            bool eventFired = false;
            EventBus.Subscribe<ShopItemPurchasedEvent>(_ => eventFired = true);

            shopState.OnPurchaseRequested(poorCtx, 0);

            Assert.IsFalse(eventFired, "Purchase should be rejected with insufficient Reputation");
            Assert.AreEqual(0, poorCtx.Reputation.Current, "Rep should be unchanged");
            Assert.AreEqual(0, poorCtx.OwnedRelics.Count, "No items should be tracked");
        }

        [Test]
        public void OnPurchaseRequested_CannotPurchaseSameCardTwice()
        {
            var shopState = EnterShop();

            shopState.OnPurchaseRequested(_ctx, 0);
            int repAfterFirst = _ctx.Reputation.Current;

            shopState.OnPurchaseRequested(_ctx, 0);

            Assert.AreEqual(repAfterFirst, _ctx.Reputation.Current,
                "Rep should not change on duplicate purchase");
            Assert.AreEqual(1, _ctx.OwnedRelics.Count,
                "Should still only have 1 item tracked");
        }

        [Test]
        public void OnPurchaseRequested_CanBuyMultipleCards()
        {
            // Use ample Rep so all 3 items are guaranteed affordable
            var richCtx = new RunContext(1, 1, new Portfolio(10000f));
            richCtx.Portfolio.StartRound(richCtx.Portfolio.Cash);
            richCtx.Reputation.Add(10000);
            var richSm = new GameStateMachine(richCtx);

            ShopState.NextConfig = new ShopStateConfig
            {
                StateMachine = richSm,
                PriceGenerator = null,
                TradeExecutor = null,
                EventScheduler = null
            };
            richSm.TransitionTo<ShopState>();
            var shopState = (ShopState)richSm.CurrentState;

            shopState.OnPurchaseRequested(richCtx, 0);
            shopState.OnPurchaseRequested(richCtx, 1);
            shopState.OnPurchaseRequested(richCtx, 2);

            Assert.AreEqual(3, richCtx.OwnedRelics.Count,
                "All 3 items should be purchased with ample Rep");
        }

        // === ShopClosedEvent tests (FIX-12: carries Rep, not cash) ===

        [Test]
        public void ShopClosedEvent_ContainsPurchasedItemIds()
        {
            ShopState.NextConfig = new ShopStateConfig
            {
                StateMachine = null,
                PriceGenerator = null,
                TradeExecutor = null,
                EventScheduler = null
            };
            var state = new ShopState();
            state.Enter(_ctx);

            state.OnPurchaseRequested(_ctx, 0);
            Assert.AreEqual(1, _ctx.OwnedRelics.Count, "Setup: one item should be purchased");
            string purchasedId = _ctx.OwnedRelics[0];

            ShopClosedEvent closedEvent = default;
            bool closedFired = false;
            EventBus.Subscribe<ShopClosedEvent>(e =>
            {
                closedFired = true;
                closedEvent = e;
            });

            InvokeCloseShop(state, _ctx);

            Assert.IsTrue(closedFired, "ShopClosedEvent should fire when shop closes");
            Assert.AreEqual(1, closedEvent.PurchasedItemIds.Length);
            Assert.AreEqual(purchasedId, closedEvent.PurchasedItemIds[0]);
            Assert.AreEqual(1, closedEvent.RoundNumber);
        }

        [Test]
        public void ShopClosedEvent_ContainsReputationRemaining()
        {
            ShopState.NextConfig = new ShopStateConfig
            {
                StateMachine = null,
                PriceGenerator = null,
                TradeExecutor = null,
                EventScheduler = null
            };
            var state = new ShopState();
            state.Enter(_ctx);

            state.OnPurchaseRequested(_ctx, 0);
            int repAfterPurchase = _ctx.Reputation.Current;

            ShopClosedEvent closedEvent = default;
            bool closedFired = false;
            EventBus.Subscribe<ShopClosedEvent>(e =>
            {
                closedFired = true;
                closedEvent = e;
            });

            InvokeCloseShop(state, _ctx);

            Assert.IsTrue(closedFired, "ShopClosedEvent should fire");
            Assert.AreEqual(repAfterPurchase, closedEvent.ReputationRemaining);
        }

        [Test]
        public void Update_DoesNotAutoCloseShop()
        {
            ShopState.NextConfig = new ShopStateConfig
            {
                StateMachine = null,
                PriceGenerator = null,
                TradeExecutor = null,
                EventScheduler = null
            };
            var state = new ShopState();
            state.Enter(_ctx);

            bool closedFired = false;
            EventBus.Subscribe<ShopClosedEvent>(_ => closedFired = true);

            for (int i = 0; i < 1000; i++)
            {
                state.Update(_ctx);
            }

            Assert.IsFalse(closedFired, "Shop should NOT auto-close — player controls when to leave");
        }

        [Test]
        public void CloseShop_ViaCallback_PublishesShopClosedEvent()
        {
            ShopState.NextConfig = new ShopStateConfig
            {
                StateMachine = null,
                PriceGenerator = null,
                TradeExecutor = null,
                EventScheduler = null
            };
            var state = new ShopState();
            state.Enter(_ctx);

            ShopClosedEvent closedEvent = default;
            bool closedFired = false;
            EventBus.Subscribe<ShopClosedEvent>(e =>
            {
                closedFired = true;
                closedEvent = e;
            });

            InvokeCloseShop(state, _ctx);

            Assert.IsTrue(closedFired, "Continue button should trigger ShopClosedEvent");
            Assert.AreEqual(0, closedEvent.PurchasedItemIds.Length, "No items purchased");
        }

        // === Zero purchases test ===

        [Test]
        public void NoPurchases_CashUnchanged()
        {
            EnterShop();
            Assert.AreEqual(1000f, _ctx.Portfolio.Cash, 0.01f);
        }

        // === Code Review Fix: Exit fires ShopClosedEvent safety net ===

        [Test]
        public void Exit_FiresShopClosedEvent_WhenShopStillActive()
        {
            ShopState.NextConfig = new ShopStateConfig
            {
                StateMachine = null,
                PriceGenerator = null,
                TradeExecutor = null,
                EventScheduler = null
            };
            var state = new ShopState();
            state.Enter(_ctx);

            ShopClosedEvent closedEvent = default;
            bool closedFired = false;
            EventBus.Subscribe<ShopClosedEvent>(e =>
            {
                closedFired = true;
                closedEvent = e;
            });

            // Exit without calling CloseShop — safety net should fire event
            state.Exit(_ctx);

            Assert.IsTrue(closedFired, "ShopClosedEvent should fire from Exit safety net");
            Assert.AreEqual(1, closedEvent.RoundNumber);
        }

        [Test]
        public void Exit_DoesNotDoubleFireShopClosedEvent_AfterCloseShop()
        {
            ShopState.NextConfig = new ShopStateConfig
            {
                StateMachine = null,
                PriceGenerator = null,
                TradeExecutor = null,
                EventScheduler = null
            };
            var state = new ShopState();
            state.Enter(_ctx);

            // Close shop normally first
            InvokeCloseShop(state, _ctx);

            int fireCount = 0;
            EventBus.Subscribe<ShopClosedEvent>(_ => fireCount++);

            // Exit should NOT fire again
            state.Exit(_ctx);
            Assert.AreEqual(0, fireCount, "ShopClosedEvent should not fire twice");
        }
    }
}
