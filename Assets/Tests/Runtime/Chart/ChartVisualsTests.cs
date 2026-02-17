using NUnit.Framework;

namespace BullRun.Tests.Chart
{
    [TestFixture]
    public class ChartVisualsTests
    {
        // --- ChartVisualConfig Tests ---

        [Test]
        public void ChartVisualConfig_DefaultLineColor_IsNeonGreen()
        {
            var config = ChartVisualConfig.Default;

            // Default line color = ColorPalette.Green (#3daa6e)
            Assert.AreEqual(ColorPalette.Green, config.LineColor);
        }

        [Test]
        public void ChartVisualConfig_GlowColor_IsSameAsLineWithLowerAlpha()
        {
            var config = ChartVisualConfig.Default;

            Assert.AreEqual(config.LineColor.r, config.GlowColor.r, 0.01f);
            Assert.AreEqual(config.LineColor.g, config.GlowColor.g, 0.01f);
            Assert.AreEqual(config.LineColor.b, config.GlowColor.b, 0.01f);
            Assert.Less(config.GlowColor.a, config.LineColor.a,
                "Glow should have lower alpha than main line");
            Assert.AreEqual(0.3f, config.GlowColor.a, 0.05f);
        }

        [Test]
        public void ChartVisualConfig_GlowWidth_IsWiderThanLineWidth()
        {
            var config = ChartVisualConfig.Default;

            Assert.Greater(config.GlowWidthPixels, config.LineWidthPixels,
                "Glow trail should be wider than main line");
        }

        [Test]
        public void ChartVisualConfig_HasIndicatorSize()
        {
            var config = ChartVisualConfig.Default;

            Assert.Greater(config.IndicatorSize, 0f,
                "Current price indicator should have a positive size");
        }
    }
}
