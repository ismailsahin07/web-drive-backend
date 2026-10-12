using Drive.SharedKernel.UnitTests.TestDoubles;

namespace Drive.SharedKernel.UnitTests.Domain
{
    public sealed class AggregateRootTests
    {
        private static readonly DateTimeOffset Now = new(2026, 10, 12, 9, 0, 0, TimeSpan.Zero);

        [Fact]
        public void NewAggregate_HasNoEvents_AndNoConcurrencyToken()
        {
            var aggregate = new TestAggregate(TestId.New());

            aggregate.DomainEvents.ShouldBeEmpty();
            aggregate.ConcurrencyToken.ShouldBeNull();
        }

        [Fact]
        public void Raise_RecordsDomainEvent()
        {
            var aggregate = new TestAggregate(TestId.New());

            aggregate.DoSomething(Now);

            var domainEvent = aggregate.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<TestDomainEvent>();
            domainEvent.OccurredAt.ShouldBe(Now);
        }

        [Fact]
        public void Raise_WithNullEvent_Throws()
        {
            var aggregate = new TestAggregate(TestId.New());

            Should.Throw<ArgumentNullException>(() => aggregate.RaiseEvent(null!));
        }

        [Fact]
        public void PullDomainEvents_ReturnsEventsInOrder_AndClearsThem()
        {
            var aggregate = new TestAggregate(TestId.New());
            aggregate.DoSomething(Now);
            aggregate.DoSomething(Now.AddMinutes(1));

            var pulled = aggregate.PullDomainEvents();

            pulled.Count.ShouldBe(2);
            pulled[0].OccurredAt.ShouldBe(Now);
            pulled[1].OccurredAt.ShouldBe(Now.AddMinutes(1));
            aggregate.DomainEvents.ShouldBeEmpty();
        }

        [Fact]
        public void PullDomainEvents_CalledTwice_ReturnsEventsOnlyOnce()
        {
            var aggregate = new TestAggregate(TestId.New());
            aggregate.DoSomething(Now);

            aggregate.PullDomainEvents();
            var second = aggregate.PullDomainEvents();

            second.ShouldBeEmpty();
        }

        [Fact]
        public void SetConcurrencyToken_StoresToken()
        {
            var aggregate = new TestAggregate(TestId.New());

            aggregate.SetConcurrencyToken("\"00000000-0000-0000-0000-000000000001\"");

            aggregate.ConcurrencyToken.ShouldBe("\"00000000-0000-0000-0000-000000000001\"");
        }
    }
}
