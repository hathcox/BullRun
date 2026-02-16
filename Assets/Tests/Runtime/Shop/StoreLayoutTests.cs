using NUnit.Framework;
using UnityEngine;

namespace BullRun.Tests.Shop
{
    /// <summary>
    /// Tests for the multi-panel store layout (Story 13.2).
    /// Covers: store open/display, currency display, event payloads, panel structure.
    /// </summary>
    [TestFixture]
    public class StoreLayoutTests
    {
        private RunContext _ctx;
        private GameStateMachine _sm;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            _ctx = new RunContext(1, 1, new Portfolio(5000f));
            _ctx.Portfolio.StartRound(_ctx.Portfolio.Cash);
            _ctx.Reputation.Add(10000); // Ample rep to guarantee all purchases succeed regardless of rarity
            _sm = new GameStateMachine(_ctx);
            ShopState.ShopUIInstance = null;
        }

        [TearDown]
        public void TearDown()
        {
            ShopState.NextConfig = null;
            ShopState.ShopUIInstance = null;
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
            var state = new ShopState();
            state.Enter(_ctx);
            return state;
        }

        // === Store Opens and Displays All 4 Panel Areas (AC: 1, 2, 3, 4, 5) ===

        [Test]
        public void Enter_PublishesShopOpenedEvent_WithSectionAvailability()
        {
            ShopOpenedEvent received = default;
            bool fired = false;
            EventBus.Subscribe<ShopOpenedEvent>(e =>
            {
                received = e;
                fired = true;
            });

            EnterShop();

            Assert.IsTrue(fired, "ShopOpenedEvent should fire on store open");
            Assert.AreEqual(_ctx.CurrentRound, received.RoundNumber);
            Assert.AreEqual(_ctx.Reputation.Current, received.CurrentReputation);
            // Placeholder sections not yet available (13.3-13.6)
            Assert.IsFalse(received.ExpansionsAvailable);
            Assert.IsFalse(received.TipsAvailable);
            Assert.IsFalse(received.BondAvailable);
        }

        [Test]
        public void Enter_ShopOpenedEvent_ContainsAvailableRelics()
        {
            ShopOpenedEvent received = default;
            EventBus.Subscribe<ShopOpenedEvent>(e => received = e);

            EnterShop();

            Assert.IsNotNull(received.AvailableItems, "AvailableItems should not be null");
            // ShopGenerator produces up to 3 items (one per category)
            Assert.LessOrEqual(received.AvailableItems.Length, 3);
        }

        // === "Next Round" button transitions to next state (AC: 9) ===

        [Test]
        public void CloseShop_PublishesShopClosedEvent_WithPerSectionCounts()
        {
            ShopClosedEvent closedEvent = default;
            bool closedFired = false;
            EventBus.Subscribe<ShopClosedEvent>(e =>
            {
                closedEvent = e;
                closedFired = true;
            });

            var state = EnterShop();

            // Simulate closing via Exit (no UI, so use Exit safety net)
            state.Exit(_ctx);

            Assert.IsTrue(closedFired, "ShopClosedEvent should fire on store close");
            Assert.AreEqual(_ctx.CurrentRound, closedEvent.RoundNumber);
            Assert.AreEqual(0, closedEvent.ExpansionsPurchased);
            Assert.AreEqual(0, closedEvent.TipsPurchased);
            Assert.AreEqual(0, closedEvent.BondsPurchased);
        }

        [Test]
        public void CloseShop_ShopClosedEvent_TracksRelicPurchaseCount()
        {
            var state = EnterShop();

            // Purchase a relic (card index 0)
            state.OnPurchaseRequested(_ctx, 0);

            ShopClosedEvent closedEvent = default;
            EventBus.Subscribe<ShopClosedEvent>(e => closedEvent = e);
            state.Exit(_ctx);

            Assert.AreEqual(1, closedEvent.RelicsPurchased);
        }

        // === Currency displays show correct values (AC: 6, 7) ===

        [Test]
        public void ShopOpenedEvent_CarriesCorrectReputation()
        {
            _ctx.Reputation.Add(25); // Now 10025 total
            ShopOpenedEvent received = default;
            EventBus.Subscribe<ShopOpenedEvent>(e => received = e);

            EnterShop();

            Assert.AreEqual(_ctx.Reputation.Current, received.CurrentReputation);
        }

        [Test]
        public void CashBalance_AvailableViaContext_DuringShop()
        {
            // AC 7: Cash balance should be accessible during shop for display
            float expectedCash = _ctx.Portfolio.Cash;
            EnterShop();
            Assert.AreEqual(expectedCash, _ctx.Portfolio.Cash, 0.01f,
                "Cash balance should remain accessible during store visit");
        }

        // === Events fire with correct payloads (AC: 10) ===

        [Test]
        public void ShopClosedEvent_ContainsReputationRemaining()
        {
            var state = EnterShop();

            ShopClosedEvent closedEvent = default;
            EventBus.Subscribe<ShopClosedEvent>(e => closedEvent = e);
            state.Exit(_ctx);

            Assert.AreEqual(_ctx.Reputation.Current, closedEvent.ReputationRemaining);
        }

        [Test]
        public void ShopClosedEvent_ContainsRoundNumber()
        {
            _ctx.CurrentRound = 3;
            var state = EnterShop();

            ShopClosedEvent closedEvent = default;
            EventBus.Subscribe<ShopClosedEvent>(e => closedEvent = e);
            state.Exit(_ctx);

            Assert.AreEqual(3, closedEvent.RoundNumber);
        }

        [Test]
        public void Enter_WithNoUI_DoesNotThrow()
        {
            ShopState.ShopUIInstance = null;
            Assert.DoesNotThrow(() => EnterShop());
        }

        [Test]
        public void Exit_WithNoUI_DoesNotThrow()
        {
            ShopState.ShopUIInstance = null;
            var state = EnterShop();
            Assert.DoesNotThrow(() => state.Exit(_ctx));
        }

        // === ShopUI color constants exist (verify static colors used in layout) ===

        [Test]
        public void ShopUI_ReputationColor_IsAmberGold()
        {
            var color = ShopUI.ReputationColor;
            Assert.AreEqual(1f, color.r, 0.01f);
            Assert.AreEqual(0.7f, color.g, 0.01f);
            Assert.AreEqual(0f, color.b, 0.01f);
        }

        [Test]
        public void ShopUI_CashColor_IsGreen()
        {
            var color = ShopUI.CashColor;
            Assert.Greater(color.g, 0.8f);
            Assert.Less(color.r, 0.5f);
        }

        [Test]
        public void ShopUI_PanelBgColor_IsDark()
        {
            var color = ShopUI.PanelBgColor;
            Assert.Less(color.r, 0.1f);
            Assert.Less(color.g, 0.1f);
        }

        [Test]
        public void ShopUI_PanelHeaderColor_IsMutedBlue()
        {
            var color = ShopUI.PanelHeaderColor;
            Assert.Greater(color.b, 0.5f);
        }

        [Test]
        public void ShopUI_GetRarityColor_ReturnsCorrectColors()
        {
            Assert.AreEqual(ShopUI.CommonColor, ShopUI.GetRarityColor(ItemRarity.Common));
            Assert.AreEqual(ShopUI.UncommonColor, ShopUI.GetRarityColor(ItemRarity.Uncommon));
            Assert.AreEqual(ShopUI.RareColor, ShopUI.GetRarityColor(ItemRarity.Rare));
            Assert.AreEqual(ShopUI.LegendaryColor, ShopUI.GetRarityColor(ItemRarity.Legendary));
        }

        // === Keyboard Navigation Tests (AC: 11) ===

        [Test]
        public void ShopUI_FocusedPanelIndex_StartsAtNegativeOne()
        {
            var go = new GameObject("TestShopUI");
            var shopUI = go.AddComponent<ShopUI>();
            Assert.AreEqual(-1, shopUI.FocusedPanelIndex, "Focus should start at -1 (no panel focused)");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void ShopUI_SetBottomPanels_CreatesFocusIndicators()
        {
            var go = new GameObject("TestShopUI");
            var shopUI = go.AddComponent<ShopUI>();

            var panel1 = new GameObject("Panel1");
            panel1.AddComponent<RectTransform>();
            var panel2 = new GameObject("Panel2");
            panel2.AddComponent<RectTransform>();
            var panel3 = new GameObject("Panel3");
            panel3.AddComponent<RectTransform>();

            shopUI.SetBottomPanels(panel1, panel2, panel3);

            // Each panel should have a FocusIndicator child
            Assert.IsNotNull(panel1.transform.Find("FocusIndicator_0"), "Panel 1 should have focus indicator");
            Assert.IsNotNull(panel2.transform.Find("FocusIndicator_1"), "Panel 2 should have focus indicator");
            Assert.IsNotNull(panel3.transform.Find("FocusIndicator_2"), "Panel 3 should have focus indicator");

            // Focus indicators should start inactive
            Assert.IsFalse(panel1.transform.Find("FocusIndicator_0").gameObject.activeSelf);
            Assert.IsFalse(panel2.transform.Find("FocusIndicator_1").gameObject.activeSelf);
            Assert.IsFalse(panel3.transform.Find("FocusIndicator_2").gameObject.activeSelf);

            Object.DestroyImmediate(panel3);
            Object.DestroyImmediate(panel2);
            Object.DestroyImmediate(panel1);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void ShopUI_FocusColor_IsBlueHighlight()
        {
            var color = ShopUI.FocusColor;
            Assert.Greater(color.b, 0.8f, "Focus color should have strong blue component");
            Assert.Greater(color.a, 0.3f, "Focus color should be somewhat visible");
        }
    }
}
