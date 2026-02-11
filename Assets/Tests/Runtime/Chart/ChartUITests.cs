using NUnit.Framework;

namespace BullRun.Tests.Chart
{
    [TestFixture]
    public class ChartUITests
    {
        private ChartUI _chartUI;
        private ChartRenderer _chartRenderer;

        [SetUp]
        public void SetUp()
        {
            _chartRenderer = new ChartRenderer();
            _chartUI = new ChartUI();
        }

        // --- Axis Label Calculation Tests ---

        [Test]
        public void CalculateAxisLabels_ReturnsLabelsForPriceRange()
        {
            var labels = ChartUI.CalculateAxisLabels(80f, 120f, 5);

            Assert.AreEqual(5, labels.Length);
            Assert.AreEqual(80f, labels[0], 0.01f);
            Assert.AreEqual(120f, labels[4], 0.01f);
        }

        [Test]
        public void CalculateAxisLabels_EvenlySpaced()
        {
            var labels = ChartUI.CalculateAxisLabels(100f, 200f, 5);

            float expectedStep = 25f;
            for (int i = 1; i < labels.Length; i++)
            {
                float step = labels[i] - labels[i - 1];
                Assert.AreEqual(expectedStep, step, 0.01f,
                    $"Step between label {i - 1} and {i} should be {expectedStep}");
            }
        }

        [Test]
        public void CalculateAxisLabels_SingleLabel_ReturnsMin()
        {
            var labels = ChartUI.CalculateAxisLabels(50f, 150f, 1);

            Assert.AreEqual(1, labels.Length);
            Assert.AreEqual(50f, labels[0], 0.01f);
        }

        [Test]
        public void CalculateAxisLabels_SameMinMax_ReturnsAllSameValue()
        {
            var labels = ChartUI.CalculateAxisLabels(100f, 100f, 3);

            foreach (var label in labels)
            {
                Assert.AreEqual(100f, label, 0.01f);
            }
        }

        // --- Time Progress Calculation ---

        [Test]
        public void CalculateTimeProgress_HalfElapsed_Returns05()
        {
            float progress = ChartUI.CalculateTimeProgress(30f, 60f);

            Assert.AreEqual(0.5f, progress, 0.001f);
        }

        [Test]
        public void CalculateTimeProgress_ZeroElapsed_ReturnsZero()
        {
            float progress = ChartUI.CalculateTimeProgress(0f, 60f);

            Assert.AreEqual(0f, progress, 0.001f);
        }

        [Test]
        public void CalculateTimeProgress_FullElapsed_ReturnsOne()
        {
            float progress = ChartUI.CalculateTimeProgress(60f, 60f);

            Assert.AreEqual(1f, progress, 0.001f);
        }

        [Test]
        public void CalculateTimeProgress_OverElapsed_ClampedToOne()
        {
            float progress = ChartUI.CalculateTimeProgress(90f, 60f);

            Assert.AreEqual(1f, progress, 0.001f);
        }

        [Test]
        public void CalculateTimeProgress_ZeroDuration_ReturnsZero()
        {
            float progress = ChartUI.CalculateTimeProgress(10f, 0f);

            Assert.AreEqual(0f, progress, 0.001f);
        }

        // --- Price Format ---

        [Test]
        public void FormatPrice_PennyRange_ShowsCents()
        {
            string formatted = ChartUI.FormatPrice(0.42f);

            Assert.AreEqual("$0.42", formatted);
        }

        [Test]
        public void FormatPrice_MidRange_ShowsDollars()
        {
            string formatted = ChartUI.FormatPrice(125.50f);

            Assert.AreEqual("$125.50", formatted);
        }

        [Test]
        public void FormatPrice_HighRange_ShowsThousands()
        {
            string formatted = ChartUI.FormatPrice(2500f);

            Assert.AreEqual("$2,500.00", formatted);
        }
    }
}
