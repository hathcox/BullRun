using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace BullRun.Tests.Items
{
    [TestFixture]
    public class RelicIconTests
    {
        // ═══════════════════════════════════════════════════════════════
        // RelicDef Icon Data Validation (AC 1, 2)
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void AllRelics_HaveNonEmptyIconChar()
        {
            var pool = ShopItemDefinitions.RelicPool;
            for (int i = 0; i < pool.Length; i++)
            {
                Assert.IsFalse(string.IsNullOrEmpty(pool[i].IconChar),
                    $"Relic '{pool[i].Name}' (index {i}) has null/empty IconChar");
            }
        }

        [Test]
        public void AllRelics_HaveValidIconColorHex()
        {
            var pool = ShopItemDefinitions.RelicPool;
            for (int i = 0; i < pool.Length; i++)
            {
                Assert.IsFalse(string.IsNullOrEmpty(pool[i].IconColorHex),
                    $"Relic '{pool[i].Name}' (index {i}) has null/empty IconColorHex");

                bool parsed = ColorUtility.TryParseHtmlString(pool[i].IconColorHex, out _);
                Assert.IsTrue(parsed,
                    $"Relic '{pool[i].Name}' has unparseable IconColorHex: '{pool[i].IconColorHex}'");
            }
        }

        [Test]
        public void AllRelics_HaveUniqueIconChar()
        {
            var pool = ShopItemDefinitions.RelicPool;
            var seen = new Dictionary<string, string>();
            for (int i = 0; i < pool.Length; i++)
            {
                if (seen.TryGetValue(pool[i].IconChar, out var existingName))
                {
                    Assert.Fail($"Duplicate IconChar '{pool[i].IconChar}' shared by " +
                        $"'{existingName}' and '{pool[i].Name}'");
                }
                seen[pool[i].IconChar] = pool[i].Name;
            }
        }

        [Test]
        public void RelicPool_Has23Relics()
        {
            Assert.AreEqual(23, ShopItemDefinitions.RelicPool.Length);
        }

        // ═══════════════════════════════════════════════════════════════
        // Color Category Consistency (AC 5)
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void TradeRelics_AllUseGreenHex()
        {
            string expectedHex = "#00FF41";
            var tradeRelicIds = new[]
            {
                "relic_short_multiplier",  // Bear Raid
                "relic_market_manipulator", // Market Manipulator
                "relic_double_dealer",     // Double Dealer
                "relic_quick_draw",        // Quick Draw
                "relic_skimmer",           // Skimmer
                "relic_short_profiteer",   // Short Profiteer
            };

            foreach (var id in tradeRelicIds)
            {
                var def = FindRelicById(id);
                Assert.IsNotNull(def, $"Trade relic '{id}' not found in pool");
                Assert.AreEqual(expectedHex, def.Value.IconColorHex,
                    $"Trade relic '{def.Value.Name}' should use green hex {expectedHex}");
            }
        }

        [Test]
        public void EventRelics_AllUseAmberHex()
        {
            string expectedHex = "#FFB000";
            var eventRelicIds = new[]
            {
                "relic_event_trigger",   // Catalyst Trader
                "relic_event_storm",     // Event Storm
                "relic_loss_liquidator", // Loss Liquidator
                "relic_profit_refresh",  // Profit Refresh
                "relic_bull_believer",   // Bull Believer
            };

            foreach (var id in eventRelicIds)
            {
                var def = FindRelicById(id);
                Assert.IsNotNull(def, $"Event relic '{id}' not found in pool");
                Assert.AreEqual(expectedHex, def.Value.IconColorHex,
                    $"Event relic '{def.Value.Name}' should use amber hex {expectedHex}");
            }
        }

        [Test]
        public void EconomyRelics_AllUseGoldHex()
        {
            string expectedHex = "#FFD700";
            var economyRelicIds = new[]
            {
                "relic_rep_doubler",    // Rep Doubler
                "relic_fail_forward",   // Fail Forward
                "relic_bond_bonus",     // Bond Bonus
                "relic_compound_rep",   // Compound Rep
                "relic_rep_interest",   // Rep Interest
                "relic_rep_dividend",   // Rep Dividend
            };

            foreach (var id in economyRelicIds)
            {
                var def = FindRelicById(id);
                Assert.IsNotNull(def, $"Economy relic '{id}' not found in pool");
                Assert.AreEqual(expectedHex, def.Value.IconColorHex,
                    $"Economy relic '{def.Value.Name}' should use gold hex {expectedHex}");
            }
        }

        [Test]
        public void MechanicRelics_AllUseCyanHex()
        {
            string expectedHex = "#00FFFF";
            var mechanicRelicIds = new[]
            {
                "relic_time_buyer",       // Time Buyer
                "relic_diamond_hands",    // Diamond Hands
                "relic_free_intel",       // Free Intel
                "relic_extra_expansion",  // Extra Expansion
            };

            foreach (var id in mechanicRelicIds)
            {
                var def = FindRelicById(id);
                Assert.IsNotNull(def, $"Mechanic relic '{id}' not found in pool");
                Assert.AreEqual(expectedHex, def.Value.IconColorHex,
                    $"Mechanic relic '{def.Value.Name}' should use cyan hex {expectedHex}");
            }
        }

        [Test]
        public void SpecialRelics_AllUseMagentaHex()
        {
            string expectedHex = "#FF00FF";
            var specialRelicIds = new[]
            {
                "relic_relic_expansion", // Relic Expansion
                "relic_event_catalyst",  // Event Catalyst
            };

            foreach (var id in specialRelicIds)
            {
                var def = FindRelicById(id);
                Assert.IsNotNull(def, $"Special relic '{id}' not found in pool");
                Assert.AreEqual(expectedHex, def.Value.IconColorHex,
                    $"Special relic '{def.Value.Name}' should use magenta hex {expectedHex}");
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // RelicIconHelper Tests (AC 7)
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void ParseHexColor_ValidGreen_ReturnsCorrectColor()
        {
            var color = RelicIconHelper.ParseHexColor("#00FF41");
            Assert.AreEqual(0f, color.r, 0.01f);
            Assert.AreEqual(1f, color.g, 0.01f);
            Assert.AreEqual(0.255f, color.b, 0.01f);
        }

        [Test]
        public void ParseHexColor_ValidAmber_ReturnsCorrectColor()
        {
            var color = RelicIconHelper.ParseHexColor("#FFB000");
            Assert.AreEqual(1f, color.r, 0.01f);
            Assert.AreEqual(0.69f, color.g, 0.01f);
            Assert.AreEqual(0f, color.b, 0.01f);
        }

        [Test]
        public void ParseHexColor_InvalidHex_ReturnsWhite()
        {
            var color = RelicIconHelper.ParseHexColor("not-a-color");
            Assert.AreEqual(Color.white, color);
        }

        [Test]
        public void ParseHexColor_EmptyString_ReturnsWhite()
        {
            var color = RelicIconHelper.ParseHexColor("");
            Assert.AreEqual(Color.white, color);
        }

        [Test]
        public void ParseHexColor_Null_ReturnsWhite()
        {
            var color = RelicIconHelper.ParseHexColor(null);
            Assert.AreEqual(Color.white, color);
        }

        [Test]
        public void GetIconColor_ValidDef_ReturnsCorrectColor()
        {
            var def = new RelicDef("test", "Test", "desc", "", 10, "T", "#FF0000");
            var color = RelicIconHelper.GetIconColor(def);
            Assert.AreEqual(1f, color.r, 0.01f);
            Assert.AreEqual(0f, color.g, 0.01f);
            Assert.AreEqual(0f, color.b, 0.01f);
        }

        [Test]
        public void GetIconColor_EmptyHex_ReturnsWhite()
        {
            var def = new RelicDef("test", "Test", "desc", "", 10, "T", "");
            var color = RelicIconHelper.GetIconColor(def);
            Assert.AreEqual(Color.white, color);
        }

        // ═══════════════════════════════════════════════════════════════
        // Helper
        // ═══════════════════════════════════════════════════════════════

        private static RelicDef? FindRelicById(string id)
        {
            var pool = ShopItemDefinitions.RelicPool;
            for (int i = 0; i < pool.Length; i++)
            {
                if (pool[i].Id == id) return pool[i];
            }
            return null;
        }
    }
}
