using NUnit.Framework;

namespace BullRun.Tests.Items
{
    /// <summary>
    /// Story 17.1: Tests for RelicFactory — relic creation by ID.
    /// </summary>
    [TestFixture]
    public class RelicFactoryTests
    {
        [SetUp]
        public void SetUp()
        {
            RelicFactory.ResetRegistry();
        }

        [TearDown]
        public void TearDown()
        {
            RelicFactory.ResetRegistry();
        }

        [Test]
        public void Create_KnownId_ReturnsInstance()
        {
            var relic = RelicFactory.Create("relic_stop_loss");
            Assert.IsNotNull(relic);
            Assert.AreEqual("relic_stop_loss", relic.Id);
        }

        [Test]
        public void Create_UnknownId_ReturnsNull()
        {
            var relic = RelicFactory.Create("nonexistent_relic");
            Assert.IsNull(relic);
        }

        [Test]
        public void Create_NullId_ReturnsNull()
        {
            var relic = RelicFactory.Create(null);
            Assert.IsNull(relic);
        }

        [Test]
        public void Create_EmptyId_ReturnsNull()
        {
            var relic = RelicFactory.Create("");
            Assert.IsNull(relic);
        }

        [Test]
        public void Register_ThenCreate_ReturnsCustomInstance()
        {
            RelicFactory.Register("test_relic", () => new StubRelic("test_relic"));
            var relic = RelicFactory.Create("test_relic");
            Assert.IsNotNull(relic);
            Assert.AreEqual("test_relic", relic.Id);
        }

        [Test]
        public void ClearRegistry_ThenCreate_ReturnsNull()
        {
            RelicFactory.ClearRegistry();
            var relic = RelicFactory.Create("relic_stop_loss");
            Assert.IsNull(relic, "After ClearRegistry, no relics should be creatable");
        }

        [Test]
        public void AllPoolRelics_AreRegistered()
        {
            for (int i = 0; i < ShopItemDefinitions.RelicPool.Length; i++)
            {
                var id = ShopItemDefinitions.RelicPool[i].Id;
                var relic = RelicFactory.Create(id);
                Assert.IsNotNull(relic, $"Relic {id} should be registered in factory");
                Assert.AreEqual(id, relic.Id);
            }
        }

        [Test]
        public void Create_ReturnsFreshInstanceEachCall()
        {
            var a = RelicFactory.Create("relic_stop_loss");
            var b = RelicFactory.Create("relic_stop_loss");
            Assert.IsNotNull(a);
            Assert.IsNotNull(b);
            Assert.AreNotSame(a, b, "Each Create call should return a new instance");
        }
    }
}
