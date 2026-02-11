using NUnit.Framework;

namespace BullRun.Tests.Core
{
    // Test event type
    public struct TestEvent
    {
        public int Value;
    }

    public struct AnotherTestEvent
    {
        public string Message;
    }

    [TestFixture]
    public class EventBusTests
    {
        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
        }

        [Test]
        public void Subscribe_AndPublish_CallsHandler()
        {
            int receivedValue = -1;
            EventBus.Subscribe<TestEvent>(e => receivedValue = e.Value);

            EventBus.Publish(new TestEvent { Value = 42 });

            Assert.AreEqual(42, receivedValue);
        }

        [Test]
        public void Publish_WithNoSubscribers_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => EventBus.Publish(new TestEvent { Value = 1 }));
        }

        [Test]
        public void Subscribe_MultipleHandlers_AllCalled()
        {
            int callCount = 0;
            EventBus.Subscribe<TestEvent>(e => callCount++);
            EventBus.Subscribe<TestEvent>(e => callCount++);

            EventBus.Publish(new TestEvent { Value = 1 });

            Assert.AreEqual(2, callCount);
        }

        [Test]
        public void Unsubscribe_RemovesHandler()
        {
            int callCount = 0;
            System.Action<TestEvent> handler = e => callCount++;
            EventBus.Subscribe(handler);

            EventBus.Publish(new TestEvent { Value = 1 });
            Assert.AreEqual(1, callCount);

            EventBus.Unsubscribe(handler);
            EventBus.Publish(new TestEvent { Value = 2 });
            Assert.AreEqual(1, callCount); // Should not have incremented
        }

        [Test]
        public void DifferentEventTypes_AreIndependent()
        {
            int testEventCount = 0;
            int anotherEventCount = 0;

            EventBus.Subscribe<TestEvent>(e => testEventCount++);
            EventBus.Subscribe<AnotherTestEvent>(e => anotherEventCount++);

            EventBus.Publish(new TestEvent { Value = 1 });

            Assert.AreEqual(1, testEventCount);
            Assert.AreEqual(0, anotherEventCount);
        }

        [Test]
        public void Clear_RemovesAllSubscriptions()
        {
            int callCount = 0;
            EventBus.Subscribe<TestEvent>(e => callCount++);
            EventBus.Subscribe<AnotherTestEvent>(e => callCount++);

            EventBus.Clear();

            EventBus.Publish(new TestEvent { Value = 1 });
            EventBus.Publish(new AnotherTestEvent { Message = "test" });

            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void Publish_IsSynchronous_HandlersRunImmediately()
        {
            int value = 0;
            EventBus.Subscribe<TestEvent>(e => value = e.Value);

            EventBus.Publish(new TestEvent { Value = 99 });
            // Value should already be set (synchronous dispatch)
            Assert.AreEqual(99, value);
        }
    }
}
