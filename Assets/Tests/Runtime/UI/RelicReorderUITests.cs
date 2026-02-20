using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BullRun.Tests.UI
{
    /// <summary>
    /// Story 17.9: Tests for ShopUI relic reorder state — selection, cancellation,
    /// reorder execution, and visual state reset via public accessors.
    /// </summary>
    [TestFixture]
    public class RelicReorderUITests
    {
        private RunContext _ctx;
        private GameObject _rootGo;
        private ShopUI _shopUI;
        private ShopUI.OwnedRelicSlotView[] _ownedSlots;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            RelicFactory.ResetRegistry();

            _ctx = new RunContext(1, 1, new Portfolio(1000f));
            _ctx.Reputation.Add(100);

            // Register and add 3 test relics
            RelicFactory.Register("r1", () => new StubRelic("r1"));
            RelicFactory.Register("r2", () => new StubRelic("r2"));
            RelicFactory.Register("r3", () => new StubRelic("r3"));
            _ctx.RelicManager.AddRelic("r1");
            _ctx.RelicManager.AddRelic("r2");
            _ctx.RelicManager.AddRelic("r3");

            // Create minimal ShopUI
            _rootGo = new GameObject("TestShopRoot");
            var canvas = _rootGo.AddComponent<Canvas>();

            var shopGo = new GameObject("ShopUI");
            shopGo.transform.SetParent(_rootGo.transform, false);
            _shopUI = shopGo.AddComponent<ShopUI>();

            // Create minimal relic slots for Initialize
            var relicSlots = new ShopUI.RelicSlotView[3];
            for (int i = 0; i < 3; i++)
            {
                var slotGo = CreateSlotGo($"RelicSlot_{i}");
                relicSlots[i] = new ShopUI.RelicSlotView
                {
                    Root = slotGo,
                    NameText = slotGo.transform.Find("Name").GetComponent<Text>(),
                    DescriptionText = slotGo.transform.Find("Desc").GetComponent<Text>(),
                    CostText = slotGo.transform.Find("Cost").GetComponent<Text>(),
                    PurchaseButton = slotGo.GetComponent<Button>(),
                    ButtonText = slotGo.transform.Find("Feedback").GetComponent<Text>(),
                    CardBackground = slotGo.GetComponent<Image>(),
                    Group = slotGo.GetComponent<CanvasGroup>(),
                };
            }

            var headerGo = new GameObject("Header");
            headerGo.transform.SetParent(_rootGo.transform, false);
            var headerText = headerGo.AddComponent<Text>();
            headerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var repGo = new GameObject("Rep");
            repGo.transform.SetParent(_rootGo.transform, false);
            var repText = repGo.AddComponent<Text>();
            repText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var cashGo = new GameObject("Cash");
            cashGo.transform.SetParent(_rootGo.transform, false);
            var cashText = cashGo.AddComponent<Text>();
            cashText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasGroup = shopGo.AddComponent<CanvasGroup>();
            _shopUI.Initialize(_rootGo, repText, cashText, headerText, relicSlots, canvasGroup);

            // Create owned relic slots
            _ownedSlots = new ShopUI.OwnedRelicSlotView[5];
            for (int i = 0; i < 5; i++)
            {
                var slotGo = new GameObject($"OwnedSlot_{i}");
                slotGo.transform.SetParent(_rootGo.transform, false);
                slotGo.AddComponent<RectTransform>();
                var bg = slotGo.AddComponent<Image>();

                var nameGo = new GameObject("Name");
                nameGo.transform.SetParent(slotGo.transform, false);
                var nameText = nameGo.AddComponent<Text>();
                nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

                var sellGo = new GameObject("Sell");
                sellGo.transform.SetParent(slotGo.transform, false);
                var sellBtn = sellGo.AddComponent<Button>();
                var sellText = sellGo.AddComponent<Text>();
                sellText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

                var emptyGo = new GameObject("Empty");
                emptyGo.transform.SetParent(slotGo.transform, false);
                var emptyText = emptyGo.AddComponent<Text>();
                emptyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

                _ownedSlots[i] = new ShopUI.OwnedRelicSlotView
                {
                    Root = slotGo,
                    NameLabel = nameText,
                    SellButton = sellBtn,
                    SellButtonText = sellText,
                    EmptyLabel = emptyText,
                    Group = slotGo.AddComponent<CanvasGroup>(),
                    Background = bg,
                };
            }

            _shopUI.SetOwnedRelicSlots(_ownedSlots);

            // Show relics to set _ctx
            var offering = new RelicDef?[]
            {
                ShopItemDefinitions.RelicPool[0],
                ShopItemDefinitions.RelicPool[1],
                ShopItemDefinitions.RelicPool[2],
            };
            _shopUI.ShowRelics(_ctx, offering, (_) => { });
        }

        [TearDown]
        public void TearDown()
        {
            if (_rootGo != null)
                Object.DestroyImmediate(_rootGo);
            EventBus.Clear();
            RelicFactory.ResetRegistry();
        }

        private GameObject CreateSlotGo(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_rootGo.transform, false);
            go.AddComponent<RectTransform>();
            go.AddComponent<Image>();
            go.AddComponent<Button>();
            go.AddComponent<CanvasGroup>();

            foreach (var childName in new[] { "Name", "Desc", "Cost", "Feedback" })
            {
                var child = new GameObject(childName);
                child.transform.SetParent(go.transform, false);
                var text = child.AddComponent<Text>();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            return go;
        }

        // ════════════════════════════════════════════════════════════════════
        // Selection state (AC 1, 6)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void SelectRelicForReorder_EntersReorderMode()
        {
            _shopUI.SelectRelicForReorder(0);

            Assert.IsTrue(_shopUI.IsRelicReorderMode);
            Assert.AreEqual(0, _shopUI.SelectedRelicIndex);
        }

        [Test]
        public void SelectRelicForReorder_InvalidIndex_DoesNotEnterMode()
        {
            _shopUI.SelectRelicForReorder(-1);

            Assert.IsFalse(_shopUI.IsRelicReorderMode);
            Assert.AreEqual(-1, _shopUI.SelectedRelicIndex);
        }

        [Test]
        public void SelectRelicForReorder_BeyondOwnedCount_DoesNotEnterMode()
        {
            _shopUI.SelectRelicForReorder(5);

            Assert.IsFalse(_shopUI.IsRelicReorderMode);
            Assert.AreEqual(-1, _shopUI.SelectedRelicIndex);
        }

        [Test]
        public void SelectRelicForReorder_SameSlotAgain_CancelsSelection()
        {
            _shopUI.SelectRelicForReorder(1);
            Assert.IsTrue(_shopUI.IsRelicReorderMode);

            _shopUI.SelectRelicForReorder(1);
            Assert.IsFalse(_shopUI.IsRelicReorderMode);
            Assert.AreEqual(-1, _shopUI.SelectedRelicIndex);
        }

        // ════════════════════════════════════════════════════════════════════
        // Cancel (AC 6)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void CancelRelicSelection_ClearsState()
        {
            _shopUI.SelectRelicForReorder(0);
            Assert.IsTrue(_shopUI.IsRelicReorderMode);

            _shopUI.CancelRelicSelection();

            Assert.IsFalse(_shopUI.IsRelicReorderMode);
            Assert.AreEqual(-1, _shopUI.SelectedRelicIndex);
        }

        [Test]
        public void CancelRelicSelection_WhenNotInMode_DoesNothing()
        {
            Assert.IsFalse(_shopUI.IsRelicReorderMode);
            _shopUI.CancelRelicSelection();
            Assert.IsFalse(_shopUI.IsRelicReorderMode);
            Assert.AreEqual(-1, _shopUI.SelectedRelicIndex);
        }

        // ════════════════════════════════════════════════════════════════════
        // Perform reorder (AC 3, 4, 5)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void PerformRelicReorder_ClearsSelectionState()
        {
            _shopUI.SelectRelicForReorder(0);
            Assert.IsTrue(_shopUI.IsRelicReorderMode);

            _shopUI.SelectRelicForReorder(2); // triggers PerformRelicReorder

            Assert.IsFalse(_shopUI.IsRelicReorderMode);
            Assert.AreEqual(-1, _shopUI.SelectedRelicIndex);
        }

        [Test]
        public void PerformRelicReorder_UpdatesRelicManagerOrder()
        {
            // Initial order: r1, r2, r3
            _shopUI.SelectRelicForReorder(0);
            _shopUI.SelectRelicForReorder(2); // Move r1 to position 2

            Assert.AreEqual("r2", _ctx.RelicManager.OrderedRelics[0].Id);
            Assert.AreEqual("r3", _ctx.RelicManager.OrderedRelics[1].Id);
            Assert.AreEqual("r1", _ctx.RelicManager.OrderedRelics[2].Id);
        }

        [Test]
        public void PerformRelicReorder_SyncsOwnedRelics()
        {
            _shopUI.SelectRelicForReorder(2);
            _shopUI.SelectRelicForReorder(0); // Move r3 to position 0

            Assert.AreEqual("r3", _ctx.OwnedRelics[0]);
            Assert.AreEqual("r1", _ctx.OwnedRelics[1]);
            Assert.AreEqual("r2", _ctx.OwnedRelics[2]);
        }

        [Test]
        public void PerformRelicReorder_WhenNotInMode_DoesNothing()
        {
            Assert.IsFalse(_shopUI.IsRelicReorderMode);
            _shopUI.PerformRelicReorder(1);

            // Order unchanged
            Assert.AreEqual("r1", _ctx.OwnedRelics[0]);
            Assert.AreEqual("r2", _ctx.OwnedRelics[1]);
            Assert.AreEqual("r3", _ctx.OwnedRelics[2]);
        }

        // ════════════════════════════════════════════════════════════════════
        // Visual state (AC 1, 2)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void SelectRelicForReorder_HighlightsSelectedSlot()
        {
            _shopUI.SelectRelicForReorder(1);

            Assert.AreEqual(ShopUI.SelectedRelicBorderColor, _ownedSlots[1].Background.color);
        }

        [Test]
        public void CancelRelicSelection_RestoresSlotColor()
        {
            _shopUI.SelectRelicForReorder(1);
            _shopUI.CancelRelicSelection();

            Assert.AreEqual(ShopUI.OwnedRelicSlotColor, _ownedSlots[1].Background.color);
        }

        [Test]
        public void SelectRelicForReorder_ScalesSelectedSlot()
        {
            _shopUI.SelectRelicForReorder(0);

            float expectedScale = ShopUI.SelectedRelicScale;
            Assert.AreEqual(expectedScale, _ownedSlots[0].Root.transform.localScale.x, 0.01f);
            Assert.AreEqual(expectedScale, _ownedSlots[0].Root.transform.localScale.y, 0.01f);
        }

        [Test]
        public void CancelRelicSelection_RestoresSlotScale()
        {
            _shopUI.SelectRelicForReorder(0);
            _shopUI.CancelRelicSelection();

            Assert.AreEqual(1f, _ownedSlots[0].Root.transform.localScale.x, 0.01f);
            Assert.AreEqual(1f, _ownedSlots[0].Root.transform.localScale.y, 0.01f);
        }
    }
}
