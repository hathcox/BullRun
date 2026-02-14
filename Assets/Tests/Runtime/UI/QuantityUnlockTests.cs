using NUnit.Framework;
using UnityEngine;

namespace BullRun.Tests.UI
{
    /// <summary>
    /// FIX-13: Tests for quantity tier unlock system.
    /// Covers: default x1, tier unlock progression, preset selection gating,
    /// round reset, MAX clamp, unlock persistence, keyboard gating, short independence.
    /// </summary>
    [TestFixture]
    public class QuantityUnlockTests
    {
        private GameObject _go;
        private QuantitySelector _qs;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            _go = new GameObject("TestQS");
            _qs = _go.AddComponent<QuantitySelector>();
            _qs.Initialize(null);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
            if (_go != null) Object.DestroyImmediate(_go);
        }

        // --- AC 1: Default quantity is 1 at run start ---

        [Test]
        public void DefaultQuantity_Is1_AtStart()
        {
            Assert.AreEqual(1, _qs.SelectedQuantity);
        }

        [Test]
        public void DefaultQuantity_GameConfig_Is1()
        {
            Assert.AreEqual(1, GameConfig.DefaultTradeQuantity);
        }

        [Test]
        public void UnlockedTierIndex_Is0_AtStart()
        {
            Assert.AreEqual(0, _qs.UnlockedTierIndex);
        }

        [Test]
        public void GetUnlockedPresets_Empty_AtStart()
        {
            var presets = _qs.GetUnlockedPresets();
            Assert.AreEqual(0, presets.Length);
        }

        // --- AC 2: Quantity tier definitions in GameConfig ---

        [Test]
        public void QuantityTiers_Has5Tiers()
        {
            Assert.AreEqual(5, GameConfig.QuantityTiers.Length);
        }

        [Test]
        public void QuantityTiers_Tier0_Is1_Free()
        {
            Assert.AreEqual(1, GameConfig.QuantityTiers[0].Value);
            Assert.AreEqual(0, GameConfig.QuantityTiers[0].RepCost);
        }

        [Test]
        public void QuantityTiers_Tier1_Is5()
        {
            Assert.AreEqual(5, GameConfig.QuantityTiers[1].Value);
            Assert.Greater(GameConfig.QuantityTiers[1].RepCost, 0);
        }

        [Test]
        public void QuantityTiers_Tier2_Is10()
        {
            Assert.AreEqual(10, GameConfig.QuantityTiers[2].Value);
        }

        [Test]
        public void QuantityTiers_Tier3_Is15()
        {
            Assert.AreEqual(15, GameConfig.QuantityTiers[3].Value);
        }

        [Test]
        public void QuantityTiers_Tier4_Is25()
        {
            Assert.AreEqual(25, GameConfig.QuantityTiers[4].Value);
        }

        [Test]
        public void QuantityTiers_CostsIncreaseWithTier()
        {
            for (int i = 1; i < GameConfig.QuantityTiers.Length; i++)
            {
                Assert.GreaterOrEqual(GameConfig.QuantityTiers[i].RepCost,
                    GameConfig.QuantityTiers[i - 1].RepCost,
                    $"Tier {i} cost should be >= tier {i - 1} cost");
            }
        }

        // --- UnlockTier progression ---

        [Test]
        public void UnlockTier1_MakesX5Available()
        {
            bool result = _qs.UnlockTier(1);
            Assert.IsTrue(result);
            Assert.AreEqual(1, _qs.UnlockedTierIndex);
            Assert.AreEqual(5, _qs.HighestUnlockedQuantity);
        }

        [Test]
        public void UnlockTier_Progressive_X5_X10_X15_X25()
        {
            Assert.IsTrue(_qs.UnlockTier(1));
            Assert.AreEqual(5, _qs.HighestUnlockedQuantity);

            Assert.IsTrue(_qs.UnlockTier(2));
            Assert.AreEqual(10, _qs.HighestUnlockedQuantity);

            Assert.IsTrue(_qs.UnlockTier(3));
            Assert.AreEqual(15, _qs.HighestUnlockedQuantity);

            Assert.IsTrue(_qs.UnlockTier(4));
            Assert.AreEqual(25, _qs.HighestUnlockedQuantity);
        }

        [Test]
        public void UnlockTier_Sequential_CannotSkip()
        {
            // Can't skip directly to tier 2
            bool result = _qs.UnlockTier(2);
            Assert.IsFalse(result);
            Assert.AreEqual(0, _qs.UnlockedTierIndex);
        }

        [Test]
        public void UnlockTier_AlreadyUnlocked_ReturnsFalse()
        {
            _qs.UnlockTier(1);
            bool result = _qs.UnlockTier(1);
            Assert.IsFalse(result);
        }

        [Test]
        public void UnlockTier_BeyondMax_ReturnsFalse()
        {
            _qs.UnlockTier(1);
            _qs.UnlockTier(2);
            _qs.UnlockTier(3);
            _qs.UnlockTier(4);
            bool result = _qs.UnlockTier(5);
            Assert.IsFalse(result);
        }

        [Test]
        public void UnlockNextTier_SequentialUnlocks()
        {
            Assert.IsTrue(_qs.UnlockNextTier()); // tier 1
            Assert.AreEqual(1, _qs.UnlockedTierIndex);
            Assert.IsTrue(_qs.UnlockNextTier()); // tier 2
            Assert.AreEqual(2, _qs.UnlockedTierIndex);
        }

        [Test]
        public void UnlockNextTier_AllUnlocked_ReturnsFalse()
        {
            _qs.UnlockTier(1);
            _qs.UnlockTier(2);
            _qs.UnlockTier(3);
            _qs.UnlockTier(4);
            Assert.IsFalse(_qs.UnlockNextTier());
        }

        // --- AC 6: SelectPreset rejected for locked tiers ---

        [Test]
        public void SelectPresetByTier_LockedTier_ReturnsFalse()
        {
            bool result = _qs.SelectPresetByTier(1);
            Assert.IsFalse(result);
            Assert.AreEqual(1, _qs.SelectedQuantity); // Still at x1
        }

        [Test]
        public void SelectPresetByTier_UnlockedTier_ReturnsTrue()
        {
            _qs.UnlockTier(1);
            bool result = _qs.SelectPresetByTier(1);
            Assert.IsTrue(result);
            Assert.AreEqual(5, _qs.SelectedQuantity);
        }

        [Test]
        public void SelectPresetByTier_HigherThanUnlocked_Rejected()
        {
            _qs.UnlockTier(1); // Only x5 unlocked
            bool result = _qs.SelectPresetByTier(2); // x10 not unlocked
            Assert.IsFalse(result);
            Assert.AreEqual(1, _qs.SelectedQuantity); // Unchanged from default
        }

        [Test]
        public void SelectPresetByTier_Tier0_Rejected()
        {
            // Tier 0 is the default (x1), not selectable as a preset
            bool result = _qs.SelectPresetByTier(0);
            Assert.IsFalse(result);
        }

        [Test]
        public void SelectPresetByTier_NegativeTier_Rejected()
        {
            bool result = _qs.SelectPresetByTier(-1);
            Assert.IsFalse(result);
        }

        // --- AC 7: ResetToDefault goes to x1 ---

        [Test]
        public void ResetToDefault_GoesToX1()
        {
            _qs.UnlockTier(1);
            _qs.SelectPresetByTier(1); // x5
            Assert.AreEqual(5, _qs.SelectedQuantity);

            _qs.ResetToDefault();
            Assert.AreEqual(1, _qs.SelectedQuantity);
        }

        [Test]
        public void ResetToDefault_GoesToX1_WithAllUnlocked()
        {
            _qs.UnlockTier(1);
            _qs.UnlockTier(2);
            _qs.UnlockTier(3);
            _qs.UnlockTier(4);
            _qs.SelectPresetByTier(4); // x25
            Assert.AreEqual(25, _qs.SelectedQuantity);

            _qs.ResetToDefault();
            Assert.AreEqual(1, _qs.SelectedQuantity);
        }

        [Test]
        public void RoundStartedEvent_ResetsToX1()
        {
            _qs.UnlockTier(1);
            _qs.SelectPresetByTier(1); // x5

            EventBus.Publish(new RoundStartedEvent
            {
                RoundNumber = 2, Act = 1,
                TierDisplayName = "Penny Stocks",
                MarginCallTarget = 200f, TimeLimit = 60f
            });

            Assert.AreEqual(1, _qs.SelectedQuantity);
        }

        // --- AC 8: GetCurrentQuantity clamped to highest unlocked ---

        [Test]
        public void GetCurrentQuantity_DefaultX1_Returns1()
        {
            var portfolio = new Portfolio(1000f);
            int qty = _qs.GetCurrentQuantity(true, false, "ACME", 25f, portfolio);
            Assert.AreEqual(1, qty);
        }

        [Test]
        public void GetCurrentQuantity_UnlockedX5_SelectedX5_Returns5()
        {
            _qs.UnlockTier(1);
            _qs.SelectPresetByTier(1);
            var portfolio = new Portfolio(1000f);
            int qty = _qs.GetCurrentQuantity(true, false, "ACME", 25f, portfolio);
            Assert.AreEqual(5, qty);
        }

        [Test]
        public void GetCurrentQuantity_ClampsToAffordable()
        {
            _qs.UnlockTier(1);
            _qs.SelectPresetByTier(1); // x5
            var portfolio = new Portfolio(75f); // Can afford floor(75/25) = 3 shares
            int qty = _qs.GetCurrentQuantity(true, false, "ACME", 25f, portfolio);
            Assert.AreEqual(3, qty);
        }

        [Test]
        public void GetCurrentQuantity_MaxRespectUnlockCap()
        {
            // Even if somehow selected quantity is higher, clamp to unlock cap
            _qs.UnlockTier(1); // x5 is max
            _qs.SelectPresetByTier(1); // x5
            var portfolio = new Portfolio(10000f); // Can afford much more
            int qty = _qs.GetCurrentQuantity(true, false, "ACME", 1f, portfolio);
            Assert.AreEqual(5, qty);
        }

        // --- AC 4: Unlocks persist across rounds ---

        [Test]
        public void UnlocksPersistAcrossRounds()
        {
            _qs.UnlockTier(1);
            _qs.UnlockTier(2);

            // Simulate round start (resets quantity to x1, but unlocks stay)
            EventBus.Publish(new RoundStartedEvent
            {
                RoundNumber = 3, Act = 2,
                TierDisplayName = "Low-Value Stocks",
                MarginCallTarget = 500f, TimeLimit = 60f
            });

            // Unlocks should still be there
            Assert.AreEqual(2, _qs.UnlockedTierIndex);
            Assert.AreEqual(10, _qs.HighestUnlockedQuantity);

            // Can still select unlocked presets after round start
            bool result = _qs.SelectPresetByTier(2);
            Assert.IsTrue(result);
            Assert.AreEqual(10, _qs.SelectedQuantity);
        }

        // --- AC 5: Keyboard shortcut ignored for locked tier ---

        [Test]
        public void IsTierUnlocked_Tier1_FalseAtStart()
        {
            Assert.IsFalse(_qs.IsTierUnlocked(1));
        }

        [Test]
        public void IsTierUnlocked_Tier1_TrueAfterUnlock()
        {
            _qs.UnlockTier(1);
            Assert.IsTrue(_qs.IsTierUnlocked(1));
        }

        [Test]
        public void IsTierUnlocked_Tier2_FalseWithOnlyTier1()
        {
            _qs.UnlockTier(1);
            Assert.IsFalse(_qs.IsTierUnlocked(2));
        }

        [Test]
        public void IsTierUnlocked_Tier0_AlwaysTrue()
        {
            Assert.IsTrue(_qs.IsTierUnlocked(0));
        }

        // --- AC 9: Short trade quantity unaffected by QuantitySelector unlocks ---

        [Test]
        public void ShortBaseShares_AlwaysOne()
        {
            Assert.AreEqual(1, GameConfig.ShortBaseShares);
        }

        [Test]
        public void ShortBaseShares_UnchangedByQuantityUnlocks()
        {
            _qs.UnlockTier(1);
            _qs.UnlockTier(2);
            _qs.UnlockTier(3);
            _qs.UnlockTier(4);

            // ShortBaseShares is a GameConfig constant, not affected by QuantitySelector state
            Assert.AreEqual(1, GameConfig.ShortBaseShares);
        }

        // --- GetUnlockedPresets ---

        [Test]
        public void GetUnlockedPresets_AfterTier1_ContainsX5()
        {
            _qs.UnlockTier(1);
            var presets = _qs.GetUnlockedPresets();
            Assert.AreEqual(1, presets.Length);
            Assert.AreEqual(5, presets[0]);
        }

        [Test]
        public void GetUnlockedPresets_AfterTier3_ContainsX5X10X15()
        {
            _qs.UnlockTier(1);
            _qs.UnlockTier(2);
            _qs.UnlockTier(3);
            var presets = _qs.GetUnlockedPresets();
            Assert.AreEqual(3, presets.Length);
            Assert.AreEqual(5, presets[0]);
            Assert.AreEqual(10, presets[1]);
            Assert.AreEqual(15, presets[2]);
        }

        // --- QuantityTierUnlockedEvent integration ---

        [Test]
        public void QuantityTierUnlockedEvent_TriggersUnlock()
        {
            EventBus.Publish(new QuantityTierUnlockedEvent
            {
                TierIndex = 1,
                TierValue = 5,
                RepCost = 10
            });

            Assert.AreEqual(1, _qs.UnlockedTierIndex);
            Assert.AreEqual(5, _qs.HighestUnlockedQuantity);
        }

        // --- OnTierUnlocked callback ---

        [Test]
        public void OnTierUnlocked_CallbackFired()
        {
            int callbackTier = -1;
            _qs.OnTierUnlocked = (tier) => callbackTier = tier;

            _qs.UnlockTier(1);
            Assert.AreEqual(1, callbackTier);
        }

        // --- RunContext.UnlockedQuantityTier ---

        [Test]
        public void RunContext_UnlockedQuantityTier_StartsAt0()
        {
            var ctx = new RunContext(1, 1, new Portfolio(1000f));
            Assert.AreEqual(0, ctx.UnlockedQuantityTier);
        }

        [Test]
        public void RunContext_ResetForNewRun_ResetsUnlockedQuantityTier()
        {
            var ctx = new RunContext(1, 1, new Portfolio(1000f));
            ctx.UnlockedQuantityTier = 3;
            ctx.ResetForNewRun();
            Assert.AreEqual(0, ctx.UnlockedQuantityTier);
        }

        // --- QuantityTier struct ---

        [Test]
        public void QuantityTier_Struct_StoresValues()
        {
            var tier = new QuantityTier(5, 10);
            Assert.AreEqual(5, tier.Value);
            Assert.AreEqual(10, tier.RepCost);
        }

        // --- Partial fill with unlock system ---

        [Test]
        public void GetCurrentQuantity_SellPartialFill()
        {
            _qs.UnlockTier(1);
            _qs.SelectPresetByTier(1); // x5
            var portfolio = new Portfolio(1000f);
            portfolio.OpenPosition("ACME", 3, 25f); // Only 3 shares
            int qty = _qs.GetCurrentQuantity(false, false, "ACME", 25f, portfolio);
            Assert.AreEqual(3, qty); // Clamped to 3 (only 3 held)
        }

        [Test]
        public void GetCurrentQuantity_X1Default_BuyPartialFill()
        {
            // At x1 default, even if can't afford 1 share, returns 0
            var portfolio = new Portfolio(0f);
            int qty = _qs.GetCurrentQuantity(true, false, "ACME", 25f, portfolio);
            Assert.AreEqual(0, qty);
        }
    }
}
