using NUnit.Framework;

namespace BullRun.Tests.UI
{
    [TestFixture]
    public class PositionPanelTests
    {
        private Portfolio _portfolio;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            _portfolio = new Portfolio(10000f);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
        }

        // --- PositionPanelData Tests ---

        [Test]
        public void RefreshFromPortfolio_NoPositions_EmptyEntries()
        {
            var data = new PositionPanelData();
            data.RefreshFromPortfolio(_portfolio);

            Assert.AreEqual(0, data.EntryCount);
            Assert.IsTrue(data.IsEmpty);
        }

        [Test]
        public void RefreshFromPortfolio_WithLongPosition_CreatesEntry()
        {
            _portfolio.OpenPosition("MEME", 10, 2.50f);

            var data = new PositionPanelData();
            data.RefreshFromPortfolio(_portfolio);

            Assert.AreEqual(1, data.EntryCount);
            Assert.IsFalse(data.IsEmpty);
        }

        [Test]
        public void RefreshFromPortfolio_LongEntry_HasCorrectData()
        {
            _portfolio.OpenPosition("MEME", 10, 2.50f);

            var data = new PositionPanelData();
            data.RefreshFromPortfolio(_portfolio);

            var entry = data.GetEntry(0);
            Assert.AreEqual("MEME", entry.StockId);
            Assert.AreEqual(10, entry.Shares);
            Assert.AreEqual(2.50f, entry.AveragePrice, 0.001f);
            Assert.IsTrue(entry.IsLong);
            Assert.IsFalse(entry.IsShort);
        }

        [Test]
        public void RefreshFromPortfolio_ShortEntry_HasCorrectData()
        {
            _portfolio.OpenShort("YOLO", 5, 50f);

            var data = new PositionPanelData();
            data.RefreshFromPortfolio(_portfolio);

            var entry = data.GetEntry(0);
            Assert.AreEqual("YOLO", entry.StockId);
            Assert.AreEqual(5, entry.Shares);
            Assert.AreEqual(50f, entry.AveragePrice, 0.001f);
            Assert.IsFalse(entry.IsLong);
            Assert.IsTrue(entry.IsShort);
        }

        [Test]
        public void RefreshFromPortfolio_MultiplePositions()
        {
            _portfolio.OpenPosition("MEME", 10, 2.50f);
            _portfolio.OpenShort("YOLO", 5, 50f);
            _portfolio.OpenPosition("PUMP", 20, 0.45f);

            var data = new PositionPanelData();
            data.RefreshFromPortfolio(_portfolio);

            Assert.AreEqual(3, data.EntryCount);
        }

        [Test]
        public void RefreshFromPortfolio_AfterClosingPosition_EntryRemoved()
        {
            _portfolio.OpenPosition("MEME", 10, 2.50f);
            _portfolio.OpenPosition("YOLO", 5, 50f);
            _portfolio.ClosePosition("MEME", 10, 3.00f);

            var data = new PositionPanelData();
            data.RefreshFromPortfolio(_portfolio);

            Assert.AreEqual(1, data.EntryCount);
            Assert.AreEqual("YOLO", data.GetEntry(0).StockId);
        }

        // --- P&L Calculation ---

        [Test]
        public void UpdatePnL_LongPosition_Profit()
        {
            var entry = new PositionDisplayEntry("MEME", 10, 2.50f, true);
            entry.UpdatePnL(3.00f);

            Assert.AreEqual(5f, entry.UnrealizedPnL, 0.001f);
        }

        [Test]
        public void UpdatePnL_LongPosition_Loss()
        {
            var entry = new PositionDisplayEntry("MEME", 10, 2.50f, true);
            entry.UpdatePnL(2.00f);

            Assert.AreEqual(-5f, entry.UnrealizedPnL, 0.001f);
        }

        [Test]
        public void UpdatePnL_ShortPosition_Profit()
        {
            var entry = new PositionDisplayEntry("YOLO", 5, 50f, false);
            entry.UpdatePnL(45f);

            Assert.AreEqual(25f, entry.UnrealizedPnL, 0.001f);
        }

        [Test]
        public void UpdatePnL_ShortPosition_Loss()
        {
            var entry = new PositionDisplayEntry("YOLO", 5, 50f, false);
            entry.UpdatePnL(55f);

            Assert.AreEqual(-25f, entry.UnrealizedPnL, 0.001f);
        }

        // --- Color Logic ---

        [Test]
        public void GetPositionTypeColor_Long_ReturnsGreen()
        {
            Assert.AreEqual(PositionPanel.LongAccentColor, PositionPanel.GetPositionTypeColor(true));
        }

        [Test]
        public void GetPositionTypeColor_Short_ReturnsPink()
        {
            Assert.AreEqual(PositionPanel.ShortAccentColor, PositionPanel.GetPositionTypeColor(false));
        }

        [Test]
        public void GetPnLColor_Positive_ReturnsGreen()
        {
            Assert.AreEqual(PositionPanel.ProfitGreen, PositionPanel.GetPnLColor(10f));
        }

        [Test]
        public void GetPnLColor_Negative_ReturnsRed()
        {
            Assert.AreEqual(PositionPanel.LossRed, PositionPanel.GetPnLColor(-5f));
        }

        [Test]
        public void GetPnLColor_Zero_ReturnsWhite()
        {
            Assert.AreEqual(UnityEngine.Color.white, PositionPanel.GetPnLColor(0f));
        }

        // --- Format ---

        [Test]
        public void FormatPositionType_Long()
        {
            Assert.AreEqual("LONG", PositionPanel.FormatPositionType(true));
        }

        [Test]
        public void FormatPositionType_Short()
        {
            Assert.AreEqual("SHORT", PositionPanel.FormatPositionType(false));
        }

        // --- Short Squeeze Warning Color (Story 5-5) ---

        [Test]
        public void ShortSqueezeWarningColor_IsRed()
        {
            Assert.AreEqual(1f, PositionPanel.ShortSqueezeWarningColor.r, 0.01f);
            Assert.AreEqual(0f, PositionPanel.ShortSqueezeWarningColor.g, 0.01f);
            Assert.AreEqual(0f, PositionPanel.ShortSqueezeWarningColor.b, 0.01f);
        }
    }
}
