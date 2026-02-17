using NUnit.Framework;
using UnityEngine;

namespace BullRun.Tests.UI
{
    [TestFixture]
    public class TierVisualDataTests
    {
        // --- All four tiers have distinct themes ---

        [Test]
        public void GetTheme_Penny_ReturnsNonDefaultColors()
        {
            var theme = TierVisualData.GetTheme(StockTier.Penny);
            Assert.AreNotEqual(Color.clear, theme.AccentColor);
            Assert.AreNotEqual(Color.clear, theme.BackgroundTint);
            Assert.AreNotEqual(Color.clear, theme.ChartLineColor);
        }

        [Test]
        public void GetTheme_LowValue_ReturnsNonDefaultColors()
        {
            var theme = TierVisualData.GetTheme(StockTier.LowValue);
            Assert.AreNotEqual(Color.clear, theme.AccentColor);
            Assert.AreNotEqual(Color.clear, theme.BackgroundTint);
            Assert.AreNotEqual(Color.clear, theme.ChartLineColor);
        }

        [Test]
        public void GetTheme_MidValue_ReturnsNonDefaultColors()
        {
            var theme = TierVisualData.GetTheme(StockTier.MidValue);
            Assert.AreNotEqual(Color.clear, theme.AccentColor);
            Assert.AreNotEqual(Color.clear, theme.BackgroundTint);
            Assert.AreNotEqual(Color.clear, theme.ChartLineColor);
        }

        [Test]
        public void GetTheme_BlueChip_ReturnsNonDefaultColors()
        {
            var theme = TierVisualData.GetTheme(StockTier.BlueChip);
            Assert.AreNotEqual(Color.clear, theme.AccentColor);
            Assert.AreNotEqual(Color.clear, theme.BackgroundTint);
            Assert.AreNotEqual(Color.clear, theme.ChartLineColor);
        }

        // --- Each tier has distinct accent color ---

        [Test]
        public void AccentColors_AreDifferentPerTier()
        {
            var penny = TierVisualData.GetTheme(StockTier.Penny);
            var low = TierVisualData.GetTheme(StockTier.LowValue);
            var mid = TierVisualData.GetTheme(StockTier.MidValue);
            var blue = TierVisualData.GetTheme(StockTier.BlueChip);

            Assert.AreNotEqual(penny.AccentColor, low.AccentColor);
            Assert.AreNotEqual(penny.AccentColor, mid.AccentColor);
            Assert.AreNotEqual(penny.AccentColor, blue.AccentColor);
            Assert.AreNotEqual(low.AccentColor, mid.AccentColor);
            Assert.AreNotEqual(low.AccentColor, blue.AccentColor);
            Assert.AreNotEqual(mid.AccentColor, blue.AccentColor);
        }

        // --- Background tints are distinct ---

        [Test]
        public void BackgroundTints_AreDifferentPerTier()
        {
            var penny = TierVisualData.GetTheme(StockTier.Penny);
            var low = TierVisualData.GetTheme(StockTier.LowValue);
            var mid = TierVisualData.GetTheme(StockTier.MidValue);
            var blue = TierVisualData.GetTheme(StockTier.BlueChip);

            Assert.AreNotEqual(penny.BackgroundTint, low.BackgroundTint);
            Assert.AreNotEqual(penny.BackgroundTint, mid.BackgroundTint);
            Assert.AreNotEqual(penny.BackgroundTint, blue.BackgroundTint);
        }

        // --- Chart line colors are distinct ---

        [Test]
        public void ChartLineColors_AreDifferentPerTier()
        {
            var penny = TierVisualData.GetTheme(StockTier.Penny);
            var low = TierVisualData.GetTheme(StockTier.LowValue);
            var mid = TierVisualData.GetTheme(StockTier.MidValue);
            var blue = TierVisualData.GetTheme(StockTier.BlueChip);

            Assert.AreNotEqual(penny.ChartLineColor, low.ChartLineColor);
            Assert.AreNotEqual(penny.ChartLineColor, mid.ChartLineColor);
            Assert.AreNotEqual(penny.ChartLineColor, blue.ChartLineColor);
        }

        // --- Penny theme: neon green accent, dark purple tint ---

        [Test]
        public void Penny_AccentColor_IsNeonGreen()
        {
            var theme = TierVisualData.GetTheme(StockTier.Penny);
            // Penny accent = ColorPalette.Green (#3daa6e)
            Assert.AreEqual(ColorPalette.Green, theme.AccentColor);
        }

        [Test]
        public void Penny_BackgroundTint_IsDarkPurple()
        {
            var theme = TierVisualData.GetTheme(StockTier.Penny);
            // Dark purple tint — blue component > red, low overall brightness
            Assert.Greater(theme.BackgroundTint.b, theme.BackgroundTint.r);
        }

        // --- Low-Value theme: amber/gold accents, dark blue tint ---

        [Test]
        public void LowValue_AccentColor_IsAmberGold()
        {
            var theme = TierVisualData.GetTheme(StockTier.LowValue);
            // LowValue accent = ColorPalette.Amber (#cc9400)
            Assert.AreEqual(ColorPalette.Amber, theme.AccentColor);
        }

        // --- Mid-Value theme: cyan/teal accents, navy tint ---

        [Test]
        public void MidValue_AccentColor_IsCyanTeal()
        {
            var theme = TierVisualData.GetTheme(StockTier.MidValue);
            // MidValue accent = ColorPalette.Cyan (#38a0b0)
            Assert.AreEqual(ColorPalette.Cyan, theme.AccentColor);
        }

        // --- Blue Chip theme: gold accents, deep black tint ---

        [Test]
        public void BlueChip_AccentColor_IsGold()
        {
            var theme = TierVisualData.GetTheme(StockTier.BlueChip);
            // BlueChip accent = ColorPalette.Gold (#ccac28)
            Assert.AreEqual(ColorPalette.Gold, theme.AccentColor);
        }

        [Test]
        public void BlueChip_BackgroundTint_IsDeepBlack()
        {
            var theme = TierVisualData.GetTheme(StockTier.BlueChip);
            // Deep black tint — very low RGB values
            Assert.Less(theme.BackgroundTint.r, 0.1f);
            Assert.Less(theme.BackgroundTint.g, 0.1f);
            Assert.Less(theme.BackgroundTint.b, 0.15f);
        }

        // --- ChartVisualConfig generation ---

        [Test]
        public void ToChartVisualConfig_SetsLineColor()
        {
            var theme = TierVisualData.GetTheme(StockTier.Penny);
            var config = TierVisualData.ToChartVisualConfig(theme);
            Assert.AreEqual(theme.ChartLineColor, config.LineColor);
        }

        [Test]
        public void ToChartVisualConfig_GlowColorMatchesLineWithLowerAlpha()
        {
            var theme = TierVisualData.GetTheme(StockTier.MidValue);
            var config = TierVisualData.ToChartVisualConfig(theme);
            Assert.AreEqual(theme.ChartLineColor.r, config.GlowColor.r, 0.01f);
            Assert.AreEqual(theme.ChartLineColor.g, config.GlowColor.g, 0.01f);
            Assert.AreEqual(theme.ChartLineColor.b, config.GlowColor.b, 0.01f);
            Assert.Less(config.GlowColor.a, config.LineColor.a);
        }

        // --- GetThemeForAct convenience ---

        [Test]
        public void GetThemeForAct_Act1_ReturnsPennyTheme()
        {
            var fromAct = TierVisualData.GetThemeForAct(1);
            var fromTier = TierVisualData.GetTheme(StockTier.Penny);
            Assert.AreEqual(fromTier.AccentColor, fromAct.AccentColor);
        }

        [Test]
        public void GetThemeForAct_Act4_ReturnsBlueChipTheme()
        {
            var fromAct = TierVisualData.GetThemeForAct(4);
            var fromTier = TierVisualData.GetTheme(StockTier.BlueChip);
            Assert.AreEqual(fromTier.AccentColor, fromAct.AccentColor);
        }
    }
}
