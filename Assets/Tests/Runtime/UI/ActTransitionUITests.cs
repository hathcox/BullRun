using NUnit.Framework;

namespace BullRun.Tests.UI
{
    [TestFixture]
    public class ActTransitionUITests
    {
        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
        }

        // --- GetHeaderText ---

        [Test]
        public void GetHeaderText_Act2_FormatsCorrectly()
        {
            string header = ActTransitionUI.GetHeaderText(2);
            Assert.AreEqual("ACT 2", header);
        }

        [Test]
        public void GetHeaderText_Act1_FormatsCorrectly()
        {
            string header = ActTransitionUI.GetHeaderText(1);
            Assert.AreEqual("ACT 1", header);
        }

        [Test]
        public void GetHeaderText_Act4_FormatsCorrectly()
        {
            string header = ActTransitionUI.GetHeaderText(4);
            Assert.AreEqual("ACT 4", header);
        }

        // --- GetTierDisplayName ---

        [Test]
        public void GetTierDisplayName_Act1_ReturnsPennyStocks()
        {
            string name = ActTransitionUI.GetTierDisplayName(1);
            Assert.AreEqual("Penny Stocks", name);
        }

        [Test]
        public void GetTierDisplayName_Act2_ReturnsLowValueStocks()
        {
            string name = ActTransitionUI.GetTierDisplayName(2);
            Assert.AreEqual("Low-Value Stocks", name);
        }

        [Test]
        public void GetTierDisplayName_Act3_ReturnsMidValueStocks()
        {
            string name = ActTransitionUI.GetTierDisplayName(3);
            Assert.AreEqual("Mid-Value Stocks", name);
        }

        [Test]
        public void GetTierDisplayName_Act4_ReturnsBlueChips()
        {
            string name = ActTransitionUI.GetTierDisplayName(4);
            Assert.AreEqual("Blue Chips", name);
        }

        // --- BuildDisplayText ---

        [Test]
        public void BuildDisplayText_CombinesActAndTier()
        {
            string text = ActTransitionUI.BuildDisplayText(2);
            Assert.AreEqual("ACT 2 \u2014 Low-Value Stocks", text);
        }

        [Test]
        public void BuildDisplayText_Act3()
        {
            string text = ActTransitionUI.BuildDisplayText(3);
            Assert.AreEqual("ACT 3 \u2014 Mid-Value Stocks", text);
        }

        // --- Display duration ---

        [Test]
        public void DisplayDuration_IsBetween1And2Seconds()
        {
            Assert.GreaterOrEqual(ActTransitionUI.DisplayDuration, 1f);
            Assert.LessOrEqual(ActTransitionUI.DisplayDuration, 2f);
        }
    }
}
