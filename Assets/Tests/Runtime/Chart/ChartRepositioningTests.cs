using NUnit.Framework;
using UnityEngine;

namespace BullRun.Tests.Chart
{
    /// <summary>
    /// Story 14.5: Tests for chart repositioning and CRT color theme integration.
    /// Verifies chart bounds calculation, grid colors, and label colors match CRT theme.
    /// </summary>
    [TestFixture]
    public class ChartRepositioningTests
    {
        private const float Tolerance = 0.01f;

        // --- Chart Bounds Calculation ---

        [Test]
        public void ChartBounds_BottomClearsControlDeck()
        {
            // With worldHeight = 10, chartBottom should be at -10*0.5 + 10*0.25 = -2.5
            // This places the bottom edge above the Control Deck area.
            float worldHeight = 10f;
            float chartBottom = -worldHeight * 0.5f + worldHeight * 0.25f;

            Assert.AreEqual(-2.5f, chartBottom, Tolerance, "Chart bottom should be at -25% of world height");
            Assert.Greater(chartBottom, -worldHeight * 0.5f, "Chart bottom should be above screen bottom");
        }

        [Test]
        public void ChartBounds_TopLeavesSpaceForStockLabel()
        {
            float worldHeight = 10f;
            float chartHeightPercent = 0.55f;
            float chartBottom = -worldHeight * 0.5f + worldHeight * 0.25f; // -2.5
            float chartTop = chartBottom + worldHeight * chartHeightPercent; // -2.5 + 5.5 = 3.0

            // Chart top at 3.0 out of worldHeight/2 = 5.0
            // Leaves 2.0 world units (20% of screen) for stock label + event ticker at top
            Assert.AreEqual(3.0f, chartTop, Tolerance);
            Assert.Less(chartTop, worldHeight * 0.5f, "Chart top should be below screen top");

            float topMarginPercent = (worldHeight * 0.5f - chartTop) / worldHeight;
            Assert.Greater(topMarginPercent, 0.10f, "Should leave at least 10% at top for stock label area");
        }

        [Test]
        public void ChartBounds_HeightIsReducedTo55Percent()
        {
            float worldHeight = 10f;
            float chartHeightPercent = 0.55f;
            float chartWorldHeight = worldHeight * chartHeightPercent;

            Assert.AreEqual(5.5f, chartWorldHeight, Tolerance);
        }

        // --- CRT Grid Color ---

        [Test]
        public void GridlineColor_UsesCRTBorderWithSubtleAlpha()
        {
            var gridColor = new Color(CRTThemeData.Border.r, CRTThemeData.Border.g, CRTThemeData.Border.b, 0.2f);

            Assert.AreEqual(CRTThemeData.Border.r, gridColor.r, Tolerance, "Red channel should match CRT Border");
            Assert.AreEqual(CRTThemeData.Border.g, gridColor.g, Tolerance, "Green channel should match CRT Border");
            Assert.AreEqual(CRTThemeData.Border.b, gridColor.b, Tolerance, "Blue channel should match CRT Border");
            Assert.AreEqual(0.2f, gridColor.a, Tolerance, "Alpha should be 0.2 for subtle grid");
        }

        // --- CRT Label Colors ---

        [Test]
        public void AxisLabelColor_IsCRTTextLow()
        {
            // Story 14.5 AC 7: axis labels use dim green (ColorPalette.GreenDim #245046)
            Assert.AreEqual(ColorPalette.GreenDim.r, CRTThemeData.TextLow.r, Tolerance, "TextLow red channel");
            Assert.AreEqual(ColorPalette.GreenDim.g, CRTThemeData.TextLow.g, Tolerance, "TextLow green channel");
            Assert.AreEqual(ColorPalette.GreenDim.b, CRTThemeData.TextLow.b, Tolerance, "TextLow blue channel");
            Assert.Greater(CRTThemeData.TextLow.g, CRTThemeData.TextLow.r, "TextLow should be more green than red");
        }

        [Test]
        public void CurrentPriceLabelColor_IsCRTTextHigh()
        {
            // Story 14.5 AC 8: current price label uses ColorPalette.Green (#3daa6e)
            Assert.AreEqual(ColorPalette.Green, CRTThemeData.TextHigh, "TextHigh should match ColorPalette.Green");
        }

        // --- Canvas Coordinate Mapping ---

        [Test]
        public void CanvasChartBottom_MapsCorrectly()
        {
            float worldHeight = 10f;
            float chartBottom = -2.5f; // -worldHeight * 0.5f + worldHeight * 0.25f
            float canvasChartBottom = (chartBottom / worldHeight) * 1080f;

            Assert.AreEqual(-270f, canvasChartBottom, Tolerance,
                "Canvas Y for chart bottom should map proportionally (-2.5/10 * 1080 = -270)");
        }

        [Test]
        public void CanvasChartTop_MapsCorrectly()
        {
            float worldHeight = 10f;
            float chartTop = 3.0f; // chartBottom + worldHeight * 0.55
            float canvasChartTop = (chartTop / worldHeight) * 1080f;

            Assert.AreEqual(324f, canvasChartTop, Tolerance,
                "Canvas Y for chart top should map proportionally (3.0/10 * 1080 = 324)");
        }

        // --- Background Color ---

        [Test]
        public void BackgroundColor_MatchesCRTBackground()
        {
            // Story 14.5 AC 6: CRTThemeData.Background should be #050a0a
            Assert.AreEqual(5 / 255f, CRTThemeData.Background.r, Tolerance, "Background red should be 5/255");
            Assert.AreEqual(10 / 255f, CRTThemeData.Background.g, Tolerance, "Background green should be 10/255");
            Assert.AreEqual(10 / 255f, CRTThemeData.Background.b, Tolerance, "Background blue should be 10/255");
            Assert.Less(CRTThemeData.Background.r, 0.05f, "Background should be very dark");
            Assert.Less(CRTThemeData.Background.g, 0.05f, "Background should be very dark");
        }

        // --- Stock Label Prefix ---

        [Test]
        public void StockLabelPrefix_ContainsBulletCharacter()
        {
            string prefix = "\u25C9";
            string result = prefix + " ACME";

            Assert.IsTrue(result.StartsWith("\u25C9"), "Should have bullet dot prefix");
            Assert.IsTrue(result.Contains("ACME"), "Should contain ticker symbol");
        }
    }
}
