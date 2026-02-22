using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace BullRun.Tests.Shop
{
    /// <summary>
    /// Tests for store visual polish and card animations (Story 13.8).
    /// Covers: animation constants, color definitions, view struct fields.
    /// Note: Coroutine-based animations require Play Mode and are verified manually.
    /// </summary>
    [TestFixture]
    public class StoreVisualPolishTests
    {
        // === Task 1: Relic Card Hover Effects (AC 1) ===

        [Test]
        public void RelicCardColor_MatchesExpectedDarkBlue()
        {
            // RelicCardColor = ColorPalette.WithAlpha(ColorPalette.Dimmed(ColorPalette.Panel, 1.2f), 0.9f)
            var expected = ColorPalette.WithAlpha(ColorPalette.Dimmed(ColorPalette.Panel, 1.2f), 0.9f);
            Assert.AreEqual(expected.r, ShopUI.RelicCardColor.r, 0.01f);
            Assert.AreEqual(expected.g, ShopUI.RelicCardColor.g, 0.01f);
            Assert.AreEqual(expected.b, ShopUI.RelicCardColor.b, 0.01f);
            Assert.AreEqual(expected.a, ShopUI.RelicCardColor.a, 0.01f);
        }

        [Test]
        public void RelicCardHoverColor_IsBrighterThanBase()
        {
            var baseColor = ShopUI.RelicCardColor;
            var hoverColor = ShopUI.RelicCardHoverColor;
            Assert.Greater(hoverColor.r, baseColor.r, "Hover red should be brighter");
            Assert.Greater(hoverColor.g, baseColor.g, "Hover green should be brighter");
            Assert.Greater(hoverColor.b, baseColor.b, "Hover blue should be brighter");
        }

        [Test]
        public void HoverScale_IsSlightlyAboveOne()
        {
            Assert.AreEqual(1.05f, ShopUI.HoverScale, 0.001f);
        }

        [Test]
        public void HoverDuration_IsShort()
        {
            Assert.AreEqual(0.15f, ShopUI.HoverDuration, 0.01f,
                "Hover transition should be ~0.15s for smooth feel");
        }

        // === Task 2: Purchase Animation (AC 2) ===

        [Test]
        public void PurchaseAnimDuration_IsHalfSecond()
        {
            Assert.AreEqual(0.5f, ShopUI.PurchaseAnimDuration, 0.01f);
        }

        [Test]
        public void SoldStampDuration_IsOneSecond()
        {
            Assert.AreEqual(1.0f, ShopUI.SoldStampDuration, 0.01f);
        }

        // === Task 3: Reroll Animation (AC 3) ===

        [Test]
        public void RerollFlipDuration_IsCorrect()
        {
            Assert.AreEqual(0.4f, ShopUI.RerollFlipDuration, 0.01f,
                "Total reroll flip duration should be ~0.4s");
        }

        [Test]
        public void RerollStaggerDelay_IsPositive()
        {
            Assert.Greater(ShopUI.RerollStaggerDelay, 0f,
                "Stagger delay should be positive for visual interest");
            Assert.Less(ShopUI.RerollStaggerDelay, ShopUI.RerollFlipDuration,
                "Stagger delay should be less than flip duration");
        }

        // === Task 4: Insider Tip Flip Animation (AC 4) ===

        [Test]
        public void TipFlipDuration_IsCorrect()
        {
            Assert.AreEqual(0.6f, ShopUI.TipFlipDuration, 0.01f,
                "Tip flip animation should be ~0.6s");
        }

        [Test]
        public void TipFlashDuration_IsShort()
        {
            Assert.Greater(ShopUI.TipFlashDuration, 0f);
            Assert.Less(ShopUI.TipFlashDuration, 0.5f,
                "Flash effect should be brief");
        }

        // === Task 5: Bond Card Pulsing Glow (AC 5) ===

        [Test]
        public void BondPulseSpeed_IsPositive()
        {
            Assert.Greater(ShopUI.BondPulseSpeed, 0f,
                "Pulse speed should be positive for visible animation");
        }

        [Test]
        public void BondPulseAlphaRange_IsValid()
        {
            Assert.Greater(ShopUI.BondPulseMaxAlpha, ShopUI.BondPulseMinAlpha,
                "Max alpha should exceed min for visible pulsing");
            Assert.GreaterOrEqual(ShopUI.BondPulseMinAlpha, 0f);
            Assert.LessOrEqual(ShopUI.BondPulseMaxAlpha, 1f);
        }

        [Test]
        public void BondPulseHoverBoost_IsPositive()
        {
            Assert.Greater(ShopUI.BondPulseHoverBoost, 0f,
                "Hover should increase glow intensity");
        }

        // === Task 6: Expansion OWNED Watermark (AC 6) ===

        [Test]
        public void OwnedFadeDuration_IsCorrect()
        {
            Assert.AreEqual(0.3f, ShopUI.OwnedFadeDuration, 0.01f,
                "OWNED watermark fade should be ~0.3s");
        }

        // === Task 7: Text Legibility (AC 7) ===

        [Test]
        public void TipCardFaceDownColor_IsDark()
        {
            // CyanDim-based: dark teal tint, channels must stay dark for text legibility
            var color = ShopUI.TipCardFaceDownColor;
            Assert.Less(color.r, 0.2f, "Red channel should be very dark");
            Assert.Less(color.g, 0.45f, "Green channel should be dark (cyan tint allowed)");
            Assert.Less(color.b, 0.45f, "Blue channel should be dark (cyan tint allowed)");
        }

        [Test]
        public void TipCardRevealedColor_IsDark()
        {
            // Dimmed Panel-based: slightly brighter than Panel, still dark for legibility
            var color = ShopUI.TipCardRevealedColor;
            Assert.Less(color.r, 0.2f, "Red channel should be very dark");
            Assert.Less(color.g, 0.3f, "Green channel should be dark");
        }

        [Test]
        public void BondCardColor_IsDarkGreen()
        {
            var color = ShopUI.BondCardColor;
            Assert.Greater(color.g, color.r, "Bond card should have green tint");
            Assert.Less(color.r, 0.2f, "Bond card should be dark");
        }

        // === Task 8: Visual Consistency (AC 8) ===

        [Test]
        public void ExpansionCardColor_IsDarkTeal()
        {
            var color = ShopUI.ExpansionCardColor;
            Assert.Less(color.r, 0.2f);
            Assert.Greater(color.b, color.r, "Expansion card should have teal tint");
        }

        [Test]
        public void OwnedOverlayColor_IsDark()
        {
            var color = ShopUI.OwnedOverlayColor;
            Assert.Less(color.r, 0.2f);
            Assert.Less(color.b, 0.2f);
        }

        // === RelicSlotView has CanvasGroup field ===

        [Test]
        public void RelicSlotView_HasGroupField()
        {
            var view = new ShopUI.RelicSlotView();
            Assert.IsNull(view.Group, "Group should be null by default (set during setup)");
        }

        // === TipCardView has CanvasGroup field ===

        [Test]
        public void TipCardView_HasGroupField()
        {
            var view = new ShopUI.TipCardView();
            Assert.IsNull(view.Group, "Group should be null by default (set during creation)");
        }

        // === FormatTipTypeName logic tests ===

        [Test]
        public void FormatTipTypeName_PriceForecast_ReturnsCorrectString()
        {
            Assert.AreEqual("PRICE FORECAST", ShopUI.FormatTipTypeName(InsiderTipType.PriceForecast));
        }

        [Test]
        public void FormatTipTypeName_PriceFloor_ReturnsCorrectString()
        {
            Assert.AreEqual("PRICE FLOOR", ShopUI.FormatTipTypeName(InsiderTipType.PriceFloor));
        }

        [Test]
        public void FormatTipTypeName_PriceCeiling_ReturnsCorrectString()
        {
            Assert.AreEqual("PRICE CEILING", ShopUI.FormatTipTypeName(InsiderTipType.PriceCeiling));
        }

        [Test]
        public void FormatTipTypeName_EventCount_ReturnsCorrectString()
        {
            Assert.AreEqual("EVENT COUNT", ShopUI.FormatTipTypeName(InsiderTipType.EventCount));
        }

        [Test]
        public void FormatTipTypeName_DipMarker_ReturnsCorrectString()
        {
            Assert.AreEqual("DIP MARKER", ShopUI.FormatTipTypeName(InsiderTipType.DipMarker));
        }

        [Test]
        public void FormatTipTypeName_PeakMarker_ReturnsCorrectString()
        {
            Assert.AreEqual("PEAK MARKER", ShopUI.FormatTipTypeName(InsiderTipType.PeakMarker));
        }

        [Test]
        public void FormatTipTypeName_ClosingDirection_ReturnsCorrectString()
        {
            Assert.AreEqual("CLOSING CALL", ShopUI.FormatTipTypeName(InsiderTipType.ClosingDirection));
        }

        [Test]
        public void FormatTipTypeName_EventTiming_ReturnsCorrectString()
        {
            Assert.AreEqual("EVENT TIMING", ShopUI.FormatTipTypeName(InsiderTipType.EventTiming));
        }

        [Test]
        public void FormatTipTypeName_TrendReversal_ReturnsCorrectString()
        {
            Assert.AreEqual("TREND REVERSAL", ShopUI.FormatTipTypeName(InsiderTipType.TrendReversal));
        }

        [Test]
        public void FormatTipTypeName_UnknownValue_ReturnsUnknown()
        {
            Assert.AreEqual("UNKNOWN", ShopUI.FormatTipTypeName((InsiderTipType)999));
        }

        // === TipCardView TypeBadgeText field test ===

        [Test]
        public void TipCardView_HasTypeBadgeTextField()
        {
            var view = new ShopUI.TipCardView();
            Assert.IsNull(view.TypeBadgeText, "TypeBadgeText should be null by default (set during creation)");
        }

        // === GetTipFaceDownHint logic tests ===

        [Test]
        public void GetTipFaceDownHint_PriceForecast_ReturnsEngagingHint()
        {
            Assert.AreEqual("What's the sweet spot?", ShopUI.GetTipFaceDownHint(InsiderTipType.PriceForecast));
        }

        [Test]
        public void GetTipFaceDownHint_PriceFloor_ReturnsEngagingHint()
        {
            Assert.AreEqual("How low can it go?", ShopUI.GetTipFaceDownHint(InsiderTipType.PriceFloor));
        }

        [Test]
        public void GetTipFaceDownHint_PriceCeiling_ReturnsEngagingHint()
        {
            Assert.AreEqual("What's the top?", ShopUI.GetTipFaceDownHint(InsiderTipType.PriceCeiling));
        }

        [Test]
        public void GetTipFaceDownHint_EventCount_ReturnsEngagingHint()
        {
            Assert.AreEqual("How many surprises?", ShopUI.GetTipFaceDownHint(InsiderTipType.EventCount));
        }

        [Test]
        public void GetTipFaceDownHint_DipMarker_ReturnsEngagingHint()
        {
            Assert.AreEqual("When's the best buy?", ShopUI.GetTipFaceDownHint(InsiderTipType.DipMarker));
        }

        [Test]
        public void GetTipFaceDownHint_PeakMarker_ReturnsEngagingHint()
        {
            Assert.AreEqual("When should you sell?", ShopUI.GetTipFaceDownHint(InsiderTipType.PeakMarker));
        }

        [Test]
        public void GetTipFaceDownHint_ClosingDirection_ReturnsEngagingHint()
        {
            Assert.AreEqual("Up or down?", ShopUI.GetTipFaceDownHint(InsiderTipType.ClosingDirection));
        }

        [Test]
        public void GetTipFaceDownHint_EventTiming_ReturnsEngagingHint()
        {
            Assert.AreEqual("When do shakeups hit?", ShopUI.GetTipFaceDownHint(InsiderTipType.EventTiming));
        }

        [Test]
        public void GetTipFaceDownHint_TrendReversal_ReturnsEngagingHint()
        {
            Assert.AreEqual("When does it turn?", ShopUI.GetTipFaceDownHint(InsiderTipType.TrendReversal));
        }

        [Test]
        public void GetTipFaceDownHint_UnknownValue_ReturnsDefault()
        {
            Assert.AreEqual("Unknown intel", ShopUI.GetTipFaceDownHint((InsiderTipType)999));
        }

        // === GetTipTypeBadge tests ===

        [Test]
        public void GetTipTypeBadge_ChartOverlayTypes_ReturnChartInCyan()
        {
            var chartTypes = new[] {
                InsiderTipType.PriceForecast, InsiderTipType.PriceFloor,
                InsiderTipType.PriceCeiling, InsiderTipType.DipMarker,
                InsiderTipType.PeakMarker, InsiderTipType.EventTiming,
                InsiderTipType.TrendReversal
            };
            foreach (var t in chartTypes)
            {
                var (label, color) = ShopUI.GetTipTypeBadge(t);
                Assert.AreEqual("[CHART]", label, $"Expected [CHART] for {t}");
                Assert.AreEqual(ColorPalette.Cyan, color, $"Expected Cyan for {t}");
            }
        }

        [Test]
        public void GetTipTypeBadge_EventCount_ReturnsLiveInGreen()
        {
            var (label, color) = ShopUI.GetTipTypeBadge(InsiderTipType.EventCount);
            Assert.AreEqual("[LIVE]", label);
            Assert.AreEqual(ColorPalette.Green, color);
        }

        [Test]
        public void GetTipTypeBadge_ClosingDirection_ReturnsCallInAmber()
        {
            var (label, color) = ShopUI.GetTipTypeBadge(InsiderTipType.ClosingDirection);
            Assert.AreEqual("[CALL]", label);
            Assert.AreEqual(ColorPalette.Amber, color);
        }

        [Test]
        public void GetTipTypeBadge_UnknownType_ReturnsEmpty()
        {
            var (label, _) = ShopUI.GetTipTypeBadge((InsiderTipType)999);
            Assert.AreEqual("", label);
        }

        // === No-duplicate validation tests ===

        [Test]
        public void FormatTipTypeName_AllNineTypes_NoDuplicateNames()
        {
            var names = new HashSet<string>();
            foreach (InsiderTipType t in System.Enum.GetValues(typeof(InsiderTipType)))
            {
                string name = ShopUI.FormatTipTypeName(t);
                Assert.IsTrue(names.Add(name), $"Duplicate display name: {name}");
            }
            Assert.AreEqual(9, names.Count, "Expected exactly 9 unique type names");
        }

        [Test]
        public void GetTipFaceDownHint_AllNineTypes_NoDuplicateHints()
        {
            var hints = new HashSet<string>();
            foreach (InsiderTipType t in System.Enum.GetValues(typeof(InsiderTipType)))
            {
                string hint = ShopUI.GetTipFaceDownHint(t);
                Assert.IsTrue(hints.Add(hint), $"Duplicate face-down hint: {hint}");
            }
            Assert.AreEqual(9, hints.Count, "Expected exactly 9 unique face-down hints");
        }

        // === InsiderTipDefinitions coverage tests ===

        [Test]
        public void InsiderTipDefinitions_AllArrayCoversEveryEnumValue()
        {
            var enumValues = System.Enum.GetValues(typeof(InsiderTipType));
            Assert.AreEqual(enumValues.Length, InsiderTipDefinitions.All.Length,
                "InsiderTipDefinitions.All must have one entry per InsiderTipType enum value");
        }

        [Test]
        public void InsiderTipDefinitions_AllTypesPresent()
        {
            var found = new HashSet<InsiderTipType>();
            foreach (var def in InsiderTipDefinitions.All)
                found.Add(def.Type);
            foreach (InsiderTipType t in System.Enum.GetValues(typeof(InsiderTipType)))
                Assert.IsTrue(found.Contains(t), $"Missing InsiderTipDefinitions entry for {t}");
        }

        [Test]
        public void InsiderTipDefinitions_ChartOverlayTemplatesContainChartReference()
        {
            // Story 18.6: Price tips now say "revealed on chart", others say "marked on chart"
            var chartTypes = new[] {
                InsiderTipType.PriceForecast, InsiderTipType.PriceFloor,
                InsiderTipType.PriceCeiling, InsiderTipType.DipMarker,
                InsiderTipType.PeakMarker, InsiderTipType.EventTiming,
                InsiderTipType.TrendReversal
            };
            foreach (var t in chartTypes)
            {
                var def = InsiderTipDefinitions.GetByType(t);
                Assert.IsNotNull(def, $"Missing definition for {t}");
                Assert.IsTrue(def.Value.DescriptionTemplate.Contains("on chart"),
                    $"Template for {t} should reference 'on chart'");
            }
        }

        [Test]
        public void InsiderTipDefinitions_EventCountTemplateContainsLiveCountdown()
        {
            var def = InsiderTipDefinitions.GetByType(InsiderTipType.EventCount);
            Assert.IsNotNull(def);
            Assert.IsTrue(def.Value.DescriptionTemplate.Contains("live countdown"),
                "EventCount template should contain 'live countdown'");
        }

        // === Animation constant relationship tests ===

        [Test]
        public void RerollStagger_FitsWithinFlipDuration()
        {
            float maxStaggerTime = ShopUI.RerollStaggerDelay * 3;
            Assert.Less(maxStaggerTime, ShopUI.RerollFlipDuration,
                "Stagger across 3 cards should complete before flip finishes");
        }

        [Test]
        public void TipFlashDuration_ShorterThanFlipDuration()
        {
            Assert.Less(ShopUI.TipFlashDuration, ShopUI.TipFlipDuration,
                "Flash effect should be shorter than the flip animation");
        }
    }
}
