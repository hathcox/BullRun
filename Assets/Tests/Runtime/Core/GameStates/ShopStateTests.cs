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
        public void Enter_StaysInShopState_UntilTimerExpires()
        {
            EnterShop();
            Assert.IsInstanceOf<ShopState>(_sm.CurrentState);
        }

        [Test]
        public void Enter_DoesNotAdvanceRound_BeforeTimerExpires()
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

        [Test]
        public void ShopTimerDuration_IsWithinGDDRange()
        {
            Assert.GreaterOrEqual(GameConfig.ShopDurationSeconds, 15f);
            Assert.LessOrEqual(GameConfig.ShopDurationSeconds, 20f);
        }

        // === Purchase flow tests (consolidated from Shop/ShopStateTests) ===

        [Test]
        public void OnPurchase_DeductsCashAndTracksItem()
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

            // Purchase first item (index 0) through actual ShopState.OnPurchase
            shopState.OnPurchase(_ctx, 0);

            Assert.IsTrue(eventFired, "ShopItemPurchasedEvent was not fired");
            Assert.Less(_ctx.Portfolio.Cash, startCash, "Cash should have been deducted");
            Assert.AreEqual(receivedEvent.Cost, (int)(startCash - _ctx.Portfolio.Cash),
                "Deducted amount should match item cost");
            Assert.AreEqual(1, _ctx.ActiveItems.Count, "One item should be tracked");
        }

        [Test]
        public void OnPurchase_RejectsWhenInsufficientCash()
        {
            // Create context with very little cash — all items cost >= $100
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

            shopState.OnPurchase(poorCtx, 0);

            Assert.IsFalse(eventFired, "Purchase should be rejected with insufficient cash");
            Assert.AreEqual(10f, poorCtx.Portfolio.Cash, 0.01f, "Cash should be unchanged");
            Assert.AreEqual(0, poorCtx.ActiveItems.Count, "No items should be tracked");
        }

        [Test]
        public void OnPurchase_CannotPurchaseSameItemTwice()
        {
            var shopState = EnterShop();

            // Purchase item at index 0
            shopState.OnPurchase(_ctx, 0);
            float cashAfterFirst = _ctx.Portfolio.Cash;

            // Try to purchase same index again
            shopState.OnPurchase(_ctx, 0);

            Assert.AreEqual(cashAfterFirst, _ctx.Portfolio.Cash, 0.01f,
                "Cash should not change on duplicate purchase");
            Assert.AreEqual(1, _ctx.ActiveItems.Count,
                "Should still only have 1 item tracked");
        }
    }
}
