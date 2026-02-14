using NUnit.Framework;

namespace BullRun.Tests.Chart
{
    [TestFixture]
    public class ChartGridlineTests
    {
        // Helper: compute 10% padded bounds from raw chart bounds (matches LateUpdate padding)
        private static void ComputePaddedBounds(float chartBottom, float chartTop,
            out float paddedBottom, out float paddedTop)
        {
            float chartHeight = chartTop - chartBottom;
            float padding = chartHeight * 0.1f;
            paddedBottom = chartBottom + padding;
            paddedTop = chartTop - padding;
        }

        // --- Gridline Y Position Calculation ---

        [Test]
        public void CalculateGridlineYPositions_ReturnsCorrectCount()
        {
            ComputePaddedBounds(-3.5f, 3.5f, out float pb, out float pt);

            var positions = ChartLineView.CalculateGridlineYPositions(pb, pt, 5);

            Assert.AreEqual(5, positions.Length);
        }

        [Test]
        public void CalculateGridlineYPositions_EvenlySpaced()
        {
            ComputePaddedBounds(-3.5f, 3.5f, out float pb, out float pt);

            var positions = ChartLineView.CalculateGridlineYPositions(pb, pt, 5);

            for (int i = 2; i < positions.Length; i++)
            {
                float step = positions[i] - positions[i - 1];
                float expectedStep = positions[1] - positions[0];
                Assert.AreEqual(expectedStep, step, 0.001f,
                    $"Step between position {i - 1} and {i} should be uniform");
            }
        }

        [Test]
        public void CalculateGridlineYPositions_WithinPaddedBounds()
        {
            ComputePaddedBounds(-3.5f, 3.5f, out float paddedBottom, out float paddedTop);

            var positions = ChartLineView.CalculateGridlineYPositions(paddedBottom, paddedTop, 5);

            Assert.AreEqual(paddedBottom, positions[0], 0.001f, "First gridline should be at padded bottom");
            Assert.AreEqual(paddedTop, positions[4], 0.001f, "Last gridline should be at padded top");
        }

        [Test]
        public void CalculateGridlineYPositions_SingleLine_AtPaddedBottom()
        {
            ComputePaddedBounds(-3.5f, 3.5f, out float paddedBottom, out float paddedTop);

            var positions = ChartLineView.CalculateGridlineYPositions(paddedBottom, paddedTop, 1);

            Assert.AreEqual(1, positions.Length);
            Assert.AreEqual(paddedBottom, positions[0], 0.001f);
        }

        [Test]
        public void CalculateGridlineYPositions_ZeroCount_ReturnsEmptyArray()
        {
            ComputePaddedBounds(-3.5f, 3.5f, out float pb, out float pt);

            var positions = ChartLineView.CalculateGridlineYPositions(pb, pt, 0);

            Assert.AreEqual(0, positions.Length);
        }

        // --- Gridline-to-AxisLabel Alignment Verification (Task 4) ---

        [Test]
        public void GridlineYPositions_MatchAxisLabelYMapping()
        {
            float chartBottom = -3.5f;
            float chartTop = 3.5f;
            float minPrice = 2.00f;
            float maxPrice = 4.00f;
            float priceRange = maxPrice - minPrice;
            int labelCount = 5;

            ComputePaddedBounds(chartBottom, chartTop, out float paddedBottom, out float paddedTop);

            var labelPrices = ChartUI.CalculateAxisLabels(minPrice, maxPrice, labelCount);
            var gridlineYs = ChartLineView.CalculateGridlineYPositions(paddedBottom, paddedTop, labelCount);

            for (int i = 0; i < labelCount; i++)
            {
                float expectedY = paddedBottom + (paddedTop - paddedBottom) * (labelPrices[i] - minPrice) / priceRange;
                Assert.AreEqual(expectedY, gridlineYs[i], 0.001f,
                    $"Gridline {i} Y position should match axis label {i} Y position");
            }
        }

        [Test]
        public void GridlineYPositions_MatchAxisLabels_LargeRange()
        {
            float chartBottom = -5f;
            float chartTop = 5f;
            float minPrice = 10f;
            float maxPrice = 500f;
            float priceRange = maxPrice - minPrice;
            int labelCount = 5;

            ComputePaddedBounds(chartBottom, chartTop, out float paddedBottom, out float paddedTop);

            var labelPrices = ChartUI.CalculateAxisLabels(minPrice, maxPrice, labelCount);
            var gridlineYs = ChartLineView.CalculateGridlineYPositions(paddedBottom, paddedTop, labelCount);

            for (int i = 0; i < labelCount; i++)
            {
                float expectedY = paddedBottom + (paddedTop - paddedBottom) * (labelPrices[i] - minPrice) / priceRange;
                Assert.AreEqual(expectedY, gridlineYs[i], 0.001f,
                    $"Gridline {i} should align with axis label at price {labelPrices[i]}");
            }
        }

        [Test]
        public void GridlineYPositions_MatchAxisLabels_PennyStockRange()
        {
            float chartBottom = -3.5f;
            float chartTop = 3.5f;
            float minPrice = 0.10f;
            float maxPrice = 0.50f;
            float priceRange = maxPrice - minPrice;
            int labelCount = 5;

            ComputePaddedBounds(chartBottom, chartTop, out float paddedBottom, out float paddedTop);

            var labelPrices = ChartUI.CalculateAxisLabels(minPrice, maxPrice, labelCount);
            var gridlineYs = ChartLineView.CalculateGridlineYPositions(paddedBottom, paddedTop, labelCount);

            for (int i = 0; i < labelCount; i++)
            {
                float expectedY = paddedBottom + (paddedTop - paddedBottom) * (labelPrices[i] - minPrice) / priceRange;
                Assert.AreEqual(expectedY, gridlineYs[i], 0.001f,
                    $"Gridline {i} should align with axis label at price {labelPrices[i]}");
            }
        }
    }
}
