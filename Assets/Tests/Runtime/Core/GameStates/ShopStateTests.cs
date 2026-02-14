using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace BullRun.Tests.Core.GameStates
{
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
                TradeExecutor = null
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
                TradeExecutor = null
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

        [Test]
        public void Enter_PublishesShopOpenedEvent_WithCurrentCash()
        {
            ShopOpenedEvent received = default;
            EventBus.Subscribe<ShopOpenedEvent>(e => received = e);

            EnterShop();

            Assert.AreEqual(1000f, received.CurrentCash, 0.01f);
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

        // === Purchase flow tests ===

        [Test]
        public void OnPurchaseRequested_DeductsCashAndTracksItem()
        {
            var shopState = EnterShop();
            float startCash = _ctx.Portfolio.Cash;

            ShopItemPurchasedEvent receivedEvent = default;
            bool eventFired = false;
            EventBus.Subscribe<ShopItemPurchasedEvent>(e =>
            {
                eventFired = true;
                receivedEvent = e;
            });

            shopState.OnPurchaseRequested(_ctx, 0);

            Assert.IsTrue(eventFired, "ShopItemPurchasedEvent was not fired");
            Assert.Less(_ctx.Portfolio.Cash, startCash, "Cash should have been deducted");
            Assert.AreEqual(receivedEvent.Cost, (int)(startCash - _ctx.Portfolio.Cash),
                "Deducted amount should match item cost");
            Assert.AreEqual(1, _ctx.ActiveItems.Count, "One item should be tracked");
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

        [Test]
        public void OnPurchaseRequested_RejectsWhenInsufficientCash()
        {
            var poorCtx = new RunContext(1, 1, new Portfolio(10f));
            poorCtx.Portfolio.StartRound(poorCtx.Portfolio.Cash);
            var poorSm = new GameStateMachine(poorCtx);

            ShopState.NextConfig = new ShopStateConfig
            {
                StateMachine = poorSm,
                PriceGenerator = null,
                TradeExecutor = null
            };
            poorSm.TransitionTo<ShopState>();
            var shopState = (ShopState)poorSm.CurrentState;

            bool eventFired = false;
            EventBus.Subscribe<ShopItemPurchasedEvent>(_ => eventFired = true);

            shopState.OnPurchaseRequested(poorCtx, 0);

            Assert.IsFalse(eventFired, "Purchase should be rejected with insufficient cash");
            Assert.AreEqual(10f, poorCtx.Portfolio.Cash, 0.01f, "Cash should be unchanged");
            Assert.AreEqual(0, poorCtx.ActiveItems.Count, "No items should be tracked");
        }

        [Test]
        public void OnPurchaseRequested_CannotPurchaseSameCardTwice()
        {
            var shopState = EnterShop();

            shopState.OnPurchaseRequested(_ctx, 0);
            float cashAfterFirst = _ctx.Portfolio.Cash;

            shopState.OnPurchaseRequested(_ctx, 0);

            Assert.AreEqual(cashAfterFirst, _ctx.Portfolio.Cash, 0.01f,
                "Cash should not change on duplicate purchase");
            Assert.AreEqual(1, _ctx.ActiveItems.Count,
                "Should still only have 1 item tracked");
        }

        [Test]
        public void OnPurchaseRequested_CanBuyMultipleCards()
        {
            // Use ample cash so all 3 items are guaranteed affordable (max item cost is $600)
            var richCtx = new RunContext(1, 1, new Portfolio(10000f));
            richCtx.Portfolio.StartRound(richCtx.Portfolio.Cash);
            var richSm = new GameStateMachine(richCtx);

            ShopState.NextConfig = new ShopStateConfig
            {
                StateMachine = richSm,
                PriceGenerator = null,
                TradeExecutor = null
            };
            richSm.TransitionTo<ShopState>();
            var shopState = (ShopState)richSm.CurrentState;

            shopState.OnPurchaseRequested(richCtx, 0);
            shopState.OnPurchaseRequested(richCtx, 1);
            shopState.OnPurchaseRequested(richCtx, 2);

            Assert.AreEqual(3, richCtx.ActiveItems.Count,
                "All 3 items should be purchased with ample cash");
        }

        // === ShopClosedEvent tests ===

        [Test]
        public void ShopClosedEvent_ContainsPurchasedItemIds()
        {
            // Use null StateMachine so CloseShop skips state transitions
            ShopState.NextConfig = new ShopStateConfig
            {
                StateMachine = null,
                PriceGenerator = null,
                TradeExecutor = null
            };
            var state = new ShopState();
            state.Enter(_ctx);

            // Purchase first item
            state.OnPurchaseRequested(_ctx, 0);
            Assert.AreEqual(1, _ctx.ActiveItems.Count, "Setup: one item should be purchased");
            string purchasedId = _ctx.ActiveItems[0];

            ShopClosedEvent closedEvent = default;
            bool closedFired = false;
            EventBus.Subscribe<ShopClosedEvent>(e =>
            {
                closedFired = true;
                closedEvent = e;
            });

            // Trigger close via Continue button callback (reflection)
            InvokeCloseShop(state, _ctx);

            Assert.IsTrue(closedFired, "ShopClosedEvent should fire when shop closes");
            Assert.AreEqual(1, closedEvent.PurchasedItemIds.Length);
            Assert.AreEqual(purchasedId, closedEvent.PurchasedItemIds[0]);
            Assert.AreEqual(1, closedEvent.RoundNumber);
        }

        [Test]
        public void ShopClosedEvent_ContainsCashRemaining()
        {
            ShopState.NextConfig = new ShopStateConfig
            {
                StateMachine = null,
                PriceGenerator = null,
                TradeExecutor = null
            };
            var state = new ShopState();
            state.Enter(_ctx);

            state.OnPurchaseRequested(_ctx, 0);
            float cashAfterPurchase = _ctx.Portfolio.Cash;

            ShopClosedEvent closedEvent = default;
            bool closedFired = false;
            EventBus.Subscribe<ShopClosedEvent>(e =>
            {
                closedFired = true;
                closedEvent = e;
            });

            InvokeCloseShop(state, _ctx);

            Assert.IsTrue(closedFired, "ShopClosedEvent should fire");
            Assert.AreEqual(cashAfterPurchase, closedEvent.CashRemaining, 0.01f);
        }

        [Test]
        public void Update_DoesNotAutoCloseShop()
        {
            ShopState.NextConfig = new ShopStateConfig
            {
                StateMachine = null,
                PriceGenerator = null,
                TradeExecutor = null
            };
            var state = new ShopState();
            state.Enter(_ctx);

            bool closedFired = false;
            EventBus.Subscribe<ShopClosedEvent>(_ => closedFired = true);

            // Simulate many Update calls — shop should never auto-close
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
                TradeExecutor = null
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

            // Simulate Continue button press via CloseShop
            InvokeCloseShop(state, _ctx);

            Assert.IsTrue(closedFired, "Continue button should trigger ShopClosedEvent");
            Assert.AreEqual(0, closedEvent.PurchasedItemIds.Length, "No items purchased");
        }

        // === Zero purchases test (AC: 2 — can buy 0 items) ===

        [Test]
        public void NoPurchases_CashUnchanged()
        {
            EnterShop();
            // Don't buy anything — cash should remain at starting value
            Assert.AreEqual(1000f, _ctx.Portfolio.Cash, 0.01f);
        }
    }
}
