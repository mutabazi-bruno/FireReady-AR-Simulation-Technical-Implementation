using FireReady.Core;
using NUnit.Framework;

namespace FireReady.Tests
{
    public class EventBusTests
    {
        struct TestEvent
        {
            public int Value;
        }

        [SetUp]
        public void SetUp() => EventBus.Clear();

        [TearDown]
        public void TearDown() => EventBus.Clear();

        [Test]
        public void Every_subscriber_receives_the_event()
        {
            int a = 0, b = 0;
            EventBus.Subscribe<TestEvent>(e => a = e.Value);
            EventBus.Subscribe<TestEvent>(e => b = e.Value * 2);

            EventBus.Publish(new TestEvent { Value = 3 });

            Assert.AreEqual(3, a);
            Assert.AreEqual(6, b);
        }

        [Test]
        public void Unsubscribed_handler_is_not_called()
        {
            int calls = 0;
            System.Action<TestEvent> handler = e => calls++;
            EventBus.Subscribe(handler);
            EventBus.Unsubscribe(handler);

            EventBus.Publish(new TestEvent());

            Assert.AreEqual(0, calls);
            Assert.AreEqual(0, EventBus.SubscriberCount<TestEvent>());
        }

        [Test]
        public void Publishing_with_no_subscribers_does_nothing()
        {
            Assert.DoesNotThrow(() => EventBus.Publish(new TestEvent { Value = 1 }));
        }
    }
}
