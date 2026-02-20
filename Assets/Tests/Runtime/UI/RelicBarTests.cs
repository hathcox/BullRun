using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BullRun.Tests.UI
{
    [TestFixture]
    public class RelicBarTests
    {
        private RunContext _ctx;
        private GameObject _barGo;
        private RelicBar _relicBar;
        private GameObject _tooltipPanel;
        private Text _tooltipNameText;
        private Text _tooltipDescText;
        private Text _tooltipEffectText;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            ItemLookup.ClearCache();
            RelicFactory.ResetRegistry();

            _ctx = new RunContext(1, 1, new Portfolio(1000f));

            _barGo = new GameObject("TestRelicBar");
            _barGo.AddComponent<RectTransform>();

            // Create tooltip panel and text components
            _tooltipPanel = new GameObject("TestTooltip");
            _tooltipPanel.AddComponent<RectTransform>();
            _tooltipPanel.AddComponent<Image>();
            _tooltipPanel.AddComponent<CanvasGroup>();

            var nameGo = new GameObject("NameText");
            nameGo.transform.SetParent(_tooltipPanel.transform, false);
            nameGo.AddComponent<RectTransform>();
            _tooltipNameText = nameGo.AddComponent<Text>();
            _tooltipNameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var descGo = new GameObject("DescText");
            descGo.transform.SetParent(_tooltipPanel.transform, false);
            descGo.AddComponent<RectTransform>();
            _tooltipDescText = descGo.AddComponent<Text>();
            _tooltipDescText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var effectGo = new GameObject("EffectText");
            effectGo.transform.SetParent(_tooltipPanel.transform, false);
            effectGo.AddComponent<RectTransform>();
            _tooltipEffectText = effectGo.AddComponent<Text>();
            _tooltipEffectText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            _relicBar = _barGo.AddComponent<RelicBar>();
            _relicBar.Initialize(_ctx, _barGo.transform, _tooltipPanel,
                _tooltipNameText, _tooltipDescText, _tooltipEffectText);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
            if (_barGo != null) Object.DestroyImmediate(_barGo);
            if (_tooltipPanel != null) Object.DestroyImmediate(_tooltipPanel);
        }

        // ═══════════════════════════════════════════════════════════════
        // RefreshRelicIcons Tests (AC 1, 2, 3, 11)
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void RefreshRelicIcons_NoRelics_CreatesZeroIcons()
        {
            _relicBar.RefreshRelicIcons();
            Assert.AreEqual(0, _relicBar.IconCount);
        }

        [Test]
        public void RefreshRelicIcons_ThreeRelics_CreatesThreeIcons()
        {
            _ctx.RelicManager.AddRelic("relic_double_dealer");
            _ctx.RelicManager.AddRelic("relic_quick_draw");
            _ctx.RelicManager.AddRelic("relic_short_multiplier");

            _relicBar.RefreshRelicIcons();

            Assert.AreEqual(3, _relicBar.IconCount);
        }

        [Test]
        public void RefreshRelicIcons_MatchesOrderedRelicsCount()
        {
            _ctx.RelicManager.AddRelic("relic_event_trigger");
            _ctx.RelicManager.AddRelic("relic_rep_doubler");

            _relicBar.RefreshRelicIcons();

            Assert.AreEqual(_ctx.RelicManager.OrderedRelics.Count, _relicBar.IconCount);
        }

        [Test]
        public void RefreshRelicIcons_ClearsOldIconsOnSecondCall()
        {
            _ctx.RelicManager.AddRelic("relic_double_dealer");
            _relicBar.RefreshRelicIcons();
            Assert.AreEqual(1, _relicBar.IconCount);

            _ctx.RelicManager.AddRelic("relic_quick_draw");
            _relicBar.RefreshRelicIcons();
            Assert.AreEqual(2, _relicBar.IconCount);
        }

        [Test]
        public void RefreshRelicIcons_EightRelics_NoOverflow()
        {
            // AC 11: Handle 5-8 relics without overflow
            _ctx.RelicManager.AddRelic("relic_double_dealer");
            _ctx.RelicManager.AddRelic("relic_quick_draw");
            _ctx.RelicManager.AddRelic("relic_short_multiplier");
            _ctx.RelicManager.AddRelic("relic_event_trigger");
            _ctx.RelicManager.AddRelic("relic_rep_doubler");
            _ctx.RelicManager.AddRelic("relic_skimmer");
            _ctx.RelicManager.AddRelic("relic_event_storm");
            _ctx.RelicManager.AddRelic("relic_loss_liquidator");

            _relicBar.RefreshRelicIcons();

            Assert.AreEqual(8, _relicBar.IconCount);
        }

        // ═══════════════════════════════════════════════════════════════
        // Tooltip Tests (AC 5, 6, 7)
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void Tooltip_InitiallyHidden()
        {
            Assert.IsFalse(_relicBar.IsTooltipVisible);
        }

        [Test]
        public void HideTooltip_SetsTooltipInactive()
        {
            _tooltipPanel.SetActive(true);
            _relicBar.HideTooltip();
            Assert.IsFalse(_tooltipPanel.activeSelf);
        }

        [Test]
        public void ShowTooltip_PopulatesNameDescEffect()
        {
            // AC 5: Tooltip contains relic name, description, effect
            _ctx.RelicManager.AddRelic("relic_double_dealer");
            _relicBar.RefreshRelicIcons();

            _relicBar.TestShowTooltip("relic_double_dealer");

            var def = ItemLookup.GetRelicById("relic_double_dealer");
            Assert.IsTrue(def.HasValue, "relic_double_dealer should exist in ItemLookup");
            Assert.IsTrue(_relicBar.TooltipNameContent.Contains(def.Value.Name));
            Assert.AreEqual(def.Value.Description, _relicBar.TooltipDescContent);
            Assert.AreEqual(def.Value.EffectDescription, _relicBar.TooltipEffectContent);
        }

        [Test]
        public void ShowTooltip_CompoundRep_ShowsDynamicSellValue()
        {
            // AC 6: Compound Rep tooltip shows current sell value
            _ctx.RelicManager.AddRelic("relic_compound_rep");
            _relicBar.RefreshRelicIcons();

            _relicBar.TestShowTooltip("relic_compound_rep");

            // CompoundRepRelic starts at 0 rounds held → sell value = 3 * 2^0 = 3
            Assert.IsTrue(_relicBar.TooltipEffectContent.Contains("Sell value:"),
                "Compound Rep tooltip should include dynamic sell value");
            Assert.IsTrue(_relicBar.TooltipEffectContent.Contains("3 Rep"),
                "Initial sell value should be 3 Rep");
        }

        [Test]
        public void ShowTooltip_NonExistentRelic_NoTooltipShown()
        {
            _relicBar.TestShowTooltip("relic_nonexistent");
            Assert.IsFalse(_relicBar.IsTooltipVisible);
        }

        // ═══════════════════════════════════════════════════════════════
        // Glow Tests (AC 8)
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void Glow_RelicActivatedEvent_StartsGlow()
        {
            _ctx.RelicManager.AddRelic("relic_double_dealer");
            _relicBar.RefreshRelicIcons();

            EventBus.Publish(new RelicActivatedEvent { RelicId = "relic_double_dealer" });

            Assert.IsTrue(_relicBar.HasActiveGlow("relic_double_dealer"));
        }

        [Test]
        public void Glow_NoMatchingIcon_NoGlowStarted()
        {
            _ctx.RelicManager.AddRelic("relic_double_dealer");
            _relicBar.RefreshRelicIcons();

            EventBus.Publish(new RelicActivatedEvent { RelicId = "relic_nonexistent" });

            Assert.IsFalse(_relicBar.HasActiveGlow("relic_nonexistent"));
        }

        [Test]
        public void Glow_MultipleSimultaneous_AllTracked()
        {
            _ctx.RelicManager.AddRelic("relic_double_dealer");
            _ctx.RelicManager.AddRelic("relic_quick_draw");
            _relicBar.RefreshRelicIcons();

            EventBus.Publish(new RelicActivatedEvent { RelicId = "relic_double_dealer" });
            EventBus.Publish(new RelicActivatedEvent { RelicId = "relic_quick_draw" });

            Assert.IsTrue(_relicBar.HasActiveGlow("relic_double_dealer"));
            Assert.IsTrue(_relicBar.HasActiveGlow("relic_quick_draw"));
        }

        [Test]
        public void Glow_InitialTimerValue_IsGlowDuration()
        {
            // AC 8: Glow lasts 0.3 seconds
            _ctx.RelicManager.AddRelic("relic_double_dealer");
            _relicBar.RefreshRelicIcons();

            EventBus.Publish(new RelicActivatedEvent { RelicId = "relic_double_dealer" });

            Assert.AreEqual(0.3f, _relicBar.GlowTimers["relic_double_dealer"], 0.001f);
        }

        // ═══════════════════════════════════════════════════════════════
        // Visibility Tests (AC 1, 9, 13)
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void Visibility_StartsHidden()
        {
            Assert.IsFalse(_barGo.activeSelf);
        }

        [Test]
        public void Visibility_ShowsOnRoundStarted()
        {
            EventBus.Publish(new RoundStartedEvent { RoundNumber = 1 });

            Assert.IsTrue(_barGo.activeSelf);
        }

        [Test]
        public void Visibility_HidesOnTradingPhaseEnded()
        {
            EventBus.Publish(new RoundStartedEvent { RoundNumber = 1 });
            Assert.IsTrue(_barGo.activeSelf);

            EventBus.Publish(new TradingPhaseEndedEvent { RoundNumber = 1 });
            Assert.IsFalse(_barGo.activeSelf);
        }

        [Test]
        public void Visibility_HidesOnMarketClosed()
        {
            EventBus.Publish(new RoundStartedEvent { RoundNumber = 1 });
            Assert.IsTrue(_barGo.activeSelf);

            EventBus.Publish(new MarketClosedEvent { RoundNumber = 1 });
            Assert.IsFalse(_barGo.activeSelf);
        }

        [Test]
        public void Visibility_HidesOnReturnToMenu()
        {
            EventBus.Publish(new RoundStartedEvent { RoundNumber = 1 });
            Assert.IsTrue(_barGo.activeSelf);

            EventBus.Publish(new ReturnToMenuEvent());
            Assert.IsFalse(_barGo.activeSelf);
        }

        // ═══════════════════════════════════════════════════════════════
        // Event Refresh Tests (AC 4)
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void Refresh_OnShopItemPurchased_UpdatesIcons()
        {
            _relicBar.RefreshRelicIcons();
            Assert.AreEqual(0, _relicBar.IconCount);

            _ctx.RelicManager.AddRelic("relic_double_dealer");
            EventBus.Publish(new ShopItemPurchasedEvent { ItemId = "relic_double_dealer" });

            Assert.AreEqual(1, _relicBar.IconCount);
        }

        [Test]
        public void Refresh_OnShopItemSold_UpdatesIcons()
        {
            _ctx.RelicManager.AddRelic("relic_double_dealer");
            _ctx.RelicManager.AddRelic("relic_quick_draw");
            _relicBar.RefreshRelicIcons();
            Assert.AreEqual(2, _relicBar.IconCount);

            _ctx.RelicManager.RemoveRelic("relic_double_dealer");
            EventBus.Publish(new ShopItemSoldEvent { RelicId = "relic_double_dealer" });

            Assert.AreEqual(1, _relicBar.IconCount);
        }

        [Test]
        public void Refresh_OnRoundStarted_RefreshesIcons()
        {
            _ctx.RelicManager.AddRelic("relic_double_dealer");
            EventBus.Publish(new RoundStartedEvent { RoundNumber = 1 });

            Assert.AreEqual(1, _relicBar.IconCount);
        }

        [Test]
        public void Refresh_OnNonRelicPurchase_DoesNotRefresh()
        {
            // H3 fix: non-relic purchases should not trigger icon rebuild
            _relicBar.RefreshRelicIcons();
            Assert.AreEqual(0, _relicBar.IconCount);

            // Purchase a non-relic item — icons should not rebuild
            EventBus.Publish(new ShopItemPurchasedEvent { ItemId = "tool_chart_analyzer" });
            Assert.AreEqual(0, _relicBar.IconCount);
        }

        // ═══════════════════════════════════════════════════════════════
        // Icon Character Tests
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void GetRelicIconChar_ReturnsFirstTwoChars()
        {
            Assert.AreEqual("DO", RelicBar.GetRelicIconChar("Double Dealer"));
        }

        [Test]
        public void GetRelicIconChar_ShortName_ReturnsFullName()
        {
            Assert.AreEqual("X", RelicBar.GetRelicIconChar("X"));
        }

        [Test]
        public void GetRelicIconChar_EmptyName_ReturnsQuestionMark()
        {
            Assert.AreEqual("?", RelicBar.GetRelicIconChar(""));
        }

        [Test]
        public void GetRelicIconChar_NullName_ReturnsQuestionMark()
        {
            Assert.AreEqual("?", RelicBar.GetRelicIconChar(null));
        }
    }
}
